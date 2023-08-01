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

namespace QuantConnect.Util
{
    /// <summary>
    /// Json Converter for Series which handles special Pie Series serialization case
    /// </summary>
    public class SeriesJsonConverter : JsonConverter
    {
        /// <summary>
        /// Write Series to Json
        /// </summary>
        /// <param name="writer">The Json Writer to use</param>
        /// <param name="value">The value to written to Json</param>
        /// <param name="serializer">The Json Serializer to use</param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var series = value as Series;
            if (series == null)
            {
                return;
            }

            writer.WriteStartObject();

            List<ISeriesPoint> values;
            if (series.SeriesType == SeriesType.Pie)
            {
                values = new List<ISeriesPoint>();
                var dataPoint = series.ConsolidateChartPoints();
                if (dataPoint != null)
                {
                    values.Add(dataPoint);
                }
            }
            else
            {
                values = series.Values;
            }

            // have to add the converter we want to use, else will use default
            serializer.Converters.Add(new ColorJsonConverter());

            writer.WritePropertyName("Name");
            writer.WriteValue(series.Name);
            writer.WritePropertyName("Unit");
            writer.WriteValue(series.Unit);
            writer.WritePropertyName("Index");
            writer.WriteValue(series.Index);
            writer.WritePropertyName("Values");
            serializer.Serialize(writer, values);
            writer.WritePropertyName("SeriesType");
            writer.WriteValue(series.SeriesType);
            writer.WritePropertyName("Color");
            serializer.Serialize(writer, series.Color);
            writer.WritePropertyName("ScatterMarkerSymbol");
            serializer.Serialize(writer, series.ScatterMarkerSymbol);
            writer.WriteEndObject();
        }

        /// <summary>
        /// Not implemented
        /// </summary>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Determine if this Converter can convert this type
        /// </summary>
        /// <param name="objectType">Type that we would like to convert</param>
        /// <returns>True if <see cref="Series"/></returns>
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Series);
        }

        /// <summary>
        /// This converter wont be used to read JSON. Will throw exception if manually called.
        /// </summary>
        public override bool CanRead => false;
    }
}
