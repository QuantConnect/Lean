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
using System.Collections.Generic;
using System;

namespace QuantConnect.Api
{
    /// <summary>
    /// Response received when fetching Object Store
    /// </summary>
    public class GetObjectStoreResponse : RestResponse
    {
        /// <summary>
        /// Job ID which can be used for querying state or packaging
        /// </summary>
        [JsonProperty("jobId")]
        public string JobId { get; set; }

        /// <summary>
        /// The URL to download the object. This can also be null
        /// </summary>
        [JsonProperty("url")]
        public string Url { get; set; }
    }

    public class BasicObjectStore
    {
        /// <summary>
        /// Object store key
        /// </summary>
        [JsonProperty(PropertyName = "key")]
        public string Key {  get; set; }

        /// <summary>
        /// Last time it was modified
        /// </summary>
        [JsonProperty(PropertyName = "modified")]
        public DateTime? Modified { get; set; }

        /// <summary>
        /// MIME type
        /// </summary>
        [JsonProperty(PropertyName = "mime")]
        public string Mime { get; set; }

        /// <summary>
        /// File size
        /// </summary>
        [JsonProperty(PropertyName = "size")]
        public decimal? Size { get; set; }
    }

    /// <summary>
    /// Summary information of the Object Store
    /// </summary>
    public class SummaryObjectStore: BasicObjectStore
    {
        /// <summary>
        /// File or folder name
        /// </summary>
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        /// <summary>
        /// True if it is a folder, false otherwise
        /// </summary>
        [JsonProperty(PropertyName = "isFolder")]
        public bool IsFolder { get; set; }
    }

    /// <summary>
    /// Object Store file properties
    /// </summary>
    public class PropertiesObjectStore: BasicObjectStore
    {
        /// <summary>
        /// Date this object was created
        /// </summary>
        [JsonProperty(PropertyName = "created")]
        public DateTime Created { get; set; }

        /// <summary>
        /// MD5 (hashing algorithm) hash authentication code
        /// </summary>
        [JsonProperty(PropertyName = "md5")]
        public string Md5 { get; set; }

        /// <summary>
        /// Preview of the Object Store file content
        /// </summary>
        [JsonProperty(PropertyName = "preview")]
        public string Preview { get; set; }
    }

    /// <summary>
    /// Response received containing a list of stored objects metadata, as well as the total size of all of them.
    /// </summary>
    public class ListObjectStoreResponse : RestResponse
    {
        /// <summary>
        /// Path to the files in the Object Store
        /// </summary>
        [JsonProperty(PropertyName = "path")]
        public string Path { get; set; }

        /// <summary>
        /// List of objects stored
        /// </summary>
        [JsonProperty(PropertyName = "objects")]
        public List<SummaryObjectStore> Objects { get; set; }

        /// <summary>
        /// Size of all objects stored in bytes
        /// </summary>
        [JsonProperty(PropertyName = "objectStorageUsed")]
        public int ObjectStorageUsed { get; set; }

        /// <summary>
        /// Size of all the objects stored in human-readable format
        /// </summary>
        [JsonProperty(PropertyName = "objectStorageUsedHuman")]
        public string ObjectStorageUsedHuman { get; set; }
    }

    /// <summary>
    /// Response received containing the properties of the requested Object Store
    /// </summary>
    public class PropertiesObjectStoreResponse : RestResponse
    {
        /// <summary>
        /// Object Store properties
        /// </summary>
        [JsonProperty(PropertyName = "metadata")]
        public PropertiesObjectStore Properties { get; set; }
    }
}
