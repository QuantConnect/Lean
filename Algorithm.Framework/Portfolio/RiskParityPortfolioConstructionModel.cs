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
using Accord.Math.Optimization;
using Accord.Math.Random;
using Accord.Statistics;
using Python.Runtime;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Indicators;
using QuantConnect.Scheduling;
using QuantConnect.Util;

namespace QuantConnect.Algorithm.Framework.Portfolio
{
    /// <summary>
    /// Risk Parity Portfolio Construction Model
    /// </summary>
    /// <remarks>Spinu, F. (2013). An algorithm for computing risk parity weights. Available at SSRN 2297383.
    /// Available at https://papers.ssrn.com/sol3/Papers.cfm?abstract_id=2297383</remarks>
    public class RiskParityPortfolioConstructionModel : PortfolioConstructionModel
    {
        private int _lookback;
        private Resolution _resolution;
        private Dictionary<Symbol, RiskParitySymbolData> _symbolData = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="RiskParityPortfolioConstructionModel"/> class
        /// </summary>
        /// <param name="rebalancingDateRules">The date rules used to define the next expected rebalance time
        /// in UTC</param>
        /// <param name="portfolioBias">Specifies the bias of the portfolio (Short, Long/Short, Long)</param>
        /// <param name="lookback">Lookback period for volatility estimation</param>
        /// <param name="resolution">The resolution of the history price and rebalancing</param>
        public RiskParityPortfolioConstructionModel(IDateRule rebalancingDateRules,
            PortfolioBias portfolioBias = PortfolioBias.LongShort,
            int lookback = 20, 
            Resolution resolution = Resolution.Daily)
            : this(rebalancingDateRules.ToFunc(), portfolioBias, lookback, resolution)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RiskParityPortfolioConstructionModel"/> class
        /// </summary>
        /// <param name="rebalanceResolution">Rebalancing frequency</param>
        /// <param name="portfolioBias">Specifies the bias of the portfolio (Short, Long/Short, Long)</param>
        /// <param name="lookback">Lookback period for volatility estimation</param>
        /// <param name="resolution">The resolution of the history price and rebalancing</param>
        public RiskParityPortfolioConstructionModel(Resolution rebalanceResolution = Resolution.Daily,
            PortfolioBias portfolioBias = PortfolioBias.LongShort,
            int lookback = 20, 
            Resolution resolution = Resolution.Daily)
            : this(rebalanceResolution.ToTimeSpan(), portfolioBias, lookback, resolution)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RiskParityPortfolioConstructionModel"/> class
        /// </summary>
        /// <param name="timeSpan">Rebalancing frequency</param>
        /// <param name="portfolioBias">Specifies the bias of the portfolio (Short, Long/Short, Long)</param>
        /// <param name="lookback">Lookback period for volatility estimation</param>
        /// <param name="resolution">The resolution of the history price and rebalancing</param>
        public RiskParityPortfolioConstructionModel(TimeSpan timeSpan,
            PortfolioBias portfolioBias = PortfolioBias.LongShort,
            int lookback = 20, 
            Resolution resolution = Resolution.Daily)
            : this(dt => dt.Add(timeSpan), portfolioBias, lookback, resolution)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RiskParityPortfolioConstructionModel"/> class
        /// </summary>
        /// <param name="rebalance">Rebalancing func or if a date rule, timedelta will be converted into func.
        /// For a given algorithm UTC DateTime the func returns the next expected rebalance time
        /// or null if unknown, in which case the function will be called again in the next loop. Returning current time
        /// will trigger rebalance. If null will be ignored</param>
        /// <param name="portfolioBias">Specifies the bias of the portfolio (Short, Long/Short, Long)</param>
        /// <param name="lookback">Lookback period for volatility estimation</param>
        /// <param name="resolution">The resolution of the history price and rebalancing</param>
        public RiskParityPortfolioConstructionModel(PyObject rebalance,
            PortfolioBias portfolioBias = PortfolioBias.LongShort,
            int lookback = 20, 
            Resolution resolution = Resolution.Daily)
            : this((Func<DateTime, DateTime?>)null, portfolioBias, lookback, resolution)
        {
            SetRebalancingFunc(rebalance);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RiskParityPortfolioConstructionModel"/> class
        /// </summary>
        /// <param name="rebalancingFunc">For a given algorithm UTC DateTime returns the next expected rebalance UTC time.
        /// Returning current time will trigger rebalance. If null will be ignored</param>
        /// will trigger rebalance. If null will be ignored</param>
        /// <param name="portfolioBias">Specifies the bias of the portfolio (Short, Long/Short, Long)</param>
        /// <param name="lookback">Lookback period for volatility estimation</param>
        /// <param name="resolution">The resolution of the history price and rebalancing</param>
        public RiskParityPortfolioConstructionModel(Func<DateTime, DateTime> rebalancingFunc,
            PortfolioBias portfolioBias = PortfolioBias.LongShort,
            int lookback = 20, 
            Resolution resolution = Resolution.Daily)
            : this(rebalancingFunc != null ? (Func<DateTime, DateTime?>)(timeUtc => rebalancingFunc(timeUtc)) : null,
                   portfolioBias, lookback, resolution)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RiskParityPortfolioConstructionModel"/> class
        /// </summary>
        /// <param name="rebalancingFunc">For a given algorithm UTC DateTime returns the next expected rebalance time
        /// or null if unknown, in which case the function will be called again in the next loop. Returning current time
        /// will trigger rebalance.</param>
        /// <param name="portfolioBias">Specifies the bias of the portfolio (Short, Long/Short, Long)</param>
        /// <param name="lookback">Lookback period for volatility estimation</param>
        /// <param name="resolution">The resolution of the history price and rebalancing</param>
        public RiskParityPortfolioConstructionModel(Func<DateTime, DateTime?> rebalancingFunc,
            PortfolioBias portfolioBias = PortfolioBias.LongShort,
            int lookback = 20, 
            Resolution resolution = Resolution.Daily)
            : base(rebalancingFunc)
        {
            Generator.Seed = 0;
            
            if (portfolioBias == PortfolioBias.Short)
            {
                throw new ArgumentException("Long position must be allowed in RiskParityPortfolioConstructionModel.");
            }
            
            _resolution = resolution;
            _lookback = lookback;
        }

        /// <summary>
        /// Will determine the target percent for each insight
        /// </summary>
        /// <param name="activeInsights">list of active insights</param>
        /// <return>dictionary of insight and respective target weight</return>
        protected override Dictionary<Insight, double> DetermineTargetPercent(List<Insight> activeInsights)
        {
            var targets = new Dictionary<Insight, double>();

            // If we have no insights or non-ready just return an empty target list
            if (activeInsights.IsNullOrEmpty() || 
                !activeInsights.All(x => _symbolData[x.Symbol].IsReady()))
            {
                return targets;
            }
        
            // Get the covariance matrix of all activeInsights' symbols
            var numOfAssets = activeInsights.Count;
            var rets = activeInsights.Select(insight => _symbolData[insight.Symbol].Returns.ToArray()).ToArray();
            var cov = Matrix.Transpose(rets).Covariance();

            // Optimization Problem
            // minimize_{w >= 0} 1/2 * x^T S x - b^T log(x)
            // b = 1/num_of_assets (equal budget of risk)
            // dy/dw = Sx - b/x
            // 0 <= w_i <= 1 for all w_i in w
            var budget = Enumerable.Repeat((double) 1/numOfAssets, numOfAssets).ToArray();
            Func<double[], double> objective = (x) => 1/2 * Matrix.Dot(Matrix.Dot(x, cov), x) - Matrix.Dot(budget, Elementwise.Log(x));
            Func<double[], double[]> gradient = (x) => Elementwise.Subtract(Matrix.Dot(cov, x), Elementwise.Divide(budget, x));
            
            // Parameters of optimization
            var lbfgs = new BoundedBroydenFletcherGoldfarbShanno(numberOfVariables: numOfAssets, function: objective, gradient: gradient);
            lbfgs.Corrections = 10;
            lbfgs.MaxIterations = 15000;
            lbfgs.FunctionTolerance = 2.220446049250313e-09;
            lbfgs.GradientTolerance = 1e-05;
            // lbfgs.UpperBounds = Enumerable.Repeat((double) 1, numOfAssets).ToArray();
            // lbfgs.LowerBounds = Enumerable.Repeat((double) 0, numOfAssets).ToArray();

            // Optimize for weights
            lbfgs.Minimize(budget);
            var solution = lbfgs.Solution;
            // Normalize
            var weights = Elementwise.Divide(solution, solution.Sum());

            // Update portfolio state
            for (int i = 0; i < numOfAssets; i++)
            {
                targets.Add(activeInsights[i], weights[i]);
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
            base.OnSecuritiesChanged(algorithm, changes);

            // clean up data for removed securities
            foreach (var removed in changes.RemovedSecurities)
            {
                _symbolData.Remove(removed.Symbol, out var symbolData);
                symbolData.Reset();
            }

            // initialize data for added securities
            var symbols = changes.AddedSecurities.Select(x => x.Symbol);

            foreach(var symbol in symbols)
            {
                if (!_symbolData.ContainsKey(symbol))
                {
                    _symbolData.Add(symbol, new RiskParitySymbolData(algorithm, symbol, _lookback, _resolution));
                }
            }
        }

        private class RiskParitySymbolData
        {
            private RateOfChange _roc;
            public RollingWindow<double> Returns;

            public RiskParitySymbolData(QCAlgorithm algo, Symbol symbol, int lookback, Resolution resolution)
            {
                // Indicator of pct return
                _roc = algo.ROC(symbol, 1, resolution);
                // RollingWindow to save the pct return
                Returns = new RollingWindow<double>(lookback);
                // Update the RollingWindow when new pct change piped
                _roc.Updated += OnROCUpdated;

                // Warmup indicator
                var history = algo.History<TradeBar>(symbol, lookback + 1, resolution);
                foreach (var bar in history)
                {
                    _roc.Update(bar.EndTime, bar.Close);
                }
            }

            private void OnROCUpdated(object _, IndicatorDataPoint updated)
            {
                Returns.Add((double)updated.Value);
            }

            public void Reset()
            {
                _roc.Updated -= OnROCUpdated;
                _roc.Reset();
                Returns.Reset();
            }
            
            public bool IsReady()
            {
                return (_roc.IsReady & Returns.IsReady);
            }
        }
    }
}