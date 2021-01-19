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

using System.Collections.Generic;
using System.Linq;
using QuantConnect.Interfaces;

namespace QuantConnect.Data.Auxiliary
{
    /// <summary>
    /// Implementation of IPrimaryExchangeProvider from map files. 
    /// </summary>
    class MapFilePrimaryExchangeProvider : IPrimaryExchangeProvider
    {
        private readonly Dictionary<SecurityIdentifier, PrimaryExchange> _primaryExchangeBySid;
        public MapFilePrimaryExchangeProvider(IMapFileProvider mapFileProvider, string market = "USA")
        {
            _primaryExchangeBySid = new Dictionary<SecurityIdentifier, PrimaryExchange>();
            foreach (var mapFile in mapFileProvider.Get(market))
            {
                var sid = SecurityIdentifier.GenerateEquity(mapFile.FirstDate, mapFile.FirstTicker, market);
                var primaryExchange = (PrimaryExchange) mapFile.Last().PrimaryExchange;
                _primaryExchangeBySid[sid] = primaryExchange;
            }
        }

        /// <summary>
        /// Gets the primary exchange for a given security identifier
        /// </summary>
        /// <param name="securityIdentifier">The security identifier to get the primary exchange for</param>
        /// <returns>Returns the primary exchange or null if not found</returns>
        public string GetPrimaryExchange(SecurityIdentifier securityIdentifier)
        {
            if (_primaryExchangeBySid.ContainsKey(securityIdentifier))
            {
                return _primaryExchangeBySid[securityIdentifier].ToString();
            }

            return null;
        }
    }
}
