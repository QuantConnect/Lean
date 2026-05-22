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

using QuantConnect.Optimizer.Parameters;

namespace QuantConnect.Optimizer
{
    /// <summary>
    /// Common-side per-trial input to <see cref="Analysis.OptimizationAnalyzer"/>. Carries
    /// only the fields the analyzer reads (backtest id, parameter set, serialized backtest
    /// result JSON). Decouples the analyzer from the <c>QuantConnect.Optimizer</c> assembly's
    /// <c>OptimizationResult</c> type so analyzer code can live in Common without forcing
    /// Common to reference Optimizer.
    /// </summary>
    public class OptimizationTrial
    {
        /// <summary>
        /// The backtest id that produced this trial.
        /// </summary>
        public string BacktestId { get; }

        /// <summary>
        /// The parameter set the trial was run with.
        /// </summary>
        public ParameterSet ParameterSet { get; }

        /// <summary>
        /// The serialized backtest result. The analyzer reads <c>Statistics</c>
        /// (for Sharpe and total orders) and <c>Analysis</c> (for the zero-order
        /// failure breakdown) off the deserialized payload.
        /// </summary>
        public string JsonBacktestResult { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="OptimizationTrial"/> class.
        /// </summary>
        /// <param name="backtestId">The backtest id that produced this trial.</param>
        /// <param name="parameterSet">The parameter set the trial was run with.</param>
        /// <param name="jsonBacktestResult">The serialized backtest result JSON.</param>
        public OptimizationTrial(string backtestId, ParameterSet parameterSet, string jsonBacktestResult)
        {
            BacktestId = backtestId;
            ParameterSet = parameterSet;
            JsonBacktestResult = jsonBacktestResult;
        }
    }
}
