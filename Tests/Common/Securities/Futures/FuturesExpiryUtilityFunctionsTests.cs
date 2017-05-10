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
using NUnit.Framework;
using QuantConnect.Securities.Future;

namespace QuantConnect.Tests.Common.Securities.Futures
{
    [TestFixture]
    public class FuturesExpiryUtilityFunctionsTests
    {
        [TestCase("08/05/2017",4,"12/05/2017")]
        [TestCase("10/05/2017", 5,"17/05/2017")]
        [TestCase("24/12/2017",3,"28/12/2017")]
        public void AddBusinessDays_WithPositiveInput_ShouldReturnNthSuccedingBusinessDay(string time, int n, string actual)
        {
            //Arrange
            var inputTime = DateTime.ParseExact(time,"dd/MM/yyyy", null);
            var actualDate = DateTime.ParseExact(actual, "dd/MM/yyyy", null);

            //Act
            var calculatedDate = FuturesExpiryUtilityFunctions.AddBusinessDays(inputTime, n);

            //Assert
            Assert.AreEqual(calculatedDate, actualDate);
        }

        [TestCase("11/05/2017", -2, "09/05/2017")]
        [TestCase("15/05/2017", -3, "10/05/2017")]
        [TestCase("26/12/2017", -5, "18/12/2017")]
        public void AddBusinessDays_WithNegativeInput_ShouldReturnNthprecedingBusinessDay(string time, int n, string actual)
        {
            //Arrange
            var inputTime = DateTime.ParseExact(time, "dd/MM/yyyy", null);
            var actualDate = DateTime.ParseExact(actual, "dd/MM/yyyy", null);

            //Act
            var calculatedDate = FuturesExpiryUtilityFunctions.AddBusinessDays(inputTime, n);

            //Assert
            Assert.AreEqual(calculatedDate, actualDate);
        }

        [TestCase("01/03/2016", 5, "24/03/2016")]
        [TestCase("01/02/2014", 3, "26/02/2014")]
        [TestCase("05/07/2017", 7, "21/07/2017")]
        public void NthLastBusinessDay_WithInputsLessThanDaysInMonth_ShouldReturnNthLastBusinessDay(string time, int numberOfDays, string actual)
        {
            //Arrange
            var inputDate = DateTime.ParseExact(time, "dd/MM/yyyy", null);
            var actualDate = DateTime.ParseExact(actual, "dd/MM/yyyy", null);

            //Act
            var calculatedDate = FuturesExpiryUtilityFunctions.NthLastBusinessDay(inputDate,numberOfDays);

            //Assert
            Assert.AreEqual(calculatedDate, actualDate);
        }

        [TestCase("01/03/2016", 45)]
        [TestCase("05/02/2017", 30)]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void NthLastBusinessDay_WithInputsMoreThanDaysInMonth_ShouldThrowException(string time, int numberOfDays)
        {
            //Arrange
            var inputDate = DateTime.ParseExact(time, "dd/MM/yyyy", null);
            
            //Act
            FuturesExpiryUtilityFunctions.NthLastBusinessDay(inputDate, numberOfDays);
            
        }

        [TestCase("01/2015","16/01/2015")]
        [TestCase("06/2016", "17/06/2016")]
        [TestCase("12/2018", "21/12/2018")]
        public void ThirdFriday_WithNormalMonth_ShouldReturnThridFriday(string time, string actual)
        {
            //Arrange
            var inputMonth = DateTime.ParseExact(time,"MM/yyyy", null);
            var actualFriday = DateTime.ParseExact(actual, "dd/MM/yyyy", null);

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
            var inputMonth = DateTime.ParseExact(time, "MM/yyyy", null);
            var actualFriday = DateTime.ParseExact(actual, "dd/MM/yyyy", null);

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
            var inputDate = DateTime.ParseExact(time, "dd/MM/yyyy", null);

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
            var inputDate = DateTime.ParseExact(time, "dd/MM/yyyy", null);

            //Act
            var calculatedValue = FuturesExpiryUtilityFunctions.NotHoliday(inputDate);

            //Assert
            Assert.AreEqual(calculatedValue, true);
        }

        [TestCase("09/04/2017")]
        [TestCase("02/04/2003")]
        [TestCase("02/03/2002")]
        [ExpectedException(typeof(ArgumentException))]
        public void NotPrecededByHoliday_WithNonThrusdayWeekday_ShouldThrowException(string day)
        {
            //Arrange
            var inputDate = DateTime.ParseExact(day, "dd/MM/yyyy", null);

            //Act
            FuturesExpiryUtilityFunctions.NotPrecededByHoliday(inputDate);

        }

        [TestCase("13/04/2017")]
        [TestCase("14/02/2002")]
        public void NotPrecededByHoliday_ForThursdayWithNoHolidayInFourPrecedingDays_ShouldReturnTrue(string day)
        {
            //Arrange
            var inputDate = DateTime.ParseExact(day, "dd/MM/yyyy", null);

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
            var inputDate = DateTime.ParseExact(day, "dd/MM/yyyy", null);

            //Act
            var calculatedOutput = FuturesExpiryUtilityFunctions.NotPrecededByHoliday(inputDate);

            //Assert
            Assert.AreEqual(calculatedOutput, false);
        }
    }
}
