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
 *
*/

using System.Collections.Concurrent;
using System.Collections.Generic;

namespace QuantConnect.Securities
{
    /// <summary>
    /// A helper class that will provide <see cref="SecurityCache"/> instances
    /// </summary>
    /// <remarks>The value of this class and its logic is performance.
    /// This class allows for two different <see cref="Security"/> to share the same
    /// data type cache through different instances of <see cref="SecurityCache"/>.
    /// This is used to directly access custom data types through their underlying</remarks>
    public class SecurityCacheProvider
    {
        private readonly ConcurrentDictionary<SecurityIdentifier, ConcurrentBag<Symbol>> _relatedSymbols;
        private readonly IDictionary<Symbol, Security> _securityProvider;

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="securityProvider">The security provider to use</param>
        public SecurityCacheProvider(IDictionary<Symbol, Security> securityProvider)
        {
            _securityProvider = securityProvider;
            _relatedSymbols = new ConcurrentDictionary<SecurityIdentifier, ConcurrentBag<Symbol>>();
        }

        /// <summary>
        /// Will return the <see cref="SecurityCache"/> instance to use for a give Symbol.
        /// If the provided Symbol is a custom type which has an underlying we will try to use the
        /// underlying SecurityCache type cache, if the underlying is not present we will keep track
        /// of the custom Symbol in case it is added later.
        /// </summary>
        /// <returns>The cache instance to use</returns>
        public SecurityCache GetSecurityCache(Symbol symbol)
        {
            var securityCache = new SecurityCache();
            if (symbol.SecurityType == SecurityType.Base && symbol.HasUnderlying)
            {
                Security underlyingSecurity;
                if(_securityProvider.TryGetValue(symbol.Underlying, out underlyingSecurity))
                {
                    // we found the underlying, lets use its data type cache
                    SecurityCache.ShareTypeCacheInstance(underlyingSecurity.Cache, securityCache);
                }
                else
                {
                    // we didn't find the underlying, lets keep track of the underlying symbol which might get added in the future.
                    // else when it is added, we would have to go through existing Securities and find any which use it as underlying
                    if (_relatedSymbols.ContainsKey(symbol.Underlying.ID))
                    {
                        _relatedSymbols[symbol.Underlying.ID].Add(symbol);
                    }
                    else
                    {
                        _relatedSymbols[symbol.Underlying.ID] = new ConcurrentBag<Symbol> { symbol };
                    }
                }
            }
            else if(_relatedSymbols.ContainsKey(symbol.ID))
            {
                // if we are here it means we added a symbol which is an underlying of some existing custom symbols
                ConcurrentBag<Symbol> customSymbols;
                if (_relatedSymbols.TryRemove(symbol.ID, out customSymbols))
                {
                    foreach (var customSymbol in customSymbols)
                    {
                        Security customSecurity;
                        if (_securityProvider.TryGetValue(customSymbol, out customSecurity))
                        {
                            // we make each existing custom security cache, use the new instance data type cache
                            // note that if any data already existed in the custom cache it will be passed
                            SecurityCache.ShareTypeCacheInstance(securityCache, customSecurity.Cache);
                        }
                    }
                }
            }

            return securityCache;
        }
    }
}
