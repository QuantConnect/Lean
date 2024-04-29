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

using System.Linq;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text.RegularExpressions;

// Collection of response objects for Quantconnect Data/ endpoints
namespace QuantConnect.Api
{
    /// <summary>
    /// Data/Read response wrapper, contains link to requested data
    /// </summary>
    public class DataLink : RestResponse
    {
        /// <summary>
        /// Url to the data requested
        /// </summary>
        [JsonProperty(PropertyName = "link")]
        public string Url { get; set; }

        /// <summary>
        /// Remaining QCC balance on account after this transaction
        /// </summary>
        [JsonProperty(PropertyName = "balance")]
        public double Balance { get; set; }

        /// <summary>
        /// QCC Cost for this data link
        /// </summary>
        [JsonProperty(PropertyName = "cost")]
        public double Cost { get; set; }
    }

    /// <summary>
    /// Data/List response wrapper for available data
    /// </summary>
    public class DataList : RestResponse
    {
        /// <summary>
        /// List of all available data from this request
        /// </summary>
        [JsonProperty(PropertyName = "objects")]
        public List<string> AvailableData { get; set; }
    }

    /// <summary>
    /// Data/Prices response wrapper for prices by vendor
    /// </summary>
    public class DataPricesList : RestResponse
    {
        /// <summary>
        /// Dictionary of available datasources
        /// </summary>
        [JsonProperty(PropertyName = "datasources")]
        public Dictionary<string, DataSource> DataSources { get; set; }

        /// <summary>
        /// Collection of prices objects
        /// </summary>
        [JsonProperty(PropertyName = "prices")]
        public List<PriceEntry> Prices { get; set; }

        /// <summary>
        /// The Agreement URL for this Organization
        /// </summary>
        [JsonProperty(PropertyName = "agreement")]
        public string AgreementUrl { get; set; }

        /// <summary>
        /// Get the price in QCC for a given data file
        /// </summary>
        /// <param name="path">Lean data path of the file</param>
        /// <returns>QCC price for data, -1 if no entry found</returns>
        public int GetPrice(string path)
        {
            if (path == null)
            {
                return -1;
            }

            var entry = Prices.FirstOrDefault(x => x.RegEx.IsMatch(path));
            return entry?.Price ?? -1;
        }
    }

    /// <summary>
    /// Class containing basic datasource information
    /// </summary>
    public class DataSource
    {
        /// <summary>
        /// Indicates whether or not the data source requires the security master
        /// </summary>
        [JsonProperty(PropertyName = "requiresSecurityMaster")]
        public bool RequiresSecurityMaster { get; set; }

        /// <summary>
        /// List of available options for requesting the data source
        /// </summary>
        [JsonProperty(PropertyName = "options")]
        public List<DataSourceOption> Options { get; set; }

        /// <summary>
        /// List of paths where the datasource can be requested
        /// </summary>
        [JsonProperty(PropertyName = "paths")]
        public List<DataSourcePath> Paths { get; set; }

        /// <summary>
        /// Dictionary with the requirements for requesting this datasource
        /// </summary>
        [JsonProperty(PropertyName = "requirements")]
        public Dictionary<string, string> Requirements { get; set; }
    }

    /// <summary>
    /// Class containing available options for requesting certain datasource
    /// </summary>
    public class DataSourceOption
    {
        /// <summary>
        /// Type of the option, i.e. 'select', 'text'
        /// </summary>
        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }

        /// <summary>
        /// ID of the option
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        /// <summary>
        /// Label of the option
        /// </summary>
        [JsonProperty(PropertyName = "label")]
        public string Label { get; set; }

        /// <summary>
        /// Default value of this option
        /// </summary>
        [JsonProperty(PropertyName = "default")]
        public string Default { get; set; }

        /// <summary>
        /// Short description of the usage of this option
        /// </summary>
        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }

        /// <summary>
        /// Available choices for this option
        /// </summary>
        [JsonProperty(PropertyName = "choices")]
        public Dictionary<string, string> Choices { get; set; }

        /// <summary>
        /// Indicates if this option allows multiple values
        /// </summary>
        [JsonProperty(PropertyName = "multiple")]
        public bool Multiple { get; set; }

        /// <summary>
        /// ?
        /// </summary>
        [JsonProperty(PropertyName = "transform")]
        public string Transform { get; set; }
    }

    /// <summary>
    /// Class containing basic information of a datasource path
    /// </summary>
    public class DataSourcePath
    {
        /// <summary>
        /// Available conditions that could be used when requesting data from this datasource
        /// </summary>
        [JsonProperty(PropertyName = "condition")]
        public DataSourcePathCondition Condition { get; set; }

        /// <summary>
        /// Available path templates for requesting data from this datasource
        /// </summary>
        [JsonProperty(PropertyName = "templates")]
        public DataSourcePathTemplates Templates { get; set; }
    }

    /// <summary>
    /// Class containing basic information about the conditions of a datasource path (?)
    /// </summary>
    public class DataSourcePathCondition
    {
        /// <summary>
        /// Type of the condition
        /// </summary>
        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }

        /// <summary>
        /// Name of the option in which this condition will be applied (?)
        /// </summary>
        [JsonProperty(PropertyName = "option")]
        public string Option { get; set; }

        /// <summary>
        /// List of valid values for this condition
        /// </summary>
        [JsonProperty(PropertyName = "values")]
        public List<string> Values { get; set; }
    }

    /// <summary>
    /// Class containing information about datasource path templates
    /// </summary>
    public class DataSourcePathTemplates
    {
        /// <summary>
        /// List of all posible path templates for requesting data from this datasource
        /// </summary>
        [JsonProperty(PropertyName = "all")]
        public List<string> All { get; set; }

        /// <summary>
        /// List of latest path templates for requesting data from this datasource (?)
        /// </summary>
        [JsonProperty(PropertyName = "latest")]
        public List<string> Latest { get; set; }
    }

    /// <summary>
    /// Prices entry for Data/Prices response
    /// </summary>
    public class PriceEntry
    {
        private Regex _regex;

        /// <summary>
        /// Vendor for this price
        /// </summary>
        [JsonProperty(PropertyName = "vendorName")]
        public string Vendor { get; set; }

        /// <summary>
        /// Regex for this data price entry
        /// Trims regex open, close, and multiline flag
        /// because it won't match otherwise
        /// </summary>
        public Regex RegEx
        {
            get
            {
                if (_regex == null && RawRegEx != null)
                {
                    _regex = new Regex(RawRegEx.TrimStart('/').TrimEnd('m').TrimEnd('/'), RegexOptions.Compiled);
                }
                return _regex;
            }
        }

        /// <summary>
        /// RegEx directly from response
        /// </summary>
        [JsonProperty(PropertyName = "regex")]
        public string RawRegEx { get; set; }

        /// <summary>
        /// The price for this entry in QCC
        /// </summary>
        [JsonProperty(PropertyName = "price")]
        public int? Price { get; set; }

        /// <summary>
        /// The type associated to this price entry if any
        /// </summary>
        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }

        /// <summary>
        /// True if the user is subscribed
        /// </summary>
        [JsonProperty(PropertyName = "subscribed")]
        public bool? Subscribed { get; set; }

        /// <summary>
        /// The associated product id
        /// </summary>
        [JsonProperty(PropertyName = "productId")]
        public int ProductId { get; set; }

        /// <summary>
        /// The associated data paths
        /// </summary>
        [JsonProperty(PropertyName = "paths")]
        public HashSet<string> Paths { get; set; }
    }
}
