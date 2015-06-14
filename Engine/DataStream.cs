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

namespace QuantConnect.Lean.Engine
{
    /// <summary>
    /// Data stream class takes a datafeed hander and converts it into a synchronized enumerable data format for looping 
    /// in the primary algorithm thread.
    /// </summary>
    public static class DataStream
    {
        //Count of bridges and subscriptions.
        private static int _subscriptions;

        /// <summary>
        /// The frontier time of the data stream
        /// </summary>
        public static DateTime AlgorithmTime { get; private set; }

        /// <summary>
        /// Process over the datafeed cross thread bridges to generate an enumerable sorted collection of the data, ready for a consumer
        /// to use and already synchronized in time.
        /// </summary>
        /// <param name="feed">DataFeed object</param>
        /// <param name="frontierOrigin">Starting date for the data feed</param>
        /// <returns></returns>
        public static IEnumerable<Dictionary<int, List<BaseData>>> GetData(IDataFeed feed, DateTime frontierOrigin)
        {
            //Initialize:
            _subscriptions = feed.Subscriptions.Count;
            AlgorithmTime = frontierOrigin;
            long algorithmTime = AlgorithmTime.Ticks;
            var frontier = frontierOrigin;
            var nextEmitTime = DateTime.MinValue;
            var periods = feed.Subscriptions.Select(x => x.Resolution.ToTimeSpan()).ToArray();

            //Wait for datafeeds to be ready, wait for first data to arrive:
            while (feed.Bridge.Length != _subscriptions) Thread.Sleep(100);

            // clear data first when in live mode, start with fresh data
            if (Engine.LiveMode)
            {
                feed.PurgeData();
            }

            //Get all data in queues: return as a sorted dictionary:
            while (!feed.EndOfBridges)
            {
                //Reset items which are not fill forward:
                long earlyBirdTicks = 0;
                var newData = new Dictionary<int, List<BaseData>>();

                // spin wait until the feed catches up to our frontier
                WaitForDataOrEndOfBridges(feed, frontier);

                for (var i = 0; i < _subscriptions; i++)
                {
                    //If there's data on the bridge, check to see if it's time to pull it off, if it's in the future
                    // we'll record the time as 'earlyBirdTicks' so we can fast forward the frontier time
                    while (!feed.Bridge[i].IsEmpty)
                    {
                        //Look at first item on list, leave it there until time passes this item.
                        List<BaseData> result;
                        if (!feed.Bridge[i].TryPeek(out result))
                        {
                            // if there's no item skip to the next subscription
                            break;
                        }

                        DateTime endTime = result[0].EndTime;
                        if (result.Count > 0 && endTime > frontier)
                        {
                            // we have at least one item, check to see if its in ahead of the frontier,
                            // if so, keep track of how many ticks in the future it is
                            if (earlyBirdTicks == 0 || earlyBirdTicks > endTime.Ticks)
                            {
                                earlyBirdTicks = endTime.Ticks;
                            }
                            break;
                        }
                        if (result.Count > 0)
                        {
                            // we have at least one item, check to see if its in ahead of the frontier,
                            // if so, keep track of how many ticks in the future it is
                            if (earlyBirdTicks == 0 || earlyBirdTicks > endTime.Ticks)
                            {
                                earlyBirdTicks = endTime.Ticks;
                            }
                        }

                        //Pull a grouped time list out of the bridge
                        List<BaseData> dataPoints;
                        if (feed.Bridge[i].TryDequeue(out dataPoints))
                        {
                            // round the time down based on the requested resolution for fill forward data
                            // this is a temporary fix, long term fill forward logic should be moved into this class
                            foreach (var point in dataPoints)
                            {
                                if (algorithmTime < point.EndTime.Ticks)
                                {
                                    // set this to most advanced end point in time, pre rounding
                                    // min  10:02  10:02:01(FF) 10:02:01.01(FF)
                                    // sec  10:02  10:02:01     10:02:01.01(FF)
                                    // tic  10:02  10:02:01     10:02:01.01
                                    // the algorithm time should always be the 'frontier' the furthest
                                    // time within this data slice
                                    algorithmTime = point.EndTime.Ticks;
                                }
                                if (point.IsFillForward)
                                {
                                    point.Time = point.Time.RoundDown(periods[i]);
                                }
                            }
                            // add the list to the collection to be yielded
                            List<BaseData> dp;
                            if (!newData.TryGetValue(i, out dp))
                            {
                                dp = new List<BaseData>();
                                newData[i] = dp;
                            }
                            dp.AddRange(dataPoints);
                        }
                        else
                        {
                            //Should never fail:
                            Log.Error("DataStream.GetData(): Failed to dequeue bridge item");
                        }
                    }
                }

                if (newData.Count > 0)
                {
                    AlgorithmTime = new DateTime(algorithmTime);
                    yield return newData;
                }

                //Update the frontier and start again.
                if (earlyBirdTicks > 0)
                {
                    //Seek forward in time to next data event from stream: there's nothing here for us to do now: why loop over empty seconds
                    frontier = new DateTime(earlyBirdTicks);
                }
                else if (feed.EndOfBridges)
                {
                    // we're out of data or quit
                    break;
                }

                //Allow loop pass through emits every second to allow event handling (liquidate/stop/ect...)
                if (Engine.LiveMode && DateTime.Now > nextEmitTime)
                {
                    AlgorithmTime = DateTime.Now.RoundDown(periods.Min());
                    nextEmitTime = DateTime.Now + TimeSpan.FromSeconds(1);
                    yield return new Dictionary<int, List<BaseData>>();
                }
            }
            Log.Trace("DataStream.GetData(): All Streams Completed.");
        }

        /// <summary>
        /// Waits until the data feed is ready for the data stream to pull data from it.
        /// </summary>
        /// <param name="feed">The IDataFeed instance populating the bridges</param>
        /// <param name="dataStreamFrontier">The frontier of the data stream</param>
        private static void WaitForDataOrEndOfBridges(IDataFeed feed, DateTime dataStreamFrontier)
        {
            //Make sure all bridges have data to to peek sync properly.

            // timeout to prevent infinite looping here -- 50ms for live and 30sec for non-live
            var timeout = DateTime.UtcNow + (Engine.LiveMode ? new TimeSpan(0, 0, 0, 0, 50) : new TimeSpan(0, 0, 30));

            if (Engine.LiveMode)
            {
                // give some time to the other threads in live mode
                Thread.Sleep(1);
            }

            //Waiting for data in the bridges:
            while (!AllBridgesHaveData(feed) && DateTime.UtcNow < timeout)
            {
                Thread.Sleep(1);
            }

            //we want to verify that our data stream is never ahead of our data feed.
            //this acts as a virtual lock around the bridge so we can wait for the feed
            //to be ahead of us
            // if we're out of data then the feed will never update (it will stay here forever if there's no more data, so use a timeout!!)
            while (dataStreamFrontier > feed.LoadedDataFrontier && (!feed.EndOfBridges && !feed.LoadingComplete) && DateTime.UtcNow < timeout)
            {
                Thread.Sleep(1);
            }
        }

        /// <summary>
        /// Check if all the bridges have data or are dead before starting the analysis
        /// 
        /// This determines whether or not the data stream can pull data from the data feed.
        /// </summary>
        /// <param name="feed">Feed Interface with concurrent connection between producer and consumer</param>
        /// <returns>Boolean true more data to download</returns>
        private static bool AllBridgesHaveData(IDataFeed feed)
        {
            //Lock on the bridge to scan if it has data:
            for (var i = 0; i < _subscriptions; i++)
            {
                if (feed.EndOfBridge[i]) continue;
                if (feed.Bridge[i].IsEmpty)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Resets the frontier time to DateTime.MinValue
        /// </summary>
        public static void ResetFrontier()
        {
            AlgorithmTime = new DateTime();
        }
    }
}
