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

using Newtonsoft.Json;
using NUnit.Framework;
using QuantConnect.Data.Custom.Estimize;
using QuantConnect.ToolBox.EstimizeDataDownloader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NodaTime;
using QuantConnect.Data;
using QuantConnect.Lean.Engine.DataFeeds;
using Type = QuantConnect.Data.Custom.Estimize.Type;

namespace QuantConnect.Tests.Common.Data.Custom
{
    [TestFixture]
    public class EstimizeTests
    {
        private readonly JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings
        {
            DateTimeZoneHandling = DateTimeZoneHandling.Utc
        };

        [Test, Ignore("Requires Estimize credentials")]
        public void EstimizeDownloadDoesNotThrow()
        {
            var tickers = new List<EstimizeDataDownloader.Company>();

            var destinationFolder = Path.Combine(Globals.DataFolder, "alternative", "estimize");
            var downloader = new EstimizeReleaseDataDownloader(destinationFolder);

            Assert.DoesNotThrow(() => tickers = downloader.GetCompanies().Result);
            Assert.IsTrue(tickers.Count > 0);
        }

        [Test]
        public void DeserializeEstimizeReleaseSuccessfully()
        {
            var content = "{" +
                          "\"fiscal_year\":2020," +
                          "\"fiscal_quarter\":1," +
                          "\"eps\":null," +
                          "\"revenue\":null," +
                          "\"consensus_eps_estimate\":1.20507936507937," +
                          "\"consensus_revenue_estimate\":31872.2258064516," +
                          "\"wallstreet_revenue_estimate\":31966.964," +
                          "\"wallstreet_eps_estimate\":1.188," +
                          "\"consensus_weighted_revenue_estimate\":31966.6230263867," +
                          "\"consensus_weighted_eps_estimate\":1.21545656570188," +
                          "\"release_date\":\"2019-10-23T16:00:00-04:00\"," +
                          "\"id\":\"155842\"}";

            var data = JsonConvert.DeserializeObject<EstimizeRelease>(content, _jsonSerializerSettings);

            Assert.NotNull(data);
            Assert.AreEqual("155842", data.Id);
            Assert.AreEqual(2020, data.FiscalYear);
            Assert.AreEqual(1, data.FiscalQuarter);
            Assert.IsFalse(data.Eps.HasValue);
            Assert.AreEqual(1.188, data.WallStreetEpsEstimate);
            Assert.AreEqual(1.20507936507937, data.ConsensusEpsEstimate);
            Assert.AreEqual(1.21545656570188, data.ConsensusWeightedEpsEstimate);
            Assert.IsFalse(data.Revenue.HasValue);
            Assert.AreEqual(31966.964, data.WallStreetRevenueEstimate);
            Assert.AreEqual(31872.2258064516, data.ConsensusRevenueEstimate);
            Assert.AreEqual(31966.6230263867, data.ConsensusWeightedRevenueEstimate);
            Assert.AreEqual(new DateTime(2019, 10, 23, 20, 0, 0), data.ReleaseDate);
            Assert.AreEqual(data.ReleaseDate, data.EndTime);
            Assert.AreEqual(data.ReleaseDate, data.Time);

            content = content.Replace("\"eps\":null,", "\"eps\":1.2,");
            data = JsonConvert.DeserializeObject<EstimizeRelease>(content, _jsonSerializerSettings);
            Assert.NotNull(data);
            Assert.AreEqual(1.2, data.Eps);
            Assert.AreEqual(1.2, data.Value);
        }

        [Test]
        public void SerializeEstimizeReleaseSuccessfully()
        {
            var data = new EstimizeRelease()
            {
                Id = "0",
                ReleaseDate = new DateTime(2019,6,10)
            };

            var content = JsonConvert.SerializeObject(data, _jsonSerializerSettings);

            Assert.IsTrue(content.Contains("\"id\":\"0\""));
            Assert.IsTrue(content.Contains("\"release_date\":\"2019-06-10T00:00:00Z\""));
            Assert.IsTrue(content.Contains("\"eps\":null"));

            data.Eps = 1.2m;
            content = JsonConvert.SerializeObject(data, _jsonSerializerSettings);
            Assert.IsTrue(content.Contains("\"eps\":1.2"));
        }

        [Test]
        public void EstimizeConsensusReaderDoesNotThrow()
        {
            var data = "20100101 12:00:00,abcdef123456789deadbeef,Estimize,Revenue,100.00,100,100,50,2010,1,100";
            var instance = new EstimizeConsensus();

            var fakeConfig = new SubscriptionDataConfig(
                typeof(EstimizeConsensus),
                Symbol.Create("AAPL", SecurityType.Base, "USA"),
                Resolution.Daily,
                TimeZones.Utc,
                TimeZones.Utc,
                false,
                false,
                false
            );

            Assert.DoesNotThrow(() => { instance.Reader(fakeConfig, data, DateTime.MinValue, false); });
        }

        [Test, Ignore("Requires Estimize data")]
        public void EstimizeReleaseReaderTest()
        {
            var dataCacheProvider = new SingleEntryDataCacheProvider(new DefaultDataProvider());

            var config = new SubscriptionDataConfig(
                typeof(EstimizeRelease),
                Symbol.Create("AAPL.R", SecurityType.Base, QuantConnect.Market.USA),
                Resolution.Daily,
                DateTimeZone.Utc,
                DateTimeZone.Utc,
                false,
                false,
                false,
                true
            );

            var data = new EstimizeRelease();
            var date = new DateTime(2019, 6, 10);
            var source = data.GetSource(config, date, false);
            var factory = SubscriptionDataSourceReader.ForSource(source, dataCacheProvider, config, date, false, data);

            var rows = factory.Read(source).ToList();

            Assert.IsTrue(rows.Count > 0);
        }

        [Test]
        public void DeserializeEstimizeEstimateSuccessfully()
        {
            var content = "{" +
                          "\"ticker\":\"AAPL\"," +
                          "\"fiscal_year\":2020," +
                          "\"fiscal_quarter\":2," +
                          "\"eps\":2.81," +
                          "\"revenue\":61413.0," +
                          "\"username\":\"Dominantstock\"," +
                          "\"created_at\":\"2019-06-07T10:40:36-04:00\"," +
                          "\"id\":\"2857028\"," +
                          "\"analyst_id\":\"657836\"," +
                          "\"flagged\":false}";

            var data = JsonConvert.DeserializeObject<EstimizeEstimate>(content, _jsonSerializerSettings);

            Assert.NotNull(data);
            Assert.AreEqual("2857028", data.Id);
            Assert.AreEqual("AAPL", data.Ticker);
            Assert.AreEqual(2020, data.FiscalYear);
            Assert.AreEqual(2, data.FiscalQuarter);
            Assert.AreEqual(new DateTime(2019, 6, 7, 14, 40, 36), data.CreatedAt);
            Assert.AreEqual(data.EndTime, data.CreatedAt);
            Assert.AreEqual(data.Time, data.CreatedAt);
            Assert.AreEqual(2.81, data.Eps);
            Assert.AreEqual(61413.0, data.Revenue);
            Assert.AreEqual("Dominantstock", data.UserName);
            Assert.AreEqual("657836", data.AnalystId);
            Assert.IsFalse(data.Flagged);
            content = content.Replace("\"eps\":2.81,", "\"eps\":null,");
            data = JsonConvert.DeserializeObject<EstimizeEstimate>(content, _jsonSerializerSettings);
            Assert.NotNull(data);
            Assert.IsFalse(data.Eps.HasValue);
            Assert.AreEqual(0, data.Value);
        }

        [Test]
        public void SerializeEstimizeEstimateSuccessfully()
        {
            var data = new EstimizeEstimate()
            {
                Id = "0",
                CreatedAt = new DateTime(2019, 6, 10)
            };

            var content = JsonConvert.SerializeObject(data, _jsonSerializerSettings);

            Assert.IsTrue(content.Contains("\"id\":\"0\""));
            Assert.IsTrue(content.Contains("\"created_at\":\"2019-06-10T00:00:00Z\""));
            Assert.IsTrue(content.Contains("\"eps\":null"));

            data.Eps = 1.2m;
            content = JsonConvert.SerializeObject(data, _jsonSerializerSettings);
            Assert.IsTrue(content.Contains("\"eps\":1.2"));
        }

        [Test, Ignore("Requires Estimize data")]
        public void EstimizeEstimateReaderTest()
        {
            var dataCacheProvider = new SingleEntryDataCacheProvider(new DefaultDataProvider());

            var config = new SubscriptionDataConfig(
                typeof(EstimizeEstimate),
                Symbol.Create("AAPL.E", SecurityType.Base, QuantConnect.Market.USA),
                Resolution.Daily,
                DateTimeZone.Utc,
                DateTimeZone.Utc,
                false,
                false,
                false,
                true
            );

            var data = new EstimizeEstimate();
            var date = new DateTime(2019, 6, 10);
            var source = data.GetSource(config, date, false);
            var factory = SubscriptionDataSourceReader.ForSource(source, dataCacheProvider, config, date, false, data);

            var rows = factory.Read(source).ToList();

            Assert.IsTrue(rows.Count > 0);
        }

        [Test, Ignore("Requires Estimize data")]
        public void EstimizeConsensusReaderTest()
        {
            var dataCacheProvider = new SingleEntryDataCacheProvider(new DefaultDataProvider());

            var config = new SubscriptionDataConfig(
                typeof(EstimizeConsensus),
                Symbol.Create("AAPL.C", SecurityType.Base, QuantConnect.Market.USA),
                Resolution.Daily,
                DateTimeZone.Utc,
                DateTimeZone.Utc,
                false,
                false,
                false,
                true
            );

            var data = new EstimizeConsensus();
            var date = new DateTime(2019, 6, 10);
            var source = data.GetSource(config, date, false);
            var factory = SubscriptionDataSourceReader.ForSource(source, dataCacheProvider, config, date, false, data);

            var rows = factory.Read(source).ToList();

            Assert.IsTrue(rows.Count > 0);
        }

        [Test]
        public void SerializeRoundTripEstimizeRelease()
        {
            var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };

            var time = new DateTime(2020, 3, 19, 10, 0, 0);
            var underlyingSymbol = Symbols.AAPL;
            var symbol = Symbol.CreateBase(typeof(EstimizeRelease), underlyingSymbol, QuantConnect.Market.USA);

            var item = new EstimizeRelease
            {
                Id = "123",
                Symbol = symbol,
                FiscalYear = 2020,
                FiscalQuarter = 1,
                Eps = 2,
                Revenue = null,
                ReleaseDate = time
            };

            var serialized = JsonConvert.SerializeObject(item, settings);
            var deserialized = JsonConvert.DeserializeObject<EstimizeRelease>(serialized, settings);

            Assert.AreEqual("123", deserialized.Id);
            Assert.AreEqual(symbol, deserialized.Symbol);
            Assert.AreEqual(2020, deserialized.FiscalYear);
            Assert.AreEqual(1, deserialized.FiscalQuarter);
            Assert.AreEqual(2, deserialized.Eps);
            Assert.AreEqual(null, deserialized.Revenue);
            Assert.AreEqual(time, deserialized.ReleaseDate);
            Assert.AreEqual(time, deserialized.Time);
            Assert.AreEqual(time, deserialized.EndTime);
        }

        [Test]
        public void SerializeRoundTripEstimizeConsensus()
        {
            var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };

            var time = new DateTime(2020, 3, 19, 10, 0, 0);
            var underlyingSymbol = Symbols.AAPL;
            var symbol = Symbol.CreateBase(typeof(EstimizeConsensus), underlyingSymbol, QuantConnect.Market.USA);

            var item = new EstimizeConsensus
            {
                Id = "123",
                Symbol = symbol,
                FiscalYear = 2020,
                FiscalQuarter = 1,
                Source = Source.WallStreet,
                Type = Type.Eps,
                Count = 3,
                Mean = 2,
                UpdatedAt = time
            };

            var serialized = JsonConvert.SerializeObject(item, settings);
            var deserialized = JsonConvert.DeserializeObject<EstimizeConsensus>(serialized, settings);

            Assert.AreEqual("123", deserialized.Id);
            Assert.AreEqual(symbol, deserialized.Symbol);
            Assert.AreEqual(2020, deserialized.FiscalYear);
            Assert.AreEqual(1, deserialized.FiscalQuarter);
            Assert.AreEqual(Source.WallStreet, deserialized.Source);
            Assert.AreEqual(Type.Eps, deserialized.Type);
            Assert.AreEqual(3, deserialized.Count);
            Assert.AreEqual(2, deserialized.Mean);
            Assert.AreEqual(time, deserialized.UpdatedAt);
            Assert.AreEqual(time, deserialized.Time);
            Assert.AreEqual(time, deserialized.EndTime);
        }

        [Test]
        public void SerializeRoundTripEstimizeEstimate()
        {
            var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };

            var time = new DateTime(2020, 3, 19, 10, 0, 0);
            var underlyingSymbol = Symbols.AAPL;
            var symbol = Symbol.CreateBase(typeof(EstimizeEstimate), underlyingSymbol, QuantConnect.Market.USA);

            var item = new EstimizeEstimate
            {
                Id = "123",
                Symbol = symbol,
                FiscalYear = 2020,
                FiscalQuarter = 1,
                Eps = 2,
                Revenue = null,
                CreatedAt = time
            };

            var serialized = JsonConvert.SerializeObject(item, settings);
            var deserialized = JsonConvert.DeserializeObject<EstimizeEstimate>(serialized, settings);

            Assert.AreEqual("123", deserialized.Id);
            Assert.AreEqual(symbol, deserialized.Symbol);
            Assert.AreEqual(2020, deserialized.FiscalYear);
            Assert.AreEqual(1, deserialized.FiscalQuarter);
            Assert.AreEqual(2, deserialized.Eps);
            Assert.AreEqual(null, deserialized.Revenue);
            Assert.AreEqual(time, deserialized.CreatedAt);
            Assert.AreEqual(time, deserialized.Time);
            Assert.AreEqual(time, deserialized.EndTime);
        }
    }
}