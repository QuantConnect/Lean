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
using NUnit.Framework;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Common.Securities
{
    [TestFixture]
    public class MarketHoursDatabaseTests
    {
        [Test]
        public void InitializesFromFile()
        {
            string file = Path.Combine("TestData", "SampleMarketHoursDatabase.json");
            var exchangeHours = GetMarketHoursDatabase(file);

            Assert.AreEqual(2, exchangeHours.ExchangeHoursListing.Count);
        }

        [Test]
        public void RetrievesExchangeHoursWithAndWithoutSymbol()
        {
            string file = Path.Combine("TestData", "SampleMarketHoursDatabase.json");
            var exchangeHours = GetMarketHoursDatabase(file);

            var hours = exchangeHours.GetExchangeHours(Market.USA, Symbols.SPY, SecurityType.Equity);
            Assert.IsNotNull(hours);

            Assert.AreEqual(hours, exchangeHours.GetExchangeHours(Market.USA, null, SecurityType.Equity));
        }

        [Test]
        public void CorrectlyReadsClosedAllDayHours()
        {
            string file = Path.Combine("TestData", "SampleMarketHoursDatabase.json");
            var exchangeHours = GetMarketHoursDatabase(file);

            var hours = exchangeHours.GetExchangeHours(Market.USA, null, SecurityType.Equity);
            Assert.IsNotNull(hours);

            Assert.IsTrue(hours.MarketHours[DayOfWeek.Saturday].IsClosedAllDay);
        }

        [Test]
        public void CorrectlyReadsOpenAllDayHours()
        {
            string file = Path.Combine("TestData", "SampleMarketHoursDatabase.json");
            var exchangeHours = GetMarketHoursDatabase(file);

            var hours = exchangeHours.GetExchangeHours(Market.FXCM, null, SecurityType.Forex);
            Assert.IsNotNull(hours);

            Assert.IsTrue(hours.MarketHours[DayOfWeek.Monday].IsOpenAllDay);
        }

        [Test]
        public void InitializesFromDataFolder()
        {
            var provider = MarketHoursDatabase.FromDataFolder();
            Assert.AreNotEqual(0, provider.ExchangeHoursListing.Count);
        }

        [Test]
        public void CorrectlyReadsUsEquityMarketHours()
        {
            string file = Path.Combine("TestData", "SampleMarketHoursDatabase.json");
            var exchangeHours = GetMarketHoursDatabase(file);

            var equityHours = exchangeHours.GetExchangeHours(Market.USA, null, SecurityType.Equity);
            foreach (var day in equityHours.MarketHours.Keys)
            {
                var marketHours = equityHours.MarketHours[day];
                if (day == DayOfWeek.Saturday || day == DayOfWeek.Sunday)
                {
                    Assert.IsTrue(marketHours.IsClosedAllDay);
                    continue;
                }
                Assert.AreEqual(new TimeSpan(4, 0, 0), marketHours.GetMarketOpen(TimeSpan.Zero, true));
                Assert.AreEqual(new TimeSpan(9, 30, 0), marketHours.GetMarketOpen(TimeSpan.Zero, false));
                Assert.AreEqual(new TimeSpan(16, 0, 0), marketHours.GetMarketClose(TimeSpan.Zero, false));
                Assert.AreEqual(new TimeSpan(20, 0, 0), marketHours.GetMarketClose(TimeSpan.Zero, true));
            }
        }

        [Test]
        public void CorrectlyReadsUsEquityEarlyCloses()
        {
            string file = Path.Combine("TestData", "SampleMarketHoursDatabase.json");
            var exchangeHours = GetMarketHoursDatabase(file);

            var equityHours = exchangeHours.GetExchangeHours(Market.USA, null, SecurityType.Equity);
            Assert.AreNotEqual(0, equityHours.EarlyCloses.Count);

            var date = new DateTime(2016, 11, 25);
            var earlyCloseTime = new TimeSpan(13, 0, 0);
            Assert.AreEqual(earlyCloseTime, equityHours.EarlyCloses[date]);
        }

        [Test]
        public void CorrectlyReadFxcmForexMarketHours()
        {
            string file = Path.Combine("TestData", "SampleMarketHoursDatabase.json");
            var exchangeHours = GetMarketHoursDatabase(file);

            var equityHours = exchangeHours.GetExchangeHours(Market.FXCM, null, SecurityType.Forex);
            foreach (var day in equityHours.MarketHours.Keys)
            {
                var marketHours = equityHours.MarketHours[day];
                if (day == DayOfWeek.Saturday)
                {
                    Assert.IsTrue(marketHours.IsClosedAllDay);
                }
                else if (day != DayOfWeek.Sunday && day != DayOfWeek.Friday)
                {
                    Assert.IsTrue(marketHours.IsOpenAllDay);
                }
                else if (day == DayOfWeek.Sunday)
                {
                    Assert.AreEqual(new TimeSpan(17, 0, 0), marketHours.GetMarketOpen(TimeSpan.Zero, true));
                    Assert.AreEqual(new TimeSpan(17, 0, 0), marketHours.GetMarketOpen(TimeSpan.Zero, false));
                    Assert.AreEqual(new TimeSpan(24, 0, 0), marketHours.GetMarketClose(TimeSpan.Zero, false));
                    Assert.AreEqual(new TimeSpan(24, 0, 0), marketHours.GetMarketClose(TimeSpan.Zero, true));
                }
                else
                {
                    Assert.AreEqual(new TimeSpan(0, 0, 0), marketHours.GetMarketOpen(TimeSpan.Zero, true));
                    Assert.AreEqual(new TimeSpan(0, 0, 0), marketHours.GetMarketOpen(TimeSpan.Zero, false));
                    Assert.AreEqual(new TimeSpan(17, 0, 0), marketHours.GetMarketClose(TimeSpan.Zero, false));
                    Assert.AreEqual(new TimeSpan(17, 0, 0), marketHours.GetMarketClose(TimeSpan.Zero, true));
                }
            }
        }

        [Test]
        public void ReadsUsEquityDataTimeZone()
        {
            string file = Path.Combine("TestData", "SampleMarketHoursDatabase.json");
            var marketHoursDatabase = GetMarketHoursDatabase(file);

            Assert.AreEqual(TimeZones.NewYork, marketHoursDatabase.GetDataTimeZone(Market.USA, null, SecurityType.Equity));
        }

        [Test]
        public void ReadsFxcmForexDataTimeZone()
        {
            string file = Path.Combine("TestData", "SampleMarketHoursDatabase.json");
            var marketHoursDatabase = GetMarketHoursDatabase(file);

            Assert.AreEqual(TimeZones.EasternStandard, marketHoursDatabase.GetDataTimeZone(Market.FXCM, null, SecurityType.Forex));
        }

        [TestCase("GC", Market.COMEX)]
        [TestCase("SI", Market.COMEX)]
        [TestCase("HG", Market.COMEX)]
        [TestCase("ES", Market.CME)]
        [TestCase("NQ", Market.CME)]
        [TestCase("CL", Market.NYMEX)]
        [TestCase("NG", Market.NYMEX)]
        [TestCase("ZB", Market.CBOT)]
        [TestCase("ZC", Market.CBOT)]
        [TestCase("ZS", Market.CBOT)]
        [TestCase("ZT", Market.CBOT)]
        [TestCase("ZW", Market.CBOT)]
        public void MissingFuturesOptionsMarketHoursResolvesToFuturesMarketHours(string ticker, string market)
        {
            var provider = MarketHoursDatabase.FromDataFolder();
            var future = Symbol.Create(ticker, SecurityType.Future, market);
            var option = Symbol.CreateOption(
                future,
                market,
                default(OptionStyle),
                default(OptionRight),
                default(decimal),
                SecurityIdentifier.DefaultDate);

            var futureEntry = provider.GetEntry(market, future, SecurityType.Future);
            var optionEntry = provider.GetEntry(market, option, SecurityType.FutureOption);

            Assert.AreEqual(futureEntry, optionEntry);
        }

        [TestCase("GC", Market.COMEX, "OG")]
        [TestCase("SI", Market.COMEX, "SO")]
        [TestCase("HG", Market.COMEX, "HXE")]
        [TestCase("ES", Market.CME, "ES")]
        [TestCase("NQ", Market.CME, "NQ")]
        [TestCase("CL", Market.NYMEX, "LO")]
        [TestCase("NG", Market.NYMEX, "ON")]
        [TestCase("ZB", Market.CBOT, "OZB")]
        [TestCase("ZC", Market.CBOT, "OZC")]
        [TestCase("ZS", Market.CBOT, "OZS")]
        [TestCase("ZT", Market.CBOT, "OZT")]
        [TestCase("ZW", Market.CBOT, "OZW")]
        public void FuturesOptionsGetDatabaseSymbolKey(string ticker, string market, string expected)
        {
            var future = Symbol.Create(ticker, SecurityType.Future, market);
            var option = Symbol.CreateOption(
                future,
                market,
                default(OptionStyle),
                default(OptionRight),
                default(decimal),
                SecurityIdentifier.DefaultDate);

            Assert.AreEqual(expected, MarketHoursDatabase.GetDatabaseSymbolKey(option));
        }

        [Test]
        public void CustomEntriesStoredAndFetched()
        {
            var database = MarketHoursDatabase.FromDataFolder();
            var ticker = "BTC";
            var hours = SecurityExchangeHours.AlwaysOpen(TimeZones.Berlin);
            var entry = database.SetEntry(Market.USA, ticker, SecurityType.Base, hours);

            // Assert our hours match the result
            Assert.AreEqual(hours, entry.ExchangeHours);
            
            // Fetch the entry to ensure we can access it with the ticker
            var fetchedEntry = database.GetEntry(Market.USA, ticker, SecurityType.Base);
            Assert.AreSame(entry, fetchedEntry);
        }

        private static MarketHoursDatabase GetMarketHoursDatabase(string file)
        {
            return MarketHoursDatabase.FromFile(file);
        }
    }
}
