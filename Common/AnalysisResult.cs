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
using QuantConnect.Util;
using System.Collections;
using System.Collections.Generic;

namespace QuantConnect
{
    /// <summary>
    /// A simple context that holds a single diagnostic sample object.
    /// </summary>
    public class ResultsAnalysisContext : IResultsAnalysisContext
    {
        /// <summary>
        /// Gets or sets a representative sample value produced by the analysis.
        /// </summary>
        public object Sample { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResultsAnalysisContext"/> class.
        /// </summary>
        /// <param name="sample">A representative sample produced by the analysis, or <c>null</c> when no issue was found.</param>
        public ResultsAnalysisContext(object sample)
        {
            Sample = sample;
        }
    }

    /// <summary>
    /// A context that represents multiple occurrences of the same diagnostic issue,
    /// exposing the first sample and the total occurrence count.
    /// </summary>
    public class ResultsAnalysisRepeatedContext : ResultsAnalysisContext
    {
        /// <summary>
        /// Gets or sets the total number of matching occurrences found by the analysis.
        /// </summary>
        public int Occurrences { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResultsAnalysisRepeatedContext"/> class.
        /// </summary>
        /// <param name="samples">All matching sample objects; the first element is stored as <see cref="ResultsAnalysisContext.Sample"/>.</param>
        public ResultsAnalysisRepeatedContext(IReadOnlyList<object> samples) : base(samples.Count > 0 ? samples[0] : null)
        {
            Occurrences = samples.Count;
        }
    }

    /// <summary>
    /// A composite context that aggregates several <see cref="IResultsAnalysisContext"/> instances
    /// into a single enumerable context.
    /// </summary>
    public class ResultsAnalysisAggregateContext : IResultsAnalysisContext, IEnumerable<IResultsAnalysisContext>
    {
        private IReadOnlyList<IResultsAnalysisContext> _contexts { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResultsAnalysisAggregateContext"/> class.
        /// </summary>
        /// <param name="contexts">The individual contexts to aggregate.</param>
        public ResultsAnalysisAggregateContext(IReadOnlyList<IResultsAnalysisContext> contexts)
        {
            _contexts = contexts;
        }

        /// <summary>
        /// Returns an enumerator that iterates over each contained context.
        /// </summary>
        /// <returns>An enumerator for the inner context list.</returns>
        public IEnumerator<IResultsAnalysisContext> GetEnumerator()
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
    /// containing the analysis name, diagnostic context, and a list of solutions.
    /// </summary>
    [JsonConverter(typeof(AnalysisResultJsonConverter))]
    public class AnalysisResult
    {
        /// <summary>
        /// Gets or sets the name of the analysis that produced this result.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets a short description of why the analysis was triggered.
        /// </summary>
        public string Issue { get; set; }

        /// <summary>
        /// Gets or sets the diagnostic context carrying sample data about the detected issue.
        /// </summary>
        public IResultsAnalysisContext Context { get; set; }

        /// <summary>
        /// Gets or sets human-readable suggestions for resolving the detected issue.
        /// </summary>
        public IReadOnlyList<string> Solutions { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AnalysisResult"/> class.
        /// </summary>
        public AnalysisResult(string name, string issue, IResultsAnalysisContext context, IReadOnlyList<string> solutions)
        {
            Name = name;
            Issue = issue;
            Context = context;
            Solutions = solutions;
        }
    }
}
