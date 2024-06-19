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
 *
*/

using System;
using QuantConnect.Securities;

namespace QuantConnect.Orders.Fees
{
    /// <summary>
    /// Represents the fee model specific to Alpaca trading platform.
    /// </summary>
    /// <remarks>This class inherits from <see cref="FeeModel"/> and provides the fee structure for Alpaca trades.
    /// It implements the <see cref="IFeeModel"/> interface and should be used for calculating fees on the Alpaca platform.</remarks>
    public class AlpacaFeeModel : FeeModel
    {
        /// <summary>
        /// The fee percentage for a maker transaction in cryptocurrency.
        /// </summary>
        /// <see href="https://docs.alpaca.markets/docs/crypto-fees"/>
        public const decimal MakerCryptoFee = 0.15m;

        /// <summary>
        /// The fee percentage for a taker transaction in cryptocurrency.
        /// </summary>
        public const decimal TakerCryptoFee = 0.25m;

        /// <inheritdoc cref="IFeeModel.GetOrderFee(OrderFeeParameters)"/>
        public override OrderFee GetOrderFee(OrderFeeParameters parameters)
        {
            if (parameters == null)
            {
                throw new ArgumentNullException(nameof(parameters), "The order fee parameters cannot be null. Please provide valid parameters to calculate the order fee.");
            }

            var security = parameters.Security;
            if (security.Symbol.ID.SecurityType == SecurityType.Crypto)
            {
                var order = parameters.Order;
                var fee = GetFee(order, MakerCryptoFee, TakerCryptoFee);
                var positionValue = security.Holdings.GetQuantityValue(order.AbsoluteQuantity, security.Price);
                return new OrderFee(new CashAmount(positionValue.Amount * fee, positionValue.Cash.Symbol));
            }

            return base.GetOrderFee(parameters);
        }

        /// <summary>
        /// Calculates the fee for a given order based on whether it is a maker or taker order.
        /// </summary>
        /// <param name="order">The order for which the fee is being calculated.</param>
        /// <param name="makerFee">The fee percentage for maker orders.</param>
        /// <param name="takerFee">The fee percentage for taker orders.</param>
        /// <returns>The calculated fee for the given order.</returns>
        private static decimal GetFee(Order order, decimal makerFee, decimal takerFee)
        {
            var fee = takerFee;
            if (order.Type == OrderType.Limit && !order.IsMarketable)
            {
                fee = makerFee;
            }

            return fee;
        }
    }
}
