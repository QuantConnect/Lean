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
using System;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    internal class NewHighsNewLowsVolumeRatioTests : NewHighsNewLowsTestsBase<TradeBar>
    {
        protected override NewHighsNewLows<TradeBar> CreateNewHighsNewLowsIndicator()
        {
            // For test purposes we use period of two
            return new NewHighsNewLowsVolume("test_name", 2);
        }

        protected override IndicatorBase<TradeBar> GetSubIndicator(IndicatorBase<TradeBar> mainIndicator)
        {
            return (mainIndicator as NewHighsNewLowsVolume).VolumeRatio;
        }

        [Test]
        public void ShouldIgnoreRemovedStocks()
        {
            var indicator = (NewHighsNewLowsVolume)CreateIndicator();
            var reference = DateTime.Today;

            indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, High = 1, Low = 1, Volume = 100, Time = reference.AddMinutes(1) });
            indicator.Update(new TradeBar() { Symbol = Symbols.IBM, High = 1, Low = 1, Volume = 100, Time = reference.AddMinutes(1) });
            indicator.Update(new TradeBar() { Symbol = Symbols.GOOG, High = 1, Low = 1, Volume = 100, Time = reference.AddMinutes(1) });

            indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, High = 1, Low = 0.9m, Volume = 100, Time = reference.AddMinutes(2) });
            indicator.Update(new TradeBar() { Symbol = Symbols.IBM, High = 1, Low = 0.9m, Volume = 100, Time = reference.AddMinutes(2) });
            indicator.Update(new TradeBar() { Symbol = Symbols.GOOG, High = 1, Low = 0.9m, Volume = 100, Time = reference.AddMinutes(2) });

            // value is not ready yet
            Assert.AreEqual(0m, indicator.VolumeRatio.Current.Value);

            // new high
            indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, High = 2, Low = 0.9m, Volume = 100, Time = reference.AddMinutes(3) });
            // new low
            indicator.Update(new TradeBar() { Symbol = Symbols.IBM, High = 1, Low = 0.3m, Volume = 100, Time = reference.AddMinutes(3) });
            // new low
            indicator.Update(new TradeBar() { Symbol = Symbols.GOOG, High = 1, Low = 0.2m, Volume = 100, Time = reference.AddMinutes(3) });

            Assert.AreEqual(0.5m, indicator.VolumeRatio.Current.Value);

            indicator.Reset();
            indicator.Remove(Symbols.GOOG);

            indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, High = 1, Low = 1, Volume = 100, Time = reference.AddMinutes(1) });
            indicator.Update(new TradeBar() { Symbol = Symbols.IBM, High = 1, Low = 1, Volume = 100, Time = reference.AddMinutes(1) });
            indicator.Update(new TradeBar() { Symbol = Symbols.GOOG, High = 1, Low = 1, Volume = 100, Time = reference.AddMinutes(1) });

            indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, High = 1, Low = 0.9m, Volume = 100, Time = reference.AddMinutes(2) });
            indicator.Update(new TradeBar() { Symbol = Symbols.IBM, High = 1, Low = 0.9m, Volume = 100, Time = reference.AddMinutes(2) });
            indicator.Update(new TradeBar() { Symbol = Symbols.GOOG, High = 1, Low = 0.9m, Volume = 100, Time = reference.AddMinutes(2) });

            // value is not ready yet
            Assert.AreEqual(0m, indicator.VolumeRatio.Current.Value);

            // new high
            indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, High = 2, Low = 0.9m, Volume = 100, Time = reference.AddMinutes(3) });
            // new high
            indicator.Update(new TradeBar() { Symbol = Symbols.IBM, High = 2, Low = 0.9m, Volume = 100, Time = reference.AddMinutes(3) });
            // new low (ignored)
            indicator.Update(new TradeBar() { Symbol = Symbols.GOOG, High = 1, Low = 0.2m, Volume = 100, Time = reference.AddMinutes(3) });

            Assert.AreEqual(200m, indicator.VolumeRatio.Current.Value);
        }

        [Test]
        public void IgnorePeriodIfAnyStockMissed()
        {
            var indicator = (NewHighsNewLowsVolume)CreateIndicator();
            indicator.Add(Symbols.MSFT);
            var reference = DateTime.Today;

            indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, High = 1, Low = 1, Volume = 100, Time = reference.AddMinutes(1) });
            indicator.Update(new TradeBar() { Symbol = Symbols.IBM, High = 1, Low = 1, Volume = 100, Time = reference.AddMinutes(1) });
            indicator.Update(new TradeBar() { Symbol = Symbols.GOOG, High = 1, Low = 1, Volume = 100, Time = reference.AddMinutes(1) });
            indicator.Update(new TradeBar() { Symbol = Symbols.MSFT, High = 1, Low = 1, Volume = 100, Time = reference.AddMinutes(1) });

            indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, High = 2, Low = 1, Volume = 100, Time = reference.AddMinutes(2) });
            indicator.Update(new TradeBar() { Symbol = Symbols.IBM, High = 2, Low = 1, Volume = 100, Time = reference.AddMinutes(2) });
            indicator.Update(new TradeBar() { Symbol = Symbols.GOOG, High = 2, Low = 1, Volume = 100, Time = reference.AddMinutes(2) });
            indicator.Update(new TradeBar() { Symbol = Symbols.MSFT, High = 2, Low = 1, Volume = 100, Time = reference.AddMinutes(2) });

            // value is not ready yet
            Assert.AreEqual(0m, indicator.VolumeRatio.Current.Value);

            indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, High = 3, Low = 1, Volume = 100, Time = reference.AddMinutes(3) });
            indicator.Update(new TradeBar() { Symbol = Symbols.IBM, High = 2, Low = 0.5m, Volume = 100, Time = reference.AddMinutes(3) });
            indicator.Update(new TradeBar() { Symbol = Symbols.GOOG, Close = 3, Low = 1, Volume = 100, Time = reference.AddMinutes(3) });

            Assert.AreEqual(0m, indicator.VolumeRatio.Current.Value);

            // new high
            indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, High = 4, Low = 1, Volume = 100, Time = reference.AddMinutes(4) });
            // new low
            indicator.Update(new TradeBar() { Symbol = Symbols.IBM, High = 2, Low = 0.3m, Volume = 100, Time = reference.AddMinutes(4) });
            // no change
            indicator.Update(new TradeBar() { Symbol = Symbols.GOOG, High = 3, Low = 1, Volume = 100, Time = reference.AddMinutes(4) });
            // new high
            indicator.Update(new TradeBar() { Symbol = Symbols.MSFT, High = 4, Low = 1, Volume = 100, Time = reference.AddMinutes(4) });

            Assert.AreEqual(2m, indicator.VolumeRatio.Current.Value);

            // new high
            indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, High = 5, Low = 1, Volume = 100, Time = reference.AddMinutes(5) });
            // new low
            indicator.Update(new TradeBar() { Symbol = Symbols.IBM, High = 2, Low = 0.2m, Volume = 100, Time = reference.AddMinutes(5) });
            // new high
            indicator.Update(new TradeBar() { Symbol = Symbols.GOOG, High = 4, Low = 1, Volume = 100, Time = reference.AddMinutes(5) });
            // new high
            indicator.Update(new TradeBar() { Symbol = Symbols.MSFT, High = 5, Low = 1, Volume = 100, Time = reference.AddMinutes(5) });

            Assert.AreEqual(3m, indicator.VolumeRatio.Current.Value);

            // new high
            indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, High = 6, Low = 1, Volume = 100, Time = reference.AddMinutes(6) });
            // new high
            indicator.Update(new TradeBar() { Symbol = Symbols.IBM, High = 3, Low = 1, Volume = 100, Time = reference.AddMinutes(6) });
            // new high
            indicator.Update(new TradeBar() { Symbol = Symbols.MSFT, High = 6, Low = 1, Volume = 100, Time = reference.AddMinutes(6) });

            Assert.AreEqual(3m, indicator.VolumeRatio.Current.Value);

            // new high
            indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, High = 7, Low = 1, Volume = 100, Time = reference.AddMinutes(7) });
            // new high
            indicator.Update(new TradeBar() { Symbol = Symbols.GOOG, High = 5, Low = 1, Volume = 100, Time = reference.AddMinutes(7) });
            // new high
            indicator.Update(new TradeBar() { Symbol = Symbols.MSFT, High = 7, Low = 1, Volume = 100, Time = reference.AddMinutes(7) });

            Assert.AreEqual(3m, indicator.VolumeRatio.Current.Value);
        }

        [Test]
        public override void WarmsUpProperly()
        {
            var indicator = CreateIndicator() as NewHighsNewLowsVolume;
            var reference = DateTime.Today;

            // setup period (unordered)
            indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, High = 1, Low = 1, Volume = 100, Time = reference.AddMinutes(1) });
            indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, High = 2, Low = 1, Volume = 100, Time = reference.AddMinutes(2) });

            indicator.Update(new TradeBar() { Symbol = Symbols.IBM, High = 1, Low = 1, Volume = 100, Time = reference.AddMinutes(1) });
            indicator.Update(new TradeBar() { Symbol = Symbols.IBM, High = 0.5m, Low = 0.2m, Volume = 100, Time = reference.AddMinutes(2) });

            indicator.Update(new TradeBar() { Symbol = Symbols.GOOG, High = 1, Low = 1, Volume = 100, Time = reference.AddMinutes(1) });
            indicator.Update(new TradeBar() { Symbol = Symbols.GOOG, High = 3, Low = 1, Volume = 100, Time = reference.AddMinutes(2) });

            // new high
            indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, High = 3, Low = 1, Volume = 100, Time = reference.AddMinutes(3) });
            // new low
            indicator.Update(new TradeBar() { Symbol = Symbols.IBM, High = 0.75m, Low = 0.1m, Volume = 100, Time = reference.AddMinutes(3) });
            // new high
            indicator.Update(new TradeBar() { Symbol = Symbols.GOOG, High = 5, Low = 2, Volume = 100, Time = reference.AddMinutes(3) });

            Assert.IsTrue(indicator.IsReady);
            Assert.AreEqual(2m, indicator.VolumeRatio.Current.Value);
            Assert.AreEqual(9, indicator.Samples);
            Assert.AreEqual(1, indicator.VolumeRatio.Samples);
        }

        [Test]
        public void WarmsUpOrdered()
        {
            var indicator = CreateIndicator() as NewHighsNewLowsVolume;
            var reference = DateTime.Today;

            // setup period (ordered)
            indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, High = 1, Low = 1, Volume = 100, Time = reference.AddMinutes(1) });
            indicator.Update(new TradeBar() { Symbol = Symbols.IBM, High = 1, Low = 1, Volume = 100, Time = reference.AddMinutes(1) });
            indicator.Update(new TradeBar() { Symbol = Symbols.GOOG, High = 1, Low = 1, Volume = 100, Time = reference.AddMinutes(1) });

            indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, High = 2, Low = 1, Volume = 100, Time = reference.AddMinutes(2) });
            indicator.Update(new TradeBar() { Symbol = Symbols.IBM, High = 0.5m, Low = 1, Volume = 100, Time = reference.AddMinutes(2) });
            indicator.Update(new TradeBar() { Symbol = Symbols.GOOG, High = 3, Low = 1, Volume = 100, Time = reference.AddMinutes(2) });

            // indicator is not ready yet
            Assert.IsFalse(indicator.IsReady);

            // new high
            indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, High = 3, Low = 1, Volume = 100, Time = reference.AddMinutes(3) });
            // new high
            indicator.Update(new TradeBar() { Symbol = Symbols.IBM, High = 4, Low = 1, Volume = 100, Time = reference.AddMinutes(3) });
            // new high
            indicator.Update(new TradeBar() { Symbol = Symbols.GOOG, High = 5, Low = 1, Volume = 100, Time = reference.AddMinutes(3) });

            Assert.IsTrue(indicator.IsReady);
            Assert.AreEqual(300m, indicator.VolumeRatio.Current.Value);
        }

        [Test]
        public override void IndicatorShouldHaveSymbolAfterUpdates()
        {
            var indicator = CreateIndicator() as NewHighsNewLowsVolume;
            var reference = DateTime.Today;

            for (int i = 0; i < 10; i++)
            {
                indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, High = 1, Low = 1, Volume = 1, Time = reference.AddMinutes(1) });
                indicator.Update(new TradeBar() { Symbol = Symbols.IBM, High = 1, Low = 1, Volume = 1, Time = reference.AddMinutes(1) });
                indicator.Update(new TradeBar() { Symbol = Symbols.GOOG, High = 1, Low = 1, Volume = 1, Time = reference.AddMinutes(1) });

                indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, High = 2, Low = 1, Volume = 1, Time = reference.AddMinutes(2) });
                indicator.Update(new TradeBar() { Symbol = Symbols.IBM, High = 1, Low = 0.5m, Volume = 1, Time = reference.AddMinutes(2) });
                indicator.Update(new TradeBar() { Symbol = Symbols.GOOG, High = 3, Low = 1, Volume = 1, Time = reference.AddMinutes(2) });

                // indicator is not ready yet

                indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, High = 3, Low = 1, Volume = 1, Time = reference.AddMinutes(3) });
                indicator.Update(new TradeBar() { Symbol = Symbols.IBM, High = 1, Low = 0.3m, Volume = 1, Time = reference.AddMinutes(3) });
                indicator.Update(new TradeBar() { Symbol = Symbols.GOOG, High = 4, Low = 1, Volume = 1, Time = reference.AddMinutes(3) });

                // indicator is ready

                // The last update used Symbol.GOOG, so the indicator's current Symbol should be GOOG
                Assert.AreEqual(Symbols.GOOG, indicator.VolumeRatio.Current.Symbol);
            }
        }

        protected override string TestFileName => "nhnl_data.csv";

        protected override string TestColumnName => "NH/NL Volume Ratio";

        /// <summary>
        /// The final value of this indicator is zero because it uses the Volume of the bars it receives.
        /// Since RenkoBar's don't always have Volume, the final current value is zero. Therefore we
        /// skip this test
        /// </summary>
        /// <param name="indicator"></param>
        protected override void IndicatorValueIsNotZeroAfterReceiveRenkoBars(IndicatorBase indicator)
        {
        }
    }
}
