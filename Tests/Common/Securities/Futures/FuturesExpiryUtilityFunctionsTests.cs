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
using NUnit.Framework;
using QuantConnect.Securities.Future;

namespace QuantConnect.Tests.Common.Securities.Futures
{
    [TestFixture, Parallelizable(ParallelScope.All)]
    public class FuturesExpiryUtilityFunctionsTests
    {
        [TestCase("08/05/2017 00:00:01", 4, "12/05/2017 00:00:01")]
        [TestCase("10/05/2017 00:00:01", 5, "17/05/2017 00:00:01")]
        [TestCase("24/12/2017 00:00:01", 3, "28/12/2017 00:00:01")]
        public void AddBusinessDays_WithPositiveInput_ShouldReturnNthSucceedingBusinessDay(string time, int n, string actual)
        {
            //Arrange
            var inputTime = Parse.DateTimeExact(time, "dd/MM/yyyy HH:mm:ss");
            var actualDate = Parse.DateTimeExact(actual, "dd/MM/yyyy HH:mm:ss");

            //Act
            var calculatedDate = FuturesExpiryUtilityFunctions.AddBusinessDays(inputTime, n);

            //Assert
            Assert.AreEqual(actualDate, calculatedDate);
        }

        [TestCase("11/05/2017 00:00:01", -2, "09/05/2017 00:00:01")]
        [TestCase("15/05/2017 00:00:01", -3, "10/05/2017 00:00:01")]
        [TestCase("26/12/2017 00:00:01", -5, "18/12/2017 00:00:01")]
        public void AddBusinessDays_WithNegativeInput_ShouldReturnNthPrecedingBusinessDay(string time, int n, string actual)
        {
            //Arrange
            var inputTime = Parse.DateTimeExact(time, "dd/MM/yyyy HH:mm:ss");
            var actualDate = Parse.DateTimeExact(actual, "dd/MM/yyyy HH:mm:ss");

            //Act
            var calculatedDate = FuturesExpiryUtilityFunctions.AddBusinessDays(inputTime, n);

            //Assert
            Assert.AreEqual(actualDate, calculatedDate);
        }

        [TestCase("08/05/2017 00:00:01", 4, "15/05/2017 00:00:01", "12/05/2017 00:00:01")]
        [TestCase("08/05/2017 00:00:01", 5, "22/05/2017 00:00:01", "15/05/2017 00:00:01", "16/05/2017 00:00:01", "17/05/2017 00:00:01", "18/05/2017 00:00:01", "19/05/2017 00:00:01")]
        [TestCase("24/12/2017 00:00:01", 3, "27/12/2017 00:00:01")]
        public void AddBusinessDays_WithPositiveInput_ShouldReturnNthSucceedingBusinessDay_ExcludingCustomHolidays(string time, int n, string actual, params string[] holidays)
        {
            //Arrange
            var inputTime = Parse.DateTimeExact(time, "dd/MM/yyyy HH:mm:ss");
            var actualDate = Parse.DateTimeExact(actual, "dd/MM/yyyy HH:mm:ss");

            //Act
            var calculatedDate = FuturesExpiryUtilityFunctions.AddBusinessDays(
                inputTime,
                n,
                useEquityHolidays: false,
                holidayList: holidays.Select(x => Parse.DateTimeExact(x, "dd/MM/yyyy HH:mm:ss")));

            //Assert
            Assert.AreEqual(actualDate, calculatedDate);
        }

        [TestCase("11/05/2017 00:00:01", -1, "09/05/2017 00:00:01", "10/05/2017 00:00:01")]
        [TestCase("15/05/2017 00:00:01", -1, "10/05/2017 00:00:01", "11/05/2017 00:00:01", "12/05/2017 00:00:01")]
        [TestCase("26/12/2017 00:00:01", -1, "18/12/2017 00:00:01", "25/12/2017 00:00:01", "22/12/2017 00:00:01", "21/12/2017 00:00:01", "20/12/2017 00:00:01", "19/12/2017 00:00:01")]
        public void AddBusinessDays_WithNegativeInput_ShouldReturnNthPrecedingBusinessDay_ExcludingCustomHolidays(string time, int n, string actual, params string[] holidays)
        {
            //Arrange
            var inputTime = Parse.DateTimeExact(time, "dd/MM/yyyy HH:mm:ss");
            var actualDate = Parse.DateTimeExact(actual, "dd/MM/yyyy HH:mm:ss");

            //Act
            var calculatedDate = FuturesExpiryUtilityFunctions.AddBusinessDays(
                inputTime,
                n,
                useEquityHolidays: false,
                holidayList: holidays.Select(x => Parse.DateTimeExact(x, "dd/MM/yyyy HH:mm:ss")));

            //Assert
            Assert.AreEqual(actualDate, calculatedDate);
        }

        [TestCase("01/03/2016 00:00:01", 5, "24/03/2016 00:00:00")]
        [TestCase("01/02/2014 00:00:01", 3, "26/02/2014 00:00:00")]
        [TestCase("05/07/2017 00:00:01", 7, "21/07/2017 00:00:00")]
        public void NthLastBusinessDay_WithInputsLessThanDaysInMonth_ShouldReturnNthLastBusinessDay(string time, int numberOfDays, string actual)
        {
            //Arrange
            var inputDate = Parse.DateTimeExact(time, "dd/MM/yyyy HH:mm:ss");
            var actualDate = Parse.DateTimeExact(actual, "dd/MM/yyyy HH:mm:ss");

            //Act
            var calculatedDate = FuturesExpiryUtilityFunctions.NthLastBusinessDay(inputDate, numberOfDays);

            //Assert
            Assert.AreEqual(actualDate, calculatedDate);
        }

        [TestCase("01/03/2016 00:00:00", 45)]
        [TestCase("05/02/2017 00:00:00", 30)]
        public void NthLastBusinessDay_WithInputsMoreThanDaysInMonth_ShouldThrowException(string time, int numberOfDays)
        {
            //Arrange
            var inputDate = Parse.DateTimeExact(time, "dd/MM/yyyy HH:mm:ss");

            //Act
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                FuturesExpiryUtilityFunctions.NthLastBusinessDay(inputDate, numberOfDays);
            });
        }

        [TestCase("01/01/2016 00:00:01", 1, "01/04/2016 00:00:00")]
        [TestCase("02/01/2019 00:00:01", 1, "02/01/2019 00:00:00")]
        [TestCase("01/01/2019 00:00:01", 1, "01/02/2019 00:00:00")]
        [TestCase("06/01/2019 00:00:01", 1, "06/03/2019 00:00:00")]
        [TestCase("07/01/2019 00:00:01", 5, "07/08/2019 00:00:00")]
        public void NthBusinessDay_ShouldReturnAccurateBusinessDay(string testDate, int nthBusinessDay, string actualDate)
        {
            var inputDate = Parse.DateTimeExact(testDate, "MM/dd/yyyy HH:mm:ss");
            var expectedResult = Parse.DateTimeExact(actualDate, "MM/dd/yyyy HH:mm:ss");

            var actual = FuturesExpiryUtilityFunctions.NthBusinessDay(inputDate, nthBusinessDay);

            Assert.AreEqual(expectedResult, actual);
        }

        [TestCase("01/01/2016 00:00:01", 1, "01/05/2016 00:00:00", "01/04/2016 00:00:01")]
        [TestCase("02/01/2019 00:00:01", 12, "02/20/2019 00:00:00", "02/19/2019 00:00:01")]
        [TestCase("01/01/2019 00:00:01", 1, "01/07/2019 00:00:00", "01/02/2019 00:00:01", "01/03/2019 00:00:01", "01/04/2019 00:00:01")]
        public void NthBusinessDay_ShouldReturnAccurateBusinessDay_WithHolidays(string testDate, int nthBusinessDay, string actualDate, params string[] holidayDates)
        {
            var inputDate = Parse.DateTimeExact(testDate, "MM/dd/yyyy HH:mm:ss");
            var expectedResult = Parse.DateTimeExact(actualDate, "MM/dd/yyyy HH:mm:ss");
            var holidays = holidayDates.Select(x => Parse.DateTimeExact(x, "MM/dd/yyyy HH:mm:ss"));

            var actual = FuturesExpiryUtilityFunctions.NthBusinessDay(inputDate, nthBusinessDay, holidays);

            Assert.AreEqual(expectedResult, actual);
        }

        [TestCase("01/2015", "16/01/2015 00:00:00")]
        [TestCase("06/2016", "17/06/2016 00:00:00")]
        [TestCase("12/2018", "21/12/2018 00:00:00")]
        public void ThirdFriday_WithNormalMonth_ShouldReturnThirdFriday(string time, string actual)
        {
            //Arrange
            var inputMonth = Parse.DateTimeExact(time, "MM/yyyy");
            var actualFriday = Parse.DateTimeExact(actual, "dd/MM/yyyy HH:mm:ss");

            //Act
            var calculatedFriday = FuturesExpiryUtilityFunctions.ThirdFriday(inputMonth);

            //Assert
            Assert.AreEqual(calculatedFriday, actualFriday);
        }

        [TestCase("04/2017", "19/04/2017 00:00:00")]
        [TestCase("02/2015", "18/02/2015 00:00:00")]
        [TestCase("01/2003", "15/01/2003 00:00:00")]
        public void ThirdWednesday_WithNormalMonth_ShouldReturnThirdWednesday(string time, string actual)
        {
            //Arrange
            var inputMonth = Parse.DateTimeExact(time, "MM/yyyy");
            var actualFriday = Parse.DateTimeExact(actual, "dd/MM/yyyy HH:mm:ss");

            //Act
            var calculatedFriday = FuturesExpiryUtilityFunctions.ThirdWednesday(inputMonth);

            //Assert
            Assert.AreEqual(calculatedFriday, actualFriday);
        }

        [TestCase("07/05/2017 00:00:01")]
        [TestCase("01/01/1998 00:00:01")]
        [TestCase("25/03/2005 00:00:01")]
        public void NotHoliday_ForAHoliday_ShouldReturnFalse(string time)
        {
            //Arrange
            var inputDate = Parse.DateTimeExact(time, "dd/MM/yyyy HH:mm:ss");

            //Act
            var calculatedValue = FuturesExpiryUtilityFunctions.NotHoliday(inputDate);

            //Assert
            Assert.AreEqual(calculatedValue, false);
        }

        [TestCase("08/05/2017 00:00:01")]
        [TestCase("05/04/2007 00:00:01")]
        [TestCase("27/05/2003 00:00:01")]
        public void NotHoliday_ForABusinessDay_ShouldReturnTrue(string time)
        {
            //Arrange
            var inputDate = Parse.DateTimeExact(time, "dd/MM/yyyy HH:mm:ss");

            //Act
            var calculatedValue = FuturesExpiryUtilityFunctions.NotHoliday(inputDate);

            //Assert
            Assert.AreEqual(calculatedValue, true);
        }

        [TestCase("09/04/2017 00:00:01")]
        [TestCase("02/04/2003 00:00:01")]
        [TestCase("02/03/2002 00:00:01")]
        public void NotPrecededByHoliday_WithNonThursdayWeekday_ShouldThrowException(string day)
        {
            //Arrange
            var inputDate = Parse.DateTimeExact(day, "dd/MM/yyyy HH:mm:ss");

            //Act
            Assert.Throws<ArgumentException>(() =>
            {
                FuturesExpiryUtilityFunctions.NotPrecededByHoliday(inputDate);
            });
        }

        [TestCase("13/04/2017 00:00:01")]
        [TestCase("14/02/2002 00:00:01")]
        public void NotPrecededByHoliday_ForThursdayWithNoHolidayInFourPrecedingDays_ShouldReturnTrue(string day)
        {
            //Arrange
            var inputDate = Parse.DateTimeExact(day, "dd/MM/yyyy HH:mm:ss");

            //Act
            var calculatedOutput = FuturesExpiryUtilityFunctions.NotPrecededByHoliday(inputDate);

            //Assert
            Assert.AreEqual(calculatedOutput, true);
        }

        [TestCase("31/03/2016 00:00:01")]
        [TestCase("30/05/2002 00:00:01")]
        public void NotPrecededByHoliday_ForThursdayWithHolidayInFourPrecedingDays_ShouldReturnFalse(string day)
        {
            //Arrange
            var inputDate = Parse.DateTimeExact(day, "dd/MM/yyyy HH:mm:ss");

            //Act
            var calculatedOutput = FuturesExpiryUtilityFunctions.NotPrecededByHoliday(inputDate);

            //Assert
            Assert.AreEqual(calculatedOutput, false);
        }

        [TestCase("01/05/2019", "01/30/2019", "17:10:00")]
        [TestCase("01/31/2019", "01/30/2019", "12:00:00")]
        [TestCase("03/01/2012", "04/02/2012", "17:10:00")]
        public void DairyReportDates_ShouldNormalizeDateTimeAndReturnCorrectReportDate(string contractMonth, string reportDate, string lastTradeTime)
        {
            var actual = FuturesExpiryUtilityFunctions.DairyLastTradeDate(
                Parse.DateTimeExact(contractMonth, "MM/dd/yyyy"),
                Parse.TimeSpan(lastTradeTime));

            var expected = Parse.DateTimeExact(reportDate, "MM/dd/yyyy")
                .AddDays(-1).Add(Parse.TimeSpan(lastTradeTime));

            Assert.AreEqual(expected, actual);
        }

        [TestCase("17/06/2020 00:00:01", DayOfWeek.Friday, 1, "05/06/2020 00:00:00")]
        [TestCase("30/08/2017 00:00:01", DayOfWeek.Monday, 2, "14/08/2017 00:00:00")]
        public void Nth_WeekDay_ShouldReturnCorrectDate(string contractDate, DayOfWeek dayOfWeek, int n, string expectedOutput)
        {
            // Arrange
            var inputDate = Parse.DateTimeExact(contractDate, "dd/MM/yyyy HH:mm:ss");
            var calculated = FuturesExpiryUtilityFunctions.NthWeekday(inputDate, n, dayOfWeek);
            var expected = Parse.DateTimeExact(expectedOutput, "dd/MM/yyyy HH:mm:ss");

            Assert.AreEqual(expected, calculated);
        }

        [TestCase("17/06/2020 00:00:01", DayOfWeek.Friday, -2)]
        [TestCase("30/08/2017 00:00:01", DayOfWeek.Monday, 7)]
        public void Nth_WeekDay_ShouldHandShouldThrowException(string contractDate, DayOfWeek dayOfWeek, int n)
        {
            // Arrange
            var inputDate = Parse.DateTimeExact(contractDate, "dd/MM/yyyy HH:mm:ss");

            //Act
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                FuturesExpiryUtilityFunctions.NthWeekday(inputDate, n, dayOfWeek);
            });
        }

        [TestCase("06/01/2015 00:00:01", DayOfWeek.Friday, "30/01/2015 00:00:00")]
        [TestCase("06/05/2016 00:00:01", DayOfWeek.Wednesday, "25/05/2016 00:00:00")]
        public void Last_WeekDay_ShouldReturnCorrectDate(string contractDate, DayOfWeek dayOfWeek, string expectedOutput)
        {
            // Arrange
            var inputDate = Parse.DateTimeExact(contractDate, "dd/MM/yyyy HH:mm:ss");
            var calculated = FuturesExpiryUtilityFunctions.LastWeekday(inputDate, dayOfWeek);
            var expected = Parse.DateTimeExact(expectedOutput, "dd/MM/yyyy HH:mm:ss");

            Assert.AreEqual(expected, calculated);
        }
    }
}
