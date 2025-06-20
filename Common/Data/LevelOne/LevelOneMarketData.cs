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
using QuantConnect.Securities;
using QuantConnect.Data.Market;

namespace QuantConnect.Data.LevelOne
{
    /// <summary>
    /// Provides real-time tracking of Level 1 market data (top-of-book) for a specific trading symbol.
    /// Updates include best bid/ask quotes and last trade executions.
    /// Publishes <see cref="Tick"/> updates to a shared <see cref="IDataAggregator"/> in a thread-safe manner.
    public class LevelOneMarketData
    {
        /// <summary>
        /// Occurs when a new tick is received, such as a last trade update or a change in bid/ask values.
        /// </summary>
        public event EventHandler<BaseDataEventArgs> BaseDataReceived;

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
        /// Gets the latest reported open interest value.
        /// </summary>
        public decimal OpenInterest { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LevelOneMarketData"/> class for a given symbol.
        /// </summary>
        /// <param name="symbol">The trading symbol to monitor.</param>
        public LevelOneMarketData(Symbol symbol)
        {
            Symbol = symbol;
            SymbolDateTimeZone = MarketHoursDatabase.FromDataFolder().GetExchangeHours(symbol.ID.Market, symbol, symbol.SecurityType).TimeZone;
        }

        /// <summary>
        /// Updates the best bid and ask prices and sizes.
        /// Constructs and publishes a quote <see cref="Tick"/> to the <see cref="IDataAggregator"/>.
        /// </summary>
        /// <param name="quoteDateTimeUtc">The UTC timestamp when the quote was received.</param>
        /// <param name="bidPrice">The best bid price.</param>
        /// <param name="bidSize">The size available at the best bid.</param>
        /// <param name="askPrice">The best ask price.</param>
        /// <param name="askSize">The size available at the best ask.</param>
        /// <param name="ignoreZeroSizeUpdates">
        /// If <c>true</c>, incoming updates with a size of 0 are treated as missing and will not overwrite
        /// the current known size. This is typically used for real-time streams to avoid data gaps.
        /// </param>
        public void UpdateQuote(DateTime? quoteDateTimeUtc, decimal? bidPrice, decimal? bidSize, decimal? askPrice, decimal? askSize, bool ignoreZeroSizeUpdates = true)
        {
            if (BestAskPrice == askPrice && BestAskSize == askSize && BestBidPrice == bidPrice && BestBidSize == bidSize)
            {
                return;
            }

            var isBidUpdated = TryResolvePriceSize(quoteDateTimeUtc, bidPrice, bidSize, BestBidPrice, BestBidSize, out var resolvedBidPrice, out var resolvedBidSize, ignoreZeroSizeUpdates);

            if (isBidUpdated)
            {
                BestBidPrice = resolvedBidPrice;
                BestBidSize = resolvedBidSize;
            }

            var isAskUpdated = TryResolvePriceSize(quoteDateTimeUtc, askPrice, askSize, BestAskPrice, BestAskSize, out var resolvedAskPrice, out var resolvedAskSize, ignoreZeroSizeUpdates);

            if (isAskUpdated)
            {
                BestAskPrice = resolvedAskPrice;
                BestAskSize = resolvedAskSize;
            }

            if (isBidUpdated || isAskUpdated)
            {
                var lastQuoteTick = new Tick(quoteDateTimeUtc.Value.ConvertFromUtc(SymbolDateTimeZone), Symbol, BestBidSize, BestBidPrice, BestAskSize, BestAskPrice);

                BaseDataReceived?.Invoke(this, new(lastQuoteTick));
            }
        }

        /// <summary>
        /// Updates the last trade price and size.
        /// Constructs and publishes a trade <see cref="Tick"/> to the <see cref="IDataAggregator"/>.
        /// </summary>
        /// <param name="tradeDateTimeUtc">The UTC timestamp when the trade occurred.</param>
        /// <param name="lastQuantity">The quantity of the last trade.</param>
        /// <param name="lastPrice">The price at which the last trade occurred.</param>
        /// <param name="saleCondition">Optional sale condition string.</param>
        /// <param name="exchange">Optional exchange identifier.</param>
        public void UpdateLastTrade(DateTime? tradeDateTimeUtc, decimal? lastQuantity, decimal? lastPrice, string saleCondition = "", string exchange = "")
        {
            if (!TryResolvePriceSize(tradeDateTimeUtc, lastPrice, lastQuantity, LastTradePrice, LastTradeSize, out var newPrice, out var newSize))
            {
                return;
            }

            LastTradePrice = newPrice;
            LastTradeSize = newSize;

            var lastTradeTick = new Tick(
                tradeDateTimeUtc.Value.ConvertFromUtc(SymbolDateTimeZone),
                Symbol,
                saleCondition,
                exchange,
                LastTradeSize,
                LastTradePrice);

            BaseDataReceived?.Invoke(this, new(lastTradeTick));
        }


        /// <summary>
        /// Updates the open interest value and publishes a corresponding <see cref="Tick"/>.
        /// </summary>
        /// <param name="openInterestDateTimeUtc">The UTC timestamp of the open interest update.</param>
        /// <param name="openInterest">The reported open interest value.</param>
        public void UpdateOpenInterest(DateTime? openInterestDateTimeUtc, decimal? openInterest)
        {
            if (openInterestDateTimeUtc.HasValue && openInterestDateTimeUtc.Value != default && !openInterest.HasValue)
            {
                return;
            }

            var openInterestTick = new Tick(openInterestDateTimeUtc.Value.ConvertFromUtc(SymbolDateTimeZone), Symbol, openInterest.Value);

            BaseDataReceived?.Invoke(this, new(openInterestTick));
        }

        /// <summary>
        /// Attempts to resolve the effective price and size values for a Level 1 market data update,
        /// using fallback values when current data is missing, zero, or invalid.
        /// </summary>
        /// <param name="dateTime">
        /// The timestamp of the incoming update. If provided and not default, the update will be ignored
        /// (e.g., used to filter out stale data).
        /// </param>
        /// <param name="price">The incoming price value, if available.</param>
        /// <param name="size">The incoming size value associated with the price, if available.</param>
        /// <param name="bestPrice">The last known valid price used as a fallback.</param>
        /// <param name="bestSize">The last known valid size used as a fallback.</param>
        /// <param name="newPrice">The resolved price value to be used in the update.</param>
        /// <param name="newSize">The resolved size value to be used in the update.</param>
        /// <param name="ignoreZeroSizeUpdates">
        /// If <c>true</c>, incoming updates with a size of 0 are treated as missing and will not overwrite
        /// the current known size. This is typically used for real-time streams to avoid data gaps.
        /// </param>
        /// <returns>
        /// <c>true</c> if a valid (resolved) price and size pair was determined; otherwise, <c>false</c>.
        /// </returns>
        private static bool TryResolvePriceSize(DateTime? dateTime, decimal? price, decimal? size, decimal bestPrice, decimal bestSize, out decimal newPrice, out decimal newSize, bool ignoreZeroSizeUpdates = true)
        {
            newPrice = default;
            newSize = default;
            if (!dateTime.HasValue || dateTime.Value == default)
            {
                return false;
            }

            if (size.HasValue && (!ignoreZeroSizeUpdates || size.Value != 0))
            {
                if (price.HasValue && price.Value != 0)
                {
                    newPrice = price.Value;
                    newSize = size.Value;
                    return true;

                }
                else if (bestPrice != 0)
                {
                    newPrice = bestPrice;
                    newSize = size.Value;
                    return true;
                }
            }
            else if (price.HasValue && price.Value != 0)
            {
                newPrice = price.Value;
                newSize = bestSize;
                return true;
            }
            return false;
        }
    }
}
