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
using System.IO.Compression;
using System.Text;
using NUnit.Framework;
using Path = System.IO.Path;
using System.Threading.Tasks;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds;

namespace QuantConnect.Tests.Engine.DataCacheProviders
{
    [TestFixture]
    public class ZipDataCacheProviderTests : DataCacheProviderTests
    {
        private readonly string _tempZipFileEntry = Path.GetTempFileName().Replace(".tmp", ".zip", StringComparison.InvariantCulture) + "#testEntry.csv";
        private readonly Random _random = new Random();

        public override IDataCacheProvider CreateDataCacheProvider()
        {
            return new ZipDataCacheProvider(TestGlobals.DataProvider);
        }

        [Test]
        public void MultiThreadReadWriteTest()
        {
            var dataCacheProvider = new ZipDataCacheProvider(TestGlobals.DataProvider, cacheTimer: 0.1);

            Parallel.For(0, 100, (i) =>
            {
                var data = new byte[300];
                _random.NextBytes(data);

                ReadAndWrite(dataCacheProvider, data);
            });

            dataCacheProvider.Dispose();
        }

        [Test]
        public void FetchRethrowsOutOfMemoryAsMemoryDiagnostic()
        {
            // Ionic wraps the allocation failure into ZipException("Cannot read that as a ZipFile"), which used to be
            // logged as a corrupt zip file and swallowed. We want an honest memory diagnostic instead.
            using var dataCacheProvider = new ZipDataCacheProvider(new OutOfMemoryDataProvider());

            var exception = Assert.Throws<OutOfMemoryException>(
                () => dataCacheProvider.Fetch("/data/option/usa/daily/pep_2026_trade_american.zip#entry.csv"));

            StringAssert.Contains("ran out of memory", exception.Message);
            StringAssert.DoesNotContain("Corrupt zip", exception.Message);
            Assert.IsNotNull(exception.InnerException);
        }

        [Test]
        public void FetchDoesNotThrowOnCorruptZipFile()
        {
            // a truly corrupt file must keep the previous behavior: log and return null instead of throwing
            using var dataCacheProvider = new ZipDataCacheProvider(TestGlobals.DataProvider, cacheTimer: 0.1);

            var tempZipFile = Path.GetTempFileName().Replace(".tmp", ".zip", StringComparison.InvariantCulture);
            File.WriteAllText(tempZipFile, "corrupted zip");

            Stream result = null;
            Assert.DoesNotThrow(() => result = dataCacheProvider.Fetch(tempZipFile + "#testEntry.csv"));
            Assert.IsNull(result);
        }

        [Test]
        public void FetchReopensCachedZipAfterReadFailure()
        {
            var dataProvider = new StaleHandleDataProvider();
            using var dataCacheProvider = new ZipDataCacheProvider(dataProvider, cacheTimer: 60);
            const string key = "/data/equity/usa/daily/spy.zip#spy.csv";

            using (var reader = new StreamReader(dataCacheProvider.Fetch(key)))
            {
                Assert.AreEqual(StaleHandleDataProvider.Content, reader.ReadToEnd());
            }

            dataProvider.LastStream.Stale = true;
            Assert.IsNull(dataCacheProvider.Fetch(key));

            var stream = dataCacheProvider.Fetch(key);
            Assert.IsNotNull(stream, "a zip whose cached handle went stale should be re-opened on the next fetch");
            using (var reader = new StreamReader(stream))
            {
                Assert.AreEqual(StaleHandleDataProvider.Content, reader.ReadToEnd());
            }
            Assert.AreEqual(2, dataProvider.FetchCount);
        }

        [Test]
        public void StoreFailsCorruptedFile()
        {
            var dataCacheProvider = new ZipDataCacheProvider(TestGlobals.DataProvider, cacheTimer: 0.1);

            var tempZipFileEntry = Path.GetTempFileName().Replace(".tmp", ".zip", StringComparison.InvariantCulture);

            var data = new byte[300];
            _random.NextBytes(data);

            File.WriteAllText(tempZipFileEntry, "corrupted zip");

            Assert.Throws<InvalidOperationException>(() => dataCacheProvider.Store(tempZipFileEntry + "#testEntry.csv", data));
            dataCacheProvider.Dispose();
        }

        private void ReadAndWrite(IDataCacheProvider dataCacheProvider, byte[] data)
        {
            dataCacheProvider.Fetch(_tempZipFileEntry);
            dataCacheProvider.Store(_tempZipFileEntry, data);
        }

        /// <summary>
        /// Provider serving a valid zip whose last stream can be made to fail like a file handle gone stale (ESTALE)
        /// </summary>
        private class StaleHandleDataProvider : IDataProvider
        {
            public const string Content = "20260928 00:00,1,2,3,4,5";
            private readonly byte[] _zip;

            public event EventHandler<DataProviderNewDataRequestEventArgs> NewDataRequest;

            public int FetchCount { get; private set; }

            public StaleHandleStream LastStream { get; private set; }

            public StaleHandleDataProvider()
            {
                using var zipStream = new MemoryStream();
                using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true))
                using (var writer = new StreamWriter(archive.CreateEntry("spy.csv").Open()))
                {
                    writer.Write(Content);
                }
                _zip = zipStream.ToArray();
            }

            public Stream Fetch(string key)
            {
                NewDataRequest?.Invoke(this, null);
                FetchCount++;
                LastStream = new StaleHandleStream(_zip);
                return LastStream;
            }
        }

        private class StaleHandleStream : MemoryStream
        {
            public bool Stale { get; set; }

            public StaleHandleStream(byte[] buffer) : base(buffer)
            {
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                return Stale ? throw new IOException("Stale file handle") : base.Read(buffer, offset, count);
            }

            public override int Read(Span<byte> buffer)
            {
                return Stale ? throw new IOException("Stale file handle") : base.Read(buffer);
            }
        }

        /// <summary>
        /// Provider whose streams fail allocation-style, simulating reading a zip while the process is out of memory
        /// </summary>
        private class OutOfMemoryDataProvider : IDataProvider
        {
            public event EventHandler<DataProviderNewDataRequestEventArgs> NewDataRequest;

            public Stream Fetch(string key)
            {
                NewDataRequest?.Invoke(this, null);
                return new OutOfMemoryStream();
            }

            private class OutOfMemoryStream : Stream
            {
                public override bool CanRead => true;
                public override bool CanSeek => true;
                public override bool CanWrite => false;
                public override long Length => 1024;
                public override long Position { get; set; }

                public override void Flush() { }
                public override int Read(byte[] buffer, int offset, int count) => throw new OutOfMemoryException();
                public override long Seek(long offset, SeekOrigin origin) => Position;
                public override void SetLength(long value) => throw new NotSupportedException();
                public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
            }
        }
    }
}
