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
using NodaTime;
using QuantConnect.Util;

namespace QuantConnect.Securities
{
    /// <summary>
    /// Base exchange class providing information and helper tools for reading the current exchange situation
    /// </summary>
    public class SecurityExchange
    {
        private DateTime _localFrontier;
        private SecurityExchangeHours _exchangeHours;

        /// <summary>
        /// Gets the <see cref="SecurityExchangeHours"/> for this exchange
        /// </summary>
        public SecurityExchangeHours Hours => _exchangeHours;

        /// <summary>
        /// Gets the time zone for this exchange
        /// </summary>
        public DateTimeZone TimeZone => _exchangeHours.TimeZone;

        /// <summary>
        /// Number of trading days per year for this security. By default the market is open 365 days per year.
        /// </summary>
        /// <remarks>Used for performance statistics to calculate sharpe ratio accurately</remarks>
        public virtual int TradingDaysPerYear => 365;

        /// <summary>
        /// Time from the most recent data
        /// </summary>
        public DateTime LocalTime => _localFrontier;

        /// <summary>
        /// Boolean property for quickly testing if the exchange is open.
        /// </summary>
        public bool ExchangeOpen => _exchangeHours.IsOpen(_localFrontier, false);

        /// <summary>
        /// Initializes a new instance of the <see cref="SecurityExchange"/> class using the specified
        /// exchange hours to determine open/close times
        /// </summary>
        /// <param name="exchangeHours">Contains the weekly exchange schedule plus holidays</param>
        public SecurityExchange(SecurityExchangeHours exchangeHours)
        {
            _exchangeHours = exchangeHours;
        }

        /// <summary>
        /// Set the current datetime in terms of the exchange's local time zone
        /// </summary>
        /// <param name="newLocalTime">Most recent data tick</param>
        public void SetLocalDateTimeFrontier(DateTime newLocalTime)
        {
            _localFrontier = newLocalTime;
        }

        /// <summary>
        /// Check if the *date* is open.
        /// </summary>
        /// <remarks>This is useful for first checking the date list, and then the market hours to save CPU cycles</remarks>
        /// <param name="dateToCheck">Date to check</param>
        /// <returns>Return true if the exchange is open for this date</returns>
        public bool DateIsOpen(DateTime dateToCheck)
        {
            return _exchangeHours.IsDateOpen(dateToCheck);
        }

        /// <summary>
        /// Check if this DateTime is open.
        /// </summary>
        /// <param name="dateTime">DateTime to check</param>
        /// <returns>Boolean true if the market is open</returns>
        public bool DateTimeIsOpen(DateTime dateTime)
        {
            return _exchangeHours.IsOpen(dateTime, false);
        }

        /// <summary>
        /// Determines if the exchange was open at any time between start and stop
        /// </summary>
        public bool IsOpenDuringBar(DateTime barStartTime, DateTime barEndTime, bool isExtendedMarketHours)
        {
            return _exchangeHours.IsOpen(barStartTime, barEndTime, isExtendedMarketHours);
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

            var marketHours = _exchangeHours.MarketHours.ToDictionary();
            marketHoursSegments = marketHoursSegments as IList<MarketHoursSegment> ?? marketHoursSegments.ToList();
            foreach (var day in days)
            {
                marketHours[day] = new LocalMarketHours(day, marketHoursSegments);
            }

            // create a new exchange hours instance for the new hours
            _exchangeHours = new SecurityExchangeHours(_exchangeHours.TimeZone, _exchangeHours.Holidays, marketHours, _exchangeHours.EarlyCloses, _exchangeHours.LateOpens);
        }
    }
}