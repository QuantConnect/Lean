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
using QuantConnect.Securities.Positions;

namespace QuantConnect.Securities.Option.StrategyMatcher
{
    /// <summary>
    /// Matches <see cref="OptionPositionCollection"/> against a collection of <see cref="OptionStrategyDefinition"/>
    /// according to the <see cref="OptionStrategyMatcherOptions"/> provided.
    /// </summary>
    public class OptionStrategyMatcher
    {
        private readonly OptionStrategyMatchMarginFunction _marginFunction;

        /// <summary>
        /// Specifies options controlling how the matcher operates
        /// </summary>
        public OptionStrategyMatcherOptions Options { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="OptionStrategyMatcher"/> class
        /// </summary>
        /// <param name="options">Specifies definitions and other options controlling the matcher</param>
        public OptionStrategyMatcher(OptionStrategyMatcherOptions options)
        {
            Options = options;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OptionStrategyMatcher"/> class
        /// </summary>
        /// <param name="options">Specifies definitions and other options controlling the matcher</param>
        /// <param name="marginFunction">Prices candidate solutions so the last candidate never replaces one requiring
        /// less maintenance margin, or null to compare candidates by their score alone</param>
        internal OptionStrategyMatcher(OptionStrategyMatcherOptions options, OptionStrategyMatchMarginFunction marginFunction)
            : this(options)
        {
            _marginFunction = marginFunction;
        }

        /// <summary>
        /// Using the definitions provided in <see cref="Options"/>, attempts to match all <paramref name="positions"/>.
        /// The resulting <see cref="OptionStrategyMatch"/> presents a single, valid solution for matching as many positions
        /// as possible. A fixed set of candidate solutions is evaluated and the one scoring highest against the configured
        /// <see cref="OptionStrategyMatcherOptions.ObjectiveFunction"/> is selected, so short positions are grouped into
        /// covered strategies instead of being charged naked option margin whenever the positions allow it.
        /// On equal scores, the solution produced by the configured definition enumeration order is preserved.
        /// </summary>
        public OptionStrategyMatch MatchOnce(OptionPositionCollection positions)
        {
            // the first candidate solution greedily matches definitions in the configured enumeration order, by
            // default descending by leg count so more complex definitions get matching priority. it's evaluated
            // first so that whenever the objective function scores another candidate equally this one is preserved
            var bestMatch = Match(Options.Definitions, positions, out var bestUnmatched);
            var bestScore = Options.ObjectiveFunction.ComputeScore(positions, bestMatch, bestUnmatched);
            if (CannotImprove(bestScore))
            {
                return bestMatch;
            }

            // the second candidate deprioritizes definitions leaving a short leg uncovered within the strategy
            // (naked calls/puts, ladders, short backspreads/straddles/strangles), so short positions are matched
            // into covered strategies whenever another grouping of the same positions allows it. this avoids
            // greedily carving, for instance, two overlapping bull call spreads into a bull call ladder, whose
            // uncovered short leg is charged naked option margin, plus an unmatched long contract
            var candidateMatch = Match(Options.CoveredShortsFirstDefinitions, positions, out var candidateUnmatched);
            var candidateScore = Options.ObjectiveFunction.ComputeScore(positions, candidateMatch, candidateUnmatched);
            if (candidateScore > bestScore)
            {
                bestMatch = candidateMatch;
                bestUnmatched = candidateUnmatched;
                bestScore = candidateScore;
            }

            if (CannotImprove(bestScore))
            {
                return bestMatch;
            }

            // the third candidate also deprioritizes definitions holding more long legs of a right than their own
            // short legs need (bull put/bear call ladders, long straddles, protective puts), since those longs could
            // cover short legs elsewhere in the book. this avoids carving, for instance, two bull put spreads into a
            // bull put ladder plus an unmatched, naked short put. it's kept separate from the second candidate
            // because deprioritizing those definitions loses the covered grouping of some books the second finds
            candidateMatch = Match(Options.BalancedLegsFirstDefinitions, positions, out candidateUnmatched);
            candidateScore = Options.ObjectiveFunction.ComputeScore(positions, candidateMatch, candidateUnmatched);
            if (candidateScore <= bestScore)
            {
                return bestMatch;
            }

            // covering more short contracts doesn't always lower the margin: the score counts a credit spread as covered
            // up to a width of 10% of its short strike, while a far out of the money naked call can require less than
            // that. so this candidate also mustn't require more maintenance margin than the best solution so far,
            // unless a security lacks the price to tell, where the score alone decides
            if (_marginFunction != null
                && _marginFunction.TryGetMaintenanceMargin(positions, bestMatch, bestUnmatched, out var bestMargin)
                && _marginFunction.TryGetMaintenanceMargin(positions, candidateMatch, candidateUnmatched, out var candidateMargin)
                && candidateMargin > bestMargin)
            {
                return bestMatch;
            }

            return candidateMatch;

            bool CannotImprove(decimal score)
            {
                // by convention solutions that can't be improved upon score zero, see IOptionStrategyMatchObjectiveFunction.
                // matching again is also pointless once the solution leaves no more short contracts uncovered than
                // the positions themselves can possibly cover, which is the case for a book of naked shorts, for a book
                // holding fewer longs than shorts, and generally whenever the solution is already optimal.
                // that bound reads the score as a negated quantity of uncovered short contracts, which only the
                // default objective function itself guarantees. any other one, including a derived function free to
                // score by different rules, always gets to evaluate every candidate
                return score >= 0
                    || Options.ObjectiveFunction.GetType() == typeof(UncoveredShortQuantityOptionStrategyMatchObjectiveFunction)
                        && -score <= GetMinimumUncoveredQuantity(positions);
            }
        }

        private OptionStrategyMatch Match(IEnumerable<OptionStrategyDefinition> definitions, OptionPositionCollection positions,
            out OptionPositionCollection unmatched)
        {
            var strategies = new List<OptionStrategy>();
            foreach (var definition in definitions)
            {
                // simplest implementation here is to match one at a time, updating positions in between
                OptionStrategyDefinitionMatch match;
                while (definition.TryMatchOnce(Options, positions, out match))
                {
                    positions = match.RemoveFrom(positions);
                    strategies.Add(match.CreateStrategy());
                }

                if (positions.IsEmpty)
                {
                    break;
                }
            }

            unmatched = positions;
            return new OptionStrategyMatch(strategies);
        }

        /// <summary>
        /// Determines the smallest quantity of short contracts any grouping of these positions can leave uncovered.
        /// A long contract covers at most its own quantity of shorts of the same right, and so does an underlying lot,
        /// which bounds how much a different grouping could possibly improve on the solution already found
        /// </summary>
        private static decimal GetMinimumUncoveredQuantity(OptionPositionCollection positions)
        {
            var calls = 0m;
            var puts = 0m;
            foreach (var position in positions)
            {
                if (position.IsUnderlying)
                {
                    continue;
                }

                if (position.Right == OptionRight.Call)
                {
                    calls += position.Quantity;
                }
                else
                {
                    puts += position.Quantity;
                }
            }

            // long underlying lots cover short calls, short underlying lots cover short puts
            return Math.Max(0, -calls - Math.Max(0, positions.UnderlyingQuantity))
                + Math.Max(0, -puts - Math.Max(0, -positions.UnderlyingQuantity));
        }
    }
}
