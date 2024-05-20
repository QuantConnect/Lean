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
using Python.Runtime;
using QuantConnect.Securities;
using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Research;
using QuantConnect.Logging;

namespace QuantConnect.Tests.Research
{
    [TestFixture]
    public class QuantBookHistoryTests
    {
        private ILogHandler _logHandler;
        dynamic _module;

        [OneTimeSetUp]
        public void Setup()
        {
            // Store initial handler
            _logHandler = Log.LogHandler;

            SymbolCache.Clear();
            MarketHoursDatabase.Reset();

            using (Py.GIL())
            {
                _module = Py.Import("Test_QuantBookHistory");
            }
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            // Reset to initial handler
            Log.LogHandler = _logHandler;
        }

        [Test]
        [TestCase(2013, 10, 11, SecurityType.Equity, "SPY")]
        [TestCase(2014, 5, 9, SecurityType.Forex, "EURUSD")]
        [TestCase(2016, 10, 9, SecurityType.Crypto, "BTCUSD")]
        public void SecurityQuantBookHistoryTests(int year, int month, int day, SecurityType securityType, string symbol)
        {
            using (Py.GIL())
            {
                var startDate = new DateTime(year, month, day);
                var securityTestHistory = _module.SecurityHistoryTest(startDate, securityType, symbol);

                // Get the last 10 candles
                var periodHistory = securityTestHistory.test_period_overload(10);
                var count = (periodHistory.shape[0] as PyObject).AsManagedObject(typeof(int));
                Assert.AreEqual(10, count);

                // Get the one day of data
                var timedeltaHistory = securityTestHistory.test_period_overload(TimeSpan.FromDays(1));
                var firstIndex = (DateTime)(timedeltaHistory.index.values[0] as PyObject).AsManagedObject(typeof(DateTime));
                Assert.GreaterOrEqual(startDate.AddDays(-1).Date, firstIndex.Date);

                // Get the one day of data, ending one day before start date
                var startEndHistory = securityTestHistory.test_daterange_overload(startDate.AddDays(-1));
                firstIndex = (DateTime)(startEndHistory.index.values[0] as PyObject).AsManagedObject(typeof(DateTime));
                Assert.GreaterOrEqual(startDate.AddDays(-2).Date, firstIndex.Date);
            }
        }

        [Test]
        [TestCase(2014, 5, 9, "Nifty", "NIFTY")]
        public void CustomDataQuantBookHistoryTests(int year, int month, int day, string customDataType, string symbol)
        {
            using (Py.GIL())
            {
                var startDate = new DateTime(year, month, day);
                var securityTestHistory = _module.CustomDataHistoryTest(startDate, customDataType, symbol);

                // Get the last 5 candles
                var periodHistory = securityTestHistory.test_period_overload(5);
                var count = (periodHistory.shape[0] as PyObject).AsManagedObject(typeof(int));
                Assert.AreEqual(5, count);

                // Get the one day of data
                var timedeltaHistory = securityTestHistory.test_period_overload(TimeSpan.FromDays(8));
                var firstIndex =
                    (DateTime)(timedeltaHistory.index.values[0] as PyObject).AsManagedObject(typeof(DateTime));
                Assert.AreEqual(startDate.AddDays(-7), firstIndex);

                // Get the one day of data, ending one day before start date
                var startEndHistory = securityTestHistory.test_daterange_overload(startDate.AddDays(-2));
                firstIndex = (DateTime)(startEndHistory.index.values[0] as PyObject).AsManagedObject(typeof(DateTime));
                Assert.AreEqual(startDate.AddDays(-2).Date, firstIndex.Date);
            }
        }

        [Test]
        public void MultipleSecuritiesQuantBookHistoryTests()
        {
            using (Py.GIL())
            {
                var startDate = new DateTime(2014, 5, 9);
                var securityTestHistory = _module.MultipleSecuritiesHistoryTest(startDate, null, null);

                // Get the last 5 candles
                var periodHistory = securityTestHistory.test_period_overload(5);

                // Note there is no data for BTCUSD at 2014

                //symbol                 EURUSD         SPY
                //time
                //2014-05-03 00:00:00        NaN        173.580655
                //2014-05-04 20:00:00   1.387185               NaN
                //2014-05-05 20:00:00   1.387480               NaN
                //2014-05-06 00:00:00        NaN        173.903690
                //2014-05-06 20:00:00   1.392925               NaN
                //2014-05-07 00:00:00        NaN        172.426958
                //2014-05-07 20:00:00   1.391070               NaN
                //2014-05-08 00:00:00        NaN        173.423752
                //2014-05-08 20:00:00   1.384265               NaN
                //2014-05-09 00:00:00        NaN        173.229931
                Log.Trace(periodHistory.ToString());

                var count = (periodHistory.shape[0] as PyObject).AsManagedObject(typeof(int));
                Assert.AreEqual(10, count);

                // Get the one day of data
                var timedeltaHistory = securityTestHistory.test_period_overload(TimeSpan.FromDays(8));
                var firstIndex = (DateTime)(timedeltaHistory.index.values[0] as PyObject).AsManagedObject(typeof(DateTime));

                // EURUSD exchange time zone is NY but data is UTC so we have a 4 hour difference with algo TZ which is NY
                Assert.AreEqual(startDate.AddDays(-8).AddHours(20), firstIndex);
            }
        }

        [Test]
        public void CanonicalOptionQuantBookHistory()
        {
            using (Py.GIL())
            {
                var symbol = "TWX";
                var startDate = new DateTime(2014, 6, 6);
                var securityTestHistory = _module.OptionHistoryTest(startDate, SecurityType.Option, symbol);

                // Get the one day of data, ending on start date
                var startEndHistory = securityTestHistory.test_daterange_overload(startDate);
                Log.Trace(startEndHistory.ToString());
                var firstIndex = (DateTime)(startEndHistory.index.values[0][4] as PyObject).AsManagedObject(typeof(DateTime));
                Assert.GreaterOrEqual(startDate.AddDays(-1).Date, firstIndex.Date);
            }
        }

        [Test]
        public void CanonicalOptionIntradayQuantBookHistory()
        {
            using (Py.GIL())
            {
                var symbol = "TWX";
                var currentDate = new DateTime(2014, 6, 6, 18, 0, 0);
                var securityTestHistory = _module.OptionHistoryTest(new DateTime(2014, 6, 7), SecurityType.Option, symbol);

                var startEndHistory = securityTestHistory.test_daterange_overload(currentDate, new DateTime(2014, 6, 6, 10, 0, 0));
                Log.Trace(startEndHistory.ToString());
                Assert.IsFalse((bool)startEndHistory.empty);
            }
        }

        [Test]
        public void OptionContractQuantBookHistory()
        {
            using (Py.GIL())
            {
                var symbol = Symbol.CreateOption("TWX", Market.USA, OptionStyle.American, OptionRight.Call, 70, new DateTime(2015, 01, 17));
                var startDate = new DateTime(2014, 6, 6);
                var securityTestHistory = _module.OptionContractHistoryTest(startDate, SecurityType.Option, symbol);

                // Get the one day of data, ending on start date
                var startEndHistory = securityTestHistory.test_daterange_overload(startDate);
                Log.Trace(startEndHistory.ToString());
                var firstIndex = (DateTime)(startEndHistory.index.values[0][4] as PyObject).AsManagedObject(typeof(DateTime));
                Assert.GreaterOrEqual(startDate.AddDays(-1).Date, firstIndex.Date);
            }
        }

        [Test]
        public void OptionIndexWeekly()
        {
            var qb = new QuantBook();
            var spxw = qb.AddIndexOption(Symbols.SPX, "SPXW");
            spxw.SetFilter(u => u.Strikes(0, 1)
                 // single week ahead since there are many SPXW contracts and we want to preserve performance
                 .Expiration(0, 7)
                 .IncludeWeeklys());

            var startTime = new DateTime(2021, 1, 4);

            var historyByOptionSymbol = qb.GetOptionHistory(spxw.Symbol, startTime);
            var historyByUnderlyingSymbol = qb.GetOptionHistory(Symbols.SPX, "SPXW", startTime);

            List<DateTime> expiry;
            List<DateTime> byUnderlyingExpiry;

            historyByOptionSymbol.GetExpiryDates().TryConvert(out expiry);
            historyByUnderlyingSymbol.GetExpiryDates().TryConvert(out byUnderlyingExpiry);

            List<decimal> strikes;
            List<decimal> byUnderlyingStrikes;

            historyByOptionSymbol.GetStrikes().TryConvert(out strikes);
            historyByUnderlyingSymbol.GetStrikes().TryConvert(out byUnderlyingStrikes);

            Assert.IsTrue(expiry.Count > 0);
            Assert.IsTrue(expiry.SequenceEqual(byUnderlyingExpiry));

            Assert.IsTrue(strikes.Count > 0);
            Assert.IsTrue(strikes.SequenceEqual(byUnderlyingStrikes));
        }

        [Test]
        public void OptionUnderlyingSymbolQuantBookHistory()
        {
            var qb = new QuantBook();
            var twx = qb.AddEquity("TWX");
            var twxOptions = qb.AddOption("TWX");

            var historyByOptionSymbol = qb.GetOptionHistory(twxOptions.Symbol, new DateTime(2014, 6, 5), new DateTime(2014, 6, 6));
            var historyByEquitySymbol = qb.GetOptionHistory(twx.Symbol, new DateTime(2014, 6, 5), new DateTime(2014, 6, 6));

            List<DateTime> expiry;
            List<DateTime> byUnderlyingExpiry;

            historyByOptionSymbol.GetExpiryDates().TryConvert(out expiry);
            historyByEquitySymbol.GetExpiryDates().TryConvert(out byUnderlyingExpiry);

            List<decimal> strikes;
            List<decimal> byUnderlyingStrikes;

            historyByOptionSymbol.GetStrikes().TryConvert(out strikes);
            historyByEquitySymbol.GetStrikes().TryConvert(out byUnderlyingStrikes);

            Assert.IsTrue(expiry.Count > 0);
            Assert.IsTrue(expiry.SequenceEqual(byUnderlyingExpiry));

            Assert.IsTrue(strikes.Count > 0);
            Assert.IsTrue(strikes.SequenceEqual(byUnderlyingStrikes));
        }

        [TestCase(182, 2)]
        [TestCase(120, 1)]
        public void CanonicalFutureQuantBookHistory(int maxFilter, int numberOfFutureContracts)
        {
            using (Py.GIL())
            {
                var symbol = Futures.Indices.SP500EMini;
                var startDate = new DateTime(2013, 10, 11);
                var securityTestHistory = _module.FutureHistoryTest(startDate, SecurityType.Future, symbol);

                // Get the one day of data, ending on start date
                var startEndHistory = securityTestHistory.test_daterange_overload(startDate, startDate.AddDays(-1), maxFilter);

                Log.Trace(startEndHistory.index.levels[1].size.ToString());
                Assert.AreEqual(numberOfFutureContracts, (int)startEndHistory.index.levels[1].size);

                Log.Trace(startEndHistory.ToString());
                var firstIndex = (DateTime)(startEndHistory.index.values[0][2] as PyObject).AsManagedObject(typeof(DateTime));
                Assert.GreaterOrEqual(startDate.AddDays(-1).Date, firstIndex.Date);
            }
        }

        [TestCase(182, 2)]
        [TestCase(120, 1)]
        public void CanonicalFutureIntradayQuantBookHistory(int maxFilter, int numberOfFutureContracts)
        {
            using (Py.GIL())
            {
                var symbol = Futures.Indices.SP500EMini;
                var currentDate = new DateTime(2013, 10, 11, 18, 0, 0);
                var securityTestHistory = _module.FutureHistoryTest(new DateTime(2013, 10, 12), SecurityType.Future, symbol);

                var startEndHistory = securityTestHistory.test_daterange_overload(currentDate, new DateTime(2013, 10, 11, 10, 0, 0), maxFilter);

                Log.Trace(startEndHistory.index.levels[1].size.ToString());
                Assert.AreEqual(numberOfFutureContracts, (int)startEndHistory.index.levels[1].size);

                Log.Trace(startEndHistory.ToString());
                Assert.IsFalse((bool)startEndHistory.empty);
            }
        }

        [Test]
        public void FutureContractQuantBookHistory()
        {
            using (Py.GIL())
            {
                var symbol = Symbol.CreateFuture(Futures.Indices.SP500EMini, Market.CME, new DateTime(2014, 12, 19));
                var startDate = new DateTime(2013, 10, 11);
                var securityTestHistory = _module.FutureContractHistoryTest(startDate, SecurityType.Future, symbol);

                // Get the one day of data, ending on start date
                var startEndHistory = securityTestHistory.test_daterange_overload(startDate);
                Log.Trace(startEndHistory.ToString());
                var firstIndex = (DateTime)(startEndHistory.index.values[0][2] as PyObject).AsManagedObject(typeof(DateTime));
                Assert.GreaterOrEqual(startDate.AddDays(-1).Date, firstIndex.Date);
            }
        }

        [Test]
        public void FuturesOptionsWithFutureContract()
        {
            using (Py.GIL())
            {
                var qb = new QuantBook();
                var expiry = new DateTime(2020, 3, 20);
                var future = Symbol.CreateFuture(Futures.Indices.SP500EMini, Market.CME, expiry);
                var start = new DateTime(2020, 1, 5);
                var end = new DateTime(2020, 1, 6);
                var history = qb.GetOptionHistory(future, start, end, Resolution.Minute, extendedMarketHours: true);
                dynamic df = history.GetAllData();

                Assert.IsNotNull(df);
                Assert.IsFalse((bool)df.empty.AsManagedObject(typeof(bool)));
                Assert.Greater((int)df.__len__().AsManagedObject(typeof(int)), 360);
                Assert.AreEqual(5, (int)df.index.levels.__len__().AsManagedObject(typeof(int)));
                Assert.IsTrue((bool)df.index.levels[0].__contains__(expiry.ToStringInvariant("yyyy-MM-dd")).AsManagedObject(typeof(bool)));
            }
        }

        [Test]
        public void FuturesOptionsWithFutureOptionContract()
        {
            using (Py.GIL())
            {
                var qb = new QuantBook();
                var expiry = new DateTime(2020, 3, 20);
                var future = Symbol.CreateFuture(Futures.Indices.SP500EMini, Market.CME, expiry);
                var futureOption = Symbol.CreateOption(
                    future,
                    future.ID.Market,
                    OptionStyle.American,
                    OptionRight.Call,
                    3300m,
                    expiry);

                var start = new DateTime(2020, 1, 5);
                var end = new DateTime(2020, 1, 6);
                var history = qb.GetOptionHistory(futureOption, start, end, Resolution.Minute, extendedMarketHours: true);
                dynamic df = history.GetAllData();

                Assert.IsNotNull(df);
                Assert.IsFalse((bool)df.empty.AsManagedObject(typeof(bool)));
                Assert.AreEqual(360, (int)df.__len__().AsManagedObject(typeof(int)));
                Assert.AreEqual(5, (int)df.index.levels.__len__().AsManagedObject(typeof(int)));
                Assert.IsTrue((bool)df.index.levels[0].__contains__(expiry.ToStringInvariant("yyyy-MM-dd")).AsManagedObject(typeof(bool)));
            }
        }

        [Test]
        public void CanoicalFutureCrashesGetOptionHistory()
        {
            var qb = new QuantBook();
            var future = Symbol.Create(Futures.Indices.SP500EMini, SecurityType.Future, Market.CME);

            Assert.Throws<ArgumentException>(() =>
            {
                qb.GetOptionHistory(future, default(DateTime), DateTime.MaxValue, Resolution.Minute);
            });
        }

        [TestCase(true, true, 1920)]
        [TestCase(true, false, 780)]
        [TestCase(false, true, 898)]
        [TestCase(false, false, 390)]
        public void OptionHistorySpecifyingFillForwardAndExtendedMarket(bool fillForward, bool extendedMarket, int expectedCount)
        {
            using (Py.GIL())
            {
                var qb = new QuantBook();
                var start = new DateTime(2013, 10, 11);
                var end = new DateTime(2013, 10, 15);

                var spy = qb.AddEquity("SPY");
                dynamic history = qb.GetOptionHistory(spy.Symbol, start, end, Resolution.Minute, fillForward, extendedMarket).GetAllData();
                var historyCount = (history.shape[0] as PyObject).As<int>();

                Assert.AreEqual(expectedCount, historyCount);
            }
        }

        [TestCase(true, true, 8640)]
        [TestCase(true, false, 2700)]
        [TestCase(false, true, 6899)]
        [TestCase(false, false, 2249)]
        public void FutureHistorySpecifyingFillForwardAndExtendedMarket(bool fillForward, bool extendedMarket, int expectedCount)
        {
            using (Py.GIL())
            {
                var qb = new QuantBook();
                var start = new DateTime(2013, 10, 6);
                var end = new DateTime(2013, 10, 15);

                var future = Symbol.CreateFuture(Futures.Indices.SP500EMini, Market.CME, new DateTime(2013, 12, 20));
                dynamic history = qb.GetFutureHistory(future, start, end, Resolution.Minute, fillForward, extendedMarket).GetAllData();
                var historyCount = (history.shape[0] as PyObject).As<int>();

                Assert.AreEqual(expectedCount, historyCount);
            }
        }

        [TestCase(Language.CSharp)]
        [TestCase(Language.Python)]
        public void OptionHistoryObjectIsIterable(Language language)
        {
            var qb = new QuantBook();
            var start = new DateTime(2013, 10, 11);
            var end = new DateTime(2013, 10, 15);

            var spy = qb.AddEquity("SPY");
            var history = qb.GetOptionHistory(spy.Symbol, start, end, Resolution.Minute);

            Assert.DoesNotThrow(() =>
            {
                if (language == Language.CSharp)
                {
                    Assert.AreEqual(780, history.Count());
                }
                else
                {
                    using (Py.GIL())
                    {
                        var testModule = PyModule.FromString("testModule",
                        @"
def getOptionHistory(qb, symbol, start, end, resolution):
    return qb.GetOptionHistory(symbol, start, end, resolution)

def getHistoryCount(history):
    return len(list(history))
        ");

                        dynamic getOptionHistory = testModule.GetAttr("getOptionHistory");
                        dynamic getHistoryCount = testModule.GetAttr("getHistoryCount");
                        var pyHistory = getOptionHistory(qb, spy.Symbol, start, end, Resolution.Minute);
                        Assert.AreEqual(780, getHistoryCount(pyHistory).AsManagedObject(typeof(int)));
                    }
                }
            });
        }

        [TestCase(Language.CSharp)]
        [TestCase(Language.Python)]
        public void FutureHistoryObjectIsIterable(Language language)
        {
            var qb = new QuantBook();
            var start = new DateTime(2013, 10, 6);
            var end = new DateTime(2013, 10, 15);

            var futureSymbol = Symbol.CreateFuture(Futures.Indices.SP500EMini, Market.CME, new DateTime(2013, 12, 20));
            var history = qb.GetFutureHistory(futureSymbol, start, end, Resolution.Minute);

            Assert.DoesNotThrow(() =>
            {
                if (language == Language.CSharp)
                {
                    Assert.AreEqual(2700, history.Count());
                }
                else
                {
                    using (Py.GIL())
                    {
                        var testModule = PyModule.FromString("testModule",
                        @"
def getFutureHistory(qb, symbol, start, end, resolution):
    return qb.GetFutureHistory(symbol, start, end, resolution)

def getHistoryCount(history):
    return len(list(history))
        ");

                        dynamic getFutureHistory = testModule.GetAttr("getFutureHistory");
                        dynamic getHistoryCount = testModule.GetAttr("getHistoryCount");
                        var pyHistory = getFutureHistory(qb, futureSymbol, start, end, Resolution.Minute);
                        Assert.AreEqual(2700, getHistoryCount(pyHistory).AsManagedObject(typeof(int)));
                    }
                }
            });
        }

        [TestCase(Language.CSharp)]
        [TestCase(Language.Python)]
        public void GetOptionContractsWithFrontMonthFilter(Language language)
        {
            using (Py.GIL())
            {
                Assert.DoesNotThrow(() =>
                {
                    if (language == Language.CSharp)
                    {
                        var qb = new QuantBook();
                        var start = new DateTime(2015, 12, 24);
                        var end = new DateTime(2015, 12, 24);

                        var goog = qb.AddEquity("GOOG");
                        var option = qb.AddOption(goog.Symbol);
                        option.SetFilter(universe => universe.Strikes(-5, 5).FrontMonth());

                        var history = qb.GetOptionHistory(goog.Symbol, start, end, Resolution.Minute, fillForward: false, extendedMarketHours: false);
                        dynamic data = history.GetAllData();
                        var labels = data.axes[0].names;
                        Assert.AreEqual("expiry", (labels[0] as PyObject).As<string>());
                    }
                    else
                    {
                        var testModule = PyModule.FromString("testModule",
                            @"
from AlgorithmImports import *
def getAllData():
    qb = QuantBook()
    underlying_symbol = qb.AddEquity(""GOOG"").Symbol
    option = qb.AddOption(underlying_symbol)
    option.SetFilter(lambda option_filter_universe: option_filter_universe.Strikes(-5, 5).FrontMonth())
    option_history = qb.OptionHistory(underlying_symbol, datetime(2015, 12, 24), datetime(2015, 12, 24), Resolution.Minute, fillForward=False, extendedMarketHours=False)
    data = option_history.GetAllData()
    return data.axes[0].names[0]");

                        dynamic getAllData = testModule.GetAttr("getAllData");
                        var data = getAllData();
                        Assert.AreEqual("expiry", data.AsManagedObject(typeof(string)));
                    }
                });
            }
        }
    }
}
