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
using System.Linq;
using Python.Runtime;
using NUnit.Framework;
using QuantConnect.Research;
using System.Collections.Generic;
using QuantConnect.Data.Fundamental;
using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.Tests.Research
{
    [TestFixture]
    public class QuantBookSelectionTests
    {
        private QuantBook _qb;
        private DateTime _end;
        private DateTime _start;

        [SetUp]
        public void Setup()
        {
            _qb = new QuantBook();
            _end = new DateTime(2014, 4, 7);
            _start = new DateTime(2014, 3, 24);
        }

        [TestCase(Language.CSharp)]
        [TestCase(Language.Python)]
        public void UniverseSelectionData(Language language)
        {
            if (language == Language.CSharp)
            {
                var universe = _qb.AddUniverse((IEnumerable<Fundamental> fundamentals) => fundamentals.Select(x => x.Symbol));
                var history = _qb.UniverseHistory(universe, _start, _end).ToList();

                // we asked for 2 weeks, 5 work days for each week expected
                Assert.AreEqual(10, history.Count);
                Assert.IsTrue(history.All(x => x.Count() > 7000));
                Assert.IsTrue(history.All(x => x.All(fundamental => fundamental.GetType() == typeof(Fundamental))));
            }
            else
            {
                using (Py.GIL())
                {
                    var testModule = PyModule.FromString("testModule",
                    @"
from AlgorithmImports import *

def getUniverseHistory(qb, start, end):
    universe = qb.AddUniverse(lambda fundamentals: [ x.Symbol for x in fundamentals ])
" + GetBaseImplementation(expectedCount: 7000, identation: "    "));

                    dynamic getUniverse = testModule.GetAttr("getUniverseHistory");
                    var pyHistory = getUniverse(_qb, _start, _end);

                    Assert.AreEqual(10, pyHistory.__len__().AsManagedObject(typeof(int)));

                    for (var i = 0; i < 10; i++)
                    {
                        var index = pyHistory.index[i];
                        var type = typeof(List<Fundamental>);
                        var fundamental = (List<Fundamental>)pyHistory.loc[index].AsManagedObject(type);

                        Assert.GreaterOrEqual(fundamental.Count, 7000);
                    }
                }
            }
        }

        [TestCase(Language.CSharp, false)]
        [TestCase(Language.Python, false)]
        [TestCase(Language.CSharp, true)]
        [TestCase(Language.Python, true)]
        public void UniverseSelection(Language language, bool useUniverseUnchanged)
        {
            var selectionState = false;
            if (language == Language.CSharp)
            {
                var universe = _qb.AddUniverse((IEnumerable<Fundamental> fundamentals) =>
                {
                    if (!useUniverseUnchanged || !selectionState)
                    {
                        selectionState = true;
                        return new[] { Symbols.AAPL };
                    }
                    // after the first call we will return 'unchanged' if 'useUniverseUnchanged' is true
                    return Universe.Unchanged;
                });
                var history = _qb.UniverseHistory(universe, _start, _end).ToList();

                // we asked for 2 weeks, 5 work days for each week expected
                Assert.AreEqual(10, history.Count);
                Assert.IsTrue(history.All(x => x.Count() == 1));
                Assert.IsTrue(history.All(x => x.Single().Symbol == Symbols.AAPL));
                Assert.IsTrue(history.All(x => x.All(fundamental => fundamental.GetType() == typeof(Fundamental))));
            }
            else
            {
                using (Py.GIL())
                {
                    dynamic testModule = PyModule.FromString("testModule",
                    @"
from AlgorithmImports import *

class Test():
    def __init__(self, useUniverseUnchanged):
        self.useUniverseUnchanged = useUniverseUnchanged
        self.state = False

    def selection(self, fundamentals):
        if not self.useUniverseUnchanged or not self.state:
            self.state = True
            return [ x.Symbol for x in fundamentals if x.Symbol.Value == ""AAPL"" ]
        return Universe.Unchanged

    def getUniverseHistory(self, qb, start, end):
        universe = qb.AddUniverse(self.selection)
" + GetBaseImplementation(expectedCount: 1, identation: "        ")).GetAttr("Test");

                    var instance = testModule(useUniverseUnchanged);
                    var pyHistory = instance.getUniverseHistory(_qb, _start, _end);

                    Assert.AreEqual(10, pyHistory.__len__().AsManagedObject(typeof(int)));

                    for (var i = 0; i < 10; i++)
                    {
                        var index = pyHistory.index[i];
                        var series = pyHistory.loc[index];
                        var type = typeof(Fundamental[]);
                        var fundamental = (Fundamental[])series.AsManagedObject(type);

                        Assert.GreaterOrEqual(fundamental.Length, 1);
                        Assert.AreEqual(Symbols.AAPL, fundamental[0].Symbol);
                    }
                }
            }
        }

        [TestCase(Language.CSharp)]
        [TestCase(Language.Python)]
        public void UniverseSelectionEtf(Language language)
        {
            _start = new DateTime(2020, 12, 1);
            _end = new DateTime(2021, 1, 31);
            var selectionState = false;
            if (language == Language.CSharp)
            {
                var spy = Symbol.Create("SPY", SecurityType.Equity, Market.USA);
                var universe = _qb.Universe.ETF(spy, _qb.UniverseSettings, (IEnumerable<ETFConstituentUniverse> etfConstituents) =>
                {
                    if (!selectionState)
                    {
                        selectionState = true;
                        return new[] { Symbols.AAPL };
                    }
                    // after the first call we will return 'unchanged' if 'useUniverseUnchanged' is true
                    return Universe.Unchanged;
                });
                var history = _qb.UniverseHistory(universe, _start, _end).ToList();

                // we asked for 2 weeks, 5 work days for each week expected
                Assert.AreEqual(41, history.Count);
                Assert.IsTrue(history.All(x => x.Count() == 1));
                Assert.IsTrue(history.All(x => x.Single().Symbol == Symbols.AAPL));
                Assert.IsTrue(history.All(x => x.All(fundamental => fundamental.GetType() == typeof(ETFConstituentUniverse))));
            }
            else
            {
                using (Py.GIL())
                {
                    dynamic testModule = PyModule.FromString("testModule",
                    @"
from AlgorithmImports import *

class Test():
    def __init__(self):
        self.state = False

    def selection(self, etfConstituents):
        if not self.state:
            self.state = True
            return [ x.Symbol for x in etfConstituents if x.Symbol.Value == ""AAPL"" ]
        return Universe.Unchanged

    def getUniverseHistory(self, qb, start, end):
        universe = qb.AddUniverse(qb.Universe.ETF(""SPY"", Market.USA, qb.UniverseSettings, self.selection))
        universeDataPerTime = qb.UniverseHistory(universe, start, end)
        for universeDataCollection in universeDataPerTime:
            dataPointCount = 0
            for etfConstituent in universeDataCollection:
                dataPointCount += 1
                if type(etfConstituent) is not ETFConstituentUniverse:
                    raise ValueError(f""Unexpected data type {type(etfConstituent)}! {str(ETFConstituentUniverse)}"")
            if dataPointCount < 1:
                raise ValueError(f""Unexpected historical Fundamentals data count {dataPointCount}! Expected > expectedCount"")
        return universeDataPerTime
").GetAttr("Test");

                    var instance = testModule();
                    var pyHistory = instance.getUniverseHistory(_qb, _start, _end);

                    Assert.AreEqual(41, pyHistory.__len__().AsManagedObject(typeof(int)));

                    for (var i = 0; i < 41; i++)
                    {
                        var index = pyHistory.index[i];
                        var series = pyHistory.loc[index];
                        var type = typeof(ETFConstituentUniverse[]);
                        var etfConstituent = (ETFConstituentUniverse[])series.AsManagedObject(type);

                        Assert.GreaterOrEqual(etfConstituent.Length, 1);
                        Assert.AreEqual(Symbols.AAPL, etfConstituent[0].Symbol);
                    }
                }
            }
        }

        [TestCase(Language.CSharp)]
        [TestCase(Language.Python)]
        public void UniverseSelectionData_BackwardsCompatibility(Language language)
        {
            if (language == Language.CSharp)
            {
                var history = _qb.UniverseHistory<Fundamentals, Fundamental>(_start, _end).ToList();

                // we asked for 2 weeks, 5 work days for each week expected
                Assert.AreEqual(10, history.Count);
                Assert.IsTrue(history.All(x => x.Count() > 7000));
                Assert.IsTrue(history.All(x => x.All(fundamental => fundamental.GetType() == typeof(Fundamental))));
            }
            else
            {
                using (Py.GIL())
                {
                    var testModule = PyModule.FromString("testModule",
                    @"
from AlgorithmImports import *

def getUniverseHistory(qb, start, end):
    return qb.UniverseHistory(Fundamentals, start, end)
                    ");

                    dynamic getUniverse = testModule.GetAttr("getUniverseHistory");
                    var pyHistory = getUniverse(_qb, _start, _end);

                    Assert.AreEqual(10, pyHistory.__len__().AsManagedObject(typeof(int)));

                    for (var i = 0; i < 10; i++)
                    {
                        var index = pyHistory.index[i];
                        var type = typeof(List<Fundamental>);
                        var fundamental = (List<Fundamental>)pyHistory.loc[index].AsManagedObject(type);

                        Assert.GreaterOrEqual(fundamental.Count, 7000);
                    }
                }
            }
        }

        [TestCase(Language.CSharp, false)]
        [TestCase(Language.Python, false)]
        [TestCase(Language.CSharp, true)]
        [TestCase(Language.Python, true)]
        public void UniverseSelection_BackwardsCompatibility(Language language, bool useUniverseUnchanged)
        {
            var selectionState = false;
            if (language == Language.CSharp)
            {
                var history = _qb.UniverseHistory<Fundamentals, Fundamental>(_start, _end, (fundamental) =>
                {
                    if (!useUniverseUnchanged || !selectionState)
                    {
                        selectionState = true;
                        return new[] { Symbols.AAPL };
                    }
                    // after the first call we will return 'unchanged' if 'useUniverseUnchanged' is true
                    return Universe.Unchanged;
                }).ToList();

                // we asked for 2 weeks, 5 work days for each week expected
                Assert.AreEqual(10, history.Count);
                Assert.IsTrue(history.All(x => x.Count() == 1));
                Assert.IsTrue(history.All(x => x.Single().Symbol == Symbols.AAPL));
                Assert.IsTrue(history.All(x => x.All(fundamental => fundamental.GetType() == typeof(Fundamental))));
            }
            else
            {
                using (Py.GIL())
                {
                    dynamic testModule = PyModule.FromString("testModule",
                    @"
from AlgorithmImports import *

class Test():
    def __init__(self, useUniverseUnchanged):
        self.useUniverseUnchanged = useUniverseUnchanged
        self.state = False

    def selection(self, fundamentals):
        if not self.useUniverseUnchanged or not self.state:
            self.state = True
            return [ x.Symbol for x in fundamentals if x.Symbol.Value == ""AAPL"" ]
        return Universe.Unchanged

    def getUniverseHistory(self, qb, start, end):
        return qb.UniverseHistory(Fundamentals, start, end, self.selection)
").GetAttr("Test");

                    var instance = testModule(useUniverseUnchanged);
                    var pyHistory = instance.getUniverseHistory(_qb, _start, _end);

                    Assert.AreEqual(10, pyHistory.__len__().AsManagedObject(typeof(int)));

                    for (var i = 0; i < 10; i++)
                    {
                        var index = pyHistory.index[i];
                        var series = pyHistory.loc[index];
                        var type = typeof(Fundamental[]);
                        var fundamental = (Fundamental[])series.AsManagedObject(type);

                        Assert.GreaterOrEqual(fundamental.Length, 1);
                        Assert.AreEqual(Symbols.AAPL, fundamental[0].Symbol);
                    }
                }
            }
        }

        private static string GetBaseImplementation(int expectedCount, string identation)
        {
            return @"
{identation}universeDataPerTime = qb.UniverseHistory(universe, start, end)
{identation}for universeDataCollection in universeDataPerTime:
{identation}    dataPointCount = 0
{identation}    for fundamental in universeDataCollection:
{identation}        dataPointCount += 1
{identation}        if type(fundamental) is not Fundamental:
{identation}            raise ValueError(f""Unexpected Fundamentals data type {type(fundamental)}! {str(fundamental)}"")
{identation}    if dataPointCount < expectedCount:
{identation}        raise ValueError(f""Unexpected historical Fundamentals data count {dataPointCount}! Expected > expectedCount"")
{identation}return universeDataPerTime
".Replace("expectedCount", expectedCount.ToStringInvariant(), StringComparison.InvariantCulture)
.Replace("{identation}", identation, StringComparison.InvariantCulture);
        }
    }
}
