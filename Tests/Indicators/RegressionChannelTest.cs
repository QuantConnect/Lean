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

using NUnit.Framework;
using QuantConnect.Indicators;
using System;

namespace QuantConnect.Tests.Indicators
{
    /// <summary>
    /// Result tested vs. Python available at: http://tinyurl.com/o7redso
    /// </summary>
    [TestFixture]
    public class RegressionChannelTest
    {
        [Test]
        public void ComputesCorrectly()
        {
            const int period = 20;
            var indicator = new RegressionChannel(period, 2);
            var stdDev = new StandardDeviation(period);
            var time = DateTime.Now;

            var prices = LeastSquaresMovingAverageTest.Prices;
            var expected = LeastSquaresMovingAverageTest.Expected;

            var actual = new decimal[prices.Length];

            for (var i = 0; i < prices.Length; i++)
            {
                indicator.Update(time.AddMinutes(i), prices[i]);
                stdDev.Update(time, prices[i]);
                actual[i] = Math.Round(indicator.Current.Value, 4);
            }
            Assert.AreEqual(expected, actual);

            var expectedUpper = indicator.Current.Value + stdDev.Current.Value * 2;
            Assert.AreEqual(expectedUpper, indicator.UpperChannel.Current.Value);
            var expectedLower = indicator.Current.Value - stdDev.Current.Value * 2;
            Assert.AreEqual(expectedLower, indicator.LowerChannel.Current.Value);
        }

        [Test]
        public void ResetsProperly()
        {
            const int period = 10;
            var indicator = new RegressionChannel(period, 2);
            var time = DateTime.Now;

            for (var i = 0; i < period + 1; i++)
            {
                indicator.Update(time.AddMinutes(i), 1m);
            }
            Assert.IsTrue(indicator.IsReady, "Regression Channel ready");
            indicator.Reset();
            TestHelper.AssertIndicatorIsInDefaultState(indicator);
        }

        [Test]
        public void WarmsUpProperly()
        {
            var indicator = new RegressionChannel(20, 2);
            var period = ((IIndicatorWarmUpPeriodProvider)indicator).WarmUpPeriod;
            var prices = LeastSquaresMovingAverageTest.Prices;
            var time = DateTime.Now;

            for (var i = 0; i < period; i++)
            {
                indicator.Update(time.AddMinutes(i), prices[i]);
                Assert.AreEqual(i == period - 1, indicator.IsReady);
            }
        }

        [Test]
        public void LowerUpperChannelUpdateOnce()
        {
            var regressionChannel = new RegressionChannel(2, 2m);
            var lowerChannelUpdateCount = 0;
            var upperChannelUpdateCount = 0;
            regressionChannel.LowerChannel.Updated += (sender, updated) =>
            {
                lowerChannelUpdateCount++;
            };
            regressionChannel.UpperChannel.Updated += (sender, updated) =>
            {
                upperChannelUpdateCount++;
            };

            Assert.AreEqual(0, lowerChannelUpdateCount);
            Assert.AreEqual(0, upperChannelUpdateCount);
            regressionChannel.Update(DateTime.Today, 1m);

            Assert.AreEqual(1, lowerChannelUpdateCount);
            Assert.AreEqual(1, upperChannelUpdateCount);
        }
    }
}