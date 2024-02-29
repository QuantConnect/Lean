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
using QuantConnect.Data;
using System.Globalization;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.HistoricalData;
using QuantConnect.Packets;
using QuantConnect.Securities;
using QuantConnect.Util;
using System;
using System.Linq;
using QuantConnect.Brokerages.Paper;
using HistoryRequest = QuantConnect.Data.HistoryRequest;

namespace QuantConnect.Tests.Engine.HistoricalData
{
    [TestFixture]
    public class HistoryProviderManagerTests
    {
        private HistoryProviderManager _historyProviderWrapper;

        [SetUp]
        public void Setup()
        {
            _historyProviderWrapper = new();
            var historyProviders = Newtonsoft.Json.JsonConvert.SerializeObject(new[] { nameof(SubscriptionDataReaderHistoryProvider), nameof(TestHistoryProvider) });
            var jobWithArrayHistoryProviders = new LiveNodePacket
            {
                HistoryProvider = historyProviders
            };
            _historyProviderWrapper.SetBrokerage(new PaperBrokerage(null, null));
            _historyProviderWrapper.Initialize(new HistoryProviderInitializeParameters(
                jobWithArrayHistoryProviders,
                null,
                TestGlobals.DataProvider,
                TestGlobals.DataCacheProvider,
                TestGlobals.MapFileProvider,
                TestGlobals.FactorFileProvider,
                null,
                false,
                new DataPermissionManager(),
                null));
        }

        [TearDown]
        public void TearDown()
        {
            Composer.Instance.Reset();
        }

        [Test]
        public void DataPointCount()
        {
            var symbol = Symbol.Create("WM", SecurityType.Equity, Market.USA);

            var request = GetHistoryRequest(symbol, new DateTime(2008, 01, 01), new DateTime(2008, 01, 05), Resolution.Daily, TickType.Trade);

            var result = _historyProviderWrapper.GetHistory(new[] { request }, TimeZones.NewYork).ToList();

            Assert.IsNotNull(result);
            Assert.IsNotEmpty(result);
            Assert.AreEqual(5, _historyProviderWrapper.DataPointCount);
        }

        [Test]
        public void TestEvents()
        {
            bool invalidConfigurationDetected = new();
            bool numericalPrecisionLimited = new();
            bool startDateLimited = new();
            bool downloadFailed = new();
            bool readerErrorDetected = new();
            _historyProviderWrapper.InvalidConfigurationDetected += (sender, args) => { invalidConfigurationDetected = true; };
            _historyProviderWrapper.NumericalPrecisionLimited += (sender, args) => { numericalPrecisionLimited = true; };
            _historyProviderWrapper.StartDateLimited += (sender, args) => { startDateLimited = true; };
            _historyProviderWrapper.DownloadFailed += (sender, args) => { downloadFailed = true; };
            _historyProviderWrapper.ReaderErrorDetected += (sender, args) => { readerErrorDetected = true; };

            var historyProvider = Composer.Instance.GetExportedValueByTypeName<IHistoryProvider>(nameof(TestHistoryProvider));
            (historyProvider as TestHistoryProvider).TriggerEvents();
            Assert.IsTrue(invalidConfigurationDetected);
            Assert.IsTrue(numericalPrecisionLimited);
            Assert.IsTrue(startDateLimited);
            Assert.IsTrue(downloadFailed);
            Assert.IsTrue(readerErrorDetected);
        }

        [Test]
        public void OptionsAreMappedCorrectly()
        {
            var symbol = Symbol.CreateOption(
                "FOXA",
                Market.USA,
                OptionStyle.American,
                OptionRight.Call,
                32,
                new DateTime(2013, 07, 20));

            var request = GetHistoryRequest(symbol, new DateTime(2013, 06, 28), new DateTime(2013, 07, 03), Resolution.Minute, TickType.Quote);

            var result = _historyProviderWrapper.GetHistory(new[] { request }, TimeZones.NewYork).ToList();

            Assert.IsNotEmpty(result);

            // assert we fetch the data for the previous and new symbol
            var firstBar = result[1].Values.Single();
            var lastBar = result.Last().Values.Single();

            Assert.IsTrue(firstBar.Symbol.Value.Contains("NWSA"));
            Assert.AreEqual(28, firstBar.Time.Date.Day);
            Assert.IsTrue(lastBar.Symbol.Value.Contains("FOXA"));
            Assert.AreEqual(2, lastBar.Time.Date.Day);
            Assert.AreEqual(425, result.Count);
            Assert.AreEqual(426, _historyProviderWrapper.DataPointCount);
        }

        [Test]
        public void EquitiesMergedCorrectly()
        {
            var symbol = Symbol.Create("WM", SecurityType.Equity, Market.USA);

            var request = GetHistoryRequest(symbol, new DateTime(2008, 01, 01), new DateTime(2008, 01, 05), Resolution.Daily, TickType.Trade);

            var result = _historyProviderWrapper.GetHistory(new[] { request }, TimeZones.NewYork).ToList();

            Assert.IsNotEmpty(result);
            var firstBar = result.First().Values.Single();
            Assert.AreEqual("WMI", firstBar.Symbol.Value);
            Assert.AreEqual(4, result.Count);
            Assert.AreEqual(5, _historyProviderWrapper.DataPointCount);
        }

        [Test]
        public void DataIncreasesInTime()
        {
            var symbol = Symbol.Create("WM", SecurityType.Equity, Market.USA);

            var request = GetHistoryRequest(symbol, new DateTime(2008, 01, 01), new DateTime(2008, 01, 05), Resolution.Daily, TickType.Trade);

            var result = _historyProviderWrapper.GetHistory(new[] { request }, TimeZones.NewYork).ToList();

            var initialTime = DateTime.MinValue;
            foreach (var slice in result)
            {
                Assert.That(slice.UtcTime, Is.GreaterThan(initialTime));
                initialTime = slice.UtcTime;
            }
        }

        [TestCase("GOOGL", "2004/08/19", 2)] // IPO: August 19, 2004
        [TestCase("GOOGL", "2008/02/01", 2)]
        [TestCase("GOOGL", "2014/04/02", 2)] // The restructuring: "GOOG" to "GOOGL" 
        [TestCase("GOOGL", "2014/02/01", 2)]
        [TestCase("GOOGL", "2020/02/01", 1)]
        [TestCase("GOOG", "2020/02/01", 1)]
        [TestCase("GOOGL", "2023/02/01", 1)]
        [TestCase("AAPL", "2008/02/01", 1)]
        [TestCase("AAPL", "2008/02/01", 1)]
        [TestCase("GOOCV", "2010/01/01", 2)] // IPO: March 27, 2014
        [TestCase("GOOG", "2014/01/01", 2)]
        [TestCase("GOOG", "2014/04/03", 1)] // The restructuring: April 2, 2014 "GOOCV" to "GOOG"
        [TestCase("SPWR", "2005/11/17", 2)] // IPO: November 17, 2005
        [TestCase("SPWR", "2023/11/16", 1)]
        public void GetHistoricalSymbolNamesByDateRequest(string ticker, string startDateTimeRequest, int expectedAmount)
        {
            var startDateTime = DateTime.ParseExact(startDateTimeRequest, "yyyy/MM/dd", CultureInfo.InvariantCulture);
            var endDateTime = new DateTime(2008, 01, 05);

            var symbol = Symbol.Create(ticker, SecurityType.Equity, Market.USA);

            var historyRequest = GetHistoryRequest(symbol, startDateTime, endDateTime, Resolution.Daily, TickType.Trade);

            var historyProvider = Composer.Instance.GetExportedValueByTypeName<IHistoryProvider>(nameof(TestHistoryProvider));
            var tickers = (historyProvider as TestHistoryProvider).RetrieveSymbolHistoricalDefinitions(historyRequest);

            Assert.IsTrue(tickers.Count == expectedAmount);
        }

        private HistoryRequest GetHistoryRequest(Symbol symbol, DateTime startDateTime, DateTime endDateTime, Resolution resolution, TickType tickType)
        {
            var dataType = LeanData.GetDataType(resolution, tickType);

            return new HistoryRequest(
                startDateTime,
                endDateTime,
                dataType,
                symbol,
                resolution,
                SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                TimeZones.NewYork,
                null,
                false,
                false,
                DataNormalizationMode.Raw,
                tickType
                );
        }
    }
}
