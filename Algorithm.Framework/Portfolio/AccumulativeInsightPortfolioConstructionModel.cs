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
    /// Provides an implementation of <see cref="IPortfolioConstructionModel"/> that allocates percent of account
    /// to each insight, defaulting to 3%.
    /// For insights of direction <see cref="InsightDirection.Up"/>, long targets are returned and
    /// for insights of direction <see cref="InsightDirection.Down"/>, short targets are returned.
    /// No rebalancing shall be done, as a new insight or the age of the insight shall determine whether to exit
    /// the positions.
    /// Rules:
    ///    1. On Up insight, increase position size by percent
    ///    2. On Down insight, decrease position size by percent
    ///    3. On Flat insight, move by percent towards 0
    ///    4. On expired insight, perform a Flat insight'''
    /// </summary>
    public class AccumulativeInsightPortfolioConstructionModel : PortfolioConstructionModel
    {
        private List<Symbol> _removedSymbols = new List<Symbol>();
        private readonly InsightCollection _insightCollection = new InsightCollection();
        private DateTime? _nextExpiryTime;
        private readonly double _percent;
        private readonly Dictionary<Insight, int> usedInsight = new Dictionary<Insight, int>();
        private readonly Dictionary<Insight, int> expiredList = new Dictionary<Insight, int>();
        private readonly Dictionary<Symbol, double> positionSizes = new Dictionary<Symbol, double>();
                
        /// <summary>
        /// Initialize a new instance of <see cref="AccumulativeInsightPortfolioConstructionModel"/>
        /// </summary>
        /// <param name="percent">The amount of portfolio to allocate to a single insight</param>
        public AccumulativeInsightPortfolioConstructionModel(double percent = 0.03)
        {
            _percent = Math.Abs(percent);
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
        public Dictionary<Insight, double> DetermineTargetPercent(ICollection<Insight> activeInsights)
        {
            var result = new Dictionary<Insight, double>();

            foreach (var insight in activeInsights)
            {
                if (usedInsight.ContainsKey(insight))
                {
                   continue;
                }
                usedInsight[insight] = 1;
                if (positionSizes.ContainsKey(insight.Symbol))
                {
                   positionSizes[insight.Symbol] += _percent * (int)insight.Direction;
                }
                else
                {
                     positionSizes[insight.Symbol] = _percent * (int)insight.Direction;
                }

                if (insight.Direction == 0)
                {
                   // We received a Flat

                   // if adding or subtracting will push past 0, then make it 0
                   if (Math.Abs(positionSizes[insight.Symbol]) < _percent)
                   {
                      positionSizes[insight.Symbol] = 0;
                   }

                   // otherwise, we flatten by percent
                   if (positionSizes[insight.Symbol] > 0)
                   {
                        positionSizes[insight.Symbol] -= _percent;
                   }
                   if (positionSizes[insight.Symbol] < 0)
                   {
                      positionSizes[insight.Symbol] += _percent;
                   }
                }
            
                result[insight] = positionSizes[insight.Symbol];
            }
            return result;
        }

        /// <summary>
        /// Will determine the target percent for each expired insight (since expired insights
        /// will remove the amount accumulated in portfolio)
        /// </summary>
        /// <param name ="activeInsights">The insights that are expiring and must be updated
        /// <returns>A target percent for each insight</returns>
        public Dictionary<Insight, double> UpdateExpiredInsights(ICollection<Insight> activeInsights)
        {
            var result = new Dictionary<Insight, double>();
        
            foreach (var insight in activeInsights)
            {
                if (expiredList.ContainsKey(insight))
                {
                    continue;
                }
                expiredList[insight] = 1;

                // if an expiring insight pushes it past 0, then flatten to 0
                if ( (Math.Abs(positionSizes[insight.Symbol]) < _percent) && (insight.Direction != 0) )
                {
                    positionSizes[insight.Symbol] = 0;
                }
                else
                {
                    positionSizes[insight.Symbol] -= _percent * (int)insight.Direction;
                }
                result[insight] = positionSizes[insight.Symbol];
            }
        
            return result;
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

            // Determine target percent for the given insights
            var percents = DetermineTargetPercent(lastActiveInsights);

            foreach (var insight in percents.Keys)
            {
                var target = PortfolioTarget.Percent(algorithm, insight.Symbol, percents[insight]);
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

                        percents = UpdateExpiredInsights(expiredInsights);

            foreach (var insight in percents.Keys)
            {
                var target = PortfolioTarget.Percent(algorithm, insight.Symbol, percents[insight]);
                if (target != null)
                {
                    targets.Add(target);
                }
                else
                {
                    errorSymbols.Add(insight.Symbol);
                }
            }

            _nextExpiryTime = _insightCollection.GetNextExpiryTime();

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


