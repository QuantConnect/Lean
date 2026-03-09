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
    public class StandardDeviationTests : CommonIndicatorTests<IndicatorDataPoint>
    {
        [Test]
        public void ComputesCorrectly()
        {
            // Indicator output was compared against the following function in Julia
            // stdpop(v) = sqrt(sum((v - mean(v)).^2) / length(v))
            var std = new StandardDeviation(3);
            var reference = DateTime.MinValue;

            std.Update(reference.AddDays(1), 1m);
            Assert.AreEqual(0m, std.Current.Value);

            std.Update(reference.AddDays(2), -1m);
            Assert.AreEqual(1m, std.Current.Value);

            std.Update(reference.AddDays(3), 1m);
            Assert.AreEqual(0.942809041582063m, std.Current.Value);

            std.Update(reference.AddDays(4), -2m);
            Assert.AreEqual(1.24721912892465m, std.Current.Value);

            std.Update(reference.AddDays(5), 3m);
            Assert.AreEqual(2.05480466765633m, std.Current.Value);
        }

        [Test]
        public void ResetsProperlyStandardDeviation()
        {
            var std = new StandardDeviation(3);
            std.Update(DateTime.Today, 1m);
            std.Update(DateTime.Today.AddSeconds(1), 5m);
            std.Update(DateTime.Today.AddSeconds(2), 1m);
            Assert.IsTrue(std.IsReady);

            std.Reset();
            TestHelper.AssertIndicatorIsInDefaultState(std);
        }

        protected override IndicatorBase<IndicatorDataPoint> CreateIndicator()
        {
            return new StandardDeviation(10);
        }

        protected override string TestFileName => "spy_var.txt";

        protected override string TestColumnName => "Var";

        protected override Action<IndicatorBase<IndicatorDataPoint>, double> Assertion =>
            (indicator, expected) =>
                Assert.AreEqual(Math.Sqrt(expected), (double) indicator.Current.Value, 1e-6);
    }
}