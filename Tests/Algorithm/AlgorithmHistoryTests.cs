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
using NodaTime;
using System.IO;
using System.Linq;
using Python.Runtime;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Util;
using QuantConnect.Python;
using System.Globalization;
using QuantConnect.Packets;
using QuantConnect.Algorithm;
using QuantConnect.Securities;
using QuantConnect.Interfaces;
using QuantConnect.Data.Market;
using System.Collections.Generic;
using QuantConnect.Lean.Engine.Storage;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Tests.Engine.DataFeeds;
using QuantConnect.Lean.Engine.HistoricalData;
using HistoryRequest = QuantConnect.Data.HistoryRequest;
using QuantConnect.Data.Fundamental;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Tests.Common.Data.Fundamental;
using QuantConnect.Logging;

namespace QuantConnect.Tests.Algorithm
{
    [TestFixture, Parallelizable(ParallelScope.Fixtures)]
    public class AlgorithmHistoryTests
    {
        private QCAlgorithm _algorithm;
        private TestHistoryProvider _testHistoryProvider;
        private IDataProvider _dataProvider;
        private IMapFileProvider _mapFileProvider;
        private IDataCacheProvider _cacheProvider;
        private IFactorFileProvider _factorFileProvider;

        [SetUp]
        public void Setup()
        {
            _algorithm = new QCAlgorithm();
            _algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(_algorithm));
            _algorithm.HistoryProvider = _testHistoryProvider = new TestHistoryProvider();
        }

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _dataProvider = TestGlobals.DataProvider;
            _mapFileProvider = TestGlobals.MapFileProvider;
            _factorFileProvider = TestGlobals.FactorFileProvider;
            _cacheProvider = TestGlobals.DataCacheProvider;
            FundamentalService.Initialize(_dataProvider, new NullFundamentalDataProvider(), false);
        }

        [TestCase(Language.Python)]
        [TestCase(Language.CSharp)]
        public void FundamentalHistory(Language language)
        {
            var start = new DateTime(2014, 04, 07);
            _algorithm = GetAlgorithm(start);

            if (language == Language.CSharp)
            {
                var result = _algorithm.History<Fundamental>(new[] { Symbols.SPY }, 10).ToList();

                Assert.AreEqual(10, result.Count);
                Assert.IsTrue(result.All(fundamentals =>
                {
                    Assert.AreEqual(1, fundamentals.Count);
                    Assert.IsTrue(fundamentals.Values.All(x => !x.FinancialStatements.CashFlowStatement.CashFlowFromContinuingFinancingActivities.HasValue));
                    return fundamentals.Values.All(x => double.IsNaN(x.FinancialStatements.CashFlowStatement.CashFlowFromContinuingFinancingActivities));
                }));
            }
            else
            {
                using (Py.GIL())
                {
                    var getHistory = PyModule.FromString("testModule",
                        @"
from AlgorithmImports import *

def getHistory(algorithm, symbol):
    return algorithm.History(Fundamental, symbol, 10)
        ").GetAttr("getHistory");
                    _algorithm.SetPandasConverter();

                    var result = getHistory.Invoke(_algorithm.ToPython(), Symbols.SPY.ToPython());
                    Assert.AreEqual(10, result.GetAttr("shape")[0].As<int>());

                    dynamic subDataFrame = result.GetAttr("loc")[Symbols.SPY.ID.ToString()];

                    for (var i = 0; i < 10; i++)
                    {
                        var index = subDataFrame.index[i];
                        var series = subDataFrame.loc[index];
                        var cashFlow = (double)series.financialstatements.CashFlowStatement.CashFlowFromContinuingFinancingActivities.Value;
                        Assert.AreEqual(double.NaN, cashFlow);
                    }
                }
            }
        }

        [TestCase(Resolution.Daily, Language.CSharp, 2)]
        [TestCase(Resolution.Hour, Language.CSharp, 14)]
        [TestCase(Resolution.Minute, Language.CSharp, 780)]
        [TestCase(Resolution.Second, Language.CSharp, 45676)]
        [TestCase(Resolution.Daily, Language.Python, 2)]
        [TestCase(Resolution.Hour, Language.Python, 14)]
        [TestCase(Resolution.Minute, Language.Python, 780)]
        [TestCase(Resolution.Second, Language.Python, 45676)]
        public void TickResolutionSubscriptionHistoryRequestOtherResolution(Resolution resolution, Language language, int expectedHistoryCount)
        {
            var start = new DateTime(2013, 10, 07);
            _algorithm = GetAlgorithm(start.AddDays(2));

            _algorithm.AddEquity(Symbols.SPY.Value, Resolution.Tick);

            if (language == Language.CSharp)
            {
                // Trades and quotes
                var result = _algorithm.History(new[] { Symbols.SPY }, start, _algorithm.Time, resolution).ToList();

                var expectedSpan = resolution == Resolution.Daily ? TimeSpan.FromHours(6.5) : resolution.ToTimeSpan();
                Assert.AreEqual(expectedHistoryCount, result.Count);
                Assert.IsTrue(result.All(slice =>
                {
                    foreach (var bar in slice.Bars.Values)
                    {
                        return (bar.EndTime - bar.Time) == expectedSpan;
                    }
                    foreach (var bar in slice.QuoteBars.Values)
                    {
                        return (bar.EndTime - bar.Time) == expectedSpan;
                    }

                    return false;
                }));
            }
            else
            {
                using (Py.GIL())
                {
                    var getHistory = PyModule.FromString("testModule",
                        @"
from AlgorithmImports import *

def getHistory(algorithm, symbol, start, resolution):
    return algorithm.History(symbol, start, algorithm.Time, resolution).loc[symbol]
        ").GetAttr("getHistory");

                    _algorithm.SetPandasConverter();

                    var result = getHistory.Invoke(_algorithm.ToPython(), Symbols.SPY.ToPython(), start.ToPython(), resolution.ToPython());
                    Assert.AreEqual(expectedHistoryCount, result.GetAttr("shape")[0].As<int>());

                    var times = result.GetAttr("index").GetAttr("tolist").Invoke().As<List<DateTime>>();

                    var prevDateTime = times[0].Subtract(resolution.ToTimeSpan());
                    Assert.IsTrue(times.Any(time =>
                    {
                        var timeSpan = time - prevDateTime;
                        prevDateTime = time;
                        return timeSpan == resolution.ToTimeSpan();
                    }));
                }
            }
        }

        [Test]
        public void GetHistoryWithCustomDataAndUnsortedData()
        {
            var customDataKey = "CustomData/ExampleCustomData";

            var customData = new string[] {
                "2024-10-03 19:00:00,5894.5,5948.85,5887.95,5935.1,120195681,4882.29",
                "2024-10-02 18:00:00,5991.2,6038.2,5980.95,6030.8,116275729,4641.97",
                "2024-10-01 17:00:00,6000.5,6019,5951.15,6009,127707078,6591.27",
                "2024-09-30 11:00:00,6000.5,6019,5951.15,6009,127707078,6591.27",
                "2024-09-27 10:00:00,5894.5,5948.85,5887.95,5935.1,120195681,4882.29",
                "2024-09-26 09:00:00,5869.9,5879.35,5802.85,5816.7,117516350,4820.53",
                "2024-09-25 08:00:00,5834.6,5864.95,5834.6,5859,110427867,4661.55",
                "2024-09-24 07:00:00,5833.15,5833.85,5775.55,5811.55,127624733,4823.52",
                "2024-09-23 06:00:00,5889.95,5900.45,5858.45,5867.9,123586417,4303.93",
                "2024-09-20 05:00:00,5794.75,5848.2,5786.05,5836.95,151929179,5429.87",
                "2024-09-19 04:00:00,5811.95,5815,5760.4,5770.9,160523863,5219.24",
                "2024-09-18 03:00:00,5885.5,5898.8,5852.3,5857.55,145721790,5163.09",
                "2024-09-17 02:00:00,5834.1,5904.35,5822.2,5898.85,144794030,5405.72",
                "2024-09-16 01:00:00,5749.5,5852.95,5749.5,5842.2,214402430,8753.33",
                "2024-09-13 16:00:00,5894.5,5948.85,5887.95,5935.1,120195681,4882.29",
                "2024-09-12 15:00:00,5984.7,6051.1,5974.55,6038.05,171728134,7774.83",
                "2024-09-11 14:00:00,5972.25,5989.8,5926.75,5973.3,191516153,8349.59",
                "2024-09-10 13:00:00,5930.8,5966.05,5910.95,5955.25,151162819,5915.8",
                "2024-09-09 12:00:00,5991.2,6038.2,5980.95,6030.8,116275729,4641.97"
            };

            var endDateAlgorithm = new DateTime(2024, 10, 4);
            var algorithm = GetAlgorithm(endDateAlgorithm.AddDays(1));

            ExampleCustomDataWithSort.CustomDataKey = customDataKey;

            var customSymbol = algorithm.AddData<ExampleCustomDataWithSort>("ExampleCustomData", Resolution.Daily).Symbol;

            algorithm.ObjectStore.Save(customDataKey, string.Join("\n", customData));

            var history = algorithm.History<ExampleCustomDataWithSort>(customSymbol, algorithm.StartDate, algorithm.EndDate, Resolution.Daily).ToList();

            Assert.IsNotEmpty(history);
            Assert.That(history.Count, Is.EqualTo(customData.Length));

            for (int i = 0; i < history.Count - 1; i++)
            {
                if (history[i].EndTime > history[i + 1].EndTime)
                {
                    Assert.Fail($"Order failure: {history[i].EndTime} > {history[i + 1].EndTime} at index {i}.");
                }
            }
        }

        [TestCase(Language.CSharp)]
        [TestCase(Language.Python)]
        public void TickResolutionHistoryRequest(Language language)
        {
            var start = new DateTime(2013, 10, 07);
            _algorithm = GetAlgorithm(start.AddDays(1));

            if (language == Language.CSharp)
            {
                var result = _algorithm.History(new[] { Symbols.SPY }, start.AddHours(9.8), start.AddHours(10), Resolution.Tick).ToList();
                var result2 = _algorithm.History<Tick>(Symbols.SPY, start.AddHours(9.8), start.AddHours(10), Resolution.Tick).ToList();

                Assert.IsNotEmpty(result);
                Assert.IsNotEmpty(result2);

                Assert.IsTrue(result2.Any(tick => tick.TickType == TickType.Trade));
                Assert.IsTrue(result2.Any(tick => tick.TickType == TickType.Quote));

                var resultTickCount = result.Sum(slice => slice.Ticks[Symbols.SPY].Count);
                Assert.AreEqual(resultTickCount, result2.Count);
            }
            else
            {
                using (Py.GIL())
                {
                    var pythonModule = PyModule.FromString("testModule",
                        @"
from AlgorithmImports import *

def getTradesAndQuotesHistory(algorithm, symbol, start):
    return algorithm.History([symbol], start + timedelta(hours=12), start + timedelta(hours=12.2), Resolution.Second).loc[symbol].to_dict()

def getTradesOnlyHistory(algorithm, symbol, start):
    return algorithm.History(Tick, symbol, start + timedelta(hours=9.8), start + timedelta(hours=10), Resolution.Tick).loc[symbol].to_dict()
        ");
                    using var getTradesAndQuotesHistory = pythonModule.GetAttr("getTradesAndQuotesHistory");
                    using var getTradesOnlyHistory = pythonModule.GetAttr("getTradesOnlyHistory");
                    _algorithm.SetPandasConverter();
                    using var pySymbol = Symbols.SPY.ToPython();
                    using var pyAlgorithm = _algorithm.ToPython();
                    using var pyStart = start.ToPython();

                    using var dict = getTradesAndQuotesHistory.Invoke(pyAlgorithm, pySymbol, pyStart);
                    var result = GetDataFrameDictionary<dynamic>(dict);
                    using var dict2 = getTradesOnlyHistory.Invoke(pyAlgorithm, pySymbol, pyStart);
                    var result2 = GetDataFrameDictionary<dynamic>(dict2);

                    Assert.IsNotEmpty(result);
                    Assert.IsNotEmpty(result2);

                    CollectionAssert.AreNotEquivalent(result.Keys, result2.Keys);
                    CollectionAssert.IsNotSubsetOf(result2.Keys, result.Keys);
                }
            }
        }

        [Test]
        public void ImplicitTickResolutionHistoryRequestTradeBarApiThrowsException()
        {
            var spy = _algorithm.AddEquity("SPY", Resolution.Tick).Symbol;
            Assert.Throws<InvalidOperationException>(() => _algorithm.History(spy, 1).ToList());
        }

        [Test]
        public void ExplicitTickResolutionHistoryRequestTradeBarApiThrowsException()
        {
            var spy = _algorithm.AddEquity("SPY", Resolution.Tick).Symbol;
            Assert.Throws<InvalidOperationException>(() => _algorithm.History(spy, 1, Resolution.Tick).ToList());
        }

        [TestCase(Resolution.Tick, Resolution.Tick, true)]
        [TestCase(Resolution.Tick, Resolution.Second, true)]
        [TestCase(Resolution.Tick, Resolution.Minute, true)]
        [TestCase(Resolution.Tick, Resolution.Hour, true)]
        [TestCase(Resolution.Tick, Resolution.Daily, true)]
        [TestCase(Resolution.Second, Resolution.Tick, true)]
        [TestCase(Resolution.Second, Resolution.Second, true)]
        [TestCase(Resolution.Second, Resolution.Minute, true)]
        [TestCase(Resolution.Second, Resolution.Hour, true)]
        [TestCase(Resolution.Second, Resolution.Daily, true)]
        [TestCase(Resolution.Minute, Resolution.Tick, true)]
        [TestCase(Resolution.Minute, Resolution.Second, true)]
        [TestCase(Resolution.Minute, Resolution.Minute, true)]
        [TestCase(Resolution.Minute, Resolution.Hour, true)]
        [TestCase(Resolution.Minute, Resolution.Daily, true)]
        [TestCase(Resolution.Hour, Resolution.Tick, false)]
        [TestCase(Resolution.Hour, Resolution.Second, false)]
        [TestCase(Resolution.Hour, Resolution.Minute, false)]
        [TestCase(Resolution.Hour, Resolution.Hour, false)]
        [TestCase(Resolution.Hour, Resolution.Daily, false)]
        [TestCase(Resolution.Daily, Resolution.Tick, false)]
        [TestCase(Resolution.Daily, Resolution.Second, false)]
        [TestCase(Resolution.Daily, Resolution.Minute, false)]
        [TestCase(Resolution.Daily, Resolution.Hour, false)]
        [TestCase(Resolution.Daily, Resolution.Daily, false)]
        public void VerifyReceivedDataBasedOnHistoryResolutionOnly(Resolution historyResolution, Resolution equityResolution, bool expected)
        {
            var algorithm = GetAlgorithm(new DateTime(2013, 10, 1));
            algorithm.SetStartDate(2013, 10, 8);
            var spy = algorithm.AddEquity("SPY", Resolution.Minute).Symbol;
            var ibm = algorithm.AddEquity("IBM", equityResolution).Symbol;

            // Flags to check if there's Quote data for SPY and IBM
            var spyFlag = false;
            var ibmFlag = false;

            // If the history resolution is Tick, check for Quote-type ticks
            if (historyResolution == Resolution.Tick)
            {
                var start = new DateTime(2013, 10, 7, 15, 0, 0);
                var allHistory = algorithm.History(new[] { spy, ibm }, start, start.AddSeconds(5), historyResolution).SelectMany(slice => slice.AllData);
                // Filter the data to get only the Quote-type ticks
                var ticks = allHistory.OfType<Tick>().Where(e => e.TickType == TickType.Quote).ToList();
                spyFlag = ticks.Any(e => e.Symbol == spy);
                ibmFlag = ticks.Any(e => e.Symbol == ibm);
            }
            else
            {
                var allHistory = algorithm.History(new[] { spy, ibm }, TimeSpan.FromDays(1), historyResolution).SelectMany(slice => slice.AllData).ToList();
                // Checking for QuoteBar data for SPY and IBM
                var quoteBars = allHistory.Where(e => e.DataType == MarketDataType.QuoteBar).ToList();
                spyFlag |= quoteBars.Any(e => e.Symbol == spy);
                ibmFlag |= quoteBars.Any(e => e.Symbol == ibm);
            }

            // Assert that the flags match the expected value
            Assert.AreEqual(expected, spyFlag);
            Assert.AreEqual(expected, ibmFlag);
        }

        [Test]
        public void VerifyHistorySupportsSpecificDataTypes()
        {
            var algorithm = GetAlgorithm(new DateTime(2013, 10, 8));
            algorithm.SetStartDate(2013, 10, 10);
            var spy = algorithm.AddEquity("SPY", Resolution.Minute).Symbol;
            var ibm = algorithm.AddEquity("IBM", Resolution.Hour).Symbol;

            var tradeBarHistory = algorithm.History<TradeBar>(new[] { spy, ibm }, TimeSpan.FromDays(1), Resolution.Minute).ToList();
            var generalHistory = algorithm.History(new[] { spy, ibm }, TimeSpan.FromDays(1), Resolution.Minute).ToList();

            // Extract all TradeBars
            var tradeBars = tradeBarHistory.SelectMany(slice => slice.Values).ToList();

            // Filter and extract only TradeBars from the general history
            var filteredTradeBars = generalHistory.SelectMany(slice => slice.AllData).Where(e => e.DataType == MarketDataType.TradeBar).ToList();

            // Assert that the count of TradeBars in both methods is consistent
            Assert.AreEqual(filteredTradeBars.Count, tradeBars.Count);
        }

        [TestCase(Language.CSharp)]
        [TestCase(Language.Python)]
        public void TickResolutionPeriodBasedHistoryRequestThrowsException(Language language)
        {
            var spy = _algorithm.AddEquity("SPY", Resolution.Tick).Symbol;

            if (language == Language.CSharp)
            {
                Assert.Throws<InvalidOperationException>(() => _algorithm.History<Tick>(spy, 1).ToList());
                Assert.Throws<InvalidOperationException>(() => _algorithm.History<Tick>(spy, 1, Resolution.Tick).ToList());
                Assert.Throws<InvalidOperationException>(() => _algorithm.History<Tick>(new[] { spy }, 1).ToList());
                Assert.Throws<InvalidOperationException>(() => _algorithm.History<Tick>(new[] { spy }, 1, Resolution.Tick).ToList());
            }
            else
            {
                using (Py.GIL())
                {
                    _algorithm.SetPandasConverter();

                    foreach (var testCase in new[] { "return algorithm.History(Tick, symbol, 1)", "return algorithm.History(Tick, symbol, 1)",
                        "return algorithm.History(Tick, [ symbol ], 1)", "return algorithm.History(Tick, [ symbol ], 1, Resolution.Tick)" })
                    {
                        dynamic getTickHistory = PyModule.FromString("testModule",
                            @"from AlgorithmImports import *

def getTickHistory(algorithm, symbol):
    " + testCase).GetAttr("getTickHistory");

                        Assert.Throws<ClrBubbledException>(() => getTickHistory(_algorithm, spy));
                    }
                }
            }
        }

        [Test]
        public void TickResolutionHistoryRequestTradeBarApiThrowsException()
        {
            Assert.Throws<InvalidOperationException>(
                () => _algorithm.History(Symbols.SPY, 1, Resolution.Tick).ToList());

            Assert.Throws<InvalidOperationException>(
                () => _algorithm.History(Symbols.SPY, TimeSpan.FromSeconds(2), Resolution.Tick).ToList());

            Assert.Throws<InvalidOperationException>(
                () => _algorithm.History(Symbols.SPY, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow, Resolution.Tick).ToList());
        }

        [TestCase(Language.CSharp)]
        [TestCase(Language.Python)]
        public void GetsTickResolutionHistoricalDataWithoutATickSubscription(Language language)
        {
            var spy = _algorithm.AddEquity("SPY", Resolution.Daily).Symbol;
            var ibm = _algorithm.AddEquity("IBM", Resolution.Daily).Symbol;
            _algorithm.SetStartDate(2014, 6, 10);
            var start = new DateTime(2013, 10, 7);
            var end = new DateTime(2013, 10, 8);
            _algorithm.SetStartDate(2013, 10, 8);

            _testHistoryProvider.Slices = new[]
            {
                new Slice(start, new[] { new Tick(start, spy, 100, 100) { TickType = TickType.Trade } }, start),
                new Slice(start, new[] { new Tick(start, ibm, 200, 200) { TickType = TickType.Trade } }, start),
                new Slice(end, new[] { new Tick(end, spy, 110, 110) { TickType = TickType.Trade }, new Tick(end, ibm, 210, 210) { TickType = TickType.Trade } }, end)
            }.ToList();

            if (language == Language.CSharp)
            {
                var spyHistory = _algorithm.History<Tick>(spy, start, end, Resolution.Tick);
                Assert.AreEqual(2, spyHistory.Count());

                var ibmHistory = _algorithm.History<Tick>(ibm, start, end, Resolution.Tick);
                Assert.AreEqual(2, ibmHistory.Count());

                var allHistory = _algorithm.History<Tick>(new[] { spy, ibm }, start, end, Resolution.Tick);
                Assert.AreEqual(3, allHistory.Count());
            }
            else
            {
                using (Py.GIL())
                {
                    var getTickHistory = PyModule.FromString("testModule",
                        @"
from AlgorithmImports import *

def getTickHistory(algorithm, symbol, start, end):
    history = algorithm.History(Tick, symbol, start, end, Resolution.Tick)
    history = history if isinstance(symbol, list) else history.loc[symbol]

    return history.values.tolist()

        ").GetAttr("getTickHistory");

                    _algorithm.SetPandasConverter();
                    using var pyAlgorithm = _algorithm.ToPython();
                    using var pySpy = spy.ToPython();
                    using var pyIbm = ibm.ToPython();
                    using var pySymbols = new PyList(new[] { pySpy, pyIbm });
                    using var pyStart = start.ToPython();
                    using var pyEnd = end.ToPython();

                    var spyHistory = getTickHistory.Invoke(pyAlgorithm, pySpy, pyStart, pyEnd).As<List<dynamic>>();
                    Assert.AreEqual(2, spyHistory.Count);

                    var ibmHistory = getTickHistory.Invoke(pyAlgorithm, pyIbm, pyStart, pyEnd).As<List<dynamic>>();
                    Assert.AreEqual(2, ibmHistory.Count);

                    var allHistory = getTickHistory.Invoke(pyAlgorithm, pySymbols, pyStart, pyEnd).As<List<dynamic>>();
                    Assert.AreEqual(4, allHistory.Count);
                }
            }
        }

        [TestCase(Resolution.Second, Language.CSharp, true)]
        [TestCase(Resolution.Minute, Language.CSharp, true)]
        [TestCase(Resolution.Hour, Language.CSharp, true)]
        [TestCase(Resolution.Daily, Language.CSharp, true)]
        [TestCase(Resolution.Second, Language.Python, true)]
        [TestCase(Resolution.Minute, Language.Python, true)]
        [TestCase(Resolution.Hour, Language.Python, true)]
        [TestCase(Resolution.Daily, Language.Python, true)]
        [TestCase(Resolution.Second, Language.CSharp, false)]
        [TestCase(Resolution.Minute, Language.CSharp, false)]
        [TestCase(Resolution.Hour, Language.CSharp, false)]
        [TestCase(Resolution.Daily, Language.CSharp, false)]
        [TestCase(Resolution.Second, Language.Python, false)]
        [TestCase(Resolution.Minute, Language.Python, false)]
        [TestCase(Resolution.Hour, Language.Python, false)]
        [TestCase(Resolution.Daily, Language.Python, false)]
        public void TimeSpanHistoryRequestIsCorrectlyBuilt(Resolution resolution, Language language, bool symbolAlreadyAdded)
        {
            _algorithm.SetStartDate(2013, 10, 07);

            var symbol = Symbols.SPY;
            if (symbolAlreadyAdded)
            {
                // it should not matter
                _algorithm.AddEquity("SPY");
            }
            if (language == Language.CSharp)
            {
                _algorithm.History(symbol, TimeSpan.FromSeconds(2), resolution);
            }
            else
            {
                using (Py.GIL())
                {
                    _algorithm.SetPandasConverter();
                    _algorithm.History(symbol.ToPython(), TimeSpan.FromSeconds(2), resolution);
                }
            }

            Resolution? fillForwardResolution = null;
            if (resolution != Resolution.Tick)
            {
                fillForwardResolution = resolution;
            }

            var expectedCount = resolution == Resolution.Hour || resolution == Resolution.Daily ? 1 : 2;
            Assert.AreEqual(expectedCount, _testHistoryProvider.HistryRequests.Count);
            Assert.AreEqual(symbol, _testHistoryProvider.HistryRequests.First().Symbol);
            Assert.AreEqual(resolution, _testHistoryProvider.HistryRequests.First().Resolution);
            Assert.IsFalse(_testHistoryProvider.HistryRequests.First().IncludeExtendedMarketHours);
            Assert.IsFalse(_testHistoryProvider.HistryRequests.First().IsCustomData);
            Assert.AreEqual(fillForwardResolution, _testHistoryProvider.HistryRequests.First().FillForwardResolution);
            Assert.AreEqual(DataNormalizationMode.Adjusted, _testHistoryProvider.HistryRequests.First().DataNormalizationMode);
            Assert.AreEqual(expectedCount == 1 ? TickType.Trade : TickType.Quote, _testHistoryProvider.HistryRequests.First().TickType);
        }

        private static IEnumerable<TestCaseData> BarCountHistoryRequestTestCases
        {
            get
            {
                var spyDate = new DateTime(2013, 10, 07);

                yield return new TestCaseData(Resolution.Second, Language.CSharp, Symbols.SPY, true, spyDate, null, false);
                yield return new TestCaseData(Resolution.Minute, Language.CSharp, Symbols.SPY, true, spyDate, null, false);
                yield return new TestCaseData(Resolution.Hour, Language.CSharp, Symbols.SPY, true, spyDate, null, false);
                yield return new TestCaseData(Resolution.Daily, Language.CSharp, Symbols.SPY, true, spyDate, null, false);
                yield return new TestCaseData(Resolution.Second, Language.Python, Symbols.SPY, true, spyDate, null, false);
                yield return new TestCaseData(Resolution.Minute, Language.Python, Symbols.SPY, true, spyDate, null, false);
                yield return new TestCaseData(Resolution.Hour, Language.Python, Symbols.SPY, true, spyDate, null, false);
                yield return new TestCaseData(Resolution.Daily, Language.Python, Symbols.SPY, true, spyDate, null, false);
                yield return new TestCaseData(Resolution.Second, Language.CSharp, Symbols.SPY, false, spyDate, null, false);
                yield return new TestCaseData(Resolution.Minute, Language.CSharp, Symbols.SPY, false, spyDate, null, false);
                yield return new TestCaseData(Resolution.Hour, Language.CSharp, Symbols.SPY, false, spyDate, null, false);
                yield return new TestCaseData(Resolution.Daily, Language.CSharp, Symbols.SPY, false, spyDate, null, false);
                yield return new TestCaseData(Resolution.Second, Language.Python, Symbols.SPY, false, spyDate, null, false);
                yield return new TestCaseData(Resolution.Minute, Language.Python, Symbols.SPY, false, spyDate, null, false);
                yield return new TestCaseData(Resolution.Hour, Language.Python, Symbols.SPY, false, spyDate, null, false);
                yield return new TestCaseData(Resolution.Daily, Language.Python, Symbols.SPY, false, spyDate, null, false);

                yield return new TestCaseData(Resolution.Second, Language.CSharp, Symbols.SPY, true, spyDate, null, true);
                yield return new TestCaseData(Resolution.Minute, Language.CSharp, Symbols.SPY, true, spyDate, null, true);
                yield return new TestCaseData(Resolution.Hour, Language.CSharp, Symbols.SPY, true, spyDate, null, true);
                yield return new TestCaseData(Resolution.Daily, Language.CSharp, Symbols.SPY, true, spyDate, null, true);
                yield return new TestCaseData(Resolution.Second, Language.Python, Symbols.SPY, true, spyDate, null, true);
                yield return new TestCaseData(Resolution.Minute, Language.Python, Symbols.SPY, true, spyDate, null, true);
                yield return new TestCaseData(Resolution.Hour, Language.Python, Symbols.SPY, true, spyDate, null, true);
                yield return new TestCaseData(Resolution.Daily, Language.Python, Symbols.SPY, true, spyDate, null, true);
                yield return new TestCaseData(Resolution.Second, Language.CSharp, Symbols.SPY, false, spyDate, null, true);
                yield return new TestCaseData(Resolution.Minute, Language.CSharp, Symbols.SPY, false, spyDate, null, true);
                yield return new TestCaseData(Resolution.Hour, Language.CSharp, Symbols.SPY, false, spyDate, null, true);
                yield return new TestCaseData(Resolution.Daily, Language.CSharp, Symbols.SPY, false, spyDate, null, true);
                yield return new TestCaseData(Resolution.Second, Language.Python, Symbols.SPY, false, spyDate, null, true);
                yield return new TestCaseData(Resolution.Minute, Language.Python, Symbols.SPY, false, spyDate, null, true);
                yield return new TestCaseData(Resolution.Hour, Language.Python, Symbols.SPY, false, spyDate, null, true);
                yield return new TestCaseData(Resolution.Daily, Language.Python, Symbols.SPY, false, spyDate, null, true);

                var spxCanonicalOption = Symbol.CreateCanonicalOption(Symbols.SPX);
                var spxDate = new DateTime(2021, 01, 12);

                yield return new TestCaseData(Resolution.Daily, Language.CSharp, spxCanonicalOption, true, spxDate, null, true);
                yield return new TestCaseData(Resolution.Daily, Language.Python, spxCanonicalOption, true, spxDate, null, true);
                yield return new TestCaseData(null, Language.CSharp, spxCanonicalOption, true, spxDate, Resolution.Daily, true);
                yield return new TestCaseData(null, Language.Python, spxCanonicalOption, true, spxDate, Resolution.Daily, true);
                yield return new TestCaseData(Resolution.Daily, Language.CSharp, spxCanonicalOption, false, spxDate, null, true);
                yield return new TestCaseData(Resolution.Daily, Language.Python, spxCanonicalOption, false, spxDate, null, true);
                yield return new TestCaseData(null, Language.CSharp, spxCanonicalOption, false, spxDate, Resolution.Daily, true);
                yield return new TestCaseData(null, Language.Python, spxCanonicalOption, false, spxDate, Resolution.Daily, true);
            }
        }

        [TestCaseSource(nameof(BarCountHistoryRequestTestCases))]
        public void BarCountHistoryRequestIsCorrectlyBuilt(Resolution? resolution, Language language, Symbol symbol,
            bool symbolAlreadyAdded, DateTime dateTime, Resolution? defaultResolution, bool multiSymbol)
        {
            _algorithm.SetStartDate(dateTime);

            if (symbolAlreadyAdded)
            {
                // it should not matter
                _algorithm.AddSecurity(symbol);
            }

            if (language == Language.CSharp)
            {
                if (multiSymbol)
                {
                    _algorithm.History(new[] { symbol }, 10, resolution);
                }
                else
                {
                    _algorithm.History(symbol, 10, resolution);
                }
            }
            else
            {
                using (Py.GIL())
                {
                    _algorithm.SetPandasConverter();
                    if (multiSymbol)
                    {
                        using var pySymbols = new[] { symbol }.ToPyListUnSafe();
                        _algorithm.History(pySymbols, 10, resolution);

                        pySymbols[0].Dispose();
                    }
                    else
                    {
                        using var pySymbol = symbol.ToPython();
                        _algorithm.History(pySymbol, 10, resolution);
                    }
                }
            }

            Resolution? fillForwardResolution = null;
            if (resolution != Resolution.Tick)
            {
                fillForwardResolution = resolution ?? defaultResolution;
            }

            if (symbol.SecurityType == SecurityType.Equity)
            {
                var expectedCount = resolution == Resolution.Hour || resolution == Resolution.Daily ? 1 : 2;
                Assert.AreEqual(expectedCount, _testHistoryProvider.HistryRequests.Count);
                var request = _testHistoryProvider.HistryRequests.First();
                Assert.AreEqual(symbol, request.Symbol);
                Assert.AreEqual(resolution, request.Resolution);
                Assert.IsFalse(request.IncludeExtendedMarketHours);
                Assert.IsFalse(request.IsCustomData);
                Assert.AreEqual(fillForwardResolution, request.FillForwardResolution);
                Assert.AreEqual(DataNormalizationMode.Adjusted, request.DataNormalizationMode);

                Assert.AreEqual(expectedCount == 1 ? TickType.Trade : TickType.Quote, request.TickType);
            }
            else if (symbol.SecurityType == SecurityType.IndexOption)
            {
                Assert.AreEqual(1, _testHistoryProvider.HistryRequests.Count);
                var request = _testHistoryProvider.HistryRequests.Single();
                Assert.AreEqual(symbol, request.Symbol);
                Assert.AreEqual(resolution ?? defaultResolution, request.Resolution);
                Assert.AreEqual(typeof(OptionUniverse), request.DataType);
                Assert.IsFalse(request.IncludeExtendedMarketHours);
                Assert.IsFalse(request.IsCustomData);
            }
        }

        [TestCase(Language.CSharp, true)]
        [TestCase(Language.Python, true)]
        [TestCase(Language.CSharp, false)]
        [TestCase(Language.Python, false)]
        public void TickHistoryRequestIgnoresFillForward(Language language, bool symbolAlreadyAdded)
        {
            _algorithm.SetStartDate(2013, 10, 07);

            var symbol = Symbols.SPY;
            if (symbolAlreadyAdded)
            {
                // it should not matter
                _algorithm.AddEquity("SPY");
            }
            if (language == Language.CSharp)
            {
                _algorithm.History(new[] { symbol }, new DateTime(1, 1, 1, 1, 1, 1), new DateTime(1, 1, 1, 1, 1, 2), Resolution.Tick,
                    fillForward: true);
            }
            else
            {
                using (Py.GIL())
                {
                    _algorithm.SetPandasConverter();
                    using var symbols = new PyList(new[] { symbol.ToPython() });
                    _algorithm.History(symbols, new DateTime(1, 1, 1, 1, 1, 1), new DateTime(1, 1, 1, 1, 1, 2),
                        Resolution.Tick, fillForward: true);
                }
            }

            Assert.AreEqual(2, _testHistoryProvider.HistryRequests.Count);
            Assert.AreEqual(symbol, _testHistoryProvider.HistryRequests.First().Symbol);
            Assert.AreEqual(Resolution.Tick, _testHistoryProvider.HistryRequests.First().Resolution);
            Assert.IsFalse(_testHistoryProvider.HistryRequests.First().IncludeExtendedMarketHours);
            Assert.IsFalse(_testHistoryProvider.HistryRequests.First().IsCustomData);
            Assert.AreEqual(null, _testHistoryProvider.HistryRequests.First().FillForwardResolution);
            Assert.AreEqual(DataNormalizationMode.Adjusted, _testHistoryProvider.HistryRequests.First().DataNormalizationMode);
            Assert.AreEqual(TickType.Quote, _testHistoryProvider.HistryRequests.First().TickType);
        }

        [Test]
        public void GetLastKnownPriceOfIlliquidAsset_RealData()
        {
            var algorithm = GetAlgorithm(new DateTime(2014, 6, 6, 11, 0, 0));

            //20140606_twx_minute_quote_american_call_230000_20150117.csv
            var optionSymbol = Symbol.CreateOption("TWX", Market.USA, OptionStyle.American, OptionRight.Call, 23, new DateTime(2015, 1, 17));
            var option = algorithm.AddOptionContract(optionSymbol);

            var lastKnownPrice = algorithm.GetLastKnownPrice(option);
            Assert.IsNotNull(lastKnownPrice);

            // Data gap of more than 15 minutes
            Assert.Greater((algorithm.Time - lastKnownPrice.EndTime).TotalMinutes, 15);
        }

        [Test]
        public void GetLastKnownPriceOfIlliquidAsset_TestData()
        {
            // Set the start date on Tuesday
            _algorithm.SetStartDate(2014, 6, 10);

            var optionSymbol = Symbol.CreateOption("TWX", Market.USA, OptionStyle.American, OptionRight.Call, 23, new DateTime(2015, 1, 17));
            var option = _algorithm.AddOptionContract(optionSymbol);

            // The last known price is on Friday, so we missed data from Monday and no data during Weekend
            var barTime = new DateTime(2014, 6, 6, 15, 0, 0, 0);
            _testHistoryProvider.Slices = new[]
            {
                new Slice(barTime, new[] { new TradeBar(barTime, optionSymbol, 100, 100, 100, 100, 1) }, barTime)
            }.ToList();

            var lastKnownPrice = _algorithm.GetLastKnownPrice(option);
            Assert.IsNotNull(lastKnownPrice);
            Assert.AreEqual(barTime.AddMinutes(1), lastKnownPrice.EndTime);
        }

        [Test]
        public void GetLastKnownPricesEquity()
        {
            var algorithm = GetAlgorithm(new DateTime(2013, 10, 8));

            var equity = algorithm.AddEquity("SPY");

            var lastKnownPrices = algorithm.GetLastKnownPrices(equity.Symbol).ToList();
            Assert.AreEqual(2, lastKnownPrices.Count);
            Assert.AreEqual(1, lastKnownPrices.Count(data => data.GetType() == typeof(TradeBar)));
            Assert.AreEqual(1, lastKnownPrices.Count(data => data.GetType() == typeof(QuoteBar)));
        }

        [TestCase(Resolution.Daily)]
        [TestCase(Resolution.Minute)]
        [TestCase(Resolution.Hour)]
        public void GetLastKnownPricesUsesCorrectResolution(Resolution resolution)
        {
            var algorithm = GetAlgorithm(new DateTime(2013, 10, 8));

            algorithm.SetSecurityInitializer(security =>
            {
                var lastKnownPrices = algorithm.GetLastKnownPrices(security).ToList();
                var data = lastKnownPrices.Where(x => x.GetType() == typeof(TradeBar)).Single().ConvertInvariant<TradeBar>();
                var expectedPeriod = new TimeSpan();
                switch (resolution)
                {
                    case Resolution.Daily:
                        expectedPeriod = TimeSpan.FromHours(6.5);
                        break;
                    case Resolution.Minute:
                        expectedPeriod = TimeSpan.FromMinutes(1);
                        break;
                    case Resolution.Hour:
                        expectedPeriod = TimeSpan.FromHours(1);
                        break;
                }

                Assert.AreEqual(expectedPeriod, data.Period);
            });

            algorithm.AddEquity("SPY", resolution);
        }

        [TestCase(Language.CSharp)]
        [TestCase(Language.Python)]
        public void GetLastKnownPricesCustomData(Language language)
        {
            var algorithm = GetAlgorithm(new DateTime(2013, 10, 8));

            Symbol symbol;
            if (language == Language.CSharp)
            {
                symbol = algorithm.AddData<CustomData>("SPY").Symbol;
            }
            else
            {
                using (Py.GIL())
                {
                    PythonInitializer.Initialize();

                    var customDataType = PyModule.FromString("testModule",
                        @"
from AlgorithmImports import *
from QuantConnect.Tests import *

class Test(PythonData):
    def GetSource(self, config, date, isLiveMode):
        fileName = LeanData.GenerateZipFileName(Symbols.SPY, date, config.Resolution, config.TickType)
        source = f'{Globals.DataFolder}equity/usa/minute/spy/{fileName}'
        return SubscriptionDataSource(source, SubscriptionTransportMedium.LocalFile, FileFormat.Csv)

    def Reader(self, config, line, date, isLiveMode):

        data = line.split(',')

        result = Test()
        result.DataType = MarketDataType.Base
        result.Symbol = config.Symbol
        result.Time = date + timedelta(milliseconds=int(data[0]))
        result.Value = 1

        return result
        ").GetAttr("Test");
                    symbol = algorithm.AddData(customDataType, "SPY").Symbol;
                }
            }

            var lastKnownPrices = algorithm.GetLastKnownPrices(symbol).ToList();
            Assert.AreEqual(1, lastKnownPrices.Count);
            if (language == Language.CSharp)
            {
                Assert.AreEqual(1, lastKnownPrices.Count(data => data.GetType() == typeof(CustomData)));
            }
            else
            {
                Assert.AreEqual(1, lastKnownPrices.Count(data => data.GetType() == typeof(PythonData)));
            }
        }

        [Test]
        public void GetLastKnownPriceEquity()
        {
            var algorithm = GetAlgorithm(new DateTime(2013, 10, 8));

            var equity = algorithm.AddEquity("SPY");

            var lastKnownPrice = algorithm.GetLastKnownPrice(equity);
            Assert.AreEqual(typeof(TradeBar), lastKnownPrice.GetType());
        }

        [Test]
        public void GetLastKnownPriceOption()
        {
            var algorithm = GetAlgorithm(new DateTime(2014, 06, 09));

            var option = algorithm.AddOptionContract(Symbols.CreateOptionSymbol("AAPL", OptionRight.Call, 250m, new DateTime(2016, 01, 15)));

            var lastKnownPrice = algorithm.GetLastKnownPrice(option);
            Assert.AreEqual(typeof(QuoteBar), lastKnownPrice.GetType());
        }

        [Test]
        public void GetLastKnownPricesOption()
        {
            var algorithm = GetAlgorithm(new DateTime(2014, 06, 09));

            var option = algorithm.AddOptionContract(Symbols.CreateOptionSymbol("AAPL", OptionRight.Call, 250m, new DateTime(2016, 01, 15)));

            var lastKnownPrices = algorithm.GetLastKnownPrices(option).ToList();
            Assert.AreEqual(2, lastKnownPrices.Count);
            Assert.AreEqual(1, lastKnownPrices.Count(data => data.GetType() == typeof(TradeBar)));
            Assert.AreEqual(1, lastKnownPrices.Count(data => data.GetType() == typeof(QuoteBar)));
        }

        [Test]
        public void GetLastKnownPriceFuture()
        {
            var algorithm = GetAlgorithm(new DateTime(2013, 10, 8));

            var future = algorithm.AddSecurity(Symbols.CreateFutureSymbol(Futures.Indices.SP500EMini, new DateTime(2013, 12, 20)));

            var lastKnownPrice = algorithm.GetLastKnownPrice(future);
            Assert.AreEqual(typeof(QuoteBar), lastKnownPrice.GetType());
        }

        [Test]
        public void GetLastKnownPricesFuture()
        {
            var algorithm = GetAlgorithm(new DateTime(2013, 10, 8));

            var future = algorithm.AddSecurity(Symbols.CreateFutureSymbol(Futures.Indices.SP500EMini, new DateTime(2013, 12, 20)));

            var lastKnownPrices = algorithm.GetLastKnownPrices(future).ToList();
            Assert.AreEqual(2, lastKnownPrices.Count);
            Assert.AreEqual(1, lastKnownPrices.Count(data => data.GetType() == typeof(TradeBar)));
            Assert.AreEqual(1, lastKnownPrices.Count(data => data.GetType() == typeof(QuoteBar)));
        }

        [TestCase(Language.CSharp)]
        [TestCase(Language.Python)]
        public void TickResolutionOpenInterestHistoryRequestIsNotFilteredWhenRequestedExplicitly(Language language)
        {
            var start = new DateTime(2014, 6, 05);
            var end = start.AddDays(10);
            _algorithm = GetAlgorithm(start);
            _algorithm.SetStartDate(start);
            _algorithm.SetDateTime(end);

            _algorithm.UniverseSettings.FillForward = false;
            var optionSymbol = Symbol.CreateOption("TWX", Market.USA, OptionStyle.American, OptionRight.Call, 45, new DateTime(2015, 1, 17));
            var historyResolution = Resolution.Minute;

            if (language == Language.CSharp)
            {
                var openInterests = _algorithm.History<OpenInterest>(optionSymbol, start, end, historyResolution).ToList();

                Assert.AreEqual(2, openInterests.Count);
                Assert.AreEqual(new DateTime(2014, 06, 05, 6, 31, 0), openInterests[0].Time);
                Assert.AreEqual(optionSymbol, openInterests[0].Symbol);
                Assert.AreEqual(new DateTime(2014, 06, 06, 6, 30, 0), openInterests[1].Time);
                Assert.AreEqual(optionSymbol, openInterests[1].Symbol);
            }
            else
            {
                using (Py.GIL())
                {
                    var getOpenInterestHistory = PyModule.FromString("testModule",
                        @"
from AlgorithmImports import *

def getOpenInterestHistory(algorithm, symbol, start, end, resolution):
    return algorithm.History(OpenInterest, symbol, start, end, resolution).reset_index().to_dict()
        ").GetAttr("getOpenInterestHistory");

                    _algorithm.SetPandasConverter();
                    using var symbols = new PyList(new[] { optionSymbol.ToPython() });
                    using var dict = getOpenInterestHistory
                        .Invoke(_algorithm.ToPython(), symbols, start.ToPython(), end.ToPython(),
                            historyResolution.ToPython());
                    var openInterestsDataFrameDict = GetDataFrameDictionary<PyObject>(dict);

                    Assert.That(openInterestsDataFrameDict, Does.ContainKey("openinterest"));
                    Assert.That(openInterestsDataFrameDict, Does.ContainKey("time"));

                    var openInterests = openInterestsDataFrameDict["openinterest"].ConvertToDictionary<int, decimal>();
                    var times = openInterestsDataFrameDict["time"].ConvertToDictionary<int, DateTime>();

                    Assert.That(openInterests, Has.Count.EqualTo(2));
                    Assert.That(times, Has.Count.EqualTo(2));
                    Assert.That(times[0], Is.EqualTo(new DateTime(2014, 06, 05, 6, 31, 0)));
                    Assert.That(times[1], Is.EqualTo(new DateTime(2014, 06, 06, 6, 30, 0)));
                }
            }
        }

        [TestCase(Language.CSharp)]
        [TestCase(Language.Python)]
        public void TickResolutionOpenInterestHistoryRequestIsFilteredByDefault_SingleSymbol(Language language)
        {
            var start = new DateTime(2014, 6, 05);
            var end = start.AddDays(2);
            var historyResolution = Resolution.Minute;
            _algorithm = GetAlgorithm(start);
            _algorithm.SetStartDate(start);
            _algorithm.SetDateTime(end);

            var optionSymbol = Symbol.CreateOption("TWX", Market.USA, OptionStyle.American, OptionRight.Call, 23, new DateTime(2015, 1, 17));

            if (language == Language.CSharp)
            {
                var result = _algorithm.History(new[] { optionSymbol }, start, end, historyResolution, fillForward: false).ToList();

                Assert.Multiple(() =>
                {
                    Assert.AreEqual(53, result.Count);
                    Assert.IsTrue(result.Any(slice => slice.ContainsKey(optionSymbol)));

                    var openInterests = result.Select(slice => slice.Get(typeof(OpenInterest)) as DataDictionary<OpenInterest>).Where(dataDictionary => dataDictionary.Count > 0).ToList();

                    Assert.AreEqual(0, openInterests.Count);
                });
            }
            else
            {
                using (Py.GIL())
                {
                    var getOpenInterestHistory = PyModule.FromString("testModule",
                        @"
from AlgorithmImports import *

def getOpenInterestHistory(algorithm, symbol, start, end, resolution):
    return algorithm.History(symbol, start, end, resolution)
        ").GetAttr("getOpenInterestHistory");

                    _algorithm.SetPandasConverter();
                    using var symbols = new PyList(new[] { optionSymbol.ToPython() });
                    using var openInterests = getOpenInterestHistory.Invoke(_algorithm.ToPython(), symbols, start.ToPython(), end.ToPython(),
                        historyResolution.ToPython());
                    Assert.AreEqual(780, openInterests.GetAttr("shape")[0].As<int>());

                    using var dict = openInterests.GetAttr("to_dict").Invoke();
                    var dataFrameDict = GetDataFrameDictionary<dynamic>(dict);
                    Assert.That(dataFrameDict, Does.Not.ContainKey("openinterest"));
                }
            }
        }

        [TestCase(Language.CSharp)]
        [TestCase(Language.Python)]
        public void TickResolutionOpenInterestHistoryRequestIsFilteredByDefault_MultipleSymbols(Language language)
        {
            var start = new DateTime(2014, 6, 05);
            var end = start.AddDays(2);
            var historyResolution = Resolution.Minute;
            _algorithm = GetAlgorithm(start.AddDays(1));
            _algorithm.SetStartDate(start);
            _algorithm.SetDateTime(start.AddDays(2));

            var optionSymbol = Symbol.CreateOption("TWX", Market.USA, OptionStyle.American, OptionRight.Call, 23, new DateTime(2015, 1, 17));
            var optionSymbol2 = Symbol.CreateOption("AAPL", Market.USA, OptionStyle.American, OptionRight.Call, 500, new DateTime(2015, 1, 17));

            if (language == Language.CSharp)
            {
                var result = _algorithm.History(new[] { optionSymbol, optionSymbol2 }, start, end, historyResolution, fillForward: false).ToList();

                Assert.AreEqual(415, result.Count);
                Assert.IsTrue(result.Any(slice => slice.ContainsKey(optionSymbol)));
                Assert.IsTrue(result.Any(slice => slice.ContainsKey(optionSymbol2)));

                var openInterests = result.Select(slice => slice.Get(typeof(OpenInterest)) as DataDictionary<OpenInterest>).Where(dataDictionary => dataDictionary.Count > 0).ToList();

                Assert.AreEqual(0, openInterests.Count);
            }
            else
            {
                using (Py.GIL())
                {
                    var getOpenInterestHistory = PyModule.FromString("testModule",
                        @"
from AlgorithmImports import *

def getOpenInterestHistory(algorithm, symbol, start, end, resolution):
    return algorithm.History(symbol, start, end, resolution)
        ").GetAttr("getOpenInterestHistory");

                    _algorithm.SetPandasConverter();
                    using var symbols = new PyList(new[] { optionSymbol.ToPython(), optionSymbol2.ToPython() });
                    var result = getOpenInterestHistory
                        .Invoke(_algorithm.ToPython(), symbols, start.ToPython(), end.ToPython(), historyResolution.ToPython());
                    Assert.AreEqual(1170, result.GetAttr("shape")[0].As<int>());

                    var dataFrameDict = GetDataFrameDictionary<PyObject>(result
                        .GetAttr("reset_index").Invoke()
                        .GetAttr("to_dict").Invoke());
                    var dataFrameSymbols = dataFrameDict["symbol"].ConvertToDictionary<int, string>().Values.ToHashSet();
                    CollectionAssert.AreEquivalent(dataFrameSymbols, new[] { optionSymbol.ID.ToString(), optionSymbol2.ID.ToString() });

                    Assert.That(dataFrameDict, Does.Not.ContainKey("openinterest"));
                }
            }
        }

        [TestCase(Language.CSharp)]
        [TestCase(Language.Python)]
        public void SubscriptionHistoryRequestWithDifferentDataMappingMode(Language language)
        {
            var dataMappingModes = GetAllDataMappingModes();
            var historyStart = new DateTime(2013, 10, 6);
            var historyEnd = new DateTime(2014, 1, 1);
            var resolution = Resolution.Daily;
            _algorithm = GetAlgorithm(historyEnd);
            var symbol = _algorithm.AddFuture(Futures.Indices.SP500EMini, resolution, dataMappingMode: dataMappingModes.First(),
                extendedMarketHours: true).Symbol;
            var expectedHistoryCount = 61;

            if (language == Language.CSharp)
            {
                var historyResults = dataMappingModes
                     .Select(x => _algorithm.History(new[] { symbol }, historyStart, historyEnd, resolution, dataMappingMode: x).ToList())
                     .ToList();

                CheckThatHistoryResultsHaveEqualBarCount(historyResults, expectedHistoryCount);

                // Check that all history results have a mapping date at some point in the history
                HashSet<DateTime> mappingDates = new HashSet<DateTime>();
                for (int i = 0; i < historyResults.Count; i++)
                {
                    var underlying = historyResults[i].First().Bars.Keys.First().Underlying;
                    int mappingsCount = 0;

                    foreach (var slice in historyResults[i])
                    {
                        var dataUnderlying = slice.Bars.Keys.First().Underlying;
                        if (dataUnderlying != underlying)
                        {
                            underlying = dataUnderlying;
                            mappingsCount++;
                            mappingDates.Add(slice.Time.Date);
                        }
                    }

                    if (mappingsCount == 0)
                    {
                        throw new Exception($"History results for {dataMappingModes[i]} data mapping mode did not contain any mappings");
                    }
                }

                if (mappingDates.Count < dataMappingModes.Length)
                {
                    throw new Exception("History results should have had different mapping dates for each data mapping mode");
                }

                CheckThatHistoryResultsHaveDifferentPrices(historyResults,
                    "History results prices should have been different for each data mapping mode at each time");
            }
            else
            {
                using (Py.GIL())
                {
                    _algorithm.SetPandasConverter();
                    using var symbols = new PyList(new[] { symbol.ToPython() });
                    var historyResults = dataMappingModes
                        .Select(x => _algorithm.History(symbols, historyStart, historyEnd, resolution, dataMappingMode: x))
                        .ToList();

                    CheckThatHistoryResultsHaveEqualBarCount(historyResults, expectedHistoryCount);
                    CheckThatHistoryResultsHaveDifferentPrices(historyResults,
                        "History results prices should have been different for each data mapping mode at each time");
                }
            }
        }

        [TestCase(DataNormalizationMode.BackwardsRatio, Language.CSharp)]
        [TestCase(DataNormalizationMode.BackwardsPanamaCanal, Language.CSharp)]
        [TestCase(DataNormalizationMode.ForwardPanamaCanal, Language.CSharp)]
        [TestCase(DataNormalizationMode.BackwardsRatio, Language.Python)]
        [TestCase(DataNormalizationMode.BackwardsPanamaCanal, Language.Python)]
        [TestCase(DataNormalizationMode.ForwardPanamaCanal, Language.Python)]

        public void HistoryThrowsForUnsupportedDataNormalizationMode_Equity(DataNormalizationMode dataNormalizationMode, Language language)
        {
            _algorithm = GetAlgorithmWithEquity(new DateTime(2014, 6, 6));
            Assert.AreEqual(2, _algorithm.SubscriptionManager.Subscriptions.ToList().Count);
            var equity = _algorithm.SubscriptionManager.Subscriptions.First();
            Assert.AreEqual(SecurityType.Equity, equity.SecurityType);

            var start = _algorithm.Time.AddDays(-1);
            var end = _algorithm.Time;
            TestDelegate historyCall;

            if (language == Language.CSharp)
            {
                historyCall = () =>
                {
                    _algorithm.History(new[] { equity.Symbol }, start, end, equity.Resolution,
                        dataNormalizationMode: dataNormalizationMode).ToList();
                };
            }
            else
            {
                historyCall = () =>
                {
                    using (Py.GIL())
                    {
                        _algorithm.SetPandasConverter();
                        var symbols = new PyList(new[] { equity.Symbol.ToPython() });
                        _algorithm.History(symbols, start, end, equity.Resolution, dataNormalizationMode: dataNormalizationMode);
                    }
                };
            }

            Assert.Throws<ArgumentOutOfRangeException>(historyCall);
        }

        [TestCase(DataNormalizationMode.Adjusted, Language.CSharp)]
        [TestCase(DataNormalizationMode.SplitAdjusted, Language.CSharp)]
        [TestCase(DataNormalizationMode.TotalReturn, Language.CSharp)]
        [TestCase(DataNormalizationMode.Adjusted, Language.Python)]
        [TestCase(DataNormalizationMode.SplitAdjusted, Language.Python)]
        [TestCase(DataNormalizationMode.TotalReturn, Language.Python)]
        public void HistoryThrowsForUnsupportedDataNormalizationMode_Future(DataNormalizationMode dataNormalizationMode, Language language)
        {
            _algorithm = GetAlgorithmWithFuture(new DateTime(2014, 1, 1));
            Assert.IsNotEmpty(_algorithm.SubscriptionManager.Subscriptions);
            var future = _algorithm.SubscriptionManager.Subscriptions.First();
            Assert.AreEqual(SecurityType.Future, future.SecurityType);

            var start = _algorithm.StartDate;
            var end = _algorithm.EndDate;
            TestDelegate historyCall;

            if (language == Language.CSharp)
            {
                historyCall = () =>
                {
                    _algorithm.History(new[] { future.Symbol }, start, end, future.Resolution,
                        dataNormalizationMode: dataNormalizationMode).ToList();
                };
            }
            else
            {
                historyCall = () =>
                {
                    using (Py.GIL())
                    {
                        _algorithm.SetPandasConverter();
                        var symbols = new PyList(new[] { future.Symbol.ToPython() });
                        _algorithm.History(symbols, start, end, future.Resolution, dataNormalizationMode: dataNormalizationMode);
                    }
                };
            }

            Assert.Throws<ArgumentOutOfRangeException>(historyCall);
        }

        [TestCase(DataNormalizationMode.Raw, Language.CSharp)]
        [TestCase(DataNormalizationMode.Adjusted, Language.CSharp)]
        [TestCase(DataNormalizationMode.SplitAdjusted, Language.CSharp)]
        [TestCase(DataNormalizationMode.TotalReturn, Language.CSharp)]
        [TestCase(DataNormalizationMode.Raw, Language.Python)]
        [TestCase(DataNormalizationMode.Adjusted, Language.Python)]
        [TestCase(DataNormalizationMode.SplitAdjusted, Language.Python)]
        [TestCase(DataNormalizationMode.TotalReturn, Language.Python)]
        public void HistoryDoesNotThrowForSupportedDataNormalizationMode_Equity(DataNormalizationMode dataNormalizationMode, Language language)
        {
            _algorithm = GetAlgorithmWithEquity(new DateTime(2014, 6, 6));
            Assert.AreEqual(2, _algorithm.SubscriptionManager.Subscriptions.ToList().Count);
            var equity = _algorithm.SubscriptionManager.Subscriptions.First();
            Assert.AreEqual(SecurityType.Equity, equity.SecurityType);

            var start = _algorithm.Time.AddDays(-1);
            var end = _algorithm.Time;
            TestDelegate historyCall;

            if (language == Language.CSharp)
            {
                historyCall = () =>
                {
                    _algorithm.History(new[] { equity.Symbol }, start, end, equity.Resolution,
                        dataNormalizationMode: dataNormalizationMode).ToList();
                };
            }
            else
            {
                historyCall = () =>
                {
                    using (Py.GIL())
                    {
                        _algorithm.SetPandasConverter();
                        var symbols = new PyList(new[] { equity.Symbol.ToPython() });
                        _algorithm.History(symbols, start, end, equity.Resolution, dataNormalizationMode: dataNormalizationMode);
                    }
                };
            }

            Assert.DoesNotThrow(historyCall);
        }

        [TestCase(DataNormalizationMode.Raw, Language.CSharp)]
        [TestCase(DataNormalizationMode.BackwardsRatio, Language.CSharp)]
        [TestCase(DataNormalizationMode.BackwardsPanamaCanal, Language.CSharp)]
        [TestCase(DataNormalizationMode.ForwardPanamaCanal, Language.CSharp)]
        [TestCase(DataNormalizationMode.Raw, Language.Python)]
        [TestCase(DataNormalizationMode.BackwardsRatio, Language.Python)]
        [TestCase(DataNormalizationMode.BackwardsPanamaCanal, Language.Python)]
        [TestCase(DataNormalizationMode.ForwardPanamaCanal, Language.Python)]
        public void HistoryDoesNotThrowForSupportedDataNormalizationMode_Future(DataNormalizationMode dataNormalizationMode, Language language)
        {
            _algorithm = GetAlgorithmWithFuture(new DateTime(2014, 1, 1));
            Assert.IsNotEmpty(_algorithm.SubscriptionManager.Subscriptions);
            var future = _algorithm.SubscriptionManager.Subscriptions.First();
            Assert.AreEqual(SecurityType.Future, future.SecurityType);

            var start = _algorithm.StartDate;
            var end = _algorithm.Time;
            TestDelegate historyCall;

            if (language == Language.CSharp)
            {
                historyCall = () =>
                {
                    _algorithm.History(new[] { future.Symbol }, start, end, future.Resolution,
                        dataNormalizationMode: dataNormalizationMode).ToList();
                };
            }
            else
            {
                historyCall = () =>
                {
                    using (Py.GIL())
                    {
                        _algorithm.SetPandasConverter();
                        var symbols = new PyList(new[] { future.Symbol.ToPython() });
                        _algorithm.History(symbols, start, end, future.Resolution, dataNormalizationMode: dataNormalizationMode);
                    }
                };
            }

            Assert.DoesNotThrow(historyCall);
        }

        [TestCase(Language.CSharp)]
        [TestCase(Language.Python)]
        public void SubscriptionHistoryRequestWithDifferentDataNormalizationModes_Equity(Language language)
        {
            var dataNormalizationModes = new DataNormalizationMode[]{
                DataNormalizationMode.Raw,
                DataNormalizationMode.Adjusted,
                DataNormalizationMode.SplitAdjusted
            };
            _algorithm = GetAlgorithmWithEquity(new DateTime(2014, 6, 6));
            var equity = _algorithm.SubscriptionManager.Subscriptions.First();

            using (Py.GIL())
            {
                _algorithm.SetPandasConverter();
                dynamic symbol = language == Language.CSharp ? equity.Symbol : equity.Symbol.ToPython();
                CheckHistoryResultsForDataNormalizationModes(_algorithm, symbol, _algorithm.Time.AddDays(-1), _algorithm.Time, equity.Resolution,
                    dataNormalizationModes, expectedHistoryCount: 390);
            }
        }

        [TestCase(Language.CSharp)]
        [TestCase(Language.Python)]
        public void SubscriptionHistoryRequestWithDifferentDataNormalizationModes_Future(Language language)
        {
            var dataNormalizationModes = new DataNormalizationMode[]{
                DataNormalizationMode.Raw,
                DataNormalizationMode.BackwardsRatio,
                DataNormalizationMode.BackwardsPanamaCanal,
                DataNormalizationMode.ForwardPanamaCanal
            };
            _algorithm = GetAlgorithmWithFuture(new DateTime(2014, 1, 1));
            var future = _algorithm.SubscriptionManager.Subscriptions.First();

            using (Py.GIL())
            {
                _algorithm.SetPandasConverter();
                dynamic symbol = language == Language.CSharp ? future.Symbol : future.Symbol.ToPython();
                CheckHistoryResultsForDataNormalizationModes(_algorithm, symbol, new DateTime(2013, 10, 6), _algorithm.Time, future.Resolution,
                    dataNormalizationModes, expectedHistoryCount: 61);
            }
        }

        [TestCase(Language.CSharp)]
        [TestCase(Language.Python)]
        public void SubscriptionHistoryRequestForContinuousContractsWithDifferentDepthOffsets(Language language)
        {
            var start = new DateTime(2013, 10, 6);
            var end = new DateTime(2014, 1, 1);
            _algorithm = GetAlgorithmWithFuture(end);
            var future = _algorithm.SubscriptionManager.Subscriptions.First();
            var expectedHistoryCount = 61;

            if (language == Language.CSharp)
            {
                Func<int, List<Slice>> getHistoryForContractDepthOffset = (contractDepthOffset) =>
                {
                    return _algorithm.History(new[] { future.Symbol }, start, end, future.Resolution, contractDepthOffset: contractDepthOffset).ToList();
                };

                var frontMonthHistory = getHistoryForContractDepthOffset(0);
                var backMonthHistory1 = getHistoryForContractDepthOffset(1);
                var backMonthHistory2 = getHistoryForContractDepthOffset(2);

                Func<List<Slice>, HashSet<Symbol>> getHistoryUnderlyings = (history) =>
                {
                    HashSet<Symbol> underlyings = new();
                    foreach (var slice in history)
                    {
                        var underlying = slice.Keys.Single().Underlying;
                        underlyings.Add(underlying);
                    }

                    Assert.GreaterOrEqual(underlyings.Count, 2, "History result did not contain any mappings");

                    return underlyings;
                };

                var frontMonthHistoryUnderlyings = getHistoryUnderlyings(frontMonthHistory);
                var backMonthHistory1Underlyings = getHistoryUnderlyings(backMonthHistory1);
                var backMonthHistory2Underlyings = getHistoryUnderlyings(backMonthHistory2);

                Assert.AreNotEqual(frontMonthHistoryUnderlyings, backMonthHistory2Underlyings);
                Assert.AreNotEqual(frontMonthHistoryUnderlyings, backMonthHistory2Underlyings);
                Assert.AreNotEqual(backMonthHistory1Underlyings, backMonthHistory2Underlyings);

                var historyResults = new List<List<Slice>> { frontMonthHistory, backMonthHistory1, backMonthHistory2 };
                CheckThatHistoryResultsHaveEqualBarCount(historyResults, expectedHistoryCount);
                CheckThatHistoryResultsHaveDifferentPrices(historyResults,
                    "History results prices should have been different for each data mapping mode at each time");
            }
            else
            {
                using (Py.GIL())
                {
                    _algorithm.SetPandasConverter();
                    using var symbols = new PyList(new[] { future.Symbol.ToPython() });

                    Func<int, PyObject> getHistoryForContractDepthOffset = (contractDepthOffset) =>
                    {
                        return _algorithm.History(symbols, start, end, future.Resolution, contractDepthOffset: contractDepthOffset);
                    };

                    var frontMonthHistory = getHistoryForContractDepthOffset(0);
                    var backMonthHistory1 = getHistoryForContractDepthOffset(1);
                    var backMonthHistory2 = getHistoryForContractDepthOffset(2);

                    Assert.Greater(frontMonthHistory.GetAttr("shape")[0].As<int>(), 0);
                    Assert.Greater(backMonthHistory1.GetAttr("shape")[0].As<int>(), 0);
                    Assert.Greater(backMonthHistory2.GetAttr("shape")[0].As<int>(), 0);

                    var historyResults = new List<PyObject> { frontMonthHistory, backMonthHistory1, backMonthHistory2 };
                    CheckThatHistoryResultsHaveEqualBarCount(historyResults, expectedHistoryCount);
                    CheckThatHistoryResultsHaveDifferentPrices(historyResults,
                        "History results prices should have been different for each contract depth offset at each time");
                }
            }
        }

        [TestCase(Language.CSharp)]
        [TestCase(Language.Python)]
        public void GetHistoryWithCustomDataType(Language language)
        {
            var algorithm = GetAlgorithm(new DateTime(2013, 10, 8));
            var start = algorithm.StartDate;
            var end = algorithm.EndDate;
            var span = end - start;
            var periods = (int)span.TotalMinutes;

            if (language == Language.CSharp)
            {
                var symbol = algorithm.AddData<CustomData>("SPY").Symbol;

                var historyResults = new[]
                {
                    algorithm.History<CustomData>(symbol, start, end, Resolution.Minute),
                    algorithm.History<CustomData>(symbol, span, Resolution.Minute),
                    algorithm.History<CustomData>(symbol, periods, Resolution.Minute)
                };

                foreach (var history in historyResults)
                {
                    AssertCustomDataTypeHistory(history.ToList());
                }

                var historyResults2 = new[]
                {
                    algorithm.History<CustomData>(new[] { symbol }, start, end, Resolution.Minute),
                    algorithm.History<CustomData>(new[] { symbol }, span, Resolution.Minute),
                    algorithm.History<CustomData>(new[] { symbol }, periods, Resolution.Minute)
                };

                foreach (var history in historyResults2)
                {
                    AssertCustomDataTypeHistory(history.ToList());
                }
            }
            else
            {
                using (Py.GIL())
                {
                    PythonInitializer.Initialize();

                    var testModule = PyModule.FromString("testModule",
                        @"
from typing import Union
from AlgorithmImports import *
from QuantConnect.Tests import *

class TestCustomMarketData(PythonData):
    def GetSource(self, config, date, isLiveMode):
        fileName = LeanData.GenerateZipFileName(Symbols.SPY, date, config.Resolution, config.TickType)
        source = f'{Globals.DataFolder}equity/usa/minute/spy/{fileName}'
        return SubscriptionDataSource(source, SubscriptionTransportMedium.LocalFile, FileFormat.Csv)

    def Reader(self, config, line, date, isLiveMode):

        data = line.split(',')

        result = TestCustomMarketData()
        result.DataType = MarketDataType.Base
        result.Symbol = config.Symbol
        result.Time = date + timedelta(milliseconds=int(data[0]))
        result.Value = 1

        return result

def getDateRangeHistory(algorithm: QCAlgorithm, symbol: Union[Symbol, List[Symbol]], start: datetime, end: datetime):
    return list(algorithm.History[TestCustomMarketData](symbol, start, end, Resolution.Minute))

def getTimeSpanHistory(algorithm: QCAlgorithm, symbol: Union[Symbol, List[Symbol]], span: Union[timedelta, int]):
    return list(algorithm.History[TestCustomMarketData](symbol, span, Resolution.Minute))
        ");
                    var customDataType = testModule.GetAttr("TestCustomMarketData");
                    var symbol = algorithm.AddData(customDataType, "SPY").Symbol;

                    dynamic getDateRangeHistory = testModule.GetAttr("getDateRangeHistory");
                    dynamic getTimeSpanHistory = testModule.GetAttr("getTimeSpanHistory");

                    var historyResults = new[]
                    {
                        getDateRangeHistory(algorithm, symbol, start, end),
                        getTimeSpanHistory(algorithm, symbol, span),
                        getTimeSpanHistory(algorithm, symbol, periods)
                    };

                    foreach (var history in historyResults)
                    {
                        AssertCustomDataTypeHistory(history.As<List<PythonData>>());
                    }

                    var historyResults2 = new[]
                    {
                        getDateRangeHistory(algorithm, new[] { symbol }, start, end),
                        getTimeSpanHistory(algorithm, new[] { symbol }, span),
                        getTimeSpanHistory(algorithm, new[] { symbol }, periods)
                    };

                    foreach (var history in historyResults2)
                    {
                        AssertCustomDataTypeHistory(history.As<List<DataDictionary<PythonData>>>());
                    }
                }
            }

            Assert.That(_testHistoryProvider.HistryRequests, Has.All.Property("IsCustomData").True);
        }

        [Test]
        public void GetHistoryFromPythonWithCSharpCustomDataType()
        {
            var algorithm = GetAlgorithm(new DateTime(2013, 10, 8));
            var start = algorithm.StartDate;
            var end = algorithm.EndDate;
            var span = end - start;
            var periods = (int)span.TotalMinutes;

            using (Py.GIL())
            {
                PythonInitializer.Initialize();

                var testModule = PyModule.FromString("testModule",
                    @"
from typing import Union
from AlgorithmImports import *
from QuantConnect.Tests import *
from QuantConnect.Tests.Algorithm import AlgorithmHistoryTests

def getDateRangeHistory(algorithm: QCAlgorithm, symbol: Union[Symbol, List[Symbol]], start: datetime, end: datetime):
    return list(algorithm.History[AlgorithmHistoryTests.CustomData](symbol, start, end, Resolution.Minute))

def getTimeSpanHistory(algorithm: QCAlgorithm, symbol: Union[Symbol, List[Symbol]], span: Union[timedelta, int]):
    return list(algorithm.History[AlgorithmHistoryTests.CustomData](symbol, span, Resolution.Minute))
        ");
                var symbol = algorithm.AddData<CustomData>("SPY").Symbol;

                dynamic getDateRangeHistory = testModule.GetAttr("getDateRangeHistory");
                dynamic getTimeSpanHistory = testModule.GetAttr("getTimeSpanHistory");

                var historyResults = new[]
                {
                        getDateRangeHistory(algorithm, symbol, start, end),
                        getTimeSpanHistory(algorithm, symbol, span),
                        getTimeSpanHistory(algorithm, symbol, periods)
                    };

                foreach (var history in historyResults)
                {
                    AssertCustomDataTypeHistory(history.As<List<CustomData>>());
                }

                var historyResults2 = new[]
                {
                        getDateRangeHistory(algorithm, new[] { symbol }, start, end),
                        getTimeSpanHistory(algorithm, new[] { symbol }, span),
                        getTimeSpanHistory(algorithm, new[] { symbol }, periods)
                    };

                foreach (var history in historyResults2)
                {
                    AssertCustomDataTypeHistory(history.As<List<DataDictionary<CustomData>>>());
                }
            }

            Assert.That(_testHistoryProvider.HistryRequests, Has.All.Property("IsCustomData").True);
        }

        [Test]
        public void GetHistoryWithCustomDataAndNormalizationMode()
        {
            var dataNormalizationModes = new DataNormalizationMode[]{
                DataNormalizationMode.Raw,
                DataNormalizationMode.Adjusted,
                DataNormalizationMode.SplitAdjusted
            };
            var start = new DateTime(2014, 6, 5);
            var end = start.AddDays(1);
            var algorithm = GetAlgorithm(end);

            using (Py.GIL())
            {
                var getHistoryForDataNormalizationMode = PyModule.FromString("testModule",
                    @"
from AlgorithmImports import *

def getHistoryForDataNormalizationMode(algorithm, symbol, start, end, resolution, dataNormalizationMode):
    return algorithm.History(TradeBar, symbol, start, end, resolution, dataNormalizationMode=dataNormalizationMode)
        ").GetAttr("getHistoryForDataNormalizationMode");

                algorithm.SetPandasConverter();
                var symbol = algorithm.AddEquity("AAPL", Resolution.Minute).Symbol.ToPython();
                var pyAlgorithm = algorithm.ToPython();
                var pyStart = start.ToPython();
                var pyEnd = end.ToPython();
                var pyResolution = Resolution.Minute.ToPython();
                var historyResults = dataNormalizationModes
                    .Select(dataNormalizationMode =>
                        getHistoryForDataNormalizationMode.Invoke(pyAlgorithm, symbol, pyStart, pyEnd, pyResolution, dataNormalizationMode.ToPython()))
                    .ToList();

                CheckThatHistoryResultsHaveEqualBarCount(historyResults, expectedHistoryCount: 390);
                CheckThatHistoryResultsHaveDifferentPrices(historyResults,
                    "History results prices should have been different for each data normalization mode at each time");
            }
        }

        [Test]
        public void GetHistoryWithCustomDataAndDataMappingMode()
        {
            var dataMappingModes = GetAllDataMappingModes();
            var historyStart = new DateTime(2013, 10, 6);
            var historyEnd = new DateTime(2014, 1, 1);
            var resolution = Resolution.Daily;
            var algorithm = GetAlgorithm(historyEnd);
            var symbol = algorithm.AddFuture(Futures.Indices.SP500EMini, resolution, dataMappingMode: dataMappingModes.First(),
                extendedMarketHours: true).Symbol;

            using (Py.GIL())
            {
                var getHistoryForDataMappingMode = PyModule.FromString("testModule",
                    @"
from AlgorithmImports import *

def getHistoryForDataMappingMode(algorithm, symbol, start, end, resolution, dataMappingMode):
    return algorithm.History(TradeBar, symbol, start, end, resolution, dataMappingMode=dataMappingMode)
        ").GetAttr("getHistoryForDataMappingMode");

                algorithm.SetPandasConverter();
                using var symbols = symbol.ToPython();
                var pyAlgorithm = algorithm.ToPython();
                var pyStart = historyStart.ToPython();
                var pyEnd = historyEnd.ToPython();
                var pyResolution = resolution.ToPython();
                var historyResults = dataMappingModes
                    .Select(dataMappingMode =>
                        getHistoryForDataMappingMode.Invoke(pyAlgorithm, symbols, pyStart, pyEnd, pyResolution, dataMappingMode.ToPython()))
                    .ToList();

                CheckThatHistoryResultsHaveEqualBarCount(historyResults, expectedHistoryCount: 61);
                CheckThatHistoryResultsHaveDifferentPrices(historyResults,
                    "History results prices should have been different for each data mapping mode at each time");
            }
        }

        [Test]
        public void GetHistoryWithCustomDataAndContractDepthOffset()
        {
            var start = new DateTime(2013, 10, 6);
            var end = new DateTime(2014, 1, 1);
            var algorithm = GetAlgorithmWithFuture(end);
            var future = algorithm.SubscriptionManager.Subscriptions.First();

            using (Py.GIL())
            {
                var getHistoryForContractDepthOffset = PyModule.FromString("testModule",
                    @"
from AlgorithmImports import *

def getHistoryForContractDepthOffset(algorithm, symbol, start, end, resolution, contractDepthOffset):
    return algorithm.History(QuoteBar, symbol, start, end, resolution, contractDepthOffset=contractDepthOffset)
        ").GetAttr("getHistoryForContractDepthOffset");

                algorithm.SetPandasConverter();
                using var symbols = new PyList(new[] { future.Symbol.ToPython() });
                var pyAlgorithm = algorithm.ToPython();
                var pyStart = start.ToPython();
                var pyEnd = end.ToPython();
                var pyResolution = future.Resolution.ToPython();

                var frontMonthHistory = getHistoryForContractDepthOffset.Invoke(pyAlgorithm, symbols, pyStart, pyEnd, pyResolution, 0.ToPython());
                var backMonthHistory1 = getHistoryForContractDepthOffset.Invoke(pyAlgorithm, symbols, pyStart, pyEnd, pyResolution, 1.ToPython());
                var backMonthHistory2 = getHistoryForContractDepthOffset.Invoke(pyAlgorithm, symbols, pyStart, pyEnd, pyResolution, 2.ToPython());

                Assert.Greater(frontMonthHistory.GetAttr("shape")[0].As<int>(), 0);
                Assert.Greater(backMonthHistory1.GetAttr("shape")[0].As<int>(), 0);
                Assert.Greater(backMonthHistory2.GetAttr("shape")[0].As<int>(), 0);

                var historyResults = new List<PyObject> { frontMonthHistory, backMonthHistory1, backMonthHistory2 };
                CheckThatHistoryResultsHaveEqualBarCount(historyResults, expectedHistoryCount: 61);
                CheckThatHistoryResultsHaveDifferentPrices(historyResults,
                    "History results prices should have been different for each contract depth offset at each time");
            }
        }

        [TestCase(Language.CSharp)]
        [TestCase(Language.Python)]
        public void GetsHistoryWithGivenBarType(Language language)
        {
            var algorithm = GetAlgorithm(new DateTime(2014, 6, 6));
            var ibmSymbol = Symbol.Create("IBM", SecurityType.Equity, Market.USA);
            var twxSymbol = Symbol.CreateOption("TWX", Market.USA, OptionStyle.American, OptionRight.Call, 45, new DateTime(2015, 1, 17));

            var ibmHistoryStart = new DateTime(2013, 10, 7);
            var ibmHistoryEnd = new DateTime(2013, 10, 8);
            var twxHistoryStart = new DateTime(2014, 6, 5);
            var twxHistoryEnd = new DateTime(2014, 6, 6);

            if (language == Language.CSharp)
            {
                var tradeHistory = algorithm.History<TradeBar>(ibmSymbol, ibmHistoryStart, ibmHistoryEnd);
                Assert.AreEqual(390, tradeHistory.Count());

                var quoteHistory = algorithm.History<QuoteBar>(ibmSymbol, ibmHistoryStart, ibmHistoryEnd);
                Assert.AreEqual(390, quoteHistory.Count());

                var tickHistory = algorithm.History<Tick>(ibmSymbol, ibmHistoryStart, ibmHistoryEnd, Resolution.Tick);
                Assert.AreEqual(132104, tickHistory.Count());

                var openInterestHistory = algorithm.History<OpenInterest>(twxSymbol, twxHistoryStart, twxHistoryEnd);
                Assert.AreEqual(391, openInterestHistory.Count());
            }
            else
            {
                using (Py.GIL())
                {
                    var testModule = PyModule.FromString("testModule",
                        @"
from AlgorithmImports import *

def getTradeBarHistory(algorithm, symbol, start, end):
    return algorithm.History(TradeBar, symbol, start, end)

def getQuoteBarHistory(algorithm, symbol, start, end):
    return algorithm.History(QuoteBar, symbol, start, end)

def getTickHistory(algorithm, symbol, start, end):
    return algorithm.History(Tick, symbol, start, end, Resolution.Tick)

def getOpenInterestHistory(algorithm, symbol, start, end):
    return algorithm.History(OpenInterest, symbol, start, end)
");

                    dynamic getTradeBarHistory = testModule.GetAttr("getTradeBarHistory");
                    dynamic getQuoteBarHistory = testModule.GetAttr("getQuoteBarHistory");
                    dynamic getTickHistory = testModule.GetAttr("getTickHistory");
                    dynamic getOpenInterestHistory = testModule.GetAttr("getOpenInterestHistory");

                    algorithm.SetPandasConverter();

                    dynamic tradeHistory = getTradeBarHistory(algorithm, ibmSymbol, ibmHistoryStart, ibmHistoryEnd);
                    Assert.AreEqual(390, tradeHistory.shape[0].As<int>());

                    dynamic quoteHistory = getQuoteBarHistory(algorithm, ibmSymbol, ibmHistoryStart, ibmHistoryEnd);
                    Assert.AreEqual(390, quoteHistory.shape[0].As<int>());

                    dynamic tickHistory = getTickHistory(algorithm, ibmSymbol, ibmHistoryStart, ibmHistoryEnd);
                    Assert.AreEqual(132104, tickHistory.shape[0].As<int>());

                    dynamic openInterestHistory = getOpenInterestHistory(algorithm, twxSymbol, twxHistoryStart, twxHistoryEnd);
                    Assert.AreEqual(391, openInterestHistory.shape[0].As<int>());
                }
            }
        }

        [Test]
        public void HistoryCallsGetSameTickCount()
        {
            var algorithm = GetAlgorithm(new DateTime(2014, 6, 6));
            var ibmSymbol = Symbol.Create("IBM", SecurityType.Equity, Market.USA);

            var start = new DateTime(2013, 10, 7);
            var end = new DateTime(2013, 10, 8);

            var history = algorithm.History(new[] { ibmSymbol }, start, end, Resolution.Tick);
            var tickCountInSliceHistoryCall = history.Sum(x => x.Ticks[ibmSymbol].Count);
            Assert.AreEqual(132104, tickCountInSliceHistoryCall);

            var tickHistory = algorithm.History<Tick>(ibmSymbol, start, end, Resolution.Tick).ToList();
            var tickCountInTickHistoryCall = tickHistory.Count;
            Assert.AreEqual(tickCountInSliceHistoryCall, tickCountInTickHistoryCall);
        }

        [Test]
        public void PricesAreProperlyAdjustedForScaledRawHistoryRequest()
        {
            var start = new DateTime(2000, 01, 01);
            var end = new DateTime(2016, 01, 01);
            var algorithm = GetAlgorithm(end.AddDays(1));
            var aaplSymbol = Symbol.Create("AAPL", SecurityType.Equity, Market.USA);

            var rawHistory = algorithm.History(new[] { aaplSymbol }, start, end, Resolution.Daily, dataNormalizationMode: DataNormalizationMode.Raw).ToList();
            var scaledRawHistory = algorithm.History(new[] { aaplSymbol }, start, end, Resolution.Daily, dataNormalizationMode: DataNormalizationMode.ScaledRaw).ToList();

            Assert.IsNotEmpty(rawHistory);
            Assert.AreEqual(rawHistory.Count, scaledRawHistory.Count);

            var factorFile = _factorFileProvider.Get(aaplSymbol);
            var factorDates = new List<DateTime>();
            var factors = new List<decimal>();
            var prevFactor = 0m;
            for (var date = start; date <= end; date = date.AddDays(1))
            {
                var factor = factorFile.GetPriceFactor(date, DataNormalizationMode.ScaledRaw);
                if (factor != prevFactor)
                {
                    factorDates.Add(date);
                    factors.Add(factor);
                    prevFactor = factor;
                }
            }
            var lastFactorDate = factorDates[factorDates.Count - 1];
            var lastFactor = factors[factors.Count - 1];
            factorDates.RemoveAt(0);
            var currentFactorIndex = 0;

            for (var i = 0; i < rawHistory.Count; i++)
            {
                var rawBar = rawHistory[i].Bars[aaplSymbol];
                var scaledRawBar = scaledRawHistory[i].Bars[aaplSymbol];

                if (currentFactorIndex < factorDates.Count && rawBar.Time > factorDates[currentFactorIndex])
                {
                    currentFactorIndex++;
                }

                if (rawBar.Time <= lastFactorDate)
                {
                    Assert.AreNotEqual(rawBar.Price, scaledRawBar.Price,
                        $@"Raw price {rawBar.Price} should have been different than scaled raw price {scaledRawBar.Price} at {rawBar.Time} (before and at the last factor date {lastFactorDate})");
                }
                else
                {
                    // after the last split/dividend, the factor is 1 because prices are adjusted to the prices after the last factor
                    Assert.AreEqual(1m, factors[currentFactorIndex] / lastFactor);
                    Assert.AreEqual(rawBar.Price, scaledRawBar.Price,
                        $@"Raw price {rawBar.Price} should have been equal to the scaled raw price {scaledRawBar.Price} at {rawBar.Time} (after the last factor date {lastFactorDate})");
                }

                var expectedScaledRawPrice = rawBar.Price * factors[currentFactorIndex] / lastFactor;
                Assert.Less(Math.Abs(expectedScaledRawPrice - scaledRawBar.Price), 1e-25m, $"Date: {rawBar.Time}");
            }
        }

        // C#
        [TestCase(Language.CSharp, Resolution.Second, true, 46800, 46800, 46800)]
        [TestCase(Language.CSharp, Resolution.Second, false, 46800, 22884, 16093)]
        [TestCase(Language.CSharp, Resolution.Minute, true, 780, 780, 780)]
        [TestCase(Language.CSharp, Resolution.Minute, false, 780, 390, 390)]
        // Python
        [TestCase(Language.Python, Resolution.Second, true, 46800, 46800, 46800)]
        [TestCase(Language.Python, Resolution.Second, false, 46800, 22884, 16093)]
        [TestCase(Language.Python, Resolution.Minute, true, 780, 780, 780)]
        [TestCase(Language.Python, Resolution.Minute, false, 780, 390, 390)]
        public void HistoryRequestWithFillForward(Language language, Resolution resolution, bool fillForward, int periods,
            int expectedHistoryCount, int expectedTradeBarOnlyHistoryCount)
        {
            // Theres data only for 2013-10-07 to 2013-10-11 for SPY. Data should be fill forwarded till the 15th.
            var start = new DateTime(2013, 10, 11);
            var end = new DateTime(2013, 10, 15);
            var timeSpan = end - start;

            var algorithm = GetAlgorithm(end);
            var symbol = algorithm.AddEquity("SPY").Symbol;

            if (language == Language.CSharp)
            {
                // No symbol, time span
                var noSymbolTimeSpanHistory = algorithm.History(timeSpan, resolution, fillForward: fillForward).ToList();
                AssertFillForwardHistoryResults(noSymbolTimeSpanHistory, expectedHistoryCount, resolution, fillForward);

                // No symbol, periods
                var noSymbolPeriodBasedHistory = algorithm.History(periods, resolution, fillForward: fillForward).ToList();
                AssertFillForwardHistoryResults(noSymbolPeriodBasedHistory, expectedHistoryCount, resolution, fillForward);

                // No symbol, date range
                // TODO: to be implemented

                // Single symbol, time span
                var singleSymbolTimeSpanHistory = algorithm.History(symbol, timeSpan, resolution, fillForward: fillForward).ToList();
                AssertFillForwardHistoryResults(singleSymbolTimeSpanHistory, expectedTradeBarOnlyHistoryCount, resolution, fillForward);

                // Single symbol, periods
                var singleSymbolPeriodBasedHistory = algorithm.History(symbol, periods, resolution, fillForward: fillForward).ToList();
                AssertFillForwardHistoryResults(singleSymbolPeriodBasedHistory, expectedTradeBarOnlyHistoryCount, resolution, fillForward);

                // Single symbol, date range
                var singleSymbolDateRangeHistory = algorithm.History(symbol, start, end,
                    resolution, fillForward: fillForward).ToList();
                AssertFillForwardHistoryResults(singleSymbolDateRangeHistory, expectedTradeBarOnlyHistoryCount, resolution, fillForward);

                // Symbol array, time span
                var symbolsTimeSpanHistory = algorithm.History(new[] { symbol }, timeSpan, resolution, fillForward: fillForward).ToList();
                AssertFillForwardHistoryResults(symbolsTimeSpanHistory, expectedHistoryCount, resolution, fillForward);

                // Symbol array, periods
                var symbolsPeriodBasedHistory = algorithm.History(new[] { symbol }, periods, resolution, fillForward: fillForward).ToList();
                AssertFillForwardHistoryResults(symbolsPeriodBasedHistory, expectedHistoryCount, resolution, fillForward);

                // Symbol array, date range
                var symbolsdateRangeHistory = algorithm.History(new[] { symbol }, start, end, resolution, fillForward: fillForward).ToList();
                AssertFillForwardHistoryResults(symbolsdateRangeHistory, expectedHistoryCount, resolution, fillForward);

                // Generic, no symbol, time span
                var typedNoSymbolTimeSpanHistory = algorithm.History<TradeBar>(timeSpan, resolution, fillForward: fillForward).ToList();
                AssertFillForwardHistoryResults(typedNoSymbolTimeSpanHistory, expectedTradeBarOnlyHistoryCount, resolution, fillForward);

                // Generic, no symbol, periods
                // TODO: to be implemented

                // Generic, no symbol, date range
                // TODO: to be implemented

                // Generic, single symbol, time span
                var typedSingleSymbolTimeSpanHistory = algorithm.History<TradeBar>(symbol, timeSpan,
                    resolution, fillForward: fillForward).ToList();
                AssertFillForwardHistoryResults(typedSingleSymbolTimeSpanHistory, expectedTradeBarOnlyHistoryCount, resolution, fillForward);

                // Generic, single symbol, periods
                var typedSingleSymbolPeriodBasedHistory = algorithm.History<TradeBar>(symbol, periods,
                    resolution, fillForward: fillForward).ToList();
                AssertFillForwardHistoryResults(typedSingleSymbolPeriodBasedHistory, expectedTradeBarOnlyHistoryCount, resolution, fillForward);

                // Generic, single symbol, date range
                var typedSingleSymbolDateRangeHistory = algorithm.History<TradeBar>(symbol, start, end,
                    resolution, fillForward: fillForward).ToList();
                AssertFillForwardHistoryResults(typedSingleSymbolDateRangeHistory, expectedTradeBarOnlyHistoryCount, resolution, fillForward);

                // Generic, symbol array, time span
                var typedSymbolsTimeSpanHistory = algorithm.History<TradeBar>(new[] { symbol }, timeSpan,
                    resolution, fillForward: fillForward).ToList();
                AssertFillForwardHistoryResults(typedSymbolsTimeSpanHistory, expectedTradeBarOnlyHistoryCount, resolution, fillForward);

                // Generic, symbol array, periods
                var typedSymbolsPeriodBasedHistory = algorithm.History<TradeBar>(new[] { symbol }, periods,
                    resolution, fillForward: fillForward).ToList();
                AssertFillForwardHistoryResults(typedSymbolsPeriodBasedHistory, expectedTradeBarOnlyHistoryCount, resolution, fillForward);

                // Generic, symbol array, date range
                var typedSymbolsDateRangeHistory = algorithm.History<TradeBar>(new[] { symbol }, start, end,
                    resolution, fillForward: fillForward).ToList();
                AssertFillForwardHistoryResults(typedSymbolsDateRangeHistory, expectedTradeBarOnlyHistoryCount, resolution, fillForward);
            }
            else
            {
                using (Py.GIL())
                {
                    var testModule = PyModule.FromString("testModule", @"
from AlgorithmImports import *

tradeBar = TradeBar
                    ");

                    algorithm.SetPandasConverter();
                    using var pySymbol = symbol.ToPython();
                    using var pySymbols = new PyList(new[] { pySymbol });
                    using var pyTradeBarType = testModule.GetAttr("tradeBar");

                    // Single symbol, time span
                    var singleSymbolTimeSpanHistory = algorithm.History(pySymbol, timeSpan, resolution, fillForward: fillForward);
                    AssertFillForwardHistoryResults(singleSymbolTimeSpanHistory, expectedHistoryCount, resolution, fillForward);

                    // Single symbol, periods
                    var singleSymbolPeriodBasedHistory = algorithm.History(pySymbol, periods, resolution, fillForward: fillForward);
                    AssertFillForwardHistoryResults(singleSymbolPeriodBasedHistory, expectedHistoryCount, resolution, fillForward);

                    // Single symbol, date range
                    var singleSymbolDateRangeHistory = algorithm.History(pySymbol, start, end, resolution, fillForward: fillForward);
                    AssertFillForwardHistoryResults(singleSymbolDateRangeHistory, expectedHistoryCount, resolution, fillForward);

                    // Symbol array, time span
                    var symbolsTimeSpanHistory = algorithm.History(pySymbols, timeSpan, resolution, fillForward: fillForward);
                    AssertFillForwardHistoryResults(symbolsTimeSpanHistory, expectedHistoryCount, resolution, fillForward);

                    // Symbol array, periods
                    var symbolsPeriodBasedHistory = algorithm.History(pySymbols, periods, resolution, fillForward: fillForward);
                    AssertFillForwardHistoryResults(symbolsPeriodBasedHistory, expectedHistoryCount, resolution, fillForward);

                    // Symbol array, date range
                    var symbolsDateRangeHistory = algorithm.History(pySymbols, start, end, resolution, fillForward: fillForward);
                    AssertFillForwardHistoryResults(symbolsDateRangeHistory, expectedHistoryCount, resolution, fillForward);

                    // Generic, single symbol, time span
                    var typedSingleSymbolTimeSpanHistory = algorithm.History(pyTradeBarType, pySymbol, timeSpan,
                        resolution, fillForward: fillForward);
                    AssertFillForwardHistoryResults(typedSingleSymbolTimeSpanHistory, expectedTradeBarOnlyHistoryCount, resolution, fillForward);

                    // Same as previous but using a Symbol instead of pySymbol
                    typedSingleSymbolTimeSpanHistory = algorithm.History(pyTradeBarType, symbol, timeSpan, resolution, fillForward: fillForward);
                    AssertFillForwardHistoryResults(typedSingleSymbolTimeSpanHistory, expectedTradeBarOnlyHistoryCount, resolution, fillForward);

                    // Generic, single symbol, periods
                    var typedSingleSymbolPeriodBasedHistory = algorithm.History(pyTradeBarType, pySymbol, periods,
                        resolution, fillForward: fillForward);
                    AssertFillForwardHistoryResults(typedSingleSymbolPeriodBasedHistory, expectedTradeBarOnlyHistoryCount, resolution, fillForward);

                    // Same as previous but using a Symbol instead of pySymbol
                    typedSingleSymbolPeriodBasedHistory = algorithm.History(pyTradeBarType, symbol, periods,
                        resolution, fillForward: fillForward);
                    AssertFillForwardHistoryResults(typedSingleSymbolPeriodBasedHistory, expectedTradeBarOnlyHistoryCount, resolution, fillForward);

                    // Generic, single symbol, date range
                    var typedSingleSymbolDateRangeHistory = algorithm.History(pyTradeBarType, pySymbol, start, end,
                        resolution, fillForward: fillForward);
                    AssertFillForwardHistoryResults(typedSingleSymbolDateRangeHistory, expectedTradeBarOnlyHistoryCount, resolution, fillForward);

                    // Same as previous but using a Symbol instead of pySymbol
                    typedSingleSymbolDateRangeHistory = algorithm.History(pyTradeBarType, symbol, start, end,
                        resolution, fillForward: fillForward);
                    AssertFillForwardHistoryResults(typedSingleSymbolDateRangeHistory, expectedTradeBarOnlyHistoryCount, resolution, fillForward);

                    // Generic, symbol array, time span
                    var typedSymbolsTimeSpanHistory = algorithm.History(pyTradeBarType, pySymbols, timeSpan,
                        resolution, fillForward: fillForward);
                    AssertFillForwardHistoryResults(typedSymbolsTimeSpanHistory, expectedTradeBarOnlyHistoryCount, resolution, fillForward);

                    // Generic, symbol array, periods
                    var typedSymbolsPeriodBasedHistory = algorithm.History(pyTradeBarType, pySymbols, periods,
                        resolution, fillForward: fillForward);
                    AssertFillForwardHistoryResults(typedSymbolsPeriodBasedHistory, expectedTradeBarOnlyHistoryCount, resolution, fillForward);

                    // Generic, symbol array, date range
                    var typedSymbolsDateRangeHistory = algorithm.History(pyTradeBarType, pySymbols, start, end,
                        resolution, fillForward: fillForward);
                    AssertFillForwardHistoryResults(typedSymbolsDateRangeHistory, expectedTradeBarOnlyHistoryCount, resolution, fillForward);
                }
            }
        }

        // C#
        [TestCase(Language.CSharp, Resolution.Minute, true, 960)]
        [TestCase(Language.CSharp, Resolution.Minute, false, 390)]
        [TestCase(Language.CSharp, Resolution.Second, true, 57600)]
        [TestCase(Language.CSharp, Resolution.Second, false, 23400)]
        // Python
        [TestCase(Language.Python, Resolution.Minute, true, 960)]
        [TestCase(Language.Python, Resolution.Minute, false, 390)]
        [TestCase(Language.Python, Resolution.Second, true, 57600)]
        [TestCase(Language.Python, Resolution.Second, false, 23400)]
        public void HistoryRequestWithExtendedMarketHours(Language language, Resolution resolution, bool extendedMarket, int expectedHistoryCount)
        {
            var start = new DateTime(2013, 10, 07);
            var end = new DateTime(2013, 10, 08);
            var algorithm = GetAlgorithm(end);
            var symbol = algorithm.AddEquity("SPY").Symbol;

            var extendedMarketPeriods = expectedHistoryCount;
            var timeSpan = end - start;

            if (language == Language.CSharp)
            {
                // No symbol, time span
                var noSymbolTimeSpanHistory = algorithm.History(timeSpan, resolution, extendedMarketHours: extendedMarket).ToList();
                AssertExtendedMarketHistoryResults(noSymbolTimeSpanHistory, expectedHistoryCount, extendedMarket, resolution, symbol);

                // No symbol, periods
                var noSymbolPeriodBasedHistory = algorithm.History(extendedMarketPeriods,
                    resolution, extendedMarketHours: extendedMarket).ToList();
                AssertExtendedMarketHistoryResults(noSymbolPeriodBasedHistory, expectedHistoryCount, extendedMarket, resolution, symbol);

                //// No symbol, date range
                //// TODO: to be implemented

                // Single symbol, time span
                var singleSymbolTimeSpanHistory = algorithm.History(symbol, timeSpan,
                    resolution, extendedMarketHours: extendedMarket).ToList();
                AssertExtendedMarketHistoryResults(singleSymbolTimeSpanHistory, expectedHistoryCount, extendedMarket, resolution, symbol);

                // Single symbol, periods
                var singleSymbolPeriodBasedHistory = algorithm.History(symbol, extendedMarketPeriods,
                    resolution, extendedMarketHours: extendedMarket).ToList();
                AssertExtendedMarketHistoryResults(singleSymbolPeriodBasedHistory, expectedHistoryCount, extendedMarket, resolution, symbol);

                // Single symbol, date range
                var singleSymbolDateRangeHistory = algorithm.History(symbol, start, end,
                    resolution, extendedMarketHours: extendedMarket).ToList();
                AssertExtendedMarketHistoryResults(singleSymbolDateRangeHistory, expectedHistoryCount, extendedMarket, resolution, symbol);

                // Symbol array, time span
                var symbolsTimeSpanHistory = algorithm.History(new[] { symbol }, timeSpan,
                    resolution, extendedMarketHours: extendedMarket).ToList();
                AssertExtendedMarketHistoryResults(symbolsTimeSpanHistory, expectedHistoryCount, extendedMarket, resolution, symbol);

                // Symbol array, periods
                var symbolsPeriodBasedHistory = algorithm.History(new[] { symbol }, extendedMarketPeriods,
                    resolution, extendedMarketHours: extendedMarket).ToList();
                AssertExtendedMarketHistoryResults(symbolsPeriodBasedHistory, expectedHistoryCount, extendedMarket, resolution, symbol);

                // Symbol array, date range
                var symbolsdateRangeHistory = algorithm.History(new[] { symbol }, start, end,
                    resolution, extendedMarketHours: extendedMarket).ToList();
                AssertExtendedMarketHistoryResults(symbolsdateRangeHistory, expectedHistoryCount, extendedMarket, resolution, symbol);

                // Generic, no symbol, time span
                var typedNoSymbolTimeSpanHistory = algorithm.History<TradeBar>(timeSpan,
                    resolution, extendedMarketHours: extendedMarket).ToList();
                AssertExtendedMarketHistoryResults(typedNoSymbolTimeSpanHistory, expectedHistoryCount, extendedMarket, resolution, symbol);

                //// Generic, no symbol, periods
                //// TODO: to be implemented

                //// Generic, no symbol, date range
                //// TODO: to be implemented

                // Generic, single symbol, time span
                var typedSingleSymbolTimeSpanHistory = algorithm.History<TradeBar>(symbol, timeSpan,
                    resolution, extendedMarketHours: extendedMarket).ToList();
                AssertExtendedMarketHistoryResults(typedSingleSymbolTimeSpanHistory, expectedHistoryCount, extendedMarket, resolution, symbol);

                // Generic, single symbol, periods
                var typedSingleSymbolPeriodBasedHistory = algorithm.History<TradeBar>(symbol, extendedMarketPeriods,
                    resolution, extendedMarketHours: extendedMarket).ToList();
                AssertExtendedMarketHistoryResults(typedSingleSymbolPeriodBasedHistory, expectedHistoryCount, extendedMarket, resolution, symbol);

                // Generic, single symbol, date range
                var typedSingleSymbolDateRangeHistory = algorithm.History<TradeBar>(symbol, start, end,
                    resolution, extendedMarketHours: extendedMarket).ToList();
                AssertExtendedMarketHistoryResults(typedSingleSymbolDateRangeHistory, expectedHistoryCount, extendedMarket, resolution, symbol);

                // Generic, symbol array, time span
                var typedSymbolsTimeSpanHistory = algorithm.History<TradeBar>(new[] { symbol }, timeSpan,
                    resolution, extendedMarketHours: extendedMarket).ToList();
                AssertExtendedMarketHistoryResults(typedSymbolsTimeSpanHistory, expectedHistoryCount, extendedMarket, resolution, symbol);

                // Generic, symbol array, periods
                var typedSymbolsPeriodBasedHistory = algorithm.History<TradeBar>(new[] { symbol }, extendedMarketPeriods,
                    resolution, extendedMarketHours: extendedMarket).ToList();
                AssertExtendedMarketHistoryResults(typedSymbolsPeriodBasedHistory, expectedHistoryCount, extendedMarket, resolution, symbol);

                // Generic, symbol array, date range
                var typedSymbolsDateRangeHistory = algorithm.History<TradeBar>(new[] { symbol }, start, end,
                    resolution, extendedMarketHours: extendedMarket).ToList();
                AssertExtendedMarketHistoryResults(typedSymbolsDateRangeHistory, expectedHistoryCount, extendedMarket, resolution, symbol);
            }
            else
            {
                using (Py.GIL())
                {
                    var testModule = PyModule.FromString("testModule", @"
from AlgorithmImports import *
tradeBar = TradeBar
                    ");

                    algorithm.SetPandasConverter();
                    using var pySymbol = symbol.ToPython();
                    using var pySymbols = new PyList(new[] { pySymbol });
                    using var pyTradeBarType = testModule.GetAttr("tradeBar");

                    // Single symbol, time span
                    var singleSymbolTimeSpanHistory = algorithm.History(pySymbol, timeSpan,
                        resolution, extendedMarketHours: extendedMarket);
                    AssertExtendedMarketHistoryResults(singleSymbolTimeSpanHistory, expectedHistoryCount, extendedMarket, resolution, symbol);

                    // Single symbol, periods
                    var singleSymbolPeriodBasedHistory = algorithm.History(pySymbol, extendedMarketPeriods,
                        resolution, extendedMarketHours: extendedMarket);
                    AssertExtendedMarketHistoryResults(singleSymbolPeriodBasedHistory, expectedHistoryCount, extendedMarket, resolution, symbol);

                    // Single symbol, date range
                    var singleSymbolDateRangeHistory = algorithm.History(pySymbol, start, end,
                        resolution, extendedMarketHours: extendedMarket);
                    AssertExtendedMarketHistoryResults(singleSymbolDateRangeHistory, expectedHistoryCount, extendedMarket, resolution, symbol);

                    // Symbol array, time span
                    var symbolsTimeSpanHistory = algorithm.History(pySymbols, timeSpan,
                        resolution, extendedMarketHours: extendedMarket);
                    AssertExtendedMarketHistoryResults(symbolsTimeSpanHistory, expectedHistoryCount, extendedMarket, resolution, symbol);

                    // Symbol array, periods
                    var symbolsPeriodBasedHistory = algorithm.History(pySymbols, extendedMarketPeriods,
                        resolution, extendedMarketHours: extendedMarket);
                    AssertExtendedMarketHistoryResults(symbolsPeriodBasedHistory, expectedHistoryCount, extendedMarket, resolution, symbol);

                    // Symbol array, date range
                    var symbolsDateRangeHistory = algorithm.History(pySymbols, start, end,
                        resolution, extendedMarketHours: extendedMarket);
                    AssertExtendedMarketHistoryResults(symbolsDateRangeHistory, expectedHistoryCount, extendedMarket, resolution, symbol);

                    // Generic, single symbol, time span
                    var typedSingleSymbolTimeSpanHistory = algorithm.History(pyTradeBarType, pySymbol, timeSpan,
                        resolution, extendedMarketHours: extendedMarket);
                    AssertExtendedMarketHistoryResults(typedSingleSymbolTimeSpanHistory, expectedHistoryCount, extendedMarket, resolution, symbol);

                    // Same as previous but using a Symbol instead of pySymbol
                    typedSingleSymbolTimeSpanHistory = algorithm.History(pyTradeBarType, symbol, timeSpan,
                        resolution, extendedMarketHours: extendedMarket);
                    AssertExtendedMarketHistoryResults(typedSingleSymbolTimeSpanHistory, expectedHistoryCount, extendedMarket, resolution, symbol);

                    // Generic, single symbol, periods
                    var typedSingleSymbolPeriodBasedHistory = algorithm.History(pyTradeBarType, pySymbol,
                        extendedMarketPeriods, resolution, extendedMarketHours: extendedMarket);
                    AssertExtendedMarketHistoryResults(typedSingleSymbolPeriodBasedHistory, expectedHistoryCount, extendedMarket, resolution, symbol);

                    // Same as previous but using a Symbol instead of pySymbol
                    typedSingleSymbolPeriodBasedHistory = algorithm.History(pyTradeBarType, symbol,
                        extendedMarketPeriods, resolution, extendedMarketHours: extendedMarket);
                    AssertExtendedMarketHistoryResults(typedSingleSymbolPeriodBasedHistory, expectedHistoryCount, extendedMarket, resolution, symbol);

                    // Generic, single symbol, date range
                    var typedSingleSymbolDateRangeHistory = algorithm.History(pyTradeBarType, pySymbol, start, end,
                        resolution, extendedMarketHours: extendedMarket);
                    AssertExtendedMarketHistoryResults(typedSingleSymbolDateRangeHistory, expectedHistoryCount, extendedMarket, resolution, symbol);

                    // Same as previous but using a Symbol instead of pySymbol
                    typedSingleSymbolDateRangeHistory = algorithm.History(pyTradeBarType, symbol, start, end,
                        resolution, extendedMarketHours: extendedMarket);
                    AssertExtendedMarketHistoryResults(typedSingleSymbolDateRangeHistory, expectedHistoryCount, extendedMarket, resolution, symbol);

                    // Generic, symbol array, time span
                    var typedSymbolsTimeSpanHistory = algorithm.History(pyTradeBarType, pySymbols, timeSpan,
                        resolution, extendedMarketHours: extendedMarket);
                    AssertExtendedMarketHistoryResults(typedSymbolsTimeSpanHistory, expectedHistoryCount, extendedMarket, resolution, symbol);

                    // Generic, symbol array, periods
                    var typedSymbolsPeriodBasedHistory = algorithm.History(pyTradeBarType, pySymbols, extendedMarketPeriods,
                        resolution, extendedMarketHours: extendedMarket);
                    AssertExtendedMarketHistoryResults(typedSymbolsPeriodBasedHistory, expectedHistoryCount, extendedMarket, resolution, symbol);

                    // Generic, symbol array, date range
                    var typedSymbolsDateRangeHistory = algorithm.History(pyTradeBarType, pySymbols, start, end,
                        resolution, extendedMarketHours: extendedMarket);
                    AssertExtendedMarketHistoryResults(typedSymbolsDateRangeHistory, expectedHistoryCount, extendedMarket, resolution, symbol);
                }
            }
        }

        // C#
        [TestCase(Language.CSharp, true, 326645, 69480)] // 326645, 69480
        [TestCase(Language.CSharp, false, 288008, 60486)] // 288008, 60486
        // Python
        [TestCase(Language.Python, true, 326645)]    //, 420604
        [TestCase(Language.Python, false, 288008)] //, 288008
        public void HistoryRequestWithExtendedMarketHoursTickResolution(
            Language language,
            bool extendedMarket,
            int historyExpectedCount,
            // History<T> methods that take multiple symbols still have a bug for Tick type,
            // where slice.Get() returns only the last tick for each symbol, so the expected count is different
            int cSharpTypedMultiSymbolHistoryExpectedCount = 0)
        {
            var start = new DateTime(2013, 10, 07, 15, 30, 0);
            var end = start.AddHours(1);
            var algorithm = GetAlgorithm(end);
            var symbol = algorithm.AddEquity("SPY").Symbol;

            var timeSpan = end - start;

            if (language == Language.CSharp)
            {
                // No symbol, time span
                var noSymbolTimeSpanHistory = algorithm.History(timeSpan, Resolution.Tick, extendedMarketHours: extendedMarket)
                    .SelectMany(x => x.Ticks[symbol]).ToList();
                AssertExtendedMarketHistoryResults(noSymbolTimeSpanHistory, historyExpectedCount, extendedMarket, Resolution.Tick, symbol);

                // No symbol, periods
                // Not available for tick resolution

                //// No symbol, date range
                //// TODO: to be implemented

                // Single symbol, time span
                // Not available for tick resolution (TradeBar API)

                // Single symbol, periods
                // Not available for tick resolution

                // Single symbol, date range
                // Not available for tick resolution (TradeBar API)

                // Symbol array, time span
                var symbolsTimeSpanHistory = algorithm.History(new[] { symbol }, timeSpan,
                    Resolution.Tick, extendedMarketHours: extendedMarket).SelectMany(x => x.Ticks[symbol]).ToList();
                AssertExtendedMarketHistoryResults(symbolsTimeSpanHistory, historyExpectedCount, extendedMarket, Resolution.Tick, symbol);

                // Symbol array, periods
                // Not available for tick resolution

                //// Symbol array, date range
                var symbolsdateRangeHistory = algorithm.History(new[] { symbol }, start, end,
                    Resolution.Tick, extendedMarketHours: extendedMarket).SelectMany(x => x.Ticks[symbol]).ToList();
                AssertExtendedMarketHistoryResults(symbolsdateRangeHistory, historyExpectedCount, extendedMarket, Resolution.Tick, symbol);

                // Generic, no symbol, time span
                var typedNoSymbolTimeSpanHistory = algorithm.History<Tick>(timeSpan,
                    Resolution.Tick, extendedMarketHours: extendedMarket).ToList();
                AssertExtendedMarketHistoryResults(typedNoSymbolTimeSpanHistory, cSharpTypedMultiSymbolHistoryExpectedCount, extendedMarket,
                    Resolution.Tick, symbol);

                //// Generic, no symbol, periods
                //// TODO: to be implemented

                //// Generic, no symbol, date range
                //// TODO: to be implemented

                //// Generic, single symbol, time span
                var typedSingleSymbolTimeSpanHistory = algorithm.History<Tick>(symbol, timeSpan,
                    Resolution.Tick, extendedMarketHours: extendedMarket).ToList();
                AssertExtendedMarketHistoryResults(typedSingleSymbolTimeSpanHistory, historyExpectedCount, extendedMarket, Resolution.Tick, symbol);

                // Generic, single symbol, periods
                // Not available for tick resolution (TradeBar API)

                //// Generic, single symbol, date range
                var typedSingleSymbolDateRangeHistory = algorithm.History<Tick>(symbol, start, end,
                    Resolution.Tick, extendedMarketHours: extendedMarket).ToList();
                AssertExtendedMarketHistoryResults(typedSingleSymbolDateRangeHistory, historyExpectedCount, extendedMarket, Resolution.Tick, symbol);

                // Generic, symbol array, time span
                var typedSymbolsTimeSpanHistory = algorithm.History<Tick>(new[] { symbol }, timeSpan,
                    Resolution.Tick, extendedMarketHours: extendedMarket).ToList();
                AssertExtendedMarketHistoryResults(typedSymbolsTimeSpanHistory, cSharpTypedMultiSymbolHistoryExpectedCount, extendedMarket,
                    Resolution.Tick, symbol);

                // Generic, symbol array, periods
                // Not available for tick resolution (TradeBar API)

                // Generic, symbol array, date range
                var typedSymbolsDateRangeHistory = algorithm.History<Tick>(new[] { symbol }, start, end,
                    Resolution.Tick, extendedMarketHours: extendedMarket).ToList();
                AssertExtendedMarketHistoryResults(typedSymbolsDateRangeHistory, cSharpTypedMultiSymbolHistoryExpectedCount, extendedMarket,
                    Resolution.Tick, symbol);
            }
            else
            {
                using (Py.GIL())
                {
                    var testModule = PyModule.FromString("testModule", @"
from AlgorithmImports import *
tick = Tick
                    ");

                    algorithm.SetPandasConverter();
                    using var pySymbol = symbol.ToPython();
                    using var pySymbols = new PyList(new[] { pySymbol });
                    using var pyTickType = testModule.GetAttr("tick");

                    // Single symbol, time span
                    var singleSymbolTimeSpanHistory = algorithm.History(pySymbol, timeSpan,
                        Resolution.Tick, extendedMarketHours: extendedMarket);
                    AssertExtendedMarketHistoryResults(singleSymbolTimeSpanHistory, historyExpectedCount, extendedMarket, Resolution.Tick, symbol);

                    // Single symbol, periods
                    // Not available for tick resolution (TradeBar API)

                    // Single symbol, date range
                    var singleSymbolDateRangeHistory = algorithm.History(pySymbol, start, end,
                        Resolution.Tick, extendedMarketHours: extendedMarket);
                    AssertExtendedMarketHistoryResults(singleSymbolDateRangeHistory, historyExpectedCount, extendedMarket, Resolution.Tick, symbol);

                    // Symbol array, time span
                    var symbolsTimeSpanHistory = algorithm.History(pySymbols, timeSpan,
                        Resolution.Tick, extendedMarketHours: extendedMarket);
                    AssertExtendedMarketHistoryResults(symbolsTimeSpanHistory, historyExpectedCount, extendedMarket, Resolution.Tick, symbol);

                    // Symbol array, periods
                    // Not available for tick resolution (TradeBar API)

                    // Symbol array, date range
                    var symbolsDateRangeHistory = algorithm.History(pySymbols, start, end,
                        Resolution.Tick, extendedMarketHours: extendedMarket);
                    AssertExtendedMarketHistoryResults(symbolsDateRangeHistory, historyExpectedCount, extendedMarket, Resolution.Tick, symbol);

                    // Generic, single symbol, time span
                    var typedSingleSymbolTimeSpanHistory = algorithm.History(pyTickType, pySymbol, timeSpan,
                        Resolution.Tick, extendedMarketHours: extendedMarket);
                    AssertExtendedMarketHistoryResults(typedSingleSymbolTimeSpanHistory, historyExpectedCount, extendedMarket,
                        Resolution.Tick, symbol);

                    // Same as previous but using a Symbol instead of pySymbol
                    typedSingleSymbolTimeSpanHistory = algorithm.History(pyTickType, symbol, timeSpan,
                        Resolution.Tick, extendedMarketHours: extendedMarket);
                    AssertExtendedMarketHistoryResults(typedSingleSymbolTimeSpanHistory, historyExpectedCount, extendedMarket,
                        Resolution.Tick, symbol);

                    // Generic, single symbol, periods
                    // Not available for tick resolution (TradeBar API)

                    // Generic, single symbol, date range
                    var typedSingleSymbolDateRangeHistory = algorithm.History(pyTickType, pySymbol, start, end,
                        Resolution.Tick, extendedMarketHours: extendedMarket);
                    AssertExtendedMarketHistoryResults(typedSingleSymbolDateRangeHistory, historyExpectedCount, extendedMarket,
                        Resolution.Tick, symbol);

                    // Same as previous but using a Symbol instead of pySymbol
                    typedSingleSymbolDateRangeHistory = algorithm.History(pyTickType, symbol, start, end,
                        Resolution.Tick, extendedMarketHours: extendedMarket);
                    AssertExtendedMarketHistoryResults(typedSingleSymbolDateRangeHistory, historyExpectedCount, extendedMarket,
                        Resolution.Tick, symbol);

                    // Generic, symbol array, time span
                    var typedSymbolsTimeSpanHistory = algorithm.History(pyTickType, pySymbols, timeSpan,
                        Resolution.Tick, extendedMarketHours: extendedMarket);
                    AssertExtendedMarketHistoryResults(typedSymbolsTimeSpanHistory, historyExpectedCount, extendedMarket,
                        Resolution.Tick, symbol);

                    // Generic, symbol array, periods
                    // Not available for tick resolution (TradeBar API)

                    // Generic, symbol array, date range
                    var typedSymbolsDateRangeHistory = algorithm.History(pyTickType, pySymbols, start, end,
                        Resolution.Tick, extendedMarketHours: extendedMarket);
                    AssertExtendedMarketHistoryResults(typedSymbolsDateRangeHistory, historyExpectedCount, extendedMarket,
                        Resolution.Tick, symbol);
                }
            }
        }

        // C#
        [TestCase(Language.CSharp, Resolution.Daily, 61)]
        [TestCase(Language.CSharp, Resolution.Hour, 477)]
        [TestCase(Language.CSharp, Resolution.Minute, 26670)]
        // Python
        [TestCase(Language.Python, Resolution.Daily, 61)]
        [TestCase(Language.Python, Resolution.Hour, 477)]
        [TestCase(Language.Python, Resolution.Minute, 26670)]
        public void HistoryRequestWithDataMappingMode(Language language, Resolution resolution, int expectedHistoryCount)
        {
            var start = new DateTime(2013, 10, 6);
            var end = new DateTime(2014, 1, 1);
            var algorithm = GetAlgorithm(end);
            var symbol = algorithm.AddFuture(Futures.Indices.SP500EMini, resolution, fillForward: true).Symbol;

            var dataMappingModes = GetAllDataMappingModes();
            var timeSpan = end - start;
            var periods = expectedHistoryCount;

            var expectedMappingDates = new List<DateTime>
            {
                // DataMappingMode.LastTradingDay (0)
                new DateTime(2013, 12, 20),
                // DataMappingMode.FirstDayMonth (1)
                new DateTime(2013, 12, 02),
                // DataMappingMode.OpenInterest (2)
                new DateTime(2013, 12, 18),
                // DataMappingMode.OpenInterestAnnual (3)
                new DateTime(2013, 11, 18),
            };

            if (language == Language.CSharp)
            {
                // No symbol, time span
                var historyResults = dataMappingModes
                     .Select(mappingMode => algorithm.History(timeSpan, resolution, dataMappingMode: mappingMode).ToList())
                     .ToList();
                AssertFuturesHistoryWithDifferentMappingModesResults(historyResults, symbol, expectedHistoryCount, resolution,
                    expectedMappingDates);

                // No symbol, periods
                historyResults = dataMappingModes
                     .Select(mappingMode => algorithm.History(periods, resolution, dataMappingMode: mappingMode).ToList())
                     .ToList();
                AssertFuturesHistoryWithDifferentMappingModesResults(historyResults, symbol, expectedHistoryCount, resolution,
                    expectedMappingDates);

                // No symbol, date range
                // TODO: to be implemented

                // Single symbols, time span
                var tradeBarHistoryResults = dataMappingModes
                     .Select(mappingMode => algorithm.History(symbol, timeSpan, resolution, dataMappingMode: mappingMode).ToList())
                     .ToList();
                AssertFuturesHistoryWithDifferentMappingModesResults(tradeBarHistoryResults, symbol, expectedHistoryCount, resolution,
                    expectedMappingDates);

                // Single symbols, periods
                tradeBarHistoryResults = dataMappingModes
                     .Select(mappingMode => algorithm.History(symbol, periods, resolution, dataMappingMode: mappingMode).ToList())
                     .ToList();
                AssertFuturesHistoryWithDifferentMappingModesResults(tradeBarHistoryResults, symbol, expectedHistoryCount, resolution,
                    expectedMappingDates);

                // Single symbols, date range
                tradeBarHistoryResults = dataMappingModes
                     .Select(mappingMode => algorithm.History(symbol, start, end, resolution, dataMappingMode: mappingMode).ToList())
                     .ToList();
                AssertFuturesHistoryWithDifferentMappingModesResults(tradeBarHistoryResults, symbol, expectedHistoryCount, resolution,
                    expectedMappingDates);

                // Symbol array, time span
                historyResults = dataMappingModes
                     .Select(mappingMode => algorithm.History(new[] { symbol }, timeSpan, resolution, dataMappingMode: mappingMode).ToList())
                     .ToList();
                AssertFuturesHistoryWithDifferentMappingModesResults(historyResults, symbol, expectedHistoryCount, resolution, expectedMappingDates);

                // Symbol array, periods
                historyResults = dataMappingModes
                     .Select(mappingMode => algorithm.History(new[] { symbol }, periods, resolution, dataMappingMode: mappingMode).ToList())
                     .ToList();
                AssertFuturesHistoryWithDifferentMappingModesResults(historyResults, symbol, expectedHistoryCount, resolution, expectedMappingDates);

                // Symbol array, date range
                historyResults = dataMappingModes
                     .Select(mappingMode => algorithm.History(new[] { symbol }, start, end, resolution, dataMappingMode: mappingMode).ToList())
                     .ToList();
                AssertFuturesHistoryWithDifferentMappingModesResults(historyResults, symbol, expectedHistoryCount, resolution, expectedMappingDates);

                // Generic, no symbol, time span
                var typedHistoryResults = dataMappingModes
                     .Select(mappingMode => algorithm.History<TradeBar>(timeSpan, resolution, dataMappingMode: mappingMode).ToList())
                     .ToList();
                AssertFuturesHistoryWithDifferentMappingModesResults(tradeBarHistoryResults, symbol, expectedHistoryCount, resolution,
                    expectedMappingDates);

                // Generic, no symbol, periods
                // TODO: to be implemented

                // Generic, no symbol, date range
                // TODO: to be implemented

                // Generic, single symbol, time span
                tradeBarHistoryResults = dataMappingModes
                     .Select(mappingMode => algorithm.History<TradeBar>(symbol, timeSpan, resolution, dataMappingMode: mappingMode).ToList())
                     .ToList();
                AssertFuturesHistoryWithDifferentMappingModesResults(tradeBarHistoryResults, symbol, expectedHistoryCount, resolution,
                    expectedMappingDates);

                // Generic, single symbol, periods
                tradeBarHistoryResults = dataMappingModes
                     .Select(mappingMode => algorithm.History<TradeBar>(symbol, periods, resolution, dataMappingMode: mappingMode).ToList())
                     .ToList();
                AssertFuturesHistoryWithDifferentMappingModesResults(tradeBarHistoryResults, symbol, expectedHistoryCount, resolution,
                    expectedMappingDates);

                // Generic, single symbol, date range
                tradeBarHistoryResults = dataMappingModes
                     .Select(mappingMode => algorithm.History<TradeBar>(symbol, start, end, resolution, dataMappingMode: mappingMode).ToList())
                     .ToList();
                AssertFuturesHistoryWithDifferentMappingModesResults(tradeBarHistoryResults, symbol, expectedHistoryCount, resolution,
                    expectedMappingDates);

                // Generic, symbol array, time span
                typedHistoryResults = dataMappingModes
                     .Select(mappingMode =>
                        algorithm.History<TradeBar>(new[] { symbol }, timeSpan, resolution, dataMappingMode: mappingMode).ToList())
                     .ToList();
                AssertFuturesHistoryWithDifferentMappingModesResults(typedHistoryResults, symbol, expectedHistoryCount, resolution,
                    expectedMappingDates);

                // Generic, symbol array, periods
                typedHistoryResults = dataMappingModes
                     .Select(mappingMode =>
                        algorithm.History<TradeBar>(new[] { symbol }, periods, resolution, dataMappingMode: mappingMode).ToList())
                     .ToList();
                AssertFuturesHistoryWithDifferentMappingModesResults(typedHistoryResults, symbol, expectedHistoryCount, resolution,
                    expectedMappingDates);

                // Generic, symbol array, date range
                typedHistoryResults = dataMappingModes
                     .Select(mappingMode =>
                        algorithm.History<TradeBar>(new[] { symbol }, start, end, resolution, dataMappingMode: mappingMode).ToList())
                     .ToList();
                AssertFuturesHistoryWithDifferentMappingModesResults(typedHistoryResults, symbol, expectedHistoryCount, resolution,
                    expectedMappingDates);
            }
            else
            {
                using (Py.GIL())
                {
                    var testModule = PyModule.FromString("testModule", @"
from AlgorithmImports import *
tradeBar = TradeBar
                    ");

                    algorithm.SetPandasConverter();
                    using var pySymbol = symbol.ToPython();
                    using var pySymbols = new PyList(new[] { pySymbol });
                    using var pyTradeBar = testModule.GetAttr("tradeBar");

                    // Single symbols, time span
                    var historyResults = dataMappingModes
                        .Select(mappingMode => algorithm.History(pySymbol, timeSpan, resolution, dataMappingMode: mappingMode))
                        .ToList();
                    AssertFuturesHistoryWithDifferentMappingModesResults(historyResults, symbol, expectedHistoryCount);

                    // Single symbols, periods
                    historyResults = dataMappingModes
                        .Select(mappingMode => algorithm.History(pySymbol, periods, resolution, dataMappingMode: mappingMode))
                        .ToList();
                    AssertFuturesHistoryWithDifferentMappingModesResults(historyResults, symbol, expectedHistoryCount);

                    // Single symbols, date range
                    historyResults = dataMappingModes
                        .Select(mappingMode => algorithm.History(pySymbol, start, end, resolution, dataMappingMode: mappingMode))
                        .ToList();
                    AssertFuturesHistoryWithDifferentMappingModesResults(historyResults, symbol, expectedHistoryCount);

                    // Symbol array, time span
                    historyResults = dataMappingModes
                        .Select(mappingMode => algorithm.History(pySymbols, timeSpan, resolution, dataMappingMode: mappingMode))
                        .ToList();
                    AssertFuturesHistoryWithDifferentMappingModesResults(historyResults, symbol, expectedHistoryCount);

                    // Symbol array, periods
                    historyResults = dataMappingModes
                        .Select(mappingMode => algorithm.History(pySymbols, periods, resolution, dataMappingMode: mappingMode))
                        .ToList();
                    AssertFuturesHistoryWithDifferentMappingModesResults(historyResults, symbol, expectedHistoryCount);

                    // Symbol array, date range
                    historyResults = dataMappingModes
                        .Select(mappingMode => algorithm.History(pySymbols, start, end, resolution, dataMappingMode: mappingMode))
                        .ToList();
                    AssertFuturesHistoryWithDifferentMappingModesResults(historyResults, symbol, expectedHistoryCount);

                    // Generic, single symbol, time span
                    historyResults = dataMappingModes
                        .Select(mappingMode => algorithm.History(pyTradeBar, pySymbol, timeSpan, resolution, dataMappingMode: mappingMode))
                        .ToList();
                    AssertFuturesHistoryWithDifferentMappingModesResults(historyResults, symbol, expectedHistoryCount);
                    // Same as previous but using a Symbol instead of pySymbol
                    historyResults = dataMappingModes
                        .Select(mappingMode => algorithm.History(pyTradeBar, symbol, timeSpan, resolution, dataMappingMode: mappingMode))
                        .ToList();
                    AssertFuturesHistoryWithDifferentMappingModesResults(historyResults, symbol, expectedHistoryCount);

                    // Generic, single symbol, periods
                    historyResults = dataMappingModes
                        .Select(mappingMode => algorithm.History(pyTradeBar, pySymbol, periods, resolution, dataMappingMode: mappingMode))
                        .ToList();
                    AssertFuturesHistoryWithDifferentMappingModesResults(historyResults, symbol, expectedHistoryCount);
                    // Same as previous but using a Symbol instead of pySymbol
                    historyResults = dataMappingModes
                        .Select(mappingMode => algorithm.History(pyTradeBar, symbol, periods, resolution, dataMappingMode: mappingMode))
                        .ToList();
                    AssertFuturesHistoryWithDifferentMappingModesResults(historyResults, symbol, expectedHistoryCount);

                    // Generic, single symbol, date range
                    historyResults = dataMappingModes
                        .Select(mappingMode => algorithm.History(pyTradeBar, pySymbol, start, end, resolution, dataMappingMode: mappingMode))
                        .ToList();
                    AssertFuturesHistoryWithDifferentMappingModesResults(historyResults, symbol, expectedHistoryCount);
                    // Same as previous but using a Symbol instead of pySymbol
                    historyResults = dataMappingModes
                        .Select(mappingMode => algorithm.History(pyTradeBar, symbol, start, end, resolution, dataMappingMode: mappingMode))
                        .ToList();
                    AssertFuturesHistoryWithDifferentMappingModesResults(historyResults, symbol, expectedHistoryCount);

                    // Generic, symbol array, time span
                    historyResults = dataMappingModes
                        .Select(mappingMode => algorithm.History(pyTradeBar, pySymbols, timeSpan, resolution, dataMappingMode: mappingMode))
                        .ToList();
                    AssertFuturesHistoryWithDifferentMappingModesResults(historyResults, symbol, expectedHistoryCount);

                    // Generic, symbol array, periods
                    historyResults = dataMappingModes
                        .Select(mappingMode => algorithm.History(pyTradeBar, pySymbols, periods, resolution, dataMappingMode: mappingMode))
                        .ToList();
                    AssertFuturesHistoryWithDifferentMappingModesResults(historyResults, symbol, expectedHistoryCount);

                    // Generic, symbol array, date range
                    historyResults = dataMappingModes
                        .Select(mappingMode => algorithm.History(pyTradeBar, pySymbols, start, end, resolution, dataMappingMode: mappingMode))
                        .ToList();
                    AssertFuturesHistoryWithDifferentMappingModesResults(historyResults, symbol, expectedHistoryCount);
                }
            }
        }

        [TestCaseSource(nameof(GetHistoryWithDataNormalizationModeTestCases))]
        public void HistoryRequestWithDataNormalizationMode(Language language, Symbol symbol, Resolution resolution,
            DateTime start, DateTime end, int expectedHistoryCount)
        {
            var algorithm = GetAlgorithm(end);
            algorithm.AddSecurity(symbol, fillForward: true);

            var dataNormalizationModes = symbol.SecurityType == SecurityType.Equity
                ? new[]
                {
                    DataNormalizationMode.Raw,
                    DataNormalizationMode.Adjusted,
                    DataNormalizationMode.SplitAdjusted
                }
                : new[]
                {
                    DataNormalizationMode.Raw,
                    DataNormalizationMode.BackwardsRatio,
                    DataNormalizationMode.BackwardsPanamaCanal,
                    DataNormalizationMode.ForwardPanamaCanal
                };
            var timeSpan = end - start;
            // Request periods is used to get the start and end dates only, the actual number of periods is expectedHistoryCount
            var periods = expectedHistoryCount;

            if (language == Language.CSharp)
            {
                // No symbol, time span
                var historyResults = dataNormalizationModes
                     .Select(normalizationMode => algorithm.History(timeSpan, resolution, dataNormalizationMode: normalizationMode).ToList())
                     .ToList();
                AssertHistoryWithDifferentNormalizationModesResults(historyResults, symbol, expectedHistoryCount, resolution);

                // No symbol, periods
                historyResults = dataNormalizationModes
                     .Select(normalizationMode => algorithm.History(periods, resolution, dataNormalizationMode: normalizationMode).ToList())
                     .ToList();
                AssertHistoryWithDifferentNormalizationModesResults(historyResults, symbol, expectedHistoryCount, resolution);

                // No symbol, date range
                // TODO: to be implemented

                // Single symbols, time span
                var tradeBarHistoryResults = dataNormalizationModes
                     .Select(normalizationMode => algorithm.History(symbol, timeSpan, resolution, dataNormalizationMode: normalizationMode).ToList())
                     .ToList();
                AssertHistoryWithDifferentNormalizationModesResults(tradeBarHistoryResults, symbol, expectedHistoryCount, resolution);

                // Single symbols, periods
                tradeBarHistoryResults = dataNormalizationModes
                     .Select(normalizationMode => algorithm.History(symbol, periods, resolution, dataNormalizationMode: normalizationMode).ToList())
                     .ToList();
                AssertHistoryWithDifferentNormalizationModesResults(tradeBarHistoryResults, symbol, expectedHistoryCount, resolution);

                // Single symbols, date range
                tradeBarHistoryResults = dataNormalizationModes
                     .Select(normalizationMode => algorithm.History(symbol, start, end, resolution,
                        dataNormalizationMode: normalizationMode).ToList())
                     .ToList();
                AssertHistoryWithDifferentNormalizationModesResults(tradeBarHistoryResults, symbol, expectedHistoryCount, resolution);

                // Symbol array, time span
                historyResults = dataNormalizationModes
                     .Select(normalizationMode => algorithm.History(new[] { symbol }, timeSpan, resolution,
                        dataNormalizationMode: normalizationMode).ToList())
                     .ToList();
                AssertHistoryWithDifferentNormalizationModesResults(historyResults, symbol, expectedHistoryCount, resolution);

                // Symbol array, periods
                historyResults = dataNormalizationModes
                     .Select(normalizationMode => algorithm.History(new[] { symbol }, periods, resolution,
                        dataNormalizationMode: normalizationMode).ToList())
                     .ToList();
                AssertHistoryWithDifferentNormalizationModesResults(historyResults, symbol, expectedHistoryCount, resolution);

                // Symbol array, date range
                historyResults = dataNormalizationModes
                     .Select(normalizationMode => algorithm.History(new[] { symbol }, start, end, resolution,
                        dataNormalizationMode: normalizationMode).ToList())
                     .ToList();
                AssertHistoryWithDifferentNormalizationModesResults(historyResults, symbol, expectedHistoryCount, resolution);

                // Generic, no symbol, time span
                var typedHistoryResults = dataNormalizationModes
                     .Select(normalizationMode => algorithm.History<TradeBar>(timeSpan, resolution,
                        dataNormalizationMode: normalizationMode).ToList())
                     .ToList();
                AssertHistoryWithDifferentNormalizationModesResults(typedHistoryResults, symbol, expectedHistoryCount, resolution);

                // Generic, no symbol, periods
                // TODO: to be implemented

                // Generic, no symbol, date range
                // TODO: to be implemented

                // Generic, single symbol, time span
                tradeBarHistoryResults = dataNormalizationModes
                     .Select(normalizationMode => algorithm.History<TradeBar>(symbol, timeSpan, resolution,
                        dataNormalizationMode: normalizationMode).ToList())
                     .ToList();
                AssertHistoryWithDifferentNormalizationModesResults(tradeBarHistoryResults, symbol, expectedHistoryCount, resolution);

                // Generic, single symbol, periods
                tradeBarHistoryResults = dataNormalizationModes
                     .Select(normalizationMode => algorithm.History<TradeBar>(symbol, periods, resolution,
                        dataNormalizationMode: normalizationMode).ToList())
                     .ToList();
                AssertHistoryWithDifferentNormalizationModesResults(tradeBarHistoryResults, symbol, expectedHistoryCount, resolution);

                // Generic, single symbol, date range
                tradeBarHistoryResults = dataNormalizationModes
                     .Select(normalizationMode => algorithm.History<TradeBar>(symbol, start, end, resolution,
                        dataNormalizationMode: normalizationMode).ToList())
                     .ToList();
                AssertHistoryWithDifferentNormalizationModesResults(tradeBarHistoryResults, symbol, expectedHistoryCount, resolution);

                // Generic, symbol array, time span
                typedHistoryResults = dataNormalizationModes
                     .Select(normalizationMode =>
                        algorithm.History<TradeBar>(new[] { symbol }, timeSpan, resolution, dataNormalizationMode: normalizationMode).ToList())
                     .ToList();
                AssertHistoryWithDifferentNormalizationModesResults(typedHistoryResults, symbol, expectedHistoryCount, resolution);

                // Generic, symbol array, periods
                typedHistoryResults = dataNormalizationModes
                     .Select(normalizationMode =>
                        algorithm.History<TradeBar>(new[] { symbol }, periods, resolution, dataNormalizationMode: normalizationMode).ToList())
                     .ToList();
                AssertHistoryWithDifferentNormalizationModesResults(typedHistoryResults, symbol, expectedHistoryCount, resolution);

                // Generic, symbol array, date range
                typedHistoryResults = dataNormalizationModes
                     .Select(normalizationMode =>
                        algorithm.History<TradeBar>(new[] { symbol }, start, end, resolution, dataNormalizationMode: normalizationMode).ToList())
                     .ToList();
                AssertHistoryWithDifferentNormalizationModesResults(typedHistoryResults, symbol, expectedHistoryCount, resolution);
            }
            else
            {
                using (Py.GIL())
                {
                    var testModule = PyModule.FromString("testModule", @"
from AlgorithmImports import *
tradeBar = TradeBar
                    ");

                    algorithm.SetPandasConverter();
                    using var pySymbol = symbol.ToPython();
                    using var pySymbols = new PyList(new[] { pySymbol });
                    using var pyTradeBar = testModule.GetAttr("tradeBar");

                    // Single symbols, time span
                    var historyResults = dataNormalizationModes
                        .Select(normalizationMode => algorithm.History(pySymbol, timeSpan, resolution, dataNormalizationMode: normalizationMode))
                        .ToList();
                    AssertHistoryWithDifferentNormalizationModesResults(historyResults, symbol, expectedHistoryCount);

                    // Single symbols, periods
                    historyResults = dataNormalizationModes
                        .Select(normalizationMode => algorithm.History(pySymbol, periods, resolution, dataNormalizationMode: normalizationMode))
                        .ToList();
                    AssertHistoryWithDifferentNormalizationModesResults(historyResults, symbol, expectedHistoryCount);

                    // Single symbols, date range
                    historyResults = dataNormalizationModes
                        .Select(normalizationMode => algorithm.History(pySymbol, start, end, resolution, dataNormalizationMode: normalizationMode))
                        .ToList();
                    AssertHistoryWithDifferentNormalizationModesResults(historyResults, symbol, expectedHistoryCount);

                    // Symbol array, time span
                    historyResults = dataNormalizationModes
                        .Select(normalizationMode => algorithm.History(pySymbols, timeSpan, resolution, dataNormalizationMode: normalizationMode))
                        .ToList();
                    AssertHistoryWithDifferentNormalizationModesResults(historyResults, symbol, expectedHistoryCount);

                    // Symbol array, periods
                    historyResults = dataNormalizationModes
                        .Select(normalizationMode => algorithm.History(pySymbols, periods, resolution, dataNormalizationMode: normalizationMode))
                        .ToList();
                    AssertHistoryWithDifferentNormalizationModesResults(historyResults, symbol, expectedHistoryCount);

                    // Symbol array, date range
                    historyResults = dataNormalizationModes
                        .Select(normalizationMode => algorithm.History(pySymbols, start, end, resolution, dataNormalizationMode: normalizationMode))
                        .ToList();
                    AssertHistoryWithDifferentNormalizationModesResults(historyResults, symbol, expectedHistoryCount);

                    // Generic, single symbol, time span
                    historyResults = dataNormalizationModes
                        .Select(normalizationMode => algorithm.History(pyTradeBar, pySymbol, timeSpan, resolution,
                            dataNormalizationMode: normalizationMode))
                        .ToList();
                    AssertHistoryWithDifferentNormalizationModesResults(historyResults, symbol, expectedHistoryCount);
                    // Same as previous but using a Symbol instead of pySymbol
                    historyResults = dataNormalizationModes
                        .Select(normalizationMode => algorithm.History(pyTradeBar, symbol, timeSpan, resolution,
                            dataNormalizationMode: normalizationMode))
                        .ToList();
                    AssertHistoryWithDifferentNormalizationModesResults(historyResults, symbol, expectedHistoryCount);

                    // Generic, single symbol, periods
                    historyResults = dataNormalizationModes
                        .Select(normalizationMode => algorithm.History(pyTradeBar, pySymbol, periods, resolution,
                            dataNormalizationMode: normalizationMode))
                        .ToList();
                    AssertHistoryWithDifferentNormalizationModesResults(historyResults, symbol, expectedHistoryCount);
                    // Same as previous but using a Symbol instead of pySymbol
                    historyResults = dataNormalizationModes
                        .Select(normalizationMode => algorithm.History(pyTradeBar, symbol, periods, resolution,
                            dataNormalizationMode: normalizationMode))
                        .ToList();
                    AssertHistoryWithDifferentNormalizationModesResults(historyResults, symbol, expectedHistoryCount);

                    // Generic, single symbol, date range
                    historyResults = dataNormalizationModes
                        .Select(normalizationMode => algorithm.History(pyTradeBar, pySymbol, start, end, resolution,
                            dataNormalizationMode: normalizationMode))
                        .ToList();
                    AssertHistoryWithDifferentNormalizationModesResults(historyResults, symbol, expectedHistoryCount);
                    // Same as previous but using a Symbol instead of pySymbol
                    historyResults = dataNormalizationModes
                        .Select(normalizationMode => algorithm.History(pyTradeBar, symbol, start, end, resolution,
                            dataNormalizationMode: normalizationMode))
                        .ToList();
                    AssertHistoryWithDifferentNormalizationModesResults(historyResults, symbol, expectedHistoryCount);

                    // Generic, symbol array, time span
                    historyResults = dataNormalizationModes
                        .Select(normalizationMode => algorithm.History(pyTradeBar, pySymbols, timeSpan, resolution,
                            dataNormalizationMode: normalizationMode))
                        .ToList();
                    AssertHistoryWithDifferentNormalizationModesResults(historyResults, symbol, expectedHistoryCount);

                    // Generic, symbol array, periods
                    historyResults = dataNormalizationModes
                        .Select(normalizationMode => algorithm.History(pyTradeBar, pySymbols, periods, resolution,
                            dataNormalizationMode: normalizationMode))
                        .ToList();
                    AssertHistoryWithDifferentNormalizationModesResults(historyResults, symbol, expectedHistoryCount);

                    // Generic, symbol array, date range
                    historyResults = dataNormalizationModes
                        .Select(normalizationMode => algorithm.History(pyTradeBar, pySymbols, start, end, resolution,
                            dataNormalizationMode: normalizationMode))
                        .ToList();
                    AssertHistoryWithDifferentNormalizationModesResults(historyResults, symbol, expectedHistoryCount);
                }
            }
        }

        // C#
        [TestCase(Language.CSharp, Resolution.Daily, 61)]
        [TestCase(Language.CSharp, Resolution.Hour, 477)]
        [TestCase(Language.CSharp, Resolution.Minute, 26670)]
        // Python
        [TestCase(Language.Python, Resolution.Daily, 61)]
        [TestCase(Language.Python, Resolution.Hour, 477)]
        [TestCase(Language.Python, Resolution.Minute, 26670)]
        public void HistoryRequestWithContracDepthOffsets(Language language, Resolution resolution, int expectedHistoryCount)
        {
            var start = new DateTime(2013, 10, 07);
            var end = new DateTime(2014, 1, 1);
            var algorithm = GetAlgorithm(end);
            var symbol = algorithm.AddFuture(Futures.Indices.SP500EMini, fillForward: true).Symbol;

            var timeSpan = end - start;
            // We are lacking minute data for the contract used for offset=2 on the request start date computed minute when using periods,
            // So we use a slightly larger number of periods to compensate for that.
            var periods = resolution != Resolution.Minute ? expectedHistoryCount : (int)(expectedHistoryCount * 1.001);
            var offsets = new[] { 0, 1, 2 };

            if (language == Language.CSharp)
            {
                // No symbol, time span
                var historyResults = offsets
                     .Select(offset => algorithm.History(timeSpan, resolution, contractDepthOffset: offset).ToList())
                     .ToList();
                AssertFuturesHistoryWithDifferentContractDepthOffsetsResults(historyResults, symbol, expectedHistoryCount, resolution);

                // No symbol, periods
                historyResults = offsets
                     .Select(offset => algorithm.History(periods, resolution, contractDepthOffset: offset).ToList())
                     .ToList();
                AssertFuturesHistoryWithDifferentContractDepthOffsetsResults(historyResults, symbol, expectedHistoryCount, resolution);

                // No symbol, date range
                // TODO: to be implemented

                // Single symbols, time span
                var tradeBarHistoryResults = offsets
                     .Select(offset => algorithm.History(symbol, timeSpan, resolution, contractDepthOffset: offset).ToList())
                     .ToList();
                AssertFuturesHistoryWithDifferentContractDepthOffsetsResults(tradeBarHistoryResults, symbol, expectedHistoryCount, resolution);

                // Single symbols, periods
                tradeBarHistoryResults = offsets
                     .Select(offset => algorithm.History(symbol, periods, resolution, contractDepthOffset: offset).ToList())
                     .ToList();
                AssertFuturesHistoryWithDifferentContractDepthOffsetsResults(tradeBarHistoryResults, symbol, expectedHistoryCount, resolution);

                // Single symbols, date range
                tradeBarHistoryResults = offsets
                     .Select(offset => algorithm.History(symbol, start, end, resolution, contractDepthOffset: offset).ToList())
                     .ToList();
                AssertFuturesHistoryWithDifferentContractDepthOffsetsResults(tradeBarHistoryResults, symbol, expectedHistoryCount, resolution);

                // Symbol array, time span
                historyResults = offsets
                     .Select(offset => algorithm.History(new[] { symbol }, timeSpan, resolution, contractDepthOffset: offset).ToList())
                     .ToList();
                AssertFuturesHistoryWithDifferentContractDepthOffsetsResults(historyResults, symbol, expectedHistoryCount, resolution);

                // Symbol array, periods
                historyResults = offsets
                     .Select(offset => algorithm.History(new[] { symbol }, periods, resolution, contractDepthOffset: offset).ToList())
                     .ToList();
                AssertFuturesHistoryWithDifferentContractDepthOffsetsResults(historyResults, symbol, expectedHistoryCount, resolution);

                // Symbol array, date range
                historyResults = offsets
                     .Select(offset => algorithm.History(new[] { symbol }, start, end, resolution, contractDepthOffset: offset).ToList())
                     .ToList();
                AssertFuturesHistoryWithDifferentContractDepthOffsetsResults(historyResults, symbol, expectedHistoryCount, resolution);

                // Generic, no symbol, time span
                var typedHistoryResults = offsets
                     .Select(offset => algorithm.History<TradeBar>(timeSpan, resolution, contractDepthOffset: offset).ToList())
                     .ToList();
                AssertFuturesHistoryWithDifferentContractDepthOffsetsResults(tradeBarHistoryResults, symbol, expectedHistoryCount, resolution);

                // Generic, no symbol, periods
                // TODO: to be implemented

                // Generic, no symbol, date range
                // TODO: to be implemented

                // Generic, single symbol, time span
                tradeBarHistoryResults = offsets
                     .Select(offset => algorithm.History<TradeBar>(symbol, timeSpan, resolution, contractDepthOffset: offset).ToList())
                     .ToList();
                AssertFuturesHistoryWithDifferentContractDepthOffsetsResults(tradeBarHistoryResults, symbol, expectedHistoryCount, resolution);

                // Generic, single symbol, periods
                tradeBarHistoryResults = offsets
                     .Select(offset => algorithm.History<TradeBar>(symbol, periods, resolution, contractDepthOffset: offset).ToList())
                     .ToList();
                AssertFuturesHistoryWithDifferentContractDepthOffsetsResults(tradeBarHistoryResults, symbol, expectedHistoryCount, resolution);

                // Generic, single symbol, date range
                tradeBarHistoryResults = offsets
                     .Select(offset => algorithm.History<TradeBar>(symbol, start, end, resolution, contractDepthOffset: offset).ToList())
                     .ToList();
                AssertFuturesHistoryWithDifferentContractDepthOffsetsResults(tradeBarHistoryResults, symbol, expectedHistoryCount, resolution);

                // Generic, symbol array, time span
                typedHistoryResults = offsets
                     .Select(offset =>
                        algorithm.History<TradeBar>(new[] { symbol }, timeSpan, resolution, contractDepthOffset: offset).ToList())
                     .ToList();
                AssertFuturesHistoryWithDifferentContractDepthOffsetsResults(typedHistoryResults, symbol, expectedHistoryCount, resolution);

                // Generic, symbol array, periods
                typedHistoryResults = offsets
                     .Select(offset =>
                        algorithm.History<TradeBar>(new[] { symbol }, periods, resolution, contractDepthOffset: offset).ToList())
                     .ToList();
                AssertFuturesHistoryWithDifferentContractDepthOffsetsResults(typedHistoryResults, symbol, expectedHistoryCount, resolution);

                // Generic, symbol array, date range
                typedHistoryResults = offsets
                     .Select(offset =>
                        algorithm.History<TradeBar>(new[] { symbol }, start, end, resolution, contractDepthOffset: offset).ToList())
                     .ToList();
                AssertFuturesHistoryWithDifferentContractDepthOffsetsResults(typedHistoryResults, symbol, expectedHistoryCount, resolution);
            }
            else
            {
                using (Py.GIL())
                {
                    var testModule = PyModule.FromString("testModule", @"
from AlgorithmImports import *
tradeBar = TradeBar
                    ");

                    algorithm.SetPandasConverter();
                    using var pySymbol = symbol.ToPython();
                    using var pySymbols = new PyList(new[] { pySymbol });
                    using var pyTradeBar = testModule.GetAttr("tradeBar");

                    // Single symbols, time span
                    var historyResults = offsets
                        .Select(offset => algorithm.History(pySymbol, timeSpan, resolution, contractDepthOffset: offset))
                        .ToList();
                    AssertFuturesHistoryWithDifferentContractDepthOffsetsResults(historyResults, symbol, expectedHistoryCount);

                    // Single symbols, periods
                    historyResults = offsets
                        .Select(offset => algorithm.History(pySymbol, periods, resolution, contractDepthOffset: offset))
                        .ToList();
                    AssertFuturesHistoryWithDifferentContractDepthOffsetsResults(historyResults, symbol, expectedHistoryCount);

                    // Single symbols, date range
                    historyResults = offsets
                        .Select(offset => algorithm.History(pySymbol, start, end, resolution, contractDepthOffset: offset))
                        .ToList();
                    AssertFuturesHistoryWithDifferentContractDepthOffsetsResults(historyResults, symbol, expectedHistoryCount);

                    // Symbol array, time span
                    historyResults = offsets
                        .Select(offset => algorithm.History(pySymbols, timeSpan, resolution, contractDepthOffset: offset))
                        .ToList();
                    AssertFuturesHistoryWithDifferentContractDepthOffsetsResults(historyResults, symbol, expectedHistoryCount);

                    // Symbol array, periods
                    historyResults = offsets
                        .Select(offset => algorithm.History(pySymbols, periods, resolution, contractDepthOffset: offset))
                        .ToList();
                    AssertFuturesHistoryWithDifferentContractDepthOffsetsResults(historyResults, symbol, expectedHistoryCount);

                    // Symbol array, date range
                    historyResults = offsets
                        .Select(offset => algorithm.History(pySymbols, start, end, resolution, contractDepthOffset: offset))
                        .ToList();
                    AssertFuturesHistoryWithDifferentContractDepthOffsetsResults(historyResults, symbol, expectedHistoryCount);

                    // Generic, single symbol, time span
                    historyResults = offsets
                        .Select(offset => algorithm.History(pyTradeBar, pySymbol, timeSpan, resolution, contractDepthOffset: offset))
                        .ToList();
                    AssertFuturesHistoryWithDifferentContractDepthOffsetsResults(historyResults, symbol, expectedHistoryCount);
                    // Same as previous but using a Symbol instead of pySymbol
                    historyResults = offsets
                        .Select(offset => algorithm.History(pyTradeBar, symbol, timeSpan, resolution, contractDepthOffset: offset))
                        .ToList();
                    AssertFuturesHistoryWithDifferentContractDepthOffsetsResults(historyResults, symbol, expectedHistoryCount);

                    // Generic, single symbol, periods
                    historyResults = offsets
                        .Select(offset => algorithm.History(pyTradeBar, pySymbol, periods, resolution, contractDepthOffset: offset))
                        .ToList();
                    AssertFuturesHistoryWithDifferentContractDepthOffsetsResults(historyResults, symbol, expectedHistoryCount);
                    // Same as previous but using a Symbol instead of pySymbol
                    historyResults = offsets
                        .Select(offset => algorithm.History(pyTradeBar, symbol, periods, resolution, contractDepthOffset: offset))
                        .ToList();
                    AssertFuturesHistoryWithDifferentContractDepthOffsetsResults(historyResults, symbol, expectedHistoryCount);

                    // Generic, single symbol, date range
                    historyResults = offsets
                        .Select(offset => algorithm.History(pyTradeBar, pySymbol, start, end, resolution, contractDepthOffset: offset))
                        .ToList();
                    AssertFuturesHistoryWithDifferentContractDepthOffsetsResults(historyResults, symbol, expectedHistoryCount);
                    // Same as previous but using a Symbol instead of pySymbol
                    historyResults = offsets
                        .Select(offset => algorithm.History(pyTradeBar, symbol, start, end, resolution, contractDepthOffset: offset))
                        .ToList();
                    AssertFuturesHistoryWithDifferentContractDepthOffsetsResults(historyResults, symbol, expectedHistoryCount);

                    // Generic, symbol array, time span
                    historyResults = offsets
                        .Select(offset => algorithm.History(pyTradeBar, pySymbols, timeSpan, resolution, contractDepthOffset: offset))
                        .ToList();
                    AssertFuturesHistoryWithDifferentContractDepthOffsetsResults(historyResults, symbol, expectedHistoryCount);

                    // Generic, symbol array, periods
                    historyResults = offsets
                        .Select(offset => algorithm.History(pyTradeBar, pySymbols, periods, resolution, contractDepthOffset: offset))
                        .ToList();
                    AssertFuturesHistoryWithDifferentContractDepthOffsetsResults(historyResults, symbol, expectedHistoryCount);

                    // Generic, symbol array, date range
                    historyResults = offsets
                        .Select(offset => algorithm.History(pyTradeBar, pySymbols, start, end, resolution, contractDepthOffset: offset))
                        .ToList();
                    AssertFuturesHistoryWithDifferentContractDepthOffsetsResults(historyResults, symbol, expectedHistoryCount);
                }
            }
        }

        // USA Equity market hours: 4am-9:30am (pre-market), 9:30am-4pm (regular market), 4pm-8pm (post-market)
        // 6.5h (regular market duration) (rounded to 7)
        [TestCase(Resolution.Hour, false, false, 7)]
        // Hour resolution doesn't have extended hours data
        [TestCase(Resolution.Hour, false, true, 7)]
        [TestCase(Resolution.Hour, true, false, 7)]
        [TestCase(Resolution.Hour, true, true, 7)]
        [TestCase(Resolution.Hour, false, null, 7)]
        [TestCase(Resolution.Hour, true, null, 7)]
        // 390 = 6.5h (regular market duration) * 60min/h = 390min (bars)
        [TestCase(Resolution.Minute, false, false, 390)]
        // 960 = [5.5h (pre-market duration) + 6.5h (regular market duration) + 4h (post-market duration)] * 60min/h = 16h * 60min/h = 960min (bars)
        [TestCase(Resolution.Minute, false, true, 960)]
        [TestCase(Resolution.Minute, true, false, 390)]
        [TestCase(Resolution.Minute, true, true, 960)]
        [TestCase(Resolution.Minute, false, null, 390)]
        [TestCase(Resolution.Minute, true, null, 960)]
        [TestCase(Resolution.Second, false, false, 390 * 60)]
        [TestCase(Resolution.Second, false, true, 960 * 60)]
        [TestCase(Resolution.Second, true, false, 390 * 60)]
        [TestCase(Resolution.Second, true, true, 960 * 60)]
        [TestCase(Resolution.Second, false, null, 390 * 60)]
        [TestCase(Resolution.Second, true, null, 960 * 60)]
        public void HistoryRequestFactoryGetsTheRightStartTimeForBarCount(Resolution resolution, bool assetWithExtendedMarket,
            bool? requestWithExtendedMarket, int requestPeriods)
        {
            var start = new DateTime(2014, 06, 09);
            var end = new DateTime(2014, 06, 10);
            var algorithm = GetAlgorithm(end);
            var aapl = algorithm.AddEquity("AAPL", extendedMarketHours: assetWithExtendedMarket);
            var config = algorithm.SubscriptionManager.SubscriptionDataConfigService.GetSubscriptionDataConfigs(aapl.Symbol).First();
            var exchangeHours = aapl.Exchange.Hours;

            var historyRequestFactory = new HistoryRequestFactory(algorithm);
            var extendedMarket = resolution != Resolution.Hour ? requestWithExtendedMarket ?? assetWithExtendedMarket : false;
            var marketOpen = exchangeHours.GetNextMarketOpen(start, extendedMarketHours: extendedMarket);
            if (resolution == Resolution.Hour)
            {
                // Adjust the expected start in case the regular hours segment is not an exact int number of hours
                var marketClose = exchangeHours.GetNextMarketClose(marketOpen, extendedMarketHours: extendedMarket);
                marketOpen += TimeSpan.FromHours((marketClose.TimeOfDay - marketOpen.TimeOfDay).TotalHours - requestPeriods);
            }

            var requestStart = historyRequestFactory.GetStartTimeAlgoTz(aapl.Symbol, requestPeriods, resolution, exchangeHours,
                config.DataTimeZone, config.Type, extendedMarketHours: requestWithExtendedMarket);
            Assert.AreEqual(marketOpen, requestStart);
        }

        // This reproduces https://github.com/QuantConnect/Lean/issues/7504
        [TestCase(Language.CSharp)]
        [TestCase(Language.Python)]
        public void DefaultAlwaysOpenMarketHoursForBaseSecurityType(Language language)
        {
            var start = new DateTime(2013, 10, 8);
            var end = start.AddDays(1);
            var algorithm = GetAlgorithm(end);

            if (language == Language.CSharp)
            {
                var spy = algorithm.AddEquity("SPY").Symbol;
                // We will try to fetch history without a subscription
                var customSymbol = Symbol.CreateBase(typeof(CustomData), spy);

                List<CustomData> history = null;
                Assert.DoesNotThrow(() => history = algorithm.History<CustomData>(customSymbol, start, end, Resolution.Minute).ToList());
                Console.WriteLine(history.Count);
                Assert.IsNotEmpty(history);
            }
            else
            {
                using (Py.GIL())
                {
                    dynamic getHistory = PyModule.FromString("testModule",
                        @"
from AlgorithmImports import *
from QuantConnect.Tests import *

class TestPythonCustomData(PythonData):
    def GetSource(self, config, date, isLiveMode):
        fileName = LeanData.GenerateZipFileName(Symbols.SPY, date, config.Resolution, config.TickType)
        source = f'{Globals.DataFolder}equity/usa/minute/spy/{fileName}'
        return SubscriptionDataSource(source, SubscriptionTransportMedium.LocalFile, FileFormat.Csv)

    def Reader(self, config, line, date, isLiveMode):
        data = line.split(',')

        result = TestPythonCustomData()
        result.DataType = MarketDataType.Base
        result.Symbol = config.Symbol
        result.Time = date.date() + timedelta(milliseconds=int(data[0]))
        result.Value = float(data[4])
        result[""Open""] = float(data[1])
        result[""High""] = float(data[2])
        result[""Low""] = float(data[3])
        result[""Close""] = float(data[4])

        return result

def getHistory(algorithm, start, end):
    spy = algorithm.AddEquity(""SPY"").Symbol
    customSymbol = Symbol.CreateBase(TestPythonCustomData, spy, Market.USA)

    return algorithm.History(TestPythonCustomData, customSymbol, start, end, Resolution.Minute)
        ").GetAttr("getHistory");

                    algorithm.SetPandasConverter();

                    dynamic history = null;
                    Assert.DoesNotThrow(() => history = getHistory(algorithm, start, end));
                    Assert.IsNotNull(history);
                    Assert.Greater(history.shape[0].As<int>(), 0);
                }
            }
        }

        [Test]
        public void PythonCustomDataThrowing()
        {
            var algorithm = GetAlgorithm(new DateTime(2013, 10, 8));
            algorithm.SetHistoryProvider(new ThrowingHistoryProvider());

            using (Py.GIL())
            {
                PythonInitializer.Initialize();
                algorithm.SetPandasConverter();

                var testModule = PyModule.FromString("testModule",
                    @"
from AlgorithmImports import *

def getHistory(algorithm, symbol, period):
    return algorithm.History(symbol, period, Resolution.Minute)
    ");
                dynamic getDateRangeHistory = testModule.GetAttr("getHistory");

                Assert.Throws<ClrBubbledException>(() => getDateRangeHistory(algorithm, Symbols.AAPL, 10));
            }
        }

        [Test]
        public void PythonUniverseHistoryDataFramesAreFlattened()
        {
            var algorithm = GetAlgorithm(new DateTime(2014, 03, 28));
            var universe = algorithm.AddUniverse(x => x.Select(x => x.Symbol));

            using (Py.GIL())
            {
                PythonInitializer.Initialize();
                algorithm.SetPandasConverter();

                var testModule = PyModule.FromString("PythonHistoryDataFramesAreFlattened",
                    @"
from AlgorithmImports import *

def getFlattenedUniverseHistory(algorithm, universe, period):
    return algorithm.history(universe, period, flatten=True)

def getUnflattenedUniverseHistory(algorithm, universe, period):
    return algorithm.history(universe, period)

def assertFlattenedHistoryDates(df, expected_dates):
    assert df.index.levels[0].to_list() == expected_dates, f'Expected dates: {expected_dates}, actual dates: {df.index.levels[0].to_list()}'

def assertUnflattenedHistoryDates(df, expected_dates):
    assert df.index.to_list() == expected_dates, f'Expected dates: {expected_dates}, actual dates: {df.index.levels[0].to_list()}'

def assertConstituents(flattened_df, unflattened_df, dates, expected_constituents_per_date):
    for i, date in enumerate(dates):
        unflattened_universe = unflattened_df.loc[date]
        assert isinstance(unflattened_universe, list), f'Unflattened DF: expected a list, found {type(unflattened_universe)}'
        assert len(unflattened_universe) == expected_constituents_per_date[i], f'Unflattened DF: expected {expected_constituents_per_date[i]} constituents for date {date}, got {len(unflattened_universe)}'

        for constituent in unflattened_universe:
            assert isinstance(constituent, Fundamental), f'Unflattened DF: expected a list of Fundamental, found {type(constituent)}'

        flattened_sub_df = flattened_df.loc[date]
        assert flattened_sub_df.shape[0] == len(unflattened_universe), f'Flattened DF: expected {len(unflattened_universe)} rows for date {date}, got {flattened_sub_df.shape[0]}'

        flattened_universe_symbols = flattened_sub_df.index.to_list()
        unflattened_universe_symbols = [constituent.symbol for constituent in unflattened_universe]
        flattened_universe_symbols.sort()
        unflattened_universe_symbols.sort()
        assert flattened_universe_symbols == unflattened_universe_symbols, f'Flattened DF: flattened universe symbols are not equal to unflattened universe symbols for date {date}'
    ");
                dynamic getFlattenedUniverseHistory = testModule.GetAttr("getFlattenedUniverseHistory");
                var flattenedDf = getFlattenedUniverseHistory(algorithm, universe, 3);

                dynamic getUnflattenedUniverseHistory = testModule.GetAttr("getUnflattenedUniverseHistory");
                var unflattenedDf = getUnflattenedUniverseHistory(algorithm, universe, 3);
                // Drop the symbol
                unflattenedDf = unflattenedDf.droplevel(0);

                var expectedDates = new List<DateTime>
                {
                    new DateTime(2014, 03, 26),
                    new DateTime(2014, 03, 27),
                    new DateTime(2014, 03, 28)
                };
                dynamic assertFlattenedHistoryDates = testModule.GetAttr("assertFlattenedHistoryDates");
                AssertDesNotThrowPythonException(() => assertFlattenedHistoryDates(flattenedDf, expectedDates));

                dynamic assertUnflattenedHistoryDates = testModule.GetAttr("assertUnflattenedHistoryDates");
                AssertDesNotThrowPythonException(() => assertUnflattenedHistoryDates(unflattenedDf, expectedDates));

                var expectedConstituentsCounts = new[] { 7068, 7055, 7049 };
                dynamic assertConstituents = testModule.GetAttr("assertConstituents");
                AssertDesNotThrowPythonException(() => assertConstituents(flattenedDf, unflattenedDf, expectedDates, expectedConstituentsCounts));
            }
        }

        [Test]
        public void CSharpCustomUniverseHistoryDataFramesHaveExpectedFormat()
        {
            var algorithm = GetAlgorithm(new DateTime(2015, 01, 15));
            var universe = algorithm.AddUniverse<CustomUniverseData>("CustomUniverse", Resolution.Daily, (x) => x.Select(y => y.Symbol));

            using (Py.GIL())
            {
                PythonInitializer.Initialize();
                algorithm.SetPandasConverter();

                using var testModule = PyModule.FromString("PythonCustomUniverseHistoryDataFramesHaveExpectedFormat",
                    $@"
from AlgorithmImports import *

def get_universe_history(algorithm, universe, flatten):
    return algorithm.history(universe, 3, flatten=flatten)
    ");

                dynamic getUniverseHistory = testModule.GetAttr("get_universe_history");
                var df = getUniverseHistory(algorithm, universe, false);
                var flattenedDf = getUniverseHistory(algorithm, universe, true);

                Func<CustomUniverseData, decimal> getWeight = (data) => data.Weight;
                AssertCustomUniverseDataFrames(df, flattenedDf, getWeight);

                var columns = ((List<PyObject>)flattenedDf.columns.to_list().As<List<PyObject>>())
                    .Select(column => column.InvokeMethod("__str__").GetAndDispose<string>());
                CollectionAssert.DoesNotContain(columns, "data");
            }
        }

        [Test]
        public void PythonCustomUniverseHistoryDataFramesHaveExpectedFormat()
        {
            var algorithm = GetAlgorithm(new DateTime(2015, 01, 15));

            using (Py.GIL())
            {
                PythonInitializer.Initialize();
                algorithm.SetPandasConverter();

                using var testModule = PyModule.FromString("PythonCustomUniverseHistoryDataFramesHaveExpectedFormat",
                    $@"
from AlgorithmImports import *

class CustomUniverseData(PythonData):

    def get_source(self, config: SubscriptionDataConfig, date: datetime, is_live_mode: bool) -> SubscriptionDataSource:
        return SubscriptionDataSource('TestData/portfolio_targets.csv',
                                      SubscriptionTransportMedium.LOCAL_FILE,
                                      FileFormat.FOLDING_COLLECTION)

    def reader(self, config: SubscriptionDataConfig, line: str, date: datetime, is_live_mode: bool) -> BaseData:
        # Skip the header row.
        if not line[0].isnumeric():
            return None
        items = line.split(',')
        data = CustomUniverseData()
        data.end_time = datetime.strptime(items[0], '%Y-%m-%d')
        data.time = data.end_time - timedelta(1)
        data.symbol = Symbol.create(items[1], SecurityType.EQUITY, Market.USA)
        data['weight'] = float(items[2])
        return data

def get_universe_history(algorithm, flatten):
    universe = algorithm.add_universe(CustomUniverseData, 'CustomUniverse', Resolution.DAILY, lambda alt_coarse: [x.symbol for x in alt_coarse])
    return algorithm.history(universe, 3, flatten=flatten)

    ");

                dynamic getUniverseHistory = testModule.GetAttr("get_universe_history");
                var df = getUniverseHistory(algorithm, false);
                var flattenedDf = getUniverseHistory(algorithm, true);

                Func<PythonData, decimal> getWeight = (data) => Convert.ToDecimal(data.GetProperty("weight"));
                AssertCustomUniverseDataFrames(df, flattenedDf, getWeight);
            }
        }

        [Test]
        public void PythonCustomUniverseHistoryCanBeFetchedUsingCSharpApi()
        {
            var algorithm = GetAlgorithm(new DateTime(2018, 6, 1));

            using (Py.GIL())
            {
                var testModule = PyModule.FromString("PythonCustomUniverseHistoryCanBeFetchedUsingCSharpApi",
                    @"
from AlgorithmImports import *


class StockDataSource(PythonData):

    def get_source(self, config: SubscriptionDataConfig, date: datetime, is_live: bool) -> SubscriptionDataSource:
        source = ""../../TestData/daily-stock-picker-backtest.csv""
        return SubscriptionDataSource(source, SubscriptionTransportMedium.LocalFile, FileFormat.Csv)

    def reader(self, config: SubscriptionDataConfig, line: str, date: datetime, is_live: bool) -> BaseData:
        if not (line.strip() and line[0].isdigit()): return None

        stocks = StockDataSource()
        stocks.symbol = config.symbol

        try:
            csv = line.split(',')
            stocks.time = datetime.strptime(csv[0], ""%Y%m%d"")
            stocks.end_time = stocks.time + timedelta(days=1)
            stocks[""Symbols""] = csv[1:]

        except ValueError:
            # Do nothing
            return None

        return stocks

def universe_selector(data):
        return [x.symbol for x in data]

def add_universe(algorithm):
    return algorithm.add_universe(StockDataSource, ""universe-stock-data-source"", Resolution.DAILY, universe_selector)

def get_history(algorithm, universe):
    return list(algorithm.history[StockDataSource](universe.symbol, datetime(2018, 1, 1), datetime(2018, 6, 1), Resolution.DAILY))
");

                dynamic getUniverse = testModule.GetAttr("add_universe");
                dynamic getHistory = testModule.GetAttr("get_history");

                var universe = getUniverse(algorithm);
                var history = getHistory(algorithm, universe).As<List<PythonData>>() as List<PythonData>;
                Assert.IsNotEmpty(history);
            }
        }

        [Test]
        public void PythonCustomDataHistoryCanBeFetchedUsingCSharpApi()
        {
            var algorithm = GetAlgorithm(new DateTime(2013, 10, 8));

            using (Py.GIL())
            {
                var testModule = PyModule.FromString("PythonCustomDataHistoryCanBeFetchedUsingCSharpApi",
                    @"
from AlgorithmImports import *
from QuantConnect.Tests import *

class MyCustomDataType(PythonData):

    def get_source(self, config: SubscriptionDataConfig, date: datetime, is_live: bool) -> SubscriptionDataSource:
        fileName = LeanData.GenerateZipFileName(Symbols.SPY, date, Resolution.MINUTE, config.TickType)
        source = f'{Globals.DataFolder}equity/usa/minute/spy/{fileName}'
        return SubscriptionDataSource(source, SubscriptionTransportMedium.LocalFile, FileFormat.Csv)

    def reader(self, config: SubscriptionDataConfig, line: str, date: datetime, is_live: bool) -> BaseData:
        data = line.split(',')
        result = MyCustomDataType()
        result.DataType = MarketDataType.Base
        result.Symbol = config.Symbol
        result.Time = date + timedelta(milliseconds=int(data[0]))
        result.Value = 1

        return result

def add_data(algorithm):
    return algorithm.add_data(MyCustomDataType, ""MyCustomDataType"", Resolution.DAILY)

def get_history(algorithm, security):
    return list(algorithm.history[MyCustomDataType](security.symbol, datetime(2013, 10, 7), datetime(2013, 10, 8), Resolution.MINUTE))
");

                dynamic getCustomSecurity = testModule.GetAttr("add_data");
                dynamic getHistory = testModule.GetAttr("get_history");

                var security = getCustomSecurity(algorithm);
                var history = getHistory(algorithm, security).As<List<PythonData>>() as List<PythonData>;
                Assert.IsNotEmpty(history);
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void HistoryHandlesSymbolChangedEventsCorrectly(bool useCreateSymbol)
        {
            var start = new DateTime(2021, 1, 1);
            _algorithm = GetAlgorithm(start);
            _algorithm.SetEndDate(2021, 1, 5);

            Symbol symbol;
            if (useCreateSymbol)
            {
                symbol = Symbol.Create(Futures.Indices.SP500EMini, SecurityType.Future, Market.CME);
            }
            else
            {
                var future = _algorithm.AddFuture(
                    Futures.Indices.SP500EMini,
                    dataMappingMode: DataMappingMode.OpenInterest,
                    dataNormalizationMode: DataNormalizationMode.BackwardsRatio,
                    contractDepthOffset: 0);
                symbol = future.Symbol;
            }

            // Retrieve historical SymbolChangedEvent data
            var history = _algorithm.History<SymbolChangedEvent>(symbol, TimeSpan.FromDays(365)).ToList();

            // Ensure the history contains symbol change events
            Assert.IsNotEmpty(history);
            Assert.AreEqual(4, history.Count);

            // Verify each event has valid old and new symbols, and they are different
            foreach (var symbolChangedEvent in history)
            {
                Assert.IsNotNull(symbolChangedEvent.OldSymbol);
                Assert.IsNotNull(symbolChangedEvent.NewSymbol);
                Assert.AreNotEqual(symbolChangedEvent.OldSymbol, symbolChangedEvent.NewSymbol);
            }
        }

        [Test]
        public void OpenInterestHistoryOnlyContainsDataDuringRegularTradingHours()
        {
            var start = new DateTime(2013, 12, 01);
            _algorithm = GetAlgorithm(start);
            _algorithm.SetEndDate(2013, 12, 31);

            // Add ES (E-mini S&P 500)
            var future = _algorithm.AddFuture("ES", Resolution.Daily, Market.CME);

            var history = _algorithm.History<OpenInterest>(future.Symbol, new DateTime(2013, 10, 10), new DateTime(2013, 11, 01), Resolution.Daily).ToList();

            /* Expected 16 trading days breakdown:
                October 2013:
                10(Thu), 11(Fri),
                14(Mon), 15(Tue), 16(Wed), 17(Thu), 18(Fri),
                21(Mon), 22(Tue), 23(Wed), 24(Thu), 25(Fri),
                28(Mon), 29(Tue), 30(Wed), 31(Thu)
            */
            Assert.AreEqual(16, history.Count);

            // Regular trading hours: Monday-Friday 9:30am-5:00pm ET
            foreach (var data in history)
            {
                var date = data.EndTime;
                var dayOfWeek = date.DayOfWeek;
                Assert.AreNotEqual(DayOfWeek.Saturday, dayOfWeek);
                Assert.AreNotEqual(DayOfWeek.Sunday, dayOfWeek);
            }
        }

        public class CustomUniverseData : BaseDataCollection
        {
            public decimal Weight { get; private set; }

            public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
            {
                return new SubscriptionDataSource("TestData/portfolio_targets.csv",
                    SubscriptionTransportMedium.LocalFile,
                    FileFormat.FoldingCollection);
            }

            public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
            {
                var csv = line.Split(',');

                try
                {
                    var endTime = DateTime.ParseExact(csv[0], "yyyy-MM-dd", CultureInfo.InvariantCulture);
                    var symbol = Symbol.Create(csv[1], SecurityType.Equity, Market.USA);
                    var weight = Convert.ToDecimal(csv[2], CultureInfo.InvariantCulture);

                    return new CustomUniverseData
                    {
                        Symbol = symbol,
                        Time = endTime - TimeSpan.FromDays(1),
                        EndTime = endTime,
                        Weight = weight
                    };
                }
                catch
                {
                    return null;
                }
            }
        }

        private static void AssertCustomUniverseDataFrames<T>(dynamic df, dynamic flattenedDf, Func<T, decimal> getWeight)
            where T : BaseData
        {
            var expectedDates = new List<DateTime>
                {
                    new DateTime(2015, 01, 13),
                    new DateTime(2015, 01, 14),
                    new DateTime(2015, 01, 15),
                };

            var flattenedDfDates = ((List<DateTime>)flattenedDf.index.get_level_values(0).to_list().As<List<DateTime>>()).Distinct().ToList();
            CollectionAssert.AreEqual(expectedDates, flattenedDfDates);

            var dfDates = ((List<DateTime>)df.index.get_level_values(1).to_list().As<List<DateTime>>()).Distinct().ToList();
            CollectionAssert.AreEqual(expectedDates, dfDates);

            df = df.droplevel(0); // drop symbol just to make access easier
            foreach (var date in expectedDates)
            {
                using var pyDate = date.ToPython();
                var constituents = (List<T>)df.loc[pyDate].As<List<T>>();
                var flattendDfConstituents = flattenedDf.loc[pyDate];

                CollectionAssert.IsNotEmpty(constituents);
                Assert.AreEqual(flattendDfConstituents.shape[0].As<int>(), constituents.Count);

                var constituentsSymbols = constituents.Select(x => x.Symbol).ToList();
                var flattendDfConstituentsSymbols = ((List<Symbol>)flattendDfConstituents.index.to_list().As<List<Symbol>>()).ToList();
                CollectionAssert.AreEqual(flattendDfConstituentsSymbols, constituentsSymbols);

                var constituentsWeights = constituents.Select(x => getWeight(x)).ToList();
                var flattendDfConstituentsWeights = constituentsSymbols
                    .Select(symbol => flattendDfConstituents.loc[symbol.ToPython()]["weight"].As<decimal>())
                    .Cast<decimal>()
                    .ToList();
                CollectionAssert.AreEqual(flattendDfConstituentsWeights, constituentsWeights);
            }

            Log.Debug((string)df.to_string());
            Log.Debug((string)flattenedDf.to_string());
        }

        private static void AssertDesNotThrowPythonException(Action action)
        {
            try
            {
                action();
            }
            catch (PythonException ex)
            {
                Assert.Fail(ex.Message);
            }
        }

        private class ThrowingHistoryProvider : HistoryProviderBase
        {
            public override int DataPointCount => 0;

            public override IEnumerable<Slice> GetHistory(IEnumerable<HistoryRequest> requests, DateTimeZone sliceTimeZone)
            {
                throw new Exception("Expected exception");
                yield return null;
            }

            public override void Initialize(HistoryProviderInitializeParameters parameters)
            {
            }
        }

        private static TestCaseData[] GetHistoryWithDataNormalizationModeTestCases()
        {
            var equityStart = new DateTime(2014, 06, 05); // There is an AAPL split on 2014/06/06
            var futureStart = new DateTime(2013, 12, 18); // There is an ES contract mapping on 2013/12/19

            return new[] { Language.CSharp, Language.Python }.SelectMany(language =>
            {
                return new[]
                {
                    // Test cases for equity
                    new TestCaseData(language, Symbols.AAPL, Resolution.Daily, equityStart, equityStart.AddDays(100), 70),
                    new TestCaseData(language, Symbols.AAPL, Resolution.Hour, equityStart, equityStart.AddDays(20), 98),
                    new TestCaseData(language, Symbols.AAPL, Resolution.Minute, equityStart, equityStart.AddDays(1), 390),

                    // Test cases for futures
                    new TestCaseData(language, Symbols.ES_Future_Chain, Resolution.Daily, futureStart, futureStart.AddDays(100), 70),
                    new TestCaseData(language, Symbols.ES_Future_Chain, Resolution.Hour, futureStart, futureStart.AddDays(20), 93),
                    new TestCaseData(language, Symbols.ES_Future_Chain, Resolution.Minute, futureStart, futureStart.AddDays(2), 900),
                };
            }).ToArray();
        }

        private QCAlgorithm GetAlgorithm(DateTime dateTime)
        {
            var algorithm = new QCAlgorithm();

            // Initialize the object store for the algorithm
            using var store = new LocalObjectStore();
            store.Initialize(0, 0, "", new Controls { StoragePermissions = FileAccess.ReadWrite });
            algorithm.SetObjectStore(store);

            algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(algorithm));
            algorithm.HistoryProvider = new SubscriptionDataReaderHistoryProvider();
            algorithm.SetDateTime(dateTime.ConvertToUtc(algorithm.TimeZone));

            algorithm.HistoryProvider.Initialize(new HistoryProviderInitializeParameters(
                null,
                null,
                _dataProvider,
                _cacheProvider,
                _mapFileProvider,
                _factorFileProvider,
                null,
                false,
                new DataPermissionManager(),
                algorithm.ObjectStore,
                algorithm.Settings));
            return algorithm;
        }

        private class TestHistoryProvider : HistoryProviderBase
        {
            public override int DataPointCount { get; }
            public List<HistoryRequest> HistryRequests { get; } = new List<HistoryRequest>();

            public List<Slice> Slices { get; set; } = new List<Slice>();

            public override void Initialize(HistoryProviderInitializeParameters parameters)
            {
                throw new NotImplementedException();
            }

            public override IEnumerable<Slice> GetHistory(IEnumerable<HistoryRequest> requests, DateTimeZone sliceTimeZone)
            {
                foreach (var request in requests)
                {
                    HistryRequests.Add(request);
                }

                if (!requests.Any()) return Enumerable.Empty<Slice>().ToList();

                var startTime = requests.Min(x => x.StartTimeUtc.ConvertFromUtc(x.DataTimeZone));
                var endTime = requests.Max(x => x.EndTimeUtc.ConvertFromUtc(x.DataTimeZone));

                return Slices.Where(x => x.Time >= startTime && x.Time <= endTime).ToList();
            }
        }

        public class CustomData : TradeBar
        {
            public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
            {
                var source = Path.Combine(Globals.DataFolder, "equity", "usa", config.Resolution.ToString().ToLower(),
                    Symbols.SPY.Value.ToLowerInvariant(), LeanData.GenerateZipFileName(Symbols.SPY, date, config.Resolution, config.TickType));
                return new SubscriptionDataSource(source, SubscriptionTransportMedium.LocalFile, FileFormat.Csv);
            }
            public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
            {
                var baseData = base.Reader(new SubscriptionDataConfig(config, symbol: Symbols.SPY), line, date, isLiveMode);

                return new CustomData
                {
                    DataType = MarketDataType.Base,
                    Symbol = config.Symbol,
                    Time = baseData.EndTime,
                    Value = baseData.Price
                };
            }
        }

        /// <summary>
        /// Represents custom data with an optional sorting functionality. The <see cref="ExampleCustomDataWithSort"/> class
        /// allows you to specify a static property <seealso cref="CustomDataKey"/>, which defines the name of the custom data source.
        /// Sorting can be enabled or disabled by setting the <seealso cref="Sort"/> property.
        /// This class overrides <see cref="GetSource(SubscriptionDataConfig, DateTime, bool)"/> to initialize the
        /// <seealso cref="SubscriptionDataSource.Sort"/> property based on the value of <see cref="Sort"/>.
        /// </summary>
        public class ExampleCustomDataWithSort : BaseData
        {
            /// <summary>
            /// The name of the custom data source.
            /// </summary>
            public static string CustomDataKey { get; set; }

            /// <summary>
            /// Specifies whether the data should be sorted. If set to true, the data will be sorted during retrieval.
            /// </summary>
            public static bool Sort { get; set; } = true;

            public decimal Open { get; set; }
            public decimal High { get; set; }
            public decimal Low { get; set; }
            public decimal Close { get; set; }

            /// <summary>
            /// Returns the data source for the subscription. It uses the custom data key and sets sorting based on the
            /// <see cref="Sort"/> property.
            /// </summary>
            /// <param name="config">Subscription configuration.</param>
            /// <param name="date">The data date.</param>
            /// <param name="isLiveMode">Specifies whether live mode is enabled.</param>
            /// <returns>The subscription data source with sorting determined by the <see cref="Sort"/> property.</returns>
            public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
            {
                return new SubscriptionDataSource(CustomDataKey, SubscriptionTransportMedium.ObjectStore, FileFormat.Csv)
                {
                    Sort = Sort
                };
            }

            /// <summary>
            /// Reads a line of CSV data and parses it into an <see cref="ExampleCustomDataWithSort"/> object.
            /// </summary>
            /// <param name="config">Subscription configuration.</param>
            /// <param name="line">The line of data to parse.</param>
            /// <param name="date">The data date.</param>
            /// <param name="isLiveMode">Specifies whether live mode is enabled.</param>
            /// <returns>A populated <see cref="ExampleCustomDataWithSort"/> instance.</returns>
            public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
            {
                var csv = line.Split(",");
                var data = new ExampleCustomDataWithSort
                {
                    Symbol = config.Symbol,
                    Time = DateTime.ParseExact(csv[0], DateFormat.DB, CultureInfo.InvariantCulture),
                    Value = csv[4].ToDecimal(),
                    Open = csv[1].ToDecimal(),
                    High = csv[2].ToDecimal(),
                    Low = csv[3].ToDecimal(),
                    Close = csv[4].ToDecimal()
                };

                return data;
            }
        }

        private QCAlgorithm GetAlgorithmWithEquity(DateTime dateTime)
        {
            var resolution = Resolution.Minute;
            var algorithm = GetAlgorithm(dateTime);
            algorithm.AddEquity("AAPL", resolution);

            return algorithm;
        }

        private QCAlgorithm GetAlgorithmWithFuture(DateTime dateTime)
        {
            var resolution = Resolution.Daily;
            var algorithm = GetAlgorithm(dateTime);
            algorithm.AddFuture(Futures.Indices.SP500EMini, resolution, extendedMarketHours: true);

            return algorithm;
        }

        /// <summary>
        /// Helper method to check that all history results have the same bar count
        /// </summary>
        private static void CheckThatHistoryResultsHaveEqualBarCount<T>(IEnumerable<IEnumerable<T>> historyResults, int expectedHistoryCount)
        {
            Assert.That(historyResults, Has.All.Not.Empty.And.All.Count.EqualTo(expectedHistoryCount),
                $@"Expected all history results to have {expectedHistoryCount} slices, but counts where {string.Join(", ", historyResults.Select(x => x.Count()))}");
        }

        /// <summary>
        /// Helper method to check that all history data frame results have the same bar count
        /// </summary>
        private static void CheckThatHistoryResultsHaveEqualBarCount(List<PyObject> historyResults, int expectedHistoryCount)
        {
            Assert.Greater(expectedHistoryCount, 0);
            var historyCounts = historyResults.Select(x => x.GetAttr("shape")[0].As<int>()).ToList();
            Assert.IsTrue(historyCounts.All(count => count == expectedHistoryCount),
                $"Expected all history results to have {expectedHistoryCount} slices/bars, but counts where {string.Join(", ", historyCounts)}");
        }

        /// <summary>
        /// Helper method to check that, for each history result, prices at each time are different
        /// </summary>
        private static void CheckThatHistoryResultsHaveDifferentPrices(List<List<BaseData>> historyResults, string message)
        {
            for (int i = 0; i < historyResults[0].Count; i++)
            {
                var prices = historyResults.Select(hr => hr[i].Price).ToHashSet();
                Assert.AreEqual(historyResults.Count, prices.Count, message);
            }
        }

        /// <summary>
        /// Helper method to check that, for each history result, prices at each time are different
        /// </summary>
        private static void CheckThatHistoryResultsHaveDifferentPrices(IEnumerable<IEnumerable<Slice>> historyResults, string message)
        {
            CheckThatHistoryResultsHaveDifferentPrices(historyResults.Select(hr => hr.Select(x => x.Values.First()).ToList()).ToList(), message);
        }

        /// <summary>
        /// Helper method to check that, for each history data frame result, prices at each time are different
        /// </summary>
        private static void CheckThatHistoryResultsHaveDifferentPrices(List<PyObject> historyResults, string message)
        {
            var closesPerResult = historyResults.Select(hr => hr["close"].GetAttr("values").GetAttr("tolist").Invoke().As<List<decimal>>()).ToList();

            for (int i = 0; i < closesPerResult.First().Count; i++)
            {
                var closePrices = closesPerResult.Select(close => close[i]).ToHashSet();
                Assert.AreEqual(historyResults.Count, closePrices.Count, message);
            }
        }

        /// <summary>
        /// Helper method to perform history checks on different data normalization modes
        /// </summary>
        private static void CheckHistoryResultsForDataNormalizationModes(QCAlgorithm algorithm, Symbol symbol, DateTime start,
            DateTime end, Resolution resolution, DataNormalizationMode[] dataNormalizationModes, int expectedHistoryCount)
        {
            var historyResults = dataNormalizationModes
                .Select(x => algorithm.History(new[] { symbol }, start, end, resolution, dataNormalizationMode: x).ToList())
                .ToList();

            CheckThatHistoryResultsHaveEqualBarCount(historyResults, expectedHistoryCount);
            CheckThatHistoryResultsHaveDifferentPrices(historyResults,
                "History results prices should have been different for each data normalization mode at each time");
        }

        /// <summary>
        /// Helper method to perform history checks on different data normalization modes
        /// </summary>
        private static void CheckHistoryResultsForDataNormalizationModes(QCAlgorithm algorithm, PyObject symbol, DateTime start,
            DateTime end, Resolution resolution, DataNormalizationMode[] dataNormalizationModes, int expectedHistoryCount)
        {
            var historyResults = dataNormalizationModes
                .Select(x => algorithm.History(symbol, start, end, resolution, dataNormalizationMode: x))
                .ToList();

            CheckThatHistoryResultsHaveEqualBarCount(historyResults, expectedHistoryCount);
            CheckThatHistoryResultsHaveDifferentPrices(historyResults,
                "History results prices should have been different for each data normalization mode at each time");
        }

        /// <summary>
        /// Helper method to assert that the right custom data history is fetched
        /// </summary>
        private static void AssertCustomDataTypeHistory<T>(List<T> history)
            where T : IBaseData
        {
            Assert.AreEqual(1539, history.Count);
            Assert.That(history, Has.All.Property("DataType").EqualTo(MarketDataType.Base));
        }

        /// <summary>
        /// Helper method to assert that the right custom data history is fetched
        /// </summary>
        private static void AssertCustomDataTypeHistory<T>(List<DataDictionary<T>> history)
            where T : IBaseData
        {
            Assert.AreEqual(1539, history.Count);
            Assert.That(history.Select(x => x.Single().Value), Has.All.Property("DataType").EqualTo(MarketDataType.Base));
        }

        private static DataMappingMode[] GetAllDataMappingModes()
        {
            return (DataMappingMode[])Enum.GetValues(typeof(DataMappingMode));
        }

        private static DataNormalizationMode[] GetAllDataNormalizationModes()
        {
            return (DataNormalizationMode[])Enum.GetValues(typeof(DataNormalizationMode));
        }

        /// <summary>
        /// Asserts history result has the expected count
        /// </summary>
        private static void AssertHistoryResultCount<T>(List<T> history, int expectedCount)
        {
            Assert.IsNotEmpty(history);
            Assert.AreEqual(expectedCount, history.Count);
        }

        /// <summary>
        /// Asserts that history result has more data when called with fillForward set to true.
        /// Used in the test <see cref="HistoryRequestWithFillForward"/> for Python cases.
        /// </summary>
        private static void AssertHistoryResultCount(PyObject history, int expectedCount)
        {
            var historyCount = history.GetAttr("shape")[0].As<int>();
            Assert.Greater(historyCount, 0);
            Assert.AreEqual(expectedCount, historyCount);
        }

        /// <summary>
        /// Asserts that data from a history request has the expected resolution
        /// </summary>
        private static void AssertHistoryResultResolution(IEnumerable<BaseData> history, Resolution resolution)
        {
            var mhdb = MarketHoursDatabase.FromDataFolder();
            var expectedTimeSpan = resolution.ToTimeSpan();
            Assert.IsTrue(history.All(data =>
            {
                if (resolution == Resolution.Daily)
                {
                    var exchange = mhdb.GetExchangeHours(data.Symbol.ID.Market, data.Symbol, data.Symbol.SecurityType);
                    var marketHours = exchange.GetMarketHours(data.EndTime);
                    expectedTimeSpan = marketHours.MarketDuration;
                }
                return data.EndTime - data.Time == expectedTimeSpan;
            }));
        }

        private static List<PyObject> GetHistoryDataFrameIndex(PyObject history)
        {
            return history
                .GetAttr("index")
                .GetAttr("to_flat_index").Invoke()
                .GetAttr("tolist").Invoke()
                .As<List<PyObject>>();
        }

        private static List<Symbol> GetSymbolsFromHistoryDataFrameIndex(List<PyObject> index)
        {
            dynamic builtins = Py.Import("builtins");
            return index.Select(x => x[builtins.len(x) > 2 ? 1 : 0].As<Symbol>()).ToList();
        }

        private static List<DateTime> GetTimesFromHistoryDataFrameIndex(List<PyObject> index)
        {
            dynamic builtins = Py.Import("builtins");
            return index.Select(x => x[builtins.len(x) > 2 ? 2 : 1].As<DateTime>()).ToList();
        }

        private static Dictionary<string, T> GetDataFrameDictionary<T>(PyObject dict)
        {
            // Using PyObject because our data frames use our PandasColunm class to wrap strings
            return dict.ConvertToDictionary<PyObject, T>().ToDictionary(
                kvp =>
                {
                    var strKey = kvp.Key.ToString();
                    kvp.Key.Dispose();
                    return strKey;
                },
                kvp => kvp.Value);
        }

        #region Fill-forwarded history assertions

        /// <summary>
        /// Asserts that fill forwarded history results has data for every period in the requested time span
        /// </summary>
        private static void AssertFillForwardedHistoryTimes(Symbol symbol, List<DateTime> times, TimeSpan period)
        {
            var hours = MarketHoursDatabase.FromDataFolder().GetEntry(symbol.ID.Market, symbol, symbol.ID.SecurityType).ExchangeHours;

            // We are assuming one regular segment per day for the test security
            var periodMultiplier = 0;
            var baseSegmentTimeIndex = 0;
            for (var i = 0; i < times.Count; i++)
            {
                var currentTime = times[i];
                if (i > 0 && currentTime.DayOfWeek != times[i - 1].DayOfWeek)
                {
                    baseSegmentTimeIndex = i;
                    periodMultiplier = 0;
                }

                var expectedCurrentTime = times[baseSegmentTimeIndex] + periodMultiplier++ * period;
                Assert.AreEqual(expectedCurrentTime, currentTime);
                Assert.IsTrue(
                    // subtract `period` since the times list has the EndTime
                    hours.IsOpen(currentTime - period, extendedMarketHours: false),
                    $"Current time {currentTime} is not open");
            }
        }

        /// <summary>
        /// Asserts that history data, when called with fillForward set to true, has a period that is equal to the resolution used.
        /// Used in the test <see cref="HistoryRequestWithFillForward"/>.
        /// </summary>
        private static void AssertFillForwardHistoryData(List<TradeBar> history, Resolution resolution, bool fillForward)
        {
            var expectedPeriod = resolution.ToTimeSpan();
            Assert.IsTrue(history.All(bar => bar.Period == expectedPeriod));

            if (fillForward)
            {
                var symbol = history.First().Symbol;
                var times = history.Select(bar => bar.EndTime).ToList();
                AssertFillForwardedHistoryTimes(symbol, times, expectedPeriod);
            }
        }

        /// <summary>
        /// Asserts that history data, when called with fillForward set to true, has a period that is equal to the resolution used.
        /// Used in the test <see cref="HistoryRequestWithFillForward"/>.
        /// </summary>
        private static void AssertFillForwardHistoryData(List<Slice> history, Resolution resolution, bool fillForward)
        {
            AssertFillForwardHistoryData(
                history.Select(slice => slice.Bars.Values.SingleOrDefault((TradeBar)null)).Where(bar => bar != null).ToList(),
                resolution,
                fillForward);
        }

        ///// <summary>
        /// Asserts that history data, when called with fillForward set to true, has a period that is equal to the resolution used.
        /// Used in the test <see cref="HistoryRequestWithFillForward"/>.
        /// </summary>
        private static void AssertFillForwardHistoryData(List<DataDictionary<TradeBar>> history, Resolution resolution, bool fillForward)
        {
            AssertFillForwardHistoryData(
                history.Select(x => x.Values.SingleOrDefault((TradeBar)null)).Where(bar => bar != null).ToList(),
                resolution,
                fillForward);
        }

        /// <summary>
        /// Asserts that history result has more data when called with fillForward set to true.
        /// Used in the test <see cref="HistoryRequestWithFillForward"/>.
        /// </summary>
        private static void AssertFillForwardHistoryResults(List<TradeBar> history, int expectedCount, Resolution resolution, bool fillForward)
        {
            AssertHistoryResultCount(history, expectedCount);
            AssertFillForwardHistoryData(history, resolution, fillForward);
        }

        /// <summary>
        /// Asserts that history result has more data when called with fillForward set to true.
        /// Used in the test <see cref="HistoryRequestWithFillForward"/>.
        /// </summary>
        private static void AssertFillForwardHistoryResults(List<Slice> history, int expectedCount, Resolution resolution, bool fillForward)
        {
            AssertHistoryResultCount(history, expectedCount);
            AssertFillForwardHistoryData(history, resolution, fillForward);
        }

        /// <summary>
        /// Asserts that history result has more data when called with fillForward set to true.
        /// Used in the test <see cref="HistoryRequestWithFillForward"/>.
        /// </summary>
        private static void AssertFillForwardHistoryResults(List<DataDictionary<TradeBar>> history, int expectedCount,
            Resolution resolution, bool fillForward)
        {
            AssertHistoryResultCount(history, expectedCount);
            AssertFillForwardHistoryData(history, resolution, fillForward);
        }

        /// <summary>
        /// Asserts that history result has more data when called with fillForward set to true.
        /// Used in the test <see cref="HistoryRequestWithFillForward"/> for Python cases.
        /// </summary>
        private static void AssertFillForwardHistoryResults(PyObject history, int expectedCount, Resolution resolution, bool fillForward)
        {
            AssertHistoryResultCount(history, expectedCount);

            if (fillForward)
            {
                var index = GetHistoryDataFrameIndex(history);
                var symbols = GetSymbolsFromHistoryDataFrameIndex(index);
                var times = GetTimesFromHistoryDataFrameIndex(index);
                AssertFillForwardedHistoryTimes(symbols[0], times, resolution.ToTimeSpan());
            }
        }

        #endregion

        #region History with extended market assertions

        /// <summary>
        /// Asserts that history with/without extended market results has data for regular hour segments and extended market segments, respectively.
        /// </summary>
        private static void AssertExtendedMarketHistoryTimes(Symbol symbol, List<DateTime> times, bool extendedMarket)
        {
            var hours = MarketHoursDatabase.FromDataFolder().GetEntry(symbol.ID.Market, symbol, symbol.ID.SecurityType).ExchangeHours;

            var getTimesCheck = (bool inExtendedHours) => (DateTime time) =>
            {
                var currentDayHours = hours.GetMarketHours(time);
                var segments = currentDayHours.Segments
                    .Where(x => inExtendedHours
                        ? x.State != MarketHoursState.Market
                        : x.State == MarketHoursState.Market);
                return segments.Any(segment => time.TimeOfDay > segment.Start && time.TimeOfDay <= segment.End);
            };
            var timeIsInRegularHours = getTimesCheck(false);
            var timeIsInExtendedHours = getTimesCheck(true);

            if (!extendedMarket)
            {
                Assert.IsTrue(times.All(timeIsInRegularHours));
            }
            else
            {
                Assert.IsTrue(times.Any(timeIsInRegularHours));
                Assert.IsTrue(times.Any(timeIsInExtendedHours));
            }
        }

        /// <summary>
        /// Asserts that history with/without extended market results has data for regular hour segments and extended market segments, respectively.
        /// </summary>
        private static void AssertExtendedMarketHistoryResultsData(List<BaseData> history, bool extendedMarket, Resolution resolution,
            Symbol expectedSymbol)
        {
            Assert.IsTrue(history.All(data => data.Symbol == expectedSymbol));
            var times = history.Select(bar => bar.EndTime).ToList();
            AssertExtendedMarketHistoryTimes(expectedSymbol, times, extendedMarket);
            AssertHistoryResultResolution(history, resolution);
        }

        /// <summary>
        /// Asserts that history with/without extended market results has data for regular hour segments and extended market segments, respectively.
        /// </summary>
        private static void AssertExtendedMarketHistoryResultsData(List<Slice> history, bool extendedMarket, Resolution resolution,
            Symbol expectedSymbol)
        {
            var data = history.Select(slice => slice.Bars.Values.Cast<BaseData>().Concat(slice.QuoteBars.Values.Cast<BaseData>())).SelectMany(x => x);
            Assert.IsTrue(data.All(x => x.Symbol == expectedSymbol));

            var times = data.Select(x => x.EndTime).ToList();
            AssertExtendedMarketHistoryTimes(expectedSymbol, times, extendedMarket);
            AssertHistoryResultResolution(data, resolution);
        }

        /// <summary>
        /// Asserts that history result has more data when called with extendedMarket set to true.
        /// Used in the test <see cref="HistoryRequestWithExtendedMarket"/>.
        /// </summary>
        private static void AssertExtendedMarketHistoryResults(List<TradeBar> history, int expectedHistoryCount, bool extendedMarket,
            Resolution resolution, Symbol expectedSymbol)
        {
            AssertHistoryResultCount(history, expectedHistoryCount);
            AssertExtendedMarketHistoryResultsData(history.Cast<BaseData>().ToList(), extendedMarket, resolution, expectedSymbol);
        }

        /// <summary>
        /// Asserts that history result has more data when called with extendedMarket set to true.
        /// Used in the test <see cref="HistoryRequestWithExtendedMarket"/>.
        /// </summary>
        private static void AssertExtendedMarketHistoryResults(List<Slice> history, int expectedHistoryCount, bool extendedMarket,
            Resolution resolution, Symbol expectedSymbol)
        {
            AssertHistoryResultCount(history, expectedHistoryCount);
            AssertExtendedMarketHistoryResultsData(history, extendedMarket, resolution, expectedSymbol);
        }

        /// <summary>
        /// Asserts that history result has more data when called with extendedMarket set to true.
        /// Used in the test <see cref="HistoryRequestWithExtendedMarket"/>.
        /// </summary>
        private static void AssertExtendedMarketHistoryResults(List<DataDictionary<TradeBar>> history, int expectedHistoryCount, bool extendedMarket,
            Resolution resolution, Symbol expectedSymbol)
        {
            AssertHistoryResultCount(history, expectedHistoryCount);
            AssertExtendedMarketHistoryResultsData(history.Select(dict => dict.Values.First()).Cast<BaseData>().ToList(), extendedMarket, resolution,
                expectedSymbol);
        }

        /// <summary>
        /// Asserts that history result has more data when called with extendedMarket set to true.
        /// Used in the test <see cref="HistoryRequestWithExtendedMarket"/>.
        /// </summary>
        private static void AssertExtendedMarketHistoryResults(List<Tick> history, int expectedHistoryCount, bool extendedMarket,
            Resolution resolution, Symbol expectedSymbol)
        {
            AssertHistoryResultCount(history, expectedHistoryCount);
            AssertExtendedMarketHistoryResultsData(history.Cast<BaseData>().ToList(), extendedMarket, resolution, expectedSymbol);
        }

        /// <summary>
        /// Asserts that history result has more data when called with extendedMarket set to true.
        /// Used in the test <see cref="HistoryRequestWithExtendedMarket"/>.
        /// </summary>
        private static void AssertExtendedMarketHistoryResults(List<DataDictionary<Tick>> history, int expectedHistoryCount, bool extendedMarket,
            Resolution resolution, Symbol expectedSymbol)
        {
            AssertExtendedMarketHistoryResults(history.SelectMany(dict => dict.Values).ToList(), expectedHistoryCount, extendedMarket, resolution,
                expectedSymbol);
        }

        /// <summary>
        /// Asserts that history result has more data when called with extendedMarket set to true.
        /// Used in the test <see cref="HistoryRequestWithExtendedMarket"/> for Python cases.
        /// </summary>
        private static void AssertExtendedMarketHistoryResults(PyObject history, int expectedHistoryCount, bool extendedMarket,
            Resolution resolution, Symbol expectedSymbol)
        {
            AssertHistoryResultCount(history, expectedHistoryCount);

            var index = GetHistoryDataFrameIndex(history);
            var symbols = GetSymbolsFromHistoryDataFrameIndex(index);
            Assert.IsTrue(symbols.All(x => x == expectedSymbol));
            var times = GetTimesFromHistoryDataFrameIndex(index);
            AssertExtendedMarketHistoryTimes(expectedSymbol, times, extendedMarket);
        }

        #endregion

        #region History with different data mapping modes assertions

        /// <summary>
        /// Asserts that for a list of history results, one for a different data mapping mode, each has its expected mapping date, that is,
        /// the date when underlying symbol change due to contract expiration.
        /// </summary>
        private static void AssertFuturesHistoryWithDifferentMappingModesResults(List<List<TradeBar>> historyResults, Symbol expectedSymbol,
            int expectedHistoryCount, Resolution resolution, List<DateTime> expectedMappingDates)
        {
            CheckThatHistoryResultsHaveEqualBarCount(historyResults, expectedHistoryCount);

            // Check that all history results have a mapping date at some point in the history
            for (var i = 0; i < historyResults.Count; i++)
            {
                var history = historyResults[i];
                var prevUnderlying = history[0].Symbol.Underlying;
                var mappingDates = new List<DateTime>();

                foreach (var bar in history)
                {
                    Assert.AreEqual(expectedSymbol, bar.Symbol, $"All bars symbol must have been {expectedSymbol} but found {bar.Symbol}");

                    var currentUnderlying = bar.Symbol.Underlying;
                    if (currentUnderlying != prevUnderlying)
                    {
                        mappingDates.Add(bar.EndTime.Date);
                        prevUnderlying = currentUnderlying;
                    }
                }

                Assert.AreEqual(1, mappingDates.Count, "We are expecting only one mapping for this case.");
                var expectedMappingDate = expectedMappingDates[i];
                Assert.AreEqual(expectedMappingDate, mappingDates[0],
                    $"Mapping date {mappingDates[0]} for {i}th history result is not the expected one {expectedMappingDate}.");

                AssertHistoryResultResolution(history, resolution);
            }

            CheckThatHistoryResultsHaveDifferentPrices(historyResults.Select(history => history.Cast<BaseData>().ToList()).ToList(),
                "History results prices should have been different for each data mapping mode at each time");
        }

        /// <summary>
        /// Asserts that for a list of history results, one for a different data mapping mode, each has its expected mapping date.
        /// </summary>
        private static void AssertFuturesHistoryWithDifferentMappingModesResults(List<List<Slice>> historyResults, Symbol expectedSymbol,
            int expectedHistoryCount, Resolution resolution, List<DateTime> expectedMappingDates)
        {
            AssertFuturesHistoryWithDifferentMappingModesResults(historyResults.Select(x => x.Select(y => y.Bars.Values.First()).ToList()).ToList(),
                expectedSymbol, expectedHistoryCount, resolution, expectedMappingDates);
        }

        /// <summary>
        /// Asserts that for a list of history results, one for a different data mapping mode, each has its expected mapping date.
        /// </summary>
        private static void AssertFuturesHistoryWithDifferentMappingModesResults(List<List<DataDictionary<TradeBar>>> historyResults,
            Symbol expectedSymbol, int expectedHistoryCount, Resolution resolution, List<DateTime> expectedMappingDates)
        {
            AssertFuturesHistoryWithDifferentMappingModesResults(historyResults.Select(x => x.Select(y => y[expectedSymbol]).ToList()).ToList(),
                expectedSymbol, expectedHistoryCount, resolution, expectedMappingDates);
        }

        /// <summary>
        /// Asserts that for a list of Python history results, one for a different data mapping mode,
        /// the result counts are the same and contain different prices.
        /// In the data frames we don't have access to the actual mapping dates, so we cannot do the same checks we do for C# in
        /// <see cref="AssertFuturesHistoryWithDifferentMappingModesResults(List{List{TradeBar}}, Symbol, int, Resolution, List{DateTime})"/>.
        /// </summary>
        private static void AssertFuturesHistoryWithDifferentMappingModesResults(List<PyObject> historyResults, Symbol expectedSymbol,
            int expectedHistoryCount)
        {
            CheckThatHistoryResultsHaveEqualBarCount(historyResults, expectedHistoryCount);
            CheckThatHistoryResultsHaveDifferentPrices(historyResults,
                "History results prices should have been different for each data mapping mode at each time");

            foreach (var history in historyResults)
            {
                var index = GetHistoryDataFrameIndex(history);
                var symbols = GetSymbolsFromHistoryDataFrameIndex(index);
                Assert.IsTrue(symbols.All(x => x == expectedSymbol));
            }
        }

        #endregion

        #region History with different data normalization modes assertions

        /// <summary>
        /// Asserts that for a list of history results, one for a different data normalization mode, prices are different for each time.
        /// </summary>
        private static void AssertHistoryWithDifferentNormalizationModesResults(List<List<TradeBar>> historyResults, Symbol expectedSymbol,
            int expectedHistoryCount, Resolution resolution)
        {
            CheckThatHistoryResultsHaveEqualBarCount(historyResults, expectedHistoryCount);
            CheckThatHistoryResultsHaveDifferentPrices(historyResults.Select(history => history.Cast<BaseData>().ToList()).ToList(),
                "History results prices should have been different for each data normalization mode at each time");

            // Assert resolution and symbol
            foreach (var history in historyResults)
            {
                AssertHistoryResultResolution(history, resolution);
                foreach (var bar in history)
                {
                    Assert.AreEqual(expectedSymbol, bar.Symbol, $"All bars symbol must have been {expectedSymbol} but found {bar.Symbol}");
                }
            }
        }

        /// <summary>
        /// Asserts that for a list of history results, one for a different data normalization mode, prices are different for each time.
        /// </summary>
        private static void AssertHistoryWithDifferentNormalizationModesResults(List<List<Slice>> historyResults, Symbol expectedSymbol,
            int expectedHistoryCount, Resolution resolution)
        {
            AssertHistoryWithDifferentNormalizationModesResults(historyResults.Select(x => x.Select(y => y.Bars.Values.First()).ToList()).ToList(),
                expectedSymbol, expectedHistoryCount, resolution);
        }

        /// <summary>
        /// Asserts that for a list of history results, one for a different data normalization mode, prices are different for each time.
        /// </summary>
        private static void AssertHistoryWithDifferentNormalizationModesResults(List<List<DataDictionary<TradeBar>>> historyResults,
            Symbol expectedSymbol, int expectedHistoryCount, Resolution resolution)
        {
            AssertHistoryWithDifferentNormalizationModesResults(historyResults.Select(x => x.Select(y => y[expectedSymbol]).ToList()).ToList(),
                expectedSymbol, expectedHistoryCount, resolution);
        }

        /// <summary>
        /// Asserts that for a list of history results, one for a different data normalization mode, prices are different for each time.
        /// </summary>
        private static void AssertHistoryWithDifferentNormalizationModesResults(List<PyObject> historyResults, Symbol expectedSymbol,
            int expectedHistoryCount)
        {
            // These are the same checks done for mapping modes, even regardless of the security type
            AssertFuturesHistoryWithDifferentMappingModesResults(historyResults, expectedSymbol, expectedHistoryCount);
        }

        #endregion

        #region History with different contract depth offsets assertions

        /// <summary>
        /// Asserts that history request with different contract depth offsets results have the expected mapped symbol depending on the offset used.
        /// </summary>
        private static void AssertFuturesHistoryWithDifferentContractDepthOffsetsResults(List<List<BaseData>> historyResults,
            Symbol expectedSymbol, int expectedHistoryCount, Resolution resolution)
        {
            CheckThatHistoryResultsHaveEqualBarCount(historyResults, expectedHistoryCount);

            var futureChainProvider = new BacktestingFutureChainProvider();
            futureChainProvider.Initialize(new(TestGlobals.MapFileProvider, TestGlobals.HistoryProvider));

            var firstDateTime = historyResults[0][0].EndTime;
            var futureChain = futureChainProvider.GetFutureContractList(expectedSymbol, firstDateTime).ToList();

            // Check that the first mapped contract corresponds to the one in the future chain depending on the depth offset used,
            // where historyResults[i] is the history result for contract depth offset = i
            for (var i = 0; i < historyResults.Count; i++)
            {
                var history = historyResults[i];
                var firstMappedContractSymbol = history[0].Symbol.Underlying;

                Assert.AreEqual(futureChain[i], firstMappedContractSymbol,
                    $@"History[{i}]: Expected the first mapped contract to be the one on index {i} ({futureChain[i]}) in the chain for date {firstDateTime}.");

                // Finally, assert the resolution and symbol
                AssertHistoryResultResolution(history, resolution);
                Assert.IsTrue(history.All(x => x.Symbol == expectedSymbol));
            }

            CheckThatHistoryResultsHaveDifferentPrices(historyResults.Select(history => history.Cast<BaseData>().ToList()).ToList(),
                "History results prices should have been different for each available offset at each time");
        }

        /// <summary>
        /// Asserts that history request with different contract depth offsets results have the expected mapped symbol depending on the offset used.
        /// </summary>
        private static void AssertFuturesHistoryWithDifferentContractDepthOffsetsResults(List<List<TradeBar>> historyResults,
            Symbol expectedSymbol, int expectedHistoryCount, Resolution resolution)
        {
            AssertFuturesHistoryWithDifferentContractDepthOffsetsResults(
                historyResults.Select(history => history.Cast<BaseData>().ToList()).ToList(),
                expectedSymbol, expectedHistoryCount, resolution);
        }

        /// <summary>
        /// Asserts that history request with different contract depth offsets results have the expected mapped symbol depending on the offset used.
        /// </summary>
        private static void AssertFuturesHistoryWithDifferentContractDepthOffsetsResults(List<List<Slice>> historyResults,
            Symbol expectedSymbol, int expectedHistoryCount, Resolution resolution)
        {
            AssertFuturesHistoryWithDifferentContractDepthOffsetsResults(
                historyResults.Select(x => x.Select(y => (BaseData)y.Bars.Values.FirstOrDefault() ?? y.QuoteBars.Values.First()).ToList()).ToList(),
                expectedSymbol, expectedHistoryCount, resolution);
        }

        /// <summary>
        /// Asserts that history request with different contract depth offsets results have the expected mapped symbol depending on the offset used.
        /// </summary>
        private static void AssertFuturesHistoryWithDifferentContractDepthOffsetsResults(List<List<DataDictionary<TradeBar>>> historyResults,
            Symbol expectedSymbol, int expectedHistoryCount, Resolution resolution)
        {
            AssertFuturesHistoryWithDifferentContractDepthOffsetsResults(
                historyResults.Select(x => x.Select(y => y[expectedSymbol]).ToList()).ToList(),
                expectedSymbol, expectedHistoryCount, resolution);
        }

        /// <summary>
        /// Asserts that history request with different contract depth offsets results have the expected mapped symbol depending on the offset used.
        /// In the data frames we don't have access to the underylings, so we cannot do the same checks we do for C# in
        /// <see cref="AssertFuturesHistoryWithDifferentContractDepthOffsetsResults(List{List{BaseData}}, Symbol, int, Resolution)"/>.
        /// </summary>
        private static void AssertFuturesHistoryWithDifferentContractDepthOffsetsResults(List<PyObject> historyResults,
            Symbol expectedSymbol, int expectedHistoryCount)
        {
            // These are the same checks done for mapping modes
            AssertFuturesHistoryWithDifferentMappingModesResults(historyResults, expectedSymbol, expectedHistoryCount);
        }

        #endregion
    }
}
