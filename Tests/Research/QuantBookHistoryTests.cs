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
                var history = qb.GetOptionHistory(future, start, end, Resolution.Minute);
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
                var history = qb.GetOptionHistory(futureOption, start, end, Resolution.Minute);
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
    }
}
