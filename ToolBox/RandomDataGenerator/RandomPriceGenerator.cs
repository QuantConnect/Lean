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

namespace QuantConnect.ToolBox.RandomDataGenerator
{
    /// <summary>
    /// Random pricing model used to determine the fair price or theoretical value for a call or a put option
    /// </summary>
    public class RandomPriceGenerator : IPriceGenerator
    {
        private readonly Symbol _symbol;
        private readonly IRandomValueGenerator _random;

        /// <summary>
        /// Creates instance of <see cref="RandomPriceGenerator"/>
        /// </summary>
        /// <param name="symbol">The symbol of the security</param>
        /// <param name="random"><see cref="IRandomValueGenerator"/> type capable of producing random values</param>
        public RandomPriceGenerator(Symbol symbol, IRandomValueGenerator random)
        {
            _symbol = symbol;
            _random = random;
        }

        /// <summary>
        /// Generates a random price used in further price calculation
        /// </summary>
        /// <param name="referencePrice">previous reference price</param>
        /// <param name="maximumPercentDeviation">The maximum percent deviation. This value is in percent space,
        ///     so a value of 1m is equal to 1%.</param>
        /// <returns>A new decimal suitable for usage as reference price</returns>
        public decimal NextReferencePrice(decimal referencePrice, decimal maximumPercentDeviation)
            => _random.NextPrice(_symbol.SecurityType, _symbol.ID.Market, referencePrice, maximumPercentDeviation);

        /// <summary>
        /// Generates an asset price
        /// </summary>
        /// <param name="referencePrice">reference price used in price calculation</param>
        /// <param name="referenceDate">date used in price calculation</param>
        /// <returns>Returns a new decimal as price</returns>
        public decimal NextValue(decimal referencePrice, DateTime referenceDate)
            => referencePrice;
    }
}
