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
 *
*/

using System;

namespace QuantConnect
{
    /// <summary>
    /// Provides static functions that can be used to compute a future <see cref="DateTime"/> (expiry) given a <see cref="DateTime"/>.
    /// </summary>
    public static class Expiry
    {
        /// <summary>
        /// Computes a date/time one month after a given date/time (nth day to nth day)
        /// </summary>
        public static Func<DateTime, DateTime> OneMonth => dt => dt.AddMonths(1);

        /// <summary>
        /// Computes a date/time one quarter after a given date/time (nth day to nth day)
        /// </summary>
        public static Func<DateTime, DateTime> OneQuarter => dt => dt.AddMonths(3);

        /// <summary>
        /// Computes a date/time one year after a given date/time (nth day to nth day)
        /// </summary>
        public static Func<DateTime, DateTime> OneYear => dt => dt.AddYears(1);

        /// <summary>
        /// Computes the end of day (mid-night of the next day) of given date/time
        /// </summary>
        public static Func<DateTime, DateTime> EndOfDay => dt => dt.AddDays(1).Date;

        /// <summary>
        /// Computes the end of week (next Monday) of given date/time
        /// </summary>
        public static Func<DateTime, DateTime> EndOfWeek
        {
            get
            {
                return dt =>
                {
                    var value = 8 - (int)dt.DayOfWeek;
                    if (value == 8) value = 1;   // Sunday
                    return dt.AddDays(value).Date;
                };
            }
        }

        /// <summary>
        /// Computes the end of month (1st of the next month) of given date/time
        /// </summary>
        public static Func<DateTime, DateTime> EndOfMonth
        {
            get
            {
                return dt =>
                {
                    var value = OneMonth(dt);
                    return new DateTime(value.Year, value.Month, 1);
                };
            }
        }

        /// <summary>
        /// Computes the end of quarter (1st of the starting month of next quarter) of given date/time
        /// </summary>
        public static Func<DateTime, DateTime> EndOfQuarter
        {
            get
            {
                return dt =>
                {
                    var nthQuarter = (dt.Month - 1) / 3;
                    var firstMonthOfQuarter = nthQuarter * 3 + 1;
                    return OneQuarter(new DateTime(dt.Year, firstMonthOfQuarter, 1));
                };
            }
        }

        /// <summary>
        /// Computes the end of year (1st of the next year) of given date/time
        /// </summary>
        public static Func<DateTime, DateTime> EndOfYear => dt => new DateTime(dt.Year + 1, 1, 1);
    }
}