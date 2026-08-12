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

        [Test]
        public void LocateFieldsDefaultToEmpty()
        {
            // Locate fields are only meaningful for Reg SHO short equity sales; for any other
            // order they must be unset so the brokerage doesn't emit EMSX_LOCATE_* on the request.
            var properties = new TerminalLinkOrderProperties();
            Assert.IsNull(properties.LocateBroker);
            Assert.IsNull(properties.LocateId);
        }

        [Test]
        public void SetsLocateFieldsFromPython()
        {
            using (Py.GIL())
            {
                var module = PyModule.FromString("locateFieldsModule",
                    @"
from AlgorithmImports import *

def getOrderProperties() -> TerminalLinkOrderProperties:
    properties = TerminalLinkOrderProperties()
    properties.LocateBroker = ""BMTB""
    properties.LocateId = ""LOC-123""
    return properties
");

                dynamic getOrderProperties = module.GetAttr("getOrderProperties");
                var properties = (TerminalLinkOrderProperties)getOrderProperties();

                Assert.IsNotNull(properties);
                Assert.AreEqual("BMTB", properties.LocateBroker);
                Assert.AreEqual("LOC-123", properties.LocateId);
            }
        }

        [Test]
        public void IsCfdTradeDefaultsToFalse()
        {
            // A regular trade is the EMSX default, and the value for which the brokerage sends no
            // EMSX_CFD_FLAG at all, so it must be what an untouched instance reports.
            var properties = new TerminalLinkOrderProperties();
            Assert.IsFalse(properties.IsCfdTrade);
        }

        [Test]
        public void CloneDoesNotShareAdditionalProperties()
        {
            // Order properties are reused across orders and cloned before being edited, e.g. by
            // BrokerageExtensions.RemoveLocateFromNonShortOrder; a shared dictionary would let an
            // edit on the copy leak back into the caller's instance.
            var properties = new TerminalLinkOrderProperties { IsCfdTrade = true };

            var clone = (TerminalLinkOrderProperties)properties.Clone();
            clone.IsCfdTrade = false;

            Assert.IsTrue(properties.IsCfdTrade);
        }

        [Test]
        public void SetsIsCfdTradeFromPython()
        {
            using (Py.GIL())
            {
                var module = PyModule.FromString("cfdTradeModule",
                    @"
from AlgorithmImports import *

def getOrderProperties() -> TerminalLinkOrderProperties:
    properties = TerminalLinkOrderProperties()
    properties.is_cfd_trade = True
    return properties
");

                dynamic getOrderProperties = module.GetAttr("getOrderProperties");
                var properties = (TerminalLinkOrderProperties)getOrderProperties();

                Assert.IsNotNull(properties);
                Assert.IsTrue(properties.IsCfdTrade);
            }
        }

        [Test]
        public void SetsAdditionalPropertiesEntryFromPython()
        {
            using (Py.GIL())
            {
                var module = PyModule.FromString("additionalPropertiesEntryModule",
                    @"
from AlgorithmImports import *

def getOrderProperties() -> TerminalLinkOrderProperties:
    properties = TerminalLinkOrderProperties()
    properties.additional_properties[""EMSX_CFD_FLAG""] = ""1""
    properties.additional_properties[""EMSX_ODD_LOT""] = ""0""
    return properties
");

                dynamic getOrderProperties = module.GetAttr("getOrderProperties");
                var properties = (TerminalLinkOrderProperties)getOrderProperties();

                Assert.IsNotNull(properties);
                Assert.AreEqual(2, properties.AdditionalProperties.Count);
                Assert.AreEqual("0", properties.AdditionalProperties["EMSX_ODD_LOT"]);
                // an entry written through the dictionary is visible on the typed property
                Assert.IsTrue(properties.IsCfdTrade);
            }
        }

        [Test]
        public void ClearsAdditionalPropertiesFromPython()
        {
            using (Py.GIL())
            {
                // clear() resets the dictionary, taking the typed properties reading from it back to
                // their defaults.
                var module = PyModule.FromString("additionalPropertiesClearModule",
                    @"
from AlgorithmImports import *

def getOrderProperties() -> TerminalLinkOrderProperties:
    properties = TerminalLinkOrderProperties()
    properties.is_cfd_trade = True
    properties.additional_properties[""EMSX_ODD_LOT""] = ""0""
    properties.additional_properties.clear()
    return properties
");

                dynamic getOrderProperties = module.GetAttr("getOrderProperties");
                var properties = (TerminalLinkOrderProperties)getOrderProperties();

                Assert.IsNotNull(properties);
                Assert.IsEmpty(properties.AdditionalProperties);
                Assert.IsFalse(properties.IsCfdTrade);
            }
        }

        [Test]
        public void UpdatesAdditionalPropertiesFromPlainPythonDictionary()
        {
            using (Py.GIL())
            {
                // update() is the way to bulk load from a plain Python dict; it takes a PyObject, so
                // it sidesteps the conversion that plain assignment cannot do.
                var module = PyModule.FromString("additionalPropertiesUpdateModule",
                    @"
from AlgorithmImports import *

def getOrderProperties() -> TerminalLinkOrderProperties:
    properties = TerminalLinkOrderProperties()
    properties.additional_properties.update({ ""EMSX_CFD_FLAG"": ""1"", ""EMSX_ODD_LOT"": ""0"" })
    return properties
");

                dynamic getOrderProperties = module.GetAttr("getOrderProperties");
                var properties = (TerminalLinkOrderProperties)getOrderProperties();

                Assert.IsNotNull(properties);
                Assert.AreEqual(2, properties.AdditionalProperties.Count);
                Assert.AreEqual("0", properties.AdditionalProperties["EMSX_ODD_LOT"]);
                Assert.IsTrue(properties.IsCfdTrade);
            }
        }

        [Test]
        public void AssigningPlainPythonDictionaryToAdditionalPropertiesThrows()
        {
            using (Py.GIL())
            {
                // pythonnet has no conversion from a Python dict to Dictionary<string, string>, so
                // the natural looking assignment fails at runtime; the entries have to be added to
                // the dictionary the properties already own.
                var module = PyModule.FromString("additionalPropertiesReplacementModule",
                    @"
from AlgorithmImports import *

def getOrderProperties() -> TerminalLinkOrderProperties:
    properties = TerminalLinkOrderProperties()
    properties.additional_properties = { ""EMSX_CFD_FLAG"": ""1"" }
    return properties
");

                dynamic getOrderProperties = module.GetAttr("getOrderProperties");

                var exception = Assert.Throws<PythonException>(() => getOrderProperties());
                Assert.IsTrue(exception.Message.Contains("cannot be converted", StringComparison.InvariantCulture),
                    $"Expected a conversion failure, got: {exception.Message}");
            }
        }

        [Test]
        public void ReadsAdditionalPropertiesEntryWrittenByTypedPropertyFromPython()
        {
            using (Py.GIL())
            {
                var module = PyModule.FromString("additionalPropertiesReadModule",
                    @"
from AlgorithmImports import *

def getCfdFlag() -> str:
    properties = TerminalLinkOrderProperties()
    properties.is_cfd_trade = True
    return properties.additional_properties[""EMSX_CFD_FLAG""]
");

                dynamic getCfdFlag = module.GetAttr("getCfdFlag");
                var flag = (string)getCfdFlag();

                Assert.AreEqual("1", flag);
            }
        }
    }
}
