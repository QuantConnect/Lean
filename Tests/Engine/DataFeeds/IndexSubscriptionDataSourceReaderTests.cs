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
using System.IO;
using System.Linq;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds;

namespace QuantConnect.Tests.Engine.DataFeeds
{
    [TestFixture]
    public class IndexSubscriptionDataSourceReaderTests
    {
        private DateTime _initialDate;
        private TestDataCacheProvider _dataCacheProvider;

        [SetUp]
        public void SetUp()
        {
            _initialDate = new DateTime(2018, 1, 1);
            _dataCacheProvider = new TestDataCacheProvider();
        }

        [Test]
        public void ThrowsIfDataIsNotIndexBased()
        {
            var config = new SubscriptionDataConfig(
                    typeof(TradeBar),
                    Symbols.SPY,
                    Resolution.Daily,
                    TimeZones.NewYork,
                    TimeZones.NewYork,
                    true,
                    true,
                    false);

            Assert.Throws<ArgumentException>(() => new IndexSubscriptionDataSourceReader(
                _dataCacheProvider,
                config,
                _initialDate,
                false,
                TestGlobals.DataProvider,
                null));
        }

        [Test]
        public void GetsIndexAndSource()
        {
            var config = new SubscriptionDataConfig(
                typeof(TestIndexedBasedFactory),
                Symbols.SPY,
                Resolution.Daily,
                TimeZones.NewYork,
                TimeZones.NewYork,
                true,
                true,
                false);

            var reader = new IndexSubscriptionDataSourceReader(
                _dataCacheProvider,
                config,
                _initialDate,
                false,
                TestGlobals.DataProvider,
                null);
            var source = (new TradeBar()).GetSource(config, _initialDate, false);
            _dataCacheProvider.Data = "20000101 00:00,2,2,2,2,2";
            var dataBars = reader.Read(source).First();

            Assert.IsNotNull(dataBars);
            Assert.IsNotNull(dataBars.Symbol, Symbols.SPY);
            Assert.AreEqual("20000101 00:00,2,2,2,2,2", TestIndexedBasedFactory.IndexLine);
            Assert.AreEqual("20000101 00:00,2,2,2,2,2", TestIndexedBasedFactory.ReaderLine);
        }

        private class TestIndexedBasedFactory : IndexedBaseData
        {
            public static string ReaderLine { get; set; }
            public static string IndexLine { get; set; }

            public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
            {
                ReaderLine = line;
                var bar = new TradeBar();
                return bar.Reader(config, line, date, isLiveMode);
            }
            public override SubscriptionDataSource GetSourceForAnIndex(SubscriptionDataConfig config, DateTime date, string index, bool isLiveMode)
            {
                IndexLine = index;
                return new SubscriptionDataSource("",
                    SubscriptionTransportMedium.LocalFile,
                    FileFormat.Csv);
            }

            public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
            {
                return new SubscriptionDataSource("",
                    SubscriptionTransportMedium.LocalFile,
                    FileFormat.Csv);
            }
        }

        private class TestDataCacheProvider : IDataCacheProvider
        {
            public string Data { set; get; }
            public bool IsDataEphemeral => false;

            public Stream Fetch(string key)
            {
                var stream = new MemoryStream();
                var writer = new StreamWriter(stream);
                writer.Write(Data);
                writer.Flush();
                stream.Position = 0;
                return stream;
            }
            public void Store(string key, byte[] data)
            {
            }
            public List<string> GetZipEntries(string zipFile)
            {
                throw new NotImplementedException();
            }
            public void Dispose()
            {
            }
        }
    }
}
