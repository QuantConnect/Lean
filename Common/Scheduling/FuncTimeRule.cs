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

using Python.Runtime;
using System;
using System.Collections.Generic;

namespace QuantConnect.Scheduling
{
    /// <summary>
    /// Uses a function to define a time rule as a projection of date times to date times
    /// </summary>
    public class FuncTimeRule : ITimeRule
    {
        private readonly Func<IEnumerable<DateTime>, IEnumerable<DateTime>> _createUtcEventTimesFunction;

        /// <summary>
        /// Initializes a new instance of the <see cref="FuncTimeRule"/> class
        /// </summary>
        /// <param name="name">The name of the time rule</param>
        /// <param name="createUtcEventTimesFunction">Function used to transform dates into event date times</param>
        public FuncTimeRule(string name, Func<IEnumerable<DateTime>, IEnumerable<DateTime>> createUtcEventTimesFunction)
        {
            Name = name;
            _createUtcEventTimesFunction = createUtcEventTimesFunction;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FuncTimeRule"/> class using a Python function
        /// </summary>
        /// <param name="name">The name of the time rule</param>
        /// <param name="createUtcEventTimesFunction">Function used to transform dates into event date times in Python</param>
        public FuncTimeRule(string name, PyObject createUtcEventTimesFunction)
        {
            Name = name;
            if (!createUtcEventTimesFunction.TryConvertToDelegate(out _createUtcEventTimesFunction))
            {
                throw new ArgumentException("Python TimeRule provided is not a function");
            }
        }

        /// <summary>
        /// Gets a name for this rule
        /// </summary>
        public string Name
        {
            get; private set;
        }

        /// <summary>
        /// Creates the event times for the specified dates in UTC
        /// </summary>
        /// <param name="dates">The dates to apply times to</param>
        /// <returns>An enumerable of date times that is the result
        /// of applying this rule to the specified dates</returns>
        public IEnumerable<DateTime> CreateUtcEventTimes(IEnumerable<DateTime> dates)
        {
            return _createUtcEventTimesFunction(dates);
        }
    }
}