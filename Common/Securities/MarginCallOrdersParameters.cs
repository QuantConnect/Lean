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

using QuantConnect.Securities.Positions;

namespace QuantConnect.Securities
{
    /// <summary>
    /// Defines the parameters for <see cref="DefaultMarginCallModel.GenerateMarginCallOrders"/>
    /// </summary>
    public class MarginCallOrdersParameters
    {
        /// <summary>
        /// Gets the position group
        /// </summary>
        public IPositionGroup PositionGroup { get; }

        /// <summary>
        /// Gets the algorithm's total portfolio value
        /// </summary>
        public decimal TotalPortfolioValue { get; }

        /// <summary>
        /// Gets the total used margin
        /// </summary>
        public decimal TotalUsedMargin { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MarginCallOrdersParameters"/> class
        /// </summary>
        /// <param name="positionGroup">The position group</param>
        /// <param name="totalPortfolioValue">The algorithm's total portfolio value</param>
        /// <param name="totalUsedMargin">The total used margin</param>
        public MarginCallOrdersParameters(
            IPositionGroup positionGroup,
            decimal totalPortfolioValue,
            decimal totalUsedMargin
        )
        {
            PositionGroup = positionGroup;
            TotalPortfolioValue = totalPortfolioValue;
            TotalUsedMargin = totalUsedMargin;
        }
    }
}
