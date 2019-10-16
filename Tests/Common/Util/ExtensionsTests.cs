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
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using Python.Runtime;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Indicators;
using QuantConnect.Orders;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Common.Util
{
    [TestFixture]
    public class ExtensionsTests
    {
        [Test]
        public void IsSubclassOfGenericWorksWorksForNonGenericType()
        {
            Assert.IsTrue(typeof(Derived2).IsSubclassOfGeneric(typeof(Derived1)));
        }

        [Test]
        public void IsSubclassOfGenericWorksForGenericTypeWithParameter()
        {
            Assert.IsTrue(typeof(Derived1).IsSubclassOfGeneric(typeof(Super<int>)));
            Assert.IsFalse(typeof(Derived1).IsSubclassOfGeneric(typeof(Super<bool>)));
        }

        [Test]
        public void IsSubclassOfGenericWorksForGenericTypeDefinitions()
        {
            Assert.IsTrue(typeof(Derived1).IsSubclassOfGeneric(typeof(Super<>)));
            Assert.IsTrue(typeof(Derived2).IsSubclassOfGeneric(typeof(Super<>)));
        }

        [Test]
        public void DateTimeRoundDownFullDayDoesntRoundDownByDay()
        {
            var date = new DateTime(2000, 01, 01);
            var rounded = date.RoundDown(TimeSpan.FromDays(1));
            Assert.AreEqual(date, rounded);
        }

        [Test]
        public void GetBetterTypeNameHandlesRecursiveGenericTypes()
        {
            var type = typeof (Dictionary<List<int>, Dictionary<int, string>>);
            const string expected = "Dictionary<List<Int32>, Dictionary<Int32, String>>";
            var actual = type.GetBetterTypeName();
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void ExchangeRoundDownSkipsWeekends()
        {
            var time = new DateTime(2015, 05, 02, 18, 01, 00);
            var expected = new DateTime(2015, 05, 01);
            var hours = MarketHoursDatabase.FromDataFolder().GetExchangeHours(Market.FXCM, null, SecurityType.Forex);
            var exchangeRounded = time.ExchangeRoundDown(Time.OneDay, hours, false);
            Assert.AreEqual(expected, exchangeRounded);
        }

        [Test]
        public void ExchangeRoundDownHandlesMarketOpenTime()
        {
            var time = new DateTime(2016, 1, 25, 9, 31, 0);
            var expected = time.Date;
            var hours = MarketHoursDatabase.FromDataFolder().GetExchangeHours(Market.USA, null, SecurityType.Equity);
            var exchangeRounded = time.ExchangeRoundDown(Time.OneDay, hours, false);
            Assert.AreEqual(expected, exchangeRounded);
        }

        [Test]
        public void ConvertToSkipsDiscontinuitiesBecauseOfDaylightSavingsStart_AddingOneHour()
        {
            var expected = new DateTime(2014, 3, 9, 3, 0, 0);
            var time = new DateTime(2014, 3, 9, 2, 0, 0).ConvertTo(TimeZones.NewYork, TimeZones.NewYork);
            var time2 = new DateTime(2014, 3, 9, 2, 0, 1).ConvertTo(TimeZones.NewYork, TimeZones.NewYork);
            Assert.AreEqual(expected, time);
            Assert.AreEqual(expected, time2);
        }

        [Test]
        public void ConvertToIgnoreDaylightSavingsEnd_SubtractingOneHour()
        {
            var time1Expected = new DateTime(2014, 11, 2, 1, 59, 59);
            var time2Expected = new DateTime(2014, 11, 2, 2, 0, 0);
            var time3Expected = new DateTime(2014, 11, 2, 2, 0, 1);
            var time1 = time1Expected.ConvertTo(TimeZones.NewYork, TimeZones.NewYork);
            var time2 = time2Expected.ConvertTo(TimeZones.NewYork, TimeZones.NewYork);
            var time3 = time3Expected.ConvertTo(TimeZones.NewYork, TimeZones.NewYork);

            Assert.AreEqual(time1Expected, time1);
            Assert.AreEqual(time2Expected, time2);
            Assert.AreEqual(time3Expected, time3);
        }

        [Test]
        public void ExchangeRoundDownInTimeZoneSkipsWeekends()
        {
            // moment before EST market open in UTC (time + one day)
            var time = new DateTime(2017, 10, 01, 9, 29, 59).ConvertToUtc(TimeZones.NewYork);
            var expected = new DateTime(2017, 09, 29).ConvertFromUtc(TimeZones.NewYork);
            var hours = MarketHoursDatabase.FromDataFolder().GetExchangeHours(Market.USA, null, SecurityType.Equity);
            var exchangeRounded = time.ExchangeRoundDownInTimeZone(Time.OneDay, hours, TimeZones.Utc, false);
            Assert.AreEqual(expected, exchangeRounded);
        }

        [Test]
        // This unit test reproduces a fixed infinite loop situation, due to a daylight saving time change, in ExchangeRoundDownInTimeZone, GH issue 2368.
        public void ExchangeRoundDownInTimeZoneCorrectValuesAroundDaylightTimeChanges_AddingOneHour_UTC()
        {
            var time = new DateTime(2014, 3, 9, 16, 0, 1);
            var expected = new DateTime(2014, 3, 7, 16, 0, 0);
            var hours = MarketHoursDatabase.FromDataFolder().GetExchangeHours(Market.Oanda, null, SecurityType.Forex);
            var exchangeRounded = time.ExchangeRoundDownInTimeZone(Time.OneHour, hours, TimeZones.Utc, false);
            Assert.AreEqual(expected, exchangeRounded);
        }

        [Test]
        // This unit test reproduces a fixed infinite loop situation, due to a daylight saving time change, in ExchangeRoundDownInTimeZone, GH issue 2368.
        public void ExchangeRoundDownInTimeZoneCorrectValuesAroundDaylightTimeChanges_SubtractingOneHour_UTC()
        {
            var time = new DateTime(2014, 11, 2, 2, 0, 1);
            var expected = new DateTime(2014, 10, 31, 16, 0, 0);
            var hours = MarketHoursDatabase.FromDataFolder().GetExchangeHours(Market.Oanda, null, SecurityType.Forex);
            var exchangeRounded = time.ExchangeRoundDownInTimeZone(Time.OneHour, hours, TimeZones.Utc, false);
            Assert.AreEqual(expected, exchangeRounded);
        }

        [Test]
        public void ExchangeRoundDownInTimeZoneCorrectValuesAroundDaylightTimeChanges_AddingOneHour_ExtendedHours_UTC()
        {
            var time = new DateTime(2014, 3, 9, 2, 0, 1);
            var expected = new DateTime(2014, 3, 9, 2, 0, 0);
            var hours = MarketHoursDatabase.FromDataFolder().GetExchangeHours(Market.GDAX, null, SecurityType.Crypto);
            var exchangeRounded = time.ExchangeRoundDownInTimeZone(Time.OneHour, hours, TimeZones.Utc, true);
            Assert.AreEqual(expected, exchangeRounded);
        }

        [Test]
        public void ExchangeRoundDownInTimeZoneCorrectValuesAroundDaylightTimeChanges_SubtractingOneHour_ExtendedHours_UTC()
        {
            var time = new DateTime(2014, 11, 2, 2, 0, 1);
            var expected = new DateTime(2014, 11, 2, 2, 0, 0);
            var hours = MarketHoursDatabase.FromDataFolder().GetExchangeHours(Market.GDAX, null, SecurityType.Crypto);
            var exchangeRounded = time.ExchangeRoundDownInTimeZone(Time.OneHour, hours, TimeZones.Utc, true);
            Assert.AreEqual(expected, exchangeRounded);
        }

        [Test]
        // this unit test reproduces a fixed infinite loop situation, due to a daylight saving time change, GH issue 3707.
        public void RoundDownInTimeZoneAroundDaylightTimeChanges()
        {
            // sydney time advanced Sunday, 6 October 2019, 02:00:00 clocks were turned forward 1 hour to
            // Sunday, 6 October 2019, 03:00:00 local daylight time instead.
            var timeAt = new DateTime(2019, 10, 6, 10, 0, 0);
            var expected = new DateTime(2019, 10, 5, 10, 0, 0);

            var exchangeRoundedAt = timeAt.RoundDownInTimeZone(Time.OneDay, TimeZones.Sydney, TimeZones.Utc);
            // even though there is an entire 'roundingInterval' unit (1 day) between 'timeAt' and 'expected' round down
            // is affected by daylight savings and rounds down the timeAt
            Assert.AreEqual(expected, exchangeRoundedAt);

            timeAt = new DateTime(2019, 10, 7, 10, 0, 0);
            expected = new DateTime(2019, 10, 6, 11, 0, 0);

            exchangeRoundedAt = timeAt.RoundDownInTimeZone(Time.OneDay, TimeZones.Sydney, TimeZones.Utc);
            Assert.AreEqual(expected, exchangeRoundedAt);
        }

        [Test]
        public void RoundDownInTimeZoneReturnsCorrectValuesAroundDaylightTimeChanges_AddingOneHour_UTC()
        {
            var timeAt = new DateTime(2014, 3, 9, 2, 0, 0);
            var timeAfter = new DateTime(2014, 3, 9, 2, 0, 1);
            var timeBefore = new DateTime(2014, 3, 9, 1, 59, 59);
            var timeAfterDaylightTimeChanges = new DateTime(2014, 3, 9, 3, 0, 0);

            var hours = MarketHoursDatabase.FromDataFolder().GetExchangeHours(Market.Oanda, null, SecurityType.Forex);

            var exchangeRoundedAt = timeAt.RoundDownInTimeZone(Time.OneSecond, hours.TimeZone, TimeZones.Utc);
            var exchangeRoundedAfter = timeAfter.RoundDownInTimeZone(Time.OneSecond, hours.TimeZone, TimeZones.Utc);
            var exchangeRoundedBefore = timeBefore.RoundDownInTimeZone(Time.OneSecond, hours.TimeZone, TimeZones.Utc);
            var exchangeRoundedAfterDaylightTimeChanges = timeAfterDaylightTimeChanges.RoundDownInTimeZone(Time.OneSecond, hours.TimeZone, TimeZones.Utc);

            var expected = new DateTime(2014, 3, 9, 3, 0, 0);
            Assert.AreEqual(expected, exchangeRoundedAt);
            Assert.AreEqual(expected, exchangeRoundedAfter);
            Assert.AreEqual(timeBefore, exchangeRoundedBefore);
            Assert.AreEqual(expected, exchangeRoundedAfterDaylightTimeChanges);
        }

        [Test]
        public void RoundDownInTimeZoneReturnsCorrectValuesAroundDaylightTimeChanges_SubtractingOneHour_UTC()
        {
            var timeAt = new DateTime(2014, 11, 2, 2, 0, 0);
            var timeAfter = new DateTime(2014, 11, 2, 2, 0, 1);
            var timeBefore = new DateTime(2014, 11, 2, 1, 59, 59);
            var timeAfterDaylightTimeChanges = new DateTime(2014, 11, 2, 3, 0, 0);

            var hours = MarketHoursDatabase.FromDataFolder().GetExchangeHours(Market.Oanda, null, SecurityType.Forex);

            var exchangeRoundedAt = timeAt.RoundDownInTimeZone(Time.OneSecond, hours.TimeZone, TimeZones.Utc);
            var exchangeRoundedAfter = timeAfter.RoundDownInTimeZone(Time.OneSecond, hours.TimeZone, TimeZones.Utc);
            var exchangeRoundedBefore = timeBefore.RoundDownInTimeZone(Time.OneSecond, hours.TimeZone, TimeZones.Utc);
            var exchangeRoundedAfterDaylightTimeChanges = timeAfterDaylightTimeChanges.RoundDownInTimeZone(Time.OneSecond, hours.TimeZone, TimeZones.Utc);

            Assert.AreEqual(timeAt, exchangeRoundedAt);
            Assert.AreEqual(timeAfter, exchangeRoundedAfter);
            Assert.AreEqual(timeBefore, exchangeRoundedBefore);
            Assert.AreEqual(timeAfterDaylightTimeChanges, exchangeRoundedAfterDaylightTimeChanges);
        }

        [Test]
        public void ExchangeRoundDownInTimeZoneCorrectValuesAroundDaylightTimeChanges_AddingOneHour_NewYork()
        {
            var time = new DateTime(2014, 3, 9, 16, 0, 1);
            var expected = new DateTime(2014, 3, 7, 16, 0, 0);
            var hours = MarketHoursDatabase.FromDataFolder().GetExchangeHours(Market.Oanda, null, SecurityType.Forex);
            var exchangeRounded = time.ExchangeRoundDownInTimeZone(Time.OneHour, hours, TimeZones.NewYork, false);
            Assert.AreEqual(expected, exchangeRounded);
        }

        [Test]
        public void ExchangeRoundDownInTimeZoneCorrectValuesAroundDaylightTimeChanges_SubtractingOneHour_NewYork()
        {
            var time = new DateTime(2014, 11, 2, 2, 0, 1);
            var expected = new DateTime(2014, 10, 31, 16, 0, 0);
            var hours = MarketHoursDatabase.FromDataFolder().GetExchangeHours(Market.Oanda, null, SecurityType.Forex);
            var exchangeRounded = time.ExchangeRoundDownInTimeZone(Time.OneHour, hours, TimeZones.NewYork, false);
            Assert.AreEqual(expected, exchangeRounded);
        }

        [Test]
        public void ExchangeRoundDownInTimeZoneCorrectValuesAroundDaylightTimeChanges_AddingOneHour_ExtendedHours_NewYork()
        {
            var time = new DateTime(2014, 3, 9, 2, 0, 1);
            var expected = new DateTime(2014, 3, 9, 2, 0, 0);
            var hours = MarketHoursDatabase.FromDataFolder().GetExchangeHours(Market.GDAX, null, SecurityType.Crypto);
            var exchangeRounded = time.ExchangeRoundDownInTimeZone(Time.OneHour, hours, TimeZones.NewYork, true);
            Assert.AreEqual(expected, exchangeRounded);
        }

        [Test]
        public void ExchangeRoundDownInTimeZoneCorrectValuesAroundDaylightTimeChanges_SubtractingOneHour_ExtendedHours_NewYork()
        {
            var time = new DateTime(2014, 11, 2, 2, 0, 1);
            var expected = new DateTime(2014, 11, 2, 2, 0, 0);
            var hours = MarketHoursDatabase.FromDataFolder().GetExchangeHours(Market.GDAX, null, SecurityType.Crypto);
            var exchangeRounded = time.ExchangeRoundDownInTimeZone(Time.OneHour, hours, TimeZones.NewYork, true);
            Assert.AreEqual(expected, exchangeRounded);
        }

        [Test]
        public void RoundDownInTimeZoneReturnsCorrectValuesAroundDaylightTimeChanges_AddingOneHour_NewYork()
        {
            var timeAt = new DateTime(2014, 3, 9, 2, 0, 0);
            var timeAfter = new DateTime(2014, 3, 9, 2, 0, 1);
            var timeBefore = new DateTime(2014, 3, 9, 1, 59, 59);
            var timeAfterDaylightTimeChanges = new DateTime(2014, 3, 9, 3, 0, 0);

            var hours = MarketHoursDatabase.FromDataFolder().GetExchangeHours(Market.Oanda, null, SecurityType.Forex);

            var exchangeRoundedAt = timeAt.RoundDownInTimeZone(Time.OneSecond, hours.TimeZone, TimeZones.NewYork);
            var exchangeRoundedAfter = timeAfter.RoundDownInTimeZone(Time.OneSecond, hours.TimeZone, TimeZones.NewYork);
            var exchangeRoundedBefore = timeBefore.RoundDownInTimeZone(Time.OneSecond, hours.TimeZone, TimeZones.NewYork);
            var exchangeRoundedAfterDaylightTimeChanges = timeAfterDaylightTimeChanges.RoundDownInTimeZone(Time.OneSecond, hours.TimeZone, TimeZones.NewYork);

            var expected = new DateTime(2014, 3, 9, 3, 0, 0);
            Assert.AreEqual(expected, exchangeRoundedAt);
            Assert.AreEqual(expected, exchangeRoundedAfter);
            Assert.AreEqual(timeBefore, exchangeRoundedBefore);
            Assert.AreEqual(expected, exchangeRoundedAfterDaylightTimeChanges);
        }

        [Test]
        public void RoundDownInTimeZoneReturnsCorrectValuesAroundDaylightTimeChanges_SubtractingOneHour_NewYork()
        {
            var timeAt = new DateTime(2014, 11, 2, 2, 0, 0);
            var timeAfter = new DateTime(2014, 11, 2, 2, 0, 1);
            var timeBefore = new DateTime(2014, 11, 2, 1, 59, 59);
            var timeAfterDaylightTimeChanges = new DateTime(2014, 11, 2, 3, 0, 0);

            var hours = MarketHoursDatabase.FromDataFolder().GetExchangeHours(Market.Oanda, null, SecurityType.Forex);

            var exchangeRoundedAt = timeAt.RoundDownInTimeZone(Time.OneSecond, hours.TimeZone, TimeZones.NewYork);
            var exchangeRoundedAfter = timeAfter.RoundDownInTimeZone(Time.OneSecond, hours.TimeZone, TimeZones.NewYork);
            var exchangeRoundedBefore = timeBefore.RoundDownInTimeZone(Time.OneSecond, hours.TimeZone, TimeZones.NewYork);
            var exchangeRoundedAfterDaylightTimeChanges = timeAfterDaylightTimeChanges.RoundDownInTimeZone(Time.OneSecond, hours.TimeZone, TimeZones.NewYork);

            Assert.AreEqual(timeAt, exchangeRoundedAt);
            Assert.AreEqual(timeAfter, exchangeRoundedAfter);
            Assert.AreEqual(timeBefore, exchangeRoundedBefore);
            Assert.AreEqual(timeAfterDaylightTimeChanges, exchangeRoundedAfterDaylightTimeChanges);
        }

        [Test]
        public void ConvertsInt32FromString()
        {
            const string input = "12345678";
            var value = input.ToInt32();
            Assert.AreEqual(12345678, value);
        }

        [Test]
        public void ConvertsInt32FromStringWithDecimalTruncation()
        {
            const string input = "12345678.9";
            var value = input.ToInt32();
            Assert.AreEqual(12345678, value);
        }

        [Test]
        public void ConvertsInt64FromString()
        {
            const string input = "12345678900";
            var value = input.ToInt64();
            Assert.AreEqual(12345678900, value);
        }

        [Test]
        public void ConvertsInt64FromStringWithDecimalTruncation()
        {
            const string input = "12345678900.12";
            var value = input.ToInt64();
            Assert.AreEqual(12345678900, value);
        }

        [Test]
        public void ConvertsDecimalFromString()
        {
            const string input = "123.45678";
            var value = input.ToDecimal();
            Assert.AreEqual(123.45678m, value);
        }

        [Test]
        public void ConvertsDecimalFromStringWithExtraWhiteSpace()
        {
            const string input = " 123.45678 ";
            var value = input.ToDecimal();
            Assert.AreEqual(123.45678m, value);
        }

        [Test]
        public void ConvertsDecimalFromIntStringWithExtraWhiteSpace()
        {
            const string input = " 12345678 ";
            var value = input.ToDecimal();
            Assert.AreEqual(12345678m, value);
        }

        [Test]
        public void ConvertsZeroDecimalFromString()
        {
            const string input = "0.45678";
            var value = input.ToDecimal();
            Assert.AreEqual(0.45678m, value);
        }

        [Test]
        public void ConvertsOneNumberDecimalFromString()
        {
            const string input = "1.45678";
            var value = input.ToDecimal();
            Assert.AreEqual(1.45678m, value);
        }

        [Test]
        public void ConvertsZeroDecimalValueFromString()
        {
            const string input = "0";
            var value = input.ToDecimal();
            Assert.AreEqual(0m, value);
        }

        [Test]
        public void ConvertsEmptyDecimalValueFromString()
        {
            const string input = "";
            var value = input.ToDecimal();
            Assert.AreEqual(0m, value);
        }

        [Test]
        public void ConvertsNegativeDecimalFromString()
        {
            const string input = "-123.45678";
            var value = input.ToDecimal();
            Assert.AreEqual(-123.45678m, value);
        }

        [Test]
        public void ConvertsNegativeDecimalFromStringWithExtraWhiteSpace()
        {
            const string input = " -123.45678 ";
            var value = input.ToDecimal();
            Assert.AreEqual(-123.45678m, value);
        }

        [Test]
        public void ConvertsNegativeDecimalFromIntStringWithExtraWhiteSpace()
        {
            const string input = " -12345678 ";
            var value = input.ToDecimal();
            Assert.AreEqual(-12345678m, value);
        }

        [Test]
        public void ConvertsNegativeZeroDecimalFromString()
        {
            const string input = "-0.45678";
            var value = input.ToDecimal();
            Assert.AreEqual(-0.45678m, value);
        }

        [Test]
        public void ConvertsNegavtiveOneNumberDecimalFromString()
        {
            const string input = "-1.45678";
            var value = input.ToDecimal();
            Assert.AreEqual(-1.45678m, value);
        }

        [Test]
        public void ConvertsNegativeZeroDecimalValueFromString()
        {
            const string input = "-0";
            var value = input.ToDecimal();
            Assert.AreEqual(-0m, value);
        }

        [Test]
        public void ConvertsTimeSpanFromString()
        {
            const string input = "16:00";
            var timespan = input.ConvertTo<TimeSpan>();
            Assert.AreEqual(TimeSpan.FromHours(16), timespan);
        }

        [Test]
        public void ConvertsDictionaryFromString()
        {
            var expected = new Dictionary<string, int> {{"a", 1}, {"b", 2}};
            var input = JsonConvert.SerializeObject(expected);
            var actual = input.ConvertTo<Dictionary<string, int>>();
            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void DictionaryAddsItemToExistsList()
        {
            const int key = 0;
            var list = new List<int> {1, 2};
            var dictionary = new Dictionary<int, List<int>> {{key, list}};
            Extensions.Add(dictionary, key, 3);
            Assert.AreEqual(3, list.Count);
            Assert.AreEqual(3, list[2]);
        }

        [Test]
        public void DictionaryAddCreatesNewList()
        {
            const int key = 0;
            var dictionary = new Dictionary<int, List<int>>();
            Extensions.Add(dictionary, key, 1);
            Assert.IsTrue(dictionary.ContainsKey(key));
            var list = dictionary[key];
            Assert.AreEqual(1, list.Count);
            Assert.AreEqual(1, list[0]);
        }

        [Test]
        public void SafeDecimalCasts()
        {
            var input = 2d;
            var output = input.SafeDecimalCast();
            Assert.AreEqual(2m, output);
        }

        [Test]
        public void SafeDecimalCastRespectsUpperBound()
        {
            var input = (double) decimal.MaxValue;
            var output = input.SafeDecimalCast();
            Assert.AreEqual(decimal.MaxValue, output);
        }

        [Test]
        public void SafeDecimalCastRespectsLowerBound()
        {
            var input = (double) decimal.MinValue;
            var output = input.SafeDecimalCast();
            Assert.AreEqual(decimal.MinValue, output);
        }

        [Test]
        [TestCase(1.200, "1.2")]
        [TestCase(1200, "1200")]
        [TestCase(123.456, "123.456")]
        public void NormalizeDecimalReturnsNoTrailingZeros(decimal input, string expectedOutput)
        {
            var output = input.Normalize();
            Assert.AreEqual(expectedOutput, output.ToStringInvariant());
        }

        [Test]
        [TestCase(0.072842, 3, "0.0728")]
        [TestCase(0.0019999, 2, "0.002")]
        [TestCase(0.01234568423, 6, "0.0123457")]
        public void RoundToSignificantDigits(double input, int digits, string expectedOutput)
        {
            var output = input.RoundToSignificantDigits(digits).ToStringInvariant();
            Assert.AreEqual(expectedOutput, output);
        }

        [Test]
        public void RoundsDownInTimeZone()
        {
            var dataTimeZone = TimeZones.Utc;
            var exchangeTimeZone = TimeZones.EasternStandard;
            var time = new DateTime(2000, 01, 01).ConvertTo(dataTimeZone, exchangeTimeZone);
            var roundedTime = time.RoundDownInTimeZone(Time.OneDay, exchangeTimeZone, dataTimeZone);
            Assert.AreEqual(time, roundedTime);
        }

        [Test]
        public void GetStringBetweenCharsTests()
        {
            const string expected = "python3.6";

            // Different characters cases
            var input = "[ python3.6 ]";
            var actual = input.GetStringBetweenChars('[', ']');
            Assert.AreEqual(expected, actual);

            input = "[ python3.6 ] [ python2.7 ]";
            actual = input.GetStringBetweenChars('[', ']');
            Assert.AreEqual(expected, actual);

            input = "[ python2.7 [ python3.6 ] ]";
            actual = input.GetStringBetweenChars('[', ']');
            Assert.AreEqual(expected, actual);

            // Same character cases
            input = "\'python3.6\'";
            actual = input.GetStringBetweenChars('\'', '\'');
            Assert.AreEqual(expected, actual);

            input = "\' python3.6 \' \' python2.7 \'";
            actual = input.GetStringBetweenChars('\'', '\'');
            Assert.AreEqual(expected, actual);

            // In this case, it is not equal
            input = "\' python2.7 \' python3.6 \' \'";
            actual = input.GetStringBetweenChars('\'', '\'');
            Assert.AreNotEqual(expected, actual);
        }

        [Test]
        public void PyObjectTryConvertQuoteBar()
        {
            // Wrap a QuoteBar around a PyObject and convert it back
            var value = ConvertToPyObject(new QuoteBar());

            QuoteBar quoteBar;
            var canConvert = value.TryConvert(out quoteBar);
            Assert.IsTrue(canConvert);
            Assert.IsNotNull(quoteBar);
            Assert.IsAssignableFrom<QuoteBar>(quoteBar);
        }

        [Test]
        public void PyObjectTryConvertSMA()
        {
            // Wrap a SimpleMovingAverage around a PyObject and convert it back
            var value = ConvertToPyObject(new SimpleMovingAverage(14));

            IndicatorBase<IndicatorDataPoint> indicatorBaseDataPoint;
            var canConvert = value.TryConvert(out indicatorBaseDataPoint);
            Assert.IsTrue(canConvert);
            Assert.IsNotNull(indicatorBaseDataPoint);
            Assert.IsAssignableFrom<SimpleMovingAverage>(indicatorBaseDataPoint);
        }

        [Test]
        public void PyObjectTryConvertATR()
        {
            // Wrap a AverageTrueRange around a PyObject and convert it back
            var value = ConvertToPyObject(new AverageTrueRange(14, MovingAverageType.Simple));

            IndicatorBase<IBaseDataBar> indicatorBaseDataBar;
            var canConvert = value.TryConvert(out indicatorBaseDataBar);
            Assert.IsTrue(canConvert);
            Assert.IsNotNull(indicatorBaseDataBar);
            Assert.IsAssignableFrom<AverageTrueRange>(indicatorBaseDataBar);
        }

        [Test]
        public void PyObjectTryConvertAD()
        {
            // Wrap a AccumulationDistribution around a PyObject and convert it back
            var value = ConvertToPyObject(new AccumulationDistribution("AD"));

            IndicatorBase<TradeBar> indicatorBaseTradeBar;
            var canConvert = value.TryConvert(out indicatorBaseTradeBar);
            Assert.IsTrue(canConvert);
            Assert.IsNotNull(indicatorBaseTradeBar);
            Assert.IsAssignableFrom<AccumulationDistribution>(indicatorBaseTradeBar);
        }

        [Test]
        public void PyObjectTryConvertSymbolArray()
        {
            PyObject value;
            using (Py.GIL())
            {
                // Wrap a Symbol Array around a PyObject and convert it back
                value = new PyList(new[] { Symbols.SPY.ToPython(), Symbols.AAPL.ToPython() });
            }

            Symbol[] symbols;
            var canConvert = value.TryConvert(out symbols);
            Assert.IsTrue(canConvert);
            Assert.IsNotNull(symbols);
            Assert.IsAssignableFrom<Symbol[]>(symbols);
        }

        [Test]
        public void PyObjectTryConvertFailCSharp()
        {
            // Try to convert a AccumulationDistribution as a QuoteBar
            var value = ConvertToPyObject(new AccumulationDistribution("AD"));

            QuoteBar quoteBar;
            bool canConvert = value.TryConvert(out quoteBar);
            Assert.IsFalse(canConvert);
            Assert.IsNull(quoteBar);
        }

        [Test]
        public void PyObjectTryConvertFailPython()
        {
            using (Py.GIL())
            {
                // Try to convert a python object as a IndicatorBase<TradeBar>
                var locals = new PyDict();
                PythonEngine.Exec("class A:\n    pass", null, locals.Handle);
                var value = locals.GetItem("A").Invoke();

                IndicatorBase<TradeBar> indicatorBaseTradeBar;
                bool canConvert = value.TryConvert(out indicatorBaseTradeBar);
                Assert.IsFalse(canConvert);
                Assert.IsNull(indicatorBaseTradeBar);
            }
        }

        [Test]
        [TestCase("coarseSelector = lambda coarse: [ x.Symbol for x in coarse if x.Price % 2 == 0 ]")]
        [TestCase("def coarseSelector(coarse): return [ x.Symbol for x in coarse if x.Price % 2 == 0 ]")]
        public void PyObjectTryConvertToFunc(string code)
        {
            Func<IEnumerable<CoarseFundamental>, Symbol[]> coarseSelector;

            using (Py.GIL())
            {
                var locals = new PyDict();
                PythonEngine.Exec(code, null, locals.Handle);
                var pyObject = locals.GetItem("coarseSelector");
                pyObject.TryConvertToDelegate(out coarseSelector);
            }

            var coarse = Enumerable
                .Range(0, 9)
                .Select(x => new CoarseFundamental { Symbol = Symbol.Create(x.ToStringInvariant(), SecurityType.Equity, Market.USA), Value = x });

            var symbols = coarseSelector(coarse);

            Assert.AreEqual(5, symbols.Length);
            foreach (var symbol in symbols)
            {
                var price = symbol.Value.ConvertInvariant<int>();
                Assert.AreEqual(0, price % 2);
            }
        }

        [Test]
        public void PyObjectTryConvertToAction1()
        {
            Action<int> action;

            using (Py.GIL())
            {
                var locals = new PyDict();
                PythonEngine.Exec("def raise_number(a): raise ValueError(a)", null, locals.Handle);
                var pyObject = locals.GetItem("raise_number");
                pyObject.TryConvertToDelegate(out action);
            }

            try
            {
                action(2);
                Assert.Fail();
            }
            catch (PythonException e)
            {
                Assert.AreEqual($"ValueError : {2}", e.Message);
            }
        }

        [Test]
        public void PyObjectTryConvertToAction2()
        {
            Action<int, decimal> action;

            using (Py.GIL())
            {
                var locals = new PyDict();
                PythonEngine.Exec("def raise_number(a, b): raise ValueError(a * b)", null, locals.Handle);
                var pyObject = locals.GetItem("raise_number");
                pyObject.TryConvertToDelegate(out action);
            }

            try
            {
                action(2, 3m);
                Assert.Fail();
            }
            catch (PythonException e)
            {
                Assert.AreEqual("ValueError : 6.0", e.Message);
            }
        }

        [Test]
        public void PyObjectTryConvertToNonDelegateFail()
        {
            int action;

            using (Py.GIL())
            {
                var locals = new PyDict();
                PythonEngine.Exec("def raise_number(a, b): raise ValueError(a * b)", null, locals.Handle);
                var pyObject = locals.GetItem("raise_number");
                Assert.Throws<ArgumentException>(() => pyObject.TryConvertToDelegate(out action));
            }
        }

        [Test]
        public void PyObjectStringConvertToSymbolEnumerable()
        {
            SymbolCache.Clear();
            SymbolCache.Set("SPY", Symbols.SPY);

            IEnumerable<Symbol> symbols;
            using (Py.GIL())
            {
                symbols = new PyString("SPY").ConvertToSymbolEnumerable();
            }

            Assert.AreEqual(Symbols.SPY, symbols.Single());
        }

        [Test]
        public void PyObjectStringListConvertToSymbolEnumerable()
        {
            SymbolCache.Clear();
            SymbolCache.Set("SPY", Symbols.SPY);

            IEnumerable<Symbol> symbols;
            using (Py.GIL())
            {
                symbols = new PyList(new[] { "SPY".ToPython() }).ConvertToSymbolEnumerable();
            }

            Assert.AreEqual(Symbols.SPY, symbols.Single());
        }

        [Test]
        public void PyObjectSymbolConvertToSymbolEnumerable()
        {
            IEnumerable<Symbol> symbols;
            using (Py.GIL())
            {
                symbols = Symbols.SPY.ToPython().ConvertToSymbolEnumerable();
            }

            Assert.AreEqual(Symbols.SPY, symbols.Single());
        }

        [Test]
        public void PyObjectSymbolListConvertToSymbolEnumerable()
        {
            IEnumerable<Symbol> symbols;
            using (Py.GIL())
            {
                symbols = new PyList(new[] {Symbols.SPY.ToPython()}).ConvertToSymbolEnumerable();
            }

            Assert.AreEqual(Symbols.SPY, symbols.Single());
        }

        [Test]
        public void PyObjectNonSymbolObjectConvertToSymbolEnumerable()
        {
            using (Py.GIL())
            {
                Assert.Throws<ArgumentException>(() => new PyInt(1).ConvertToSymbolEnumerable().ToList());
            }
        }

        [Test]
        public void PyObjectDictionaryConvertToDictionary_Success()
        {
            using (Py.GIL())
            {
                var actualDictionary = PythonEngine.ModuleFromString(
                    "PyObjectDictionaryConvertToDictionary_Success",
                    @"
from datetime import datetime as dt
actualDictionary = dict()
actualDictionary.update({'SPY': dt(2019,10,3)})
actualDictionary.update({'QQQ': dt(2019,10,4)})
actualDictionary.update({'IBM': dt(2019,10,5)})
"
                ).GetAttr("actualDictionary").ConvertToDictionary<string, DateTime>();

                Assert.AreEqual(3, actualDictionary.Count);
                var expectedDictionary = new Dictionary<string, DateTime>
                {
                    {"SPY", new DateTime(2019,10,3) },
                    {"QQQ", new DateTime(2019,10,4) },
                    {"IBM", new DateTime(2019,10,5) },
                };

                foreach (var kvp in expectedDictionary)
                {
                    Assert.IsTrue(actualDictionary.ContainsKey(kvp.Key));
                    var actual = actualDictionary[kvp.Key];
                    Assert.AreEqual(kvp.Value, actual);
                }
            }
        }

        [Test]
        public void PyObjectDictionaryConvertToDictionary_FailNotDictionary()
        {
            using (Py.GIL())
            {
                var pyObject = PythonEngine.ModuleFromString(
                    "PyObjectDictionaryConvertToDictionary_FailNotDictionary",
                    "actualDictionary = list()"
                ).GetAttr("actualDictionary");

                Assert.Throws<ArgumentException>(() => pyObject.ConvertToDictionary<string, DateTime>());
            }
        }

        [Test]
        public void PyObjectDictionaryConvertToDictionary_FailWrongItemType()
        {
            using (Py.GIL())
            {
                var pyObject = PythonEngine.ModuleFromString(
                    "PyObjectDictionaryConvertToDictionary_FailWrongItemType",
                    @"
actualDictionary = dict()
actualDictionary.update({'SPY': 3})
actualDictionary.update({'QQQ': 4})
actualDictionary.update({'IBM': 5})
"
                ).GetAttr("actualDictionary");

                Assert.Throws<ArgumentException>(() => pyObject.ConvertToDictionary<string, DateTime>());
            }
        }


        [Test]
        public void BatchByDoesNotDropItems()
        {
            var list = new List<int> {1, 2, 3, 4, 5};
            var by2 = list.BatchBy(2).ToList();
            Assert.AreEqual(3, by2.Count);
            Assert.AreEqual(2, by2[0].Count);
            Assert.AreEqual(2, by2[1].Count);
            Assert.AreEqual(1, by2[2].Count);
            CollectionAssert.AreEqual(list, by2.SelectMany(x => x));
        }

        [Test]
        public void ToOrderTicketCreatesCorrectTicket()
        {
            var orderRequest = new SubmitOrderRequest(OrderType.Limit, SecurityType.Equity, Symbols.USDJPY, 1000, 0, 1.11m, DateTime.Now, "Pepe");
            var order = Order.CreateOrder(orderRequest);
            order.Status = OrderStatus.Submitted;
            order.Id = 11;
            var orderTicket = order.ToOrderTicket(null);
            Assert.AreEqual(order.Id, orderTicket.OrderId);
            Assert.AreEqual(order.Quantity, orderTicket.Quantity);
            Assert.AreEqual(order.Status, orderTicket.Status);
            Assert.AreEqual(order.Type, orderTicket.OrderType);
            Assert.AreEqual(order.Symbol, orderTicket.Symbol);
            Assert.AreEqual(order.Tag, orderTicket.Tag);
            Assert.AreEqual(order.Time, orderTicket.Time);
            Assert.AreEqual(order.SecurityType, orderTicket.SecurityType);
        }

        [Test]
        public void DecimalTruncateTo3DecimalPlaces()
        {
            var value = 10.999999m;
            Assert.AreEqual(10.999m, value.TruncateTo3DecimalPlaces());
        }

        private PyObject ConvertToPyObject(object value)
        {
            using (Py.GIL())
            {
                return value.ToPython();
            }
        }

        private class Super<T>
        {
        }

        private class Derived1 : Super<int>
        {
        }

        private class Derived2 : Derived1
        {
        }
    }
}
