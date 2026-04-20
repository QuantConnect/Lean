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
            RenkoBarSize = 1m;
            VolumeRenkoBarSize = 0.5m;
            return new WaveTrendOscillator(10, 21, 4);
        }

        protected override string TestFileName => "spy_wto.txt";

        protected override string TestColumnName => "WaveTrend";

        protected override Action<IndicatorBase<IBaseDataBar>, double> Assertion =>
            (indicator, expected) =>
                Assert.AreEqual(expected, (double)indicator.Current.Value, 1e-4);

        [Test]
        public void ComparesWithExternalDataSignal()
        {
            var wto = CreateIndicator();
            TestHelper.TestIndicator(
                wto,
                TestFileName,
                "Signal",
                (ind, expected) => Assert.AreEqual(
                    expected,
                    (double)((WaveTrendOscillator)ind).Signal.Current.Value,
                    delta: 1e-4
                )
            );
        }

        [Test]
        public override void ResetsProperly()
        {
            var wto = new WaveTrendOscillator(10, 21, 4);
            foreach (var bar in TestHelper.GetTradeBarStream(TestFileName, false))
            {
                wto.Update(bar);
            }
            Assert.IsTrue(wto.IsReady);
            Assert.IsTrue(wto.WaveTrend.IsReady);
            Assert.IsTrue(wto.Signal.IsReady);

            wto.Reset();
            TestHelper.AssertIndicatorIsInDefaultState(wto);
            TestHelper.AssertIndicatorIsInDefaultState(wto.WaveTrend);
            TestHelper.AssertIndicatorIsInDefaultState(wto.Signal);
        }

        [Test]
        public override void WarmsUpProperly()
        {
            const int channelPeriod = 3;
            const int averagePeriod = 4;
            const int signalPeriod = 2;
            var wto = new WaveTrendOscillator(channelPeriod, averagePeriod, signalPeriod);
            var expectedWarmUp = 2 * channelPeriod + averagePeriod + signalPeriod - 3;
            Assert.AreEqual(expectedWarmUp, wto.WarmUpPeriod);

            var reference = DateTime.Today;
            for (var i = 0; i < expectedWarmUp - 1; i++)
            {
                Assert.IsFalse(wto.IsReady);
                wto.Update(new TradeBar
                {
                    Symbol = Symbols.SPY,
                    Time = reference.AddDays(i),
                    Open = 100m + i,
                    High = 101m + i,
                    Low = 99m + i,
                    Close = 100.5m + i,
                    Volume = 1000
                });
            }
            wto.Update(new TradeBar
            {
                Symbol = Symbols.SPY,
                Time = reference.AddDays(expectedWarmUp - 1),
                Open = 100m,
                High = 101m,
                Low = 99m,
                Close = 100.5m,
                Volume = 1000
            });
            Assert.IsTrue(wto.IsReady);
            Assert.IsTrue(wto.WaveTrend.IsReady);
            Assert.IsTrue(wto.Signal.IsReady);
        }

        [Test]
        public void DefaultConstructorUsesStandardParameters()
        {
            var wto = new WaveTrendOscillator();
            Assert.AreEqual("WTO(10,21,4)", wto.Name);
            Assert.AreEqual(2 * 10 + 21 + 4 - 3, wto.WarmUpPeriod);
        }
    }
}
