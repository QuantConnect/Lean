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
using System.Linq;

namespace QuantConnect.Lean.Engine.Results.Analysis.Analyses
{
    /// <summary>
    /// Abstract base class for all backtest diagnostic tests.
    /// </summary>
    public abstract class BaseBacktestAnalysis
    {
        /// <summary>
        /// Wraps a single <see cref="BacktestAnalysisResult"/> in a one-element read-only list.
        /// </summary>
        /// <param name="context">The context object carrying diagnostic sample data.</param>
        /// <param name="potentialSolutions">Optional list of human-readable remediation suggestions.</param>
        /// <returns>A one-element read-only list containing the constructed result.</returns>
        protected IReadOnlyList<BacktestAnalysisResult> SingleResponse(IBacktestAnalysisContext context, List<string> potentialSolutions = null)
            => [CreateResponse(context, potentialSolutions)];

        /// <summary>
        /// Creates a single <see cref="BacktestAnalysisResult"/> named after the concrete analysis type.
        /// </summary>
        /// <param name="context">The context object carrying diagnostic sample data.</param>
        /// <param name="potentialSolutions">Optional list of human-readable remediation suggestions.</param>
        /// <returns>A new <see cref="BacktestAnalysisResult"/> instance.</returns>
        protected BacktestAnalysisResult CreateResponse(IBacktestAnalysisContext context, List<string> potentialSolutions = null)
            => new(GetType().Name, context, potentialSolutions ?? []);

        /// <summary>
        /// Filters <paramref name="responses"/> to those with solutions,
        /// prefixes the class name, and returns a flat list.
        /// </summary>
        protected IReadOnlyList<BacktestAnalysisResult> CreateAggregatedResponse(IEnumerable<BacktestAnalysisResult> responses)
            => responses
                .Where(x => x.PotentialSolutions.Count > 0)
                .Select(x => new BacktestAnalysisResult(GetType().Name + " / " + x.Name, x.Context, x.PotentialSolutions))
                .ToList();
    }
}
