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
/**********************************************************
* USING NAMESPACES
**********************************************************/
using System;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using QuantConnect.Data;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Logging;


namespace QuantConnect.Lean.Engine
{
    /********************************************************
    * QUANTCONNECT NAMESPACES
    *********************************************************/
    /// <summary>
    /// Data stream class takes a datafeed hander and converts it into a synchronized enumerable data format for looping 
    /// in the primary algorithm thread.
    /// </summary>
    public static class DataStream
    {
        /********************************************************
        * CLASS VARIABLES
        *********************************************************/
        //Count of bridges and subscriptions.
        private static int _subscriptions = 0;

        /********************************************************
        * CLASS PROPERTIES
        *********************************************************/
        
        /********************************************************
        * CLASS METHODS
        *********************************************************/
        /// <summary>
        /// Process over the datafeed cross thread bridges to generate an enumerable sorted collection of the data, ready for a consumer
        /// to use and already synchronized in time.
        /// </summary>
        /// <param name="feed">DataFeed object</param>
        /// <param name="frontierOrigin">Starting date for the data feed</param>
        /// <returns></returns>
        public static IEnumerable<SortedDictionary<DateTime, Dictionary<int, List<BaseData>>>> GetData(IDataFeed feed, DateTime frontierOrigin)
        {
            //Initialize:
            long earlyBirdTicks = 0;
            var increment = TimeSpan.FromSeconds(1);
            _subscriptions = feed.Subscriptions.Count;
            var frontier = frontierOrigin;
            var nextEmitTime = DateTime.MinValue;

            //Wait for datafeeds to be ready, wait for first data to arrive:
            while (feed.Bridge.Length != _subscriptions) Thread.Sleep(100);

            //Get all data in queues: return as a sorted dictionary:
            while (!feed.EndOfBridges)
            {
                //Reset items which are not fill forward:
                earlyBirdTicks = 0; 
                var newData = new SortedDictionary<DateTime, Dictionary<int, List<BaseData>>>();

                // spin wait until the feed catches up to our frontier
                WaitForDataOrEndOfBridges(feed, frontier);

                for (var i = 0; i < _subscriptions; i++)
                {
                    //If there's data, download a little of it: Put 100 items of each type into the queue maximum
                    while (feed.Bridge[i].Count > 0)
                    {
                        //Log.Debug("DataStream.GetData(): Bridge has data: Bridge Count:" + feed.Bridge[i].Count.ToString());

                        //Look at first item on list, leave it there until time passes this item.
                        List<BaseData> result;
                        if (!feed.Bridge[i].TryPeek(out result) || (result.Count > 0 && result[0].Time > frontier))
                        {
                            if (result != null)
                            {
                                //Log.Debug("DataStream.GetData(): Result != null: " + result[0].Time.ToShortTimeString());
                                if (earlyBirdTicks == 0 || earlyBirdTicks > result[0].Time.Ticks) earlyBirdTicks = result[0].Time.Ticks;
                            }
                            break;
                        }

                        //Pull a grouped time list out of the bridge
                        List<BaseData> dataPoints;
                        if (feed.Bridge[i].TryDequeue(out dataPoints))
                        {
                            //Log.Debug("DataStream.GetData(): Bridge has data: DataPoints Count: " + dataPoints.Count);
                            foreach (var point in dataPoints)
                            {
                                //Add the new data point to the list of generic points in this timestamp.
                                if (!newData.ContainsKey(point.Time)) newData.Add(point.Time, new Dictionary<int, List<BaseData>>());
                                if (!newData[point.Time].ContainsKey(i)) newData[point.Time].Add(i, new List<BaseData>());
                                //Add the data point:
                                newData[point.Time][i].Add(point);
                                //Log.Debug("DataStream.GetData(): Added Datapoint: Time:" + point.Time.ToShortTimeString() + " Symbol: " + point.Symbol);
                            }
                        }
                        else 
                        {
                            //Should never fail:
                            Log.Error("DataStream.GetData(): Failed to dequeue bridge item");
                        }
                    }
                }
                
                //Update the frontier and start again.
                if (earlyBirdTicks > 0)
                {
                    //Seek forward in time to next data event from stream: there's nothing here for us to do now: why loop over empty seconds?
                    frontier = (new DateTime(earlyBirdTicks));
                }
                else
                {
                    frontier += increment;
                }

                //Submit the next data array, even if there's no data, allow emits every second to allow event handling (liquidate/stop/ect...)
                if (newData.Count > 0 || (Engine.LiveMode && DateTime.Now > nextEmitTime))
                {
                    nextEmitTime = DateTime.Now + TimeSpan.FromSeconds(1);
                    yield return newData;
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
            var now = Stopwatch.StartNew();

            // timeout to prevent infinite looping here -- 2sec for live and 30sec for non-live
            var loopTimeout = (Engine.LiveMode) ? 50 : 30000;

            if (Engine.LiveMode)
            {
                // give some time to the other threads in live mode
                Thread.Sleep(1);
            }

            //Waiting for data in the bridges:
            while (!AllBridgesHaveData(feed) && now.ElapsedMilliseconds < loopTimeout)
            {
                Thread.Sleep(1);
            }

            //we want to verify that our data stream is never ahead of our data feed.
            //this acts as a virtual lock around the bridge so we can wait for the feed
            //to be ahead of us
            // if we're out of data then the feed will never update (it will stay here forever if there's no more data, so use a timeout!!)
            while (dataStreamFrontier > feed.LoadedDataFrontier && (!feed.EndOfBridges && !feed.LoadingComplete) && now.ElapsedMilliseconds < loopTimeout)
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
                if (feed.Bridge[i].Count == 0)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
