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
using Python.Runtime;
using System.Collections.Generic;
using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.Algorithm.Framework.Selection
{
    /// <summary>
    /// Universe selection model that selects the constituents of an ETF.
    /// </summary>
    public class ETFConstituentsUniverseSelectionModel : UniverseSelectionModel
    {
        private readonly Symbol _etfSymbol;
        private readonly UniverseSettings _universeSettings;
        private readonly Func<IEnumerable<ETFConstituentUniverse>, IEnumerable<Symbol>> _universeFilterFunc;
        private Universe _universe;

        /// <summary>
        /// Initializes a new instance of the <see cref="ETFConstituentsUniverseSelectionModel"/> class
        /// </summary>
        /// <param name="etfSymbol">Symbol of the ETF to get constituents for</param>
        /// <param name="universeSettings">Universe settings</param>
        /// <param name="universeFilterFunc">Function to filter universe results</param>
        public ETFConstituentsUniverseSelectionModel(
            Symbol etfSymbol,
            UniverseSettings universeSettings,
            Func<IEnumerable<ETFConstituentUniverse>, IEnumerable<Symbol>> universeFilterFunc)
        {
            _etfSymbol = etfSymbol;
            _universeSettings = universeSettings;
            _universeFilterFunc = universeFilterFunc;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ETFConstituentsUniverseSelectionModel"/> class
        /// </summary>
        /// <param name="etfSymbol">Symbol of the ETF to get constituents for</param>
        /// <param name="universeFilterFunc">Function to filter universe results</param>
        public ETFConstituentsUniverseSelectionModel(
            Symbol etfSymbol,
            Func<IEnumerable<ETFConstituentUniverse>, IEnumerable<Symbol>> universeFilterFunc)
            : this(etfSymbol, null, universeFilterFunc)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ETFConstituentsUniverseSelectionModel"/> class
        /// </summary>
        /// <param name="etfSymbol">Symbol of the ETF to get constituents for</param>
        /// <param name="universeSettings">Universe settings</param>
        /// <param name="universeFilterFunc">Function to filter universe results</param>
        public ETFConstituentsUniverseSelectionModel(
            Symbol etfSymbol,
            UniverseSettings universeSettings = null,
            PyObject universeFilterFunc = null) :
            this(etfSymbol, universeSettings, universeFilterFunc.ConvertPythonUniverseFilterFunction<ETFConstituentUniverse>())
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ETFConstituentsUniverseSelectionModel"/> class
        /// </summary>
        /// <param name="etfTicker">The string ETF ticker symbol</param>
        /// <param name="universeSettings">Universe settings</param>
        /// <param name="universeFilterFunc">Function to filter universe results</param>
        public ETFConstituentsUniverseSelectionModel(
            string etfTicker,
            UniverseSettings universeSettings,
            Func<IEnumerable<ETFConstituentUniverse>, IEnumerable<Symbol>> universeFilterFunc)
        {
            _etfSymbol = SymbolCache.TryGetSymbol(etfTicker, out var symbol)
                && symbol.SecurityType == SecurityType.Equity
                ? symbol : Symbol.Create(etfTicker, SecurityType.Equity, Market.USA);

            _universeSettings = universeSettings;
            _universeFilterFunc = universeFilterFunc;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ETFConstituentsUniverseSelectionModel"/> class
        /// </summary>
        /// <param name="etfTicker">The string ETF ticker symbol</param>
        /// <param name="universeFilterFunc">Function to filter universe results</param>
        public ETFConstituentsUniverseSelectionModel(
            string etfTicker,
            Func<IEnumerable<ETFConstituentUniverse>, IEnumerable<Symbol>> universeFilterFunc)
            : this(etfTicker, null, universeFilterFunc)
        { }


        /// <summary>
        /// Initializes a new instance of the <see cref="ETFConstituentsUniverseSelectionModel"/> class
        /// </summary>
        /// <param name="etfTicker">The string ETF ticker symbol</param>
        /// <param name="universeSettings">Universe settings</param>
        /// <param name="universeFilterFunc">Function to filter universe results</param>
        public ETFConstituentsUniverseSelectionModel(
            string etfTicker,
            UniverseSettings universeSettings = null,
            PyObject universeFilterFunc = null) :
            this(etfTicker, universeSettings, universeFilterFunc.ConvertPythonUniverseFilterFunction<ETFConstituentUniverse>())
        { }

        /// <summary>
        /// Creates a new ETF constituents universe using this class's selection function
        /// </summary>
        /// <param name="algorithm">The algorithm instance to create universes for</param>
        /// <returns>The universe defined by this model</returns>
        public override IEnumerable<Universe> CreateUniverses(QCAlgorithm algorithm)
        {
            _universe ??= algorithm?.Universe.ETF(_etfSymbol, _universeSettings, _universeFilterFunc);
            return new[] { _universe };
        }
    }
}
