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
    /// Uses a function to define an enumerable of dates over a requested start/end period
    /// </summary>
    public class FuncDateRule : IDateRule
    {
        private readonly Func<DateTime, DateTime, IEnumerable<DateTime>> _getDatesFunction;

        /// <summary>
        /// Initializes a new instance of the <see cref="FuncDateRule"/> class
        /// </summary>
        /// <param name="name">The name of this rule</param>
        /// <param name="getDatesFunction">The time applicator function</param>
        public FuncDateRule(string name, Func<DateTime, DateTime, IEnumerable<DateTime>> getDatesFunction)
        {
            Name = name;
            _getDatesFunction = getDatesFunction;
        }

        /// <summary>
        /// Gets a name for this rule
        /// </summary>
        public string Name
        {
            get; private set;
        }

        /// <summary>
        /// Gets the dates produced by this date rule between the specified times
        /// </summary>
        /// <param name="start">The start of the interval to produce dates for</param>
        /// <param name="end">The end of the interval to produce dates for</param>
        /// <returns>All dates in the interval matching this date rule</returns>
        public IEnumerable<DateTime> GetDates(DateTime start, DateTime end)
        {
            return _getDatesFunction(start, end);
        }
    }
}