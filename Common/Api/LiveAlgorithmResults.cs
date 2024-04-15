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
using Newtonsoft.Json.Converters;
using QuantConnect.Packets;
using System;
using System.Collections.Generic;

namespace QuantConnect.Api
{
    /// <summary>
    /// Details a live algorithm from the "live/read" Api endpoint
    /// </summary>
    public class LiveAlgorithmResults : RestResponse
    {
        [JsonProperty(PropertyName = "message")]
        public string Message { get; set; }

        [JsonProperty(PropertyName = "status")]
        public string Status { get; set; }

        [JsonProperty(PropertyName = "deployId")]
        public string DeployId { get; set; }

        [JsonProperty(PropertyName = "cloneId")]
        public int CloneId { get; set; }

        [JsonProperty(PropertyName = "launched")]
        public DateTime Launched { get; set; }

        [JsonProperty(PropertyName = "stopped")]
        public DateTime? Stopped { get; set; }

        [JsonProperty(PropertyName = "brokerage")]
        public string Brokerage { get; set; }

        [JsonProperty(PropertyName = "securityTypes")]
        public string SecurityTypes { get; set; }

        [JsonProperty(PropertyName = "projectName")]
        public string ProjectName { get; set; }

        [JsonProperty(PropertyName = "datacenter")]
        public string Datacenter { get; set; }

        [JsonProperty(PropertyName = "isPublicStreaming")]
        public bool IsPublicStreaming { get; set; }

        [JsonProperty(PropertyName = "files")]
        public List<ProjectFile> Files { get; set; }

        /// <summary>
        /// Runtime banner/updating statistics in the title banner of the live algorithm GUI.
        /// </summary>
        [JsonProperty(PropertyName = "runtimeStatistics", NullValueHandling = NullValueHandling.Ignore)]
        public IDictionary<string, string> RuntimeStatistics { get; set; }

        /// <summary>
        /// Charts updates for the live algorithm since the last result packet
        /// </summary>
        [JsonProperty(PropertyName = "charts", NullValueHandling = NullValueHandling.Ignore)]
        public IDictionary<string, Chart> Charts { get; set; }
    }

    /// <summary>
    /// Holds information about the state and operation of the live running algorithm
    /// </summary>
    public class LiveResultsData
    {
        /// <summary>
        /// Results version
        /// </summary>
        [JsonProperty(PropertyName = "version")]
        public int Version { get; set; }

        /// <summary>
        /// Temporal resolution of the results returned from the Api
        /// </summary>
        [JsonProperty(PropertyName = "resolution"), JsonConverter(typeof(StringEnumConverter))]
        public Resolution Resolution { get; set; }

        /// <summary>
        /// Class to represent the data groups results return from the Api
        /// </summary>
        [JsonProperty(PropertyName = "results")]
        public LiveResult Results { get; set; }
    }
}
