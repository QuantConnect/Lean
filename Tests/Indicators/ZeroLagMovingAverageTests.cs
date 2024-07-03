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
using System.Linq;
using NUnit.Framework;
using QuantConnect.Indicators;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    public class ZeroLagMovingAverageTests : CommonIndicatorTests<IndicatorDataPoint>
    {
        protected override IndicatorBase<IndicatorDataPoint> CreateIndicator()
        {
            return new ZeroLagMovingAverage(5);
        }

        protected override string TestFileName => "spy_with_zlema.txt";

        protected override string TestColumnName => "ZLEMA5";

        protected override Action<IndicatorBase<IndicatorDataPoint>, double> Assertion
        {
            get { return (indicator, expected) => Assert.AreEqual(expected, (double)indicator.Current.Value, 1e-2); }
        }

        [Test]
        public void ZlemaComputesCorrectly()
        {
            var zlema = new ZeroLagMovingAverage(5);
            var data = new[] { 1m, 10m, 100m, 1000m, 10000m, 1234m, 56789m };

            var seen = new List<decimal>();
            for (int i = 0; i < data.Length; i++)
            {
                var datum = data[i];
                seen.Add(datum);
                zlema.Update(new IndicatorDataPoint(DateTime.Now.AddSeconds(i), datum));
                Assert.AreEqual(Enumerable.Reverse(seen).Take(zlema.Period).Average(), zlema.Current.Value);
            }
        }

        [Test]
        public void IsReadyAfterPeriodUpdates()
        {
            var zlema = new ZeroLagMovingAverage(3);

            zlema.Update(DateTime.UtcNow, 1m);
            zlema.Update(DateTime.UtcNow, 1m);
            Assert.IsFalse(zlema.IsReady);
            zlema.Update(DateTime.UtcNow, 1m);
            Assert.IsTrue(zlema.IsReady);
        }

        [Test]
        public override void ResetsProperly()
        {
            var zlema = new ZeroLagMovingAverage(3);

            foreach (var data in TestHelper.GetDataStream(4))
            {
                zlema.Update(data);
            }
            Assert.IsTrue(zlema.IsReady);

            zlema.Reset();

            TestHelper.AssertIndicatorIsInDefaultState(zlema);
            //TestHelper.AssertIndicatorIsInDefaultState(zlema._ema);
            zlema.Update(DateTime.UtcNow, 2.0m);
            Assert.AreEqual(zlema.Current.Value, 2.0m);
        }
    }
}
