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

namespace QuantConnect.Securities.Positions
{
    /// <summary>
    /// Defines the parameters for <see cref="IPositionGroupBuyingPowerModel.GetMaximumLotsForDeltaBuyingPower"/>
    /// </summary>
    public class GetMaximumLotsForDeltaBuyingPowerParameters
    {
        /// <summary>
        /// Gets the algorithm's portfolio manager
        /// </summary>
        public SecurityPortfolioManager Portfolio { get; }

        /// <summary>
        /// Gets the position group
        /// </summary>
        public IPositionGroup PositionGroup { get; }

        /// <summary>
        /// The delta buying power.
        /// </summary>
        /// <remarks>Sign defines the position side to apply the delta, positive long, negative short side.</remarks>
        public decimal DeltaBuyingPower { get; }

        /// <summary>
        /// True enables the <see cref="IBuyingPowerModel"/> to skip setting <see cref="GetMaximumLotsResult.Reason"/>
        /// for non error situations, for performance
        /// </summary>
        public bool SilenceNonErrorReasons { get; }

        /// <summary>
        /// Configurable minimum order margin portfolio percentage to ignore bad orders, orders with unrealistic small sizes
        /// </summary>
        /// <remarks>Default value is 0. This setting is useful to avoid small trading noise when using SetHoldings</remarks>
        public decimal MinimumOrderMarginPortfolioPercentage { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GetMaximumLotsForDeltaBuyingPowerParameters"/> class
        /// </summary>
        /// <param name="portfolio">The algorithm's portfolio manager</param>
        /// <param name="positionGroup">The position group</param>
        /// <param name="deltaBuyingPower">The delta buying power to apply. Sign defines the position side to apply the delta</param>
        /// <param name="minimumOrderMarginPortfolioPercentage">Configurable minimum order margin portfolio percentage to ignore orders with unrealistic small sizes</param>
        /// <param name="silenceNonErrorReasons">True will not return <see cref="GetMaximumLotsResult.Reason"/>
        /// set for non error situation, this is for performance</param>
        public GetMaximumLotsForDeltaBuyingPowerParameters(
            SecurityPortfolioManager portfolio,
            IPositionGroup positionGroup,
            decimal deltaBuyingPower,
            decimal minimumOrderMarginPortfolioPercentage,
            bool silenceNonErrorReasons = false
        )
        {
            Portfolio = portfolio;
            PositionGroup = positionGroup;
            DeltaBuyingPower = deltaBuyingPower;
            SilenceNonErrorReasons = silenceNonErrorReasons;
            MinimumOrderMarginPortfolioPercentage = minimumOrderMarginPortfolioPercentage;
        }

        /// <summary>
        /// Creates a new <see cref="GetMaximumLotsResult"/> with zero quantity and an error message.
        /// </summary>
        public GetMaximumLotsResult Error(string reason)
        {
            return new GetMaximumLotsResult(0, reason, true);
        }

        /// <summary>
        /// Creates a new <see cref="GetMaximumLotsResult"/> with zero quantity and no message.
        /// </summary>
        public GetMaximumLotsResult Zero()
        {
            return new GetMaximumLotsResult(0, string.Empty, false);
        }

        /// <summary>
        /// Creates a new <see cref="GetMaximumLotsResult"/> with zero quantity and an info message.
        /// </summary>
        public GetMaximumLotsResult Zero(string reason)
        {
            return new GetMaximumLotsResult(0, reason, false);
        }

        /// <summary>
        /// Creates a new <see cref="GetMaximumLotsResult"/> for the specified quantity and no message.
        /// </summary>
        public GetMaximumLotsResult Result(decimal quantity)
        {
            return new GetMaximumLotsResult(quantity, string.Empty, false);
        }
    }
}
