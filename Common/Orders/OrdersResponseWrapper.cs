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

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QuantConnect.Api;
using QuantConnect.Interfaces;
using QuantConnect.Orders.Serialization;
using QuantConnect.Securities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Orders
{
    /// <summary>
    /// Class for the order responses from the API
    /// </summary>
    public class OrderAPIResponse
    {
        /// <summary>
        /// Order ID.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Order id to process before processing this order.
        /// </summary>
        public int ContingentId { get; set; }

        /// <summary>
        /// Brokerage Id for this order for when the brokerage splits orders into multiple pieces
        /// </summary>
        public List<string> BrokerId { get; set; }

        /// <summary>
        /// Symbol of the Asset
        /// </summary>
        public Symbol Symbol { get; set; }

        /// <summary>
        /// Price of the Order.
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// Currency for the order price
        /// </summary>
        public string PriceCurrency { get; set; }

        /// <summary>
        /// Gets the utc time the order was created.
        /// </summary>
        public DateTime Time { get; set; }

        /// <summary>
        /// Gets the utc time this order was created. Alias for <see cref="Time"/>
        /// </summary>
        public DateTime CreatedTime => Time;

        /// <summary>
        /// Gets the utc time the last fill was received, or null if no fills have been received
        /// </summary>
        public DateTime? LastFillTime { get; set; }

        /// <summary>
        /// Gets the utc time this order was last updated, or null if the order has not been updated.
        /// </summary>
        public DateTime? LastUpdateTime { get; set; }

        /// <summary>
        /// Gets the utc time this order was canceled, or null if the order was not canceled.
        /// </summary>
        public DateTime? CanceledTime { get; set; }

        /// <summary>
        /// Number of shares to execute.
        /// </summary>
        public decimal Quantity { get; set; }

        /// <summary>
        /// Order Type
        /// </summary>
        public OrderType Type { get; set; }

        /// <summary>
        /// Status of the Order
        /// </summary>
        public OrderStatus Status { get; set; }

        /// <summary>
        /// Tag the order with some custom data
        /// </summary>
        public string Tag { get; set; }

        /// <summary>
        /// Additional properties of the order
        /// </summary>
        public IOrderProperties Properties { get; set; }

        /// <summary>
        /// The symbol's security type
        /// </summary>
        public SecurityType SecurityType { get; set; }

        /// <summary>
        /// Order Direction Property based off Quantity.
        /// </summary>
        public OrderDirection Direction { get; set; }

        /// <summary>
        /// Deprecated
        /// </summary>
        public decimal Value { get; set; }

        /// <summary>
        /// Gets the price data at the time the order was submitted
        /// </summary>
        public OrderSubmissionData OrderSubmissionData { get; set; }

        /// <summary>
        /// Returns true if the order is a marketable order.
        /// </summary>
        public bool IsMarketable { get; set; }

        /// <summary>
        /// The adjustment mode used on the order fill price
        /// </summary>
        public DataNormalizationMode PriceAdjustmentMode { get; set; }
        public List<OrderEvent> Events { get; set; }
    }

    /// <summary>
    /// JSON converter for order responses from the API
    /// </summary>
    public class OrderAPIResponseJsonConverter : OrderJsonConverter
    {
        /// <summary>
        /// Determines whether this instance can convert the specified object type.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <returns>
        /// <c>true</c> if this instance can convert the specified object type; otherwise, <c>false</c>.
        /// </returns>
        public override bool CanConvert(Type objectType)
        {
            return typeof(OrderAPIResponse).IsAssignableFrom(objectType);
        }

        /// <summary>
        /// Reads the JSON representation of the object.
        /// </summary>
        /// <param name="reader">The <see cref="T:Newtonsoft.Json.JsonReader"/> to read from.</param><param name="objectType">Type of the object.</param><param name="existingValue">The existing value of object being read.</param><param name="serializer">The calling serializer.</param>
        /// <returns>
        /// The object value.
        /// </returns>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jObject = JObject.Load(reader);

            var order = Order.ToOrderAPIResponse(CreateOrderFromJObject(jObject));

            order.Events = jObject["Events"].Select(x =>
            {
                return OrderEvent.FromSerialized(new SerializedOrderEvent()
                {
                    SymbolValue = x["symbol-value"].Value<string>(),
                    SymbolPermtick = x["symbol-permtick"].Value<string>(),
                    AlgorithmId = x["algorithm-id"].Value<string>(),
                    OrderId = x["order-id"].Value<int>(),
                    OrderEventId = x["order-event-id"].Value<int>(),
                    Symbol = new Symbol(SecurityIdentifier.Parse(x["symbol"].Value<string>()), x["symbol-value"].Value<string>()),
                    Time = x["time"].Value<double>(),
                    Status = CreateOrderStatus(x["status"]),
                    OrderFeeAmount = x["order-fee-amount"]?.Value<decimal>(),
                    OrderFeeCurrency = x["order-fee-currency"]?.Value<string>(),
                    FillPrice = x["fill-price"].Value<decimal>(),
                    FillPriceCurrency = x["fill-price-currency"].Value<string>(),
                    FillQuantity = x["fill-quantity"].Value<decimal>(),
                    Direction = CreateOrderDirection(x["direction"]),
                    Message = x["message"].Value<string>(),
                    IsAssignment = x["is-assignment"].Value<bool>(),
                    Quantity = x["quantity"].Value<decimal>(),
                    LimitPrice = x["limit-price"]?.Value<decimal>(),
                    StopPrice = x["stop-price"]?.Value<decimal>(),
                    IsInTheMoney = x["is-in-the-money"]?.Value<bool>() ?? false,
                });
            }).ToList();

            return order;
        }

        /// <summary>
        /// Creates a order status of the correct type
        /// </summary>
        private static OrderStatus CreateOrderStatus(JToken orderStatus)
        {
            var value = orderStatus.Value<string>();

            switch (value)
            {
                case "new":
                    return OrderStatus.New;
                case "submitted":
                    return OrderStatus.Submitted;
                case "partiallyFilled":
                    return OrderStatus.PartiallyFilled;
                case "filled":
                    return OrderStatus.Filled;
                case "canceled":
                    return OrderStatus.Canceled;
                case "none":
                    return OrderStatus.None;
                case "invalid":
                    return OrderStatus.Invalid;
                case "cancelPending":
                    return OrderStatus.CancelPending;
                case "updateSubmitted":
                    return OrderStatus.UpdateSubmitted;
                default:
                    throw new Exception($"Unknown order status: {value}");
            }
        }

        /// <summary>
        /// Creates an order direction of the correct type
        /// </summary>
        private static OrderDirection CreateOrderDirection(JToken orderStatus)
        {
            var value = orderStatus.Value<string>();

            switch (value)
            {
                case "buy":
                    return OrderDirection.Buy;
                case "sell":
                    return OrderDirection.Sell;
                case "hold":
                    return OrderDirection.Hold;
                default:
                    throw new Exception($"Unknown order direction: {value}");
            }
        }
    }

    /// <summary>
    /// Collection container for a list of orders for a project
    /// </summary>
    public class OrdersResponseWrapper : RestResponse
    {
        /// <summary>
        /// Total number of returned orders
        /// </summary>
        [JsonProperty(PropertyName = "length")]
        public int Length { get; set; }

        /// <summary>
        /// Collection of summarized Orders objects
        /// </summary>
        [JsonProperty(PropertyName = "orders")]
        public List<OrderAPIResponse> Orders { get; set; } = new();
    }
}
