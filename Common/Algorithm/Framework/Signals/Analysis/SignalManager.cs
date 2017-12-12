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

namespace QuantConnect.Algorithm.Framework.Signals.Analysis
{
    /// <summary>
    /// Encapsulates the storage and on-line scoring of signals
    /// </summary>
    public class SignalManager
    {
        /// <summary>
        /// Gets all signal score types
        /// </summary>
        public static readonly IReadOnlyCollection<SignalScoreType> ScoreTypes = Enum.GetValues(typeof(SignalScoreType)).Cast<SignalScoreType>().ToArray();

        private readonly double _extraAnalysisPeriodRatio;
        private readonly ISecurityValuesProvider _securityValuesProvider;
        private readonly ISignalScoreFunctionProvider _scoreFunctionProvider;

        private readonly ConcurrentDictionary<Guid, SignalAnalysisContext> _openSignalContexts;
        private readonly ConcurrentDictionary<Guid, SignalAnalysisContext> _closedSignalContexts;
        private readonly ConcurrentDictionary<Guid, SignalAnalysisContext> _updatedSignalContextsBySignalId;

        /// <summary>
        /// Enumerable of signals still under analysis
        /// </summary>
        public IEnumerable<Signal> OpenSignals => _openSignalContexts.Values.Select(context => context.Signal);

        /// <summary>
        /// Enumerable of signals who's analysis has been completed
        /// </summary>
        public IEnumerable<Signal> ClosedSignals => _closedSignalContexts.Values.Select(context => context.Signal);

        /// <summary>
        /// Enumerable of all internally maintained signals
        /// </summary>
        public IEnumerable<Signal> AllSignals => OpenSignals.Concat(ClosedSignals);

        /// <summary>
        /// Gets flag indicating that there are open signals being analyzed
        /// </summary>
        public bool HasOpenSignals => !_openSignalContexts.IsEmpty;

        /// <summary>
        /// Initializes a new instance of the <see cref="SignalManager"/> class
        /// </summary>
        /// <param name="securityValuesProvider">Providers security values, such as price/volatility</param>
        /// <param name="scoreFunctionProvider">Provides scoring functions by signal type/score type</param>
        /// <param name="extraAnalysisPeriodRatio">Ratio of the signal period to keep the analysis open</param>
        public SignalManager(ISecurityValuesProvider securityValuesProvider, ISignalScoreFunctionProvider scoreFunctionProvider, double extraAnalysisPeriodRatio)
        {
            if (extraAnalysisPeriodRatio < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(extraAnalysisPeriodRatio), "extraAnalysisPeriodRatio must be greater than or equal to zero.");
            }

            _scoreFunctionProvider = scoreFunctionProvider;
            _securityValuesProvider = securityValuesProvider;
            _extraAnalysisPeriodRatio = extraAnalysisPeriodRatio;

            _openSignalContexts = new ConcurrentDictionary<Guid, SignalAnalysisContext>();
            _closedSignalContexts = new ConcurrentDictionary<Guid, SignalAnalysisContext>();
            _updatedSignalContextsBySignalId = new ConcurrentDictionary<Guid, SignalAnalysisContext>();
        }

        /// <summary>
        /// Add signals to the manager from the collection
        /// </summary>
        /// <param name="collection">The signal collection emitted from <see cref="IAlgorithm.SignalsGenerated"/></param>
        public void AddSignals(SignalCollection collection)
        {
            foreach (var signal in collection.Signals)
            {
                var initialValues = _securityValuesProvider.GetValues(signal.Symbol);
                var analysisPeriod = signal.Period + TimeSpan.FromTicks((long) (_extraAnalysisPeriodRatio * signal.Period.Ticks));
                _openSignalContexts[signal.Id] = new SignalAnalysisContext(signal, initialValues, analysisPeriod);
            }
        }

        /// <summary>
        /// Removes signals from the manager with the specified ids
        /// </summary>
        /// <param name="signalIds">The signals ids to be removed</param>
        public void RemoveSignals(IEnumerable<Guid> signalIds)
        {
            foreach (var id in signalIds)
            {
                SignalAnalysisContext context;
                _closedSignalContexts.TryRemove(id, out context);
            }
        }

        /// <summary>
        /// Updates all open signal scores
        /// </summary>
        public void UpdateScores()
        {
            if (_openSignalContexts.IsEmpty)
            {
                // short circuit to prevent enumeration
                return;
            }

            foreach (var context in _openSignalContexts.Values)
            {
                // update the security values: price/volatility
                context.SetCurrentValues(_securityValuesProvider.GetValues(context.Symbol));

                // update scores for each score type
                foreach (var scoreType in ScoreTypes)
                {
                    // resolve and evaluate the scoring function, storing the result in the context
                    var function = _scoreFunctionProvider.GetScoreFunction(context.Signal.Type, scoreType);
                    var score = function.Evaluate(context, scoreType);
                    context.Score.SetScore(scoreType, score, context.CurrentValues.TimeUtc);
                }

                // if we've passed the end time then mark it as finalized
                if (context.CurrentValues.TimeUtc >= context.AnalysisEndTimeUtc)
                {
                    context.Signal.Score.IsFinalScore = true;

                    var id = context.Signal.Id;
                    _closedSignalContexts[id] = context;

                    SignalAnalysisContext c;
                    _openSignalContexts.TryRemove(id, out c);
                }

                // mark the as having been updated
                _updatedSignalContextsBySignalId[context.Signal.Id] = context;
            }
        }

        /// <summary>
        /// Gets all signal analysis contexts that have been updated since this method's last invocation.
        /// Contexts are marked as not updated during the enumeration, so in order to remove a context from
        /// the updated set, the enumerable must be enumerated.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<SignalAnalysisContext> GetUpdatedContexts()
        {
            foreach (var context in _updatedSignalContextsBySignalId.Values)
            {
                SignalAnalysisContext c;
                _updatedSignalContextsBySignalId.TryRemove(context.Signal.Id, out c);
                yield return context;
            }
        }
    }
}
