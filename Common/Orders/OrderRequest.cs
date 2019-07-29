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
using static QuantConnect.StringExtensions;

namespace QuantConnect.Orders
{
    /// <summary>
    /// Represents a request to submit, update, or cancel an order
    /// </summary>
    public abstract class OrderRequest
    {
        /// <summary>
        /// Gets the type of this order request
        /// </summary>
        public abstract OrderRequestType OrderRequestType
        {
            get;
        }

        /// <summary>
        /// Gets the status of this request
        /// </summary>
        public OrderRequestStatus Status
        {
            get; private set;
        }

        /// <summary>
        /// Gets the UTC time the request was created
        /// </summary>
        public DateTime Time
        {
            get; private set;
        }

        /// <summary>
        /// Gets the order id the request acts on
        /// </summary>
        public int OrderId
        {
            get; protected set;
        }

        /// <summary>
        /// Gets a tag for this request
        /// </summary>
        public string Tag
        {
            get; private set;
        }

        /// <summary>
        /// Gets the response for this request. If this request was never processed then this
        /// will equal <see cref="OrderResponse.Unprocessed"/>. This value is never equal to null.
        /// </summary>
        public OrderResponse Response
        {
            get; private set;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderRequest"/> class
        /// </summary>
        /// <param name="time">The time this request was created</param>
        /// <param name="orderId">The order id this request acts on, specify zero for <see cref="SubmitOrderRequest"/></param>
        /// <param name="tag">A custom tag for the request</param>
        protected OrderRequest(DateTime time, int orderId, string tag)
        {
            Time = time;
            OrderId = orderId;
            Tag = tag;
            Response = OrderResponse.Unprocessed;
            Status = OrderRequestStatus.Unprocessed;
        }

        /// <summary>
        /// Sets the <see cref="Response"/> for this request
        /// </summary>
        /// <param name="response">The response to this request</param>
        /// <param name="status">The current status of this request</param>
        public void SetResponse(OrderResponse response, OrderRequestStatus status = OrderRequestStatus.Error)
        {
            if (response == null)
            {
                throw new ArgumentNullException(nameof(response), "Response can not be null");
            }

            // if the response is an error, ignore the input status
            Status = response.IsError ? OrderRequestStatus.Error : status;
            Response = response;
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public override string ToString()
        {
            return Invariant($"{Time} UTC: Order: ({OrderId.ToStringInvariant()}) - {Tag} Status: {Status}");
        }
    }
}