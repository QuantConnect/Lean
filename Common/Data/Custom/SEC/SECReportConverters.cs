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
using Newtonsoft.Json.Converters;

namespace QuantConnect.Data.Custom.SEC
{
    /// <summary>
    /// Specifies format for parsing <see cref="DateTime"/> values from SEC data
    /// </summary>
    public class SECReportDateTimeConverter : IsoDateTimeConverter
    {
        public SECReportDateTimeConverter()
        {
            base.DateTimeFormat = "yyyyMMdd HH:mm:ss";
        }
    }

    /// <summary>
    /// Class converts single elements from JSON into a <see cref="List{T}"/>.
    /// This is done because there can be multiple filings per day, and we
    /// can't serialize single elements to a <see cref="List{T}"/> without this
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class PossibleListConverter<T> : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return true;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }

        /// <summary>
        /// Converts single JSON elements to <see cref="List{T}"/> containing element.
        /// If multiple elements are found, the original value is returned.
        /// </summary>
        /// <param name="reader">Reader</param>
        /// <param name="objectType">Type of the current object the reader is on</param>
        /// <param name="existingValue"></param>
        /// <param name="serializer">Serializer instance in use</param>
        /// <returns><see cref="List{T}"/> containing potentially multiple entries</returns>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.StartObject || reader.TokenType == JsonToken.String)
            {
                var document = serializer.Deserialize<T>(reader);
                return new List<T>() {document};
            }
            if (reader.TokenType == JsonToken.StartArray)
            {
                return serializer.Deserialize(reader, objectType);
            }
            return new List<T>();
        }
    }
}
