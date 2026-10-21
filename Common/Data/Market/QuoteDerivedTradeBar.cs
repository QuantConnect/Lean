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
using QuantConnect.Data.Consolidators;

namespace QuantConnect.Data.Market
{
    /// <summary>
    /// Trade bar collapsed from a quote bar (mid-point of bid and ask) so trade bar consolidators and indicators
    /// can be fed from quote-only feeds, see <see cref="QuoteBarToTradeBarAdapter"/>. Quote data carries no trade
    /// volume, so reading or writing <see cref="Volume"/> throws instead of silently reporting zero.
    /// </summary>
    internal class QuoteDerivedTradeBar : TradeBar
    {
        /// <summary>
        /// Always throws: quote data carries no trade volume
        /// </summary>
        public override decimal Volume
        {
            get => throw NoVolume();
            set => throw NoVolume();
        }

        /// <summary>
        /// Creates a new bar with the given prices and no volume
        /// </summary>
        public QuoteDerivedTradeBar(DateTime time, Symbol symbol, decimal open, decimal high, decimal low, decimal close, TimeSpan? period = null)
        {
            Time = time;
            Symbol = symbol;
            Value = close;
            Open = open;
            High = high;
            Low = low;
            Close = close;
            Period = period ?? QuantConnect.Time.OneMinute;
            Initialized = 1;
        }

        /// <summary>
        /// Creates a new bar from the mid-point prices of the given quote bar
        /// </summary>
        public QuoteDerivedTradeBar(QuoteBar quoteBar)
            : this(quoteBar.Time, quoteBar.Symbol, quoteBar.Open, quoteBar.High, quoteBar.Low, quoteBar.Close, quoteBar.Period)
        {
        }

        /// <summary>
        /// Return a new instance clone of this object, used in fill forward
        /// </summary>
        public override BaseData Clone(bool fillForward)
        {
            // the trade bar implementation zeroes out the volume of fill forward bars, this bar has none
            var clone = (QuoteDerivedTradeBar)Clone();
            clone.IsFillForward = fillForward;
            return clone;
        }

        /// <summary>
        /// Formats a string with the symbol and the prices
        /// </summary>
        public override string ToString()
        {
            return $"{Symbol}: " +
                   $"O: {Open.SmartRounding()} " +
                   $"H: {High.SmartRounding()} " +
                   $"L: {Low.SmartRounding()} " +
                   $"C: {Close.SmartRounding()}";
        }

        private InvalidOperationException NoVolume()
        {
            return new InvalidOperationException($"The trade bar for {Symbol} was collapsed from a quote bar and has no volume: " +
                "the security only provides quote (bid/ask) data, which carries no trade volume. " +
                "Use a QuoteBar consolidator or an indicator that does not depend on volume.");
        }
    }
}
