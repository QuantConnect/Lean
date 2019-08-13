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

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using QuantConnect.Data.Fundamental;

namespace QuantConnect.Tests.Common.Data.Fundamental
{
    [TestFixture]
    public class MultiPeriodFieldTests
    {
        private NormalizedBasicEPSGrowth _field;

        [SetUp]
        public void SetUp()
        {
            _field = new NormalizedBasicEPSGrowth();
            _field.SetPeriodValue("3M", 1);
            _field.SetPeriodValue("1Y", 5);
            _field.SetPeriodValue("2Y", 2);
        }

        [Test]
        public void ReturnsDefaultPeriod()
        {
            Assert.IsTrue(_field.HasValue);

            Assert.AreEqual(5, _field);
            Assert.AreEqual(5, _field.Value);
        }

        [Test]
        public void ReturnsRequestedPeriodWithDataAvailable()
        {
            Assert.IsTrue(_field.HasPeriodValue("3M"));

            Assert.AreEqual(1, _field.GetPeriodValue("3M"));
            Assert.AreEqual(1, _field.ThreeMonths);
        }

        [Test]
        public void ReturnsRequestedPeriodWithNoData()
        {
            Assert.IsFalse(_field.HasPeriodValue("3Y"));

            Assert.AreEqual(0, _field.GetPeriodValue("3Y"));
            Assert.AreEqual(0, _field.ThreeYears);
        }

        [Test]
        public void ReturnsCorrectPeriodNamesAndValues()
        {
            Assert.AreEqual(new[] { "3M", "1Y", "2Y" }, _field.GetPeriodNames());

            Assert.AreEqual(new[] { "3M", "1Y", "2Y" }, _field.GetPeriodValues().Keys);

            Assert.AreEqual(new[] { 1, 5, 2 }, _field.GetPeriodValues().Values);
        }

        [Test]
        public void ReturnsFirstPeriodIfNoDefaultAvailable()
        {
            var data = new Dictionary<string, decimal> { { "3M", 1 }, { "2Y", 2 } };
            var field = new NormalizedBasicEPSGrowth(data);

            Assert.IsFalse(field.HasValue);

            Assert.AreEqual(1, field);
            Assert.AreEqual(1, field.Value);
        }

        [Test]
        public void EmptyStore()
        {
            var field = new NormalizedBasicEPSGrowth();
            Assert.IsFalse(field.HasValue);
            Assert.AreEqual(0, field.Value);
            Assert.AreEqual(0, field.FiveYears);
            Assert.AreEqual(0, field.OneYear);
            Assert.AreEqual(Enumerable.Empty<string>(), field.GetPeriodNames());
            Assert.AreEqual(0,
                field.GetPeriodValue(QuantConnect.Data.Fundamental.Period.OneYear));
            Assert.AreEqual(0,
                field.GetPeriodValue(QuantConnect.Data.Fundamental.Period.TenYears));
            Assert.AreEqual(0, field.GetPeriodValues().Count);
            Assert.IsFalse(field.HasPeriodValue(QuantConnect.Data.Fundamental.Period.OneYear));
            Assert.IsFalse(field.HasPeriodValue(QuantConnect.Data.Fundamental.Period.TenYears));
        }

        [Test]
        public void EmptyStoreSetPeriodValue()
        {
            var field = new NormalizedBasicEPSGrowth();
            // add the default value
            field.SetPeriodValue(QuantConnect.Data.Fundamental.Period.OneYear, 1);

            Assert.IsTrue(field.HasValue);
            Assert.AreEqual(1, field.Value);
            Assert.AreEqual(QuantConnect.Data.Fundamental.Period.OneYear, field.GetPeriodNames().Single());

            var values = field.GetPeriodValues();
            Assert.AreEqual(1, values.Count);
            Assert.AreEqual(1, values.First().Value);
            Assert.AreEqual(QuantConnect.Data.Fundamental.Period.OneYear, values.First().Key);
        }

        [Test]
        public void SetPeriodValue()
        {
            var field = new NormalizedBasicEPSGrowth();
            // add the default value
            field.SetPeriodValue(QuantConnect.Data.Fundamental.Period.OneYear, 1);
            field.SetPeriodValue(QuantConnect.Data.Fundamental.Period.TenYears, 10);

            Assert.IsTrue(field.HasValue);
            Assert.AreEqual(1, field.Value);
            var names = field.GetPeriodNames().ToList();
            Assert.AreEqual(QuantConnect.Data.Fundamental.Period.OneYear, names[0]);
            Assert.AreEqual(QuantConnect.Data.Fundamental.Period.TenYears, names[1]);

            var values = field.GetPeriodValues();
            Assert.AreEqual(2, values.Count);
            Assert.AreEqual(1, values[QuantConnect.Data.Fundamental.Period.OneYear]);
            Assert.AreEqual(10, values[QuantConnect.Data.Fundamental.Period.TenYears]);
        }

        [Test]
        public void EmptyStoreUpdateValues()
        {
            var field = new NormalizedBasicEPSGrowth();

            // update the default value
            var data = new Dictionary<string, decimal> { { QuantConnect.Data.Fundamental.Period.OneYear, 2 } };
            field.UpdateValues(new NormalizedBasicEPSGrowth(data));

            Assert.IsTrue(field.HasValue);
            Assert.AreEqual(2, field.Value);
            Assert.AreEqual(QuantConnect.Data.Fundamental.Period.OneYear, field.GetPeriodNames().Single());

            var values = field.GetPeriodValues();
            Assert.AreEqual(1, values.Count);
            Assert.AreEqual(2, values.First().Value);
            Assert.AreEqual(QuantConnect.Data.Fundamental.Period.OneYear, values.First().Key);
        }

        [Test]
        public void EmptyStoreToString()
        {
            var field = new NormalizedBasicEPSGrowth();
            Assert.AreEqual("", field.ToString());
        }

        [Test]
        public void NonEmptyStoreToString()
        {
            var field = new NormalizedBasicEPSGrowth();
            field.SetPeriodValue(QuantConnect.Data.Fundamental.Period.OneYear, 1);
            field.SetPeriodValue(QuantConnect.Data.Fundamental.Period.TenYears, 10);

            Assert.AreEqual($"{QuantConnect.Data.Fundamental.Period.OneYear}:1;" +
                            $"{QuantConnect.Data.Fundamental.Period.TenYears}:10", field.ToString());
        }

        [Test]
        public void UpdateValues()
        {
            var field = new NormalizedBasicEPSGrowth();
            // add the default value
            field.SetPeriodValue(QuantConnect.Data.Fundamental.Period.OneYear, 1);

            // update the default value
            var data = new Dictionary<string, decimal> { { QuantConnect.Data.Fundamental.Period.OneYear, 2 } };
            field.UpdateValues(new NormalizedBasicEPSGrowth(data));

            Assert.IsTrue(field.HasValue);
            Assert.AreEqual(2, field.Value);
            Assert.AreEqual(QuantConnect.Data.Fundamental.Period.OneYear, field.GetPeriodNames().Single());

            var values = field.GetPeriodValues();
            Assert.AreEqual(1, values.Count);
            Assert.AreEqual(2, values.First().Value);
            Assert.AreEqual(QuantConnect.Data.Fundamental.Period.OneYear, values.First().Key);
        }

        [Test]
        public void UpdateValuesWithNull()
        {
            var field = new NormalizedBasicEPSGrowth();
            Assert.DoesNotThrow(() => field.UpdateValues(new NormalizedBasicEPSGrowth(null)));

        }
    }
}
