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
using QuantConnect.Algorithm;
using QuantConnect.Securities;
using QuantConnect.Interfaces;
using QuantConnect.Data.Market;
using System.Collections.Generic;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Tests.Engine.DataFeeds;
using QuantConnect.Data.Custom.AlphaStreams;
using QuantConnect.Lean.Engine.HistoricalData;
using HistoryRequest = QuantConnect.Data.HistoryRequest;
using QuantConnect.Orders.Fills;

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
            _cacheProvider = new ZipDataCacheProvider(TestGlobals.DataProvider);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _cacheProvider.DisposeSafely();
        }

        [TestCase(Resolution.Daily, Language.CSharp, 2)]
        [TestCase(Resolution.Hour, Language.CSharp, 14)]
        [TestCase(Resolution.Minute, Language.CSharp, 780)]
        [TestCase(Resolution.Second, Language.CSharp, 45676)]
        [TestCase(Resolution.Daily, Language.Python, 2)]
        [TestCase(Resolution.Hour, Language.Python, 14)]
        [TestCase(Resolution.Minute, Language.Python, 780)]
        [TestCase(Resolution.Second, Language.Python, 33948)]
        public void TickResolutionSubscriptionHistoryRequestOtherResolution(Resolution resolution, Language language, int expectedHistoryCount)
        {
            var start = new DateTime(2013, 10, 07);
            _algorithm = GetAlgorithm(start.AddDays(2));

            _algorithm.AddEquity(Symbols.SPY, Resolution.Tick);

            if (language == Language.CSharp)
            {
                // Trades and quotes
                var result = _algorithm.History(new [] { Symbols.SPY }, start, _algorithm.Time, resolution).ToList();

                Assert.AreEqual(expectedHistoryCount, result.Count);
                Assert.IsTrue(result.All(slice =>
                {
                    foreach (var bar in slice.Bars.Values)
                    {
                        return (bar.EndTime - bar.Time) == resolution.ToTimeSpan();
                    }
                    foreach (var bar in slice.QuoteBars.Values)
                    {
                        return (bar.EndTime - bar.Time) == resolution.ToTimeSpan();
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

        [TestCase(Language.CSharp)]
        [TestCase(Language.Python)]
        public void TickResolutionHistoryRequest(Language language)
        {
            var start = new DateTime(2013, 10, 07);
            _algorithm = GetAlgorithm(start.AddDays(1));

            if (language == Language.CSharp)
            {
                var result = _algorithm.History(new [] { Symbols.SPY }, start.AddHours(9.8), start.AddHours(10), Resolution.Tick).ToList();
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
                    var getTradesAndQuotesHistory = pythonModule.GetAttr("getTradesAndQuotesHistory");
                    var getTradesOnlyHistory = pythonModule.GetAttr("getTradesOnlyHistory");
                    _algorithm.SetPandasConverter();
                    var pySymbol = Symbols.SPY.ToPython();
                    var pyAlgorithm = _algorithm.ToPython();
                    var pyStart = start.ToPython();

                    var result = getTradesAndQuotesHistory.Invoke(pyAlgorithm, pySymbol, pyStart).ConvertToDictionary<string, dynamic>();
                    var result2 = getTradesOnlyHistory.Invoke(pyAlgorithm, pySymbol, pyStart).ConvertToDictionary<string, dynamic>();

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

        [TestCase(Language.CSharp)]
        [TestCase(Language.Python)]
        public void TickResolutionPeriodBasedHistoryRequestThrowsException(Language language)
        {
            var spy = _algorithm.AddEquity("SPY", Resolution.Tick).Symbol;

            if (language == Language.CSharp)
            {
                Assert.Throws<ArgumentException>(() => _algorithm.History<Tick>(spy, 1).ToList());
                Assert.Throws<ArgumentException>(() => _algorithm.History<Tick>(spy, 1, Resolution.Tick).ToList());
                Assert.Throws<ArgumentException>(() => _algorithm.History<Tick>(new [] { spy }, 1).ToList());
                Assert.Throws<ArgumentException>(() => _algorithm.History<Tick>(new [] { spy }, 1, Resolution.Tick).ToList());
            }
            else
            {
                using (Py.GIL())
                {
                    _algorithm.SetPandasConverter();
                    using var pyTickType = typeof(Tick).ToPython();
                    using var pySymbols = new PyList(new [] { spy.ToPython() });
                    Assert.Throws<ArgumentException>(() => _algorithm.History(pyTickType, spy, 1));
                    Assert.Throws<ArgumentException>(() => _algorithm.History(pyTickType, spy, 1, Resolution.Tick));
                    Assert.Throws<ArgumentException>(() => _algorithm.History(pyTickType, pySymbols, 1));
                    Assert.Throws<ArgumentException>(() => _algorithm.History(pyTickType, pySymbols, 1, Resolution.Tick));
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
                    using var pySymbols = new PyList(new [] { pySpy, pyIbm });
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

        [TestCase(Resolution.Second, Language.CSharp)]
        [TestCase(Resolution.Minute, Language.CSharp)]
        [TestCase(Resolution.Hour, Language.CSharp)]
        [TestCase(Resolution.Daily, Language.CSharp)]
        [TestCase(Resolution.Second, Language.Python)]
        [TestCase(Resolution.Minute, Language.Python)]
        [TestCase(Resolution.Hour, Language.Python)]
        [TestCase(Resolution.Daily, Language.Python)]
        public void TimeSpanHistoryRequestIsCorrectlyBuilt(Resolution resolution, Language language)
        {
            _algorithm.SetStartDate(2013, 10, 07);

            if (language == Language.CSharp)
            {
                _algorithm.History(Symbols.SPY, TimeSpan.FromSeconds(2), resolution);
            }
            else
            {
                using (Py.GIL())
                {
                    _algorithm.SetPandasConverter();
                    _algorithm.History(Symbols.SPY.ToPython(), TimeSpan.FromSeconds(2), resolution);
                }
            }

            Resolution? fillForwardResolution = null;
            if (resolution != Resolution.Tick)
            {
                fillForwardResolution = resolution;
            }

            var expectedCount = resolution == Resolution.Hour || resolution == Resolution.Daily ? 1 : 2;
            Assert.AreEqual(expectedCount, _testHistoryProvider.HistryRequests.Count);
            Assert.AreEqual(Symbols.SPY, _testHistoryProvider.HistryRequests.First().Symbol);
            Assert.AreEqual(resolution, _testHistoryProvider.HistryRequests.First().Resolution);
            Assert.IsFalse(_testHistoryProvider.HistryRequests.First().IncludeExtendedMarketHours);
            Assert.IsFalse(_testHistoryProvider.HistryRequests.First().IsCustomData);
            Assert.AreEqual(fillForwardResolution, _testHistoryProvider.HistryRequests.First().FillForwardResolution);
            Assert.AreEqual(DataNormalizationMode.Adjusted, _testHistoryProvider.HistryRequests.First().DataNormalizationMode);
            Assert.AreEqual(TickType.Trade, _testHistoryProvider.HistryRequests.First().TickType);
        }

        [TestCase(Resolution.Second, Language.CSharp)]
        [TestCase(Resolution.Minute, Language.CSharp)]
        [TestCase(Resolution.Hour, Language.CSharp)]
        [TestCase(Resolution.Daily, Language.CSharp)]
        [TestCase(Resolution.Second, Language.Python)]
        [TestCase(Resolution.Minute, Language.Python)]
        [TestCase(Resolution.Hour, Language.Python)]
        [TestCase(Resolution.Daily, Language.Python)]
        public void BarCountHistoryRequestIsCorrectlyBuilt(Resolution resolution, Language language)
        {
            _algorithm.SetStartDate(2013, 10, 07);

            if (language == Language.CSharp)
            {
                _algorithm.History(Symbols.SPY, 10, resolution);
            }
            else
            {
                using (Py.GIL())
                {
                    _algorithm.SetPandasConverter();
                    _algorithm.History(Symbols.SPY.ToPython(), 10, resolution);
                }
            }

            Resolution? fillForwardResolution = null;
            if (resolution != Resolution.Tick)
            {
                fillForwardResolution = resolution;
            }

            var expectedCount = resolution == Resolution.Hour || resolution == Resolution.Daily ? 1 : 2;
            Assert.AreEqual(expectedCount, _testHistoryProvider.HistryRequests.Count);
            Assert.AreEqual(Symbols.SPY, _testHistoryProvider.HistryRequests.First().Symbol);
            Assert.AreEqual(resolution, _testHistoryProvider.HistryRequests.First().Resolution);
            Assert.IsFalse(_testHistoryProvider.HistryRequests.First().IncludeExtendedMarketHours);
            Assert.IsFalse(_testHistoryProvider.HistryRequests.First().IsCustomData);
            Assert.AreEqual(fillForwardResolution, _testHistoryProvider.HistryRequests.First().FillForwardResolution);
            Assert.AreEqual(DataNormalizationMode.Adjusted, _testHistoryProvider.HistryRequests.First().DataNormalizationMode);
            Assert.AreEqual(TickType.Trade, _testHistoryProvider.HistryRequests.First().TickType);
        }

        [TestCase(Language.CSharp)]
        [TestCase(Language.Python)]
        public void TickHistoryRequestIgnoresFillForward(Language language)
        {
            _algorithm.SetStartDate(2013, 10, 07);

            if (language == Language.CSharp)
            {
                _algorithm.History(new [] {Symbols.SPY}, new DateTime(1,1,1,1,1,1), new DateTime(1, 1, 1, 1, 1, 2), Resolution.Tick,
                    fillForward: true);
            }
            else
            {
                using (Py.GIL())
                {
                    _algorithm.SetPandasConverter();
                    using var symbols = new PyList(new [] {Symbols.SPY.ToPython()});
                    _algorithm.History(symbols, new DateTime(1,1,1,1,1,1), new DateTime(1, 1, 1, 1, 1, 2),
                        Resolution.Tick, fillForward: true);
                }
            }

            Assert.AreEqual(2, _testHistoryProvider.HistryRequests.Count);
            Assert.AreEqual(Symbols.SPY, _testHistoryProvider.HistryRequests.First().Symbol);
            Assert.AreEqual(Resolution.Tick, _testHistoryProvider.HistryRequests.First().Resolution);
            Assert.IsFalse(_testHistoryProvider.HistryRequests.First().IncludeExtendedMarketHours);
            Assert.IsFalse(_testHistoryProvider.HistryRequests.First().IsCustomData);
            Assert.AreEqual(null, _testHistoryProvider.HistryRequests.First().FillForwardResolution);
            Assert.AreEqual(DataNormalizationMode.Adjusted, _testHistoryProvider.HistryRequests.First().DataNormalizationMode);
            Assert.AreEqual(TickType.Trade, _testHistoryProvider.HistryRequests.First().TickType);
        }

        [Test]
        public void GetLastKnownPriceOfIlliquidAsset_RealData()
        {
            var algorithm = GetAlgorithm(new DateTime(2014, 6, 6, 11, 0, 0));

            //20140606_twx_minute_quote_american_call_230000_20150117.csv
            var optionSymbol = Symbol.CreateOption("TWX", Market.USA, OptionStyle.American, OptionRight.Call, 23, new DateTime(2015,1,17));
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
        public void GetLastKnownPriceOfCustomData()
        {
            var algorithm = GetAlgorithm(new DateTime(2018, 4, 4));

            var alpha = algorithm.AddData<AlphaStreamsPortfolioState>("9fc8ef73792331b11dbd5429a");

            var lastKnownPrice = algorithm.GetLastKnownPrice(alpha);
            Assert.IsNotNull(lastKnownPrice);
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

            var lastKnownPrices = algorithm.GetLastKnownPrices(option).ToList();;
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
                    using var symbols = new PyList(new [] {optionSymbol.ToPython()});
                    var openInterestsDataFrameDict = getOpenInterestHistory
                        .Invoke(_algorithm.ToPython(), symbols, start.ToPython(), end.ToPython(),
                            historyResolution.ToPython())
                        .ConvertToDictionary<string, PyObject>();

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
                var result = _algorithm.History(new[] { optionSymbol }, start, end, historyResolution, fillForward:false).ToList();

                Assert.AreEqual(53, result.Count);
                Assert.IsTrue(result.Any(slice => slice.ContainsKey(optionSymbol)));

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
                    using var symbols = new PyList(new [] {optionSymbol.ToPython()});
                    var openInterests = getOpenInterestHistory.Invoke(_algorithm.ToPython(), symbols, start.ToPython(), end.ToPython(),
                        historyResolution.ToPython());
                    Assert.AreEqual(780, openInterests.GetAttr("shape")[0].As<int>());

                    var dataFrameDict = openInterests.GetAttr("to_dict").Invoke().ConvertToDictionary<string, dynamic>();
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
                    using var symbols = new PyList(new [] { optionSymbol.ToPython(), optionSymbol2.ToPython() });
                    var result = getOpenInterestHistory
                        .Invoke(_algorithm.ToPython(), symbols, start.ToPython(), end.ToPython(), historyResolution.ToPython());
                    Assert.AreEqual(1170, result.GetAttr("shape")[0].As<int>());

                    var dataFrameDict = result
                        .GetAttr("reset_index").Invoke()
                        .GetAttr("to_dict").Invoke()
                        .ConvertToDictionary<string, PyObject>();
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
            var expectedHistoryCount = 74;

            if (language == Language.CSharp)
            {
               var historyResults = dataMappingModes
                    .Select(x => _algorithm.History(new [] { symbol }, historyStart, historyEnd, resolution, dataMappingMode: x).ToList())
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
                    using var symbols = new PyList(new [] { symbol.ToPython() });
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
                    _algorithm.History(new [] { equity.Symbol }, start, end, equity.Resolution,
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
                        var symbols = new PyList(new [] { equity.Symbol.ToPython() });
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
                    _algorithm.History(new [] { future.Symbol }, start, end, future.Resolution,
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
                        var symbols = new PyList(new [] { future.Symbol.ToPython() });
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
                    _algorithm.History(new [] { equity.Symbol }, start, end, equity.Resolution,
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
                        var symbols = new PyList(new [] { equity.Symbol.ToPython() });
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
                    _algorithm.History(new [] { future.Symbol }, start, end, future.Resolution,
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
                        var symbols = new PyList(new [] { future.Symbol.ToPython() });
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
                    dataNormalizationModes, expectedHistoryCount: 74);
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
            var expectedHistoryCount = 74;

            if (language == Language.CSharp)
            {
                Func<int, List<Slice>> getHistoryForContractDepthOffset = (contractDepthOffset) =>
                {
                    return _algorithm.History(new [] { future.Symbol }, start, end, future.Resolution, contractDepthOffset: contractDepthOffset).ToList();
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

                var historyResults = new List<List<Slice>>{ frontMonthHistory, backMonthHistory1, backMonthHistory2 };
                CheckThatHistoryResultsHaveEqualBarCount(historyResults, expectedHistoryCount);
                CheckThatHistoryResultsHaveDifferentPrices(historyResults,
                    "History results prices should have been different for each data mapping mode at each time");
            }
            else
            {
                using (Py.GIL())
                {
                    _algorithm.SetPandasConverter();
                    using var symbols = new PyList(new [] { future.Symbol.ToPython() });

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

                var historyResults = new List<PyObject>{ frontMonthHistory, backMonthHistory1, backMonthHistory2 };
                CheckThatHistoryResultsHaveEqualBarCount(historyResults, expectedHistoryCount);
                CheckThatHistoryResultsHaveDifferentPrices(historyResults,
                    "History results prices should have been different for each contract depth offset at each time");
                }
            }
        }

        [Test]
        public void GetHistoryWithCustomData_CSharp()
        {
            var algorithm = GetAlgorithm(new DateTime(2013, 10, 8));
            var symbol = algorithm.AddData<CustomData>("SPY").Symbol;
            var history = algorithm.History<CustomData>(symbol, algorithm.StartDate, algorithm.EndDate, Resolution.Minute).ToList();

            Assert.That(history, Has.Count.EqualTo(1539));
            Assert.That(history, Has.All.Property("DataType").EqualTo(MarketDataType.Base));
            Assert.IsTrue(_testHistoryProvider.HistryRequests.All(x => x.IsCustomData));
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

                CheckThatHistoryResultsHaveEqualBarCount(historyResults, expectedHistoryCount: 74);
                CheckThatHistoryResultsHaveDifferentPrices(historyResults,
                    "History results prices should have been different for each data mapping mode at each time");
            }
        }

        [Test]
        public void GetHistoryWithCustomDataAndContractDepthOffset()
        {
            var start = new DateTime(2013, 10, 6);
            var end = new DateTime(2014, 1, 1);
            var  algorithm = GetAlgorithmWithFuture(end);
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
                using var symbols = new PyList(new [] { future.Symbol.ToPython() });
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

                var historyResults = new List<PyObject>{ frontMonthHistory, backMonthHistory1, backMonthHistory2 };
                CheckThatHistoryResultsHaveEqualBarCount(historyResults, expectedHistoryCount: 74);
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
                Assert.AreEqual(1050, openInterestHistory.Count());
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
                    Assert.AreEqual(1050, openInterestHistory.shape[0].As<int>());
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

            var history = algorithm.History(new [] { ibmSymbol }, start, end, Resolution.Tick);
            var tickCountInSliceHistoryCall = history.Sum(x => x.Ticks[ibmSymbol].Count);
            Assert.AreEqual(132104, tickCountInSliceHistoryCall);

            var tickHistory = algorithm.History<Tick>(ibmSymbol, start, end, Resolution.Tick).ToList();
            var tickCountInTickHistoryCall = tickHistory.Count;
            Assert.AreEqual(tickCountInSliceHistoryCall, tickCountInTickHistoryCall);
        }

        private QCAlgorithm GetAlgorithm(DateTime dateTime)
        {
            var algorithm = new QCAlgorithm();
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
                new DataPermissionManager()));
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
        private static void CheckThatHistoryResultsHaveEqualBarCount(List<List<Slice>> historyResults, int expectedHistoryCount)
        {
            Assert.That(historyResults, Has.All.Not.Empty.And.All.Count.EqualTo(expectedHistoryCount));
        }

        /// <summary>
        /// Helper method to check that all history data frame results have the same bar count
        /// </summary>
        private static void CheckThatHistoryResultsHaveEqualBarCount(List<PyObject> historyResults, int expectedHistoryCount)
        {
            Assert.Greater(expectedHistoryCount, 0);
            Assert.IsTrue(historyResults.All(x => x.GetAttr("shape")[0].As<int>() == expectedHistoryCount));
        }

        /// <summary>
        /// Helper method to check that, for each history result, prices at each time are different
        /// </summary>
        private static void CheckThatHistoryResultsHaveDifferentPrices(List<List<Slice>> historyResults, string message)
        {
            for (int i = 0; i < historyResults[0].Count; i++)
            {
                var closePrices = historyResults.Select(hr => hr[i].Values.Single().Price).ToHashSet();
                Assert.AreEqual(historyResults.Count, closePrices.Count, message);
            }
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
                .Select(x => algorithm.History(new [] { symbol }, start, end, resolution, dataNormalizationMode: x).ToList())
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

        private static DataMappingMode[] GetAllDataMappingModes()
        {
            return (DataMappingMode[])Enum.GetValues(typeof(DataMappingMode));
        }

        private static DataNormalizationMode[] GetAllDataNormalizationModes()
        {
            return (DataNormalizationMode[])Enum.GetValues(typeof(DataNormalizationMode));
        }
    }
}
