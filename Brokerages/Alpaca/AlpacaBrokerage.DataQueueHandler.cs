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
using NodaTime;
using QuantConnect.Brokerages.Alpaca.Markets;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Logging;
using QuantConnect.Packets;

namespace QuantConnect.Brokerages.Alpaca
{
    /// <summary>
    /// Alpaca Brokerage IDataQueueHandler implementation
    /// </summary>
    public partial class AlpacaBrokerage
    {
        /// <summary>
        /// The list of ticks received
        /// </summary>
        private readonly List<Tick> _ticks = new List<Tick>();

        #region IDataQueueHandler implementation

        /// <summary>
        /// Get the next ticks from the live trading data queue
        /// </summary>
        /// <returns>IEnumerable list of ticks since the last update.</returns>
        public IEnumerable<BaseData> GetNextTicks()
        {
            lock (_ticks)
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
        /// <param name="symbols">The symbols to be added keyed by SecurityType</param>
        public void Subscribe(LiveNodePacket job, IEnumerable<Symbol> symbols)
        {
            foreach (var symbol in symbols.Where(CanSubscribe))
            {
                Log.Trace($"AlpacaBrokerage.Subscribe(): {symbol}");

                _natsClient.SubscribeQuote(symbol.Value);
                _natsClient.SubscribeTrade(symbol.Value);
            }
        }

        /// <summary>
        /// Removes the specified symbols from the subscription
        /// </summary>
        /// <param name="job">Job we're processing.</param>
        /// <param name="symbols">The symbols to be removed keyed by SecurityType</param>
        public void Unsubscribe(LiveNodePacket job, IEnumerable<Symbol> symbols)
        {
            foreach (var symbol in symbols.Where(CanSubscribe))
            {
                Log.Trace($"AlpacaBrokerage.Unsubscribe(): {symbol}");

                _natsClient.UnsubscribeQuote(symbol.Value);
                _natsClient.UnsubscribeTrade(symbol.Value);
            }
        }

        /// <summary>
        /// Returns true if this brokerage supports the specified symbol
        /// </summary>
        private static bool CanSubscribe(Symbol symbol)
        {
            // ignore unsupported security types
            if (symbol.ID.SecurityType != SecurityType.Equity)
                return false;

            if (symbol.Value.ToLower().IndexOf("universe", StringComparison.Ordinal) != -1)
                return false;

            return true;
        }

        /// <summary>
        /// Event handler for streaming quote ticks
        /// </summary>
        /// <param name="quote">The data object containing the received tick</param>
        private void OnQuoteReceived(IStreamQuote quote)
        {
            var symbol = Symbol.Create(quote.Symbol, SecurityType.Equity, Market.USA);
            var time = quote.Time;

            // live ticks timestamps must be in exchange time zone
            DateTimeZone exchangeTimeZone;
            if (!_symbolExchangeTimeZones.TryGetValue(key: symbol, value: out exchangeTimeZone))
            {
                exchangeTimeZone = _marketHours.GetExchangeHours(Market.USA, symbol, SecurityType.Equity).TimeZone;
                _symbolExchangeTimeZones.Add(symbol, exchangeTimeZone);
            }
            time = time.ConvertFromUtc(exchangeTimeZone);

            var bidPrice = quote.BidPrice;
            var askPrice = quote.AskPrice;
            var tick = new Tick(time, symbol, bidPrice, bidPrice, askPrice)
            {
                TickType = TickType.Quote,
                BidSize = quote.BidSize,
                AskSize = quote.AskSize
            };

            lock (_ticks)
            {
                _ticks.Add(tick);
            }
        }

        /// <summary>
        /// Event handler for streaming trade ticks
        /// </summary>
        /// <param name="trade">The data object containing the received tick</param>
        private void OnTradeReceived(IStreamTrade trade)
        {
            var symbol = Symbol.Create(trade.Symbol, SecurityType.Equity, Market.USA);
            var time = trade.Time;

            // live ticks timestamps must be in exchange time zone
            DateTimeZone exchangeTimeZone;
            if (!_symbolExchangeTimeZones.TryGetValue(key: symbol, value: out exchangeTimeZone))
            {
                exchangeTimeZone = _marketHours.GetExchangeHours(Market.USA, symbol, SecurityType.Equity).TimeZone;
                _symbolExchangeTimeZones.Add(symbol, exchangeTimeZone);
            }
            time = time.ConvertFromUtc(exchangeTimeZone);

            var tick = new Tick(time, symbol, trade.Price, trade.Price, trade.Price)
            {
                TickType = TickType.Trade,
                Quantity = trade.Size
            };

            lock (_ticks)
            {
                _ticks.Add(tick);
            }
        }

        #endregion
    }
}
