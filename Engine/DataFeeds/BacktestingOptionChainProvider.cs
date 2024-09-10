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
using QuantConnect.Interfaces;
using System.Collections.Generic;
using QuantConnect.Data.Auxiliary;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// An implementation of <see cref="IOptionChainProvider"/> that reads the list of contracts from open interest zip data files
    /// </summary>
    public class BacktestingOptionChainProvider : BacktestingChainProvider, IOptionChainProvider
    {
        private IMapFileProvider _mapFileProvider;

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="dataCacheProvider">The data cache provider instance to use</param>
        /// <param name="mapFileProvider">The map file provider instance to use</param>
        public BacktestingOptionChainProvider(IDataCacheProvider dataCacheProvider, IMapFileProvider mapFileProvider)
            : base(dataCacheProvider)
        {
            _mapFileProvider = mapFileProvider;
        }

        /// <summary>
        /// Gets the list of option contracts for a given underlying symbol
        /// </summary>
        /// <param name="symbol">The option or the underlying symbol to get the option chain for.
        /// Providing the option allows targeting an option ticker different than the default e.g. SPXW</param>
        /// <param name="date">The date for which to request the option chain (only used in backtesting)</param>
        /// <returns>The list of option contracts</returns>
        public virtual IEnumerable<Symbol> GetOptionContractList(Symbol symbol, DateTime date)
        {
            return GetSymbols(symbol.GetCanonical(date), date);
        }
    }
}
