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
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.Algorithm.Framework.Portfolio
{
    /// <summary>
    /// Provides an implementation of <see cref="IPortfolioConstructionModel"/> that gives equal weighting to all
    /// securities. The target percent holdings of each security is 1/N where N is the number of securities. For
    /// insights of direction <see cref="InsightDirection.Up"/>, long targets are returned and for insights of direction
    /// <see cref="InsightDirection.Down"/>, short targets are returned.
    /// </summary>
    public class EqualWeightingPortfolioConstructionModel : PortfolioConstructionModel
    {
        private readonly PortfolioBias _portfolioBias;
        private readonly Func<DateTime, DateTime> _rebalancingFunc;
        private DateTime _rebalancingTime;
        private List<Symbol> _removedSymbols;
        private readonly InsightCollection _insightCollection = new InsightCollection();
        private DateTime? _nextExpiryTime;

        /// <summary>
        /// Creates a new instance of <see cref="EqualWeightingPortfolioConstructionModel"/> with Long portfolio bias.
        /// </summary>
        /// <param name="rebalancingFunc">For a given algorithm UTC DateTime returns the next expected rebalance time</param>
        public static EqualWeightingPortfolioConstructionModel LongOnly(Func<DateTime, DateTime> rebalancingFunc)
        {
            return new EqualWeightingPortfolioConstructionModel(rebalancingFunc, PortfolioBias.Long);
        }

        /// <summary>
        /// Creates a new instance of <see cref="EqualWeightingPortfolioConstructionModel"/> with Long portfolio bias.
        /// </summary>
        /// <param name="timeSpan">Rebalancing frequency</param>
        public static EqualWeightingPortfolioConstructionModel LongOnly(TimeSpan timeSpan)
        {
            return new EqualWeightingPortfolioConstructionModel(timeSpan, PortfolioBias.Long);
        }

        /// <summary>
        /// Creates a new instance of <see cref="EqualWeightingPortfolioConstructionModel"/> with Long portfolio bias.
        /// </summary>
        /// <param name="resolution">Rebalancing frequency</param>
        public static EqualWeightingPortfolioConstructionModel LongOnly(Resolution resolution = Resolution.Daily)
        {
            return new EqualWeightingPortfolioConstructionModel(resolution, PortfolioBias.Long);
        }

        /// <summary>
        /// Initialize a new instance of <see cref="EqualWeightingPortfolioConstructionModel"/>
        /// </summary>
        /// <param name="rebalancingFunc">For a given algorithm UTC DateTime returns the next expected rebalance time</param>
        /// <param name="portfolioBias">Specifies the bias of the portfolio (Short, Long/Short, Long)</param>
        public EqualWeightingPortfolioConstructionModel(Func<DateTime, DateTime> rebalancingFunc, PortfolioBias portfolioBias = PortfolioBias.LongShort)
        {
            _portfolioBias = portfolioBias;
            _rebalancingFunc = rebalancingFunc;
        }

        /// <summary>
        /// Initialize a new instance of <see cref="EqualWeightingPortfolioConstructionModel"/>
        /// </summary>
        /// <param name="timeSpan">Rebalancing frequency</param>
        /// <param name="portfolioBias">Specifies the bias of the portfolio (Short, Long/Short, Long)</param>
        public EqualWeightingPortfolioConstructionModel(TimeSpan timeSpan, PortfolioBias portfolioBias = PortfolioBias.LongShort)
            : this(dt => dt.Add(timeSpan), portfolioBias)
        {
        }

        /// <summary>
        /// Initialize a new instance of <see cref="EqualWeightingPortfolioConstructionModel"/>
        /// </summary>
        /// <param name="resolution">Rebalancing frequency</param>
        /// <param name="portfolioBias">Specifies the bias of the portfolio (Short, Long/Short, Long)</param>
        public EqualWeightingPortfolioConstructionModel(Resolution resolution = Resolution.Daily, PortfolioBias portfolioBias = PortfolioBias.LongShort)
            : this(resolution.ToTimeSpan(), portfolioBias)
        {
        }

        /// <summary>
        /// Method that will determine if the portfolio construction model should create a
        /// target for this insight
        /// </summary>
        /// <param name="insight">The insight to create a target for</param>
        /// <returns>True if the portfolio should create a target for the insight</returns>
        public virtual bool ShouldCreateTargetForInsight(Insight insight)
        {
            return true;
        }

        /// <summary>
        /// Will determine the target percent for each insight
        /// </summary>
        /// <param name="activeInsights">The active insights to generate a target for</param>
        /// <returns>A target percent for each insight</returns>
        public virtual Dictionary<Insight, double> DetermineTargetPercent(ICollection<Insight> activeInsights)
        {
            var result = new Dictionary<Insight, double>();

            // give equal weighting to each security
            var count = activeInsights.Count(x => x.Direction != InsightDirection.Flat);
            var percent = count == 0 ? 0 : 1m / count;
            foreach (var insight in activeInsights)
            {
                result[insight] = (double)((int)insight.Direction * percent);
            }
            return result;
        }

        /// <summary>
        /// Method that will determine if a given insight respects the portfolio bias
        /// </summary>
        /// <param name="insight">The insight to create a target for</param>
        /// <returns>True if the insight respects the portfolio bias</returns>
        protected bool RespectPortfolioBias(Insight insight)
        {
            return _portfolioBias == PortfolioBias.LongShort || (int) insight.Direction == (int) _portfolioBias;
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

            if (algorithm.UtcTime <= _nextExpiryTime &&
                algorithm.UtcTime <= _rebalancingTime &&
                insights.Length == 0 &&
                _removedSymbols == null)
            {
                return targets;
            }

            // Validate we should create a target for this insight
            _insightCollection.AddRange(insights.Where(ShouldCreateTargetForInsight));

            // Create flatten target for each security that was removed from the universe
            if (_removedSymbols != null)
            {
                var universeDeselectionTargets = _removedSymbols.Select(symbol => new PortfolioTarget(symbol, 0));
                targets.AddRange(universeDeselectionTargets);
                _removedSymbols = null;
            }

            // Get insight that haven't expired of each symbol that is still in the universe
            var activeInsights = _insightCollection.GetActiveInsights(algorithm.UtcTime);

            // Get the last generated active insight for each symbol
            var lastActiveInsights = (from insight in activeInsights
                                      group insight by insight.Symbol into g
                                      select g.OrderBy(x => x.GeneratedTimeUtc).Last()).ToList();

            var errorSymbols = new HashSet<Symbol>();

            // Determine target percent for the given insights that respect the portfolio bias
            var percents = DetermineTargetPercent(lastActiveInsights.Where(RespectPortfolioBias).ToList());

            foreach (var insight in lastActiveInsights)
            {
                // If the insight is not in percents, it doesn't respect the portfolio bias.
                // Consequently, the target percent is zero
                double percent;
                percents.TryGetValue(insight, out percent);

                var target = PortfolioTarget.Percent(algorithm, insight.Symbol, percent);
                if (target != null)
                {
                    targets.Add(target);
                }
                else
                {
                    errorSymbols.Add(insight.Symbol);
                }
            }

            // Get expired insights and create flatten targets for each symbol
            var expiredInsights = _insightCollection.RemoveExpiredInsights(algorithm.UtcTime);

            var expiredTargets = from insight in expiredInsights
                                 group insight.Symbol by insight.Symbol into g
                                 where !_insightCollection.HasActiveInsights(g.Key, algorithm.UtcTime) && !errorSymbols.Contains(g.Key)
                                 select new PortfolioTarget(g.Key, 0);

            targets.AddRange(expiredTargets);

            _nextExpiryTime = _insightCollection.GetNextExpiryTime();
            _rebalancingTime = _rebalancingFunc(algorithm.UtcTime);

            return targets;
        }

        /// <summary>
        /// Event fired each time the we add/remove securities from the data feed
        /// </summary>
        /// <param name="algorithm">The algorithm instance that experienced the change in securities</param>
        /// <param name="changes">The security additions and removals from the algorithm</param>
        public override void OnSecuritiesChanged(QCAlgorithm algorithm, SecurityChanges changes)
        {
            // Get removed symbol and invalidate them in the insight collection
            _removedSymbols = changes.RemovedSecurities.Select(x => x.Symbol).ToList();
            _insightCollection.Clear(_removedSymbols.ToArray());
        }
    }
}