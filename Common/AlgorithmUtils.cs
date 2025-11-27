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

using QuantConnect.Interfaces;
using QuantConnect.Securities;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect
{
    /// <summary>
    /// Provides utility methods for or related to algorithms
    /// </summary>
    public static class AlgorithmUtils
    {
        /// <summary>
        /// Seeds the provided securities with their last known prices from the algorithm
        /// </summary>
        /// <param name="securities">The securities to seed</param>
        /// <param name="algorithm">The algorithm instance</param>
        public static void SeedSecurities(IReadOnlyCollection<Security> securities, IAlgorithm algorithm)
        {
            var securitiesToSeed = securities.Where(x => x.Price == 0);
            var data = algorithm.GetLastKnownPrices(securitiesToSeed.Select(x => x.Symbol));

            foreach (var security in securitiesToSeed)
            {
                if (data.TryGetValue(security.Symbol, out var seedData))
                {
                    foreach (var datum in seedData)
                    {
                        security.SetMarketPrice(datum);
                    }
                }
            }
        }
    }
}
