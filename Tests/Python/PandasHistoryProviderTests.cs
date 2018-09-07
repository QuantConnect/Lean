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
using QuantConnect.Algorithm;
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Util;
using System;
using System.Diagnostics;
using System.Linq;

namespace QuantConnect.Tests.Engine.DataFeeds
{
    [TestFixture]
    public class PandasHistoryProviderTests
    {
        private PyObject _module;
        private IHistoryProvider _historyProvider;

        [SetUp]
        public void SetUp()
        {
            var composer = new Composer();
            var algorithmHandlers = LeanEngineAlgorithmHandlers.FromConfiguration(composer);
            var dataCacheProvider = new ZipDataCacheProvider(algorithmHandlers.DataProvider);
            var mapFileProvider = algorithmHandlers.MapFileProvider;
            _historyProvider = composer.GetExportedValueByTypeName<IHistoryProvider>(Config.Get("history-provider", "SubscriptionDataReaderHistoryProvider"));
            _historyProvider.Initialize(null, algorithmHandlers.DataProvider, dataCacheProvider, mapFileProvider, algorithmHandlers.FactorFileProvider, null);

            using (Py.GIL())
            {
                _module = PythonEngine.ModuleFromString(Guid.NewGuid().ToString(), $@"
import os, sys
sys.path.append(os.getcwd())
from QCAlgorithm import QCAlgorithm

class MyAlgo(QCAlgorithm):
    pass");
            }
        }

        public TestCaseData[] TestIntOverload
        {
            get
            {
                return new[]
                {
                    new TestCaseData(new DateTime(2013,10,10), SecurityType.Equity, "SPY", Resolution.Second, 10),
                    new TestCaseData(new DateTime(2013,10,10), SecurityType.Equity, "SPY", Resolution.Minute, 10),
                    new TestCaseData(new DateTime(2013,10,10), SecurityType.Equity, "SPY", Resolution.Hour, 10),
                    new TestCaseData(new DateTime(2013,10,10), SecurityType.Equity, "SPY", Resolution.Daily, 10),

                    new TestCaseData(new DateTime(2014,5,8), SecurityType.Forex, "EURUSD", Resolution.Second, 10),
                    new TestCaseData(new DateTime(2014,5,8), SecurityType.Forex, "EURUSD", Resolution.Minute, 10),
                    new TestCaseData(new DateTime(2014,5,8), SecurityType.Forex, "EURUSD", Resolution.Hour, 10),
                    new TestCaseData(new DateTime(2014,5,8), SecurityType.Forex, "EURUSD", Resolution.Daily, 10),
                };
            }
        }

        [Test]
        [TestCaseSource("TestIntOverload")]
        public void GetsHistoryIntOverload(DateTime startDate, SecurityType securityType, string ticker, Resolution resolution, int period)
        {
            var expectedAlgorithm = GetExpectedAlgorithm(startDate, ticker, resolution);
            expectedAlgorithm.AddSecurity(securityType, ticker, resolution, fillDataForward: false);

            var expectedHistory = expectedAlgorithm.History(new Symbol[] { ticker }, period)
                .Select(x => (BaseData)x[ticker]).ToArray();

            using (Py.GIL())
            {
                dynamic algorithm = _module.GetAttr("MyAlgo").Invoke();
                algorithm.SubscriptionManager.SetDataManager(new DataManager());
                algorithm.SetHistoryProvider(_historyProvider);
                algorithm.SetStartDate(startDate);
                algorithm.AddSecurity(securityType, ticker, resolution);
                dynamic df = algorithm.History(ticker, period);

                AssertHistory(securityType, expectedHistory, df);
            }
        }

        public TestCaseData[] TestTimeSpanOverload
        {
            get
            {
                var span = TimeSpan.FromDays(2);

                return new[]
                {
                    new TestCaseData(new DateTime(2013,10,10), SecurityType.Equity, "SPY", Resolution.Second, span),
                    new TestCaseData(new DateTime(2013,10,10), SecurityType.Equity, "SPY", Resolution.Minute, span),
                    new TestCaseData(new DateTime(2013,10,10), SecurityType.Equity, "SPY", Resolution.Hour, span),
                    new TestCaseData(new DateTime(2013,10,10), SecurityType.Equity, "SPY", Resolution.Daily, span),

                    new TestCaseData(new DateTime(2014,5,8), SecurityType.Forex, "EURUSD", Resolution.Second, span),
                    new TestCaseData(new DateTime(2014,5,8), SecurityType.Forex, "EURUSD", Resolution.Minute, span),
                    new TestCaseData(new DateTime(2014,5,8), SecurityType.Forex, "EURUSD", Resolution.Hour, span),
                    new TestCaseData(new DateTime(2014,5,8), SecurityType.Forex, "EURUSD", Resolution.Daily, span),
                };
            }
        }

        [Test]
        [TestCaseSource("TestTimeSpanOverload")]
        public void GetsHistoryTimeSpanOverload(DateTime startDate, SecurityType securityType, string ticker, Resolution resolution, TimeSpan span)
        {
            var expectedAlgorithm = GetExpectedAlgorithm(startDate, ticker, resolution);
            expectedAlgorithm.AddSecurity(securityType, ticker, resolution, fillDataForward: false);

            var expectedHistory = expectedAlgorithm.History(new Symbol[] { ticker }, span)
                .Select(x => (BaseData)x[ticker]).ToArray();

            using (Py.GIL())
            {
                dynamic algorithm = _module.GetAttr("MyAlgo").Invoke();
                algorithm.SubscriptionManager.SetDataManager(new DataManager());
                algorithm.SetHistoryProvider(_historyProvider);
                algorithm.SetStartDate(startDate);
                algorithm.AddSecurity(securityType, ticker, resolution);
                dynamic df = algorithm.History(ticker, span);

                AssertHistory(securityType, expectedHistory, df);
            }
        }

        public TestCaseData[] TestDateTimeOverload
        {
            get
            {
                var spyDates = Tuple.Create(new DateTime(2013, 10, 10), new DateTime(2013, 10, 7), new DateTime(2013, 10, 9));
                var eurDates = Tuple.Create(new DateTime(2014, 5, 8), new DateTime(2014, 5, 5), new DateTime(2014, 5, 7));

                return new[]
                {
                    new TestCaseData(spyDates.Item1, SecurityType.Equity, "SPY", Resolution.Second, spyDates.Item2, spyDates.Item3),
                    new TestCaseData(spyDates.Item1, SecurityType.Equity, "SPY", Resolution.Minute, spyDates.Item2, spyDates.Item3),
                    new TestCaseData(spyDates.Item1, SecurityType.Equity, "SPY", Resolution.Hour, spyDates.Item2, spyDates.Item3),
                    new TestCaseData(spyDates.Item1, SecurityType.Equity, "SPY", Resolution.Daily, spyDates.Item2, spyDates.Item3),

                    new TestCaseData(eurDates.Item1, SecurityType.Forex, "EURUSD", Resolution.Second, eurDates.Item2, eurDates.Item3),
                    new TestCaseData(eurDates.Item1, SecurityType.Forex, "EURUSD", Resolution.Minute, eurDates.Item2, eurDates.Item3),
                    new TestCaseData(eurDates.Item1, SecurityType.Forex, "EURUSD", Resolution.Hour, eurDates.Item2, eurDates.Item3),
                    new TestCaseData(eurDates.Item1, SecurityType.Forex, "EURUSD", Resolution.Daily, eurDates.Item2, eurDates.Item3),
                };
            }
        }

        [Test]
        [TestCaseSource("TestDateTimeOverload")]
        public void GetsHistoryDateTimeOverload(DateTime startDate, SecurityType securityType, string ticker, Resolution resolution, DateTime start, DateTime end)
        {
            var expectedAlgorithm = GetExpectedAlgorithm(startDate, ticker, resolution);
            expectedAlgorithm.AddSecurity(securityType, ticker, resolution, fillDataForward: false);

            var expectedHistory = expectedAlgorithm.History(new Symbol[] { ticker }, start, end)
                .Select(x => (BaseData)x[ticker]).ToArray();

            using (Py.GIL())
            {
                dynamic algorithm = _module.GetAttr("MyAlgo").Invoke();
                algorithm.SubscriptionManager.SetDataManager(new DataManager());
                algorithm.SetHistoryProvider(_historyProvider);
                algorithm.SetStartDate(startDate);
                algorithm.AddSecurity(securityType, ticker, resolution);
                dynamic df = algorithm.History(ticker, start, end);

                AssertHistory(securityType, expectedHistory, df);
            }
        }

        public TestCaseData[] TestTwoSymbolIntOverload
        {
            get
            {
                return new[]
                {
                    new TestCaseData(new DateTime(2013,10,10), "SPY", "IBM", Resolution.Second, 10),
                    new TestCaseData(new DateTime(2013,10,10), "SPY", "IBM", Resolution.Minute, 10),
                    new TestCaseData(new DateTime(2013,10,10), "SPY", "IBM", Resolution.Hour, 10),
                    new TestCaseData(new DateTime(2013,10,10), "SPY", "IBM", Resolution.Daily, 10),
                };
            }
        }

        [Test]
        [TestCaseSource("TestTwoSymbolIntOverload")]
        public void GetsHistoryTwoSymbolIntOverload(DateTime startDate, string tickerA, string tickerB, Resolution resolution, int periods)
        {
            var expectedAlgorithm = GetExpectedAlgorithm(startDate, tickerA, resolution);
            expectedAlgorithm.AddEquity(tickerA, resolution, fillDataForward: false);
            expectedAlgorithm.AddEquity(tickerB, resolution, fillDataForward: false);

            var expectedHistory = expectedAlgorithm.History(new Symbol[] { tickerA, tickerB }, periods)
                .Select(x => x.Bars).ToArray();

            using (Py.GIL())
            {
                dynamic algorithm = _module.GetAttr("MyAlgo").Invoke();
                algorithm.SubscriptionManager.SetDataManager(new DataManager());
                algorithm.SetHistoryProvider(_historyProvider);
                algorithm.SetStartDate(startDate);
                algorithm.AddEquity(tickerA, resolution);
                algorithm.AddEquity(tickerB, resolution);
                var symbols = new PyList(new[] { new PyString(tickerA), new PyString(tickerB) });
                dynamic df = algorithm.History(symbols, periods);

                AssertTwoSymbolsHistory(expectedHistory, df);
            }
        }

        public TestCaseData[] TestTwoSymbolTimeSpanOverload
        {
            get
            {
                return new[]
                {
                    new TestCaseData(new DateTime(2013,10,10), "SPY", "IBM", Resolution.Second, TimeSpan.FromDays(2)),
                    new TestCaseData(new DateTime(2013,10,10), "SPY", "IBM", Resolution.Minute, TimeSpan.FromDays(2)),
                    new TestCaseData(new DateTime(2013,10,10), "SPY", "IBM", Resolution.Hour, TimeSpan.FromDays(2)),
                    new TestCaseData(new DateTime(2013,10,10), "SPY", "IBM", Resolution.Daily, TimeSpan.FromDays(2)),
                };
            }
        }

        [Test]
        [TestCaseSource("TestTwoSymbolTimeSpanOverload")]
        public void GetsHistoryTwoSymbolTimeSpanOverload(DateTime startDate, string tickerA, string tickerB, Resolution resolution, TimeSpan span)
        {
            var expectedAlgorithm = GetExpectedAlgorithm(startDate, tickerA, resolution);
            expectedAlgorithm.AddEquity(tickerA, resolution, fillDataForward: false);
            expectedAlgorithm.AddEquity(tickerB, resolution, fillDataForward: false);

            var expectedHistory = expectedAlgorithm.History(new Symbol[] { tickerA, tickerB }, span)
                .Select(x => x.Bars).ToArray();

            using (Py.GIL())
            {
                dynamic algorithm = _module.GetAttr("MyAlgo").Invoke();
                algorithm.SubscriptionManager.SetDataManager(new DataManager());
                algorithm.SetHistoryProvider(_historyProvider);
                algorithm.SetStartDate(startDate);
                algorithm.AddEquity(tickerA, resolution);
                algorithm.AddEquity(tickerB, resolution);
                var symbols = new PyList(new[] { new PyString(tickerA), new PyString(tickerB) });
                dynamic df = algorithm.History(symbols, span);

                AssertTwoSymbolsHistory(expectedHistory, df);
            }
        }

        private QCAlgorithm GetExpectedAlgorithm(DateTime startDate, string ticker, Resolution resolution)
        {
            var algorithm = new QCAlgorithm();
            algorithm.SubscriptionManager.SetDataManager(new DataManager());
            algorithm.SetHistoryProvider(_historyProvider);
            algorithm.SetStartDate(startDate);
            return algorithm;
        }

        private void AssertHistory(SecurityType securityType, BaseData[] expectedHistory, dynamic df)
        {
            using (Py.GIL())
            {
                var expectedLength = expectedHistory.Length;
                var actualLength = (int)df.index.__len__();
                Assert.AreEqual(expectedLength, actualLength);

                for (var i = 0; i < expectedLength; i++)
                {
                    var expectedBar = expectedHistory[i];

                    var actualEndTime = (DateTime)df.index.levels[1][i];
                    Assert.AreEqual(expectedBar.EndTime, actualEndTime);

                    var actualBar = df.iloc[i];
                    var actualValue = 0.0;

                    if (securityType == SecurityType.Equity)
                    {
                        actualValue = (double)actualBar.close;
                    }
                    else
                    {
                        var bidclose = (double)actualBar.bidclose;
                        var askclose = (double)actualBar.askclose;
                        actualValue = (bidclose + askclose) / 2;
                    }
                    Assert.AreEqual((double)expectedBar.Value, actualValue, 1e-6);
                }
            }
        }

        private void AssertTwoSymbolsHistory(TradeBars[] expectedHistory, dynamic df)
        {
            using (Py.GIL())
            {
                var timeIndex = df.index.levels[1];
                var expectedLength = expectedHistory.Length;
                var actualLength = (int)timeIndex.__len__();
                Assert.AreEqual(expectedLength, actualLength);

                for (var i = 0; i < expectedLength; i++)
                {
                    var expectedBars = expectedHistory[i];

                    var actualEndTime = (DateTime)timeIndex[i];

                    foreach(var kvp in expectedBars)
                    {
                        var expectedBar = kvp.Value;
                        Assert.AreEqual(expectedBar.EndTime, actualEndTime);

                        var index = new PyTuple(new PyObject[] { kvp.Key.Value.ToPython(), timeIndex[i] });
                        var actualBar = df.loc[index];
                        var actualValue = (double)actualBar.close;
                        Assert.AreEqual((double)expectedBar.Value, actualValue, 1e-6);
                    }
                }
            }
        }

        [Test]
        [TestCase("from QCAlgorithm import QCAlgorithm")]
        [TestCase("AddReference('QuantConnect.Algorithm'); from QuantConnect.Algorithm import *")]
        public void PandasHistoryBenchmark(string statement)
        {
            var millisecondPerIteration = 0.0;

            using (Py.GIL())
            {
                var module = PythonEngine.ModuleFromString(Guid.NewGuid().ToString(), $@"
import os, sys
sys.path.append(os.getcwd())

from clr import AddReference
AddReference('QuantConnect.Common')
from QuantConnect import *

{statement}

from datetime import datetime, timedelta

class MyAlgo(QCAlgorithm):
    def Initialize(self):
        self.SetStartDate(2014,5,9)
        symbol = self.AddForex('EURUSD', Resolution.Minute).Symbol
        history = self.History(symbol, timedelta(7))");

                dynamic algorithm = module.GetAttr("MyAlgo").Invoke();

                algorithm.SetPandasConverter();
                algorithm.SetHistoryProvider(_historyProvider);
                algorithm.SubscriptionManager.SetDataManager(new DataManager());

                var count = 100;
                var timer = Stopwatch.StartNew();

                for (var i = 0; i < count; i++)
                {
                    algorithm.Initialize();
                }

                timer.Stop();
                millisecondPerIteration = timer.Elapsed.TotalMilliseconds / count;
            }
        }
    }
}