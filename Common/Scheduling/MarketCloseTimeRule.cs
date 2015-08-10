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
using System.Linq;
using QuantConnect.Securities;

namespace QuantConnect.Scheduling
{
    /// <summary>
    /// Specifies that an event should fire some amount of time relative to market close for the requested security
    /// </summary>
    public class MarketCloseTimeRule : ITimeRule
    {
        private readonly Security _security;
        private readonly TimeSpan _timeBeforeClose;
        private readonly bool _extendedMarketClose;

        /// <summary>
        /// Initializes a new instance of the <see cref="MarketCloseTimeRule"/> class
        /// </summary>
        /// <param name="security">The security whose market close we want an event for</param>
        /// <param name="timeBeforeClose">The time before market close that the event should fire</param>
        /// <param name="extendedMarketClose">True to use extended market close, false to use regular market close</param>
        public MarketCloseTimeRule(Security security, TimeSpan timeBeforeClose, bool extendedMarketClose)
        {
            _security = security;
            _timeBeforeClose = timeBeforeClose;
            _extendedMarketClose = extendedMarketClose;
        }

        /// <summary>
        /// Gets a name for this rule
        /// </summary>
        public string Name
        {
            get
            {
                string type = _extendedMarketClose ? "ExtendedMarketClose" : "MarketClose";
                return string.Format("{0}: {1} min before {2}", _security.Symbol, _timeBeforeClose.TotalMinutes.ToString("0.##"), type);
            }
        }

        /// <summary>
        /// Creates the event times for the specified dates in UTC
        /// </summary>
        /// <param name="dates">The dates to apply times to</param>
        /// <returns>An enumerable of date times that is the result
        /// of applying this rule to the specified dates</returns>
        public IEnumerable<DateTime> CreateUtcEventTimes(IEnumerable<DateTime> dates)
        {
            return from date in dates
                   where _security.Exchange.DateIsOpen(date)
                   let marketClose = _security.Exchange.Hours.GetNextMarketClose(date, _extendedMarketClose)
                   let localEventTime = marketClose - _timeBeforeClose
                   let utcEventTime = localEventTime.ConvertToUtc(_security.Exchange.TimeZone)
                   select utcEventTime;
        }
    }
}