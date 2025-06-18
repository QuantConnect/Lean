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
using NodaTime;
using QuantConnect.Data;
using QuantConnect.Securities;
using QuantConnect.Data.Market;

namespace QuantConnect.Brokerages
{
    /// <summary>
    /// Provides real-time tracking of Level 1 market data (top-of-book) for a specific trading symbol.
    /// Publishes updates to an <see cref="IDataAggregator"/> when quotes or trades are updated.
    /// </summary>
    public class LevelOneService
    {
        /// <summary>
        /// Occurs when a new tick is received, such as a last trade update or a change in bid/ask values.
        /// </summary>
        public event EventHandler<TickEventArgs> TickReceived;

        /// <summary>
        /// Gets the symbol this service is tracking.
        /// </summary>
        public Symbol Symbol { get; }

        /// <summary>
        /// Gets the time zone associated with the symbol's exchange.
        /// Used for consistent time stamping.
        /// </summary>
        public DateTimeZone SymbolDateTimeZone { get; }

        /// <summary>
        /// Gets the price of the last executed trade.
        /// </summary>
        public decimal LastTradePrice { get; private set; }

        /// <summary>
        /// Gets the size of the last executed trade.
        /// </summary>
        public decimal LastTradeSize { get; private set; }

        /// <summary>
        /// Gets the best available bid price.
        /// </summary>
        public decimal BestBidPrice { get; private set; }

        /// <summary>
        /// Gets the size of the best available bid.
        /// </summary>
        public decimal BestBidSize { get; private set; }

        /// <summary>
        /// Gets the best available ask price.
        /// </summary>
        public decimal BestAskPrice { get; private set; }

        /// <summary>
        /// Gets the size of the best available ask.
        /// </summary>
        public decimal BestAskSize { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LevelOneService"/> class for the specified symbol.
        /// </summary>
        /// <param name="symbol">The trading symbol to track.</param>
        /// <param name="tickHandler"></param>
        public LevelOneService(Symbol symbol, EventHandler<TickEventArgs> tickHandler = null)
        {
            Symbol = symbol;
            SymbolDateTimeZone = MarketHoursDatabase.FromDataFolder().GetExchangeHours(symbol.ID.Market, symbol, symbol.SecurityType).TimeZone;
            TickReceived += tickHandler;
        }

        /// <summary>
        /// Updates the best bid and ask prices and sizes.
        /// Sends a new quote <see cref="Tick"/> to the <see cref="IDataAggregator"/> if the quote has changed.
        /// </summary>
        /// <param name="quoteDateTimeUtc">The UTC time of the quote update.</param>
        /// <param name="bidPrice">The new best bid price.</param>
        /// <param name="bidSize">The size at the new best bid price.</param>
        /// <param name="askPrice">The new best ask price.</param>
        /// <param name="askSize">The size at the new best ask price.</param>
        public void UpdateQuote(DateTime quoteDateTimeUtc, decimal bidPrice, decimal bidSize, decimal askPrice, decimal askSize)
        {
            BestBidPrice = bidPrice;
            BestBidSize = bidSize;
            BestAskPrice = askPrice;
            BestAskSize = askSize;

            var lastQuoteTick = new Tick(quoteDateTimeUtc.ConvertFromUtc(SymbolDateTimeZone), Symbol, BestBidSize, BestBidPrice, BestAskSize, BestAskPrice);

            TickReceived?.Invoke(this, new TickEventArgs(lastQuoteTick));
        }

        /// <summary>
        /// Updates the last trade information.
        /// Sends a trade <see cref="Tick"/> to the <see cref="IDataAggregator"/>.
        /// </summary>
        /// <param name="tradeDateTimeUtc">The UTC time of the trade execution.</param>
        /// <param name="lastQuantity">The size of the last trade.</param>
        /// <param name="lastPrice">The price of the last trade.</param>
        /// <param name="saleCondition">Optional sale condition code.</param>
        /// <param name="exchange">Optional exchange code where the trade occurred.</param>
        public void UpdateLastTrade(DateTime tradeDateTimeUtc, decimal lastQuantity, decimal lastPrice, string saleCondition = "", string exchange = "")
        {
            LastTradePrice = lastPrice;
            LastTradeSize = lastQuantity;

            var lastTradeTick = new Tick(
                tradeDateTimeUtc.ConvertFromUtc(SymbolDateTimeZone),
                Symbol,
                saleCondition,
                exchange,
                LastTradeSize,
                LastTradePrice);

            TickReceived?.Invoke(this, new TickEventArgs(lastTradeTick));
        }
    }
}
