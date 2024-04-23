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
using QuantConnect.Util;
using QuantConnect.Orders;
using Newtonsoft.Json.Linq;
using QuantConnect.Packets;
using QuantConnect.Securities;
using System.Collections.Generic;

namespace QuantConnect.Api
{
    /// <summary>
    /// Custom JsonConverter for LiveResults data for live algorithms
    /// </summary>
    public class LiveAlgorithmResultsJsonConverter : JsonConverter
    {
        /// <summary>
        /// Gets a value indicating whether this <see cref="T:Newtonsoft.Json.JsonConverter"/> can write JSON.
        /// </summary>
        /// <value>
        /// <c>true</c> if this <see cref="T:Newtonsoft.Json.JsonConverter"/> can write JSON; otherwise, <c>false</c>.
        /// </value>
        public override bool CanWrite
        {
            get { return false; }
        }

        /// <summary>
        /// Writes the JSON representation of the object.
        /// </summary>
        /// <param name="writer">The <see cref="T:Newtonsoft.Json.JsonWriter"/> to write to.</param><param name="value">The value.</param><param name="serializer">The calling serializer.</param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException("The LiveAlgorithmResultsJsonConverter does not implement a WriteJson method.");
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
            return typeof(LiveAlgorithmResults).IsAssignableFrom(objectType);
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

            // We don't deserialize the json object directly since it contains properties such as `files` and `charts`
            // that need to be deserialized in a different way
            var liveAlgoResults = new LiveAlgorithmResults
            {
                Message = jObject["message"].Value<string>(),
                Status = jObject["status"].Value<string>(),
                DeployId = jObject["deployId"].Value<string>(),
                CloneId = jObject["cloneId"].Value<int>(),
                Launched = jObject["launched"].Value<DateTime>(),
                Stopped = jObject["stopped"].Value<DateTime?>(),
                Brokerage = jObject["brokerage"].Value<string>(),
                SecurityTypes = jObject["securityTypes"].Value<string>(),
                ProjectName = jObject["projectName"].Value<string>(),
                Datacenter = jObject["datacenter"].Value<string>(),
                Public = jObject["public"].Value<bool>(),
                Success = jObject["success"].Value<bool>()
            };

            if (!liveAlgoResults.Success)
            {
                // Either there was an error in the running algorithm or the algorithm hasn't started
                liveAlgoResults.Errors = jObject.Last.Children().Select(error => error.ToString()).ToList();
                return liveAlgoResults;
            }

            // Deserialize charting data
            var chartDictionary = new Dictionary<string, Chart>();
            var charts = jObject["charts"] ?? jObject["Charts"];
            if (charts != null)
            {
                var stringCharts = jObject["charts"]?.ToString() ?? jObject["Charts"].ToString();
                if(!string.IsNullOrEmpty(stringCharts))
                {
                    chartDictionary = JsonConvert.DeserializeObject<Dictionary<string, Chart>>(stringCharts);
                }
            }

            // Deserialize files data
            var projectFiles = new List<ProjectFile>();
            var files = jObject["files"] ?? jObject["Files"];
            if (files != null)
            {
                var stringFiles = jObject["files"]?.ToString() ?? jObject["Files"].ToString();
                if (!string.IsNullOrEmpty(stringFiles))
                {
                    projectFiles = JsonConvert.DeserializeObject<List<ProjectFile>>(stringFiles);
                }
            }

            liveAlgoResults.Charts = chartDictionary;
            liveAlgoResults.Files = projectFiles;

            return liveAlgoResults;
        }
    }
}
