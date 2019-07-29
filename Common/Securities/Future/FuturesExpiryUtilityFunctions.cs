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
using static QuantConnect.StringExtensions;

namespace QuantConnect.Securities.Future
{
    /// <summary>
    /// Class to implement common functions used in FuturesExpiryFunctions
    /// </summary>
    public static class FuturesExpiryUtilityFunctions
    {
        /// <summary>
        /// Method to retrieve n^th succeeding/preceding business day for a given day
        /// </summary>
        /// <param name="time">The current Time</param>
        /// <param name="n">Number of business days succeeding current time. Use negative value for preceding business days</param>
        /// <returns>The date-time after adding n business days</returns>
        public static DateTime AddBusinessDays(DateTime time, int n)
        {
            if (n < 0)
            {
                var businessDays = (-1) * n;
                var totalDays = 1;
                do
                {
                    var previousDay = time.AddDays(-totalDays);
                    if (NotHoliday(previousDay))
                    {
                        businessDays--;
                    }
                    if (businessDays > 0) totalDays++;
                } while (businessDays > 0);

                return time.AddDays(-totalDays);
            }
            else
            {
                var businessDays = n;
                var totalDays = 1;
                do
                {
                    var previousDay = time.AddDays(totalDays);
                    if (NotHoliday(previousDay))
                    {
                        businessDays--;
                    }
                    if (businessDays > 0) totalDays++;
                } while (businessDays > 0);

                return time.AddDays(totalDays);
            }
        }

        /// <summary>
        /// Method to retrieve the n^th last business day of the delivery month.
        /// </summary>
        /// <param name="time">DateTime for delivery month</param>
        /// <param name="n">Number of days</param>
        /// <param name="holidayList">Additional holidays to use while calculating n^th business day. Useful for MHDB entries</param>
        /// <returns>Nth Last Business day of the month</returns>
        public static DateTime NthLastBusinessDay(DateTime time, int n, IEnumerable<DateTime> holidayList = null)
        {
            var daysInMonth = DateTime.DaysInMonth(time.Year, time.Month);
            var lastDayOfMonth = new DateTime(time.Year, time.Month, daysInMonth);
            var holidays = (holidayList ?? new List<DateTime>()).ToList();

            if(n > daysInMonth)
            {
                throw new ArgumentOutOfRangeException(nameof(n), Invariant(
                    $"Number of days ({n}) is larger than the size of month({daysInMonth})"
                ));
            }
            // Count the number of days in the month after the third to last business day
            var businessDays = n;
            var totalDays = 0;
            do
            {
                var previousDay = lastDayOfMonth.AddDays(-totalDays);
                if (NotHoliday(previousDay) && !holidays.Contains(previousDay))
                {
                    businessDays--;
                }
                if (businessDays > 0) totalDays++;
            } while (businessDays > 0);

            return lastDayOfMonth.AddDays(-totalDays);
        }

        /// <summary>
        /// Calculates the n^th business day of the month (includes checking for holidays)
        /// </summary>
        /// <param name="time">Month to calculate business day for</param>
        /// <param name="nthBusinessDay">n^th business day to get</param>
        /// <param name="additionalHolidays">Additional user provided holidays to not count as business days</param>
        /// <returns>Nth business day of the month</returns>
        public static DateTime NthBusinessDay(DateTime time, int nthBusinessDay, IEnumerable<DateTime> additionalHolidays = null)
        {
            var daysInMonth = DateTime.DaysInMonth(time.Year, time.Month);
            if (nthBusinessDay > daysInMonth)
            {
                throw new ArgumentOutOfRangeException(Invariant(
                    $"Argument nthBusinessDay (${nthBusinessDay}) is larger than the amount of days in the current month (${daysInMonth})"
                ));
            }
            if (nthBusinessDay < 1)
            {
                throw new ArgumentOutOfRangeException(Invariant(
                    $"Argument nthBusinessDay (${nthBusinessDay}) is less than one. Provide a number greater than one and less than the days in month"
                ));
            }

            time = new DateTime(time.Year, time.Month, 1);

            var daysCounted = time.IsCommonBusinessDay() ? 1 : 0;
            var i = 0;
            var holidays = additionalHolidays ?? new List<DateTime>();

            // Check for holiday up here in case we want the first business day and it is a holiday so that we don't skip over it.
            // We also want to make sure that we don't stop on a weekend.
            while (daysCounted < nthBusinessDay || holidays.Contains(time) || USHoliday.Dates.Contains(time) || !time.IsCommonBusinessDay())
            {
                // The asset continues trading on days contained within `USHoliday.Dates`, but
                // the last trade date is affected by those holidays. We check for
                // both MHDB entries and holidays to get accurate business days
                if (holidays.Contains(time) || USHoliday.Dates.Contains(time))
                {
                    // Catches edge case where first day is on a friday
                    if (i == 0 && time.DayOfWeek == DayOfWeek.Friday)
                    {
                        daysCounted = 0;
                    }

                    time = time.AddDays(1);

                    if (i != 0 && time.IsCommonBusinessDay())
                    {
                        daysCounted++;
                    }
                    i++;
                    continue;
                }

                time = time.AddDays(1);

                if (!holidays.Contains(time) && NotHoliday(time))
                {
                    daysCounted++;
                }
                i++;
            }

            return time;
        }

        /// <summary>
        /// Method to retrieve the 2nd Friday of the given month
        /// </summary>
        /// <param name="time">Date from the given month</param>
        /// <returns>2nd Friday of given month</returns>
        public static DateTime SecondFriday(DateTime time) => NthFriday(time, 2);

        /// <summary>
        /// Method to retrieve the 3rd Friday of the given month
        /// </summary>
        /// <param name="time">Date from the given month</param>
        /// <returns>3rd Friday of given month</returns>
        public static DateTime ThirdFriday(DateTime time) => NthFriday(time, 3);

        /// <summary>
        /// Method to retrieve the Nth Friday of the given month
        /// </summary>
        /// <param name="time">Date from the given month</param>
        /// <param name="n">The order of the Friday in the period</param>
        /// <returns>Nth Friday of given month</returns>
        public static DateTime NthFriday(DateTime time, int n)
        {
            if (n < 1 || n > 5)
            {
                throw new ArgumentOutOfRangeException($"'n' lower than 1 or greater than 5");
            }

            var daysInMonth = DateTime.DaysInMonth(time.Year, time.Month);
            return (from day in Enumerable.Range(1, daysInMonth)
                    where new DateTime(time.Year, time.Month, day).DayOfWeek == DayOfWeek.Friday
                    select new DateTime(time.Year, time.Month, day)).ElementAt(n - 1);
        }

        /// <summary>
        /// Method to retrieve third Wednesday of the given month (usually Monday).
        /// </summary>
        /// <param name="time">Date from the given month</param>
        /// <returns>Third Wednesday of the given month</returns>
        public static DateTime ThirdWednesday(DateTime time)
        {
            var daysInMonth = DateTime.DaysInMonth(time.Year, time.Month);
            return (from day in Enumerable.Range(1, daysInMonth)
                                  where new DateTime(time.Year, time.Month, day).DayOfWeek == DayOfWeek.Wednesday
                                  select new DateTime(time.Year, time.Month, day)).ElementAt(2);
        }

        /// <summary>
        /// Method to check whether a given time is holiday or not
        /// </summary>
        /// <param name="time">The DateTime for consideration</param>
        /// <returns>True if the time is not a holidays, otherwise returns false</returns>
        public static bool NotHoliday(DateTime time)
        {
            return time.IsCommonBusinessDay() && !USHoliday.Dates.Contains(time);
        }

        /// <summary>
        /// This function takes Thursday as input and returns true if four weekdays preceding it are not Holidays
        /// </summary>
        /// <param name="thursday">DateTime of a given Thursday</param>
        /// <returns>False if DayOfWeek is not Thursday or is not preceded by four weekdays,Otherwise returns True</returns>
        public static bool NotPrecededByHoliday(DateTime thursday)
        {
            if (thursday.DayOfWeek != DayOfWeek.Thursday)
            {
                throw new ArgumentException("Input to NotPrecededByHolidays must be a Thursday");
            }
            var result = true;
            // for Monday, Tuesday and Wednesday
            for (var i = 1; i <= 3; i++)
            {
                if (!NotHoliday(thursday.AddDays(-i)))
                {
                    result = false;
                }
            }
            // for Friday
            if (!NotHoliday(thursday.AddDays(-6)))
            {
                result = false;
            }
            return result;
        }

        /// <summary>
        /// Gets the last trade date corresponding to the contract month
        /// </summary>
        /// <param name="time">Contract month</param>
        /// <param name="lastTradeTime">Time at which the dairy future contract stops trading (usually should be on 17:10:00 UTC)</param>
        /// <returns></returns>
        public static DateTime DairyLastTradeDate(DateTime time, TimeSpan? lastTradeTime = null)
        {
            // Trading shall terminate on the business day immediately preceding the day on which the USDA announces the <DAIRY_PRODUCT> price for that contract month. (LTD 12:10 p.m.)
            var contractMonth = new DateTime(time.Year, time.Month, 1);
            var lastTradeTs = lastTradeTime ?? new TimeSpan(17, 10, 0);

            DateTime publicationDate;
            if (FuturesExpiryFunctions.DairyReportDates.TryGetValue(contractMonth, out publicationDate))
            {
                do
                {
                    publicationDate = publicationDate.AddDays(-1);
                }
                while (USHoliday.Dates.Contains(publicationDate) || publicationDate.DayOfWeek == DayOfWeek.Saturday);
            }
            else
            {
                publicationDate = contractMonth.AddMonths(1);
            }

            // The USDA price announcements are erratic in their publication date. You can view the calendar the USDA announces prices here: https://www.ers.usda.gov/calendar/
            // More specifically, the report you should be looking for has the name "National Dairy Products Sales Report".
            // To get the report dates found in FutuesExpiryFunctions.DairyReportDates, visit this website: https://mpr.datamart.ams.usda.gov/menu.do?path=Products\Dairy\All%20Dairy\(DY_CL102)%20National%20Dairy%20Products%20Prices%20-%20Monthly

            return publicationDate.Add(lastTradeTs);
        }

        /// <summary>
        /// Returns true if the future expires in the month before the contract month
        /// </summary>
        /// <param name="underlying">The future symbol</param>
        /// <returns>True if the future expires in the month before the contract month</returns>
        public static bool ExpiresInPreviousMonth(string underlying)
        {
            return
                underlying == Futures.Energies.CrudeOilWTI ||
                underlying == Futures.Energies.Gasoline ||
                underlying == Futures.Energies.HeatingOil ||
                underlying == Futures.Energies.NaturalGas;
        }
    }
}
