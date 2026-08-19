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
using System.Collections.Generic;
using QuantConnect.Brokerages.Services.OrderPolling.Models;

namespace QuantConnect.Brokerages.Services.OrderPolling
{
    /// <summary>
    /// For a broker with only a bulk endpoint. A poll calls the read once, whatever is watched, and the
    /// read returns everything the broker lists.
    /// </summary>
    /// <remarks>
    /// Use it when the broker cannot be asked about one order id, or when one request already returns the
    /// whole account cheaply. The poll runs even with nothing watched, so it can be the order path of a
    /// whole run.
    /// <code>
    /// CreateOrderPollingService(() => _api.GetAllOrders().Select(ToOrderSnapshot), _messageHandler, _orderProvider);
    /// </code>
    /// </remarks>
    public class BulkOrdersPollingService : BaseBrokerageOrderPollingService
    {
        /// <summary>
        /// Reads every order the broker lists.
        /// </summary>
        private readonly Func<IEnumerable<BrokerageOrderSnapshot>> _getAllBrokerageOrders;

        /// <summary>
        /// Creates a new <see cref="BulkOrdersPollingService"/> with the default poll interval and
        /// notification timeout.
        /// </summary>
        /// <param name="getAllBrokerageOrders">Reads every order the broker lists, one snapshot per brokerage order id.</param>
        /// <param name="messageHandler">Serializes the snapshots with the brokerage's other messages. Null processes them directly.</param>
        /// <param name="orderProvider">Resolves brokerage order ids to Lean orders.</param>
        public BulkOrdersPollingService(
            Func<IEnumerable<BrokerageOrderSnapshot>> getAllBrokerageOrders,
            BrokerageConcurrentMessageHandler messageHandler,
            IOrderProvider orderProvider)
            : this(getAllBrokerageOrders, messageHandler, orderProvider, pollInterval: null, notificationTimeout: null)
        {
        }

        /// <summary>
        /// Creates a new <see cref="BulkOrdersPollingService"/> with the default notification timeout.
        /// </summary>
        /// <param name="getAllBrokerageOrders">Reads every order the broker lists, one snapshot per brokerage order id.</param>
        /// <param name="messageHandler">Serializes the snapshots with the brokerage's other messages. Null processes them directly.</param>
        /// <param name="orderProvider">Resolves brokerage order ids to Lean orders.</param>
        /// <param name="pollInterval">The sleep between polls. Null takes <c>brokerage-order-poll-interval-ms</c>, default 3000 ms.</param>
        public BulkOrdersPollingService(
            Func<IEnumerable<BrokerageOrderSnapshot>> getAllBrokerageOrders,
            BrokerageConcurrentMessageHandler messageHandler,
            IOrderProvider orderProvider,
            TimeSpan? pollInterval)
            : this(getAllBrokerageOrders, messageHandler, orderProvider, pollInterval, notificationTimeout: null)
        {
        }

        /// <summary>
        /// Creates a new <see cref="BulkOrdersPollingService"/>.
        /// </summary>
        /// <param name="getAllBrokerageOrders">Reads every order the broker lists, one snapshot per brokerage order id.</param>
        /// <param name="messageHandler">Serializes the snapshots with the brokerage's other messages. Null processes them directly.</param>
        /// <param name="orderProvider">Resolves brokerage order ids to Lean orders.</param>
        /// <param name="pollInterval">The sleep between polls. Null takes <c>brokerage-order-poll-interval-ms</c>, default 3000 ms.</param>
        /// <param name="notificationTimeout">The silence that raises
        /// <see cref="BaseBrokerageOrderPollingService.BrokerageOrderNeverNotified"/> for a watched order. Null takes 60000 ms.</param>
        public BulkOrdersPollingService(
            Func<IEnumerable<BrokerageOrderSnapshot>> getAllBrokerageOrders,
            BrokerageConcurrentMessageHandler messageHandler,
            IOrderProvider orderProvider,
            TimeSpan? pollInterval,
            TimeSpan? notificationTimeout)
            : base(messageHandler, orderProvider, pollInterval, notificationTimeout)
        {
            _getAllBrokerageOrders = getAllBrokerageOrders;
        }

        /// <summary>
        /// Calls the read once per poll, one request for everything the broker lists.
        /// </summary>
        protected override IEnumerable<BrokerageOrderSnapshot> GetOrderSnapshots()
        {
            return _getAllBrokerageOrders();
        }
    }
}
