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
using QuantConnect.Algorithm.Framework.Alphas.Analysis.Providers;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.Framework.Alphas.Analysis
{
    /// <summary>
    /// Encapsulates the storage and on-line scoring of alphas
    /// </summary>
    public class AlphaManager
    {
        /// <summary>
        /// Gets all alpha score types
        /// </summary>
        public static readonly IReadOnlyCollection<AlphaScoreType> ScoreTypes = Enum.GetValues(typeof(AlphaScoreType)).Cast<AlphaScoreType>().ToArray();

        private readonly double _extraAnalysisPeriodRatio;
        private readonly ISecurityValuesProvider _securityValuesProvider;
        private readonly IAlphaScoreFunctionProvider _scoreFunctionProvider;

        private readonly ConcurrentDictionary<Guid, AlphaAnalysisContext> _openAlphaContexts;
        private readonly ConcurrentDictionary<Guid, AlphaAnalysisContext> _closedAlphaContexts;
        private readonly ConcurrentDictionary<Guid, AlphaAnalysisContext> _updatedAlphaContextsByAlphaId;

        /// <summary>
        /// Event fired when new alpha is received by this manager
        /// </summary>
        public event EventHandler<AlphaAnalysisContext> AlphaReceived;

        /// <summary>
        /// Event fired when an an alpha's period has expired
        /// </summary>
        public event EventHandler<AlphaAnalysisContext> AlphaPeriodClosed;

        /// <summary>
        /// Event fired when an alpha context is finished scoring
        /// </summary>
        public event EventHandler<AlphaAnalysisContext> AlphaAnalysisCompleted;

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
        /// Gets flag indicating that there are open alphas being analyzed
        /// </summary>
        public bool HasOpenAlphas => !_openAlphaContexts.IsEmpty;

        /// <summary>
        /// Initializes a new instance of the <see cref="AlphaManager"/> class
        /// </summary>
        /// <param name="securityValuesProvider">Providers security values, such as price/volatility</param>
        /// <param name="scoreFunctionProvider">Provides scoring functions by alpha type/score type</param>
        /// <param name="extraAnalysisPeriodRatio">Ratio of the alpha period to keep the analysis open</param>
        public AlphaManager(ISecurityValuesProvider securityValuesProvider, IAlphaScoreFunctionProvider scoreFunctionProvider, double extraAnalysisPeriodRatio)
        {
            if (extraAnalysisPeriodRatio < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(extraAnalysisPeriodRatio), "extraAnalysisPeriodRatio must be greater than or equal to zero.");
            }

            _scoreFunctionProvider = scoreFunctionProvider;
            _securityValuesProvider = securityValuesProvider;
            _extraAnalysisPeriodRatio = extraAnalysisPeriodRatio;

            _openAlphaContexts = new ConcurrentDictionary<Guid, AlphaAnalysisContext>();
            _closedAlphaContexts = new ConcurrentDictionary<Guid, AlphaAnalysisContext>();
            _updatedAlphaContextsByAlphaId = new ConcurrentDictionary<Guid, AlphaAnalysisContext>();
        }

        /// <summary>
        /// Add alphas to the manager from the collection
        /// </summary>
        /// <param name="collection">The alpha collection emitted from <see cref="IAlgorithm.AlphasGenerated"/></param>
        public void AddAlphas(AlphaCollection collection)
        {
            foreach (var alpha in collection.Alphas)
            {
                // save initial security values and deterine analysis period
                var initialValues = _securityValuesProvider.GetValues(alpha.Symbol);
                var analysisPeriod = alpha.Period + TimeSpan.FromTicks((long) (_extraAnalysisPeriodRatio * alpha.Period.Ticks));

                // set this as an open analysis context
                var context = new AlphaAnalysisContext(alpha, initialValues, analysisPeriod);
                _openAlphaContexts[alpha.Id] = context;

                // let everyone know we've received alpha
                OnAlphaReceived(context);
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
        /// Updates all open alpha scores
        /// </summary>
        public void UpdateScores()
        {
            if (_openAlphaContexts.IsEmpty)
            {
                // short circuit to prevent enumeration
                return;
            }

            foreach (var kvp in _openAlphaContexts)
            {
                var context = kvp.Value;

                // was this alpha period closed before we update the times?
                var alphaPeriodClosed = context.AlphaPeriodClosed;

                // update the security values: price/volatility
                context.SetCurrentValues(_securityValuesProvider.GetValues(context.Symbol));

                // update scores for each score type
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
                    context.Score.SetScore(scoreType, score, context.CurrentValues.TimeUtc, context.AnalysisEndTimeUtc);
                }

                // it wasn't closed and now it is closed, fire the event.
                if (!alphaPeriodClosed && context.AlphaPeriodClosed)
                {
                    OnAlphaPeriodCompleted(context);
                }

                // if this score has been finalized, remove it from the open set
                if (context.Score.IsFinalScore)
                {
                    OnAlphaScoringFinalzed(context);

                    var id = context.Alpha.Id;
                    _closedAlphaContexts[id] = context;

                    AlphaAnalysisContext c;
                    _openAlphaContexts.TryRemove(id, out c);
                }

                // mark the context as having been updated
                _updatedAlphaContextsByAlphaId[context.Alpha.Id] = context;
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
        /// Event invocator for the <see cref="AlphaPeriodClosed"/> event
        /// </summary>
        /// <param name="context">The context whose alpha period has closed</param>
        protected virtual void OnAlphaPeriodCompleted(AlphaAnalysisContext context)
        {
            AlphaPeriodClosed?.Invoke(this, context);
        }

        /// <summary>
        /// Event invocator for the <see cref="AlphaAnalysisCompleted"/> event
        /// </summary>
        /// <param name="context">The context whose analysis period and scoring has finalized</param>
        protected virtual void OnAlphaScoringFinalzed(AlphaAnalysisContext context)
        {
            AlphaAnalysisCompleted?.Invoke(this, context);
        }

        /// <summary>
        /// Event invocator for the <see cref="AlphaReceived"/> event
        /// </summary>
        /// <param name="context">The context whose alpha was generated this time step</param>
        protected virtual void OnAlphaReceived(AlphaAnalysisContext context)
        {
            AlphaReceived?.Invoke(this, context);
        }
    }
}
