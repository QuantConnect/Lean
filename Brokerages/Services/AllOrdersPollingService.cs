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

namespace QuantConnect.Brokerages.Services
{
    /// <summary>
    /// For a broker with only a bulk endpoint. A sweep calls the read once, whatever is watched, and the
    /// read returns everything the broker lists.
    /// </summary>
    public class AllOrdersPollingService : BrokerageOrderPollingService
    {
        /// <summary>
        /// Reads every order the broker lists.
        /// </summary>
        private readonly Func<IEnumerable<BrokerOrderState>> _readAllOrders;

        /// <summary>
        /// Creates a new <see cref="AllOrdersPollingService"/>.
        /// </summary>
        /// <param name="readAllOrders">Reads every order the broker lists, one state per brokerage order id.</param>
        /// <param name="messageHandler">The brokerage's message handler; the service registers itself and
        /// enqueues every polled state through it. Null processes each state directly.</param>
        /// <param name="orderProvider">Resolves brokerage order ids to Lean orders.</param>
        /// <param name="pollInterval">How long the loop sleeps between sweeps. Null falls back to the
        /// <c>brokerage-order-poll-interval-ms</c> configuration entry, default 3000 ms.</param>
        /// <param name="watchTimeout">How long a watched order may stay unreported before
        /// <see cref="BrokerageOrderPollingService.OrderNotAcknowledged"/> is raised. Null falls back to one minute.</param>
        public AllOrdersPollingService(Func<IEnumerable<BrokerOrderState>> readAllOrders, BrokerageConcurrentMessageHandler messageHandler,
            IOrderProvider orderProvider, TimeSpan? pollInterval = null, TimeSpan? watchTimeout = null)
            : base(messageHandler, orderProvider, pollInterval, watchTimeout)
        {
            _readAllOrders = readAllOrders;
        }

        /// <summary>
        /// Calls the read once for the whole sweep.
        /// </summary>
        protected override IEnumerable<BrokerOrderState> Sweep()
        {
            return _readAllOrders();
        }
    }
}
