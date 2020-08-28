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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QuantConnect.Brokerages.Bitfinex.Messages;

namespace QuantConnect.Brokerages.Bitfinex.Converters
{
    /// <summary>
    /// A custom JSON converter for the Bitfinex <see cref="Order"/> class
    /// </summary>
    public class OrderConverter : JsonConverter
    {
        /// <summary>
        /// Gets a value indicating whether this <see cref="T:Newtonsoft.Json.JsonConverter" /> can write JSON.
        /// </summary>
        /// <value><c>true</c> if this <see cref="T:Newtonsoft.Json.JsonConverter" /> can write JSON; otherwise, <c>false</c>.</value>
        public override bool CanWrite => false;

        /// <summary>Writes the JSON representation of the object.</summary>
        /// <param name="writer">The <see cref="T:Newtonsoft.Json.JsonWriter" /> to write to.</param>
        /// <param name="value">The value.</param>
        /// <param name="serializer">The calling serializer.</param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        /// <summary>Reads the JSON representation of the object.</summary>
        /// <param name="reader">The <see cref="T:Newtonsoft.Json.JsonReader" /> to read from.</param>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="existingValue">The existing value of object being read.</param>
        /// <param name="serializer">The calling serializer.</param>
        /// <returns>The object value.</returns>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var array = JArray.Load(reader);

            return new Order
            {
                Id = array[0].Type == JTokenType.Null ? 0 : (long)array[0],
                ClientOrderId = array[2].Type == JTokenType.Null ? 0 : (long)array[2],
                Symbol = array[3].Type == JTokenType.Null ? string.Empty : (string)array[3],
                MtsCreate = array[4].Type == JTokenType.Null ? 0 : (long)array[4],
                MtsUpdate = array[5].Type == JTokenType.Null ? 0 : (long)array[5],
                Amount = array[6].Type == JTokenType.Null ? 0 : Convert.ToDecimal((double)array[6]),
                AmountOrig = array[7].Type == JTokenType.Null ? 0 : Convert.ToDecimal((double)array[7]),
                Type = array[8].Type == JTokenType.Null ? string.Empty : (string)array[8],
                Status = array[13].Type == JTokenType.Null ? string.Empty : (string)array[13],
                Price = array[16].Type == JTokenType.Null ? 0 : Convert.ToDecimal((double)array[16]),
                PriceAvg = array[17].Type == JTokenType.Null ? 0 : Convert.ToDecimal((double)array[17]),
                PriceTrailing = array[18].Type == JTokenType.Null ? 0 : Convert.ToDecimal((double)array[18]),
                PriceAuxLimit = array[19].Type == JTokenType.Null ? 0 : Convert.ToDecimal((double)array[19])
            };
        }

        /// <summary>
        /// Determines whether this instance can convert the specified object type.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <returns>
        /// 	<c>true</c> if this instance can convert the specified object type; otherwise, <c>false</c>.
        /// </returns>
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Order);
        }
    }
}
