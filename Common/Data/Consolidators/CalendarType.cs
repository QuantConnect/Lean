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

namespace QuantConnect.Data.Consolidators
{
    static public class CalendarType
    {
        /// <summary>
        /// Computes the start of week (previous Monday) of given date/time
        /// </summary>
        public static Func<DateTime, CalendarInfo> Weekly
        {
            get
            {
                return dt =>
                {
                    var start = Expiry.EndOfWeek(dt).AddDays(-7);
                    return new CalendarInfo(start, TimeSpan.FromDays(7));
                };
            }
        }

        /// <summary>
        /// Computes the start of month (1st of the current month) of given date/time
        /// </summary>
        public static Func<DateTime, CalendarInfo> Monthly
        {
            get
            {
                return dt =>
                {
                    var start = dt.AddDays(1 - dt.Day).Date;
                    var end = Expiry.EndOfMonth(dt);
                    return new CalendarInfo(start, end - start);
                };
            }
        }
    }

    public struct CalendarInfo
    {
        public readonly DateTime Start;
        public readonly TimeSpan Period;

        public CalendarInfo(DateTime start, TimeSpan period)
        {
            Start = start;
            Period = period;
        }
    }
}
