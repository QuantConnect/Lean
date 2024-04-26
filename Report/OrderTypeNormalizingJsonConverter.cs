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
    public class OrderTypeNormalizingJsonConverter : JsonConverter
    {
        /// <summary>
        /// Determine if this Converter can convert a given object type
        /// </summary>
        /// <param name="objectType">Object type to convert</param>
        /// <returns>True if assignable from <see cref="Order"/></returns>
        public override bool CanConvert(Type objectType)
        {
            return typeof(Order).IsAssignableFrom(objectType);
        }

        /// <summary>
        /// Read Json and convert
        /// </summary>
        /// <returns>Resulting <see cref="Order"/></returns>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var token = JToken.ReadFrom(reader);
            var jtokenType = token["Type"] ?? token["type"];
            int orderType = GetOrderType(jtokenType);
            if (token["Type"] != null)
            {
                token["Type"] = orderType;
            }
            else if (token["type"] != null)
            {
                token["type"] = orderType;
            }

            return OrderJsonConverter.CreateOrderFromJObject((JObject)token);
        }

        /// <summary>
        /// Write Json; Not implemented
        /// </summary>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        private int GetOrderType(JToken type)
        {
            var orderTypeValue = type.Value<string>();
            int orderTypeNumber;
            return Parse.TryParse(orderTypeValue, NumberStyles.Any, out orderTypeNumber) ?
                orderTypeNumber :
                (int)(OrderType)Enum.Parse(typeof(OrderType), orderTypeValue, true);
        }
    }
}
