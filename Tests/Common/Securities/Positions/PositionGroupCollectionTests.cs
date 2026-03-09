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

using System.Linq;
using NUnit.Framework;
using System.Collections.Generic;
using QuantConnect.Securities.Positions;

namespace QuantConnect.Tests.Common.Securities.Positions
{
    [TestFixture, Parallelizable(ParallelScope.All)]
    public class PositionGroupCollectionTests
    {
        [Test]
        public void CombineWith()
        {
            var collection = PositionGroupCollection.Empty;
            var positions = new IPosition[] { new Position(Symbols.SPY, 10, 1) };
            var group = new PositionGroup(new PositionGroupKey(new SecurityPositionGroupBuyingPowerModel(), positions), positions[0].Quantity, positions);
            var result = collection.CombineWith(new PositionGroupCollection(new []{ group }));

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.IsOnlyDefaultGroups);
            Assert.IsTrue(result.Contains(group.Key));

            IReadOnlyCollection<IPositionGroup> resultingGroups;
            Assert.IsTrue(result.TryGetGroups(Symbols.SPY, out resultingGroups));
            Assert.AreEqual(1, resultingGroups.Count);
            Assert.AreEqual(10, resultingGroups.Single().Positions.Single().Quantity);
        }

        [Test]
        public void CombineWith_Empty()
        {
            var collection = PositionGroupCollection.Empty;
            var newCollection = collection.CombineWith(PositionGroupCollection.Empty);

            Assert.AreEqual(0, newCollection.Count);
        }

        [Test]
        public void AddTwice()
        {
            var collection = PositionGroupCollection.Empty;

            var positions = new IPosition[] {new Position(Symbols.SPY, 10, 1)};
            var group = new PositionGroup(new PositionGroupKey(new SecurityPositionGroupBuyingPowerModel(), positions), positions[0].Quantity, positions);
            var newCollection = collection.Add(group);
            var result = newCollection.Add(group);

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.IsOnlyDefaultGroups);
            Assert.IsTrue(result.Contains(group.Key));

            IReadOnlyCollection<IPositionGroup> resultingGroups;
            Assert.IsTrue(result.TryGetGroups(Symbols.SPY, out resultingGroups));
            Assert.AreEqual(1, resultingGroups.Count);
            Assert.AreEqual(10, resultingGroups.Single().Positions.Single().Quantity);
        }

        [Test]
        public void AddDifferentQuantity()
        {
            var collection = PositionGroupCollection.Empty;

            var positions = new IPosition[] { new Position(Symbols.SPY, 10, 1) };
            var group = new PositionGroup(new PositionGroupKey(new SecurityPositionGroupBuyingPowerModel(), positions), positions[0].Quantity, positions);
            var newCollection = collection.Add(group);

            var positions2 = new IPosition[] { new Position(Symbols.SPY, 20, 1) };
            var group2 = new PositionGroup(new PositionGroupKey(new SecurityPositionGroupBuyingPowerModel(), positions2), positions2[0].Quantity, positions2);
            var result = newCollection.Add(group2);

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.IsOnlyDefaultGroups);
            Assert.IsTrue(result.Contains(group.Key));
        }
    }
}
