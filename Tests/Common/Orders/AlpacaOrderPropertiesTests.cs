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
    public class AlpacaOrderPropertiesTests
    {
        [Test]
        public void ClientOrderIdIsNullByDefault()
        {
            var properties = new AlpacaOrderProperties();

            Assert.That(properties.ClientOrderId, Is.Null);
        }

        [Test]
        public void CloneKeepsTheClientOrderId()
        {
            var properties = new AlpacaOrderProperties { ClientOrderId = "my-algorithm-17", OutsideRegularTradingHours = true };

            var clone = (AlpacaOrderProperties)properties.Clone();

            Assert.That(clone, Is.Not.SameAs(properties));
            Assert.That(clone.ClientOrderId, Is.EqualTo("my-algorithm-17"));
            Assert.That(clone.OutsideRegularTradingHours, Is.True);
        }
    }
}
