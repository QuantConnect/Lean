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

using QuantConnect.Securities;
using System;
using System.Collections.Generic;
using static QuantConnect.StringExtensions;

namespace QuantConnect.Orders.Fees
{
    /// <summary>
    /// Provides an implementation of <see cref="FeeModel"/> that models order fees that alpha stream clients pay/receive
    /// </summary>
    public class AlphaStreamsFeeModel : FeeModel
    {
        private readonly IDictionary<SecurityType, decimal> _feeRates = new Dictionary<SecurityType, decimal>
        {
            {SecurityType.Equity, 0m},
            {SecurityType.Forex, 0.000002m},
            // Commission plus clearing fee
            {SecurityType.Future, 0.4m + 0.1m},
            {SecurityType.Option, 0.4m + 0.1m},
            {SecurityType.Cfd, 0m}
        };
        private const decimal _makerFee = 0.001m;
        private const decimal _takerFee = 0.002m;

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

            // Option exercise is free of charge
            if (order.Type == OrderType.OptionExercise)
            {
                return OrderFee.Zero;
            }

            if (security.Type == SecurityType.Crypto)
            {
                decimal fee = _takerFee;
                var props = order.Properties as BitfinexOrderProperties;

                if (order.Type == OrderType.Limit &&
                    props?.Hidden != true &&
                    (props?.PostOnly == true || !order.IsMarketable))
                {
                    // limit order posted to the order book
                    fee = _makerFee;
                }

                // get order value in quote currency
                var unitPrice = order.Direction == OrderDirection.Buy ? security.AskPrice : security.BidPrice;
                if (order.Type == OrderType.Limit)
                {
                    // limit order posted to the order book
                    unitPrice = ((LimitOrder)order).LimitPrice;
                }

                unitPrice *= security.SymbolProperties.ContractMultiplier;

                // apply fee factor, currently we do not model 30-day volume, so we use the first tier
                return new OrderFee(new CashAmount(
                    unitPrice * order.AbsoluteQuantity * fee,
                    security.QuoteCurrency.Symbol));
            }

            decimal feeRate;

            if (!_feeRates.TryGetValue(security.Type, out feeRate))
            {
                throw new ArgumentException(
                    Invariant($"Unsupported security type: {security.Type}.")
                );
            }

            var value = order.AbsoluteQuantity;

            switch (security.Type)
            {
                case SecurityType.Equity:
                    value = order.GetValue(security);
                    break;
                case SecurityType.Forex:
                    value = Math.Abs(order.GetValue(security));
                    break;
            }

            return new OrderFee(new CashAmount(feeRate * value, Currencies.USD));
        }
    }
}