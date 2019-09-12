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

using System.Collections.Generic;
using Newtonsoft.Json;

namespace QuantConnect.Packets
{
    /// <summary>
    /// Security types packet contains information on the markets the user data has requested.
    /// </summary>
    public class SecurityTypesPacket : Packet
    {
        /// <summary>
        /// List of Security Type the user has requested (Equity, Forex, Futures etc).
        /// </summary>
        [JsonProperty(PropertyName = "aMarkets")]
        public List<SecurityType> Types = new List<SecurityType>();

        /// <summary>
        /// CSV formatted, lower case list of SecurityTypes for the web API.
        /// </summary>
        public string TypesCSV
        {
            get
            {
                var result = "";
                foreach (var type in Types)
                {
                    result += type + ",";
                }
                result = result.TrimEnd(',');
                return result.ToLowerInvariant();
            }
        }

        /// <summary>
        /// Default constructor for JSON
        /// </summary>
        public SecurityTypesPacket()
            : base (PacketType.SecurityTypes)
        { }

    } // End Work Packet:

} // End of Namespace:
