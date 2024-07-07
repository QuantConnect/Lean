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
using QuantConnect.Securities;

namespace QuantConnect.ToolBox.RandomDataGenerator
{
    /// <summary>
    /// Random pricing model used to determine the fair price or theoretical value for a call or a put option
    /// </summary>
    public class RandomPriceGenerator : IPriceGenerator
    {
        private readonly Security _security;
        private readonly IRandomValueGenerator _random;

        /// <summary>
        /// Creates instance of <see cref="RandomPriceGenerator"/>
        /// </summary>
        ///<param name="security"><see cref="Security"/> object for which to generate price data</param>
        /// <param name="random"><see cref="IRandomValueGenerator"/> type capable of producing random values</param>
        public RandomPriceGenerator(Security security, IRandomValueGenerator random)
        {
            _security = security;
            _random = random;
        }

        /// <summary>
        /// <see cref="RandomPriceGenerator"/> is always ready to generate new price values as it does not depend on volatility model
        /// </summary>
        public bool WarmedUp => true;

        /// <summary>
        /// Generates an asset price
        /// </summary>
        /// <param name="maximumPercentDeviation">The maximum percent deviation. This value is in percent space,
        ///     so a value of 1m is equal to 1%.</param>
        /// <param name="referenceDate">date used in price calculation</param>
        /// <returns>Returns a new decimal as price</returns>
        public decimal NextValue(decimal maximumPercentDeviation, DateTime referenceDate) =>
            _random.NextPrice(
                _security.Symbol.SecurityType,
                _security.Symbol.ID.Market,
                _security.Price,
                maximumPercentDeviation
            );
    }
}
