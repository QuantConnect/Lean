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
using QuantConnect.Data;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// Represents a grouping of data emitted at a certain time.
    /// </summary>
    public class TimeSlice
    {
        /// <summary>
        /// Gets the time this data was emitted
        /// </summary>
        public DateTime Time { get; private set; }

        /// <summary>
        /// Gets the data in the time slice
        /// </summary>
        /// <remarks>
        /// This is defined as a dictionary to limit changes in the AlgorithmManager,
        /// in the future we may want to redefine this to a simple list or maybe
        /// just an enumerable.
        /// </remarks>
        public Dictionary<int, List<BaseData>> Data { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeSlice"/> class
        /// </summary>
        /// <param name="time">The time the data was emitted</param>
        /// <param name="data">The data in the time slice</param>
        public TimeSlice(DateTime time, Dictionary<int, List<BaseData>> data)
        {
            Time = time;
            Data = data;
        }
    }
}