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

        protected override string TestColumnName => "TCI";

        protected override Action<IndicatorBase<IBaseDataBar>, double> Assertion =>
            (indicator, expected) =>
                Assert.AreEqual(expected, (double)indicator.Current.Value, 1e-3);

        [Test]
        public void ConstructorWithDefaultName()
        {
            var indicator = new WaveTrendOscillator(10, 21, 4);
            Assert.AreEqual("WTO(10,21,4)", indicator.Name);
        }

        [Test]
        public void ConstructorWithCustomName()
        {
            var indicator = new WaveTrendOscillator("CustomWTO", 10, 21, 4);
            Assert.AreEqual("CustomWTO", indicator.Name);
        }

        [Test]
        public void WarmUpPeriodIsCorrect()
        {
            var indicator = new WaveTrendOscillator(10, 21, 4);
            Assert.AreEqual(42, indicator.WarmUpPeriod);
        }

        [Test]
        public void IsReadyAfterWarmUp()
        {
            var indicator = new WaveTrendOscillator(10, 21, 4);
            Assert.IsFalse(indicator.IsReady);

            var start = new DateTime(2024, 1, 1);
            for (var i = 0; i < indicator.WarmUpPeriod; i++)
            {
                var price = 100m + i;
                indicator.Update(new TradeBar
                {
                    Time = start.AddDays(i),
                    Symbol = Symbol.Empty,
                    Open = price,
                    High = price + 1m,
                    Low = price - 1m,
                    Close = price,
                    Volume = 1000
                });
            }

            Assert.IsTrue(indicator.IsReady);
        }

        [Test]
        public void SignalIsReadyAfterWarmUp()
        {
            var indicator = new WaveTrendOscillator(10, 21, 4);
            var start = new DateTime(2024, 1, 1);
            for (var i = 0; i < indicator.WarmUpPeriod; i++)
            {
                var price = 100m + (decimal)Math.Sin(i / 3.0);
                indicator.Update(new TradeBar
                {
                    Time = start.AddDays(i),
                    Symbol = Symbol.Empty,
                    Open = price,
                    High = price + 0.5m,
                    Low = price - 0.5m,
                    Close = price,
                    Volume = 1000
                });
            }

            Assert.IsTrue(indicator.Signal.IsReady);
            Assert.AreNotEqual(0m, indicator.Signal.Current.Value);
        }
    }
}
