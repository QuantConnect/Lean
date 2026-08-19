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
        public bool SubmittedOrderEventInvoked;

        /// <summary>
        /// Set once the order's end was reported, so the id leaves the read list and a later state
        /// for it reports nothing new.
        /// </summary>
        public bool TerminalReported;

        /// <summary>
        /// Set by <see cref="BaseBrokerageOrderPollingService.Subscribe(string)"/>: the notification timeout
        /// only applies to explicitly subscribed orders.
        /// </summary>
        public bool Subscribed;

        /// <summary>
        /// Set by <see cref="BaseBrokerageOrderPollingService.SubscribeReplacement"/>: the id is the new id
        /// of a replace, so the first state to carry it reports the update submit instead of a plain submit.
        /// </summary>
        public bool IsReplacement;

        /// <summary>
        /// Set once anything carried the id: a polled state, a stream write, or a seed. Stops the
        /// notification timeout.
        /// </summary>
        public bool Acknowledged;

        /// <summary>
        /// How long the order has been subscribed with nothing reporting it, in polling time.
        /// </summary>
        public TimeSpan UnacknowledgedDuration;

        /// <summary>
        /// Creates an empty entry: nothing seen, nothing reported yet.
        /// </summary>
        public OrderTrackingEntry()
        {
        }

        /// <summary>
        /// Creates an entry seeded from a snapshot another path already reported: the fill quantity
        /// counts as reported, the id as acknowledged, and the snapshot's status decides whether the
        /// submit and the end already went out.
        /// </summary>
        /// <param name="lastSnapshot">The snapshot another path already reported for the order.</param>
        public OrderTrackingEntry(BrokerageOrderSnapshot lastSnapshot)
            : this(lastSnapshot,
                lastSnapshot.FilledQuantity ?? 0m,
                acknowledged: true,
                submittedOrderEventInvoked: lastSnapshot.Status != OrderStatus.New,
                terminalReported: lastSnapshot.Status == OrderStatus.Canceled || lastSnapshot.Status == OrderStatus.Invalid)
        {
        }

        /// <summary>
        /// Creates an entry seeded with what another path already reported for the order.
        /// </summary>
        /// <param name="lastSnapshot">The last snapshot seen for the order.</param>
        /// <param name="reportedFilledQuantity">The cumulative filled quantity already reported to Lean.</param>
        /// <param name="acknowledged">Whether anything already carried the order's id.</param>
        /// <param name="submittedOrderEventInvoked">Whether the submit was already reported.</param>
        /// <param name="terminalReported">Whether the order's end was already reported.</param>
        public OrderTrackingEntry(BrokerageOrderSnapshot lastSnapshot, decimal reportedFilledQuantity, bool acknowledged,
            bool submittedOrderEventInvoked, bool terminalReported)
        {
            LastSnapshot = lastSnapshot;
            ReportedFilledQuantity = reportedFilledQuantity;
            Acknowledged = acknowledged;
            SubmittedOrderEventInvoked = submittedOrderEventInvoked;
            TerminalReported = terminalReported;
        }
    }
}
