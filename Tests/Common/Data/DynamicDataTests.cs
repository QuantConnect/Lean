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
using System.Collections.Generic;
using NUnit.Framework;
using QuantConnect.Data;

namespace QuantConnect.Tests.Common.Data
{
    [TestFixture]
    public class DynamicDataTests
    {
        [Test]
        public void StoresValues_Using_LowerCaseKeys()
        {
            dynamic data = new DataType();
            data.Property = 1;

            Assert.AreEqual(1, data.Property);
            Assert.AreEqual(data.Property, data.property);
        }

        [Test]
        public void StoresBaseDataValues_Using_BaseDataProperties()
        {
            var value = 1234.567890m;
            var time = new DateTime(2000, 1, 2, 3, 4, 5, 6);
            var symbol = Symbol.Create("ticker", SecurityType.Base, QuantConnect.Market.USA, baseDataType: typeof(DataType));
            dynamic data = new DataType();
            data.Time = time;
            data.Value = value;
            data.Symbol = symbol;

            BaseData baseData = data;
            Assert.AreEqual(time, baseData.Time);
            Assert.AreEqual(value, baseData.Value);
            Assert.AreEqual(value, baseData.Price);
            Assert.AreEqual(symbol, baseData.Symbol);
        }

        [Test]
        public void AccessingPropertyThatDoesNotExist_ThrowsKeyNotFoundException()
        {
            dynamic data = new DataType();
            Assert.Throws<KeyNotFoundException>(() =>
            {
                var _ = data.UndefinedPropertyName;
            });
        }

        private class DataType : DynamicData
        {
        }
    }
}
