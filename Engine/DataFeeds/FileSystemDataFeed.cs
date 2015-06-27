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
using QuantConnect.Lean.Engine.Results;
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
        private IAlgorithm _algorithm;
        private int _subscriptions;
        private int _bridgeMax = 500000;
        private bool _exitTriggered;
        private bool[] _endOfBridge;

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
        /// Flag indicating the hander thread is completely finished and ready to dispose.
        /// </summary>
        public bool IsActive { get; private set; }

        /// <summary>
        /// Flag indicating the file system has loaded all files.
        /// </summary>
        public bool LoadingComplete { get; private set; }

        /// <summary>
        /// Stream created from the configuration settings.
        /// </summary>
        private IEnumerator<BaseData>[] SubscriptionReaders { get; set; }

        /// <summary>
        /// Cross-threading queue so the datafeed pushes data into the queue and the primary algorithm thread reads it out.
        /// </summary>
        public ConcurrentQueue<TimeSlice> Data
        {
            get; private set;
        } 

        /// <summary>
        /// Frontiers for each fill forward high water mark
        /// </summary>
        public DateTime[] FillForwardFrontiers;

        public void Initialize(IAlgorithm algorithm, AlgorithmNodePacket job, IResultHandler resultHandler)
        {
            Data =  new ConcurrentQueue<TimeSlice>();

            Subscriptions = algorithm.SubscriptionManager.Subscriptions;
            _subscriptions = Subscriptions.Count;

            //Public Properties:
            IsActive = true;
            _endOfBridge = new bool[_subscriptions];
            SubscriptionReaders = new IEnumerator<BaseData>[_subscriptions];
            FillForwardFrontiers = new DateTime[_subscriptions];
            RealtimePrices = new List<decimal>(_subscriptions);

            //Class Privates:
            _algorithm = algorithm;
            _bridgeMax = _bridgeMax / _subscriptions; //Set the bridge maximum count:

            // find the minimum resolution, ignoring ticks
            var fillForwardResolution = Subscriptions
                .Where(x => x.Resolution != Resolution.Tick)
                .Select(x => x.Resolution.ToTimeSpan())
                .DefaultIfEmpty(TimeSpan.FromSeconds(1))
                .Min();

            for (var i = 0; i < _subscriptions; i++)
            {
                _endOfBridge[i] = false;
                var security = _algorithm.Securities[Subscriptions[i].Symbol];

                SubscriptionDataConfig config = Subscriptions[i];
                DateTime start = algorithm.StartDate;
                DateTime end = algorithm.EndDate;

                var tradeableDates = Time.EachTradeableDay(security, start.Date, end.Date);
                IEnumerator<BaseData> enumerator = new SubscriptionDataReader(config, security, DataFeedEndpoint.FileSystem, start, end, resultHandler, tradeableDates);

                // optionally apply fill forward logic, but never for tick data
                if (config.FillDataForward && config.Resolution != Resolution.Tick)
                {
                    enumerator = new FillForwardEnumerator(enumerator, security.Exchange, fillForwardResolution, security.IsExtendedMarketHours, end, config.Resolution.ToTimeSpan());
                }

                // finally apply exchange/user filters
                SubscriptionReaders[i] = new SubscriptionFilterEnumerator(enumerator, security, end);
                FillForwardFrontiers[i] = new DateTime();

                // prime the pump for iteration in Run
                _endOfBridge[i] = !SubscriptionReaders[i].MoveNext();
            }
        }

        /// <summary>
        /// Main routine for datafeed analysis.
        /// </summary>
        /// <remarks>This is a hot-thread and should be kept extremely lean. Modify with caution.</remarks>
        public void Run()
        {
            var frontier = SubscriptionReaders
                .Where(x => x.Current != null)
                .Select(x => x.Current.EndTime)
                .DefaultIfEmpty(DateTime.MinValue)
                .Min();

            var maxTimeSliceCount = 500000/_subscriptions;

            // continue to loop over each subscription, enqueuing data in time order
            while (!_exitTriggered)
            {
                var data = new Dictionary<int, List<BaseData>>();
                var earlyBirdTicks = long.MaxValue;
                for (int i = 0; i < _subscriptions; i++)
                {
                    if (_endOfBridge[i])
                    {
                        // skip subscriptions that are finished
                        continue;
                    }

                    var cache = new List<BaseData>();
                    data[i] = cache;

                    var enumerator = SubscriptionReaders[i];
                    while (enumerator.Current.EndTime <= frontier)
                    {
                        // we want bars rounded using their subscription times, we make a clone
                        // so we don't interfere with the enumerator's internal logic
                        var clone = enumerator.Current.Clone(enumerator.Current.IsFillForward);
                        clone.Time = clone.Time.RoundDown(Subscriptions[i].Increment);
                        cache.Add(clone);
                        if (!enumerator.MoveNext())
                        {
                            _endOfBridge[i] = true;
                            break;
                        }
                    }

                    // next data point time
                    earlyBirdTicks = Math.Min(enumerator.Current.EndTime.Ticks, earlyBirdTicks);
                }

                // there's no more data to pull off, we're done
                if (earlyBirdTicks == long.MaxValue)
                {
                    break;
                }

                var newFrontier = new DateTime(earlyBirdTicks);
                if (newFrontier.Date != frontier.Date)
                {
                    // yield while we have plenty of data (single core machines, you're welcome.)
                    while (Data.Count > maxTimeSliceCount && !_exitTriggered)
                    {
                        Thread.Sleep(0);
                    }
                }

                // enqueue our next time slice and set the frontier for the next
                Data.Enqueue(new TimeSlice(frontier, data));
                frontier = newFrontier;
            }

            Log.Trace("FileSystemDataFeed.Run(): Data Feed Completed.");
            LoadingComplete = true;

            //Close up all streams:
            for (var i = 0; i < Subscriptions.Count; i++)
            {
                SubscriptionReaders[i].Dispose();
            }

            Log.Trace("FileSystemDataFeed.Run(): Ending Thread... ");
            IsActive = false;
        }

        /// <summary>
        /// Send an exit signal to the thread.
        /// </summary>
        public void Exit()
        {
            _exitTriggered = true;
            Data.Clear();
        }
    }
}
