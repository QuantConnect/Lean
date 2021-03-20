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

using NUnit.Framework;
using QuantConnect.Orders;
using QuantConnect.Securities.Option;
using QuantConnect.Securities.Option.StrategyMatcher;

namespace QuantConnect.Tests.Common.Securities.Options.StrategyMatcher
{
    [TestFixture]
    public class OptionStrategyLegDefinitionMatchTests
    {
        [Test]
        public void CreatesOptionOptionStrategyLegData()
        {
            var match = new OptionStrategyLegDefinitionMatch(3, Option.Position(Option.Call[100], 3));
            var leg = match.CreateOptionStrategyLeg(3);
            Assert.IsInstanceOf<OptionStrategy.OptionLegData>(leg);
            Assert.AreEqual(3, leg.Quantity);
            Assert.AreEqual(0, leg.OrderPrice);
            Assert.AreEqual(leg.OrderType, OrderType.Market);
        }

        [Test]
        public void CreatesUnderlyingOptionStrategyLegData()
        {
            var match = new OptionStrategyLegDefinitionMatch(3, Option.Position(Option.Underlying, 3));
            var leg = match.CreateOptionStrategyLeg(3);
            Assert.IsInstanceOf<OptionStrategy.UnderlyingLegData>(leg);
            Assert.AreEqual(3, leg.Quantity);
            Assert.AreEqual(0, leg.OrderPrice);
            Assert.AreEqual(leg.OrderType, OrderType.Market);
        }

        [Test]
        public void CreateOptionStrategyLeg_RespectsProvidedMultiplier()
        {
            // multiplier of 2 w/ position quantity of 4 means leg definition quantity is +2
            // so we request a multiplier of 1 and except +2 leg quantity
            var match = new OptionStrategyLegDefinitionMatch(2, Option.Position(Option.Underlying, 4));
            var leg = match.CreateOptionStrategyLeg(1);
            Assert.IsInstanceOf<OptionStrategy.UnderlyingLegData>(leg);
            Assert.AreEqual(2, leg.Quantity);
            Assert.AreEqual(0, leg.OrderPrice);
            Assert.AreEqual(leg.OrderType, OrderType.Market);
        }
    }
}