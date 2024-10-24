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
        [TestCase(Language.Python)]
        public void UniverseSelectionData(Language language)
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
" + GetBaseImplementation(expectedCount: 7000, identation: "    "));

                    dynamic getUniverse = testModule.GetAttr("getUniverseHistory");
                    var pyHistory = getUniverse(_qb, _start, _end);

                    Assert.AreEqual(20, pyHistory.index.levels[0].__len__().AsManagedObject(typeof(int)));

                    foreach (var date in pyHistory.index.levels[0])
                    {
                        var fundamentalDataCount = pyHistory.loc[date].shape[0].AsManagedObject(typeof(int));
                        Assert.GreaterOrEqual(fundamentalDataCount, 7000);
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
        universe = qb.AddUniverse(self.selection)
" + GetBaseImplementation(expectedCount: 1, identation: "        ")).GetAttr("Test");

                    var instance = testModule(useUniverseUnchanged);
                    var pyHistory = instance.getUniverseHistory(_qb, _start, _end);

                    Assert.AreEqual(20, pyHistory.index.levels[0].__len__().AsManagedObject(typeof(int)));

                    foreach (var date in pyHistory.index.levels[0])
                    {
                        var fundamentalData = pyHistory.loc[date];
                        var fundamentalDataCount = fundamentalData.shape[0].AsManagedObject(typeof(int));

                        Assert.GreaterOrEqual(fundamentalDataCount, 1);
                        Assert.AreEqual(Symbols.AAPL, fundamentalData.index[0].AsManagedObject(typeof(Symbol)));
                    }
                }
            }
        }

        [TestCase(Language.CSharp)]
        [TestCase(Language.Python)]
        public void UniverseSelectionWithDateRule(Language language)
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
                    dynamic testModule = PyModule.FromString("testModule",
                    @"
from AlgorithmImports import *

class Test():
    def selection(self, fundamentals):
        return [ x.Symbol for x in fundamentals if x.Symbol.Value == ""AAPL"" ]

    def getUniverseHistory(self, qb, start, end):
        universe = qb.AddUniverse(self.selection)
        universeDataPerTime = qb.universe_history(universe, start, end, date_rule = qb.date_rules.week_end())

        for date in universeDataPerTime.index.levels[0]:
            dateUniverseData = universeDataPerTime.loc[date]
            dataPointCount = dateUniverseData.shape[0]
            if dataPointCount < 1:
                raise ValueError(f'Unexpected historical Fundamentals data count {dataPointCount}! Expected > 0')

        return universeDataPerTime").GetAttr("Test");

                    var instance = testModule();
                    var pyHistory = instance.getUniverseHistory(_qb, _start, _end);

                    Assert.AreEqual(4, pyHistory.index.levels[0].__len__().AsManagedObject(typeof(int)));

                    foreach (var date in pyHistory.index.levels[0])
                    {
                        var fundamentalData = pyHistory.loc[date];
                        var fundamentalDataCount = fundamentalData.shape[0].AsManagedObject(typeof(int));

                        Assert.GreaterOrEqual(fundamentalDataCount, 1);
                        Assert.AreEqual(Symbols.AAPL, fundamentalData.index[0].AsManagedObject(typeof(Symbol)));
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

        for date in universeDataPerTime.index.levels[0]:
            dateUniverseData = universeDataPerTime.loc[date]
            dataPointCount = dateUniverseData.shape[0]
            if dataPointCount < 1:
                raise ValueError(f""Unexpected historical Fundamentals data count {dataPointCount}! Expected > 0"")

        return universeDataPerTime
").GetAttr("Test");

                    var instance = testModule();
                    var pyHistory = instance.getUniverseHistory(_qb, _start, _end);

                    Assert.AreEqual(41, pyHistory.index.levels[0].__len__().AsManagedObject(typeof(int)));

                    foreach (var date in pyHistory.index.levels[0])
                    {
                        var fundamentalData = pyHistory.loc[date];
                        var fundamentalDataCount = fundamentalData.shape[0].AsManagedObject(typeof(int));

                        Assert.GreaterOrEqual(fundamentalDataCount, 1);
                        Assert.AreEqual(Symbols.AAPL, fundamentalData.index[0].AsManagedObject(typeof(Symbol)));
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
    return qb.UniverseHistory(Fundamentals, start, end)
                    ");

                    dynamic getUniverse = testModule.GetAttr("getUniverseHistory");
                    var pyHistory = getUniverse(_qb, _start, _end);

                    Assert.AreEqual(20, pyHistory.index.levels[0].__len__().AsManagedObject(typeof(int)));

                    foreach (var date in pyHistory.index.levels[0])
                    {
                        var fundamentalDataCount = pyHistory.loc[date].shape[0].AsManagedObject(typeof(int));
                        Assert.GreaterOrEqual(fundamentalDataCount, 7000);
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
        return qb.UniverseHistory(Fundamentals, start, end, self.selection)
").GetAttr("Test");

                    var instance = testModule(useUniverseUnchanged);
                    var pyHistory = instance.getUniverseHistory(_qb, _start, _end);

                    Assert.AreEqual(20, pyHistory.index.levels[0].__len__().AsManagedObject(typeof(int)));

                    foreach (var date in pyHistory.index.levels[0])
                    {
                        var fundamentalData = pyHistory.loc[date];
                        var fundamentalDataCount = fundamentalData.shape[0].AsManagedObject(typeof(int));

                        Assert.GreaterOrEqual(fundamentalDataCount, 1);
                        Assert.AreEqual(Symbols.AAPL, fundamentalData.index[0].AsManagedObject(typeof(Symbol)));
                    }
                }
            }
        }

        [TestCase(Language.CSharp)]
        [TestCase(Language.Python)]
        public void GenericUniverseSelectionIsCompatibleWithDateRule(Language language)
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

    def getUniverseHistory(self, qb, start, end):
        return qb.universe_history(Fundamentals, start, end, self.selection, date_rule = qb.date_rules.week_end())
").GetAttr("Test");

                    var instance = testModule();
                    var pyHistory = instance.getUniverseHistory(_qb, _start, _end);

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
        public void PythonMonthlyStartGenericUniverseSelectionWorksAsExpected()
        {
            using (Py.GIL())
            {
                dynamic testModule = PyModule.FromString("testModule",
                @"
from AlgorithmImports import *

class Test():
    def selection(self, fundamentals):
        return [ x.Symbol for x in fundamentals if x.Symbol.Value == ""AAPL"" ]

    def getUniverseHistory(self, qb, start, end, symbol):
        return qb.universe_history(Fundamentals, start, end, self.selection, date_rule = qb.date_rules.month_start(symbol))
").GetAttr("Test");

                var instance = testModule();
                var pyHistory = instance.getUniverseHistory(_qb, new DateTime(2014, 3, 24), new DateTime(2014, 4, 7), Symbols.AAPL);
                Assert.AreEqual(1, pyHistory.__len__().AsManagedObject(typeof(int)));

                var firstDayOfTheMonth = (pyHistory.index[0][0]).AsManagedObject(typeof(DateTime));
                Assert.AreEqual(new DateTime(2014, 4, 1), firstDayOfTheMonth);
            }
        }


        [Test]
        public void PythonMonthlyEndGenericUniverseSelectionWorksAsExpected()
        {
            using (Py.GIL())
            {
                dynamic testModule = PyModule.FromString("testModule",
                @"
from AlgorithmImports import *

class Test():
    def selection(self, fundamentals):
        return [ x.Symbol for x in fundamentals if x.Symbol.Value == ""AAPL"" ]

    def getUniverseHistory(self, qb, start, end, symbol):
        return qb.universe_history(Fundamentals, start, end, self.selection, date_rule = qb.date_rules.month_end(symbol))
").GetAttr("Test");

                var instance = testModule();
                var pyHistory = instance.getUniverseHistory(_qb, new DateTime(2014, 3, 24), new DateTime(2014, 4, 7), Symbols.AAPL);
                Assert.AreEqual(1, pyHistory.__len__().AsManagedObject(typeof(int)));

                var firstDayOfTheMonth = (pyHistory.index[0][0]).AsManagedObject(typeof(DateTime));
                Assert.AreEqual(new DateTime(2014, 3, 29), firstDayOfTheMonth);
            }
        }

        [Test]
        public void PythonDailyGenericUniverseSelectionWorksAsExpected()
        {
            using (Py.GIL())
            {
                dynamic testModule = PyModule.FromString("testModule",
                @"
from AlgorithmImports import *

class Test():
    def selection(self, fundamentals):
        return [ x.Symbol for x in fundamentals if x.Symbol.Value == ""AAPL"" ]

    def getUniverseHistory(self, qb, start, end, symbol):
        return qb.universe_history(Fundamentals, start, end, self.selection, date_rule = qb.date_rules.every_day())
").GetAttr("Test");

                var instance = testModule();
                var pyHistory = instance.getUniverseHistory(_qb, new DateTime(2014, 3, 24), new DateTime(2014, 4, 7), Symbols.AAPL);

                Assert.AreEqual(10, pyHistory.index.levels[0].__len__().AsManagedObject(typeof(int)));

                foreach (var date in pyHistory.index.levels[0])
                {
                    var fundamentalData = pyHistory.loc[date];
                    var fundamentalDataCount = fundamentalData.shape[0].AsManagedObject(typeof(int));

                    Assert.GreaterOrEqual(fundamentalDataCount, 1);
                    Assert.AreEqual(Symbols.AAPL, fundamentalData.index[0].AsManagedObject(typeof(Symbol)));
                }
            }
        }

        [Test]
        public void PythonWeekendGenericUniverseSelectionWorksAsExpected()
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

    def getUniverseHistory(self, qb, start, end, symbol):
        return qb.universe_history(Fundamentals, start, end, self.selection, date_rule = qb.date_rules.on(datetime(2014, 3, 30), datetime(2014, 3, 31), datetime(2014, 4, 1)))
").GetAttr("Test");

                var instance = testModule();
                var pyHistory = instance.getUniverseHistory(_qb, new DateTime(2014, 3, 24), new DateTime(2014, 4, 7), Symbols.AAPL);
                var str = pyHistory.ToString();

                Assert.AreEqual(2, pyHistory.index.levels[0].__len__().AsManagedObject(typeof(int)));

                foreach (var date in pyHistory.index.levels[0])
                {
                    var fundamentalData = pyHistory.loc[date];
                    var fundamentalDataCount = fundamentalData.shape[0].AsManagedObject(typeof(int));

                    Assert.GreaterOrEqual(fundamentalDataCount, 1);
                    Assert.AreEqual(Symbols.AAPL, fundamentalData.index[0].AsManagedObject(typeof(Symbol)));
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

        private static string GetBaseImplementation(int expectedCount, string identation)
        {
            return @"
{identation}universeDataDf = qb.UniverseHistory(universe, start, end)
{identation}for date in universeDataDf.index.levels[0]:
{identation}    dateUniverseData = universeDataDf.loc[date]
{identation}    dataPointCount = dateUniverseData.shape[0]
{identation}    if dataPointCount < expectedCount:
{identation}        raise ValueError(f""Unexpected historical Fundamentals data count {dataPointCount}! Expected > expectedCount"")
{identation}return universeDataDf
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
