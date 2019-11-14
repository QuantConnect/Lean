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
using QuantConnect.Securities;

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
                Log.Trace($"LiveResultPacket(): Error converting json: {err}");
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
    public class LiveResult : Result
    {
        /// <summary>
        /// Holdings dictionary of algorithm holdings information
        /// </summary>
        [JsonProperty(PropertyName = "Holdings", NullValueHandling = NullValueHandling.Ignore)]
        public IDictionary<string, Holding> Holdings;

        /// <summary>
        /// Cashbook for the algorithm's live results.
        /// </summary>
        [JsonProperty(PropertyName = "Cash", NullValueHandling = NullValueHandling.Ignore)]
        public CashBook Cash;

        /// <summary>
        /// Server status information, including CPU/RAM usage, ect...
        /// </summary>
        [JsonProperty(PropertyName = "ServerStatistics", NullValueHandling = NullValueHandling.Ignore)]
        public IDictionary<string, string> ServerStatistics;

        /// <summary>
        /// Default Constructor
        /// </summary>
        public LiveResult()
        { }

        /// <summary>
        /// Constructor for the result class for dictionary objects
        /// </summary>
        public LiveResult(IDictionary<string, Chart> charts, IDictionary<int, Order> orders, IDictionary<DateTime, decimal> profitLoss, IDictionary<string, Holding> holdings, CashBook cashbook, IDictionary<string, string> statistics, IDictionary<string, string> runtime, IDictionary<string, string> serverStatistics = null)
        {
            Charts = charts;
            Orders = orders;
            ProfitLoss = profitLoss;
            Statistics = statistics;
            Holdings = holdings;
            Cash = cashbook;
            RuntimeStatistics = runtime;
            ServerStatistics = serverStatistics ?? OS.GetServerStatistics();
        }
    }

} // End of Namespace:
