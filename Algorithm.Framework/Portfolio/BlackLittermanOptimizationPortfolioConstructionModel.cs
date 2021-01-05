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
using System;
using System.Collections.Generic;
using System.Linq;
using Accord.Statistics;
using Accord.Math;
using Python.Runtime;
using QuantConnect.Scheduling;

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
    public class BlackLittermanOptimizationPortfolioConstructionModel : PortfolioConstructionModel
    {
        private readonly IPortfolioOptimizer _optimizer;
        private readonly PortfolioBias _portfolioBias;
        private readonly Resolution _resolution;
        private readonly double _riskFreeRate;
        private readonly double _delta;
        private readonly int _lookback;
        private readonly double _tau;
        private readonly int _period;

        private readonly Dictionary<Symbol, ReturnsSymbolData> _symbolDataDict;

        /// <summary>
        /// Initialize the model
        /// </summary>
        /// <param name="timeSpan">Rebalancing frequency</param>
        /// <param name="portfolioBias">Specifies the bias of the portfolio (Short, Long/Short, Long)</param>
        /// <param name="lookback">Historical return lookback period</param>
        /// <param name="period">The time interval of history price to calculate the weight</param>
        /// <param name="resolution">The resolution of the history price</param>
        /// <param name="riskFreeRate">The risk free rate</param>
        /// <param name="delta">The risk aversion coeffficient of the market portfolio</param>
        /// <param name="tau">The model parameter indicating the uncertainty of the CAPM prior</param>
        /// <param name="optimizer">The portfolio optimization algorithm. If no algorithm is explicitly provided then the default will be max Sharpe ratio optimization.</param>
        public BlackLittermanOptimizationPortfolioConstructionModel(TimeSpan timeSpan,
            PortfolioBias portfolioBias = PortfolioBias.LongShort,
            int lookback = 1,
            int period = 63,
            Resolution resolution = Resolution.Daily,
            double riskFreeRate = 0.0,
            double delta = 2.5,
            double tau = 0.05,
            IPortfolioOptimizer optimizer = null)
            : this(dt => dt.Add(timeSpan), portfolioBias, lookback, period, resolution, riskFreeRate, delta, tau, optimizer)
        {
        }

        /// <summary>
        /// Initialize the model
        /// </summary>
        /// <param name="rebalanceResolution">Rebalancing frequency</param>
        /// <param name="portfolioBias">Specifies the bias of the portfolio (Short, Long/Short, Long)</param>
        /// <param name="lookback">Historical return lookback period</param>
        /// <param name="period">The time interval of history price to calculate the weight</param>
        /// <param name="resolution">The resolution of the history price</param>
        /// <param name="riskFreeRate">The risk free rate</param>
        /// <param name="delta">The risk aversion coeffficient of the market portfolio</param>
        /// <param name="tau">The model parameter indicating the uncertainty of the CAPM prior</param>
        /// <param name="optimizer">The portfolio optimization algorithm. If no algorithm is explicitly provided then the default will be max Sharpe ratio optimization.</param>
        public BlackLittermanOptimizationPortfolioConstructionModel(Resolution rebalanceResolution = Resolution.Daily,
            PortfolioBias portfolioBias = PortfolioBias.LongShort,
            int lookback = 1,
            int period = 63,
            Resolution resolution = Resolution.Daily,
            double riskFreeRate = 0.0,
            double delta = 2.5,
            double tau = 0.05,
            IPortfolioOptimizer optimizer = null)
            : this(rebalanceResolution.ToTimeSpan(), portfolioBias, lookback, period, resolution, riskFreeRate, delta, tau, optimizer)
        {
        }

        /// <summary>
        /// Initialize the model
        /// </summary>
        /// <param name="rebalancingFunc">For a given algorithm UTC DateTime returns the next expected rebalance UTC time.
        /// Returning current time will trigger rebalance. If null will be ignored</param>
        /// <param name="portfolioBias">Specifies the bias of the portfolio (Short, Long/Short, Long)</param>
        /// <param name="lookback">Historical return lookback period</param>
        /// <param name="period">The time interval of history price to calculate the weight</param>
        /// <param name="resolution">The resolution of the history price</param>
        /// <param name="riskFreeRate">The risk free rate</param>
        /// <param name="delta">The risk aversion coeffficient of the market portfolio</param>
        /// <param name="tau">The model parameter indicating the uncertainty of the CAPM prior</param>
        /// <param name="optimizer">The portfolio optimization algorithm. If no algorithm is explicitly provided then the default will be max Sharpe ratio optimization.</param>
        public BlackLittermanOptimizationPortfolioConstructionModel(Func<DateTime, DateTime> rebalancingFunc,
            PortfolioBias portfolioBias = PortfolioBias.LongShort,
            int lookback = 1,
            int period = 63,
            Resolution resolution = Resolution.Daily,
            double riskFreeRate = 0.0,
            double delta = 2.5,
            double tau = 0.05,
            IPortfolioOptimizer optimizer = null)
            : this(rebalancingFunc != null ? (Func<DateTime, DateTime?>)(timeUtc => rebalancingFunc(timeUtc)) : null,
                portfolioBias,
                lookback,
                period,
                resolution,
                riskFreeRate,
                delta,
                tau,
                optimizer)
        {
        }

        /// <summary>
        /// Initialize the model
        /// </summary>
        /// <param name="rebalancingDateRules">The date rules used to define the next expected rebalance time
        /// in UTC</param>
        /// <param name="portfolioBias">Specifies the bias of the portfolio (Short, Long/Short, Long)</param>
        /// <param name="lookback">Historical return lookback period</param>
        /// <param name="period">The time interval of history price to calculate the weight</param>
        /// <param name="resolution">The resolution of the history price</param>
        /// <param name="riskFreeRate">The risk free rate</param>
        /// <param name="delta">The risk aversion coeffficient of the market portfolio</param>
        /// <param name="tau">The model parameter indicating the uncertainty of the CAPM prior</param>
        /// <param name="optimizer">The portfolio optimization algorithm. If no algorithm is explicitly provided then the default will be max Sharpe ratio optimization.</param>
        public BlackLittermanOptimizationPortfolioConstructionModel(IDateRule rebalancingDateRules,
            PortfolioBias portfolioBias = PortfolioBias.LongShort,
            int lookback = 1,
            int period = 63,
            Resolution resolution = Resolution.Daily,
            double riskFreeRate = 0.0,
            double delta = 2.5,
            double tau = 0.05,
            IPortfolioOptimizer optimizer = null)
            : this(rebalancingDateRules.ToFunc(), portfolioBias, lookback, period, resolution, riskFreeRate, delta, tau, optimizer)
        {
        }

        /// <summary>
        /// Initialize the model
        /// </summary>
        /// <param name="rebalance">Rebalancing func or if a date rule, timedelta will be converted into func.
        /// For a given algorithm UTC DateTime the func returns the next expected rebalance time
        /// or null if unknown, in which case the function will be called again in the next loop. Returning current time
        /// will trigger rebalance. If null will be ignored</param>
        /// <param name="portfolioBias">Specifies the bias of the portfolio (Short, Long/Short, Long)</param>
        /// <param name="lookback">Historical return lookback period</param>
        /// <param name="period">The time interval of history price to calculate the weight</param>
        /// <param name="resolution">The resolution of the history price</param>
        /// <param name="riskFreeRate">The risk free rate</param>
        /// <param name="delta">The risk aversion coeffficient of the market portfolio</param>
        /// <param name="tau">The model parameter indicating the uncertainty of the CAPM prior</param>
        /// <param name="optimizer">The portfolio optimization algorithm. If no algorithm is explicitly provided then the default will be max Sharpe ratio optimization.</param>
        /// <remarks>This is required since python net can not convert python methods into func nor resolve the correct
        /// constructor for the date rules parameter.
        /// For performance we prefer python algorithms using the C# implementation</remarks>
        public BlackLittermanOptimizationPortfolioConstructionModel(PyObject rebalance,
            PortfolioBias portfolioBias = PortfolioBias.LongShort,
            int lookback = 1,
            int period = 63,
            Resolution resolution = Resolution.Daily,
            double riskFreeRate = 0.0,
            double delta = 2.5,
            double tau = 0.05,
            IPortfolioOptimizer optimizer = null)
            : this((Func<DateTime, DateTime?>)null, portfolioBias, lookback, period, resolution, riskFreeRate, delta, tau, optimizer)
        {
            SetRebalancingFunc(rebalance);
        }

        /// <summary>
        /// Initialize the model
        /// </summary>
        /// <param name="rebalancingFunc">For a given algorithm UTC DateTime returns the next expected rebalance time
        /// or null if unknown, in which case the function will be called again in the next loop. Returning current time
        /// will trigger rebalance.</param>
        /// <param name="portfolioBias">Specifies the bias of the portfolio (Short, Long/Short, Long)</param>
        /// <param name="lookback">Historical return lookback period</param>
        /// <param name="period">The time interval of history price to calculate the weight</param>
        /// <param name="resolution">The resolution of the history price</param>
        /// <param name="riskFreeRate">The risk free rate</param>
        /// <param name="delta">The risk aversion coeffficient of the market portfolio</param>
        /// <param name="tau">The model parameter indicating the uncertainty of the CAPM prior</param>
        /// <param name="optimizer">The portfolio optimization algorithm. If no algorithm is explicitly provided then the default will be max Sharpe ratio optimization.</param>
        public BlackLittermanOptimizationPortfolioConstructionModel(Func<DateTime, DateTime?> rebalancingFunc,
            PortfolioBias portfolioBias = PortfolioBias.LongShort,
            int lookback = 1,
            int period = 63,
            Resolution resolution = Resolution.Daily,
            double riskFreeRate = 0.0,
            double delta = 2.5,
            double tau = 0.05,
            IPortfolioOptimizer optimizer = null)
            : base(rebalancingFunc)
        {
            _lookback = lookback;
            _period = period;
            _resolution = resolution;
            _riskFreeRate = riskFreeRate;
            _delta = delta;
            _tau = tau;

            var lower = portfolioBias == PortfolioBias.Long ? 0 : -1;
            var upper = portfolioBias == PortfolioBias.Short ? 0 : 1;
            _optimizer = optimizer ?? new MaximumSharpeRatioPortfolioOptimizer(lower, upper, riskFreeRate);
            _portfolioBias = portfolioBias;
            _symbolDataDict = new Dictionary<Symbol, ReturnsSymbolData>();
        }

        /// <summary>
        /// Method that will determine if the portfolio construction model should create a
        /// target for this insight
        /// </summary>
        /// <param name="insight">The insight to create a target for</param>
        /// <returns>True if the portfolio should create a target for the insight</returns>
        protected override bool ShouldCreateTargetForInsight(Insight insight)
        {
            return FilterInvalidInsightMagnitude(Algorithm, new []{ insight }).Length != 0;
        }

        /// <summary>
        /// Will determine the target percent for each insight
        /// </summary>
        /// <param name="lastActiveInsights">The active insights to generate a target for</param>
        /// <returns>A target percent for each insight</returns>
        protected override Dictionary<Insight, double> DetermineTargetPercent(List<Insight> lastActiveInsights)
        {
            var targets = new Dictionary<Insight, double>();

            double[,] P;
            double[] Q;
            if (TryGetViews(lastActiveInsights, out P, out Q))
            {
                // Updates the ReturnsSymbolData with insights
                foreach (var insight in lastActiveInsights)
                {
                    ReturnsSymbolData symbolData;
                    if (_symbolDataDict.TryGetValue(insight.Symbol, out symbolData))
                    {
                        if (insight.Magnitude == null)
                        {
                            Algorithm.SetRunTimeError(new ArgumentNullException("BlackLittermanOptimizationPortfolioConstructionModel does not accept \'null\' as Insight.Magnitude. Please make sure your Alpha Model is generating Insights with the Magnitude property set."));
                            return targets;
                        }
                        symbolData.Add(insight.GeneratedTimeUtc, insight.Magnitude.Value.SafeDecimalCast());
                    }
                }
                // Get symbols' returns
                var symbols = lastActiveInsights.Select(x => x.Symbol).Distinct().ToList();
                var returns = _symbolDataDict.FormReturnsMatrix(symbols);

                // Calculate posterior estimate of the mean and uncertainty in the mean
                double[,] Σ;
                var Π = GetEquilibriumReturns(returns, out Σ);

                ApplyBlackLittermanMasterFormula(ref Π, ref Σ, P, Q);

                // Create portfolio targets from the specified insights
                var W = _optimizer.Optimize(returns, Π, Σ);
                var sidx = 0;
                foreach (var symbol in symbols)
                {
                    var weight = W[sidx];

                    // don't trust the optimizer
                    if (_portfolioBias != PortfolioBias.LongShort
                        && Math.Sign(weight) != (int)_portfolioBias)
                    {
                        weight = 0;
                    }
                    targets[lastActiveInsights.First(insight => insight.Symbol == symbol)] = weight;

                    sidx++;
                }
            }

            return targets;
        }

        /// <summary>
        /// Gets the target insights to calculate a portfolio target percent for
        /// </summary>
        /// <returns>An enumerable of the target insights</returns>
        protected override List<Insight> GetTargetInsights()
        {
            // Get insight that haven't expired of each symbol that is still in the universe
            var activeInsights = InsightCollection.GetActiveInsights(Algorithm.UtcTime);

            // Get the last generated active insight for each symbol
            return (from insight in activeInsights
                    group insight by new { insight.Symbol, insight.SourceModel } into g
                    select g.OrderBy(x => x.GeneratedTimeUtc).Last())
                    .OrderBy(x => x.Symbol).ToList();
        }

        /// <summary>
        /// Event fired each time the we add/remove securities from the data feed
        /// </summary>
        /// <param name="algorithm">The algorithm instance that experienced the change in securities</param>
        /// <param name="changes">The security additions and removals from the algorithm</param>
        public override void OnSecuritiesChanged(QCAlgorithm algorithm, SecurityChanges changes)
        {
            base.OnSecuritiesChanged(algorithm, changes);

            foreach (var symbol in changes.RemovedSecurities.Select(x => x.Symbol))
            {
                if (_symbolDataDict.ContainsKey(symbol))
                {
                    _symbolDataDict[symbol].Reset();
                    _symbolDataDict.Remove(symbol);
                }
            }

            // initialize data for added securities
            var addedSymbols = changes.AddedSecurities.ToDictionary(x => x.Symbol, x => x.Exchange.TimeZone);
            algorithm.History(addedSymbols.Keys, _lookback * _period, _resolution)
                .PushThrough(bar =>
                {
                    ReturnsSymbolData symbolData;
                    if (!_symbolDataDict.TryGetValue(bar.Symbol, out symbolData))
                    {
                        symbolData = new ReturnsSymbolData(bar.Symbol, _lookback, _period);
                        _symbolDataDict.Add(bar.Symbol, symbolData);
                    }
                    // Convert the data timestamp to UTC
                    var utcTime = bar.EndTime.ConvertToUtc(addedSymbols[bar.Symbol]);
                    symbolData.Update(utcTime, bar.Value);
                });
        }

        /// <summary>
        /// Calculate equilibrium returns and covariance
        /// </summary>
        /// <param name="returns">Matrix of returns where each column represents a security and each row returns for the given date/time (size: K x N)</param>
        /// <param name="Σ">Multi-dimensional array of double with the portfolio covariance of returns (size: K x K).</param>
        /// <returns>Array of double of equilibrium returns</returns>
        public virtual double[] GetEquilibriumReturns(double[,] returns, out double[,] Σ)
        {
            // equal weighting scheme
            var W = Vector.Create(returns.GetLength(1), 1.0 / returns.GetLength(1));
            // annualized covariance
            Σ = returns.Covariance().Multiply(252);
            //annualized return
            var annualReturn = W.Dot(Elementwise.Add(returns.Mean(0), 1.0).Pow(252.0).Subtract(1.0));
            //annualized variance of return
            var annualVariance = W.Dot(Σ.Dot(W));
            // the risk aversion coefficient
            var riskAversion = (annualReturn - _riskFreeRate) / annualVariance;
            // the implied excess equilibrium return Vector (N x 1 column vector)
            return Σ.Dot(W).Multiply(riskAversion);
        }

        /// <summary>
        /// Generate views from multiple alpha models
        /// </summary>
        /// <param name="insights">Array of insight that represent the investors' views</param>
        /// <param name="P">A matrix that identifies the assets involved in the views (size: K x N)</param>
        /// <param name="Q">A view vector (size: K x 1)</param>
        private bool TryGetViews(ICollection<Insight> insights, out double[,] P, out double[] Q)
        {
            try
            {
                var tmpQ = insights.GroupBy(insight => insight.SourceModel)
                    .Select(values =>
                    {
                        var upInsightsSum = values.Where(i => i.Direction == InsightDirection.Up).Sum(i => Math.Abs(i.Magnitude.Value));
                        var dnInsightsSum = values.Where(i => i.Direction == InsightDirection.Down).Sum(i => Math.Abs(i.Magnitude.Value));
                        return new { View = values.Key, Q = upInsightsSum > dnInsightsSum ? upInsightsSum : dnInsightsSum };
                    })
                    .Where(x => x.Q != 0)
                    .ToDictionary(k => k.View, v => v.Q);

                var tmpP = insights.GroupBy(insight => insight.SourceModel)
                    .Select(values =>
                    {
                        var q = tmpQ[values.Key];
                        var results = values.ToDictionary(x => x.Symbol, insight =>
                        {
                            var value = (int)insight.Direction * Math.Abs(insight.Magnitude.Value);
                            return value / q;
                        });
                        // Add zero for other symbols that are listed but active insight
                        foreach (var symbol in _symbolDataDict.Keys)
                        {
                            if (!results.ContainsKey(symbol))
                            {
                                results.Add(symbol, 0d);
                            }
                        }
                        return new { View = values.Key, Results = results };
                    })
                    .Where(r => !r.Results.Select(v => Math.Abs(v.Value)).Sum().IsNaNOrZero())
                    .ToDictionary(k => k.View, v => v.Results);

                P = Matrix.Create(tmpP.Select(d => d.Value.Values.ToArray()).ToArray());
                Q = tmpQ.Values.ToArray();
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
        /// Apply Black-Litterman master formula
        /// http://www.blacklitterman.org/cookbook.html
        /// </summary>
        /// <param name="Π">Prior/Posterior mean array</param>
        /// <param name="Σ">Prior/Posterior covariance matrix</param>
        /// <param name="P">A matrix that identifies the assets involved in the views (size: K x N)</param>
        /// <param name="Q">A view vector (size: K x 1)</param>
        private void ApplyBlackLittermanMasterFormula(ref double[] Π, ref double[,] Σ, double[,] P, double[] Q)
        {
            // Create the diagonal covariance matrix of error terms from the expressed views
            var eye = Matrix.Diagonal(Q.GetLength(0), 1);
            var Ω = Elementwise.Multiply(P.Dot(Σ).DotWithTransposed(P).Multiply(_tau), eye);
            if (Ω.Determinant() != 0)
            {
                // Define matrices Στ and A to avoid recalculations
                var Στ = Σ.Multiply(_tau);
                var A = Στ.DotWithTransposed(P).Dot(P.Dot(Στ).DotWithTransposed(P).Add(Ω).Inverse());

                // Compute posterior estimate of the mean: Black-Litterman "master equation"
                Π = Π.Add(A.Dot(Q.Subtract(P.Dot(Π))));

                // Compute posterior estimate of the uncertainty in the mean
                var M = Στ.Subtract(A.Dot(P).Dot(Στ));
                Σ = Σ.Add(M).Multiply(_delta);

                // Compute posterior weights based on uncertainty in mean
                var W = Π.Dot(Σ.Inverse());
            }
        }
    }
}