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
using QuantConnect.Indicators;
using System.Collections.Generic;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    public class McGinleyDynamicTests : CommonIndicatorTests<IndicatorDataPoint>
    {
        protected override IndicatorBase<IndicatorDataPoint> CreateIndicator()
        {
            return new McGinleyDynamic(14);
        }

        protected override string TestFileName => "spy_with_McGinleyDynamic.csv";
        protected override string TestColumnName => "McGinleyDynamic14";


        [Test]
        public void IsReadyAfterPeriodUpdates()
        {
            var indicator = new McGinleyDynamic(3);

            indicator.Update(new DateTime(2024, 7, 9, 0, 1, 0), 1m);
            indicator.Update(new DateTime(2024, 7, 9, 0, 2, 0), 1m);
            Assert.IsFalse(indicator.IsReady);
            indicator.Update(new DateTime(2024, 7, 9, 0, 3, 0), 1m);
            Assert.IsTrue(indicator.IsReady);
        }

        [Test]
        public override void ResetsProperly()
        {
            var indicator = new McGinleyDynamic(3);

            foreach (var data in TestHelper.GetDataStream(4))
            {
                indicator.Update(data);
            }
            Assert.IsTrue(indicator.IsReady);

            indicator.Reset();

            TestHelper.AssertIndicatorIsInDefaultState(indicator);
            indicator.Update(new DateTime(2024, 7, 9, 0, 1, 0), 2.0m);
            indicator.Update(new DateTime(2024, 7, 9, 0, 2, 0), 2.0m);
            indicator.Update(new DateTime(2024, 7, 9, 0, 3, 0), 2.0m);
            Assert.AreEqual(indicator.Current.Value, 2.0m);
        }

        [Test]
        public override void WorksWithLowValues()
        {
            var indicator = new McGinleyDynamic("test", 10);

            var startDate = new DateTime(2020, 6, 4);
            var dataPoints = new List<IndicatorDataPoint>()
            {
                new IndicatorDataPoint(startDate, 0m),
                new IndicatorDataPoint(startDate.AddDays(1), 0m),
                new IndicatorDataPoint(startDate.AddDays(2), 0m),
                new IndicatorDataPoint(startDate.AddDays(3), 0m),
                new IndicatorDataPoint(startDate.AddDays(4), 3.27743794800m),
                new IndicatorDataPoint(startDate.AddDays(5), 7.46527532600m),
                new IndicatorDataPoint(startDate.AddDays(6), 2.54419732600m),
                new IndicatorDataPoint(startDate.AddDays(7), 0m),
                new IndicatorDataPoint(startDate.AddDays(8), 0m),
                new IndicatorDataPoint(startDate.AddDays(9), 0.71847738800m),
                new IndicatorDataPoint(startDate.AddDays(10), 0m),
                new IndicatorDataPoint(startDate.AddDays(11), 1.86016748400m),
                new IndicatorDataPoint(startDate.AddDays(12), 0.45273917600m),
                new IndicatorDataPoint(startDate.AddDays(13), 0m),
                new IndicatorDataPoint(startDate.AddDays(14), 1.80111454800m),
                new IndicatorDataPoint(startDate.AddDays(15), 0m),
                new IndicatorDataPoint(startDate.AddDays(16), 2.74596152400m),
                new IndicatorDataPoint(startDate.AddDays(17), 0m),
                new IndicatorDataPoint(startDate.AddDays(18), 0m),
                new IndicatorDataPoint(startDate.AddDays(19), 0m),
                new IndicatorDataPoint(startDate.AddDays(20), 0.84642541600m),
            };

            for (int i=0; i < 21; i++)
            {
                Assert.DoesNotThrow(() => indicator.Update(dataPoints[i]));
            }
        }
    }
}
