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
    /// Specifies that an event should fire every day that the requested security trades
    /// </summary>
    public class EveryTradeableDayDateRule : IDateRule
    {
        private readonly Security _security;

        /// <summary>
        /// Initializes a new instance of the <see cref="EveryTradeableDayDateRule"/> class
        /// </summary>
        /// <param name="security">The security whose tradeable dates we want an event for</param>
        public EveryTradeableDayDateRule(Security security)
        {
            _security = security;
        }

        /// <summary>
        /// Gets a name for this rule
        /// </summary>
        public string Name
        {
            get { return _security.Symbol + ": EveryDay"; }
        }

        /// <summary>
        /// Gets the dates produced by this date rule between the specified times
        /// </summary>
        /// <param name="start">The start of the interval to produce dates for</param>
        /// <param name="end">The end of the interval to produce dates for</param>
        /// <returns>All dates in the interval matching this date rule</returns>
        public IEnumerable<DateTime> GetDates(DateTime start, DateTime end)
        {
            return Time.EachTradeableDay(_security, start, end);
        }
    }
}