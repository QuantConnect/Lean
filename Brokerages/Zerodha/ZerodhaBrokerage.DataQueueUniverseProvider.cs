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
using System.Linq;

namespace QuantConnect.Brokerages.Zerodha
{
    /// <summary>
    /// ZerodhaBrokerage - IDataQueueUniverseProvider implementation
    /// </summary>
    public partial class ZerodhaBrokerage
    {
        #region IDataQueueUniverseProvider
        /// <summary>
        /// Method returns a collection of Symbols that are available at the broker.
        /// </summary>
        /// <param name="lookupSymbol">Representation of the Symbol</param>
        /// <param name="includeExpired">Include expired contracts</param>
        /// <param name="securityCurrency">Expected security currency(if any)</param>
        /// <returns></returns>
        public IEnumerable<Symbol> LookupSymbols(Symbol lookupSymbol, bool includeExpired, string securityCurrency = null)
        {
            Func<Symbol, string> lookupFunc;
            var securityExchange = lookupSymbol.ID.Market;
            switch (lookupSymbol.SecurityType)
            {
                case SecurityType.Option:
                    throw new ArgumentException("Lean does not currently support Option for Zerodha");
                    break;
                case SecurityType.Future:
                    throw new ArgumentException("Lean does not currently support Future for Zerodha");
                    break;
                default:
                    lookupFunc = symbol => symbol.Value;
                    break;
            }

            var result = _symbolMapper.KnownSymbols.Where(x => lookupFunc(x) == lookupSymbol &&
                                            x.ID.SecurityType == lookupSymbol.SecurityType &&
                                            (securityExchange == null || x.ID.Market == securityExchange))
                                         .ToList();

            return result.Select(x => x);
        }

        // <summary>
        /// Returns whether selection can take place or not.
        /// </summary>
        /// <returns>True if selection can take place</returns>
        public bool CanPerformSelection()
        {
            return true;
        }
        #endregion
    }
}
