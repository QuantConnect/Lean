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

using System.Collections.Generic;

namespace QuantConnect.Algorithm.Framework.Alphas.Analysis
{
    /// <summary>
    /// Defines the security values at a given instant. This is analagous
    /// to TimeSlice/Slice, but decoupled from the algorithm thread and is
    /// intended to contain all of the information necessary to score all
    /// alphas at this particular time step
    /// </summary>
    public class ReadOnlySecurityValuesCollection
    {
        private readonly Dictionary<Symbol, SecurityValues> _securityValuesBySymbol;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReadOnlySecurityValuesCollection"/> class
        /// </summary>
        /// <param name="securityValuesBySymbol"></param>
        public ReadOnlySecurityValuesCollection(Dictionary<Symbol, SecurityValues> securityValuesBySymbol)
        {
            _securityValuesBySymbol = securityValuesBySymbol;
        }

        /// <summary>
        /// Symbol indexer into security values collection.
        /// </summary>
        /// <param name="symbol">The symbol</param>
        /// <returns>The security values for the specified symbol</returns>
        public SecurityValues this[Symbol symbol] => _securityValuesBySymbol[symbol];
    }
}