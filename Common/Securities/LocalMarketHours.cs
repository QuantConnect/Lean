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

namespace QuantConnect.Securities
{
    /// <summary>
    /// Represents the market hours under normal conditions for an exchange and a specific day of the week in terms of local time
    /// </summary>
    public class LocalMarketHours
    {
        private readonly bool _hasPreMarket;
        private readonly bool _hasPostMarket;
        private readonly bool _isOpenAllDay;
        private readonly bool _isClosedAllDay;
        private readonly DayOfWeek _dayOfWeek;
        private readonly MarketHoursSegment[] _segments;

        /// <summary>
        /// Gets whether or not this exchange is closed all day
        /// </summary>
        public bool IsClosedAllDay
        {
            get { return _isClosedAllDay; }
        }

        /// <summary>
        /// Gets whether or not this exchange is closed all day
        /// </summary>
        public bool IsOpenAllDay
        {
            get { return _isOpenAllDay; }
        }

        /// <summary>
        /// Gets the day of week these hours apply to
        /// </summary>
        public DayOfWeek DayOfWeek 
        {
            get { return _dayOfWeek; }
        }

        /// <summary>
        /// Gets the individual market hours segments that define the hours of operation for this day
        /// </summary>
        public IEnumerable<MarketHoursSegment> Segments
        {
            get { return _segments; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalMarketHours"/> class
        /// </summary>
        /// <param name="day">The day of the week these hours are applicable</param>
        /// <param name="segments">The open/close segments defining the market hours for one day</param>
        public LocalMarketHours(DayOfWeek day, params MarketHoursSegment[] segments)
            : this(day, (IEnumerable<MarketHoursSegment>) segments)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalMarketHours"/> class
        /// </summary>
        /// <param name="day">The day of the week these hours are applicable</param>
        /// <param name="segments">The open/close segments defining the market hours for one day</param>
        public LocalMarketHours(DayOfWeek day, IEnumerable<MarketHoursSegment> segments)
        {
            _dayOfWeek = day;
            // filter out the closed states, we'll assume closed if no segment exists
            _segments = (segments ?? Enumerable.Empty<MarketHoursSegment>()).Where(x => x.State != MarketHoursState.Closed).ToArray();
            _isClosedAllDay = _segments.Length == 0;
            _isOpenAllDay = _segments.Length == 1 
                && _segments[0].Start == TimeSpan.Zero 
                && _segments[0].End == Time.OneDay
                && _segments[0].State == MarketHoursState.Market;

            foreach (var segment in _segments)
            {
                if (segment.State == MarketHoursState.PreMarket)
                {
                    _hasPreMarket = true;
                }
                if (segment.State == MarketHoursState.PostMarket)
                {
                    _hasPostMarket = true;
                }
            }
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

            var segments = new List<MarketHoursSegment>();

            if (extendedMarketOpen != marketOpen)
            {
                _hasPreMarket = true;
                segments.Add(new MarketHoursSegment(MarketHoursState.PreMarket, extendedMarketOpen, marketOpen));
            }

            if (marketOpen != TimeSpan.Zero || marketClose != TimeSpan.Zero)
            {
                segments.Add(new MarketHoursSegment(MarketHoursState.Market, marketOpen, marketClose));
            }

            if (marketClose != extendedMarketClose)
            {
                _hasPostMarket = true;
                segments.Add(new MarketHoursSegment(MarketHoursState.PostMarket, marketClose, extendedMarketClose));
            }

            _segments = segments.ToArray();
            _isClosedAllDay = _segments.Length == 0;

            // perform some sanity checks
            if (marketOpen < extendedMarketOpen)
            {
                throw new ArgumentException("Extended market open time must be less than or equal to market open time.");
            }
            if (marketClose < marketOpen)
            {
                throw new ArgumentException("Market close time must be after market open time.");
            }
            if (extendedMarketClose < marketClose)
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
        /// <param name="time">The reference time, the open returned will be the first open after the specified time if there are multiple market open segments</param>
        /// <param name="extendedMarket">True to include extended market hours, false for regular market hours</param>
        /// <returns>The market's opening time of day</returns>
        public TimeSpan? GetMarketOpen(TimeSpan time, bool extendedMarket)
        {
            for (int i = 0; i < _segments.Length; i++)
            {
                if (_segments[i].State == MarketHoursState.Closed || _segments[i].End <= time)
                {
                    continue;
                }

                if (extendedMarket && _hasPreMarket)
                {
                    if (_segments[i].State == MarketHoursState.PreMarket)
                    {
                        return _segments[i].Start;
                    }
                }
                else if (_segments[i].State == MarketHoursState.Market)
                {
                    return _segments[i].Start;
                }
            }

            // we couldn't locate an open segment after the specified time
            return null;
        }

        /// <summary>
        /// Gets the market closing time of day
        /// </summary>
        /// <param name="time">The reference time, the close returned will be the first close after the specified time if there are multiple market open segments</param>
        /// <param name="extendedMarket">True to include extended market hours, false for regular market hours</param>
        /// <returns>The market's closing time of day</returns>
        public TimeSpan? GetMarketClose(TimeSpan time, bool extendedMarket)
        {
            for (int i = 0; i < _segments.Length; i++)
            {
                if (_segments[i].State == MarketHoursState.Closed || _segments[i].End <= time)
                {
                    continue;
                }

                if (extendedMarket && _hasPostMarket)
                {
                    if (_segments[i].State == MarketHoursState.PostMarket)
                    {
                        return _segments[i].End;
                    }
                }
                else if (_segments[i].State == MarketHoursState.Market)
                {
                    return _segments[i].End;
                }
            }
            
            // we couldn't locate an open segment after the specified time
            return null;
        }

        /// <summary>
        /// Determines if the exchange is open at the specified time
        /// </summary>
        /// <param name="time">The time of day to check</param>
        /// <param name="extendedMarket">True to check exended market hours, false to check regular market hours</param>
        /// <returns>True if the exchange is considered open, false otherwise</returns>
        public bool IsOpen(TimeSpan time, bool extendedMarket)
        {
            for (int i = 0; i < _segments.Length; i++)
            {
                if (_segments[i].State == MarketHoursState.Closed)
                {
                    continue;
                }

                if (_segments[i].Contains(time))
                {
                    return extendedMarket || _segments[i].State == MarketHoursState.Market;
                }
            }

            // if we didn't find a segment then we're closed
            return false;
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
            
            for (int i = 0; i < _segments.Length; i++)
            {
                if (_segments[i].State == MarketHoursState.Closed)
                {
                    continue;
                }

                if (extendedMarket || _segments[i].State == MarketHoursState.Market)
                {
                    if (_segments[i].Overlaps(start, end))
                    {
                        return true;
                    }
                }
            }

            // if we didn't find a segment then we're closed
            return false;
        }

        /// <summary>
        /// Gets a <see cref="LocalMarketHours"/> instance that is always closed
        /// </summary>
        /// <param name="dayOfWeek">The day of week</param>
        /// <returns>A <see cref="LocalMarketHours"/> instance that is always closed</returns>
        public static LocalMarketHours ClosedAllDay(DayOfWeek dayOfWeek)
        {
            return new LocalMarketHours(dayOfWeek);
        }

        /// <summary>
        /// Gets a <see cref="LocalMarketHours"/> instance that is always open
        /// </summary>
        /// <param name="dayOfWeek">The day of week</param>
        /// <returns>A <see cref="LocalMarketHours"/> instance that is always open</returns>
        public static LocalMarketHours OpenAllDay(DayOfWeek dayOfWeek)
        {
            return new LocalMarketHours(dayOfWeek, new MarketHoursSegment(MarketHoursState.Market, TimeSpan.Zero, Time.OneDay));
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
            if (IsClosedAllDay)
            {
                return "Closed All Day";
            }
            if (IsOpenAllDay)
            {
                return "Open All Day";
            }
            return DayOfWeek + ": " + string.Join(" | ", (IEnumerable<MarketHoursSegment>) _segments);
        }
    }
}