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

namespace QuantConnect.Brokerages.Services
{
    /// <summary>
    /// Raised when a watched brokerage order id went unreported for the whole watch timeout of polling.
    /// This is a question, not a verdict: the service does not know whether the order never reached the
    /// broker or closed before the first sweep saw it. The brokerage decides what to do next.
    /// </summary>
    public class OrderNotAcknowledgedEventArgs : EventArgs
    {
        /// <summary>
        /// The brokerage order id nothing ever reported.
        /// </summary>
        public string BrokerageOrderId { get; }

        /// <summary>
        /// How long the id was watched, in polling time, before the timeout fired.
        /// </summary>
        public TimeSpan WatchedFor { get; }

        /// <summary>
        /// Creates a new <see cref="OrderNotAcknowledgedEventArgs"/>.
        /// </summary>
        /// <param name="brokerageOrderId">The brokerage order id nothing ever reported.</param>
        /// <param name="watchedFor">How long the id was watched, in polling time, before the timeout fired.</param>
        public OrderNotAcknowledgedEventArgs(string brokerageOrderId, TimeSpan watchedFor)
        {
            BrokerageOrderId = brokerageOrderId;
            WatchedFor = watchedFor;
        }
    }
}
