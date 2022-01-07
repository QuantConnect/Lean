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
using System.IO;
using System.Linq;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Util;
using QuantConnect.Securities;
using QuantConnect.Data.Market;
using System.Collections.Generic;
using NodaTime;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.HistoricalData;
using QuantConnect.Tests.Algorithm;
using QuantConnect.ToolBox;

namespace QuantConnect.Tests.ToolBox
{
    [TestFixture]
    public class LeanDataWriterTests
    {
        private readonly string _dataDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        private Symbol _forex;
        private Symbol _cfd;
        private Symbol _equity;
        private Symbol _crypto;
        private DateTime _date;

        [OneTimeSetUp]
        public void Setup()
        {
            _forex = Symbol.Create("EURUSD", SecurityType.Forex, Market.FXCM);
            _cfd = Symbol.Create("BCOUSD", SecurityType.Cfd, Market.Oanda);
            _equity = Symbol.Create("spy", SecurityType.Equity, Market.USA);
            _date = Parse.DateTime("3/16/2017 12:00:00 PM");
            _crypto = Symbol.Create("BTCUSD", SecurityType.Crypto, Market.GDAX);
        }

        private List<Tick> GetTicks(Symbol sym)
        {
            return new List<Tick>()
            {
                new Tick(Parse.DateTime("3/16/2017 12:00:00 PM"), sym, 1.0m, 2.0m),
                new Tick(Parse.DateTime("3/16/2017 12:00:01 PM"), sym, 3.0m, 4.0m),
                new Tick(Parse.DateTime("3/16/2017 12:00:02 PM"), sym, 5.0m, 6.0m),
            };
        }

        private List<QuoteBar> GetQuoteBars(Symbol sym)
        {
            return new List<QuoteBar>()
            {
                new QuoteBar(Parse.DateTime("3/16/2017 12:00:00 PM"), sym, new Bar(1m, 2m, 3m, 4m),  1, new Bar(5m, 6m, 7m, 8m),  2),
                new QuoteBar(Parse.DateTime("3/16/2017 12:00:01 PM"), sym, new Bar(11m, 21m, 31m, 41m),  3, new Bar(51m, 61m, 71m, 81m), 4),
                new QuoteBar(Parse.DateTime("3/16/2017 12:00:02 PM"), sym, new Bar(10m, 20m, 30m, 40m),  5, new Bar(50m, 60m, 70m, 80m),  6),
            };
        }

        [Test]
        public void LeanDataWriter_MultipleDays()
        {
            var leanDataWriter = new LeanDataWriter(Resolution.Second, _forex, _dataDirectory, TickType.Quote);
            var sourceData = new List<QuoteBar>
            {
                new (Parse.DateTime("3/16/2021 12:00:00 PM"), _forex, new Bar(1m, 2m, 3m, 4m),  1, new Bar(5m, 6m, 7m, 8m),  2)
            };

            for (var i = 1; i < 100; i++)
            {
                sourceData.Add(new QuoteBar(sourceData.Last().Time.AddDays(1),
                    _forex,
                    new Bar(1m, 2m, 3m, 4m),
                    1, new Bar(5m, 6m, 7m, 8m),
                    2));
            }
            leanDataWriter.Write(sourceData);

            foreach (var bar in sourceData)
            {
                var filePath = LeanData.GenerateZipFilePath(_dataDirectory, _forex, bar.Time, Resolution.Second, TickType.Quote);
                Assert.IsTrue(File.Exists(filePath));
                Assert.IsFalse(File.Exists(filePath + ".tmp"));

                var data = QuantConnect.Compression.Unzip(filePath).Single();

                Assert.AreEqual(1, data.Value.Count());
                Assert.IsTrue(data.Key.Contains(bar.Time.ToStringInvariant(DateFormat.EightCharacter)), $"Key {data.Key} BarTime: {bar.Time}");
            }
        }

        [Test]
        public void LeanDataWriter_CanWriteForex()
        {
            var filePath = LeanData.GenerateZipFilePath(_dataDirectory, _forex, _date, Resolution.Second, TickType.Quote);

            var leanDataWriter = new LeanDataWriter(Resolution.Second, _forex, _dataDirectory, TickType.Quote);
            leanDataWriter.Write(GetQuoteBars(_forex));

            Assert.IsTrue(File.Exists(filePath));
            Assert.IsFalse(File.Exists(filePath + ".tmp"));

            var data = QuantConnect.Compression.Unzip(filePath);

            Assert.AreEqual(data.First().Value.Count(), 3);
        }

        [TestCase(SecurityType.FutureOption)]
        [TestCase(SecurityType.Future)]
        [TestCase(SecurityType.Option)]
        public void LeanDataWriter_CanWriteZipWithMultipleContracts(SecurityType securityType)
        {
            Symbol contract1;
            Symbol contract2;
            if (securityType == SecurityType.Future)
            {
                contract1 = Symbol.CreateFuture(Futures.Indices.SP500EMini, Market.CME, new DateTime(2020, 02, 01));
                contract2 = Symbol.CreateFuture(Futures.Indices.SP500EMini, Market.CME, new DateTime(2020, 03, 01));
            }
            else if (securityType == SecurityType.Option)
            {
                contract1 = Symbol.CreateOption("AAPL", Market.USA, OptionStyle.American, OptionRight.Call, 1, new DateTime(2020, 02, 01));
                contract2 = Symbol.CreateOption("AAPL", Market.USA, OptionStyle.American, OptionRight.Call, 1, new DateTime(2020, 03, 01));
            }
            else if (securityType == SecurityType.FutureOption)
            {
                contract1 = Symbol.CreateOption(Futures.Indices.SP500EMini, Market.CME, OptionStyle.American, OptionRight.Call, 1, new DateTime(2020, 02, 01));
                contract2 = Symbol.CreateOption(Futures.Indices.SP500EMini, Market.CME, OptionStyle.American, OptionRight.Call, 1, new DateTime(2020, 03, 01));
            }
            else
            {
                throw new NotImplementedException($"{securityType} not implemented!");
            }

            var filePath1 = LeanData.GenerateZipFilePath(_dataDirectory, contract1, _date, Resolution.Second, TickType.Quote);
            var leanDataWriter1 = new LeanDataWriter(Resolution.Second, contract1, _dataDirectory, TickType.Quote);
            leanDataWriter1.Write(GetQuoteBars(contract1));

            var filePath2 = LeanData.GenerateZipFilePath(_dataDirectory, contract2, _date, Resolution.Second, TickType.Quote);
            var leanDataWriter2 = new LeanDataWriter(Resolution.Second, contract2, _dataDirectory, TickType.Quote);
            leanDataWriter2.Write(GetQuoteBars(contract2));

            Assert.AreEqual(filePath1, filePath2);
            Assert.IsTrue(File.Exists(filePath1));
            Assert.IsFalse(File.Exists(filePath1 + ".tmp"));

            var data = QuantConnect.Compression.Unzip(filePath1).ToDictionary(x => x.Key, x => x.Value.ToList());
            Assert.AreEqual(2, data.Count);
            Assert.That(data.Values, Has.All.Count.EqualTo(3));
        }

        [Test]
        public void LeanDataWriter_CanWriteCfd()
        {
            var filePath = LeanData.GenerateZipFilePath(_dataDirectory, _cfd, _date, Resolution.Minute, TickType.Quote);

            var leanDataWriter = new LeanDataWriter(Resolution.Minute, _cfd, _dataDirectory, TickType.Quote);
            leanDataWriter.Write(GetQuoteBars(_cfd));

            Assert.IsTrue(File.Exists(filePath));
            Assert.IsFalse(File.Exists(filePath + ".tmp"));

            var data = QuantConnect.Compression.Unzip(filePath);

            Assert.AreEqual(data.First().Value.Count(), 3);
        }

        [Test]
        public void LeanDataWriter_CanWriteEquity()
        {
            var filePath = LeanData.GenerateZipFilePath(_dataDirectory, _equity, _date, Resolution.Tick, TickType.Trade);

            var leanDataWriter = new LeanDataWriter(Resolution.Tick, _equity, _dataDirectory);
            leanDataWriter.Write(GetTicks(_equity));

            Assert.IsTrue(File.Exists(filePath));
            Assert.IsFalse(File.Exists(filePath + ".tmp"));

            var data = QuantConnect.Compression.Unzip(filePath);

            Assert.AreEqual(data.First().Value.Count(), 3);
        }

        [Test]
        public void LeanDataWriter_CanWriteCrypto()
        {
            var filePath = LeanData.GenerateZipFilePath(_dataDirectory, _crypto, _date, Resolution.Second, TickType.Quote);

            var leanDataWriter = new LeanDataWriter(Resolution.Second, _crypto, _dataDirectory, TickType.Quote);
            leanDataWriter.Write(GetQuoteBars(_crypto));

            Assert.IsTrue(File.Exists(filePath));
            Assert.IsFalse(File.Exists(filePath + ".tmp"));

            var data = QuantConnect.Compression.Unzip(filePath);

            Assert.AreEqual(data.First().Value.Count(), 3);
        }

        [TestCase(SecurityType.Equity, TickType.Quote, Resolution.Minute)]
        [TestCase(SecurityType.Equity, TickType.Trade, Resolution.Daily)]
        [TestCase(SecurityType.Equity, TickType.Trade, Resolution.Hour)]
        [TestCase(SecurityType.Equity, TickType.Trade, Resolution.Minute)]
        [TestCase(SecurityType.Crypto, TickType.Quote, Resolution.Minute)]
        [TestCase(SecurityType.Crypto, TickType.Trade, Resolution.Daily)]
        [TestCase(SecurityType.Crypto, TickType.Trade, Resolution.Minute)]
        [TestCase(SecurityType.Option, TickType.Quote, Resolution.Minute)]
        [TestCase(SecurityType.Option, TickType.Trade, Resolution.Minute)]
        public void CanDownloadAndSave(SecurityType securityType, TickType tickType, Resolution resolution)
        {
            var symbol = Symbols.GetBySecurityType(securityType);
            var startTimeUtc = GetRepoDataDates(securityType, resolution);

            // Override for this case because symbol from Symbols does not have data included
            if (securityType == SecurityType.Option)
            {
                symbol = Symbols.CreateOptionSymbol("GOOG", OptionRight.Call, 770, new DateTime(2015, 12, 24));
                startTimeUtc = new DateTime(2015, 12, 23);
            }

            // EndTime based on start, only do 1 day for anything less than hour because we compare datafiles below
            // and minute and finer resolutions store by day
            var endTimeUtc = startTimeUtc + TimeSpan.FromDays(resolution >= Resolution.Hour ? 15 : 1);

            // Create our writer and LocalHistory brokerage to "download" from
            var writer = new LeanDataWriter(_dataDirectory, resolution, securityType, tickType);
            var brokerage = new LocalHistoryBrokerage();
            var symbols = new List<Symbol>() {symbol};

            // "Download" and write to file
            writer.DownloadAndSave(brokerage, symbols, startTimeUtc, endTimeUtc);

            // Verify the file exists where we expect
            var filePath = LeanData.GenerateZipFilePath(_dataDirectory, symbol, startTimeUtc, resolution, tickType);
            Assert.IsTrue(File.Exists(filePath));

            // Read the file and data
            var reader = new LeanDataReader(filePath);
            var dataFromFile = reader.Parse().ToList();

            // Ensure its not empty and it is actually for this symbol
            Assert.IsNotEmpty(dataFromFile);
            Assert.IsTrue(dataFromFile.All(x => x.Symbol == symbol));

            // Get history directly ourselves and compare with the data in the file
            var history = GetHistory(brokerage, resolution, securityType, symbol, tickType, startTimeUtc, endTimeUtc);
            CollectionAssert.AreEqual(history.Select(x => x.Time), dataFromFile.Select(x => x.Time));

            brokerage.Dispose();
        }

        /// <summary>
        /// Helper to get history for tests from a brokerage implementation
        /// </summary>
        /// <returns>List of data points from history request</returns>
        private List<BaseData> GetHistory(IBrokerage brokerage, Resolution resolution, SecurityType securityType, Symbol symbol, TickType tickType, DateTime startTimeUtc, DateTime endTimeUtc)
        {
            var dataType = LeanData.GetDataType(resolution, tickType);

            var marketHoursDatabase = MarketHoursDatabase.FromDataFolder();

            var ticker = symbol.ID.Symbol;
            var market = symbol.ID.Market;

            var canonicalSymbol = Symbol.Create(ticker, securityType, market);

            var exchangeHours = marketHoursDatabase.GetExchangeHours(canonicalSymbol.ID.Market, canonicalSymbol, securityType);
            var dataTimeZone = marketHoursDatabase.GetDataTimeZone(canonicalSymbol.ID.Market, canonicalSymbol, securityType);

            var historyRequest = new HistoryRequest(
                startTimeUtc,
                endTimeUtc,
                dataType,
                symbol,
                resolution,
                exchangeHours,
                dataTimeZone,
                resolution,
                true,
                false,
                DataNormalizationMode.Raw,
                tickType
            );

            return brokerage.GetHistory(historyRequest)
                .Select(
                    x =>
                    {
                        // Convert to date timezone before we write it
                        x.Time = x.Time.ConvertTo(exchangeHours.TimeZone, dataTimeZone);
                        return x;
                    })
                .ToList();
        }

        /// <summary>
        /// Test helper method to get dates for data we have in the repo
        /// Could possibly be refactored and used in Tests.Symbols in a similar way
        /// </summary>
        /// <returns>Start time where some data included in the repo exists</returns>
        private static DateTime GetRepoDataDates(SecurityType securityType, Resolution resolution)
        {
            // Because I intend to use this with GetBySecurityType here are the symbols we expect
            // case SecurityType.Equity:   return SPY;
            // case SecurityType.Option:   return SPY_C_192_Feb19_2016;
            // case SecurityType.Forex:    return EURUSD;
            // case SecurityType.Future:   return Future_CLF19_Jan2019;
            // case SecurityType.Cfd:      return XAGUSD;
            // case SecurityType.Crypto:   return BTCUSD;
            // case SecurityType.Index:    return SPX;
            switch (securityType)
            {
                case SecurityType.Equity: // SPY; Daily/Hourly/Minute/Second/Tick
                    return new DateTime(2013, 10, 7);
                case SecurityType.Crypto: // GDAX BTCUSD Daily/Minute/Second
                    if (resolution == Resolution.Hour || resolution == Resolution.Tick)
                    {
                        throw new ArgumentException($"GDAX BTC Crypto does not have data for this resolution {resolution}");
                    }
                    return new DateTime(2017, 9, 3);
                case SecurityType.Option: // No Data for the default symbol...
                    return DateTime.MinValue;
                default:
                    throw new NotImplementedException("This has only implemented a few security types (Equity/Crypto/Option)");
            }
        }

        /// <summary>
        /// Fake brokerage that just uses Local Disk Data to do history requests
        /// </summary>
        internal class LocalHistoryBrokerage : NullBrokerage 
        {
            private readonly IDataCacheProvider _dataCacheProvider;
            private readonly IHistoryProvider _historyProvider;

            public LocalHistoryBrokerage()
            {
                var mapFileProvider = TestGlobals.MapFileProvider;
                var dataProvider = TestGlobals.DataProvider;
                _dataCacheProvider = new ZipDataCacheProvider(dataProvider);
                var factorFileProvider = TestGlobals.FactorFileProvider;
                var dataPermissionManager = new DataPermissionManager();

                mapFileProvider.Initialize(dataProvider);
                factorFileProvider.Initialize(mapFileProvider, dataProvider);

                _historyProvider = new SubscriptionDataReaderHistoryProvider();
                _historyProvider.Initialize(
                    new HistoryProviderInitializeParameters(
                        null,
                        null,
                        dataProvider,
                        _dataCacheProvider,
                        mapFileProvider,
                        factorFileProvider,
                        null,
                        true,
                        dataPermissionManager
                    )
                );
            }

            public override IEnumerable<BaseData> GetHistory(HistoryRequest request)
            {
                var requests = new List<HistoryRequest> {request};
                var slices = _historyProvider.GetHistory(requests, DateTimeZone.Utc);

                // Grab all the bar values for this
                switch (request.TickType)
                {
                    case TickType.Quote:
                        return slices.SelectMany(x => x.QuoteBars.Values);
                    case TickType.Trade:
                        return slices.SelectMany(x => x.Bars.Values);
                    default:
                        throw new NotImplementedException("Only support Trade & Quote bars");
                }
            }

            public override void Dispose()
            {
                _dataCacheProvider.Dispose();
            }
        }
    }
}
