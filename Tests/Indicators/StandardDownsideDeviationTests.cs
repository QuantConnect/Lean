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
    public class StandardDownsideDeviationTests : CommonIndicatorTests<IndicatorDataPoint>
    {
        [Test]
        public void ComputesCorrectly()
        {
            var stdd = new StandardDownsideDeviation(3);
            var reference = DateTime.MinValue;

            stdd.Update(reference.AddDays(1), 1m);
            Assert.AreEqual(0m, stdd.Current.Value);

            stdd.Update(reference.AddDays(2), -1m);
            Assert.AreEqual(0m, stdd.Current.Value);

            stdd.Update(reference.AddDays(3), 1m);
            Assert.AreEqual(0m, stdd.Current.Value);

            stdd.Update(reference.AddDays(4), -2m);
            Assert.AreEqual(0.5m, stdd.Current.Value);

            stdd.Update(reference.AddDays(5), 3m);
            Assert.AreEqual(0m, stdd.Current.Value);
        }

        [Test]
        public void AlwaysZeroWitNonNegativeNumbers()
        {
            var stdd = new StandardDownsideDeviation(3);
            var reference = DateTime.MinValue;
            for (var i = 0; i < 100; i++)
            {
                stdd.Update(reference.AddDays(i), i);
                Assert.AreEqual(0m, stdd.Current.Value);
            }
        }

        [Test]
        public void ResetsProperlyStandardDeviation()
        {
            var stdd = new StandardDownsideDeviation(3);
            stdd.Update(DateTime.Today, 1m);
            stdd.Update(DateTime.Today.AddSeconds(1), 5m);
            stdd.Update(DateTime.Today.AddSeconds(2), 1m);
            Assert.IsTrue(stdd.IsReady);

            stdd.Reset();
            TestHelper.AssertIndicatorIsInDefaultState(stdd);
        }

        protected override IndicatorBase<IndicatorDataPoint> CreateIndicator()
        {
            return new StandardDownsideDeviation(10);
        }

        protected override string TestFileName => "downside_variance.csv";

        protected override string TestColumnName => "downside_std_10";

        protected override Action<IndicatorBase<IndicatorDataPoint>, double> Assertion =>
            (indicator, expected) =>
                Assert.AreEqual(expected, (double) indicator.Current.Value, 1e-6);
    }
}
