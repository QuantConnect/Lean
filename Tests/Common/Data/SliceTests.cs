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
using System.Linq;
using NUnit.Framework;
using Python.Runtime;
using QuantConnect.Data;
using QuantConnect.Data.Custom;
using QuantConnect.Data.Custom.Tiingo;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Python;

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

        [Test]
        public void PythonGetCustomData()
        {
            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                @"
from clr import AddReference
AddReference(""QuantConnect.Common"")
from QuantConnect import *
from QuantConnect.Data.Custom import *

def Test(slice):
    data = slice.Get(Quandl)
    return data").GetAttr("Test");
                var quandlSpy = new Quandl { Symbol = Symbols.SPY, Time = DateTime.Now, Value = 10};
                var quandlAapl = new Quandl { Symbol = Symbols.AAPL, Time = DateTime.Now, Value = 11 };
                var slice = new Slice(DateTime.Now, new[] { quandlSpy, quandlAapl });

                var data = test(new PythonSlice(slice));
                Assert.AreEqual(2, (int)data.Count);
                Assert.AreEqual(10, (int)data[Symbols.SPY].Value);
                Assert.AreEqual(11, (int)data[Symbols.AAPL].Value);
            }
        }

        [Test]
        public void PythonGetBySymbolCustomData()
        {
            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    @"
from clr import AddReference
AddReference(""QuantConnect.Common"")
from QuantConnect import *
from QuantConnect.Tests import *
from QuantConnect.Data.Custom import *

def Test(slice):
    data = slice.Get(Quandl)
    value = data[Symbols.AAPL].Value
    if value != 11:
        raise Exception('Unexpected value')").GetAttr("Test");
                var quandlSpy = new Quandl { Symbol = Symbols.SPY, Time = DateTime.Now, Value = 10 };
                var quandlAapl = new Quandl { Symbol = Symbols.AAPL, Time = DateTime.Now, Value = 11 };
                var slice = new Slice(DateTime.Now, new[] { quandlSpy, quandlAapl });

                Assert.DoesNotThrow(() => test(new PythonSlice(slice)));
            }
        }

        [Test]
        public void PythonGetAndSymbolCustomData()
        {
            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    @"
from clr import AddReference
AddReference(""QuantConnect.Common"")
from QuantConnect import *
from QuantConnect.Tests import *
from QuantConnect.Data.Custom import *

def Test(slice):
    data = slice.Get(Quandl, Symbols.AAPL)
    value = data.Value
    if value != 11:
        raise Exception('Unexpected value')").GetAttr("Test");
                var quandlSpy = new Quandl { Symbol = Symbols.SPY, Time = DateTime.Now, Value = 10 };
                var quandlAapl = new Quandl { Symbol = Symbols.AAPL, Time = DateTime.Now, Value = 11 };
                var slice = new Slice(DateTime.Now, new[] { quandlSpy, quandlAapl });

                Assert.DoesNotThrow(() => test(new PythonSlice(slice)));
            }
        }

        [Test]
        public void PythonGetTradeBar()
        {
            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    @"
from clr import AddReference
AddReference(""QuantConnect.Common"")
from QuantConnect import *
from QuantConnect.Data.Market import *

def Test(slice):
    data = slice.Get(TradeBar)
    return data").GetAttr("Test");
                var TradeBarSpy = new TradeBar { Symbol = Symbols.SPY, Time = DateTime.Now, Value = 8 };
                var TradeBarAapl = new TradeBar { Symbol = Symbols.AAPL, Time = DateTime.Now, Value = 9 };
                var quandlSpy = new Quandl { Symbol = Symbols.SPY, Time = DateTime.Now, Value = 10 };
                var quandlAapl = new Quandl { Symbol = Symbols.AAPL, Time = DateTime.Now, Value = 11 };
                var slice = new Slice(DateTime.Now, new BaseData[] { quandlSpy, TradeBarAapl, quandlAapl, TradeBarSpy });

                var data = test(new PythonSlice(slice));
                Assert.AreEqual(2, (int)data.Count);
                Assert.AreEqual(8, (int)data[Symbols.SPY].Value);
                Assert.AreEqual(9, (int)data[Symbols.AAPL].Value);
            }
        }

        [Test]
        public void PythonGetBySymbolTradeBar()
        {
            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    @"
from clr import AddReference
AddReference(""QuantConnect.Common"")
from QuantConnect import *
from QuantConnect.Tests import *
from QuantConnect.Data.Market import *

def Test(slice):
    data = slice.Get(TradeBar)
    value = data[Symbols.AAPL].Value
    if value != 9:
        raise Exception('Unexpected value')").GetAttr("Test");
                var TradeBarSpy = new TradeBar { Symbol = Symbols.SPY, Time = DateTime.Now, Value = 8 };
                var TradeBarAapl = new TradeBar { Symbol = Symbols.AAPL, Time = DateTime.Now, Value = 9 };
                var quandlSpy = new Quandl { Symbol = Symbols.SPY, Time = DateTime.Now, Value = 10 };
                var quandlAapl = new Quandl { Symbol = Symbols.AAPL, Time = DateTime.Now, Value = 11 };
                var slice = new Slice(DateTime.Now, new BaseData[] { quandlSpy, TradeBarAapl, quandlAapl, TradeBarSpy });

                Assert.DoesNotThrow(() => test(new PythonSlice(slice)));
            }
        }

        [Test]
        public void PythonGetAndSymbolTradeBar()
        {
            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    @"
from clr import AddReference
AddReference(""QuantConnect.Common"")
from QuantConnect import *
from QuantConnect.Tests import *
from QuantConnect.Data.Market import *

def Test(slice):
    data = slice.Get(TradeBar, Symbols.AAPL)
    value = data.Value
    if value != 9:
        raise Exception('Unexpected value')").GetAttr("Test");
                var TradeBarSpy = new TradeBar { Symbol = Symbols.SPY, Time = DateTime.Now, Value = 8 };
                var TradeBarAapl = new TradeBar { Symbol = Symbols.AAPL, Time = DateTime.Now, Value = 9 };
                var quandlSpy = new Quandl { Symbol = Symbols.SPY, Time = DateTime.Now, Value = 10 };
                var quandlAapl = new Quandl { Symbol = Symbols.AAPL, Time = DateTime.Now, Value = 11 };
                var slice = new Slice(DateTime.Now, new BaseData[] { quandlSpy, TradeBarAapl, quandlAapl, TradeBarSpy });

                Assert.DoesNotThrow(() => test(new PythonSlice(slice)));
            }
        }

        [Test]
        public void PythonGetCustomData_Iterate_Tiingo()
        {
            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    @"
from clr import AddReference
AddReference(""QuantConnect.Common"")
from QuantConnect import *
from QuantConnect.Data.Custom.Tiingo import TiingoNews
from QuantConnect.Logging import *

def Test(slice):
    data = slice.Get(TiingoNews)
    count = 0
    for singleData in data:
        Log.Trace(str(singleData))
        count += 1
    if count != 2:
        raise Exception('Unexpected value')").GetAttr("Test");
                var quandlSpy = new TiingoNews { Symbol = Symbols.SPY, Time = DateTime.Now, Value = 10 };
                var tradeBarAapl = new TradeBar { Symbol = Symbols.AAPL, Time = DateTime.Now, Value = 9 };
                var quandlAapl = new TiingoNews { Symbol = Symbols.AAPL, Time = DateTime.Now, Value = 11 };
                var slice = new Slice(DateTime.Now, new BaseData[] { quandlSpy, tradeBarAapl, quandlAapl });

                Assert.DoesNotThrow(() => test(new PythonSlice(slice)));
            }
        }

        [Test]
        public void PythonGetCustomData_Iterate_Tiingo_Empty()
        {
            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    @"
from clr import AddReference
AddReference(""QuantConnect.Common"")
from QuantConnect import *
from QuantConnect.Data.Custom.Tiingo import TiingoNews

def Test(slice):
    data = slice.Get(TiingoNews)
    for singleData in data:
        raise Exception('Unexpected iteration')
    for singleData in data.Values:
        raise Exception('Unexpected iteration')
    data = slice.Get(TiingoNews)
    for singleData in data:
        raise Exception('Unexpected iteration')
    for singleData in data.Values:
        raise Exception('Unexpected iteration')").GetAttr("Test");
                var tradeBarAapl = new TradeBar { Symbol = Symbols.AAPL, Time = DateTime.Now, Value = 9 };
                var slice = new Slice(DateTime.Now, new List<BaseData> { tradeBarAapl });

                Assert.DoesNotThrow(() => test(new PythonSlice(slice)));
            }
        }

        [Test]
        public void PythonGetCustomData_Iterate()
        {
            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    @"
from clr import AddReference
AddReference(""QuantConnect.Common"")
from QuantConnect import *
from QuantConnect.Data.Custom import *

def Test(slice):
    data = slice.Get(Quandl)
    count = 0
    for singleData in data:
        count += 1
    if count != 2:
        raise Exception('Unexpected value')").GetAttr("Test");
                var quandlSpy = new Quandl { Symbol = Symbols.SPY, Time = DateTime.Now, Value = 10 };
                var quandlAapl = new Quandl { Symbol = Symbols.AAPL, Time = DateTime.Now, Value = 11 };
                var slice = new Slice(DateTime.Now, new[] { quandlSpy, quandlAapl });

                Assert.DoesNotThrow(() => test(new PythonSlice(slice)));
            }
        }

        [Test]
        public void EnumeratorDoesNotThrowWithTicks()
        {
            var slice = new Slice(DateTime.Now, new[]
            {
                new Tick(DateTime.Now, Symbols.SPY, 1, 2),
                new Tick(DateTime.Now, Symbols.SPY, 1.1m, 2.1m),
                new Tick(DateTime.Now, Symbols.AAPL, 1, 2),
                new Tick(DateTime.Now, Symbols.AAPL, 1.1m, 2.1m)
            });

            Assert.AreEqual(4, slice.Count());
        }

        [Test]
        public void AccessesTradeBarAndQuoteBarForSameSymbol()
        {
            var tradeBar = new TradeBar(DateTime.Now, Symbols.BTCUSD,
                3000, 3000, 3000, 3000, 100, Time.OneMinute);

            var quoteBar = new QuoteBar(DateTime.Now, Symbols.BTCUSD,
                    new Bar(3100, 3100, 3100, 3100), 0,
                    new Bar(3101, 3101, 3101, 3101), 0,
                    Time.OneMinute);

            var tradeBars = new TradeBars { { Symbols.BTCUSD, tradeBar } };
            var quoteBars = new QuoteBars { { Symbols.BTCUSD, quoteBar } };

            var slice = new Slice(DateTime.Now, new BaseData[] { tradeBar, quoteBar }, tradeBars, quoteBars, null, null, null, null, null, null, null);

            var tradeBarData = slice.Get<TradeBar>();
            Assert.AreEqual(1, tradeBarData.Count);
            Assert.AreEqual(3000, tradeBarData[Symbols.BTCUSD].Close);

            var quoteBarData = slice.Get<QuoteBar>();
            Assert.AreEqual(1, quoteBarData.Count);
            Assert.AreEqual(3100, quoteBarData[Symbols.BTCUSD].Bid.Close);
            Assert.AreEqual(3101, quoteBarData[Symbols.BTCUSD].Ask.Close);

            slice = new Slice(DateTime.Now, new BaseData[] { tradeBar, quoteBar });

            tradeBarData = slice.Get<TradeBar>();
            Assert.AreEqual(1, tradeBarData.Count);
            Assert.AreEqual(3000, tradeBarData[Symbols.BTCUSD].Close);

            quoteBarData = slice.Get<QuoteBar>();
            Assert.AreEqual(1, quoteBarData.Count);
            Assert.AreEqual(3100, quoteBarData[Symbols.BTCUSD].Bid.Close);
            Assert.AreEqual(3101, quoteBarData[Symbols.BTCUSD].Ask.Close);
        }
    }
}
