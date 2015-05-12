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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Data.Market;
using QuantConnect.Packets;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /******************************************************** 
    * CLASS DEFINITIONS
    *********************************************************/
    /// <summary>
    /// Live Data Feed Streamed From QC Source.
    /// </summary>
    public class LiveTradingDataFeed : IDataFeed
    {
        /******************************************************** 
        * CLASS VARIABLES
        *********************************************************/
        private LiveNodePacket _job;
        private List<SubscriptionDataConfig> _subscriptions = new List<SubscriptionDataConfig>();
        private List<bool> _isDynamicallyLoadedData = new List<bool>();
        private SubscriptionDataReader[] _subscriptionManagers;
        private ConcurrentQueue<List<BaseData>>[] _bridge;
        private bool _endOfBridges = false;
        private bool _isActive = true;
        private bool[] _endOfBridge = new bool[1];
        private DataFeedEndpoint _dataFeed = DataFeedEndpoint.LiveTrading;
        private IAlgorithm _algorithm;
        private object _lock = new Object();
        private bool _exitTriggered = false;
        private List<string> _symbols = new List<string>();
        private Dictionary<int, StreamStore> _streamStore = new Dictionary<int, StreamStore>();
        private List<decimal> _realtimePrices;
        private IDataQueueHandler _dataQueue;

        /******************************************************** 
        * CLASS PROPERTIES
        *********************************************************/
        /// <summary>
        /// Subscription collection for data requested.
        /// </summary>
        public List<SubscriptionDataConfig> Subscriptions
        {
            get  { return _subscriptions; }
            set { _subscriptions = value; }
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
        /// Manager for the subscription data classes.
        /// </summary>
        public SubscriptionDataReader[] SubscriptionReaderManagers
        {
            get { return _subscriptionManagers; }
            set { _subscriptionManagers = value; }
        }

        /// <summary>
        /// Cross thread bridge queues to pass the data from data-feed to primary algorithm thread.
        /// </summary>
        public ConcurrentQueue<List<BaseData>>[] Bridge
        {
            get { return _bridge; }
            set { _bridge = value; }
        }

        /// <summary>
        /// Boolean flag indicating there is no more data in any of our subscriptions.
        /// </summary>
        public bool EndOfBridges
        {
            get { return _endOfBridges; }
            set { _endOfBridges = value; }
        }

        /// <summary>
        /// Array of boolean flags indicating the data status for each queue/subscription we're tracking.
        /// </summary>
        public bool[] EndOfBridge
        {
            get { return _endOfBridge; }
            set { _endOfBridge = value; }
        }

        /// <summary>
        /// Set the source of the data we're requesting for the type-readers to know where to get data from.
        /// </summary>
        /// <remarks>Live or Backtesting Datafeed</remarks>
        public DataFeedEndpoint DataFeed
        {
            get { return _dataFeed; }
            set { _dataFeed = value; }
        }

        /// <summary>
        /// Public flag indicator that the thread is still busy.
        /// </summary>
        public bool IsActive
        {
            get { return _isActive; }
            set { _isActive = value; }
        }

        /// <summary>
        /// Data has completely loaded and we don't expect any more.
        /// </summary>
        public bool LoadingComplete
        {
            get { return false; }
        }

        /// <summary>
        /// The most advanced moment in time for which the data feed has completed loading data
        /// </summary>
        public DateTime LoadedDataFrontier { get; private set; }

        /******************************************************** 
        * CLASS CONSTRUCTOR
        *********************************************************/

        /// <summary>
        /// Live trading datafeed handler provides a base implementation of a live trading datafeed. Derived types
        /// need only implement the GetNextTicks() function to return unprocessed ticks from a data source.
        /// This creates a new data feed with a DataFeedEndpoint of LiveTrading.
        /// </summary>
        public LiveTradingDataFeed(IAlgorithm algorithm, LiveNodePacket job, IDataQueueHandler dataSource)
        {
            //Subscription Count:
            _subscriptions = algorithm.SubscriptionManager.Subscriptions;

            //Set Properties:
            _isActive = true;
            _dataFeed = DataFeedEndpoint.LiveTrading;
            _bridge = new ConcurrentQueue<List<BaseData>>[Subscriptions.Count];
            _endOfBridge = new bool[Subscriptions.Count];
            _subscriptionManagers = new SubscriptionDataReader[Subscriptions.Count];
            _realtimePrices = new List<decimal>();

            //Set the source of the live data:
            _dataQueue = dataSource;

            //Class Privates:
            _algorithm = algorithm;
            _job = job;

            //Setup the arrays:
            for (var i = 0; i < Subscriptions.Count; i++)
            {
                _endOfBridge[i] = false;
                _bridge[i] = new ConcurrentQueue<List<BaseData>>();

                //This is quantconnect data source, store here for speed/ease of access
                _isDynamicallyLoadedData.Add(algorithm.Securities[_subscriptions[i].Symbol].IsDynamicallyLoadedData);

                //Subscription managers for downloading user data:
                _subscriptionManagers[i] = new SubscriptionDataReader(_subscriptions[i], algorithm.Securities[_subscriptions[i].Symbol], DataFeedEndpoint.LiveTrading, DateTime.MinValue, DateTime.MaxValue);

                //Set up the source file for today:
                _subscriptionManagers[i].RefreshSource(DateTime.Now.Date);

                _realtimePrices.Add(0);
            }

            // request for data from these symbols
            _dataQueue.Subscribe(job, BuildTypeSymbolList(algorithm));
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
                _streamStore.Add(i, new StreamStore(config));
            }
            Log.Trace(string.Format("LiveTradingDataFeed.Stream(): Initialized {0} stream stores.", _streamStore.Count));

            // Set up separate thread to handle stream and building packets:
            var streamThread = new Thread(StreamStoreConsumer);
            streamThread.Start();
            Thread.Sleep(5); // Wait a little for the other thread to init.

            var sourceDate = DateTime.Now.Date;
            var resumeRun = new ManualResetEvent(true);

            // This thread converts data into bars "on" the second - assuring the bars are close as 
            // possible to a second unit tradebar (starting at 0 milliseconds).
            var realtime = new RealTimeSynchronizedTimer(TimeSpan.FromSeconds(1), () =>
            {
                //Pause bridge queing operations:
                resumeRun.Reset();

                var now = DateTime.Now;
                var onMinute = (now.Second == 0);

                // Determine if this subscription needs to be archived:
                for (var i = 0; i < Subscriptions.Count; i++)
                {
                    //Do critical events every second regardless of the market/hybernate state:
                    if (now.Date != sourceDate)
                    {
                        //Every day refresh the source file for the custom user data:
                        _subscriptionManagers[i].RefreshSource(now.Date);
                        sourceDate = now.Date;
                    }

                    switch (_subscriptions[i].Resolution)
                    { 
                        //This is a second resolution data source:
                        case Resolution.Second:
                            //Enqueue our live data:
                            _streamStore[i].TriggerArchive(_subscriptions[i].FillDataForward);
                            break;

                        //This is a minute resolution data source:
                        case Resolution.Minute:
                            if (onMinute)
                            {
                                _streamStore[i].TriggerArchive(_subscriptions[i].FillDataForward);
                            }
                            break;
                    }
                }
                //Resume bridge queing operations:
                resumeRun.Set();
            });

            //Start the realtime sampler above
            realtime.Start();

            while (!_exitTriggered && !_endOfBridges)
            {
                resumeRun.WaitOne();
                
                try
                {
                    //Scan the Stream Store Queue's and if there are any shuffle them over to the bridge for synchronization:
                    DateTime? last = null;
                    for (var i = 0; i < Subscriptions.Count; i++)
                    {
                        BaseData data;
                        while (_streamStore[i].Queue.TryDequeue(out data))
                        {
                            last = data.Time;
                            Bridge[i].Enqueue(new List<BaseData> { data });
                        }
                    }

                    // if we dequeued someone, update frontier for live data sync on bridge
                    if (last.HasValue)
                    {
                        LoadedDataFrontier = last.Value;
                    }
                }
                catch (Exception err)
                {
                    Log.Error("LiveTradingDataFeed.Run(): " + err.Message);
                }

                Thread.Sleep(1);
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
                //Blocking ForEach - Should stay within this loop as long as there is a data-connection
                while (true)
                {
                    var ticks = GetNextTicks();

                    int ticksCount = 0;
                    foreach (var tick in ticks)
                    {
                        ticksCount++;
                        //Get the stream store with this symbol:
                        for (var i = 0; i < Subscriptions.Count; i++)
                        {
                            if (_subscriptions[i].Symbol == tick.Symbol)
                            {
                                // Update our internal counter
                                _streamStore[i].Update(tick);
                                // Update the realtime price stream value
                                _realtimePrices[i] = tick.Value;
                            }
                        }
                    }

                    if (_exitTriggered) return;
                    if (ticksCount == 0) Thread.Sleep(5);
                }
            });

            // Micro-thread for custom data/feeds. This onl supports polling at this time. todo: Custom data sockets
            var customFeedsTask = new Task(() =>
            {
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
                                if (!_subscriptionManagers[i].EndOfStream)
                                {
                                    //Attempt 10 times to download the updated data:
                                    var attempts = 0;
                                    var feedSuccess = false;
                                    do 
                                    {
                                        feedSuccess = _subscriptionManagers[i].MoveNext();
                                        if (!feedSuccess) Thread.Sleep(1000);   //Network issues may cause download to fail. Sleep a little to make it more robust.
                                    }
                                    while (!feedSuccess && attempts++ < 10);

                                    if (!feedSuccess)
                                    {
                                        _subscriptionManagers[i].EndOfStream = true;
                                        continue;
                                    }

                                    //Use the latest data, push it into the store:
                                    var data = _subscriptionManagers[i].Current;
                                    if (data != null)
                                    {
                                        _streamStore[i].Update(data);       //Update bar builder.
                                        _realtimePrices[i] = data.Value;    //Update realtime price value.
                                    }
                                }
                                update[i] = DateTime.Now.Add(_subscriptions[i].Increment);
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
            var tasks = new Task[2] {liveThreadTask, Task.FromResult(1)};

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
        public virtual IEnumerable<Tick> GetNextTicks()
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
                _dataQueue.Unsubscribe(_job, BuildTypeSymbolList(_algorithm));
                _exitTriggered = true;
                PurgeData();
            }
        }

        /// <summary>
        /// Clear any remaining data from the queues
        /// </summary>
        public void PurgeData()
        {
            for (var i = 0; i < _bridge.Length; i++)
            {
                _bridge[i].Clear();
            }
        }


        /// <summary>
        /// Return true when at least one security is open.
        /// </summary>
        /// <returns>Boolean flag true when there is a market asset open.</returns>
        public bool AnySecurityOpen()
        {
            var open = false;
            foreach (var security in _algorithm.Securities.Values)
            {
                if (DateTime.Now.TimeOfDay > security.Exchange.MarketOpen && DateTime.Now.TimeOfDay < security.Exchange.MarketClose)
                {
                    open = true;
                    break;
                }
            }
            return open;
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

    } // End Live Trading Data Feed Class:

} // End Namespace
