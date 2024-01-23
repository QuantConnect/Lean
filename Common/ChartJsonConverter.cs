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
using Newtonsoft.Json;

namespace QuantConnect
{
    public class ChartJsonConverter : JsonConverter
    {
        /// <summary>
        /// This converter wont be used to read JSON. Will throw exception if manually called.
        /// </summary>
        public override bool CanRead => false;

        public override bool CanConvert(Type objectType)
        {
            return typeof(Chart).IsAssignableFrom(objectType);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var chart = value as Chart;
            if (chart == null)
            {
                return;
            }

            writer.WriteStartObject();

            writer.WritePropertyName("Name");
            writer.WriteValue(chart.Name);
            writer.WritePropertyName("ChartType");
            writer.WriteValue(chart.ChartType);

            if (chart.Symbol != null)
            {
                writer.WritePropertyName("Symbol");
                serializer.Serialize(writer, chart.Symbol);
            }

            if(chart.LegendDisabled)
            {
                writer.WritePropertyName("LegendDisabled");
                writer.WriteValue(true);
            }

            writer.WritePropertyName("Series");
            writer.WriteStartObject();
            // we sort the series in ascending count so that they are chart nicely, has value for stacked area series so they're continuous 
            foreach (var kvp in chart.Series.OrderBy(x => x.Value.Values.Count))
            {
                writer.WritePropertyName(kvp.Key);
                serializer.Serialize(writer, kvp.Value);
            }
            writer.WriteEndObject();

            writer.WriteEndObject();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
