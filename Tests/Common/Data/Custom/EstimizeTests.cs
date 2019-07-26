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

namespace QuantConnect.Tests.Common.Data.Custom
{
    [TestFixture]
    public class EstimizeTests
    {
        [Test, Ignore]
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

            var data = JsonConvert.DeserializeObject<EstimizeRelease>(content);

            Assert.NotNull(data);
            Assert.AreEqual(data.Id, "155842");
            Assert.AreEqual(data.FiscalYear, 2020);
            Assert.AreEqual(data.FiscalQuarter, 1);
            Assert.IsFalse(data.Eps.HasValue);
            Assert.AreEqual(data.WallStreetEpsEstimate, 1.188);
            Assert.AreEqual(data.ConsensusEpsEstimate, 1.20507936507937);
            Assert.AreEqual(data.ConsensusWeightedEpsEstimate, 1.21545656570188);
            Assert.IsFalse(data.Revenue.HasValue);
            Assert.AreEqual(data.WallStreetRevenueEstimate, 31966.964);
            Assert.AreEqual(data.ConsensusRevenueEstimate, 31872.2258064516);
            Assert.AreEqual(data.ConsensusWeightedRevenueEstimate, 31966.6230263867);
            Assert.AreEqual(data.ReleaseDate, new DateTime(2019, 10, 23, 20, 0, 0).ToLocalTime());
            Assert.AreEqual(data.ReleaseDate, data.EndTime);

            content = content.Replace("\"eps\":null,", "\"eps\":1.2,");
            data = JsonConvert.DeserializeObject<EstimizeRelease>(content);
            Assert.NotNull(data);
            Assert.AreEqual(data.Eps, 1.2);
            Assert.AreEqual(data.Value, 1.2);
        }

        [Test]
        public void SerializeEstimizeReleaseSuccessfully()
        {
            var data = new EstimizeRelease()
            {
                Id = "0",
                ReleaseDate = new DateTime(2019,6,10)
            };

            var content = JsonConvert.SerializeObject(data);

            Assert.IsTrue(content.Contains("\"id\":\"0\""));
            Assert.IsTrue(content.Contains("\"release_date\":\"2019-06-10T00:00:00\""));
            Assert.IsTrue(content.Contains("\"eps\":null"));

            data.Eps = 1.2m;
            content = JsonConvert.SerializeObject(data);
            Assert.IsTrue(content.Contains("\"eps\":1.2"));
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
            var factory = SubscriptionDataSourceReader.ForSource(source, dataCacheProvider, config, date, false);

            var rows = factory.Read(source).ToList();

            Assert.IsTrue(rows.Count > 0);
        }

        [Test]
        public void DeserializeEstimateReleaseSuccessfully()
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

            var data = JsonConvert.DeserializeObject<EstimizeEstimate>(content);

            Assert.NotNull(data);
            Assert.AreEqual(data.Id, "2857028");
            Assert.AreEqual(data.Ticker, "AAPL");
            Assert.AreEqual(data.FiscalYear, 2020);
            Assert.AreEqual(data.FiscalQuarter, 2);
            Assert.AreEqual(data.CreatedAt, new DateTime(2019, 6, 7, 14, 40, 36).ToLocalTime());
            Assert.AreEqual(data.CreatedAt, data.EndTime);
            Assert.AreEqual(data.Eps, 2.81);
            Assert.AreEqual(data.Revenue, 61413.0);
            Assert.AreEqual(data.UserName, "Dominantstock");
            Assert.AreEqual(data.AnalystId, "657836");
            Assert.IsFalse(data.Flagged);
            content = content.Replace("\"eps\":2.81,", "\"eps\":null,");
            data = JsonConvert.DeserializeObject<EstimizeEstimate>(content);
            Assert.NotNull(data);
            Assert.IsFalse(data.Eps.HasValue);
            Assert.AreEqual(data.Value, 0);
        }

        [Test]
        public void SerializeEstimizeEstimateSuccessfully()
        {
            var data = new EstimizeEstimate()
            {
                Id = "0",
                CreatedAt = new DateTime(2019, 6, 10)
            };

            var content = JsonConvert.SerializeObject(data);

            Assert.IsTrue(content.Contains("\"id\":\"0\""));
            Assert.IsTrue(content.Contains("\"created_at\":\"2019-06-10T00:00:00\""));
            Assert.IsTrue(content.Contains("\"eps\":null"));

            data.Eps = 1.2m;
            content = JsonConvert.SerializeObject(data);
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
            var factory = SubscriptionDataSourceReader.ForSource(source, dataCacheProvider, config, date, false);

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
            var factory = SubscriptionDataSourceReader.ForSource(source, dataCacheProvider, config, date, false);

            var rows = factory.Read(source).ToList();

            Assert.IsTrue(rows.Count > 0);
        }
    }
}