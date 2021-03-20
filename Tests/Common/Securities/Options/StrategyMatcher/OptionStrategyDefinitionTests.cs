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
using System.Globalization;
using System.Linq;
using NUnit.Framework;
using QuantConnect.Logging;
using QuantConnect.Securities.Option.StrategyMatcher;
using static QuantConnect.Securities.Option.StrategyMatcher.OptionPositionCollection;
using static QuantConnect.Securities.Option.StrategyMatcher.OptionStrategyDefinitions;
using static QuantConnect.Tests.Common.Securities.Options.StrategyMatcher.Option;

namespace QuantConnect.Tests.Common.Securities.Options.StrategyMatcher
{
    [TestFixture]
    public class OptionStrategyDefinitionTests
    {
        [Test]
        [TestCaseSource(nameof(TestCases))]
        public void Run(TestCase test)
        {
            var result = test.Definition.Match(test.Positions).ToList();
            foreach (var match in result)
            {
                Log.Trace(string.Join(";", match.Legs.Select(leg => String(leg.Position))));
            }

            test.AssertMatch(result);
        }

        [Test]
        public void CoveredCall_MatchesMultipleTimes_ForEachUniqueShortCallContract()
        {
            // CoveredCall
            // 0: +1 underlying lot
            // 1: -1 Call
            // so we should match
            // (4U, -4C100)
            // (3U, -3C110)
            // (5U, -5C120)
            // (9U, -9C130)
            // (20U, -20C140)
            // OptionStrategyDefinition.Match produces ALL possible matches
            var positions = Empty.AddRange(Position(Underlying, +20),
                Position(Call[100], -4), Position(Put[105], -4),
                Position(Call[105], +4), Position(Put[110], +4),
                Position(Call[110], -3), Position(Put[115], -3),
                Position(Call[115], +3), Position(Put[120], +3),
                Position(Call[120], -5), Position(Put[125], -5),
                Position(Call[125], +5), Position(Put[130], +5),
                Position(Call[130], -9), Position(Put[135], -9),
                Position(Call[140], -21), Position(Put[145], -21)
            );

            // force lower strikes to be evaluated first to provide determinism for this test
            var options = OptionStrategyMatcherOptions.ForDefinitions(CoveredCall)
                .WithPositionEnumerator(new FunctionalOptionPositionCollectionEnumerator(
                    pos => pos.OrderBy(p => p.IsUnderlying ? 0 : p.Strike)
                ));

            var matches = CoveredCall.Match(options, positions).ToList();
            Assert.AreEqual(5, matches.Count);
            Assert.AreEqual(1, matches.Count(m => m.Multiplier == 4));
            Assert.AreEqual(1, matches.Count(m => m.Multiplier == 3));
            Assert.AreEqual(1, matches.Count(m => m.Multiplier == 5));
            Assert.AreEqual(1, matches.Count(m => m.Multiplier == 9));
            Assert.AreEqual(1, matches.Count(m => m.Multiplier == 20));
        }

        [Test]
        public void DoubleCountPositionsMatchingSamePositionMultipleTimesInDifferentMatches()
        {
            // this test aims to verify that we can match the same definition multiple times if positions allows
            // 1: -C110 +C105
            // 0: -C110 +C100
            // 2: -C115 +C105
            // 3: -C115 +C100
            var positions = Empty.AddRange(
                Position(Call[110], -1),
                Position(Call[115], -1),
                Position(Call[100]),
                Position(Call[105])
            );

            var matches = BearCallSpread.Match(positions).ToList();
            Assert.AreEqual(4, matches.Count);
            Assert.AreEqual(1, matches.Count(m => m.Legs[0].Position.Strike == 110 && m.Legs[1].Position.Strike == 105));
            Assert.AreEqual(1, matches.Count(m => m.Legs[0].Position.Strike == 110 && m.Legs[1].Position.Strike == 100));
            Assert.AreEqual(1, matches.Count(m => m.Legs[0].Position.Strike == 115 && m.Legs[1].Position.Strike == 105));
            Assert.AreEqual(1, matches.Count(m => m.Legs[0].Position.Strike == 115 && m.Legs[1].Position.Strike == 100));
        }

        private static string String(OptionPosition position)
        {
            var sign = position.Quantity > 0 ? "+" : "";

            var s = position.Symbol;
            var symbol = s.HasUnderlying
                ? $"{s.Underlying.Value}:{s.ID.OptionRight}@{s.ID.StrikePrice}:{s.ID.Date:MM-dd}"
                : s.Value;

            return $"{sign}{position.Quantity} {symbol}";
        }

        public static TestCaseData[] TestCases
        {
            get
            {
                return new[]
                {
                    TestCase.ExactPosition(BearCallSpread, Position(Call[110], -1), Position(Call[100], +1)),
                    TestCase.ExactPosition(BearCallSpread, Position(Call[100], +1), Position(Call[110], -1)),
                    TestCase.ExactPosition(BearPutSpread,  Position( Put[100], +1), Position( Put[110], -1)),
                    TestCase.ExactPosition(BearPutSpread,  Position( Put[110], -1), Position( Put[100], +1)),
                    TestCase.ExactPosition(BullCallSpread, Position(Call[110], +1), Position(Call[100], -1)),
                    TestCase.ExactPosition(BullCallSpread, Position(Call[100], -1), Position(Call[110], +1)),
                    TestCase.ExactPosition(BullPutSpread,  Position( Put[110], -1), Position( Put[100], +1)),
                    TestCase.ExactPosition(BullPutSpread,  Position( Put[100], +1), Position( Put[110], -1)),
                    TestCase.ExactPosition(Straddle,       Position(Call[100], +1), Position( Put[100], -1)),
                    TestCase.ExactPosition(Straddle,       Position( Put[100], -1), Position(Call[100], +1)),
                    TestCase.ExactPosition(CallButterfly,  Position(Call[100], +1), Position(Call[105], -2), Position(Call[110], +1)),
                    TestCase.ExactPosition(CallButterfly,  Position(Call[105], -2), Position(Call[100], +1), Position(Call[110], +1)),
                    TestCase.ExactPosition(CallButterfly,  Position(Call[110], +1), Position(Call[105], -2), Position(Call[100], +1)),
                    TestCase.ExactPosition(PutButterfly,   Position( Put[100], +1), Position( Put[105], -2), Position( Put[110], +1)),
                    TestCase.ExactPosition(PutButterfly,   Position( Put[105], -2), Position( Put[100], +1), Position( Put[110], +1)),
                    TestCase.ExactPosition(PutButterfly,   Position( Put[110], +1), Position( Put[105], -2), Position( Put[100], +1)),

                    TestCase.ExactPosition(CallCalendarSpread, Position(Call[100, 1], +1), Position(Call[100, 0], +1)),
                    TestCase.ExactPosition(CallCalendarSpread, Position(Call[100, 0], +1), Position(Call[100, 1], +1)),
                    TestCase.ExactPosition(PutCalendarSpread,  Position( Put[100, 1], +1), Position( Put[100, 0], +1)),
                    TestCase.ExactPosition(PutCalendarSpread,  Position( Put[100, 0], +1), Position( Put[100, 1], +1))

                }.Select(x => new TestCaseData(x).SetName(x.Name)).ToArray();
            }
        }

        // aim for perfect match, extra quantity in position, extra symbol position, no match/missing leg
        public class TestCase
        {
            private static readonly Dictionary<string, int> NameCounts
                = new Dictionary<string, int>();

            public string Name { get; }
            public OptionStrategyDefinition Definition { get; }
            public OptionPositionCollection Positions { get; }
            public IReadOnlyList<OptionPosition> Extra { get; }
            public IReadOnlyList<OptionPosition> Missing { get; }
            public IReadOnlyDictionary<string, int> ExpectedMatches { get; }
            public OptionStrategyMatcher CreateMatcher()
                => new OptionStrategyMatcher(new OptionStrategyMatcherOptions(
                    new[] {Definition}, new List<int> {100, 100, 100, 100}
                ));

            private readonly string _methodName;

            private TestCase(
                string methodName,
                OptionStrategyDefinition definition,
                IReadOnlyList<OptionPosition> positions,
                IReadOnlyList<OptionPosition> extra,
                IReadOnlyList<OptionPosition> missing
                )
            {
                _methodName = methodName;

                Extra = extra;
                Missing = missing;
                Definition = definition;
                Positions = FromPositions(positions);
                var suffix = positions.Select(p =>
                {
                    var quantity = p.Quantity.ToString(CultureInfo.InvariantCulture);
                    if (p.Quantity > 0)
                    {
                        quantity = $"+{quantity}";
                    }

                    if (p.IsUnderlying)
                    {
                        return $"{quantity}{p.Symbol.Value}";
                    }

                    return $"{quantity}{p.Right.ToString()[0]}@{p.Strike}";
                });
                Name = $"{definition.Name}:{methodName}({string.Join(", ", suffix)})";

                //int count;
                //if (NameCounts.TryGetValue(Name, out count))
                //{
                //    // test runner doesn't like duplicate names -- ensure uniqueness by counting instances of names
                //    count++;
                //}

                //NameCounts[Name] = count;
                //Name = $"{Name} ({count})";
                ExpectedMatches = new Dictionary<string, int>
                {
                    {nameof(ExactPosition),   1},
                    {nameof(ExtraQuantity),   1},
                    {nameof(ExtraPosition),   1},
                    {nameof(MissingPosition), 0}
                };
            }

            public void AssertMatch(List<OptionStrategyDefinitionMatch> matches)
            {
                switch (_methodName)
                {
                    case nameof(ExactPosition):
                        Assert.AreEqual(ExpectedMatches[_methodName], matches.Count);
                        Assert.AreEqual(Definition, matches[0].Definition);
                        break;

                    case nameof(ExtraQuantity):
                        Assert.AreEqual(ExpectedMatches[_methodName], matches.Count);
                        Assert.AreEqual(Definition, matches[0].Definition);
                        break;

                    case nameof(ExtraPosition):
                        Assert.AreEqual(ExpectedMatches[_methodName], matches.Count);
                        Assert.AreEqual(Definition, matches[0].Definition);
                        break;

                    case nameof(MissingPosition):
                        Assert.AreEqual(0, matches.Count);
                        break;

                    default:
                        Assert.Fail("Failed to perform assertion.");
                        break;
                }
            }

            public override string ToString()
            {
                return Name;
            }

            public static TestCase ExactPosition(OptionStrategyDefinition definition, params OptionPosition[] positions)
            {
                return new TestCase(nameof(ExactPosition), definition, positions, Array.Empty<OptionPosition>(), Array.Empty<OptionPosition>());
            }

            public static TestCase ExtraQuantity(OptionStrategyDefinition definition, params OptionPosition[] positions)
            {
                // add 1 to the first position
                var extra = positions[0].WithQuantity(1);
                var pos = positions.Select((p, i) => i == 0 ? p + extra : p).ToList();
                return new TestCase(nameof(ExtraQuantity), definition, pos, new[] {extra}, Array.Empty<OptionPosition>());
            }

            public static TestCase ExtraPosition(OptionStrategyDefinition definition, params OptionPosition[] positions)
            {
                // add a random position w/ the same underlying
                var maxStrike = positions.Where(p => p.Symbol.HasUnderlying).Max(p => p.Strike);
                var extra = new OptionPosition(positions[0].Symbol.WithStrike(maxStrike + 5m), 1);
                var pos = positions.Concat(new[] {extra}).ToList();
                return new TestCase(nameof(ExtraPosition), definition, pos, new[] {extra}, Array.Empty<OptionPosition>());
            }

            public static TestCase MissingPosition(OptionStrategyDefinition definition, OptionPosition missing, params OptionPosition[] positions)
            {
                return new TestCase(nameof(MissingPosition), definition, positions, Array.Empty<OptionPosition>(), new []{missing});
            }
        }
    }
}
