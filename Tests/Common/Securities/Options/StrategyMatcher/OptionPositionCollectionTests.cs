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
using System.Linq.Expressions;
using NUnit.Framework;
using QuantConnect.Securities;
using QuantConnect.Securities.Option.StrategyMatcher;
using QuantConnect.Util;
using static QuantConnect.BinaryComparison;
using static QuantConnect.Tests.Common.Securities.Options.StrategyMatcher.Option;

namespace QuantConnect.Tests.Common.Securities.Options.StrategyMatcher
{
    [TestFixture]
    public class OptionPositionCollectionTests
    {
        private const int UnderlyingLots = 10;
        private const int ContractMultiplier = 100;
        private static readonly DateTime Reference = new DateTime(2020, 10, 16);

        private SecurityHolding[] _holdings;
        private OptionPositionCollection _positions;

        [SetUp]
        public void Setup()
        {
            _holdings = new[]
            {
                CreateHolding(1),
                CreateHolding(2),
                CreateHolding(3),
                CreateHolding(4),
                CreateHolding(Symbols.SPY, ContractMultiplier * UnderlyingLots)
            };

            _positions = OptionPositionCollection.Create(Symbols.SPY, ContractMultiplier, _holdings);
        }

        [Test]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(10)]
        public void IsEmpty_ReturnsTrue_WhenCountIsZero(int count)
        {
            var positions = OptionPositionCollection.Empty;
            for (int i = 0; i < count; i++)
            {
                var position = new OptionPosition(Call[100 + i], 1 + i);
                positions = positions.Add(position);
            }

            Assert.AreEqual(count, positions.Count);
            Assert.AreEqual(count == 0, positions.IsEmpty);
        }

        [Test]
        public void Create_InitializesNewInstance_FromSecurityHoldings()
        {
            Assert.AreEqual(5, _positions.Count);
            Assert.AreEqual(2, _positions.UniquePuts);
            Assert.AreEqual(2, _positions.UniqueCalls);
            Assert.AreEqual(3, _positions.UniqueExpirations);
            Assert.AreEqual(UnderlyingLots, _positions.UnderlyingQuantity);
        }

        [Test]
        public void Slice_FiltersByRight()
        {
            var puts = _positions.Slice(OptionRight.Put);
            Assert.AreEqual(3, puts.Count);
            Assert.AreEqual(0, puts.UniqueCalls);
            Assert.AreEqual(2, puts.UniquePuts);
            Assert.AreEqual(2, puts.UniqueExpirations);
            Assert.AreEqual(UnderlyingLots, puts.UnderlyingQuantity);

            var calls = _positions.Slice(OptionRight.Call);
            Assert.AreEqual(3, calls.Count);
            Assert.AreEqual(0, calls.UniquePuts);
            Assert.AreEqual(2, calls.UniqueCalls);
            Assert.AreEqual(2, calls.UniqueExpirations);
            Assert.AreEqual(UnderlyingLots, puts.UnderlyingQuantity);
        }

        [Test]
        public void Slice_FiltersByPositionSide()
        {
            var positions = OptionPositionCollection.Empty.AddRange(
                new OptionPosition(Call[100], +1),
                new OptionPosition(Call[105], -1),
                new OptionPosition( Put[110], +1),
                new OptionPosition( Put[115], -1)
            );
            var shorts = positions.Slice(PositionSide.Short);
            var expected = positions.Where(p => p.Side == PositionSide.Short).ToHashSet();
            foreach (var position in shorts)
            {
                Assert.IsTrue(expected.Remove(position));
            }
            Assert.IsEmpty(expected);

            var longs = positions.Slice(PositionSide.Long);
            expected = positions.Where(p => p.Side == PositionSide.Long).ToHashSet();
            foreach (var position in longs)
            {
                Assert.IsTrue(expected.Remove(position));
            }
            Assert.IsEmpty(expected);
        }

        [Test]
        [TestCase(ExpressionType.Equal, 2)]
        [TestCase(ExpressionType.NotEqual, 2)]
        [TestCase(ExpressionType.LessThan, 2)]
        [TestCase(ExpressionType.LessThanOrEqual, 2)]
        [TestCase(ExpressionType.GreaterThan, 2)]
        [TestCase(ExpressionType.GreaterThanOrEqual, 2)]
        public void Slice_FiltersByStrikePrice(ExpressionType type, decimal reference)
        {
            var comparison = FromExpressionType(type);
            var actual = _positions.Slice(comparison, reference);
            Assert.AreEqual(UnderlyingLots, actual.UnderlyingQuantity);

            var strikes = _positions.Where(p => p.Symbol.HasUnderlying).ToList(p => p.Strike);
            var expected = comparison.Filter(strikes, reference);

            var positions = actual.ToList();
            Assert.AreEqual(expected.Count + 1, positions.Count);
            foreach (var strike in expected)
            {
                Assert.IsTrue(positions.Any(
                    p => p.Symbol.HasUnderlying && p.Strike == strike
                ));
            }

            actual = _positions.Slice(comparison, reference, false);
            Assert.AreEqual(0, actual.UnderlyingQuantity);
            Assert.AreEqual(expected.Count, actual.Count);
        }

        [Test]
        [TestCase(ExpressionType.Equal, 2)]
        [TestCase(ExpressionType.NotEqual, 2)]
        [TestCase(ExpressionType.LessThan, 2)]
        [TestCase(ExpressionType.LessThanOrEqual, 2)]
        [TestCase(ExpressionType.GreaterThan, 2)]
        [TestCase(ExpressionType.GreaterThanOrEqual, 2)]
        public void Slice_FiltersByExpiration(ExpressionType type, int reference)
        {
            var expiration = Reference.AddDays((reference - 1) * 7);
            var comparison = FromExpressionType(type);
            var actual = _positions.Slice(comparison, expiration);
            Assert.AreEqual(UnderlyingLots, actual.UnderlyingQuantity);

            var expirations = _positions.Where(p => p.Symbol.HasUnderlying).ToList(p => p.Expiration);
            var expected = comparison.Filter(expirations, expiration);

            var positions = actual.ToList();
            Assert.AreEqual(expected.Count + 1, positions.Count);
            foreach (var exp in expected)
            {
                Assert.AreEqual(expected.Count(e => e == exp), positions.Count(
                    p => p.Symbol.HasUnderlying && p.Expiration == exp)
                );
            }

            actual = _positions.Slice(comparison, expiration, false);
            Assert.AreEqual(0, actual.UnderlyingQuantity);
            Assert.AreEqual(expected.Count, actual.Count);
        }

        private decimal _previousStrike;
        private OptionRight _previousRight;

        private static readonly CircularQueue<DateTime> Expirations = new CircularQueue<DateTime>(
            Reference, Reference.AddDays(7), Reference.AddDays(14)
        );

        private SecurityHolding CreateHolding(int quantity)
            => CreateHolding(Symbol.CreateOption(Symbols.SPY, Market.USA, OptionStyle.American, _previousRight.Invert(), _previousStrike + 1, Expirations.Dequeue()), quantity);

        private SecurityHolding CreateHolding(Symbol symbol, int quantity)
        {
            if (symbol.SecurityType == SecurityType.Option)
            {
                _previousRight = symbol.ID.OptionRight;
                _previousStrike = symbol.ID.StrikePrice;
            }

            var properties = SymbolProperties.GetDefault("USD");
            var cash = new Cash("USD", 0m, 1m);
            var security = new Security(symbol, null, cash, properties, null, null, new SecurityCache());
            var holding = new SecurityHolding(security, new IdentityCurrencyConverter("USD"));
            holding.SetHoldings(2, quantity);
            return holding;
        }
    }
}
