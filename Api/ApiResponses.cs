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
    /// Verify if the credentials are OK.
    /// </summary>
    public class AuthenticationResponse : RestResponse
    {

    }

    /// <summary>
    /// Response from the compiler on a build event
    /// </summary>
    public class Compile : RestResponse
    {
        /// <summary>
        /// True on successful compile
        /// </summary>
        public bool BuildSuccess;

        /// <summary>
        /// Logs of the compilation request
        /// </summary>
        public List<string> Logs;
    }

    /// <summary>
    /// Response from reading a project by id.
    /// </summary>
    public class Project : RestResponse
    {
        /// <summary>
        /// Project id
        /// </summary>
        [JsonProperty(PropertyName = "projectId")]
        public int ProjectId;

        /// <summary>
        /// Name of the project
        /// </summary>
        [JsonProperty(PropertyName = "name")]
        public string Name;

        /// <summary>
        /// Date the project was created
        /// </summary>
        [JsonProperty(PropertyName = "created")]
        public DateTime Created;

        /// <summary>
        /// Modified date for the project
        /// </summary>
        [JsonProperty(PropertyName = "modified")]
        public DateTime Modified;

        /// <summary>
        /// Files for the project
        /// </summary>
        [JsonProperty(PropertyName = "files")]
        public List<ProjectFile> Files;
    }

    /// <summary>
    /// File for a project
    /// </summary>
    public class ProjectFile
    {
        /// <summary>
        /// Name of a project file
        /// </summary>
        [JsonProperty(PropertyName = "name")]
        public string Name;

        /// <summary>
        /// Contents of the project file
        /// </summary>
        [JsonProperty(PropertyName = "code")]
        public string Code;
    }

    /// <summary>
    /// Project list response
    /// </summary>
    public class ProjectList : RestResponse
    {
        /// <summary>
        /// List of projects for the authenticated user
        /// </summary>
        [JsonProperty(PropertyName = "projects")]
        public List<Project> Projects;
    }

    /// <summary>
    /// Base API response class for the QuantConnect API.
    /// </summary>
    public class RestResponse
    {
        /// <summary>
        /// JSON Constructor
        /// </summary>
        public RestResponse()
        {
            Success = false;
            Errors = new List<string>();
        }

        /// <summary>
        /// Indicate if the API request was successful.
        /// </summary>
        [JsonProperty(PropertyName = "success")]
        public bool Success;

        /// <summary>
        /// List of errors with the API call.
        /// </summary>
        [JsonProperty(PropertyName = "errors")]
        public List<string> Errors;
    }
}
