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

namespace QuantConnect.Data.Custom.SEC
{
    public class SECReportBusinessAddress
    {
        /// <summary>
        /// Street Address 1
        /// </summary>
        [JsonProperty("STREET1")]
        public string StreetOne;

        /// <summary>
        /// Street Address 2
        /// </summary>
        [JsonProperty("STREET2")]
        public string StreetTwo;

        /// <summary>
        /// City
        /// </summary>
        [JsonProperty("CITY")]
        public string City;

        /// <summary>
        /// US State
        /// </summary>
        [JsonProperty("STATE")]
        public string State;

        /// <summary>
        /// ZIP Code
        /// </summary>
        /// <remarks>
        /// Not as integer because of special ZIP codes and potential dashes
        /// </remarks>
        [JsonProperty("ZIP")]
        public string Zip;

        /// <summary>
        /// Business phone number
        /// </summary>
        [JsonProperty("PHONE")]
        public string Phone;
    }
}