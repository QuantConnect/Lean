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
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using QuantConnect.Logging;
using QuantConnect.ToolBox.IBDownloader;

namespace QuantConnect.Tests.ToolBox.IBDownloader
{
    [TestFixture]
    [Ignore("These tests require the IBGateway to be installed.")]
    public class IBDataDownloaderTests
    {
        [TestCase("ES", Resolution.Daily, 15)]
        [TestCase("ES", Resolution.Hour, 15)]
        [TestCase("ES", Resolution.Minute, 15)]
        public void DownloadsFuturesData(string ticker, Resolution resolution, int days)
        {
            var symbol = Symbol.Create(ticker, SecurityType.Future, Market.CME);
            const SecurityType securityType = SecurityType.Future;

            using (var downloader = new IBDataDownloader())
            {
                var symbols = downloader.GetChainSymbols(symbol, true).ToList();

                var startDate = DateTime.UtcNow.Date.AddDays(-days);
                var endDate = DateTime.UtcNow.Date;

                downloader.DownloadAndSave(symbols, resolution, securityType, TickType.Trade, startDate, endDate);
                downloader.DownloadAndSave(symbols, resolution, securityType, TickType.Quote, startDate, endDate);
            }
        }

        [Test]
        public void ThrowsIfDifferentSecurityType()
        {
            Assert.Throws<ArgumentException>(
                () =>
                {
                    using (var downloader = new IBDataDownloader())
                    {
                        var startDate = DateTime.UtcNow.Date.AddDays(-1);
                        var endDate = DateTime.UtcNow.Date;

                        var symbols = new List<Symbol> { Symbols.SPY_C_192_Feb19_2016 };
                        downloader.DownloadAndSave(symbols, Resolution.Minute, SecurityType.Future, TickType.Trade, startDate, endDate);
                    }
                });
        }

        [Test]
        public void ThrowsIfDifferentRootSymbols()
        {
            Assert.Throws<ArgumentException>(
                () =>
                {
                    using (var downloader = new IBDataDownloader())
                    {
                        var startDate = DateTime.UtcNow.Date.AddDays(-1);
                        var endDate = DateTime.UtcNow.Date;

                        var symbols = new List<Symbol> { Symbols.Future_ESZ18_Dec2018, Symbols.Future_CLF19_Jan2019 };
                        downloader.DownloadAndSave(symbols, Resolution.Minute, SecurityType.Future, TickType.Trade, startDate, endDate);
                    }
                });
        }

    }
}
