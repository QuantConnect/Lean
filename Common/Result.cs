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

using System;
using Newtonsoft.Json;
using QuantConnect.Orders;
using QuantConnect.Packets;
using System.Collections.Generic;

namespace QuantConnect
{
    /// <summary>
    /// Base class for backtesting and live results that packages result data.
    /// <see cref="LiveResult"/>
    /// <see cref="BacktestResult"/>
    /// </summary>
    public class Result
    {
        /// <summary>
        /// Charts updates for the live algorithm since the last result packet
        /// </summary>
        [JsonProperty(PropertyName = "Charts", NullValueHandling = NullValueHandling.Ignore)]
        public IDictionary<string, Chart> Charts;

        /// <summary>
        /// Order updates since the last result packet
        /// </summary>
        [JsonProperty(PropertyName = "Orders", NullValueHandling = NullValueHandling.Ignore)]
        public IDictionary<int, Order> Orders;

        /// <summary>
        /// OrderEvent updates since the last result packet
        /// </summary>
        [JsonProperty(PropertyName = "OrderEvents", NullValueHandling = NullValueHandling.Ignore)]
        public List<OrderEvent> OrderEvents;

        /// <summary>
        /// Trade profit and loss information since the last algorithm result packet
        /// </summary>
        [JsonProperty(PropertyName = "ProfitLoss", NullValueHandling = NullValueHandling.Ignore)]
        public IDictionary<DateTime, decimal> ProfitLoss;

        /// <summary>
        /// Statistics information sent during the algorithm operations.
        /// </summary>
        /// <remarks>Intended for update mode -- send updates to the existing statistics in the result GUI. If statistic key does not exist in GUI, create it</remarks>
        [JsonProperty(PropertyName = "Statistics", NullValueHandling = NullValueHandling.Ignore)]
        public IDictionary<string, string> Statistics;

        /// <summary>
        /// Runtime banner/updating statistics in the title banner of the live algorithm GUI.
        /// </summary>
        [JsonProperty(PropertyName = "RuntimeStatistics", NullValueHandling = NullValueHandling.Ignore)]
        public IDictionary<string, string> RuntimeStatistics;

        /// <summary>
        /// State of the result packet.
        /// </summary>
        [JsonProperty(PropertyName = "State", NullValueHandling = NullValueHandling.Ignore)]
        public IDictionary<string, string> State;

        /// <summary>
        /// Server status information, including CPU/RAM usage, ect...
        /// </summary>
        [JsonProperty(PropertyName = "ServerStatistics", NullValueHandling = NullValueHandling.Ignore)]
        public IDictionary<string, string> ServerStatistics;

        /// <summary>
        /// The algorithm's configuration required for report generation
        /// </summary>
        [JsonProperty(PropertyName = "AlgorithmConfiguration", NullValueHandling = NullValueHandling.Ignore)]
        public AlgorithmConfiguration AlgorithmConfiguration;
    }
}
