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

namespace QuantConnect.Api
{
    /// <summary>
    /// File for a project
    /// </summary>
    public class ProjectFile
    {
        /// <summary>
        /// Name of a project file
        /// </summary>
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        /// <summary>
        /// Contents of the project file
        /// </summary>
        [JsonProperty(PropertyName = "content")]
        public string Code { get; set; }

        /// <summary>
        /// DateTime project file was modified
        /// </summary>
        [JsonProperty(PropertyName = "modified")]
        public DateTime DateModified{ get; set; }

        /// <summary>
        /// Indicates if the project file is a library or not
        /// </summary>
        [JsonProperty(PropertyName = "isLibrary")]
        public bool IsLibrary { get; set; }

        /// <summary>
        /// Indicates if the project file is open or not
        /// </summary>
        [JsonProperty(PropertyName = "open")]
        public bool Open { get; set; }

        /// <summary>
        /// ID of the project
        /// </summary>
        [JsonProperty(PropertyName = "projectId")]
        public int ProjectId { get; set; }

        /// <summary>
        /// ID of the project file, can be null
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public int? Id { get; set; }
    }

    /// <summary>
    /// Response received when creating a file or reading one file or more in a project
    /// </summary>
    public class ProjectFilesResponse : RestResponse
    {
        /// <summary>
        /// List of project file information
        /// </summary>
        [JsonProperty(PropertyName = "files")]
        public List<ProjectFile> Files { get; set; }
    }
}
