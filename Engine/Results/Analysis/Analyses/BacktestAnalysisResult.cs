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
using System.Collections;
using System.Collections.Generic;

namespace QuantConnect.Lean.Engine.Results.Analysis.Analyses
{
    /// <summary>
    /// A simple context that holds a single diagnostic sample object.
    /// </summary>
    public class BacktestAnalysisContext : IBacktestAnalysisContext
    {
        /// <summary>
        /// Gets or sets a representative sample value produced by the analysis.
        /// </summary>
        public object Sample { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BacktestAnalysisContext"/> class.
        /// </summary>
        /// <param name="sample">A representative sample produced by the analysis, or <c>null</c> when no issue was found.</param>
        public BacktestAnalysisContext(object sample)
        {
            Sample = sample;
        }
    }

    /// <summary>
    /// A context that represents multiple occurrences of the same diagnostic issue,
    /// exposing the first sample and the total occurrence count.
    /// </summary>
    public class BacktestAnalysisRepeatedContext : BacktestAnalysisContext
    {
        /// <summary>
        /// Gets or sets the total number of matching occurrences found by the analysis.
        /// </summary>
        public int Occurrences { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BacktestAnalysisRepeatedContext"/> class.
        /// </summary>
        /// <param name="samples">All matching sample objects; the first element is stored as <see cref="BacktestAnalysisContext.Sample"/>.</param>
        public BacktestAnalysisRepeatedContext(IReadOnlyList<object> samples) : base(samples.Count > 0 ? samples[0] : null)
        {
            Occurrences = samples.Count;
        }
    }

    /// <summary>
    /// A composite context that aggregates several <see cref="IBacktestAnalysisContext"/> instances
    /// into a single enumerable context.
    /// </summary>
    public class BacktestAnalysisAggregateContext : IBacktestAnalysisContext, IEnumerable<IBacktestAnalysisContext>
    {
        private IReadOnlyList<IBacktestAnalysisContext> _contexts { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BacktestAnalysisAggregateContext"/> class.
        /// </summary>
        /// <param name="contexts">The individual contexts to aggregate.</param>
        public BacktestAnalysisAggregateContext(IReadOnlyList<IBacktestAnalysisContext> contexts)
        {
            _contexts = contexts;
        }

        /// <summary>
        /// Returns an enumerator that iterates over each contained context.
        /// </summary>
        /// <returns>An enumerator for the inner context list.</returns>
        public IEnumerator<IBacktestAnalysisContext> GetEnumerator()
        {
            return _contexts.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    /// <summary>
    /// Represents the outcome of a single backtest diagnostic analysis,
    /// containing the analysis name, diagnostic context, and a list of potential solutions.
    /// </summary>
    public class BacktestAnalysisResult : IBacktestAnalysisResult
    {
        /// <summary>
        /// Gets or sets the name of the analysis that produced this result.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the diagnostic context carrying sample data about the detected issue.
        /// </summary>
        public IBacktestAnalysisContext Context { get; set; }

        /// <summary>
        /// Gets or sets human-readable suggestions for resolving the detected issue.
        /// </summary>
        public IReadOnlyList<string> PotentialSolutions { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BacktestAnalysisResult"/> class.
        /// </summary>
        /// <param name="name">The name of the analysis that produced this result.</param>
        /// <param name="context">The diagnostic context object describing the detected issue.</param>
        /// <param name="potentialSolutions">A list of human-readable remediation suggestions.</param>
        public BacktestAnalysisResult(string name, IBacktestAnalysisContext context, IReadOnlyList<string> potentialSolutions)
        {
            Name = name;
            Context = context;
            PotentialSolutions = potentialSolutions;
        }
    }
}
