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
using QuantConnect.Data.Market;
using QuantConnect.Indicators;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    public class WaveTrendOscillatorTests : CommonIndicatorTests<IBaseDataBar>
    {
        protected override IndicatorBase<IBaseDataBar> CreateIndicator()
        {
            return new WaveTrendOscillator(channelPeriod: 10, averagePeriod: 21, signalPeriod: 4);
        }

        protected override string TestFileName => "spy_wto.csv";

        protected override string TestColumnName => "WT1";

        protected override Action<IndicatorBase<IBaseDataBar>, double> Assertion =>
            (indicator, expected) =>
                Assert.AreEqual(expected, (double)indicator.Current.Value, 1e-4);

        [Test]
        public void ComparesAgainstExternalDataSignalLine()
        {
            var wto = (WaveTrendOscillator)CreateIndicator();
            TestHelper.TestIndicator(
                wto,
                TestFileName,
                "WT2",
                (ind, expected) => Assert.AreEqual(
                    expected,
                    (double)((WaveTrendOscillator)ind).Signal.Current.Value,
                    delta: 1e-4
                )
            );
        }

        [Test]
        public void ConstructorThrowsOnNonPositivePeriods()
        {
            Assert.Throws<ArgumentException>(() => new WaveTrendOscillator(0, 21, 4));
            Assert.Throws<ArgumentException>(() => new WaveTrendOscillator(10, 0, 4));
            Assert.Throws<ArgumentException>(() => new WaveTrendOscillator(10, 21, 0));
        }

        [Test]
        public void IsReadyOnlyAfterAllSubIndicatorsAreReady()
        {
            const int channelPeriod = 3;
            const int averagePeriod = 4;
            const int signalPeriod = 2;
            var wto = new WaveTrendOscillator(channelPeriod, averagePeriod, signalPeriod);

            Assert.IsFalse(wto.ChannelAverage.IsReady);
            Assert.IsFalse(wto.ChannelDeviation.IsReady);
            Assert.IsFalse(wto.ChannelIndexAverage.IsReady);
            Assert.IsFalse(wto.Signal.IsReady);
            Assert.IsFalse(wto.IsReady);

            // ChannelAverage (ESA) becomes ready first.
            for (var i = 0; i < channelPeriod; i++)
            {
                Assert.IsFalse(wto.ChannelAverage.IsReady);
                wto.Update(MakeBar(i));
            }
            Assert.IsTrue(wto.ChannelAverage.IsReady);
            Assert.IsFalse(wto.ChannelDeviation.IsReady);

            // ChannelDeviation (D) takes another channelPeriod-1 bars to fill its window
            // because we only feed it once ChannelAverage is ready.
            for (var i = 0; i < channelPeriod - 1; i++)
            {
                Assert.IsFalse(wto.ChannelDeviation.IsReady);
                wto.Update(MakeBar(channelPeriod + i));
            }
            Assert.IsTrue(wto.ChannelDeviation.IsReady);
            Assert.IsFalse(wto.ChannelIndexAverage.IsReady);

            // ChannelIndexAverage (WT1) takes another averagePeriod-1 bars.
            for (var i = 0; i < averagePeriod - 1; i++)
            {
                Assert.IsFalse(wto.ChannelIndexAverage.IsReady);
                wto.Update(MakeBar(2 * channelPeriod - 1 + i));
            }
            Assert.IsTrue(wto.ChannelIndexAverage.IsReady);
            Assert.IsFalse(wto.Signal.IsReady);

            // Signal (WT2) takes another signalPeriod-1 bars.
            for (var i = 0; i < signalPeriod - 1; i++)
            {
                Assert.IsFalse(wto.Signal.IsReady);
                Assert.IsFalse(wto.IsReady);
                wto.Update(MakeBar(2 * channelPeriod + averagePeriod - 2 + i));
            }
            Assert.IsTrue(wto.Signal.IsReady);
            Assert.IsTrue(wto.IsReady);
            Assert.AreEqual(wto.WarmUpPeriod, wto.Samples);
        }

        [Test]
        public override void ResetsProperly()
        {
            var wto = (WaveTrendOscillator)CreateIndicator();
            TestHelper.TestIndicatorReset(wto, TestFileName);

            TestHelper.AssertIndicatorIsInDefaultState(wto.ChannelAverage);
            TestHelper.AssertIndicatorIsInDefaultState(wto.ChannelDeviation);
            TestHelper.AssertIndicatorIsInDefaultState(wto.ChannelIndexAverage);
            TestHelper.AssertIndicatorIsInDefaultState(wto.Signal);
        }

        private static TradeBar MakeBar(int days)
        {
            var time = new DateTime(2024, 1, 1).AddDays(days);
            var close = 100m + days;
            return new TradeBar(time, Symbols.SPY, close, close + 5m, close - 5m, close, 100m, Time.OneDay);
        }
    }
}
