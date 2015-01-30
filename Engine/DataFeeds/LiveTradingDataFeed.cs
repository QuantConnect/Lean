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
using QuantConnect.Packets;
using QuantConnect.Data.Market;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /******************************************************** 
    * CLASS DEFINITIONS
    *********************************************************/
    /// <summary>
    /// Live Data Feed Streamed From QC Source.
    /// </summary>
    public abstract class LiveTradingDataFeed : IDataFeed
    {
        /******************************************************** 
        * CLASS VARIABLES
        *********************************************************/
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
        private bool _hibernate = false;

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
        /// <param name="algorithm">Algorithm requesting data</param>
        protected LiveTradingDataFeed(IAlgorithm algorithm)
        {
            //Subscription Count:
            _subscriptions = algorithm.SubscriptionManager.Subscriptions;

            //Set Properties:
            _dataFeed = DataFeedEndpoint.LiveTrading;
            _isActive = true;
            _bridge = new ConcurrentQueue<List<BaseData>>[Subscriptions.Count];
            _endOfBridge = new bool[Subscriptions.Count];
            _subscriptionManagers = new SubscriptionDataReader[Subscriptions.Count];

            //Class Privates:
            _algorithm = algorithm;

            //Setup the arrays:
            for (var i = 0; i < Subscriptions.Count; i++)
            {
                _endOfBridge[i] = false;
                _bridge[i] = new ConcurrentQueue<List<BaseData>>();

                //This is quantconnect data source, store here for speed/ease of access
                _isDynamicallyLoadedData.Add(algorithm.Securities[_subscriptions[i].Symbol].IsDynamicallyLoadedData);

                //Subscription managers for downloading user data:
                _subscriptionManagers[i] = new SubscriptionDataReader(_subscriptions[i], algorithm.Securities[_subscriptions[i].Symbol], DataFeedEndpoint.LiveTrading, new DateTime(), new DateTime(9999, 12, 12));

                //Set up the source file for today:
                _subscriptionManagers[i].RefreshSource(DateTime.Now.Date);

                //Subscribe( ... )
            }
        }


        /// <summary>
        /// Subscribe to a new live data stream
        /// </summary>
        /// <param name="type"></param>
        /// <param name="symbol"></param>
        //protected abstract void Subscribe(SecurityType type, string symbol);


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

            Log.Trace("LiveTradingDataFeed.Stream(): Initializing subscription stream stores...");
            for (var i = 0; i < Subscriptions.Count; i++)
            {
                var config = _subscriptions[i];
                _streamStore.Add(i, new StreamStore(config));
            }
            Log.Trace("LiveTradingDataFeed.Stream(): Initialized " + _streamStore.Count + " stream stores.");

            // Set up separate thread to handle stream and building packets:
            var streamThread = new Thread(Stream);
            streamThread.Start();
            Thread.Sleep(5); // Wait a little for the other thread to init.

            bool storingData = false;

            // Setup Real Time Event Trigger:
            var realtime = new RealTimeSynchronizedTimer(TimeSpan.FromSeconds(1), () =>
            {
                storingData = true;

                //This is a minute start / 0-seconds.
                var now = DateTime.Now;
                var onMinute = (now.Second == 0);
                var onDay = ((now.Second == 0) && (now.Hour == 0));

                // Determine if this subscription needs to be archived:
                for (var i = 0; i < Subscriptions.Count; i++)
                {
                    //Do critical events every second regardless of the market/hybernate state:
                    if (onDay)
                    {
                        //Every day refresh the source file for the custom user data:
                        _subscriptionManagers[i].RefreshSource(now.Date);

                        //Update the securities market open/close.
                        UpdateSecurityMarketHours();
                    }

                    //If hybernate stop sending data until its resumed.
                    if (_hibernate) continue;

                    switch (_subscriptions[i].Resolution)
                    { 
                        //This is a second resolution data source:
                        case Resolution.Second:
                            //Enqueue our live data:
                            _streamStore[i].TriggerArchive(_subscriptions[i].FillDataForward);
                            Log.Debug("LiveTradingDataFeed.Run(): Triggered Archive: " + _subscriptions[i].Symbol + "-Second... " + now.ToLongTimeString());
                            break;

                        //This is a minute resolution data source:
                        case Resolution.Minute:
                            if (onMinute)
                            {
                                _streamStore[i].TriggerArchive(_subscriptions[i].FillDataForward);
                                Log.Debug("LiveTradingDataFeed.Run(): Triggered Archive: " + _subscriptions[i].Symbol + "-Minute... " + now.ToLongTimeString());
                            }
                            break;
                    }
                }

                storingData = false;
            });

            //Start the realtime sampler above
            realtime.Start();

            // Scan the Stream Stores for Archived Bars, 
            do
            {
                while (storingData)
                {
                    Thread.Sleep(1);
                }

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
                            Log.Debug("LiveTradingDataFeed.Run(): Enqueuing Data... s:" + data.Symbol + " >> v:" + data.Value);
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

                //Prevent Thread Locking Up - Sleep 1ms (linux only, on windows will sleep 15ms).
                Thread.Sleep(1);

            } while (!_exitTriggered && !_endOfBridges);

            //Dispose of the realtime clock.
            realtime.Stop();

            //Stop thread
            _isActive = false;

            //Exiting RealTime Events:
            Log.Trace("LiveTradingDataFeed.Run(): Exiting Realtime Run Routine");
        }

        /// <summary>
        /// Stream thread handler uses the GetNextTicks() function to get current ticks from a data source and
        /// then uses the stream store to compile them into trade bars.
        /// </summary>
        public void Stream()
        {
            //Initialize
            var exitTasks = false;
            var update = new Dictionary<int, DateTime>();

            //Loop over stream
            do
            {
                //Scan for the required time period to stream:
                UpdateSecurityMarketHours();

                //Wait for one of our equity securities to be open! Attempt to reopen stream when day changes.
                Hibernate();

                //Awake:
                Log.Trace("LiveTradingDataFeed.Stream(): Market Open, Starting stream for " + string.Join(",", _symbols));

                //Micro-thread for polling for new data from data source:
                var liveThreadTask = new Task(()=> {

                    //Blocking ForEach - Should stay within this loop as long as there is a data-connection
                    while (true)
                    {
                        var ticks = GetNextTicks();

                        foreach (var tick in ticks)
                        {
                            //Get the stream store with this symbol:
                            for (var i = 0; i < Subscriptions.Count; i++)
                            {
                                if (_subscriptions[i].Symbol == tick.Symbol)
                                {
                                    _streamStore[i].Update(tick);
                                    Log.Debug("LiveDataFeed.Stream(): New Packet >> " + tick.Symbol + " " + tick.LastPrice.ToString("C"));
                                }
                            }
                        }

                        //If we did hibernate we'll probably need a new session variable, or Quit if signalled.
                        if (Hibernate()) return;
                        if (_exitTriggered) return;

                        Thread.Sleep(1);
                    }
                });

                // Micro-thread for custom data/feeds. This onl supports polling at this time. todo: Custom data sockets
                var customFeedsTask = new Task(() => {
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
                                        _subscriptionManagers[i].MoveNext();
                                        var data = _subscriptionManagers[i].Current;
                                        if (data != null)
                                        {
                                            _streamStore[i].Update(data);
                                        }
                                    }
                                    update[i] = DateTime.Now.Add(_subscriptions[i].Increment);
                                }
                            }
                        }
                        if (Hibernate()) return;
                        if (_exitTriggered) return;
                        if (exitTasks) return;
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
                    customFeedsTask.Start();
                    tasks[1] = customFeedsTask;
                }
                
                Task.WaitAll(tasks);

                //Sleep 10s, then attempt reconnection to prevent thread lock-up
                if (!_exitTriggered) Thread.Sleep(1000);
                Log.Trace("LiveTradingDataFeed.Stream(): Loop exited blocking foreach, reconnecting to stream...");
            }
            while (!_exitTriggered);

            Log.Trace("LiveTradingDataFeed.Stream(): EXITING STREAM");
        }

        /// <summary>
        /// Returns the next ticks from the data source. The data source itself is defined in the derived class's
        /// implementation of this function. For example, if obtaining data from a brokerage, say IB, then the derived
        /// implementation would ask the IB API for the next ticks
        /// </summary>
        /// <returns>The next ticks to be aggregated and sent to algoithm</returns>
        public abstract IEnumerable<Tick> GetNextTicks();

        /// <summary>
        /// Trigger the live trading datafeed thread to abort and stop looping.
        /// </summary>
        public void Exit()
        {
            lock (_lock)
            {
                _exitTriggered = true;
                PurgeData();
            }
        }

        /// <summary>
        /// Conditionally hibernate if the market has closed to avoid constantly pinging the API or trying to login while the market is closed.
        /// </summary>
        public bool Hibernate()
        {
            //Wait for one of our equity securities to be open! Attempt to reopen stream when day changes.
            var hibernateDate = DateTime.Now.Date;
            var announced = false;
            
            //Wait here while market is closed.
            while (!AnySecurityOpen() && hibernateDate.Date == DateTime.Now.Date)
            { 
                if (!announced) 
                {
                    Log.Trace("LiveTradingDataFeed.Stream(): All securities closed, hibernating until market open."); 
                    announced = true;
                    _hibernate = true;
                } 
                Thread.Sleep(1000); 
            }
            _hibernate = false;
            return announced;
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
        /// Update the algorithm market open and close hours on today's values using controls
        /// </summary>
        public void UpdateSecurityMarketHours()
        {
            //Update the "Today's Market" Status: Set the market times so we know when to close/hibernate the algorithm

            foreach (var sub in _subscriptions)
            {
                var security = _algorithm.Securities[sub.Symbol];

                switch (security.Type)
                {
                    case SecurityType.Equity:
                        var _todayEquity = Engine.Api.MarketToday(SecurityType.Equity);
                        Log.Trace("LiveTradingDataFeed.Run(): New Day Market Status: " + _todayEquity.Status);
                        //If we're open set both market open&close to midnight, so it won't open.
                        if (_todayEquity.Status != "open")
                        {
                            _algorithm.Securities[sub.Symbol].Exchange.MarketOpen = TimeSpan.FromHours(0);
                            _algorithm.Securities[sub.Symbol].Exchange.MarketClose = TimeSpan.FromHours(0);
                        }

                        if (sub.ExtendedMarketHours)
                        {
                            _algorithm.Securities[sub.Symbol].Exchange.MarketOpen = _todayEquity.PreMarket.Start;
                            _algorithm.Securities[sub.Symbol].Exchange.MarketClose = _todayEquity.PostMarket.End;
                        }
                        else
                        {
                            _algorithm.Securities[sub.Symbol].Exchange.MarketOpen = _todayEquity.Open.Start;
                            _algorithm.Securities[sub.Symbol].Exchange.MarketClose = _todayEquity.Open.End;
                        }
                        break;

                    case SecurityType.Forex:
                        //var _todayForex = Engine.Api.MarketToday(SecurityType.Forex);
                        //Do nothing, standard market hours are always right.
                        break;
                }
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
                }
            }
            return open;
        }

    } // End Live Trading Data Feed Class:

} // End Namespace
