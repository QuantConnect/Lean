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

namespace QuantConnect.Brokerages.CrossZero
{
    /// <summary>
    /// Represents a second request to cross zero order.
    /// </summary>
    public class CrossZeroSecondOrderRequest : CrossZeroFirstOrderRequest
    {
        /// <summary>
        /// Gets or sets the first part of CrossZeroOrder.
        /// </summary>
        public CrossZeroFirstOrderRequest FirstPartCrossZeroOrder { get; }

        /// <summary>
        /// Gets the type of order, converted to stop-crossing order type.
        /// </summary>
        /// <returns>
        /// The converted stop-crossing order type.
        /// </returns>
        public new OrderType OrderType => ConvertStopCrossingOrderType(base.OrderType);

        /// <summary>
        /// Initializes a new instance of the <see cref="CrossZeroFirstOrderRequest"/> struct.
        /// </summary>
        /// <param name="leanOrder">The lean order.</param>
        /// <param name="orderType">The type of the order.</param>
        /// <param name="orderQuantity">The quantity of the order.</param>
        /// <param name="orderQuantityHolding">The current holding quantity of the order's symbol.</param>
        /// <param name="orderPosition">The position of the order, which depends on the <paramref name="orderQuantityHolding"/>.</param>
        /// <param name="crossZeroFirstOrder">The first part of the cross zero order.</param>
        public CrossZeroSecondOrderRequest(Order leanOrder, OrderType orderType, decimal orderQuantity, decimal orderQuantityHolding,
            OrderPosition orderPosition, CrossZeroFirstOrderRequest crossZeroFirstOrder)
            : base(leanOrder, orderType, orderQuantity, orderQuantityHolding, orderPosition)
        {
            FirstPartCrossZeroOrder = crossZeroFirstOrder;
        }

        /// <summary>
        /// Converts a stop order type to its corresponding market or limit order type.
        /// </summary>
        /// <param name="orderType">The original order type to be converted.</param>
        /// <returns>
        /// The converted order type. If the original order type is <see cref="OrderType.StopMarket"/>, 
        /// it returns <see cref="OrderType.Market"/>. If the original order type is <see cref="OrderType.StopLimit"/>,
        /// it returns <see cref="OrderType.Limit"/>. Otherwise, it returns the original order type.
        /// </returns>
        private static OrderType ConvertStopCrossingOrderType(OrderType orderType) => orderType switch
        {
            OrderType.StopMarket => OrderType.Market,
            OrderType.StopLimit => OrderType.Limit,
            _ => orderType
        };
    }
}
