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
 *
*/

using Newtonsoft.Json;

namespace QuantConnect.Brokerages.Tradier
{
    /// <summary>
    /// Wrapper container for fault:
    /// </summary>
    public class TradierFaultContainer
    {
        /// Inner Fault Object
        [JsonProperty(PropertyName = "fault")]
        public TradierFault Fault;

        /// Fault Container Constructor:
        public TradierFaultContainer()
        { }
    }

    /// <summary>
    /// Tradier fault object:
    /// {"fault":{"faultstring":"Access Token expired","detail":{"errorcode":"keymanagement.service.access_token_expired"}}}
    /// </summary>
    public class TradierFault
    {
        /// Description of fault
        [JsonProperty(PropertyName = "faultstring")]
        public string Description = "";

        /// Detail object for fault exception
        [JsonProperty(PropertyName = "detail")]
        public TradierFaultDetail Details = new TradierFaultDetail();

        /// Tradier Fault Constructor:
        public TradierFault()
        { }
    }

    /// <summary>
    /// Error code associated with this fault.
    /// </summary>
    public class TradierFaultDetail
    {
        /// Error code for fault
        [JsonProperty(PropertyName = "errorcode")]
        public string ErrorCode;

        /// Tradier Detail Fault Constructor
        public TradierFaultDetail()
        { }
    }
}
