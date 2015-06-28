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
using System.Threading.Tasks;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Data.Market;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Packets;
using QuantConnect.Securities;
using QuantConnect.Util;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// Live Data Feed Streamed From QC Source.
    /// </summary>
    public class LiveTradingDataFeed : IDataFeed
    {
        private LiveNodePacket _job;
        private List<SubscriptionDataConfig> _subscriptions;
        private readonly List<bool> _isDynamicallyLoadedData = new List<bool>();
        private IEnumerator<BaseData>[] _subscriptionManagers;
        private ConcurrentQueue<List<BaseData>>[] _bridge;
        private bool _endOfBridges;
        private bool _isActive;
        private bool[] _endOfBridge;
        private DataFeedEndpoint _dataFeed;
        private IAlgorithm _algorithm;
        private readonly object _lock = new object();
        private bool _exitTriggered;
        private List<string> _symbols = new List<string>();
        private Dictionary<int, StreamStore> _streamStore = new Dictionary<int, StreamStore>();
        private List<decimal> _realtimePrices;
        private IDataQueueHandler _dataQueue;

        /// <summary>
        /// Cross-threading queue so the datafeed pushes data into the queue and the primary algorithm thread reads it out.
        /// </summary>
        public BlockingCollection<TimeSlice> Bridge
        {
            get; private set;
        }

        /// <summary>
        /// Subscription collection for data requested.
        /// </summary>
        public List<SubscriptionDataConfig> Subscriptions
        {
            get  { return _subscriptions; }
            private set { _subscriptions = value; }
        }

        /// <summary>
        /// Prices of the datafeed this instant for dynamically updating security values (and calculation of the total portfolio value in realtime).
        /// </summary>
        /// <remarks>Indexed in order of the subscriptions</remarks>
        public List<decimal> RealtimePrices 
        {
            get { return _realtimePrices; }
        }

        /// <summary>
        /// Public flag indicator that the thread is still busy.
        /// </summary>
        public bool IsActive
        {
            get { return _isActive; }
            private set { _isActive = value; }
        }

        /// <summary>
        /// Data has completely loaded and we don't expect any more.
        /// </summary>
        public bool LoadingComplete
        {
            get { return false; }
        }

        /// <summary>
        /// Live trading datafeed handler provides a base implementation of a live trading datafeed. Derived types
        /// need only implement the GetNextTicks() function to return unprocessed ticks from a data source.
        /// This creates a new data feed with a DataFeedEndpoint of LiveTrading.
        /// </summary>
        public void Initialize(IAlgorithm algorithm, AlgorithmNodePacket job, IResultHandler resultHandler)
        {
            //Subscription Count:
            _subscriptions = algorithm.SubscriptionManager.Subscriptions;

            Bridge = new BlockingCollection<TimeSlice>();

            //Set Properties:
            _isActive = true;
            _dataFeed = DataFeedEndpoint.LiveTrading;
            _bridge = new ConcurrentQueue<List<BaseData>>[Subscriptions.Count];
            _endOfBridge = new bool[Subscriptions.Count];
            _subscriptionManagers = new SubscriptionDataReader[Subscriptions.Count];
            _realtimePrices = new List<decimal>();

            //Set the source of the live data:
            _dataQueue = Composer.Instance.GetExportedValueByTypeName<IDataQueueHandler>(Configuration.Config.Get("data-queue-handler", "LiveDataQueue"));

            //Class Privates:
            _algorithm = algorithm;
            if (!(job is LiveNodePacket))
            {
                throw new ArgumentException("The LiveTradingDataFeed requires a LiveNodePacket.");
            }

            _job = (LiveNodePacket) job;

            //Setup the arrays:
            for (var i = 0; i < Subscriptions.Count; i++)
            {
                _endOfBridge[i] = false;
                _bridge[i] = new ConcurrentQueue<List<BaseData>>();

                //This is quantconnect data source, store here for speed/ease of access
                var security = algorithm.Securities[_subscriptions[i].Symbol];
                _isDynamicallyLoadedData.Add(security.IsDynamicallyLoadedData);

                // only make readers for custom data, live data will come through data queue handler
                if (_isDynamicallyLoadedData[i])
                {
                    //Subscription managers for downloading user data:
                    // TODO: Update this when warmup comes in, we back up so we can get data that should have emitted at midnight today
                    var periodStart = DateTime.Today.AddDays(-7);
                    var subscriptionDataReader = new SubscriptionDataReader(
                        _subscriptions[i],
                        security,
                        DataFeedEndpoint.LiveTrading,
                        periodStart,
                        DateTime.MaxValue,
                        resultHandler,
                        Time.EachTradeableDay(algorithm.Securities, periodStart, DateTime.MaxValue)
                        );

                    // wrap the subscription data reader with a filter enumerator
                    _subscriptionManagers[i] = SubscriptionFilterEnumerator.WrapForDataFeed(resultHandler, subscriptionDataReader, security, DateTime.MaxValue);
                }

                _realtimePrices.Add(0);
            }

            // request for data from these symbols
            var symbols = BuildTypeSymbolList(algorithm);
            if (symbols.Any())
            {
                // don't subscribe if there's nothing there, this allows custom data to
                // work without an IDataQueueHandler implementation by specifying LiveDataQueue
                // in the configuration, that implementation throws on every method, but we actually
                // don't need it if we're only doing custom data
                _dataQueue.Subscribe(_job, symbols);
            }
        }


        /// <summary>
        /// Execute the primary thread for retrieving stock data.
        /// 1. Subscribe to the streams requested.
        /// 2. Build bars or tick data requested, primary loop increment smallest possible.
        /// </summary>
        public void Run()
        {
            // Symbols requested:
            _symbols = (from security in _algorithm.Securities.Values
                        where !security.IsDynamicallyLoadedData && (security.Type == SecurityType.Equity || security.Type == SecurityType.Forex)
                        select security.Symbol).ToList<string>();

            //Initialize:
            _streamStore = new Dictionary<int, StreamStore>();
            for (var i = 0; i < Subscriptions.Count; i++)
            {
                var config = _subscriptions[i];
                _streamStore.Add(i, new StreamStore(config, _algorithm.Securities[config.Symbol]));
            }
            Log.Trace(string.Format("LiveTradingDataFeed.Stream(): Initialized {0} stream stores.", _streamStore.Count));

            // Set up separate thread to handle stream and building packets:
            var streamThread = new Thread(StreamStoreConsumer);
            streamThread.Start();
            Thread.Sleep(5); // Wait a little for the other thread to init.

            // This thread converts data into bars "on" the second - assuring the bars are close as 
            // possible to a second unit tradebar (starting at 0 milliseconds).
            var realtime = new RealTimeSynchronizedTimer(TimeSpan.FromSeconds(1), triggerTime =>
            {
                // determine if we're on even time boundaries for data emit
                var onMinute = triggerTime.Second == 0;
                var onHour = onMinute && triggerTime.Minute == 0;
                var onDay = onHour && triggerTime.Hour == 0;

                // Determine if this subscription needs to be archived:
                var items = new Dictionary<int, List<BaseData>>();
                for (var i = 0; i < Subscriptions.Count; i++)
                {
                    bool triggerArchive = false;
                    switch (_subscriptions[i].Resolution)
                    {
                        case Resolution.Second:
                            triggerArchive = true;
                            break;
                        case Resolution.Minute:
                            triggerArchive = onMinute;
                            break;
                        case Resolution.Hour:
                            triggerArchive = onHour;
                            break;
                        case Resolution.Daily:
                            triggerArchive = onDay;
                            break;
                    }

                    if (triggerArchive)
                    {
                        _streamStore[i].TriggerArchive(triggerTime, _subscriptions[i].FillDataForward);

                        BaseData data;
                        var dataPoints = new List<BaseData>();
                        while (_streamStore[i].Queue.TryDequeue(out data))
                        {
                            dataPoints.Add(data);
                        }
                        items[i] = dataPoints;
                    }
                }

                Bridge.Add(new TimeSlice(triggerTime, items));
            });

            //Start the realtime sampler above
            realtime.Start();

            while (!_exitTriggered && !_endOfBridges)
            {
                // main work of this class is done in the realtime and stream store consumer threads
                Thread.Sleep(1000);
            }

            //Dispose of the realtime clock.
            realtime.Stop();

            //Stop thread
            _isActive = false;

            //Exit Live DataStream Feed:
            Log.Trace("LiveTradingDataFeed.Run(): Exiting LiveTradingDataFeed Run Method");
        }

        /// <summary>
        /// Stream Store Consumer uses the GetNextTicks() function to get current ticks from a data source and
        /// then uses the stream store to compile them into trade bars.
        /// </summary>
        public void StreamStoreConsumer()
        {
            //Initialize
            var update = new Dictionary<int, DateTime>();

            //Scan for the required time period to stream:
            Log.Trace("LiveTradingDataFeed.Stream(): Waiting for updated market hours...", true);

            //Awake:
            Log.Trace("LiveTradingDataFeed.Stream(): Market open, starting stream for " + string.Join(",", _symbols));

            //Micro-thread for polling for new data from data source:
            var liveThreadTask = new Task(()=> 
            {
                if (_isDynamicallyLoadedData.All(x => x))
                {
                    // if we're all custom data data don't waste CPU cycle with this thread
                    return;
                }

                //Blocking ForEach - Should stay within this loop as long as there is a data-connection
                while (true)
                {
                    var dataCollection = GetNextTicks();

                    int ticksCount = 0;
                    foreach (var point in dataCollection)
                    {
                        ticksCount++;

                        //Get the stream store with this symbol:
                        for (var i = 0; i < Subscriptions.Count; i++)
                        {
                            if (_subscriptions[i].Symbol != point.Symbol) continue;

                            var tick = point as Tick;
                            if (tick != null)
                            {
                                // Update our internal counter
                                _streamStore[i].Update(tick);
                                // Update the realtime price stream value
                                _realtimePrices[i] = point.Value;
                            }
                            else
                            {
                                // reset the start time so it goes in sync with other data
                                point.Time = DateTime.Now.RoundDown(_subscriptions[i].Increment);

                                //If its not a tick, inject directly into bridge for this symbol:
                                //Bridge[i].Enqueue(new List<BaseData> {point});
                                Bridge.Add(new TimeSlice(DateTime.Now, new Dictionary<int, List<BaseData>>
                                {
                                    {i, new List<BaseData> {point}}
                                }));
                            }
                        }
                    }

                    if (_exitTriggered) return;
                    if (ticksCount == 0) Thread.Sleep(5);
                }
            });

            // Micro-thread for custom data/feeds. This only supports polling at this time. todo: Custom data sockets
            var customFeedsTask = new Task(() =>
            {
                // used to help prevent future data from entering the algorithm
                // initial to all true, when we get future data, flip flag to false to prevent move next
                var needsMoveNext = Enumerable.Range(0, Subscriptions.Count).Select(x => true).ToArray();
                while(true)
                {
                    for (var i = 0; i < Subscriptions.Count; i++)
                    {
                        if (_isDynamicallyLoadedData[i])
                        {
                            if (!update.ContainsKey(i)) update.Add(i, new DateTime());

                            if (DateTime.Now > update[i])
                            {
                                //Now Time has passed -> Trigger a refresh,

                                if (needsMoveNext[i])
                                {
                                    // if we didn't emit the previous value it's because it was in
                                    // the future, so don't call MoveNext, just perform the date range
                                    // checks below again
                                    
                                    // in live mode subscription reader will move next but return null for current since nothing is there
                                    _subscriptionManagers[i].MoveNext();
                                    // true success defined as if we got a non-null value
                                    if (_subscriptionManagers[i].Current == null)
                                    {
                                        // we failed to get new data for this guy, try again next second
                                        update[i] = DateTime.Now.Add(Time.OneSecond);
                                        continue;
                                    }
                                }

                                // if we didn't get anything keep going
                                var data = _subscriptionManagers[i].Current;
                                if (data == null)
                                {
                                    // heuristically speaking this should already be true, but no harm in explicitly setting it
                                    needsMoveNext[i] = true;
                                    continue;
                                }

                                // check to see if the data is too far in the past
                                // this is useful when using custom remote files that may stretch far into the past,
                                // so this if block will cause us to fast forward the reader until its recent increment
                                if (data.EndTime < DateTime.Now.Subtract(_subscriptions[i].Increment.Add(Time.OneSecond)))
                                {
                                    // repeat this subscription, we're in the past still
                                    i--;
                                    continue;
                                }

                                // don't emit data in the future
                                if (data.EndTime < DateTime.Now)
                                {
                                    _streamStore[i].Update(data); //Update bar builder.
                                    _realtimePrices[i] = data.Value; //Update realtime price value.
                                    needsMoveNext[i] = true;
                                }
                                else
                                {
                                    // since this data is in the future and we didn't emit it,
                                    // don't call MoveNext again and we'll keep performing time checks
                                    // until its end time has passed and we can emit it into the bridge
                                    needsMoveNext[i] = false;
                                }

                                // REVIEW:: We may want to set update to 'just before' the time, I'm thinking of daily data, so we
                                //          ask for it at midnight, let's assume it's there, did we get it into the stream store
                                //          before we called trigger archive?? I hope so, otherwise the data is always a full day late
                                update[i] = DateTime.Now.Add(_subscriptions[i].Increment).RoundDown(_subscriptions[i].Increment);
                            }
                        }
                    }

                    if (_exitTriggered) return;
                    Thread.Sleep(10);
                }
            });

            //Wait for micro-threads to break before continuing
            liveThreadTask.Start();

            // define what tasks we're going to wait on, we use a task from result in place of the custom task, just in case we never start it
            var tasks = new [] {liveThreadTask, Task.FromResult(1)};

            // if we have any dynamically loaded data, start the custom thread
            if (_isDynamicallyLoadedData.Any(x => x))
            {
                //Start task and set it as the second one we want to monitor:
                customFeedsTask.Start();
                tasks[1] = customFeedsTask;
            }
                
            Task.WaitAll(tasks);

            //Once we're here the tasks have died, signal 
            if (!_exitTriggered) _endOfBridges = true;

            Log.Trace(string.Format("LiveTradingDataFeed.Stream(): Stream Task Completed. Exit Signal: {0}", _exitTriggered));
        }

        /// <summary>
        /// Returns the next ticks from the data source. The data source itself is defined in the derived class's
        /// implementation of this function. For example, if obtaining data from a brokerage, say IB, then the derived
        /// implementation would ask the IB API for the next ticks
        /// </summary>
        /// <returns>The next ticks to be aggregated and sent to algoithm</returns>
        public virtual IEnumerable<BaseData> GetNextTicks()
        {
            return _dataQueue.GetNextTicks();
        }

        /// <summary>
        /// Trigger the live trading datafeed thread to abort and stop looping.
        /// </summary>
        public void Exit()
        {
            lock (_lock)
            {
                // Unsubscribe from these symbols
                var symbols = BuildTypeSymbolList(_algorithm);
                if (symbols.Any())
                {
                    // don't unsubscribe if there's nothing there, this allows custom data to
                    // work with the LiveDataQueue default LEAN implemetation that just throws on every method.
                    _dataQueue.Unsubscribe(_job, symbols);
                }
                _exitTriggered = true;
                
                foreach (var bridge in _bridge)
                {
                    bridge.Clear();
                }
            }
        }

        /// <summary>
        /// Create list of symbols grouped by security type.
        /// </summary>
        private Dictionary<SecurityType, List<string>> BuildTypeSymbolList(IAlgorithm algorithm)
        {
            // create a lookup keyed by SecurityType
            var symbols = new Dictionary<SecurityType, List<string>>();

            // Only subscribe equities and forex symbols
            foreach (var security in algorithm.Securities.Values)
            {
                if (security.Type == SecurityType.Equity || security.Type == SecurityType.Forex)
                {
                    if (!symbols.ContainsKey(security.Type)) symbols.Add(security.Type, new List<string>());
                    symbols[security.Type].Add(security.Symbol);
                }
            }
            return symbols;
        }
    }
}