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
using NodaTime;
using System.Linq;
using QuantConnect.Util;
using System.Collections.Generic;

namespace QuantConnect.Securities
{
    /// <summary>
    /// Base exchange class providing information and helper tools for reading the current exchange situation
    /// </summary>
    public class SecurityExchange
    {
        private LocalTimeKeeper _timeProvider;

        /// <summary>
        /// Gets the <see cref="SecurityExchangeHours"/> for this exchange
        /// </summary>
        public SecurityExchangeHours Hours { get; private set; }

        /// <summary>
        /// Gets the time zone for this exchange
        /// </summary>
        public DateTimeZone TimeZone => Hours.TimeZone;

        /// <summary>
        /// Number of trading days per year for this security. By default the market is open 365 days per year.
        /// </summary>
        /// <remarks>Used for performance statistics to calculate sharpe ratio accurately</remarks>
        public virtual int TradingDaysPerYear => 365;

        /// <summary>
        /// Time from the most recent data
        /// </summary>
        public DateTime LocalTime => _timeProvider.LocalTime;

        /// <summary>
        /// Boolean property for quickly testing if the exchange is open.
        /// </summary>
        public bool ExchangeOpen => Hours.IsOpen(LocalTime, false);

        /// <summary>
        /// Boolean property for quickly testing if the exchange is 10 minutes away from closing.
        /// </summary>
        public bool ClosingSoon => IsClosingSoon(minutesToClose:10);

        /// <summary>
        /// Initializes a new instance of the <see cref="SecurityExchange"/> class using the specified
        /// exchange hours to determine open/close times
        /// </summary>
        /// <param name="exchangeHours">Contains the weekly exchange schedule plus holidays</param>
        public SecurityExchange(SecurityExchangeHours exchangeHours)
        {
            Hours = exchangeHours;
        }

        /// <summary>
        /// Set the current datetime in terms of the exchange's local time zone
        /// </summary>
        /// <param name="timeProvider">Most recent data tick</param>
        public void SetLocalDateTimeFrontierProvider(LocalTimeKeeper timeProvider)
        {
            _timeProvider = timeProvider;
        }

        /// <summary>
        /// Check if the *date* is open.
        /// </summary>
        /// <remarks>This is useful for first checking the date list, and then the market hours to save CPU cycles</remarks>
        /// <param name="dateToCheck">Date to check</param>
        /// <returns>Return true if the exchange is open for this date</returns>
        public bool DateIsOpen(DateTime dateToCheck)
        {
            return Hours.IsDateOpen(dateToCheck);
        }

        /// <summary>
        /// Check if this DateTime is open.
        /// </summary>
        /// <param name="dateTime">DateTime to check</param>
        /// <returns>Boolean true if the market is open</returns>
        public bool DateTimeIsOpen(DateTime dateTime)
        {
            return Hours.IsOpen(dateTime, false);
        }

        /// <summary>
        /// Determines if the exchange was open at any time between start and stop
        /// </summary>
        public bool IsOpenDuringBar(DateTime barStartTime, DateTime barEndTime, bool isExtendedMarketHours)
        {
            return Hours.IsOpen(barStartTime, barEndTime, isExtendedMarketHours);
        }

        /// <summary>
        /// Determines if the exchange is going to close in the next provided minutes
        /// </summary>
        /// <param name="minutesToClose">Minutes to close to check</param>
        /// <returns>Returns true if the exchange is going to close in the next provided minutes</returns>
        public bool IsClosingSoon(int minutesToClose)
        {
            return !Hours.IsOpen(LocalTime.AddMinutes(minutesToClose), false);
        }

        /// <summary>
        /// Sets the regular market hours for the specified days If no days are specified then
        /// all days will be updated.
        /// </summary>
        /// <param name="marketHoursSegments">Specifies each segment of the market hours, such as premarket/market/postmark</param>
        /// <param name="days">The days of the week to set these times for</param>
        public void SetMarketHours(IEnumerable<MarketHoursSegment> marketHoursSegments, params DayOfWeek[] days)
        {
            if (days.IsNullOrEmpty()) days = Enum.GetValues(typeof(DayOfWeek)).OfType<DayOfWeek>().ToArray();

            var marketHours = Hours.MarketHours.ToDictionary();
            marketHoursSegments = marketHoursSegments as IList<MarketHoursSegment> ?? marketHoursSegments.ToList();
            foreach (var day in days)
            {
                marketHours[day] = new LocalMarketHours(day, marketHoursSegments);
            }

            // create a new exchange hours instance for the new hours
            Hours = new SecurityExchangeHours(Hours.TimeZone, Hours.Holidays, marketHours, Hours.EarlyCloses, Hours.LateOpens);
        }
    }
}
