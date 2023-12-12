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
    /// Candlestick Json Converter
    /// </summary>
    public class CandlestickJsonConverter : JsonConverter
    {
        /// <summary>
        /// Write Series to Json
        /// </summary>
        /// <param name="writer">The Json Writer to use</param>
        /// <param name="value">The value to written to Json</param>
        /// <param name="serializer">The Json Serializer to use</param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            // Candlesticks will be written as a single array of 5 values: [time, open, high, low, close]

            var candlestick = value as Candlestick;
            if (candlestick == null)
            {
                return;
            }

            writer.WriteStartArray();

            writer.WriteValue(candlestick.LongTime);
            writer.WriteValue(candlestick.Open);
            writer.WriteValue(candlestick.High);
            writer.WriteValue(candlestick.Low);
            writer.WriteValue(candlestick.Close);

            writer.WriteEndArray();
        }

        /// <summary>
        /// Json reader implementation which handles backwards compatiblity for old equity chart points
        /// </summary>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if(reader.TokenType == JsonToken.StartObject)
            {
                var chartPoint = serializer.Deserialize<ChartPoint>(reader);
                if(chartPoint == null)
                {
                    return null;
                }
                return new Candlestick(chartPoint.X, chartPoint.Y, chartPoint.Y, chartPoint.Y, chartPoint.Y);
            }
            var jArray = JArray.Load(reader);
            if(jArray.Count <= 2)
            {
                var chartPoint = jArray.ToObject<ChartPoint>();
                if (chartPoint == null)
                {
                    return null;
                }
                return new Candlestick(chartPoint.X, chartPoint.Y, chartPoint.Y, chartPoint.Y, chartPoint.Y);
            }
            return new Candlestick(jArray[0].Value<long>(), jArray[1].Value<decimal?>(), jArray[2].Value<decimal?>(),
                jArray[3].Value<decimal?>(), jArray[4].Value<decimal?>());
        }

        /// <summary>
        /// Determine if this Converter can convert this type
        /// </summary>
        /// <param name="objectType">Type that we would like to convert</param>
        /// <returns>True if <see cref="Series"/></returns>
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Candlestick);
        }

        /// <summary>
        /// This converter wont be used to read JSON. Will throw exception if manually called.
        /// </summary>
        public override bool CanRead => true;
    }
}
