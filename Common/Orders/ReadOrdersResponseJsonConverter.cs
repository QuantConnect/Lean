/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2024 QuantConnect Corporation.
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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using QuantConnect.Orders.Serialization;

namespace QuantConnect.Orders
{
    /// <summary>
    /// Api orders read response json converter
    /// </summary>
    public class ReadOrdersResponseJsonConverter : JsonConverter
    {
        /// <summary>
        /// Determines if can convert the given open type
        /// </summary>
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(ApiOrderResponse);
        }

        /// <summary>
        /// Serialize the given api order response
        /// </summary>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var orderResponse = (ApiOrderResponse)value;
            var jObject = JObject.FromObject(orderResponse.Order);
            jObject["symbol"] = JToken.FromObject(orderResponse.Symbol);
            jObject["events"] = JToken.FromObject(orderResponse.Events);
            jObject.WriteTo(writer);
        }

        /// <summary>
        /// Deserialize the given api order response
        /// </summary>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jObject = JObject.Load(reader);

            serializer.Converters.Add(new OrderJsonConverter());
            var order = jObject.ToObject<Order>(serializer);

            var events = jObject["Events"] ?? jObject["events"];
            List<SerializedOrderEvent> deserializedEvents = null;
            if (events != null)
            {
                deserializedEvents = events.ToObject<List<SerializedOrderEvent>>();
            }

            var symbol = jObject["Symbol"] ?? jObject["symbol"];
            Symbol deserializedSymbol = null;
            if (symbol != null)
            {
                deserializedSymbol = symbol.ToObject<Symbol>();
            }

            return new ApiOrderResponse(order, deserializedEvents ?? new(), deserializedSymbol);
        }
    }
}
