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
using System.Threading;
using QuantConnect.Data;
using QuantConnect.Util;
using QuantConnect.Logging;
using System.Threading.Tasks;
using QuantConnect.Securities;
using QuantConnect.Data.Market;
using System.Collections.Generic;
using DataFeed = QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Data.Custom.IconicTypes;

namespace QuantConnect.Tests.Engine.DataFeeds
{
    [TestFixture]
    public class DownloaderDataProviderTests
    {
        [Test]
        public void ConcurrentDownloadsSameFile()
        {
            Log.DebuggingEnabled = true;
            var downloader = new DataDownloaderTest();
            using var dataProvider = new DataFeed.DownloaderDataProvider(downloader);

            var date = new DateTime(2000, 3, 17);
            var dataSymbol = Symbol.Create("TEST", SecurityType.Equity, Market.USA);
            var actualPath = LeanData.GenerateZipFilePath(Globals.DataFolder, dataSymbol, date, Resolution.Daily, TickType.Trade);

            // the symbol of the data is the same
            for (var i = 0; i < 10; i++)
            {
                downloader.Data.Add(new TradeBar(date.AddDays(i), dataSymbol, i, i, i, i, i));
            }
            File.Delete(actualPath);

            var failures = 0L;
            using var cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            // here we request symbols which is different than the data the downloader will return & we need to store, simulating mapping behavior
            // the data will always use 'dataSymbol' and the same output path
            var endedCount = 0;
            var taskCount = 5;
            for (var i = 0; i < taskCount; i++)
            {
                var myLockId = i;
                var task = new Task(() =>
                {
                    try
                    {
                        var count = 0;
                        while (count++ < 10)
                        {
                            var requestSymbol = Symbol.Create($"TEST{count + Math.Pow(10, myLockId)}", SecurityType.Equity, Market.USA);
                            var path = LeanData.GenerateZipFilePath(Globals.DataFolder, requestSymbol, date, Resolution.Daily, TickType.Trade);
                            // we will get null back because the data is stored to another path, the 'dataSymbol' path which is read bellow
                            Assert.IsNull(dataProvider.Fetch(path));
                        }
                    }
                    catch (Exception exception)
                    {
                        Interlocked.Increment(ref failures);
                        Log.Error(exception);
                        cancellationToken.Cancel();
                    }

                    if(Interlocked.Increment(ref endedCount) == taskCount)
                    {
                        // the end
                        cancellationToken.Cancel();
                    }
                }, cancellationToken.Token);
                task.Start();
            }

            cancellationToken.Token.WaitHandle.WaitOne();

            Assert.AreEqual(0, Interlocked.Read(ref failures));
            lock (downloader.DataDownloaderGetParameters)
            {
                Assert.AreEqual(downloader.DataDownloaderGetParameters.Count, 50);
            }

            var data = QuantConnect.Compression.Unzip(actualPath).Single();

            // the data was merged
            Assert.AreEqual(66, data.Value.Count);
        }

        [Test]
        public void CustomDataRequest()
        {
            var downloader = new DataDownloaderTest();
            using var dataProvider = new DataFeed.DownloaderDataProvider(downloader);

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
        [TestCase(Resolution.Daily, SecurityType.IndexOption)]
        [TestCase(Resolution.Hour, SecurityType.IndexOption)]
        [TestCase(Resolution.Minute, SecurityType.IndexOption)]
        [TestCase(Resolution.Second, SecurityType.IndexOption)]
        [TestCase(Resolution.Tick, SecurityType.IndexOption)]
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
            using var dataProvider = new DataFeed.DownloaderDataProvider(downloader);

            Symbol symbol = null;
            Symbol expectedSymbol = null;
            var expectedLowResolutionStart = new DateTime(1998, 1, 2);
            var expectedLowResolutionEndUtc = DateTime.UtcNow.Date.AddDays(-1);
            if (securityType == SecurityType.Equity)
            {
                symbol = expectedSymbol = Symbols.AAPL;
            }
            else if (securityType == SecurityType.Option)
            {
                symbol = Symbols.SPY_C_192_Feb19_2016;
                expectedSymbol = symbol.Canonical;
            }
            else if (securityType == SecurityType.IndexOption)
            {
                symbol = Symbol.CreateOption(Symbols.SPX, Symbols.SPX.ID.Market, OptionStyle.European, OptionRight.Call, 38000m, new DateTime(2021, 01, 15));
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
            var timezone = MarketHoursDatabase.FromDataFolder().GetDataTimeZone(symbol.ID.Market, symbol, symbol.SecurityType);

            // For options and index option in hour or daily resolution, the whole year is downloaded
            if (resolution > Resolution.Minute && (securityType == SecurityType.Option || securityType == SecurityType.IndexOption))
            {
                var expectedStartUtc = new DateTime(date.Year, 1, 1);
                expectedLowResolutionStart = expectedStartUtc.ConvertFromUtc(timezone);
                expectedLowResolutionEndUtc = expectedStartUtc.AddYears(1);
            }

            var path = LeanData.GenerateZipFilePath(Globals.DataFolder + "fake", symbol, date, resolution, TickType.Trade);
            Assert.IsNull(dataProvider.Fetch(path));

            var arguments = downloader.DataDownloaderGetParameters.Single();

            Assert.AreEqual(expectedSymbol, arguments.Symbol);
            Assert.AreEqual(resolution, arguments.Resolution);
            if (resolution < Resolution.Hour)
            {
                // 1 day
                Assert.AreEqual(date.ConvertToUtc(timezone), arguments.StartUtc);
                Assert.AreEqual(date.AddDays(1).ConvertToUtc(timezone), arguments.EndUtc);
            }
            else
            {
                // the whole history
                Assert.AreEqual(expectedLowResolutionStart.ConvertToUtc(timezone), arguments.StartUtc);
                Assert.AreEqual(expectedLowResolutionEndUtc, arguments.EndUtc);
            }
        }

        private class DataDownloaderTest : IDataDownloader
        {
            public List<BaseData> Data { get; } = new();
            public List<DataDownloaderGetParameters > DataDownloaderGetParameters { get; } = new ();
            public IEnumerable<BaseData> Get(DataDownloaderGetParameters dataDownloaderGetParameters)
            {
                lock(DataDownloaderGetParameters)
                {
                    DataDownloaderGetParameters.Add(dataDownloaderGetParameters);

                    if (dataDownloaderGetParameters.Symbol.ID.Symbol.StartsWith("TEST", StringComparison.InvariantCultureIgnoreCase))
                    {
                        // let's create some more data based on the symbol, so we can assert it's merged
                        var id = int.Parse(dataDownloaderGetParameters.Symbol.ID.Symbol.RemoveFromStart("TEST"));
                        return Data.Select(bar =>
                        {
                            var result = bar.Clone();
                            result.Time = bar.Time.AddDays(id);
                            result.EndTime = bar.EndTime.AddDays(id);
                            return result;
                        });
                    }

                    return Data;
                }
            }
        }
    }
}
