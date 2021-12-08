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

namespace QuantConnect.Api
{
    /// <summary>
    /// OptimizationDTO item from the QuantConnect.com API.
    /// </summary>
    public class OptimizationDTO
    {
        /// <summary>
        /// Name of the optimization
        /// </summary>
        [JsonProperty(PropertyName = "name")]
        public string Name;

        /// <summary>
        /// Creation time of the optimization
        /// </summary>
        [JsonProperty(PropertyName = "created")]
        public DateTime Created;
        
        /// <summary>
        /// Optimization ID
        /// </summary>
        [JsonProperty(PropertyName = "optimizationId")]
        public string OptimizationId;

        /// <summary>
        /// Status of the optimization
        /// </summary>
        [JsonProperty(PropertyName = "status")]
        public OptimizationStatus Status;

        /// <summary>
        /// Project ID of the project the optimization belongs to
        /// </summary>
        [JsonProperty(PropertyName = "projectId")]
        public int ProjectId;

        /// <summary>
        /// Optimization node type
        /// </summary>
        [JsonProperty(PropertyName = "nodeType")]
        public string NodeType;

        /// <summary>
        /// Probabilistic Sharpe Ratio of the optimization
        /// </summary>
        [JsonProperty(PropertyName = "psr")]
        public decimal PSR;

        /// <summary>
        /// Sharpe Ratio of the optimization
        /// </summary>
        [JsonProperty(PropertyName = "sharpeRatio")]
        public decimal SharpeRatio;

        /// <summary>
        /// Number of trades in the optimization
        /// </summary>
        [JsonProperty(PropertyName = "trades")]
        public int Trades;

        /// <summary>
        /// Optimization clone ID
        /// </summary>
        [JsonProperty(PropertyName = "cloneId")]
        public int CloneId;
    }
}
