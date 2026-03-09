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
using System.Linq;
using QuantConnect.Orders;

namespace QuantConnect.Securities.Positions
{
    /// <summary>
    /// Defines the parameters for <see cref="IPositionGroupBuyingPowerModel.HasSufficientBuyingPowerForOrder"/>
    /// </summary>
    public class HasSufficientPositionGroupBuyingPowerForOrderParameters
    {
        /// <summary>
        /// The orders associated with this request
        /// </summary>
        public List<Order> Orders { get; }

        /// <summary>
        /// Gets the position group representing the holdings changes contemplated by the order
        /// </summary>
        public IPositionGroup PositionGroup { get; }

        /// <summary>
        /// Gets the algorithm's portfolio manager
        /// </summary>
        public SecurityPortfolioManager Portfolio { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="HasSufficientPositionGroupBuyingPowerForOrderParameters"/> class
        /// </summary>
        /// <param name="portfolio">The algorithm's portfolio manager</param>
        /// <param name="positionGroup">The position group</param>
        /// <param name="orders">The orders</param>
        public HasSufficientPositionGroupBuyingPowerForOrderParameters(
            SecurityPortfolioManager portfolio,
            IPositionGroup positionGroup,
            List<Order> orders
            )
        {
            Orders = orders;
            Portfolio = portfolio;
            PositionGroup = positionGroup;
        }

        /// <summary>
        /// This may be called for non-combo type orders where the position group is guaranteed to have exactly one position
        /// </summary>
        public static implicit operator HasSufficientBuyingPowerForOrderParameters(
            HasSufficientPositionGroupBuyingPowerForOrderParameters parameters
            )
        {
            var position = parameters.PositionGroup.Single();
            var security = parameters.Portfolio.Securities[position.Symbol];
            return new HasSufficientBuyingPowerForOrderParameters(parameters.Portfolio, security, parameters.Orders.Single());
        }

        /// <summary>
        /// Creates a new result indicating that there is sufficient buying power for the contemplated order
        /// </summary>
        public HasSufficientBuyingPowerForOrderResult Sufficient()
        {
            return new HasSufficientBuyingPowerForOrderResult(true);
        }

        /// <summary>
        /// Creates a new result indicating that there is insufficient buying power for the contemplated order
        /// </summary>
        public HasSufficientBuyingPowerForOrderResult Insufficient(string reason)
        {
            return new HasSufficientBuyingPowerForOrderResult(false, reason);
        }

        /// <summary>
        /// Creates a new result indicating that there was an error
        /// </summary>
        public HasSufficientBuyingPowerForOrderResult Error(string reason)
        {
            return new HasSufficientBuyingPowerForOrderResult(false, reason);
        }
    }
}
