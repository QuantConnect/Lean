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
using System.Collections.Generic;
using QuantConnect.Notifications;

namespace QuantConnect.Packets
{
    /// <summary>
    /// Live job task packet: container for any live specific job variables
    /// </summary>
    public class LiveNodePacket : AlgorithmNodePacket
    {
        /// <summary>
        /// Deploy Id for this live algorithm.
        /// </summary>
        public string DeployId = "";

        /// <summary>
        /// String name of the brokerage we're trading with
        /// </summary>
        public string Brokerage = "";

        /// <summary>
        /// String-String Dictionary of Brokerage Data for this Live Job
        /// </summary>
        public Dictionary<string, string> BrokerageData = new Dictionary<string, string>();

        /// <summary>
        /// String name of the DataQueueHandler or LiveDataProvider we're running with
        /// </summary>
        public string DataQueueHandler = "";

        /// <summary>
        /// String name of the DataChannelProvider we're running with
        /// </summary>
        public string DataChannelProvider = "";

        /// <summary>
        /// Gets flag indicating whether or not the message should be acknowledged and removed from the queue
        /// </summary>
        public bool DisableAcknowledgement;

        /// <summary>
        /// A list of event types to generate notifications for, which will use <see cref="NotificationTargets"/>
        /// </summary>
        public HashSet<string> NotificationEvents;

        /// <summary>
        /// A list of notification targets to use
        /// </summary>
        public List<Notification> NotificationTargets;

        /// <summary>
        /// List of real time data types available in the live trading environment
        /// </summary>
        public HashSet<string> LiveDataTypes;

        /// <summary>
        /// Algorithm running mode.
        /// </summary>
        [JsonIgnore]
        public override AlgorithmMode AlgorithmMode
        {
            get
            {
                return AlgorithmMode.Live;
            }
        }

        /// <summary>
        /// Default constructor for JSON of the Live Task Packet
        /// </summary>
        public LiveNodePacket()
            : base(PacketType.LiveNode)
        {
            Controls = new Controls
            {
                MinuteLimit = 100,
                SecondLimit = 50,
                TickLimit = 25,
                RamAllocation = 512
            };
        }
    }
}
