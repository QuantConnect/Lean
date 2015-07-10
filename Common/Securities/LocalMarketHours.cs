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
using System.Collections.ObjectModel;
using System.Linq;
using NodaTime;

namespace QuantConnect.Securities
{
    /// <summary>
    /// Represents the market hours under normal conditions for an exchange and a specific day of the week in terms of local time
    /// </summary>
    public class LocalMarketHours
    {
        private static readonly IReadOnlyDictionary<DayOfWeek, LocalMarketHours> Open = new ReadOnlyDictionary<DayOfWeek, LocalMarketHours>(
            Enum.GetValues(typeof(IsoDayOfWeek)).Cast<DayOfWeek>()
                .ToDictionary(x => x, x => new LocalMarketHours(x, TimeSpan.Zero, TimeSpan.FromTicks(Time.OneDay.Ticks - 1)))
            ); 
        private static readonly IReadOnlyDictionary<DayOfWeek, LocalMarketHours> Closed = new ReadOnlyDictionary<DayOfWeek, LocalMarketHours>(
            Enum.GetValues(typeof(IsoDayOfWeek)).Cast<DayOfWeek>()
                .ToDictionary(x => x, x => new LocalMarketHours(x, TimeSpan.Zero, TimeSpan.Zero))
            );

        private readonly DayOfWeek _dayOfWeek;
        private readonly TimeSpan _extendedMarketOpen;
        private readonly TimeSpan _marketOpen;
        private readonly TimeSpan _marketClose;
        private readonly TimeSpan _extendedMarketClose;

        /// <summary>
        /// Gets whether or not this exchange is closed all day
        /// </summary>
        public bool IsClosedAllDay
        {
            get { return _extendedMarketOpen == _marketOpen && _marketOpen == _marketClose && _marketClose == _extendedMarketClose; }
        }

        /// <summary>
        /// Gets the day of week these hours apply to
        /// </summary>
        public DayOfWeek DayOfWeek 
        {
            get { return _dayOfWeek; }
        }

        /// <summary>
        /// Gets the regular market opening time
        /// </summary>
        public TimeSpan MarketOpen
        {
            get { return _marketOpen; }
        }

        /// <summary>
        /// Gets the regular market closing time
        /// </summary>
        public TimeSpan MarketClose
        {
            get { return _marketClose; }
        }

        /// <summary>
        /// Gets the extended market opening time
        /// </summary>
        public TimeSpan ExtendedMarketOpen
        {
            get { return _extendedMarketOpen; }
        }

        /// <summary>
        /// Gets the extended market closing time
        /// </summary>
        public TimeSpan ExtendedMarketClose
        {
            get { return _extendedMarketClose; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalMarketHours"/> class from the specified open/close times
        /// </summary>
        /// <param name="day">The day of week these hours apply to</param>
        /// <param name="extendedMarketOpen">The extended market open time</param>
        /// <param name="marketOpen">The regular market open time, must be greater than or equal to the extended market open time</param>
        /// <param name="marketClose">The regular market close time, must be greater than the regular market open time</param>
        /// <param name="extendedMarketClose">The extended market close time, must be greater than or equal to the regular market close time</param>
        public LocalMarketHours(DayOfWeek day, TimeSpan extendedMarketOpen, TimeSpan marketOpen, TimeSpan marketClose, TimeSpan extendedMarketClose)
        {
            _dayOfWeek = day;
            _extendedMarketOpen = extendedMarketOpen;
            _marketOpen = marketOpen;
            _marketClose = marketClose;
            _extendedMarketClose = extendedMarketClose;

            // perform some sanity checks
            if (_marketOpen < _extendedMarketOpen)
            {
                throw new ArgumentException("Extended market open time must be less than or equal to market open time.");
            }
            if (_marketClose < _marketOpen)
            {
                throw new ArgumentException("Market close time must be after market open time.");
            }
            if (_extendedMarketClose < _marketClose)
            {
                throw new ArgumentException("Extended market close time must be greater than or equal to market close time.");
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalMarketHours"/> class from the specified open/close times
        /// using the market open as the extended market open and the market close as the extended market close, effectively
        /// removing any 'extended' session from these exchange hours
        /// </summary>
        /// <param name="day">The day of week these hours apply to</param>
        /// <param name="marketOpen">The regular market open time</param>
        /// <param name="marketClose">The regular market close time, must be greater than the regular market open time</param>
        public LocalMarketHours(DayOfWeek day, TimeSpan marketOpen, TimeSpan marketClose)
            : this(day, marketOpen, marketOpen, marketClose, marketClose)
        {
        }

        /// <summary>
        /// Gets the market opening time of day
        /// </summary>
        /// <param name="extendedMarket">True to include extended market hours, false for regular market hours</param>
        /// <returns>The market's opening time of day</returns>
        public TimeSpan GetMarketOpen(bool extendedMarket)
        {
            return extendedMarket ? ExtendedMarketOpen : MarketOpen;
        }

        /// <summary>
        /// Gets the market closing time of day
        /// </summary>
        /// <param name="extendedMarket">True to include extended market hours, false for regular market hours</param>
        /// <returns>The market's closing time of day</returns>
        public TimeSpan GetMarketClose(bool extendedMarket)
        {
            return extendedMarket ? ExtendedMarketClose : MarketClose;
        }

        /// <summary>
        /// Determines if the exchange is open at the specified time
        /// </summary>
        /// <param name="time">The time of day to check</param>
        /// <param name="extendedMarket">True to check exended market hours, false to check regular market hours</param>
        /// <returns>True if the exchange is considered open, false otherwise</returns>
        public bool IsOpen(TimeSpan time, bool extendedMarket)
        {
            // the market open is included but market close is excluded
            if (extendedMarket)
            {
                return time >= _extendedMarketOpen
                    && time < _extendedMarketClose;
            }
            return time >= _marketOpen
                && time < _marketClose;
        }

        /// <summary>
        /// Determines if the exchange is open during the specified interval
        /// </summary>
        /// <param name="start">The start time of the interval</param>
        /// <param name="end">The end time of the interval</param>
        /// <param name="extendedMarket">True to check exended market hours, false to check regular market hours</param>
        /// <returns>True if the exchange is considered open, false otherwise</returns>
        public bool IsOpen(TimeSpan start, TimeSpan end, bool extendedMarket)
        {
            if (start == end)
            {
                return IsOpen(start, extendedMarket);
            }

            if (extendedMarket)
            {
                return end > _extendedMarketOpen
                    && start < _extendedMarketClose;
            }
            return end > _marketOpen
                && start < _marketClose;
        }

        /// <summary>
        /// Gets a <see cref="LocalMarketHours"/> instance that is always closed
        /// </summary>
        /// <param name="dayOfWeek">The day of week</param>
        /// <returns>A <see cref="LocalMarketHours"/> instance that is always closed</returns>
        public static LocalMarketHours ClosedAllDay(DayOfWeek dayOfWeek)
        {
            return Closed[dayOfWeek];
        }

        /// <summary>
        /// Gets a <see cref="LocalMarketHours"/> instance that is always open
        /// </summary>
        /// <param name="dayOfWeek">The day of week</param>
        /// <returns>A <see cref="LocalMarketHours"/> instance that is always open</returns>
        public static LocalMarketHours OpenAllDay(DayOfWeek dayOfWeek)
        {
            return Open[dayOfWeek];
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public override string ToString()
        {
            if (this == ClosedAllDay(DayOfWeek))
            {
                return "Closed All Day";
            }
            if (this == OpenAllDay(DayOfWeek))
            {
                return "Open All Day";
            }
            if (ExtendedMarketOpen != MarketOpen && ExtendedMarketClose != MarketClose)
            {
                return string.Format("{0} - {1},{2},{3},{4}", DayOfWeek, ExtendedMarketOpen, MarketOpen, MarketClose, ExtendedMarketClose);
            }
            return string.Format("{0} - {1},{2}", DayOfWeek, MarketOpen, MarketClose);
        }
    }
}