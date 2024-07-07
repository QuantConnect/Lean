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
using Newtonsoft.Json.Linq;

namespace QuantConnect.Api.Serialization
{
    /// <summary>
    /// Provides an implementation of <see cref="JsonConverter"/> that can deserialize <see cref="Product"/>
    /// </summary>
    public class ProductJsonConverter : JsonConverter
    {
        private Dictionary<string, ProductType> _productTypeMap = new Dictionary<
            string,
            ProductType
        >()
        {
            { "Professional Seats", ProductType.ProfessionalSeats },
            { "Backtest Node", ProductType.BacktestNode },
            { "Research Node", ProductType.ResearchNode },
            { "Live Trading Node", ProductType.LiveNode },
            { "Support", ProductType.Support },
            { "Data", ProductType.Data },
            { "Modules", ProductType.Modules }
        };

        /// <summary>
        /// Gets a value indicating whether this <see cref="JsonConverter"/> can write JSON.
        /// </summary>
        /// <value>
        /// <c>true</c> if this <see cref="JsonConverter"/> can write JSON; otherwise, <c>false</c>.
        /// </value>
        public override bool CanWrite => false;

        /// <summary>
        /// Determines whether this instance can convert the specified object type.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <returns>
        /// <c>true</c> if this instance can convert the specified object type; otherwise, <c>false</c>.
        /// </returns>
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Product);
        }

        /// <summary>
        /// Writes the JSON representation of the object.
        /// </summary>
        /// <param name="writer">The <see cref="JsonWriter"/> to write to.</param><param name="value">The value.</param><param name="serializer">The calling serializer.</param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException(
                "The OrderJsonConverter does not implement a WriteJson method;."
            );
        }

        /// <summary>
        /// Reads the JSON representation of the object.
        /// </summary>
        /// <param name="reader">The <see cref="JsonReader"/> to read from.</param><param name="objectType">Type of the object.</param><param name="existingValue">The existing value of object being read.</param><param name="serializer">The calling serializer.</param>
        /// <returns>
        /// The object value.
        /// </returns>
        public override object ReadJson(
            JsonReader reader,
            Type objectType,
            object existingValue,
            JsonSerializer serializer
        )
        {
            var jObject = JObject.Load(reader);

            var result = CreateProductFromJObject(jObject);

            return result;
        }

        /// <summary>
        /// Create an order from a simple JObject
        /// </summary>
        /// <param name="jObject"></param>
        /// <returns>Order Object</returns>
        public Product CreateProductFromJObject(JObject jObject)
        {
            if (jObject == null)
            {
                return null;
            }

            var productTypeName = jObject["name"].Value<string>();
            if (!_productTypeMap.ContainsKey(productTypeName))
            {
                return null;
            }

            return new Product
            {
                Type = _productTypeMap[productTypeName],
                Items = jObject["items"].ToObject<List<ProductItem>>()
            };
        }
    }
}
