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

namespace QuantConnect.Brokerages.Services.OrderPolling.Models
{
    /// <summary>
    /// What the <see cref="BaseBrokerageOrderPollingService"/> registry keeps per brokerage order id.
    /// </summary>
    internal class OrderTrackingEntry
    {
        /// <summary>
        /// The last snapshot seen for the order, from any path. Null when nothing was seen yet, so the
        /// submit is still due.
        /// </summary>
        public BrokerageOrderSnapshot LastSnapshot;

        /// <summary>
        /// The cumulative filled quantity already reported to Lean, by any path. Never shrinks.
        /// </summary>
        public decimal ReportedFilledQuantity;

        /// <summary>
        /// Set once the submit was reported for the order, by any path, so it goes out exactly once.
        /// </summary>
        public bool SubmitReported;

        /// <summary>
        /// Set once the order's end was reported, so the id leaves the read list and a later state
        /// for it reports nothing new.
        /// </summary>
        public bool TerminalReported;

        /// <summary>
        /// Set by <see cref="BaseBrokerageOrderPollingService.Watch(string)"/>: the notification timeout
        /// only applies to explicitly watched orders.
        /// </summary>
        public bool Watched;

        /// <summary>
        /// Set by <see cref="BaseBrokerageOrderPollingService.WatchReplacement"/>: the id is the new id
        /// of a replace, so the first state to carry it reports the update submit instead of a plain submit.
        /// </summary>
        public bool IsReplacement;

        /// <summary>
        /// Set once anything carried the id: a polled state, a stream write, or a seed. Stops the
        /// notification timeout.
        /// </summary>
        public bool Acknowledged;

        /// <summary>
        /// How long the order has been watched with nothing reporting it, in polling time.
        /// </summary>
        public TimeSpan UnacknowledgedDuration;
    }
}
