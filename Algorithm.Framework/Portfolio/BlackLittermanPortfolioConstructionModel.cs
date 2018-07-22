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

using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Data;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Securities;
using System;
using System.Collections.Generic;
using System.Linq;
using Accord.Statistics;
using QuantConnect.Util;
using Accord.Math;

namespace QuantConnect.Algorithm.Framework.Portfolio
{
    /// <summary>
    /// Provides an implementation of Black-Litterman portfolio optimization. The model adjusts equilibrium market
    /// returns by incorporating views from multiple alpha models and therefore to get the optimal risky portfolio
    /// reflecting those views. If insights of all alpha models have None magnitude or there are linearly dependent
    /// vectors in link matrix of views, the expected return would be the implied excess equilibrium return.
    /// The interval of weights in optimization method can be changed based on the long-short algorithm.
    /// The default model uses the 0.0025 as weight-on-views scalar parameter tau. The optimization method
    /// maximizes the Sharpe ratio with the weight range from -1 to 1.
    /// </summary>
    public class BlackLittermanPortfolioConstructionModel : PortfolioConstructionModel
    {
        private readonly int _lookback;
        private readonly int _period;
        private readonly Resolution _resolution;
        private readonly double _riskFreeRate;
        private readonly double _tau;
        private readonly IPortfolioOptimizer _optimizer;

        private readonly List<Symbol> _pendingRemoval;
        private readonly Dictionary<Symbol, ReturnsSymbolData> _symbolDataDict;

        /// <summary>
        /// Initialize the model
        /// </summary>
        /// <param name="lookback">Historical return lookback period</param>
        /// <param name="period">The time interval of history price to calculate the weight</param>
        /// <param name="resolution">The resolution of the history price</param>
        /// <param name="riskFreeRate">The risk free rate</param>
        /// <param name="tau">The model parameter indicating the uncertainty of the CAPM prior</param>
        /// <param name="optimizer">The portfolio optimization algorithm. If no algorithm is explicitly provided then the default will be max Sharpe ratio optimization.</param>
        public BlackLittermanPortfolioConstructionModel(
            int lookback = 1,
            int period = 63,
            Resolution resolution = Resolution.Daily,
            double riskFreeRate = 0.0,
            double tau = 0.025,
            IPortfolioOptimizer optimizer = null
            )
        {
            _lookback = lookback;
            _period = period;
            _resolution = resolution;
            _riskFreeRate = riskFreeRate;
            _tau = tau;

            _optimizer = optimizer ?? new MaximumSharpeRatioPortfolioOptimizer(riskFreeRate: riskFreeRate);

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
            if (symbols.Count() == 0 || insights.All(x => x.Magnitude == 0))
            {
                return targets;
            }

            // Get symbols' returns
            var returns = _symbolDataDict.FormReturnsMatrix(symbols);

            // Calculate equilibrium returns
            double[]  Π;
            double[,] Σ;
            GetEquilibriumReturns(returns, out Π, out Σ);

            // Calculate implied equilibrium returns
            double[,] P;
            double[] Q;
            if (TryGetViews(insights, out P, out Q))
            {
                // Create the diagonal covariance matrix of error terms from the expressed views
                var Ω = P.Dot(Σ).DotWithTransposed(P).Multiply(_tau);
                double[,] matrix = Matrix.Diagonal(P.Dot(Σ).DotWithTransposed(P).Multiply(_tau).Diagonal());
                if (Ω.Determinant() != 0)
                {
                    var invCov = Σ.Multiply(_tau).Inverse();
                    var PTomega = P.TransposeAndDot(Ω.Inverse());

                    var A = invCov.Add(PTomega.Dot(P));
                    var B = invCov.Dot(Π).Add(PTomega.Dot(Q));
                    Π = A.Inverse().Dot(B);
                }
            }

            // The optimization method processes the data frame
            var W = _optimizer.Optimize(returns, expectedReturns: Π);

            // Create portfolio targets from the specified insights
            if (W.Length > 0)
            {
                int sidx = 0;
                foreach (var symbol in symbols)
                {
                    var weight = (decimal)W[sidx];
                    targets.Add(PortfolioTarget.Percent(algorithm, symbol, weight));
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
                    data.Reset();
                }
            }

            // initialize data for added securities
            var addedSymbols = new List<Symbol>();
            foreach (var added in changes.AddedSecurities)
            {
                if (!_symbolDataDict.ContainsKey(added.Symbol))
                {
                    var symbolData = new ReturnsSymbolData(algorithm, added.Symbol, _lookback, _period, _resolution);
                    _symbolDataDict[added.Symbol] = symbolData;
                    addedSymbols.Add(symbolData.Symbol);
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
                        symbolData.ROC.Update(bar.EndTime, bar.Value);
                    }
                });
        }

        /// <summary>
        /// Calculate equilibrium returns and convariance
        /// </summary>
        /// <param name="returns">Returns</param>
        /// <param name="Π">Equilibrium returns</param>
        /// <param name="Σ">Covariance</param>
        private void GetEquilibriumReturns(double[,] returns, out double[] Π, out double[,] Σ)
        {
            // equal weighting scheme
            double[] W = Vector.Create(returns.GetLength(1), 1.0 / returns.GetLength(1));
            // annualized covariance
            Σ = returns.Covariance().Multiply(252);
            //annualized return
            double annualReturn = W.Dot(Elementwise.Add(returns.Mean(0), 1.0).Pow(252.0).Subtract(1.0));
            //annualized variance of return
            double annualVariance = W.Dot(Σ.Dot(W));
            // the risk aversion coefficient
            var riskAversion = (annualReturn - _riskFreeRate) / annualVariance;            
            // the implied excess equilibrium return Vector (N x 1 column vector)
            Π = Σ.Dot(W).Multiply(riskAversion);
        }

        /// <summary>
        /// Generate views from multiple alpha models
        /// </summary>
        /// <param name="insights"></param>
        /// <param name="P">a matrix that identifies the assets involved in the views (size: K x N)</param>
        /// <param name="Q">a view vector (size: K x 1)</param>
        /// <returns></returns>
        private bool TryGetViews(Insight[] insights, out double[,] P, out double[] Q)
        {
            try
            {
                var tmpP = insights.GroupBy(insight => insight.SourceModel)
                    .Select(values =>
                    {
                        var results = _symbolDataDict.ToDictionary(x => x.Key, v => 0.0);
                        var upInsightsSum = values.Where(i => i.Direction == InsightDirection.Up).Sum(i => (int)i.Direction);
                        var dnInsightsSum = values.Where(i => i.Direction == InsightDirection.Down).Sum(i => (int)i.Direction);

                        foreach (var insight in values)
                        {
                            var direction = (double)insight.Direction;
                            if (direction == 0) continue;

                            var sum = direction > 0 ? upInsightsSum : -dnInsightsSum;
                            results[insight.Symbol] = direction / sum;
                        }
                        return new { View = values.Key, Results = results };
                    })
                    .Where(r => !r.Results.Select(v => Math.Abs(v.Value)).Sum().IsNaNOrZero())
                    .ToDictionary(k => k.View, v => v.Results);

                var tmpQ = insights.GroupBy(insight => insight.SourceModel)
                    .Select(values =>
                    {
                        var q = 0.0;
                        foreach (var insight in values)
                        {
                            q += tmpP[values.Key][insight.Symbol] * (insight.Magnitude ?? 0.0);
                        }
                        return q;
                    });

                P = Matrix.Create(tmpP.Select(d => d.Value.Values.ToArray()).ToArray());
                Q = tmpQ.ToArray();
            }
            catch
            {
                P = null;
                Q = null;
                return false;
            }
            return true;
        }  
    }
}
