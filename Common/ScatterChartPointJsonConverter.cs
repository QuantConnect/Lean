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

namespace QuantConnect
{
    /// <summary>
    /// <see cref="ScatterChartPoint"/> json converter
    /// </summary>
    public class ScatterChartPointJsonConverter : JsonConverter
    {
        /// <summary>
        /// Default writer
        /// </summary>
        public override bool CanWrite => false;

        /// <summary>
        /// Determine if this Converter can convert this type
        /// </summary>
        /// <param name="objectType">Type that we would like to convert</param>
        /// <returns>True if <see cref="Series"/></returns>
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(ScatterChartPoint);
        }

        /// <summary>
        /// Reads series from Json
        /// </summary>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.StartObject)
            {
                var jObject = JObject.Load(reader);
                var tooltip = jObject.TryGetPropertyValue<string>("tooltip");

                return new ScatterChartPoint(jObject["x"].Value<long>(), jObject["y"].Value<decimal?>(), tooltip);
            }

            var jArray = JArray.Load(reader);
            return new ScatterChartPoint(jArray[0].Value<long>(), jArray[1].Value<decimal?>());
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
