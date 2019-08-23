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
using QuantConnect.Api;
using QuantConnect.Packets;

namespace QuantConnect.API
{
    /// <summary>
    /// Details a live algorithm from the "live/read" Api endpoint
    /// </summary>
    public class LiveAlgorithmResults : RestResponse
    {
        /// <summary>
        /// Represents data about the live running algorithm returned from the server
        /// </summary>
        public LiveResultsData LiveResults { get; set; }
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
