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


namespace QuantConnect.Orders.Fees
{
    /// <summary>
    /// Provides the default implementation of <see cref="IFeeModel"/>
    /// </summary>
    public class InteractiveBrokersFeeModel : FeeModel
    {
        private readonly decimal _forexCommissionRate;
        private readonly decimal _forexMinimumOrderFee;

        // option commission function takes number of contracts and the size of the option premium and returns total commission
        private readonly Func<decimal, decimal, decimal>  _optionsCommissionFunc;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImmediateFillModel"/>
        /// </summary>
        /// <param name="monthlyForexTradeAmountInUSDollars">Monthly FX dollar volume traded</param>
        /// <param name="monthlyOptionsTradeAmountInContracts">Monthly options contracts traded</param>
        public InteractiveBrokersFeeModel(decimal monthlyForexTradeAmountInUSDollars = 0, decimal monthlyOptionsTradeAmountInContracts = 0)
        {
            ProcessForexRateSchedule(monthlyForexTradeAmountInUSDollars, out _forexCommissionRate, out _forexMinimumOrderFee);
            ProcessOptionsRateSchedule(monthlyOptionsTradeAmountInContracts, out _optionsCommissionFunc);
        }

        /// <summary>
        /// Gets the order fee associated with the specified order. This returns the cost
        /// of the transaction in the account currency
        /// </summary>
        /// <param name="parameters">A <see cref="OrderFeeParameters"/> object
        /// containing the security and order</param>
        /// <returns>The cost of the order in units of the account currency</returns>
        public override OrderFee GetOrderFee(OrderFeeParameters parameters)
        {
            var order = parameters.Order;
            var security = parameters.Security;

            // Option exercise for equity options is free of charge
            if (order.Type == OrderType.OptionExercise)
            {
                var optionOrder = (OptionExerciseOrder)order;

                if (optionOrder.Symbol.ID.SecurityType == SecurityType.Option &&
                    optionOrder.Symbol.ID.Underlying.SecurityType == SecurityType.Equity)
                {
                    return new OrderFee(new CashAmount(
                        0,
                        security.QuoteCurrency.AccountCurrency));
                }
            }

            decimal feeResult = 0;
            switch (security.Type)
            {
                case SecurityType.Forex:
                    // get the total order value in the account currency
                    var totalOrderValue = order.GetValue(security);
                    var fee = Math.Abs(_forexCommissionRate*totalOrderValue);
                    feeResult = Math.Max(_forexMinimumOrderFee, fee);
                    break;
                case SecurityType.Option:
                    // applying commission function to the order
                    feeResult = _optionsCommissionFunc(order.AbsoluteQuantity, order.Price);
                    break;
                case SecurityType.Future:
                    // currently we treat all futures as USD denominated generic US futures
                    feeResult = order.AbsoluteQuantity * (0.85m + 1.0m);
                    break;
                case SecurityType.Equity:
                    var tradeValue = Math.Abs(order.GetValue(security));

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
                    feeResult = Math.Abs(tradeFee);
                    break;
            }

            // all other types default to zero fees
            return new OrderFee(new CashAmount(
                feeResult,
                security.QuoteCurrency.AccountCurrency));
        }

        /// <summary>
        /// Determines which tier an account falls into based on the monthly trading volume
        /// </summary>
        private static void ProcessForexRateSchedule(decimal monthlyForexTradeAmountInUSDollars, out decimal commissionRate, out decimal minimumOrderFee)
        {
            const decimal bp = 0.0001m;
            if (monthlyForexTradeAmountInUSDollars <= 1000000000)      // 1 billion
            {
                commissionRate = 0.20m * bp;
                minimumOrderFee = 2.00m;
            }
            else if (monthlyForexTradeAmountInUSDollars <= 2000000000) // 2 billion
            {
                commissionRate = 0.15m * bp;
                minimumOrderFee = 1.50m;
            }
            else if (monthlyForexTradeAmountInUSDollars <= 5000000000) // 5 billion
            {
                commissionRate = 0.10m * bp;
                minimumOrderFee = 1.25m;
            }
            else
            {
                commissionRate = 0.08m * bp;
                minimumOrderFee = 1.00m;
            }
        }

        /// <summary>
        /// Determines which tier an account falls into based on the monthly trading volume
        /// </summary>
        private static void ProcessOptionsRateSchedule(decimal monthlyOptionsTradeAmountInContracts, out Func<decimal, decimal, decimal> optionsCommissionFunc)
        {
            const decimal bp = 0.0001m;
            if (monthlyOptionsTradeAmountInContracts <= 10000)
            {
                optionsCommissionFunc = (orderSize, premium) =>
                {
                    var commissionRate = premium >= 0.1m ?
                                            0.7m :
                                            (0.05m <= premium && premium < 0.1m ? 0.5m : 0.25m);
                    return Math.Min(orderSize * commissionRate, 1.0m);
                };
            }
            else if (monthlyOptionsTradeAmountInContracts <= 50000)
            {
                optionsCommissionFunc = (orderSize, premium) =>
                {
                    var commissionRate = premium >= 0.05m ? 0.5m : 0.25m;
                    return Math.Min(orderSize * commissionRate, 1.0m);
                };
            }
            else if (monthlyOptionsTradeAmountInContracts <= 100000)
            {
                optionsCommissionFunc = (orderSize, premium) =>
                {
                    var commissionRate = 0.25m;
                    return Math.Min(orderSize * commissionRate, 1.0m);
                };
            }
            else
            {
                optionsCommissionFunc = (orderSize, premium) =>
                {
                    var commissionRate = 0.15m;
                    return Math.Min(orderSize * commissionRate, 1.0m);
                };
            }
        }
    }
}
