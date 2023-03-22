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
using System.Linq;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Util;
using QuantConnect.Securities;
using System.Collections.Generic;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Data.Custom.IconicTypes;

namespace QuantConnect.Tests.Engine.DataFeeds
{
    [TestFixture]
    public class DownloaderDataProviderTests
    {
        [Test]
        public void CustomDataRequest()
        {
            var downloader = new DataDownloaderTest();
            using var dataProvider = new DownloaderDataProvider(downloader);

            var customData = Symbol.CreateBase(typeof(LinkedData), Symbols.SPY, Market.USA);
            var date = new DateTime(2023, 3, 17);
            var path = LeanData.GenerateZipFilePath(Globals.DataFolder + "fake", customData, date, Resolution.Minute, TickType.Trade);
            Assert.IsNull(dataProvider.Fetch(path));

            Assert.AreEqual(0, downloader.DataDownloaderGetParameters.Count);
        }

        [TestCase(Resolution.Daily, SecurityType.Equity)]
        [TestCase(Resolution.Hour, SecurityType.Equity)]
        [TestCase(Resolution.Minute, SecurityType.Equity)]
        [TestCase(Resolution.Second, SecurityType.Equity)]
        [TestCase(Resolution.Tick, SecurityType.Equity)]
        [TestCase(Resolution.Daily, SecurityType.Option)]
        [TestCase(Resolution.Hour, SecurityType.Option)]
        [TestCase(Resolution.Minute, SecurityType.Option)]
        [TestCase(Resolution.Second, SecurityType.Option)]
        [TestCase(Resolution.Tick, SecurityType.Option)]
        [TestCase(Resolution.Daily, SecurityType.Crypto)]
        [TestCase(Resolution.Hour, SecurityType.Crypto)]
        [TestCase(Resolution.Minute, SecurityType.Crypto)]
        [TestCase(Resolution.Second, SecurityType.Crypto)]
        [TestCase(Resolution.Tick, SecurityType.Crypto)]
        [TestCase(Resolution.Daily, SecurityType.Future)]
        [TestCase(Resolution.Hour, SecurityType.Future)]
        [TestCase(Resolution.Minute, SecurityType.Future)]
        [TestCase(Resolution.Second, SecurityType.Future)]
        [TestCase(Resolution.Tick, SecurityType.Future)]
        [TestCase(Resolution.Daily, SecurityType.FutureOption)]
        [TestCase(Resolution.Hour, SecurityType.FutureOption)]
        [TestCase(Resolution.Minute, SecurityType.FutureOption)]
        [TestCase(Resolution.Second, SecurityType.FutureOption)]
        [TestCase(Resolution.Tick, SecurityType.FutureOption)]
        public void CorrectArguments(Resolution resolution, SecurityType securityType)
        {
            var downloader = new DataDownloaderTest();
            using var dataProvider = new DownloaderDataProvider(downloader);

            Symbol symbol = null;
            Symbol expectedSymbol = null;
            var expectedLowResolutionStart = new DateTime(1998, 1, 2);
            if (securityType == SecurityType.Equity)
            {
                symbol = expectedSymbol = Symbols.AAPL;
            }
            else if (securityType == SecurityType.Option)
            {
                symbol = Symbols.SPY_C_192_Feb19_2016;
                expectedSymbol = symbol.Canonical;
            }
            else if (securityType == SecurityType.Crypto)
            {
                expectedLowResolutionStart = new DateTime(2009, 1, 1);
                symbol = expectedSymbol = Symbols.BTCUSD;
            }
            else if (securityType == SecurityType.Future)
            {
                symbol = Symbols.Future_CLF19_Jan2019;
                expectedSymbol = symbol.Canonical;
            }
            else if (securityType == SecurityType.FutureOption)
            {
                symbol = Symbols.CreateFutureOptionSymbol(Symbols.Future_CLF19_Jan2019, OptionRight.Call, 10, new DateTime(2019, 1, 21));
                expectedSymbol = symbol.Canonical;
            }

            var date = new DateTime(2023, 3, 17);
            var path = LeanData.GenerateZipFilePath(Globals.DataFolder + "fake", symbol, date, resolution, TickType.Trade);
            Assert.IsNull(dataProvider.Fetch(path));

            var arguments = downloader.DataDownloaderGetParameters.Single();

            Assert.AreEqual(expectedSymbol, arguments.Symbol);
            Assert.AreEqual(resolution, arguments.Resolution);
            if (resolution < Resolution.Hour)
            {
                var mhdb = MarketHoursDatabase.FromDataFolder();
                var timezone = mhdb.GetDataTimeZone(symbol.ID.Market, symbol, symbol.SecurityType);
                // 1 day
                Assert.AreEqual(date.ConvertToUtc(timezone), arguments.StartUtc);
                Assert.AreEqual(date.AddDays(1).ConvertToUtc(timezone), arguments.EndUtc);
            }
            else
            {
                // the whole history
                Assert.AreEqual(expectedLowResolutionStart, arguments.StartUtc);
                Assert.AreEqual(DateTime.UtcNow.Date.AddDays(-1), arguments.EndUtc);
            }
        }

        private class DataDownloaderTest : IDataDownloader
        {
            public List<DataDownloaderGetParameters > DataDownloaderGetParameters { get; set; } = new ();
            public IEnumerable<BaseData> Get(DataDownloaderGetParameters dataDownloaderGetParameters)
            {
                DataDownloaderGetParameters.Add(dataDownloaderGetParameters);
                return Enumerable.Empty<BaseData>();
            }
        }
    }
}
