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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Securities;
using QuantConnect.Util;

namespace QuantConnect.Tests.Engine.DataFeeds
{
    [TestFixture]
    public class SubscriptionDataReaderTests
    {
        // this is the core of this unit test:
        // the stream will have two data points one of which does not correspond to the tradable date,
        // and it shouldn't be emitted because we have a new source for the next tradable date
        [TestCase(@"25980000,1679600,1679700,1679600,1679700,200
                99900000,1679400,1679400,1679200,1679200,600", false, Resolution.Minute)]
        // this test case has two data point which should be emitted because they correspond to the same tradable date
        [TestCase(@"25980000,1679600,1679700,1679600,1679700,200
                30980000,1679400,1679400,1679200,1679200,600", true, Resolution.Minute)]
        // even if the second data point is another tradable date we emit it because daily resolution
        // always uses the same source
        [TestCase(@"20191209 00:00,956900,959700,947200,958100,3647000
                20191211 00:00,956900,959700,947200,958100,3647000", true, Resolution.Daily)]
        public void DoesNotEmitDataBeyondTradableDate(string data, bool shouldEmitSecondDataPoint, Resolution dataResolution)
        {
            var start = new DateTime(2019, 12, 9);
            var end = new DateTime(2019, 12, 12);

            var symbol = Symbols.SPY;
            var entry = MarketHoursDatabase.FromDataFolder().GetEntry(symbol.ID.Market, symbol, symbol.SecurityType);
            var config = new SubscriptionDataConfig(typeof(TradeBar),
                symbol,
                dataResolution,
                TimeZones.NewYork,
                TimeZones.NewYork,
                false,
                false,
                false);
            using var testDataCacheProvider = new TestDataCacheProvider() { Data = data};
            using var dataReader = new SubscriptionDataReader(config,
                new HistoryRequest(config, entry.ExchangeHours, start, end),
                TestGlobals.MapFileProvider,
                TestGlobals.FactorFileProvider,
                testDataCacheProvider,
                TestGlobals.DataProvider,
                null);

            Assert.IsTrue(dataReader.MoveNext());
            Assert.AreEqual(shouldEmitSecondDataPoint, dataReader.MoveNext());

        }

        [TestCase(typeof(TradeBar))]
        [TestCase(typeof(QuoteBar))]
        [TestCase(typeof(OpenInterest))]
        public void EmitsNewTradableDateWhenDateAfterDelistingIsNonTradable(Type dataType)
        {
            var start = new DateTime(2023, 06, 30);
            var end = new DateTime(2023, 08, 01);

            var symbol = Symbol.CreateOption(
                Symbols.SPX,
                "SPXW",
                Market.USA,
                OptionStyle.European,
                OptionRight.Call,
                4445m,
                // Next day is a holiday
                new DateTime(2023, 7, 3));

            var entry = MarketHoursDatabase.FromDataFolder().GetEntry(symbol.ID.Market, symbol, symbol.SecurityType);
            var config = new SubscriptionDataConfig(dataType,
                symbol,
                Resolution.Minute,
                entry.DataTimeZone,
                entry.ExchangeHours.TimeZone,
                false,
                false,
                false);
            using var testDataCacheProvider = new TestDataCacheProvider();
            var request = new HistoryRequest(config, entry.ExchangeHours, start, end);
            using var dataReader = new SubscriptionDataReader(config,
                request,
                TestGlobals.MapFileProvider,
                TestGlobals.FactorFileProvider,
                testDataCacheProvider,
                TestGlobals.DataProvider,
                null);

            var expectedLastTradableDate = new DateTime(2023, 07, 05);
            var lastTradableDate = default(DateTime);

            dataReader.NewTradableDate += (sender, args) =>
            {
                lastTradableDate = args.Date;
            };

            while (dataReader.MoveNext())
            {
            }

            Assert.AreEqual(expectedLastTradableDate, lastTradableDate);
        }

        private class TestDataCacheProvider : IDataCacheProvider
        {
            private StreamWriter _writer;
            private bool _alreadyEmitted;

            public string Data { get; set; }
            public void Dispose()
            {
                _writer.DisposeSafely();
            }
            public List<string> GetZipEntries(string zipFile)
            {
                throw new NotImplementedException();
            }
            public bool IsDataEphemeral => true;
            public Stream Fetch(string key)
            {
                if (_alreadyEmitted)
                {
                    return null;
                }
                _alreadyEmitted = true;

                var stream = new MemoryStream();
                _writer = new StreamWriter(stream);
                _writer.Write(Data);
                _writer.Flush();
                stream.Position = 0;
                return stream;
            }
            public void Store(string key, byte[] data)
            {
            }
        }
    }
}
