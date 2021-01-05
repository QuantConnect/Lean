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
using static System.FormattableString;

namespace QuantConnect.Algorithm.Framework.Alphas.Analysis
{
    /// <summary>
    /// Defines the security values at a given instant. This is analagous
    /// to TimeSlice/Slice, but decoupled from the algorithm thread and is
    /// intended to contain all of the information necessary to score all
    /// insight at this particular time step
    /// </summary>
    public class ReadOnlySecurityValuesCollection
    {
        private Dictionary<Symbol, SecurityValues> _securityValuesBySymbol;
        private readonly Func<Symbol, SecurityValues> _securityValuesBySymbolFunc;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReadOnlySecurityValuesCollection"/> class
        /// </summary>
        /// <param name="securityValuesBySymbol"></param>
        public ReadOnlySecurityValuesCollection(Dictionary<Symbol, SecurityValues> securityValuesBySymbol)
        {
            _securityValuesBySymbol = securityValuesBySymbol;
            _securityValuesBySymbolFunc = null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReadOnlySecurityValuesCollection"/> class
        /// </summary>
        /// <remarks>This constructor has performance in mind. Only create the <see cref="SecurityValues"/>
        /// for a <see cref="Symbol"/> if requested by a consumer.</remarks>
        /// <param name="securityValuesBySymbolFunc">Function used to get the
        /// <see cref="SecurityValues"/> for a specified <see cref="Symbol"/></param>
        public ReadOnlySecurityValuesCollection(Func<Symbol, SecurityValues> securityValuesBySymbolFunc)
        {
            _securityValuesBySymbolFunc = securityValuesBySymbolFunc;
            // lets be lazy for constructing the dictionary too!
            _securityValuesBySymbol = null;
        }

        /// <summary>
        /// Symbol indexer into security values collection.
        /// </summary>
        /// <param name="symbol">The symbol</param>
        /// <returns>The security values for the specified symbol</returns>
        public SecurityValues this[Symbol symbol]
        {
            get
            {
                if (_securityValuesBySymbol == null)
                {
                    _securityValuesBySymbol = new Dictionary<Symbol, SecurityValues>();
                }

                SecurityValues result;
                if(!_securityValuesBySymbol.TryGetValue(symbol, out result))
                {
                    if (_securityValuesBySymbolFunc == null)
                    {
                        throw new KeyNotFoundException(Invariant($"SecurityValues for symbol {symbol} was not found"));
                    }
                    result = _securityValuesBySymbolFunc(symbol);
                    _securityValuesBySymbol[symbol] = result;
                }
                return result;
            }
        }
    }
}