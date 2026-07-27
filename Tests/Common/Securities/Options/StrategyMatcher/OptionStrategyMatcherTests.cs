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
using System.Linq;
using NUnit.Framework;
using QuantConnect.Securities.Option.StrategyMatcher;
using static QuantConnect.Tests.Common.Securities.Options.StrategyMatcher.Option;
using static QuantConnect.Securities.Option.StrategyMatcher.OptionStrategyDefinitions;
using QuantConnect.Logging;

namespace QuantConnect.Tests.Common.Securities.Options.StrategyMatcher
{
    [TestFixture]
    public class OptionStrategyMatcherTests
    {
        [Test]
        [TestCaseSource(typeof(OptionStrategyDefinitionTests), nameof(OptionStrategyDefinitionTests.TestCases))]
        public void RunSingleDefinition(OptionStrategyDefinitionTests.TestCase test)
        {
            var matcher = test.CreateMatcher();
            var match = matcher.MatchOnce(test.Positions);
            Assert.AreEqual(1, match.Strategies.Count);
        }

        [Test]
        public void DoesNotDoubleCountPositions()
        {
            // this test aims to verify that match solutions do not reference the same position in multiple matches
            // this behavior is different than the OptionStrategyDefinition.Match, which by design produces all possible
            // matches which permits the same position to appear in different matches, allowing the matcher to pick matches

            // this test aims to verify that we can match the same definition multiple times if positions allows
            // 0: -C110 +C105
            // 1: -C115 +C100
            var positions = OptionPositionCollection.Empty.AddRange(
                Position(Call[100], -1),
                Position(Call[105], -1),
                Position(Call[110]),
                Position(Call[115])
            );

            var matcher = new OptionStrategyMatcher(OptionStrategyMatcherOptions.ForDefinitions(BearCallSpread));
            var matches = matcher.MatchOnce(positions);
            Assert.AreEqual(2, matches.Strategies.Count);
        }

        [Test]
        public void MatchesAgainstFullPositionCollection()
        {
            // sort definitions by leg count so that we match more complex definitions first
            var options = OptionStrategyMatcherOptions.ForDefinitions(OptionStrategyDefinitions.AllDefinitions
                .OrderByDescending(definition => definition.LegCount)
            );
            var matcher = new OptionStrategyMatcher(options);
            var positions = OptionPositionCollection.Empty.AddRange(Option.Position(Option.Underlying, +20),
                Option.Position(Option.Call[100, -4]), Option.Position(Option.Put[105, -4]),
                Option.Position(Option.Call[105, +4]), Option.Position(Option.Put[110, +4]),
                Option.Position(Option.Call[110, -3]), Option.Position(Option.Put[115, -3]),
                Option.Position(Option.Call[115, +3]), Option.Position(Option.Put[120, +3]),
                Option.Position(Option.Call[120, -5]), Option.Position(Option.Put[125, -5]),
                Option.Position(Option.Call[124, +5]), Option.Position(Option.Put[130, +5])
            );

            var match = matcher.MatchOnce(positions);
            foreach (var strategy in match.Strategies)
            {
                Log.Trace($"{strategy.Name}");
                foreach (var leg in strategy.OptionLegs)
                {
                    // steal OptionPosition's ToString() implementation
                    Console.Write($"\t{new OptionPosition(leg.Symbol, leg.Quantity)}");
                }
            }
        }

        [Test]
        public void MatchesOverlappingDebitSpreadsAsSpreadsInsteadOfLadder()
        {
            // two overlapping bull call spreads with interleaved strikes, same expiration.
            // a leg-count-greedy match carves this book into a bull call ladder, whose second short leg is
            // charged naked call margin, plus an unmatched long. the correct, margin-free solution is two spreads
            var positions = OptionPositionCollection.Empty.AddRange(
                Position(Call[598]),
                Position(Call[600]),
                Position(Call[603], -1),
                Position(Call[605], -1)
            );

            var matcher = new OptionStrategyMatcher(OptionStrategyMatcherOptions.ForDefinitions(AllDefinitions));
            var match = matcher.MatchOnce(positions);

            Assert.AreEqual(2, match.Strategies.Count);
            Assert.IsTrue(match.Strategies.All(strategy => strategy.Name == BullCallSpread.Name),
                string.Join(", ", match.Strategies.Select(strategy => strategy.Name)));
            // all four contracts must be consumed, either spread pairing is acceptable
            Assert.AreEqual(4, match.Strategies.Sum(strategy => strategy.OptionLegs.Count));
        }

        [Test]
        public void MatchLeavesNoShortContractUncoveredWhenFullCoverageExists()
        {
            // every short strike has a long at a lower strike available to cover it, same expiration:
            // pairing shorts in ascending order against lower longs covers all of them, so no solution
            // should leave a short contract uncovered (inside a ladder) or unmatched
            var positions = OptionPositionCollection.Empty.AddRange(
                Position(Call[598], 3), Position(Call[600], 2), Position(Call[604], 3), Position(Call[608], 2),
                Position(Call[603], -3), Position(Call[605], -2), Position(Call[609], -2), Position(Call[613], -1)
            );

            var matcher = new OptionStrategyMatcher(OptionStrategyMatcherOptions.ForDefinitions(AllDefinitions));
            var match = matcher.MatchOnce(positions);

            var matchedShortQuantity = 0;
            foreach (var strategy in match.Strategies)
            {
                // no strategy is allowed to hold net short calls, which would be charged naked call margin
                Assert.GreaterOrEqual(strategy.OptionLegs.Sum(leg => leg.Quantity), 0,
                    $"{strategy.Name}: {string.Join("|", strategy.OptionLegs.Select(leg => new OptionPosition(leg.Symbol, leg.Quantity)))}");

                matchedShortQuantity -= strategy.OptionLegs.Where(leg => leg.Quantity < 0).Sum(leg => leg.Quantity);
            }

            // all 8 short contracts are matched into strategies covering them
            Assert.AreEqual(8, matchedShortQuantity);
        }

        [Test]
        public void MatchesTrueButterflyBookAsButterfly()
        {
            // a true butterfly book must not be decomposed into a bull call spread plus a bear call spread,
            // which would require margin for the bear spread's strike width while the butterfly requires none
            var positions = OptionPositionCollection.Empty.AddRange(
                Position(Call[595]),
                Position(Call[600], -2),
                Position(Call[605])
            );

            var matcher = new OptionStrategyMatcher(OptionStrategyMatcherOptions.ForDefinitions(AllDefinitions));
            var match = matcher.MatchOnce(positions);

            Assert.AreEqual(1, match.Strategies.Count);
            Assert.AreEqual(ButterflyCall.Name, match.Strategies.Single().Name);
        }

        [Test]
        public void MatchesLadderBookAsLadderWhenNoBetterSolutionExists()
        {
            // an actual ladder book has one genuinely uncovered short either way it's grouped,
            // so on equal scores the original leg-count-greedy solution is preserved
            var positions = OptionPositionCollection.Empty.AddRange(
                Position(Call[595]),
                Position(Call[600], -1),
                Position(Call[605], -1)
            );

            var matcher = new OptionStrategyMatcher(OptionStrategyMatcherOptions.ForDefinitions(AllDefinitions));
            var match = matcher.MatchOnce(positions);

            Assert.AreEqual(1, match.Strategies.Count);
            Assert.AreEqual(BullCallLadder.Name, match.Strategies.Single().Name);
        }
    }
}
