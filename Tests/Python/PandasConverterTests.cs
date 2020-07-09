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
 *
*/

using NUnit.Framework;
using Python.Runtime;
using QuantConnect.Data;
using QuantConnect.Data.Custom;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Python;
using QuantConnect.Securities;
using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Tests.ToolBox;
using QuantConnect.ToolBox;
using QuantConnect.Util;

namespace QuantConnect.Tests.Python
{
    [TestFixture]
    public partial class PandasConverterTests
    {
        private PandasConverter _converter = new PandasConverter();

        [SetUp]
        public void Setup()
        {
            SymbolCache.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            SymbolCache.Clear();
        }

        [Test]
        public void HandlesEmptyEnumerable()
        {
            var converter = new PandasConverter();
            var rawBars = Enumerable.Empty<TradeBar>().ToArray();

            // GetDataFrame with argument of type IEnumerable<TradeBar>
            dynamic dataFrame = converter.GetDataFrame(rawBars);

            using (Py.GIL())
            {
                Assert.IsTrue(dataFrame.empty.AsManagedObject(typeof(bool)));
            }

            // GetDataFrame with argument of type IEnumerable<TradeBar>
            var history = GetHistory(Symbols.SPY, Resolution.Minute, rawBars);
            dataFrame = converter.GetDataFrame(history);

            using (Py.GIL())
            {
                Assert.IsTrue(dataFrame.empty.AsManagedObject(typeof(bool)));
            }
        }

        [Test]
        public void HandlesTradeBars()
        {
            var converter = new PandasConverter();
            var symbol = Symbols.SPY;

            var rawBars = Enumerable
                .Range(0, 10)
                .Select(i => new TradeBar(DateTime.UtcNow.AddMinutes(i), symbol, i + 101m, i + 102m, i + 100m, i + 101m, 0m))
                .ToArray();

            // GetDataFrame with argument of type IEnumerable<TradeBar>
            dynamic dataFrame = converter.GetDataFrame(rawBars);

            using (Py.GIL())
            {
                Assert.IsFalse(dataFrame.empty.AsManagedObject(typeof(bool)));

                var subDataFrame = dataFrame.loc[symbol];
                Assert.IsFalse(subDataFrame.empty.AsManagedObject(typeof(bool)));

                var count = subDataFrame.__len__().AsManagedObject(typeof(int));
                Assert.AreEqual(count, 10);

                for (var i = 0; i < count; i++)
                {
                    var index = subDataFrame.index[i];
                    var close = subDataFrame.loc[index].close.AsManagedObject(typeof(decimal));
                    Assert.AreEqual(rawBars[i].Close, close);
                }
            }

            // GetDataFrame with argument of type IEnumerable<TradeBar>
            var history = GetHistory(symbol, Resolution.Minute, rawBars);
            dataFrame = converter.GetDataFrame(history);

            using (Py.GIL())
            {
                Assert.IsFalse(dataFrame.empty.AsManagedObject(typeof(bool)));

                var subDataFrame = dataFrame.loc[symbol];
                Assert.IsFalse(subDataFrame.empty.AsManagedObject(typeof(bool)));

                var count = subDataFrame.__len__().AsManagedObject(typeof(int));
                Assert.AreEqual(count, 10);

                for (var i = 0; i < count; i++)
                {
                    var index = subDataFrame.index[i];
                    var close = subDataFrame.loc[index].close.AsManagedObject(typeof(decimal));
                    Assert.AreEqual(rawBars[i].Close, close);
                }
            }
        }

        [Test]
        public void HandlesQuoteBars()
        {
            var converter = new PandasConverter();
            var symbol = Symbols.EURUSD;

            var rawBars = Enumerable
                .Range(0, 10)
                .Select(i => new QuoteBar(DateTime.UtcNow.AddMinutes(i), symbol, new Bar(i + 1.01m, i + 1.02m, i + 1.00m, i + 1.01m), 0m, new Bar(i + 1.01m, i + 1.02m, i + 1.00m, i + 1.01m), 0m))
                .ToArray();

            // GetDataFrame with argument of type IEnumerable<QuoteBar>
            dynamic dataFrame = converter.GetDataFrame(rawBars);

            using (Py.GIL())
            {
                Assert.IsFalse(dataFrame.empty.AsManagedObject(typeof(bool)));

                var subDataFrame = dataFrame.loc[symbol];
                Assert.IsFalse(subDataFrame.empty.AsManagedObject(typeof(bool)));

                var count = subDataFrame.__len__().AsManagedObject(typeof(int));
                Assert.AreEqual(count, 10);

                for (var i = 0; i < count; i++)
                {
                    var index = subDataFrame.index[i];
                    var close = subDataFrame.loc[index].close.AsManagedObject(typeof(decimal));
                    Assert.AreEqual(rawBars[i].Close, close);
                }
            }

            // GetDataFrame with argument of type IEnumerable<QuoteBar>
            var history = GetHistory(symbol, Resolution.Minute, rawBars);
            dataFrame = converter.GetDataFrame(history);

            using (Py.GIL())
            {
                Assert.IsFalse(dataFrame.empty.AsManagedObject(typeof(bool)));

                var subDataFrame = dataFrame.loc[symbol];
                Assert.IsFalse(subDataFrame.empty.AsManagedObject(typeof(bool)));

                var count = subDataFrame.__len__().AsManagedObject(typeof(int));
                Assert.AreEqual(count, 10);

                for (var i = 0; i < count; i++)
                {
                    var index = subDataFrame.index[i];
                    var close = subDataFrame.loc[index].askclose.AsManagedObject(typeof(decimal));
                    Assert.AreEqual(rawBars[i].Ask.Close, close);
                }
            }
        }

        [Test]
        public void HandlesNullableValues()
        {
            var converter = new PandasConverter();
            var rawBars = Enumerable
                .Range(0, 10)
                .Select(i => new NullableValueData {
                    Symbol = Symbols.AAPL,
                    EndTime = new DateTime(2020, 1, 1),
                    NullableInt = null,
                    NullableTime = null,
                    NullableColumn = null
                })
                .ToArray();

            rawBars[0].NullableInt = 0;
            rawBars[1].NullableTime = new DateTime(2020, 1, 2);

            // GetDataFrame with argument of type IEnumerable<QuoteBar>
            dynamic dataFrame = converter.GetDataFrame(rawBars);

            using (Py.GIL())
            {
                Assert.AreEqual(2, dataFrame.columns.__len__().AsManagedObject(typeof(int)));
                var count = dataFrame.__len__().AsManagedObject(typeof(int));
                Assert.AreEqual(10, count);
            }

            // GetDataFrame with argument of type IEnumerable<QuoteBar>
            var history = GetHistory(Symbols.AAPL, Resolution.Daily, rawBars);
            dataFrame = converter.GetDataFrame(history);

            using (Py.GIL())
            {
                Assert.AreEqual(2, dataFrame.columns.__len__().AsManagedObject(typeof(int)));
                var count = dataFrame.__len__().AsManagedObject(typeof(int));
                Assert.AreEqual(10, count);
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilityDataFrame_add_prefix(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, symbol):
    data = dataFrame.add_prefix('price_')['price_lastprice'].unstack(level=0)
    data = data.iloc[-1][{index}]
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilityDataFrame_add_suffix(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, symbol):
    data = dataFrame.add_suffix('_tick')['lastprice_tick'].unstack(level=0)
    data = data.iloc[-1][{index}]
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilityDataFrame_align(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, other, symbol):
    data = dataFrame.lastprice.unstack(level=0)
    other = other.lastprice.unstack(level=0)
    data, other = data.align(other, axis=0)
    data = data[{index}]
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), GetTestDataFrame(Symbols.AAPL), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilityDataFrame_apply(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
import numpy as np
def Test(dataFrame, symbol):
    data = dataFrame.lastprice.unstack(level=0).apply(np.sqrt)
    data = data[{index}]
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilityDataFrame_applymap(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, symbol):
    data = dataFrame.lastprice.unstack(level=0).applymap(lambda x: x*2)
    data = data[{index}]
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilityDataFrame_asfreq(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, symbol):
    data = dataFrame.lastprice.unstack(level=0).asfreq(freq='30S')
    data = data[{index}]
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilityDataFrame_asof(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
import pandas as pd
def Test(dataFrame, symbol):
    idx = pd.DatetimeIndex(['2018-02-27 09:03:30'])
    data = dataFrame.lastprice.unstack(level=0).asof(idx)
    data = data[{index}]
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilityDataFrame_assign(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, symbol):
    data = dataFrame.assign(tmp=lambda x: x.lastprice * 1.1)['tmp'].unstack(level=0)
    data = data[{index}]
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilityDataFrame_astype(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, symbol):
    data = dataFrame.lastprice.unstack(level=0).astype('float16')
    data = data[{index}]
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilityDataFrame_at(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, symbol):
    data = dataFrame.at[({index},), 'lastprice']").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilityDataFrame_at_time(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, symbol):
    data = dataFrame.lastprice.unstack(level=0).at_time('04:00')
    data = data[{index}]
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilityDataFrame_axes(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, symbol):
    axes = dataFrame.axes[0]
    if {index} not in axes.levels[0]:
        raise ValueError('SPY was not found')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilityDataFrame_between_time(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, symbol):
    data = dataFrame.lastprice.unstack(level=0).between_time('02:00', '06:00')
    data = data[{index}]
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilityDataFrame_columns(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, symbol):
    columns = dataFrame.lastprice.unstack(level=0).columns
    if {index} not in columns:
        raise ValueError('SPY was not found')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilityDataFrame_combine(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
import numpy as np
def Test(dataFrame, other, symbol):
    dataFrame = dataFrame.lastprice.unstack(level=0)
    other = other.lastprice.unstack(level=0)
    data = dataFrame.combine(other, np.minimum)
    data = data[{index}]
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), GetTestDataFrame(Symbols.AAPL), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilityDataFrame_concat(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
import pandas as pd
def Test(dataFrame, dataFrame2, symbol):
    newDataFrame = pd.concat([dataFrame, dataFrame2])
    data = newDataFrame['lastprice'].unstack(level=0).ix[-1][{index}]
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), GetTestDataFrame(Symbols.AAPL), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilityDataFrame_corrwith(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
import numpy as np
def Test(dataFrame, other, symbol):
    other = other.lastprice.unstack(level=0)
    data = dataFrame.lastprice.unstack(level=0).corrwith(other)
    data = data.loc[{index}]
    if not np.isnan(data):
        raise Exception('Data should be NaN')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), GetTestDataFrame(Symbols.AAPL), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilityDataFrame_drop(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, symbol):
    data = dataFrame.drop(columns=['exchange']).lastprice.unstack(level=0)
    data = data[{index}]
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilityDataFrame_droplevel(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, symbol):
    data = dataFrame.droplevel('time').lastprice
    data = data.loc[{index}]
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilityDataFrame_dtypes(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
import numpy as np
def Test(dataFrame, symbol):
    data = dataFrame.lastprice.unstack(level=0).dtypes
    data = data.loc[{index}]
    if data != np.float64:
        raise Exception('Data type is ' + str(data))").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilityDataFrame_eval(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, symbol):
    data = dataFrame.eval('tmp=lastprice * 1.1')['tmp'].unstack(level=0)
    data = data[{index}]
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilityDataFrame_equals(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, other, symbol):
    if not dataFrame.equals(other):
        raise Exception('Dataframe are not equal')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilityDataFrame_ewm(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, symbol):
    data = dataFrame.lastprice.unstack(level=0)
    data = data.ewm(com=0.5).mean()
    data = data[{index}]
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilityDataFrame_expanding(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, symbol):
    data = dataFrame.lastprice.unstack(level=0)
    data = data.expanding(2).sum()
    data = data[{index}]
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilityDataFrame_explode(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, symbol):
    data = dataFrame.explode('lastprice').lastprice.unstack(level=0)
    data = data[{index}]
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilityDataFrame_filter(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, symbol):
    data = dataFrame.filter(items=['lastprice']).lastprice.unstack(level=0)
    data = data[{index}]
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilityDataFrame_ftypes(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, symbol):
    data = dataFrame.lastprice.unstack(level=0).ftypes
    data = data.loc[{index}]
    if str(data) != 'float64:dense':
        raise Exception('Data type is ' + str(data))").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilityDataFrame_get_OnProperty(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, symbol):
    data = dataFrame.lastprice.get({index})
    if data.empty:
        raise Exception('Data is empty')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilityDataFrame_getitem(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, symbol):
    data = dataFrame['lastprice'][{index}]").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilityDataFrame_get_value_index(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, symbol):
    data = dataFrame.get_value(({index},), 'lastprice')
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilityDataFrame_get_value_column(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, symbol):
    data = dataFrame.lastprice.unstack(level=0)
    idx = data.index[0]
    data = data.get_value(idx, {index})
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilityDataFrame_groupby(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
import pandas as pd
def Test(df, other, symbol):
    df = pd.concat([df, other])
    df = df.groupby(level=0).mean()
    data = df.lastprice.loc[{index}]
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY, 10), GetTestDataFrame(Symbols.AAPL, 10), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilityDataFrame_iloc(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, symbol):
    data = dataFrame['lastprice'].unstack(level=0).iloc[-1][{index}]
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilityDataFrame_insert(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, symbol):
    data = dataFrame.lastprice.unstack(level=0)
    data.insert(0, 'nine', 999)
    data = data[{index}]
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");
                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilityDataFrame_index_levels_contains(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, symbol):
    if {index} not in dataFrame.index.levels[0]:
        raise ValueError('SPY was not found')").GetAttr("Test");
                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }


        [TestCase("items", "'SPY'", true)]
        [TestCase("items", "symbol")]
        [TestCase("items", "str(symbol.ID)")]
        [TestCase("iteritems", "'SPY'", true)]
        [TestCase("iteritems", "symbol")]
        [TestCase("iteritems", "str(symbol.ID)")]
        public void BackwardsCompatibilityDataFrame_items(string method, string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, symbol):
    for index, data in dataFrame.{method}():
        pass
    data = data[{index}]
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilityDataFrame_iterrows(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, symbol):
    for index, data in dataFrame.lastprice.unstack(level=0).iterrows():
        pass
    data = data[{index}]
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilityDataFrame_ix(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, symbol):
    data = dataFrame['lastprice'].unstack(level=0).ix[-1][{index}]
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilityDataFrame_join(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, dataFrame2, symbol):
    newDataFrame = dataFrame.join(dataFrame2, lsuffix='_')
    data = newDataFrame['lastprice'].unstack(level=0).ix[-1][{index}]
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), GetTestDataFrame(Symbols.AAPL), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilityDataFrame_loc(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, symbol):
    data = dataFrame.loc[{index}]").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [TestCase("['SPY','AAPL']", true)]
        [TestCase("symbols")]
        [TestCase("[str(symbols[0].ID), str(symbols[1].ID)]")]
        public void BackwardsCompatibilityDataFrame_loc_list(string index, bool cache = false)
        {
            if (cache)
            {
                SymbolCache.Set("SPY", Symbols.SPY);
                SymbolCache.Set("AAPL", Symbols.AAPL);
            }

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, symbols):
    data = dataFrame.lastprice.unstack(0)
    data = data.loc[:, {index}]").GetAttr("Test");

                var symbols = new List<Symbol> {Symbols.SPY, Symbols.AAPL};
                Assert.DoesNotThrow(() => test(GetTestDataFrame(symbols), symbols));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilityDataFrame_loc_after_xs(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, symbol):
    time = dataFrame.index.get_level_values('time')[0]
    dataFrame = dataFrame.xs(time, level='time')
    data = dataFrame.loc[{index}]").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilityDataFrame_loc_OnProperty(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, symbol):
    data = dataFrame.lastprice.loc[{index}]").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilityDataFrame_loc_SubDataFrame(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, symbol):
    data = dataFrame.loc[{index}].loc['2013-10-07 04:00:00']").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilityDataFrame_mask(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, symbol):
    data = dataFrame.lastprice.unstack(0)
    data = data.mask(data > 0)
    data = data[{index}]
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilityDataFrame_pivot_table(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
import pandas as pd
def Test(dataFrame, symbol):
    df = dataFrame.reset_index()
    table = pd.pivot_table(df, index=['symbol', 'time'])
    data = table.lastprice.unstack(0)
    data = data[{index}]
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY, 10), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilityDataFrame_merge(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, dataFrame2, symbol):
    newDataFrame = dataFrame.merge(dataFrame2, on='symbol', how='outer')
    data = newDataFrame.loc[{index}]
    if len(data) is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), GetTestDataFrame(Symbols.AAPL), Symbols.SPY));
            }
        }

        [TestCase("nlargest", "'SPY'", true)]
        [TestCase("nlargest", "symbol")]
        [TestCase("nlargest", "str(symbol.ID)")]
        [TestCase("nsmallest", "'SPY'", true)]
        [TestCase("nsmallest", "symbol")]
        [TestCase("nsmallest", "str(symbol.ID)")]
        public void BackwardsCompatibilityDataFrame_nlarg_smallest(string method, string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, symbol):
    data = dataFrame.{method}(3, 'lastprice')
    data = data.loc[{index}]
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY, 10), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilityDataFrame_pipe(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
import pandas as pd
def Test(dataFrame, other, symbol):
    def mean_by_group(dataframe, level):
        return dataframe.groupby(level=level).mean()

    df = pd.concat([dataFrame, other])
    data = df.pipe(mean_by_group, level=0)
    data = data.loc[{index}]
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY, 10), GetTestDataFrame(Symbols.AAPL, 10), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilityDataFrame_pop(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, symbol):
    data = dataFrame.pop('lastprice')
    data = data.loc[{index}]
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilityDataFrame_query(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, symbol):
    data = dataFrame.query('lastprice > 100').lastprice.unstack(0)
    data = data[{index}]
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilityDataFrame_query_index(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, symbol):
    time = '2011-02-01'
    data = dataFrame.lastprice.unstack(0).query('index > @time')
    data = data[{index}]
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY, 10), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilityDataFrame_reindex_like(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, other, symbol):
    data = other.reindex_like(dataFrame)
    data = data.lastprice.unstack(0)
    data = data[{index}]
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY, 10), GetTestDataFrame(Symbols.AAPL), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilityDataFrame_rename(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            var rename = "columns={'lastprice': 'price', 'B': 'c'}";

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, symbol):
    data = dataFrame.rename({rename})['price'].unstack(0)
    data = data[{index}]
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilityDataFrame_reorder_levels(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, symbol):
    data = dataFrame.reorder_levels(['time', 'symbol']).lastprice.unstack(1)
    data = data[{index}]
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilityDataFrame_resample(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, symbol):
    data = dataFrame.lastprice.unstack(0) 
    data = data.resample('2S').sum()
    data = data[{index}]
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY, 10), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilityDataFrame_reset_index(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, symbol):
    data = dataFrame.lastprice.unstack(0).reset_index('time')
    data = data[{index}]
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilityDataFrame_rolling(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, symbol):
    data = dataFrame.rolling(2).sum()
    data = data.lastprice.unstack(0)
    data = data[{index}]
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY, 10), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilityDataFrame_select_dtypes(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, symbol):
    data = dataFrame.select_dtypes(exclude=['int']).lastprice.unstack(0)
    data = data[{index}]
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilityDataFrame_set_value(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, symbol):
    idx = dataFrame.first_valid_index()
    data = dataFrame.set_value(idx, 'lastprice', 100).lastprice.unstack(0)
    data = data[{index}]
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY, 3), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilityDataFrame_slice_shift(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, symbol):
    data = dataFrame.slice_shift().lastprice.unstack(0)
    data = data[{index}]
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY, 10), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilityDataFrame_sort_values(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, symbol):
    data = dataFrame.sort_values(by=['lastprice']).lastprice.unstack(0)
    data = data[{index}]
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilityDataFrame_swapaxes(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, symbol):
    data = dataFrame.lastprice.unstack(0).swapaxes('index', 'columns')
    data = data.loc[{index}]
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilityDataFrame_swaplevel(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, symbol):
    data = dataFrame.swaplevel().lastprice.unstack(1)
    data = data[{index}]
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilityDataFrame_take(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, symbol):
    data = dataFrame.take([0, 3]).lastprice.unstack(0)
    data = data[{index}]
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY, 10), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilityDataFrame_transform(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
import numpy as np
def Test(dataFrame, symbol):
    data = dataFrame.lastprice.unstack(0).transform(np.sqrt)
    data = data[{index}]
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilityDataFrame_T(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, symbol):
    data = dataFrame.T.iloc[0]
    data = data.loc[{index}]
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilityDataFrame_transpose(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, symbol):
    data = dataFrame.transpose().iloc[0]
    data = data.loc[{index}]
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilityDataFrame_tshift(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
from datetime import timedelta as d
def Test(dataFrame, symbol):
    series = dataFrame.droplevel(0)
    data = series.tshift(freq=d(1))").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilityDataFrame_tz_localize_convert(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, symbol):
    data = dataFrame.tz_localize('UTC', level=1)
    data = data.tz_convert('America/New_York', level=1)
    data = data.lastprice.unstack(0)[{index}]
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilityDataFrame_unstack_lastprice(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, symbol):
    data = dataFrame.unstack(level=0).lastprice[{index}]").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilityDataFrame_unstack_loc_loc(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, symbol):
    df2 = dataFrame.unstack(level=0)
    df3 = df2.loc[:,'lastprice']
    data = df3.loc[:, {index}]").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilityDataFrame_unstack_get(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, symbol):
    df2 = dataFrame.lastprice.unstack(level=0)
    data = df2.get({index})
    if data.empty:
        raise Exception('Data is empty')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilityDataFrame_update(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            var other = "{'lastprice': ['d', 'e', 'f']}";

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
import pandas as pd
def Test(dataFrame, symbol):
    other = pd.DataFrame({other}, index=dataFrame.index)
    dataFrame.update(other)
    data = dataFrame.lastprice.unstack(0)[{index}]
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY, 3), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilityDataFrame_where(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, symbol):
    dataFrame = dataFrame.lastprice.unstack(0)
    data = dataFrame.where(dataFrame > 167)
    data = data[{index}]
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY, 3), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilityDataFrame_xs(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, symbol):
    data = dataFrame.xs({index})").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol)")]
        public void BackwardsCompatibilitySeries__str__(string symbol, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);
            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
import pandas as pd
from datetime import datetime as dt
def Test(dataFrame, symbol):
    close = dataFrame.lastprice.unstack(0)
    to_append = pd.Series([100], name={symbol}, index=pd.Index([dt.now()], name='time'))
    result = pd.concat([close, to_append], ignore_index=True)
    return str([result[x] for x in [symbol]])").GetAttr("Test");
                var result = "Remapper";
                Assert.DoesNotThrow(() => result = test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
                // result should not contain "Remapper" string because the test
                // returns the string representation of a Series object
                Assert.False(result.Contains("Remapper"));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol)")]
        public void BackwardsCompatibilitySeries__repr__(string symbol, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);
            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
import pandas as pd
from datetime import datetime as dt
def Test(dataFrame, symbol):
    close = dataFrame.lastprice.unstack(0)
    to_append = pd.Series([100], name={symbol}, index=pd.Index([dt.now()], name='time'))
    result = pd.concat([close, to_append], ignore_index=True)
    return repr([result[x] for x in [symbol]])").GetAttr("Test");
                var result = "Remapper";
                Assert.DoesNotThrow(() => result = test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
                // result should not contain "Remapper" string because the test
                // returns the string representation of a Series object
                Assert.False(result.Contains("Remapper"));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilitySeries_align(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, other, symbol):
    data = dataFrame.lastprice
    other = other.lastprice
    data, other = data.align(other, axis=0)
    data = data.loc[{index}]
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), GetTestDataFrame(Symbols.AAPL), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilitySeries_apply(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
import numpy as np
def Test(dataFrame, symbol):
    data = dataFrame.lastprice.apply(np.sqrt)
    data = data.loc[{index}]
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilitySeries_asfreq(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, symbol):
    series = dataFrame.lastprice.droplevel(0) 
    data = series.asfreq(freq='30S')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilitySeries_asof(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
import pandas as pd
def Test(dataFrame, symbol):
    idx = pd.DatetimeIndex(['2018-02-27 09:03:30'])
    series = dataFrame.lastprice.droplevel(0)
    data = series.asof(idx)").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilitySeries_astype(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, symbol):
    data = dataFrame.lastprice.astype('float16')
    data = data.loc[{index}]
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilitySeries_at_time(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, symbol):
    series = dataFrame.lastprice.droplevel(0) 
    data = series.at_time('04:00')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilitySeries_between(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, symbol):
    series = dataFrame.lastprice
    data = series.between(100, 200)").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilitySeries_between_time(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, symbol):
    series = dataFrame.lastprice.droplevel(0) 
    data = series.between_time('02:00', '06:00')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilitySeries_combine(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
import numpy as np
def Test(dataFrame, other, symbol):
    series = dataFrame.lastprice
    other = other.lastprice
    data = series.combine(other, np.minimum)
    data = data.loc[{index}]
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), GetTestDataFrame(Symbols.AAPL), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilitySeries_drop(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, symbol):
    series = dataFrame.lastprice
    dt = series.first_valid_index()[1]
    data = series.drop(dt, level=1)
    data = data.loc[{index}]
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY, 2), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilitySeries_droplevel(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, symbol):
    series = dataFrame.lastprice
    data = series.droplevel('time')
    data = data.loc[{index}]
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilitySeries_equals(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, other, symbol):
    series = dataFrame.lastprice
    other = other.lastprice
    if not series.equals(other):
        raise Exception('Dataframe are not equal')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilitySeries_ewm(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, symbol):
    series = dataFrame.lastprice
    data = series.ewm(com=0.5).mean()
    data = data.loc[{index}]
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilitySeries_expanding(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, symbol):
    series = dataFrame.lastprice
    data = series.expanding(2).sum()
    data = data.loc[{index}]
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilitySeries_filter(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, symbol):
    series = dataFrame.lastprice
    data = series.filter(like='04:00')
    data = data.loc[{index}]
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilitySeries_first(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, symbol):
    series = dataFrame.lastprice.droplevel(0) 
    data = series.first('2S')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY, 10), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilitySeries_get(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, symbol):
    series = dataFrame.lastprice
    data = series.get({index})
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilitySeries_get_value(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, symbol):
    series = dataFrame.lastprice
    dt = series.first_valid_index()[1]
    data = series.get_value(({index}, dt))
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilitySeries_groupby(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
import pandas as pd
def Test(df, other, symbol):
    series = pd.concat([df, other]).lastprice
    data = series.groupby(level=0).mean()
    data = data.loc[{index}]
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY, 10), GetTestDataFrame(Symbols.AAPL, 10), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilitySeries_last(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, symbol):
    series = dataFrame.lastprice.droplevel(0) 
    data = series.last('2S')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY, 10), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilitySeries_map(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            var map = "map({167: 100})";

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, symbol):
    series = dataFrame.lastprice
    data = series.{map}
    data = data.loc[{index}]
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY, 10), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilitySeries_mask(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, symbol):
    series = dataFrame.lastprice
    data = series.mask(series > 0)
    data = data.loc[{index}]
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY, 10), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilitySeries_pipe(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
import pandas as pd
def Test(dataFrame, other, symbol):
    def mean_by_group(dataframe, level):
        return dataframe.groupby(level=level).mean()

    df = pd.concat([dataFrame, other]).lastprice
    data = df.pipe(mean_by_group, level=0)
    data = data.loc[{index}]
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY, 10), GetTestDataFrame(Symbols.AAPL, 10), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilitySeries_pop(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, symbol):
    data = dataFrame.lastprice.pop({index})
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilitySeries_reindex_like(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, other, symbol):
    series = dataFrame.lastprice
    other = other.lastprice
    data = other.reindex_like(series)
    data = data.loc[{index}]
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY, 10), GetTestDataFrame(Symbols.AAPL), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilitySeries_rename(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, symbol):
    series = dataFrame.lastprice
    data = series.rename('price')
    data = data.loc[{index}]
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilitySeries_repeat(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, symbol):
    series = dataFrame.lastprice
    data = series.repeat(3)
    data = data.loc[{index}]
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilitySeries_reorder_levels(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, symbol):
    series = dataFrame.lastprice
    data = series.reorder_levels(['time', 'symbol'])
    data = data.unstack(1)[{index}]
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilitySeries_resample(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, symbol):
    series = dataFrame.lastprice
    data = series.resample('2S', level=1).mean()").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY, 10), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilitySeries_reset_index(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, symbol):
    series = dataFrame.lastprice
    data = series.reset_index('time')
    data = data.loc[{index}]
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY, 10), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilitySeries_rolling(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, symbol):
    series = dataFrame.lastprice
    data = series.rolling(2).sum()
    data = data.loc[{index}]
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY, 10), Symbols.SPY));
            }
        }


        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilitySeries_set_value(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, symbol):
    series = dataFrame.lastprice
    idx = series.first_valid_index()
    data = series.set_value(idx, 100)
    data = data.loc[{index}]
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY, 10), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilitySeries_slice_shift(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, symbol):
    series = dataFrame.lastprice
    data = series.slice_shift()
    data = data.loc[{index}]
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY, 10), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilitySeries_swapaxes(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, symbol):
    series = dataFrame.lastprice
    data = series.swapaxes('index', 'index')
    data = data.loc[{index}]
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilitySeries_swaplevel(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, symbol):
    series = dataFrame.lastprice
    data = series.swaplevel()
    data = data.unstack(0).loc[{index}]
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilitySeries_take(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, symbol):
    series = dataFrame.lastprice
    data = series.take([0, 3])
    data = data.loc[{index}]
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY, 10), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilitySeries_transform(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
import numpy as np
def Test(dataFrame, symbol):
    series = dataFrame.lastprice
    data = series.transform([np.sqrt, np.exp])
    data = data.loc[{index}]
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }


        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilitySeries_T(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, symbol):
    series = dataFrame.lastprice
    data = series.T
    data = data[{index}]
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilitySeries_transpose(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, symbol):
    series = dataFrame.lastprice
    data = series.transpose()
    data = data[{index}]
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilitySeries_tshift(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
from datetime import timedelta as d
def Test(dataFrame, symbol):
    series = dataFrame.lastprice.droplevel(0)
    data = series.tshift(freq=d(1))").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilitySeries_tz_localize_convert(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, symbol):
    series = dataFrame.lastprice
    data = series.tz_localize('UTC', level=1)
    data = data.tz_convert('America/New_York', level=1)
    data = data.loc[{index}]
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilitySeries_update(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
import pandas as pd
def Test(dataFrame, symbol):
    series = dataFrame.lastprice
    other = pd.Series(['d', 'e', 'f'], index=series.index)
    series.update(other)
    data = series.loc[{index}]
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY, 3), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilitySeries_where(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, symbol):
    series = dataFrame.lastprice
    data = series.where(series > 167)
    data = data.loc[{index}]
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY, 3), Symbols.SPY));
            }
        }

        [TestCase("'SPY'", true)]
        [TestCase("symbol")]
        [TestCase("str(symbol.ID)")]
        public void BackwardsCompatibilitySeries_xs(string index, bool cache = false)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    $@"
def Test(dataFrame, symbol):
    series = dataFrame.lastprice
    data = series.xs({index})").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }


        [Test]
        public void NotBackwardsCompatibilityDataFrame_index_levels_contains_ticker_notInCache()
        {
            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    @"
def Test(dataFrame, symbol):
    if 'SPY' not in dataFrame.index.levels[0]:
        raise ValueError('SPY was not found')").GetAttr("Test");
                Assert.Throws<PythonException>(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [Test]
        public void HandlesTradeTicks()
        {
            var converter = new PandasConverter();
            var symbol = Symbols.SPY;

            var rawBars = Enumerable
                .Range(0, 10)
                .Select(i => new Tick(symbol, $"1440{i:D2}00,167{i:D2}00,1{i:D2},T,T,0", new DateTime(2013, 10, 7)))
                .ToArray();

            // GetDataFrame with argument of type IEnumerable<QuoteBar>
            dynamic dataFrame = converter.GetDataFrame(rawBars);

            using (Py.GIL())
            {
                Assert.IsFalse(dataFrame.empty.AsManagedObject(typeof(bool)));

                var subDataFrame = dataFrame.loc[symbol];
                Assert.IsFalse(subDataFrame.empty.AsManagedObject(typeof(bool)));

                Assert.IsTrue(subDataFrame.get("askprice") == null);
                Assert.IsTrue(subDataFrame.get("exchange") != null);

                var count = subDataFrame.__len__().AsManagedObject(typeof(int));
                Assert.AreEqual(count, 10);

                for (var i = 0; i < count; i++)
                {
                    var index = subDataFrame.index[i];
                    var value = subDataFrame.loc[index].lastprice.AsManagedObject(typeof(decimal));
                    Assert.AreEqual(rawBars[i].LastPrice, value);
                }
            }

            // GetDataFrame with argument of type IEnumerable<QuoteBar>
            var history = GetHistory(symbol, Resolution.Tick, rawBars);
            dataFrame = converter.GetDataFrame(history);

            using (Py.GIL())
            {
                Assert.IsFalse(dataFrame.empty.AsManagedObject(typeof(bool)));

                var subDataFrame = dataFrame.loc[symbol];
                Assert.IsFalse(subDataFrame.empty.AsManagedObject(typeof(bool)));

                Assert.IsTrue(subDataFrame.get("askprice") == null);
                Assert.IsTrue(subDataFrame.get("exchange") != null);

                var count = subDataFrame.__len__().AsManagedObject(typeof(int));
                Assert.AreEqual(count, 10);

                for (var i = 0; i < count; i++)
                {
                    var index = subDataFrame.index[i];
                    var value = subDataFrame.loc[index].lastprice.AsManagedObject(typeof(decimal));
                    Assert.AreEqual(rawBars[i].LastPrice, value);
                }
            }
        }

        [Test]
        public void HandlesQuoteTicks()
        {
            var converter = new PandasConverter();
            var symbol = Symbols.EURUSD;

            var rawBars = Enumerable
                .Range(0, 10)
                .Select(i => new Tick(DateTime.UtcNow.AddMilliseconds(100 * i), symbol, 0.99m, 1.01m))
                .ToArray();

            // GetDataFrame with argument of type IEnumerable<QuoteBar>
            dynamic dataFrame = converter.GetDataFrame(rawBars);

            using (Py.GIL())
            {
                Assert.IsFalse(dataFrame.empty.AsManagedObject(typeof(bool)));

                var subDataFrame = dataFrame.loc[symbol];
                Assert.IsFalse(subDataFrame.empty.AsManagedObject(typeof(bool)));

                Assert.IsTrue(subDataFrame.get("askprice") != null);
                Assert.IsTrue(subDataFrame.get("exchange") == null);

                var count = subDataFrame.__len__().AsManagedObject(typeof(int));
                Assert.AreEqual(count, 10);

                for (var i = 0; i < count; i++)
                {
                    var index = subDataFrame.index[i];
                    var value = subDataFrame.loc[index].lastprice.AsManagedObject(typeof(decimal));
                    Assert.AreEqual(rawBars[i].LastPrice, value);
                }
            }

            // GetDataFrame with argument of type IEnumerable<QuoteBar>
            var history = GetHistory(symbol, Resolution.Tick, rawBars);
            dataFrame = converter.GetDataFrame(history);

            using (Py.GIL())
            {
                Assert.IsFalse(dataFrame.empty.AsManagedObject(typeof(bool)));

                var subDataFrame = dataFrame.loc[symbol];
                Assert.IsFalse(subDataFrame.empty.AsManagedObject(typeof(bool)));

                Assert.IsTrue(subDataFrame.get("askprice") != null);
                Assert.IsTrue(subDataFrame.get("exchange") == null);

                var count = subDataFrame.__len__().AsManagedObject(typeof(int));
                Assert.AreEqual(count, 10);

                for (var i = 0; i < count; i++)
                {
                    var index = subDataFrame.index[i];
                    var value = subDataFrame.loc[index].askprice.AsManagedObject(typeof(decimal));
                    Assert.AreEqual(rawBars[i].AskPrice, value);
                }
            }
        }


        private static Resolution[] ResolutionCases = { Resolution.Tick, Resolution.Minute, Resolution.Second };
        private static Symbol[] SymbolCases = {Symbols.Fut_SPY_Feb19_2016, Symbols.Fut_SPY_Mar19_2016, Symbols.SPY_C_192_Feb19_2016, Symbols.SPY_P_192_Feb19_2016};

        [Test]
        public void HandlesOpenInterestTicks([ValueSource(nameof(ResolutionCases))]Resolution resolution, [ValueSource(nameof(SymbolCases))] Symbol symbol)
        {
            // Arrange
            var converter = new PandasConverter();
            var tickType = TickType.OpenInterest;
            var dataType = LeanData.GetDataType(resolution, tickType);
            var subcriptionDataConfig = new SubscriptionDataConfig(dataType, symbol, resolution,
                                                                   TimeZones.Chicago, TimeZones.Chicago,
                                                                   tickType: tickType, fillForward: false,
                                                                   extendedHours: true, isInternalFeed: true);
            var openinterest = new List<OpenInterest>();
            for (int i = 0; i < 10; i++)
            {
                var line = $"{1000 * i},{11 * i}";
                var openInterestTicks = new OpenInterest(subcriptionDataConfig, symbol, line, new DateTime(2017, 10, 10));
                openinterest.Add(openInterestTicks);
            }

            // Act
            dynamic dataFrame = converter.GetDataFrame(openinterest);

            //Assert
            using (Py.GIL())
            {
                Assert.IsFalse(dataFrame.empty.AsManagedObject(typeof(bool)));

                var subDataFrame = dataFrame.loc[symbol];
                Assert.IsFalse(subDataFrame.empty.AsManagedObject(typeof(bool)));

                Assert.IsTrue(subDataFrame.get("openinterest") != null);

                var count = subDataFrame.shape[0].AsManagedObject(typeof(int));
                Assert.AreEqual(count, 10);

                for (var i = 0; i < count; i++)
                {
                    var index = subDataFrame.index[i];
                    var value = subDataFrame.loc[index].openinterest.AsManagedObject(typeof(decimal));
                    Assert.AreEqual(openinterest[i].Value, value);
                }
            }

        }

        [Test]
        [TestCase(typeof(Quandl), "yyyy-MM-dd")]
        [TestCase(typeof(FxcmVolume), "yyyyMMdd HH:mm")]
        public void HandlesCustomDataBars(Type type, string format)
        {
            var converter = new PandasConverter();
            var symbol = Symbols.LTCUSD;

            var config = GetSubscriptionDataConfig<Quandl>(symbol, Resolution.Daily);
            var custom = Activator.CreateInstance(type) as BaseData;
            if (type == typeof(Quandl)) custom.Reader(config, "date,open,high,low,close,transactions", DateTime.UtcNow, false);

            var rawBars = Enumerable
                .Range(0, 10)
                .Select(i =>
                {
                    var line = $"{DateTime.UtcNow.AddDays(i).ToStringInvariant(format)},{i + 101},{i + 102},{i + 100},{i + 101},{i + 101}";
                    return custom.Reader(config, line, DateTime.UtcNow.AddDays(i), false);
                })
                .ToArray();

            // GetDataFrame with argument of type IEnumerable<BaseData>
            dynamic dataFrame = converter.GetDataFrame(rawBars);

            using (Py.GIL())
            {
                Assert.IsFalse(dataFrame.empty.AsManagedObject(typeof(bool)));

                var subDataFrame = dataFrame.loc[symbol];
                Assert.IsFalse(subDataFrame.empty.AsManagedObject(typeof(bool)));

                var count = subDataFrame.__len__().AsManagedObject(typeof(int));
                Assert.AreEqual(count, 10);

                for (var i = 0; i < count; i++)
                {
                    var index = subDataFrame.index[i];
                    var value = subDataFrame.loc[index].value.AsManagedObject(typeof(decimal));
                    Assert.AreEqual(rawBars[i].Value, value);

                    var transactions = subDataFrame.loc[index].transactions.AsManagedObject(typeof(decimal));
                    var expected = (rawBars[i] as DynamicData)?.GetProperty("transactions");
                    expected = expected ?? type.GetProperty("Transactions")?.GetValue(rawBars[i]);
                    Assert.AreEqual(expected, transactions);
                }
            }

            // GetDataFrame with argument of type IEnumerable<BaseData>
            var history = GetHistory(symbol, Resolution.Daily, rawBars);
            dataFrame = converter.GetDataFrame(history);

            using (Py.GIL())
            {
                Assert.IsFalse(dataFrame.empty.AsManagedObject(typeof(bool)));

                var subDataFrame = dataFrame.loc[symbol];
                Assert.IsFalse(subDataFrame.empty.AsManagedObject(typeof(bool)));

                var count = subDataFrame.__len__().AsManagedObject(typeof(int));
                Assert.AreEqual(10, count);

                for (var i = 0; i < count; i++)
                {
                    var index = subDataFrame.index[i];
                    var value = subDataFrame.loc[index].value.AsManagedObject(typeof(decimal));
                    Assert.AreEqual(rawBars[i].Value, value);
                    var transactions = subDataFrame.loc[index].transactions.AsManagedObject(typeof(decimal));
                    var expected = (rawBars[i] as DynamicData)?.GetProperty("transactions");
                    expected = expected ?? type.GetProperty("Transactions")?.GetValue(rawBars[i]);
                    Assert.AreEqual(expected, transactions);
                }
            }
        }

        [Test]
        [TestCase(typeof(SubTradeBar), "SubProperty")]
        [TestCase(typeof(SubSubTradeBar), "SubSubProperty")]
        public void HandlesCustomDataBarsInheritsFromTradeBar(Type type, string propertyName)
        {
            var converter = new PandasConverter();
            var symbol = Symbols.LTCUSD;

            var config = GetSubscriptionDataConfig<Quandl>(symbol, Resolution.Daily);
            dynamic custom = Activator.CreateInstance(type);

            var rawBars = Enumerable
                .Range(0, 10)
                .Select(i =>
                {
                    var line = $"{DateTime.UtcNow.AddDays(i).ToStringInvariant("yyyyMMdd HH:mm")},{i + 101},{i + 102},{i + 100},{i + 101},{i + 101}";
                    return custom.Reader(config, line, DateTime.UtcNow.AddDays(i), false) as BaseData;
                })
                .ToArray();

            // GetDataFrame with argument of type IEnumerable<BaseData>
            dynamic dataFrame = converter.GetDataFrame(rawBars);

            using (Py.GIL())
            {
                Assert.IsFalse(dataFrame.empty.AsManagedObject(typeof(bool)));

                var subDataFrame = dataFrame.loc[symbol];
                Assert.IsFalse(subDataFrame.empty.AsManagedObject(typeof(bool)));

                var count = subDataFrame.__len__().AsManagedObject(typeof(int));
                Assert.AreEqual(count, 10);

                for (var i = 0; i < count; i++)
                {
                    var index = subDataFrame.index[i];
                    var value = subDataFrame.loc[index].value.AsManagedObject(typeof(decimal));
                    Assert.AreEqual(rawBars[i].Value, value);

                    var transactions = subDataFrame.loc[index][propertyName.ToLowerInvariant()].AsManagedObject(typeof(decimal));
                    var expected = type.GetProperty(propertyName)?.GetValue(rawBars[i]);
                    Assert.AreEqual(expected, transactions);
                }
            }

            // GetDataFrame with argument of type IEnumerable<BaseData>
            var history = GetHistory(symbol, Resolution.Daily, rawBars);
            dataFrame = converter.GetDataFrame(history);

            using (Py.GIL())
            {
                Assert.IsFalse(dataFrame.empty.AsManagedObject(typeof(bool)));

                var subDataFrame = dataFrame.loc[symbol];
                Assert.IsFalse(subDataFrame.empty.AsManagedObject(typeof(bool)));

                var count = subDataFrame.__len__().AsManagedObject(typeof(int));
                Assert.AreEqual(10, count);

                for (var i = 0; i < count; i++)
                {
                    var index = subDataFrame.index[i];
                    var value = subDataFrame.loc[index].value.AsManagedObject(typeof(decimal));
                    Assert.AreEqual(rawBars[i].Value, value);
                }
            }
        }

        private static object[] SpotMarketCases => LeanDataReaderTests.SpotMarketCases;
        private static object[] OptionAndFuturesCases => LeanDataReaderTests.OptionAndFuturesCases;

        [Test, TestCaseSource(nameof(SpotMarketCases))]
        public void HandlesLeanDataReaderOutputForSpotMarkets(string securityType, string market, string resolution, string ticker, string fileName, int rowsInfile, double sumValue)
        {
            using (Py.GIL())
            {
                // Arrange
                var dataFolder = "../../../Data";
                var filepath = LeanDataReaderTests.GenerateFilepathForTesting(dataFolder, securityType, market, resolution, ticker, fileName);
                var leanDataReader = new LeanDataReader(filepath);
                var data = leanDataReader.Parse();
                var converter = new PandasConverter();
                // Act
                dynamic df = converter.GetDataFrame(data);
                // Assert
                Assert.AreEqual(rowsInfile, df.shape[0].AsManagedObject(typeof(int)));

                int columnsNumber = df.shape[1].AsManagedObject(typeof(int));
                if (columnsNumber == 3 || columnsNumber == 6)
                {
                    Assert.AreEqual(sumValue, df.get("lastprice").sum().AsManagedObject(typeof(double)), 1e-4);
                }
                else
                {
                    Assert.AreEqual(sumValue, df.get("close").sum().AsManagedObject(typeof(double)), 1e-4);
                }
            }
        }

        [Test, TestCaseSource(nameof(OptionAndFuturesCases))]
        public void HandlesLeanDataReaderOutputForOptionAndFutures(string composedFilePath, Symbol symbol, int rowsInfile, double sumValue)
        {
            using (Py.GIL())
            {
                // Arrange
                var leanDataReader = new LeanDataReader(composedFilePath);
                var data = leanDataReader.Parse();
                var converter = new PandasConverter();
                // Act
                dynamic df = converter.GetDataFrame(data);
                // Assert
                Assert.AreEqual(rowsInfile, df.shape[0].AsManagedObject(typeof(int)));

                int columnsNumber = df.shape[1].AsManagedObject(typeof(int));
                if (columnsNumber == 3 || columnsNumber == 6)
                {
                    Assert.AreEqual(sumValue, df.get("lastprice").sum().AsManagedObject(typeof(double)), 1e-4);
                }
                else if (columnsNumber == 1)
                {
                    Assert.AreEqual(sumValue, df.get("openinterest").sum().AsManagedObject(typeof(double)), 1e-4);
                }
                else
                {
                    Assert.AreEqual(sumValue, df.get("close").sum().AsManagedObject(typeof(double)), 1e-4);
                }
            }
        }

        public IEnumerable<Slice> GetHistory<T>(Symbol symbol, Resolution resolution, IEnumerable<T> data)
            where T : IBaseData
        {
            var subscriptionDataConfig = GetSubscriptionDataConfig<T>(symbol, resolution);
            var security = GetSecurity(subscriptionDataConfig);
            var timeSliceFactory = new TimeSliceFactory(TimeZones.Utc);
            return data.Select(t => timeSliceFactory.Create(
               t.Time,
               new List<DataFeedPacket> { new DataFeedPacket(security, subscriptionDataConfig, new List<BaseData>() { t as BaseData }) },
               new SecurityChanges(Enumerable.Empty<Security>(), Enumerable.Empty<Security>()),
                new Dictionary<Universe, BaseDataCollection>()).Slice);
        }

        private SubscriptionDataConfig GetSubscriptionDataConfig<T>(Symbol symbol, Resolution resolution)
        {
            return new SubscriptionDataConfig(
                typeof(T),
                symbol,
                resolution,
                TimeZones.Utc,
                TimeZones.Utc,
                true,
                true,
                false);
        }

        private Security GetSecurity(SubscriptionDataConfig subscriptionDataConfig)
        {
            return new Security(
                SecurityExchangeHours.AlwaysOpen(TimeZones.Utc),
                subscriptionDataConfig,
                new Cash(Currencies.USD, 0, 1m),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );
        }

        private dynamic GetTestDataFrame(Symbol symbol, int count = 1)
        {
            return GetTestDataFrame(new[] {symbol}, count);
        }

        private dynamic GetTestDataFrame(IEnumerable<Symbol> symbols, int count = 1)
        {
            var slices = Enumerable
                .Range(0, count)
                .Select(
                    i =>
                    {
                        var time = new DateTime(2013, 10, 7).AddMilliseconds(14400000 + i * 10000);
                        return new Slice(
                            time,
                            symbols.Select(
                                symbol => new Tick
                                {
                                    Time = time,
                                    Symbol = symbol,
                                    Value = 167 + i / 10,
                                    Quantity = 1 + i * 10,
                                    Exchange = "T"
                                }
                            )
                        );
                    }
                );
            return _converter.GetDataFrame(slices);
        }

        internal class SubTradeBar : TradeBar
        {
            public decimal SubProperty => Value;

            public SubTradeBar() { }

            public SubTradeBar(TradeBar tradeBar) : base(tradeBar) { }

            public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode) =>
                new SubTradeBar((TradeBar) base.Reader(config, line, date, isLiveMode));
        }

        internal class SubSubTradeBar : SubTradeBar
        {
            public decimal SubSubProperty => Value;

            public SubSubTradeBar() { }

            public SubSubTradeBar(TradeBar tradeBar) : base(tradeBar) { }

            public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode) =>
                new SubSubTradeBar((TradeBar) base.Reader(config, line, date, isLiveMode));
        }

        internal class NullableValueData : BaseData
        {
            public int? NullableInt { get; set; }

            public DateTime? NullableTime { get; set; }

            public double? NullableColumn { get; set; }
        }
    }
}