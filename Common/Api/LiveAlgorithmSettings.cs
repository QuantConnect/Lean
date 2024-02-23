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
using QuantConnect.Brokerages;

namespace QuantConnect.Api
{
    /// <summary>
    /// Helper class to put BaseLiveAlgorithmSettings in proper format.
    /// </summary>
    public class LiveAlgorithmApiSettingsWrapper
    {
        /// <summary>
        /// Constructor for LiveAlgorithmApiSettingsWrapper
        /// </summary>
        /// <param name="projectId">Id of project from QuantConnect</param>
        /// <param name="compileId">Id of compilation of project from QuantConnect</param>
        /// <param name="nodeId">Server type to run live Algorithm</param>
        /// <param name="settings">Dictionary with Live Algorithm Settings</see> for a specific brokerage</param>
        /// <param name="version">The version identifier</param>
        public LiveAlgorithmApiSettingsWrapper(int projectId, string compileId, string nodeId, Dictionary<string, string> settings, string version = "-1")
        {
            VersionId = version;
            ProjectId = projectId;
            CompileId = compileId;
            NodeId = nodeId;
            Brokerage = settings;
        }

        /// <summary>
        /// -1 is master
        /// </summary>
        [JsonProperty(PropertyName = "versionId")]
        public string VersionId { get; set; }

        /// <summary>
        /// Project id for the live instance
        /// </summary>
        [JsonProperty(PropertyName = "projectId")]
        public int ProjectId { get; private set; }

        /// <summary>
        /// Compile Id for the live algorithm
        /// </summary>
        [JsonProperty(PropertyName = "compileId")]
        public string CompileId { get; private set; }

        /// <summary>
        /// Id of the node being used to run live algorithm
        /// </summary>
        [JsonProperty(PropertyName = "nodeId")]
        public string NodeId { get; private set; }

        /// <summary>
        /// The API expects the settings as part of a brokerage object
        /// </summary>
        [JsonProperty(PropertyName = "brokerage")]
        public Dictionary<string, string> Brokerage { get; private set; }
    }
}
