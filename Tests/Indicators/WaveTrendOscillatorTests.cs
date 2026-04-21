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
            return new WaveTrendOscillator(10, 21, 4);
        }

        protected override string TestFileName => "spy_wto.csv";

        protected override string TestColumnName => "WT1";

        [Test]
        public void ComparesSignalAgainstExternalData()
        {
            var wto = (WaveTrendOscillator)CreateIndicator();
            TestHelper.TestIndicator(
                wto,
                TestFileName,
                "WT2",
                (indicator, expected) => Assert.AreEqual(
                    expected,
                    (double)((WaveTrendOscillator)indicator).Signal.Current.Value,
                    1e-3)
            );
        }

        [Test]
        public void WarmUpPeriodMatchesFormula()
        {
            var wto = new WaveTrendOscillator(10, 21, 4);
            Assert.AreEqual(42, wto.WarmUpPeriod);
            Assert.AreEqual(2 * 10 + 21 + 4 - 3, wto.WarmUpPeriod);
        }

        [Test]
        public void IsReadyOnlyAfterWarmUp()
        {
            const int channelPeriod = 3;
            const int averagePeriod = 4;
            const int signalPeriod = 2;
            var wto = new WaveTrendOscillator(channelPeriod, averagePeriod, signalPeriod);
            var time = new DateTime(2024, 1, 1);

            for (var i = 1; i <= wto.WarmUpPeriod; i++)
            {
                Assert.IsFalse(wto.IsReady, $"Should not be ready after {i - 1} samples");
                wto.Update(new TradeBar
                {
                    Time = time.AddDays(i),
                    High = 10m + i,
                    Low = 9m + i,
                    Close = 9.5m + i
                });
            }

            Assert.IsTrue(wto.IsReady);
            Assert.IsTrue(wto.Signal.IsReady);
        }

        [Test]
        public void DefaultConstructorProducesExpectedName()
        {
            var wto = new WaveTrendOscillator(10, 21, 4);
            Assert.AreEqual("WTO(10,21,4)", wto.Name);
        }

        [Test]
        public void NamedConstructorPreservesName()
        {
            var wto = new WaveTrendOscillator("custom", 10, 21, 4);
            Assert.AreEqual("custom", wto.Name);
        }
    }
}
