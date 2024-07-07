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
using System.Threading.Tasks;
using NUnit.Framework;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds;
using Path = System.IO.Path;

namespace QuantConnect.Tests.Engine.DataCacheProviders
{
    [TestFixture]
    public class ZipDataCacheProviderTests : DataCacheProviderTests
    {
        private readonly string _tempZipFileEntry =
            Path.GetTempFileName().Replace(".tmp", ".zip", StringComparison.InvariantCulture)
            + "#testEntry.csv";
        private readonly Random _random = new Random();

        public override IDataCacheProvider CreateDataCacheProvider()
        {
            return new ZipDataCacheProvider(TestGlobals.DataProvider);
        }

        [Test]
        public void MultiThreadReadWriteTest()
        {
            var dataCacheProvider = new ZipDataCacheProvider(
                TestGlobals.DataProvider,
                cacheTimer: 0.1
            );

            Parallel.For(
                0,
                100,
                (i) =>
                {
                    var data = new byte[300];
                    _random.NextBytes(data);

                    ReadAndWrite(dataCacheProvider, data);
                }
            );

            dataCacheProvider.Dispose();
        }

        [Test]
        public void StoreFailsCorruptedFile()
        {
            var dataCacheProvider = new ZipDataCacheProvider(
                TestGlobals.DataProvider,
                cacheTimer: 0.1
            );

            var tempZipFileEntry = Path.GetTempFileName()
                .Replace(".tmp", ".zip", StringComparison.InvariantCulture);

            var data = new byte[300];
            _random.NextBytes(data);

            File.WriteAllText(tempZipFileEntry, "corrupted zip");

            Assert.Throws<InvalidOperationException>(
                () => dataCacheProvider.Store(tempZipFileEntry + "#testEntry.csv", data)
            );
            dataCacheProvider.Dispose();
        }

        private void ReadAndWrite(IDataCacheProvider dataCacheProvider, byte[] data)
        {
            dataCacheProvider.Fetch(_tempZipFileEntry);
            dataCacheProvider.Store(_tempZipFileEntry, data);
        }
    }
}
