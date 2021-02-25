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
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace QuantConnect.Notifications
{
    /// <summary>
    /// Defines a <see cref="JsonConverter"/> to be used when deserializing to the <see cref="Notification"/> class.
    /// </summary>
    public class NotificationJsonConverter : JsonConverter
    {
        /// <summary>
        /// Use default implementation to write the json
        /// </summary>
        public override bool CanWrite => false;

        /// <summary>
        /// Writes the JSON representation of the object.
        /// </summary>
        /// <param name="writer">The <see cref="T:Newtonsoft.Json.JsonWriter"/> to write to.</param><param name="value">The value.</param><param name="serializer">The calling serializer.</param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException("Not implemented, should not be called");
        }

        /// <summary>
        /// Reads the JSON representation of the object.
        /// </summary>
        /// <param name="reader">The <see cref="T:Newtonsoft.Json.JsonReader"/> to read from.</param><param name="objectType">Type of the object.</param><param name="existingValue">The existing value of object being read.</param><param name="serializer">The calling serializer.</param>
        /// <returns>
        /// The object value.
        /// </returns>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jObject = JObject.Load(reader);

            JToken token;
            if (jObject.TryGetValue("PhoneNumber", StringComparison.InvariantCultureIgnoreCase, out token))
            {
                var message = jObject.GetValue("Message", StringComparison.InvariantCultureIgnoreCase);

                return new NotificationSms(token.ToString(), message?.ToString());
            }
            else if (jObject.TryGetValue("Subject", StringComparison.InvariantCultureIgnoreCase, out token))
            {
                var data = jObject.GetValue("Data", StringComparison.InvariantCultureIgnoreCase);
                var message = jObject.GetValue("Message", StringComparison.InvariantCultureIgnoreCase);
                var address = jObject.GetValue("Address", StringComparison.InvariantCultureIgnoreCase);
                var headers= jObject.GetValue("Headers", StringComparison.InvariantCultureIgnoreCase);

                return new NotificationEmail(address?.ToString(), token.ToString(), message?.ToString(), data?.ToString(), headers?.ToObject<Dictionary<string, string>>());
            }
            else if (jObject.TryGetValue("Address", StringComparison.InvariantCultureIgnoreCase, out token))
            {
                var headers = jObject.GetValue("Headers", StringComparison.InvariantCultureIgnoreCase);
                var data = jObject.GetValue("Data", StringComparison.InvariantCultureIgnoreCase);

                return new NotificationWeb(token.ToString(), data?.ToString(), headers?.ToObject<Dictionary<string, string>>());
            }

            throw new NotImplementedException($"Unexpected json object: '{jObject.ToString(Formatting.None)}'");
        }

        /// <summary>
        /// Determines whether this instance can convert the specified object type.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <returns>
        /// <c>true</c> if this instance can convert the specified object type; otherwise, <c>false</c>.
        /// </returns>
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Notification);
        }
    }
}
