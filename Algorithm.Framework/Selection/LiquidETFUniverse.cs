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

using System.Linq;
using System.Collections.Generic;

namespace QuantConnect.Algorithm.Framework.Selection
{
    /// <summary>
    /// Universe Selection Model that adds the following ETFs at their inception date
    /// </summary>
    public class LiquidETFUniverse : InceptionDateUniverseSelectionModel
    {
        /// <summary>
        /// Represents the Energy ETF Category which can be used to access the list of Long and Inverse symbols
        /// </summary>
        public static readonly Grouping Energy = new Grouping(
            new[]
            {
                "VDE", "USO", "XES", "XOP", "UNG", "ICLN", "ERX",
                "UCO", "AMJ", "BNO", "AMLP", "UGAZ", "TAN"
            },
            new[] {"ERY", "SCO", "DGAZ" }
        );

        /// <summary>
        /// Represents the Metals ETF Category which can be used to access the list of Long and Inverse symbols
        /// </summary>
        public static readonly Grouping Metals = new Grouping(
            new[] {"GLD", "IAU", "SLV", "GDX", "AGQ", "PPLT", "NUGT", "USLV", "UGLD", "JNUG"},
            new[] {"DUST", "JDST"}
        );

        /// <summary>
        /// Represents the Technology ETF Category which can be used to access the list of Long and Inverse symbols
        /// </summary>
        public static readonly Grouping Technology = new Grouping(
            new[] {"QQQ", "IGV", "QTEC", "FDN", "FXL", "TECL", "SOXL", "SKYY", "KWEB"},
            new[] {"TECS", "SOXS"}
        );

        /// <summary>
        /// Represents the Treasuries ETF Category which can be used to access the list of Long and Inverse symbols
        /// </summary>
        public static readonly Grouping Treasuries = new Grouping(
            new[]
            {
                "IEF", "SHY", "TLT", "IEI", "TLH", "BIL", "SPTL",
                "TMF", "SCHO", "SCHR", "SPTS", "GOVT"
            },
            new[] {"SHV", "TBT", "TBF", "TMV"}
        );

        /// <summary>
        /// Represents the Volatility ETF Category which can be used to access the list of Long and Inverse symbols
        /// </summary>
        public static readonly Grouping Volatility = new Grouping(
            new[] {"TVIX", "VIXY", "SPLV", "UVXY", "EEMV", "EFAV", "USMV"},
            new[] {"SVXY"}
        );

        /// <summary>
        /// Represents the SP500 Sectors ETF Category which can be used to access the list of Long and Inverse symbols
        /// </summary>
        public static readonly Grouping SP500Sectors = new Grouping(
            new[] {"XLB", "XLE", "XLF", "XLI", "XLK", "XLP", "XLU", "XLV", "XLY"},
            new string[0]
        );

        /// <summary>
        /// Initializes a new instance of the LiquidETFUniverse class
        /// </summary>
        public LiquidETFUniverse() :
            base(
                "qc-liquid-etf-basket",
                SP500Sectors
                    .Concat(Energy)
                    .Concat(Metals)
                    .Concat(Technology)
                    .Concat(Treasuries)
                    .Concat(Volatility)
                    // Convert the concatenated list of Symbol into a Dictionary of DateTime keyed by Symbol
                    // For equities, Symbol.ID is the first date the security is traded.
                    .ToDictionary(x => x.Value, x => x.ID.Date)
            )
        {

        }

        /// <summary>
        /// Represent a collection of ETF symbols that is grouped according to a given criteria
        /// </summary>
        public class Grouping : List<Symbol>
        {
            /// <summary>
            /// List of Symbols that follow the components direction
            /// </summary>
            public readonly List<Symbol> Long;

            /// <summary>
            /// List of Symbols that follow the components inverse direction
            /// </summary>
            public readonly List<Symbol> Inverse;

            /// <summary>
            /// Creates a new instance of <see cref="Grouping"/>.
            /// </summary>
            /// <param name="longTickers">List of tickers of ETFs that follows the components direction</param>
            /// <param name="inverseTickers">List of tickers of ETFs that follows the components inverse direction</param>
            public Grouping(IEnumerable<string> longTickers, IEnumerable<string> inverseTickers)
            {
                Long = longTickers.Select(x => Symbol.Create(x, SecurityType.Equity, Market.USA)).ToList();
                Inverse = inverseTickers.Select(x => Symbol.Create(x, SecurityType.Equity, Market.USA)).ToList();
                AddRange(Long);
                AddRange(Inverse);
            }

            /// <summary>
            /// Returns a string that represents the current object.
            /// </summary>
            /// <returns>
            /// A string that represents the current object.
            /// </returns>
            public override string ToString()
            {
                if (Count == 0)
                {
                    return "No Symbols";
                }

                var longSymbols = Long.Count == 0
                    ? string.Empty
                    : $" Long: {string.Join(",", Long.Select(x => x.Value))}";

                var inverseSymbols = Inverse.Count == 0
                    ? string.Empty
                    : $" Inverse: {string.Join(",", Inverse.Select(x => x.Value))}";

                return $"{Count} symbols:{longSymbols}{inverseSymbols}";
            }
        }
    }
}
