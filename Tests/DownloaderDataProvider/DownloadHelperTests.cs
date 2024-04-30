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
using System.Linq;
using NUnit.Framework;
using System.Text.Json;
using QuantConnect.Data;
using QuantConnect.Util;
using QuantConnect.Data.Market;
using QuantConnect.Configuration;
using System.Collections.Generic;
using QuantConnect.DownloaderDataProvider.Launcher;

namespace QuantConnect.Tests.DownloaderDataProvider
{
    [TestFixture]
    public class DownloadHelperTests
    {
        [TestCase("2020/01/01", "2024/01/01", 3, 0)]
        public void CalculateETAShouldDownloadAllSymbols(DateTime downloadStartDate, DateTime downloadEndDate, int amountDownloadSymbol, int alreadyDownloadedSymbol)
        {
            var totalDataPerSymbolInSeconds = (downloadEndDate - downloadStartDate).TotalSeconds;
            var totalDataInSeconds = totalDataPerSymbolInSeconds * amountDownloadSymbol;

            var mockRunningDateTime = new DateTime(2024, 04, 26, 1, 10, 10);
            var endDateTime = downloadStartDate;
            while (alreadyDownloadedSymbol != amountDownloadSymbol)
            {
                do
                {
                    endDateTime = endDateTime.AddDays(1);

                    // Simulate real-time by advancing the mockRunningDateTime by 2 milliseconds
                    var utcNow = mockRunningDateTime.AddMilliseconds(2);
                    var progressSoFar = (endDateTime - downloadStartDate).TotalSeconds + totalDataPerSymbolInSeconds * alreadyDownloadedSymbol;
                    var eta = Program.CalculateETA(utcNow, mockRunningDateTime, totalDataInSeconds, progressSoFar);

                    if (endDateTime < downloadEndDate)
                    {
                        Assert.Greater(eta.TotalSeconds, 0);
                    }
                } while (endDateTime != downloadEndDate);

                endDateTime = downloadStartDate;
                alreadyDownloadedSymbol++;
            }

            Assert.That(amountDownloadSymbol, Is.EqualTo(alreadyDownloadedSymbol));
        }

        [TestCase("2020/01/01", "2024/01/01", "2020/1/10", 1, 0, 1, 161)]
        [TestCase("2019/01/01", "2023/01/01", "2021/2/10", 2, 1, 10, 3)]
        [TestCase("2021/01/01", "2022/01/01", "2021/5/10", 3, 2, 5, 1)]
        public void CalculateETAShouldReturnCorrectETA(
            DateTime downloadStartDate,
            DateTime downloadEndDate,
            DateTime currentDownloadedDataFromDataDownloader,
            int amountOfDownloadSymbol,
            int alreadyDownloadedSymbol,
            int minusUtcNowSecond,
            int expectedTotalSeconds)
        {
            var mockUtcTimeNow = new DateTime(2024, 04, 26, 1, 1, 10);

            DateTime runUtcTime = mockUtcTimeNow.AddSeconds(-minusUtcNowSecond);

            var totalDataPerSymbolInSeconds = (downloadEndDate - downloadStartDate).TotalSeconds;
            var totalDataInSeconds = totalDataPerSymbolInSeconds * amountOfDownloadSymbol;

            var progressSoFar = (currentDownloadedDataFromDataDownloader - downloadStartDate).TotalSeconds + totalDataPerSymbolInSeconds * alreadyDownloadedSymbol;

            var eta = Program.CalculateETA(mockUtcTimeNow, runUtcTime, totalDataInSeconds, progressSoFar);

            Assert.That(expectedTotalSeconds, Is.EqualTo((int)eta.TotalSeconds));
        }

        [TestCase(TickType.Trade, Resolution.Daily)]
        public void RunDownload(TickType tickType, Resolution resolution)
        {
            var startDate = new DateTime(2024, 01, 01);
            var tradeDate = new DateTime(2024, 01, 10);
            var endDate = new DateTime(2024, 02, 02);
            var symbol = Symbols.AAPL;

            var downloadDataConfig = InitializeDataDownloadConfigParameters(tickType, SecurityType.Option, resolution, startDate, endDate, new string[] { symbol.Value });

            var optionContracts = GenerateOptionContracts(symbol, 100, new DateTime(2024, 03, 16));

            Assert.That(optionContracts.Distinct().Count(), Is.EqualTo(optionContracts.Count));

            var mockBaseDate = GenerateTradeBarByEachSymbol(optionContracts, tradeDate);

            var downloader = new DataDownloaderTest(mockBaseDate);

            Program.RunDownload(downloader, downloadDataConfig);

            var filePath = LeanData.GenerateZipFilePath(Globals.DataFolder, optionContracts.First(), startDate, resolution, tickType);
            var data = QuantConnect.Compression.Unzip(filePath).ToDictionary(x => x.Key, x => x.Value.ToList());

            Assert.Greater(data.Count, 1);
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

        private static DataDownloadConfig InitializeDataDownloadConfigParameters(TickType tickType, SecurityType securityType, Resolution resolution,
            DateTime startDate, DateTime endDate, string[] tickers, string market = Market.USA)
        {
            Config.Set("data-type", tickType.ToStringInvariant());
            Config.Set("security-type", securityType.ToStringInvariant());
            Config.Set("market", market);
            Config.Set("resolution", resolution.ToStringInvariant());
            Config.Set("start-date", startDate.ToStringInvariant("yyyyMMdd"));
            Config.Set("end-date", endDate.ToStringInvariant("yyyyMMdd"));
            // serializes tickers into a dictionary similar to using DownloaderDataProviderArgumentParser.
            Config.Set("tickers", JsonSerializer.Serialize(tickers.ToDictionary(t => t, _ => "")));

            return new DataDownloadConfig();
        }
    }
}
