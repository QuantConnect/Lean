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
using System.Linq;
using QuantConnect.Interfaces;

namespace QuantConnect.Data.Auxiliary
{
    /// <summary>
    /// Implementation of IPrimaryExchangeProvider from map files. 
    /// </summary>
    public class MapFilePrimaryExchangeProvider : IPrimaryExchangeProvider
    {
        private readonly IMapFileProvider _mapFileProvider;
        private readonly ConcurrentDictionary<SecurityIdentifier, PrimaryExchange> _primaryExchangeBySid;

        public MapFilePrimaryExchangeProvider(IMapFileProvider mapFileProvider)
        {
            _mapFileProvider = mapFileProvider;
            _primaryExchangeBySid = new ConcurrentDictionary<SecurityIdentifier, PrimaryExchange>();
        }

        /// <summary>
        /// Gets the primary exchange for a given security identifier
        /// </summary>
        /// <param name="securityIdentifier">The security identifier to get the primary exchange for</param>
        /// <returns>Returns the primary exchange or null if not found</returns>
        public PrimaryExchange GetPrimaryExchange(SecurityIdentifier securityIdentifier)
        {
            PrimaryExchange primaryExchange;
            if (!_primaryExchangeBySid.TryGetValue(securityIdentifier, out primaryExchange))
            {
                var mapFile = _mapFileProvider.Get(securityIdentifier.Market).ResolveMapFile(securityIdentifier.Symbol, securityIdentifier.Date);
                if (mapFile != null && mapFile.Any())
                {
                    primaryExchange = mapFile.Last().PrimaryExchange;
                }
                _primaryExchangeBySid[securityIdentifier] = primaryExchange;
            }

            return primaryExchange;
        }
    }
}
