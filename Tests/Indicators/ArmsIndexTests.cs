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
using System.Linq;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    public class ArmsIndexTests : AdvanceDeclineDifferenceTests
    {
        protected override IndicatorBase<TradeBar> CreateIndicator()
        {
            var indicator = new ArmsIndex("test_name");
            if (SymbolList.Count > 2)
            {
                SymbolList.Take(3).ToList().ForEach(indicator.AddStock);
            }
            else
            {
                indicator.Add(Symbols.AAPL);
                indicator.Add(Symbols.IBM);
                indicator.Add(Symbols.GOOG);
                RenkoBarSize = 5000000;
            }
            return indicator;
        }

        [Test]
        public override void ShouldIgnoreRemovedStocks()
        {
            var trin = (ArmsIndex)CreateIndicator();
            var reference = System.DateTime.Today;

            trin.Update(new TradeBar() { Symbol = Symbols.AAPL, Close = 1, Volume = 100, Time = reference.AddMinutes(1) });
            trin.Update(new TradeBar() { Symbol = Symbols.IBM, Close = 1, Volume = 100, Time = reference.AddMinutes(1) });
            trin.Update(new TradeBar() { Symbol = Symbols.GOOG, Close = 1, Volume = 100, Time = reference.AddMinutes(1) });

            // value is not ready yet
            Assert.AreEqual(0m, trin.Current.Value);

            trin.Update(new TradeBar() { Symbol = Symbols.AAPL, Close = 2, Volume = 100, Time = reference.AddMinutes(2) });
            trin.Update(new TradeBar() { Symbol = Symbols.IBM, Close = 0.5m, Volume = 100, Time = reference.AddMinutes(2) });
            trin.Update(new TradeBar() { Symbol = Symbols.GOOG, Close = 3, Volume = 100, Time = reference.AddMinutes(2) });

            Assert.AreEqual(1m, trin.Current.Value);
            trin.Reset();
            trin.Remove(Symbols.IBM);

            trin.Update(new TradeBar() { Symbol = Symbols.AAPL, Close = 1, Volume = 100, Time = reference.AddMinutes(1) });
            trin.Update(new TradeBar() { Symbol = Symbols.IBM, Close = 1, Volume = 100, Time = reference.AddMinutes(1) });
            trin.Update(new TradeBar() { Symbol = Symbols.GOOG, Close = 1, Volume = 100, Time = reference.AddMinutes(1) });

            // value is not ready yet
            Assert.AreEqual(0m, trin.Current.Value);

            trin.Update(new TradeBar() { Symbol = Symbols.AAPL, Close = 2, Volume = 100, Time = reference.AddMinutes(2) });
            trin.Update(new TradeBar() { Symbol = Symbols.IBM, Close = 0.5m, Volume = 100, Time = reference.AddMinutes(2) });
            trin.Update(new TradeBar() { Symbol = Symbols.GOOG, Close = 3, Volume = 100, Time = reference.AddMinutes(2) });

            Assert.AreNotEqual(1m, trin.Current.Value);
        }

        [Test]
        public override void IgnorePeriodIfAnyStockMissed()
        {
            var adr = (ArmsIndex)CreateIndicator();
            adr.Add(Symbols.MSFT);
            var reference = System.DateTime.Today;

            adr.Update(new TradeBar() { Symbol = Symbols.AAPL, Close = 1, Volume = 100, Time = reference.AddMinutes(1) });
            adr.Update(new TradeBar() { Symbol = Symbols.IBM, Close = 1, Volume = 100, Time = reference.AddMinutes(1) });
            adr.Update(new TradeBar() { Symbol = Symbols.GOOG, Close = 1, Volume = 100, Time = reference.AddMinutes(1) });
            adr.Update(new TradeBar() { Symbol = Symbols.MSFT, Close = 1, Volume = 100, Time = reference.AddMinutes(1) });

            // value is not ready yet
            Assert.AreEqual(0m, adr.Current.Value);

            adr.Update(new TradeBar() { Symbol = Symbols.AAPL, Close = 2, Volume = 100, Time = reference.AddMinutes(2) });
            adr.Update(new TradeBar() { Symbol = Symbols.IBM, Close = 0.5m, Volume = 100, Time = reference.AddMinutes(2) });
            adr.Update(new TradeBar() { Symbol = Symbols.GOOG, Close = 3, Volume = 100, Time = reference.AddMinutes(2) });

            Assert.AreEqual(0m, adr.Current.Value);

            adr.Update(new TradeBar() { Symbol = Symbols.AAPL, Close = 3, Volume = 100, Time = reference.AddMinutes(3) });
            adr.Update(new TradeBar() { Symbol = Symbols.IBM, Close = 1, Volume = 100, Time = reference.AddMinutes(3) });
            adr.Update(new TradeBar() { Symbol = Symbols.GOOG, Close = 1, Volume = 200, Time = reference.AddMinutes(3) });
            adr.Update(new TradeBar() { Symbol = Symbols.MSFT, Close = 1, Volume = 100, Time = reference.AddMinutes(3) });

            Assert.AreEqual(2m, adr.Current.Value);

            adr.Update(new TradeBar() { Symbol = Symbols.AAPL, Close = 1, Volume = 100, Time = reference.AddMinutes(4) });
            adr.Update(new TradeBar() { Symbol = Symbols.IBM, Close = 2, Volume = 250, Time = reference.AddMinutes(4) });
            adr.Update(new TradeBar() { Symbol = Symbols.GOOG, Close = 2, Volume = 100, Time = reference.AddMinutes(4) });
            adr.Update(new TradeBar() { Symbol = Symbols.MSFT, Close = 2, Volume = 150, Time = reference.AddMinutes(4) });

            Assert.AreEqual(3m / 5m, adr.Current.Value);

            adr.Update(new TradeBar() { Symbol = Symbols.AAPL, Close = 2, Volume = 140, Time = reference.AddMinutes(5) });
            adr.Update(new TradeBar() { Symbol = Symbols.GOOG, Close = 5, Volume = 110, Time = reference.AddMinutes(5) });
            adr.Update(new TradeBar() { Symbol = Symbols.MSFT, Close = 1, Volume = 150, Time = reference.AddMinutes(5) });

            Assert.AreEqual(3m / 5m, adr.Current.Value);

            adr.Update(new TradeBar() { Symbol = Symbols.AAPL, Close = 2, Volume = 120, Time = reference.AddMinutes(6) });
            adr.Update(new TradeBar() { Symbol = Symbols.IBM, Close = 3, Volume = 350, Time = reference.AddMinutes(6) });
            adr.Update(new TradeBar() { Symbol = Symbols.GOOG, Close = 4, Volume = 200, Time = reference.AddMinutes(6) });

            Assert.AreEqual(3m / 5m, adr.Current.Value);
        }

        [Test]
        public override void WarmsUpProperly()
        {
            var indicator = CreateIndicator();
            var reference = System.DateTime.Today;

            indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, Close = 1, Volume = 1, Time = reference.AddMinutes(1) });
            indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, Close = 2, Volume = 60, Time = reference.AddMinutes(2) });

            indicator.Update(new TradeBar() { Symbol = Symbols.IBM, Close = 1, Volume = 1, Time = reference.AddMinutes(1) });
            indicator.Update(new TradeBar() { Symbol = Symbols.IBM, Close = 0.5m, Volume = 10, Time = reference.AddMinutes(2) });

            indicator.Update(new TradeBar() { Symbol = Symbols.GOOG, Close = 1, Volume = 1, Time = reference.AddMinutes(1) });
            indicator.Update(new TradeBar() { Symbol = Symbols.GOOG, Close = 3, Volume = 40, Time = reference.AddMinutes(2) });

            Assert.IsTrue(indicator.IsReady);
            Assert.AreEqual(0.2m, indicator.Current.Value);
            Assert.AreEqual(6, indicator.Samples);
        }

        [Test]
        public override void WarmsUpOrdered()
        {
            var indicator = CreateIndicator();
            var reference = System.DateTime.Today;

            indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, Close = 1, Volume = 1, Time = reference.AddMinutes(1) });
            indicator.Update(new TradeBar() { Symbol = Symbols.IBM, Close = 1, Volume = 1, Time = reference.AddMinutes(1) });
            indicator.Update(new TradeBar() { Symbol = Symbols.GOOG, Close = 1, Volume = 1, Time = reference.AddMinutes(1) });

            // indicator is not ready yet
            Assert.IsFalse(indicator.IsReady);

            indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, Close = 2, Volume = 60, Time = reference.AddMinutes(2) });
            indicator.Update(new TradeBar() { Symbol = Symbols.IBM, Close = 0.5m, Volume = 10, Time = reference.AddMinutes(2) });
            indicator.Update(new TradeBar() { Symbol = Symbols.GOOG, Close = 3, Volume = 40, Time = reference.AddMinutes(2) });

            Assert.IsTrue(indicator.IsReady);
            Assert.AreEqual(0.2m, indicator.Current.Value);
        }

        [Test]
        public override void TimeMovesForward()
        {
            var indicator = CreateIndicator();
            var period = (indicator as IIndicatorWarmUpPeriodProvider)?.WarmUpPeriod;

            if (!period.HasValue)
            {
                Assert.Ignore($"{indicator.Name} is not IIndicatorWarmUpPeriodProvider");
                return;
            }

            var startDate = new DateTime(2019, 1, 1);

            for (var i = 10; i > 0; i--)
            {
                var input = GetInput(Symbols.AAPL, startDate, i);
                indicator.Update(input);
            }

            for (var i = 10; i > 0; i--)
            {
                var input = GetInput(Symbols.IBM, startDate, i);
                indicator.Update(input);
            }

            for (var i = 10; i > 0; i--)
            {
                var input = GetInput(Symbols.GOOG, startDate, i);
                indicator.Update(input);
            }

            Assert.AreEqual(3, indicator.Samples);

            indicator.Reset();

            for (var i = 0; i < 10; i++)
            {
                var input = GetInput(Symbols.AAPL, startDate, i);
                indicator.Update(input);
            }

            for (var i = 0; i < 10; i++)
            {
                var input = GetInput(Symbols.IBM, startDate, i);
                indicator.Update(input);
            }

            for (var i = 0; i < 10; i++)
            {
                var input = GetInput(Symbols.GOOG, startDate, i);
                indicator.Update(input);
            }

            Assert.AreEqual(30, indicator.Samples);
        }

        protected override string TestFileName => "arms_data.txt";

        protected override string TestColumnName => "TRIN";

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
