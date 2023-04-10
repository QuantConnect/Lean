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
using System.Linq;
using NUnit.Framework;
using QuantConnect.Indicators;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    public class ExponentialMovingAverageTests : CommonIndicatorTests<IndicatorDataPoint>
    {
        protected override IndicatorBase<IndicatorDataPoint> CreateIndicator()
        {
             return new ExponentialMovingAverage(14);
        }

        protected override string TestFileName => "spy_ema.csv";

        protected override string TestColumnName => "EMA14";

        protected override Action<IndicatorBase<IndicatorDataPoint>, double> Assertion => TestHelper.AssertDeltaDecreases(2.5e-2);

        [Test]
        public void EmaComputesCorrectly()
        {
            const int period = 4;
            decimal[] values = { 1m, 10m, 100m, 1000m, 2000m, 3000m, 4000m, 5000m, 6000m, 7000m, 8000m, 9000m, 10000m };
            const decimal expFactor = 2m/(1m + period);

            var ema4 = new ExponentialMovingAverage(period);

            decimal expectedCurrent = 0m;
            for (int i = 0; i < values.Length; i++)
            {
                ema4.Update(new IndicatorDataPoint(DateTime.UtcNow.AddSeconds(i), values[i]));
                if (i == period - 1)
                {
                    // The indicator is ready after the first full period, the first value should be a SMA of the first period
                    expectedCurrent = values.Take(period).Sum() / period;
                }
                if (i >= period)
                {
                    expectedCurrent = values[i] * expFactor + (1 - expFactor) * expectedCurrent;
                }
                Assert.AreEqual(expectedCurrent, ema4.Current.Value, $"Index: {i}");
            }
        }

        [Test]
        public override void ResetsProperly()
        {
            // ema reset is just setting the value and samples back to 0
            var ema = new ExponentialMovingAverage(3);

            foreach (var data in TestHelper.GetDataStream(5))
            {
                ema.Update(data);
            }
            Assert.IsTrue(ema.IsReady);
            Assert.AreNotEqual(0m, ema.Current.Value);
            Assert.AreNotEqual(0, ema.Samples);

            ema.Reset();

            TestHelper.AssertIndicatorIsInDefaultState(ema);
        }
    }
}
