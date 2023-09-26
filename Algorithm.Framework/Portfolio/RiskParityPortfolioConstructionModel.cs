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
        private readonly int _lookback;
        private readonly int _period;
        private readonly Resolution _resolution;
        private readonly IPortfolioOptimizer _optimizer;
        private readonly Dictionary<Symbol, ReturnsSymbolData> _symbolDataDict;

        /// <summary>
        /// Initialize the model
        /// </summary>
        /// <param name="rebalancingDateRules">The date rules used to define the next expected rebalance time in UTC</param>
        /// <param name="portfolioBias">Specifies the bias of the portfolio (Short, Long/Short, Long)</param>
        /// <param name="lookback">Historical return lookback period</param>
        /// <param name="period">The time interval of history price to calculate the weight</param>
        /// <param name="resolution">The resolution of the history price</param>
        /// <param name="optimizer">The portfolio optimization algorithm. If the algorithm is not provided then the default will be mean-variance optimization.</param>
        public RiskParityPortfolioConstructionModel(IDateRule rebalancingDateRules,
            PortfolioBias portfolioBias = PortfolioBias.LongShort,
            int lookback = 1,
            int period = 252,
            Resolution resolution = Resolution.Daily,
            IPortfolioOptimizer optimizer = null)
            : this(rebalancingDateRules.ToFunc(), portfolioBias, lookback, period, resolution, optimizer)
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
        /// <param name="optimizer">The portfolio optimization algorithm. If the algorithm is not provided then the default will be mean-variance optimization.</param>
        public RiskParityPortfolioConstructionModel(Resolution rebalanceResolution = Resolution.Daily,
            PortfolioBias portfolioBias = PortfolioBias.LongShort,
            int lookback = 1,
            int period = 252,
            Resolution resolution = Resolution.Daily,
            IPortfolioOptimizer optimizer = null)
            : this(rebalanceResolution.ToTimeSpan(), portfolioBias, lookback, period, resolution, optimizer)
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
        /// <param name="optimizer">The portfolio optimization algorithm. If the algorithm is not provided then the default will be mean-variance optimization.</param>
        public RiskParityPortfolioConstructionModel(TimeSpan timeSpan,
            PortfolioBias portfolioBias = PortfolioBias.LongShort,
            int lookback = 1,
            int period = 252,
            Resolution resolution = Resolution.Daily,
            IPortfolioOptimizer optimizer = null)
            : this(dt => dt.Add(timeSpan), portfolioBias, lookback, period, resolution, optimizer)
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
        /// <param name="optimizer">The portfolio optimization algorithm. If the algorithm is not provided then the default will be mean-variance optimization.</param>
        /// <remarks>This is required since python net can not convert python methods into func nor resolve the correct
        /// constructor for the date rules parameter.
        /// For performance we prefer python algorithms using the C# implementation</remarks>
        public RiskParityPortfolioConstructionModel(PyObject rebalance,
            PortfolioBias portfolioBias = PortfolioBias.LongShort,
            int lookback = 1,
            int period = 252,
            Resolution resolution = Resolution.Daily,
            IPortfolioOptimizer optimizer = null)
            : this((Func<DateTime, DateTime?>)null, portfolioBias, lookback, period, resolution, optimizer)
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
        /// <param name="optimizer">The portfolio optimization algorithm. If the algorithm is not provided then the default will be mean-variance optimization.</param>
        public RiskParityPortfolioConstructionModel(Func<DateTime, DateTime> rebalancingFunc,
            PortfolioBias portfolioBias = PortfolioBias.LongShort,
            int lookback = 1,
            int period = 252,
            Resolution resolution = Resolution.Daily,
            IPortfolioOptimizer optimizer = null)
            : this(rebalancingFunc != null ? (Func<DateTime, DateTime?>)(timeUtc => rebalancingFunc(timeUtc)) : null,
                portfolioBias,
                lookback,
                period,
                resolution,
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
        /// <param name="optimizer">The portfolio optimization algorithm. If the algorithm is not provided then the default will be mean-variance optimization.</param>
        public RiskParityPortfolioConstructionModel(Func<DateTime, DateTime?> rebalancingFunc,
            PortfolioBias portfolioBias = PortfolioBias.LongShort,
            int lookback = 1,
            int period = 252,
            Resolution resolution = Resolution.Daily,
            IPortfolioOptimizer optimizer = null)
            : base(rebalancingFunc)
        {
            if (portfolioBias == PortfolioBias.Short)
            {
                throw new ArgumentException("Long position must be allowed in RiskParityPortfolioConstructionModel.");
            }

            _lookback = lookback;
            _period = period;
            _resolution = resolution;

            _optimizer = optimizer ?? new RiskParityPortfolioOptimizer();

            _symbolDataDict = new Dictionary<Symbol, ReturnsSymbolData>();
        }

        /// <summary>
        /// Will determine the target percent for each insight
        /// </summary>
        /// <param name="activeInsights">The active insights to generate a target for</param>
        /// <returns>A target percent for each insight</returns>
        protected override Dictionary<Insight, double> DetermineTargetPercent(List<Insight> activeInsights)
        {
            var targets = new Dictionary<Insight, double>();

            // If we have no insights just return an empty target list
            if (activeInsights.IsNullOrEmpty())
            {
                return targets;
            }

            var symbols = activeInsights.Select(x => x.Symbol).ToList();

            // Get symbols' returns
            var returns = _symbolDataDict.FormReturnsMatrix(symbols);

            // The optimization method processes the data frame
            var w = _optimizer.Optimize(returns);

            // process results
            if (w.Length > 0)
            {
                var sidx = 0;
                foreach (var symbol in symbols)
                {
                    var weight = w[sidx];
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
                _symbolDataDict.Remove(removed.Symbol, out var removedSymbolData);
                algorithm.UnregisterIndicator(removedSymbolData.ROC);
            }

            if (changes.AddedSecurities.Count == 0)
            {
                return;
            }

            // initialize data for added securities
            foreach (var added in changes.AddedSecurities)
            {
                if (!_symbolDataDict.ContainsKey(added.Symbol))
                {
                    var symbolData = new ReturnsSymbolData(added.Symbol, _lookback, _period);
                    _symbolDataDict[added.Symbol] = symbolData;
                    algorithm.RegisterIndicator(added.Symbol, symbolData.ROC, _resolution);
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
