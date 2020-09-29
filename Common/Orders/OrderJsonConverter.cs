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
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QuantConnect.Brokerages;
using QuantConnect.Securities;

namespace QuantConnect.Orders
{
    /// <summary>
    /// Provides an implementation of <see cref="JsonConverter"/> that can deserialize Orders
    /// </summary>
    public class OrderJsonConverter : JsonConverter
    {
        /// <summary>
        /// Gets a value indicating whether this <see cref="T:Newtonsoft.Json.JsonConverter"/> can write JSON.
        /// </summary>
        /// <value>
        /// <c>true</c> if this <see cref="T:Newtonsoft.Json.JsonConverter"/> can write JSON; otherwise, <c>false</c>.
        /// </value>
        public override bool CanWrite
        {
            get { return false; }
        }

        /// <summary>
        /// Determines whether this instance can convert the specified object type.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <returns>
        /// <c>true</c> if this instance can convert the specified object type; otherwise, <c>false</c>.
        /// </returns>
        public override bool CanConvert(Type objectType)
        {
            return typeof(Order).IsAssignableFrom(objectType);
        }

        /// <summary>
        /// Writes the JSON representation of the object.
        /// </summary>
        /// <param name="writer">The <see cref="T:Newtonsoft.Json.JsonWriter"/> to write to.</param><param name="value">The value.</param><param name="serializer">The calling serializer.</param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException("The OrderJsonConverter does not implement a WriteJson method;.");
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

            var order = CreateOrderFromJObject(jObject);

            return order;
        }

        /// <summary>
        /// Create an order from a simple JObject
        /// </summary>
        /// <param name="jObject"></param>
        /// <returns>Order Object</returns>
        public static Order CreateOrderFromJObject(JObject jObject)
        {
            // create order instance based on order type field
            var orderType = (OrderType) jObject["Type"].Value<int>();
            var order = CreateOrder(orderType, jObject);

            // populate common order properties
            order.Id = jObject["Id"].Value<int>();

            var jsonStatus = jObject["Status"];
            var jsonTime = jObject["Time"];
            if (jsonStatus.Type == JTokenType.Integer)
            {
                order.Status = (OrderStatus) jsonStatus.Value<int>();
            }
            else if (jsonStatus.Type == JTokenType.Null)
            {
                order.Status = OrderStatus.Canceled;
            }
            else
            {
                // The `Status` tag can sometimes appear as a string of the enum value in the LiveResultPacket.
                order.Status = (OrderStatus) Enum.Parse(typeof(OrderStatus), jsonStatus.Value<string>(), true);
            }
            if (jsonTime != null && jsonTime.Type != JTokenType.Null)
            {
                order.Time = jsonTime.Value<DateTime>();
            }
            else
            {
                // `Time` can potentially be null in some LiveResultPacket instances, but
                // `CreatedTime` will always be there if `Time` is absent.
                order.Time = jObject["CreatedTime"].Value<DateTime>();
            }

            var orderSubmissionData = jObject["OrderSubmissionData"];
            if (orderSubmissionData != null && orderSubmissionData.Type != JTokenType.Null)
            {
                var bidPrice = orderSubmissionData["BidPrice"].Value<decimal>();
                var askPrice = orderSubmissionData["AskPrice"].Value<decimal>();
                var lastPrice = orderSubmissionData["LastPrice"].Value<decimal>();
                order.OrderSubmissionData = new OrderSubmissionData(bidPrice, askPrice, lastPrice);
            }

            var lastFillTime = jObject["LastFillTime"];
            var lastUpdateTime = jObject["LastUpdateTime"];
            var canceledTime = jObject["CanceledTime"];

            if (canceledTime != null && canceledTime.Type != JTokenType.Null)
            {
                order.CanceledTime = canceledTime.Value<DateTime>();
            }
            if (lastFillTime != null && lastFillTime.Type != JTokenType.Null)
            {
                order.LastFillTime = lastFillTime.Value<DateTime>();
            }
            if (lastUpdateTime != null && lastUpdateTime.Type != JTokenType.Null)
            {
                order.LastUpdateTime = lastUpdateTime.Value<DateTime>();
            }
            var tag = jObject["Tag"];
            if (tag != null && tag.Type != JTokenType.Null)
            {
                order.Tag = tag.Value<string>();
            }
            else
            {
                order.Tag = "";
            }

            order.Quantity = jObject["Quantity"].Value<decimal>();
            var orderPrice = jObject["Price"];
            if (orderPrice != null && orderPrice.Type != JTokenType.Null)
            {
                order.Price = orderPrice.Value<decimal>();
            }
            else
            {
                order.Price = default(decimal);
            }

            var priceCurrency = jObject["PriceCurrency"];
            if (priceCurrency != null && priceCurrency.Type != JTokenType.Null)
            {
                order.PriceCurrency = priceCurrency.Value<string>();
            }
            var securityType = (SecurityType) jObject["SecurityType"].Value<int>();
            order.BrokerId = jObject["BrokerId"].Select(x => x.Value<string>()).ToList();
            order.ContingentId = jObject["ContingentId"].Value<int>();

            var timeInForce = jObject["Properties"]?["TimeInForce"] ?? jObject["TimeInForce"] ?? jObject["Duration"];
            order.Properties.TimeInForce = timeInForce != null
                ? CreateTimeInForce(timeInForce, jObject)
                : TimeInForce.GoodTilCanceled;

            string market = null;

            //does data have market?
            var suppliedMarket = jObject.SelectTokens("Symbol.ID.Market");
            if (suppliedMarket.Any())
            {
                market = suppliedMarket.Single().Value<string>();
            }

            if (jObject.SelectTokens("Symbol.ID").Any())
            {
                var sid = SecurityIdentifier.Parse(jObject.SelectTokens("Symbol.ID").Single().Value<string>());
                var ticker = jObject.SelectTokens("Symbol.Value").Single().Value<string>();
                order.Symbol = new Symbol(sid, ticker);
            }
            else if (jObject.SelectTokens("Symbol.Value").Any())
            {
                // provide for backwards compatibility
                var ticker = jObject.SelectTokens("Symbol.Value").Single().Value<string>();

                if (market == null && !SymbolPropertiesDatabase.FromDataFolder().TryGetMarket(ticker, securityType, out market))
                {
                    market = DefaultBrokerageModel.DefaultMarketMap[securityType];
                }
                order.Symbol = Symbol.Create(ticker, securityType, market);
            }
            else
            {
                var tickerstring = jObject["Symbol"].Value<string>();

                if (market == null && !SymbolPropertiesDatabase.FromDataFolder().TryGetMarket(tickerstring, securityType, out market))
                {
                    market = DefaultBrokerageModel.DefaultMarketMap[securityType];
                }
                order.Symbol = Symbol.Create(tickerstring, securityType, market);
            }

            return order;
        }

        /// <summary>
        /// Creates an order of the correct type
        /// </summary>
        private static Order CreateOrder(OrderType orderType, JObject jObject)
        {
            Order order;
            switch (orderType)
            {
                case OrderType.Market:
                    order = new MarketOrder();
                    break;

                case OrderType.Limit:
                    order = new LimitOrder {LimitPrice = jObject["LimitPrice"] == null ? default(decimal) : jObject["LimitPrice"].Value<decimal>() };
                    break;

                case OrderType.StopMarket:
                    order = new StopMarketOrder
                    {
                        StopPrice = jObject["StopPrice"] == null ? default(decimal) : jObject["StopPrice"].Value<decimal>()
                    };
                    break;

                case OrderType.StopLimit:
                    order = new StopLimitOrder
                    {
                        LimitPrice = jObject["LimitPrice"] == null ? default(decimal) : jObject["LimitPrice"].Value<decimal>(),
                        StopPrice = jObject["StopPrice"] == null ? default(decimal) : jObject["StopPrice"].Value<decimal>()
                    };
                    break;

                case OrderType.MarketOnOpen:
                    order = new MarketOnOpenOrder();
                    break;

                case OrderType.MarketOnClose:
                    order = new MarketOnCloseOrder();
                    break;

                case OrderType.OptionExercise:
                    order = new OptionExerciseOrder();
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
            return order;
        }

        /// <summary>
        /// Creates a Time In Force of the correct type
        /// </summary>
        private static TimeInForce CreateTimeInForce(JToken timeInForce, JObject jObject)
        {
            // for backward-compatibility support deserialization of old JSON format
            if (timeInForce is JValue)
            {
                var value = timeInForce.Value<int>();

                switch (value)
                {
                    case 0:
                        return TimeInForce.GoodTilCanceled;

                    case 1:
                        return TimeInForce.Day;

                    case 2:
                        var expiry = jObject["DurationValue"].Value<DateTime>();
                        return TimeInForce.GoodTilDate(expiry);

                    default:
                        throw new Exception($"Unknown time in force value: {value}");
                }
            }

            // convert with TimeInForceJsonConverter
            return timeInForce.ToObject<TimeInForce>();
        }
    }
}
