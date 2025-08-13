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
 *
*/

using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;

namespace QuantConnect.Util
{
    /// <summary>
    /// Provides a json converter that allows defining the date time format used
    /// </summary>
    public class DateTimeJsonConverter : JsonConverter
    {
        private readonly List<IsoDateTimeConverter> _converters;

        /// <summary>
        /// True, can read a json into a date time
        /// </summary>
        public override bool CanRead => true;

        /// <summary>
        /// True, can write a datetime to json
        /// </summary>
        public override bool CanWrite => true;

        /// <summary>
        ///  Initializes a new instance of the <see cref="DateTimeJsonConverter"/> class
        /// </summary>
        /// <param name="format">>The date time format</param>
        public DateTimeJsonConverter(string format)
        {
            _converters = [new IsoDateTimeConverter() { DateTimeFormat = format }];
        }

        /// <summary>
        ///  Initializes a new instance of the <see cref="DateTimeJsonConverter"/> class
        /// </summary>
        /// <param name="format">>The date time format</param>
        /// <param name="format2">Other format for backwards compatibility</param>
        public DateTimeJsonConverter(string format, string format2) : this(format)
        {
            _converters.Add(new IsoDateTimeConverter() { DateTimeFormat = format2 });
        }

        /// <summary>
        ///  Initializes a new instance of the <see cref="DateTimeJsonConverter"/> class
        /// </summary>
        /// <param name="format">>The date time format</param>
        /// <param name="format2">Other format for backwards compatibility</param>
        /// <param name="format3">Other format for backwards compatibility</param>
        public DateTimeJsonConverter(string format, string format2, string format3) : this(format, format2)
        {
            _converters.Add(new IsoDateTimeConverter() { DateTimeFormat = format3 });
        }

        /// <summary>
        /// True if can convert the given object type
        /// </summary>
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(DateTime) || objectType == typeof(string) || objectType == typeof(DateTime?);
        }

        /// <summary>
        /// Converts the given value
        /// </summary>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            foreach (var converter in _converters)
            {
                try
                {
                    return converter.ReadJson(reader, objectType, existingValue, serializer);
                }
                catch
                {
                }
            }
            throw new JsonSerializationException($"Unexpected value when converting date. Expected formats: {string.Join(",", _converters.Select(x => x.DateTimeFormat))}");
        }

        /// <summary>
        /// Writes the given value to json
        /// </summary>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            _converters[0].WriteJson(writer, value, serializer);
        }
    }
}
