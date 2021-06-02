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
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

// Collection of response objects for Quantconnect Data/ endpoints
namespace QuantConnect.Api
{
    /// <summary>
    /// Data/Read response wrapper, contains link to requested data
    /// </summary>
    public class DataLink : RestResponse
    {
        /// <summary>
        /// Link to the data requested
        /// </summary>
        [JsonProperty(PropertyName = "link")]
        public string Link { get; set; }

        /// <summary>
        /// Remaining QCC balance on account
        /// </summary>
        [JsonProperty(PropertyName = "balance")]
        public double Balance { get; set; }

        /// <summary>
        /// QCC Cost of the transaction for this link
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
        public List<DataEntry> AvailableData { get; set; }
    }

    /// <summary>
    /// Data entry for Data/List response
    /// </summary>
    public class DataEntry
    {
        /// <summary>
        /// Data Directory
        /// </summary>
        /// TODO: NEEDS JSON PROP NAME
        public string Data { get; set; }
    }

    /// <summary>
    /// Data/Prices response wrapper for prices by vendor
    /// </summary>
    public class DataPricesList : RestResponse
    {
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
        /// Get the price for a given data file
        /// </summary>
        /// <param name="path">Lean data path of the file</param>
        /// <returns>Price</returns>
        public int GetPrice(string path)
        {
            //TODO Handling no match case, try catch
            //TODO Regex deserialization is including the escape chars?? UGLY
            return Prices.First(x => Regex.IsMatch(path, Regex.Unescape(x.RegEx))).Price;
        }
    }

    /// <summary>
    /// Prices entry for Data/Prices response
    /// </summary>
    public class PriceEntry
    {
        /// <summary>
        /// Vendor for this price
        /// </summary>
        [JsonProperty(PropertyName = "vendorName")]
        public string Vendor { get; set; }

        /// <summary>
        /// The requested symbol ID
        /// </summary>
        [JsonProperty(PropertyName = "regex")]
        public string RegEx { get; set; }

        /// <summary>
        /// The requested price
        /// </summary>
        [JsonProperty(PropertyName = "price")]
        public int Price { get; set; }
    }
}
