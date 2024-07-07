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

using System.Collections.Generic;

namespace QuantConnect.Securities.Index
{
    /// <summary>
    /// Helper methods for Index Symbols
    /// </summary>
    public static class IndexSymbol
    {
        private static readonly Dictionary<string, string> _indexMarket = new Dictionary<
            string,
            string
        >
        {
            { "SPX", Market.CBOE },
            { "NDX", "NASDAQ" },
            { "VIX", Market.CBOE },
            { "SPXW", Market.CBOE },
            { "NQX", "NASDAQ" },
            { "VIXW", Market.CBOE }
        };

        /// <summary>
        /// Gets the actual exchange the index lives on
        /// </summary>
        /// <returns>The market of the index</returns>
        public static string GetIndexExchange(Symbol symbol)
        {
            string market;
            return _indexMarket.TryGetValue(symbol.Value, out market) ? market : symbol.ID.Market;
        }
    }
}
