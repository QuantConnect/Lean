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

namespace QuantConnect.Tests.Common.Orders
{
    [TestFixture]
    public class InteractiveBrokersFixOrderPropertiesTests
    {
        [Test]
        public void CloneWhenFinancialAdvisorPropertiesAreSetReturnsFixPropertiesWithSameValues()
        {
            // Arrange
            var properties = new InteractiveBrokersFixOrderProperties
            {
                Account = "DU1",
                FaGroup = "TestGroup",
                FaMethod = "NetLiq",
                TimeInForce = TimeInForce.Day
            };

            // Act
            var clone = properties.Clone() as InteractiveBrokersFixOrderProperties;

            // Assert
            Assert.That(clone, Is.Not.Null);
            Assert.That(clone, Is.Not.SameAs(properties));
            Assert.That(clone, Is.InstanceOf<InteractiveBrokersOrderProperties>());
            Assert.That(clone.Account, Is.EqualTo("DU1"));
            Assert.That(clone.FaGroup, Is.EqualTo("TestGroup"));
            Assert.That(clone.FaMethod, Is.EqualTo("NetLiq"));
            Assert.That(clone.TimeInForce, Is.EqualTo(properties.TimeInForce));
        }
    }
}
