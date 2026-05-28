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

using Newtonsoft.Json;
using System.Collections.Generic;

namespace QuantConnect.Optimizer
{
    /// <summary>
    /// Per-backtest identity + Sharpe ratio shared by all optimization-analysis records that describe one backtest.
    /// </summary>
    public class BacktestSummary
    {
        /// <summary>
        /// The backtest id; kept for programmatic access but not serialized into the analysis JSON.
        /// </summary>
        [JsonIgnore]
        public string BacktestId { get; set; }

        /// <summary>
        /// Parameter values the backtest was run with.
        /// </summary>
        public IReadOnlyDictionary<string, decimal> Parameters { get; set; }

        /// <summary>
        /// The backtest's Sharpe ratio.
        /// </summary>
        public decimal SharpeRatio { get; set; }
    }
}
