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
using System.Globalization;
using Newtonsoft.Json;
using NUnit.Framework;
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
        public void ConvertsInt32FromString()
        {
            const string input = "12345678";
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
            Assert.AreEqual(expectedOutput, output.ToString(CultureInfo.InvariantCulture));
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
