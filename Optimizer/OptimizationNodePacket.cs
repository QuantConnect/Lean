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

using System.Collections.Generic;
using Newtonsoft.Json;
using QuantConnect.Optimizer.Objectives;
using QuantConnect.Optimizer.Parameters;
using QuantConnect.Optimizer.Strategies;
using QuantConnect.Packets;

namespace QuantConnect.Optimizer
{
    /// <summary>
    /// Provide a packet type containing information on the optimization compute job.
    /// </summary>
    public class OptimizationNodePacket : Packet
    {
        /// <summary>
        /// User Id placing request
        /// </summary>
        [JsonProperty(PropertyName = "userId")]
        public int UserId;

        /// User API Token
        [JsonProperty(PropertyName = "userToken")]
        public string UserToken = "";

        /// <summary>
        /// Project Id of the request
        /// </summary>
        [JsonProperty(PropertyName = "projectId")]
        public int ProjectId;

        /// <summary>
        /// Unique compile id of this optimization
        /// </summary>
        [JsonProperty(PropertyName = "compileId")]
        public string CompileId = "";

        /// <summary>
        /// The unique optimization Id of the request
        /// </summary>
        [JsonProperty(PropertyName = "optimizationId")]
        public string OptimizationId = "";

        /// <summary>
        /// Organization Id of the request
        /// </summary>
        [JsonProperty(PropertyName = "organizationId")]
        public string OrganizationId = "";

        /// <summary>
        /// Limit for the amount of concurrent backtests being run
        /// </summary>
        [JsonProperty(PropertyName = "maximumConcurrentBacktests")]
        public int MaximumConcurrentBacktests;

        /// <summary>
        /// Optimization strategy name
        /// </summary>
        [JsonProperty(PropertyName = "optimizationStrategy")]
        public string OptimizationStrategy = "QuantConnect.Optimizer.Strategies.GridSearchOptimizationStrategy";

        /// <summary>
        /// Objective settings
        /// </summary>
        [JsonProperty(PropertyName = "criterion")]
        public Target Criterion;

        /// <summary>
        /// Optimization constraints
        /// </summary>
        [JsonProperty(PropertyName = "constraints")]
        public IReadOnlyList<Constraint> Constraints;

        /// <summary>
        /// The user optimization parameters
        /// </summary>
        [JsonProperty(PropertyName = "optimizationParameters")]
        public HashSet<OptimizationParameter> OptimizationParameters;

        /// <summary>
        /// The user optimization parameters
        /// </summary>
        [JsonProperty(PropertyName = "optimizationStrategySettings", TypeNameHandling = TypeNameHandling.All)]
        public OptimizationStrategySettings OptimizationStrategySettings;

        /// <summary>
        /// Creates a new instance
        /// </summary>
        public OptimizationNodePacket() : this(PacketType.OptimizationNode)
        {
        }

        /// <summary>
        /// Creates a new instance
        /// </summary>
        protected OptimizationNodePacket(PacketType packetType) : base(packetType)
        {
        }
    }

    /// <summary>
    /// The different optimization status
    /// </summary>
    public enum OptimizationStatus
    {
        /// <summary>
        /// Just created and not running optimization
        /// </summary>
        New,

        /// <summary>
        /// We failed or we were aborted
        /// </summary>
        Aborted,

        /// <summary>
        /// We are running
        /// </summary>
        Running,

        /// <summary>
        /// Optimization job has completed
        /// </summary>
        Completed
    }
}
