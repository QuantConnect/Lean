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
using System.Collections.Generic;
using System.Linq;
using Accord.Math;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Data;
using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.Algorithm.Framework.Portfolio
{
    /// <summary>
    /// Provides an implementation of Mean-Variance portfolio optimization based on modern portfolio theory.
    /// The interval of weights in optimization method can be changed based on the long-short algorithm.
    /// The default model uses the last three months daily price to calculate the optimal weight
    /// with the weight range from -1 to 1 and minimize the portfolio variance with a target return of 2%
    /// </summary>
    public class MeanVarianceOptimizationPortfolioConstructionModel : PortfolioConstructionModel
    {
        private readonly int _lookback;
        private readonly int _period;
        private readonly Resolution _resolution;
        private readonly IPortfolioOptimizer _optimizer;

        private readonly List<Symbol> _pendingRemoval;
        private readonly Dictionary<Symbol, ReturnsSymbolData> _symbolDataDict;

        /// <summary>
        /// Initialize the model
        /// </summary>
        /// <param name="lookback">Historical return lookback period</param>
        /// <param name="period">The time interval of history price to calculate the weight</param>
        /// <param name="resolution">The resolution of the history price</param>
        /// <param name="targetReturn">The target portfolio return</param>
        /// <param name="optimizer">The portfolio optimization algorithm. If the algorithm is not provided then the default will be mean-variance optimization.</param>
        public MeanVarianceOptimizationPortfolioConstructionModel(
            int lookback = 1,
            int period = 63,
            Resolution resolution = Resolution.Daily,
            double targetReturn = 0.02,
            IPortfolioOptimizer optimizer = null
            )
        {
            _lookback = lookback;
            _period = period;
            _resolution = resolution;

            _optimizer = optimizer ?? new MinimumVariancePortfolioOptimizer(targetReturn: targetReturn);

            _pendingRemoval = new List<Symbol>();
            _symbolDataDict = new Dictionary<Symbol, ReturnsSymbolData>();
        }

        /// <summary>
        /// Create portfolio targets from the specified insights
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="insights">The insights to create portfolio targets from</param>
        /// <returns>An enumerable of portfolio targets to be sent to the execution model</returns>
        public override IEnumerable<IPortfolioTarget> CreateTargets(QCAlgorithm algorithm, Insight[] insights)
        {
            var targets = new List<IPortfolioTarget>();

            // remove pending
            foreach (var symbol in _pendingRemoval)
            {
                targets.Add(new PortfolioTarget(symbol, 0));
            }
            _pendingRemoval.Clear();

            insights = FilterInvalidInsightMagnitude(algorithm, insights);

            var symbols = insights.Select(x => x.Symbol).Distinct();
            if (symbols.Count() == 0 || insights.All(x => x.Magnitude == 0))
            {
                return targets;
            }

            foreach (var insight in insights)
            {
                ReturnsSymbolData data;
                if (_symbolDataDict.TryGetValue(insight.Symbol, out data))
                {
                    if (!insight.Magnitude.HasValue)
                    {
                        algorithm.SetRunTimeError(
                            new ArgumentNullException(
                                insight.Symbol.Value,
                                "MeanVarianceOptimizationPortfolioConstructionModel does not accept 'null' as Insight.Magnitude. "+
                                "Please checkout the selected Alpha Model specifications: " + insight.SourceModel));
                        continue;
                    }
                    data.Add(algorithm.Time, insight.Magnitude.Value.SafeDecimalCast());
                }
            }

            // Get symbols' returns
            var returns = _symbolDataDict.FormReturnsMatrix(symbols);
            // Calculate rate of returns
            var rreturns = returns.Apply(e => Math.Pow(1.0 + e, 252.0) - 1.0);
            // Calculate geometric mean of rate of returns
            var gmean = Enumerable.Range(0, rreturns.GetLength(1))
                .Select(i => rreturns.GetColumn(i))
                .Select(c => Math.Pow(Elementwise.Add(c, 1.0).Product(), 1.0 / c.Length) - 1.0)
                .ToArray();

            // The optimization method processes the data frame
            var W = _optimizer.Optimize(rreturns); //, gmean);

            // process results
            if (W.Length > 0)
            {
                int sidx = 0;
                foreach (var symbol in symbols)
                {
                    var weight = W[sidx].SafeDecimalCast();

                    var target = PortfolioTarget.Percent(algorithm, symbol, weight);
                    if (target != null)
                    {
                        targets.Add(target);
                    }

                    sidx++;
                }
            }

            return targets;
        }

        /// <summary>
        /// Event fired each time the we add/remove securities from the data feed
        /// </summary>
        /// <param name="algorithm">The algorithm instance that experienced the change in securities</param>
        /// <param name="changes">The security additions and removals from the algorithm</param>
        public override void OnSecuritiesChanged(QCAlgorithm algorithm, SecurityChanges changes)
        {
            // clean up data for removed securities
            foreach (var removed in changes.RemovedSecurities)
            {
                _pendingRemoval.Add(removed.Symbol);
                ReturnsSymbolData data;
                if (_symbolDataDict.TryGetValue(removed.Symbol, out data))
                {
                    _symbolDataDict.Remove(removed.Symbol);
                    data.Reset();
                }
            }

            // initialize data for added securities
            var addedSymbols = new List<Symbol>();
            foreach (var added in changes.AddedSecurities)
            {
                if (!_symbolDataDict.ContainsKey(added.Symbol))
                {
                    var symbolData = new ReturnsSymbolData(added.Symbol, _lookback, _period);
                    _symbolDataDict[added.Symbol] = symbolData;
                    addedSymbols.Add(added.Symbol);
                }
            }
            if (addedSymbols.Count == 0)
                return;

            // warmup our indicators by pushing history through the consolidators
            algorithm.History(addedSymbols, _lookback * _period, _resolution)
                .PushThrough(bar =>
                {
                    ReturnsSymbolData symbolData;
                    if (_symbolDataDict.TryGetValue(bar.Symbol, out symbolData))
                    {
                        symbolData.Update(bar.EndTime, bar.Value);
                    }
                });
        }
    }
}
