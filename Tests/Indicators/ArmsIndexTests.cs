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
using Oanda.RestV20.Model;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    public class ArmsIndexTests : CommonIndicatorTests<TradeBar>
    {
        protected override IndicatorBase<TradeBar> CreateIndicator()
        {
            var indicator = new ArmsIndex("test_name");
            indicator.AddStock(Symbols.AAPL);
            indicator.AddStock(Symbols.IBM);
            indicator.AddStock(Symbols.GOOG);
            return indicator;
        }

        [Test]
        public virtual void ShouldIgnoreRemovedStocks()
        {
            var trin = (ArmsIndex) CreateIndicator();
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
            trin.RemoveStock(Symbols.IBM);

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
        public virtual void IgnorePeriodIfAnyStockMissed()
        {
            var adr = (ArmsIndex)CreateIndicator();
            adr.AddStock(Symbols.MSFT);
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

            Assert.AreEqual(3m/5m, adr.Current.Value);

            adr.Update(new TradeBar() { Symbol = Symbols.AAPL, Close = 2, Volume = 140, Time = reference.AddMinutes(5) });
            adr.Update(new TradeBar() { Symbol = Symbols.GOOG, Close = 5, Volume = 110, Time = reference.AddMinutes(5) });
            adr.Update(new TradeBar() { Symbol = Symbols.MSFT, Close = 1, Volume = 150, Time = reference.AddMinutes(5) });

            Assert.AreEqual(3m / 5m, adr.Current.Value);

            adr.Update(new TradeBar() { Symbol = Symbols.AAPL, Close = 2, Volume = 120, Time = reference.AddMinutes(6) });
            adr.Update(new TradeBar() { Symbol = Symbols.IBM, Close = 3, Volume = 350, Time = reference.AddMinutes(6) });
            adr.Update(new TradeBar() { Symbol = Symbols.GOOG, Close = 4, Volume = 200, Time = reference.AddMinutes(6) });

            Assert.AreEqual(3m / 5m, adr.Current.Value);
        }


        protected override string TestFileName => "arms_data.txt";

        protected override string TestColumnName => "TRIN";
    }
}