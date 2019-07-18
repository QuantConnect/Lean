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
using IBApi;

namespace QuantConnect.Brokerages.InteractiveBrokers.Client
{
    /// <summary>
    /// Event arguments class for the <see cref="InteractiveBrokersClient.OpenOrder"/> event
    /// </summary>
    public sealed class OpenOrderEventArgs : EventArgs
    {
        /// <summary>
        /// The order Id assigned by TWS. Used to cancel or update the order.
        /// </summary>
        public int OrderId { get; }

        /// <summary>
        /// The Contract class attributes describe the contract.
        /// </summary>
        public Contract Contract { get; }

        /// <summary>
        /// The Order class attributes define the details of the order.
        /// </summary>
        public Order Order { get; }

        /// <summary>
        /// The orderState attributes include margin and commissions fields for both pre and post trade data.
        /// </summary>
        public OrderState OrderState { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenOrderEventArgs"/> class
        /// </summary>
        public OpenOrderEventArgs(int orderId, Contract contract, Order order, OrderState orderState)
        {
            OrderId = orderId;
            Contract = contract;
            Order = order;
            OrderState = orderState;
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        public override string ToString()
        {
            return $"OrderId: {OrderId}, Contract: {Contract}, OrderStatus: {OrderState.Status}";
        }
    }
}