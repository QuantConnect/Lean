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
    public class PandasConverterTests
    {
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
        public void BackwardsCompatibilityDataFrame_ix_UsingTickerInCache()
        {
            using (Py.GIL())
            {
                SymbolCache.Set("SPY", Symbols.SPY);
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    @"
def Test(dataFrame, symbol):
    data = dataFrame['lastprice'].unstack(level=0).iloc[-1]['SPY']
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [Test]
        public void BackwardsCompatibilityDataFrame_iloc_UsingSymbol()
        {
            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    @"
def Test(dataFrame, symbol):
    data = dataFrame['lastprice'].unstack(level=0).iloc[-1][symbol]
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [Test]
        public void BackwardsCompatibilityDataFrame_iloc_UsingTickerInCache()
        {
            using (Py.GIL())
            {
                SymbolCache.Set("SPY", Symbols.SPY);
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    @"
def Test(dataFrame, symbol):
    data = dataFrame['lastprice'].unstack(level=0).ix[-1]['SPY']
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [Test]
        public void BackwardsCompatibilityDataFrame_ix_UsingSymbol()
        {
            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    @"
def Test(dataFrame, symbol):
    data = dataFrame['lastprice'].unstack(level=0).ix[-1][symbol]
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [Test]
        public void BackwardsCompatibilityDataFrame_concat_UsingTickerInCache()
        {
            using (Py.GIL())
            {
                SymbolCache.Set("SPY", Symbols.SPY);
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    @"
import pandas as pd

def Test(dataFrame, dataFrame2, symbol):
    newDataFrame = pd.concat([dataFrame, dataFrame2])
    data = newDataFrame['lastprice'].unstack(level=0).ix[-1]['SPY']
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), GetTestDataFrame(Symbols.AAPL), Symbols.SPY));
            }
        }

        [Test]
        public void BackwardsCompatibilityDataFrame_concat_UsingSymbol()
        {
            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    @"
import pandas as pd

def Test(dataFrame, dataFrame2, symbol):
    newDataFrame = pd.concat([dataFrame, dataFrame2])
    data = newDataFrame['lastprice'].unstack(level=0).ix[-1][symbol]
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), GetTestDataFrame(Symbols.AAPL), Symbols.SPY));
            }
        }

        [Test]
        public void BackwardsCompatibilityDataFrame_join_UsingTickerInCache()
        {
            using (Py.GIL())
            {
                SymbolCache.Set("SPY", Symbols.SPY);
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    @"
import pandas as pd

def Test(dataFrame, dataFrame2, symbol):
    newDataFrame = dataFrame.join(dataFrame2, lsuffix='_')
    data = newDataFrame['lastprice'].unstack(level=0).ix[-1]['SPY']
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), GetTestDataFrame(Symbols.AAPL), Symbols.SPY));
            }
        }

        [Test]
        public void BackwardsCompatibilityDataFrame_join_UsingSymbol()
        {
            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    @"
import pandas as pd

def Test(dataFrame, dataFrame2, symbol):
    newDataFrame = dataFrame.join(dataFrame2, lsuffix='_')
    data = newDataFrame['lastprice'].unstack(level=0).ix[-1][symbol]
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), GetTestDataFrame(Symbols.AAPL), Symbols.SPY));
            }
        }

        [Test]
        public void BackwardsCompatibilityDataFrame_append_UsingTickerInCache()
        {
            using (Py.GIL())
            {
                SymbolCache.Set("SPY", Symbols.SPY);
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    @"
import pandas as pd

def Test(dataFrame, dataFrame2, symbol):
    newDataFrame = dataFrame.append(dataFrame2)
    data = newDataFrame['lastprice'].unstack(level=0).ix[-1]['SPY']
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), GetTestDataFrame(Symbols.AAPL), Symbols.SPY));
            }
        }

        [Test]
        public void BackwardsCompatibilityDataFrame_append_UsingSymbol()
        {
            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    @"
import pandas as pd

def Test(dataFrame, dataFrame2, symbol):
    newDataFrame = dataFrame.append(dataFrame2)
    data = newDataFrame['lastprice'].unstack(level=0).ix[-1][symbol]
    if data is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), GetTestDataFrame(Symbols.AAPL), Symbols.SPY));
            }
        }

        [Test]
        public void BackwardsCompatibilityDataFrame_merge_UsingTickerInCache()
        {
            using (Py.GIL())
            {
                SymbolCache.Set("SPY", Symbols.SPY);
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    @"
import pandas as pd

def Test(dataFrame, dataFrame2, symbol):
    newDataFrame = dataFrame.merge(dataFrame2, on='symbol', how='outer')
    data = newDataFrame.loc['SPY']
    if len(data) is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), GetTestDataFrame(Symbols.AAPL), Symbols.SPY));
            }
        }

        [Test]
        public void BackwardsCompatibilityDataFrame_merge_UsingSymbol()
        {
            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    @"
import pandas as pd

def Test(dataFrame, dataFrame2, symbol):
    newDataFrame = dataFrame.merge(dataFrame2, on='symbol', how='outer')
    data = newDataFrame.loc[symbol]
    if len(data) is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), GetTestDataFrame(Symbols.AAPL), Symbols.SPY));
            }
        }

        [Test]
        public void DataFrame_loc_UsingSymbol()
        {
            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    @"
def Test(dataFrame, symbol):
    data = dataFrame.loc[symbol]").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [Test]
        public void DataFrame_unstack_UsingSymbol()
        {
            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    @"
def Test(dataFrame, symbol):
    data = dataFrame.unstack(level = 0).lastprice[symbol]").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [Test]
        public void DataFrame_get_UsingSymbol()
        {
            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    @"
def Test(dataFrame, symbol):
    data = dataFrame['lastprice'].unstack(level=0).get(symbol)
    if data.empty:
        raise Exception('Data is empty')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [Test]
        public void BackwardsCompatibilityDataFrame_get_NewWay()
        {
            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    @"
def Test(dataFrame, symbol):
    data = dataFrame['lastprice'].unstack(level=0).get(str(symbol.ID))
    if data.empty:
        raise Exception('Data is empty')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [Test]
        public void BackwardsCompatibilityDataFrame_get_UsingTickerInCache()
        {
            using (Py.GIL())
            {
                SymbolCache.Set("SPY", Symbols.SPY);
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    @"
def Test(dataFrame, symbol):
    data = dataFrame['lastprice'].unstack(level=0).get('SPY')
    if data.empty:
        raise Exception('Data is empty')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [Test]
        public void BackwardsCompatibilityDataFrame_get_OnPropertyUsingSymbol()
        {
            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    @"
def Test(dataFrame, symbol):
    data = dataFrame.lastprice.get(str(symbol))
    if data.empty:
        raise Exception('Data is empty')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [Test]
        public void BackwardsCompatibilityDataFrame_loc_NewWay()
        {
            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    @"
def Test(dataFrame, symbol):
    data = dataFrame.loc[str(symbol.ID)]").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [Test]
        public void BackwardsCompatibilityDataFrame_loc_SubDataFrame_NewWay()
        {
            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    @"
def Test(dataFrame, symbol):
    data = dataFrame.loc[str(symbol.ID)].loc['2013-10-07 04:00:00']").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [Test]
        public void BackwardsCompatibilityDataFrame_loc_OnPropertyNewWay()
        {
            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    @"
def Test(dataFrame, symbol):
    data = dataFrame.lastprice.loc[str(symbol.ID)]").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [Test]
        public void BackwardsCompatibilityDataFrame_loc_UsingTickerInCache()
        {
            using (Py.GIL())
            {
                SymbolCache.Set("SPY", Symbols.SPY);
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    @"
def Test(dataFrame, symbol):
    data = dataFrame.loc['SPY']").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [Test]
        public void BackwardsCompatibilityDataFrame_loc_OnPropertyUsingTickerInCache()
        {
            using (Py.GIL())
            {
                SymbolCache.Set("SPY", Symbols.SPY);
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    @"
def Test(dataFrame, symbol):
    data = dataFrame.lastprice.loc['SPY']").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [Test]
        public void BackwardsCompatibilityDataFrame_loc_UsingSymbol()
        {
            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    @"
def Test(dataFrame, symbol):
    data = dataFrame.loc[str(symbol)]").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [Test]
        public void BackwardsCompatibilityDataFrame_loc_OnPropertyUsingSymbol()
        {
            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    @"
def Test(dataFrame, symbol):
    data = dataFrame.lastprice.loc[str(symbol)]").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [Test]
        public void BackwardsCompatibilityDataFrame_at_NewWay()
        {
            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    @"
def Test(dataFrame, symbol):
    data = dataFrame.at[(str(symbol.ID),), 'lastprice']").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [Test]
        public void BackwardsCompatibilityDataFrame_at_UsingSymbol()
        {
            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    @"
def Test(dataFrame, symbol):
    data = dataFrame.at[(str(symbol),), 'lastprice']").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [Test]
        public void BackwardsCompatibilityDataFrame_at_UsingTickerInCache()
        {
            using (Py.GIL())
            {
                SymbolCache.Set("SPY", Symbols.SPY);
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    @"
def Test(dataFrame, symbol):
    data = dataFrame.at[('SPY',), 'lastprice']").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [Test]
        public void BackwardsCompatibilityDataFrame_xs_NewWay()
        {
            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    @"
def Test(dataFrame, symbol):
    data = dataFrame.xs(str(symbol.ID))").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [Test]
        public void BackwardsCompatibilityDataFrame_xs_UsingSymbol()
        {
            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    @"
def Test(dataFrame, symbol):
    data = dataFrame.xs(str(symbol))").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [Test]
        public void BackwardsCompatibilityDataFrame_xs_UsingTickerInCache()
        {
            using (Py.GIL())
            {
                SymbolCache.Set("SPY", Symbols.SPY);
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    @"
def Test(dataFrame, symbol):
    data = dataFrame.xs('SPY')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [Test]
        public void BackwardsCompatibilityDataFrame_loc_after_xs_NewWay()
        {
            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    @"
def Test(dataFrame, symbol):
    time = dataFrame.index.get_level_values('time')[0]
    dataFrame = dataFrame.xs(time, level='time')
    data = dataFrame.loc[str(symbol.ID)]").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [Test]
        public void BackwardsCompatibilityDataFrame_loc_after_xs_UsingSymbol()
        {
            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    @"
def Test(dataFrame, symbol):
    time = dataFrame.index.get_level_values('time')[0]
    dataFrame = dataFrame.xs(time, level='time')
    data = dataFrame.loc[str(symbol)]").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [Test]
        public void BackwardsCompatibilityDataFrame_loc_after_xs_UsingTickerInCache()
        {
            using (Py.GIL())
            {
                SymbolCache.Set("SPY", Symbols.SPY);
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    @"
def Test(dataFrame, symbol):
    time = dataFrame.index.get_level_values('time')[0]
    dataFrame = dataFrame.xs(time, level='time')
    data = dataFrame.loc['SPY']").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [Test]
        public void BackwardsCompatibilityDataFrame_unstack_get_NewWay()
        {
            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    @"
def Test(dataFrame, symbol):
    df2 = dataFrame.lastprice.unstack(level=0)
    data = df2.get(str(symbol.ID))").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [Test]
        public void BackwardsCompatibilityDataFrame_unstack_get_UsingSymbol()
        {
            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    @"
def Test(dataFrame, symbol):
    df2 = dataFrame.lastprice.unstack(level=0)
    data = df2.get(str(symbol))").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [Test]
        public void BackwardsCompatibilityDataFrame_unstack_get_UsingTickerInCache()
        {
            using (Py.GIL())
            {
                SymbolCache.Set("SPY", Symbols.SPY);
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    @"
def Test(dataFrame, symbol):
    df2 = dataFrame.lastprice.unstack(level=0)
    data = df2.get('SPY')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [Test]
        public void BackwardsCompatibilityDataFrame_unstack_NewWay()
        {
            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    @"
def Test(dataFrame, symbol):
    df2 = dataFrame.lastprice.unstack(level=0)
    data = df2[str(symbol.ID)]").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [Test]
        public void BackwardsCompatibilityDataFrame_unstack_UsingSymbol()
        {
            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    @"
def Test(dataFrame, symbol):
    df2 = dataFrame.lastprice.unstack(level=0)
    data = df2[str(symbol)]").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [Test]
        public void BackwardsCompatibilityDataFrame_unstack_UsingTickerInCache()
        {
            using (Py.GIL())
            {
                SymbolCache.Set("SPY", Symbols.SPY);
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    @"
def Test(dataFrame, symbol):
    df2 = dataFrame.lastprice.unstack(level=0)
    data = df2['SPY']").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [Test]
        public void BackwardsCompatibilityDataFrame_getitem_UsingTickerInCache()
        {
            using (Py.GIL())
            {
                SymbolCache.Set("SPY", Symbols.SPY);
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    @"
def Test(dataFrame, symbol):
    data = dataFrame['lastprice']['SPY']").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [Test]
        public void BackwardsCompatibilityDataFrame_getitem_UsingSymbol()
        {
            using (Py.GIL())
            {
                SymbolCache.Set("SPY", Symbols.SPY);
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    @"
def Test(dataFrame, symbol):
    data = dataFrame['lastprice'][str(symbol)]").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [Test]
        public void BackwardsCompatibilityDataFrame_getitem_NewWay()
        {
            using (Py.GIL())
            {
                SymbolCache.Set("SPY", Symbols.SPY);
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    @"
def Test(dataFrame, symbol):
    data = dataFrame['lastprice'][str(symbol.ID)]").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [Test]
        public void BackwardsCompatibilityDataFrame_index_levels_contains_ticker_inCache()
        {
            using (Py.GIL())
            {
                SymbolCache.Set("SPY", Symbols.SPY);
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    @"
def Test(dataFrame, symbol):
    if 'SPY' not in dataFrame.index.levels[0]:
        raise ValueError('SPY was not found')").GetAttr("Test");
                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [Test]
        public void BackwardsCompatibilityDataFrame_index_levels_contains_symbol_inCache()
        {
            using (Py.GIL())
            {
                SymbolCache.Set("SPY", Symbols.SPY);
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    @"
def Test(dataFrame, symbol):
    if str(symbol) not in dataFrame.index.levels[0]:
        raise ValueError('SPY was not found')").GetAttr("Test");
                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [Test]
        public void BackwardsCompatibilityDataFrame_index_levels_contains_symbol_notInCache()
        {
            using (Py.GIL())
            {
                dynamic test = PythonEngine.ModuleFromString("testModule",
                    @"
def Test(dataFrame, symbol):
    if str(symbol) not in dataFrame.index.levels[0]:
        raise ValueError('SPY was not found')").GetAttr("Test");
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

        private object[] SpotMarketCases => LeanDataReaderTests.SpotMarketCases;
        private object[] OptionAndFuturesCases => LeanDataReaderTests.OptionAndFuturesCases;

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

        private dynamic GetTestDataFrame(Symbol symbol)
        {
            var converter = new PandasConverter();
            var rawBars = Enumerable
                .Range(0, 1)
                .Select(i => new Tick(symbol, $"1440{i:D2}00,167{i:D2}00,1{i:D2},T,T,0", new DateTime(2013, 10, 7)))
                .ToArray();
            return converter.GetDataFrame(rawBars);
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
    }
}