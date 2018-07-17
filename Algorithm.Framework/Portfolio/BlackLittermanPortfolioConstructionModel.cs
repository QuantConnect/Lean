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
using Accord.Math.Optimization;

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
        private readonly Resolution _resolution;
        private readonly double _minimumWeight;
        private readonly double _maximumWeight;
        private readonly double _riskFreeRate;
        private readonly double _tau;
        private readonly Optimization.IPortfolioOptimization _optimization;

        private readonly List<Symbol> _pendingRemoval;
        private readonly Dictionary<Symbol, ReturnsSymbolData> _symbolDataDict;

        /// <summary>
        /// Initialize the model
        /// </summary>
        /// <param name="lookback">Historical return lookback period</param>
        /// <param name="resolution">The resolution of the history price</param>
        /// <param name="minimumWeight">The lower bounds on portfolio weights</param>
        /// <param name="maximumWeight">The upper bounds on portfolio weights</param>
        /// <param name="riskFreeRate">The risk free rate</param>
        /// <param name="tau">The model parameter indicating the uncertainty of the CAPM prior</param>
        public BlackLittermanPortfolioConstructionModel(
            Optimization.IPortfolioOptimization optimization,
            int lookback = 5,
            Resolution resolution = Resolution.Daily,
            double minimumWeight = -1.0,
            double maximumWeight = 1.0,
            double riskFreeRate = 0.0,
            double tau = 0.025
            )
        {
            _lookback = lookback;
            _resolution = resolution;
            _minimumWeight = minimumWeight;
            _maximumWeight = maximumWeight;
            _riskFreeRate = riskFreeRate;
            _tau = tau;
            _optimization = optimization;

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

            // Get symbols' returns
            var returns = GetReturns(from s in symbols join sd in _symbolDataDict on s equals sd.Key select sd.Value.Returns);

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
                double[,] matrix = Matrix.Diagonal<double>(P.Dot(Σ).DotWithTransposed(P).Multiply(this._tau).Diagonal<double>());
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
            double[] W;
            _optimization.SetCovariance(Σ);
            _optimization.SetBounds(_minimumWeight, _maximumWeight);
            var ret = _optimization.Optimize(out W, expectedReturns: Π);

            /// process results
            if (ret > 0)
            {
                var weights = symbols.Zip(W, (sym, w) => new { S = sym, W = w }).ToDictionary(r => r.S, r => r.W);
                algorithm.Debug(" ### [" + string.Join(",", weights.Keys) + "] = [" + string.Join(",", weights.Values) + "]");

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
            var addedSymbols = new List<Symbol>();
            foreach (var added in changes.AddedSecurities)
            {
                if (!_symbolDataDict.ContainsKey(added.Symbol))
                {
                    var symbolData = new ReturnsSymbolData(algorithm, added.Symbol, _lookback, _resolution);
                    _symbolDataDict[added.Symbol] = symbolData;
                    addedSymbols.Add(symbolData.Symbol);
                }
            }

            if (addedSymbols.Count <= 0)
                return;

            // warmup our indicators by pushing history through the consolidators
            algorithm.History(addedSymbols, _lookback, _resolution)
                .PushThrough(bar =>
                {
                    ReturnsSymbolData symbolData;
                    if (_symbolDataDict.TryGetValue(bar.Symbol, out symbolData))
                    {
                        symbolData.ROC.Update(bar.EndTime, bar.Value);
                        if (symbolData.IsReady)
                        {
                            var values = symbolData.Window.Select(x => x.Value).ToArray();
                            algorithm.Log(" ### " + symbolData.Symbol.Value + string.Join(",", values));
                        }
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

        /// <summary>
        /// Calculate implied equilibrium returns
        /// </summary>
        /// <param name="covariance">Annualized covariance matrix</param>
        /// <returns>Vector of implied equilibrium returns</returns>
        private double[] GetEquilibriumReturn(out double[,] covariance)
        {
            var matrix = Matrix.Create(_symbolDataDict.Select(kvp => kvp.Value.Window.Select(x => (double)x.Value).ToArray()).ToArray());

            covariance = Measures.Covariance(matrix).Multiply(252);

            // equal weighting scheme
            var count = _symbolDataDict.Count;
            var W = Vector.Create(count, 1.0/count);

            // annualized return
            var annualReturn = Math.Pow(1 + W.Dot(matrix.Mean(1)), 252) - 1;

            //annualized variance of return
            var annualVariance = W.Dot(covariance.Dot(W));

            // the risk aversion coefficient
            var riskAversion = (annualReturn - _riskFreeRate) / annualVariance;

            // the implied excess equilibrium return Vector (N x 1 column vector)
            return  covariance.Dot(W).Multiply(riskAversion);
        }     
    }
}
