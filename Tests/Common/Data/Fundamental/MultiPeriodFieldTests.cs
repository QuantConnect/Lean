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
    }
}
