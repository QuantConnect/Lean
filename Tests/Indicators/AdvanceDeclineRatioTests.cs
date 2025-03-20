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
using System.Linq;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    public class AdvanceDeclineRatioTests : AdvanceDeclineDifferenceTests
    {
        protected override IndicatorBase<TradeBar> CreateIndicator()
        {
            var adr = new AdvanceDeclineRatio("test_name");
            if (SymbolList.Count > 2)
            {
                SymbolList.Take(3).ToList().ForEach(adr.AddStock);
            }
            else
            {
                adr.Add(Symbols.AAPL);
                adr.Add(Symbols.IBM);
                adr.Add(Symbols.GOOG);
                RenkoBarSize = 5000000;
            }
            return adr;
        }

        [Test]
        public override void ShouldIgnoreRemovedStocks()
        {
            var adr = (AdvanceDeclineRatio)CreateIndicator();
            var reference = System.DateTime.Today;

            adr.Update(new TradeBar() { Symbol = Symbols.AAPL, Close = 1, Volume = 100, Time = reference.AddMinutes(1) });
            adr.Update(new TradeBar() { Symbol = Symbols.IBM, Close = 1, Volume = 100, Time = reference.AddMinutes(1) });
            adr.Update(new TradeBar() { Symbol = Symbols.GOOG, Close = 1, Volume = 100, Time = reference.AddMinutes(1) });

            // value is not ready yet
            Assert.AreEqual(0m, adr.Current.Value);

            adr.Update(new TradeBar() { Symbol = Symbols.AAPL, Close = 2, Volume = 100, Time = reference.AddMinutes(2) });
            adr.Update(new TradeBar() { Symbol = Symbols.IBM, Close = 0.5m, Volume = 100, Time = reference.AddMinutes(2) });
            adr.Update(new TradeBar() { Symbol = Symbols.GOOG, Close = 3, Volume = 100, Time = reference.AddMinutes(2) });

            Assert.AreEqual(2m, adr.Current.Value);
            adr.Reset();
            adr.Remove(Symbols.GOOG);

            adr.Update(new TradeBar() { Symbol = Symbols.AAPL, Close = 1, Volume = 100, Time = reference.AddMinutes(1) });
            adr.Update(new TradeBar() { Symbol = Symbols.IBM, Close = 1, Volume = 100, Time = reference.AddMinutes(1) });
            adr.Update(new TradeBar() { Symbol = Symbols.GOOG, Close = 1, Volume = 100, Time = reference.AddMinutes(1) });

            // value is not ready yet
            Assert.AreEqual(0m, adr.Current.Value);

            adr.Update(new TradeBar() { Symbol = Symbols.AAPL, Close = 2, Volume = 100, Time = reference.AddMinutes(2) });
            adr.Update(new TradeBar() { Symbol = Symbols.IBM, Close = 0.5m, Volume = 100, Time = reference.AddMinutes(2) });
            adr.Update(new TradeBar() { Symbol = Symbols.GOOG, Close = 3, Volume = 100, Time = reference.AddMinutes(2) });

            Assert.AreEqual(1m, adr.Current.Value);
        }

        [Test]
        public override void IgnorePeriodIfAnyStockMissed()
        {
            var adr = (AdvanceDeclineRatio)CreateIndicator();
            adr.Add(Symbols.MSFT);
            var reference = System.DateTime.Today;

            adr.Update(new TradeBar() { Symbol = Symbols.AAPL, Close = 1, Time = reference.AddMinutes(1) });
            adr.Update(new TradeBar() { Symbol = Symbols.IBM, Close = 1, Time = reference.AddMinutes(1) });
            adr.Update(new TradeBar() { Symbol = Symbols.GOOG, Close = 1, Time = reference.AddMinutes(1) });
            adr.Update(new TradeBar() { Symbol = Symbols.MSFT, Close = 1, Time = reference.AddMinutes(1) });

            // value is not ready yet
            Assert.AreEqual(0m, adr.Current.Value);

            adr.Update(new TradeBar() { Symbol = Symbols.AAPL, Close = 2, Time = reference.AddMinutes(2) });
            adr.Update(new TradeBar() { Symbol = Symbols.IBM, Close = 0.5m, Time = reference.AddMinutes(2) });
            adr.Update(new TradeBar() { Symbol = Symbols.GOOG, Close = 3, Time = reference.AddMinutes(2) });

            Assert.AreEqual(0m, adr.Current.Value);

            adr.Update(new TradeBar() { Symbol = Symbols.AAPL, Close = 3, Time = reference.AddMinutes(3) });
            adr.Update(new TradeBar() { Symbol = Symbols.IBM, Close = 1, Time = reference.AddMinutes(3) });
            adr.Update(new TradeBar() { Symbol = Symbols.GOOG, Close = 2, Time = reference.AddMinutes(3) });
            adr.Update(new TradeBar() { Symbol = Symbols.MSFT, Close = 1, Time = reference.AddMinutes(3) });

            Assert.AreEqual(2m, adr.Current.Value);

            adr.Update(new TradeBar() { Symbol = Symbols.AAPL, Close = 1, Time = reference.AddMinutes(4) });
            adr.Update(new TradeBar() { Symbol = Symbols.IBM, Close = 2, Time = reference.AddMinutes(4) });
            adr.Update(new TradeBar() { Symbol = Symbols.GOOG, Close = 1, Time = reference.AddMinutes(4) });
            adr.Update(new TradeBar() { Symbol = Symbols.MSFT, Close = 2, Time = reference.AddMinutes(4) });

            Assert.AreEqual(1m, adr.Current.Value);

            adr.Update(new TradeBar() { Symbol = Symbols.AAPL, Close = 2, Time = reference.AddMinutes(5) });
            adr.Update(new TradeBar() { Symbol = Symbols.IBM, Close = 3, Time = reference.AddMinutes(5) });
            adr.Update(new TradeBar() { Symbol = Symbols.MSFT, Close = 1, Time = reference.AddMinutes(5) });

            Assert.AreEqual(1m, adr.Current.Value);

            adr.Update(new TradeBar() { Symbol = Symbols.AAPL, Close = 2, Time = reference.AddMinutes(6) });
            adr.Update(new TradeBar() { Symbol = Symbols.GOOG, Close = 4, Time = reference.AddMinutes(6) });
            adr.Update(new TradeBar() { Symbol = Symbols.MSFT, Close = 5, Time = reference.AddMinutes(6) });

            Assert.AreEqual(1m, adr.Current.Value);
        }

        [Test]
        public override void WarmsUpProperly()
        {
            var indicator = CreateIndicator();
            var reference = System.DateTime.Today;

            indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, Close = 1, Volume = 1, Time = reference.AddMinutes(1) });
            indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, Close = 2, Volume = 1, Time = reference.AddMinutes(2) });

            indicator.Update(new TradeBar() { Symbol = Symbols.IBM, Close = 1, Volume = 1, Time = reference.AddMinutes(1) });
            indicator.Update(new TradeBar() { Symbol = Symbols.IBM, Close = 0.5m, Volume = 1, Time = reference.AddMinutes(2) });

            indicator.Update(new TradeBar() { Symbol = Symbols.GOOG, Close = 1, Volume = 1, Time = reference.AddMinutes(1) });
            indicator.Update(new TradeBar() { Symbol = Symbols.GOOG, Close = 3, Volume = 1, Time = reference.AddMinutes(2) });

            Assert.IsTrue(indicator.IsReady);
            Assert.AreEqual(2m, indicator.Current.Value);
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

            indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, Close = 2, Volume = 1, Time = reference.AddMinutes(2) });
            indicator.Update(new TradeBar() { Symbol = Symbols.IBM, Close = 0.5m, Volume = 1, Time = reference.AddMinutes(2) });
            indicator.Update(new TradeBar() { Symbol = Symbols.GOOG, Close = 3, Volume = 1, Time = reference.AddMinutes(2) });

            Assert.IsTrue(indicator.IsReady);
            Assert.AreEqual(2m, indicator.Current.Value);
        }

        protected override string TestFileName => "arms_data.txt";

        protected override string TestColumnName => "A/D Ratio";
    }
}