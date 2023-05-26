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
 *
*/

using System;
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;

using QuantConnect.Securities.Positions;
using QuantConnect.Securities.Option;

namespace QuantConnect.Tests.Common.Securities.Positions
{
    [TestFixture, Parallelizable(ParallelScope.All)]
    public class PositionGroupTests
    {
        [TestCase(new[] { 10, 55 }, new[] { 1, 5 })]
        [TestCase(new[] { 10, -20, 4 }, new[] { 1, 2, 1 })]
        [TestCase(new[] { 10, -20, 4 }, new[] { 1, 2, 4 })]
#if !DEBUG
        [Ignore("Not on debug mode")]
#endif
        public void PositionGroupCreationThrowsOnInvalidRatio(int[] positionsQuantities, int[] positionsUnitQuantities)
        {
            var symbols = GetSymbols(positionsQuantities.Length);

            Assert.Throws<ArgumentException>(
                () => new PositionGroup(
                    new OptionStrategyPositionGroupBuyingPowerModel(null),
                    positionsQuantities.GreatestCommonDivisor(),
                    positionsUnitQuantities
                        .Select((positionUnitQuantity, i) => new Position(symbols[i], positionsQuantities[i], positionUnitQuantity))
                        .ToArray()));
        }

        [TestCase(10, new[] { 1, 5 }, new[] { 10 * 1, 10 * 5 }, new[] { 1, 5 })]
        [TestCase(-10, new[] { 1, 5 }, new[] { -10 * 1, -10 * 5 }, new[] { 1, 5 })]
        [TestCase(10, new[] { -1, 5 }, new[] { 10 * -1, 10 * 5 }, new[] { 1, 5 })]
        [TestCase(-10, new[] { -1, 5 }, new[] { -10 * -1, -10 * 5 }, new[] { 1, 5 })]
        public void PositionGroupCreation(int groupQuantity, int[] positionsUnitQuantities, int[] expectedPositionsQuantities,
            int[] expectedPositionsUnitQuantities)
        {
            var symbols = GetSymbols(positionsUnitQuantities.Length);
            var group = CreatePositionGroup(groupQuantity, symbols, positionsUnitQuantities);
            var expectedPositions = symbols
                .Select((symbol, i) => new Position(symbol, expectedPositionsQuantities[i], expectedPositionsUnitQuantities[i]))
                .Cast<IPosition>()
                .ToList();

            AssertPositionGroup(group, groupQuantity, expectedPositions);
        }

        [TestCase(10, new[] { 1, 5 }, 5, new[] { 1, 5 }, true)]
        [TestCase(10, new[] { 1, 5 }, 0, new[] { 1, 5 }, true)]
        [TestCase(10, new[] { 1, 5 }, -5, new[] { 1, 5 }, true)]
        [TestCase(10, new[] { 1, 5 }, 15, new[] { 1, 5 }, false)]
        [TestCase(-10, new[] { 1, 5 }, -5, new[] { 1, 5 }, true)]
        [TestCase(-10, new[] { 1, 5 }, 0, new[] { 1, 5 }, true)]
        [TestCase(-10, new[] { 1, 5 }, 5, new[] { 1, 5 }, true)]
        [TestCase(-10, new[] { 1, 5 }, -15, new[] { 1, 5 }, false)]
        [TestCase(10, new[] { 1, 5 }, -5, new[] { -1, -5 }, true)]
        [TestCase(10, new[] { 1, 5 }, 0, new[] { -1, -5 }, true)]
        [TestCase(10, new[] { 1, 5 }, 5, new[] { -1, -5 }, true)]
        [TestCase(10, new[] { 1, 5 }, -15, new[] { -1, -5 }, false)]
        [TestCase(-10, new[] { 1, 5 }, 5, new[] { -1, -5 }, true)]
        [TestCase(-10, new[] { 1, 5 }, 0, new[] { -1, -5 }, true)]
        [TestCase(-10, new[] { 1, 5 }, -5, new[] { -1, -5 }, true)]
        [TestCase(-10, new[] { 1, 5 }, 15, new[] { -1, -5 }, false)]
        [TestCase(10, new[] { 1, 5 }, 5, new[] { -1, 5 }, false)]
        [TestCase(10, new[] { 1, 5 }, -5, new[] { -1, 5 }, false)]
        [TestCase(10, new[] { 1, 5 }, 15, new[] { -1, 5 }, false)]
        [TestCase(-10, new[] { 1, 5 }, 5, new[] { -1, 5 }, false)]
        [TestCase(-10, new[] { 1, 5 }, -5, new[] { -1, 5 }, false)]
        [TestCase(-10, new[] { 1, 5 }, 15, new[] { -1, 5 }, false)]
        [TestCase(10, new[] { -1, 5 }, 5, new[] { -1, 5 }, true)]
        [TestCase(10, new[] { -1, 5 }, 0, new[] { -1, 5 }, true)]
        [TestCase(10, new[] { -1, 5 }, -5, new[] { -1, 5 }, true)]
        [TestCase(10, new[] { -1, 5 }, 15, new[] { -1, 5 }, false)]
        [TestCase(-10, new[] { -1, 5 }, -5, new[] { -1, 5 }, true)]
        [TestCase(-10, new[] { -1, 5 }, 0, new[] { -1, 5 }, true)]
        [TestCase(-10, new[] { -1, 5 }, 5, new[] { -1, 5 }, true)]
        [TestCase(-10, new[] { -1, 5 }, -15, new[] { -1, 5 }, false)]
        [TestCase(10, new[] { -1, 5 }, -5, new[] { 1, -5 }, true)]
        [TestCase(10, new[] { -1, 5 }, 0, new[] { 1, -5 }, true)]
        [TestCase(10, new[] { -1, 5 }, 5, new[] { 1, -5 }, true)]
        [TestCase(10, new[] { -1, 5 }, -15, new[] { 1, -5 }, false)]
        [TestCase(-10, new[] { -1, 5 }, 5, new[] { 1, -5 }, true)]
        [TestCase(-10, new[] { -1, 5 }, 0, new[] { 1, -5 }, true)]
        [TestCase(-10, new[] { -1, 5 }, -5, new[] { 1, -5 }, true)]
        [TestCase(-10, new[] { -1, 5 }, 15, new[] { 1, -5 }, false)]
        public void PositionGroupClosesAnother(int initialGroupQuantity, int[] initialGroupPositionsUnitQuantities,
            int finalGroupQuantity, int[] finalGroupPositionsUnitQuantities, bool expectedResult)
        {
            Assert.AreEqual(initialGroupPositionsUnitQuantities.Length, finalGroupPositionsUnitQuantities.Length);

            var symbols = GetSymbols(initialGroupPositionsUnitQuantities.Length);
            var initialGroup = CreatePositionGroup(initialGroupQuantity, symbols, initialGroupPositionsUnitQuantities);
            var finalGroup = CreatePositionGroup(finalGroupQuantity, symbols, finalGroupPositionsUnitQuantities);

            Assert.AreEqual(expectedResult, finalGroup.Closes(initialGroup));
        }

        private static List<Symbol> GetSymbols(int count)
        {
            var baseExpiry = new DateTime(2023, 05, 19);
            return Enumerable.Range(0, count).Select(i => Symbols.CreateOptionSymbol("SPY", OptionRight.Call, 300, baseExpiry.AddMonths(i))).ToList();
        }

        private static IPositionGroup CreatePositionGroup(int quantity, List<Symbol> symbols, int[] positionsUnitQuantities)
        {
            Assert.IsNotEmpty(positionsUnitQuantities);
            Assert.AreEqual(positionsUnitQuantities.Length, symbols.Count);

            return new PositionGroup(
                new OptionStrategyPositionGroupBuyingPowerModel(null),
                quantity,
                positionsUnitQuantities
                    .Select((positionUnitQuantity, i) => new Position(symbols[i], quantity * positionUnitQuantity, Math.Abs(positionUnitQuantity)))
                    .ToArray());
        }

        /// <summary>
        /// Asserts that the specified group has the expected quantity and positions
        /// </summary>
        private static void AssertPositionGroup(IPositionGroup group, int expectedQuantity, List<IPosition> expectedPositions)
        {
            var expectedAbsQuantity = Math.Abs(expectedQuantity);
            Assert.AreEqual(expectedAbsQuantity, Math.Abs(group.Quantity));
            Assert.AreEqual(expectedPositions.Count, group.Count);

            foreach (var expectedPosition in expectedPositions)
            {
                var position = group.GetPosition(expectedPosition.Symbol);
                Assert.AreEqual(expectedPosition.Quantity, position.Quantity);
                Assert.AreEqual(expectedPosition.UnitQuantity, position.UnitQuantity);

                // The position group quantity should be a ratio shared by all positions
                Assert.AreEqual(expectedAbsQuantity, Math.Abs(position.Quantity) / position.UnitQuantity);
            }
        }
    }
}
