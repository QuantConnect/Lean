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
        public SecurityExchangeHours Hours
        {
            get { return _exchangeHours; }
        }

        /// <summary>
        /// Gets the time zone for this exchange
        /// </summary>
        public DateTimeZone TimeZone 
        {
            get { return _exchangeHours.TimeZone; }
        }

        /// <summary>
        /// Gets the market open time for the current day
        /// </summary>
        public TimeSpan MarketOpen
        {
            get { return _exchangeHours.GetMarketHours(_localFrontier).MarketOpen; }
        }

        /// <summary>
        /// Gets the market close time for the current day
        /// </summary>
        public TimeSpan MarketClose
        {
            get { return _exchangeHours.GetMarketHours(_localFrontier).MarketClose; }
        }

        /// <summary>
        /// Gets the extended market open time for the current day
        /// </summary>
        public TimeSpan ExtendedMarketOpen
        {
            get { return _exchangeHours.GetMarketHours(_localFrontier).ExtendedMarketOpen; }
        }

        /// <summary>
        /// Gets the extended market close time for the current day
        /// </summary>
        public TimeSpan ExtendedMarketClose
        {
            get { return _exchangeHours.GetMarketHours(_localFrontier).ExtendedMarketClose; }
        }

        /// <summary>
        /// Number of trading days per year for this security. By default the market is open 365 days per year.
        /// </summary>
        /// <remarks>Used for performance statistics to calculate sharpe ratio accurately</remarks>
        public virtual int TradingDaysPerYear
        {
            get { return 365; }
        }

        /// <summary>
        /// Time from the most recent data
        /// </summary>
        public DateTime LocalTime
        {
            get { return _localFrontier; }
        }

        /// <summary>
        /// Boolean property for quickly testing if the exchange is open.
        /// </summary>
        public bool ExchangeOpen
        {
            get { return _exchangeHours.IsOpen(_localFrontier, false); }
        }

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
        /// Gets the date time the market opens on the specified day
        /// </summary>
        /// <param name="time">DateTime object for this date</param>
        /// <returns>DateTime the market is considered open</returns>
        public DateTime TimeOfDayOpen(DateTime time)
        {
            return time.Date + _exchangeHours.GetMarketHours(time).MarketOpen;
        }

        /// <summary>
        /// Gets the date time the market closes on the specified day
        /// </summary>
        /// <param name="time">DateTime object for this date</param>
        /// <returns>DateTime the market day is considered closed</returns>
        public DateTime TimeOfDayClosed(DateTime time)
        {
            return time.Date + _exchangeHours.GetMarketHours(time).MarketClose;
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
        /// Check if the object is open including the *Extended* market hours
        /// </summary>
        /// <param name="time">Current time of day</param>
        /// <returns>True if we are in extended or primary marketing hours.</returns>
        public bool DateTimeIsExtendedOpen(DateTime time)
        {
            return _exchangeHours.IsOpen(time, true);
        }

        /// <summary>
        /// Determines if the exchange was open at any time between start and stop
        /// </summary>
        public bool IsOpenDuringBar(DateTime barStartTime, DateTime barEndTime, bool isExtendedMarketHours)
        {
            return _exchangeHours.IsOpen(barStartTime, barEndTime, isExtendedMarketHours);
        }

        /// <summary>
        /// Sets the regular market hours for the specified days. Extended market hours are
        /// set to the same as the regular market hours. If no days are specified then
        /// all days will be updated. 
        /// <para>Specify <see cref="TimeSpan.Zero"/> for both <paramref name="marketOpen"/>
        /// and <paramref name="marketClose"/> to close the exchange for the specified days.</para>
        /// <para>Specify
        /// <see cref="TimeSpan.Zero"/> for <paramref name="marketOpen"/> and <see cref="QuantConnect.Time.OneDay"/>
        /// for open all day</para>
        /// </summary>
        /// <param name="marketOpen">The time of day the market opens</param>
        /// <param name="marketClose">The time of day the market closes</param>
        /// <param name="days">The days of the week to set these times for</param>
        public void SetMarketHours(TimeSpan marketOpen, TimeSpan marketClose, params DayOfWeek[] days)
        {
            SetMarketHours(marketOpen, marketOpen, marketClose, marketClose, days);
        }

        /// <summary>
        /// Sets the regular market hours for the specified days If no days are specified then
        /// all days will be updated.
        /// </summary>
        /// <param name="extendedMarketOpen">The time of day the pre market opens</param>
        /// <param name="marketOpen">The time of day the market opens</param>
        /// <param name="marketClose">The time of day the market closes</param>
        /// <param name="extendedMarketClose">The time of day the post market opens</param>
        /// <param name="days">The days of the week to set these times for</param>
        public void SetMarketHours(TimeSpan extendedMarketOpen, TimeSpan marketOpen, TimeSpan marketClose, TimeSpan extendedMarketClose, params DayOfWeek[] days)
        {
            if (days.IsNullOrEmpty()) days = Enum.GetValues(typeof(DayOfWeek)).OfType<DayOfWeek>().ToArray();

            // if we specify close as 1 tick before the day rolls over, the exchange is still
            // considered to be open all day,so set it to one day and this impl will make it OpenAllDay
            if (extendedMarketOpen == marketOpen && marketOpen == TimeSpan.Zero && extendedMarketClose == marketClose && marketClose.Ticks == Time.OneDay.Ticks - 1)
            {
                marketClose = Time.OneDay;
            }
            
            // make sure extended hours are outside of regular hours
            extendedMarketOpen = TimeSpan.FromTicks(Math.Min(extendedMarketOpen.Ticks, marketOpen.Ticks));
            extendedMarketClose = TimeSpan.FromTicks(Math.Max(extendedMarketClose.Ticks, marketClose.Ticks));

            var marketHours = _exchangeHours.MarketHours.ToDictionary();
            foreach (var day in days)
            {
                if (marketOpen == TimeSpan.Zero && marketClose == TimeSpan.Zero)
                {
                    marketHours[day] = LocalMarketHours.ClosedAllDay(day);
                }
                else if (marketOpen == TimeSpan.Zero && marketClose == Time.OneDay)
                {
                    marketHours[day] = LocalMarketHours.OpenAllDay(day);
                }
                else
                {
                    marketHours[day] = new LocalMarketHours(day, extendedMarketOpen, marketOpen, marketClose, extendedMarketClose);
                }
            }

            // create a new exchange hours instance for the new hours
            _exchangeHours = new SecurityExchangeHours(_exchangeHours.TimeZone, _exchangeHours.Holidays, marketHours);
        }
    }
}