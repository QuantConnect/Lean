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

namespace QuantConnect.Scheduling
{
    /// <summary>
    /// Specifies times times on dates for events, used in conjunction with <see cref="IDateRule"/>
    /// </summary>
    public interface ITimeRule
    {
        /// <summary>
        /// Gets a name for this rule
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Creates the event times for the specified dates in UTC
        /// </summary>
        /// <param name="dates">The dates to apply times to</param>
        /// <returns>An enumerable of date times that is the result
        /// of applying this rule to the specified dates</returns>
        IEnumerable<DateTime> CreateUtcEventTimes(IEnumerable<DateTime> dates);
    }
}