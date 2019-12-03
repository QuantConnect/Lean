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
using QuantConnect.Scheduling;
using System.Collections.Generic;

namespace QuantConnect.Data.UniverseSelection
{
    /// <summary>
    /// Entity in charge of managing a schedule
    /// </summary>
    public class Schedule
    {
        private IDateRule _dateRule;

        /// <summary>
        /// True if this schedule is set
        /// </summary>
        public bool Initialized => _dateRule != null;

        /// <summary>
        /// Set a <see cref="IDateRule"/> for this schedule
        /// </summary>
        public void On(IDateRule dateRule)
        {
            _dateRule = dateRule;
        }

        /// <summary>
        /// Gets the current schedule for a given start time
        /// </summary>
        public IEnumerable<DateTime> Get(DateTime startTime, DateTime endTime)
        {
            return _dateRule?.GetDates(startTime, endTime) ?? Enumerable.Empty<DateTime>();
        }

        /// <summary>
        /// Creates a new instance holding the same schedule if any
        /// </summary>
        public Schedule Clone()
        {
            return new Schedule { _dateRule = _dateRule };
        }
    }
}
