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
        public int UserId { get; set; }

        /// User API Token
        public string UserToken { get; set; } = string.Empty;

        /// <summary>
        /// Project Id of the request
        /// </summary>
        public int ProjectId { get; set; }

        /// <summary>
        /// Unique compile id of this optimization
        /// </summary>
        public string CompileId { get; set; } = string.Empty;

        /// <summary>
        /// The unique optimization Id of the request
        /// </summary>
        public string OptimizationId { get; set; } = string.Empty;

        /// <summary>
        /// Organization Id of the request
        /// </summary>
        public string OrganizationId { get; set; } = string.Empty;

        /// <summary>
        /// Limit for the amount of concurrent backtests being run
        /// </summary>
        public int MaximumConcurrentBacktests { get; set; }

        /// <summary>
        /// Optimization strategy name
        /// </summary>
        public string OptimizationStrategy { get; set; } =
            "QuantConnect.Optimizer.Strategies.GridSearchOptimizationStrategy";

        /// <summary>
        /// Objective settings
        /// </summary>
        public Target Criterion { get; set; }

        /// <summary>
        /// Optimization constraints
        /// </summary>
        public IReadOnlyList<Constraint> Constraints { get; set; }

        /// <summary>
        /// The user optimization parameters
        /// </summary>
        public HashSet<OptimizationParameter> OptimizationParameters { get; set; }

        /// <summary>
        /// The user optimization parameters
        /// </summary>
        [JsonProperty(TypeNameHandling = TypeNameHandling.All)]
        public OptimizationStrategySettings OptimizationStrategySettings { get; set; }

        /// <summary>
        /// Backtest out of sample maximum end date
        /// </summary>
        public DateTime? OutOfSampleMaxEndDate { get; set; }

        /// <summary>
        /// The backtest out of sample day count
        /// </summary>
        public int OutOfSampleDays { get; set; }

        /// <summary>
        /// Creates a new instance
        /// </summary>
        public OptimizationNodePacket()
            : this(PacketType.OptimizationNode) { }

        /// <summary>
        /// Creates a new instance
        /// </summary>
        protected OptimizationNodePacket(PacketType packetType)
            : base(packetType) { }
    }
}
