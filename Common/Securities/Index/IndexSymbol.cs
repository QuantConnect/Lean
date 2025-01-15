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

namespace QuantConnect.Securities.Index
{
    /// <summary>
    /// Helper methods for Index Symbols
    /// </summary>
    public static class IndexSymbol
    {
        private static readonly Dictionary<string, string> _indexExchange = new(StringComparer.InvariantCultureIgnoreCase)
        {
            { "SPX", Market.CBOE },
            { "NDX", "NASDAQ" },
            { "VIX", Market.CBOE },
            { "SPXW", Market.CBOE },
            { "NQX", "NASDAQ" },
            { "VIXW", Market.CBOE },
            { "RUT", "RUSSELL" }
        };

        private static readonly Dictionary<string, string> _indexMarket = new(StringComparer.InvariantCultureIgnoreCase)
        {
            { "HSI", Market.HKFE },
            { "N225", Market.OSE },
            { "SX5E", Market.EUREX }
        };

        /// <summary>
        /// Gets the actual exchange the index lives on
        /// </summary>
        /// <remarks>Useful for live trading</remarks>
        /// <returns>The exchange of the index</returns>
        public static string GetIndexExchange(Symbol symbol)
        {
            return _indexExchange.TryGetValue(symbol.Value, out var market)
                ? market
                : symbol.ID.Market;
        }

        /// <summary>
        /// Gets the lean market for this index ticker
        /// </summary>
        /// <returns>The market of the index</returns>
        public static bool TryGetIndexMarket(string ticker, out string market)
        {
            return _indexMarket.TryGetValue(ticker, out market);
        }
    }
}
