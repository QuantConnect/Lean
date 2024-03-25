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
    }

    /// <summary>
    /// Class for CreateProject API response
    /// </summary>
    public class ProjectFileCreate : ProjectFile
    {
        /// <summary>
        /// Project id
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
    }

    /// <summary>
    /// Base class for ReadProject API response
    /// </summary>
    public class BaseProjectFileRead : ProjectFile
    {
        [JsonProperty(PropertyName = "open")]
        public int Open { get; set; }

        [JsonProperty(PropertyName = "projectId")]
        public int ProjectId { get; set; }

        [JsonProperty(PropertyName = "id")]
        public int Id { get; set; }

        [JsonProperty(PropertyName = "collaborationId")]
        public string CollaborationId {  get; set; }

        [JsonProperty(PropertyName = "readOnly")]
        public bool ReadOnly { get; set; }
    }

    /// <summary>
    /// Class for read project file API response
    /// </summary>
    public class ProjectFileRead : BaseProjectFileRead
    {
        [JsonProperty(PropertyName = "binary")]
        public bool Binary { get; set; }
    }

    /// <summary>
    /// Class for read project files API response
    /// </summary>
    public class ProjectFilesRead : BaseProjectFileRead
    {
        [JsonProperty(PropertyName = "isLibrary")]
        public bool IsLibrary { get; set; }

        [JsonProperty(PropertyName = "userHasAccess")]
        public bool UserHasAccess { get; set; }
    }

    /// <summary>
    /// Class for update project filenames API response
    /// </summary>
    public class ProjectFileUpdateFileName : ProjectFile
    {
        [JsonProperty(PropertyName = "open")]
        public bool Open { get; set; }
    }

    // Response received when reading one file in a project
    public class ProjectFileReadResponse : RestResponse
    {
        /// <summary>
        /// List of project file information
        /// </summary>
        [JsonProperty(PropertyName = "files")]
        public List<ProjectFileRead> Files { get; set; }
    }

    /// <summary>
    /// Response received when reading all files in a project
    /// </summary>
    public class ProjectFilesReadResponse : RestResponse
    {
        /// <summary>
        /// List of project file information
        /// </summary>
        [JsonProperty(PropertyName = "files")]
        public List<ProjectFilesRead> Files { get; set; }
    }

    /// <summary>
    /// Response received when creating or updating the content a file
    /// </summary>
    public class ProjectFileCreateResponse : RestResponse
    {
        /// <summary>
        /// List of project file information
        /// </summary>
        [JsonProperty(PropertyName = "files")]
        public List<ProjectFileCreate> Files { get; set; }
    }

    /// <summary>
    /// Response received when updating the name of a file
    /// </summary>
    public class ProjectFileUpdateFileNameResponse : RestResponse
    {
        /// <summary>
        /// List of project file information
        /// </summary>
        [JsonProperty(PropertyName = "files")]
        public List<ProjectFileUpdateFileName> Files { get; set; }
    }
}