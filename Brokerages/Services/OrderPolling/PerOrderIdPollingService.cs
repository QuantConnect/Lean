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
    /// For a broker with a get-order endpoint. A sweep calls the read once per watched brokerage id -
    /// no request when nothing is watched. A null return means the broker does not know the id, so the
    /// watch timeout keeps counting.
    /// </summary>
    public class PerOrderIdPollingService : BrokerageOrderPollingService
    {
        /// <summary>
        /// Reads the current state of one order by its brokerage id.
        /// </summary>
        private readonly Func<string, BrokerOrderState> _readOrder;

        /// <summary>
        /// Creates a new <see cref="PerOrderIdPollingService"/>.
        /// </summary>
        /// <param name="readOrder">Reads the current state of one order by its brokerage id. A null
        /// return means the broker does not know the id.</param>
        /// <param name="messageHandler">The brokerage's message handler; the service registers itself and
        /// enqueues every polled state through it. Null processes each state directly.</param>
        /// <param name="orderProvider">Resolves brokerage order ids to Lean orders.</param>
        /// <param name="pollInterval">How long the loop sleeps between sweeps. Null falls back to the
        /// <c>brokerage-order-poll-interval-ms</c> configuration entry, default 3000 ms.</param>
        /// <param name="watchTimeout">How long a watched order may stay unreported before
        /// <see cref="BrokerageOrderPollingService.BrokerageOrderNeverNotified"/> is raised. Null falls back to one minute.</param>
        public PerOrderIdPollingService(Func<string, BrokerOrderState> readOrder, BrokerageConcurrentMessageHandler messageHandler,
            IOrderProvider orderProvider, TimeSpan? pollInterval = null, TimeSpan? watchTimeout = null)
            : base(messageHandler, orderProvider, pollInterval, watchTimeout)
        {
            _readOrder = readOrder;
        }

        /// <summary>
        /// Calls the read once per watched brokerage id. One id whose read throws is logged and skipped,
        /// so it cannot starve the other watched orders; the sweep only counts as failed when every read
        /// of the sweep failed.
        /// </summary>
        protected override IEnumerable<BrokerOrderState> Sweep()
        {
            var brokerageIds = GetWatchedBrokerageIds();
            var orderStates = new List<BrokerOrderState>(brokerageIds.Count);
            var failedReadCount = 0;
            var lastError = default(Exception);
            foreach (var brokerageId in brokerageIds)
            {
                try
                {
                    orderStates.Add(_readOrder(brokerageId));
                }
                catch (Exception ex)
                {
                    failedReadCount++;
                    lastError = ex;
                    Log.Error($"{nameof(PerOrderIdPollingService)}.{nameof(Sweep)}(): failed to read order '{brokerageId}': {ex.Message}");
                }
            }

            if (failedReadCount > 0 && failedReadCount == brokerageIds.Count)
            {
                throw lastError;
            }

            return orderStates;
        }
    }
}
