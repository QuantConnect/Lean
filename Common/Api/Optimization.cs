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

namespace QuantConnect.Api
{
    /// <summary>
    /// Optimization response packet from the QuantConnect.com API.
    /// </summary>
    public class Optimization : BaseOptimization
    {
        /// <summary>
        /// Snapshot ID of this optimization
        /// </summary>
        public int? SnapshotId { get; set; }

        /// <summary>
        /// Statistic to be optimized
        /// </summary>
        public string OptimizationTarget { get; set; }

        /// <summary>
        /// List with grid charts representing the grid layout
        /// </summary>
        public List<GridChart> GridLayout { get; set; }

        /// <summary>
        /// Runtime banner/updating statistics for the optimization
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public IDictionary<string, string> RuntimeStatistics { get; set; }

        /// <summary>
        /// Optimization constraints
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public IReadOnlyList<Constraint> Constraints { get; set; }

        /// <summary>
        /// Number of parallel nodes for optimization
        /// </summary>
        public int ParallelNodes { get; set; }

        /// <summary>
        /// Optimization constraints
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public IDictionary<string, OptimizationBacktest> Backtests { get; set; }

        /// <summary>
        /// Optimization strategy
        /// </summary>
        public string Strategy { get; set; }
        
        /// <summary>
        /// Optimization requested date and time
        /// </summary>
        public DateTime Requested { get; set; }
    }

    /// <summary>
    /// Wrapper class for Optimizations/Read endpoint JSON response
    /// </summary>
    public class OptimizationResponseWrapper : RestResponse
    {
        /// <summary>
        /// Optimization object
        /// </summary>
        public Optimization Optimization { get; set; }
    }

    /// <summary>
    /// Collection container for a list of summarized optimizations for a project
    /// </summary>
    public class OptimizationList : RestResponse
    {
        /// <summary>
        /// Collection of summarized optimization objects
        /// </summary>
        public List<OptimizationSummary> Optimizations { get; set; }
    }
}
