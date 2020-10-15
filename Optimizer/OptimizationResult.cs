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

namespace QuantConnect.Optimizer
{
    /// <summary>
    /// Defines the result of Lean compute job
    /// </summary>
    public class OptimizationResult
    {
        /// <summary>
        /// Corresponds to empty result - zero profit at empty permission set
        /// </summary>
        public static readonly OptimizationResult Empty = new OptimizationResult();

        /// <summary>
        /// Id of optimization job. Equals to parameter set Id
        /// </summary>
        public int Id => ParameterSet?.Id ?? 0;

        /// <summary>
        /// Target criterion value
        /// </summary>
        public decimal? Target { get; }

        /// <summary>
        /// The parameter set at which the result was achieved
        /// </summary>
        public ParameterSet ParameterSet { get; }

        /// <summary>
        /// Create an empty instance of <see cref="OptimizationResult"/>
        /// </summary>
        private OptimizationResult() {}

        /// <summary>
        /// Create an instance of <see cref="OptimizationResult"/>
        /// </summary>
        /// <param name="target">Optimization target value for this backtest</param>
        /// <param name="parameterSet">Parameter set used in compute job</param>
        public OptimizationResult(decimal? target, ParameterSet parameterSet)
        {
            Target = target;
            ParameterSet = parameterSet;
        }
    }
}
