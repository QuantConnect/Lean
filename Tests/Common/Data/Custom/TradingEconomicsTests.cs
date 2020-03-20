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
using QuantConnect.Data;
using QuantConnect.Data.Custom.TradingEconomics;
using QuantConnect.Data.UniverseSelection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Tests.Common.Data.Custom
{
    [TestFixture]
    public class TradingEconomicsTests
    {
        public const string TestCalendarJson =
@"[{
  ""CalendarId"": ""0"",
  ""Date"": ""2019-01-01T00:00:00"",
  ""Country"": ""United States"",
  ""Category"": ""PPI PCE"",
  ""Event"": ""PPI PCE YoY"",
  ""Reference"": ""Jan"",
  ""Source"": ""U.S."",
  ""Actual"": 0,
  ""Previous"": 0,
  ""Forecast"": null,
  ""TEForecast"": 0,
  ""DateSpan"": ""0"",
  ""Importance"": 2,
  ""LastUpdate"": ""2019-01-01T00:00:00"",
  ""Revised"": 0,
  ""OCountry"": ""United States"",
  ""OCategory"": ""PPI PCE"",
  ""Ticker"": ""US"",
  ""IsPercentage"": true,
  ""DataType"": 0,
  ""IsFillForward"": false,
  ""Value"": 0.0,
  ""Price"": 0.0
}]";


        public const string TestCalendarRawAPIResponseJSON =
@"[{
  ""CalendarId"": ""0"",
  ""Date"": ""2019-01-01T00:00:00"",
  ""Country"": ""United States"",
  ""Category"": ""PPI PCE"",
  ""Event"": ""PPI PCE YoY"",
  ""Reference"": ""Jan"",
  ""Source"": ""U.S."",
  ""Actual"": ""0.0%"",
  ""Previous"": ""0.0"",
  ""Forecast"": null,
  ""TEForecast"": ""0"",
  ""DateSpan"": ""0"",
  ""Importance"": 2,
  ""LastUpdate"": ""2019-01-01T00:00:00"",
  ""Revised"": ""0"",
  ""OCountry"": ""United States"",
  ""OCategory"": ""PPI PCE"",
  ""Ticker"": ""US"",
  ""Symbol"": ""US"",
  ""DataType"": 0,
  ""IsFillForward"": false,
  ""Value"": 0.0,
  ""Price"": 0.0
}]";

        [TestCase("EarnInGs n.s.a", "earnings not seasonally adjusted")]
        [TestCase("                 GDP", "gdp")]
        [TestCase("  GDP  ", "gdp")]
        [TestCase("GDP 1st est", "gdp first estimate")]
        [TestCase("GDP PPI", "gdp producer price index")]
        [TestCase("GDP PPI 1st EsT", "gdp producer price index first estimate")]
        [TestCase("G.d.P. P.P.I. 1st EsT .... n.s.a", "gdp producer price index first estimate not seasonally adjusted")]
        [TestCase("GDP Growth Rate 2014", "gdp growth rate")]
        [TestCase("GDP Growth Rate 2015", "gdp growth rate")]
        [TestCase("GDP Westpac Growth", "westpac gdp growth")]
        [TestCase("Westpac GDP Growth", "westpac gdp growth")]
        public void CalendarFilterAppliedCorrectly(string eventName, string expected)
        {
            var filteredName = TradingEconomicsEventFilter.FilterEvent(eventName);

            Assert.AreEqual(expected, filteredName);
        }

        [Test]
        public void DeserializesProperlyUsingCSV()
        {
            var instance = new TradingEconomicsCalendar();
            var csv = JsonConvert.DeserializeObject<List<TradingEconomicsCalendar>>(TestCalendarJson).First().ToCsv();

            var result = instance.Reader(
                new SubscriptionDataConfig(
                    typeof(TradingEconomicsCalendar),
                    Symbol.CreateBase(typeof(TradingEconomicsCalendar), Symbol.Create($"UNITED-STATES{TradingEconomics.Calendar.Delimiter}US", SecurityType.Base, QuantConnect.Market.USA), QuantConnect.Market.USA),
                    Resolution.Daily,
                    TimeZones.Utc,
                    TimeZones.Utc,
                    false,
                    false,
                    false,
                    isCustom: true
                ),
                csv,
                new DateTime(2019, 1, 1),
                false
            );

            var calendar = (TradingEconomicsCalendar)result;

            Assert.AreEqual("0", calendar.CalendarId);
            Assert.AreEqual(new DateTime(2019, 1, 1), calendar.Time.Date);
            Assert.AreEqual(new DateTime(2019, 1, 1), calendar.EndTime.Date);
            Assert.AreEqual("United States", calendar.Country);
            Assert.AreEqual("PPI PCE", calendar.Category);
            Assert.AreEqual("producer price index personal consumption expenditure price index yoy", calendar.Event);
            Assert.AreEqual("Jan", calendar.Reference);
            Assert.AreEqual("U.S.", calendar.Source);
            Assert.AreEqual(0m, calendar.Actual);
            Assert.AreEqual(0m, calendar.Previous);
            Assert.AreEqual(null, calendar.Forecast);
            Assert.AreEqual(0m, calendar.TradingEconomicsForecast);
            Assert.AreEqual("0", calendar.DateSpan);
            Assert.AreEqual(TradingEconomicsImportance.High, calendar.Importance);
            Assert.AreEqual(new DateTime(2019, 1, 1), calendar.LastUpdate.Date);
            Assert.AreEqual(0m, calendar.Revised);
            Assert.AreEqual("United States", calendar.OCountry);
            Assert.AreEqual("PPI PCE", calendar.OCategory);
            Assert.AreEqual("US", calendar.Ticker);
            Assert.AreEqual(true, calendar.IsPercentage);
        }

        [Test]
        public void DeserializesProperlyUsingJsonStringReader()
        {
            var instance = new TradingEconomicsCalendar();

            var result = instance.Reader(
                new SubscriptionDataConfig(
                    typeof(TradingEconomicsCalendar),
                    Symbol.CreateBase(typeof(TradingEconomicsCalendar), Symbol.Create("UNITED-STATES//US", SecurityType.Base, QuantConnect.Market.USA), QuantConnect.Market.USA),
                    Resolution.Daily,
                    TimeZones.Utc,
                    TimeZones.Utc,
                    false,
                    false,
                    false,
                    isCustom: true
                ),
                TestCalendarRawAPIResponseJSON,
                new DateTime(2019, 1, 1),
                true
            );

            var calendar = (TradingEconomicsCalendar)((BaseDataCollection)result).Data.Single();

            Assert.AreEqual("0", calendar.CalendarId);
            Assert.AreEqual(new DateTime(2019, 1, 1), calendar.Time.Date);
            Assert.AreEqual(new DateTime(2019, 1, 1), calendar.EndTime.Date);
            Assert.AreEqual("United States", calendar.Country);
            Assert.AreEqual("PPI PCE", calendar.Category);
            Assert.AreEqual("producer price index personal consumption expenditure price index yoy", calendar.Event);
            Assert.AreEqual("Jan", calendar.Reference);
            Assert.AreEqual("U.S.", calendar.Source);
            Assert.AreEqual(0m, calendar.Actual);
            Assert.AreEqual(0m, calendar.Previous);
            Assert.AreEqual(null, calendar.Forecast);
            Assert.AreEqual(0m, calendar.TradingEconomicsForecast);
            Assert.AreEqual("0", calendar.DateSpan);
            Assert.AreEqual(TradingEconomicsImportance.High, calendar.Importance);
            Assert.AreEqual(new DateTime(2019, 1, 1), calendar.LastUpdate.Date);
            Assert.AreEqual(0m, calendar.Revised);
            Assert.AreEqual("United States", calendar.OCountry);
            Assert.AreEqual("PPI PCE", calendar.OCategory);
            Assert.AreEqual("US", calendar.Ticker);
            Assert.AreEqual(true, calendar.IsPercentage);
        }

        [Test]
        public void ReaderDeserializationProducesSameResult()
        {
            var instance = new TradingEconomicsCalendar();

            var csv = JsonConvert.DeserializeObject<List<TradingEconomicsCalendar>>(TestCalendarJson).Single().ToCsv();

            var resultBacktest = instance.Reader(
                new SubscriptionDataConfig(
                    typeof(TradingEconomicsCalendar),
                    Symbol.CreateBase(typeof(TradingEconomicsCalendar), Symbol.Create("UNITED-STATES//US", SecurityType.Base, QuantConnect.Market.USA), QuantConnect.Market.USA),
                    Resolution.Daily,
                    TimeZones.Utc,
                    TimeZones.Utc,
                    false,
                    false,
                    false,
                    isCustom: true
                ),
                csv,
                new DateTime(2019, 1, 1),
                false
            );

            var resultLive = instance.Reader(
                new SubscriptionDataConfig(
                    typeof(TradingEconomicsCalendar),
                    Symbol.CreateBase(typeof(TradingEconomicsCalendar), Symbol.Create("UNITED-STATES//US", SecurityType.Base, QuantConnect.Market.USA), QuantConnect.Market.USA),
                    Resolution.Daily,
                    TimeZones.Utc,
                    TimeZones.Utc,
                    false,
                    false,
                    false,
                    isCustom: true
                ),
                TestCalendarRawAPIResponseJSON,
                new DateTime(2019, 1, 1),
                true
            );

            var calendarBacktest = (TradingEconomicsCalendar)resultBacktest;
            var calendarLive = (TradingEconomicsCalendar)((BaseDataCollection)resultLive).Data.Single();

            Assert.AreEqual(calendarBacktest.CalendarId, calendarLive.CalendarId);
            Assert.AreEqual(calendarBacktest.Time.Date, calendarLive.Time.Date);
            Assert.AreEqual(calendarBacktest.EndTime.Date, calendarLive.EndTime.Date);
            Assert.AreEqual(calendarBacktest.Country, calendarLive.Country);
            Assert.AreEqual(calendarBacktest.Category, calendarLive.Category);
            Assert.AreEqual(calendarBacktest.Event, calendarLive.Event);
            Assert.AreEqual(calendarBacktest.Reference, calendarLive.Reference);
            Assert.AreEqual(calendarBacktest.Source, calendarLive.Source);
            Assert.AreEqual(calendarBacktest.Actual, calendarLive.Actual);
            Assert.AreEqual(calendarBacktest.Previous, calendarLive.Previous);
            Assert.AreEqual(calendarBacktest.Forecast, calendarLive.Forecast);
            Assert.AreEqual(calendarBacktest.TradingEconomicsForecast, calendarLive.TradingEconomicsForecast);
            Assert.AreEqual(calendarBacktest.DateSpan, calendarLive.DateSpan);
            Assert.AreEqual(calendarBacktest.Importance, calendarLive.Importance);
            Assert.AreEqual(calendarBacktest.LastUpdate.Date, calendarLive.LastUpdate.Date);
            Assert.AreEqual(calendarBacktest.Revised, calendarLive.Revised);
            Assert.AreEqual(calendarBacktest.OCountry, calendarLive.OCountry);
            Assert.AreEqual(calendarBacktest.OCategory, calendarLive.OCategory);
            Assert.AreEqual(calendarBacktest.Ticker, calendarLive.Ticker);
            Assert.AreEqual(calendarBacktest.IsPercentage, calendarLive.IsPercentage);
        }

        [Test]
        public void ReturnsNullOnInvalidSymbolOrDate_AndContinuesWithoutErrors()
        {
            var instance = new TradingEconomicsCalendar();
            var normalLines = new List<string>();
            var corruptedLines = new List<string>();
            var deserialized = JsonConvert.DeserializeObject<List<TradingEconomicsCalendar>>(TestCalendarJson).First();

            for (var i = 0; i < 5; i++)
            {
                normalLines.Add(deserialized.ToCsv());
            }

            deserialized.Ticker = "Foobar" + deserialized.Ticker;
            for (var i = 0; i < 5; i++)
            {
                corruptedLines.Add(deserialized.ToCsv());
            }

            var corruptedResult = instance.Reader(
                new SubscriptionDataConfig(
                    typeof(TradingEconomicsCalendar),
                    Symbol.CreateBase(typeof(TradingEconomicsCalendar), Symbol.Create("UNITED-STATES//US", SecurityType.Base, QuantConnect.Market.USA), QuantConnect.Market.USA),
                    Resolution.Daily,
                    TimeZones.Utc,
                    TimeZones.Utc,
                    false,
                    false,
                    false,
                    isCustom: true
                ),
                corruptedLines.First(),
                new DateTime(2019, 1, 1),
                false
            );

            var corruptedNormalResult = instance.Reader(
                new SubscriptionDataConfig(
                    typeof(TradingEconomicsCalendar),
                    Symbol.CreateBase(typeof(TradingEconomicsCalendar), Symbol.Create("UNITED-STATES//US", SecurityType.Base, QuantConnect.Market.USA), QuantConnect.Market.USA),
                    Resolution.Daily,
                    TimeZones.Utc,
                    TimeZones.Utc,
                    false,
                    false,
                    false,
                    isCustom: true
                ),
                normalLines.First(),
                DateTime.MaxValue,
                false
            );

            Assert.AreEqual(null, corruptedResult);
            Assert.AreEqual(null, corruptedNormalResult);

            foreach (var line in normalLines.Skip(1))
            {
                var calendar = (TradingEconomicsCalendar)instance.Reader(
                    new SubscriptionDataConfig(
                        typeof(TradingEconomicsCalendar),
                        Symbol.CreateBase(typeof(TradingEconomicsCalendar), Symbol.Create("UNITED-STATES//US", SecurityType.Base, QuantConnect.Market.USA), QuantConnect.Market.USA),
                        Resolution.Daily,
                        TimeZones.Utc,
                        TimeZones.Utc,
                        false,
                        false,
                        false,
                        isCustom: true
                    ),
                    line,
                    new DateTime(2019, 1, 1),
                    false
                );
                Assert.AreEqual("0", calendar.CalendarId);
                Assert.AreEqual(new DateTime(2019, 1, 1), calendar.Time.Date);
                Assert.AreEqual(new DateTime(2019, 1, 1), calendar.EndTime.Date);
                Assert.AreEqual("United States", calendar.Country);
                Assert.AreEqual("PPI PCE", calendar.Category);
                Assert.AreEqual("producer price index personal consumption expenditure price index yoy", calendar.Event);
                Assert.AreEqual("Jan", calendar.Reference);
                Assert.AreEqual("U.S.", calendar.Source);
                Assert.AreEqual(0m, calendar.Actual);
                Assert.AreEqual(0m, calendar.Previous);
                Assert.AreEqual(null, calendar.Forecast);
                Assert.AreEqual(0m, calendar.TradingEconomicsForecast);
                Assert.AreEqual("0", calendar.DateSpan);
                Assert.AreEqual(TradingEconomicsImportance.High, calendar.Importance);
                Assert.AreEqual(new DateTime(2019, 1, 1), calendar.LastUpdate.Date);
                Assert.AreEqual(0m, calendar.Revised);
                Assert.AreEqual("United States", calendar.OCountry);
                Assert.AreEqual("PPI PCE", calendar.OCategory);
                Assert.AreEqual("US", calendar.Ticker);
                Assert.AreEqual(true, calendar.IsPercentage);
            }
        }

        [TestCase("United States", "USD")]
        [TestCase("Canada", "CAD")]
        [TestCase("Australia", "AUD")]
        [TestCase("China", "CNY")]
        [TestCase("Korea", "KRW")]
        [TestCase("Foobar", null)]
        public void GetsCountryCurrencyCode(string country, string expectedCurrencyCode)
        {
            var currencyCode = TradingEconomicsCalendar.CountryToCurrencyCode(country);

            Assert.AreEqual(expectedCurrencyCode, currencyCode);
        }

        [TestCase("2.5.0", 2.5, false)]
        [TestCase("Foobar", null, false)]
        [TestCase("1.0%", 0.01, true)]
        [TestCase("--1.0", -1.0, false)]
        [TestCase("1.0T", 1000000000000.0, false)]
        [TestCase("1.0B", 1000000000.0, false)]
        [TestCase("1.0M", 1000000.0, false)]
        [TestCase("1.0K", 1000.0, false)]
        [TestCase("1.0K%", 0.01, true)]
        [TestCase("1", 0.01, true)]
        public void DecimalIsParsedCorrectly(string value, double? expected, bool inPercentage)
        {
            // Cast inside since we can't pass in decimal values through TestCase attributes
            Assert.AreEqual((decimal?)expected, TradingEconomicsCalendar.ParseDecimal(value, inPercentage));
        }

        [Test]
        public void SerializeRoundTrip()
        {
            var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };

            var time = new DateTime(2020, 3, 20, 14, 0, 0);
            var symbol = Symbol.Create(
                TradingEconomics.Calendar.UnitedStates.ExistingHomeSales,
                SecurityType.Base,
                QuantConnect.Market.USA,
                baseDataType: typeof(TradingEconomicsCalendar)
            );

            var item = new TradingEconomicsCalendar
            {
                Country = "United States",
                Category = "Existing Home Sales",
                Importance = TradingEconomicsImportance.Medium,
                Event = "existing home sales",
                Actual = 5770000m,
                Previous = 5460000m,
                LastUpdate = time,
                EndTime = time,
                Symbol = symbol,
            };

            var serialized = JsonConvert.SerializeObject(item, settings);
            var deserialized = JsonConvert.DeserializeObject<TradingEconomicsCalendar>(serialized, settings);

            Assert.AreEqual("United States", deserialized.Country);
            Assert.AreEqual("Existing Home Sales", deserialized.Category);
            Assert.AreEqual(TradingEconomicsImportance.Medium, deserialized.Importance);
            Assert.AreEqual(5770000m, deserialized.Actual);
            Assert.AreEqual(5460000m, deserialized.Previous);
            Assert.AreEqual(time, deserialized.LastUpdate);
            Assert.AreEqual(time, deserialized.Time);
            Assert.AreEqual(time, deserialized.EndTime);
            Assert.AreEqual(symbol, deserialized.Symbol);
        }
    }
}
