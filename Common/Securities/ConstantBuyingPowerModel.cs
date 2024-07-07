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

namespace QuantConnect.Securities
{
    /// <summary>
    /// Provides an implementation of <see cref="IBuyingPowerModel"/> that uses an absurdly low margin
    /// requirement to ensure all orders have sufficient margin provided the portfolio is not underwater.
    /// </summary>
    public class ConstantBuyingPowerModel : BuyingPowerModel
    {
        private readonly decimal _marginRequiredPerUnitInAccountCurrency;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConstantBuyingPowerModel"/> class
        /// </summary>
        /// <param name="marginRequiredPerUnitInAccountCurrency">The constant amount of margin required per single unit
        /// of an asset. Each unit is defined as a quantity of 1 and NOT based on the lot size.</param>
        public ConstantBuyingPowerModel(decimal marginRequiredPerUnitInAccountCurrency)
        {
            _marginRequiredPerUnitInAccountCurrency = marginRequiredPerUnitInAccountCurrency;
        }

        /// <summary>
        /// Sets the leverage for the applicable securities, i.e, equities
        /// </summary>
        /// <remarks>
        /// This is added to maintain backwards compatibility with the old margin/leverage system
        /// </remarks>
        /// <param name="security"></param>
        /// <param name="leverage">The new leverage</param>
        public override void SetLeverage(Security security, decimal leverage)
        {
            // ignored -- reasoning is user has an algorithm that has margin issues and so they quickly swap
            // this impl in, but their code calls set leverage, they would need to comment that out and such
            // said another way -- user made the decision to ignore margin/leverage by selecting this model
        }

        /// <summary>
        /// The margin that must be held in order to increase the position by the provided quantity
        /// </summary>
        /// <param name="parameters">An object containing the security and quantity of shares</param>
        /// <returns>The initial margin required for the provided security and quantity</returns>
        public override InitialMargin GetInitialMarginRequirement(
            InitialMarginParameters parameters
        )
        {
            return parameters.Quantity * _marginRequiredPerUnitInAccountCurrency;
        }

        /// <summary>
        /// Gets the margin currently allocated to the specified holding
        /// </summary>
        /// <param name="parameters">An object containing the security</param>
        /// <returns>The maintenance margin required for the provided holdings quantity/cost/value</returns>
        public override MaintenanceMargin GetMaintenanceMargin(
            MaintenanceMarginParameters parameters
        )
        {
            return parameters.AbsoluteQuantity * _marginRequiredPerUnitInAccountCurrency;
        }
    }
}
