using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds;
using Path = System.IO.Path;

namespace QuantConnect.Tests.Engine.DataFeeds
{
    class ZipDataCacheProviderTests
    {
        private ZipDataCacheProvider _zipDataCacheProvider;
        private string _tempZipFileEntry;
        private Random _random;

        [SetUp]
        public void Setup()
        {
            _zipDataCacheProvider = new ZipDataCacheProvider(TestGlobals.DataProvider);
            _tempZipFileEntry = Path.GetTempFileName() + "#testEntry.csv";
            _random = new Random();
        }

        [TearDown]
        public void TearDown()
        {
            _zipDataCacheProvider.Dispose();
        }

        [Test]
        public void MultiThreadReadWriteTest()
        {
            var threadCount = 100;

            Thread[] threads = new Thread[threadCount];

            for (int i = 0; i < threads.Length; i++)
            {
                threads[i] = new Thread(ReadAndWrite);
            }

            Parallel.ForEach(threads, (t) =>
            {
                t.Start();
                t.Join();
            });
        }

        private void ReadAndWrite()
        {
            var read = _zipDataCacheProvider.Fetch(_tempZipFileEntry);

            var data = new byte[20];
            _random.NextBytes(data);
            _zipDataCacheProvider.Store(_tempZipFileEntry, data);
        }
    }
}
