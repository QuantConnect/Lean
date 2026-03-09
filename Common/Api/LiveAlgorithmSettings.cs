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
using System.Linq;
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
        /// <param name="settings">Dictionary with brokerage specific settings. Each brokerage requires certain specific credentials
        ///                         in order to process the given orders. Each key in this dictionary represents a required field/credential
        ///                         to provide to the brokerage API and its value represents the value of that field. For example: "brokerageSettings: {
        ///                         "id": "Binance", "binance-api-secret": "123ABC", "binance-api-key": "ABC123"}. It is worth saying,
        ///                         that this dictionary must always contain an entry whose key is "id" and its value is the name of the brokerage
        ///                         (see <see cref="Brokerages.BrokerageName"/>)</param>
        /// <param name="version">The version identifier</param>
        /// <param name="dataProviders">Dictionary with data providers credentials. Each data provider requires certain credentials
        ///                         in order to retrieve data from their API. Each key in this dictionary describes a data provider name
        ///                         and its corresponding value is another dictionary with the required key-value pairs of credential
        ///                         names and values. For example: "dataProviders: {InteractiveBrokersBrokerage : { "id": 12345, "environement" : "paper",
        ///                         "username": "testUsername", "password": "testPassword"}}"</param>
        /// <param name="parameters">Dictionary to specify the parameters for the live algorithm</param>
        /// <param name="notification">Dictionary with the lists of events and targets</param>
        public LiveAlgorithmApiSettingsWrapper(
            int projectId,
            string compileId,
            string nodeId,
            Dictionary<string, object> settings,
            string version = "-1",
            Dictionary<string, object> dataProviders = null,
            Dictionary<string, string> parameters = null,
            Dictionary<string, List<string>> notification = null)
        {
            VersionId = version;
            ProjectId = projectId;
            CompileId = compileId;
            NodeId = nodeId;
            Brokerage = settings;

            var quantConnectDataProvider = new Dictionary<string, string>
            {
                { "id", "QuantConnectBrokerage" },
            };

            DataProviders = dataProviders ?? new Dictionary<string, object>()
            {
                { "QuantConnectBrokerage", quantConnectDataProvider },
            };
            Signature = CompileId.Split("-").LastOrDefault();
            Parameters = parameters ?? new Dictionary<string, string>();
            Notification = notification ?? new Dictionary<string, List<string>>();
            AutomaticRedeploy = false;
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
        /// Signature of the live algorithm
        /// </summary>
        [JsonProperty(PropertyName = "signature")]
        public string Signature { get; private set; }

        /// <summary>
        /// True to enable Automatic Re-Deploy of the live algorithm,
        /// false otherwise
        /// </summary>
        [JsonProperty(PropertyName = "automaticRedeploy")]
        public bool AutomaticRedeploy { get; private set; }

        /// <summary>
        /// The API expects the settings as part of a brokerage object
        /// </summary>
        [JsonProperty(PropertyName = "brokerage")]
        public Dictionary<string, object> Brokerage { get; private set; }

        /// <summary>
        /// Dictionary with the data providers and their corresponding credentials
        /// </summary>
        [JsonProperty(PropertyName = "dataProviders")]
        public Dictionary<string, object> DataProviders { get; private set; }

        /// <summary>
        /// Dictionary with the parameters to be used in the live algorithm
        /// </summary>
        [JsonProperty(PropertyName = "parameters")]
        public Dictionary<string, string> Parameters { get; private set; }

        /// <summary>
        /// Dictionary with the lists of events and targets
        /// </summary>
        [JsonProperty(PropertyName = "notification")]
        public Dictionary<string, List<string>> Notification { get; private set; }
    }
}
