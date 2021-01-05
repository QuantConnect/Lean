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
using Python.Runtime;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Scheduling;
using QuantConnect.Util;

namespace QuantConnect.Algorithm.Framework.Portfolio
{
    /// <summary>
    /// Provides an implementation of <see cref="IPortfolioConstructionModel"/> that allocates percent of account
    /// to each insight, defaulting to 3%.
    /// For insights of direction <see cref="InsightDirection.Up"/>, long targets are returned and
    /// for insights of direction <see cref="InsightDirection.Down"/>, short targets are returned.
    /// By default, no rebalancing shall be done.
    /// Rules:
    ///    1. On active Up insight, increase position size by percent
    ///    2. On active Down insight, decrease position size by percent
    ///    3. On active Flat insight, move by percent towards 0
    ///    4. On expired insight, and no other active insight, emits a 0 target'''
    /// </summary>
    public class AccumulativeInsightPortfolioConstructionModel : PortfolioConstructionModel
    {
        private readonly PortfolioBias _portfolioBias;
        private readonly double _percent;

        /// <summary>
        /// Initialize a new instance of <see cref="AccumulativeInsightPortfolioConstructionModel"/>
        /// </summary>
        /// <param name="rebalancingDateRules">The date rules used to define the next expected rebalance time
        /// in UTC</param>
        /// <param name="portfolioBias">Specifies the bias of the portfolio (Short, Long/Short, Long)</param>
        /// <param name="percent">The percentage amount of the portfolio value to allocate
        /// to a single insight. The value of percent should be in the range [0,1].
        /// The default value is 0.03.</param>
        public AccumulativeInsightPortfolioConstructionModel(IDateRule rebalancingDateRules,
            PortfolioBias portfolioBias = PortfolioBias.LongShort,
            double percent = 0.03)
            : this(rebalancingDateRules.ToFunc(), portfolioBias, percent)
        {
        }

        /// <summary>
        /// Initialize a new instance of <see cref="AccumulativeInsightPortfolioConstructionModel"/>
        /// </summary>
        /// <param name="rebalancingFunc">For a given algorithm UTC DateTime returns the next expected rebalance time
        /// or null if unknown, in which case the function will be called again in the next loop. Returning current time
        /// will trigger rebalance. If null will be ignored</param>
        /// <param name="portfolioBias">Specifies the bias of the portfolio (Short, Long/Short, Long)</param>
        /// <param name="percent">The percentage amount of the portfolio value to allocate
        /// to a single insight. The value of percent should be in the range [0,1].
        /// The default value is 0.03.</param>
        public AccumulativeInsightPortfolioConstructionModel(Func<DateTime, DateTime?> rebalancingFunc = null,
            PortfolioBias portfolioBias = PortfolioBias.LongShort,
            double percent = 0.03)
            : base(rebalancingFunc)
        {
            _portfolioBias = portfolioBias;
            _percent = Math.Abs(percent);
        }

        /// <summary>
        /// Initialize a new instance of <see cref="AccumulativeInsightPortfolioConstructionModel"/>
        /// </summary>
        /// <param name="rebalancingFunc">For a given algorithm UTC DateTime returns the next expected rebalance UTC time.
        /// Returning current time will trigger rebalance. If null will be ignored</param>
        /// <param name="portfolioBias">Specifies the bias of the portfolio (Short, Long/Short, Long)</param>
        /// <param name="percent">The percentage amount of the portfolio value to allocate
        /// to a single insight. The value of percent should be in the range [0,1].
        /// The default value is 0.03.</param>
        public AccumulativeInsightPortfolioConstructionModel(Func<DateTime, DateTime> rebalancingFunc,
            PortfolioBias portfolioBias = PortfolioBias.LongShort,
            double percent = 0.03)
            : this(rebalancingFunc != null ? (Func<DateTime, DateTime?>)(timeUtc => rebalancingFunc(timeUtc)) : null,
                portfolioBias,
                percent)
        {
        }

        /// <summary>
        /// Initialize a new instance of <see cref="AccumulativeInsightPortfolioConstructionModel"/>
        /// </summary>
        /// <param name="rebalance">Rebalancing func or if a date rule, timedelta will be converted into func.
        /// For a given algorithm UTC DateTime the func returns the next expected rebalance time
        /// or null if unknown, in which case the function will be called again in the next loop. Returning current time
        /// will trigger rebalance. If null will be ignored</param>
        /// <param name="portfolioBias">Specifies the bias of the portfolio (Short, Long/Short, Long)</param>
        /// <remarks>This is required since python net can not convert python methods into func nor resolve the correct
        /// constructor for the date rules parameter.
        /// For performance we prefer python algorithms using the C# implementation</remarks>
        /// <param name="percent">The percentage amount of the portfolio value to allocate
        /// to a single insight. The value of percent should be in the range [0,1].
        /// The default value is 0.03.</param>
        public AccumulativeInsightPortfolioConstructionModel(PyObject rebalance,
            PortfolioBias portfolioBias = PortfolioBias.LongShort,
            double percent = 0.03)
            : this((Func<DateTime, DateTime?>)null,
                portfolioBias,
                percent)
        {
            SetRebalancingFunc(rebalance);
        }

        /// <summary>
        /// Initialize a new instance of <see cref="AccumulativeInsightPortfolioConstructionModel"/>
        /// </summary>
        /// <param name="timeSpan">Rebalancing frequency</param>
        /// <param name="portfolioBias">Specifies the bias of the portfolio (Short, Long/Short, Long)</param>
        /// <param name="percent">The percentage amount of the portfolio value to allocate
        /// to a single insight. The value of percent should be in the range [0,1].
        /// The default value is 0.03.</param>
        public AccumulativeInsightPortfolioConstructionModel(TimeSpan timeSpan,
            PortfolioBias portfolioBias = PortfolioBias.LongShort,
            double percent = 0.03)
            : this(dt => dt.Add(timeSpan), portfolioBias, percent)
        {
        }

        /// <summary>
        /// Initialize a new instance of <see cref="AccumulativeInsightPortfolioConstructionModel"/>
        /// </summary>
        /// <param name="resolution">Rebalancing frequency</param>
        /// <param name="portfolioBias">Specifies the bias of the portfolio (Short, Long/Short, Long)</param>
        /// <param name="percent">The percentage amount of the portfolio value to allocate
        /// to a single insight. The value of percent should be in the range [0,1].
        /// The default value is 0.03.</param>
        public AccumulativeInsightPortfolioConstructionModel(Resolution resolution,
            PortfolioBias portfolioBias = PortfolioBias.LongShort,
            double percent = 0.03)
            : this(resolution.ToTimeSpan(), portfolioBias, percent)
        {
        }

        /// <summary>
        /// Gets the target insights to calculate a portfolio target percent for
        /// </summary>
        /// <returns>An enumerable of the target insights</returns>
        protected override List<Insight> GetTargetInsights()
        {
            return InsightCollection.GetActiveInsights(Algorithm.UtcTime)
                .OrderBy(insight => insight.GeneratedTimeUtc)
                .ToList();
        }

        /// <summary>
        /// Determines the target percent for each insight
        /// </summary>
        /// <param name="activeInsights">The active insights to generate a target for</param>
        /// <returns>A target percent for each insight</returns>
        protected override Dictionary<Insight, double> DetermineTargetPercent(List<Insight> activeInsights)
        {
            var percentPerSymbol = new Dictionary<Symbol, double>();

            foreach (var insight in activeInsights)
            {
                double targetPercent;
                if (percentPerSymbol.TryGetValue(insight.Symbol, out targetPercent))
                {
                    if (insight.Direction == InsightDirection.Flat)
                    {
                        // We received a Flat
                        // if adding or subtracting will push past 0, then make it 0
                        if (Math.Abs(targetPercent) < _percent)
                        {
                            targetPercent = 0;
                        }
                        else
                        {
                            // otherwise, we flatten by percent
                            targetPercent += (targetPercent > 0 ? -_percent : _percent);
                        }
                    }
                }
                targetPercent += _percent * (int)insight.Direction;

                // adjust to respect portfolio bias
                if (_portfolioBias != PortfolioBias.LongShort
                    && Math.Sign(targetPercent) != (int)_portfolioBias)
                {
                    targetPercent = 0;
                }

                percentPerSymbol[insight.Symbol] = targetPercent;
            }

            return activeInsights.DistinctBy(insight => insight.Symbol)
                .ToDictionary(insight => insight, insight => percentPerSymbol[insight.Symbol]);
        }
    }
}