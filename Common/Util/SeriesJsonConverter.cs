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
using System.Drawing;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace QuantConnect.Util
{
    /// <summary>
    /// Json Converter for Series which handles special Pie Series serialization case
    /// </summary>
    public class SeriesJsonConverter : JsonConverter
    {
        private ColorJsonConverter _colorJsonConverter = new ();

        /// <summary>
        /// Write Series to Json
        /// </summary>
        /// <param name="writer">The Json Writer to use</param>
        /// <param name="value">The value to written to Json</param>
        /// <param name="serializer">The Json Serializer to use</param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var baseSeries = value as BaseSeries;
            if (baseSeries == null)
            {
                return;
            }

            writer.WriteStartObject();

            writer.WritePropertyName("name");
            writer.WriteValue(baseSeries.Name);
            writer.WritePropertyName("unit");
            writer.WriteValue(baseSeries.Unit);
            writer.WritePropertyName("index");
            writer.WriteValue(baseSeries.Index);
            writer.WritePropertyName("seriesType");
            writer.WriteValue(baseSeries.SeriesType);

            if (baseSeries.ZIndex.HasValue)
            {
                writer.WritePropertyName("zIndex");
                writer.WriteValue(baseSeries.ZIndex.Value);
            }

            if (baseSeries.IndexName != null)
            {
                writer.WritePropertyName("indexName");
                writer.WriteValue(baseSeries.IndexName);
            }

            if (baseSeries.Tooltip != null)
            {
                writer.WritePropertyName("tooltip");
                writer.WriteValue(baseSeries.Tooltip);
            }

            switch (value)
            {
                case Series series:
                    var values = series.Values;
                    if (series.SeriesType == SeriesType.Pie)
                    {
                        values = new List<ISeriesPoint>();
                        var dataPoint = series.ConsolidateChartPoints();
                        if (dataPoint != null)
                        {
                            values.Add(dataPoint);
                        }
                    }

                    // have to add the converter we want to use, else will use default
                    serializer.Converters.Add(_colorJsonConverter);

                    writer.WritePropertyName("values");
                    serializer.Serialize(writer, values);
                    writer.WritePropertyName("color");
                    serializer.Serialize(writer, series.Color);
                    writer.WritePropertyName("scatterMarkerSymbol");
                    serializer.Serialize(writer, series.ScatterMarkerSymbol);
                    break;

                default:
                    writer.WritePropertyName("values");
                    serializer.Serialize(writer, (value as BaseSeries).Values);
                    break;
            }

            writer.WriteEndObject();
        }

        /// <summary>
        /// Reads series from Json
        /// </summary>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jObject = JObject.Load(reader);

            var name = (jObject["Name"] ?? jObject["name"]).Value<string>();
            var unit = (jObject["Unit"] ?? jObject["unit"]).Value<string>();
            var index = (jObject["Index"] ?? jObject["index"]).Value<int>();
            var seriesType = (SeriesType)(jObject["SeriesType"] ?? jObject["seriesType"]).Value<int>();
            var values = (JArray)(jObject["Values"] ?? jObject["values"]);

            var zindex = jObject.TryGetPropertyValue<int?>("ZIndex") ?? jObject.TryGetPropertyValue<int?>("zIndex");
            var indexName = jObject.TryGetPropertyValue<string>("IndexName") ?? jObject.TryGetPropertyValue<string>("indexName");
            var tooltip = jObject.TryGetPropertyValue<string>("Tooltip") ?? jObject.TryGetPropertyValue<string>("tooltip");

            if (seriesType == SeriesType.Candle)
            {
                return new CandlestickSeries()
                {
                    Name = name,
                    Unit = unit,
                    Index = index,
                    ZIndex = zindex,
                    Tooltip = tooltip,
                    IndexName = indexName,
                    SeriesType = seriesType,
                    Values = values.ToObject<List<Candlestick>>(serializer).Where(x => x != null).Cast<ISeriesPoint>().ToList()
                };
            }

            var result = new Series()
            {
                Name = name,
                Unit = unit,
                Index = index,
                ZIndex = zindex,
                Tooltip = tooltip,
                IndexName = indexName,
                SeriesType = seriesType,
                Color = (jObject["Color"] ?? jObject["color"])?.ToObject<Color>(serializer) ?? Color.Empty,
                ScatterMarkerSymbol = (jObject["ScatterMarkerSymbol"] ?? jObject["scatterMarkerSymbol"])?.ToObject<ScatterMarkerSymbol>(serializer) ?? ScatterMarkerSymbol.None
            };

            if (seriesType == SeriesType.Scatter)
            {
                result.Values = values.ToObject<List<ScatterChartPoint>>(serializer).Where(x => x != null).Cast<ISeriesPoint>().ToList();
            }
            else
            {
                result.Values = values.ToObject<List<ChartPoint>>(serializer).Where(x => x != null).Cast<ISeriesPoint>().ToList();
            }
            return result;
        }

        /// <summary>
        /// Determine if this Converter can convert this type
        /// </summary>
        /// <param name="objectType">Type that we would like to convert</param>
        /// <returns>True if <see cref="Series"/></returns>
        public override bool CanConvert(Type objectType)
        {
            return typeof(BaseSeries).IsAssignableFrom(objectType);
        }
    }
}
