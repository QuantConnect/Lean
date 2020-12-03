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

namespace QuantConnect.Optimizer
{
    public class OptimizationEstimate
    {
        /// <summary>
        /// Total number of backtests, approximately
        /// </summary>
        [JsonProperty("totalBacktest")]
        public int TotalBacktest { get; set; }

        /// <summary>
        /// Number of completed backtests
        /// </summary>
        [JsonProperty("completedBacktest")]
        public int CompletedBacktest { get; set; }

        /// <summary>
        /// Number of failed backtests
        /// </summary>
        [JsonProperty("failedBacktest")]
        public int FailedBacktest { get; set; }

        /// <summary>
        /// Number of running backtests
        /// </summary>
        [JsonProperty("runningBacktest")]
        public int RunningBacktest { get; set; }

        /// <summary>
        /// Number of backtests in queue
        /// </summary>
        [JsonProperty("inQueueBacktest")]
        public int InQueueBacktest { get; set; }

        /// <summary>
        /// Indicates backtest average duration; (start - now) / CompletedBacktest
        /// </summary>
        [JsonProperty("averageBacktest")]
        public TimeSpan AverageBacktest { get; set; }

        /// <summary>
        /// The run time of this optimization
        /// </summary>
        [JsonProperty(PropertyName = "totalRuntime")]
        public TimeSpan TotalRuntime { get; set; }

        /// <summary>
        /// Pretty representation of an optimization estimate
        /// </summary>
        public override string ToString()
        {
            return $"TotalBacktest: {TotalBacktest}. CompletedBacktest: {CompletedBacktest}. FailedBacktest: {FailedBacktest}." +
                $" RunningBacktest: {RunningBacktest}. InQueueBacktest: {InQueueBacktest}. TotalRuntime {TotalRuntime}. AverageBacktest: {AverageBacktest}";
        }
    }
}
