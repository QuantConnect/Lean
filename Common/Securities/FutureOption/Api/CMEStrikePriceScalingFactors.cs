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

namespace QuantConnect.Securities.FutureOption
{
    /// <summary>
    /// Provides a means to get the scaling factor for CME's quotes API
    /// </summary>
    public class CMEStrikePriceScalingFactors
    {
        /// <summary>
        /// CME's option chain quotes strike price scaling factor
        /// </summary>
        private static readonly IReadOnlyDictionary<string, decimal> _scalingFactors = new Dictionary<string, decimal>
        {
            { "ES", 100m },
            { "NQ", 100m },
            { "HG", 100m },
            { "SI", 100m },
            { "CL", 100m },
            { "NG", 1000m },
            { "DC", 100m }
        };

        /// <summary>
        /// Gets the option chain strike price scaling factor for the quote response from CME
        /// </summary>
        /// <param name="underlyingFuture">Underlying future Symbol to normalize</param>
        /// <returns>Scaling factor for the strike price</returns>
        public static decimal GetScaleFactor(Symbol underlyingFuture)
        {
            return _scalingFactors.ContainsKey(underlyingFuture.ID.Symbol)
                ? _scalingFactors[underlyingFuture.ID.Symbol]
                : 1m;
        }
    }
}
