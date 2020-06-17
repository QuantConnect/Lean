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
        [TestCase("08/05/2017", 4, "12/05/2017")]
        [TestCase("10/05/2017", 5, "17/05/2017")]
        [TestCase("24/12/2017", 3, "28/12/2017")]
        public void AddBusinessDays_WithPositiveInput_ShouldReturnNthSuccedingBusinessDay(string time, int n, string actual)
        {
            //Arrange
            var inputTime = Parse.DateTimeExact(time, "dd/MM/yyyy");
            var actualDate = Parse.DateTimeExact(actual, "dd/MM/yyyy");

            //Act
            var calculatedDate = FuturesExpiryUtilityFunctions.AddBusinessDays(inputTime, n);

            //Assert
            Assert.AreEqual(actualDate, calculatedDate);
        }

        [TestCase("11/05/2017", -2, "09/05/2017")]
        [TestCase("15/05/2017", -3, "10/05/2017")]
        [TestCase("26/12/2017", -5, "18/12/2017")]
        public void AddBusinessDays_WithNegativeInput_ShouldReturnNthprecedingBusinessDay(string time, int n, string actual)
        {
            //Arrange
            var inputTime = Parse.DateTimeExact(time, "dd/MM/yyyy");
            var actualDate = Parse.DateTimeExact(actual, "dd/MM/yyyy");

            //Act
            var calculatedDate = FuturesExpiryUtilityFunctions.AddBusinessDays(inputTime, n);

            //Assert
            Assert.AreEqual(actualDate, calculatedDate);
        }

        [TestCase("08/05/2017", 4, "15/05/2017", "12/05/2017")]
        [TestCase("08/05/2017", 5, "22/05/2017", "15/05/2017", "16/05/2017", "17/05/2017", "18/05/2017", "19/05/2017")]
        [TestCase("24/12/2017", 3, "27/12/2017")]
        public void AddBusinessDays_WithPositiveInput_ShouldReturnNthSuccedingBusinessDay_ExcludingCustomHolidays(string time, int n, string actual, params string[] holidays)
        {
            //Arrange
            var inputTime = Parse.DateTimeExact(time, "dd/MM/yyyy");
            var actualDate = Parse.DateTimeExact(actual, "dd/MM/yyyy");

            //Act
            var calculatedDate = FuturesExpiryUtilityFunctions.AddBusinessDays(
                inputTime,
                n,
                useEquityHolidays: false,
                holidayList: holidays.Select(x => Parse.DateTimeExact(x, "dd/MM/yyyy")));

            //Assert
            Assert.AreEqual(actualDate, calculatedDate);
        }

        [TestCase("11/05/2017", -1, "09/05/2017", "10/05/2017")]
        [TestCase("15/05/2017", -1, "10/05/2017", "11/05/2017", "12/05/2017")]
        [TestCase("26/12/2017", -1, "18/12/2017", "25/12/2017", "22/12/2017", "21/12/2017", "20/12/2017", "19/12/2017")]
        public void AddBusinessDays_WithNegativeInput_ShouldReturnNthprecedingBusinessDay_ExcludingCustomHolidays(string time, int n, string actual, params string[] holidays)
        {
            //Arrange
            var inputTime = Parse.DateTimeExact(time, "dd/MM/yyyy");
            var actualDate = Parse.DateTimeExact(actual, "dd/MM/yyyy");

            //Act
            var calculatedDate = FuturesExpiryUtilityFunctions.AddBusinessDays(
                inputTime,
                n,
                useEquityHolidays: false,
                holidayList: holidays.Select(x => Parse.DateTimeExact(x, "dd/MM/yyyy")));

            //Assert
            Assert.AreEqual(actualDate, calculatedDate);
        }

        [TestCase("01/03/2016", 5, "24/03/2016")]
        [TestCase("01/02/2014", 3, "26/02/2014")]
        [TestCase("05/07/2017", 7, "21/07/2017")]
        public void NthLastBusinessDay_WithInputsLessThanDaysInMonth_ShouldReturnNthLastBusinessDay(string time, int numberOfDays, string actual)
        {
            //Arrange
            var inputDate = Parse.DateTimeExact(time, "dd/MM/yyyy");
            var actualDate = Parse.DateTimeExact(actual, "dd/MM/yyyy");

            //Act
            var calculatedDate = FuturesExpiryUtilityFunctions.NthLastBusinessDay(inputDate, numberOfDays);

            //Assert
            Assert.AreEqual(actualDate, calculatedDate);
        }

        [TestCase("01/03/2016", 45)]
        [TestCase("05/02/2017", 30)]
        public void NthLastBusinessDay_WithInputsMoreThanDaysInMonth_ShouldThrowException(string time, int numberOfDays)
        {
            //Arrange
            var inputDate = Parse.DateTimeExact(time, "dd/MM/yyyy");

            //Act
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                FuturesExpiryUtilityFunctions.NthLastBusinessDay(inputDate, numberOfDays);
            });
        }

        [TestCase("01/01/2016", 1, "01/04/2016")]
        [TestCase("02/01/2019", 1, "02/01/2019")]
        [TestCase("01/01/2019", 1, "01/02/2019")]
        [TestCase("06/01/2019", 1, "06/03/2019")]
        [TestCase("07/01/2019", 5, "07/08/2019")]
        public void NthBusinessDay_ShouldReturnAccurateBusinessDay(string testDate, int nthBusinessDay, string actualDate)
        {
            var inputDate = Parse.DateTimeExact(testDate, "MM/dd/yyyy");
            var expectedResult = Parse.DateTimeExact(actualDate, "MM/dd/yyyy");

            var actual = FuturesExpiryUtilityFunctions.NthBusinessDay(inputDate, nthBusinessDay);

            Assert.AreEqual(expectedResult, actual);
        }

        [TestCase("01/01/2016", 1, "01/05/2016", "01/04/2016")]
        [TestCase("02/01/2019", 12, "02/20/2019", "02/19/2019")]
        [TestCase("01/01/2019", 1, "01/07/2019", "01/02/2019", "01/03/2019", "01/04/2019")]
        public void NthBusinessDay_ShouldReturnAccurateBusinessDay_WithHolidays(string testDate, int nthBusinessDay, string actualDate, params string[] holidayDates)
        {
            var inputDate = Parse.DateTimeExact(testDate, "MM/dd/yyyy");
            var expectedResult = Parse.DateTimeExact(actualDate, "MM/dd/yyyy");
            var holidays = holidayDates.Select(x => Parse.DateTimeExact(x, "MM/dd/yyyy"));

            var actual = FuturesExpiryUtilityFunctions.NthBusinessDay(inputDate, nthBusinessDay, holidays);

            Assert.AreEqual(expectedResult, actual);
        }

        [TestCase("01/2015","16/01/2015")]
        [TestCase("06/2016", "17/06/2016")]
        [TestCase("12/2018", "21/12/2018")]
        public void ThirdFriday_WithNormalMonth_ShouldReturnThridFriday(string time, string actual)
        {
            //Arrange
            var inputMonth = Parse.DateTimeExact(time, "MM/yyyy");
            var actualFriday = Parse.DateTimeExact(actual, "dd/MM/yyyy");

            //Act
            var calculatedFriday = FuturesExpiryUtilityFunctions.ThirdFriday(inputMonth);

            //Assert
            Assert.AreEqual(calculatedFriday, actualFriday);
        }

        [TestCase("04/2017", "19/04/2017")]
        [TestCase("02/2015", "18/02/2015")]
        [TestCase("01/2003", "15/01/2003")]
        public void ThirdWednesday_WithNormalMonth_ShouldReturnThridWednesday(string time, string actual)
        {
            //Arrange
            var inputMonth = Parse.DateTimeExact(time, "MM/yyyy");
            var actualFriday = Parse.DateTimeExact(actual, "dd/MM/yyyy");

            //Act
            var calculatedFriday = FuturesExpiryUtilityFunctions.ThirdWednesday(inputMonth);

            //Assert
            Assert.AreEqual(calculatedFriday, actualFriday);
        }

        [TestCase("07/05/2017")]
        [TestCase("01/01/1998")]
        [TestCase("25/03/2005")]
        public void NotHoliday_ForAHoliday_ShouldReturnFalse(string time)
        {
            //Arrange
            var inputDate = Parse.DateTimeExact(time, "dd/MM/yyyy");

            //Act
            var calculatedValue = FuturesExpiryUtilityFunctions.NotHoliday(inputDate);

            //Assert
            Assert.AreEqual(calculatedValue, false);
        }

        [TestCase("08/05/2017")]
        [TestCase("05/04/2007")]
        [TestCase("27/05/2003")]
        public void NotHoliday_ForABusinessDay_ShouldReturnTrue(string time)
        {
            //Arrange
            var inputDate = Parse.DateTimeExact(time, "dd/MM/yyyy");

            //Act
            var calculatedValue = FuturesExpiryUtilityFunctions.NotHoliday(inputDate);

            //Assert
            Assert.AreEqual(calculatedValue, true);
        }

        [TestCase("09/04/2017")]
        [TestCase("02/04/2003")]
        [TestCase("02/03/2002")]
        public void NotPrecededByHoliday_WithNonThrusdayWeekday_ShouldThrowException(string day)
        {
            //Arrange
            var inputDate = Parse.DateTimeExact(day, "dd/MM/yyyy");

            //Act
            Assert.Throws<ArgumentException>(() =>
            {
                FuturesExpiryUtilityFunctions.NotPrecededByHoliday(inputDate);
            });
        }

        [TestCase("13/04/2017")]
        [TestCase("14/02/2002")]
        public void NotPrecededByHoliday_ForThursdayWithNoHolidayInFourPrecedingDays_ShouldReturnTrue(string day)
        {
            //Arrange
            var inputDate = Parse.DateTimeExact(day, "dd/MM/yyyy");

            //Act
            var calculatedOutput = FuturesExpiryUtilityFunctions.NotPrecededByHoliday(inputDate);

            //Assert
            Assert.AreEqual(calculatedOutput, true);
        }

        [TestCase("31/03/2016")]
        [TestCase("30/05/2002")]
        public void NotPrecededByHoliday_ForThursdayWithHolidayInFourPrecedingDays_ShouldReturnFalse(string day)
        {
            //Arrange
            var inputDate = Parse.DateTimeExact(day, "dd/MM/yyyy");

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

        [TestCase("17/06/2020", DayOfWeek.Friday, 1, "05/06/2020")]
        [TestCase("30/08/2017", DayOfWeek.Monday, 2, "14/08/2017")]
        public void Nth_WeekDay_ShouldReturnCorrectDate(string contractDate, DayOfWeek dayOfWeek, int n, string expectedOutput)
        {
            // Arrange
            var inputDate = Parse.DateTimeExact(contractDate, "dd/MM/yyyy");
            var calculated = FuturesExpiryUtilityFunctions.NthWeekday(inputDate, n, dayOfWeek);
            var expected = Parse.DateTimeExact(expectedOutput, "dd/MM/yyyy");

            Assert.AreEqual(expected, calculated);
        }

        [TestCase("17/06/2020", DayOfWeek.Friday, -2)]
        [TestCase("30/08/2017", DayOfWeek.Monday, 7)]
        public void Nth_WeekDay_ShouldHandShouldThrowException(string contractDate, DayOfWeek dayOfWeek, int n)
        {
            // Arrange
            var inputDate = Parse.DateTimeExact(contractDate, "dd/MM/yyyy");

            //Act
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                FuturesExpiryUtilityFunctions.NthWeekday(inputDate, n, dayOfWeek);
            });
        }

        [TestCase("06/01/2015", DayOfWeek.Friday, "30/01/2015")]
        [TestCase("06/05/2016", DayOfWeek.Wednesday, "25/05/2016")]
        public void Last_WeekDay_ShouldReturnCorrectDate(string contractDate, DayOfWeek dayOfWeek, string expectedOutput)
        {
            // Arrange
            var inputDate = Parse.DateTimeExact(contractDate, "dd/MM/yyyy");
            var calculated = FuturesExpiryUtilityFunctions.LastWeekday(inputDate, dayOfWeek);
            var expected = Parse.DateTimeExact(expectedOutput, "dd/MM/yyyy");

            Assert.AreEqual(expected, calculated);
        }
    }
}
