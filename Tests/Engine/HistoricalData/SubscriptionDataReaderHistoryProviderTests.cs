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
using System.Linq;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Data.Market;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.HistoricalData;
using QuantConnect.Securities;
using HistoryRequest = QuantConnect.Data.HistoryRequest;

namespace QuantConnect.Tests.Engine.HistoricalData
{
    [TestFixture]
    public class SubscriptionDataReaderHistoryProviderTests
    {
        [Test]
        public void OptionsAreMappedCorrectly()
        {
            var historyProvider = new SubscriptionDataReaderHistoryProvider();
            historyProvider.Initialize(new HistoryProviderInitializeParameters(
                null,
                null,
                new DefaultDataProvider(),
                new ZipDataCacheProvider(new DefaultDataProvider()), 
                new LocalDiskMapFileProvider(),
                new LocalDiskFactorFileProvider(),
                null,
                false));
            var symbol = Symbol.CreateOption(
                "FOXA",
                Market.USA,
                OptionStyle.American,
                OptionRight.Call,
                32,
                new DateTime(2013, 07, 20));

            var result = historyProvider.GetHistory(
                new[]
                {
                    new HistoryRequest(new DateTime(2013, 06,28),
                        new DateTime(2013, 07,03),
                        typeof(QuoteBar),
                        symbol,
                        Resolution.Minute,
                        SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                        TimeZones.NewYork,
                        null,
                        false,
                        false,
                        DataNormalizationMode.Raw,
                        TickType.Quote)
                },
                TimeZones.NewYork).ToList();

            Assert.IsNotEmpty(result);

            // assert we fetch the data for the previous and new symbol
            var firstBar = result.First().Values.Single();
            var lastBar = result.Last().Values.Single();

            Assert.IsTrue(firstBar.Symbol.Value.Contains("NWSA"));
            Assert.AreEqual(28, firstBar.Time.Date.Day);
            Assert.IsTrue(lastBar.Symbol.Value.Contains("FOXA"));
            Assert.AreEqual(2, lastBar.Time.Date.Day);
        }

        [Test]
        public void EquitiesAreMappedCorrectly()
        {
            var historyProvider = new SubscriptionDataReaderHistoryProvider();
            historyProvider.Initialize(new HistoryProviderInitializeParameters(
                null,
                null,
                new DefaultDataProvider(),
                new ZipDataCacheProvider(new DefaultDataProvider()),
                new LocalDiskMapFileProvider(),
                new LocalDiskFactorFileProvider(),
                null,
                false));
            var symbol = Symbol.Create("WM",SecurityType.Equity,Market.USA);

            var result = historyProvider.GetHistory(
                new[]
                {
                    new HistoryRequest(new DateTime(2000, 01,01),
                        new DateTime(2000, 01,05),
                        typeof(TradeBar),
                        symbol,
                        Resolution.Daily,
                        SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                        TimeZones.NewYork,
                        null,
                        false,
                        false,
                        DataNormalizationMode.Raw,
                        TickType.Trade)
                },
                TimeZones.NewYork).ToList();

            var firstBar = result.First().Values.Single();
            Assert.IsTrue(firstBar.Symbol.Value.Contains("WMI"));
            Assert.IsNotEmpty(result);
        }
    }
}
