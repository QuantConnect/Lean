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
    public class JurikMovingAverageTests : CommonIndicatorTests<IndicatorDataPoint>
    {
        protected override IndicatorBase<IndicatorDataPoint> CreateIndicator()
        {
            return new JurikMovingAverage(7);
        }

        protected override string TestFileName => "spy_jma.txt";

        protected override string TestColumnName => "JMA_7";

        [Test]
        public void JmaComputesCorrectly()
        {
            // Values verified against pandas_ta_classic reference implementation:
            //   from pandas_ta_classic.overlap.jma import jma
            //   jma(pd.Series([10,11,12,11,10,11,12,13,12,11]), length=7, phase=0)
            var jma = new JurikMovingAverage(7, 0, 2);
            var time = new DateTime(2024, 1, 1);
            var prices = new decimal[] { 10, 11, 12, 11, 10, 11, 12, 13, 12, 11 };

            // Feed first 6 bars: not ready (returns 0)
            for (var i = 0; i < 6; i++)
            {
                jma.Update(time.AddDays(i), prices[i]);
                Assert.IsFalse(jma.IsReady);
                Assert.AreEqual(0m, jma.Current.Value);
            }

            // Bar 7 (first ready bar)
            jma.Update(time.AddDays(6), prices[6]);
            Assert.IsTrue(jma.IsReady);
            Assert.AreEqual(11.504809085586068, (double)jma.Current.Value, 1e-6,
                "JMA at bar 7 should match pandas_ta reference");

            // Bar 8
            jma.Update(time.AddDays(7), prices[7]);
            Assert.AreEqual(12.474846874222544, (double)jma.Current.Value, 1e-6,
                "JMA at bar 8 should match pandas_ta reference");

            // Bar 9
            jma.Update(time.AddDays(8), prices[8]);
            Assert.AreEqual(12.515689573056372, (double)jma.Current.Value, 1e-6,
                "JMA at bar 9 should match pandas_ta reference");

            // Bar 10
            jma.Update(time.AddDays(9), prices[9]);
            Assert.AreEqual(11.711050292217287, (double)jma.Current.Value, 1e-6,
                "JMA at bar 10 should match pandas_ta reference");
        }

        [Test]
        public void PeriodAffectsOutput()
        {
            // Verify that different period values produce different outputs,
            // confirming the period parameter controls smoothing behavior
            var jma7 = new JurikMovingAverage(7);
            var jma14 = new JurikMovingAverage(14);
            var time = new DateTime(2024, 1, 1);

            // Feed enough data for both to be ready
            var prices = new decimal[] { 10, 11, 12, 11, 10, 11, 12, 13, 12, 11, 12, 13, 14, 13, 12 };
            for (var i = 0; i < prices.Length; i++)
            {
                jma7.Update(time.AddDays(i), prices[i]);
                jma14.Update(time.AddDays(i), prices[i]);
            }

            Assert.IsTrue(jma7.IsReady);
            Assert.IsTrue(jma14.IsReady);
            Assert.AreNotEqual(jma7.Current.Value, jma14.Current.Value,
                "JMA(7) and JMA(14) should produce different values for the same input");
        }
    }
}
