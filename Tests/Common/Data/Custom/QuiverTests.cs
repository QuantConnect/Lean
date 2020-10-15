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
using Type = QuantConnect.Data.Custom.Quiver.Type;

namespace QuantConnect.Tests.Common.Data.Custom
{
    [TestFixture]
    public class QuiverTests
    {
        private readonly JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings
        {
            DateTimeZoneHandling = DateTimeZoneHandling.Utc
        };

        [Test, Ignore("Requires Quiver credentials")]
        public void QuiverDownloadDoesNotThrow()
        {
            var tickers = new List<QuiverDataDownloader.Company>();

            var destinationFolder = Path.Combine(Globals.DataFolder, "alternative", "quiver");
            var downloader = new QuiverReleaseDataDownloader(destinationFolder);

            Assert.DoesNotThrow(() => tickers = downloader.GetCompanies().Result);
            Assert.IsTrue(tickers.Count > 0);
        }

        [Test]
        public void DeserializeQuiverWikipediaSuccessfully()
        {
            var content = "{" +
                          "\"Date\":\"2020-01-01\"," +
                          "\"Ticker\":\"ABBV\"," +
                          "\"Views\":3500," +
                          "\"pct_change_week\":3.2," +
                          "\"pct_change_month\":6.75}";

            var data = JsonConvert.DeserializeObject<QuiverWikipedia>(content, _jsonSerializerSettings);

            Assert.NotNull(data);
            Assert.AreEqual("ABBV", data.Ticker);
            Assert.AreEqual(new DateTime(2020, 01, 01, 0, 0, 0), data.Date);
            Assert.AreEqual(3500, data.Views);
            Assert.AreEqual(3.2, data.pct_change_week);
            Assert.AreEqual(6.75, data.pct_change_month);

        }


        [Test]
        public void QuiverWikipediaReaderDoesNotThrow()
        {
            var data = "11/14/2016 12:00:00 AM,ABBV,1599,-1.9018404908,-9.4050991501";
            var instance = new QuiverWikipedia();

            var fakeConfig = new SubscriptionDataConfig(
                typeof(QuiverWikipedia),
                Symbol.Create("ABBV", SecurityType.Base, "USA"),
                Resolution.Daily,
                TimeZones.Utc,
                TimeZones.Utc,
                false,
                false,
                false
            );

            Assert.DoesNotThrow(() => { instance.Reader(fakeConfig, data, DateTime.MinValue, false); });
        }

        [Test, Ignore("Requires Quiver Wikipedia data")]
        public void QuiverWikipediaReaderTest()
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

            var data = new QuiverWikipedia();
            var date = new DateTime(2019, 6, 10);
            var source = data.GetSource(config, date, false);
            var factory = SubscriptionDataSourceReader.ForSource(source, dataCacheProvider, config, date, false, data);

            var rows = factory.Read(source).ToList();

            Assert.IsTrue(rows.Count > 0);
        }
    }
}