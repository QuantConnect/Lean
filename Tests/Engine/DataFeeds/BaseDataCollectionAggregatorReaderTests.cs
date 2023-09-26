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
using QuantConnect.Util;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.Tests.Engine.DataFeeds
{
    [TestFixture, Parallelizable(ParallelScope.Fixtures)]
    public class BaseDataCollectionAggregatorReaderTests
    {
        [TestCase(Resolution.Daily, 1, true)]
        [TestCase( Resolution.Hour, 1, true)]
        [TestCase(Resolution.Daily, 5849, false)]
        [TestCase(Resolution.Hour, 40832, false)]
        public void AggregatesDataPerTime(Resolution resolution, int expectedCount, bool singleDate)
        {
            var reader = Initialize(false, resolution, out var dataSource);
            TestBaseDataCollection.SingleDate = singleDate;

            var result = reader.Read(dataSource).ToList();

            Assert.AreEqual(expectedCount, result.Count);
            Assert.IsTrue(result.All(data => data is TestBaseDataCollection));

            if (expectedCount == 1)
            {
                var collection = result[0] as TestBaseDataCollection;
                Assert.IsNotNull(collection);
                Assert.GreaterOrEqual(collection.Data.Count, 5000);
                Assert.AreEqual(expectedCount, collection.Data.DistinctBy(data => data.Time).Count());
            }
        }

        private static ISubscriptionDataSourceReader Initialize(bool liveMode, Resolution resolution, out SubscriptionDataSource source)
        {
            using var dataProvider = new DefaultDataProvider();
            using var cache = new ZipDataCacheProvider(dataProvider);
            var config = new SubscriptionDataConfig(typeof(TestBaseDataCollection),
                Symbols.SPY,
                resolution,
                TimeZones.NewYork,
                TimeZones.NewYork,
                false,
                false,
                false);
            var date = DateTime.MinValue;
            var path = LeanData.GenerateZipFilePath(Globals.DataFolder, config.Symbol, date, resolution, TickType.Trade);
            source = new SubscriptionDataSource(path, SubscriptionTransportMedium.LocalFile);
            return new BaseDataCollectionAggregatorReader(cache, config, date, liveMode, null);
        }

        private class TestBaseDataCollection : BaseDataCollection
        {
            public static volatile bool SingleDate;
            private static readonly TradeBar _factory = new TradeBar();
            public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
            {
                var dataPoint = _factory.Reader(config, line, date, isLiveMode);
                if (SingleDate)
                {
                    // single day
                    dataPoint.Time = date;
                }
                return dataPoint;
            }

            public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
            {
                return _factory.GetSource(config, date, isLiveMode);
            }
        }
    }
}
