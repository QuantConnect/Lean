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
using Accord.Statistics;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Portfolio.Optimization;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Securities;

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
        private readonly double _minimumWeight;
        private readonly double _maximumWeight;
        private readonly double _targetReturn;
        private readonly Optimization.IPortfolioOptimization _optimization;

        private readonly List<Symbol> _pendingRemoval;
        private readonly Dictionary<Symbol, ReturnsSymbolData> _symbolDataDict;

        /// <summary>
        /// Initialize the model
        /// </summary>
        /// <param name="lookback">Historical return lookback period</param>
        /// <param name="resolution">The resolution of the history price</param>
        /// <param name="period">The time interval of history price to calculate the weight</param>
        /// <param name="minimumWeight">The lower bounds on portfolio weights</param>
        /// <param name="maximumWeight">The upper bounds on portfolio weights</param>
        /// <param name="targetReturn">The target portfolio return</param>
        /// <param name="optimization">The portfolio optimization algorithm. If no algorithm is explicitly provided then the default will be mean-variance optimization.</param>
        public MeanVarianceOptimizationPortfolioConstructionModel(
            int lookback = 1,
            int period = 63,
            Resolution resolution = Resolution.Daily,
            double minimumWeight = -1,
            double maximumWeight = 1,
            double targetReturn = 0.02,
            IPortfolioOptimization optimization = null
            )
        {
            _lookback = lookback;
            _period = period;
            _resolution = resolution;
            _minimumWeight = minimumWeight;
            _maximumWeight = maximumWeight;
            _targetReturn = targetReturn;
            
            _optimization = optimization ?? new MeanVariancePortfolio(minimumWeight, maximumWeight);

            _pendingRemoval = new List<Symbol>();
            _symbolDataDict = new Dictionary<Symbol, ReturnsSymbolData>();
        }

        /// <summary>
        /// Create portfolio targets from the specified insights
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="insights">The insights to create portoflio targets from</param>
        /// <returns>An enumerable of portoflio targets to be sent to the execution model</returns>
        public override IEnumerable<IPortfolioTarget> CreateTargets(QCAlgorithmFramework algorithm, Insight[] insights)
        {
            var targets = new List<IPortfolioTarget>();

            // remove pending
            foreach (var symbol in _pendingRemoval)
            {
                targets.Add(new PortfolioTarget(symbol, 0));
            }
            _pendingRemoval.Clear();

            var symbols = insights.Select(x => x.Symbol).Distinct();
            if (symbols.Count() == 0)
            {
                return Enumerable.Empty<IPortfolioTarget>();
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
                    data.Add(algorithm.Time, (decimal)insight.Magnitude.Value);
                }
            }

            // Get symbols' returns
            var returns = _symbolDataDict.FormReturnsMatrix(symbols);
            // Calculate rate of returns
            var rreturns = returns.Apply(e => System.Math.Pow(1.0 + e, 252.0) - 1.0);
            // Calculate geometric mean of rate of returns            
            var gmean = Enumerable.Range(0, rreturns.GetLength(1))
                .Select(i => rreturns.GetColumn(i))
                .Select(c => System.Math.Pow(Elementwise.Add(c, 1.0).Product(), 1.0 / (double)c.Length) - 1.0)
                .ToArray();

            // The optimization method processes the data frame
            double[] W;
            _optimization.SetCovariance(rreturns.Covariance());
            var ret = _optimization.Optimize(out W, expectedReturns: gmean);

            // process results
            if (ret > 0)
            {
                var weights = symbols.Zip(W, (sym, w) => new { S = sym, W = w }).ToDictionary(r => r.S, r => r.W);

                // Create portfolio targets from the specified insights
                foreach (var insight in insights)
                {
                    var weight = (decimal)weights[insight.Symbol];
                    targets.Add(PortfolioTarget.Percent(algorithm, insight.Symbol, weight));
                }
            }
            return targets;
        }

        /// <summary>
        /// Event fired each time the we add/remove securities from the data feed
        /// </summary>
        /// <param name="algorithm">The algorithm instance that experienced the change in securities</param>
        /// <param name="changes">The security additions and removals from the algorithm</param>
        public override void OnSecuritiesChanged(QCAlgorithmFramework algorithm, SecurityChanges changes)
        {
            // clean up data for removed securities
            foreach (var removed in changes.RemovedSecurities)
            {
                _pendingRemoval.Add(removed.Symbol);
                ReturnsSymbolData data;
                if (_symbolDataDict.TryGetValue(removed.Symbol, out data))
                {
                    _symbolDataDict.Remove(removed.Symbol);
                    data.RemoveConsolidators(algorithm);
                }
            }

            // initialize data for added securities
            var addedSymbols = changes.AddedSecurities.Select(s => s.Symbol);
            var history = algorithm.History(addedSymbols, _lookback * _period, _resolution);
            if (history.Count() == 0)
                return;

            foreach (var symbol in addedSymbols)
            {
                if (!_symbolDataDict.ContainsKey(symbol))
                {
                    var symbolData = new ReturnsSymbolData(algorithm, symbol, _lookback * _period, _resolution);
                    symbolData.WarmUpIndicators(history);
                    _symbolDataDict[symbol] = symbolData;
                }
            }            
        }
    }
}
