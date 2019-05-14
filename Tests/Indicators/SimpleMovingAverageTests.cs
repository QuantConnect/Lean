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
    public class SimpleMovingAverageTests : CommonIndicatorTests<IndicatorDataPoint>
    {
        protected override IndicatorBase<IndicatorDataPoint> CreateIndicator()
        {
            return new SimpleMovingAverage(14);
        }

        protected override string TestFileName => "spy_with_indicators.txt";

        protected override string TestColumnName => "SMA14";

        protected override Action<IndicatorBase<IndicatorDataPoint>, double> Assertion
        {
            get { return (indicator, expected) => Assert.AreEqual(expected, (double)indicator.Current.Value, 1e-2); }
        }

        [Test]
        public void SmaComputesCorrectly()
        {
            var sma = new SimpleMovingAverage(4);
            var data = new[] {1m, 10m, 100m, 1000m, 10000m, 1234m, 56789m};

            var seen = new List<decimal>();
            for (int i = 0; i < data.Length; i++)
            {
                var datum = data[i];
                seen.Add(datum);
                sma.Update(new IndicatorDataPoint(DateTime.Now.AddSeconds(i), datum));
                Assert.AreEqual(Enumerable.Reverse(seen).Take(sma.Period).Average(), sma.Current.Value);
            }
        }

        [Test]
        public void IsReadyAfterPeriodUpdates()
        {
            var sma = new SimpleMovingAverage(3);

            sma.Update(DateTime.UtcNow, 1m);
            sma.Update(DateTime.UtcNow, 1m);
            Assert.IsFalse(sma.IsReady);
            sma.Update(DateTime.UtcNow, 1m);
            Assert.IsTrue(sma.IsReady);
        }

        [Test]
        public override void ResetsProperly()
        {
            var sma = new SimpleMovingAverage(3);

            foreach (var data in TestHelper.GetDataStream(4))
            {
                sma.Update(data);
            }
            Assert.IsTrue(sma.IsReady);
            
            sma.Reset();

            TestHelper.AssertIndicatorIsInDefaultState(sma);
            TestHelper.AssertIndicatorIsInDefaultState(sma.RollingSum);
            sma.Update(DateTime.UtcNow, 2.0m);
            Assert.AreEqual(sma.Current.Value, 2.0m);
        }
    }
}