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
using QuantConnect.Orders;
using QuantConnect.Securities.Interfaces;

namespace QuantConnect.Securities.Equity 
{
    /// <summary>
    /// Transaction model for equity security trades. 
    /// </summary>
    /// <seealso cref="SecurityTransactionModel"/>
    /// <seealso cref="ISecurityTransactionModel"/>
    public class EquityTransactionModel : SecurityTransactionModel 
    {
        /// <summary>
        /// Uses the Interactive Brokers equities fixes fee schedule.
        /// </summary>
        /// <remarks>
        /// Default implementation uses the Interactive Brokers fee model of 0.5c per share with a maximum of 0.5% per order
        /// and minimum of $1.00.
        /// </remarks>
        /// <param name="security">The security matching the order</param>
        /// <param name="order">The order to compute fees for</param>
        /// <returns>The cost of the order in units of the account currency</returns>
        public override decimal GetOrderFee(Security security, Order order)
        {
            var price = order.Status.IsFill() ? order.Price : security.Price;
            var tradeValue = Math.Abs(order.GetValue(price));

            //Per share fees
            var tradeFee = 0.005m*order.AbsoluteQuantity;

            //Maximum Per Order: 0.5%
            //Minimum per order. $1.0
            var maximumPerOrder = 0.005m*tradeValue;
            if (tradeFee < 1)
            {
                tradeFee = 1;
            }
            else if (tradeFee > maximumPerOrder)
            {
                tradeFee = maximumPerOrder;
            }

            //Always return a positive fee.
            return Math.Abs(tradeFee);
        }
    }
}
