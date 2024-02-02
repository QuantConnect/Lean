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

namespace QuantConnect.Util
{
    /// <summary>
    /// Json Converter for ChartPoint which handles special reading
    /// </summary>
    public class ChartPointJsonConverter : JsonConverter
    {
        /// <summary>
        /// Determine if this Converter can convert this type
        /// </summary>
        /// <param name="objectType">Type that we would like to convert</param>
        /// <returns>True if <see cref="Series"/></returns>
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(ChartPoint);
        }

        /// <summary>
        /// Reads series from Json
        /// </summary>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.StartObject)
            {
                var jObject = JObject.Load(reader);
                var x = jObject["x"];

                if (!jObject.ContainsKey("y"))
                {
                    return new ChartPoint(x.Value<long>(), 0);
                }

                var y = jObject["y"];
                if (y != null && (y.Type == JTokenType.Float || y.Type == JTokenType.Integer))
                {
                    return new ChartPoint(x.Value<long>(), y.Value<decimal>());
                }

                if (y.Type == JTokenType.Null)
                {
                    return new ChartPoint(x.Value<long>(), null);
                }

                return null;
            }

            var jArray = JArray.Load(reader);
            return new ChartPoint(jArray[0].Value<long>(), jArray[1].Value<decimal?>());
        }

        /// <summary>
        /// Write point to Json
        /// </summary>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var chartPoint = (ChartPoint)value;
            writer.WriteStartArray();
            writer.WriteValue(chartPoint.X);
            writer.WriteValue(chartPoint.Y);
            writer.WriteEndArray();
        }
    }
}
