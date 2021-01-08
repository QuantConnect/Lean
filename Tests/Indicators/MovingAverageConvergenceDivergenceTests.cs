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
    public class MovingAverageConvergenceDivergenceTests : CommonIndicatorTests<IndicatorDataPoint>
    {
        protected override IndicatorBase<IndicatorDataPoint> CreateIndicator()
        {
            return new MovingAverageConvergenceDivergence(fastPeriod: 12, slowPeriod: 26, signalPeriod: 9);
        }

        protected override string TestFileName => "spy_with_macd.txt";

        protected override string TestColumnName => "MACD";

        [Test]
        public void FastPeriodLessThanSlowPeriod()
        {
            var a = new MovingAverageConvergenceDivergence(fastPeriod: 2, slowPeriod: 3, signalPeriod: 2);
            Assert.Throws<ArgumentException>(() => new MovingAverageConvergenceDivergence(fastPeriod: 3, slowPeriod: 3, signalPeriod: 2));
            Assert.Throws<ArgumentException>(() => new MovingAverageConvergenceDivergence(fastPeriod: 4, slowPeriod: 3, signalPeriod: 2));
        }

        [Test]
        public void ComparesWithExternalDataMacdHistogram()
        {
            var macd = CreateIndicator();
            TestHelper.TestIndicator(
                macd,
                TestFileName,
                "Histogram",
                (ind, expected) => Assert.AreEqual(
                    expected,
                    (double) ((MovingAverageConvergenceDivergence) ind).Histogram.Current.Value,
                    delta: 1e-4
                )
            );
        }

        [Test]
        public void ComparesWithExternalDataMacdSignal()
        {
            var macd = CreateIndicator();
            TestHelper.TestIndicator(
                macd,
                TestFileName,
                "Signal",
                (ind, expected) => Assert.AreEqual(
                    expected,
                    (double) ((MovingAverageConvergenceDivergence) ind).Signal.Current.Value,
                    delta: 1e-4
                )
            );
        }

        [Test]
        public void ComparesWithExternalDataMacdValue()
        {
            var macd = CreateIndicator();
            TestHelper.TestIndicator(
                macd,
                TestFileName,
                "MACD",
                (ind, expected) => Assert.AreEqual(
                    expected,
                    (double)((MovingAverageConvergenceDivergence)ind).Current.Value,
                    delta: 1e-4
                )
            );
        }

        [Test]
        public override void WarmsUpProperly()
        {
            int fastPeriod = 3,
                slowPeriod = 4,
                signalPeriod = 2;
            var macd = new MovingAverageConvergenceDivergence(fastPeriod: fastPeriod, slowPeriod: slowPeriod, signalPeriod: signalPeriod);

            Assert.IsFalse(macd.Signal.IsReady);
            Assert.IsFalse(macd.Histogram.IsReady);
            Assert.IsFalse(macd.IsReady);

            for (var i = 0; i < fastPeriod; i++)
            {
                Assert.IsFalse(macd.Fast.IsReady);
                macd.Update(new IndicatorDataPoint(DateTime.Today.AddSeconds(i), i));
            }
            Assert.IsTrue(macd.Fast.IsReady);


            for (var i = fastPeriod; i < slowPeriod; i++)
            {
                Assert.IsFalse(macd.Slow.IsReady);
                macd.Update(new IndicatorDataPoint(DateTime.Today.AddSeconds(i), i));
            }
            Assert.IsTrue(macd.Slow.IsReady);


            for (var i = slowPeriod; i < macd.WarmUpPeriod; i++)
            {
                Assert.IsFalse(macd.Signal.IsReady);
                Assert.IsFalse(macd.Histogram.IsReady);
                Assert.IsFalse(macd.IsReady);
                macd.Update(new IndicatorDataPoint(DateTime.Today.AddSeconds(i), i));
            }
            Assert.IsTrue(macd.Signal.IsReady);
            Assert.IsTrue(macd.Histogram.IsReady);
            Assert.IsTrue(macd.IsReady);
        }
    }
}