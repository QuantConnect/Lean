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

using System.Collections.Generic;
using Newtonsoft.Json;

namespace QuantConnect.Api
{
    /// <summary>
    /// Collection of <see cref="Node"/> objects for each target environment.
    /// </summary>
    public class ProjectNodes : NodeList
    {
    }

    /// <summary>
    /// Response received when reading all files of a project
    /// </summary>
    public class ProjectNodesResponse : RestResponse
    {
        /// <summary>
        /// List of project file information
        /// </summary>
        [JsonProperty(PropertyName = "nodes")]
        public ProjectNodes Nodes { get; set; }

        /// <summary>
        /// True if the node is automatically selected
        /// </summary>
        [JsonProperty(PropertyName = "autoSelectNode")]
        public bool AutoSelectNode { get; set; }
    }
}
