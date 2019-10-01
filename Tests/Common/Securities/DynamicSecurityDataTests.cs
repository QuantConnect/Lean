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
    }
}
