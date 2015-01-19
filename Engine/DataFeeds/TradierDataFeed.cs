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
using QuantConnect.Brokerages.Tradier;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Packets;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /******************************************************** 
    * CLASS DEFINITIONS
    *********************************************************/
    /// <summary>
    /// Tradier live data feed streamed from Tradier API.
    /// </summary>
    public class TradierDataFeed : IDataFeed
    {
        /******************************************************** 
        * CLASS VARIABLES
        *********************************************************/
        private List<SubscriptionDataConfig> _subscriptions = new List<SubscriptionDataConfig>();
        private List<bool> _isQuantConnectData = new List<bool>();
        private SubscriptionDataReader[] _subscriptionManagers;
        private int _subscriptionCount = 0;
        private ConcurrentQueue<List<BaseData>>[] _bridge;
        private bool _endOfBridges = false;
        private bool _isActive = true;
        private bool[] _endOfBridge = new bool[1];
        private DataFeedEndpoint _dataFeed = DataFeedEndpoint.Tradier;
        private IAlgorithm _algorithm;
        private LiveNodePacket _job;
        private object _lock = new Object();
        private bool _exitTriggered = false;
        private List<string> _symbols = new List<string>();
        private Dictionary<int, StreamStore> _streamStore = new Dictionary<int, StreamStore>();
        private TradierBrokerage _tradier = new TradierBrokerage();
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
        /// Public access to the tradier brokerage class.
        /// </summary>
        public TradierBrokerage Tradier
        {
            get { return _tradier; }
            set { _tradier = value; }
        }

        public DateTime LoadedDataFrontier { get; private set; }

        /******************************************************** 
        * CLASS CONSTRUCTOR
        *********************************************************/
        /// <summary>
        /// Tradier datafeed handler for getting free data from the tradier brokerage api.
        /// </summary>
        /// <param name="algorithm">Algorithm requesting data</param>
        /// <param name="job">Job packet requesting data</param>
        /// <param name="brokerage">Brokerage instance to avoid access token duplication.</param>
        public TradierDataFeed(IAlgorithm algorithm, IBrokerage brokerage, LiveNodePacket job)
        {
            //Subscription Count:
            _subscriptions = algorithm.SubscriptionManager.Subscriptions;
            _subscriptionCount = Subscriptions.Count;

            //Set Properties:
            _dataFeed = DataFeedEndpoint.Tradier;
            _isActive = true;
            _bridge = new ConcurrentQueue<List<BaseData>>[_subscriptionCount];
            _endOfBridge = new bool[_subscriptionCount];
            _subscriptionManagers = new SubscriptionDataReader[_subscriptionCount];

            //Class Privates:
            _job = job;
            _algorithm = algorithm;

            //Setup the arrays:
            for (var i = 0; i < _subscriptionCount; i++)
            {
                _endOfBridge[i] = false;
                _bridge[i] = new ConcurrentQueue<List<BaseData>>();

                //This is quantconnect data source, store here for speed/ease of access
                _isQuantConnectData.Add(algorithm.Securities[_subscriptions[i].Symbol].IsQuantConnectData);

                //Subscription managers for downloading user data:
                _subscriptionManagers[i] = new SubscriptionDataReader(_subscriptions[i], algorithm.Securities[_subscriptions[i].Symbol], DataFeedEndpoint.LiveTrading, new DateTime(), new DateTime(9999, 12, 12));

                //Set up the source file for today:
                _subscriptionManagers[i].RefreshSource(DateTime.Now.Date);
            }

            //Setup Brokerage Access:
            _tradier = (TradierBrokerage)brokerage;
        }

        /// <summary>
        /// Execute the primary Tradier thread for stock data.
        /// 1. Subscribe to the streams requested.
        /// 2. Build bars or tick data requested, primary loop increment smallest possible.
        /// </summary>
        public void Run()
        {
            // Symbols requested:
            _symbols = (from security in _algorithm.Securities.Values
                        where security.IsQuantConnectData && security.Type == SecurityType.Equity
                        select security.Symbol).ToList<string>();

            // Set up separate thread to handle stream and building packets:
            var streamThread = new Thread(Stream);
            streamThread.Start();
            Thread.Sleep(5); // Wait a little for the other thread to init.

            // Setup Real Time Event Trigger:
            var realtime = new RealTimeSynchronizedTimer(TimeSpan.FromSeconds(1), () =>
            {
                //This is a minute start / 0-seconds.
                var onMinute = (DateTime.Now.Second == 0);
                var onDay = ((DateTime.Now.Second == 0) && (DateTime.Now.Hour == 0));

                // Determine if this subscription needs to be archived:
                for (var i = 0; i < _subscriptionCount; i++)
                {
                    //Do critical events every second regardless of the market/hybernate state:
                    if (onDay)
                    {
                        //Every day refresh the source file for the custom user data:
                        _subscriptionManagers[i].RefreshSource(DateTime.Now.Date);

                        //Update the securities market open/close.
                        UpdateSecurityMarketHours();
                    }

                    //If hybernate stop sending data until its resumed.
                    if (_hibernate) continue;

                    switch (_subscriptions[i].Resolution)
                    { 
                        //This is a second resolution data source:
                        case Resolution.Second:
                            //Enqueue Tradier data:
                            _streamStore[i].TriggerArchive(_subscriptions[i].FillDataForward, _isQuantConnectData[i]);
                            Log.Debug("TradierDataFeed.Run(): Triggered Archive: " + _subscriptions[i].Symbol + "-Second... " + DateTime.Now.ToLongTimeString());
                            break;

                        //This is a minute resolution data source:
                        case Resolution.Minute:
                            if (onMinute)
                            {
                                _streamStore[i].TriggerArchive(_subscriptions[i].FillDataForward, _isQuantConnectData[i]);
                                Log.Debug("TradierDataFeed.Run(): Triggered Archive: " + _subscriptions[i].Symbol + "-Minute... " + DateTime.Now.ToLongTimeString());
                            }
                            break;
                    }
                }
            });

            //Start the realtime sampler above
            realtime.Start();

            // Scan the Stream Stores for Archived Bars, 
            do
            {
                try
                {
                    //Scan the Stream Store Queue's and if there are any shuffle them over to the bridge for synchronization:
                    for (var i = 0; i < _subscriptionCount; i++)
                    {
                        if (_streamStore[i].Queue.Count > 0)
                        { 
                            BaseData data;
                            if (_streamStore[i].Queue.TryDequeue(out data))
                            {
                                Bridge[i].Enqueue(new List<BaseData> { data });
                                Log.Debug("TradierDataFeed.Run(): Enqueuing Data... s:" + data.Symbol + " >> v:" + data.Value);
                            }
                        }
                    }

                    LoadedDataFrontier = DateTime.Now;
                }
                catch (Exception err)
                {
                    Log.Error("TradierDataFeed.Run(): " + err.Message);
                }

                //Prevent Thread Locking Up - Sleep 1ms (linux only, on windows will sleep 15ms).
                Thread.Sleep(1);

            } while (!_exitTriggered && !_endOfBridges);

            //Dispose of the realtime clock.
            realtime.Stop();

            //Stop thread
            _isActive = false;

            //Exiting RealTime Events:
            Log.Trace("TradierDataFeed.Run(): Exiting Realtime Run Routine");
        }

        /// <summary>
        /// Stream thread handler uses the tradier object to build bars or ticks.
        /// </summary>
        public void Stream()
        {
            //Initialize
            var exitTasks = false;
            var update = new Dictionary<int, DateTime>();
            _streamStore = new Dictionary<int, StreamStore>();
            
            //Initialize:
            Log.Trace("TradierDataFeed.Stream(): Initializing subscription stream stores...");
            for (var i = 0; i < _subscriptionCount; i++)
            {
                var config = _subscriptions[i];
                _streamStore.Add(i, new StreamStore(config));
            }
            Log.Trace("TradierDataFeed.Stream(): Initialized " + _streamStore.Count + " stream stores.");

            //Loop over stream
            do
            {
                //Initialize loop:
                exitTasks = false;

                //Scan for the required time period to stream:
                UpdateSecurityMarketHours();

                //Wait for one of our equity securities to be open! Attempt to reopen stream when day changes.
                Hibernate();

                //Awake:
                Log.Trace("TradierDataFeed.Stream(): Market Open, Starting stream for " + string.Join(",", _symbols));

                //Micro-thread for tradier stream running:
                var tradierTask = new Task(() => {
                    //Blocking ForEach - Should stay within this loop as long as there is a data-connection
                    foreach (var data in Tradier.Stream(_symbols))
                    {
                        if (data != null && data.Type != "trade") continue;

                        //Get the stream store with this symbol:
                        for (var i = 0; i < _subscriptionCount; i++)
                        {
                            if (_subscriptions[i].Symbol == data.Symbol)
                            {
                                _streamStore[i].Update(data.TradePrice, data.TradeSize, data.BidPrice, data.AskPrice);
                                //Log.Debug("TradierDataFeed.Stream(): New Packet >> " + data.Symbol + " " + data.TradePrice.ToString("C"));
                            }
                        }

                        //If we did hibernate we'll probably need a new session variable, or Quit if signalled.
                        if (Hibernate()) return;
                        if (exitTasks) return;
                        if (_exitTriggered) return;
                    }

                    //If real symbols have died here, reboot tasks. Otherwise just stop polling this thread.
                    exitTasks = (_symbols.Count > 0);
                });

                // Micro-thread for custom feeds pooling.
                var customFeedsTask = new Task(() => {
                    while(true)
                    {
                        for (var i = 0; i < _subscriptionCount; i++)
                        {
                            if (!_isQuantConnectData[i])
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
                        Thread.Sleep(1);
                    }
                });

                //Wait for micro-threads to break before continuing
                tradierTask.Start(); customFeedsTask.Start();
                Task.WaitAll(tradierTask, customFeedsTask);

                //Make sure its not a token expired error:
                if (Tradier.Faults.Count > 0)
                {
                    foreach (var fault in Tradier.Faults)
                    {
                        if (fault.Description == "Access Token expired")
                        { 
                            //If access token has expired, we've missed our refresh window, there's not much we can do: just quit the algorithm
                            _exitTriggered = true;
                            _endOfBridges = true; //No data.
                            Log.Trace("TradierDataFeed.Stream(): Access token expired, sending signal to stop data streams.");
                        }
                    }
                }

                //Sleep 10s, then attempt reconnection to prevent thread lock-up
                if (!_exitTriggered) Thread.Sleep(1000);
                Log.Trace("TradierDataFeed.Stream(): Loop exited blocking foreach, reconnecting to stream...");
            }
            while (!_exitTriggered);

            Log.Trace("TradierDataFeed.Stream(): EXITING STREAM");
        }


        /// <summary>
        /// Trigger the tradier datafeed thread to abort and stop looping.
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
        /// Conditionally hybernate if the market has closed to avoid constantly pinging the Tradier API or trying to login while the market is closed.
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
                    Log.Trace("TradierDataFeed.Stream(): All securities closed, hibernating until market open."); 
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
        /// Update the algorithm market open and close hours on today's values from the Tradier API.
        /// </summary>
        public void UpdateSecurityMarketHours()
        {
            //Update the "Today's Market" Status:
            var _today = Engine.Controls.MarketToday(SecurityType.Equity);
            Log.Trace("TradierDataFeed.Run(): New Day Market Status: " + _today.Status);

            foreach (var sub in _subscriptions)
            {
                var security = _algorithm.Securities[sub.Symbol];
                if (security.Type == SecurityType.Equity)
                {
                    //If we're open set both market open&close to midnight, so it won't open.
                    if (_today.Status != "open")
                    {
                        _algorithm.Securities[sub.Symbol].Exchange.MarketOpen = TimeSpan.FromHours(0);
                        _algorithm.Securities[sub.Symbol].Exchange.MarketClose = TimeSpan.FromHours(0);
                    }

                    if (sub.ExtendedMarketHours)
                    {
                        _algorithm.Securities[sub.Symbol].Exchange.MarketOpen = _today.PreMarket.Start;
                        _algorithm.Securities[sub.Symbol].Exchange.MarketClose = _today.PostMarket.End;
                    }
                    else
                    {
                        _algorithm.Securities[sub.Symbol].Exchange.MarketOpen = _today.Open.Start;
                        _algorithm.Securities[sub.Symbol].Exchange.MarketClose = _today.Open.End;
                    }
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

    } // End Tradier Data Feed Class:

} // End Namespace
