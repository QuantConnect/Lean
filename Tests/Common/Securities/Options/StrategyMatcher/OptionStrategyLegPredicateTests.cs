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
using System.Linq.Expressions;
using NUnit.Framework;
using QuantConnect.Securities;
using QuantConnect.Securities.Option.StrategyMatcher;
using static QuantConnect.Tests.Common.Securities.Options.StrategyMatcher.Option;

namespace QuantConnect.Tests.Common.Securities.Options.StrategyMatcher
{
    [TestFixture]
    public class OptionStrategyLegPredicateTests
    {
        public const decimal DefaultLegStrike = 100m;
        public const decimal DefaultPositionStrike = 95m;

        private static readonly OptionPositionCollection Positions
            = OptionPositionCollection.Create(Symbols.SPY, ContractMultiplier, Enumerable.Empty<SecurityHolding>())
                .Add(new OptionPosition(Symbols.SPY, 1000))
                .Add(new OptionPosition(Put[95m], 1))
                .Add(new OptionPosition(Put[95m, 1], 1))
                .Add(new OptionPosition(Put[95m, 2], 1))
                .Add(new OptionPosition(Call[95m], 1))
                .Add(new OptionPosition(Call[95m, 1], 1))
                .Add(new OptionPosition(Call[95m, 2], 1))
                .Add(new OptionPosition(Put[100m], 1))
                .Add(new OptionPosition(Put[100m, 1], 1))
                .Add(new OptionPosition(Put[100m, 2], 1))
                .Add(new OptionPosition(Call[100m], 1))
                .Add(new OptionPosition(Call[100m, 1], 1))
                .Add(new OptionPosition(Call[100m, 2], 1))
                .Add(new OptionPosition(Put[105m], 1))
                .Add(new OptionPosition(Put[105m, 1], 1))
                .Add(new OptionPosition(Put[105m, 2], 1))
                .Add(new OptionPosition(Call[105m], 1))
                .Add(new OptionPosition(Call[105m, 1], 1))
                .Add(new OptionPosition(Call[105m, 2], 1));

        [Test]
        public void CreatesStrikePredicate()
        {
            var predicate = OptionStrategyLegPredicate.Create(
                (legs, p) => p.Strike < legs[0].Strike
            );

            var position = new OptionPosition(Put[95m], 1);
            var positiveSet = new List<OptionPosition> {new OptionPosition(Put[100m], 1)};
            Assert.IsTrue(predicate.Matches(positiveSet, position));

            var negativeSet = new List<OptionPosition> {new OptionPosition(Put[90m], 1)};
            Assert.IsFalse(predicate.Matches(negativeSet, position));
        }

        [Test]
        [TestCaseSource(nameof(TestCases))]
        public void CreatesAndEvaluatesPredicateAccordingToProvidedExpression(TestCase testCase)
        {
            // creates expected reference value provider w/ correct target
            var referenceValue = testCase.CreateReferenceValue();
            Assert.AreEqual(testCase.Target, referenceValue.Target);

            // reference value provider resolves expected reference value
            var expectedReferenceValue = testCase.ReferenceValueProvider(testCase.Legs);
            var actualReferenceValue = referenceValue.Resolve(testCase.Legs);
            Assert.AreEqual(expectedReferenceValue, actualReferenceValue);

            // creates predicate and matches as expected
            var predicate = testCase.CreatePredicate();
            var testCaseMatch = predicate.Matches(testCase.Legs, testCase.Position);
            Assert.AreEqual(testCase.Match, testCaseMatch,
                $"Predicate: {predicate}{Environment.NewLine}" +
                $"Position: {testCase.Position}{Environment.NewLine}" +
                $"Legs: {string.Join(Environment.NewLine, testCase.Legs)}"
            );

            // filters positions collection as expected
            var filtered = predicate.Filter(testCase.Legs, Positions, false);

            // verify items NOT in the filtered set fail the predicate
            // verify items IN the filtered set pass the predicate
            foreach (var position in Positions)
            {
                // if it's in the filtered set then it better match the predicate, and vice-versa
                Assert.AreEqual(
                    filtered.HasPosition(position.Symbol),
                    predicate.Matches(testCase.Legs, position)
                );
            }
        }

        [Test]
        public void CreatesOptionRightPredicate()
        {
            var definition = OptionStrategyDefinition.Create("CallsOnly", OptionStrategyDefinition.CallLeg(1));
            var onlyLeg = definition.Legs.Single();
            // put/call isn't phrased as a predicate since every one has it, also due to complexities w/ enums in expressions
            Assert.IsEmpty(onlyLeg);
            var filtered = onlyLeg.Filter(Array.Empty<OptionPosition>(), Positions, false);
            Assert.IsTrue(filtered.All(p => p.Right == OptionRight.Call));
        }

        public static TestCaseData[] TestCases
        {
            get
            {
                // many of these cases are logically the same, just with the binary comparison operator flipped
                // this is done to verify that the underlying infrastructure is agnostic to where the positions/legs
                // parameter expressions appear in the comparison expression.
                return new[]
                    {
                        new TestCase((legs, position) => position.Strike < legs[0].Strike)
                            .WithTarget(PredicateTargetValue.Strike, legs => legs[0].Strike)
                            .ExpectMatch(),

                        new TestCase((legs, position) => position.Strike > legs[0].Strike)
                            .WithTarget(PredicateTargetValue.Strike, legs => legs[0].Strike)
                            .ExpectNoMatch(),

                        new TestCase((legs, position) => legs[0].Strike > position.Strike)
                            .WithTarget(PredicateTargetValue.Strike, legs => legs[0].Strike)
                            .ExpectMatch(),

                        new TestCase((legs, position) => legs[0].Strike < position.Strike)
                            .WithTarget(PredicateTargetValue.Strike, legs => legs[0].Strike)
                            .ExpectNoMatch(),

                        new TestCase((legs, position) => position.Expiration < legs[0].Expiration)
                            .WithTarget(PredicateTargetValue.Expiration, legs => legs[0].Expiration)
                            .ExpectNoMatch(),

                        new TestCase((legs, position) => position.Expiration > legs[0].Expiration)
                            .WithTarget(PredicateTargetValue.Expiration, legs => legs[0].Expiration)
                            .ExpectNoMatch(),

                        new TestCase((legs, position) => legs[0].Expiration > position.Expiration)
                            .WithTarget(PredicateTargetValue.Expiration, legs => legs[0].Expiration)
                            .ExpectNoMatch(),

                        new TestCase((legs, position) => legs[0].Expiration < position.Expiration)
                            .WithTarget(PredicateTargetValue.Expiration, legs => legs[0].Expiration)
                            .ExpectNoMatch(),

                        new TestCase((legs, position) => position.Expiration == legs[0].Expiration)
                            .WithTarget(PredicateTargetValue.Expiration, legs => legs[0].Expiration)
                            .ExpectMatch(),

                        new TestCase((legs, position) => position.Expiration == legs[0].Expiration)
                            .WithTarget(PredicateTargetValue.Expiration, legs => legs[0].Expiration)
                            .ExpectMatch(),

                        new TestCase((legs, position) => legs[0].Expiration == position.Expiration)
                            .WithTarget(PredicateTargetValue.Expiration, legs => legs[0].Expiration)
                            .ExpectMatch(),

                        new TestCase((legs, position) => legs[0].Expiration == position.Expiration)
                            .WithTarget(PredicateTargetValue.Expiration, legs => legs[0].Expiration)
                            .ExpectMatch(),
                    }
                    .Select(x => new TestCaseData(x.WithDefaults()).SetName(x.Name))
                    .ToArray();
            }
        }


        public class TestCase
        {
            public bool Match { get; private set; }
            public string Name { get; private set; }
            public List<OptionPosition> Legs { get; }
            public OptionPosition Position { get; private set; }
            public PredicateTargetValue Target { get; private set; }
            public Func<List<OptionPosition>, OptionPosition, bool> Predicate { get; }
            public Func<List<OptionPosition>, object> ReferenceValueProvider { get; private set; }
            public Expression<Func<IReadOnlyList<OptionPosition>, OptionPosition, bool>> Expression { get; }
            public ParameterExpression LegsExpression => Expression.Parameters[0];
            public ParameterExpression PositionExpression => Expression.Parameters[1];
            public Expression BinaryComparisonExpression => Expression.Body;

            private readonly Lazy<OptionStrategyLegPredicate> _predicate;

            public IOptionStrategyLegPredicateReferenceValue CreateReferenceValue()
                => _predicate.Value.GetReferenceValue();

            public OptionStrategyLegPredicate CreatePredicate()
                => _predicate.Value;

            public TestCase(Expression<Func<IReadOnlyList<OptionPosition>, OptionPosition, bool>> expression)
            {
                Expression = expression;
                Name = expression.ToString();
                Predicate = expression.Compile();
                Legs = new List<OptionPosition>();
                _predicate = new Lazy<OptionStrategyLegPredicate>(
                    () => OptionStrategyLegPredicate.Create(Expression)
                );
            }

            public TestCase WithTarget(
                PredicateTargetValue target,
                Func<List<OptionPosition>, object> referenceValueProvider
                )
            {
                Target = target;
                ReferenceValueProvider = referenceValueProvider;
                return this;
            }

            public TestCase WithName(string name)
            {
                Name = name;
                return this;
            }

            public TestCase AddLeg(Symbol symbol, int quantity)
            {
                Legs.Add(new OptionPosition(symbol, quantity));
                return this;
            }

            public TestCase WithPosition(Symbol symbol, int quantity)
            {
                if (Position != default(OptionPosition))
                {
                    throw new InvalidOperationException($"Position has already been initialized: {Position}");
                }

                Position = new OptionPosition(symbol, quantity);
                return this;
            }

            public TestCase ExpectMatch()
            {
                Match = true;
                return this;
            }

            public TestCase ExpectNoMatch()
            {
                Match = false;
                return this;
            }

            public TestCase WithDefaults()
            {
                if (Legs.Count == 0)
                {
                    AddLeg(Put[DefaultLegStrike], 1);
                }

                if (Position == default(OptionPosition))
                {
                    WithPosition(Put[DefaultPositionStrike], 1);
                }

                return this;
            }
        }
    }
}
