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

using QuantConnect.Orders;

namespace QuantConnect.Securities.Equity
{
    /// <summary>
    /// Represents a simple margining model where margin/leverage depends on market state (open or close).
    /// During regular market hours, leverage is 4x, otherwise 2x
    /// </summary>
    public class PatternDayTradingMarginModel : SecurityMarginModel
    {
        private decimal _openmarketleverage = 4.0m;
        private decimal _closedmarketleverage = 2.0m;

        /// <summary>
        /// Initializes a new instance of the <see cref="PatternDayTradingMarginModel" />
        /// </summary>
        /// <remarks>
        /// Set the base leverage to 4x (regular trading hours)
        /// </remarks>
        public PatternDayTradingMarginModel()
            : base(4.0m)
        {
        }

        public PatternDayTradingMarginModel(decimal closedmarketleverage, decimal openmarketleverage):
            base(openmarketleverage)
        {
            _closedmarketleverage = closedmarketleverage;
            _openmarketleverage = openmarketleverage;           
        }

        /// <summary>
        /// Sets the leverage for the applicable securities, i.e, equities
        /// </summary>
        /// <remarks>
        /// Do nothing, we use a constant leverage for this model
        /// </remarks>
        /// <param name="security"></param>
        /// <param name="leverage">The new leverage</param>
        public override void SetLeverage(Security security, decimal leverage)
        {
        }

        /// <summary>
        /// Gets the total margin required to execute the specified order in units of the account currency including fees
        /// </summary>
        /// <remarks>
        /// If we are in regular market hours, base 4x leverage is used, otherwise leverage is reduced to 2x
        /// </remarks>
        /// <param name="security">The security to compute initial margin for</param>
        /// <param name="order">The order to be executed</param>
        /// <returns>The total margin in terms of the currency quoted in the order</returns>
        public override decimal GetInitialMarginRequiredForOrder(Security security, Order order)
        {
            var closedopenratio = security.Exchange.ExchangeOpen ? 1 : _closedmarketleverage/_openmarketleverage;

            var orderFees = security.FeeModel.GetOrderFee(security, order);

            var price = order.Status.IsFill() ? order.Price : security.Price;
            return order.GetValue(price) * InitialMarginRequirement / closedopenratio + orderFees;        
        }
    }
}