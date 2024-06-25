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
using System;

namespace QuantConnect.Brokerages.CrossZero
{
    /// <summary>
    /// Represents a first request to cross zero order.
    /// </summary>
    public class CrossZeroFirstOrderRequest : ICrossZeroOrderRequest
    {
        /// <inheritdoc cref="ICrossZeroOrderRequest.LeanOrder"/>
        public Order LeanOrder { get; }

        /// <inheritdoc cref="ICrossZeroOrderRequest.OrderType"/>
        public OrderType OrderType { get; }

        /// <inheritdoc cref="ICrossZeroOrderRequest.OrderQuantity"/>
        public decimal OrderQuantity { get; }

        /// <inheritdoc cref="ICrossZeroOrderRequest.AbsoluteOrderQuantity"/>
        public decimal AbsoluteOrderQuantity => Math.Abs(OrderQuantity);

        /// <inheritdoc cref="ICrossZeroOrderRequest.OrderQuantityHolding"/>
        public decimal OrderQuantityHolding { get; }

        /// <inheritdoc cref="ICrossZeroOrderRequest.OrderPosition"/>
        public OrderPosition OrderPosition { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CrossZeroFirstOrderRequest"/> struct.
        /// </summary>
        /// <param name="leanOrder">The lean order.</param>
        /// <param name="orderType">The type of the order.</param>
        /// <param name="orderQuantity">The quantity of the order.</param>
        /// <param name="orderQuantityHolding">The current holding quantity of the order's symbol.</param>
        /// <param name="orderPosition">The position of the order, which depends on the <paramref name="orderQuantityHolding"/>.</param>
        public CrossZeroFirstOrderRequest(Order leanOrder, OrderType orderType, decimal orderQuantity, decimal orderQuantityHolding, OrderPosition orderPosition)
        {
            LeanOrder = leanOrder;
            OrderType = orderType;
            OrderQuantity = orderQuantity;
            OrderPosition = orderPosition;
            OrderQuantityHolding = orderQuantityHolding;
        }
    }
}
