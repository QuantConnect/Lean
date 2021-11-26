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

namespace QuantConnect.Securities.FutureOption.Api
{
    /// <summary>
    /// Product slate API call root response
    /// </summary>
    public class CMEProductSlateV2ListResponse
    {
        /// <summary>
        /// Products matching the search criteria
        /// </summary>
        [JsonProperty("products")]
        public List<CMEProductSlateV2ListEntry> Products { get; private set; }
    }

    /// <summary>
    /// Product entry describing the asset matching the search criteria
    /// </summary>
    public class CMEProductSlateV2ListEntry
    {
        /// <summary>
        /// CME ID for the asset
        /// </summary>
        [JsonProperty("id")]
        public int Id { get; private set; }

        /// <summary>
        /// Name of the product (e.g. E-mini NASDAQ futures)
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; private set; }

        /// <summary>
        /// Clearing code
        /// </summary>
        [JsonProperty("clearing")]
        public string Clearing { get; private set; }

        /// <summary>
        /// GLOBEX ticker
        /// </summary>
        [JsonProperty("globex")]
        public string Globex { get; private set; }

        /// <summary>
        /// Is traded in the GLOBEX venue
        /// </summary>
        [JsonProperty("globexTraded")]
        public bool GlobexTraded { get; private set; }

        /// <summary>
        /// Venues this asset trades on
        /// </summary>
        [JsonProperty("venues")]
        public string Venues { get; private set; }

        /// <summary>
        /// Asset type this product is cleared as (i.e. "Futures", "Options")
        /// </summary>
        [JsonProperty("cleared")]
        public string Cleared { get; private set; }

        /// <summary>
        /// Exchange the asset trades on (i.e. CME, NYMEX, COMEX, CBOT)
        /// </summary>
        [JsonProperty("exch")]
        public string Exchange { get; private set; }

        /// <summary>
        /// Asset class group ID - describes group of asset class (e.g. equities, agriculture, etc.)
        /// </summary>
        [JsonProperty("groupId")]
        public int GroupId { get; private set; }

        /// <summary>
        /// More specific ID describing product
        /// </summary>
        [JsonProperty("subGroupId")]
        public int subGroupId { get; private set; }
    }
}
