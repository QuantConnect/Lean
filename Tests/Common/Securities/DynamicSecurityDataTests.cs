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
        private SecurityCache _cache;
        private RegisteredSecurityDataTypesProvider _dataTypesProvider;

        [SetUp]
        public void SetUp()
        {
            _cache = new SecurityCache();
            _dataTypesProvider = new RegisteredSecurityDataTypesProvider();
            _dataTypesProvider.RegisterType(typeof(TradeBar));
        }

        [TearDown]
        public void TearDown()
        {
            _cache.Reset();
        }

        [Test]
        public void StoreData_UsesTypeName_AsKey()
        {
            var data = new DynamicSecurityData(_dataTypesProvider, _cache);
            _cache.StoreData(new List<TradeBar>
            {
                new TradeBar(DateTime.UtcNow, Symbols.SPY, 10m, 20m, 5m, 15m, 10000)
            }, typeof(TradeBar));

            Assert.IsTrue(data.HasProperty(typeof(TradeBar).Name));

            var arr = (IReadOnlyList<TradeBar>)data.GetProperty(typeof(TradeBar).Name);
            Assert.AreEqual(1, arr.Count);
            Assert.AreEqual(15, arr[0].Close);
        }

        [Test]
        public void Get_UsesTypeName_AsKey_And_ReturnsLastItem()
        {
            var data = new DynamicSecurityData(_dataTypesProvider, _cache);
            _cache.StoreData(new List<TradeBar> {
                new TradeBar(DateTime.UtcNow, Symbols.SPY, 10m, 20m, 5m, 1, 10000),
                new TradeBar(DateTime.UtcNow, Symbols.SPY, 10m, 20m, 5m, 2, 10000),
                new TradeBar(DateTime.UtcNow, Symbols.SPY, 10m, 20m, 5m, 3, 10000)
            }, typeof(TradeBar));

            var item = data.Get<TradeBar>();
            Assert.AreEqual(3, item.Close);
        }

        [Test]
        public void GetAll_UsesTypeName_AsKey()
        {
            var data = new DynamicSecurityData(_dataTypesProvider, _cache);
            _cache.StoreData(new List<TradeBar> {
                new TradeBar(DateTime.UtcNow, Symbols.SPY, 10m, 20m, 5m, 1, 10000),
                new TradeBar(DateTime.UtcNow, Symbols.SPY, 10m, 20m, 5m, 2, 10000),
                new TradeBar(DateTime.UtcNow, Symbols.SPY, 10m, 20m, 5m, 3, 10000)
            }, typeof(TradeBar));

            var arr = data.GetAll<TradeBar>();
            Assert.AreEqual(3, arr.Count);
            Assert.AreEqual(1, arr[0].Close);
        }

        [Test]
        public void AccessesDataDynamically()
        {
            var securityData = new DynamicSecurityData(_dataTypesProvider, _cache);
            _cache.StoreData(new List<TradeBar> {
                new TradeBar(DateTime.UtcNow, Symbols.SPY, 10m, 20m, 5m, 1, 10000),
                new TradeBar(DateTime.UtcNow, Symbols.SPY, 10m, 20m, 5m, 2, 10000),
                new TradeBar(DateTime.UtcNow, Symbols.SPY, 10m, 20m, 5m, 3, 10000)
            }, typeof(TradeBar));

            dynamic dynamicSecurityData = securityData;
            var tradeBars = dynamicSecurityData.TradeBar;
            Assert.IsInstanceOf<IReadOnlyList<TradeBar>>(tradeBars);
        }

        [Test]
        public void DataCanNotBeSetDynamically()
        {
            var securityData = new DynamicSecurityData(_dataTypesProvider, _cache);
            var data = new List<TradeBar> {
                new TradeBar(DateTime.UtcNow, Symbols.SPY, 10m, 20m, 5m, 1, 10000)
            };

            dynamic dynamicSecurityData = securityData;
            Assert.Throws<InvalidOperationException>(() =>
            {
                dynamicSecurityData.TradeBar = data;
            });
        }

        [Test]
        public void AccessingPropertyThatDoesNotExists_ThrowsKeyNotFoundException_WhenNotIncludedInRegisteredTypes()
        {
            var registeredTypes = new RegisteredSecurityDataTypesProvider();
            dynamic securityData = new DynamicSecurityData(registeredTypes, _cache);

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
            dynamic securityData = new DynamicSecurityData(registeredTypes, _cache);

            var tradeBars = securityData.TradeBar;
            Assert.IsInstanceOf<List<TradeBar>>(tradeBars);
            Assert.IsEmpty(tradeBars);
        }

        [Test]
        public void Py_StoreData_GetProperty()
        {
            var data = new DynamicSecurityData(_dataTypesProvider, _cache);
            _cache.StoreData(new List<TradeBar>
            {
                new TradeBar(DateTime.UtcNow, Symbols.SPY, 10m, 20m, 5m, 1, 10000)
            }, typeof(TradeBar));

            using (Py.GIL())
            {
                dynamic test = PyModule.FromString("testModule",
                    @"
from AlgorithmImports import *

def Test(dynamicData):
    data = dynamicData.GetProperty(""TradeBar"")
    if len(data) != 1:
        raise Exception('Unexpected length')
    if data[0].Close != 1:
        raise Exception('Unexpected value')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(data));
            }
        }

        [Test]
        public void Py_StoreData_HasProperty()
        {
            var data = new DynamicSecurityData(_dataTypesProvider, _cache);
            _cache.StoreData(new List<TradeBar>
            {
                new TradeBar(DateTime.UtcNow, Symbols.SPY, 10m, 20m, 5m, 1, 10000)
            }, typeof(TradeBar));

            using (Py.GIL())
            {
                dynamic test = PyModule.FromString("testModule",
                    @"
from AlgorithmImports import *

def Test(dynamicData):
    data = dynamicData.HasProperty(""TradeBar"")
    if not data:
        raise Exception('Unexpected HasProperty result')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(data));
            }
        }

        [Test]
        public void Py_StoreData_Get_UsesTypeName()
        {
            var data = new DynamicSecurityData(_dataTypesProvider, _cache);
            _cache.StoreData(new List<TradeBar>
            {
                new TradeBar(DateTime.UtcNow, Symbols.SPY, 10m, 20m, 5m, 1, 10000)
            }, typeof(TradeBar));

            using (Py.GIL())
            {
                dynamic test = PyModule.FromString("testModule",
                    @"
from AlgorithmImports import *

def Test(dynamicData):
    data = dynamicData.Get(TradeBar)
    if data.Close != 1:
        raise Exception('Unexpected value')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(data));
            }
        }

        [Test]
        public void Py_StoreData_GetAll_UsesTypeName()
        {
            var data = new DynamicSecurityData(_dataTypesProvider, _cache);
            _cache.StoreData(new List<TradeBar>
            {
                new TradeBar(DateTime.UtcNow, Symbols.SPY, 10m, 20m, 5m, 1, 10000)
            }, typeof(TradeBar));

            using (Py.GIL())
            {
                dynamic test = PyModule.FromString("testModule",
                    @"
from AlgorithmImports import *

def Test(dynamicData):
    data = dynamicData.GetAll(TradeBar)
    if len(data) != 1:
        raise Exception('Unexpected length')
    if data[0].Close != 1:
        raise Exception('Unexpected value')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(data));
            }
        }

        [Test]
        public void Py_Get_UsesTypeName_AsKey_And_ReturnsLastItem()
        {
            var data = new DynamicSecurityData(_dataTypesProvider, _cache);
            _cache.StoreData(new List<TradeBar>
            {
                new TradeBar(DateTime.UtcNow, Symbols.SPY, 10m, 20m, 5m, 1, 10000),
                new TradeBar(DateTime.UtcNow, Symbols.SPY, 10m, 20m, 5m, 3, 10000)
            }, typeof(TradeBar));

            using (Py.GIL())
            {
                dynamic test = PyModule.FromString("testModule",
                    @"
from AlgorithmImports import *

def Test(dynamicData):
    data = dynamicData.Get(TradeBar)
    if data.Close != 3:
        raise Exception('Unexpected value')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(data));
            }
        }

        [Test]
        public void Py_GetAll_TradeBar()
        {
            var securityData = new DynamicSecurityData(_dataTypesProvider, _cache);
            var data = new List<TradeBar>
            {
                new TradeBar(DateTime.UtcNow, Symbols.SPY, 10m, 20m, 5m, 15m, 10000)
            };
            _cache.StoreData(data, typeof(TradeBar));

            using (Py.GIL())
            {
                dynamic test = PyModule.FromString("testModule",
                    @"
from AlgorithmImports import *

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
            var securityData = new DynamicSecurityData(_dataTypesProvider, _cache);
            var data = new List<TradeBar>
            {
                new TradeBar(DateTime.UtcNow, Symbols.SPY, 10m, 20m, 5m, 15m, 10000)
            };
            _cache.StoreData(data, typeof(TradeBar));

            using (Py.GIL())
            {
                dynamic test = PyModule.FromString("testModule",
                    @"
from AlgorithmImports import *

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
            var securityData = new DynamicSecurityData(_dataTypesProvider, _cache);
            var data = new[]
            {
                new TradeBar(DateTime.UtcNow, Symbols.SPY, 10m, 20m, 5m, 15m, 10000)
            };
            _cache.StoreData(data, typeof(TradeBar));

            using (Py.GIL())
            {
                dynamic test = PyModule.FromString("testModule",
                    @"
from AlgorithmImports import *

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
            dynamic securityData = new DynamicSecurityData(registeredTypes, _cache);

            using (Py.GIL())
            {
                dynamic test = PyModule.FromString("testModule",
                    @"
from AlgorithmImports import *

def Test(dynamicData):
    data = dynamicData.Get(TradeBar)").GetAttr("Test");

                Assert.That(() => test(securityData),
                    Throws.InstanceOf<ClrBubbledException>().With.InnerException.InstanceOf<KeyNotFoundException>());
            }
        }

        [Test]
        public void Py_AccessPropertyThatDoesNotExists_ReturnsEmptyList_WhenTypeIsIncludedInRegisteredTypes()
        {
            var registeredTypes = new RegisteredSecurityDataTypesProvider();
            registeredTypes.RegisterType(typeof(TradeBar));
            dynamic securityData = new DynamicSecurityData(registeredTypes, _cache);

            using (Py.GIL())
            {
                dynamic test = PyModule.FromString("testModule",
                    @"
from AlgorithmImports import *

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
