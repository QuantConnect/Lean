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
using System.Linq;
using QuantConnect.Util;
using HistoryRequest = QuantConnect.Data.HistoryRequest;
using Timer = System.Timers.Timer;
using System.Threading;

namespace QuantConnect.ToolBox.IQFeed
{
    /// <summary>
    /// IQFeedDataQueueHandler is an implementation of IDataQueueHandler and IHistoryProvider
    /// </summary>
    public class IQFeedDataQueueHandler : HistoryProviderBase, IDataQueueHandler, IDataQueueUniverseProvider
    {
        private bool _isConnected;
        private int _dataPointCount;
        private readonly HashSet<Symbol> _symbols;
        private readonly Dictionary<Symbol, Symbol> _underlyings;
        private readonly object _sync = new object();
        private IQFeedDataQueueUniverseProvider _symbolUniverse;

        //Socket connections:
        private AdminPort _adminPort;
        private Level1Port _level1Port;
        private HistoryPort _historyPort;

        private readonly IDataAggregator _aggregator = Composer.Instance.GetExportedValueByTypeName<IDataAggregator>(
            Config.Get("data-aggregator", "QuantConnect.Lean.Engine.DataFeeds.AggregationManager"));
        private readonly EventBasedDataQueueHandlerSubscriptionManager _subscriptionManager;

        /// <summary>
        /// Gets the total number of data points emitted by this history provider
        /// </summary>
        public override int DataPointCount => _dataPointCount;

        /// <summary>
        /// IQFeedDataQueueHandler is an implementation of IDataQueueHandler:
        /// </summary>
        public IQFeedDataQueueHandler()
        {
            _symbols = new HashSet<Symbol>();
            _underlyings = new Dictionary<Symbol, Symbol>();
            _subscriptionManager = new EventBasedDataQueueHandlerSubscriptionManager();
            _subscriptionManager.SubscribeImpl += (s, t) =>
            {
                Subscribe(s);
                return true;
            };

            _subscriptionManager.UnsubscribeImpl += (s, t) =>
            {
                Unsubscribe(s);
                return true;
            };

            if (!IsConnected) Connect();
        }

        /// <summary>
        /// Subscribe to the specified configuration
        /// </summary>
        /// <param name="dataConfig">defines the parameters to subscribe to a data feed</param>
        /// <param name="newDataAvailableHandler">handler to be fired on new data available</param>
        /// <returns>The new enumerator for this subscription request</returns>
        public IEnumerator<BaseData> Subscribe(SubscriptionDataConfig dataConfig, EventHandler newDataAvailableHandler)
        {
            if (!CanSubscribe(dataConfig.Symbol))
            {
                return Enumerable.Empty<BaseData>().GetEnumerator();
            }

            var enumerator = _aggregator.Add(dataConfig, newDataAvailableHandler);
            _subscriptionManager.Subscribe(dataConfig);

            return enumerator;
        }

        /// <summary>
        /// Adds the specified symbols to the subscription: new IQLevel1WatchItem("IBM", true)
        /// </summary>
        /// <param name="symbols">The symbols to be added keyed by SecurityType</param>
        public void Subscribe(IEnumerable<Symbol> symbols)
        {
            try
            {
                foreach (var symbol in symbols)
                {
                    lock (_sync)
                    {
                        Log.Trace("IQFeed.Subscribe(): Subscribe Request: " + symbol.ToString());

                        if (_symbols.Add(symbol))
                        {
                            // processing canonical option symbol to subscribe to underlying prices
                            var subscribeSymbol = symbol;

                            if (symbol.ID.SecurityType == SecurityType.Option && symbol.IsCanonical())
                            {
                                subscribeSymbol = symbol.Underlying;
                                _underlyings.Add(subscribeSymbol, symbol);
                            }

                            if (symbol.ID.SecurityType == SecurityType.Future && symbol.IsCanonical())
                            {
                                // do nothing for now. Later might add continuous contract symbol.
                                return;
                            }

                            var ticker = _symbolUniverse.GetBrokerageSymbol(subscribeSymbol);

                            if (!string.IsNullOrEmpty(ticker))
                            {
                                _level1Port.Subscribe(ticker);
                                Log.Trace("IQFeed.Subscribe(): Subscribe Processed: {0} ({1})", symbol.Value, ticker);
                            }
                            else
                            {
                                Log.Error("IQFeed.Subscribe(): Symbol {0} was not found in IQFeed symbol universe", symbol.Value);
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
        /// Removes the specified configuration
        /// </summary>
        /// <param name="dataConfig">Subscription config to be removed</param>
        public void Unsubscribe(SubscriptionDataConfig dataConfig)
        {
            _subscriptionManager.Unsubscribe(dataConfig);
            _aggregator.Remove(dataConfig);
        }

        /// <summary>
        /// Sets the job we're subscribing for
        /// </summary>
        /// <param name="job">Job we're subscribing for</param>
        public void SetJob(LiveNodePacket job)
        {
        }

        /// <summary>
        /// Removes the specified symbols to the subscription
        /// </summary>
        /// <param name="symbols">The symbols to be removed keyed by SecurityType</param>
        public void Unsubscribe(IEnumerable<Symbol> symbols)
        {
            try
            {
                foreach (var symbol in symbols)
                {
                    lock (_sync)
                    {
                        Log.Trace("IQFeed.Unsubscribe(): " + symbol.ToString());

                        _symbols.Remove(symbol);

                        var subscribeSymbol = symbol;

                        if (symbol.ID.SecurityType == SecurityType.Option && symbol.ID.StrikePrice == 0.0m)
                        {
                            subscribeSymbol = symbol.Underlying;
                            _underlyings.Remove(subscribeSymbol);
                        }

                        var ticker = _symbolUniverse.GetBrokerageSymbol(subscribeSymbol);
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
        /// <param name="parameters">The initialization parameters</param>
        public override void Initialize(HistoryProviderInitializeParameters parameters)
        {
        }

        /// <summary>
        /// Gets the history for the requested securities
        /// </summary>
        /// <param name="requests">The historical data requests</param>
        /// <param name="sliceTimeZone">The time zone used when time stamping the slice instances</param>
        /// <returns>An enumerable of the slices of data covering the span specified in each request</returns>
        public override IEnumerable<Slice> GetHistory(IEnumerable<HistoryRequest> requests, DateTimeZone sliceTimeZone)
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
        public bool IsConnected => _isConnected;

        /// <summary>
        /// Connect to the IQ Feed using supplied username and password information.
        /// </summary>
        private void Connect()
        {
            try
            {
                // Launch the IQ Feed Application:
                Log.Trace("IQFeed.Connect(): Launching client...");

                if (OS.IsWindows)
                {
                    // IQConnect is only supported on Windows
                    var connector = new IQConnect(Config.Get("iqfeed-productName"), "1.0");
                    connector.Launch();
                }

                // Initialise one admin port
                Log.Trace("IQFeed.Connect(): Connecting to admin...");
                _adminPort = new AdminPort();
                _adminPort.Connect();
                _adminPort.SetAutoconnect();
                _adminPort.SetClientStats(false);
                _adminPort.SetClientName("Admin");

                _adminPort.DisconnectedEvent += AdminPortOnDisconnectedEvent;
                _adminPort.ConnectedEvent += AdminPortOnConnectedEvent;

                _symbolUniverse = new IQFeedDataQueueUniverseProvider();

                Log.Trace("IQFeed.Connect(): Connecting to L1 data...");
                _level1Port = new Level1Port(_aggregator, _symbolUniverse);
                _level1Port.Connect();
                _level1Port.SetClientName("Level1");

                Log.Trace("IQFeed.Connect(): Connecting to Historical data...");
                _historyPort = new HistoryPort(_symbolUniverse);
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
        private static bool CanSubscribe(Symbol symbol)
        {
            var market = symbol.ID.Market;
            var securityType = symbol.ID.SecurityType;

            if (symbol.Value.IndexOfInvariant("universe", true) != -1) return false;

            return
                (securityType == SecurityType.Equity && market == Market.USA) ||
                (securityType == SecurityType.Forex && market == Market.FXCM) ||
                (securityType == SecurityType.Option && market == Market.USA) ||
                (securityType == SecurityType.Future);
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
        /// Method returns a collection of Symbols that are available at the data source.
        /// </summary>
        /// <param name="lookupName">String representing the name to lookup</param>
        /// <param name="securityType">Expected security type of the returned symbols (if any)</param>
        /// <param name="includeExpired">Include expired contracts</param>
        /// <param name="securityCurrency">Expected security currency(if any)</param>
        /// <param name="securityExchange">Expected security exchange name(if any)</param>
        /// <returns></returns>
        public IEnumerable<Symbol> LookupSymbols(string lookupName, SecurityType securityType, bool includeExpired, string securityCurrency = null, string securityExchange = null)
        {
            return _symbolUniverse.LookupSymbols(lookupName, securityType, includeExpired, securityCurrency, securityExchange);
        }

        /// <summary>
        /// Returns whether the time can be advanced or not.
        /// </summary>
        /// <param name="securityType">The security type</param>
        /// <returns>true if the time can be advanced</returns>
        public bool CanAdvanceTime(SecurityType securityType)
        {
            return _symbolUniverse.CanAdvanceTime(securityType);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _symbolUniverse.DisposeSafely();
        }
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
        private readonly ConcurrentDictionary<string, double> _prices;
        private readonly ConcurrentDictionary<string, int> _openInterests;
        private readonly IQFeedDataQueueUniverseProvider _symbolUniverse;
        private readonly IDataAggregator _aggregator;
        private int _dataQueueCount;

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

        public Level1Port(IDataAggregator aggregator, IQFeedDataQueueUniverseProvider symbolUniverse)
            : base(80)
        {
            start = DateTime.Now;
            _prices = new ConcurrentDictionary<string, double>();
            _openInterests = new ConcurrentDictionary<string, int>();

            _aggregator = aggregator;
            _symbolUniverse = symbolUniverse;
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
                var ticksPerSecond = count / (DateTime.Now - start).TotalSeconds;
                int dataQueueCount = Interlocked.Exchange(ref _dataQueueCount, 0);
                if (ticksPerSecond > 1000 || dataQueueCount > 31)
                {
                    Log.Trace($"IQFeed.OnSecond(): Ticks/sec: {ticksPerSecond.ToStringInvariant("0000.00")} " +
                        $"Engine.Ticks.Count: {dataQueueCount} CPU%: {OS.CpuUsage.ToStringInvariant("0.0") + "%"}"
                    );
                }

                count = 0;
                start = DateTime.Now;
            };

            _timer.Enabled = true;
        }

        private Symbol GetLeanSymbol(string ticker)
        {
            return _symbolUniverse.GetLeanSymbol(ticker, SecurityType.Base, null);
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

                var symbol = GetLeanSymbol(e.Symbol);
                var split = new Split(symbol, FeedTime, (decimal)referencePrice, (decimal)e.SplitFactor1, SplitType.SplitOccurred);
                Emit(split);
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

            // only accept trade and B/A updates
            if (e.TypeOfUpdate != Level1SummaryUpdateEventArgs.UpdateType.ExtendedTrade
             && e.TypeOfUpdate != Level1SummaryUpdateEventArgs.UpdateType.Trade
             && e.TypeOfUpdate != Level1SummaryUpdateEventArgs.UpdateType.Bid
             && e.TypeOfUpdate != Level1SummaryUpdateEventArgs.UpdateType.Ask) return;

            count++;
            var time = FeedTime;
            var last = (decimal)(e.TypeOfUpdate == Level1SummaryUpdateEventArgs.UpdateType.ExtendedTrade ? e.ExtendedTradingLast : e.Last);

            var symbol = GetLeanSymbol(e.Symbol);

            TickType tradeType;

            switch (symbol.ID.SecurityType)
            {
                // the feed time is in NYC/EDT, convert it into EST
                case SecurityType.Forex:

                    time = FeedTime.ConvertTo(TimeZones.NewYork, TimeZones.EasternStandard);
                    // TypeOfUpdate always equal to UpdateType.Trade for FXCM, but the message contains B/A and last data
                    tradeType = TickType.Quote;

                    break;

                // for all other asset classes we leave it as is (NYC/EDT)
                default:

                    time = FeedTime;
                    tradeType = e.TypeOfUpdate == Level1SummaryUpdateEventArgs.UpdateType.Bid ||
                                e.TypeOfUpdate == Level1SummaryUpdateEventArgs.UpdateType.Ask ?
                                TickType.Quote :
                                TickType.Trade;
                    break;
            }

            var tick = new Tick(time, symbol, last, (decimal)e.Bid, (decimal)e.Ask)
            {
                AskSize = e.AskSize,
                BidSize = e.BidSize,
                Quantity = e.IncrementalVolume,
                TickType = tradeType,
                DataType = MarketDataType.Tick
            };
            Emit(tick);
            _prices[e.Symbol] = e.Last;

            if (symbol.ID.SecurityType == SecurityType.Option || symbol.ID.SecurityType == SecurityType.Future)
            {
                if (!_openInterests.ContainsKey(e.Symbol) || _openInterests[e.Symbol] != e.OpenInterest)
                {
                    var oi = new OpenInterest(time, symbol, e.OpenInterest);
                    Emit(oi);

                    _openInterests[e.Symbol] = e.OpenInterest;
                }
            }
        }

        private void Emit(BaseData tick)
        {
            _aggregator.Update(tick);
            Interlocked.Increment(ref _dataQueueCount);
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

    // this type is expected to be used for exactly one job at a time
    public class HistoryPort : IQLookupHistorySymbolClient
    {
        private bool _inProgress;
        private ConcurrentDictionary<string, HistoryRequest> _requestDataByRequestId;
        private ConcurrentDictionary<string, List<BaseData>> _currentRequest;
        private readonly string DataDirectory = Config.Get("data-directory", "../../../Data");
        private readonly double MaxHistoryRequestMinutes = Config.GetDouble("max-history-minutes", 5);
        private readonly IQFeedDataQueueUniverseProvider _symbolUniverse;

        /// <summary>
        /// ...
        /// </summary>
        public HistoryPort(IQFeedDataQueueUniverseProvider symbolUniverse)
            : base(80)
        {
            _symbolUniverse = symbolUniverse;
            _requestDataByRequestId = new ConcurrentDictionary<string, HistoryRequest>();
            _currentRequest = new ConcurrentDictionary<string, List<BaseData>>();
        }

        /// <summary>
        /// ...
        /// </summary>
        public HistoryPort(IQFeedDataQueueUniverseProvider symbolUniverse, int maxDataPoints, int dataPointsPerSend)
            : this(symbolUniverse)
        {
            MaxDataPoints = maxDataPoints;
            DataPointsPerSend = dataPointsPerSend;
        }

        /// <summary>
        /// Populate request data
        /// </summary>
        public IEnumerable<Slice> ProcessHistoryRequests(HistoryRequest request)
        {
            // skipping universe and canonical symbols
            if (!CanHandle(request.Symbol) ||
                (request.Symbol.ID.SecurityType == SecurityType.Option && request.Symbol.IsCanonical()) ||
                (request.Symbol.ID.SecurityType == SecurityType.Future && request.Symbol.IsCanonical()))
            {
                yield break;
            }

            // Set this process status
            _inProgress = true;

            var ticker = _symbolUniverse.GetBrokerageSymbol(request.Symbol);
            var start = request.StartTimeUtc.ConvertFromUtc(TimeZones.NewYork);
            DateTime? end = request.EndTimeUtc.ConvertFromUtc(TimeZones.NewYork);
            // if we're within a minute of now, don't set the end time
            if (request.EndTimeUtc >= DateTime.UtcNow.AddMinutes(-1))
            {
                end = null;
            }

            Log.Trace($"HistoryPort.ProcessHistoryJob(): Submitting request: {request.Symbol.SecurityType.ToStringInvariant()}-{ticker}: " +
                $"{request.Resolution.ToStringInvariant()} {start.ToStringInvariant()}->{(end ?? DateTime.UtcNow.AddMinutes(-1)).ToStringInvariant()}"
            );

            int id;
            var reqid = string.Empty;

            switch (request.Resolution)
            {
                case Resolution.Tick:
                    id = RequestTickData(ticker, start, end, true);
                    reqid = CreateRequestID(LookupType.REQ_HST_TCK, id);
                    break;
                case Resolution.Daily:
                    id = RequestDailyData(ticker, start, end, true);
                    reqid = CreateRequestID(LookupType.REQ_HST_DWM, id);
                    break;
                default:
                    var interval = new Interval(GetPeriodType(request.Resolution), 1);
                    id = RequestIntervalData(ticker, interval, start, end, true);
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
        /// Returns true if this data provide can handle the specified symbol
        /// </summary>
        /// <param name="symbol">The symbol to be handled</param>
        /// <returns>True if this data provider can get data for the symbol, false otherwise</returns>
        private bool CanHandle(Symbol symbol)
        {
            var market = symbol.ID.Market;
            var securityType = symbol.ID.SecurityType;
            return
                (securityType == SecurityType.Equity && market == Market.USA) ||
                (securityType == SecurityType.Forex && market == Market.FXCM) ||
                (securityType == SecurityType.Option && market == Market.USA) ||
                (securityType == SecurityType.Future && IQFeedDataQueueUniverseProvider.FuturesExchanges.Values.Contains(market));
        }

        /// <summary>
        /// Created new request ID for a given lookup type (tick, intraday bar, daily bar)
        /// </summary>
        /// <param name="lookupType">Lookup type: REQ_HST_TCK (tick), REQ_HST_DWM (daily) or REQ_HST_INT (intraday resolutions)</param>
        /// <param name="id">Sequential identifier</param>
        /// <returns></returns>
        private static string CreateRequestID(LookupType lookupType, int id)
        {
            return lookupType + id.ToStringInvariant("0000000");
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
            var isEquity = requestData.Symbol.SecurityType == SecurityType.Equity;
            try
            {
                switch (e.Type)
                {
                    case LookupType.REQ_HST_TCK:
                        var t = (LookupTickEventArgs)e;
                        var time = isEquity ? t.DateTimeStamp : t.DateTimeStamp.ConvertTo(TimeZones.NewYork, TimeZones.EasternStandard);
                        return new Tick(time, requestData.Symbol, (decimal)t.Last, (decimal)t.Bid, (decimal)t.Ask) { Quantity = t.LastSize };
                    case LookupType.REQ_HST_INT:
                        var i = (LookupIntervalEventArgs)e;
                        if (i.DateTimeStamp == DateTime.MinValue) return null;
                        var istartTime = i.DateTimeStamp - requestData.Resolution.ToTimeSpan();
                        if (!isEquity) istartTime = istartTime.ConvertTo(TimeZones.NewYork, TimeZones.EasternStandard);
                        return new TradeBar(istartTime, requestData.Symbol, (decimal)i.Open, (decimal)i.High, (decimal)i.Low, (decimal)i.Close, i.PeriodVolume);
                    case LookupType.REQ_HST_DWM:
                        var d = (LookupDayWeekMonthEventArgs)e;
                        if (d.DateTimeStamp == DateTime.MinValue) return null;
                        var dstartTime = d.DateTimeStamp.Date;
                        if (!isEquity) dstartTime = dstartTime.ConvertTo(TimeZones.NewYork, TimeZones.EasternStandard);
                        return new TradeBar(dstartTime, requestData.Symbol, (decimal)d.Open, (decimal)d.High, (decimal)d.Low, (decimal)d.Close, d.PeriodVolume, requestData.Resolution.ToTimeSpan());

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
    }
}
