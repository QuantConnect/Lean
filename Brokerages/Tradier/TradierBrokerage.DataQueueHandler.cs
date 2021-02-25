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
 *
*/

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NodaTime;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Packets;
using RestSharp;

namespace QuantConnect.Brokerages.Tradier
{
    /// <summary>
    /// Tradier Class: IDataQueueHandler implementation
    /// </summary>
    public partial class TradierBrokerage
    {
        #region IDataQueueHandler implementation

        private const string WebSocketUrl = "wss://ws.tradier.com/v1/markets/events";
        private const int ConnectionTimeout = 30000;

        private readonly WebSocketClientWrapper _webSocketClient = new WebSocketClientWrapper();
        private TradierStreamSession _streamSession;
        private readonly ConcurrentDictionary<string, Symbol> _subscribedTickers = new ConcurrentDictionary<string, Symbol>();

        /// <summary>
        /// Sets the job we're subscribing for
        /// </summary>
        /// <param name="job">Job we're subscribing for</param>
        public void SetJob(LiveNodePacket job)
        {
        }

        /// <summary>
        /// Subscribe to the specified configuration
        /// </summary>
        /// <param name="dataConfig">defines the parameters to subscribe to a data feed</param>
        /// <param name="newDataAvailableHandler">handler to be fired on new data available</param>
        /// <returns>The new enumerator for this subscription request</returns>
        public IEnumerator<BaseData> Subscribe(SubscriptionDataConfig dataConfig, EventHandler newDataAvailableHandler)
        {
            // streaming is not supported by sandbox
            if (_useSandbox)
            {
                throw new NotSupportedException(
                    "TradierBrokerage.DataQueueHandler.Subscribe(): The sandbox does not support data streaming.");
            }

            if (!CanSubscribe(dataConfig.Symbol))
            {
                return Enumerable.Empty<BaseData>().GetEnumerator();
            }

            var enumerator = _aggregator.Add(dataConfig, newDataAvailableHandler);
            _subscriptionManager.Subscribe(dataConfig);

            return enumerator;
        }

        private bool CanSubscribe(Symbol symbol)
        {
            return (symbol.ID.SecurityType == SecurityType.Equity || symbol.ID.SecurityType == SecurityType.Option)
                && !symbol.Value.Contains("-UNIVERSE-");
        }

        /// <summary>
        /// Removes the specified configuration
        /// </summary>
        /// <param name="dataConfig">Subscription config to be removed</param>
        public void Unsubscribe(SubscriptionDataConfig dataConfig)
        {
            _subscriptionManager.Unsubscribe(dataConfig);
            _aggregator.Remove(dataConfig);
        }

        private bool Subscribe(IEnumerable<Symbol> symbols, TickType tickType)
        {
            var symbolsAdded = false;

            foreach (var symbol in symbols)
            {
                if (!symbol.IsCanonical())
                {
                    var ticker = _symbolMapper.GetBrokerageSymbol(symbol);
                    if (!_subscribedTickers.ContainsKey(ticker))
                    {
                        _subscribedTickers.TryAdd(ticker, symbol);
                        symbolsAdded = true;
                    }
                }
            }

            if (symbolsAdded)
            {
                SendSubscribeMessage(_subscribedTickers.Keys.ToList());
            }

            return true;
        }

        private bool Unsubscribe(IEnumerable<Symbol> symbols, TickType tickType)
        {
            var symbolsRemoved = false;

            foreach (var symbol in symbols)
            {
                if (!symbol.IsCanonical())
                {
                    var ticker = _symbolMapper.GetBrokerageSymbol(symbol);
                    if (_subscribedTickers.ContainsKey(ticker))
                    {
                        Symbol removedSymbol;
                        _subscribedTickers.TryRemove(ticker, out removedSymbol);
                        symbolsRemoved = true;
                    }
                }
            }

            if (symbolsRemoved)
            {
                var tickers = _subscribedTickers.Keys.ToList();

                // Tradier expects at least one symbol
                SendSubscribeMessage(tickers.Count > 0
                    ? tickers
                    : new List<string> { "$empty$" });
            }

            return true;
        }

        private void SendSubscribeMessage(List<string> tickers)
        {
            var obj = new
            {
                sessionid = _streamSession.SessionId,
                symbols = tickers,
                filter = new[] { "trade", "quote" },
                linebreak = true
            };

            var json = JsonConvert.SerializeObject(obj);

            _webSocketClient.Send(json);
        }

        private void OnMessage(object sender, WebSocketMessage e)
        {
            var obj = JObject.Parse(e.Message);
            JToken error;
            if (obj.TryGetValue("error", out error))
            {
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Error, -1, error.Value<string>()));
                return;
            }

            var tsd = obj.ToObject<TradierStreamData>();

            if (tsd?.Type == "trade" || tsd?.Type == "quote")
            {
                var tick = CreateTick(tsd);
                if (tick != null)
                {
                    _aggregator.Update(tick);
                }
            }
        }

        /// <summary>
        /// Create a tick from the tradier stream data
        /// </summary>
        /// <param name="tsd">Tradier stream data object</param>
        /// <returns>LEAN Tick object</returns>
        private Tick CreateTick(TradierStreamData tsd)
        {
            Symbol symbol;
            if (!_subscribedTickers.TryGetValue(tsd.Symbol, out symbol))
            {
                // Not subscribed to this symbol.
                return null;
            }

            if (tsd.Type == "trade")
            {
                // Occasionally Tradier sends trades with 0 volume?
                if (tsd.TradeSize == 0) return null;
            }

            // Tradier trades are US NY time only. Convert local server time to NY Time:
            var utc = tsd.GetTickTimestamp();

            // Occasionally Tradier sends old ticks every 20sec-ish if no trading?
            if (DateTime.UtcNow - utc > TimeSpan.FromSeconds(10)) return null;

            // Convert the timestamp to exchange timezone and pass into algorithm
            var time = utc.ConvertTo(DateTimeZone.Utc, TimeZones.NewYork);

            switch (tsd.Type)
            {
                case "trade":
                    return new Tick(time, symbol, "", tsd.TradeExchange, (int) tsd.TradeSize, tsd.TradePrice);

                case "quote":
                    return new Tick(time, symbol, "", "", tsd.BidSize, tsd.BidPrice, tsd.AskSize, tsd.AskPrice);
            }

            return null;
        }

        /// <summary>
        /// Get the current market status
        /// </summary>
        private TradierStreamSession CreateStreamSession()
        {
            var request = new RestRequest("markets/events/session", Method.POST);
            return Execute<TradierStreamSession>(request, TradierApiRequestType.Data, "stream");
        }

        #endregion
    }
}
