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
    public class LinearWeightedMovingAverageTests
    {
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        // See http://en.wikipedia.org/wiki/Moving_average
        // for the formula and the numbers in this test.
        public void ComputesCorrectly(int period)
        {
            var values = new[] {77m, 79m, 79m, 81m, 83m};
            var weights = Enumerable.Range(1, period).ToArray();
            var current = weights.Sum(i => i * values[i - 1]) / weights.Sum();

            var lwma = new LinearWeightedMovingAverage(period);
            var time = DateTime.UtcNow;

            for (var i = 0; i < period; i++)
            {
                lwma.Update(time.AddSeconds(i), values[i]);
            }
            Assert.AreEqual(current, lwma.Current.Value);
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        // See http://en.wikipedia.org/wiki/Moving_average
        // for the formula and the numbers in this test.
        public void ResetsProperly(int period)
        {
            var values = new[] { 77m, 79m, 79m, 81m, 83m };
            var weights = Enumerable.Range(1, period).ToArray();
            var current = weights.Sum(i => i * values[i - 1]) / weights.Sum();

            var lwma = new LinearWeightedMovingAverage(period);
            var time = DateTime.UtcNow;

            for (var i = 0; i < period; i++)
            {
                lwma.Update(time.AddSeconds(i), values[i]);
            }
            Assert.AreEqual(current, lwma.Current.Value);
            Assert.IsTrue(lwma.IsReady);
            Assert.AreNotEqual(0m, lwma.Current.Value);
            Assert.AreNotEqual(0, lwma.Samples);

            lwma.Reset();

            TestHelper.AssertIndicatorIsInDefaultState(lwma);
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        // See http://en.wikipedia.org/wiki/Moving_average
        // for the formula and the numbers in this test.
        public void WarmsUpProperly(int period)
        {
            var values = new[] { 77m, 79m, 79m, 81m, 83m };
            var weights = Enumerable.Range(1, period).ToArray();
            var current = weights.Sum(i => i * values[i - 1]) / weights.Sum();

            var lwma = new LinearWeightedMovingAverage(period);
            var time = DateTime.UtcNow;

            var warmUpPeriod = (lwma as IIndicatorWarmUpPeriodProvider)?.WarmUpPeriod;

            for (var i = 0; i < warmUpPeriod; i++)
            {
                lwma.Update(time.AddSeconds(i), values[i]);
                Assert.AreEqual(i == warmUpPeriod - 1, lwma.IsReady);
            }
            Assert.AreEqual(current, lwma.Current.Value);
        }
    }
}