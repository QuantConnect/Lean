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
            return new WaveTrendOscillator("WTO", 10, 21, 4);
        }

        protected override string TestFileName => "spy_wto.csv";

        protected override string TestColumnName => "WTO";

        [Test]
        public void ComparesWithExternalDataSignal()
        {
            var waveTrendOscillator = new WaveTrendOscillator("WTO", 10, 21, 4);
            TestHelper.TestIndicator(
                waveTrendOscillator,
                TestFileName,
                "WTO_Signal",
                (ind, expected) => Assert.AreEqual(expected, (double)((WaveTrendOscillator)ind).Signal.Current.Value, 1e-4));
        }

        [Test]
        public void IsReadyAfterWarmUpPeriod()
        {
            var waveTrendOscillator = new WaveTrendOscillator(10, 21, 4);
            Assert.AreEqual(42, waveTrendOscillator.WarmUpPeriod);

            var reference = new DateTime(2020, 1, 1);
            for (var i = 0; i < waveTrendOscillator.WarmUpPeriod - 1; i++)
            {
                waveTrendOscillator.Update(new TradeBar
                {
                    Time = reference.AddDays(i),
                    High = 100m + i,
                    Low = 99m + i,
                    Close = 99.5m + i,
                    Volume = 1000
                });
                Assert.IsFalse(waveTrendOscillator.IsReady);
            }

            waveTrendOscillator.Update(new TradeBar
            {
                Time = reference.AddDays(waveTrendOscillator.WarmUpPeriod - 1),
                High = 200m,
                Low = 199m,
                Close = 199.5m,
                Volume = 1000
            });
            Assert.IsTrue(waveTrendOscillator.IsReady);
            Assert.IsTrue(waveTrendOscillator.Signal.IsReady);
        }

        [Test]
        public void ReturnsZeroOnFlatMarket()
        {
            var waveTrendOscillator = new WaveTrendOscillator("WTO", 10, 21, 4);
            var reference = new DateTime(2020, 1, 1);

            for (var i = 0; i < 60; i++)
            {
                waveTrendOscillator.Update(new TradeBar
                {
                    Time = reference.AddDays(i),
                    Open = 100m,
                    High = 100m,
                    Low = 100m,
                    Close = 100m,
                    Volume = 1000
                });
            }

            // On a flat market the absolute deviation EMA is zero so the indicator short-circuits
            // to zero rather than dividing by zero.
            Assert.AreEqual(0m, waveTrendOscillator.Current.Value);
            Assert.AreEqual(0m, waveTrendOscillator.Signal.Current.Value);
        }

        [Test]
        public void DefaultConstructorUsesStandardParameters()
        {
            var waveTrendOscillator = new WaveTrendOscillator();
            Assert.AreEqual("WTO(10,21,4)", waveTrendOscillator.Name);
            Assert.AreEqual(42, waveTrendOscillator.WarmUpPeriod);
        }
    }
}
