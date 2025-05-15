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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using QuantConnect.Configuration;
using QuantConnect.Securities;
using QuantConnect.Securities.Option;

namespace QuantConnect.Tests.Common.Securities
{
    [TestFixture]
    public class MarketHoursDatabaseTests
    {
        [SetUp]
        public void Setup()
        {
            MarketHoursDatabase.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            MarketHoursDatabase.Reset();
        }

        [Test]
        public void InitializesFromFile()
        {
            string file = Path.Combine("TestData", "SampleMarketHoursDatabase.json");
            var exchangeHours = GetMarketHoursDatabase(file);

            Assert.AreEqual(3, exchangeHours.ExchangeHoursListing.Count);
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

        [TestCase("AUP", Market.COMEX, true)]
        [TestCase("AA6", Market.NYMEX, true)]
        [TestCase("6A", Market.CME, false)]
        [TestCase("30Y", Market.CBOT, false)]
        [TestCase("HE", Market.CME, true)]
        [TestCase("AW", Market.CBOT, true)]
        [TestCase("HE", Market.CME, true)]
        [TestCase("AW", Market.CBOT, true)]
        [TestCase("LE", Market.CME, true)]
        [TestCase("BCF", Market.CBOT, true)]
        [TestCase("GD", Market.CME, true)]
        [TestCase("BWF", Market.CBOT, true)]
        [TestCase("CSC", Market.CME, true)]
        [TestCase("GNF", Market.CME, true)]
        [TestCase("GDK", Market.CME, true)]
        public void CorrectlyReadsCMEGroupFutureHolidayGoodFridaySchedule(string futureTicker, string market, bool isHoliday)
        {
            var provider = MarketHoursDatabase.FromDataFolder();
            var ticker = OptionSymbol.MapToUnderlying(futureTicker, SecurityType.Future);
            var future = Symbol.Create(ticker, SecurityType.Future, market);

            var futureEntry = provider.GetEntry(market, ticker, future.SecurityType);
            var holidays = futureEntry.ExchangeHours.Holidays;
            var holidayDate = DateTime.Parse("4/7/2023", CultureInfo.InvariantCulture);
            Assert.AreEqual(isHoliday, holidays.Contains(holidayDate));
        }

        [TestCase("2YY", Market.CBOT, "4/7/2023", true)]
        [TestCase("TN", Market.CBOT, "4/7/2023", true)]
        [TestCase("6A", Market.CME, "4/7/2023", true)]
        [TestCase("6Z", Market.CME, "4/7/2023", true)]
        [TestCase("M6A", Market.CME, "4/7/2023", true)]
        [TestCase("MCD", Market.CME, "4/7/2023", true)]
        [TestCase("AW", Market.CBOT, "4/6/2023", false)]
        [TestCase("BCF", Market.CBOT, "4/6/2023", false)]
        [TestCase("BWF", Market.CBOT, "4/6/2023", false)]
        [TestCase("ZC", Market.CBOT, "4/6/2023", false)]
        [TestCase("DC", Market.CME, "4/7/2023", false)]
        [TestCase("DY", Market.CME, "4/7/2023", false)]
        [TestCase("GNF", Market.CME, "4/6/2023", true)]
        [TestCase("GDK", Market.CME, "4/6/2023", true)]
        public void CorrectlyReadsCMEGroupFutureEarlyCloses(string futureTicker, string market, string date, bool isEarlyClose)
        {
            var provider = MarketHoursDatabase.FromDataFolder();
            var ticker = OptionSymbol.MapToUnderlying(futureTicker, SecurityType.Future);
            var future = Symbol.Create(ticker, SecurityType.Future, market);

            var futureEntry = provider.GetEntry(market, ticker, future.SecurityType);
            var earlyCloses = futureEntry.ExchangeHours.EarlyCloses;
            var earlyCloseDate = DateTime.Parse(date, CultureInfo.InvariantCulture);
            Assert.AreEqual(isEarlyClose, earlyCloses.Keys.Contains(earlyCloseDate));
            if (isEarlyClose)
            {
                var holidays = futureEntry.ExchangeHours.Holidays;
                Assert.IsFalse(holidays.Contains(earlyCloseDate));
            }
        }

        [TestCase("BIO", Market.CME, true)]
        [TestCase("5YY", Market.CBOT, true)]
        [TestCase("6E", Market.CME, true)]
        [TestCase("BTC", Market.CME, true)]
        [TestCase("A8O", Market.NYMEX, true)]
        [TestCase("PAM", Market.NYMEX, true)]
        [TestCase("ZC", Market.CBOT, false)]
        [TestCase("LBR", Market.CME, false)]
        [TestCase("HE", Market.CME, false)]
        [TestCase("DY", Market.CME, false)]
        [TestCase("YO", Market.NYMEX, true)]
        public void CorrectlyReadsCMEGroupFutureBankHolidays(string futureTicker, string market, bool isBankHoliday)
        {
            var provider = MarketHoursDatabase.FromDataFolder();
            var future = Symbol.Create(futureTicker, SecurityType.Future, market);

            var futureEntry = provider.GetEntry(market, future, future.SecurityType);
            var bankHolidays = futureEntry.ExchangeHours.BankHolidays;
            var bankHoliday = new DateTime(2025, 11, 27);
            Assert.AreEqual(isBankHoliday, bankHolidays.Contains(bankHoliday));
            Assert.AreEqual(isBankHoliday, futureEntry.ExchangeHours.IsDateOpen(bankHoliday, extendedMarketHours: true));
        }

        [TestCase("2YY", Market.CBOT, true)]
        [TestCase("TN", Market.CBOT, true)]
        [TestCase("6A", Market.CME, true)]
        [TestCase("6Z", Market.CME, true)]
        [TestCase("M6A", Market.CME, true)]
        [TestCase("MCD", Market.CME, true)]
        [TestCase("AW", Market.CBOT, false)]
        [TestCase("BCF", Market.CBOT, false)]
        [TestCase("BWF", Market.CBOT, false)]
        [TestCase("ZC", Market.CBOT, false)]
        [TestCase("DC", Market.CME, false)]
        [TestCase("DY", Market.CME, false)]
        [TestCase("GNF", Market.CME, false)]
        [TestCase("GDK", Market.CME, false)]
        public void CheckJustEarlyClosesOrJustHolidaysForCMEGroupFuturesOnGoodFriday(string futureTicker, string market, bool isEarlyClose)
        {
            var provider = MarketHoursDatabase.FromDataFolder();
            var ticker = OptionSymbol.MapToUnderlying(futureTicker, SecurityType.Future);
            var future = Symbol.Create(ticker, SecurityType.Future, market);

            var futureEntry = provider.GetEntry(market, ticker, future.SecurityType);
            var earlyCloses = futureEntry.ExchangeHours.EarlyCloses;
            var holidays = futureEntry.ExchangeHours.Holidays;

            var goodFriday = DateTime.Parse("4/7/2023", CultureInfo.InvariantCulture);

            Assert.AreEqual(isEarlyClose, earlyCloses.Keys.Contains(goodFriday));
            Assert.AreEqual(!isEarlyClose, holidays.Contains(goodFriday));
        }

        [TestCase("ES", Market.CME)]
        [TestCase("30Y", Market.CBOT)]
        [TestCase("M6B", Market.CME)]
        [TestCase("BTC", Market.CME)]
        [TestCase("ABT", Market.NYMEX)]
        [TestCase("AUP", Market.COMEX)]
        public void EarlyClosesResumesAgainIfLateOpen(string futureTicker, string market)
        {
            var provider = MarketHoursDatabase.FromDataFolder();
            var ticker = OptionSymbol.MapToUnderlying(futureTicker, SecurityType.Future);
            var future = Symbol.Create(ticker, SecurityType.Future, market);

            var futureEntry = provider.GetEntry(market, ticker, future.SecurityType);
            var earlyCloseDate = DateTime.Parse("9/4/2023", CultureInfo.InvariantCulture);
            var earlyCloseHour = futureEntry.ExchangeHours.EarlyCloses[earlyCloseDate];
            var lateOpenHour = futureEntry.ExchangeHours.LateOpens[earlyCloseDate];

            Assert.AreEqual(earlyCloseHour, futureEntry.ExchangeHours.GetMarketHours(earlyCloseDate).GetMarketClose(new TimeSpan(0, 0, 0), true));
            Assert.AreEqual(lateOpenHour, futureEntry.ExchangeHours.GetMarketHours(earlyCloseDate).GetMarketOpen(earlyCloseHour, true));
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
        public void AllMarketsAreAlwaysOpenWhenForceExchangeAlwaysOpenIsTrue()
        {
            var originalConfigValue = Config.Get("force-exchange-always-open");
            // Force all exchanges to be treated as always open, regardless of their actual hours
            Config.Set("force-exchange-always-open", "true");

            string file = Path.Combine("TestData", "SampleMarketHoursDatabase.json");
            var marketHoursDatabase = GetMarketHoursDatabase(file);

            foreach (var entry in marketHoursDatabase.ExchangeHoursListing)
            {
                var key = entry.Key;
                var exchangeHours = marketHoursDatabase.GetExchangeHours(key.Market, key.Symbol, key.SecurityType);

                // Assert that the market is considered always open under this configuration
                Assert.IsTrue(exchangeHours.IsMarketAlwaysOpen);
            }

            // Restore original config value after test
            Config.Set("force-exchange-always-open", originalConfigValue);
        }

        [Test]
        public void ReadsFxcmForexDataTimeZone()
        {
            string file = Path.Combine("TestData", "SampleMarketHoursDatabase.json");
            var marketHoursDatabase = GetMarketHoursDatabase(file);

            Assert.AreEqual(TimeZones.EasternStandard, marketHoursDatabase.GetDataTimeZone(Market.FXCM, null, SecurityType.Forex));
        }

        [TestCase("SPX", SecurityType.Index, Market.USA)]
        [TestCase("SPXW", SecurityType.Index, Market.USA)]
        [TestCase("AAPL", SecurityType.Equity, Market.USA)]
        [TestCase("SPY", SecurityType.Equity, Market.USA)]

        [TestCase("GC", SecurityType.Future, Market.COMEX)]
        [TestCase("SI", SecurityType.Future, Market.COMEX)]
        [TestCase("HG", SecurityType.Future, Market.COMEX)]
        [TestCase("ES", SecurityType.Future, Market.CME)]
        [TestCase("NQ", SecurityType.Future, Market.CME)]
        [TestCase("CL", SecurityType.Future, Market.NYMEX)]
        [TestCase("NG", SecurityType.Future, Market.NYMEX)]
        [TestCase("ZB", SecurityType.Future, Market.CBOT)]
        [TestCase("ZC", SecurityType.Future, Market.CBOT)]
        [TestCase("ZS", SecurityType.Future, Market.CBOT)]
        [TestCase("ZT", SecurityType.Future, Market.CBOT)]
        [TestCase("ZW", SecurityType.Future, Market.CBOT)]
        public void MissingOptionsEntriesResolveToUnderlyingMarketHours(string optionTicker, SecurityType securityType, string market)
        {
            var provider = MarketHoursDatabase.FromDataFolder();
            var underlyingTIcker = OptionSymbol.MapToUnderlying(optionTicker, securityType);
            var underlying = Symbol.Create(underlyingTIcker, securityType, market);
            var option = Symbol.CreateOption(
                underlying,
                market,
                default,
                default,
                default,
                SecurityIdentifier.DefaultDate);

            var underlyingEntry = provider.GetEntry(market, underlying, underlying.SecurityType);
            var optionEntry = provider.GetEntry(market, option, option.SecurityType);

            if (securityType == SecurityType.Future)
            {
                Assert.AreEqual(underlyingEntry, optionEntry);
            }
            else
            {
                Assert.AreEqual(underlyingEntry.ExchangeHours.Holidays, optionEntry.ExchangeHours.Holidays);
                Assert.AreEqual(underlyingEntry.ExchangeHours.LateOpens, optionEntry.ExchangeHours.LateOpens);
                Assert.AreEqual(underlyingEntry.ExchangeHours.EarlyCloses, optionEntry.ExchangeHours.EarlyCloses);
            }
        }

        [TestCase("SPX")]
        [TestCase("NDX")]
        [TestCase("VIX")]
        public void USIndexOptionsResolveToUnderlyingEarlyCloses(string optionTicker)
        {
            var provider = MarketHoursDatabase.FromDataFolder();
            var underlyingTicker = OptionSymbol.MapToUnderlying(optionTicker, SecurityType.Index);
            var underlying = Symbol.Create(underlyingTicker, SecurityType.Index, Market.USA);
            var option = Symbol.CreateOption(
                underlying,
                Market.USA,
                default,
                default,
                default,
                SecurityIdentifier.DefaultDate);

            var underlyingEntry = provider.GetEntry(Market.USA, underlying, underlying.SecurityType);
            var optionEntry = provider.GetEntry(Market.USA, option, option.SecurityType);
            Assert.AreEqual(underlyingEntry.ExchangeHours.EarlyCloses, optionEntry.ExchangeHours.EarlyCloses);
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

        [TestCase("UWU", SecurityType.Base)]
        [TestCase("SPX", SecurityType.Index)]
        public void CustomEntriesAreNotLostWhenReset(string ticker, SecurityType securityType)
        {
            var database = MarketHoursDatabase.FromDataFolder();
            var hours = SecurityExchangeHours.AlwaysOpen(TimeZones.Chicago);
            var entry = database.SetEntry(Market.USA, ticker, securityType, hours);

            MarketHoursDatabase.Entry returnedEntry;
            Assert.IsTrue(database.TryGetEntry(Market.USA, ticker, securityType, out returnedEntry));
            Assert.AreEqual(returnedEntry, entry);
            Assert.DoesNotThrow(() => database.UpdateDataFolderDatabase());
            Assert.IsTrue(database.TryGetEntry(Market.USA, ticker, securityType, out returnedEntry));
            Assert.AreEqual(returnedEntry, entry);
        }

        [Test]
        public void VerifyMarketHoursDataIntegrityForAllSymbols()
        {
            // Load the market hours database
            var marketHoursDatabase = MarketHoursDatabase.FromDataFolder();

            // Test all specific entries in parallel
            Parallel.ForEach(marketHoursDatabase.ExchangeHoursListing, entry =>
            {
                var securityType = entry.Key.SecurityType;
                var ticker = entry.Key.Symbol;
                Assert.IsFalse(string.IsNullOrEmpty(ticker), $"Ticker is null or empty");
                var market = entry.Key.Market;

                // Create symbol
                Symbol symbol;
                if (ticker.Contains("[*]") || ticker == "*")
                {
                    symbol = Symbol.Create("TEST_SYMBOL", securityType, market);
                }
                else
                {
                    symbol = Symbol.Create(ticker, securityType, market);
                }

                TestMarketHoursForSymbol(marketHoursDatabase, market, symbol, securityType);
            });
        }

        private static void TestMarketHoursForSymbol(MarketHoursDatabase marketHoursDatabase, string market, Symbol symbol, SecurityType securityType)
        {
            // Define date range (1998-01-01 to today, checking daily)
            var startDate = new DateTime(1998, 1, 1);
            var endDate = DateTime.Now;

            var exchangeHours = marketHoursDatabase.GetExchangeHours(market, symbol, securityType);

            // Check every day
            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                // Get market hours for this date
                var marketHours = exchangeHours.GetMarketHours(date);

                // Ensure market hours exist for the date
                Assert.IsNotNull(exchangeHours, "Exchange hours should not be null.");

                var segments = marketHours.Segments;
                for (int i = 1; i < segments.Count; i++)
                {
                    // Ensure segments do not overlap
                    Assert.LessOrEqual(segments[i - 1].End, segments[i].Start,
                        $"Segments overlap for {symbol} on {date:yyyy-MM-dd} between {segments[i - 1]} and {segments[i]}");
                }

                bool hasEarlyClose = exchangeHours.EarlyCloses.TryGetValue(date, out var earlyCloseTime);
                bool hasLateOpen = exchangeHours.LateOpens.TryGetValue(date, out var lateOpenTime);
                if (hasEarlyClose && hasLateOpen && segments.Count > 0)
                {
                    // Ensure LateOpen time is not after market close, but only when there is an EarlyClose
                    Assert.LessOrEqual(lateOpenTime, segments[^1].End,
                        $"Late open time {lateOpenTime} is after market close {segments[^1].End} for {symbol} on {date:yyyy-MM-dd}");
                }

                if (exchangeHours.Holidays.Contains(date))
                {
                    // Ensure market is fully closed on holidays
                    Assert.IsTrue(marketHours.IsClosedAllDay,
                        $"Market should be fully closed on holiday {date:yyyy-MM-dd} for {symbol}");
                }
            }
        }

        private static MarketHoursDatabase GetMarketHoursDatabase(string file)
        {
            return MarketHoursDatabase.FromFile(file);
        }
    }
}
