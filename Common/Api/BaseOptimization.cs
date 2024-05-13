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
using QuantConnect.Optimizer;
using QuantConnect.Optimizer.Objectives;
using System.Collections.Generic;
using QuantConnect.Optimizer.Parameters;

namespace QuantConnect.Api
{
    /// <summary>
    /// BaseOptimization item from the QuantConnect.com API.
    /// </summary>
    public class BaseOptimization : RestResponse
    {
        /// <summary>
        /// Optimization ID
        /// </summary>
        public string OptimizationId { get; set; }

        /// <summary>
        /// Project ID of the project the optimization belongs to
        /// </summary>
        public int ProjectId { get; set; }

        /// <summary>
        /// Name of the optimization
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// <summary>
        /// Status of the optimization
        /// </summary>
        public OptimizationStatus Status { get; set; }

        /// <summary>
        /// Optimization node type
        /// </summary>
        /// <remarks><see cref="OptimizationNodes"/></remarks>
        public string NodeType { get; set; }

        /// <summary>
        /// Number of days of out of sample days
        /// </summary>
        public int OutOfSampleDays { get; set; }

        /// <summary>
        /// End date of out of sample data
        /// </summary>
        public DateTime OutOfSampleMaxEndDate { get; set; }

        /// <summary>
        /// Parameters used in this optimization
        /// </summary>
        public List<OptimizationParameter> Parameters { get; set; }

        /// <summary>
        /// Optimization statistical target
        /// </summary>
        public Target Criterion { get; set; }
    }

    /// <summary>
    /// Optimization summary response for creating an optimization
    /// </summary>
    public class OptimizationSummary: BaseOptimization
    {
        /// <summary>
        /// Date when this optimization was created
        /// </summary>
        public DateTime Created { get; set; }

        /// <summary>
        /// Price-sales ratio stastic
        /// </summary>
        public decimal? PSR { get; set; }

        /// <summary>
        /// Sharpe ratio statistic
        /// </summary>
        public decimal? SharpeRatio { get; set; }

        /// <summary>
        /// Number of trades
        /// </summary>
        public int? Trades { get; set; }

        /// <summary>
        /// ID of project, were this current project was originally cloned
        /// </summary>
        public int? CloneId { get; set; }
    }
}
