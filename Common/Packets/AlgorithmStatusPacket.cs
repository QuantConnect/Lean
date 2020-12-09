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
using Newtonsoft.Json.Converters;

namespace QuantConnect.Packets
{
    /// <summary>
    /// Algorithm status update information packet
    /// </summary>
    public class AlgorithmStatusPacket : Packet
    {
        /// <summary>
        /// Current algorithm status
        /// </summary>
        [JsonProperty(PropertyName = "eStatus")]
        [JsonConverter(typeof(StringEnumConverter))]
        public AlgorithmStatus Status;

        /// <summary>
        /// Chart we're subscribed to for live trading.
        /// </summary>
        [JsonProperty(PropertyName = "sChartSubscription")]
        public string ChartSubscription;

        /// <summary>
        /// Optional message or reason for state change.
        /// </summary>
        [JsonProperty(PropertyName = "sMessage")]
        public string Message;

        /// <summary>
        /// Algorithm Id associated with this status packet
        /// </summary>
        [JsonProperty(PropertyName = "sAlgorithmID")]
        public string AlgorithmId;

        /// <summary>
        /// OptimizationId for this result packet if any
        /// </summary>
        [JsonProperty(PropertyName = "sOptimizationID")]
        public string OptimizationId;

        /// <summary>
        /// Project Id associated with this status packet
        /// </summary>
        [JsonProperty(PropertyName = "iProjectID")]
        public int ProjectId;

        /// <summary>
        /// The current state of the channel
        /// </summary>
        [JsonProperty(PropertyName = "sChannelStatus")]
        public string ChannelStatus;

        /// <summary>
        /// Default constructor for JSON
        /// </summary>
        public AlgorithmStatusPacket()
            : base(PacketType.AlgorithmStatus)
        {
        }

        /// <summary>
        /// Initialize algorithm state packet:
        /// </summary>
        public AlgorithmStatusPacket(string algorithmId, int projectId, AlgorithmStatus status, string message = "")
            : base (PacketType.AlgorithmStatus)
        {
            Status = status;
            ProjectId = projectId;
            AlgorithmId = algorithmId;
            Message = message;
        }   
    }
}
