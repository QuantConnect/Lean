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
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Logging;

namespace QuantConnect.Lean.Engine
{
    /// <summary>
    /// Manages dequeing data from a data feed.
    /// </summary>
    public class DataStream
    {
        private readonly IDataFeed _feed;

        /// <summary>
        /// The frontier time of the data stream
        /// </summary>
        public DateTime AlgorithmTime { get; private set; }

        /// <summary>
        /// Initializes a new <see cref="DataStream"/> for the specified data feed instance
        /// </summary>
        /// <param name="feed">The data feed to be streamed</param>
        public DataStream(IDataFeed feed)
        {
            _feed = feed;
        }

        /// <summary>
        /// Process over the datafeed cross thread bridges to generate an enumerable sorted collection of the data, ready for a consumer
        /// to use and already synchronized in time.
        /// </summary>
        /// <returns>An enumerable that represents all the data coming from the initialized data feed since the start</returns>
        public IEnumerable<Dictionary<int, List<BaseData>>> GetData()
        {
            do
            {
                foreach (var timeSlice in _feed.Bridge.GetConsumingEnumerable())
                {
                    AlgorithmTime = timeSlice.Time;
                    yield return timeSlice.Data;
                }
            } 
            while (!_feed.Bridge.IsCompleted);

            Log.Trace("DataStream.GetData(): All Streams Completed.");
        }
    }
}
