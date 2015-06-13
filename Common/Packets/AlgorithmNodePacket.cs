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

namespace QuantConnect.Packets
{
    /// <summary>
    /// Algorithm Node Packet is a work task for the Lean Engine
    /// </summary>
    public class AlgorithmNodePacket : Packet
    {
        /// <summary>
        /// Default constructor for the algorithm node:
        /// </summary>
        /// <param name="type"></param>
        public AlgorithmNodePacket(PacketType type)
            : base(type)
        { }

        /// <summary>
        /// User Id placing request
        /// </summary>
        [JsonProperty(PropertyName = "iUserID")]
        public int UserId = 0;

        /// <summary>
        /// Project Id of the request
        /// </summary>
        [JsonProperty(PropertyName = "iProjectID")]
        public int ProjectId = 0;

        /// <summary>
        /// Algorithm Id - BacktestId or DeployId - Common Id property between packets.
        /// </summary>
        [JsonProperty(PropertyName = "sAlgorithmID")]
        public string AlgorithmId
        {
            get
            {
                if (Type == PacketType.LiveNode)
                {
                    return ((LiveNodePacket)this).DeployId;
                }
                return ((BacktestNodePacket)this).BacktestId;
            }
        }

        /// <summary>
        /// User session Id for authentication
        /// </summary>
        [JsonProperty(PropertyName = "sSessionID")]
        public string SessionId = "";

        /// <summary>
        /// User subscriptions state - free or paid.
        /// </summary>
        [JsonProperty(PropertyName = "sUserPlan")]
        public UserPlan UserPlan = UserPlan.Free;

        /// <summary>
        /// Server type for the deployment (512, 1024, 2048)
        /// </summary>
        [JsonProperty(PropertyName = "sServerType")]
        public ServerType ServerType = ServerType.Server512;

        /// <summary>
        /// Unique compile id of this backtest
        /// </summary>
        [JsonProperty(PropertyName = "sCompileID")]
        public string CompileId = "";

        /// <summary>
        /// Version number identifier for the lean engine.
        /// </summary>
        [JsonProperty(PropertyName = "sVersion")]
        public string Version;

        /// <summary>
        /// An algorithm packet which has already been run and is being redelivered on this node.
        /// In this event we don't want to relaunch the task as it may result in unexpected behaviour for user.
        /// </summary>
        [JsonProperty(PropertyName = "bRedelivered")]
        public bool Redelivered = false;

        /// <summary>
        /// Algorithm binary with zip of contents
        /// </summary>
        [JsonProperty(PropertyName = "oAlgorithm")]
        public byte[] Algorithm = new byte[] { };

        /// <summary>
        /// Request source - Web IDE or API - for controling result handler behaviour
        /// </summary>
        [JsonProperty(PropertyName = "sRequestSource")]
        public string RequestSource = "WebIDE";

        /// <summary>
        /// DataFeed plugin name to select for the task
        /// </summary>
        [JsonProperty(PropertyName = "eDataEndpoint")]
        public DataFeedEndpoint DataEndpoint = DataFeedEndpoint.Backtesting;

        /// <summary>
        /// Transaction handler plugin to select for task
        /// </summary>
        [JsonProperty(PropertyName = "eTransactionEndpoint")]
        public TransactionHandlerEndpoint TransactionEndpoint = TransactionHandlerEndpoint.Backtesting;

        /// <summary>
        /// Result endpoint plugin to select for task
        /// </summary>
        /// <remarks>
        ///     DEPRECATED: Maintained here for temporary consistency. Eventually all the endpoint enums will be replaced with MEF / Type import loading by config.
        /// </remarks>
        //[JsonProperty(PropertyName = "eResultEndpoint")]
        //public ResultHandlerEndpoint ResultEndpoint = ResultHandlerEndpoint.Backtesting;

        /// <summary>
        /// Setup handler endpoint for this task
        /// </summary>
        [JsonProperty(PropertyName = "eSetupEndpoint")]
        public SetupHandlerEndpoint SetupEndpoint = SetupHandlerEndpoint.Backtesting;

        /// <summary>
        /// Realtime events handler for this task
        /// </summary>
        [JsonProperty(PropertyName = "eRealTimeEndpoint")]
        public RealTimeEndpoint RealTimeEndpoint = RealTimeEndpoint.Backtesting;
    } // End Node Packet:

} // End of Namespace:
