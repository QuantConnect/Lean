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

        [TestCase(Resolution.Daily)]
        [TestCase(Resolution.Hour)]
        [TestCase(Resolution.Minute)]
        [TestCase(Resolution.Second)]
        public void TickResolutionSubscriptionHistoryRequestOtherResolution(Resolution resolution)
        {
            var start = new DateTime(2013, 10, 07);
            _algorithm = GetAlgorithm(start.AddDays(1));

            _algorithm.AddEquity(Symbols.SPY, Resolution.Tick);
            // Trades and quotes
            var result = _algorithm.History(new [] { Symbols.SPY }, start, _algorithm.Time, resolution).ToList();

            Assert.IsNotEmpty(result);
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

        [Test]
        public void TickResolutionHistoryRequest()
        {
            var start = new DateTime(2013, 10, 07);
            _algorithm = GetAlgorithm(start.AddDays(1));

            // Trades and quotes
            var result = _algorithm.History(new [] { Symbols.SPY }, start.AddHours(9.8), start.AddHours(10), Resolution.Tick).ToList();

            // Just Trades
            var result2 = _algorithm.History<Tick>(Symbols.SPY, start.AddHours(9.8), start.AddHours(10), Resolution.Tick).ToList();

            Assert.IsNotEmpty(result);
            Assert.IsNotEmpty(result2);

            Assert.IsTrue(result2.All(tick => tick.TickType == TickType.Trade));

            // (Trades and quotes).Count > Trades * 2
            Assert.Greater(result.Count, result2.Count * 2);
        }

        [Test]
        public void ImplicitTickResolutionHistoryRequestTradeBarApiThrowsException()
        {
            var spy = _algorithm.AddEquity("SPY", Resolution.Tick).Symbol;
            Assert.Throws<InvalidOperationException>(() => _algorithm.History(spy, 1).ToList());
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

        [TestCase(Resolution.Second)]
        [TestCase(Resolution.Minute)]
        [TestCase(Resolution.Hour)]
        [TestCase(Resolution.Daily)]
        public void TimeSpanHistoryRequestIsCorrectlyBuilt(Resolution resolution)
        {
            _algorithm.SetStartDate(2013, 10, 07);
            _algorithm.History(Symbols.SPY, TimeSpan.FromSeconds(2), resolution);
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

        [TestCase(Resolution.Second)]
        [TestCase(Resolution.Minute)]
        [TestCase(Resolution.Hour)]
        [TestCase(Resolution.Daily)]
        public void BarCountHistoryRequestIsCorrectlyBuilt(Resolution resolution)
        {
            _algorithm.SetStartDate(2013, 10, 07);
            _algorithm.History(Symbols.SPY, 10, resolution);
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

        [Test]
        public void TickHistoryRequestIgnoresFillForward()
        {
            _algorithm.SetStartDate(2013, 10, 07);
            _algorithm.History(new [] {Symbols.SPY}, new DateTime(1,1,1,1,1,1), new DateTime(1, 1, 1, 1, 1, 2), Resolution.Tick, fillForward: true);

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

        [Test]
        public void TickResolutionOpenInterestHistoryRequestIsNotFilteredWhenRequestedExplicitly()
        {
            var start = new DateTime(2014, 6, 05);
            _algorithm = GetAlgorithm(start);
            _algorithm.SetStartDate(start);
            _algorithm.SetDateTime(start.AddDays(2));

            _algorithm.UniverseSettings.FillForward = false;
            var optionSymbol = Symbol.CreateOption("TWX", Market.USA, OptionStyle.American, OptionRight.Call, 23, new DateTime(2015, 1, 17));
            var openInterests = _algorithm.History<OpenInterest>(optionSymbol, start, start.AddDays(2), Resolution.Minute).ToList();

            Assert.IsNotEmpty(openInterests);

            Assert.AreEqual(2, openInterests.Count);
            Assert.AreEqual(new DateTime(2014, 06, 05, 6, 32, 0), openInterests[0].Time);
            Assert.AreEqual(optionSymbol, openInterests[0].Symbol);
            Assert.AreEqual(new DateTime(2014, 06, 06, 6, 32, 0), openInterests[1].Time);
            Assert.AreEqual(optionSymbol, openInterests[1].Symbol);
        }

        [Test]
        public void TickResolutionOpenInterestHistoryRequestIsFilteredByDefault_SingleSymbol()
        {
            var start = new DateTime(2014, 6, 05);
            _algorithm = GetAlgorithm(start);
            _algorithm.SetStartDate(start);
            _algorithm.SetDateTime(start.AddDays(2));

            var optionSymbol = Symbol.CreateOption("TWX", Market.USA, OptionStyle.American, OptionRight.Call, 23, new DateTime(2015, 1, 17));
            var result = _algorithm.History(new[] { optionSymbol }, start, start.AddDays(2), Resolution.Minute, fillForward:false).ToList();

            Assert.IsNotEmpty(result);
            Assert.IsTrue(result.Any(slice => slice.ContainsKey(optionSymbol)));

            var openInterests = result.Select(slice => slice.Get(typeof(OpenInterest)) as DataDictionary<OpenInterest>).Where(dataDictionary => dataDictionary.Count > 0).ToList();

            Assert.AreEqual(0, openInterests.Count);
        }

        [Test]
        public void TickResolutionOpenInterestHistoryRequestIsFilteredByDefault_MultipleSymbols()
        {
            var start = new DateTime(2014, 6, 05);
            _algorithm = GetAlgorithm(start.AddDays(1));
            _algorithm.SetStartDate(start);
            _algorithm.SetDateTime(start.AddDays(2));

            var optionSymbol = Symbol.CreateOption("TWX", Market.USA, OptionStyle.American, OptionRight.Call, 23, new DateTime(2015, 1, 17));
            var optionSymbol2 = Symbol.CreateOption("AAPL", Market.USA, OptionStyle.American, OptionRight.Call, 500, new DateTime(2015, 1, 17));
            var result = _algorithm.History(new[] { optionSymbol, optionSymbol2 }, start, start.AddDays(2), Resolution.Minute, fillForward: false).ToList();

            Assert.IsNotEmpty(result);

            Assert.IsTrue(result.Any(slice => slice.ContainsKey(optionSymbol)));
            Assert.IsTrue(result.Any(slice => slice.ContainsKey(optionSymbol2)));

            var openInterests = result.Select(slice => slice.Get(typeof(OpenInterest)) as DataDictionary<OpenInterest>).Where(dataDictionary => dataDictionary.Count > 0).ToList();

            Assert.AreEqual(0, openInterests.Count);
        }

        [Test]
        public void SubscriptionHistoryRequestWithDifferentDataMappingMode()
        {
            var dataMappingModes = GetAllDataMappingModes();
            var historyStart = new DateTime(2013, 10, 6);
            var historyEnd = new DateTime(2014, 1, 1);
            var resolution = Resolution.Daily;
            _algorithm = GetAlgorithm(historyEnd);
            var symbol = _algorithm.AddFuture(Futures.Indices.SP500EMini, resolution, dataMappingMode: dataMappingModes.First()).Symbol;

            var historyResults = dataMappingModes
                .Select(x => _algorithm.History(new [] { symbol }, historyStart, historyEnd, resolution, dataMappingMode: x).ToList())
                .ToList();

            var expectedBarsCount = historyResults.First().Count;
            Assert.That(historyResults, Has.All.Not.Empty.And.All.Count.EqualTo(expectedBarsCount));

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
                throw new Exception($"History results should have had different mapping dates for each data mapping mode");
            }

            // Check that close prices at each time are different for different data mapping modes
            for (int j = 0; j < historyResults[0].Count; j++)
            {
                var closePrices = historyResults.Select(hr => hr[j].Bars.First().Value.Close).ToHashSet();
                if (closePrices.Count != dataMappingModes.Length)
                {
                    throw new Exception($"History results close prices should have been different for each data mapping mode at each time");
                }
            }
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

        private static DataMappingMode[] GetAllDataMappingModes()
        {
            return (DataMappingMode[])Enum.GetValues(typeof(DataMappingMode));
        }
    }
}
