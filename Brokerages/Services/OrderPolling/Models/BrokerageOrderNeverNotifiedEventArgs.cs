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
using QuantConnect.Orders;

namespace QuantConnect.Brokerages.Services.OrderPolling.Models
{
    /// <summary>
    /// Raised when no read saw a watched brokerage order id for the whole watch timeout. A question, not
    /// a verdict: the order may never have reached the broker, or closed before the first sweep. The
    /// brokerage decides what to do next.
    /// </summary>
    public class BrokerageOrderNeverNotifiedEventArgs : EventArgs
    {
        /// <summary>
        /// The brokerage order id no read ever saw.
        /// </summary>
        public string BrokerageOrderId { get; }

        /// <summary>
        /// The Lean order behind the id, or null when nothing resolves - a placement whose id was never assigned.
        /// </summary>
        public Order Order { get; }

        /// <summary>
        /// How long the id was watched, in polling time.
        /// </summary>
        public TimeSpan WatchDuration { get; }

        /// <summary>
        /// Creates a new <see cref="BrokerageOrderNeverNotifiedEventArgs"/>.
        /// </summary>
        /// <param name="brokerageOrderId">The brokerage order id no read ever saw.</param>
        /// <param name="order">The Lean order behind the id, or null when nothing resolves.</param>
        /// <param name="watchDuration">How long the id was watched, in polling time.</param>
        public BrokerageOrderNeverNotifiedEventArgs(string brokerageOrderId, Order order, TimeSpan watchDuration)
        {
            BrokerageOrderId = brokerageOrderId;
            Order = order;
            WatchDuration = watchDuration;
        }

        /// <summary>
        /// The order and the watch duration, ready for a log line or a warning.
        /// </summary>
        public override string ToString()
        {
            var order = Order?.ToString() ?? $"brokerage order id '{BrokerageOrderId}'";
            return $"{order}, watched for {WatchDuration.TotalSeconds:F0} seconds";
        }
    }
}
