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
    public class MaximumTests : CommonIndicatorTests<IndicatorDataPoint>
    {
        protected override IndicatorBase<IndicatorDataPoint> CreateIndicator()
        {
            return new Maximum(5);
        }

        protected override string TestFileName
        {
            get { return "spy_max.txt"; }
        }

        protected override string TestColumnName
        {
            get { return "MAX_5"; }
        }

        [Test]
        public void ComputesCorrectly()
        {
            var max = new Maximum(3);

            var reference = DateTime.MinValue;

            max.Update(reference.AddDays(1), 1m);
            Assert.AreEqual(1m, max.Current.Value);
            Assert.AreEqual(0, max.PeriodsSinceMaximum);

            max.Update(reference.AddDays(2), -1m);
            Assert.AreEqual(1m, max.Current.Value);
            Assert.AreEqual(1, max.PeriodsSinceMaximum);

            max.Update(reference.AddDays(3), 0m);
            Assert.AreEqual(1m, max.Current.Value);
            Assert.AreEqual(2, max.PeriodsSinceMaximum);

            max.Update(reference.AddDays(4), -2m);
            Assert.AreEqual(0m, max.Current.Value);
            Assert.AreEqual(1, max.PeriodsSinceMaximum);

            max.Update(reference.AddDays(5), -2m);
            Assert.AreEqual(0m, max.Current.Value);
            Assert.AreEqual(2, max.PeriodsSinceMaximum);
        }

        [Test]
        public void ComputesCorrectlyMaximum()
        {
            const int period = 5;
            var max = new Maximum(period);

            Assert.AreEqual(0m, max.Current.Value);

            // test an increasing stream of data
            for (int i = 0; i < period; i++)
            {
                max.Update(DateTime.Now.AddDays(i), i);
                Assert.AreEqual(i, max.Current.Value);
                Assert.AreEqual(0, max.PeriodsSinceMaximum);
            }

            // test a decreasing stream of data
            for (int i = 0; i < period; i++)
            {
                max.Update(DateTime.Now.AddDays(period + i), period - i - 1);
                Assert.AreEqual(period - 1, max.Current.Value);
                Assert.AreEqual(i, max.PeriodsSinceMaximum);
            }

            Assert.AreEqual(max.Period, max.PeriodsSinceMaximum + 1);
        }

        [Test]
        public void ResetsProperlyMaximum()
        {
            var max = new Maximum(3);
            max.Update(DateTime.Today, 1m);
            max.Update(DateTime.Today.AddSeconds(1), 2m);
            max.Update(DateTime.Today.AddSeconds(2), 1m);
            Assert.IsTrue(max.IsReady);

            max.Reset();
            Assert.AreEqual(0, max.PeriodsSinceMaximum);
            TestHelper.AssertIndicatorIsInDefaultState(max);
        }
    }
}
