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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Interfaces;
using QuantConnect.Util;

namespace QuantConnect.Algorithm.Framework.Alphas.Analysis
{
    /// <summary>
    /// Encapsulates the storage and on-line scoring of insights.
    /// </summary>
    /// <remarks>
    /// This type assumes a forward progression of time, and as such, methods invoked will
    /// return data that is current as of the last update time.
    /// This type is designed to be invoked from two separate threads. That does not mean this type
    /// is thread-safe in the general sense, but given a particular invocation pattern. The goal is
    /// to allow the algorithm thread to continue while scoring of the insights happens on a separate
    /// thread. Insights are added on the algorithm thread via AddInsights and scoring updates are pushed
    /// on the insight thrad via UpdateScores. This means that the various collections (open, closed, updated)
    /// are potentially at different frontiers. In fact, it is the common case where the openInsightContexts
    /// collection is ahead of everything else.
    /// </remarks>
    public class InsightManager : IDisposable
    {
        /// <summary>
        /// Gets all insight score types
        /// </summary>
        public static readonly IReadOnlyCollection<InsightScoreType> ScoreTypes = Enum.GetValues(typeof(InsightScoreType)).Cast<InsightScoreType>().ToArray();

        private readonly double _extraAnalysisPeriodRatio;
        private readonly List<IInsightManagerExtension> _extensions;
        private readonly IInsightScoreFunctionProvider _scoreFunctionProvider;

        private readonly HashSet<InsightAnalysisContext> _updatedInsightContexts;
        private readonly HashSet<InsightAnalysisContext> _openInsightContexts;
        private readonly ConcurrentDictionary<Guid, InsightAnalysisContext> _closedInsightContexts;

        /// <summary>
        /// Enumerable of insights still under analysis
        /// </summary>
        public IEnumerable<Insight> OpenInsights => _openInsightContexts.Select(context => context.Insight);

        /// <summary>
        /// Enumerable of insights who's analysis has been completed
        /// </summary>
        public IEnumerable<Insight> ClosedInsights => _closedInsightContexts.Select(kvp => kvp.Value.Insight);

        /// <summary>
        /// Enumerable of all internally maintained insights
        /// </summary>
        public IEnumerable<Insight> AllInsights => OpenInsights.Concat(ClosedInsights);

        /// <summary>
        /// Gets the unique set of symbols from analysis contexts that will
        /// </summary>
        /// <param name="frontierTimeUtc"></param>
        /// <returns></returns>
        public IEnumerable<InsightAnalysisContext> ContextsOpenAt(DateTime frontierTimeUtc) =>
            _openInsightContexts.Where(context => context.AnalysisEndTimeUtc <= frontierTimeUtc);

        /// <summary>
        /// Initializes a new instance of the <see cref="InsightManager"/> class
        /// </summary>
        /// <param name="scoreFunctionProvider">Provides scoring functions by insight type/score type</param>
        /// <param name="extraAnalysisPeriodRatio">Ratio of the insight period to keep the analysis open</param>
        /// <param name="extensions">Extensions used to perform tasks at certain events</param>
        public InsightManager(IInsightScoreFunctionProvider scoreFunctionProvider, double extraAnalysisPeriodRatio, params IInsightManagerExtension[] extensions)
        {
            if (extraAnalysisPeriodRatio < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(extraAnalysisPeriodRatio), "extraAnalysisPeriodRatio must be greater than or equal to zero.");
            }

            _scoreFunctionProvider = scoreFunctionProvider;
            _extraAnalysisPeriodRatio = extraAnalysisPeriodRatio;
            _extensions = extensions?.ToList() ?? new List<IInsightManagerExtension>();

            _openInsightContexts = new HashSet<InsightAnalysisContext>();
            _updatedInsightContexts = new HashSet<InsightAnalysisContext>();
            _closedInsightContexts = new ConcurrentDictionary<Guid, InsightAnalysisContext>();
        }

        /// <summary>
        /// Add an extension to this manager
        /// </summary>
        /// <param name="extension">The extension to be added</param>
        public void AddExtension(IInsightManagerExtension extension)
        {
            _extensions.Add(extension);
        }

        /// <summary>
        /// Initializes any extensions for the specified backtesting range
        /// </summary>
        /// <param name="start">The start date of the backtest (current time in live mode)</param>
        /// <param name="end">The end date of the backtest (<see cref="Time.EndOfTime"/> in live mode)</param>
        /// <param name="current">The algorithm's current utc time</param>
        public void InitializeExtensionsForRange(DateTime start, DateTime end, DateTime current)
        {
            foreach (var extension in _extensions)
            {
                extension.InitializeForRange(start, end, current);
            }
        }

        /// <summary>
        /// Steps the manager forward in time, accepting new state information and potentialy newly generated insights
        /// </summary>
        /// <param name="frontierTimeUtc">The frontier time of the insight analysis</param>
        /// <param name="securityValuesCollection">Snap shot of the securities at the frontier time</param>
        /// <param name="generatedInsights">Any insight generated by the algorithm at the frontier time</param>
        public void Step(DateTime frontierTimeUtc, ReadOnlySecurityValuesCollection securityValuesCollection, GeneratedInsightsCollection generatedInsights)
        {
            if (generatedInsights != null && generatedInsights.Insights.Count > 0)
            {
                foreach (var insight in generatedInsights.Insights)
                {
                    // save initial security values and deterine analysis period
                    var initialValues = securityValuesCollection[insight.Symbol];
                    var analysisPeriod = insight.Period + TimeSpan.FromTicks((long)(_extraAnalysisPeriodRatio * insight.Period.Ticks));

                    // set this as an open analysis context
                    var context = new InsightAnalysisContext(insight, initialValues, analysisPeriod);
                    _openInsightContexts.Add(context);

                    // let everyone know we've received an insight
                    _extensions.ForEach(e => e.OnInsightGenerated(context));
                }
            }

            UpdateScores(securityValuesCollection);

            foreach (var extension in _extensions)
            {
                extension.Step(frontierTimeUtc);
            }
        }

        /// <summary>
        /// Removes insights from the manager with the specified ids
        /// </summary>
        /// <param name="insightIds">The insights ids to be removed</param>
        public void RemoveInsights(IEnumerable<Guid> insightIds)
        {
            foreach (var id in insightIds)
            {
                InsightAnalysisContext context;
                _closedInsightContexts.TryRemove(id, out context);
            }
        }

        /// <summary>
        /// Gets all insight analysis contexts that have been updated since this method's last invocation.
        /// Contexts are marked as not updated during the enumeration, so in order to remove a context from
        /// the updated set, the enumerable must be enumerated.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<InsightAnalysisContext> GetUpdatedContexts()
        {
            var copy = _updatedInsightContexts.ToList();
            _updatedInsightContexts.Clear();
            return copy;
        }

        /// <summary>
        /// Updates all open insight scores
        /// </summary>
        private void UpdateScores(ReadOnlySecurityValuesCollection securityValuesCollection)
        {
            var removals = new HashSet<InsightAnalysisContext>();
            foreach (var context in _openInsightContexts)
            {
                // was this insight period closed before we update the times?
                var previouslyClosed = context.InsightPeriodClosed;

                // update the security values: price/volatility
                context.SetCurrentValues(securityValuesCollection[context.Symbol]);

                // update scores for each score type
                var currentTimeUtc = context.CurrentValues.TimeUtc;
                foreach (var scoreType in ScoreTypes)
                {
                    if (!context.ShouldAnalyze(scoreType))
                    {
                        // not all insights can receive every score type, for example, insight.Magnitude==null, not point in doing magnitude scoring
                        continue;
                    }

                    // resolve and evaluate the scoring function, storing the result in the context
                    var function = _scoreFunctionProvider.GetScoreFunction(context.Insight.Type, scoreType);
                    var score = function.Evaluate(context, scoreType);
                    context.Score.SetScore(scoreType, score, currentTimeUtc);
                }

                // it wasn't closed and now it is closed, fire the event.
                if (!previouslyClosed && context.InsightPeriodClosed)
                {
                    _extensions.ForEach(e => e.OnInsightClosed(context));
                }

                // if this score has been finalized, remove it from the open set
                if (currentTimeUtc >= context.AnalysisEndTimeUtc)
                {
                    context.Score.Finalize(currentTimeUtc);

                    _extensions.ForEach(e => e.OnInsightAnalysisCompleted(context));

                    var id = context.Insight.Id;
                    _closedInsightContexts[id] = context;

                    removals.Add(context);
                }

                // mark the context as having been updated
                _updatedInsightContexts.Add(context);
            }

            _openInsightContexts.RemoveWhere(removals.Contains);
        }

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            foreach (var ext in _extensions)
            {
                (ext as IDisposable)?.DisposeSafely();
            }
        }
    }
}
