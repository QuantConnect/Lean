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

/**********************************************************
* USING NAMESPACES
**********************************************************/
using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace QuantConnect.Packets
{
    /********************************************************
    * CLASS DEFINITIONS
    *********************************************************/
    /// <summary>
    /// Live job task packet: container for any live specific job variables
    /// </summary>
    public class LiveNodePacket : AlgorithmNodePacket
    {
        /********************************************************
        * CLASS VARIABLES
        *********************************************************/
        /// <summary>
        /// Deploy Id for this live algorithm.
        /// </summary>
        [JsonProperty(PropertyName = "sDeployID")]
        public string DeployId = "";

        /// <summary>
        /// String name of the brokerage we're trading with
        /// </summary>
        [JsonProperty(PropertyName = "sBrokerage")]
        public string Brokerage = "";

        /// <summary>
        /// Access token for the broker login (oAuth 2.0)
        /// </summary>
        [JsonProperty(PropertyName = "sAccessToken")]
        public string AccessToken = "";

        /// <summary>
        /// Refresh token for brokerage login (oAuth 2.0)
        /// </summary>
        [JsonProperty(PropertyName = "sRefreshToken")]
        public string RefreshToken = "";

        /// <summary>
        /// DateTime the RefreshToken was issued (oAuth 2.0)
        /// </summary>
        [JsonProperty(PropertyName = "dtIssuedAt")]
        public DateTime IssuedAt = new DateTime();

        /// <summary>
        /// Life span of the issued access token (oAuth 2.0)
        /// </summary>
        [JsonProperty(PropertyName = "iLifeTime")]
        public TimeSpan LifeTime = TimeSpan.FromSeconds(0);

        /// <summary>
        /// String-String Dictionary of Brokerage Data for this Live Job
        /// </summary>
        [JsonProperty(PropertyName = "aBrokerageData")]
        public Dictionary<string, string> BrokerageData = new Dictionary<string, string>();

        /// <summary>
        /// Account Id for specified brokerage
        /// </summary>
        [JsonProperty(PropertyName = "sAccountID")]
        public string AccountId = "";

        /********************************************************
        * CLASS CONSTRUCTOR
        *********************************************************/
        /// <summary>
        /// Default constructor for JSON of the Live Task Packet
        /// </summary>
        public LiveNodePacket()
            : base(PacketType.LiveNode)
        { }

    } // End Work Packet:

} // End of Namespace:
