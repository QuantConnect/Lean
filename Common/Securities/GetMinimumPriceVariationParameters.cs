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

namespace QuantConnect.Securities
{
    /// <summary>
    /// Defines the parameters for <see cref="IPriceVariationModel.GetMinimumPriceVariation"/>
    /// </summary>
    public class GetMinimumPriceVariationParameters
    {
        /// <summary>
        /// Gets the security
        /// </summary>
        public Security Security { get; }

        /// <summary>
        /// Gets the reference price to be used for the calculation
        /// </summary>
        public decimal ReferencePrice { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GetMinimumPriceVariationParameters"/> class
        /// </summary>
        /// <param name="security">The security</param>
        /// <param name="referencePrice">The reference price to be used for the calculation</param>
        public GetMinimumPriceVariationParameters(Security security, decimal referencePrice)
        {
            Security = security;
            ReferencePrice = referencePrice;
        }
    }
}
