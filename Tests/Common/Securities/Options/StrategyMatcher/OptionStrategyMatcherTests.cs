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
                Position(Call[110], -1),
                Position(Call[115], -1),
                Position(Call[100]),
                Position(Call[105])
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
    }
}