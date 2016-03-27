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

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using QuantConnect.Logging;
using QuantConnect.Orders;

namespace QuantConnect.Packets
{
    /// <summary>
    /// Live result packet from a lean engine algorithm.
    /// </summary>
    public class LiveResultPacket : Packet 
    {
        /// <summary>
        /// User Id sending result packet
        /// </summary>
        [JsonProperty(PropertyName = "iUserID")]
        public int UserId = 0;

        /// <summary>
        /// Project Id of the result packet
        /// </summary>
        [JsonProperty(PropertyName = "iProjectID")]
        public int ProjectId = 0;

        /// <summary>
        /// User session Id who issued the result packet
        /// </summary>
        [JsonProperty(PropertyName = "sSessionID")]
        public string SessionId = "";

        /// <summary>
        /// Live Algorithm Id (DeployId) for this result packet
        /// </summary>
        [JsonProperty(PropertyName = "sDeployID")]
        public string DeployId = "";

        /// <summary>
        /// Compile Id algorithm which generated this result packet
        /// </summary>
        [JsonProperty(PropertyName = "sCompileID")]
        public string CompileId = "";

        /// <summary>
        /// Result data object for this result packet
        /// </summary>
        [JsonProperty(PropertyName = "oResults")]
        public LiveResult Results = new LiveResult();

        /// <summary>
        /// Processing time / running time for the live algorithm.
        /// </summary>
        [JsonProperty(PropertyName = "dProcessingTime")]
        public double ProcessingTime = 0;

        /// <summary>
        /// Default constructor for JSON Serialization
        /// </summary>
        public LiveResultPacket()
            : base(PacketType.LiveResult)
        { }
        
        /// <summary>
        /// Compose the packet from a JSON string:
        /// </summary>
        public LiveResultPacket(string json)
            : base(PacketType.LiveResult)
        {
            try
            {
                var packet = JsonConvert.DeserializeObject<LiveResultPacket>(json);
                CompileId          = packet.CompileId;
                Channel            = packet.Channel;
                SessionId          = packet.SessionId;
                DeployId           = packet.DeployId;
                Type               = packet.Type;
                UserId             = packet.UserId;
                ProjectId          = packet.ProjectId;
                Results            = packet.Results;
                ProcessingTime     = packet.ProcessingTime;
            } 
            catch (Exception err)
            {
                Log.Trace("LiveResultPacket(): Error converting json: " + err);
            }
        }

        /// <summary>
        /// Compose Live Result Data Packet - With tradable dates
        /// </summary>
        /// <param name="job">Job that started this request</param>
        /// <param name="results">Results class for the Backtest job</param>
        public LiveResultPacket(LiveNodePacket job, LiveResult results) 
            :base (PacketType.LiveResult)
        {
            try
            {
                SessionId = job.SessionId;
                CompileId = job.CompileId;
                DeployId = job.DeployId;
                Results = results;
                UserId = job.UserId;
                ProjectId = job.ProjectId;
                SessionId = job.SessionId;
                Channel = job.Channel;
            }
            catch (Exception err) {
                Log.Error(err);
            }
        }
    } // End Queue Packet:


    /// <summary>
    /// Live results object class for packaging live result data.
    /// </summary>
    public class LiveResult
    {
        /// <summary>
        /// Charts updates for the live algorithm since the last result packet
        /// </summary>
        public Dictionary<string, Chart> Charts = new Dictionary<string, Chart>();

        /// <summary>
        /// Holdings dictionary of algorithm holdings information
        /// </summary>
        public Dictionary<string, Holding> Holdings = new Dictionary<string, Holding>();
        
        /// <summary>
        /// Order updates since the last result packet
        /// </summary>
        public Dictionary<int, Order> Orders = new Dictionary<int, Order>();
        
        /// <summary>
        /// Trade profit and loss information since the last algorithm result packet
        /// </summary>
        public Dictionary<DateTime, decimal> ProfitLoss = new Dictionary<DateTime, decimal>();

        /// <summary>
        /// Statistics information sent during the algorithm operations.
        /// </summary>
        /// <remarks>Intended for update mode -- send updates to the existing statistics in the result GUI. If statistic key does not exist in GUI, create it</remarks>
        public Dictionary<string, string> Statistics = new Dictionary<string, string>();

        /// <summary>
        /// Runtime banner/updating statistics in the title banner of the live algorithm GUI.
        /// </summary>
        public Dictionary<string, string> RuntimeStatistics = new Dictionary<string, string>();

        /// <summary>
        /// Server status information, including CPU/RAM usage, ect...
        /// </summary>
        public Dictionary<string, string> ServerStatistics = new Dictionary<string, string>();

        /// <summary>
        /// Default Constructor
        /// </summary>
        public LiveResult() 
        { }

        /// <summary>
        /// Constructor for the result class for dictionary objects
        /// </summary>
        public LiveResult(Dictionary<string, Chart> charts, Dictionary<int, Order> orders, Dictionary<DateTime, decimal> profitLoss, Dictionary<string, Holding> holdings, Dictionary<string, string> statistics, Dictionary<string, string> runtime, Dictionary<string, string> serverStatistics = null)
        {
            Charts = charts;
            Orders = orders;
            ProfitLoss = profitLoss;
            Statistics = statistics;
            Holdings = holdings;
            RuntimeStatistics = runtime;
            ServerStatistics = serverStatistics ?? OS.GetServerStatistics();
        } 
    }

} // End of Namespace:
