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
    /// One order, as the brokerage last saw it. The plugin converts its broker model into this shape;
    /// <see cref="BrokerageOrderPollingService"/> diffs it against the last snapshot seen and reports
    /// only what is new. Null in an optional field means "my read does not know", never "zero".
    /// </summary>
    public class BrokerageOrderSnapshot
    {
        /// <summary>
        /// The brokerage order id - per combo leg or one for the whole combo, whatever the broker uses.
        /// </summary>
        public string BrokerageOrderId { get; set; }

        /// <summary>
        /// The Lean status the plugin maps its broker's own status to.
        /// </summary>
        public OrderStatus Status { get; set; }

        /// <summary>
        /// The total absolute quantity filled so far, never the last fill's size. Null when the read
        /// does not carry it - no fill event goes out then. A shared-id combo counts in strategy units.
        /// </summary>
        public decimal? FilledQuantity { get; set; }

        /// <summary>
        /// The price the broker reports for the fills. No fill event goes out without it - the service
        /// never invents a number.
        /// </summary>
        public decimal? FillPrice { get; set; }

        /// <summary>
        /// When the brokerage reported this snapshot, in UTC.
        /// </summary>
        public DateTime TimeUtc { get; set; }

        /// <summary>
        /// The broker's own words for a closing status, e.g. the reject reason.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Creates a snapshot with no fill numbers.
        /// </summary>
        /// <param name="brokerageOrderId">The brokerage order id.</param>
        /// <param name="status">The Lean status the plugin maps its broker's own status to.</param>
        /// <param name="timeUtc">When the brokerage reported this snapshot, in UTC. Null takes <see cref="DateTime.UtcNow"/>.</param>
        /// <param name="message">The broker's own words for a closing status.</param>
        public BrokerageOrderSnapshot(string brokerageOrderId, OrderStatus status, DateTime? timeUtc, string message)
            : this(brokerageOrderId, status, timeUtc, filledQuantity: null, fillPrice: null, message: message)
        {
        }

        /// <summary>
        /// Creates a snapshot from what one read saw; only the id and the status are always known.
        /// </summary>
        /// <param name="brokerageOrderId">The brokerage order id.</param>
        /// <param name="status">The Lean status the plugin maps its broker's own status to.</param>
        /// <param name="timeUtc">When the brokerage reported this snapshot, in UTC. Null takes <see cref="DateTime.UtcNow"/>.</param>
        /// <param name="filledQuantity">The total absolute quantity filled so far.</param>
        /// <param name="fillPrice">The price the broker reports for the fills.</param>
        /// <param name="message">The broker's own words for a closing status.</param>
        public BrokerageOrderSnapshot(string brokerageOrderId, OrderStatus status, DateTime? timeUtc = null,
            decimal? filledQuantity = null, decimal? fillPrice = null, string message = null)
        {
            BrokerageOrderId = brokerageOrderId;
            Status = status;
            TimeUtc = timeUtc ?? DateTime.UtcNow;
            FilledQuantity = filledQuantity;
            FillPrice = fillPrice;
            Message = message;
        }
    }
}
