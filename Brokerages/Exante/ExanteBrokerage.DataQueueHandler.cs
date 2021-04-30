/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using QuantConnect.Interfaces;
using System.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Exante.Net.Enums;
using Exante.Net.Objects;
using QuantConnect.Data;
using QuantConnect.Packets;
using NodaTime;
using QuantConnect.Data.Market;
using QuantConnect.Logging;
using QuantConnect.Util;

namespace QuantConnect.Brokerages.Exante
{
    public partial class ExanteBrokerage : IDataQueueHandler
    {
        private readonly ConcurrentDictionary<string, Symbol> _subscribedTickers =
            new ConcurrentDictionary<string, Symbol>();

        private readonly ConcurrentDictionary<string, (ExanteStreamSubscription, ExanteStreamSubscription)>
            _subscribedTickersStreamSubscriptions =
                new ConcurrentDictionary<string, (ExanteStreamSubscription, ExanteStreamSubscription)>();

        public IEnumerator<BaseData> Subscribe(SubscriptionDataConfig dataConfig, EventHandler newDataAvailableHandler)
        {
            if (!CanSubscribe(dataConfig.Symbol))
            {
                return Enumerable.Empty<BaseData>().GetEnumerator();
            }

            var enumerator = Aggregator.Add(dataConfig, newDataAvailableHandler);
            _subscriptionManager.Subscribe(dataConfig);

            return enumerator;
        }

        private bool CanSubscribe(Symbol symbol)
        {
            var supportedSecurityTypes = new HashSet<SecurityType>
            {
                SecurityType.Forex,
                SecurityType.Equity,
                SecurityType.Future,
                SecurityType.Option,
                SecurityType.Cfd,
                SecurityType.Index,
                SecurityType.Crypto,
            };

            // ignore unsupported security types
            if (!supportedSecurityTypes.Contains(symbol.ID.SecurityType))
            {
                return false;
            }

            // ignore universe symbols
            return !symbol.Value.Contains("-UNIVERSE-");
        }

        public void Unsubscribe(SubscriptionDataConfig dataConfig)
        {
            _subscriptionManager.Unsubscribe(dataConfig);
            Aggregator.Remove(dataConfig);
        }

        public void SetJob(LiveNodePacket job)
        {
        }

        public override void Dispose()
        {
            Aggregator.DisposeSafely();
        }

        private bool Subscribe(IEnumerable<Symbol> symbols, TickType tickType)
        {
            foreach (var symbol in symbols)
            {
                if (!symbol.IsCanonical())
                {
                    var ticker = _symbolMapper.GetBrokerageSymbol(symbol);
                    if (!_subscribedTickers.ContainsKey(ticker))
                    {
                        _subscribedTickers.TryAdd(ticker, symbol);
                        var feedQuoteStream = _client.StreamClient.GetFeedQuoteStreamAsync(
                            new[] {ticker},
                            tickShort =>
                            {
                                var tick = CreateTick(tickShort);
                                if (tick != null)
                                {
                                    Aggregator.Update(tick);
                                }
                            },
                            level: ExanteQuoteLevel.BestPrice).SynchronouslyAwaitTaskResult();
                        if (!feedQuoteStream.Success)
                        {
                            Log.Error(
                                $"Exante.StreamClient.GetFeedQuoteStreamAsync({ticker}): " +
                                $"Error: {feedQuoteStream.Error}"
                            );
                        }

                        var feedTradesStream = _client.StreamClient.GetFeedTradesStreamAsync(
                            new[] {ticker},
                            feedTrade =>
                            {
                                var tick = CreateTick(feedTrade);
                                if (tick != null)
                                {
                                    Aggregator.Update(tick);
                                }
                            }).SynchronouslyAwaitTaskResult();
                        if (!feedTradesStream.Success)
                        {
                            Log.Error(
                                $"Exante.StreamClient.GetFeedTradesStreamAsync({ticker}): " +
                                $"Error: {feedTradesStream.Error}"
                            );
                        }

                        _subscribedTickersStreamSubscriptions[ticker] = (feedQuoteStream.Data, feedTradesStream.Data);
                    }
                }
            }

            return true;
        }

        private Tick CreateTick(ExanteFeedTrade et)
        {
            var symbolId = et.SymbolId;
            if (!_subscribedTickers.TryGetValue(symbolId, out var symbol))
            {
                // Not subscribed to this symbol.
                return null;
            }

            if (et.Size == decimal.Zero)
            {
                return null;
            }

            var utc = et.Date;
            // Convert the timestamp to exchange timezone and pass into algorithm
            var time = utc.ConvertTo(DateTimeZone.Utc, TimeZones.NewYork);

            var instrument = _client.GetSymbol(symbolId);

            var size = et.Size ?? 0m;
            var price = et.Price ?? 0m;
            return new Tick(time, symbol, "", instrument.Data.Exchange, size, price);
        }

        private Tick CreateTick(ExanteTickShort ets)
        {
            if (!_subscribedTickers.TryGetValue(ets.SymbolId, out var symbol))
            {
                // Not subscribed to this symbol.
                return null;
            }

            // Tradier trades are US NY time only. Convert local server time to NY Time:
            var utc = ets.Date;

            // Convert the timestamp to exchange timezone and pass into algorithm
            var time = utc.ConvertTo(DateTimeZone.Utc, TimeZones.NewYork);

            var bids = ets.Bid.ToList();
            var asks = ets.Ask.ToList();
            return new Tick(time, symbol, "", "",
                bids.IsNullOrEmpty() ? decimal.Zero : bids[0].Size,
                bids.IsNullOrEmpty() ? decimal.Zero : bids[0].Price,
                asks.IsNullOrEmpty() ? decimal.Zero : asks[0].Size,
                asks.IsNullOrEmpty() ? decimal.Zero : asks[0].Price);
        }

        private bool Unsubscribe(IEnumerable<Symbol> symbols, TickType tickType)
        {
            foreach (var symbol in symbols)
            {
                if (!symbol.IsCanonical())
                {
                    var ticker = _symbolMapper.GetBrokerageSymbol(symbol);
                    if (_subscribedTickers.ContainsKey(ticker))
                    {
                        _subscribedTickers.TryRemove(ticker, out _);
                    }

                    if (_subscribedTickersStreamSubscriptions.ContainsKey(ticker))
                    {
                        _subscribedTickersStreamSubscriptions.TryRemove(ticker,
                            out (ExanteStreamSubscription stream1, ExanteStreamSubscription stream2) streams);
                        _client.StreamClient.StopStream(streams.stream1);
                        _client.StreamClient.StopStream(streams.stream2);
                    }
                }
            }

            return true;
        }
    }
}
