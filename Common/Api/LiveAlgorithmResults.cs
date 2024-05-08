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
        /// <summary>
        /// Error message
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Indicates the status of the algorihtm, i.e. 'Running', 'Stopped'
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Algorithm deployment ID
        /// </summary>
        public string DeployId { get; set; }

        /// <summary>
        /// The snapshot project ID for cloning the live development's source code.
        /// </summary>
        public int CloneId { get; set; }

        /// <summary>
        /// Date the live algorithm was launched
        /// </summary>
        public DateTime Launched { get; set; }

        /// <summary>
        /// Date the live algorithm was stopped
        /// </summary>
        public DateTime? Stopped { get; set; }

        /// <summary>
        /// Brokerage used in the live algorithm
        /// </summary>
        public string Brokerage { get; set; }

        /// <summary>
        /// Security types present in the live algorithm
        /// </summary>
        public string SecurityTypes { get; set; }

        /// <summary>
        /// Name of the project the live algorithm is in
        /// </summary>
        public string ProjectName { get; set; }

        /// <summary>
        /// Name of the data center where the algorithm is physically located.
        /// </summary>
        public string Datacenter { get; set; }

        /// <summary>
        /// Indicates if the algorithm is being live shared
        /// </summary>
        public bool Public { get; set; }

        /// <summary>
        /// Files present in the project in which the algorithm is
        /// </summary>
        public List<ProjectFile> Files { get; set; }

        /// <summary>
        /// Runtime banner/updating statistics in the title banner of the live algorithm GUI.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public IDictionary<string, string> RuntimeStatistics { get; set; }

        /// <summary>
        /// Charts updates for the live algorithm since the last result packet
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
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
