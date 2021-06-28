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
using QuantConnect.Data.Market;
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
            var aps = new AugenPriceSpike(20);
            foreach (var data in TestHelper.GetDataStream(50))
            {
                aps.Update(data.Time, data.Value);
            }
        }

        protected override Action<IndicatorBase<IndicatorDataPoint>, double> Assertion
        {
            get { return (indicator, expected) => Assert.AreEqual(expected, (double)indicator.Current.Value, 1); }
        }

        [Test]
        public void PeriodSet()
        {
            var aps = new AugenPriceSpike(period: 3);
            var reference = System.DateTime.Today;

            aps.Update(reference.AddMinutes(1), 5);
            Assert.AreEqual(0, aps.Current.Value);
            Assert.IsFalse(aps.IsReady);

            aps.Update(reference.AddMinutes(2), 10);
            Assert.AreEqual(0, (double)aps.Current.Value, 0.00001);
            Assert.IsFalse(aps.IsReady);

            aps.Update(reference.AddMinutes(3), 8);
            Assert.AreEqual(-0.09733285267845752, (double)aps.Current.Value, 0.00001);
            Assert.IsTrue(aps.IsReady);

            aps.Update(reference.AddMinutes(4), 12);
            Assert.AreEqual(0.30618621784789724, (double)aps.Current.Value, 0.00001);
            Assert.IsTrue(aps.IsReady);
        }

        [Test]
        public override void ResetsProperly()
        {
            var aps = new AugenPriceSpike(3);
            var reference = System.DateTime.Today;

            aps.Update(reference.AddMinutes(1), 5);
            aps.Update(reference.AddMinutes(2), 10);
            aps.Update(reference.AddMinutes(3), 8);
            Assert.IsTrue(aps.IsReady);
            Assert.AreNotEqual(0m, aps.Current.Value);

            aps.Reset();
            TestHelper.AssertIndicatorIsInDefaultState(aps);
        }
    }
}
