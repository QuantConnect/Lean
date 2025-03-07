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
using QuantConnect.Data;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Util;

namespace QuantConnect.Tests.Engine.DataFeeds
{
    [TestFixture]
    public class ZipEntryNameSubscriptionFactoryTests
    {
        [Test]
        public void ReadsZipEntryNames()
        {
            var time = new DateTime(2016, 03, 03, 12, 48, 15);
            var source = Path.Combine("TestData", "20151224_quote_american.zip");
            var config = new SubscriptionDataConfig(typeof (CustomData), Symbol.Create("XLRE", SecurityType.Option, Market.USA), Resolution.Tick,
                TimeZones.NewYork, TimeZones.NewYork, false, false, false);
            using var cacheProvider = new ZipDataCacheProvider(TestGlobals.DataProvider);
            var factory = new ZipEntryNameSubscriptionDataSourceReader(cacheProvider, config, time, false);
            var expected = new[]
            {
                Symbol.CreateOption("XLRE", Market.USA, OptionStyle.American, OptionRight.Call, 21m, new DateTime(2016, 08, 19)),
                Symbol.CreateOption("XLRE", Market.USA, OptionStyle.American, OptionRight.Call, 22m, new DateTime(2016, 08, 19)),
                Symbol.CreateOption("XLRE", Market.USA, OptionStyle.American, OptionRight.Put, 37m, new DateTime(2016, 08, 19)),
            };

            var actual = factory.Read(new SubscriptionDataSource(source, SubscriptionTransportMedium.LocalFile, FileFormat.ZipEntryName)).ToList();

            // we only really care about the symbols
            CollectionAssert.AreEqual(expected, actual.Select(x => x.Symbol));
            Assert.IsTrue(actual.All(x => x is CustomData));
        }

        private class CustomData : BaseData
        {
            public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
            {
                var symbol = LeanData.ReadSymbolFromZipEntry(config.Symbol, config.Resolution, line);
                return new CustomData { Time = date, Symbol = symbol };
            }

            public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
            {
                var source = LeanData.GenerateZipFilePath(Globals.DataFolder, config.Symbol, date, config.Resolution, config.TickType);
                return new SubscriptionDataSource(source, SubscriptionTransportMedium.LocalFile, FileFormat.ZipEntryName);
            }
        }
    }
}
