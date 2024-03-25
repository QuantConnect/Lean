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
using Newtonsoft.Json;
using System.Collections.Generic;

namespace QuantConnect.Api
{
    /// <summary>
    /// Collaborator responses
    /// </summary>
    public class Collaborator
    {
        /// <summary>
        /// User id
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public int Id { get; set; }

        /// <summary>
        /// User id
        /// </summary>
        [JsonProperty(PropertyName = "uid")]
        public int Uid { get; set; }

        /// <summary>
        /// Indicate if the user have live control
        /// </summary>
        [JsonProperty(PropertyName = "liveControl")]
        public bool LiveControl { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [JsonProperty(PropertyName = "bliveControl")]
        public bool BLiveControl { get; set; }

        /// <summary>
        /// The permission this user is given. Can be "read"
        /// or "write"
        /// </summary>
        [JsonProperty(PropertyName = "permission")]
        public string Permission { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [JsonProperty(PropertyName = "epermission")]
        public string EPermission { get; set; }

        /// <summary>
        /// The url of the user profile image
        /// </summary>
        [JsonProperty(PropertyName = "profileImage")]
        public string ProfileImage { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [JsonProperty(PropertyName = "profileimage")]
        public string Profileimage { get; set; }

        /// <summary>
        /// The registered email of the user
        /// </summary>
        [JsonProperty(PropertyName = "email")]
        public string Email { get; set; }

        /// <summary>
        /// The display name of the user
        /// </summary>
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        /// <summary>
        /// The biography of the user
        /// </summary>
        [JsonProperty(PropertyName = "bio")]
        public string Bio { get; set; }

        /// <summary>
        /// Indicate if the user is the owner of the project
        /// </summary>
        [JsonProperty(PropertyName = "owner")]
        public bool Owner { get; set; }
    }

    /// <summary>
    /// Library response
    /// </summary>
    public class Library
    {

        /// <summary>
        /// Project Id of the library project
        /// </summary>
        [JsonProperty(PropertyName = "projectId")]
        public int Projectid { get; set; }

        /// <summary>
        /// Name of the library project
        /// </summary>
        [JsonProperty(PropertyName = "libraryName")]
        public string LibraryName { get; set; }

        /// <summary>
        /// Name of the library project owner
        /// </summary>
        [JsonProperty(PropertyName = "ownerName")]
        public string OwnerName { get; set; }

        /// <summary>
        /// Indicate if the library project can be accessed
        /// </summary>
        [JsonProperty(PropertyName = "access")]
        public bool Access { get; set; }
    }

    /// <summary>
    /// The chart display properties
    /// </summary>
    public class GridChart
    {
        /// <summary>
        /// The chart name
        /// </summary>
        [JsonProperty(PropertyName = "chartName")]
        public string ChartName { get; set;}

        /// <summary>
        /// Width of the chart
        /// </summary>
        [JsonProperty(PropertyName = "width")]
        public int Width { get; set; }

        /// <summary>
        /// Height of the chart
        /// </summary>
        [JsonProperty(PropertyName = "height")]
        public int Height { get; set; }

        /// <summary>
        /// Number of rows of the chart
        /// </summary>
        [JsonProperty(PropertyName = "row")]
        public int Row { get; set; }

        /// <summary>
        /// Number of columns of the chart
        /// </summary>
        [JsonProperty(PropertyName = "column")]
        public int Column { get; set; }

        /// <summary>
        /// Sort of the chart
        /// </summary>
        [JsonProperty(PropertyName = "sort")]
        public int Sort { get; set; }
    }

    /// <summary>
    /// The grid arrangement of charts
    /// </summary>
    public class Grid
    {
        /// <summary>
        /// List of chart in the xs position
        /// </summary>
        [JsonProperty(PropertyName = "xs")]
        public List<GridChart> Xs { get; set; }

        /// <summary>
        /// List of chart in the sm position
        /// </summary>
        [JsonProperty(PropertyName = "sm")]
        public List<GridChart> Sm { get; set; }

        /// <summary>
        /// List of chart in the md position
        /// </summary>
        [JsonProperty(PropertyName = "md")]
        public List<GridChart> Md { get; set; }

        /// <summary>
        /// List of chart in the lg position
        /// </summary>
        [JsonProperty(PropertyName = "lg")]
        public List<GridChart> Lg { get; set; }

        /// <summary>
        /// List of chart in the xl position
        /// </summary>
        [JsonProperty(PropertyName = "xl")]
        public List<GridChart> Xl { get; set; }
    }

    /// <summary>
    /// Encryption key details
    /// </summary>
    public class EncryptionKey
    {
        /// <summary>
        /// Encryption key id
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        /// <summary>
        /// Name of the encryption key
        /// </summary>
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }
    }

    /// <summary>
    /// Parameter set
    /// </summary>
    public class Parameter
    {
        /// <summary>
        /// Name of parameter
        /// </summary>
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        /// <summary>
        /// Value of parameter
        /// </summary>
        [JsonProperty(PropertyName = "value")]
        public string Value { get; set; }
    }

    /// <summary>
    /// Response from reading a project by id.
    /// </summary>
    public class Project : RestResponse
    {
        private List<GridChart> _gridObject;
        private List<GridChart> _liveGridObject;

        /// <summary>
        /// Project id
        /// </summary>
        [JsonProperty(PropertyName = "projectId")]
        public int ProjectId { get; set; }

        /// <summary>
        /// Name of the project
        /// </summary>
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        /// <summary>
        /// Date the project was created
        /// </summary>
        [JsonProperty(PropertyName = "created")]
        public DateTime Created { get; set; }

        /// <summary>
        /// Modified date for the project
        /// </summary>
        [JsonProperty(PropertyName = "modified")]
        public DateTime Modified { get; set; }

        /// <summary>
        /// Programming language of the project
        /// </summary>
        [JsonProperty(PropertyName = "language")]
        public Language Language { get; set; }

        /// <summary>
        /// The projects owner id
        /// </summary>
        [JsonProperty(PropertyName = "ownerId")]
        public int OwnerId { get; set; }

        /// <summary>
        /// List of collaborators
        /// </summary>
        [JsonProperty(PropertyName = "collaborators")]
        public List<Collaborator> Collaborators { get; set; }

        /// <summary>
        /// The version of LEAN this project is running on
        /// </summary>
        [JsonProperty(PropertyName = "leanVersionId")]
        public int LeanVersionId { get; set; }

        /// <summary>
        /// Indicate if the project is pinned to the master branch of LEAN
        /// </summary>
        [JsonProperty(PropertyName = "leanPinnedToMaster")]
        public bool LeanPinnedToMaster { get; set; }

        /// <summary>
        /// Indicate if you are the owner of the project
        /// </summary>
        [JsonProperty(PropertyName = "owner")]
        public bool Owner { get; set; }

        /// <summary>
        /// The project description
        /// </summary>
        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }

        /// <summary>
        /// Channel id
        /// </summary>
        [JsonProperty(PropertyName = "channelId")]
        public string ChannelId { get; set; }

        /// <summary>
        /// Optimization parameters
        /// </summary>
        [JsonProperty(PropertyName = "parameters")]
        public List<Parameter> Parameters { get; set; }

        [JsonProperty(PropertyName = "liveResults")]
        public List<Project> LiveResults { get; set; }

        [JsonProperty(PropertyName = "libraries")]
        public List<Library> Libraries { get; set; }

        /// <summary>
        /// Indicate if the project is an alpha stream deployment
        /// </summary>
        [JsonProperty(PropertyName = "isAlphaStreamDeployment")]
        public int IsAlphaStreamDeployment { get; set; }

        [JsonProperty(PropertyName = "grid" )]
        public string Grid { get; set; }

        /// <summary>
        /// Grid property de-serialized into a list of <see cref="GridChart"/>
        /// </summary>
        public List<GridChart> GridObject
        {
            get
            {
                if (_gridObject == null && !string.IsNullOrEmpty(Grid))
                {
                    _gridObject = JsonConvert.DeserializeObject<List<GridChart>>(Grid);
                }

                return _gridObject;
            }
        }

        [JsonProperty(PropertyName = "liveGrid")]
        public string LiveGrid { get; set; }

        /// <summary>
        /// LiveGrid property de-serialized into a list of <see cref="GridChart"/>
        /// </summary>
        public List<GridChart> LiveGridObject
        {
            get
            {
                if (_liveGridObject == null && !string.IsNullOrEmpty(LiveGrid))
                {
                    _liveGridObject = JsonConvert.DeserializeObject<List<GridChart>>(LiveGrid);
                }

                return _liveGridObject;
            }
        }

        [JsonProperty(PropertyName = "builder")]
        public int Builder { get; set; }

        /// <summary>
        /// The equity value of the last paper trading instance
        /// </summary>
        [JsonProperty(PropertyName = "paperEquity")]
        public decimal? PaperEquity { get; set; }

        /// <summary>
        /// The last live deployment active time
        /// </summary>
        [JsonProperty(PropertyName = "lastLiveDeployment")]
        public DateTime? LastLiveDeployment { get; set; }

        [JsonProperty(PropertyName = "liveForm")]
        public object LiveForm { get; set; }

        /// <summary>
        /// Indicates if the project is encrypted
        /// </summary>
        [JsonProperty(PropertyName = "encrypted")]
        public bool? Encrypted { get; set; }

        [JsonProperty(PropertyName = "codeRunning")]
        public bool CodeRunning {  get; set; }

        [JsonProperty(PropertyName = "leanEnvironment")]
        public int LeanEnvironment { get; set; }

        [JsonProperty(PropertyName = "encryptionKey")]
        public EncryptionKey EncryptionKey { get; set; }
    }

    /// <summary>
    /// Project list response
    /// </summary>
    public class ProjectResponse : RestResponse
    {
        /// <summary>
        /// List of projects for the authenticated user
        /// </summary>
        [JsonProperty(PropertyName = "projects")]
        public List<Project> Projects { get; set; }
    }
}
