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
using System.Linq;
using Newtonsoft.Json;
using QuantConnect.Orders;
using QuantConnect.Logging;
using QuantConnect.Securities;
using System.Collections.Generic;

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
        public int UserId { get; set; }

        /// <summary>
        /// Project Id of the result packet
        /// </summary>
        public int ProjectId { get; set; }

        /// <summary>
        /// Live Algorithm Id (DeployId) for this result packet
        /// </summary>
        public string DeployId { get; set; } = string.Empty;

        /// <summary>
        /// Result data object for this result packet
        /// </summary>
        public LiveResult Results { get; set; } = new LiveResult();

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
                Channel            = packet.Channel;
                DeployId           = packet.DeployId;
                Type               = packet.Type;
                UserId             = packet.UserId;
                ProjectId          = packet.ProjectId;
                Results            = packet.Results;
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
                DeployId = job.DeployId;
                Results = results;
                UserId = job.UserId;
                ProjectId = job.ProjectId;
                Channel = job.Channel;
            }
            catch (Exception err) {
                Log.Error(err);
            }
        }

        /// <summary>
        /// Creates an empty result packet, useful when the algorithm fails to initialize
        /// </summary>
        /// <param name="job">The associated job packet</param>
        /// <returns>An empty result packet</returns>
        public static LiveResultPacket CreateEmpty(LiveNodePacket job)
        {
            return new LiveResultPacket(job, new LiveResult(new LiveResultParameters(
                new Dictionary<string, Chart>(), new Dictionary<int, Order>(), new Dictionary<DateTime, decimal>(),
                new Dictionary<string, Holding>(), new CashBook(), new Dictionary<string, string>(),
                new SortedDictionary<string, string>(), new List<OrderEvent>(), new Dictionary<string, string>(),
                new AlgorithmConfiguration(), new Dictionary<string, string>())));
        }
    } // End Queue Packet:


    /// <summary>
    /// Live results object class for packaging live result data.
    /// </summary>
    public class LiveResult : Result
    {
        private CashBook _cashBook;

        /// <summary>
        /// Holdings dictionary of algorithm holdings information
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public IDictionary<string, Holding> Holdings { get; set; }

        /// <summary>
        /// Cashbook for the algorithm's live results.
        /// </summary>
        [JsonIgnore]
        public CashBook CashBook
        {
            get
            {
                return _cashBook;
            }
            set
            {
                _cashBook = value;

                Cash = _cashBook?.ToDictionary(pair => pair.Key, pair => pair.Value);
                AccountCurrency = CashBook?.AccountCurrency;
                AccountCurrencySymbol = AccountCurrency != null ? Currencies.GetCurrencySymbol(AccountCurrency) : null;
            }
        }

        /// <summary>
        /// Cash for the algorithm's live results.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, Cash> Cash { get; set; }

        /// <summary>
        /// The algorithm's account currency
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string AccountCurrency { get; set; }

        /// <summary>
        /// The algorithm's account currency
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string AccountCurrencySymbol { get; set; }

        /// <summary>
        /// Default Constructor
        /// </summary>
        public LiveResult()
        { }

        /// <summary>
        /// Constructor for the result class for dictionary objects
        /// </summary>
        public LiveResult(LiveResultParameters parameters) : base(parameters)
        {
            Holdings = parameters.Holdings;
            CashBook = parameters.CashBook;
        }
    }
} // End of Namespace:
