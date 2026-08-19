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
using QuantConnect.Logging;
using QuantConnect.Securities;
using System.Collections.Generic;
using QuantConnect.Brokerages.Services.OrderPolling.Models;

namespace QuantConnect.Brokerages.Services.OrderPolling
{
    /// <summary>
    /// For a broker with a get-order endpoint. A poll calls the read once per subscribed brokerage id -
    /// no request when nothing is subscribed. A null return means the broker does not know the id, so the
    /// notification timeout keeps counting.
    /// </summary>
    /// <remarks>
    /// Use it when the broker can be asked about one order id. The poll reads only the subscribed orders,
    /// so an idle account sends no requests and rate limits stay untouched.
    /// <code>
    /// InitializeOrderPollingService(id => ToOrderSnapshot(_api.GetOrderById(id)), _messageHandler, _orderProvider);
    /// </code>
    /// </remarks>
    public class SingleOrderPollingService : BaseBrokerageOrderPollingService
    {
        /// <summary>
        /// Reads the current state of one order by its brokerage id.
        /// </summary>
        private readonly Func<string, BrokerageOrderSnapshot> _getBrokerageOrderById;

        /// <summary>
        /// Creates a new <see cref="SingleOrderPollingService"/> with the default poll interval and
        /// notification timeout.
        /// </summary>
        /// <param name="getBrokerageOrderById">Reads one order by its brokerage id. A null return means the broker does not know the id.</param>
        /// <param name="messageHandler">Serializes the snapshots with the brokerage's other messages. Null processes them directly.</param>
        /// <param name="orderProvider">Resolves brokerage order ids to Lean orders.</param>
        public SingleOrderPollingService(
            Func<string, BrokerageOrderSnapshot> getBrokerageOrderById,
            BrokerageConcurrentMessageHandler messageHandler,
            IOrderProvider orderProvider)
            : this(getBrokerageOrderById, messageHandler, orderProvider, pollInterval: null, notificationTimeout: null)
        {
        }

        /// <summary>
        /// Creates a new <see cref="SingleOrderPollingService"/> with the default notification timeout.
        /// </summary>
        /// <param name="getBrokerageOrderById">Reads one order by its brokerage id. A null return means the broker does not know the id.</param>
        /// <param name="messageHandler">Serializes the snapshots with the brokerage's other messages. Null processes them directly.</param>
        /// <param name="orderProvider">Resolves brokerage order ids to Lean orders.</param>
        /// <param name="pollInterval">The sleep between polls. Null takes <c>brokerage-order-poll-interval-ms</c>, default 3000 ms.</param>
        public SingleOrderPollingService(
            Func<string, BrokerageOrderSnapshot> getBrokerageOrderById,
            BrokerageConcurrentMessageHandler messageHandler,
            IOrderProvider orderProvider,
            TimeSpan? pollInterval)
            : this(getBrokerageOrderById, messageHandler, orderProvider, pollInterval, notificationTimeout: null)
        {
        }

        /// <summary>
        /// Creates a new <see cref="SingleOrderPollingService"/>.
        /// </summary>
        /// <param name="getBrokerageOrderById">Reads one order by its brokerage id. A null return means the broker does not know the id.</param>
        /// <param name="messageHandler">Serializes the snapshots with the brokerage's other messages. Null processes them directly.</param>
        /// <param name="orderProvider">Resolves brokerage order ids to Lean orders.</param>
        /// <param name="pollInterval">The sleep between polls. Null takes <c>brokerage-order-poll-interval-ms</c>, default 3000 ms.</param>
        /// <param name="notificationTimeout">The silence that raises
        /// <see cref="BaseBrokerageOrderPollingService.BrokerageOrderNeverNotified"/> for a subscribed order. Null takes 60000 ms.</param>
        public SingleOrderPollingService(
            Func<string, BrokerageOrderSnapshot> getBrokerageOrderById,
            BrokerageConcurrentMessageHandler messageHandler,
            IOrderProvider orderProvider,
            TimeSpan? pollInterval,
            TimeSpan? notificationTimeout)
            : base(messageHandler, orderProvider, pollInterval, notificationTimeout)
        {
            _getBrokerageOrderById = getBrokerageOrderById;
        }

        /// <summary>
        /// Calls the read once per subscribed brokerage id. One id whose read throws is logged and skipped,
        /// so it cannot block the other subscribed orders; the poll only counts as failed when every read failed.
        /// </summary>
        protected override IEnumerable<BrokerageOrderSnapshot> GetOrderSnapshots()
        {
            var orderStates = new List<BrokerageOrderSnapshot>();
            var readCount = 0;
            var failedReadCount = 0;
            var lastError = default(Exception);
            foreach (var brokerageId in GetOpenBrokerageIds())
            {
                readCount++;
                try
                {
                    orderStates.Add(_getBrokerageOrderById(brokerageId));
                }
                catch (Exception ex)
                {
                    failedReadCount++;
                    lastError = ex;
                    Log.Error($"{nameof(SingleOrderPollingService)}.{nameof(GetOrderSnapshots)}(): failed to read order '{brokerageId}': {ex.Message}");
                }
            }

            if (failedReadCount > 0 && failedReadCount == readCount)
            {
                throw lastError;
            }

            return orderStates;
        }
    }
}
