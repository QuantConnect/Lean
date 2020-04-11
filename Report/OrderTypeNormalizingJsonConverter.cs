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
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QuantConnect.Orders;

namespace QuantConnect.Report
{
    /// <summary>
    /// Normalizes the "Type" field to a value that will allow for
    /// successful deserialization in the <see cref="OrderJsonConverter"/> class.
    /// </summary>
    /// <example>
    /// All of these values should result in the same object:
    /// <code>
    /// [
    ///     { "Type": "marketOnOpen", ... },
    ///     { "Type": "MarketOnOpen", ... },
    ///     { "Type": 4, ... },
    /// ]
    /// </code>
    /// </example>
    /// <typeparam name="T">Result type to deserialize into</typeparam>
    public class OrderTypeNormalizingJsonConverter : JsonConverter
    {
        private readonly JsonConverter _converter;

        /// <summary>
        /// Creates an instance of the class
        /// </summary>
        public OrderTypeNormalizingJsonConverter()
        {
            _converter = new OrderJsonConverter();
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(Order).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var token = JToken.ReadFrom(reader);
            // Takes the Type field and selects the correct OrderType instance
            var orderTypeValue = token["Type"].Value<string>();
            int orderTypeNumber;
            var orderType = Parse.TryParse(orderTypeValue, NumberStyles.Any, out orderTypeNumber) ?
                (OrderType)orderTypeNumber :
                (OrderType)Enum.Parse(typeof(OrderType), orderTypeValue.ToPascalCase());

            var typeOfOrder = TypeFromOrderTypeEnum(orderType);
            token["Type"] = (int)orderType;
            return JsonConvert.DeserializeObject(token.ToString(), typeOfOrder, _converter);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns the object type belonging to the provided OrderType
        /// </summary>
        /// <param name="orderType">Order type</param>
        /// <returns>Class type that supports the given OrderType</returns>
        public static Type TypeFromOrderTypeEnum(OrderType orderType)
        {
            switch (orderType)
            {
                case OrderType.Market:
                    return typeof(MarketOrder);

                case OrderType.Limit:
                    return typeof(LimitOrder);

                case OrderType.StopMarket:
                    return typeof(StopMarketOrder);

                case OrderType.StopLimit:
                    return typeof(StopLimitOrder);

                case OrderType.MarketOnOpen:
                    return typeof(MarketOnOpenOrder);

                case OrderType.MarketOnClose:
                    return typeof(MarketOnCloseOrder);

                case OrderType.OptionExercise:
                    return typeof(OptionExerciseOrder);

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
