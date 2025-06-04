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

using QuantConnect.Brokerages;
using QuantConnect.Securities;
using System;

namespace QuantConnect.Orders.Fees
{
    /// <summary>
    /// Represents a fee model specific to Tastytrade.
    /// </summary>
    /// <see href="https://tastytrade.com/pricing/"/>
    public class TastytradeFeeModel : FeeModel
    {
        /// <summary>
        /// Represents the fee associated with equity options transactions (per contract).
        /// </summary>
        private const decimal _optionFeeOpen = 1m;

        /// <summary>
        /// The fee associated with futures transactions (per contract).
        /// </summary>
        private const decimal _futureFee = 1.25m;

        /// <summary>
        /// The fee associated with futures options transactions (per contract).
        /// </summary>
        private const decimal _futureOptionFeeOpen = 2.5m;

        /// <summary>
        /// Gets the order fee for a given security and order.
        /// </summary>
        /// <param name="parameters">The parameters including the security and order details.</param>
        /// <returns>
        /// A <see cref="OrderFee"/> instance representing the total fee for the order,
        /// or <see cref="OrderFee.Zero"/> if no fee is applicable.
        /// </returns>
        public override OrderFee GetOrderFee(OrderFeeParameters parameters)
        {
            var feeRate = default(decimal);
            switch (parameters.Security.Type)
            {
                case SecurityType.Option:
                case SecurityType.IndexOption:
                    feeRate = IsOpenPosition(parameters.Order.Direction, parameters.Security.Holdings.Quantity) ? _optionFeeOpen : 0m;
                    break;
                case SecurityType.Future:
                    feeRate = _futureFee;
                    break;
                case SecurityType.FutureOption:
                    feeRate = IsOpenPosition(parameters.Order.Direction, parameters.Security.Holdings.Quantity) ? _futureOptionFeeOpen : 0m;
                    break;
                default:
                    break;
            }

            return new OrderFee(new CashAmount(parameters.Order.AbsoluteQuantity * feeRate, Currencies.USD));
        }

        /// <summary>
        /// Determines whether the specified order represents the opening of a new position.
        /// </summary>
        /// <param name="orderDirection">The direction of the order (buy/sell).</param>
        /// <param name="holdingsQuantity">The current holdings quantity for the security.</param>
        /// <returns>
        /// <c>true</c> if the order is intended to open a new position; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="NotSupportedException">
        /// Thrown when the resolved <see cref="OrderPosition"/> is not recognized.
        /// </exception>
        private static bool IsOpenPosition(OrderDirection orderDirection, decimal holdingsQuantity)
        {
            var orderPosition = BrokerageExtensions.GetOrderPosition(orderDirection, holdingsQuantity);

            return orderPosition switch
            {
                OrderPosition.BuyToClose or OrderPosition.SellToClose => false,
                OrderPosition.BuyToOpen or OrderPosition.SellToOpen => true,
                _ => throw new NotSupportedException($"{nameof(TastytradeFeeModel)}.{nameof(IsOpenPosition)}: Unsupported order position: {orderPosition}")
            };
        }
    }
}
