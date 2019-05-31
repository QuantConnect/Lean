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
using Accord.Math.Comparers;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds;

namespace QuantConnect.Tests.Engine.DataFeeds
{
    [TestFixture]
    public class TextSubscriptionDataSourceReaderTests
    {
        private SubscriptionDataConfig _config;
        private DateTime _initialDate;

        [SetUp]
        public void SetUp()
        {
            _config = new SubscriptionDataConfig(
                typeof(TestTradeBarFactory),
                Symbols.SPY,
                Resolution.Daily,
                TimeZones.NewYork,
                TimeZones.NewYork,
                true,
                true,
                false);
            _initialDate = new DateTime(2018, 1, 1);
        }

        [Test]
        public void CachedDataIsReturnedAsClone()
        {
            var reader = new TextSubscriptionDataSourceReader(
                new SingleEntryDataCacheProvider(new DefaultDataProvider()),
                _config,
                _initialDate,
                false);
            var source = (new TradeBar()).GetSource(_config, _initialDate, false);

            var dataBars = reader.Read(source).First();
            dataBars.Value = 0;
            var dataBars2 = reader.Read(source).First();

            Assert.AreNotEqual(dataBars.Price, dataBars2.Price);
        }

        [Test]
        public void DataIsNotCachedForEphemeralDataCacheProvider()
        {
            var config = new SubscriptionDataConfig(
                    typeof(TestTradeBarFactory),
                    Symbol.Create("SymbolNonEphemeralTest1", SecurityType.Equity, Market.USA),
                    Resolution.Daily,
                    TimeZones.NewYork,
                    TimeZones.NewYork,
                    true,
                    true,
                    false);
            var dataCacheProvider = new CustomEphemeralDataCacheProvider { IsDataEphemeral = true};
            var reader = new TextSubscriptionDataSourceReader(
                dataCacheProvider,
                config,
                _initialDate,
                false);
            var source = (new TradeBar()).GetSource(config, _initialDate, false);
            dataCacheProvider.Data = "20000101 00:00,1,1,1,1,1";
            var dataBars = reader.Read(source).First();
            dataCacheProvider.Data = "20000101 00:00,2,2,2,2,2";
            var dataBars2 = reader.Read(source).First();

            Assert.AreEqual(new DateTime(2000, 1, 1), dataBars.Time);
            Assert.AreEqual(new DateTime(2000, 1, 1), dataBars2.Time);
            Assert.AreNotEqual(dataBars.Price, dataBars2.Price);
        }

        [Test]
        public void DataIsCachedForNonEphemeralDataCacheProvider()
        {
            var config = new SubscriptionDataConfig(
                typeof(TestTradeBarFactory),
                Symbol.Create("SymbolNonEphemeralTest2", SecurityType.Equity, Market.USA),
                Resolution.Daily,
                TimeZones.NewYork,
                TimeZones.NewYork,
                true,
                true,
                false);
            var dataCacheProvider = new CustomEphemeralDataCacheProvider { IsDataEphemeral = false };
            var reader = new TextSubscriptionDataSourceReader(
                dataCacheProvider,
                config,
                _initialDate,
                false);
            var source = (new TradeBar()).GetSource(config, _initialDate, false);
            dataCacheProvider.Data = "20000101 00:00,1,1,1,1,1";
            var dataBars = reader.Read(source).First();
            // even if the data changes it already cached
            dataCacheProvider.Data = "20000101 00:00,2,2,2,2,2";
            var dataBars2 = reader.Read(source).First();

            Assert.AreEqual(new DateTime(2000, 1, 1), dataBars.Time);
            Assert.AreEqual(new DateTime(2000, 1, 1), dataBars2.Time);
            Assert.AreEqual(dataBars.Price, dataBars2.Price);
        }

        [Test]
        public void DataIsCachedCorrectly()
        {
            var reader = new TextSubscriptionDataSourceReader(
                new SingleEntryDataCacheProvider(new DefaultDataProvider()),
                _config,
                _initialDate,
                false);
            var source = (new TradeBar()).GetSource(_config, _initialDate, false);

            var dataBars = reader.Read(source).ToList();
            var dataBars2 = reader.Read(source).ToList();

            Assert.AreEqual(dataBars2.Count, dataBars.Count);
            Assert.IsTrue(dataBars.SequenceEqual(dataBars2, new CustomComparer<BaseData>(
                (data, baseData) =>
                {
                    if (data.EndTime == baseData.EndTime
                        && data.Time == baseData.Time
                        && data.Symbol == baseData.Symbol
                        && data.Price == baseData.Price
                        && data.DataType == baseData.DataType
                        && data.Value == baseData.Value)
                    {
                        return 0;
                    }
                    return 1;
                })));
        }

        [Test]
        public void RespectsInitialDate()
        {
            var reader = new TextSubscriptionDataSourceReader(
                new SingleEntryDataCacheProvider(new DefaultDataProvider()),
                _config,
                _initialDate,
                false);
            var source = (new TradeBar()).GetSource(_config, _initialDate, false);
            var dataBars = reader.Read(source).First();

            Assert.Less(dataBars.EndTime, _initialDate);

            // 80 days after _initialDate
            var initialDate2 = _initialDate.AddDays(80);
            var reader2 = new TextSubscriptionDataSourceReader(
                new SingleEntryDataCacheProvider(new DefaultDataProvider()),
                _config,
                initialDate2,
                false);
            var source2 = (new TradeBar()).GetSource(_config, initialDate2, false);
            var dataBars2 = reader2.Read(source2).First();

            Assert.Less(dataBars2.EndTime, initialDate2);

            // 80 days before _initialDate
            var initialDate3 = _initialDate.AddDays(-80);
            var reader3 = new TextSubscriptionDataSourceReader(
                new SingleEntryDataCacheProvider(new DefaultDataProvider()),
                _config,
                initialDate3,
                false);
            var source3 = (new TradeBar()).GetSource(_config, initialDate3, false);
            var dataBars3 = reader3.Read(source3).First();

            Assert.Less(dataBars3.EndTime, initialDate3);
        }

        [TestCase(Resolution.Daily, true)]
        [TestCase(Resolution.Hour, true)]
        [TestCase(Resolution.Minute, false)]
        [TestCase(Resolution.Second, false)]
        [TestCase(Resolution.Tick, false)]
        public void CacheBehaviorDifferentResolutions(Resolution resolution, bool shouldBeCached)
        {
            _config = new SubscriptionDataConfig(
                typeof(TestTradeBarFactory),
                Symbols.SPY,
                resolution,
                TimeZones.NewYork,
                TimeZones.NewYork,
                true,
                true,
                false);
            var reader = new TextSubscriptionDataSourceReader(
                new SingleEntryDataCacheProvider(new DefaultDataProvider(), isDataEphemeral: false),
                _config,
                new DateTime(2013, 10, 07),
                false);
            var source = (new TradeBar()).GetSource(_config, new DateTime(2013, 10, 07), false);

            // first call should cache
            reader.Read(source).First();
            TestTradeBarFactory.ReaderWasCalled = false;
            reader.Read(source).First();
            Assert.AreEqual(!shouldBeCached, TestTradeBarFactory.ReaderWasCalled);
        }

        private class TestTradeBarFactory : TradeBar
        {
            /// <summary>
            /// Will be true when data is created from a parsed file line
            /// </summary>
            public static bool ReaderWasCalled { get; set; }

            public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
            {
                ReaderWasCalled = true;
                return base.Reader(config, line, date, isLiveMode);
            }
        }

        private class CustomEphemeralDataCacheProvider : IDataCacheProvider
        {
            public string Data { set; get; }
            public bool IsDataEphemeral { set; get; }

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
            public void Dispose()
            {
            }
        }
    }
}
