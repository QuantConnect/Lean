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

namespace QuantConnect.Data.Custom.SEC
{
    public class SECReportIndexFile
    {
        /// <summary>
        /// First and only root entry of SEC index.json
        /// </summary>
        [JsonProperty("directory")]
        public SECReportIndexDirectory Directory;
    }

    public class SECReportIndexDirectory
    {
        /// <summary>
        /// Contains additional metadata regarding files present on the server
        /// </summary>
        [JsonProperty("item")]
        public List<SECReportIndexItem> Items;
        
        /// <summary>
        /// Path directory
        /// </summary>
        [JsonProperty("name")]
        public string Name;
        
        /// <summary>
        /// Parent directory (if one exists)
        /// </summary>
        [JsonProperty("parent-dir")]
        public string ParentDirectory;
    }

    public class SECReportIndexItem
    {
        /// <summary>
        /// Date the SEC submission was published
        /// </summary>
        [JsonProperty("last-modified")]
        public DateTime LastModified;
        
        /// <summary>
        /// Name of folder/file. Usually accession number
        /// </summary>
        [JsonProperty("name")]
        public string Name;
        
        /// <summary>
        /// Specifies what kind of file the entry is
        /// </summary>
        [JsonProperty("type")]
        public string FileType;
        
        /// <summary>
        /// Size of the file. Empty if directory
        /// </summary>
        [JsonProperty("size")]
        public string Size;
    }
}

