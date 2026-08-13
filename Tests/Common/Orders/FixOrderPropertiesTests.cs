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
using Python.Runtime;
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

        [Test]
        public void LocateTagPassthroughsSurviveTheDictionarySwap()
        {
            // The tags are the store behind the properties, so writing either way must be visible
            // from the other.
            var properties = new BloombergFixOrderProperties { LocateBroker = "MLCO" };
            Assert.AreEqual("MLCO", properties.AdditionalProperties["5700"]);

            properties.AdditionalProperties["114"] = "Y";
            Assert.AreEqual("Y", properties.LocateReqd);

            properties.LocateBroker = null;
            Assert.IsFalse(properties.AdditionalProperties.ContainsKey("5700"));
            Assert.IsNull(properties.LocateBroker);
        }

        [Test]
        public void UpdatesAdditionalPropertiesFromPlainPythonDictionary()
        {
            using (Py.GIL())
            {
                // pythonnet cannot convert a plain dict for assignment, so update() is what lets a
                // Python algorithm bulk load its custom tags.
                var module = PyModule.FromString("fixAdditionalPropertiesModule",
                    @"
from AlgorithmImports import *

def getOrderProperties() -> BloombergFixOrderProperties:
    properties = BloombergFixOrderProperties()
    properties.additional_properties.update({ ""5700"": ""MLCO"", ""9301"": ""1"" })
    return properties
");

                dynamic getOrderProperties = module.GetAttr("getOrderProperties");
                var properties = (BloombergFixOrderProperties)getOrderProperties();

                Assert.IsNotNull(properties);
                Assert.AreEqual(2, properties.AdditionalProperties.Count);
                Assert.AreEqual("1", properties.AdditionalProperties["9301"]);
                Assert.AreEqual("MLCO", properties.LocateBroker);
            }
        }
    }
}
