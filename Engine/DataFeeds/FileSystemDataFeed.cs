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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Packets;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// Historical datafeed stream reader for processing files on a local disk.
    /// </summary>
    /// <remarks>Filesystem datafeeds are incredibly fast</remarks>
    public class FileSystemDataFeed : IDataFeed
    {
        // Set types in public area to speed up:
        private IAlgorithm _algorithm;
        private BacktestNodePacket _job;
        private bool _endOfStreams;
        private int _subscriptions;
        private int _bridgeMax = 500000;
        private bool _exitTriggered;

        /// <summary>
        /// List of the subscription the algorithm has requested. Subscriptions contain the type, sourcing information and manage the enumeration of data.
        /// </summary>
        public List<SubscriptionDataConfig> Subscriptions { get; private set; }

        /// <summary>
        /// Prices of the datafeed this instant for dynamically updating security values (and calculation of the total portfolio value in realtime).
        /// </summary>
        /// <remarks>Indexed in order of the subscriptions</remarks>
        public List<decimal> RealtimePrices { get; private set; } 

        /// <summary>
        /// Cross-threading queues so the datafeed pushes data into the queue and the primary algorithm thread reads it out.
        /// </summary>
        public ConcurrentQueue<List<BaseData>>[] Bridge { get; set; }

        /// <summary>
        /// Set the source of the data we're requesting for the type-readers to know where to get data from.
        /// </summary>
        /// <remarks>Live or Backtesting Datafeed</remarks>
        public DataFeedEndpoint DataFeed { get; set; }

        /// <summary>
        /// Flag indicating the hander thread is completely finished and ready to dispose.
        /// </summary>
        public bool IsActive { get; private set; }

        /// <summary>
        /// Flag indicating the file system has loaded all files.
        /// </summary>
        public bool LoadingComplete { get; private set; }

        /// <summary>
        /// Furthest point in time that the data has loaded into the bridges.
        /// </summary>
        public DateTime LoadedDataFrontier { get; private set; }

        /// <summary>
        /// Stream created from the configuration settings.
        /// </summary>
        private SubscriptionDataReader[] SubscriptionReaders { get; set; }

        /// <summary>
        /// Signifying no more data across all bridges
        /// </summary>
        public bool EndOfBridges 
        {
            get 
            {
                for (var i = 0; i < Bridge.Length; i++)
                {
                    if (Bridge[i].Count != 0 || EndOfBridge[i] != true || _endOfStreams != true)
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        /// <summary>
        /// End of Stream for Each Bridge:
        /// </summary>
        public bool[] EndOfBridge { get; set; }

        /// <summary>
        /// Frontiers for each fill forward high water mark
        /// </summary>
        public DateTime[] FillForwardFrontiers;

        /// <summary>
        /// Create a new backtesting data feed.
        /// </summary>
        /// <param name="algorithm">Instance of the algorithm</param>
        /// <param name="job">Algorithm work task</param>
        public FileSystemDataFeed(IAlgorithm algorithm, BacktestNodePacket job)
        {
            Subscriptions = algorithm.SubscriptionManager.Subscriptions;
            _subscriptions = Subscriptions.Count;

            //Public Properties:
            DataFeed = DataFeedEndpoint.FileSystem;
            IsActive = true;
            Bridge = new ConcurrentQueue<List<BaseData>>[_subscriptions];
            EndOfBridge = new bool[_subscriptions];
            SubscriptionReaders = new SubscriptionDataReader[_subscriptions];
            FillForwardFrontiers = new DateTime[_subscriptions];
            RealtimePrices = new List<decimal>(_subscriptions);

            //Class Privates:
            _job = job;
            _algorithm = algorithm;
            _endOfStreams = false;
            _bridgeMax = _bridgeMax / _subscriptions; //Set the bridge maximum count:

            for (var i = 0; i < _subscriptions; i++)
            {
                //Create a new instance in the dictionary:
                Bridge[i] = new ConcurrentQueue<List<BaseData>>();
                EndOfBridge[i] = false;
                SubscriptionReaders[i] = new SubscriptionDataReader(Subscriptions[i], _algorithm.Securities[Subscriptions[i].Symbol], DataFeed, _job.PeriodStart, _job.PeriodFinish);
                FillForwardFrontiers[i] = new DateTime();
            }
        }

        /// <summary>
        /// Main routine for datafeed analysis.
        /// </summary>
        /// <remarks>This is a hot-thread and should be kept extremely lean. Modify with caution.</remarks>
        public void Run()
        {   
            //Calculate the increment based on the subscriptions:
            var tradeBarIncrements = CalculateIncrement(includeTick: false);
            var increment = CalculateIncrement(includeTick: true);

            //Loop over each date in the job
            foreach (var date in Time.EachTradeableDay(_algorithm.Securities, _job.PeriodStart, _job.PeriodFinish))
            {
                //Update the source-URL from the BaseData, reset the frontier to today. Update the source URL once per day.
                // this is really the next frontier in the future
                var frontier = date.Add(increment);
                var activeStreams = _subscriptions;

                //Initialize the feeds to this date:
                for (var i = 0; i < _subscriptions; i++) 
                {
                    //Don't refresh source when we know the market is closed for this security:
                    var success = SubscriptionReaders[i].RefreshSource(date);

                    //If we know the market is closed for security then can declare bridge closed.
                    if (success) {
                        EndOfBridge[i] = false;
                    }
                    else
                    {
                        ProcessMissingFileFillForward(SubscriptionReaders[i], i, tradeBarIncrements, date);
                        EndOfBridge[i] = true;
                    }
                }

                //Pause the DataFeed
                var bridgeFullCount = 1;
                var bridgeZeroCount = 0;
                var active = GetActiveStreams();

                //Pause here while bridges are full, but allow missing files to pass
                int count = 0;
                while (bridgeFullCount > 0 && ((_subscriptions - active) == bridgeZeroCount) && !_exitTriggered)
                {
                    bridgeFullCount = Bridge.Count(bridge => bridge.Count >= _bridgeMax);
                    bridgeZeroCount = Bridge.Count(bridge => bridge.Count == 0);
                    if (count++ > 0)
                    {
                        Thread.Sleep(5);
                    }
                }

                // for each smallest resolution
                var datePlusOneDay = date.Date.AddDays(1);
                while ((frontier.Date == date.Date || frontier.Date == datePlusOneDay) && !_exitTriggered)
                {
                    var cache = new List<BaseData>[_subscriptions];
                    
                    //Reset Loop:
                    long earlyBirdTicks = 0;

                    //Go over all the subscriptions, one by one add a minute of data to the bridge.
                    for (var i = 0; i < _subscriptions; i++)
                    {
                        //Get the reader manager:
                        var manager = SubscriptionReaders[i];

                        //End of the manager stream set flag to end bridge: also if the EOB flag set, from the refresh source method above
                        if (manager.EndOfStream || EndOfBridge[i])
                        {
                            EndOfBridge[i] = true;
                            activeStreams = GetActiveStreams();
                            if (activeStreams == 0)
                            {
                                frontier = frontier.Date + TimeSpan.FromDays(1);
                            }
                            continue;
                        }

                        //Initialize data store:
                        cache[i] = new List<BaseData>(2);

                        //Add the last iteration to the new list: only if it falls into this time category
                        var cacheAtIndex = cache[i];
                        while (manager.Current.EndTime < frontier)
                        {
                            cacheAtIndex.Add(manager.Current);
                            if (!manager.MoveNext()) break;
                        }

                        //Save the next earliest time from the bridges: only if we're not filling forward.
                        if (manager.Current != null)
                        {
                            if (earlyBirdTicks == 0 || manager.Current.EndTime.Ticks < earlyBirdTicks)
                            {
                                earlyBirdTicks = manager.Current.EndTime.Ticks;
                            }
                        }
                    }

                    if (activeStreams == 0)
                    {
                        break;
                    }

                    //Add all the lists to the bridge, release the bridge
                    //we push all the data up to this frontier into the bridge at once
                    for (var i = 0; i < _subscriptions; i++)
                    {
                        if (cache[i] != null && cache[i].Count > 0)
                        {
                            FillForwardFrontiers[i] = cache[i][0].EndTime;
                            Bridge[i].Enqueue(cache[i]);
                        }
                        ProcessFillForward(SubscriptionReaders[i], i, tradeBarIncrements);
                    }

                    //This will let consumers know we have loaded data up to this date
                    //So that the data stream doesn't pull off data from the same time period in different events
                    LoadedDataFrontier = frontier;

                    if (earlyBirdTicks > 0 && earlyBirdTicks > frontier.Ticks) 
                    {
                        //Jump increment to the nearest second, in the future: Round down, add increment
                        frontier = (new DateTime(earlyBirdTicks)).RoundDown(increment) + increment;
                    }
                    else
                    {
                        //Otherwise step one forward.
                        frontier += increment;
                    }

                } // End of This Day.

                if (_exitTriggered) break;

            } // End of All Days:

            Log.Trace(DataFeed + ".Run(): Data Feed Completed.");
            LoadingComplete = true;

            //Make sure all bridges empty before declaring "end of bridge":
            while (!EndOfBridges && !_exitTriggered)
            {
                for (var i = 0; i < _subscriptions; i++)
                {
                    //Nothing left in the bridge, mark it as finished
                    if (Bridge[i].Count == 0)
                    {
                        EndOfBridge[i] = true;
                    }
                }
                if (GetActiveStreams() == 0) _endOfStreams = true;
                Thread.Sleep(100);
            }

            //Close up all streams:
            for (var i = 0; i < Subscriptions.Count; i++)
            {
                SubscriptionReaders[i].Dispose();
            }

            Log.Trace(DataFeed + ".Run(): Ending Thread... ");
            IsActive = false;
        }



        /// <summary>
        /// Send an exit signal to the thread.
        /// </summary>
        public void Exit()
        {
            _exitTriggered = true;
            PurgeData();
        }


        /// <summary>
        /// Loop over all the queues and clear them to fast-quit this thread and return to main.
        /// </summary>
        public void PurgeData()
        {
            foreach (var t in Bridge)
            {
                t.Clear();
            }
        }

        private void ProcessMissingFileFillForward(SubscriptionDataReader manager, int i, TimeSpan increment, DateTime dateToFill)
        {
            // we'll copy the current into the next day
            var subscription = Subscriptions[i];
            if (!subscription.FillDataForward || manager.Current == null) return;

            var start = dateToFill.Date + manager.Exchange.MarketOpen;
            if (subscription.ExtendedMarketHours)
            {
                start = dateToFill.Date + manager.Exchange.ExtendedMarketOpen;
            }

            // shift the 'start' time to the end of the bar by adding the increment, this makes 'date'
            // the end time which also allows the market open functions to behave as expected

            var current = manager.Current;
            for (var endTime = start.Add(increment); endTime.Date == dateToFill.Date; endTime = endTime + increment)
            {
                if (manager.IsMarketOpen(endTime) || (subscription.ExtendedMarketHours && manager.IsExtendedMarketOpen(endTime)))
                {
                    EnqueueFillForwardData(i, current, endTime);
                }
                else
                {
                    // stop fill forwarding when we're no longer open
                    break;
                }
            }
        }

        /// <summary>
        /// If this is a fillforward subscription, look at the previous time, and current time, and add new 
        /// objects to queue until current time to fill up the gaps.
        /// </summary>
        /// <param name="manager">Subscription to process</param>
        /// <param name="i">Subscription position in the bridge ( which queue are we pushing data to )</param>
        /// <param name="increment">Timespan increment to jump the fillforward results</param>
        private void ProcessFillForward(SubscriptionDataReader manager, int i, TimeSpan increment)
        {
            // If previous == null cannot fill forward nothing there to move forward (e.g. cases where file not found on first file).
            if (!Subscriptions[i].FillDataForward || manager.Previous == null || manager.Current == null) return;

            //Last tradebar and the current one we're about to add to queue:
            var previous = manager.Previous;
            var current = manager.Current;

            // final two points of file that ends at midnight, causes issues in the day rollover/fill forward
            if (current.EndTime.TimeOfDay.Ticks == 0 && previous.EndTime == current.Time)
            {
                return;
            }

            //Initialize the frontier:
            if (FillForwardFrontiers[i].Ticks == 0) FillForwardFrontiers[i] = previous.EndTime;

            // using the previous to fill forward since 'current' is ahead the frontier
            var whatToFill = previous;
            // using current.EndTime as fill until because it's the next piece of data we have for this subscription
            var fillUntil = current.EndTime;

            //Data ended before the market closed: premature ending flag - continue filling forward until market close.
            if (manager.EndOfStream && manager.IsMarketOpen(current.EndTime))
            {
                //Make sure we only fill forward to end of *today* -- don't fill forward tomorrow just because its also open
                fillUntil = FillForwardFrontiers[i].Date.AddDays(1);
                // since we ran out of data, use the current as the clone source, it's more recent than previous
                whatToFill = current;
            }

            // loop from our last time (previous.EndTime) to our current.EndTime, filling in all missing day during
            // request market hours
            for (var endTime = FillForwardFrontiers[i] + increment; (endTime < fillUntil); endTime = endTime + increment)
            {
                if (Subscriptions[i].ExtendedMarketHours)
                {
                    if (!manager.IsExtendedMarketOpen(endTime.Subtract(increment)))
                    {
                        //If we've asked for extended hours, and the security is no longer inside extended market hours, skip:
                        continue;
                    }
                }
                else
                {
                    // if the market isn't open skip to the current.EndTime and rewind until the market is open
                    // this is the case where the previous value is from yesterday but we're trying to fill forward
                    // the next day, so instead of zooming through 18 hours of off-market hours, skip to our current data
                    // point and rewind the market open.
                    //
                    // E.g, Current.EndTime = 9:40am and Previous.EndTime = 2:00pm, so fill in from 2->4pm
                    // and then skip to 9:40am, reverse to 9:30am and fill from 9:30->9:40
                    if (!manager.IsMarketOpen(endTime.Subtract(increment)) && Subscriptions[i].Resolution != Resolution.Daily)
                    {
                        // Move fill forward so we don't waste time in this tight loop.
                        endTime = fillUntil;
                        do
                        {
                            endTime = endTime - increment;
                        }
                        // is market open assumes start time of bars, open at 9:30 closed at 4:00
                        // so decrement our date to use the start time
                        while (manager.IsMarketOpen(endTime.Subtract(increment)));
                        continue;
                    }
                }

                // add any overlap condition here
                if (Subscriptions[i].Resolution == Resolution.Daily)
                {
                    // handle fill forward on lower resolutions
                    var barStartTime = endTime - increment;
                    if (manager.Exchange.IsOpenDuringBar(barStartTime, endTime, Subscriptions[i].ExtendedMarketHours))
                    {
                        EnqueueFillForwardData(i, previous, endTime);
                    }
                    // special case catch missing days
                    else if (endTime.TimeOfDay.Ticks == 0 && manager.Exchange.DateIsOpen(endTime.Date.AddDays(-1)))
                    {
                        EnqueueFillForwardData(i, previous, endTime);
                    }
                    continue;
                }

                EnqueueFillForwardData(i, whatToFill, endTime);
            }
        }


        private void EnqueueFillForwardData(int i, BaseData previous, DateTime dataEndTime)
        {
            var cache = new List<BaseData>(1);
            var fillforward = previous.Clone(true);
            fillforward.Time = dataEndTime.Subtract(Subscriptions[i].Increment);
            fillforward.EndTime = dataEndTime;
            FillForwardFrontiers[i] = dataEndTime;
            cache.Add(fillforward);
            Bridge[i].Enqueue(cache);
        }


        /// <summary>
        /// Get the number of active streams still EndOfBridge array.
        /// </summary>
        /// <returns>Count of the number of streams with data</returns>
        private int GetActiveStreams()
        {
            //Get the number of active streams:
            var activeStreams = (from stream in EndOfBridge
                                 where stream == false
                                 select stream).Count();
            return activeStreams;
        }


        /// <summary>
        /// Calculate the minimum increment to scan for data based on the data requested.
        /// </summary>
        /// <param name="includeTick">When true the subscriptions include a tick data source, meaning there is almost no increment.</param>
        /// <returns>Timespan to jump the data source so it efficiently orders the results</returns>
        private TimeSpan CalculateIncrement(bool includeTick)
        {
            var increment = TimeSpan.FromDays(1);
            foreach (var config in Subscriptions)
            {
                switch (config.Resolution)
                {
                    //Hourly TradeBars:
                    case Resolution.Hour:
                        if (increment > TimeSpan.FromHours(1))
                        {
                            increment = TimeSpan.FromHours(1);
                        }
                        break;

                    //Minutely TradeBars:
                    case Resolution.Minute:
                        if (increment > TimeSpan.FromMinutes(1))
                        {
                            increment = TimeSpan.FromMinutes(1);
                        }
                        break;

                    //Secondly Bars:
                    case Resolution.Second:
                        if (increment > TimeSpan.FromSeconds(1))
                        {
                            increment = TimeSpan.FromSeconds(1);
                        }
                        break;

                    //Ticks: No increment; just fire each data piece in as they happen.
                    case Resolution.Tick:
                        if (increment > TimeSpan.FromMilliseconds(1) && includeTick)
                        {
                            increment = new TimeSpan(0, 0, 0, 0, 1);
                        }
                        break;
                }
            }
            return increment;
        }

    } // End FileSystem Local Feed Class:
} // End Namespace
