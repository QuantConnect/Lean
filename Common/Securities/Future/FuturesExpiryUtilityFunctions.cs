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
        private static readonly Dictionary<DateTime, DateTime> _reverseDairyReportDates = FuturesExpiryFunctions.DairyReportDates
            .ToDictionary(kvp => kvp.Value, kvp => kvp.Key);

        private static readonly HashSet<string> _dairyUnderlying = new HashSet<string>
        {
            "CB",
            "CSC",
            "DC",
            "DY",
            "GDK",
            "GNF"
        };

        /// <summary>
        /// True to account for bank holidays which will adjust futures expiration dates
        /// </summary>
        public static bool BankHolidays { get; set; }

        /// <summary>
        /// Get holiday list from the MHDB given the market and the symbol of the security
        /// </summary>
        /// <param name="market">The market the exchange resides in, i.e, 'usa', 'fxcm', ect...</param>
        /// <param name="symbol">The particular symbol being traded</param>s
        internal static HashSet<DateTime> GetExpirationHolidays(string market, string symbol)
        {
            var exchangeHours = MarketHoursDatabase.FromDataFolder()
                        .GetEntry(market, symbol, SecurityType.Future)
                        .ExchangeHours;
            if (BankHolidays)
            {
                return exchangeHours.Holidays.Concat(exchangeHours.BankHolidays).ToHashSet();
            }
            return exchangeHours.Holidays;
        }

        /// <summary>
        /// Method to retrieve n^th succeeding/preceding business day for a given day
        /// </summary>
        /// <param name="time">The current Time</param>
        /// <param name="n">Number of business days succeeding current time. Use negative value for preceding business days</param>
        /// <param name="holidays">Set of holidays to exclude. These should be sourced from the <see cref="MarketHoursDatabase"/></param>
        /// <returns>The date-time after adding n business days</returns>
        public static DateTime AddBusinessDays(DateTime time, int n, HashSet<DateTime> holidays)
        {
            if (n < 0)
            {
                var businessDays = -n;
                var totalDays = 1;
                do
                {
                    var previousDay = time.AddDays(-totalDays);
                    if (!holidays.Contains(previousDay.Date) && previousDay.IsCommonBusinessDay())
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
                    if (!holidays.Contains(previousDay.Date) && previousDay.IsCommonBusinessDay())
                    {
                        businessDays--;
                    }

                    if (businessDays > 0) totalDays++;
                } while (businessDays > 0);

                return time.AddDays(totalDays);
            }
        }

        /// <summary>
        /// Method to retrieve n^th succeeding/preceding business day for a given day if there was a holiday on that day
        /// </summary>
        /// <param name="time">The current Time</param>
        /// <param name="n">Number of business days succeeding current time. Use negative value for preceding business days</param>
        /// <param name="holidayList">Enumerable of holidays to exclude. These should be sourced from the <see cref="MarketHoursDatabase"/></param>
        /// <returns>The date-time after adding n business days</returns>
        public static DateTime AddBusinessDaysIfHoliday(DateTime time, int n, HashSet<DateTime> holidayList)
        {
            if (holidayList.Contains(time))
            {
                return AddBusinessDays(time, n, holidayList);
            }
            else
            {
                return time;
            }
        }

        /// <summary>
        /// Method to retrieve the n^th last business day of the delivery month.
        /// </summary>
        /// <param name="time">DateTime for delivery month</param>
        /// <param name="n">Number of days</param>
        /// <param name="holidayList">Holidays to use while calculating n^th business day. Useful for MHDB entries</param>
        /// <returns>Nth Last Business day of the month</returns>
        public static DateTime NthLastBusinessDay(DateTime time, int n, IEnumerable<DateTime> holidayList)
        {
            var daysInMonth = DateTime.DaysInMonth(time.Year, time.Month);
            var lastDayOfMonth = new DateTime(time.Year, time.Month, daysInMonth);
            var holidays = holidayList.Select(x => x.Date);

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
                if (NotHoliday(previousDay, holidays) && !holidays.Contains(previousDay))
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
        /// <param name="holidayList"> Holidays to not count as business days</param>
        /// <returns>Nth business day of the month</returns>
        public static DateTime NthBusinessDay(DateTime time, int nthBusinessDay, IEnumerable<DateTime> holidayList)
        {
            var daysInMonth = DateTime.DaysInMonth(time.Year, time.Month);
            var holidays = holidayList.Select(x => x.Date);
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

            var calculatedTime = new DateTime(time.Year, time.Month, 1);

            var daysCounted = calculatedTime.IsCommonBusinessDay() ? 1 : 0;
            var i = 0;

            // Check for holiday up here in case we want the first business day and it is a holiday so that we don't skip over it.
            // We also want to make sure that we don't stop on a weekend.
            while (daysCounted < nthBusinessDay || holidays.Contains(calculatedTime) || !calculatedTime.IsCommonBusinessDay())
            {
                // The asset continues trading on days contained within `USHoliday.Dates`, but
                // the last trade date is affected by those holidays. We check for
                // both MHDB entries and holidays to get accurate business days
                if (holidays.Contains(calculatedTime))
                {
                    // Catches edge case where first day is on a friday
                    if (i == 0 && calculatedTime.DayOfWeek == DayOfWeek.Friday)
                    {
                        daysCounted = 0;
                    }

                    calculatedTime = calculatedTime.AddDays(1);

                    if (i != 0 && calculatedTime.IsCommonBusinessDay())
                    {
                        daysCounted++;
                    }
                    i++;
                    continue;
                }

                calculatedTime = calculatedTime.AddDays(1);

                if (!holidays.Contains(calculatedTime) && NotHoliday(calculatedTime, holidays))
                {
                    daysCounted++;
                }
                i++;
            }

            return calculatedTime;
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
        public static DateTime NthFriday(DateTime time, int n) => NthWeekday(time, n, DayOfWeek.Friday);

        /// <summary>
        /// Method to retrieve third Wednesday of the given month (usually Monday).
        /// </summary>
        /// <param name="time">Date from the given month</param>
        /// <returns>Third Wednesday of the given month</returns>
        public static DateTime ThirdWednesday(DateTime time) => NthWeekday(time, 3, DayOfWeek.Wednesday);

        /// <summary>
        /// Method to retrieve the Nth Weekday of the given month
        /// </summary>
        /// <param name="time">Date from the given month</param>
        /// <param name="n">The order of the Weekday in the period</param>
        /// <param name="dayOfWeek">The day of the week</param>
        /// <returns>Nth Weekday of given month</returns>
        public static DateTime NthWeekday(DateTime time, int n, DayOfWeek dayOfWeek)
        {
            if (n < 1 || n > 5)
            {
                throw new ArgumentOutOfRangeException(nameof(n), "'n' lower than 1 or greater than 5");
            }

            var daysInMonth = DateTime.DaysInMonth(time.Year, time.Month);
            return (from day in Enumerable.Range(1, daysInMonth)
                    where new DateTime(time.Year, time.Month, day).DayOfWeek == dayOfWeek
                    select new DateTime(time.Year, time.Month, day)).ElementAt(n - 1);
        }


        /// <summary>
        /// Method to retrieve the last weekday of any month
        /// </summary>
        /// <param name="time">Date from the given month</param>
        /// <param name="dayOfWeek">the last weekday to be found</param>
        /// <returns>Last day of the we</returns>
        public static DateTime LastWeekday(DateTime time, DayOfWeek dayOfWeek)
        {

            var daysInMonth = DateTime.DaysInMonth(time.Year, time.Month);
            return (from day in Enumerable.Range(1, daysInMonth).Reverse()
                    where new DateTime(time.Year, time.Month, day).DayOfWeek == dayOfWeek
                    select new DateTime(time.Year, time.Month, day)).First();
        }

        /// <summary>
        /// Method to retrieve the last Thursday of any month
        /// </summary>
        /// <param name="time">Date from the given month</param>
        /// <returns>Last Thursday of the given month</returns>
        public static DateTime LastThursday(DateTime time) => LastWeekday(time, DayOfWeek.Thursday);

        /// <summary>
        /// Method to retrieve the last Friday of any month
        /// </summary>
        /// <param name="time">Date from the given month</param>
        /// <returns>Last Friday of the given month</returns>
        public static DateTime LastFriday(DateTime time) => LastWeekday(time, DayOfWeek.Friday);

        /// <summary>
        /// Method to check whether a given time is holiday or not
        /// </summary>
        /// <param name="time">The DateTime for consideration</param>
        /// <param name="holidayList">Enumerable of holidays to exclude. These should be sourced from the <see cref="MarketHoursDatabase"/></param>
        /// <returns>True if the time is not a holidays, otherwise returns false</returns>
        public static bool NotHoliday(DateTime time, IEnumerable<DateTime> holidayList)
        {
            return time.IsCommonBusinessDay() && !holidayList.Contains(time.Date);
        }

        /// <summary>
        /// This function takes Thursday as input and returns true if four weekdays preceding it are not Holidays
        /// </summary>
        /// <param name="thursday">DateTime of a given Thursday</param>
        /// <param name="holidayList">Enumerable of holidays to exclude. These should be sourced from the <see cref="MarketHoursDatabase"/></param>
        /// <returns>False if DayOfWeek is not Thursday or is not preceded by four weekdays,Otherwise returns True</returns>
        public static bool NotPrecededByHoliday(DateTime thursday, IEnumerable<DateTime> holidayList)
        {
            if (thursday.DayOfWeek != DayOfWeek.Thursday)
            {
                throw new ArgumentException("Input to NotPrecededByHolidays must be a Thursday");
            }
            var result = true;
            // for Monday, Tuesday and Wednesday
            for (var i = 1; i <= 3; i++)
            {
                if (!NotHoliday(thursday.AddDays(-i), holidayList))
                {
                    result = false;
                }
            }
            // for Friday
            if (!NotHoliday(thursday.AddDays(-6), holidayList))
            {
                result = false;
            }
            return result;
        }

        /// <summary>
        /// Gets the last trade date corresponding to the contract month
        /// </summary>
        /// <param name="time">Contract month</param>
        /// <param name="holidayList">Enumerable of holidays to exclude. These should be sourced from the <see cref="MarketHoursDatabase"/></param>
        /// <param name="lastTradeTime">Time at which the dairy future contract stops trading (usually should be on 17:10:00 UTC)</param>
        /// <returns></returns>
        public static DateTime DairyLastTradeDate(DateTime time, IEnumerable<DateTime> holidayList, TimeSpan? lastTradeTime = null)
        {
            // Trading shall terminate on the business day immediately preceding the day on which the USDA announces the <DAIRY_PRODUCT> price for that contract month. (LTD 12:10 p.m.)
            var contractMonth = new DateTime(time.Year, time.Month, 1);
            var lastTradeTs = lastTradeTime ?? new TimeSpan(17, 10, 0);

            if (FuturesExpiryFunctions.DairyReportDates.TryGetValue(contractMonth, out DateTime publicationDate))
            {
                do
                {
                    publicationDate = publicationDate.AddDays(-1);
                }
                while (holidayList.Contains(publicationDate) || publicationDate.DayOfWeek == DayOfWeek.Saturday);
            }
            else
            {
                publicationDate = contractMonth.AddMonths(1);
            }

            // The USDA price announcements are erratic in their publication date. You can view the calendar the USDA announces prices here: https://www.ers.usda.gov/calendar/
            // More specifically, the report you should be looking for has the name "National Dairy Products Sales Report".
            // To get the report dates found in FuturesExpiryFunctions.DairyReportDates, visit this website: https://mpr.datamart.ams.usda.gov/menu.do?path=Products\Dairy\All%20Dairy\(DY_CL102)%20National%20Dairy%20Products%20Prices%20-%20Monthly

            return publicationDate.Add(lastTradeTs);
        }

        /// <summary>
        /// Gets the number of months between the contract month and the expiry date.
        /// </summary>
        /// <param name="underlying">The future symbol ticker</param>
        /// <param name="futureExpiryDate">Expiry date to use to look up contract month delta. Only used for dairy, since we need to lookup its contract month in a pre-defined table.</param>
        /// <returns>The number of months between the contract month and the contract expiry</returns>
        public static int GetDeltaBetweenContractMonthAndContractExpiry(string underlying, DateTime? futureExpiryDate = null)
        {
            if (futureExpiryDate != null && _dairyUnderlying.Contains(underlying))
            {
                // Dairy can expire in the month following the contract month.
                var dairyReportDate = futureExpiryDate.Value.Date.AddDays(1);
                if (_reverseDairyReportDates.ContainsKey(dairyReportDate))
                {
                    var contractMonth = _reverseDairyReportDates[dairyReportDate];
                    // Gets the distance between two months in months
                    return ((contractMonth.Year - dairyReportDate.Year) * 12) + contractMonth.Month - dairyReportDate.Month;
                }

                return 0;
            }

            return ExpiriesPriorMonth.TryGetValue(underlying, out int value) ? value : 0;
        }

        /// <summary>
        /// Calculates the date of Good Friday for a given year.
        /// </summary>
        /// <param name="year">Year to calculate Good Friday for</param>
        /// <returns>Date of Good Friday</returns>
        public static DateTime GetGoodFriday(int year)
        {
            // Acknowledgement
            // Author: Jan Schreuder
            // Link: https://www.codeproject.com/Articles/10860/Calculating-Christian-Holidays
            // Calculates Easter Sunday as Easter is always celebrated on the Sunday immediately following the Paschal Full Moon date of the year
            int g = year % 19;
            int c = year / 100;
            int h = (c - c / 4 - (8 * c + 13) / 25 + 19 * g + 15) % 30;
            int i = h - h / 28 * (1 - h / 28 * (29 / (h + 1)) * ((21 - g) / 11));

            int day = i - (year + year / 4 + i + 2 - c + c / 4) % 7 + 28;
            int month = 3;
            if (day > 31)
            {
                month++;
                day -= 31;
            }

            // Calculate Good Friday
            return new DateTime(year, month, day).AddDays(-2);
        }

        private static readonly Dictionary<string, int> ExpiriesPriorMonth = new Dictionary<string, int>
        {
            { Futures.Energy.ArgusLLSvsWTIArgusTradeMonth, 1 },
            { Futures.Energy.ArgusPropaneSaudiAramco, 1 },
            { Futures.Energy.BrentCrude, 2 },
            { Futures.Energy.BrentLastDayFinancial, 2 },
            { Futures.Energy.CrudeOilWTI, 1 },
            { Futures.Energy.MicroCrudeOilWTI, 1 },
            { Futures.Energy.Gasoline, 1 },
            { Futures.Energy.HeatingOil, 1 },
            { Futures.Energy.MarsArgusVsWTITradeMonth, 1 },
            { Futures.Energy.NaturalGas, 1 },
            { Futures.Energy.NaturalGasHenryHubLastDayFinancial, 1 },
            { Futures.Energy.NaturalGasHenryHubPenultimateFinancial, 1 },
            { Futures.Energy.WTIHoustonArgusVsWTITradeMonth, 1 },
            { Futures.Energy.WTIHoustonCrudeOil, 1 },
            { Futures.Softs.Sugar11, 1 },
            { Futures.Softs.Sugar11CME, 1 }
        };
    }
}
