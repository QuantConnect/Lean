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
using NodaTime;
using QuantConnect.Securities;

namespace QuantConnect.Scheduling
{
    /// <summary>
    /// Helper class used to provide better syntax when defining time rules
    /// </summary>
    public class TimeRules
    {
        private DateTimeZone _timeZone;

        private readonly SecurityManager _securities;

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeRules"/> helper class
        /// </summary>
        /// <param name="securities">The security manager</param>
        /// <param name="timeZone">The algorithm's default time zone</param>
        public TimeRules(SecurityManager securities, DateTimeZone timeZone)
        {
            _securities = securities;
            _timeZone = timeZone;
        }

        /// <summary>
        /// Sets the default time zone
        /// </summary>
        /// <param name="timeZone">The time zone to use for helper methods that can't resolve a time zone</param>
        public void SetDefaultTimeZone(DateTimeZone timeZone)
        {
            _timeZone = timeZone;
        }

        /// <summary>
        /// Specifies an event should fire at the specified time of day in the algorithm's time zone
        /// </summary>
        /// <param name="timeOfDay">The time of day in the algorithm's time zone the event should fire</param>
        /// <returns>A time rule that fires at the specified time in the algorithm's time zone</returns>
        public ITimeRule At(TimeSpan timeOfDay)
        {
            return new SpecificTimeTimeRule(_timeZone, timeOfDay);
        }

        /// <summary>
        /// Specifies an event should fire at market open +- <param name="minutesAfterOpen"></param>
        /// </summary>
        /// <param name="symbol">The symbol whose market open we want an event for</param>
        /// <param name="minutesAfterOpen">The time after market open that the event should fire</param>
        /// <param name="extendedMarketOpen">True to use extended market open, false to use regular market open</param>
        /// <returns>A time rule that fires the specified number of minutes after the symbol's market open</returns>
        public ITimeRule MarketOpen(string symbol, double minutesAfterOpen = 0, bool extendedMarketOpen = false)
        {
            symbol = symbol.ToUpper();

            Security security;
            if (!_securities.TryGetValue(symbol, out security))
            {
                throw new Exception(symbol + " not found in portfolio. Request this data when initializing the algorithm.");
            }

            return new MarketOpenTimeRule(security, TimeSpan.FromMinutes(minutesAfterOpen), extendedMarketOpen);
        }

        /// <summary>
        /// Specifies an event should fire at the market close +- <param name="minuteBeforeClose"></param>
        /// </summary>
        /// <param name="symbol">The symbol whose market close we want an event for</param>
        /// <param name="minuteBeforeClose">The time before market close that the event should fire</param>
        /// <param name="extendedMarketClose">True to use extended market close, false to use regular market close</param>
        /// <returns>A time rule that fires the specified number of minutes before the symbol's market close</returns>
        public ITimeRule MarketClose(string symbol, double minuteBeforeClose = 0, bool extendedMarketClose = false)
        {
            symbol = symbol.ToUpper();

            Security security;
            if (!_securities.TryGetValue(symbol, out security))
            {
                throw new Exception(symbol + " not found in portfolio. Request this data when initializing the algorithm.");
            }

            return new MarketCloseTimeRule(security, TimeSpan.FromMinutes(minuteBeforeClose), extendedMarketClose);
        }
    }
}