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
using QuantConnect.Interfaces;
using System.ComponentModel;
using QuantConnect.Api;

namespace QuantConnect.Orders
{
    /// <summary>
    /// Orders response packet from the QuantConnect.com API.
    /// </summary>
    public class OrderParameters : RestResponse
    {
        protected volatile int _incrementalId;
        private decimal _quantity;
        private decimal _price;

        /// <summary>
        /// Order ID.
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public int Id { get; internal set; }

        /// <summary>
        /// Order id to process before processing this order.
        /// </summary>
        [JsonProperty(PropertyName = "contingentId")]
        public int ContingentId { get; internal set; }

        /// <summary>
        /// Brokerage Id for this order for when the brokerage splits orders into multiple pieces
        /// </summary>
        [JsonProperty(PropertyName = "brokerId")]
        public List<string> BrokerId { get; set; }

        /// <summary>
        /// Symbol of the Asset
        /// </summary>
        [JsonProperty(PropertyName = "symbol")]
        public Symbol Symbol { get; internal set; }

        /// <summary>
        /// Price of the Order.
        /// </summary>
        [JsonProperty(PropertyName = "price")]
        public decimal Price
        {
            get { return _price; }
            internal set { _price = value.Normalize(); }
        }

        /// <summary>
        /// Currency for the order price
        /// </summary>
        [JsonProperty(PropertyName = "priceCurrency")]
        public string PriceCurrency { get; internal set; }

        /// <summary>
        /// Gets the utc time the order was created.
        /// </summary>
        [JsonProperty(PropertyName = "time", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime Time { get; internal set; }

        /// <summary>
        /// Gets the utc time this order was created. Alias for <see cref="Time"/>
        /// </summary>
        [JsonProperty(PropertyName = "createdTime", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime CreatedTime => Time;

        /// <summary>
        /// Gets the utc time the last fill was received, or null if no fills have been received
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? LastFillTime { get; internal set; }

        /// <summary>
        /// Gets the utc time this order was last updated, or null if the order has not been updated.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? LastUpdateTime { get; internal set; }

        /// <summary>
        /// Gets the utc time this order was canceled, or null if the order was not canceled.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? CanceledTime { get; internal set; }

        /// <summary>
        /// Number of shares to execute.
        /// </summary>
        [JsonProperty(PropertyName = "quantity")]
        public decimal Quantity
        {
            get { return _quantity; }
            internal set { _quantity = value.Normalize(); }
        }

        /// <summary>
        /// Order Type
        /// </summary>
        [JsonProperty(PropertyName = "type")]
        public virtual OrderType Type { get; }

        /// <summary>
        /// Status of the Order
        /// </summary>
        [JsonProperty(PropertyName = "status")]
        public OrderStatus Status { get; set; }

        /// <summary>
        /// Order Time In Force
        /// </summary>
        [JsonIgnore]
        public TimeInForce TimeInForce => Properties.TimeInForce;

        /// <summary>
        /// Tag the order with some custom data
        /// </summary>
        [DefaultValue(""), JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Tag { get; internal set; }

        /// <summary>
        /// Additional properties of the order
        /// </summary>
        public IOrderProperties Properties { get; protected set; }

        /// <summary>
        /// The symbol's security type
        /// </summary>
        [JsonProperty(PropertyName = "securityType")]
        public SecurityType SecurityType => Symbol.ID.SecurityType;

        /// <summary>
        /// Order Direction Property based off Quantity.
        /// </summary>
        [JsonProperty(PropertyName = "direction")]
        public OrderDirection Direction
        {
            get
            {
                if (Quantity > 0)
                {
                    return OrderDirection.Buy;
                }
                if (Quantity < 0)
                {
                    return OrderDirection.Sell;
                }
                return OrderDirection.Hold;
            }
        }

        /// <summary>
        /// Get the absolute quantity for this order
        /// </summary>
        [JsonIgnore]
        public decimal AbsoluteQuantity => Math.Abs(Quantity);

        /// <summary>
        /// Gets the executed value of this order. If the order has not yet filled,
        /// then this will return zero.
        /// </summary>
        [JsonProperty(PropertyName = "value")]
        public decimal Value => Quantity * Price;

        /// <summary>
        /// Gets the price data at the time the order was submitted
        /// </summary>
        [JsonProperty(PropertyName = "orderSubmissionData")]
        public OrderSubmissionData OrderSubmissionData { get; internal set; }

        /// <summary>
        /// Returns true if the order is a marketable order.
        /// </summary>
        [JsonProperty(PropertyName = "isMarkeable")]
        public bool IsMarketable
        {
            get
            {
                if (Type == OrderType.Limit)
                {
                    // check if marketable limit order using bid/ask prices
                    var limitOrder = (LimitOrder)this;
                    return OrderSubmissionData != null &&
                           (Direction == OrderDirection.Buy && limitOrder.LimitPrice >= OrderSubmissionData.AskPrice ||
                            Direction == OrderDirection.Sell && limitOrder.LimitPrice <= OrderSubmissionData.BidPrice);
                }

                return Type == OrderType.Market;
            }
        }
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
        public List<OrderParameters> Orders { get; set; }
    }
}
