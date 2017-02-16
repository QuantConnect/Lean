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

namespace QuantConnect.Brokerages.InteractiveBrokers.Client
{
    /// <summary>
    /// Order Status constants.
    /// </summary>
    public static class OrderStatus
    {
        /// <summary>
        /// indicates that you have transmitted the order, but have not yet received
        /// confirmation that it has been accepted by the order destination.
        /// This order status is not sent by TWS and should be explicitly set by the API developer when an order is submitted.
        /// </summary>
        public const string PendingSubmit = "PendingSubmit";

        /// <summary>
        /// PendingCancel - indicates that you have sent a request to cancel the order
        /// but have not yet received cancel confirmation from the order destination.
        /// At this point, your order is not confirmed canceled. You may still receive
        /// an execution while your cancellation request is pending.
        /// This order status is not sent by TWS and should be explicitly set by the API developer when an order is canceled.
        /// </summary>
        public const string PendingCancel = "PendingCancel";

        /// <summary>
        /// indicates that a simulated order type has been accepted by the IB system and
        /// that this order has yet to be elected. The order is held in the IB system
        /// (and the status remains DARK BLUE) until the election criteria are met.
        /// At that time the order is transmitted to the order destination as specified
        /// (and the order status color will change).
        /// </summary>
        public const string PreSubmitted = "PreSubmitted";

        /// <summary>
        /// indicates that your order has been accepted at the order destination and is working.
        /// </summary>
        public const string Submitted = "Submitted";

        /// <summary>
        /// indicates that the balance of your order has been confirmed canceled by the IB system.
        /// This could occur unexpectedly when IB or the destination has rejected your order.
        /// </summary>
        public const string Cancelled = "Cancelled";

        /// <summary>
        /// The order has been completely filled.
        /// </summary>
        public const string Filled = "Filled";

        /// <summary>
        /// The Order is inactive
        /// </summary>
        public const string Inactive = "Inactive";

        /// <summary>
        /// The order is Partially Filled
        /// </summary>
        public const string PartiallyFilled = "PartiallyFilled";

        /// <summary>
        /// Api Pending
        /// </summary>
        public const string ApiPending = "ApiPending";

        /// <summary>
        /// Api Cancelled
        /// </summary>
        public const string ApiCancelled = "ApiCancelled";

        /// <summary>
        /// Indicates that there is an error with this order
        /// This order status is not sent by TWS and should be explicitly set by the API developer when an error has occured.
        /// </summary>
        public const string Error = "Error";

        /// <summary>
        /// No Order Status
        /// </summary>
        public const string None = "";
    }
}
