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
using System.Collections.Generic;
using NUnit.Framework;
using Python.Runtime;
using QuantConnect.Data.Market;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Common.Securities
{
    [TestFixture]
    public class DynamicSecurityDataTests
    {
        [Test]
        public void StoreData_UsesTypeName_AsKey()
        {
            var data = new DynamicSecurityData(RegisteredSecurityDataTypesProvider.Null);
            data.StoreData(typeof(int), new[] {1});

            Assert.IsTrue(data.HasProperty(typeof(int).Name));

            var arr = (IReadOnlyList<int>) data.GetProperty(typeof(int).Name);
            Assert.AreEqual(1, arr.Count);
            Assert.AreEqual(1, arr[0]);
        }

        [Test]
        public void Get_UsesTypeName_AsKey_And_ReturnsLastItem()
        {
            var data = new DynamicSecurityData(RegisteredSecurityDataTypesProvider.Null);
            data.StoreData(typeof(int), new[] {1, 2, 3});

            var item = data.Get<int>();
            Assert.AreEqual(3, item);
        }

        [Test]
        public void GetAll_UsesTypeName_AsKey()
        {
            var data = new DynamicSecurityData(RegisteredSecurityDataTypesProvider.Null);
            data.SetProperty(typeof(int).Name, new[] {1});

            var arr = data.GetAll<int>();
            Assert.AreEqual(1, arr.Count);
            Assert.AreEqual(1, arr[0]);
        }

        [Test]
        public void AccessesDataDynamically()
        {
            var securityData = new DynamicSecurityData(RegisteredSecurityDataTypesProvider.Null);
            var data = new List<TradeBar>
            {
                new TradeBar(DateTime.UtcNow, Symbols.SPY, 10m, 20m, 5m, 15m, 10000)
            };
            securityData.StoreData(typeof(TradeBar), data);

            dynamic dynamicSecurityData = securityData;
            var tradeBars = dynamicSecurityData.TradeBar;
            Assert.IsInstanceOf<IReadOnlyList<TradeBar>>(tradeBars);
        }

        [Test]
        public void AccessingPropertyThatDoesNotExists_ThrowsKeyNotFoundException_WhenNotIncludedInRegisteredTypes()
        {
            var registeredTypes = new RegisteredSecurityDataTypesProvider();
            registeredTypes.RegisterType(typeof(TradeBar));
            dynamic securityData = new DynamicSecurityData(registeredTypes);

            Assert.Throws<KeyNotFoundException>(() =>
            {
                var _ = securityData.NotFoundProperty;
            });
        }

        [Test]
        public void AccessPropertyThatDoesNotExists_ReturnsEmptyList_WhenTypeIsIncludedInRegisteredTypes()
        {
            var registeredTypes = new RegisteredSecurityDataTypesProvider();
            registeredTypes.RegisterType(typeof(TradeBar));
            dynamic securityData = new DynamicSecurityData(registeredTypes);

            var tradeBars = securityData.TradeBar;
            Assert.IsInstanceOf<List<TradeBar>>(tradeBars);
            Assert.IsEmpty(tradeBars);
        }

        [Test]
        public void Py_StoreData_GetProperty()
        {
            var data = new DynamicSecurityData(RegisteredSecurityDataTypesProvider.Null);
            data.StoreData(typeof(int), new[] { 1 });

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    @"
from clr import AddReference
AddReference(""QuantConnect.Common"")
AddReference(""System"")
from System import *
from QuantConnect import *

def Test(dynamicData):
    data = dynamicData.GetProperty(""Int32"")
    if len(data) != 1:
        raise Exception('Unexpected length')
    if data[0] != 1:
        raise Exception('Unexpected value')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(data));
            }
        }

        [Test]
        public void Py_StoreData_HasProperty()
        {
            var data = new DynamicSecurityData(RegisteredSecurityDataTypesProvider.Null);
            data.StoreData(typeof(int), new[] { 1 });

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    @"
from clr import AddReference
AddReference(""QuantConnect.Common"")
AddReference(""System"")
from System import *
from QuantConnect import *

def Test(dynamicData):
    data = dynamicData.HasProperty(""Int32"")
    if not data:
        raise Exception('Unexpected HasProperty result')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(data));
            }
        }

        [Test]
        public void Py_StoreData_Get_UsesTypeName()
        {
            var data = new DynamicSecurityData(RegisteredSecurityDataTypesProvider.Null);
            data.StoreData(typeof(int), new[] { 1 });

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    @"
from clr import AddReference
AddReference(""QuantConnect.Common"")
AddReference(""System"")
from System import *
from QuantConnect import *

def Test(dynamicData):
    data = dynamicData.Get(Int32)
    if data != 1:
        raise Exception('Unexpected value')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(data));
            }
        }

        [Test] public void Py_StoreData_GetAll_UsesTypeName()
        {
            var data = new DynamicSecurityData(RegisteredSecurityDataTypesProvider.Null);
            data.StoreData(typeof(int), new[] { 1 });

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    @"
from clr import AddReference
AddReference(""QuantConnect.Common"")
AddReference(""System"")
from System import *
from QuantConnect import *

def Test(dynamicData):
    data = dynamicData.GetAll(Int32)
    if len(data) != 1:
        raise Exception('Unexpected length')
    if data[0] != 1:
        raise Exception('Unexpected value')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(data));
            }
        }

        [Test]
        public void Py_Get_UsesTypeName_AsKey_And_ReturnsLastItem()
        {
            var data = new DynamicSecurityData(RegisteredSecurityDataTypesProvider.Null);
            data.StoreData(typeof(int), new[] { 1, 2, 3 });

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    @"
from clr import AddReference
AddReference(""QuantConnect.Common"")
AddReference(""System"")
from System import *
from QuantConnect import *

def Test(dynamicData):
    data = dynamicData.Get(Int32)
    if data != 3:
        raise Exception('Unexpected value')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(data));
            }
        }

        [Test]
        public void Py_GetAll_TradeBar()
        {
            var securityData = new DynamicSecurityData(RegisteredSecurityDataTypesProvider.Null);
            var data = new List<TradeBar>
            {
                new TradeBar(DateTime.UtcNow, Symbols.SPY, 10m, 20m, 5m, 15m, 10000)
            };
            securityData.StoreData(typeof(TradeBar), data);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    @"
from clr import AddReference
AddReference(""QuantConnect.Common"")
from QuantConnect import *
from QuantConnect.Data.Market import *

def Test(dynamicData):
    data = dynamicData.GetAll(TradeBar)
    if data[0].Low != 5:
        raise Exception('Unexpected value')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(securityData));
            }
        }

        [Test]
        public void Py_Get_TradeBar()
        {
            var securityData = new DynamicSecurityData(RegisteredSecurityDataTypesProvider.Null);
            var data = new List<TradeBar>
            {
                new TradeBar(DateTime.UtcNow, Symbols.SPY, 10m, 20m, 5m, 15m, 10000)
            };
            securityData.StoreData(typeof(TradeBar), data);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    @"
from clr import AddReference
AddReference(""QuantConnect.Common"")
from QuantConnect import *
from QuantConnect.Data.Market import *

def Test(dynamicData):
    data = dynamicData.Get(TradeBar)
    if data.Low != 5:
        raise Exception('Unexpected value')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(securityData));
            }
        }

        [Test]
        public void Py_Get_TradeBarArray()
        {
            var securityData = new DynamicSecurityData(RegisteredSecurityDataTypesProvider.Null);
            var data = new []
            {
                new TradeBar(DateTime.UtcNow, Symbols.SPY, 10m, 20m, 5m, 15m, 10000)
            };
            securityData.StoreData(typeof(TradeBar), data);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    @"
from clr import AddReference
AddReference(""QuantConnect.Common"")
from QuantConnect import *
from QuantConnect.Data.Market import *

def Test(dynamicData):
    data = dynamicData.Get(TradeBar)
    if data.Low != 5:
        raise Exception('Unexpected value')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(securityData));
            }
        }

        [Test]
        public void Py_GetTypeThatDoesNotExists_ThrowsKeyNotFoundException_WhenNotIncludedInRegisteredTypes()
        {
            var registeredTypes = new RegisteredSecurityDataTypesProvider();
            dynamic securityData = new DynamicSecurityData(registeredTypes);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    @"
from clr import AddReference
AddReference(""QuantConnect.Common"")
AddReference(""System"")
from System import *
from QuantConnect import *

def Test(dynamicData):
    data = dynamicData.Get(TradeBar)").GetAttr("Test");

                Assert.Throws<PythonException>(() => test(securityData));
            }
        }

        [Test]
        public void Py_AccessPropertyThatDoesNotExists_ReturnsEmptyList_WhenTypeIsIncludedInRegisteredTypes()
        {
            var registeredTypes = new RegisteredSecurityDataTypesProvider();
            registeredTypes.RegisterType(typeof(TradeBar));
            dynamic securityData = new DynamicSecurityData(registeredTypes);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    @"
from clr import AddReference
AddReference(""QuantConnect.Common"")
from QuantConnect import *
from QuantConnect.Data.Market import *

def Test(dynamicData):
    data = dynamicData.GetAll(TradeBar)
    if data is None:
        raise Exception('Unexpected None value')
    if len(data) != 0:
        raise Exception('Unexpected length')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(securityData));
            }
        }
    }
}
