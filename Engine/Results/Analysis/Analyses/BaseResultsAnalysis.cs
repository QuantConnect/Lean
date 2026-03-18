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
using Python.Runtime;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Lean.Engine.Results.Analysis.Analyses
{
    /// <summary>
    /// Abstract base class for all backtest diagnostic tests.
    /// </summary>
    public abstract class BaseResultsAnalysis
    {
        /// <summary>
        /// Gets a short (3–8 word) description of why the analysis was triggered.
        /// </summary>
        public abstract string Issue { get; }

        /// <summary>
        /// Gets the severity/impact weight (0–100). Higher values run first and rank higher in results.
        /// </summary>
        public abstract int Weight { get; }

        /// <summary>
        /// Wraps a single <see cref="AnalysisResult"/> in a one-element read-only list.
        /// </summary>
        protected IReadOnlyList<AnalysisResult> SingleResponse(IResultsAnalysisContext context, IReadOnlyList<string> solutions = null)
            => [CreateResponse(context, solutions)];

        /// <summary>
        /// Creates a single <see cref="AnalysisResult"/> named after the concrete analysis type.
        /// </summary>
        protected AnalysisResult CreateResponse(IResultsAnalysisContext context, IReadOnlyList<string> solutions = null)
            => new(GetType().Name, Issue, Weight, context, solutions ?? []);

        /// <summary>
        /// Filters <paramref name="responses"/> to those with solutions,
        /// prefixes the class name, and returns a flat list.
        /// </summary>
        protected IReadOnlyList<AnalysisResult> CreateAggregatedResponse(IEnumerable<AnalysisResult> responses)
            => [.. responses
                .Where(x => x.Solutions.Count > 0)
                .Select(x => new AnalysisResult(GetType().Name + " / " + x.Name, x.Issue, x.Weight, x.Context, x.Solutions))];

        /// <summary>
        /// Formats the specified code string according to the conventions of the given programming language.
        /// </summary>
        protected static string FormatCode(string code, Language language)
        {
            return language switch
            {
                Language.Python => code.ToSnakeCase(),
                _ => code
            };
        }
    }
}
