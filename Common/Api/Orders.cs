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
using System.Collections.Generic;
using Newtonsoft.Json;
using QuantConnect.Orders;
using QuantConnect.Optimizer.Parameters;
using QuantConnect.Statistics;

namespace QuantConnect.Api
{
    /// <summary>
    /// Orders response packet from the QuantConnect.com API.
    /// </summary>
    public class Orders : RestResponse
    {
        /// <summary>
        /// Order ID
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public int Id { get; set; }

        /// <summary>
        /// Order ID ot process before processing this order
        /// </summary>
        [JsonProperty(PropertyName = "contingentId")]
        public int ContingentId { get; set; }

        /// <summary>
        /// Brokerage Id for this order for when the brokerage splits orders into multiple pieces
        /// </summary>
        [JsonProperty(PropertyName = "brokerId")]
        public string[] BrokerId { get; set; }

        /// <summary>
        /// Represents a unique security identifier. This is made of two components, the unique SID
        /// and the Value. The value is the current ticker symbol while the SID is constant over the
        /// life of a security
        /// </summary>
        [JsonProperty(PropertyName = "symbol")]
        public Symbol Symbol { get; set; }

        /// <summary>
        /// Price of the Order
        /// </summary>
        [JsonProperty(PropertyName = "price")]
        public float Price { get; set; }

        /// <summary>
        /// Currency for the order price
        /// </summary>
        [JsonProperty(PropertyName = "priceCurrency")]
        public string PriceCurrency { get; set; }

        /// <summary>
        /// Gets the utc time the order was created
        /// </summary>
        [JsonProperty(PropertyName = "time")]
        public DateTime? Time { get; set; }

        /// <summary>
        /// Gets the utc time this order was created. Alias for Time
        /// </summary>
        [JsonProperty(PropertyName = "createdTime")]
        public DateTime? CreatedTime { get; set; }

        /// <summary>
        /// Gets the utc time the last fill was received, or null if no fills have been received
        /// </summary>
        [JsonProperty(PropertyName = "lastFillTime")]
        public DateTime? LastFillName { get; set; }

        /// <summary>
        /// Gets the utc time this order was last updated, or null if the order has not been updated
        /// </summary>
        [JsonProperty(PropertyName = "lastUpdateTime")]
        public DateTime? LastUpdateTime { get; set; }

        /// <summary>
        /// Gets the utc time this order was canceled, or null if the order was not canceled
        /// </summary>
        [JsonProperty(PropertyName = "canceledTime")]
        public DateTime? CanceledTime { get; set; }

        /// <summary>
        /// Number of shares to execute
        /// </summary>
        [JsonProperty(PropertyName = "quantity")]
        public float Quantity { get; set; }

        /// <summary>
        /// Order type. Options : ['Market', 'Limit', 'StopMarket', 'StopLimit', 'MarketOnOpen', 'MarketOnClose', 'OptionExercise']
        /// </summary>
        [JsonProperty(PropertyName = "type")]
        public OrderType Type { get; set; }

        /// <summary>
        /// Status of the Order. Options : ['New', 'Submitted', 'PartiallyFilled', 'Filled', 'Canceled', 'None', 'Invalid', 'CancelPending', 'UpdateSubmitted']
        /// </summary>
        [JsonProperty(PropertyName = "status")]
        public OrderStatus Status { get; set; }

        /// <summary>
        /// Tag the order with some custom data
        /// </summary>
        [JsonProperty(PropertyName = "tag")]
        public string Tag { get; set; }

        /// <summary>
        /// Type of tradable security / underlying asset. Options : ['Base', 'Equity', 'Option', 'Commodity', 'Forex', 'Future', 'Cfd', 'Crypto']
        /// </summary>
        [JsonProperty(PropertyName = "securityType")]
        public SecurityType SecurityType { get; set; }

        /// <summary>
        /// Direction of the order. Options : ['Buy', 'Sell', 'Hold']
        /// </summary>
        [JsonProperty(PropertyName = "direction")]
        public OrderDirection Direction { get; set; }

        /// <summary>
        /// Gets the executed value of this order. If the order has not yet filled, then this will return zero
        /// </summary>
        [JsonProperty(PropertyName = "value")]
        public double? Value { get; set; }

        /// <summary>
        /// Stores time and price information available at the time an order was submitted
        /// </summary>
        [JsonProperty(PropertyName = "orderSubmissionData")]
        public OrderSubmissionData OrderSubmissionData { get; set; }

        /// <summary>
        /// Returns true if the order is a marketable order
        /// </summary>
        [JsonProperty(PropertyName = "isMarkeable")]
        public bool IsMarkeable { get; set; }
    }

    /// <summary>
    /// Collection container for a list of orders for a project
    /// </summary>
    public class OrdersResponseWrapper : RestResponse
    {
        /// <summary>
        /// Collection of summarized Orders objects
        /// </summary>
        [JsonProperty(PropertyName = "orders")]
        public List<Orders> Orders { get; set; }
    }
}
