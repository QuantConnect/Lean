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
    public class MeanAbsoluteDeviationTests
    {
        [Test]
        public void ComputesCorrectly()
        {
            // Indicator output was compared against the octave code:
            // mad = @(v) mean(abs(v - mean(v)));
            var mad = new MeanAbsoluteDeviation(3);
            var reference = DateTime.MinValue;

            mad.Update(reference.AddDays(1), 1m);
            Assert.AreEqual(0m, mad.Current.Value);

            mad.Update(reference.AddDays(2), -1m);
            Assert.AreEqual(1m, mad.Current.Value);

            mad.Update(reference.AddDays(3), 1m);
            Assert.AreEqual(0.888888888888889m, decimal.Round(mad.Current.Value, 15));

            mad.Update(reference.AddDays(4), -2m);
            Assert.AreEqual(1.111111111111111m, decimal.Round(mad.Current.Value, 15));

            mad.Update(reference.AddDays(5), 3m);
            Assert.AreEqual(1.777777777777778m, decimal.Round(mad.Current.Value, 15));
        }

        [Test]
        public void ResetsProperly()
        {
            var mad = new MeanAbsoluteDeviation(3);
            mad.Update(DateTime.Today, 1m);
            mad.Update(DateTime.Today.AddSeconds(1), 2m);
            mad.Update(DateTime.Today.AddSeconds(1), 1m);
            Assert.IsTrue(mad.IsReady);

            mad.Reset();

            TestHelper.AssertIndicatorIsInDefaultState(mad);
            TestHelper.AssertIndicatorIsInDefaultState(mad.Mean);
        }

        [Test]
        public void WarmsUpProperly()
        {
            var mad = new MeanAbsoluteDeviation(20);
            var time = DateTime.Today;
            var period = ((IIndicatorWarmUpPeriodProvider)mad).WarmUpPeriod;

            for (var i = 0; i < period; i++)
            {
                mad.Update(time.AddDays(i), i);
                Assert.AreEqual(i == period - 1, mad.IsReady);
            }
        }
    }
}