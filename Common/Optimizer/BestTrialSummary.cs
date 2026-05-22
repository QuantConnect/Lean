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

using System.Collections.Generic;

namespace QuantConnect.Optimizer
{
    /// <summary>
    /// Identifies the single best-performing trial in an optimization (argmax of Sharpe).
    /// </summary>
    public class BestTrialSummary
    {
        /// <summary>
        /// The backtest id of the best-performing trial.
        /// </summary>
        public string BacktestId { get; set; }

        /// <summary>
        /// Parameter values for the best trial (parameter name -> numeric value).
        /// </summary>
        public IReadOnlyDictionary<string, double> Parameters { get; set; }

        /// <summary>
        /// Sharpe ratio of the best trial.
        /// </summary>
        public double SharpeRatio { get; set; }
    }
}
