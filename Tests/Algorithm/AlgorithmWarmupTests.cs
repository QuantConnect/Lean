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

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using QuantConnect.Algorithm;
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Packets;
using QuantConnect.Indicators;
using QuantConnect.Tests.Engine.DataFeeds;
using Python.Runtime;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.HistoricalData;
using QuantConnect.Util;
using QuantConnect.Statistics;

namespace QuantConnect.Tests.Algorithm
{
    [TestFixture]
    public class AlgorithmWarmupTests
    {
        private TestWarmupAlgorithm _algorithm;

        [TearDown]
        public void TearDown()
        {
            Config.Reset();
        }

        [TestCase(Resolution.Tick, SecurityType.Forex)]
        [TestCase(Resolution.Second, SecurityType.Forex)]
        [TestCase(Resolution.Hour, SecurityType.Forex)]
        [TestCase(Resolution.Minute, SecurityType.Forex)]
        [TestCase(Resolution.Daily, SecurityType.Forex)]
        [TestCase(Resolution.Tick, SecurityType.Equity)]
        [TestCase(Resolution.Second, SecurityType.Equity)]
        [TestCase(Resolution.Hour, SecurityType.Equity)]
        [TestCase(Resolution.Minute, SecurityType.Equity)]
        [TestCase(Resolution.Daily, SecurityType.Equity)]
        [TestCase(Resolution.Minute, SecurityType.Crypto)]
        [TestCase(Resolution.Daily, SecurityType.Crypto)]
        public void WarmupDifferentResolutions(Resolution resolution, SecurityType securityType)
        {
            _algorithm = TestSetupHandler.TestAlgorithm = new TestWarmupAlgorithm(resolution);

            _algorithm.SecurityType = securityType;
            if (securityType == SecurityType.Forex)
            {
                _algorithm.StartDateToUse = new DateTime(2014, 05, 03);
                _algorithm.EndDateToUse = new DateTime(2014, 05, 04);
            }
            else if (securityType == SecurityType.Equity)
            {
                _algorithm.StartDateToUse = new DateTime(2013, 10, 09);
                _algorithm.EndDateToUse = new DateTime(2013, 10, 10);
            }
            else if (securityType == SecurityType.Crypto)
            {
                _algorithm.StartDateToUse = new DateTime(2018, 04, 06);
                _algorithm.EndDateToUse = new DateTime(2018, 04, 07);
            }

            AlgorithmRunner.RunLocalBacktest(nameof(TestWarmupAlgorithm),
                new Dictionary<string, string> { { PerformanceMetrics.TotalOrders, "1" } },
                Language.CSharp,
                AlgorithmStatus.Completed,
                setupHandler: "TestSetupHandler");

            int estimateExpectedDataCount;
            switch (resolution)
            {
                case Resolution.Tick:
                    estimateExpectedDataCount = 2 * (securityType == SecurityType.Forex ? 19 : 4) * 60;
                    break;
                case Resolution.Second:
                    estimateExpectedDataCount = 2 * (securityType == SecurityType.Forex ? 19 : 6) * 60 * 60;
                    break;
                case Resolution.Minute:
                    estimateExpectedDataCount = 2 * (securityType == SecurityType.Forex ? 19 : 6) * 60;
                    break;
                case Resolution.Hour:
                    estimateExpectedDataCount = 2 * (securityType == SecurityType.Forex ? 19 : 6);
                    break;
                case Resolution.Daily:
                    // Warmup is 2 days. During warmup we expect the daily data point which goes from T-2 to T-1, once warmup finished,
                    // we will get T-1 to T data point which is let through but the data feed since the algorithm starts at T
                    estimateExpectedDataCount = 1;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(resolution), resolution, null);
            }

            Log.Debug($"WarmUpDataCount: {_algorithm.WarmUpDataCount}. Resolution {resolution}. SecurityType {securityType}");
            Assert.GreaterOrEqual(_algorithm.WarmUpDataCount, estimateExpectedDataCount);
        }

        [Test]
        public void WarmUpInternalSubscriptions()
        {
            var algo = new AlgorithmStub(new MockDataFeed())
            {
                HistoryProvider = new SubscriptionDataReaderHistoryProvider()
            };

            algo.SetStartDate(2013, 10, 08);
            algo.AddCfd("DE30EUR", Resolution.Second, Market.Oanda);
            algo.SetWarmup(10);
            algo.PostInitialize();
            algo.DataManager.UniverseSelection.EnsureCurrencyDataFeeds(SecurityChanges.None);

            Assert.AreEqual(algo.StartDate - TimeSpan.FromSeconds(10), algo.Time);
        }

        [Test]
        public void WarmUpUniverseSelection()
        {
            var algo = new AlgorithmStub(new MockDataFeed())
            {
                HistoryProvider = new SubscriptionDataReaderHistoryProvider()
            };

            algo.SetStartDate(2013, 10, 08);
            var universe = algo.AddUniverse((_) => Enumerable.Empty<Symbol>());
            var barCount = 3;
            algo.SetWarmup(barCount);
            algo.PostInitialize();

            // +2 is due to the weekend
            Assert.AreEqual(algo.StartDate - universe.Configuration.Resolution.ToTimeSpan() * (barCount + 2), algo.Time);
        }

        [Test]
        public void WarmUpPythonIndicatorProperly()
        {
            var algo = new AlgorithmStub
            {
                HistoryProvider = new SubscriptionDataReaderHistoryProvider()
            };
            algo.HistoryProvider.Initialize(new HistoryProviderInitializeParameters(
                null,
                null,
                TestGlobals.DataProvider,
                TestGlobals.DataCacheProvider,
                TestGlobals.MapFileProvider,
                TestGlobals.FactorFileProvider,
                null,
                false,
                new DataPermissionManager(),
                algo.ObjectStore,
                algo.Settings));
            algo.SetStartDate(2013, 10, 08);
            algo.AddEquity("SPY", Resolution.Minute);

            // Different types of indicators
            var indicatorDataPoint = new SimpleMovingAverage("SPY", 10);
            var indicatorDataBar = new AverageTrueRange("SPY", 10);
            var indicatorTradeBar = new VolumeWeightedAveragePriceIndicator("SPY", 10);

            using (Py.GIL())
            {
                var sma = indicatorDataPoint.ToPython();
                var atr = indicatorTradeBar.ToPython();
                var vwapi = indicatorDataBar.ToPython();

                Assert.DoesNotThrow(() => algo.WarmUpIndicator("SPY", sma, Resolution.Minute));
                Assert.DoesNotThrow(() => algo.WarmUpIndicator("SPY", atr, Resolution.Minute));
                Assert.DoesNotThrow(() => algo.WarmUpIndicator("SPY", vwapi, Resolution.Minute));

                var smaIsReady = ((dynamic)sma).IsReady;
                var atrIsReady = ((dynamic)atr).IsReady;
                var vwapiIsReady = ((dynamic)vwapi).IsReady;

                Assert.IsTrue(smaIsReady.IsTrue());
                Assert.IsTrue(atrIsReady.IsTrue());
                Assert.IsTrue(vwapiIsReady.IsTrue());
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void WarmupStartDate_NoAsset(bool withResolution)
        {
            var algo = new AlgorithmStub();
            algo.SetStartDate(2013, 10, 01);
            DateTime expected;
            if (withResolution)
            {
                algo.SetWarmUp(100, Resolution.Daily);
                expected = new DateTime(2013, 06, 23);
            }
            else
            {
                algo.SetWarmUp(100);
                // defaults to universe settings
                expected = new DateTime(2013, 09, 30, 22, 20, 0);
            }
            algo.PostInitialize();

            Assert.AreEqual(expected, algo.Time);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void WarmupStartDate_Equity_BarCount(bool withResolution)
        {
            var algo = new AlgorithmStub(new NullDataFeed { ShouldThrow = false });
            algo.SetStartDate(2013, 10, 01);
            algo.AddEquity("AAPL");
            // since SPY is a smaller resolution, won't affect in the bar count case, only the smallest warmup start time will be used
            algo.AddEquity("SPY", Resolution.Tick);
            DateTime expected;
            if (withResolution)
            {
                algo.SetWarmUp(100, Resolution.Daily);
                expected = new DateTime(2013, 05, 09);
            }
            else
            {
                algo.SetWarmUp(100);
                // uses the assets resolution
                expected = new DateTime(2013, 9, 30, 14, 20, 0);
            }
            algo.PostInitialize();

            // before than the case with no asset because takes into account 100 tradable dates of AAPL
            Assert.AreEqual(expected, algo.Time);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        public void WarmupStart_Equivalents(int testCase)
        {
            var algo = new AlgorithmStub(new NullDataFeed { ShouldThrow = false });
            algo.SetStartDate(2013, 10, 01);
            algo.AddEquity("AAPL", Resolution.Daily);
            // since SPY is a smaller resolution, won't affect in the bar count case, only the smallest warmup start time will be used
            algo.AddEquity("SPY", Resolution.Tick);
            var expected = new DateTime(2013, 09, 20);
            if (testCase == 0)
            {
                algo.SetWarmUp(7, Resolution.Daily);
            }
            else if (testCase == 1)
            {
                algo.SetWarmUp(7);
            }
            else if (testCase == 2)
            {
                algo.SetWarmUp(7);
                algo.Settings.WarmupResolution = Resolution.Daily;
            }
            else if (testCase == 3)
            {
                // account for 2 weeknds
                algo.SetWarmUp(TimeSpan.FromDays(11), Resolution.Daily);
            }
            else if (testCase == 4)
            {
                // account for 2 weeknds
                algo.SetWarmUp(TimeSpan.FromDays(11));
                algo.Settings.WarmupResolution = Resolution.Daily;
            }
            else if (testCase == 5)
            {
                // account for 2 weeknds
                algo.SetWarmUp(TimeSpan.FromDays(11));
            }
            algo.PostInitialize();

            Assert.AreEqual(expected, algo.Time);
        }

        [TestCase("UTC")]
        [TestCase("Asia/Hong_Kong")]
        [TestCase("America/New_York")]
        public void WarmupEndTime(string timeZone)
        {
            var algo = new AlgorithmStub(new NullDataFeed { ShouldThrow = false });
            algo.SetLiveMode(true);

            algo.SetWarmup(TimeSpan.FromDays(1));
            algo.SetTimeZone(timeZone);
            algo.PostInitialize();
            algo.SetLocked();

            Assert.IsTrue(algo.IsWarmingUp);

            var start = DateTime.UtcNow;

            algo.SetDateTime(start.AddMinutes(-1));
            Assert.IsTrue(algo.IsWarmingUp);
            algo.SetDateTime(start);
            Assert.IsFalse(algo.IsWarmingUp);
        }

        [Test]
        public void WarmupResolutionPython()
        {
            using (Py.GIL())
            {
                dynamic algo = PyModule.FromString("testModule",
                    @"
from AlgorithmImports import *
from QuantConnect.Tests.Engine.DataFeeds import *

class TestAlgo(AlgorithmStub):
    def initialize(self):
        self.data_feed.should_throw = False

        self.set_start_date(2013, 10, 1)
        self.add_equity(""AAPL"")
        self.set_warm_up(60)
").GetAttr("TestAlgo").Invoke();

                algo.initialize();
                algo.post_initialize();

                // the last trading hour of the previous day
                Assert.AreEqual(new DateTime(2013, 09, 30, 15, 0, 0), (DateTime)algo.time);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void WarmupResolutionPythonPassThrough(bool passThrough)
        {
            using (Py.GIL())
            {
                dynamic algo = PyModule.FromString("testModule",
                    @"
from AlgorithmImports import *
from QuantConnect.Tests.Engine.DataFeeds import *

class TestAlgo(AlgorithmStub):
    def __init__(self, pass_through):
        self.pass_through = pass_through

    def initialize(self):
        self.data_feed.should_throw = False

        self.set_start_date(2013, 10, 1)
        self.add_equity(""AAPL"")
        self.set_warm_up(10)
        if self.pass_through:
            self.settings.warm_up_resolution = Resolution.DAILY
        else:
            self.settings.warmup_resolution = Resolution.DAILY
").GetAttr("TestAlgo").Invoke(passThrough.ToPython());

                algo.initialize();
                algo.post_initialize();

                Assert.AreEqual(passThrough, (bool)algo.pass_through);
                // 10 daily bars including 2 weekends
                Assert.AreEqual(new DateTime(2013, 09, 17), (DateTime)algo.time);
            }
        }

        private class TestSetupHandler : AlgorithmRunner.RegressionSetupHandlerWrapper
        {
            public static TestWarmupAlgorithm TestAlgorithm { get; set; }

            public override IAlgorithm CreateAlgorithmInstance(AlgorithmNodePacket algorithmNodePacket, string assemblyPath)
            {
                Algorithm = TestAlgorithm;
                return Algorithm;
            }
        }

        private class TestWarmupAlgorithm : QCAlgorithm
        {
            private readonly Resolution _resolution;
            private Symbol _symbol;
            public SecurityType SecurityType { get; set; }

            public DateTime StartDateToUse { get; set; }

            public DateTime EndDateToUse { get; set; }

            public int WarmUpDataCount { get; set; }

            public TestWarmupAlgorithm(Resolution resolution)
            {
                _resolution = resolution;
            }

            public override void Initialize()
            {
                SetStartDate(StartDateToUse);
                SetEndDate(EndDateToUse);

                if (SecurityType == SecurityType.Forex)
                {
                    SetCash("NZD", 1);
                    _symbol = AddForex("EURUSD", _resolution).Symbol;
                }
                else if (SecurityType == SecurityType.Equity)
                {
                    _symbol = AddEquity("SPY", _resolution).Symbol;
                }
                else if (SecurityType == SecurityType.Crypto)
                {
                    _symbol = AddCrypto("BTCUSD", _resolution).Symbol;
                }
                SetWarmUp(TimeSpan.FromDays(2));
            }

            public override void OnData(Slice data)
            {
                if (IsWarmingUp)
                {
                    WarmUpDataCount += data.Count;
                }
                else
                {
                    if (!Portfolio.Invested)
                    {
                        SetHoldings(_symbol, 1);
                    }
                }
            }
        }
    }
}
