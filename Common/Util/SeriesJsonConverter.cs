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
using System.Drawing;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
            var baseSeries = value as BaseSeries;
            if (baseSeries == null)
            {
                return;
            }

            writer.WriteStartObject();

            writer.WritePropertyName("Name");
            writer.WriteValue(baseSeries.Name);
            writer.WritePropertyName("Unit");
            writer.WriteValue(baseSeries.Unit);
            writer.WritePropertyName("Index");
            writer.WriteValue(baseSeries.Index);
            writer.WritePropertyName("SeriesType");
            writer.WriteValue(baseSeries.SeriesType);

            switch (value)
            {
                case Series series:
                    List<ChartPoint> values;
                    if (series.SeriesType == SeriesType.Pie)
                    {
                        values = new List<ChartPoint>();
                        var dataPoint = series.ConsolidateChartPoints();
                        if (dataPoint != null)
                        {
                            values.Add(dataPoint);
                        }
                    }
                    else
                    {
                        values = series.Values.Cast<ChartPoint>().ToList();
                    }

                    // have to add the converter we want to use, else will use default
                    serializer.Converters.Add(new ColorJsonConverter());

                    writer.WritePropertyName("Values");
                    serializer.Serialize(writer, values);
                    writer.WritePropertyName("Color");
                    serializer.Serialize(writer, series.Color);
                    writer.WritePropertyName("ScatterMarkerSymbol");
                    serializer.Serialize(writer, series.ScatterMarkerSymbol);
                    break;

                case CandlestickSeries candlestickSeries:
                    writer.WritePropertyName("Values");
                    serializer.Serialize(writer, candlestickSeries.Values.Cast<Candlestick>().ToList(), typeof(Candlestick));
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

            var name = jObject["Name"].Value<string>();
            var unit = jObject["Unit"].Value<string>();
            var index = jObject["Index"].Value<int>();
            var seriesType = (SeriesType)jObject["SeriesType"].Value<int>();

            var values = (JArray)jObject["Values"];

            try
            {
                var chartPoints = values.ToObject<List<ChartPoint>>(serializer);
                // This is a Series
                return new Series()
                {
                    Name = name,
                    Unit = unit,
                    Index = index,
                    SeriesType = seriesType,
                    Color = jObject["Color"].ToObject<Color>(serializer),
                    ScatterMarkerSymbol = jObject["ScatterMarkerSymbol"].ToObject<ScatterMarkerSymbol>(serializer),
                    Values = chartPoints.Cast<ISeriesPoint>().ToList()
                };
            } catch (JsonSerializationException)
            {
                // Do nothing, try another series type
            }

            try
            {
                var candlesticks = values.ToObject<List<Candlestick>>(serializer);
                // This is a CandlestickSeries
                return new CandlestickSeries()
                {
                    Name = name,
                    Unit = unit,
                    Index = index,
                    SeriesType = seriesType,
                    Values = candlesticks.Cast<ISeriesPoint>().ToList()
                };
            }
            catch (JsonSerializationException)
            {
                // Do nothing, we'll return null
            }

            return null;
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

        /// <summary>
        /// This converter wont be used to read JSON. Will throw exception if manually called.
        /// </summary>
        public override bool CanRead => true;
    }
}
