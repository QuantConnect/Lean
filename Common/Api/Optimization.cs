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

namespace QuantConnect.Api
{
    /// <summary>
    /// Optimization response packet from the QuantConnect.com API.
    /// </summary>
    public class Optimization : BaseOptimization
    {
        /// <summary>
        /// Runtime banner/updating statistics for the optimization
        /// </summary>
        [JsonProperty(PropertyName = "runtimeStatistics", NullValueHandling = NullValueHandling.Ignore)]
        public IDictionary<string, string> RuntimeStatistics;

        /// <summary>
        /// Optimization constraints
        /// </summary>
        [JsonProperty(PropertyName = "constraints", NullValueHandling = NullValueHandling.Ignore)]
        IReadOnlyList<Constraint> Constraints;

        /// <summary>
        /// Optimization parameters
        /// </summary>
        [JsonProperty(PropertyName = "parameters", NullValueHandling = NullValueHandling.Ignore)]
        public HashSet<OptimizationParameter> Parameters;

        /// <summary>
        /// Number of parallel nodes for optimization
        /// </summary>
        [JsonProperty(PropertyName = "parallelNodes")]
        public int ParallelNodes;

        /// <summary>
        /// Optimization constraints
        /// </summary>
        [JsonProperty(PropertyName = "backtests", NullValueHandling = NullValueHandling.Ignore)]
        IDictionary<string, Backtest> Backtests;

        /// <summary>
        /// Optimization strategy
        /// </summary>
        [JsonProperty(PropertyName = "strategy")]
        public string Strategy;

        /// <summary>
        /// Optimization target
        /// </summary>
        [JsonProperty(PropertyName = "optimizationTarget")]
        public string Target;

        /// <summary>
        /// Optimization target value
        /// </summary>
        [JsonProperty(PropertyName = "targetValue")]
        public decimal? TargetValue;

        /// <summary>
        /// Optimization target extremum
        /// </summary>
        [JsonProperty(PropertyName = "extremum")]
        public Extremum Extremum;

        /// <summary>
        /// Optimization requested date and time
        /// </summary>
        [JsonProperty(PropertyName = "requested")]
        public DateTime Requested;

        /// <summary>
        /// Estimated cost for optimization
        /// </summary>
        [JsonProperty(PropertyName = "estimatedCost")]
        public decimal EstimatedCost;

        /// <summary>
        /// Optimization compile ID
        /// </summary>
        [JsonProperty(PropertyName = "compileId")]
        public string CompileId;
    }

    /// <summary>
    /// Wrapper class for Optimizations/* endpoints JSON response
    /// Currently used by Optimizations/Read, Optimizations/Create and Optimizations/Estimate
    /// </summary>
    public class OptimizationResponseWrapper : RestResponse
    {
        /// <summary>
        /// Optimization object
        /// </summary>
        [JsonProperty(PropertyName = "optimization")]
        public Optimization Optimization;
    }

    /// <summary>
    /// Collection container for a list of summarized optimizations for a project
    /// </summary>
    public class OptimizationList : RestResponse
    {
        /// <summary>
        /// Collection of summarized optimization objects
        /// </summary>
        [JsonProperty(PropertyName = "optimizations")]
        public List<BaseOptimization> Optimizations;
    }
}
