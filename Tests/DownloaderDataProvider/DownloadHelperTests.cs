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
using System.IO;
using System.Linq;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Util;
using QuantConnect.Interfaces;
using QuantConnect.Data.Market;
using System.Collections.Generic;
using QuantConnect.DownloaderDataProvider.Launcher;
using QuantConnect.DownloaderDataProvider.Launcher.Models;

namespace QuantConnect.Tests.DownloaderDataProvider
{
    [TestFixture]
    public class DownloadHelperTests
    {
        private IDataCacheProvider _cacheProvider;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _cacheProvider = new DiskDataCacheProvider();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            if (_cacheProvider != null)
            {
                _cacheProvider.Dispose();
            }
        }

        /// <summary>
        /// Temporary data download directory
        /// </summary>
        private readonly string _dataDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        [TestCase(TickType.Trade, Resolution.Daily)]
        public void RunDownload(TickType tickType, Resolution resolution)
        {
            var startDate = new DateTime(2024, 01, 01);
            var tradeDate = new DateTime(2024, 01, 10);
            var endDate = new DateTime(2024, 02, 02);
            var symbol = Symbol.CreateCanonicalOption(Symbols.AAPL);

            var downloadDataConfig = new DataDownloadConfig(tickType, SecurityType.Option, resolution, startDate, endDate, Market.USA, new List<Symbol>() { symbol });

            var optionContracts = GenerateOptionContracts(Symbols.AAPL, 100, new DateTime(2024, 03, 16), expiryAddDay: 30);
            var generateOptionContactFileName = optionContracts.ToList(contract => LeanData.GenerateZipEntryName(contract, contract.ID.Date, resolution, tickType));

            Assert.That(optionContracts.Distinct().Count(), Is.EqualTo(optionContracts.Count));

            var mockBaseDate = GenerateTradeBarByEachSymbol(optionContracts, tradeDate);

            var downloader = new DataDownloaderTest(mockBaseDate);

            Program.RunDownload(downloader, downloadDataConfig, _dataDirectory, _cacheProvider);

            var filePath = LeanData.GenerateZipFilePath(_dataDirectory, symbol, startDate, resolution, tickType);
            var fileNames = _cacheProvider.GetZipEntries(filePath);
            var unZipData = QuantConnect.Compression.Unzip(filePath).ToDictionary(x => x.Key, x => x.Value.ToList());

            Assert.AreEqual(fileNames.Count, unZipData.Count);
            Assert.AreEqual(fileNames.Count, optionContracts.Count);

            foreach (var dataInZip in unZipData)
            {
                Assert.IsTrue(generateOptionContactFileName.Contains(dataInZip.Key));
                Assert.Greater(dataInZip.Value.Count, 0);
                Assert.IsTrue(dataInZip.Value.All(row => row.Length > 0));
            }
        }

        private static IEnumerable<BaseData> GenerateTradeBarByEachSymbol(IEnumerable<Symbol> symbols, DateTime tradeDateTime)
        {
            var multiplier = 100;
            foreach (var option in symbols)
            {
                yield return new TradeBar(tradeDateTime, option, multiplier, multiplier, multiplier, multiplier, multiplier);
                multiplier *= 2;
            }
        }

        private static List<Symbol> GenerateOptionContracts(Symbol underlying, decimal strikePrice, DateTime expiryDate, int strikeMultiplier = 2, int expiryAddDay = 1, int count = 2)
        {
            var contracts = new List<Symbol>();
            for (int i = 0; i < count; i++)
            {
                contracts.Add(Symbol.CreateOption(underlying, underlying.ID.Market, OptionStyle.American, OptionRight.Put, strikePrice, expiryDate));
                expiryDate = expiryDate.AddDays(expiryAddDay);
                strikePrice *= strikeMultiplier;
            }
            return contracts;
        }

        private class DataDownloaderTest : IDataDownloader
        {
            public IEnumerable<BaseData> Data { get; }

            public DataDownloaderTest(IEnumerable<BaseData> data)
            {
                Data = data;
            }

            public IEnumerable<BaseData> Get(DataDownloaderGetParameters dataDownloaderGetParameters)
            {
                return Data.Select(x => x);
            }
        }
    }
}
