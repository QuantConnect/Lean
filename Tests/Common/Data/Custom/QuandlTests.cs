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
using NodaTime;
using NUnit.Framework;
using QuantConnect.Algorithm.CSharp;
using QuantConnect.Data;
using QuantConnect.Data.Custom;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.DataFeeds.Transport;

namespace QuantConnect.Tests.Common.Data.Custom
{
    [TestFixture]
    public class QuandlTests
    {
        [Test]
        public void QuandlDownloadDoesNotThrow()
        {
            Quandl.SetAuthCode("WyAazVXnq7ATy_fefTqm");
            RemoteFileSubscriptionStreamReader.SetDownloadProvider(new Api.Api());
            var data = new HistoryAlgorithm.QuandlFuture();

            const string ticker = "CHRIS/CME_SP1";
            var date = new DateTime(2018, 8, 31);

            var config = new SubscriptionDataConfig(typeof(HistoryAlgorithm.QuandlFuture), Symbol.Create(ticker, SecurityType.Base, QuantConnect.Market.USA), Resolution.Daily, DateTimeZone.Utc, DateTimeZone.Utc, false, false, false, true);
            var source = data.GetSource(config, date, false);
            var dataCacheProvider = new SingleEntryDataCacheProvider(new DefaultDataProvider());
            var factory = SubscriptionDataSourceReader.ForSource(source, dataCacheProvider, config, date, false);

            var rows = factory.Read(source).ToList();

            Assert.IsTrue(rows.Count > 0);
        }
    }
}
