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

namespace QuantConnect.Orders.Fees
{
    /// <summary>
    /// Provides an implementation of <see cref="FeeModel"/> that models order fees that alpha stream clients pay/receive
    /// </summary>
    public class AlphaStreamsFeeModel : FeeModel
    {
        private readonly Security _libor;

        private readonly IDictionary<SecurityType, decimal> _feeRates = new Dictionary<SecurityType, decimal>
        {
            {SecurityType.Equity, 0.004m},
            {SecurityType.Forex, 0.000002m},
            // Commission plus clearing fee
            {SecurityType.Future, 0.4m + 0.1m},
            {SecurityType.Option, 0.4m + 0.1m}
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="AlphaStreamsFeeModel"/>
        /// </summary>
        /// <param name="libor">Average interest rate at which major global banks borrow from one another</param>
        public AlphaStreamsFeeModel(Security libor = null)
        {
            _libor = libor;
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

            // Option exercise is free of charge
            if (order.Type == OrderType.OptionExercise)
            {
                return OrderFee.Zero;
            }

            decimal feeRate;

            if (!_feeRates.TryGetValue(security.Type, out feeRate))
            {
                throw new ArgumentException($"Unsupported security type: {security.Type}");
            }

            var value = security.Type == SecurityType.Equity || security.Type == SecurityType.Forex
                ? Math.Abs(order.GetValue(security))
                : order.AbsoluteQuantity;

            // The LIBOR is taken into account for Equity trading
            // Long positions on margin pays LIBOR
            // Short positions on margin receives LIBOR
            if (security.Type == SecurityType.Equity)
            {
                if (_libor == null)
                {
                    throw new ArgumentNullException($"AlphaStreamsFeeModel.GetOrderFee(): LIBOR security cannot be null for fee calculation of equity orders");
                }

                if (order.Direction == OrderDirection.Buy)
                {
                    feeRate += _libor.Price;
                }
                else
                {
                    feeRate -= _libor.Price;
                }
            }

            return new OrderFee(new CashAmount(feeRate * value, Currencies.USD));
        }
    }
}