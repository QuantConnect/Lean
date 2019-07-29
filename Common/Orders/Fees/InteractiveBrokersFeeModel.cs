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
using System.Collections.Generic;
using QuantConnect.Orders.Fills;
using QuantConnect.Securities;
using static QuantConnect.StringExtensions;

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
        private readonly Dictionary<string, Func<decimal, decimal, CashAmount>> _optionFee =
            new Dictionary<string, Func<decimal, decimal, CashAmount>>();

        private readonly Dictionary<string, EquityFee> _equityFee =
            new Dictionary<string, EquityFee> {
                { Market.USA, new EquityFee("USD", feePerShare: 0.005m, minimumFee: 1, maximumFeeRate: 0.005m) }
            };

        private readonly Dictionary<string, CashAmount> _futureFee =
            //                                                               IB fee + exchange fee
            new Dictionary<string, CashAmount> { { Market.USA, new CashAmount(0.85m + 1, "USD") } };

        /// <summary>
        /// Initializes a new instance of the <see cref="ImmediateFillModel"/>
        /// </summary>
        /// <param name="monthlyForexTradeAmountInUSDollars">Monthly FX dollar volume traded</param>
        /// <param name="monthlyOptionsTradeAmountInContracts">Monthly options contracts traded</param>
        public InteractiveBrokersFeeModel(decimal monthlyForexTradeAmountInUSDollars = 0, decimal monthlyOptionsTradeAmountInContracts = 0)
        {
            ProcessForexRateSchedule(monthlyForexTradeAmountInUSDollars, out _forexCommissionRate, out _forexMinimumOrderFee);
            Func<decimal, decimal, CashAmount> optionsCommissionFunc;
            ProcessOptionsRateSchedule(monthlyOptionsTradeAmountInContracts, out optionsCommissionFunc);
            // only USA for now
            _optionFee.Add(Market.USA, optionsCommissionFunc);
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
                    return OrderFee.Zero;
                }
            }

            decimal feeResult;
            string feeCurrency;
            var market = security.Symbol.ID.Market;
            switch (security.Type)
            {
                case SecurityType.Forex:
                    // get the total order value in the account currency
                    var totalOrderValue = order.GetValue(security);
                    var fee = Math.Abs(_forexCommissionRate*totalOrderValue);
                    feeResult = Math.Max(_forexMinimumOrderFee, fee);
                    // IB Forex fees are all in USD
                    feeCurrency = Currencies.USD;
                    break;

                case SecurityType.Option:
                    Func<decimal, decimal, CashAmount> optionsCommissionFunc;
                    if (!_optionFee.TryGetValue(market, out optionsCommissionFunc))
                    {
                        throw new KeyNotFoundException($"InteractiveBrokersFeeModel(): unexpected option Market {market}");
                    }
                    // applying commission function to the order
                    var optionFee = optionsCommissionFunc(order.AbsoluteQuantity, order.Price);
                    feeResult = optionFee.Amount;
                    feeCurrency = optionFee.Currency;
                    break;

                case SecurityType.Future:
                    if (market == Market.Globex || market == Market.NYMEX
                        || market == Market.CBOT || market == Market.ICE
                        || market == Market.CBOE || market == Market.NSE)
                    {
                        // just in case...
                        market = Market.USA;
                    }

                    CashAmount feeRatePerContract;
                    if (!_futureFee.TryGetValue(market, out feeRatePerContract))
                    {
                        throw new KeyNotFoundException($"InteractiveBrokersFeeModel(): unexpected future Market {market}");
                    }
                    feeResult = order.AbsoluteQuantity * feeRatePerContract.Amount;
                    feeCurrency = feeRatePerContract.Currency;
                    break;

                case SecurityType.Equity:
                    EquityFee equityFee;
                    if (!_equityFee.TryGetValue(market, out equityFee))
                    {
                        throw new KeyNotFoundException($"InteractiveBrokersFeeModel(): unexpected equity Market {market}");
                    }
                    var tradeValue = Math.Abs(order.GetValue(security));

                    //Per share fees
                    var tradeFee = equityFee.FeePerShare * order.AbsoluteQuantity;

                    //Maximum Per Order: equityFee.MaximumFeeRate
                    //Minimum per order. $equityFee.MinimumFee
                    var maximumPerOrder = equityFee.MaximumFeeRate * tradeValue;
                    if (tradeFee < equityFee.MinimumFee)
                    {
                        tradeFee = equityFee.MinimumFee;
                    }
                    else if (tradeFee > maximumPerOrder)
                    {
                        tradeFee = maximumPerOrder;
                    }

                    feeCurrency = equityFee.Currency;
                    //Always return a positive fee.
                    feeResult = Math.Abs(tradeFee);
                    break;

                default:
                    // unsupported security type
                    throw new ArgumentException(Invariant($"Unsupported security type: {security.Type}"));
            }

            return new OrderFee(new CashAmount(
                feeResult,
                feeCurrency));
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
        private static void ProcessOptionsRateSchedule(decimal monthlyOptionsTradeAmountInContracts, out Func<decimal, decimal, CashAmount> optionsCommissionFunc)
        {
            const decimal bp = 0.0001m;
            if (monthlyOptionsTradeAmountInContracts <= 10000)
            {
                optionsCommissionFunc = (orderSize, premium) =>
                {
                    var commissionRate = premium >= 0.1m ?
                                            0.7m :
                                            (0.05m <= premium && premium < 0.1m ? 0.5m : 0.25m);
                    return new CashAmount(Math.Max(orderSize * commissionRate, 1.0m), Currencies.USD);
                };
            }
            else if (monthlyOptionsTradeAmountInContracts <= 50000)
            {
                optionsCommissionFunc = (orderSize, premium) =>
                {
                    var commissionRate = premium >= 0.05m ? 0.5m : 0.25m;
                    return new CashAmount(Math.Max(orderSize * commissionRate, 1.0m), Currencies.USD);
                };
            }
            else if (monthlyOptionsTradeAmountInContracts <= 100000)
            {
                optionsCommissionFunc = (orderSize, premium) =>
                {
                    var commissionRate = 0.25m;
                    return new CashAmount(Math.Max(orderSize * commissionRate, 1.0m), Currencies.USD);
                };
            }
            else
            {
                optionsCommissionFunc = (orderSize, premium) =>
                {
                    var commissionRate = 0.15m;
                    return new CashAmount(Math.Max(orderSize * commissionRate, 1.0m), Currencies.USD);
                };
            }
        }

        /// <summary>
        /// Helper class to handle IB Equity fees
        /// </summary>
        private class EquityFee
        {
            public string Currency { get; }
            public decimal FeePerShare { get; }
            public decimal MinimumFee { get; }
            public decimal MaximumFeeRate { get; }

            public EquityFee(string currency,
                decimal feePerShare,
                decimal minimumFee,
                decimal maximumFeeRate)
            {
                Currency = currency;
                FeePerShare = feePerShare;
                MinimumFee = minimumFee;
                MaximumFeeRate = maximumFeeRate;
            }
        }
    }
}
