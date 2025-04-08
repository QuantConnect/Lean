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
using QuantConnect.Data.Fundamental;
using System.Data;
using QuantConnect.Securities.Future;
using QuantConnect.Data;
using NodaTime;
using QuantConnect.Interfaces;
using QuantConnect.Data.UniverseSelection;

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
                //2014-05-02 16:00:00        NaN        164.219446
                //2014-05-04 20:00:00   1.387185               NaN
                //2014-05-05 16:00:00        NaN        164.551273
                //2014-05-05 20:00:00   1.387480               NaN
                //2014-05-06 16:00:00        NaN        163.127909
                //2014-05-06 20:00:00   1.392925               NaN
                //2014-05-07 16:00:00        NaN        164.070997
                //2014-05-07 20:00:00   1.391070               NaN
                //2014-05-08 16:00:00        NaN        163.905083
                //2014-05-08 20:00:00   1.384265               NaN
                Log.Trace(periodHistory.ToString());

                var count = (periodHistory.shape[0] as PyObject).AsManagedObject(typeof(int));
                Assert.AreEqual(10, count);

                // Get the one day of data
                var timedeltaHistory = securityTestHistory.test_period_overload(TimeSpan.FromDays(8));
                var firstIndex = (DateTime)(timedeltaHistory.index.values[0] as PyObject).AsManagedObject(typeof(DateTime));

                // EURUSD exchange time zone is NY but data is UTC so we have a 4 hour difference with algo TZ which is NY
                Assert.AreEqual(startDate.AddDays(-8).AddHours(16), firstIndex);
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

        private static TestCaseData[] CanonicalOptionIntradayHistoryTestCases
        {
            get
            {
                var twx = Symbol.Create("TWX", SecurityType.Equity, Market.USA);
                var twxOption = Symbol.CreateCanonicalOption(twx);

                var spx = Symbol.Create("SPX", SecurityType.Index, Market.USA);
                var spxwOption = Symbol.CreateCanonicalOption(spx, Market.USA, null);

                return
                [
                    new TestCaseData(twxOption, new DateTime(2014, 06, 05), (DateTime?)null, Resolution.Minute),
                    new TestCaseData(twxOption, new DateTime(2014, 06, 05), new DateTime(2014, 06, 05), Resolution.Minute),
                    new TestCaseData(twxOption, new DateTime(2014, 06, 05), new DateTime(2014, 06, 06), Resolution.Minute),
                    new TestCaseData(twxOption, new DateTime(2014, 06, 05, 0, 0, 0), new DateTime(2014, 06, 05, 15, 0, 0), Resolution.Minute),
                    new TestCaseData(twxOption, new DateTime(2014, 06, 05, 10, 0, 0), new DateTime(2014, 06, 05, 15, 0, 0), Resolution.Minute),
                    new TestCaseData(twxOption, new DateTime(2014, 06, 05, 10, 0, 0), new DateTime(2014, 06, 06), Resolution.Minute),
                    new TestCaseData(twxOption, new DateTime(2014, 06, 05, 10, 0, 0), new DateTime(2014, 06, 06, 10, 0, 0), Resolution.Minute),
                    new TestCaseData(twxOption, new DateTime(2014, 06, 05, 10, 0, 0), new DateTime(2014, 06, 06, 15, 0, 0), Resolution.Minute),

                    new TestCaseData(spxwOption, new DateTime(2021, 01, 04), (DateTime?)null, Resolution.Hour),
                    new TestCaseData(spxwOption, new DateTime(2021, 01, 04), new DateTime(2021, 01, 04), Resolution.Hour),
                    new TestCaseData(spxwOption, new DateTime(2021, 01, 04), new DateTime(2021, 01, 05), Resolution.Hour),
                    new TestCaseData(spxwOption, new DateTime(2021, 01, 04, 10, 0, 0), new DateTime(2021, 01, 04, 15, 0, 0), Resolution.Hour),
                    new TestCaseData(spxwOption, new DateTime(2021, 01, 04, 10, 0, 0), new DateTime(2021, 01, 05, 15, 0, 0), Resolution.Hour),
                    new TestCaseData(spxwOption, new DateTime(2021, 01, 14, 10, 0, 0), new DateTime(2021, 01, 14, 15, 0, 0), Resolution.Hour),
                ];
            }
        }

        [TestCaseSource(nameof(CanonicalOptionIntradayHistoryTestCases))]
        public void CanonicalOptionIntradayQuantBookHistoryWithIntradayRange(Symbol canonicalOption, DateTime start, DateTime? end, Resolution resolution)
        {
            var quantBook = new QuantBook();
            var historyProvider = new TestHistoryProvider(quantBook.HistoryProvider);
            quantBook.SetHistoryProvider(historyProvider);
            quantBook.SetStartDate((end ?? start).Date.AddDays(1));

            var option = quantBook.AddSecurity(canonicalOption);
            var history = quantBook.OptionHistory(canonicalOption, start, end, resolution);

            Assert.Greater(history.Count, 0);

            var symbolsInHistory = history.SelectMany(slice => slice.AllData.Select(x => x.Symbol)).Distinct().ToList();
            Assert.Greater(symbolsInHistory.Count, 1);

            var underlying = symbolsInHistory.Where(x => x == canonicalOption.Underlying).ToList();
            Assert.AreEqual(1, underlying.Count);

            var contractsSymbols = symbolsInHistory.Where(x => x.SecurityType == canonicalOption.SecurityType).ToList();
            Assert.Greater(contractsSymbols.Count, 1);

            var expectedDates = new HashSet<DateTime> { start.Date };
            if (end.HasValue && end.Value > end.Value.Date)
            {
                expectedDates.Add(end.Value.Date);
            }

            var dataDates = history.SelectMany(slice => slice.AllData.Where(x => contractsSymbols.Contains(x.Symbol)).Select(x => x.EndTime.Date)).ToHashSet();
            CollectionAssert.AreEqual(expectedDates, dataDates);

            // OptionUniverse must have been requested for all dates in the range
            foreach (var date in Time.EachTradeableDay(option, start.Date, (end ?? start).Date))
            {
                Assert.AreEqual(1, historyProvider.HistoryRequests.Count(request => request.DataType == typeof(OptionUniverse) && request.EndTimeLocal == date));
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

        private static TestCaseData[] CanonicalFutureIntradayHistoryTestCases
        {
            get
            {
                var es = Symbol.Create(Futures.Indices.SP500EMini, SecurityType.Future, Market.CME);
                return
                [
                    new TestCaseData(es, new DateTime(2013, 10, 10), (DateTime?)null),
                    new TestCaseData(es, new DateTime(2013, 10, 10), new DateTime(2013, 10, 10)),
                    new TestCaseData(es, new DateTime(2013, 10, 10), new DateTime(2013, 10, 11)),
                    new TestCaseData(es, new DateTime(2013, 10, 10, 0, 0, 0), new DateTime(2013, 10, 10, 15, 0, 0)),
                    new TestCaseData(es, new DateTime(2013, 10, 10, 10, 0, 0), new DateTime(2013, 10, 10, 15, 0, 0)),
                    new TestCaseData(es, new DateTime(2013, 10, 10, 10, 0, 0), new DateTime(2013, 10, 11)),
                    new TestCaseData(es, new DateTime(2013, 10, 10, 10, 0, 0), new DateTime(2013, 10, 11, 10, 0, 0)),
                    new TestCaseData(es, new DateTime(2013, 10, 10, 10, 0, 0), new DateTime(2013, 10, 11, 15, 0, 0))
                ];
            }
        }

        [TestCaseSource(nameof(CanonicalFutureIntradayHistoryTestCases))]
        public void CanonicalFutureIntradayQuantBookHistoryWithIntradayRange(Symbol canonicalFuture, DateTime start, DateTime? end)
        {
            var quantBook = new QuantBook();
            var historyProvider = new TestHistoryProvider(quantBook.HistoryProvider);
            quantBook.SetHistoryProvider(historyProvider);
            quantBook.SetStartDate((end ?? start).Date.AddDays(1));
            var future = quantBook.AddSecurity(canonicalFuture) as Future;
            future.SetFilter(universe => universe);

            var history = quantBook.FutureHistory(canonicalFuture, start, end, Resolution.Minute);
            Assert.Greater(history.Count, 0);

            var symbolsInHistory = history.SelectMany(slice => slice.AllData.Select(x => x.Symbol)).Distinct().ToList();
            Assert.Greater(symbolsInHistory.Count, 1);

            var expectedDates = new HashSet<DateTime> { start.Date };
            if (end.HasValue && end.Value > end.Value.Date)
            {
                expectedDates.Add(end.Value.Date);
            }

            var dataDates = history.SelectMany(slice => slice.AllData.Select(x => x.EndTime.Date)).ToHashSet();
            CollectionAssert.AreEqual(expectedDates, dataDates);

            // FutureUniverse must have been requested for all dates in the range
            foreach (var date in Time.EachTradeableDay(future, start.Date, (end ?? start).Date))
            {
                Assert.AreEqual(1, historyProvider.HistoryRequests.Count(request => request.DataType == typeof(FutureUniverse) && request.EndTimeLocal == date));
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
                    Assert.AreEqual(780, history.Count);
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
                    Assert.AreEqual(2700, history.Count);
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

        [Test]
        public void HistoryDataDoesnNotReturnDataLabelWithBaseDataCollectionTypes()
        {
            using (Py.GIL())
            {
                var testModule = PyModule.FromString("testModule",
                    @"
from AlgorithmImports import *

def getHistory():
    qb = QuantBook()
    symbol = qb.AddEquity(""AAPL"", Resolution.Daily).symbol
    dataset_symbol = qb.AddData(FundamentalUniverse, symbol).symbol
    history = qb.History(dataset_symbol, datetime(2014, 3, 1), datetime(2014, 4, 1), Resolution.Daily)
    return history
");
                dynamic getHistory = testModule.GetAttr("getHistory");
                var pyHistory = getHistory() as PyObject;
                var isHistoryEmpty = pyHistory.GetAttr("empty").GetAndDispose<bool?>();
                Assert.IsFalse(isHistoryEmpty);
                Assert.IsFalse(pyHistory.HasAttr("data"));
            }
        }

        [Test]
        public void HistoryDataDoesWorksCorrecltyWithoutAddingTheCustomDataInPython()
        {
            using (Py.GIL())
            {
                var testModule = PyModule.FromString("testModule",
                    @"
from AlgorithmImports import *

def getHistory():
    qb = QuantBook()
    symbol = qb.AddEquity(""AAPL"", Resolution.Daily).symbol
    dataset_symbol = Symbol.CreateBase(FundamentalUniverse, symbol, symbol.ID.Market)
    history = qb.History(dataset_symbol, datetime(2014, 3, 1), datetime(2014, 4, 1), Resolution.Daily)
    return history
");
                dynamic getHistory = testModule.GetAttr("getHistory");
                var pyHistory = getHistory() as PyObject;
                var isHistoryEmpty = pyHistory.GetAttr("empty").GetAndDispose<bool?>();
                Assert.IsFalse(isHistoryEmpty);
                Assert.IsFalse(pyHistory.HasAttr("data"));
            }
        }

        [Test]
        public void HistoryDataDoesWorksCorrectlyWithCustomDataInPython()
        {
            using (Py.GIL())
            {
                var testModule = PyModule.FromString("testModule",
                    @"
from AlgorithmImports import *

from datetime import datetime

from AlgorithmImports import *

def getHistory():
    qb = QuantBook()
    qb.add_data(
        type=TestTradeBar,
        ticker='TEST1',
        properties=SymbolProperties(
            description='TEST1',
            quoteCurrency='USD',
            contractMultiplier=1,
            minimumPriceVariation=0.01,
            lotSize=1,
            marketTicker='TEST1',
        ),
        exchange_hours=SecurityExchangeHours.always_open(TimeZones.NEW_YORK),
        resolution=Resolution.MINUTE,
        fill_forward=True,
        leverage=1,
    )
    history = qb.history(qb.securities.keys(), datetime(2024, 8, 2), datetime(2024, 8, 3))
    return history

class TestTradeBar(TradeBar):
    def get_source(self, config: SubscriptionDataConfig, date: datetime, is_live_mode: bool) -> SubscriptionDataSource:
        return SubscriptionDataSource(source='../../TestData/test.csv',
                                      transportMedium=SubscriptionTransportMedium.LOCAL_FILE,
                                      format=FileFormat.CSV)

    def reader(self, config: SubscriptionDataConfig, line: str, date: datetime, is_live_mode: bool) -> BaseData:
        if not line[0].isdigit():
            return None
        data = line.split(',')
        bar_time = datetime.utcfromtimestamp(int(data[0]))

        open = float(data[1])
        high = float(data[2])
        low = float(data[3])
        close = float(data[4])
        volume = int(float(data[7]))
        return TradeBar(bar_time, config.symbol, open, high, low, close, volume)
");
                dynamic getHistory = testModule.GetAttr("getHistory");
                var pyHistory = getHistory() as PyObject;
                var isHistoryEmpty = pyHistory.GetAttr("empty").GetAndDispose<bool?>();
                Assert.IsFalse(isHistoryEmpty);
                Assert.IsFalse(pyHistory.HasAttr("data"));
            }
        }

        [Test]
        public void HistoryDataWorksCorrecltyWithoutAddingTheCustomDataInCSharp()
        {
            var qb = new QuantBook();
            var symbol = qb.AddEquity("AAPL", Resolution.Daily).Symbol;
            var datasetSymbol = Symbol.CreateBase(typeof(FundamentalUniverse), symbol, symbol.ID.Market);
            MarketHoursDatabase.Reset();
            Assert.DoesNotThrow(() => qb.History(datasetSymbol, new DateTime(2014, 3, 1), new DateTime(2014, 4, 1), Resolution.Daily).ToList());
        }

        [Test]
        public void HistoryDataDoesnNotReturnDataLabelWithBaseDataCollectionTypesAndPeriods()
        {
            using (Py.GIL())
            {
                var testModule = PyModule.FromString("testModule",
                    @"
from AlgorithmImports import *

def get_history():
    qb = QuantBook()
    qb.set_start_date(2014, 4, 8)
    symbol = qb.add_equity(""AAPL"", Resolution.DAILY).symbol
    dataset_symbol = qb.add_data(FundamentalUniverse, symbol).symbol
    history = qb.history(dataset_symbol, 20, Resolution.DAILY)
    return history
");
                dynamic getHistory = testModule.GetAttr("get_history");
                var pyHistory = getHistory() as PyObject;
                var isHistoryEmpty = pyHistory.GetAttr("empty").GetAndDispose<bool?>();
                Assert.IsFalse(isHistoryEmpty);
                Assert.IsFalse(pyHistory.HasAttr("data"));
            }
        }

        private class TestHistoryProvider : HistoryProviderBase
        {
            private IHistoryProvider _provider;

            public List<HistoryRequest> HistoryRequests { get; } = new();

            public override int DataPointCount => _provider.DataPointCount;

            public TestHistoryProvider(IHistoryProvider provider)
            {
                _provider = provider;
            }

            public override void Initialize(HistoryProviderInitializeParameters parameters)
            {
            }

            public override IEnumerable<Slice> GetHistory(IEnumerable<HistoryRequest> requests, DateTimeZone sliceTimeZone)
            {
                requests = requests.ToList();
                HistoryRequests.AddRange(requests);
                return _provider.GetHistory(requests, sliceTimeZone);
            }
        }
    }
}
