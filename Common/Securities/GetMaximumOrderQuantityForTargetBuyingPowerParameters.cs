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
    /// Defines the parameters for <see cref="IBuyingPowerModel.GetMaximumOrderQuantityForTargetBuyingPower"/>
    /// </summary>
    public class GetMaximumOrderQuantityForTargetBuyingPowerParameters
    {
        /// <summary>
        /// Gets the algorithm's portfolio
        /// </summary>
        public SecurityPortfolioManager Portfolio { get; }

        /// <summary>
        /// Gets the security
        /// </summary>
        public Security Security { get; }

        /// <summary>
        /// Gets the target signed percentage buying power
        /// </summary>
        public decimal TargetBuyingPower { get; }

        /// <summary>
        /// True enables the <see cref="IBuyingPowerModel"/> to skip setting <see cref="GetMaximumOrderQuantityResult.Reason"/>
        /// for non error situations, for performance
        /// </summary>
        public bool SilenceNonErrorReasons { get; }

        /// <summary>
        /// Configurable minimum order margin portfolio percentage to ignore bad orders, orders with unrealistic small sizes
        /// </summary>
        /// <remarks>Default value is 0. This setting is useful to avoid small trading noise when using SetHoldings</remarks>
        public decimal MinimumOrderMarginPortfolioPercentage { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GetMaximumOrderQuantityForTargetBuyingPowerParameters"/> class
        /// </summary>
        /// <param name="portfolio">The algorithm's portfolio</param>
        /// <param name="security">The security</param>
        /// <param name="targetBuyingPower">The target percentage buying power</param>
        /// <param name="minimumOrderMarginPortfolioPercentage">Configurable minimum order margin portfolio percentage to ignore orders with unrealistic small sizes</param>
        /// <param name="silenceNonErrorReasons">True will not return <see cref="GetMaximumOrderQuantityResult.Reason"/>
        /// set for non error situation, this is for performance</param>
        public GetMaximumOrderQuantityForTargetBuyingPowerParameters(
            SecurityPortfolioManager portfolio,
            Security security,
            decimal targetBuyingPower,
            decimal minimumOrderMarginPortfolioPercentage,
            bool silenceNonErrorReasons = false
        )
        {
            Portfolio = portfolio;
            Security = security;
            TargetBuyingPower = targetBuyingPower;
            SilenceNonErrorReasons = silenceNonErrorReasons;
            MinimumOrderMarginPortfolioPercentage = minimumOrderMarginPortfolioPercentage;
        }
    }
}
