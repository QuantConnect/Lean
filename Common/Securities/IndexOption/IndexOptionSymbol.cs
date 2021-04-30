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

namespace QuantConnect.Securities.IndexOption
{
    /// <summary>
    /// Index Option Symbol
    /// </summary>
    public static class IndexOptionSymbol
    {
        private static readonly HashSet<string> _supportedIndexOptionTickers = new HashSet<string>
        {
            "SPX",
            "NDX",
            "VIX",
            "SPXW",
            "NQX",
            "VIXW"
        };

        /// <summary>
        /// Determines if the Index Option Symbol is for a monthly contract
        /// </summary>
        /// <param name="symbol">Index Option Symbol</param>
        /// <returns>True if monthly contract, false otherwise</returns>
        public static bool IsStandard(Symbol symbol)
        {
            if (symbol.ID.Market != Market.USA)
            {
                return true;
            }

            switch (symbol.ID.Symbol)
            {
                // These are known assets that are weeklies or end-of-month settled contracts.
                case "SPXW":
                case "VIXW":
                case "NDXP":
                case "NQX":
                    return false;

                default:
                    return true;
            }
        }

        /// <summary>
        /// Checks if the ticker provided is a supported Index Option
        /// </summary>
        /// <param name="ticker">Ticker of the index option</param>
        /// <returns>true if the ticker matches an index option's ticker</returns>
        /// <remarks>
        /// This is only used in IB brokerage, since they don't distinguish index options
        /// from regular equity options. When we do the conversion from a contract to a SecurityType,
        /// the only information we're provided that can reverse it to the <see cref="SecurityType.IndexOption"/>
        /// enum value is the ticker.
        /// </remarks>
        public static bool IsIndexOption(string ticker)
        {
            return _supportedIndexOptionTickers.Contains(ticker.ToUpper());
        }
    }
}
