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
    /// Represents a request for a cross zero order.
    /// </summary>
    public interface ICrossZeroOrderRequest
    {
        /// <summary>
        /// Gets the original lean order.
        /// </summary>
        public Order LeanOrder { get; }

        /// <summary>
        /// Gets the type of the order.
        /// </summary>
        public OrderType OrderType { get; }

        /// <summary>
        /// Gets the quantity of the order.
        /// </summary>
        public decimal OrderQuantity { get; }

        /// <summary>
        /// Gets the absolute quantity of the order.
        /// </summary>
        public decimal AbsoluteOrderQuantity { get; }

        /// <summary>
        /// Gets the current holding quantity of the order's symbol.
        /// </summary>
        public decimal OrderQuantityHolding { get; }

        /// <summary>
        /// Gets the position of the order.
        /// </summary>
        /// <value>
        /// The position of the order, which depends on the <see cref="OrderQuantityHolding"/>.
        /// </value>
        public OrderPosition OrderPosition { get; }
    }
}
