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

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// An implementation of <see cref="IFutureChainProvider"/> that reads the list of contracts from open interest zip data files
    /// </summary>
    public class BacktestingFutureChainProvider : BacktestingChainProvider, IFutureChainProvider
    {
        /// <summary>
        /// Gets the list of future contracts for a given underlying symbol
        /// </summary>
        /// <param name="symbol">The underlying symbol</param>
        /// <param name="date">The date for which to request the future chain (only used in backtesting)</param>
        /// <returns>The list of future contracts</returns>
        public virtual IEnumerable<Symbol> GetFutureContractList(Symbol symbol, DateTime date)
        {
            return GetSymbols(GetSymbol(symbol), date);
        }

        /// <summary>
        /// Helper method to get the symbol to use
        /// </summary>
        protected static Symbol GetSymbol(Symbol symbol)
        {
            if (symbol.SecurityType != SecurityType.Future)
            {
                if (symbol.SecurityType == SecurityType.FutureOption && symbol.Underlying != null)
                {
                    // be user friendly and take the underlying
                    symbol = symbol.Underlying;
                }
                else
                {
                    throw new NotSupportedException($"BacktestingFutureChainProvider.GetFutureContractList():" +
                        $" {nameof(SecurityType.Future)} or {nameof(SecurityType.FutureOption)} is expected but was {symbol.SecurityType}");
                }
            }

            return symbol.Canonical;
        }
    }
}
