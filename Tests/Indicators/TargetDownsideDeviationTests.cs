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
    public class TargetDownsideDeviationTests : CommonIndicatorTests<IndicatorDataPoint>
    {

        [Test]
        public void AlwaysZeroWitNonNegativeNumbers()
        {
            var tdd = new TargetDownsideDeviation(3);
            var reference = DateTime.MinValue;
            for (var i = 0; i < 100; i++)
            {
                tdd.Update(reference.AddDays(i), i);
                Assert.AreEqual(0m, tdd.Current.Value);
            }
        }

        [Test]
        public void ResetsProperlyTargetDownsideDeviation()
        {
            var tdd = new TargetDownsideDeviation(3);
            tdd.Update(DateTime.Today, 1m);
            tdd.Update(DateTime.Today.AddSeconds(1), 5m);
            tdd.Update(DateTime.Today.AddSeconds(2), 1m);
            Assert.IsTrue(tdd.IsReady);

            tdd.Reset();
            TestHelper.AssertIndicatorIsInDefaultState(tdd);
        }

        protected override IndicatorBase<IndicatorDataPoint> CreateIndicator()
        {
            // Even if the indicator is ready, there may be zero values
            ValueCanBeZero = true;
            return new TargetDownsideDeviation(15);
        }

        protected override string TestFileName => "target_downside_deviation.csv";

        protected override string TestColumnName => "denominator_rf_0_period_15";

        protected override Action<IndicatorBase<IndicatorDataPoint>, double> Assertion =>
            (indicator, expected) =>
                Assert.AreEqual(expected, (double)indicator.Current.Value, 1e-6);
    }
}
