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
            // Hand-computed values for JMA(7, phase=0, power=2)
            // with prices: 10, 11, 12, 11, 10, 11, 12, 13, 12, 11
            var jma = new JurikMovingAverage(7, 0, 2);
            var time = new DateTime(2024, 1, 1);
            var prices = new decimal[] { 10, 11, 12, 11, 10, 11, 12, 13, 12, 11 };

            // Feed prices and verify
            for (var i = 0; i < prices.Length; i++)
            {
                jma.Update(time.AddDays(i), prices[i]);
            }

            // Bars 1-6: not ready (returns 0)
            Assert.IsFalse(new JurikMovingAverage(7, 0, 2).IsReady);

            // Build fresh indicator and check each step
            var jma2 = new JurikMovingAverage(7, 0, 2);

            // Feed first 6 bars — not ready
            for (var i = 0; i < 6; i++)
            {
                jma2.Update(time.AddDays(i), prices[i]);
                Assert.IsFalse(jma2.IsReady);
                Assert.AreEqual(0m, jma2.Current.Value);
            }

            // Bar 7 (seed): JMA = 12.0
            jma2.Update(time.AddDays(6), prices[6]);
            Assert.IsTrue(jma2.IsReady);
            Assert.AreEqual(12m, jma2.Current.Value);

            // Bar 8: JMA ≈ 12.395300300975162
            jma2.Update(time.AddDays(7), prices[7]);
            Assert.AreEqual(12.395300300975162, (double)jma2.Current.Value, 1e-6,
                "JMA at bar 8 should match hand-computed value");

            // Bar 9: JMA ≈ 12.351126982231602
            jma2.Update(time.AddDays(8), prices[8]);
            Assert.AreEqual(12.351126982231602, (double)jma2.Current.Value, 1e-6,
                "JMA at bar 9 should match hand-computed value");

            // Bar 10: JMA ≈ 11.800059939173682
            jma2.Update(time.AddDays(9), prices[9]);
            Assert.AreEqual(11.800059939173682, (double)jma2.Current.Value, 1e-6,
                "JMA at bar 10 should match hand-computed value");
        }
    }
}
