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
using QuantConnect.Orders.Fills;
using QuantConnect.Securities;
using QuantConnect.Securities.Forex;

namespace QuantConnect.Orders.Fees
{
    /// <summary>
    /// Provides the default implementation of <see cref="IFeeModel"/>
    /// </summary>
    public class InteractiveBrokersFeeModel : IFeeModel
    {
        private readonly decimal _commissionRate;
        private readonly decimal _minimumOrderFee;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImmediateFillModel"/>
        /// </summary>
        /// <param name="monthlyTradeAmountInUSDollars">Monthly dollar volume traded</param>
        public InteractiveBrokersFeeModel(decimal monthlyTradeAmountInUSDollars = 0)
        {
            const decimal bp = 0.0001m;
            if (monthlyTradeAmountInUSDollars <= 1000000000) // 1 billion
            {
                _commissionRate = 0.20m * bp;
                _minimumOrderFee = 2.00m;
            }
            else if (monthlyTradeAmountInUSDollars <= 2000000000) // 2 billion
            {
                _commissionRate = 0.15m * bp;
                _minimumOrderFee = 1.50m;
            }
            else if (monthlyTradeAmountInUSDollars <= 5000000000) // 5 billion
            {
                _commissionRate = 0.10m * bp;
                _minimumOrderFee = 1.25m;
            }
            else
            {
                _commissionRate = 0.08m * bp;
                _minimumOrderFee = 1.00m;
            }
        }

        /// <summary>
        /// Gets the order fee associated with the specified order. This returns the cost
        /// of the transaction in the account currency
        /// </summary>
        /// <param name="security">The security matching the order</param>
        /// <param name="order">The order to compute fees for</param>
        /// <returns>The cost of the order in units of the account currency</returns>
        public decimal GetOrderFee(Security security, Order order)
        {
            if (security.Type == SecurityType.Forex)
            {
                var forex = (Forex)security;

                // get the total order value in the account currency
                var price = order.Status.IsFill() ? order.Price : security.Price;
                var totalOrderValue = order.GetValue(price) * forex.QuoteCurrency.ConversionRate;
                var fee = Math.Abs(_commissionRate*totalOrderValue);
                return Math.Max(_minimumOrderFee, fee);
            }

            if (security.Type == SecurityType.Equity)
            {
                var price = order.Status.IsFill() ? order.Price : security.Price;
                var tradeValue = Math.Abs(order.GetValue(price));

                //Per share fees
                var tradeFee = 0.005m * order.AbsoluteQuantity;

                //Maximum Per Order: 0.5%
                //Minimum per order. $1.0
                var maximumPerOrder = 0.005m * tradeValue;
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

            // all other types default to zero fees
            return 0m;
        }
    }
}
