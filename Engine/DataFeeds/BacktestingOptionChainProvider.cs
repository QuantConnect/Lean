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
        /// <summary>
        /// Gets the list of option contracts for a given underlying symbol
        /// </summary>
        /// <param name="symbol">The option or the underlying symbol to get the option chain for.
        /// Providing the option allows targeting an option ticker different than the default e.g. SPXW</param>
        /// <param name="date">The date for which to request the option chain (only used in backtesting)</param>
        /// <returns>The list of option contracts</returns>
        public virtual IEnumerable<Symbol> GetOptionContractList(Symbol symbol, DateTime date)
        {
            Symbol canonicalSymbol;
            if (!symbol.SecurityType.HasOptions())
            {
                // we got an option
                if (symbol.SecurityType.IsOption() && symbol.Underlying != null)
                {
                    canonicalSymbol = GetCanonical(symbol, date);
                }
                else
                {
                    throw new NotSupportedException($"BacktestingOptionChainProvider.GetOptionContractList(): " +
                        $"{nameof(SecurityType.Equity)}, {nameof(SecurityType.Future)}, or {nameof(SecurityType.Index)} is expected but was {symbol.SecurityType}");
                }
            }
            else
            {
                // we got the underlying
                var mappedUnderlyingSymbol = MapUnderlyingSymbol(symbol, date);
                canonicalSymbol = Symbol.CreateCanonicalOption(mappedUnderlyingSymbol);
            }

            return GetSymbols(canonicalSymbol, date);
        }

        private Symbol GetCanonical(Symbol optionSymbol, DateTime date)
        {
            // Resolve any mapping before requesting option contract list for equities
            // Needs to be done in order for the data file key to be accurate
            if (optionSymbol.Underlying.RequiresMapping())
            {
                var mappedUnderlyingSymbol = MapUnderlyingSymbol(optionSymbol.Underlying, date);

                return Symbol.CreateCanonicalOption(mappedUnderlyingSymbol);
            }
            else
            {
                return optionSymbol.Canonical;
            }
        }

        private Symbol MapUnderlyingSymbol(Symbol underlying, DateTime date)
        {
            if (underlying.RequiresMapping())
            {
                var mapFileResolver = MapFileProvider.Get(AuxiliaryDataKey.Create(underlying));
                var mapFile = mapFileResolver.ResolveMapFile(underlying);
                var ticker = mapFile.GetMappedSymbol(date, underlying.Value);
                return underlying.UpdateMappedSymbol(ticker);
            }
            else
            {
                return underlying;
            }
        }
    }
}
