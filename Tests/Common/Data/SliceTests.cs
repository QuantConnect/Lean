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
using System.Collections.Generic;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Custom;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;

namespace QuantConnect.Tests.Common.Data
{
    [TestFixture]
    public class SliceTests
    {
        [Test]
        public void AccessesBaseBySymbol()
        {
            IndicatorDataPoint tick = new IndicatorDataPoint(Symbols.SPY, DateTime.Now, 1);
            Slice slice = new Slice(DateTime.Now, new[] { tick });

            IndicatorDataPoint data = slice[tick.Symbol];

            Assert.AreEqual(tick, data);
        }

        [Test]
        public void AccessesTradeBarBySymbol()
        {
            TradeBar tradeBar = new TradeBar {Symbol = Symbols.SPY, Time = DateTime.Now};
            Slice slice = new Slice(DateTime.Now, new[] { tradeBar });

            TradeBar data = slice[tradeBar.Symbol];

            Assert.AreEqual(tradeBar, data);
        }

        [Test]
        public void AccessesTradeBarCollection()
        {
            TradeBar tradeBar1 = new TradeBar { Symbol = Symbols.SPY, Time = DateTime.Now };
            TradeBar tradeBar2 = new TradeBar { Symbol = Symbols.AAPL, Time = DateTime.Now };
            Slice slice = new Slice(DateTime.Now, new[] { tradeBar1, tradeBar2 });

            TradeBars tradeBars = slice.Bars;
            Assert.AreEqual(2, tradeBars.Count);
        }

        [Test]
        public void AccessesTicksBySymbol()
        {
            Tick tick1 = new Tick(DateTime.Now, Symbols.SPY, 1, 2);
            Tick tick2 = new Tick(DateTime.Now, Symbols.SPY, 1.1m, 2.1m);
            Slice slice = new Slice(DateTime.Now, new[] { tick1, tick2 });

            List<Tick> data = slice[tick1.Symbol];
            Assert.IsInstanceOf(typeof(List<Tick>), data);
            Assert.AreEqual(2, data.Count);
        }

        [Test]
        public void AccessesTicksCollection()
        {
            Tick tick1 = new Tick(DateTime.Now, Symbols.SPY, 1, 2);
            Tick tick2 = new Tick(DateTime.Now, Symbols.SPY, 1.1m, 2.1m);
            Tick tick3 = new Tick(DateTime.Now, Symbols.AAPL, 1, 2);
            Tick tick4 = new Tick(DateTime.Now, Symbols.AAPL, 1.1m, 2.1m);
            Slice slice = new Slice(DateTime.Now, new[] { tick1, tick2, tick3, tick4 });

            Ticks ticks = slice.Ticks;
            Assert.AreEqual(2, ticks.Count);
            Assert.AreEqual(2, ticks[Symbols.SPY].Count);
            Assert.AreEqual(2, ticks[Symbols.AAPL].Count);
        }

        [Test]
        public void AccessesCustomGenericallyByType()
        {
            Quandl quandlSpy = new Quandl { Symbol = Symbols.SPY, Time = DateTime.Now };
            Quandl quandlAapl = new Quandl { Symbol = Symbols.AAPL, Time = DateTime.Now };
            Slice slice = new Slice(DateTime.Now, new[] { quandlSpy, quandlAapl });

            DataDictionary<Quandl> quandlData = slice.Get<Quandl>();
            Assert.AreEqual(2, quandlData.Count);
        }

        [Test]
        public void AccessesTickGenericallyByType()
        {
            Tick TickSpy = new Tick { Symbol = Symbols.SPY, Time = DateTime.Now };
            Tick TickAapl = new Tick { Symbol = Symbols.AAPL, Time = DateTime.Now };
            Slice slice = new Slice(DateTime.Now, new[] { TickSpy, TickAapl });

            DataDictionary<Tick> TickData = slice.Get<Tick>();
            Assert.AreEqual(2, TickData.Count);
        }


        [Test]
        public void AccessesTradeBarGenericallyByType()
        {
            TradeBar TradeBarSpy = new TradeBar { Symbol = Symbols.SPY, Time = DateTime.Now };
            TradeBar TradeBarAapl = new TradeBar { Symbol = Symbols.AAPL, Time = DateTime.Now };
            Slice slice = new Slice(DateTime.Now, new[] { TradeBarSpy, TradeBarAapl });

            DataDictionary<TradeBar> TradeBarData = slice.Get<TradeBar>();
            Assert.AreEqual(2, TradeBarData.Count);
        }

        [Test]
        public void AccessesGenericallyByTypeAndSymbol()
        {
            Quandl quandlSpy = new Quandl { Symbol = Symbols.SPY, Time = DateTime.Now };
            Quandl quandlAapl = new Quandl { Symbol = Symbols.AAPL, Time = DateTime.Now };
            Slice slice = new Slice(DateTime.Now, new[] { quandlSpy, quandlAapl });

            Quandl quandlData = slice.Get<Quandl>(Symbols.SPY);
            Assert.AreEqual(quandlSpy, quandlData);
        }
    }
}
