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

using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.DataFeeds.Transport;

namespace QuantConnect.Tests.Engine.DataFeeds.Transport
{
    [TestFixture]
    public class RemoteFileSubscriptionStreamReaderTests
    {
        [SetUp]
        public void SetUp()
        {
            RemoteFileSubscriptionStreamReader.SetDownloadProvider(new TestDownloadProvider());
            TestDownloadProvider.DownloadCount = 0;
            // create cache directory if not existing
            if (!Directory.Exists(Globals.Cache))
            {
                Directory.CreateDirectory(Globals.Cache);
            }
            else
            {
                // clean old files out of the cache
                Directory.Delete(Globals.Cache, true);
                Directory.CreateDirectory(Globals.Cache);
            }
        }

        [Test]
        public void LiveModeIsNotCached()
        {
            var remoteReader = new RemoteFileSubscriptionStreamReader(
                new SingleEntryDataCacheProvider(new DefaultDataProvider()),
                @"https://www.quantconnect.com/api/v2/proxy/quandl/api/v3/datasets/BCHARTS/BITSTAMPUSD.csv?order=asc&api_key=WyAazVXnq7ATy_fefTqm",
                Globals.Cache,
                null,
                liveMode: true);

            Assert.IsFalse(remoteReader.EndOfStream);
            Assert.AreEqual(1, TestDownloadProvider.DownloadCount);

            var remoteReader2 = new RemoteFileSubscriptionStreamReader(
                new SingleEntryDataCacheProvider(new DefaultDataProvider()),
                @"https://www.quantconnect.com/api/v2/proxy/quandl/api/v3/datasets/BCHARTS/BITSTAMPUSD.csv?order=asc&api_key=WyAazVXnq7ATy_fefTqm",
                Globals.Cache,
                null,
                liveMode: true);

            Assert.IsFalse(remoteReader.EndOfStream);
            Assert.AreEqual(2, TestDownloadProvider.DownloadCount);

            remoteReader.Dispose();
            remoteReader2.Dispose();
        }

        [Test]
        public void BacktestingIsCached()
        {
            var remoteReader = new RemoteFileSubscriptionStreamReader(
                new SingleEntryDataCacheProvider(new DefaultDataProvider()),
                @"https://www.quantconnect.com/api/v2/proxy/quandl/api/v3/datasets/BCHARTS/BITSTAMPUSD.csv?order=asc&api_key=WyAazVXnq7ATy_fefTqm",
                Globals.Cache,
                null,
                liveMode: false);

            Assert.IsFalse(remoteReader.EndOfStream);
            Assert.AreEqual(1, TestDownloadProvider.DownloadCount);

            var remoteReader2 = new RemoteFileSubscriptionStreamReader(
                new SingleEntryDataCacheProvider(new DefaultDataProvider()),
                @"https://www.quantconnect.com/api/v2/proxy/quandl/api/v3/datasets/BCHARTS/BITSTAMPUSD.csv?order=asc&api_key=WyAazVXnq7ATy_fefTqm",
                Globals.Cache,
                null,
                liveMode: false);

            Assert.IsFalse(remoteReader.EndOfStream);
            Assert.AreEqual(1, TestDownloadProvider.DownloadCount);

            remoteReader.Dispose();
            remoteReader2.Dispose();
        }

        [Test]
        public void EphemeralDataSourceIsNeverCached()
        {
            var remoteReader = new RemoteFileSubscriptionStreamReader(
                new CustomEphemeralDataCacheProvider{IsDataEphemeral = true, Data = ""},
                @"https://www.quantconnect.com/api/v2/proxy/quandl/api/v3/datasets/BCHARTS/BITSTAMPUSD.csv?order=asc&api_key=WyAazVXnq7ATy_fefTqm",
                Globals.Cache,
                null,
                liveMode: false);

            Assert.IsFalse(remoteReader.EndOfStream);
            Assert.AreEqual(1, TestDownloadProvider.DownloadCount);

            var remoteReader2 = new RemoteFileSubscriptionStreamReader(
                new CustomEphemeralDataCacheProvider { IsDataEphemeral = true, Data = "" },
                @"https://www.quantconnect.com/api/v2/proxy/quandl/api/v3/datasets/BCHARTS/BITSTAMPUSD.csv?order=asc&api_key=WyAazVXnq7ATy_fefTqm",
                Globals.Cache,
                null,
                liveMode: false);

            Assert.IsFalse(remoteReader.EndOfStream);
            Assert.AreEqual(2, TestDownloadProvider.DownloadCount);

            remoteReader.Dispose();
            remoteReader2.Dispose();
        }

        private class TestDownloadProvider : Api.Api
        {
            public static int DownloadCount { get; set; }
            static TestDownloadProvider()
            {
                DownloadCount = 0;
            }
            public override string Download(string address, IEnumerable<KeyValuePair<string, string>> headers, string userName, string password)
            {
                DownloadCount++;
                return base.Download(address, headers, userName, password);
            }
        }

        private class CustomEphemeralDataCacheProvider : IDataCacheProvider
        {
            public string Data { set; get; }
            public bool IsDataEphemeral { set; get; }

            public Stream Fetch(string key)
            {
                return new FileStream(key, FileMode.Open, FileAccess.Read, FileShare.Read);
            }
            public void Store(string key, byte[] data)
            {
            }
            public void Dispose()
            {
            }
        }
    }
}
