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

using QuantConnect.Optimizer.Parameters;

namespace QuantConnect.Optimizer
{
    /// <summary>
    /// Defines the result of Lean compute job
    /// </summary>
    public class OptimizationResult
    {
        /// <summary>
        /// Corresponds to initial result to drive the optimization strategy
        /// </summary>
        public static readonly OptimizationResult Initial = new OptimizationResult(null, null, null);

        /// <summary>
        /// The backtest id that generated this result
        /// </summary>
        public string BacktestId { get; }

        /// <summary>
        /// Parameter set Id
        /// </summary>
        public int Id => ParameterSet?.Id ?? 0;

        /// <summary>
        /// Json Backtest result
        /// </summary>
        public string JsonBacktestResult { get; }

        /// <summary>
        /// The parameter set at which the result was achieved
        /// </summary>
        public ParameterSet ParameterSet { get; }

        /// <summary>
        /// Create an instance of <see cref="OptimizationResult"/>
        /// </summary>
        /// <param name="jsonBacktestResult">Optimization target value for this backtest</param>
        /// <param name="parameterSet">Parameter set used in compute job</param>
        /// <param name="backtestId">The backtest id that generated this result</param>
        public OptimizationResult(string jsonBacktestResult, ParameterSet parameterSet, string backtestId)
        {
            JsonBacktestResult = jsonBacktestResult;
            ParameterSet = parameterSet;
            BacktestId = backtestId;
        }
    }
}
