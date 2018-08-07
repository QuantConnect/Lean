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

using System.Collections.Generic;
using System.Linq;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Securities;

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
        private List<Symbol> _removedSymbols;
        private readonly InsightCollection _insightCollection = new InsightCollection();

        /// <summary>
        /// Create portfolio targets from the specified insights
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="insights">The insights to create portoflio targets from</param>
        /// <returns>An enumerable of portoflio targets to be sent to the execution model</returns>
        public override IEnumerable<IPortfolioTarget> CreateTargets(QCAlgorithmFramework algorithm, Insight[] insights)
        {
            _insightCollection.AddRange(insights);

            var targets = new List<IPortfolioTarget>();
            if (_removedSymbols != null)
            {
                // zero out securities removes from the universe
                targets.AddRange(_removedSymbols.Select(s => new PortfolioTarget(s, 0)));
                _removedSymbols = null;
            }

            if (insights.Length == 0)
            {
                return targets;
            }

            // Get last insight that haven't expired of each symbol that is still in the universe
            var activeInsights = (from insight in _insightCollection
                                  // Remove expired insights
                                  where insight.CloseTimeUtc > algorithm.UtcTime
                                  // Force one group per symbol
                                  group insight by insight.Symbol into g
                                  // For direction, we'll trust the most recent insight
                                  select g.OrderBy(x => x.GeneratedTimeUtc).LastOrDefault())
                                  .ToList();

            if (activeInsights.Count == 0)
            {
                return targets;
            }

            // give equal weighting to each security
            var count = activeInsights.Count(x => x.Direction != InsightDirection.Flat);
            var percent = count == 0 ? 0 : 1m / count;

            foreach (var insight in activeInsights)
            {
                targets.Add(PortfolioTarget.Percent(algorithm, insight.Symbol, (int)insight.Direction * percent));
            }

            // add targets to remove invested securities
            targets.AddRange(from kvp in algorithm.Portfolio
                             where kvp.Value.Invested
                             where targets.All(x => kvp.Key != x.Symbol)
                             select new PortfolioTarget(kvp.Key, 0));

            return targets;
        }

        /// <summary>
        /// Event fired each time the we add/remove securities from the data feed
        /// </summary>
        /// <param name="algorithm">The algorithm instance that experienced the change in securities</param>
        /// <param name="changes">The security additions and removals from the algorithm</param>
        public override void OnSecuritiesChanged(QCAlgorithmFramework algorithm, SecurityChanges changes)
        {
            // save securities removed so we can zero out our holdings
            _removedSymbols = changes.RemovedSecurities.Select(x => x.Symbol).ToList();

            // remove the insights of the removed symbol from the collection 
            foreach (var removedSymbol in _removedSymbols)
            {
                List<Insight> insights;
                if (_insightCollection.TryGetValue(removedSymbol, out insights))
                {
                    foreach (var insight in insights.ToList())
                    {
                        _insightCollection.Remove(insight);
                    }
                }
            }
        }
    }
}