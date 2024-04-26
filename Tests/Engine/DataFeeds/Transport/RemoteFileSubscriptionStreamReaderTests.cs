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
using System.Globalization;
using System.IO;
using System.Net;
using NUnit.Framework;
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
            var api = new TestDownloadProvider();
            api.Initialize(Globals.UserId, Globals.UserToken, Globals.DataFolder);
            RemoteFileSubscriptionStreamReader.SetDownloadProvider(api);
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

        [TestCase(true)]
        [TestCase(false)]
        public void SingleEntryDataCacheProviderEphemeralDataIsRespected(bool isDataEphemeral)
        {
            using var cacheProvider = new SingleEntryDataCacheProvider(TestGlobals.DataProvider, isDataEphemeral: isDataEphemeral);
            using var remoteReader = new RemoteFileSubscriptionStreamReader(
                cacheProvider,
                @"https://www.quantconnect.com/api/v2/proxy/quandl/api/v3/datasets/BCHARTS/BITSTAMPUSD.csv?order=asc&api_key=WyAazVXnq7ATy_fefTqm",
                Globals.Cache,
                null);

            Assert.IsFalse(remoteReader.EndOfStream);
            Assert.AreEqual(1, TestDownloadProvider.DownloadCount);

            using var cacheProvider2 = new SingleEntryDataCacheProvider(TestGlobals.DataProvider, isDataEphemeral: isDataEphemeral);
            using var remoteReader2 = new RemoteFileSubscriptionStreamReader(
                cacheProvider2,
                @"https://www.quantconnect.com/api/v2/proxy/quandl/api/v3/datasets/BCHARTS/BITSTAMPUSD.csv?order=asc&api_key=WyAazVXnq7ATy_fefTqm",
                Globals.Cache,
                null);

            Assert.IsFalse(remoteReader.EndOfStream);
            Assert.AreEqual(isDataEphemeral ? 2 : 1, TestDownloadProvider.DownloadCount);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void ZipDataCacheProviderEphemeralDataIsRespected(bool isDataEphemeral)
        {
            using var cacheProvider = new ZipDataCacheProvider(TestGlobals.DataProvider, isDataEphemeral: isDataEphemeral);
            using var remoteReader = new RemoteFileSubscriptionStreamReader(
                cacheProvider,
                @"https://www.quantconnect.com/api/v2/proxy/quandl/api/v3/datasets/BCHARTS/BITSTAMPUSD.csv?order=asc&api_key=WyAazVXnq7ATy_fefTqm",
                Globals.Cache,
                null);

            Assert.IsFalse(remoteReader.EndOfStream);
            Assert.AreEqual(1, TestDownloadProvider.DownloadCount);

            using var cacheProvider2 = new ZipDataCacheProvider(TestGlobals.DataProvider, isDataEphemeral: isDataEphemeral);
            using var remoteReader2 = new RemoteFileSubscriptionStreamReader(
                cacheProvider2,
                @"https://www.quantconnect.com/api/v2/proxy/quandl/api/v3/datasets/BCHARTS/BITSTAMPUSD.csv?order=asc&api_key=WyAazVXnq7ATy_fefTqm",
                Globals.Cache,
                null);

            Assert.IsFalse(remoteReader.EndOfStream);
            Assert.AreEqual(isDataEphemeral ? 2 : 1, TestDownloadProvider.DownloadCount);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void ZipDataCacheProviderEphemeralDataIsRespectedForZippedData(bool isDataEphemeral)
        {
            using var cacheProvider = new ZipDataCacheProvider(TestGlobals.DataProvider, isDataEphemeral: isDataEphemeral);
            using var remoteReader = new RemoteFileSubscriptionStreamReader(
                cacheProvider,
                @"https://cdn.quantconnect.com/uploads/multi_csv_zipped_file.zip",
                Globals.Cache,
                null);

            Assert.IsFalse(remoteReader.EndOfStream);
            Assert.AreEqual(1, TestDownloadProvider.DownloadCount);

            using var remoteReader2 = new RemoteFileSubscriptionStreamReader(
                cacheProvider,
                @"https://cdn.quantconnect.com/uploads/multi_csv_zipped_file.zip",
                Globals.Cache,
                null);

            Assert.IsFalse(remoteReader.EndOfStream);
            Assert.AreEqual(isDataEphemeral ? 2 : 1, TestDownloadProvider.DownloadCount);
        }

        [TestCase(true, "", 78)]    // No fragment, will read the first entry
        [TestCase(false, "", 78)]
        [TestCase(true, "#csv_file_10.csv", 1)]
        [TestCase(false, "#csv_file_10.csv", 1)]
        public void GetsZippedDataForUrlNotEndingWithZipExtension(bool withQuery, string entryName, int expectedLines)
        {
            using var cacheProvider = new ZipDataCacheProvider(TestGlobals.DataProvider);
            var url = @"https://cdn.quantconnect.com/uploads/multi_csv_zipped_file.zip" + (withQuery ? "?some=query" : "") + entryName;
            using var remoteReader = new RemoteFileSubscriptionStreamReader(cacheProvider, url, Globals.Cache, null);

            Assert.IsFalse(remoteReader.EndOfStream);
            Assert.AreEqual(1, TestDownloadProvider.DownloadCount);

            var count = 0;
            while (!remoteReader.EndOfStream)
            {
                var line = remoteReader.ReadLine();
                count++;

                var csv = line.ToCsv();
                Assert.AreEqual(2, csv.Count);
                Assert.IsTrue(int.TryParse(csv[0], NumberStyles.Number, CultureInfo.InvariantCulture, out _));
                Assert.IsTrue(decimal.TryParse(csv[1], NumberStyles.Number, CultureInfo.InvariantCulture, out _));
            }

            Assert.AreEqual(expectedLines, count);
        }

        [Test]
        public void InvalidDataSource()
        {
            using var cacheProvider = new ZipDataCacheProvider(TestGlobals.DataProvider);
            using var remoteReader = new RemoteFileSubscriptionStreamReader(
                cacheProvider,
                @"http://quantconnect.com",
                Globals.Cache,
                null);

            Assert.IsFalse(remoteReader.EndOfStream);

            // Fails to get quantconnect.com, missing http://
            Assert.Throws<WebException>(() => new RemoteFileSubscriptionStreamReader(
                    new SingleEntryDataCacheProvider(new DefaultDataProvider()),
                    @"quantconnect.com",
                    Globals.Cache,
                    null),
                "Api.Download(): Failed to download data from quantconnect.com. Please verify the source for missing http:// or https://"
            );
        }

        private class TestDownloadProvider : QuantConnect.Api.Api
        {
            public static int DownloadCount { get; set; }
            static TestDownloadProvider()
            {
                DownloadCount = 0;
            }
            public override byte[] DownloadBytes(string address, IEnumerable<KeyValuePair<string, string>> headers, string userName, string password)
            {
                DownloadCount++;
                return base.DownloadBytes(address, headers, userName, password);
            }
        }
    }
}
