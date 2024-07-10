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
    }
}
