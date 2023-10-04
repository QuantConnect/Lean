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
using QuantConnect.Orders;

namespace QuantConnect.Tests.Common.Orders
{
    [TestFixture]
    public class TerminalLinkOrderPropertiesTests
    {
        [Test]
        public void SeamlesslySetsStrategyInPython()
        {
            using (Py.GIL())
            {
                var module = PyModule.FromString("testModule",
                    @"
from AlgorithmImports import *

def getOrderProperties() -> TerminalLinkOrderProperties:
    strategyFields = [
        TerminalLinkOrderProperties.StrategyField(""09:30:00""),
        TerminalLinkOrderProperties.StrategyField(""10:30:00""),
        TerminalLinkOrderProperties.StrategyField(),
        TerminalLinkOrderProperties.StrategyField()
    ]

    properties = TerminalLinkOrderProperties()
    properties.Strategy = TerminalLinkOrderProperties.StrategyParameters(""VWAP"", strategyFields)

    return properties
");

                dynamic getOrderProperties = module.GetAttr("getOrderProperties");
                var orderProperties = (TerminalLinkOrderProperties)getOrderProperties();

                Assert.IsNotNull(orderProperties);
                Assert.AreEqual("VWAP", orderProperties.Strategy.Name);
                Assert.AreEqual(4, orderProperties.Strategy.Fields.Count);

                Assert.IsTrue(orderProperties.Strategy.Fields[0].HasValue);
                Assert.AreEqual("09:30:00", orderProperties.Strategy.Fields[0].Value);

                Assert.IsTrue(orderProperties.Strategy.Fields[1].HasValue);
                Assert.AreEqual("10:30:00", orderProperties.Strategy.Fields[1].Value);

                Assert.IsFalse(orderProperties.Strategy.Fields[2].HasValue);
                Assert.IsNull(orderProperties.Strategy.Fields[2].Value);

                Assert.IsFalse(orderProperties.Strategy.Fields[3].HasValue);
                Assert.IsNull(orderProperties.Strategy.Fields[3].Value);
            }
        }
    }
}
