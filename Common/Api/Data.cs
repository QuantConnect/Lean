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
        public string Link { get; set; }

        /// <summary>
        /// Remaining QCC balance on account after this transaction
        /// </summary>
        public double Balance { get; set; }

        /// <summary>
        /// QCC Cost for this data link
        /// </summary>
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
        /// Collection of prices objects
        /// </summary>
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
        public int? Price { get; set; }

        /// <summary>
        /// The type associated to this price entry if any
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// True if the user is subscribed
        /// </summary>
        public bool? Subscribed { get; set; }

        /// <summary>
        /// The associated product id
        /// </summary>
        public int ProductId { get; set; }

        /// <summary>
        /// The associated data paths
        /// </summary>
        public HashSet<string> Paths { get; set; }
    }
}
