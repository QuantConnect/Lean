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

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using NodaTime;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Packets;
using QuantConnect.Securities;
using QuantConnect.ToolBox.Polygon.Messages;

namespace QuantConnect.ToolBox.Polygon
{
    /// <summary>
    /// An implementation of <see cref="IDataQueueHandler"/> for Polygon.io
    /// </summary>
    public class PolygonDataQueueHandler : IDataQueueHandler
    {
        private readonly HashSet<Symbol> _subscribedSymbols = new HashSet<Symbol>();

        private readonly List<Tick> _ticks = new List<Tick>();
        private readonly object _locker = new object();

        private readonly Dictionary<SecurityType, PolygonWebSocketClientWrapper> _webSocketClientWrappers = new Dictionary<SecurityType, PolygonWebSocketClientWrapper>();
        private readonly PolygonSymbolMapper _symbolMapper = new PolygonSymbolMapper();
        private readonly MarketHoursDatabase _marketHoursDatabase = MarketHoursDatabase.FromDataFolder();

        // exchange time zones by symbol
        private readonly Dictionary<Symbol, DateTimeZone> _symbolExchangeTimeZones = new Dictionary<Symbol, DateTimeZone>();

        /// <summary>
        /// Initializes a new instance of the <see cref="PolygonDataQueueHandler"/> class
        /// </summary>
        public PolygonDataQueueHandler()
        {
            foreach (var securityType in new[] { SecurityType.Equity, SecurityType.Forex, SecurityType.Crypto })
            {
                var client = new PolygonWebSocketClientWrapper(_symbolMapper, securityType, OnMessage);
                _webSocketClientWrappers.Add(securityType, client);
            }
        }

        /// <summary>
        /// Indicates the connection is live.
        /// </summary>
        public bool IsConnected => _webSocketClientWrappers.Values.All(client => client.IsOpen);

        /// <summary>
        /// Get the next ticks from the live trading data queue
        /// </summary>
        /// <returns>IEnumerable list of ticks since the last update.</returns>
        public IEnumerable<BaseData> GetNextTicks()
        {
            lock (_locker)
            {
                var copy = _ticks.ToArray();
                _ticks.Clear();
                return copy;
            }
        }

        /// <summary>
        /// Adds the specified symbols to the subscription
        /// </summary>
        /// <param name="job">Job we're subscribing for:</param>
        /// <param name="symbols">The symbols to be added</param>
        public void Subscribe(LiveNodePacket job, IEnumerable<Symbol> symbols)
        {
            foreach (var symbol in symbols)
            {
                if (CanSubscribe(symbol) && !_subscribedSymbols.Contains(symbol))
                {
                    var webSocket = GetWebSocket(symbol.SecurityType);
                    webSocket.Subscribe(symbol, true);

                    _subscribedSymbols.Add(symbol);
                }
            }
        }

        /// <summary>
        /// Removes the specified symbols from the subscription
        /// </summary>
        /// <param name="job">Job we're processing.</param>
        /// <param name="symbols">The symbols to be removed</param>
        public void Unsubscribe(LiveNodePacket job, IEnumerable<Symbol> symbols)
        {
            foreach (var symbol in symbols)
            {
                if (CanSubscribe(symbol) && _subscribedSymbols.Contains(symbol))
                {
                    var webSocket = GetWebSocket(symbol.SecurityType);
                    webSocket.Subscribe(symbol, false);

                    _subscribedSymbols.Remove(symbol);
                }
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
        }

        private PolygonWebSocketClientWrapper GetWebSocket(SecurityType securityType)
        {
            PolygonWebSocketClientWrapper client;
            if (!_webSocketClientWrappers.TryGetValue(securityType, out client))
            {
                throw new Exception($"Unsupported security type: {securityType}");
            }

            return client;
        }

        private bool CanSubscribe(Symbol symbol)
        {
            var securityType = symbol.ID.SecurityType;

            if (symbol.Value.IndexOfInvariant("universe", true) != -1) return false;

            return
                securityType == SecurityType.Equity ||
                securityType == SecurityType.Forex ||
                securityType == SecurityType.Crypto;
        }

        private void OnMessage(string message)
        {
            foreach (var obj in JArray.Parse(message))
            {
                var eventType = obj["ev"].ToString();

                switch (eventType)
                {
                    case "T":
                        ProcessEquityTrade(obj.ToObject<EquityTradeMessage>());
                        break;

                    case "Q":
                        ProcessEquityQuote(obj.ToObject<EquityQuoteMessage>());
                        break;

                    case "C":
                        ProcessForexQuote(obj.ToObject<ForexQuoteMessage>());
                        break;

                    case "XT":
                        ProcessCryptoTrade(obj.ToObject<CryptoTradeMessage>());
                        break;

                    case "XQ":
                        ProcessCryptoQuote(obj.ToObject<CryptoQuoteMessage>());
                        break;
                }
            }
        }

        private void ProcessEquityTrade(EquityTradeMessage trade)
        {
            var symbol = _symbolMapper.GetLeanSymbol(trade.Symbol, SecurityType.Equity, Market.USA);
            var time = GetTickTime(symbol, trade.Timestamp);

            lock (_locker)
            {
                _ticks.Add(new Tick
                {
                    TickType = TickType.Trade,
                    Symbol = symbol,
                    Time = time,
                    Value = trade.Price,
                    Quantity = trade.Size
                });
            }
        }

        private void ProcessEquityQuote(EquityQuoteMessage quote)
        {
            var symbol = _symbolMapper.GetLeanSymbol(quote.Symbol, SecurityType.Equity, Market.USA);
            var time = GetTickTime(symbol, quote.Timestamp);

            lock (_locker)
            {
                _ticks.Add(new Tick
                {
                    TickType = TickType.Quote,
                    Symbol = symbol,
                    Time = time,
                    AskPrice = quote.AskPrice,
                    BidPrice = quote.BidPrice,
                    AskSize = quote.AskSize,
                    BidSize = quote.BidSize,
                    Value = (quote.AskPrice + quote.BidPrice) / 2m
                });
            }
        }

        private void ProcessForexQuote(ForexQuoteMessage quote)
        {
            var symbol = _symbolMapper.GetLeanSymbol(quote.Symbol, SecurityType.Forex, Market.FXCM);
            var time = GetTickTime(symbol, quote.Timestamp);

            lock (_locker)
            {
                _ticks.Add(new Tick
                {
                    TickType = TickType.Quote,
                    Symbol = symbol,
                    Time = time,
                    AskPrice = quote.AskPrice,
                    BidPrice = quote.BidPrice,
                    Value = (quote.AskPrice + quote.BidPrice) / 2m
                });
            }
        }

        private void ProcessCryptoTrade(CryptoTradeMessage trade)
        {
            var market = GetMarketFromCryptoExchangeId(trade.ExchangeId);
            if (string.IsNullOrWhiteSpace(market))
            {
                return;
            }

            var symbol = _symbolMapper.GetLeanSymbol(trade.Symbol, SecurityType.Crypto, market);
            var time = GetTickTime(symbol, trade.Timestamp);

            lock (_locker)
            {
                _ticks.Add(new Tick
                {
                    TickType = TickType.Trade,
                    Symbol = symbol,
                    Time = time,
                    Value = trade.Price,
                    Quantity = trade.Size
                });
            }
        }

        private void ProcessCryptoQuote(CryptoQuoteMessage quote)
        {
            var market = GetMarketFromCryptoExchangeId(quote.ExchangeId);
            if (string.IsNullOrWhiteSpace(market))
            {
                return;
            }

            var symbol = _symbolMapper.GetLeanSymbol(quote.Symbol, SecurityType.Crypto, market);
            var time = GetTickTime(symbol, quote.ExchangeTimestamp);

            lock (_locker)
            {
                _ticks.Add(new Tick
                {
                    TickType = TickType.Quote,
                    Symbol = symbol,
                    Time = time,
                    AskPrice = quote.AskPrice,
                    BidPrice = quote.BidPrice,
                    AskSize = quote.AskSize,
                    BidSize = quote.BidSize,
                    Value = (quote.AskPrice + quote.BidPrice) / 2m
                });
            }
        }

        private DateTime GetTickTime(Symbol symbol, long timestamp)
        {
            var utcTime = Time.UnixMillisecondTimeStampToDateTime(timestamp);

            DateTimeZone exchangeTimeZone;
            if (!_symbolExchangeTimeZones.TryGetValue(symbol, out exchangeTimeZone))
            {
                // read the exchange time zone from market-hours-database
                exchangeTimeZone = _marketHoursDatabase.GetExchangeHours(symbol.ID.Market, symbol, symbol.SecurityType).TimeZone;
                _symbolExchangeTimeZones.Add(symbol, exchangeTimeZone);
            }

            return utcTime.ConvertFromUtc(exchangeTimeZone);
        }

        private string GetMarketFromCryptoExchangeId(int exchangeId)
        {
            // Crypto exchanges from: https://api.polygon.io/v1/meta/crypto-exchanges?apiKey=xxx
            switch (exchangeId)
            {
                case 1:
                    return Market.GDAX;

                case 2:
                    return Market.Bitfinex;

                default:
                    return string.Empty;
            }
        }
    }
}
