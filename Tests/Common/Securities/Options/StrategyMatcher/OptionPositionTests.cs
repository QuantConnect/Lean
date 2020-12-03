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
using NUnit.Framework;
using QuantConnect.Securities.Option.StrategyMatcher;
using static QuantConnect.Tests.Common.Securities.Options.StrategyMatcher.Option;

namespace QuantConnect.Tests.Common.Securities.Options.StrategyMatcher
{
    [TestFixture]
    public class OptionPositionTests
    {
        [Test]
        [TestCase(OptionRight.Put)]
        [TestCase(OptionRight.Call)]
        public void Initializes_OptionRight(OptionRight right)
        {
            // grab a random symbol and make it the correct right
            var symbol = Call[100].WithRight(right);
            var position = new OptionPosition(symbol, 1);
            Assert.AreEqual(right, position.Right);
        }

        [Test]
        [TestCase(PositionSide.Long)]
        [TestCase(PositionSide.None)]
        [TestCase(PositionSide.Short)]
        public void Initializes_OptionRight(PositionSide side)
        {
            // grab a random symbol and make it the correct right
            var quantity = (int) side;
            var position = new OptionPosition(Call[100], quantity);
            Assert.AreEqual(side, position.Side);
        }

        [Test]
        public void AdditionOperator_AddsQuantity_WhenSymbolsMatch()
        {
            var left = new OptionPosition(Symbols.SPY, 42);
            var right = new OptionPosition(Symbols.SPY, 1);
            var sum = left + right;
            Assert.AreEqual(Symbols.SPY, sum.Symbol);
            Assert.AreEqual(43, sum.Quantity);
        }

        [Test]
        public void AdditionOperator_ThrowsInvalidOperationException_WhenSymbolsDoNotMatch()
        {
            OptionPosition sum;
            var left = new OptionPosition(Symbols.SPY, 42);
            var right = new OptionPosition(Symbols.SPY_P_192_Feb19_2016, 1);
            Assert.Throws<InvalidOperationException>(
                () => sum = left + right
            );
        }

        [Test]
        public void AdditionOperator_DoesNotThrow_WhenOneSideEqualsDefault()
        {
            var value = new OptionPosition(Symbols.SPY, 42);
            var defaultValue = default(OptionPosition);
            var valueFirst = value + defaultValue;
            var defaultFirst = defaultValue + value;

            Assert.AreEqual(value, valueFirst);
            Assert.AreEqual(value, defaultFirst);
        }

        [Test]
        public void SubtractionOperator_SubtractsQuantity_WhenSymbolsMatch()
        {
            var left = new OptionPosition(Symbols.SPY, 42);
            var right = new OptionPosition(Symbols.SPY, 1);
            var sum = left - right;
            Assert.AreEqual(Symbols.SPY, sum.Symbol);
            Assert.AreEqual(41, sum.Quantity);
        }

        [Test]
        public void SubtractionOperator_ThrowsInvalidOperationException_WhenSymbolsDoNotMatch()
        {
            OptionPosition difference;
            var left = new OptionPosition(Symbols.SPY, 42);
            var right = new OptionPosition(Symbols.SPY_P_192_Feb19_2016, 1);
            Assert.Throws<InvalidOperationException>(
                () => difference = left - right
            );
        }

        [Test]
        public void SubtractionOperator_DoesNotThrow_WhenOneSideEqualsDefault()
        {
            var value = new OptionPosition(Symbols.SPY, 42);
            var defaultValue = default(OptionPosition);
            var valueFirst = value - defaultValue;
            var defaultFirst = defaultValue - value;

            Assert.AreEqual(value, valueFirst);
            Assert.AreEqual(value.Negate(), defaultFirst);
        }

        [Test]
        public void Negate_ReturnsOptionPosition_WithSameSymbolAndNegativeQuantity()
        {
            var position = new OptionPosition(Symbols.SPY, 42);
            var negated = position.Negate();
            Assert.AreEqual(position.Symbol, negated.Symbol);
            Assert.AreEqual(-position.Quantity, negated.Quantity);
        }

        [Test]
        public void MultiplyOperator_ScalesQuantity()
        {
            const int factor = 2;
            var position = new OptionPosition(Symbols.SPY, 42);
            var positionFirst = position * factor;
            Assert.AreEqual(position.Symbol, positionFirst.Symbol);
            Assert.AreEqual(factor * 42, positionFirst.Quantity);

            var factorFirst = factor * position;
            Assert.AreEqual(positionFirst, factorFirst);
        }

        [Test]
        public void Equality_IsDefinedUsing_SymbolAndQuantity()
        {
            var left = new OptionPosition(Symbols.SPY, 42);
            var right = new OptionPosition(Symbols.SPY, 42);
            Assert.AreEqual(left, right);
            Assert.IsTrue(left == right);

            right = right.Negate();
            Assert.AreNotEqual(left, right);
            Assert.IsTrue(left != right);
        }

        [Test]
        public void None_CreatesOptionPosition_WithZeroQuantity()
        {
            var none = OptionPosition.None(Symbols.SPY);
            Assert.AreEqual(0, none.Quantity);
            Assert.AreEqual(Symbols.SPY, none.Symbol);
        }
    }
}
