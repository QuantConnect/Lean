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
    public class ZeroLagExponentialMovingAverageTests : CommonIndicatorTests<IndicatorDataPoint>
    {
        protected override IndicatorBase<IndicatorDataPoint> CreateIndicator()
        {
            return new ZeroLagExponentialMovingAverage(5);
        }

        protected override string TestFileName => "spy_with_zlema.csv";

        protected override string TestColumnName => "ZLEMA5";


        [Test]
        public void IsReadyAfterPeriodUpdates()
        {
            var zlema = new ZeroLagExponentialMovingAverage(5);
            zlema.Update(DateTime.UtcNow, 1m);
            zlema.Update(DateTime.UtcNow, 1m);
            zlema.Update(DateTime.UtcNow, 1m);
            zlema.Update(DateTime.UtcNow, 1m);
            zlema.Update(DateTime.UtcNow, 1m);
            zlema.Update(DateTime.UtcNow, 1m);
            Assert.IsFalse(zlema.IsReady);
            zlema.Update(DateTime.UtcNow, 1m);
            Assert.IsTrue(zlema.IsReady);
            Assert.AreEqual(zlema.WarmUpPeriod, 7);
        }

        [Test]
        public override void ResetsProperly()
        {
            var zlema = new ZeroLagExponentialMovingAverage(3);

            foreach (var data in TestHelper.GetDataStream(4))
            {
                zlema.Update(data);
            }
            Assert.IsTrue(zlema.IsReady);

            zlema.Reset();

            TestHelper.AssertIndicatorIsInDefaultState(zlema);
            zlema.Update(DateTime.UtcNow, 2.0m);
            zlema.Update(DateTime.UtcNow, 2.0m);
            zlema.Update(DateTime.UtcNow, 2.0m);
            zlema.Update(DateTime.UtcNow, 2.0m);
            Assert.AreEqual(zlema.Current.Value, 2.0m);
        }
    }
}
