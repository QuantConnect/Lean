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
using QuantConnect.Interfaces;
using QuantConnect.Orders;

namespace QuantConnect.Tests.Common.Orders
{
    [TestFixture]
    public class FixOrderPropertiesTests
    {
        [Test]
        public void BloombergFixOrderPropertiesSupportsAdditionalPropertiesAndClone()
        {
            var properties = new BloombergFixOrderProperties();
            properties.AdditionalProperties["9301"] = "1";

            var clone = (BloombergFixOrderProperties)properties.Clone();

            Assert.IsInstanceOf<FixOrderProperties>(clone);
            Assert.IsInstanceOf<IOrderProperties>(clone);
            Assert.AreEqual("1", clone.AdditionalProperties["9301"]);

            properties.AdditionalProperties["9301"] = "2";
            Assert.AreEqual("1", clone.AdditionalProperties["9301"]);
        }
    }
}
