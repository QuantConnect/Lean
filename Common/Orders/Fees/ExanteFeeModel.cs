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
using QuantConnect.Securities;

namespace QuantConnect.Orders.Fees
{
    /// <summary>
    /// Provides an implementation of <see cref="FeeModel"/> that models Exante order fees.
    /// According to:
    /// <list type="bullet">
    ///   <item>https://support.exante.eu/hc/en-us/articles/115005873143-Fees-overview-exchange-imposed-fees?source=search</item>
    ///   <item>https://exante.eu/markets/</item>
    /// </list>
    /// </summary>
    public class ExanteFeeModel : FeeModel
    {
        /// <summary>
        /// Market USA rate
        /// </summary>
        public const decimal MarketUsaRate = 0.02m;

        /// <summary>
        /// Default rate
        /// </summary>
        public const decimal DefaultRate = 0.02m;

        private readonly decimal _forexCommissionRate;

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="forexCommissionRate">Commission rate for FX operations</param>
        public ExanteFeeModel(decimal forexCommissionRate = 0.25m)
        {
            _forexCommissionRate = forexCommissionRate;
        }

        /// <summary>
        /// Gets the order fee associated with the specified order.
        /// </summary>
        /// <param name="parameters">A <see cref="OrderFeeParameters"/> object
        /// containing the security and order</param>
        /// <returns>The cost of the order in a <see cref="CashAmount"/> instance</returns>
        public override OrderFee GetOrderFee(OrderFeeParameters parameters)
        {
            var order = parameters.Order;
            var security = parameters.Security;

            decimal feeResult;
            string feeCurrency;
            switch (security.Type)
            {
                case SecurityType.Forex:
                    var totalOrderValue = order.GetValue(security);
                    feeResult = Math.Abs(_forexCommissionRate * totalOrderValue);
                    feeCurrency = Currencies.USD;
                    break;

                case SecurityType.Equity:
                    var equityFee = ComputeEquityFee(order);
                    feeResult = equityFee.Amount;
                    feeCurrency = equityFee.Currency;
                    break;

                case SecurityType.Option:
                case SecurityType.IndexOption:
                    var optionsFee = ComputeOptionFee(order);
                    feeResult = optionsFee.Amount;
                    feeCurrency = optionsFee.Currency;
                    break;

                case SecurityType.Future:
                case SecurityType.FutureOption:
                    feeResult = 1.5m;
                    feeCurrency = Currencies.USD;
                    break;

                default:
                    throw new ArgumentException(Messages.FeeModel.UnsupportedSecurityType(security));
            }

            return new OrderFee(new CashAmount(feeResult, feeCurrency));
        }

        /// <summary>
        /// Computes fee for equity order
        /// </summary>
        /// <param name="order">LEAN order</param>
        private static CashAmount ComputeEquityFee(Order order)
        {
            switch (order.Symbol.ID.Market)
            {
                case Market.USA:
                    return new CashAmount(order.AbsoluteQuantity * MarketUsaRate, Currencies.USD);

                default:
                    return new CashAmount(order.AbsoluteQuantity * order.Price * DefaultRate, Currencies.USD);
            }
        }

        /// <summary>
        /// Computes fee for option order
        /// </summary>
        /// <param name="order">LEAN order</param>
        private static CashAmount ComputeOptionFee(Order order)
        {
            return order.Symbol.ID.Market switch
            {
                Market.USA => new CashAmount(order.AbsoluteQuantity * 1.5m, Currencies.USD),
                _ =>
                    // ToDo: clarify the value for different exchanges
                    throw new ArgumentException(Messages.ExanteFeeModel.UnsupportedExchange(order))
            };
        }
    }
}
