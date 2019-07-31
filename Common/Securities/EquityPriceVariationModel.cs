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

using System;

namespace QuantConnect.Securities
{
    /// <summary>
    /// Provides an implementation of <see cref="IPriceVariationModel"/>
    /// for use in defining the minimum price variation for a given equity
    /// under Regulation NMS – Rule 612 (a.k.a – the “sub-penny rule”)
    /// </summary>
    public class EquityPriceVariationModel : SecurityPriceVariationModel
    {
        /// <summary>
        /// Get the minimum price variation from a security
        /// </summary>
        /// <param name="security">Security which we want the minimum price variation from</param>
        /// <param name="referencePrice">The reference price to be used for the calculation (usually the limit/stop order price)</param>
        /// <returns>Decimal minimum price variation of a given security</returns>
        public override decimal GetMinimumPriceVariation(Security security, decimal referencePrice)
        {
            if (security.Type != SecurityType.Equity)
            {
                throw new ArgumentException("EquityPriceVariationModel.GetMinimumPriceVariation(): Invalid SecurityType " + security.Type);
            }

            // If the quotation is priced less than $1.00 per share, the minimum pricing increment is $0.0001.
            // Source: https://www.law.cornell.edu/cfr/text/17/242.612
            if (referencePrice < 1m)
            {
                return 0.0001m;
            }

            return base.GetMinimumPriceVariation(security, referencePrice);
        }
    }
}