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
using System.Collections.Generic;
using QuantConnect.Indicators;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    public class AugenPriceSpikeTests : CommonIndicatorTests<IndicatorDataPoint>
    {
        protected override IndicatorBase<IndicatorDataPoint> CreateIndicator()
        {
            return new AugenPriceSpike(20);
        }

        protected override string TestFileName => "spy_aps.txt";

        protected override string TestColumnName => "APS";

        [Test]
        public void TestWithStream()
        {
            var aps = new AugenPriceSpike(22);
            foreach (var data in TestHelper.GetDataStream(50))
            {
                aps.Update(data.Time, data.Value);
            }
        }

        [Test]
        public void TestForPeriod()
        {
            Assert.Throws<ArgumentException>(() => new AugenPriceSpike(1));
            Assert.Throws<ArgumentException>(() => new AugenPriceSpike(2));
        }

        protected override Action<IndicatorBase<IndicatorDataPoint>, double> Assertion
        {
            get { return (indicator, expected) => Assert.AreEqual(expected, (double)indicator.Current.Value, 1); }
        }

        [Test]
        public void PeriodSet()
        {
            var aps = new AugenPriceSpike(period: 20);
            var reference = DateTime.Today;

            double correctValue = 0.31192350881956543;
            decimal finalTestValue = 22;

            int count = 0;
            List<double> testValues = new List<double>() { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21 };

            foreach (decimal i in testValues)
            {
                count += 1;
                aps.Update(reference.AddMinutes(count), i);
                Assert.IsFalse(aps.IsReady);
                Assert.AreEqual(0, aps.Current.Value);
            }
            aps.Update(reference.AddMinutes(count + 1), finalTestValue);
            Assert.IsTrue(aps.IsReady);
            Assert.AreEqual(correctValue, (double)aps.Current.Value, 0.00001);
        }

        [Test]
        public override void ResetsProperly()
        {
            var aps = new AugenPriceSpike(10);
            var reference = DateTime.Today;

            aps.Update(reference.AddMinutes(1), 5);
            aps.Update(reference.AddMinutes(2), 10);
            aps.Update(reference.AddMinutes(3), 8);
            aps.Update(reference.AddMinutes(4), 12);
            aps.Update(reference.AddMinutes(5), 103);
            aps.Update(reference.AddMinutes(6), 82);
            aps.Update(reference.AddMinutes(7), 55);
            aps.Update(reference.AddMinutes(8), 10);
            aps.Update(reference.AddMinutes(9), 878);
            aps.Update(reference.AddMinutes(10), 84);
            aps.Update(reference.AddMinutes(11), 832);
            aps.Update(reference.AddMinutes(12), 81);
            aps.Update(reference.AddMinutes(13), 867);
            aps.Update(reference.AddMinutes(14), 89);
            Assert.IsTrue(aps.IsReady);
            Assert.AreNotEqual(0m, aps.Current.Value);

            aps.Reset();
            TestHelper.AssertIndicatorIsInDefaultState(aps);
        }

        [Test]
        public void DoesNotThrowOverflowException()
        {
            var aps = new AugenPriceSpike(5);
            var values = new List<decimal>
            {
                decimal.MaxValue,
                0,
                1e-18m,
                decimal.MaxValue,
                1m
            };

            var date = new DateTime(2024, 12, 2, 12, 0, 0);

            for (var i = 0; i < values.Count; i++)
            {
                Assert.DoesNotThrow(() => aps.Update(date, values[i]));
            }
        }
    }
}
