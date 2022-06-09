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
        /// <param name="symbol">The underlying symbol</param>
        /// <param name="date">The date for which to request the option chain (only used in backtesting)</param>
        /// <returns>The list of option contracts</returns>
        public virtual IEnumerable<Symbol> GetOptionContractList(Symbol symbol, DateTime date)
        {
            if (!symbol.SecurityType.HasOptions())
            {
                if (symbol.SecurityType.IsOption() && symbol.Underlying != null)
                {
                    // be user friendly and take the underlying
                    symbol = symbol.Underlying;
                }
                else
                {
                    throw new NotSupportedException($"BacktestingOptionChainProvider.GetOptionContractList(): " +
                        $"{nameof(SecurityType.Equity)}, {nameof(SecurityType.Future)}, or {nameof(SecurityType.Index)} is expected but was {symbol.SecurityType}");
                }
            }

            // Resolve any mapping before requesting option contract list for equities
            // Needs to be done in order for the data file key to be accurate
            Symbol mappedSymbol;
            if (symbol.RequiresMapping())
            {
                var mapFileResolver = _mapFileProvider.Get(AuxiliaryDataKey.Create(symbol));
                var mapFile = mapFileResolver.ResolveMapFile(symbol);
                var ticker = mapFile.GetMappedSymbol(date, symbol.Value);
                mappedSymbol = symbol.UpdateMappedSymbol(ticker);
            }
            else
            {
                mappedSymbol = symbol;
            }

            // create a canonical option symbol for the given underlying
            var canonicalSymbol = Symbol.CreateOption(
                mappedSymbol,
                mappedSymbol.ID.Market,
                mappedSymbol.SecurityType.DefaultOptionStyle(),
                default(OptionRight),
                0,
                SecurityIdentifier.DefaultDate);

            return GetSymbols(canonicalSymbol, date);
        }
    }
}
