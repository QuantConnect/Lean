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

using NodaTime;
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Packets;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using HistoryRequest = QuantConnect.Data.HistoryRequest;
using Timer = System.Timers.Timer;

namespace QuantConnect.ToolBox.IQFeed
{
    /// <summary>
    /// IQFeedDataQueueHandler is an implementation of IDataQueueHandler and IHistoryProvider
    /// </summary>
    public class IQFeedDataQueueHandler : IDataQueueHandler, IHistoryProvider
    {
        private bool _isConnected;
        private int _dataPointCount;
        private readonly HashSet<Symbol> _symbols;
        private readonly object _sync = new object();

        //Socket connections:
        private AdminPort _adminPort;
        private Level1Port _level1Port;
        private HistoryPort _historyPort;
        private BlockingCollection<BaseData> _outputCollection;

        /// <summary>
        /// Gets the total number of data points emitted by this history provider
        /// </summary>
        public int DataPointCount
        {
            get { return _dataPointCount; }
        }

        /// <summary>
        /// IQFeedDataQueueHandler is an implementation of IDataQueueHandler:
        /// </summary>
        public IQFeedDataQueueHandler()
        {
            _symbols = new HashSet<Symbol>();
            _outputCollection = new BlockingCollection<BaseData>();
            if (!IsConnected) Connect();
        }

        /// <summary>
        /// Get the next ticks from the live trading data queue
        /// </summary>
        /// <returns>IEnumerable list of ticks since the last update.</returns>
        public IEnumerable<BaseData> GetNextTicks()
        {
            foreach (var tick in _outputCollection.GetConsumingEnumerable())
            {
                yield return tick;
            }
        }

        /// <summary>
        /// Adds the specified symbols to the subscription: new IQLevel1WatchItem("IBM", true)
        /// </summary>
        /// <param name="job">Job we're subscribing for:</param>
        /// <param name="symbols">The symbols to be added keyed by SecurityType</param>
        public void Subscribe(LiveNodePacket job, IEnumerable<Symbol> symbols)
        {
            try
            {
                foreach (var symbol in symbols)
                {
                    if (CanSubscribe(symbol))
                    {
                        lock (_sync)
                        {
                            Log.Trace("IQFeed.Subscribe(): Subscribe Request: " + symbol.ToString());

                            var type = symbol.ID.SecurityType;
                            if (_symbols.Add(symbol))
                            {
                                var ticker = symbol.Value;
                                if (type == SecurityType.Forex) ticker += ".FXCM";
                                _level1Port.Subscribe(ticker);

                                Log.Trace("IQFeed.Subscribe(): Subscribe Processed: " + symbol.ToString());
                            }
                        }
                    }
                }
            }
            catch (Exception err)
            {
                Log.Error("IQFeed.Subscribe(): " + err.Message);
            }
        }

        /// <summary>
        /// Removes the specified symbols to the subscription
        /// </summary>
        /// <param name="job">Job we're processing.</param>
        /// <param name="symbols">The symbols to be removed keyed by SecurityType</param>
        public void Unsubscribe(LiveNodePacket job, IEnumerable<Symbol> symbols)
        {
            try
            {
                foreach (var symbol in symbols)
                {
                    lock (_sync)
                    {
                        Log.Trace("IQFeed.Unsubscribe(): " + symbol.ToString());
                        var type = symbol.ID.SecurityType;
                        _symbols.Remove(symbol);
                        var ticker = symbol.Value;
                        if (type == SecurityType.Forex) ticker += ".FXCM";

                        if (_level1Port.Contains(ticker))
                        {
                            _level1Port.Unsubscribe(ticker);
                        }
                    }
                }
            }
            catch (Exception err)
            {
                Log.Error("IQFeed.Unsubscribe(): " + err.Message);
            }
        }

        /// <summary>
        /// Initializes this history provider to work for the specified job
        /// </summary>
        /// <param name="job">The job</param>
        /// <param name="mapFileProvider">Provider used to get a map file resolver to handle equity mapping</param>
        /// <param name="factorFileProvider">Provider used to get factor files to handle equity price scaling</param>
        /// <param name="statusUpdate">Function used to send status updates</param>
        public void Initialize(AlgorithmNodePacket job, IMapFileProvider mapFileProvider, IFactorFileProvider factorFileProvider, Action<int> statusUpdate)
        {
            return;
        }

        /// <summary>
        /// Gets the history for the requested securities
        /// </summary>
        /// <param name="requests">The historical data requests</param>
        /// <param name="sliceTimeZone">The time zone used when time stamping the slice instances</param>
        /// <returns>An enumerable of the slices of data covering the span specified in each request</returns>
        public IEnumerable<Slice> GetHistory(IEnumerable<HistoryRequest> requests, DateTimeZone sliceTimeZone)
        {
            foreach (var request in requests)
            {
                foreach (var slice in _historyPort.ProcessHistoryRequests(request))
                {
                    yield return slice;
                }
            }
        }

        /// <summary>
        /// Indicates the connection is live.
        /// </summary>
        private bool IsConnected
        {
            get { return _isConnected; }
        }

        /// <summary>
        /// Connect to the IQ Feed using supplied username and password information.
        /// </summary>
        private void Connect()
        {
            try
            {
                //Launch the IQ Feed Application:
                Log.Trace("IQFeed.Connect(): Launching client...");

                var connector = new IQConnect(Config.Get("iqfeed-productName"), "1.0");
                connector.Launch();

                // Initialise one admin port
                Log.Trace("IQFeed.Connect(): Connecting to admin...");
                _adminPort = new AdminPort();
                _adminPort.Connect();
                _adminPort.SetAutoconnect();
                _adminPort.SetClientStats(false);
                _adminPort.SetClientName("Admin");

                _adminPort.DisconnectedEvent += AdminPortOnDisconnectedEvent;
                _adminPort.ConnectedEvent += AdminPortOnConnectedEvent;

                Log.Trace("IQFeed.Connect(): Connecting to L1 data...");
                _level1Port = new Level1Port(_outputCollection);
                _level1Port.Connect();
                _level1Port.SetClientName("Level1");

                Log.Trace("IQFeed.Connect(): Connecting to Historical data...");
                _historyPort = new HistoryPort();
                _historyPort.Connect();
                _historyPort.SetClientName("History");

                _isConnected = true;
            }
            catch (Exception err)
            {
                Log.Error("IQFeed.Connect(): Error Connecting to IQFeed: " + err.Message);
                _isConnected = false;
            }
        }

        /// <summary>
        /// Disconnect from all ports we're subscribed to:
        /// </summary>
        /// <remarks>
        /// Not being used. IQ automatically disconnect on killing LEAN
        /// </remarks>
        private void Disconnect()
        {
            if (_adminPort != null) _adminPort.Disconnect();
            if (_level1Port != null) _level1Port.Disconnect();
            _isConnected = false;
            Log.Trace("IQFeed.Disconnect(): Disconnected");
        }

        /// <summary>
        /// Returns true if this data provide can handle the specified symbol
        /// </summary>
        /// <param name="symbol">The symbol to be handled</param>
        /// <returns>True if this data provider can get data for the symbol, false otherwise</returns>
        private bool CanSubscribe(Symbol symbol)
        {
            var market = symbol.ID.Market;
            var securityType = symbol.ID.SecurityType;
            return
                (securityType == SecurityType.Equity && market == Market.USA) ||
                (securityType == SecurityType.Forex && market == Market.FXCM);
        }

        /// <summary>
        /// Admin port is connected.
        /// </summary>
        private void AdminPortOnConnectedEvent(object sender, ConnectedEventArgs connectedEventArgs)
        {
            _isConnected = true;
            Log.Error("IQFeed.AdminPortOnConnectedEvent(): ADMIN PORT CONNECTED!");
        }

        /// <summary>
        /// Admin port disconnected from the IQFeed server.
        /// </summary>
        private void AdminPortOnDisconnectedEvent(object sender, DisconnectedEventArgs disconnectedEventArgs)
        {
            _isConnected = false;
            Log.Error("IQFeed.AdminPortOnDisconnectedEvent(): ADMIN PORT DISCONNECTED!");
        }

        /// <summary>
        /// Admin class type
        /// </summary>
        public class AdminPort : IQAdminSocketClient
        {
            public AdminPort()
                : base(80)
            {
            }
        }

        /// <summary>
        /// Level 1 Data Request:
        /// </summary>
        public class Level1Port : IQLevel1Client
        {
            private int count;
            private DateTime start;
            private DateTime _feedTime;
            private Stopwatch _stopwatch = new Stopwatch();
            private readonly Timer _timer;
            private readonly BlockingCollection<BaseData> _dataQueue;
            private readonly ConcurrentDictionary<string, double> _prices;

            public DateTime FeedTime
            {
                get
                {
                    if (_feedTime == new DateTime()) return DateTime.Now;
                    return _feedTime.AddMilliseconds(_stopwatch.ElapsedMilliseconds);
                }
                set
                {
                    _feedTime = value;
                    _stopwatch = Stopwatch.StartNew();
                }
            }

            public Level1Port(BlockingCollection<BaseData> dataQueue)
                : base(80)
            {
                start = DateTime.Now;
                _prices = new ConcurrentDictionary<string, double>();

                _dataQueue = dataQueue;
                Level1SummaryUpdateEvent += OnLevel1SummaryUpdateEvent;
                Level1TimerEvent += OnLevel1TimerEvent;
                Level1ServerDisconnectedEvent += OnLevel1ServerDisconnected;
                Level1ServerReconnectFailed += OnLevel1ServerReconnectFailed;
                Level1UnknownEvent += OnLevel1UnknownEvent;
                Level1FundamentalEvent += OnLevel1FundamentalEvent;

                _timer = new Timer(1000);
                _timer.Enabled = false;
                _timer.AutoReset = true;
                _timer.Elapsed += (sender, args) =>
                {
                    var ticksPerSecond = count/(DateTime.Now - start).TotalSeconds;
                    if (ticksPerSecond > 1000 || _dataQueue.Count > 31)
                    {
                        Log.Trace(string.Format("IQFeed.OnSecond(): Ticks/sec: {0} Engine.Ticks.Count: {1} CPU%: {2}",
                            ticksPerSecond.ToString("0000.00"),
                            _dataQueue.Count,
                            OS.CpuUsage.NextValue().ToString("0.0") + "%"
                            ));
                    }

                    count = 0;
                    start = DateTime.Now;
                };

                _timer.Enabled = true;
            }

            private void OnLevel1FundamentalEvent(object sender, Level1FundamentalEventArgs e)
            {
                // handle split data, they're only valid today, they'll show up around 4:45am EST
                if (e.SplitDate1.Date == DateTime.Today && DateTime.Now.TimeOfDay.TotalHours <= 8) // they will always be sent premarket
                {
                    // get the last price, if it doesn't exist then we'll just issue the split claiming the price was zero
                    // this should (ideally) never happen, but sending this without the price is much better then not sending
                    // it at all
                    double referencePrice;
                    _prices.TryGetValue(e.Symbol, out referencePrice);
                    var sid = SecurityIdentifier.GenerateEquity(e.Symbol, Market.USA);
                    var split = new Split(new Symbol(sid, e.Symbol), FeedTime, (decimal) referencePrice, (decimal) e.SplitFactor1);
                    _dataQueue.Add(split);
                }
            }

            /// <summary>
            /// Handle a new price update packet:
            /// </summary>
            private void OnLevel1SummaryUpdateEvent(object sender, Level1SummaryUpdateEventArgs e)
            {
                // if ticker is not found, unsubscribe
                if (e.NotFound) Unsubscribe(e.Symbol);

                // only update if we have a value
                if (e.Last == 0) return;

                // only accept trade updates
                if (e.TypeOfUpdate != Level1SummaryUpdateEventArgs.UpdateType.ExtendedTrade
                 && e.TypeOfUpdate != Level1SummaryUpdateEventArgs.UpdateType.Trade) return;

                count++;
                var time = FeedTime;
                var symbol = Symbol.Create(e.Symbol, SecurityType.Equity, Market.USA);
                var last = (decimal)(e.TypeOfUpdate == Level1SummaryUpdateEventArgs.UpdateType.ExtendedTrade ? e.ExtendedTradingLast : e.Last);

                if (e.Symbol.Contains(".FXCM"))
                {
                    // the feed time is in NYC/EDT, convert it into EST
                    time = FeedTime.ConvertTo(TimeZones.NewYork, TimeZones.EasternStandard);
                    symbol = Symbol.Create(e.Symbol.Replace(".FXCM", string.Empty), SecurityType.Forex, Market.FXCM);
                }

                var tick = new Tick(time, symbol, last, (decimal)e.Bid, (decimal)e.Ask)
                {
                    AskSize = e.AskSize,
                    BidSize = e.BidSize,
                    TickType = TickType.Trade,
                    Quantity = e.IncrementalVolume
                };

                _dataQueue.Add(tick);
                _prices[e.Symbol] = e.Last;
            }

            /// <summary>
            /// Set the interal clock time.
            /// </summary>
            private void OnLevel1TimerEvent(object sender, Level1TimerEventArgs e)
            {
                //If there was a bad tick and the time didn't set right, skip setting it here and just use our millisecond timer to set the time from last time it was set.
                if (e.DateTimeStamp != DateTime.MinValue)
                {
                    FeedTime = e.DateTimeStamp;
                }
            }

            /// <summary>
            /// Server has disconnected, reconnect.
            /// </summary>
            private void OnLevel1ServerDisconnected(object sender, Level1ServerDisconnectedArgs e)
            {
                Log.Error("IQFeed.OnLevel1ServerDisconnected(): LEVEL 1 PORT DISCONNECTED! " + e.TextLine);
            }

            /// <summary>
            /// Server has disconnected, reconnect.
            /// </summary>
            private void OnLevel1ServerReconnectFailed(object sender, Level1ServerReconnectFailedArgs e)
            {
                Log.Error("IQFeed.OnLevel1ServerReconnectFailed(): LEVEL 1 PORT DISCONNECT! " + e.TextLine);
            }

            /// <summary>
            /// Got a message we don't know about, log it for posterity.
            /// </summary>
            private void OnLevel1UnknownEvent(object sender, Level1TextLineEventArgs e)
            {
                Log.Error("IQFeed.OnUnknownEvent(): " + e.TextLine);
            }
        }

        private static PeriodType GetPeriodType(Resolution resolution)
        {
            switch (resolution)
            {
                case Resolution.Second:
                    return PeriodType.Second;
                case Resolution.Minute:
                    return PeriodType.Minute;
                case Resolution.Hour:
                    return PeriodType.Hour;
                case Resolution.Tick:
                case Resolution.Daily:
                default:
                    throw new ArgumentOutOfRangeException("resolution", resolution, null);
            }
        }

        // this type is expected to be used for exactly one job at a time
        public class HistoryPort : IQLookupHistorySymbolClient
        {
            private bool _inProgress;
            private ConcurrentDictionary<string, HistoryRequest> _requestDataByRequestId;
            private ConcurrentDictionary<string, List<BaseData>> _currentRequest;
            private readonly string DataDirectory = Config.Get("data-directory", "../../../Data");
            private readonly double MaxHistoryRequestMinutes = Config.GetDouble("max-history-minutes", 5);

            /// <summary>
            /// ... 
            /// </summary>
            public HistoryPort()
                : base(80)
            {
                _requestDataByRequestId = new ConcurrentDictionary<string, HistoryRequest>();
                _currentRequest = new ConcurrentDictionary<string, List<BaseData>>();
            }

            /// <summary>
            /// Populate request data
            /// </summary>
            public IEnumerable<Slice> ProcessHistoryRequests(HistoryRequest request)
            {                
                // we can only process equity/forex types here
                if (request.SecurityType != SecurityType.Forex && request.SecurityType != SecurityType.Equity)
                {
                    yield break;
                }

                // Set this process status
                _inProgress = true;

                var symbol = request.Symbol.Value;
                if (request.SecurityType == SecurityType.Forex)
                {
                    symbol = symbol + ".FXCM";
                }

                var start = request.StartTimeUtc.ConvertFromUtc(TimeZones.NewYork);
                DateTime? end = request.EndTimeUtc.ConvertFromUtc(TimeZones.NewYork);
                // if we're within a minute of now, don't set the end time
                if (request.EndTimeUtc >= DateTime.UtcNow.AddMinutes(-1))
                {
                    end = null;
                }

                Log.Trace(string.Format("HistoryPort.ProcessHistoryJob(): Submitting request: {0}-{1}: {2} {3}->{4}", request.SecurityType, symbol, request.Resolution, start, end ?? DateTime.UtcNow.AddMinutes(-1)));

                int id;
                var reqid = string.Empty;

                switch (request.Resolution)
                {
                    case Resolution.Tick:
                        id = RequestTickData(symbol, start, end, true);
                        reqid = CreateRequestID(LookupType.REQ_HST_TCK, id);
                        break;
                    case Resolution.Daily:
                        id = RequestDailyData(symbol, start, end, true);
                        reqid = CreateRequestID(LookupType.REQ_HST_DWM, id);
                        break;
                    default:
                        var interval = new Interval(GetPeriodType(request.Resolution), 1);
                        id = RequestIntervalData(symbol, interval, start, end, true);
                        reqid = CreateRequestID(LookupType.REQ_HST_INT, id);
                        break;
                }

                _requestDataByRequestId[reqid] = request;

                while (_inProgress)
                {
                    continue;
                }

                // After all data arrive, we pass it to the algorithm through memory and write to a file
                foreach (var key in _currentRequest.Keys)
                {
                    List<BaseData> tradeBars;
                    if (_currentRequest.TryRemove(key, out tradeBars))
                    {
                        foreach (var tradeBar in tradeBars)
                        {
                            // Returns IEnumerable<Slice> object
                            yield return new Slice(tradeBar.EndTime, new[] { tradeBar });
                        }
                    }
                }
            }

            /// <summary>
            /// Created new request ID for a given lookup type (tick, intraday bar, daily bar)
            /// </summary>
            /// <param name="lookupType">Lookup type: REQ_HST_TCK (tick), REQ_HST_DWM (daily) or REQ_HST_INT (intraday resolutions)</param>
            /// <param name="id">Sequential identifier</param>
            /// <returns></returns>
            private static string CreateRequestID(LookupType lookupType, int id)
            {
                return lookupType + id.ToString("0000000");
            }

            /// <summary>
            /// Method called when a new Lookup event is fired
            /// </summary>
            /// <param name="e">Received data</param>
            protected override void OnLookupEvent(LookupEventArgs e)
            {
                try
                {
                    switch (e.Sequence)
                    {
                        case LookupSequence.MessageStart:
                            _currentRequest.AddOrUpdate(e.Id, new List<BaseData>());
                            break;
                        case LookupSequence.MessageDetail:
                            List<BaseData> current;
                            if (_currentRequest.TryGetValue(e.Id, out current))
                            {
                                HandleMessageDetail(e, current);
                            }
                            break;
                        case LookupSequence.MessageEnd:
                            _inProgress = false;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                catch (Exception err)
                {
                    Log.Error(err);
                }
            }

            /// <summary>
            /// Put received data into current list of BaseData object
            /// </summary>
            /// <param name="e">Received data</param>
            /// <param name="current">Current list of BaseData object</param>
            private void HandleMessageDetail(LookupEventArgs e, List<BaseData> current)
            {
                var requestData = _requestDataByRequestId[e.Id];
                var data = GetData(e, requestData);
                if (data != null && data.Time != DateTime.MinValue)
                {
                    current.Add(data);
                }
            }

            /// <summary>
            /// Transform received data into BaseData object
            /// </summary>
            /// <param name="e">Received data</param>
            /// <param name="requestData">Request information</param>
            /// <returns>BaseData object</returns>
            private BaseData GetData(LookupEventArgs e, HistoryRequest requestData)
            {
                var isEquity = requestData.SecurityType == SecurityType.Equity;
                var scale = isEquity ? 1000m : 1m;
                try
                {
                    switch (e.Type)
                    {
                        case LookupType.REQ_HST_TCK:
                            var t = (LookupTickEventArgs) e;
                            var time = isEquity ? t.DateTimeStamp : t.DateTimeStamp.ConvertTo(TimeZones.NewYork, TimeZones.EasternStandard);
                            return new Tick(time, requestData.Symbol, (decimal) t.Last*scale, (decimal) t.Bid*scale, (decimal) t.Ask*scale);
                        case LookupType.REQ_HST_INT:
                            var i = (LookupIntervalEventArgs) e;
                            if (i.DateTimeStamp == DateTime.MinValue) return null;
                            var istartTime = i.DateTimeStamp - requestData.Resolution.ToTimeSpan();
                            if (!isEquity) istartTime = istartTime.ConvertTo(TimeZones.NewYork, TimeZones.EasternStandard);
                            return new TradeBar(istartTime, requestData.Symbol, (decimal) i.Open*scale, (decimal) i.High*scale, (decimal) i.Low*scale, (decimal) i.Close*scale, i.PeriodVolume);
                        case LookupType.REQ_HST_DWM:
                            var d = (LookupDayWeekMonthEventArgs) e;
                            if (d.DateTimeStamp == DateTime.MinValue) return null;
                            var dstartTime = d.DateTimeStamp - requestData.Resolution.ToTimeSpan();
                            if (!isEquity) dstartTime = dstartTime.ConvertTo(TimeZones.NewYork, TimeZones.EasternStandard);
                            return new TradeBar(dstartTime, requestData.Symbol, (decimal) d.Open*scale, (decimal) d.High*scale, (decimal) d.Low*scale, (decimal) d.Close*scale, d.PeriodVolume);

                        // we don't need to handle these other types
                        case LookupType.REQ_SYM_SYM:
                        case LookupType.REQ_SYM_SIC:
                        case LookupType.REQ_SYM_NAC:
                        case LookupType.REQ_TAB_MKT:
                        case LookupType.REQ_TAB_SEC:
                        case LookupType.REQ_TAB_MKC:
                        case LookupType.REQ_TAB_SIC:
                        case LookupType.REQ_TAB_NAC:
                        default:
                            return null;
                    }
                }
                catch (Exception err)
                {
                    Log.Error("Encountered error while processing request: " + e.Id);
                    Log.Error(err);
                    return null;
                }
            }
        }
    }
}
