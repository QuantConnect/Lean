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
using Newtonsoft.Json;

namespace QuantConnect.Packets
{

    /// <summary>
    /// Market today information class
    /// </summary>
    public class MarketToday
    {
        /// <summary>
        /// Date this packet was generated.
        /// </summary>
        [JsonProperty(PropertyName = "date")]
        public DateTime Date { get; set; }

        /// <summary>
        /// Given the dates and times above, what is the current market status - open or closed.
        /// </summary>
        [JsonProperty(PropertyName = "status")]
        public string Status = "";

        /// <summary>
        /// Premarket hours for today
        /// </summary>
        [JsonProperty(PropertyName = "premarket")]
        public MarketHours PreMarket = new MarketHours(DateTime.Now, 4, 9.5);

        /// <summary>
        /// Normal trading market hours for today
        /// </summary>
        [JsonProperty(PropertyName = "open")]
        public MarketHours Open = new MarketHours(DateTime.Now, 9.5, 16);

        /// <summary>
        /// Post market hours for today
        /// </summary>
        [JsonProperty(PropertyName = "postmarket")]
        public MarketHours PostMarket = new MarketHours(DateTime.Now, 16, 20);

        /// <summary>
        /// Default constructor (required for JSON serialization)
        /// </summary>
        public MarketToday()
        { }

        /// <summary>
        /// Gets a MarketToday instance that represents an always open market
        /// </summary>
        public static MarketToday OpenAllDay(DateTime date)
        {
            return new MarketToday
            {
                Date = date.Date,
                Open = new MarketHours(date, 0, 24),
                PreMarket = new MarketHours(date, 0, 0),
                PostMarket = new MarketHours(date, 24, 24),
                Status = "open"
            };
        }

        /// <summary>
        /// Gets a MarketToday instance that represents a closed market
        /// </summary>
        public static MarketToday ClosedAllDay(DateTime date)
        {
            return new MarketToday
            {
                Date = date.Date,
                Open = new MarketHours(date, 0, 0),
                PostMarket = new MarketHours(date, 0, 0),
                PreMarket = new MarketHours(date, 0, 0),
                Status = "closed"
            };
        }

        /// <summary>
        /// Gets a MarketToday instance that represents the forex markets on the
        /// specified date. For simplicity, we assume forex is always opens from 
        /// 5pm sunday EST to 5pm friday EST
        /// </summary>
        public static MarketToday Forex(DateTime date)
        {
            if (Configuration.Config.Get("force-exchange-always-open") != "true")
            {
                // closed all day onf saturdays
                if (date.DayOfWeek == DayOfWeek.Saturday)
                {
                    return ClosedAllDay(date);
                }

                // most days are always open
                var marketToday = OpenAllDay(date);
                if (date.DayOfWeek == DayOfWeek.Sunday)
                {
                    // open at 5 on sundays
                    marketToday.Open = new MarketHours(date, 17, 24);
                    marketToday.PreMarket = new MarketHours(date, 17, 17);
                }
                else if (date.DayOfWeek == DayOfWeek.Friday)
                {
                    // closes at 5 on fridays
                    marketToday.Open = new MarketHours(date, 0, 17);
                    marketToday.PostMarket = new MarketHours(date, 17, 17);
                }
                return marketToday;
            }
            else
            {
                var marketToday = OpenAllDay(date);
                return marketToday;
            }
        }

        /// <summary>
        /// Gets a MarketToday instance that represents equity markets in the united states.
        /// Closed all day on Saturday and Sunday as well as for USHolidays, otherwise open
        /// between 9:30 and 4:00pm EST
        /// </summary>
        public static MarketToday Equity(DateTime date)
        {
            if (Configuration.Config.Get("force-exchange-always-open") != "true")
            {
                if (date.DayOfWeek == DayOfWeek.Saturday
                 || date.DayOfWeek == DayOfWeek.Sunday
                 || USHoliday.Dates.Contains(date.Date))
                {
                    return ClosedAllDay(date);
                }

                // determine if we're not within normal market hours
                var status = "open";
                if (date.TimeOfDay > TimeSpan.FromHours(16) || date.TimeOfDay < TimeSpan.FromHours(9.5))
                {
                    status = "closed";
                }

                return new MarketToday
                {
                    PreMarket = new MarketHours(date, 4, 9.5),
                    Open = new MarketHours(date, 9.5, 16),
                    PostMarket = new MarketHours(date, 16, 20),
                    Status = status
                };
            }
            else
            {
                var marketToday = OpenAllDay(date);
                return marketToday;
            }
        }
    }

    /// <summary>
    /// Market open hours model for pre, normal and post market hour definitions.
    /// </summary>
    public class MarketHours
    {
        /// <summary>
        /// Start time for this market hour category
        /// </summary>
        [JsonProperty(PropertyName = "start")]
        public DateTime Start;

        /// <summary>
        /// End time for this market hour category
        /// </summary>
        [JsonProperty(PropertyName = "end")]
        public DateTime End;

        /// <summary>
        /// Market hours initializer given an hours since midnight measure for the market hours today
        /// </summary>
        /// <param name="referenceDate">Reference date used for as base date from the specified hour offsets</param>
        /// <param name="defaultStart">Time in hours since midnight to start this open period.</param>
        /// <param name="defaultEnd">Time in hours since midnight to end this open period.</param>
        public MarketHours(DateTime referenceDate, double defaultStart, double defaultEnd)
        {
            Start = referenceDate.Date.AddHours(defaultStart);
            End = referenceDate.Date.AddHours(defaultEnd);
            if (defaultEnd == 24)
            {
                // when we mark it as the end of the day other code that relies on .TimeOfDay has issues
                End = End.AddTicks(-1);
            }
        }
    }
}
