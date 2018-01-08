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

namespace QuantConnect.Algorithm.Framework.Alphas.Analysis
{
    /// <summary>
    /// Encapsulates the storage and on-line scoring of alphas.
    /// </summary>
    /// <remarks>
    /// This type assumes a forward progression of time, and as such, methods invoked will
    /// return data that is current as of the last update time.
    /// This type is designed to be invoked from two separate threads. That does not mean this type
    /// is thread-safe in the general sense, but given a particular invocation pattern. The goal is
    /// to allow the algorithm thread to continue while scoring of the alphas happens on a separate
    /// thread. Alphas are added on the algorithm thread via AddAlphas and scoring updates are pushed
    /// on the alpha thrad via UpdateScores. This means that the various collections (open, closed, updated)
    /// are potentially at different frontiers. In fact, it is the common case where the openAlphaContexts
    /// collection is ahead of everything else.
    /// </remarks>
    public class AlphaManager
    {
        /// <summary>
        /// Gets all alpha score types
        /// </summary>
        public static readonly IReadOnlyCollection<AlphaScoreType> ScoreTypes = Enum.GetValues(typeof(AlphaScoreType)).Cast<AlphaScoreType>().ToArray();

        private readonly double _extraAnalysisPeriodRatio;
        private readonly List<IAlphaManagerExtension> _extensions;
        private readonly IAlphaScoreFunctionProvider _scoreFunctionProvider;

        private readonly ConcurrentDictionary<Guid, AlphaAnalysisContext> _openAlphaContexts;
        private readonly ConcurrentDictionary<Guid, AlphaAnalysisContext> _closedAlphaContexts;
        private readonly ConcurrentDictionary<Guid, AlphaAnalysisContext> _updatedAlphaContextsByAlphaId;

        /// <summary>
        /// Enumerable of alphas still under analysis
        /// </summary>
        public IEnumerable<Alpha> OpenAlphas => _openAlphaContexts.Select(kvp => kvp.Value.Alpha);

        /// <summary>
        /// Enumerable of alphas who's analysis has been completed
        /// </summary>
        public IEnumerable<Alpha> ClosedAlphas => _closedAlphaContexts.Select(kvp => kvp.Value.Alpha);

        /// <summary>
        /// Enumerable of all internally maintained alphas
        /// </summary>
        public IEnumerable<Alpha> AllAlphas => OpenAlphas.Concat(ClosedAlphas);

        /// <summary>
        /// Gets the unique set of symbols from analysis contexts that will
        /// </summary>
        /// <param name="frontierTimeUtc"></param>
        /// <returns></returns>
        public IEnumerable<AlphaAnalysisContext> ContextsOpenAt(DateTime frontierTimeUtc) =>
            _openAlphaContexts
                .Where(kvp => kvp.Value.AnalysisEndTimeUtc <= frontierTimeUtc)
                .Select(kvp => kvp.Value);

        /// <summary>
        /// Gets flag indicating that there are open alphas being analyzed
        /// </summary>
        public bool HasOpenAlphas => !_openAlphaContexts.IsEmpty;

        /// <summary>
        /// Initializes a new instance of the <see cref="AlphaManager"/> class
        /// </summary>
        /// <param name="scoreFunctionProvider">Provides scoring functions by alpha type/score type</param>
        /// <param name="extraAnalysisPeriodRatio">Ratio of the alpha period to keep the analysis open</param>
        /// <param name="extensions">Extensions used to perform tasks at certain events</param>
        public AlphaManager(IAlphaScoreFunctionProvider scoreFunctionProvider, double extraAnalysisPeriodRatio, params IAlphaManagerExtension[] extensions)
        {
            if (extraAnalysisPeriodRatio < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(extraAnalysisPeriodRatio), "extraAnalysisPeriodRatio must be greater than or equal to zero.");
            }

            _scoreFunctionProvider = scoreFunctionProvider;
            _extraAnalysisPeriodRatio = extraAnalysisPeriodRatio;
            _extensions = extensions?.ToList() ?? new List<IAlphaManagerExtension>();

            _openAlphaContexts = new ConcurrentDictionary<Guid, AlphaAnalysisContext>();
            _closedAlphaContexts = new ConcurrentDictionary<Guid, AlphaAnalysisContext>();
            _updatedAlphaContextsByAlphaId = new ConcurrentDictionary<Guid, AlphaAnalysisContext>();
        }

        /// <summary>
        /// Add an extension to this manager
        /// </summary>
        /// <param name="extension">The extension to be added</param>
        public void AddExtension(IAlphaManagerExtension extension)
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
        /// Steps the manager forward in time, accepting new state information and potentialy newly generated alphas
        /// </summary>
        /// <param name="frontierTimeUtc">The frontier time of the alpha analysis</param>
        /// <param name="securityValuesCollection">Snap shot of the securities at the frontier time</param>
        /// <param name="generatedAlphas">Any alpha generated by the algorithm at the frontier time</param>
        public void Step(DateTime frontierTimeUtc, ReadOnlySecurityValuesCollection securityValuesCollection, AlphaCollection generatedAlphas)
        {
            if (generatedAlphas != null && generatedAlphas.Alphas.Count > 0)
            {
                foreach (var alpha in generatedAlphas.Alphas)
                {
                    // save initial security values and deterine analysis period
                    var initialValues = securityValuesCollection[alpha.Symbol];
                    var analysisPeriod = alpha.Period + TimeSpan.FromTicks((long)(_extraAnalysisPeriodRatio * alpha.Period.Ticks));

                    // set this as an open analysis context
                    var context = new AlphaAnalysisContext(alpha, initialValues, analysisPeriod);
                    _openAlphaContexts[alpha.Id] = context;

                    // let everyone know we've received alpha
                    OnAlphaReceived(context);
                }
            }

            UpdateScores(securityValuesCollection);

            foreach (var extension in _extensions)
            {
                extension.Step(frontierTimeUtc);
            }
        }

        /// <summary>
        /// Removes alphas from the manager with the specified ids
        /// </summary>
        /// <param name="alphaIds">The alphas ids to be removed</param>
        public void RemoveAlphas(IEnumerable<Guid> alphaIds)
        {
            foreach (var id in alphaIds)
            {
                AlphaAnalysisContext context;
                _closedAlphaContexts.TryRemove(id, out context);
            }
        }

        /// <summary>
        /// Gets all alpha analysis contexts that have been updated since this method's last invocation.
        /// Contexts are marked as not updated during the enumeration, so in order to remove a context from
        /// the updated set, the enumerable must be enumerated.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<AlphaAnalysisContext> GetUpdatedContexts()
        {
            foreach (var kvp in _updatedAlphaContextsByAlphaId)
            {
                var context = kvp.Value;

                AlphaAnalysisContext c;
                _updatedAlphaContextsByAlphaId.TryRemove(context.Alpha.Id, out c);
                yield return context;
            }
        }

        /// <summary>
        /// Event invocator for the <see cref="AlphaClosed"/> event
        /// </summary>
        /// <param name="context">The context whose alpha period has closed</param>
        protected virtual void OnAlphaPeriodCompleted(AlphaAnalysisContext context)
        {
            _extensions.ForEach(e => e.OnAlphaClosed(context));
        }

        /// <summary>
        /// Event invocator for the <see cref="AlphaAnalysisCompleted"/> event
        /// </summary>
        /// <param name="context">The context whose analysis period and scoring has finalized</param>
        protected virtual void OnAlphaAnalysisCompleted(AlphaAnalysisContext context)
        {
            _extensions.ForEach(e => e.OnAlphaAnalysisCompleted(context));
        }

        /// <summary>
        /// Event invocator for the <see cref="AlphaReceived"/> event
        /// </summary>
        /// <param name="context">The context whose alpha was generated this time step</param>
        protected virtual void OnAlphaReceived(AlphaAnalysisContext context)
        {
            _extensions.ForEach(e => e.OnAlphaGenerated(context));
        }

        /// <summary>
        /// Updates all open alpha scores
        /// </summary>
        private void UpdateScores(ReadOnlySecurityValuesCollection securityValuesCollection)
        {
            foreach (var kvp in _openAlphaContexts)
            {
                var context = kvp.Value;

                // was this alpha period closed before we update the times?
                var previouslyClosed = context.AlphaPeriodClosed;

                // update the security values: price/volatility
                context.SetCurrentValues(securityValuesCollection[context.Symbol]);

                // update scores for each score type
                var currentTimeUtc = context.CurrentValues.TimeUtc;
                foreach (var scoreType in ScoreTypes)
                {
                    if (!context.ShouldAnalyze(scoreType))
                    {
                        // not all alphas can receive every score type, for example, alpha.Magnitude==null, not point in doing magnitude scoring
                        continue;
                    }

                    // resolve and evaluate the scoring function, storing the result in the context
                    var function = _scoreFunctionProvider.GetScoreFunction(context.Alpha.Type, scoreType);
                    var score = function.Evaluate(context, scoreType);
                    context.Score.SetScore(scoreType, score, currentTimeUtc);
                }

                // it wasn't closed and now it is closed, fire the event.
                if (!previouslyClosed && context.AlphaPeriodClosed)
                {
                    OnAlphaPeriodCompleted(context);
                }

                // if this score has been finalized, remove it from the open set
                if (currentTimeUtc >= context.AnalysisEndTimeUtc)
                {
                    context.Score.Finalize(currentTimeUtc);

                    OnAlphaAnalysisCompleted(context);

                    var id = context.Alpha.Id;
                    _closedAlphaContexts[id] = context;

                    AlphaAnalysisContext c;
                    _openAlphaContexts.TryRemove(id, out c);
                }

                // mark the context as having been updated
                _updatedAlphaContextsByAlphaId[context.Alpha.Id] = context;
            }
        }
    }
}
