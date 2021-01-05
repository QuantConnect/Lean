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
using Python.Runtime;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Data;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Scheduling;

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
        private readonly PortfolioBias _portfolioBias;
        private readonly IPortfolioOptimizer _optimizer;
        private readonly Dictionary<Symbol, ReturnsSymbolData> _symbolDataDict;

        /// <summary>
        /// Initialize the model
        /// </summary>
        /// <param name="rebalancingDateRules">The date rules used to define the next expected rebalance time
        /// in UTC</param>
        /// <param name="portfolioBias">Specifies the bias of the portfolio (Short, Long/Short, Long)</param>
        /// <param name="lookback">Historical return lookback period</param>
        /// <param name="period">The time interval of history price to calculate the weight</param>
        /// <param name="resolution">The resolution of the history price</param>
        /// <param name="targetReturn">The target portfolio return</param>
        /// <param name="optimizer">The portfolio optimization algorithm. If the algorithm is not provided then the default will be mean-variance optimization.</param>
        public MeanVarianceOptimizationPortfolioConstructionModel(IDateRule rebalancingDateRules,
            PortfolioBias portfolioBias = PortfolioBias.LongShort,
            int lookback = 1,
            int period = 63,
            Resolution resolution = Resolution.Daily,
            double targetReturn = 0.02,
            IPortfolioOptimizer optimizer = null)
            : this(rebalancingDateRules.ToFunc(), portfolioBias, lookback, period, resolution, targetReturn, optimizer)
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
        /// <param name="targetReturn">The target portfolio return</param>
        /// <param name="optimizer">The portfolio optimization algorithm. If the algorithm is not provided then the default will be mean-variance optimization.</param>
        public MeanVarianceOptimizationPortfolioConstructionModel(Resolution rebalanceResolution = Resolution.Daily,
            PortfolioBias portfolioBias = PortfolioBias.LongShort,
            int lookback = 1,
            int period = 63,
            Resolution resolution = Resolution.Daily,
            double targetReturn = 0.02,
            IPortfolioOptimizer optimizer = null)
            : this(rebalanceResolution.ToTimeSpan(), portfolioBias, lookback, period, resolution, targetReturn, optimizer)
        {
        }

        /// <summary>
        /// Initialize the model
        /// </summary>
        /// <param name="timeSpan">Rebalancing frequency</param>
        /// <param name="portfolioBias">Specifies the bias of the portfolio (Short, Long/Short, Long)</param>
        /// <param name="lookback">Historical return lookback period</param>
        /// <param name="period">The time interval of history price to calculate the weight</param>
        /// <param name="resolution">The resolution of the history price</param>
        /// <param name="targetReturn">The target portfolio return</param>
        /// <param name="optimizer">The portfolio optimization algorithm. If the algorithm is not provided then the default will be mean-variance optimization.</param>
        public MeanVarianceOptimizationPortfolioConstructionModel(TimeSpan timeSpan,
            PortfolioBias portfolioBias = PortfolioBias.LongShort,
            int lookback = 1,
            int period = 63,
            Resolution resolution = Resolution.Daily,
            double targetReturn = 0.02,
            IPortfolioOptimizer optimizer = null)
            : this(dt => dt.Add(timeSpan), portfolioBias, lookback, period, resolution, targetReturn, optimizer)
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
        /// <param name="targetReturn">The target portfolio return</param>
        /// <param name="optimizer">The portfolio optimization algorithm. If the algorithm is not provided then the default will be mean-variance optimization.</param>
        /// <remarks>This is required since python net can not convert python methods into func nor resolve the correct
        /// constructor for the date rules parameter.
        /// For performance we prefer python algorithms using the C# implementation</remarks>
        public MeanVarianceOptimizationPortfolioConstructionModel(PyObject rebalance,
            PortfolioBias portfolioBias = PortfolioBias.LongShort,
            int lookback = 1,
            int period = 63,
            Resolution resolution = Resolution.Daily,
            double targetReturn = 0.02,
            IPortfolioOptimizer optimizer = null)
            : this((Func<DateTime, DateTime?>)null, portfolioBias, lookback, period, resolution, targetReturn, optimizer)
        {
            SetRebalancingFunc(rebalance);
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
        /// <param name="targetReturn">The target portfolio return</param>
        /// <param name="optimizer">The portfolio optimization algorithm. If the algorithm is not provided then the default will be mean-variance optimization.</param>
        public MeanVarianceOptimizationPortfolioConstructionModel(Func<DateTime, DateTime> rebalancingFunc,
            PortfolioBias portfolioBias = PortfolioBias.LongShort,
            int lookback = 1,
            int period = 63,
            Resolution resolution = Resolution.Daily,
            double targetReturn = 0.02,
            IPortfolioOptimizer optimizer = null)
            : this(rebalancingFunc != null ? (Func<DateTime, DateTime?>)(timeUtc => rebalancingFunc(timeUtc)) : null,
                portfolioBias,
                lookback,
                period,
                resolution,
                targetReturn,
                optimizer)
        {
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
        /// <param name="targetReturn">The target portfolio return</param>
        /// <param name="optimizer">The portfolio optimization algorithm. If the algorithm is not provided then the default will be mean-variance optimization.</param>
        public MeanVarianceOptimizationPortfolioConstructionModel(Func<DateTime, DateTime?> rebalancingFunc,
            PortfolioBias portfolioBias = PortfolioBias.LongShort,
            int lookback = 1,
            int period = 63,
            Resolution resolution = Resolution.Daily,
            double targetReturn = 0.02,
            IPortfolioOptimizer optimizer = null)
            : base(rebalancingFunc)
        {
            _lookback = lookback;
            _period = period;
            _resolution = resolution;
            _portfolioBias = portfolioBias;

            var lower = portfolioBias == PortfolioBias.Long ? 0 : -1;
            var upper = portfolioBias == PortfolioBias.Short ? 0 : 1;
            _optimizer = optimizer ?? new MinimumVariancePortfolioOptimizer(lower, upper, targetReturn);

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
            var filteredInsight = FilterInvalidInsightMagnitude(Algorithm, new[] { insight }).FirstOrDefault();
            if (filteredInsight == null)
            {
                return false;
            }

            ReturnsSymbolData data;
            if (_symbolDataDict.TryGetValue(insight.Symbol, out data))
            {
                if (!insight.Magnitude.HasValue)
                {
                    Algorithm.SetRunTimeError(
                        new ArgumentNullException(
                            insight.Symbol.Value,
                            "MeanVarianceOptimizationPortfolioConstructionModel does not accept 'null' as Insight.Magnitude. " +
                            "Please checkout the selected Alpha Model specifications: " + insight.SourceModel));
                    return false;
                }
                data.Add(Algorithm.Time, insight.Magnitude.Value.SafeDecimalCast());
            }

            return true;
        }

        /// <summary>
        /// Will determine the target percent for each insight
        /// </summary>
        /// <param name="activeInsights">The active insights to generate a target for</param>
        /// <returns>A target percent for each insight</returns>
        protected override Dictionary<Insight, double> DetermineTargetPercent(List<Insight> activeInsights)
        {
            var targets = new Dictionary<Insight, double>();

            var symbols = activeInsights.Select(x => x.Symbol).ToList();

            // Get symbols' returns
            var returns = _symbolDataDict.FormReturnsMatrix(symbols);

            // Calculate rate of returns
            var rreturns = returns.Apply(e => Math.Pow(1.0 + e, 252.0) - 1.0);

            // The optimization method processes the data frame
            var w = _optimizer.Optimize(rreturns);

            // process results
            if (w.Length > 0)
            {
                var sidx = 0;
                foreach (var symbol in symbols)
                {
                    var weight = w[sidx];

                    // don't trust the optimizer
                    if (_portfolioBias != PortfolioBias.LongShort
                        && Math.Sign(weight) != (int)_portfolioBias)
                    {
                        weight = 0;
                    }

                    targets[activeInsights.First(insight => insight.Symbol == symbol)] = weight;

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
            base.OnSecuritiesChanged(algorithm, changes);
            // clean up data for removed securities
            foreach (var removed in changes.RemovedSecurities)
            {
                ReturnsSymbolData data;
                if (_symbolDataDict.TryGetValue(removed.Symbol, out data))
                {
                    _symbolDataDict.Remove(removed.Symbol);
                }
            }

            if (changes.AddedSecurities.Count == 0)
                return;

            // initialize data for added securities
            foreach (var added in changes.AddedSecurities)
            {
                if (!_symbolDataDict.ContainsKey(added.Symbol))
                {
                    var symbolData = new ReturnsSymbolData(added.Symbol, _lookback, _period);
                    _symbolDataDict[added.Symbol] = symbolData;
                }
            }

            // warmup our indicators by pushing history through the consolidators
            algorithm.History(changes.AddedSecurities.Select(security => security.Symbol), _lookback * _period, _resolution)
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
