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
using System.Collections.Generic;
using QuantConnect.Securities;

namespace QuantConnect.Scheduling
{
    /// <summary>
    /// Specifies that an event should fire on the first trading day of the month
    /// </summary>
    public class MonthStartDateRule : IDateRule
    {
        /// <summary>
        /// Gets an instance of <see cref="MonthStartDateRule"/> that always fires on the first of the month
        /// </summary>
        public static readonly IDateRule Instance = new MonthStartDateRule();

        private readonly Security _security;

        /// <summary>
        /// Initializes a new instance of the <see cref="MonthStartDateRule"/> class
        /// </summary>
        /// <remarks>
        /// This constructor will produce events that always fire on the first day of the month, regardless
        /// if markets are open or closed
        /// </remarks>
        public MonthStartDateRule()
        {
            _security = null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MonthStartDateRule"/> class
        /// </summary>
        /// <param name="security">The security used to determine the first trading day of the month</param>
        public MonthStartDateRule(Security security)
        {
            _security = security;
        }

        /// <summary>
        /// Gets a name for this rule
        /// </summary>
        public string Name
        {
            get { return _security == null ? "MonthStart" : _security.Symbol + ": MonthStart"; }
        }

        /// <summary>
        /// Gets the dates produced by this date rule between the specified times
        /// </summary>
        /// <param name="start">The start of the interval to produce dates for</param>
        /// <param name="end">The end of the interval to produce dates for</param>
        /// <returns>All dates in the interval matching this date rule</returns>
        public IEnumerable<DateTime> GetDates(DateTime start, DateTime end)
        {
            if (_security == null)
            {
                foreach (var date in Time.EachDay(start, end))
                {
                    // fire on the first of each month
                    if (date.Day == 1) yield return date;
                }
                yield break;
            }

            // start a month back so we can properly resolve the first event (we may have passed it)
            var aMonthBeforeStart = start.AddMonths(-1);
            int lastMonth = aMonthBeforeStart.Month;
            foreach (var date in Time.EachTradeableDay(_security, aMonthBeforeStart, end))
            {
                if (date.Month != lastMonth)
                {
                    if (date >= start)
                    {
                        // only emit if the date is on or after the start
                        // the date may be before here because we backed up a month
                        // to properly resolve the first tradeable date
                        yield return date;
                    }
                    lastMonth = date.Month;
                }
            }
        }
    }
}