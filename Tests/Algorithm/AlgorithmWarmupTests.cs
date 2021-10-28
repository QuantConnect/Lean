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
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.HistoricalData;
using QuantConnect.Util;

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
                new Dictionary<string, string> { { "Total Trades", "1" } },
                null,
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
                    estimateExpectedDataCount = 2;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(resolution), resolution, null);
            }

            Log.Trace($"WarmUpDataCount: {_algorithm.WarmUpDataCount}. Resolution {resolution}. SecurityType {securityType}");
            Assert.GreaterOrEqual(_algorithm.WarmUpDataCount, estimateExpectedDataCount);
        }

        [Test]
        public void WarmUpPythonIndicatorProperly()
        {
            var algo = new AlgorithmStub
            {
                HistoryProvider = new SubscriptionDataReaderHistoryProvider()
            };
            var zipCacheProvider = new ZipDataCacheProvider(TestGlobals.DataProvider);
            algo.HistoryProvider.Initialize(new HistoryProviderInitializeParameters(
                null,
                null,
                TestGlobals.DataProvider,
                zipCacheProvider,
                TestGlobals.MapFileProvider,
                TestGlobals.FactorFileProvider,
                null,
                false,
                new DataPermissionManager()));
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

            zipCacheProvider.DisposeSafely();
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
