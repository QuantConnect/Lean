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
using QuantConnect.Data.Market;
using QuantConnect.Indicators;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    public class WaveTrendOscillatorTests : CommonIndicatorTests<IBaseDataBar>
    {
        protected override IndicatorBase<IBaseDataBar> CreateIndicator()
        {
            return new WaveTrendOscillator(10, 21, 4);
        }

        protected override string TestFileName => "spy_wto.csv";

        protected override string TestColumnName => "WT1";

        [Test]
        public void ComparesWithExternalDataSignal()
        {
            var wto = (WaveTrendOscillator)CreateIndicator();
            TestHelper.TestIndicator(
                wto,
                TestFileName,
                "WT2",
                (ind, expected) => Assert.AreEqual(
                    expected,
                    (double)((WaveTrendOscillator)ind).Signal.Current.Value,
                    delta: 1e-3
                )
            );
        }

        [Test]
        public void WarmUpPeriodMatchesConstructorArguments()
        {
            var wto = new WaveTrendOscillator(10, 21, 4);
            Assert.AreEqual(2 * 10 + 21 + 4 - 3, wto.WarmUpPeriod);
            Assert.AreEqual(42, wto.WarmUpPeriod);
        }

        [Test]
        public void IsReadyOnlyAfterWarmUp()
        {
            var channelPeriod = 3;
            var averagePeriod = 4;
            var signalPeriod = 2;
            var wto = new WaveTrendOscillator(channelPeriod, averagePeriod, signalPeriod);
            var warmUp = 2 * channelPeriod + averagePeriod + signalPeriod - 3;

            for (var i = 1; i <= warmUp; i++)
            {
                Assert.IsFalse(wto.IsReady, $"Should not be ready after {i - 1} samples");
                wto.Update(new TradeBar
                {
                    Time = System.DateTime.Today.AddDays(i),
                    High = 10m + i,
                    Low = 9m + i,
                    Close = 9.5m + i
                });
            }

            Assert.IsTrue(wto.IsReady);
            Assert.IsTrue(wto.WT1.IsReady);
            Assert.IsTrue(wto.Signal.IsReady);
        }

        [Test]
        public void NamedConstructorProducesExpectedName()
        {
            var wto = new WaveTrendOscillator("custom", 10, 21, 4);
            Assert.AreEqual("custom", wto.Name);

            var defaultNamed = new WaveTrendOscillator(10, 21, 4);
            Assert.AreEqual("WTO(10,21,4)", defaultNamed.Name);
        }
    }
}
