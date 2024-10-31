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
using QuantConnect.Scheduling;

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
            _end = new DateTime(2014, 4, 22);
            _start = new DateTime(2014, 3, 24);
        }

        [TestCase(Language.CSharp)]
        [TestCase(Language.Python, true)]
        [TestCase(Language.Python, false)]
        public void UniverseSelectionData(Language language, bool flatten = false)
        {
            if (language == Language.CSharp)
            {
                var universe = _qb.AddUniverse((IEnumerable<Fundamental> fundamentals) => fundamentals.Select(x => x.Symbol));
                var history = _qb.UniverseHistory(universe, _start, _end).ToList();

                // we asked for 4 weeks, 5 work days for each week expected
                Assert.AreEqual(20, history.Count);
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
" + GetBaseImplementation(expectedCount: 7000, identation: "    ", flatten: flatten));

                    dynamic getUniverse = testModule.GetAttr("getUniverseHistory");
                    var pyHistory = getUniverse(_qb, _start, _end);

                    Console.WriteLine((string)pyHistory.to_string());

                    if (flatten)
                    {
                        Assert.AreEqual(20, pyHistory.index.levels[0].__len__().AsManagedObject(typeof(int)));

                        foreach (var date in pyHistory.index.levels[0])
                        {
                            var fundamentalDataCount = pyHistory.loc[date].shape[0].AsManagedObject(typeof(int));
                            Assert.GreaterOrEqual(fundamentalDataCount, 7000);
                        }
                    }
                    else
                    {
                        Assert.AreEqual(20, pyHistory.__len__().AsManagedObject(typeof(int)));

                        for (var i = 0; i < 20; i++)
                        {
                            var index = pyHistory.index[i];
                            var type = typeof(List<Fundamental>);
                            var fundamental = (List<Fundamental>)pyHistory.loc[index].AsManagedObject(type);

                            Assert.GreaterOrEqual(fundamental.Count, 7000);
                        }
                    }
                }
            }
        }

        [TestCase(Language.CSharp, false)]
        [TestCase(Language.Python, false, true)]
        [TestCase(Language.Python, false, false)]
        [TestCase(Language.CSharp, true)]
        [TestCase(Language.Python, true, true)]
        [TestCase(Language.Python, true, false)]
        public void UniverseSelection(Language language, bool useUniverseUnchanged, bool flatten = false)
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

                // we asked for 4 weeks, 5 work days for each week expected
                Assert.AreEqual(20, history.Count);
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
        universe = qb.add_universe(self.selection)
" + GetBaseImplementation(expectedCount: 1, identation: "        ", flatten: flatten)).GetAttr("Test");

                    var instance = testModule(useUniverseUnchanged);
                    var pyHistory = instance.getUniverseHistory(_qb, _start, _end);

                    if (flatten)
                    {
                        Assert.AreEqual(20, pyHistory.index.levels[0].__len__().AsManagedObject(typeof(int)));

                        foreach (var date in pyHistory.index.levels[0])
                        {
                            var fundamentalData = pyHistory.loc[date];
                            var fundamentalDataCount = fundamentalData.shape[0].AsManagedObject(typeof(int));

                            Assert.GreaterOrEqual(fundamentalDataCount, 1);
                            Assert.AreEqual(Symbols.AAPL, fundamentalData.index[0].AsManagedObject(typeof(Symbol)));
                        }

                    }
                    else
                    {
                        Assert.AreEqual(20, pyHistory.__len__().AsManagedObject(typeof(int)));

                        for (var i = 0; i < 20; i++)
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
        }

        [TestCase(Language.CSharp)]
        [TestCase(Language.Python, true)]
        [TestCase(Language.Python, false)]
        public void UniverseSelectionWithDateRule(Language language, bool flatten = false)
        {
            if (language == Language.CSharp)
            {
                var universe = _qb.AddUniverse((IEnumerable<Fundamental> fundamentals) =>
                {
                    return new[] { Symbols.AAPL };
                });
                var history = _qb.UniverseHistory(universe, _start, _end, _qb.DateRules.WeekEnd()).ToList();

                Assert.AreEqual(4, history.Count);
                Assert.IsTrue(history.All(x => x.Count() == 1));
                Assert.IsTrue(history.All(x => x.Single().Symbol == Symbols.AAPL));
                Assert.IsTrue(history.All(x => x.All(fundamental => fundamental.GetType() == typeof(Fundamental))));
            }
            else
            {
                using (Py.GIL())
                {
                    dynamic testModule = PyModule.FromString("testModule", @"
from AlgorithmImports import *

class Test():
    def selection(self, fundamentals):
        return [ x.Symbol for x in fundamentals if x.Symbol.Value == ""AAPL"" ]

    def getUniverseHistory(self, qb, start, end, flatten):
        universe = qb.add_universe(self.selection)
        universeDataPerTime = qb.universe_history(universe, start, end, date_rule = qb.date_rules.week_end(), flatten=flatten)

        if flatten:
            for date in universeDataPerTime.index.levels[0]:
                dateUniverseData = universeDataPerTime.loc[date]
                dataPointCount = dateUniverseData.shape[0]
                if dataPointCount < 1:
                    raise ValueError(f'Unexpected historical Fundamentals data count {dataPointCount}! Expected > 0')
        else:
            for universeDataCollection in universeDataPerTime:
                dataPointCount = 0
                for fundamental in universeDataCollection:
                    dataPointCount += 1
                    if type(fundamental) is not Fundamental:
                        raise ValueError(f""Unexpected Fundamentals data type {type(fundamental)}! {str(fundamental)}"")
                if dataPointCount < 1:
                    raise ValueError(f""Unexpected historical Fundamentals data count {dataPointCount}! Expected > expectedCount"")

        return universeDataPerTime
").GetAttr("Test");

                    var instance = testModule();
                    var pyHistory = instance.getUniverseHistory(_qb, _start, _end, flatten);

                    if (flatten)
                    {
                        Assert.AreEqual(4, pyHistory.index.levels[0].__len__().AsManagedObject(typeof(int)));

                        foreach (var date in pyHistory.index.levels[0])
                        {
                            var fundamentalData = pyHistory.loc[date];
                            var fundamentalDataCount = fundamentalData.shape[0].AsManagedObject(typeof(int));

                            Assert.GreaterOrEqual(fundamentalDataCount, 1);
                            Assert.AreEqual(Symbols.AAPL, fundamentalData.index[0].AsManagedObject(typeof(Symbol)));
                        }
                    }
                    else
                    {
                        Assert.AreEqual(4, pyHistory.__len__().AsManagedObject(typeof(int)));

                        for (var i = 0; i < 4; i++)
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
        }

        [TestCase(Language.CSharp)]
        [TestCase(Language.Python, true)]
        [TestCase(Language.Python, false)]
        public void UniverseSelectionEtf(Language language, bool flatten = false)
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

    def getUniverseHistory(self, qb, start, end, flatten):
        universe = qb.add_universe(qb.universe.etf(""SPY"", Market.USA, qb.universe_settings, self.selection))
        universeDataPerTime = qb.universe_history(universe, start, end, flatten=flatten)

        if flatten:
            for date in universeDataPerTime.index.levels[0]:
                dateUniverseData = universeDataPerTime.loc[date]
                dataPointCount = dateUniverseData.shape[0]
                if dataPointCount < 1:
                    raise ValueError(f""Unexpected historical Fundamentals data count {dataPointCount}! Expected > 0"")
        else:
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
                    var pyHistory = instance.getUniverseHistory(_qb, _start, _end, flatten);

                    if (flatten)
                    {
                        Assert.AreEqual(41, pyHistory.index.levels[0].__len__().AsManagedObject(typeof(int)));

                        foreach (var date in pyHistory.index.levels[0])
                        {
                            var fundamentalData = pyHistory.loc[date];
                            var fundamentalDataCount = fundamentalData.shape[0].AsManagedObject(typeof(int));

                            Assert.GreaterOrEqual(fundamentalDataCount, 1);
                            Assert.AreEqual(Symbols.AAPL, fundamentalData.index[0].AsManagedObject(typeof(Symbol)));
                        }
                    }
                    else
                    {
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
        }

        [TestCase(Language.CSharp)]
        [TestCase(Language.Python, true)]
        [TestCase(Language.Python, false)]
        public void UniverseSelectionData_BackwardsCompatibility(Language language, bool flatten = false)
        {
            if (language == Language.CSharp)
            {
                var history = _qb.UniverseHistory<Fundamentals, Fundamental>(_start, _end).ToList();

                // we asked for 4 weeks, 5 work days for each week expected
                Assert.AreEqual(20, history.Count);
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

def getUniverseHistory(qb, start, end, flatten):
    return qb.universe_history(Fundamentals, start, end, flatten=flatten)
                    ");

                    dynamic getUniverse = testModule.GetAttr("getUniverseHistory");
                    var pyHistory = getUniverse(_qb, _start, _end, flatten);

                    if (flatten)
                    {
                        Assert.AreEqual(20, pyHistory.index.levels[0].__len__().AsManagedObject(typeof(int)));

                        foreach (var date in pyHistory.index.levels[0])
                        {
                            var fundamentalDataCount = pyHistory.loc[date].shape[0].AsManagedObject(typeof(int));
                            Assert.GreaterOrEqual(fundamentalDataCount, 7000);
                        }
                    }
                    else
                    {
                        Assert.AreEqual(20, pyHistory.__len__().AsManagedObject(typeof(int)));

                        for (var i = 0; i < 20; i++)
                        {
                            var index = pyHistory.index[i];
                            var type = typeof(List<Fundamental>);
                            var fundamental = (List<Fundamental>)pyHistory.loc[index].AsManagedObject(type);

                            Assert.GreaterOrEqual(fundamental.Count, 7000);
                        }
                    }
                }
            }
        }

        [TestCase(Language.CSharp, false)]
        [TestCase(Language.Python, false, true)]
        [TestCase(Language.Python, false, false)]
        [TestCase(Language.CSharp, true)]
        [TestCase(Language.Python, true, true)]
        [TestCase(Language.Python, true, false)]
        public void UniverseSelection_BackwardsCompatibility(Language language, bool useUniverseUnchanged, bool flatten = false)
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

                // we asked for 4 weeks, 5 work days for each week expected
                Assert.AreEqual(20, history.Count);
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

    def getUniverseHistory(self, qb, start, end, flatten):
        return qb.universe_history(Fundamentals, start, end, self.selection, flatten=flatten)
").GetAttr("Test");

                    var instance = testModule(useUniverseUnchanged);
                    var pyHistory = instance.getUniverseHistory(_qb, _start, _end, flatten);

                    if (flatten)
                    {
                        Assert.AreEqual(20, pyHistory.index.levels[0].__len__().AsManagedObject(typeof(int)));

                        foreach (var date in pyHistory.index.levels[0])
                        {
                            var fundamentalData = pyHistory.loc[date];
                            var fundamentalDataCount = fundamentalData.shape[0].AsManagedObject(typeof(int));

                            Assert.GreaterOrEqual(fundamentalDataCount, 1);
                            Assert.AreEqual(Symbols.AAPL, fundamentalData.index[0].AsManagedObject(typeof(Symbol)));
                        }
                    }
                    else
                    {
                        Assert.AreEqual(20, pyHistory.__len__().AsManagedObject(typeof(int)));

                        for (var i = 0; i < 20; i++)
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
        }

        [TestCase(Language.CSharp)]
        [TestCase(Language.Python, true)]
        [TestCase(Language.Python, false)]
        public void GenericUniverseSelectionIsCompatibleWithDateRule(Language language, bool flatten = false)
        {
            if (language == Language.CSharp)
            {
                var history = _qb.UniverseHistory<Fundamentals, Fundamental>(_start, _end, (fundamental) =>
                {
                    return new[] { Symbols.AAPL };
                }, _qb.DateRules.WeekEnd()).ToList();

                // we asked for 4 weeks, 5 work days for each week expected
                Assert.AreEqual(4, history.Count);
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
    def selection(self, fundamentals):
        return [ x.Symbol for x in fundamentals if x.Symbol.Value == ""AAPL"" ]

    def getUniverseHistory(self, qb, start, end, flatten):
        return qb.universe_history(Fundamentals, start, end, self.selection, date_rule = qb.date_rules.week_end(), flatten=flatten)
").GetAttr("Test");

                    var instance = testModule();
                    var pyHistory = instance.getUniverseHistory(_qb, _start, _end, flatten);

                    if (flatten)
                    {
                        Assert.AreEqual(4, pyHistory.index.levels[0].__len__().AsManagedObject(typeof(int)));

                        foreach (var date in pyHistory.index.levels[0])
                        {
                            var fundamentalData = pyHistory.loc[date];
                            var fundamentalDataCount = fundamentalData.shape[0].AsManagedObject(typeof(int));

                            Assert.GreaterOrEqual(fundamentalDataCount, 1);
                            Assert.AreEqual(Symbols.AAPL, fundamentalData.index[0].AsManagedObject(typeof(Symbol)));
                        }

                        Assert.AreEqual(4, pyHistory.__len__().AsManagedObject(typeof(int)));
                    }
                    else
                    {
                        Assert.AreEqual(4, pyHistory.__len__().AsManagedObject(typeof(int)));

                        for (var i = 0; i < 4; i++)
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
        }

        [Test]
        public void MonthlyEndGenericUniverseSelectionWorksAsExpected()
        {
            var history = _qb.UniverseHistory<Fundamentals, Fundamental>(
                new DateTime(2014, 3, 24),
                new DateTime(2014, 4, 7),
                (fundamental) =>
                {
                    return new[] { Symbols.AAPL };
                },
                _qb.DateRules.MonthEnd(Symbols.AAPL)).ToList();
            var lastDayOfMonth = history.Select(x => x.First()).Select(x => x.EndTime).First();
            Assert.IsNotNull(lastDayOfMonth);
            Assert.AreEqual(new DateTime(2014, 3, 29), lastDayOfMonth);
        }

        [Test]
        public void MonthlyStartGenericUniverseSelectionWorksAsExpected()
        {
            var history = _qb.UniverseHistory<Fundamentals, Fundamental>(
                new DateTime(2014, 3, 24),
                new DateTime(2014, 4, 7),
                (fundamental) =>
                {
                    return new[] { Symbols.AAPL };
                },
                _qb.DateRules.MonthStart(Symbols.AAPL)).ToList();
            var firstDayOfMonth = history.Select(x => x.First()).Select(x => x.EndTime).First();
            Assert.IsNotNull(firstDayOfMonth);
            Assert.AreEqual(new DateTime(2014, 4, 1), firstDayOfMonth);
        }

        [Test]
        public void MonthlyEndSelectionWorksAsExpected()
        {
            var universe = _qb.AddUniverse((IEnumerable<Fundamental> fundamentals) =>
            {
                return new[] { Symbols.AAPL };
            });
            var history = _qb.UniverseHistory(universe, new DateTime(2014, 3, 15), new DateTime(2014, 4, 7), _qb.DateRules.MonthEnd(Symbols.AAPL)).ToList();
            var lastDayOfMonth = history.Select(x => x.First()).Select(x => x.EndTime).First();
            Assert.IsNotNull(lastDayOfMonth);
            Assert.AreEqual(new DateTime(2014, 3, 29), lastDayOfMonth);
        }

        [Test]
        public void MonthlyStartSelectionWorksAsExpected()
        {
            var universe = _qb.AddUniverse((IEnumerable<Fundamental> fundamentals) =>
            {
                return new[] { Symbols.AAPL };
            });
            var history = _qb.UniverseHistory(universe, new DateTime(2014, 3, 15), new DateTime(2014, 4, 7), _qb.DateRules.MonthStart(Symbols.AAPL)).ToList();
            var firstDayOfMonth = history.Select(x => x.First()).Select(x => x.EndTime).First();
            Assert.IsNotNull(firstDayOfMonth);
            Assert.AreEqual(new DateTime(2014, 4, 1), firstDayOfMonth);
        }

        [Test]
        public void WeekendGenericUniverseSelectionWorksAsExpected()
        {
            var history = _qb.UniverseHistory<Fundamentals, Fundamental>(
                new DateTime(2014, 3, 24),
                new DateTime(2014, 4, 7),
                (fundamental) =>
                {
                    return new[] { Symbols.AAPL };
                },
                _qb.DateRules.Every(DayOfWeek.Wednesday)).ToList();
            var dates = history.Select(x => x.First()).Select(x => x.EndTime).ToList();
            Assert.IsNotNull(dates);
            Assert.IsTrue(dates.All(x => x.DayOfWeek == DayOfWeek.Wednesday));
        }

        [Test]
        public void PythonMonthlyStartGenericUniverseSelectionWorksAsExpected([Values] bool flatten)
        {
            using (Py.GIL())
            {
                dynamic testModule = PyModule.FromString("testModule",
                @"
from AlgorithmImports import *

class Test():
    def selection(self, fundamentals):
        return [ x.Symbol for x in fundamentals if x.Symbol.Value == ""AAPL"" ]

    def getUniverseHistory(self, qb, start, end, symbol, flatten):
        return qb.universe_history(Fundamentals, start, end, self.selection, date_rule = qb.date_rules.month_start(symbol), flatten=flatten)
").GetAttr("Test");

                var instance = testModule();
                var pyHistory = instance.getUniverseHistory(_qb, new DateTime(2014, 3, 24), new DateTime(2014, 4, 7), Symbols.AAPL, flatten);
                Assert.AreEqual(1, pyHistory.__len__().AsManagedObject(typeof(int)));

                var firstDayOfTheMonth = pyHistory.index[0][flatten ? 0 : 1].AsManagedObject(typeof(DateTime));
                Assert.AreEqual(new DateTime(2014, 4, 1), firstDayOfTheMonth);
            }
        }


        [Test]
        public void PythonMonthlyEndGenericUniverseSelectionWorksAsExpected([Values] bool flatten)
        {
            using (Py.GIL())
            {
                dynamic testModule = PyModule.FromString("testModule",
                @"
from AlgorithmImports import *

class Test():
    def selection(self, fundamentals):
        return [ x.Symbol for x in fundamentals if x.Symbol.Value == ""AAPL"" ]

    def getUniverseHistory(self, qb, start, end, symbol, flatten):
        return qb.universe_history(Fundamentals, start, end, self.selection, date_rule = qb.date_rules.month_end(symbol), flatten=flatten)
").GetAttr("Test");

                var instance = testModule();
                var pyHistory = instance.getUniverseHistory(_qb, new DateTime(2014, 3, 24), new DateTime(2014, 4, 7), Symbols.AAPL, flatten);
                Assert.AreEqual(1, pyHistory.__len__().AsManagedObject(typeof(int)));

                var firstDayOfTheMonth = (pyHistory.index[0][flatten ? 0 : 1]).AsManagedObject(typeof(DateTime));
                Assert.AreEqual(new DateTime(2014, 3, 29), firstDayOfTheMonth);
            }
        }

        [Test]
        public void PythonDailyGenericUniverseSelectionWorksAsExpected([Values] bool flatten)
        {
            using (Py.GIL())
            {
                dynamic testModule = PyModule.FromString("testModule",
                @"
from AlgorithmImports import *

class Test():
    def selection(self, fundamentals):
        return [ x.Symbol for x in fundamentals if x.Symbol.Value == ""AAPL"" ]

    def getUniverseHistory(self, qb, start, end, symbol, flatten):
        return qb.universe_history(Fundamentals, start, end, self.selection, date_rule = qb.date_rules.every_day(), flatten=flatten)
").GetAttr("Test");

                var instance = testModule();
                var pyHistory = instance.getUniverseHistory(_qb, new DateTime(2014, 3, 24), new DateTime(2014, 4, 7), Symbols.AAPL, flatten);

                if (flatten)
                {
                    Assert.AreEqual(10, pyHistory.index.levels[0].__len__().AsManagedObject(typeof(int)));

                    foreach (var date in pyHistory.index.levels[0])
                    {
                        var fundamentalData = pyHistory.loc[date];
                        var fundamentalDataCount = fundamentalData.shape[0].AsManagedObject(typeof(int));

                        Assert.GreaterOrEqual(fundamentalDataCount, 1);
                        Assert.AreEqual(Symbols.AAPL, fundamentalData.index[0].AsManagedObject(typeof(Symbol)));
                    }
                }
                else
                {
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

        [Test]
        public void PythonWeekendGenericUniverseSelectionWorksAsExpected([Values] bool flatten)
        {
            using (Py.GIL())
            {
                dynamic testModule = PyModule.FromString("testModule",
                @"
from AlgorithmImports import *
from datetime import datetime

class Test():
    def selection(self, fundamentals):
        return [ x.Symbol for x in fundamentals if x.Symbol.Value == ""AAPL"" ]

    def getUniverseHistory(self, qb, start, end, symbol, flatten):
        return qb.universe_history(Fundamentals, start, end, self.selection,
            date_rule=qb.date_rules.on(datetime(2014, 3, 30), datetime(2014, 3, 31), datetime(2014, 4, 1)),
            flatten=flatten)
").GetAttr("Test");

                var instance = testModule();
                var pyHistory = instance.getUniverseHistory(_qb, new DateTime(2014, 3, 24), new DateTime(2014, 4, 7), Symbols.AAPL, flatten);

                if (flatten)
                {
                    Assert.AreEqual(2, pyHistory.index.levels[0].__len__().AsManagedObject(typeof(int)));

                    foreach (var date in pyHistory.index.levels[0])
                    {
                        var fundamentalData = pyHistory.loc[date];
                        var fundamentalDataCount = fundamentalData.shape[0].AsManagedObject(typeof(int));

                        Assert.GreaterOrEqual(fundamentalDataCount, 1);
                        Assert.AreEqual(Symbols.AAPL, fundamentalData.index[0].AsManagedObject(typeof(Symbol)));
                    }
                }
                else
                {
                    Assert.AreEqual(2, pyHistory.__len__().AsManagedObject(typeof(int)));

                    for (var i = 0; i < 2; i++)
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

        [Test]
        public void PerformSelectionDoesNotSkipDataPointWhenPreviousDataPointIsYielded()
        {
            var historyDataPoints = new List<BaseDataCollection>()
            {
                new BaseDataCollection(new DateTime(2024, 10, 14), Symbols.AAPL),
                new BaseDataCollection(new DateTime(2024, 10, 15), Symbols.AAPL),
                new BaseDataCollection(new DateTime(2024, 10, 17), Symbols.AAPL),
                new BaseDataCollection(new DateTime(2024, 10, 22), Symbols.AAPL),
            };

            var dateRule = _qb.DateRules.On(new DateTime(2024, 10, 14), new DateTime(2024, 10, 16), new DateTime(2024, 10, 18));
            var selectedDates = QuantBookTestClass.PerformSelection(historyDataPoints, new DateTime(2024, 10, 14), new DateTime(2024, 10, 22), dateRule).Select(x => x.EndTime).ToList();

            for (int index = 0; index < 3; index++)
            {
                Assert.AreEqual(historyDataPoints[index].EndTime, selectedDates[index]);
            }
        }

        private static string GetBaseImplementation(int expectedCount, string identation, bool flatten = true)
        {
            if (flatten)
            {
                return @"
{identation}universe_data_df = qb.universe_history(universe, start, end, flatten=True)
{identation}for date in universe_data_df.index.levels[0]:
{identation}    dateUniverseData = universe_data_df.loc[date]
{identation}    dataPointCount = dateUniverseData.shape[0]
{identation}    if dataPointCount < expectedCount:
{identation}        raise ValueError(f""Unexpected historical Fundamentals data count {dataPointCount}! Expected > expectedCount"")
{identation}return universe_data_df
".Replace("expectedCount", expectedCount.ToStringInvariant(), StringComparison.InvariantCulture)
.Replace("{identation}", identation, StringComparison.InvariantCulture);
            }

            return @"
{identation}universeDataPerTime = qb.universe_history(universe, start, end)
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

        private class QuantBookTestClass: QuantBook
        {
            public static IEnumerable<BaseDataCollection> PerformSelection(IEnumerable<BaseDataCollection> history, DateTime start, DateTime end, IDateRule dateRule)
            {
                Func<BaseDataCollection, BaseDataCollection> processDataPointFunction = dataPoint => dataPoint;
                Func<BaseDataCollection, DateTime> getTime = dataPoint => dataPoint.EndTime.Date;
                return PerformSelection<BaseDataCollection, BaseDataCollection>(history, processDataPointFunction, getTime, start, end, dateRule);
            }
        }
    }
}
