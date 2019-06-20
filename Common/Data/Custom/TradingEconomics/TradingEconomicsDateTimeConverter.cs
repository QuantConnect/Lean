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

using Newtonsoft.Json;
using System;

namespace QuantConnect.Data.Custom.TradingEconomics
{
    /// <summary>
    /// DateTime JSON Converter that handles null value
    /// </summary>
    public class TradingEconomicsDateTimeConverter : JsonConverter
    {
        /// <summary>
        /// Parse Trading Economics DateTime to C# DateTime
        /// </summary>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return reader.Value ?? DateTime.MinValue;
        }

        /// <summary>
        /// Write DateTime objects to JSON
        /// </summary>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            // The CanConvert method guarantees the value will be a DateTime
            var date = (DateTime) value;
            if (date == DateTime.MinValue)
            {
                writer.WriteNull();
            }
            else
            {
                writer.WriteValue(date);
            }
        }

        /// <summary>
        /// Indicate if we can convert this object.
        /// </summary>
        public override bool CanConvert(Type objectType) => objectType == typeof(DateTime);
    }
}