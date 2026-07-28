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
using Python.Runtime;
using QuantConnect.Data;

namespace QuantConnect.Tests.Common.Data
{
    [TestFixture]
    public class DynamicDataTests
    {
        [Test]
        public void SupportsSnakeNameRetrival()
        {
            dynamic data = new DataType();
            data.PropertyA = 1;

            Assert.AreEqual(1, data.property_a);
            Assert.AreEqual(data.PropertyA, data.property_a);
        }

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
            Assert.AreEqual(time, baseData.EndTime);
            Assert.AreEqual(value, baseData.Value);
            Assert.AreEqual(value, baseData.Price);
            Assert.AreEqual(symbol, baseData.Symbol);

            // let's access the properties through the dynamic handling
            Assert.AreEqual(time, data.Time);
            Assert.AreEqual(time, data.EndTime);
            Assert.AreEqual(value, data.Value);
            Assert.AreEqual(value, data.Price);
            Assert.AreEqual(symbol, data.Symbol);
        }

        [Test]
        public void SettingReservedPropertyWithUnsupportedPythonValueThrowsDescriptiveError()
        {
            var data = new DataType();
            using (Py.GIL())
            {
                using var pyString = "not a valid value".ToPython();
                using var pyInt = 123.ToPython();

                var exception = Assert.Throws<ArgumentException>(() => data.SetProperty("time", pyString));
                Assert.That(exception.Message, Does.Contain("'time'"));
                Assert.That(exception.Message, Does.Contain("'str'"));
                Assert.That(exception.Message, Does.Contain(nameof(DateTime)));
                Assert.That(exception.InnerException, Is.TypeOf<InvalidCastException>());

                exception = Assert.Throws<ArgumentException>(() => data.SetProperty("end_time", pyString));
                Assert.That(exception.Message, Does.Contain("'end_time'"));
                Assert.That(exception.Message, Does.Contain(nameof(DateTime)));

                exception = Assert.Throws<ArgumentException>(() => data.SetProperty("value", pyString));
                Assert.That(exception.Message, Does.Contain("'value'"));
                Assert.That(exception.Message, Does.Contain(nameof(Decimal)));

                exception = Assert.Throws<ArgumentException>(() => data.SetProperty("symbol", pyInt));
                Assert.That(exception.Message, Does.Contain("'symbol'"));
                Assert.That(exception.Message, Does.Contain("'int'"));
                Assert.That(exception.Message, Does.Contain(nameof(Symbol)));
            }
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
