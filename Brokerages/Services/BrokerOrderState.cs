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

namespace QuantConnect.Brokerages.Services
{
    /// <summary>
    /// One order, as the brokerage last saw it. The brokerage converts its own order model into this shape
    /// and passes it to <see cref="BrokerageOrderPollingService"/>, which compares it with the last state
    /// seen for the same order and reports only what is new. Every field except the id and the status is
    /// optional, and null means "my read does not know", never "zero".
    /// </summary>
    public class BrokerOrderState
    {
        /// <summary>
        /// The brokerage order id. Some brokers give every combo leg its own id, some give the whole
        /// combo one id; the state carries whatever the broker uses.
        /// </summary>
        public string BrokerageOrderId { get; set; }

        /// <summary>
        /// The Lean status the brokerage maps its broker's own status to.
        /// </summary>
        public OrderStatus Status { get; set; }

        /// <summary>
        /// The total absolute quantity filled so far, never the size of the last fill. Null when the
        /// read does not carry it - a <see cref="OrderStatus.Filled"/> status without this number can
        /// not produce the fill event that closes the order. For a combo that shares one brokerage id
        /// across its legs, this counts in strategy units, the same units the group quantity counts in.
        /// </summary>
        public decimal? FilledQuantity { get; set; }

        /// <summary>
        /// The price the broker reports for the fills. Null when the read does not carry it, and no
        /// fill event goes out without it - the service never invents a number.
        /// </summary>
        public decimal? FillPrice { get; set; }

        /// <summary>
        /// When the brokerage reported this state, in UTC.
        /// </summary>
        public DateTime TimeUtc { get; set; }

        /// <summary>
        /// The broker's own words for a closing status, e.g. the reject reason.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Creates an empty state the caller fills through the properties.
        /// </summary>
        public BrokerOrderState()
        {
        }

        /// <summary>
        /// Creates a state with no fill numbers: the id, the status, the time and the broker's words
        /// for a closing status.
        /// </summary>
        /// <param name="brokerageOrderId">The brokerage order id.</param>
        /// <param name="status">The Lean status the brokerage maps its broker's own status to.</param>
        /// <param name="timeUtc">When the brokerage reported this state, in UTC.</param>
        /// <param name="message">The broker's own words for a closing status.</param>
        public BrokerOrderState(string brokerageOrderId, OrderStatus status, DateTime timeUtc, string message)
            : this(brokerageOrderId, status, timeUtc, filledQuantity: null, fillPrice: null, message: message)
        {
        }

        /// <summary>
        /// Creates a state from what one read saw. Only the first three are always known; the fill
        /// numbers and the message stay null when the read does not carry them.
        /// </summary>
        /// <param name="brokerageOrderId">The brokerage order id.</param>
        /// <param name="status">The Lean status the brokerage maps its broker's own status to.</param>
        /// <param name="timeUtc">When the brokerage reported this state, in UTC.</param>
        /// <param name="filledQuantity">The total absolute quantity filled so far.</param>
        /// <param name="fillPrice">The price the broker reports for the fills.</param>
        /// <param name="message">The broker's own words for a closing status.</param>
        public BrokerOrderState(string brokerageOrderId, OrderStatus status, DateTime timeUtc,
            decimal? filledQuantity = null, decimal? fillPrice = null, string message = null)
        {
            BrokerageOrderId = brokerageOrderId;
            Status = status;
            TimeUtc = timeUtc;
            FilledQuantity = filledQuantity;
            FillPrice = fillPrice;
            Message = message;
        }
    }
}
