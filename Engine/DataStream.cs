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
using System.Threading;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Logging;
using QuantConnect.Util;

namespace QuantConnect.Lean.Engine
{
    /// <summary>
    /// Data stream class takes a datafeed hander and converts it into a synchronized enumerable data format for looping 
    /// in the primary algorithm thread.
    /// </summary>
    public class DataStream
    {
        private readonly IDataFeed _feed;

        //Count of bridges and subscriptions.
        private readonly bool _liveMode;

        /// <summary>
        /// The frontier time of the data stream
        /// </summary>
        public DateTime AlgorithmTime { get; private set; }

        /// <summary>
        /// Initializes a new <see cref="DataStream"/> for the specified data feed instance
        /// </summary>
        /// <param name="feed">The data feed to be streamed</param>
        /// <param name="liveMode"></param>
        public DataStream(IDataFeed feed, bool liveMode)
        {
            _feed = feed;
            _liveMode = liveMode;
        }

        /// <summary>
        /// Process over the datafeed cross thread bridges to generate an enumerable sorted collection of the data, ready for a consumer
        /// to use and already synchronized in time.
        /// </summary>
        /// <param name="frontierOrigin">Starting date for the data feed</param>
        /// <returns></returns>
        public IEnumerable<Dictionary<int, List<BaseData>>> GetData(DateTime frontierOrigin)
        {
            //Initialize:
            AlgorithmTime = frontierOrigin;
            var nextEmitTime = DateTime.UtcNow + Time.OneSecond;

            int count = 0;
            while (!_feed.LoadingComplete)
            {
                TimeSlice timeSlice;
                while (_feed.Data.TryDequeue(out timeSlice))
                {
                    count ++;
                    AlgorithmTime = timeSlice.Time;
                    yield return timeSlice.Data;
                }

                if (_liveMode && DateTime.UtcNow > nextEmitTime)
                {
                    AlgorithmTime = DateTime.Now;
                    nextEmitTime = DateTime.UtcNow + Time.OneSecond;
                    yield return new Dictionary<int, List<BaseData>>();
                }
            }
            Log.Trace(string.Format("Data Stream Count: {0} algo time: {1} feed complete: {2} bridge count: {3}", count, AlgorithmTime, _feed.LoadingComplete, _feed.Data.Count));
            Log.Trace("DataStream.GetData(): All Streams Completed.");
        }
    }
}
