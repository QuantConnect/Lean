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
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using NodaTime;
using NodaTime.TimeZones;
using QuantConnect.Benchmarks;
using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Notifications;
using QuantConnect.Orders;
using QuantConnect.Parameters;
using QuantConnect.Scheduling;
using QuantConnect.Securities;
using QuantConnect.Securities.Cfd;
using QuantConnect.Securities.Equity;
using QuantConnect.Securities.Forex;
using QuantConnect.Securities.Option;
using QuantConnect.Statistics;
using QuantConnect.Util;
using System.Collections.Concurrent;
using QuantConnect.Securities.Future;
using QuantConnect.Securities.Crypto;
using System.Net;
using QuantConnect.Algorithm.Framework.Alphas;

namespace QuantConnect.Algorithm
{
    /// <summary>
    /// QC Algorithm Base Class - Handle the basic requirements of a trading algorithm,
    /// allowing user to focus on event methods. The QCAlgorithm class implements Portfolio,
    /// Securities, Transactions and Data Subscription Management.
    /// </summary>
    public partial class QCAlgorithm : MarshalByRefObject, IAlgorithm
    {
        private readonly TimeKeeper _timeKeeper;
        private LocalTimeKeeper _localTimeKeeper;

        private DateTime _startDate;   //Default start and end dates.
        private DateTime _endDate;     //Default end to yesterday
        private RunMode _runMode = RunMode.Series;
        private bool _locked;
        private bool _liveMode;
        private string _algorithmId = "";
        private ConcurrentQueue<string> _debugMessages = new ConcurrentQueue<string>();
        private ConcurrentQueue<string> _logMessages = new ConcurrentQueue<string>();
        private ConcurrentQueue<string> _errorMessages = new ConcurrentQueue<string>();

        //Error tracking to avoid message flooding:
        private string _previousDebugMessage = "";
        private string _previousErrorMessage = "";
        private readonly SymbolPropertiesDatabase _symbolPropertiesDatabase;

        /// <summary>
        /// Gets the market hours database in use by this algorithm
        /// </summary>
        protected MarketHoursDatabase MarketHoursDatabase { get; }

        // used for calling through to void OnData(Slice) if no override specified
        private bool _checkedForOnDataSlice;
        private Action<Slice> _onDataSlice;

        // set by SetBenchmark helper API functions
        private Symbol _benchmarkSymbol = QuantConnect.Symbol.Empty;

        // flips to true when the user
        private bool _userSetSecurityInitializer = false;

        // warmup resolution variables
        private TimeSpan? _warmupTimeSpan;
        private int? _warmupBarCount;
        private Resolution? _warmupResolution;
        private Dictionary<string, string> _parameters = new Dictionary<string, string>();

        /// <summary>
        /// QCAlgorithm Base Class Constructor - Initialize the underlying QCAlgorithm components.
        /// QCAlgorithm manages the transactions, portfolio, charting and security subscriptions for the users algorithms.
        /// </summary>
        public QCAlgorithm()
        {
            Name = GetType().Name;
            Status = AlgorithmStatus.Running;

            // AlgorithmManager will flip this when we're caught up with realtime
            IsWarmingUp = true;

            //Initialise the Algorithm Helper Classes:
            //- Note - ideally these wouldn't be here, but because of the DLL we need to make the classes shared across
            //  the Worker & Algorithm, limiting ability to do anything else.

            //Initialise Start and End Dates:
            _startDate = new DateTime(1998, 01, 01);
            _endDate = DateTime.Now.AddDays(-1);

            // intialize our time keeper with only new york
            _timeKeeper = new TimeKeeper(_startDate, new[] { TimeZones.NewYork });
            // set our local time zone
            _localTimeKeeper = _timeKeeper.GetLocalTimeKeeper(TimeZones.NewYork);

            Settings = new AlgorithmSettings();

            //Initialise Data Manager
            SubscriptionManager = new SubscriptionManager(Settings, _timeKeeper);

            Securities = new SecurityManager(_timeKeeper);
            Transactions = new SecurityTransactionManager(this, Securities);
            Portfolio = new SecurityPortfolioManager(Securities, Transactions, DefaultOrderProperties);
            BrokerageModel = new DefaultBrokerageModel();
            Notify = new NotificationManager(false); // Notification manager defaults to disabled.

            //Initialise Algorithm RunMode to Series - Parallel Mode deprecated:
            _runMode = RunMode.Series;

            //Initialise to unlocked:
            _locked = false;

            // get exchange hours loaded from the market-hours-database.csv in /Data/market-hours
            MarketHoursDatabase = MarketHoursDatabase.FromDataFolder();

            // get symbol properties loaded from the symbol-properties-database.csv in /Data/symbol-properties
            _symbolPropertiesDatabase = SymbolPropertiesDatabase.FromDataFolder();

            // universe selection
            UniverseManager = new UniverseManager();
            Universe = new UniverseDefinitions(this);
            UniverseSettings = new UniverseSettings(Resolution.Minute, 2m, true, false, TimeSpan.FromDays(1));

            // initialize our scheduler, this acts as a liason to the real time handler
            Schedule = new ScheduleManager(Securities, TimeZone);

            // initialize the trade builder
            TradeBuilder = new TradeBuilder(FillGroupingMethod.FillToFill, FillMatchingMethod.FIFO);

            SecurityInitializer = new BrokerageModelSecurityInitializer(new DefaultBrokerageModel(AccountType.Margin), SecuritySeeder.Null);

            CandlestickPatterns = new CandlestickPatterns(this);

            // initialize trading calendar
            TradingCalendar = new TradingCalendar(Securities, MarketHoursDatabase);

            OptionChainProvider = new EmptyOptionChainProvider();
            FutureChainProvider = new EmptyFutureChainProvider();
        }

        /// <summary>
        /// Event fired when the algorithm generates insights
        /// </summary>
        public event AlgorithmEvent<GeneratedInsightsCollection> InsightsGenerated;

        /// <summary>
        /// Security collection is an array of the security objects such as Equities and FOREX. Securities data
        /// manages the properties of tradeable assets such as price, open and close time and holdings information.
        /// </summary>
        public SecurityManager Securities
        {
            get;
            set;
        }

        /// <summary>
        /// Portfolio object provieds easy access to the underlying security-holding properties; summed together in a way to make them useful.
        /// This saves the user time by providing common portfolio requests in a single
        /// </summary>
        public SecurityPortfolioManager Portfolio
        {
            get;
            set;
        }

        /// <summary>
        /// Generic Data Manager - Required for compiling all data feeds in order, and passing them into algorithm event methods.
        /// The subscription manager contains a list of the data feed's we're subscribed to and properties of each data feed.
        /// </summary>
        public SubscriptionManager SubscriptionManager
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the brokerage model - used to model interactions with specific brokerages.
        /// </summary>
        public IBrokerageModel BrokerageModel
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the brokerage message handler used to decide what to do
        /// with each message sent from the brokerage
        /// </summary>
        public IBrokerageMessageHandler BrokerageMessageHandler
        {
            get;
            set;
        }

        /// <summary>
        /// Notification Manager for Sending Live Runtime Notifications to users about important events.
        /// </summary>
        public NotificationManager Notify
        {
            get;
            set;
        }

        /// <summary>
        /// Gets schedule manager for adding/removing scheduled events
        /// </summary>
        public ScheduleManager Schedule
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets the current status of the algorithm
        /// </summary>
        public AlgorithmStatus Status
        {
            get;
            set;
        }

        /// <summary>
        /// Returns false since algorithms derived from this do not use the framework
        /// </summary>
        public virtual bool IsFrameworkAlgorithm
        {
            get { return false; }
        }

        /// <summary>
        /// Gets an instance that is to be used to initialize newly created securities.
        /// </summary>
        public ISecurityInitializer SecurityInitializer
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the Trade Builder to generate trades from executions
        /// </summary>
        public ITradeBuilder TradeBuilder
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets an instance to access the candlestick pattern helper methods
        /// </summary>
        public CandlestickPatterns CandlestickPatterns
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the date rules helper object to make specifying dates for events easier
        /// </summary>
        public DateRules DateRules
        {
            get { return Schedule.DateRules; }
        }

        /// <summary>
        /// Gets the time rules helper object to make specifying times for events easier
        /// </summary>
        public TimeRules TimeRules
        {
            get { return Schedule.TimeRules; }
        }

        /// <summary>
        /// Gets trading calendar populated with trading events
        /// </summary>
        public TradingCalendar TradingCalendar
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the user settings for the algorithm
        /// </summary>
        public AlgorithmSettings Settings
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the option chain provider, used to get the list of option contracts for an underlying symbol
        /// </summary>
        public IOptionChainProvider OptionChainProvider { get; private set; }

        /// <summary>
        /// Gets the future chain provider, used to get the list of future contracts for an underlying symbol
        /// </summary>
        public IFutureChainProvider FutureChainProvider { get; private set; }

        /// <summary>
        /// Gets the default order properties
        /// </summary>
        public IOrderProperties DefaultOrderProperties { get; set; }

        /// <summary>
        /// Public name for the algorithm as automatically generated by the IDE. Intended for helping distinguish logs by noting
        /// the algorithm-id.
        /// </summary>
        /// <seealso cref="AlgorithmId"/>
        public string Name
        {
            get;
            set;
        }

        /// <summary>
        /// Read-only value for current time frontier of the algorithm in terms of the <see cref="TimeZone"/>
        /// </summary>
        /// <remarks>During backtesting this is primarily sourced from the data feed. During live trading the time is updated from the system clock.</remarks>
        public DateTime Time
        {
            get { return _localTimeKeeper.LocalTime; }
        }

        /// <summary>
        /// Current date/time in UTC.
        /// </summary>
        public DateTime UtcTime
        {
            get { return _timeKeeper.UtcTime; }
        }

        /// <summary>
        /// Gets the time zone used for the <see cref="Time"/> property. The default value
        /// is <see cref="TimeZones.NewYork"/>
        /// </summary>
        public DateTimeZone TimeZone
        {
            get { return _localTimeKeeper.TimeZone; }
        }

        /// <summary>
        /// Value of the user set start-date from the backtest.
        /// </summary>
        /// <remarks>This property is set with SetStartDate() and defaults to the earliest QuantConnect data available - Jan 1st 1998. It is ignored during live trading </remarks>
        /// <seealso cref="SetStartDate(DateTime)"/>
        public DateTime StartDate
        {
            get
            {
                return _startDate;
            }
        }

        /// <summary>
        /// Value of the user set start-date from the backtest. Controls the period of the backtest.
        /// </summary>
        /// <remarks> This property is set with SetEndDate() and defaults to today. It is ignored during live trading.</remarks>
        /// <seealso cref="SetEndDate(DateTime)"/>
        public DateTime EndDate
        {
            get
            {
                return _endDate;
            }
        }

        /// <summary>
        /// Algorithm Id for this backtest or live algorithm.
        /// </summary>
        /// <remarks>A unique identifier for </remarks>
        public string AlgorithmId
        {
            get
            {
                return _algorithmId;
            }
        }

        /// <summary>
        /// Control the server setup run style for the backtest: Automatic, Parallel or Series.
        /// </summary>
        /// <remark>
        ///     Series mode runs all days through one computer, allowing memory of the previous days.
        ///     Parallel mode runs all days separately which maximises speed but gives no memory of a previous day trading.
        /// </remark>
        /// <obsolete>The RunMode enum propert is now obsolete. All algorithms will default to RunMode.Series for series backtests.</obsolete>
        [Obsolete("The RunMode enum propert is now obsolete. All algorithms will default to RunMode.Series for series backtests.")]
        public RunMode RunMode
        {
            get
            {
                return _runMode;
            }
        }

        /// <summary>
        /// Boolean property indicating the algorithm is currently running in live mode.
        /// </summary>
        /// <remarks>Intended for use where certain behaviors will be enabled while the algorithm is trading live: such as notification emails, or displaying runtime statistics.</remarks>
        public bool LiveMode
        {
            get
            {
                return _liveMode;
            }
        }

        /// <summary>
        /// Storage for debugging messages before the event handler has passed control back to the Lean Engine.
        /// </summary>
        /// <seealso cref="Debug(string)"/>
        public ConcurrentQueue<string> DebugMessages
        {
            get
            {
                return _debugMessages;
            }
            set
            {
                _debugMessages = value;
            }
        }

        /// <summary>
        /// Storage for log messages before the event handlers have passed control back to the Lean Engine.
        /// </summary>
        /// <seealso cref="Log(string)"/>
        public ConcurrentQueue<string> LogMessages
        {
            get
            {
                return _logMessages;
            }
            set
            {
                _logMessages = value;
            }
        }

        /// <summary>
        /// Gets the run time error from the algorithm, or null if none was encountered.
        /// </summary>
        public Exception RunTimeError { get; set; }

        /// <summary>
        /// List of error messages generated by the user's code calling the "Error" function.
        /// </summary>
        /// <remarks>This method is best used within a try-catch bracket to handle any runtime errors from a user algorithm.</remarks>
        /// <see cref="Error(string)"/>
        public ConcurrentQueue<string> ErrorMessages
        {
            get
            {
                return _errorMessages;
            }
            set
            {
                _errorMessages = value;
            }
        }

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        /// <seealso cref="SetStartDate(DateTime)"/>
        /// <seealso cref="SetEndDate(DateTime)"/>
        /// <seealso cref="SetCash(decimal)"/>
        public virtual void Initialize()
        {
            //Setup Required Data
            throw new NotImplementedException("Please override the Initialize() method");
        }

        /// <summary>
        /// Called by setup handlers after Initialize and allows the algorithm a chance to organize
        /// the data gather in the Initialize method
        /// </summary>
        public virtual void PostInitialize()
        {
            // if the benchmark hasn't been set yet, set it
            if (Benchmark == null)
            {
                if (_benchmarkSymbol != null)
                {
                    // if the requested benchmark symbol wasn't already added, then add it now
                    // we do a simple compare here for simplicity, also it avoids confusion over
                    // the desired market.
                    Security security;
                    if (!Securities.TryGetValue(_benchmarkSymbol, out security))
                    {
                        // add the security as an internal feed so the algorithm doesn't receive the data
                        security = CreateBenchmarkSecurity();
                        AddToUserDefinedUniverse(security);
                    }

                    // just return the current price
                    Benchmark = new SecurityBenchmark(security);
                }
                else
                {
                    var start = StartDate;
                    var startingCapital = Portfolio.TotalPortfolioValue;
                    Benchmark = new FuncBenchmark(dt =>
                    {
                        var years = (dt - start).TotalDays / 365.25;
                        return startingCapital * (decimal) Math.Exp(0.02 * years);
                    });
                }
            }

            // perform end of time step checks, such as enforcing underlying securities are in raw data mode
            OnEndOfTimeStep();
        }

        /// <summary>
        /// Called when the algorithm has completed initialization and warm up.
        /// </summary>
        public virtual void OnWarmupFinished()
        {
        }

        /// <summary>
        /// Gets the parameter with the specified name. If a parameter
        /// with the specified name does not exist, null is returned
        /// </summary>
        /// <param name="name">The name of the parameter to get</param>
        /// <returns>The value of the specified parameter, or null if not found</returns>
        public string GetParameter(string name)
        {
            string value;
            return _parameters.TryGetValue(name, out value) ? value : null;
        }

        /// <summary>
        /// Sets the parameters from the dictionary
        /// </summary>
        /// <param name="parameters">Dictionary containing the parameter names to values</param>
        public void SetParameters(Dictionary<string, string> parameters)
        {
            // save off a copy and try to apply the parameters
            _parameters = parameters.ToDictionary();
            try
            {
                ParameterAttribute.ApplyAttributes(parameters, this);
            }
            catch (Exception err)
            {
                Error("Error applying parameter values: " + err.Message);
            }
        }

        /// <summary>
        /// Set the available data feeds in the <see cref="SecurityManager"/>
        /// </summary>
        /// <param name="availableDataTypes">The different <see cref="TickType"/> each <see cref="Security"/> supports</param>
        public void SetAvailableDataTypes(Dictionary<SecurityType, List<TickType>> availableDataTypes)
        {
            foreach (var dataFeed in availableDataTypes)
            {
                SubscriptionManager.AvailableDataTypes[dataFeed.Key] = dataFeed.Value;
            }
        }

        /// <summary>
        /// Sets the security initializer, used to initialize/configure securities after creation
        /// </summary>
        /// <param name="securityInitializer">The security initializer</param>
        public void SetSecurityInitializer(ISecurityInitializer securityInitializer)
        {
            // this flag will prevent calls to SetBrokerageModel from overwriting this initializer
            _userSetSecurityInitializer = true;
            SecurityInitializer = securityInitializer;
        }

        /// <summary>
        /// Sets the security initializer function, used to initialize/configure securities after creation
        /// </summary>
        /// <param name="securityInitializer">The security initializer function</param>
        [Obsolete("This method is deprecated. Please use this overload: SetSecurityInitializer(Action<Security> securityInitializer)")]
        public void SetSecurityInitializer(Action<Security, bool> securityInitializer)
        {
            SetSecurityInitializer(new FuncSecurityInitializer(security => securityInitializer(security, false)));
        }

        /// <summary>
        /// Sets the security initializer function, used to initialize/configure securities after creation
        /// </summary>
        /// <param name="securityInitializer">The security initializer function</param>
        public void SetSecurityInitializer(Action<Security> securityInitializer)
        {
            SetSecurityInitializer(new FuncSecurityInitializer(securityInitializer));
        }

        /// <summary>
        /// Sets the option chain provider, used to get the list of option contracts for an underlying symbol
        /// </summary>
        /// <param name="optionChainProvider">The option chain provider</param>
        public void SetOptionChainProvider(IOptionChainProvider optionChainProvider)
        {
            OptionChainProvider = optionChainProvider;
        }

        /// <summary>
        /// Sets the future chain provider, used to get the list of future contracts for an underlying symbol
        /// </summary>
        /// <param name="futureChainProvider">The future chain provider</param>
        public void SetFutureChainProvider(IFutureChainProvider futureChainProvider)
        {
            FutureChainProvider = futureChainProvider;
        }

        /// <summary>
        /// Event - v3.0 DATA EVENT HANDLER: (Pattern) Basic template for user to override for receiving all subscription data in a single event
        /// </summary>
        /// <code>
        /// TradeBars bars = slice.Bars;
        /// Ticks ticks = slice.Ticks;
        /// TradeBar spy = slice["SPY"];
        /// List{Tick} aaplTicks = slice["AAPL"]
        /// Quandl oil = slice["OIL"]
        /// dynamic anySymbol = slice[symbol];
        /// DataDictionary{Quandl} allQuandlData = slice.Get{Quand}
        /// Quandl oil = slice.Get{Quandl}("OIL")
        /// </code>
        /// <param name="slice">The current slice of data keyed by symbol string</param>
        public virtual void OnData(Slice slice)
        {
            // as a default implementation, let's look for and call OnData(Slice) just in case a user forgot to use the override keyword
            if (!_checkedForOnDataSlice)
            {
                _checkedForOnDataSlice = true;

                var method = GetType().GetMethods()
                    .Where(x => x.Name == "OnData")
                    .Where(x => x.DeclaringType != typeof(QCAlgorithm))
                    .Where(x => x.GetParameters().Length == 1)
                    .FirstOrDefault(x => x.GetParameters()[0].ParameterType == typeof (Slice));

                if (method == null)
                {
                    return;
                }

                var self = Expression.Constant(this);
                var parameter = Expression.Parameter(typeof (Slice), "data");
                var call = Expression.Call(self, method, parameter);
                var lambda = Expression.Lambda<Action<Slice>>(call, parameter);
                _onDataSlice = lambda.Compile();
            }
            // if we have it, then invoke it
            if (_onDataSlice != null)
            {
                _onDataSlice(slice);
            }
        }

        /// <summary>
        /// Used to send data updates to algorithm framework models
        /// </summary>
        /// <param name="slice">The current data slice</param>
        public virtual void OnFrameworkData(Slice slice)
        {
            // NOP - overriden by QCAlgorithmFramework
        }

        /// <summary>
        /// Event fired each time the we add/remove securities from the data feed
        /// </summary>
        /// <param name="changes">Security additions/removals for this time step</param>
        public virtual void OnSecuritiesChanged(SecurityChanges changes)
        {
        }

        /// <summary>
        /// Used to send security changes to algorithm framework models
        /// </summary>
        /// <param name="changes">Security additions/removals for this time step</param>
        public virtual void OnFrameworkSecuritiesChanged(SecurityChanges changes)
        {
            // NOP - overriden by QCAlgorithmFramework
        }

        // <summary>
        // Event - v2.0 TRADEBAR EVENT HANDLER: (Pattern) Basic template for user to override when requesting tradebar data.
        // </summary>
        // <param name="data"></param>
        //public void OnData(TradeBars data)
        //{
        //
        //}

        // <summary>
        // Event - v2.0 QUOTEBAR EVENT HANDLER: (Pattern) Basic template for user to override when requesting quotebar data.
        // </summary>
        // <param name="data"></param>
        //public void OnData(QuoteBars data)
        //{
        //
        //}

        // <summary>
        // Event - v2.0 TICK EVENT HANDLER: (Pattern) Basic template for user to override when requesting tick data.
        // </summary>
        // <param name="data">List of Tick Data</param>
        //public void OnData(Ticks data)
        //{
        //
        //}

        // <summary>
        // Event - v2.0 OPTIONCHAIN EVENT HANDLER: (Pattern) Basic template for user to override when requesting option data.
        // </summary>
        // <param name="data">List of Tick Data</param>
        //public void OnData(OptionChains data)
        //{
        //
        //}

        // <summary>
        // Event - v2.0 SPLIT EVENT HANDLER: (Pattern) Basic template for user to override when inspecting split data.
        // </summary>
        // <param name="data">IDictionary of Split Data Keyed by Symbol String</param>
        //public void OnData(Splits data)
        //{
        //
        //}

        // <summary>
        // Event - v2.0 DIVIDEND EVENT HANDLER: (Pattern) Basic template for user to override when inspecting dividend data
        // </summary>
        // <param name="data">IDictionary of Dividend Data Keyed by Symbol String</param>
        //public void OnData(Dividends data)
        //{
        //
        //}

        // <summary>
        // Event - v2.0 DELISTING EVENT HANDLER: (Pattern) Basic template for user to override when inspecting delisting data
        // </summary>
        // <param name="data">IDictionary of Delisting Data Keyed by Symbol String</param>
        //public void OnData(Delistings data)

        // <summary>
        // Event - v2.0 SYMBOL CHANGED EVENT HANDLER: (Pattern) Basic template for user to override when inspecting symbol changed data
        // </summary>
        // <param name="data">IDictionary of SymbolChangedEvent Data Keyed by Symbol String</param>
        //public void OnData(SymbolChangedEvents data)

        /// <summary>
        /// Margin call event handler. This method is called right before the margin call orders are placed in the market.
        /// </summary>
        /// <param name="requests">The orders to be executed to bring this algorithm within margin limits</param>
        public virtual void OnMarginCall(List<SubmitOrderRequest> requests)
        {
        }

        /// <summary>
        /// Margin call warning event handler. This method is called when Portfolio.MarginRemaining is under 5% of your Portfolio.TotalPortfolioValue
        /// </summary>
        public virtual void OnMarginCallWarning()
        {
        }

        /// <summary>
        /// End of a trading day event handler. This method is called at the end of the algorithm day (or multiple times if trading multiple assets).
        /// </summary>
        /// <remarks>Method is called 10 minutes before closing to allow user to close out position.</remarks>
        public virtual void OnEndOfDay()
        {

        }

        /// <summary>
        /// End of a trading day event handler. This method is called at the end of the algorithm day (or multiple times if trading multiple assets).
        /// </summary>
        /// <remarks>
        /// This method is left for backwards compatibility and is invoked via <see cref="OnEndOfDay(Symbol)"/>, if that method is
        /// override then this method will not be called without a called to base.OnEndOfDay(string)
        /// </remarks>
        /// <param name="symbol">Asset symbol for this end of day event. Forex and equities have different closing hours.</param>
        public virtual void OnEndOfDay(string symbol)
        {
        }

        /// <summary>
        /// End of a trading day event handler. This method is called at the end of the algorithm day (or multiple times if trading multiple assets).
        /// </summary>
        /// <param name="symbol">Asset symbol for this end of day event. Forex and equities have different closing hours.</param>
        public virtual void OnEndOfDay(Symbol symbol)
        {
            OnEndOfDay(symbol.ToString());
        }

        /// <summary>
        /// End of algorithm run event handler. This method is called at the end of a backtest or live trading operation. Intended for closing out logs.
        /// </summary>
        public virtual void OnEndOfAlgorithm()
        {

        }

        /// <summary>
        /// Order fill event handler. On an order fill update the resulting information is passed to this method.
        /// </summary>
        /// <param name="orderEvent">Order event details containing details of the evemts</param>
        /// <remarks>This method can be called asynchronously and so should only be used by seasoned C# experts. Ensure you use proper locks on thread-unsafe objects</remarks>
        public virtual void OnOrderEvent(OrderEvent orderEvent)
        {

        }

        /// <summary>
        /// Option assignment event handler. On an option assignment event for short legs the resulting information is passed to this method.
        /// </summary>
        /// <param name="assignmentEvent">Option exercise event details containing details of the assignment</param>
        /// <remarks>This method can be called asynchronously and so should only be used by seasoned C# experts. Ensure you use proper locks on thread-unsafe objects</remarks>
        public virtual void OnAssignmentOrderEvent(OrderEvent assignmentEvent)
        {

        }

        /// <summary>
        /// Brokerage message event handler. This method is called for all types of brokerage messages.
        /// </summary>
        public virtual void OnBrokerageMessage(BrokerageMessageEvent messageEvent)
        {

        }

        /// <summary>
        /// Brokerage disconnected event handler. This method is called when the brokerage connection is lost.
        /// </summary>
        public virtual void OnBrokerageDisconnect()
        {

        }

        /// <summary>
        /// Brokerage reconnected event handler. This method is called when the brokerage connection is restored after a disconnection.
        /// </summary>
        public virtual void OnBrokerageReconnect()
        {

        }

        /// <summary>
        /// Update the internal algorithm time frontier.
        /// </summary>
        /// <remarks>For internal use only to advance time.</remarks>
        /// <param name="frontier">Current datetime.</param>
        public void SetDateTime(DateTime frontier)
        {
            _timeKeeper.SetUtcDateTime(frontier);
        }

        /// <summary>
        /// Sets the time zone of the <see cref="Time"/> property in the algorithm
        /// </summary>
        /// <param name="timeZone">The desired time zone</param>
        public void SetTimeZone(string timeZone)
        {
            DateTimeZone tz;
            try
            {
                tz = DateTimeZoneProviders.Tzdb[timeZone];
            }
            catch (DateTimeZoneNotFoundException)
            {
                throw new ArgumentException(string.Format("TimeZone with id '{0}' was not found. For a complete list of time zones please visit: http://en.wikipedia.org/wiki/List_of_tz_database_time_zones", timeZone));
            }

            SetTimeZone(tz);
        }

        /// <summary>
        /// Sets the time zone of the <see cref="Time"/> property in the algorithm
        /// </summary>
        /// <param name="timeZone">The desired time zone</param>
        public void SetTimeZone(DateTimeZone timeZone)
        {
            if (_locked)
            {
                throw new Exception("Algorithm.SetTimeZone(): Cannot change time zone after algorithm running.");
            }

            if (timeZone == null) throw new ArgumentNullException("timeZone");
            _timeKeeper.AddTimeZone(timeZone);
            _localTimeKeeper = _timeKeeper.GetLocalTimeKeeper(timeZone);

            // the time rules need to know the default time zone as well
            TimeRules.SetDefaultTimeZone(timeZone);

            // reset the current time according to the time zone
            SetDateTime(_startDate.ConvertToUtc(TimeZone));
        }

        /// <summary>
        /// Set the RunMode for the Servers. If you are running an overnight algorithm, you must select series.
        /// Automatic will analyse the selected data, and if you selected only minute data we'll select series for you.
        /// </summary>
        /// <obsolete>This method is now obsolete and has no replacement. All algorithms now run in Series mode.</obsolete>
        /// <param name="mode">Enum RunMode with options Series, Parallel or Automatic. Automatic scans your requested symbols and resolutions and makes a decision on the fastest analysis</param>
        [Obsolete("This method is now obsolete and has no replacement. All algorithms now run in Series mode.")]
        public void SetRunMode(RunMode mode)
        {
            if (mode != RunMode.Parallel) return;
            Debug("Algorithm.SetRunMode(): RunMode-Parallel Type has been deprecated. Series analysis selected instead");
        }

        /// <summary>
        /// Sets the brokerage to emulate in backtesting or paper trading.
        /// This can be used for brokerages that have been implemented in LEAN
        /// </summary>
        /// <param name="brokerage">The brokerage to emulate</param>
        /// <param name="accountType">The account type (Cash or Margin)</param>
        public void SetBrokerageModel(BrokerageName brokerage, AccountType accountType = AccountType.Margin)
        {
            SetBrokerageModel(Brokerages.BrokerageModel.Create(brokerage, accountType));
        }

        /// <summary>
        /// Sets the brokerage to emulate in backtesting or paper trading.
        /// This can be used to set a custom brokerage model.
        /// </summary>
        /// <param name="model">The brokerage model to use</param>
        public void SetBrokerageModel(IBrokerageModel model)
        {
            BrokerageModel = model;
            if (!_userSetSecurityInitializer)
            {
                // purposefully use the direct setter vs Set method so we don't flip the switch :/
                SecurityInitializer = new BrokerageModelSecurityInitializer(model, SecuritySeeder.Null);

                // update models on securities added earlier (before SetBrokerageModel is called)
                foreach (var kvp in Securities)
                {
                    var security = kvp.Value;

                    // save the existing leverage specified in AddSecurity,
                    // if Leverage needs to be set in a SecurityInitializer,
                    // SetSecurityInitializer must be called before SetBrokerageModel
                    var leverage = security.Leverage;

                    SecurityInitializer.Initialize(security);

                    // restore the saved leverage
                    security.SetLeverage(leverage);
                }
            }
        }

        /// <summary>
        /// Sets the implementation used to handle messages from the brokerage.
        /// The default implementation will forward messages to debug or error
        /// and when a <see cref="BrokerageMessageType.Error"/> occurs, the algorithm
        /// is stopped.
        /// </summary>
        /// <param name="handler">The message handler to use</param>
        public void SetBrokerageMessageHandler(IBrokerageMessageHandler handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException("handler");
            }

            BrokerageMessageHandler = handler;
        }

        /// <summary>
        /// Sets the benchmark used for computing statistics of the algorithm to the specified symbol
        /// </summary>
        /// <param name="symbol">symbol to use as the benchmark</param>
        /// <param name="securityType">Is the symbol an equity, forex, base, etc. Default SecurityType.Equity</param>
        /// <remarks>
        /// Must use symbol that is available to the trade engine in your data store(not strictly enforced)
        /// </remarks>
        [Obsolete("Symbol implicit operator to string is provided for algorithm use only.")]
        public void SetBenchmark(SecurityType securityType, string symbol)
        {
            string market = Market.USA;

            this.BrokerageModel.DefaultMarkets.TryGetValue(securityType, out market);

            _benchmarkSymbol = QuantConnect.Symbol.Create(symbol, securityType, market);
        }

        /// <summary>
        /// Sets the benchmark used for computing statistics of the algorithm to the specified symbol, defaulting to SecurityType.Equity
        /// if the symbol doesn't exist in the algorithm
        /// </summary>
        /// <param name="symbol">symbol to use as the benchmark</param>
        /// <remarks>
        /// Overload to accept symbol without passing SecurityType. If symbol is in portfolio it will use that SecurityType, otherwise will default to SecurityType.Equity
        /// </remarks>
        public void SetBenchmark(string symbol)
        {
            // check existence
            symbol = symbol.ToUpper();
            var security = Securities.FirstOrDefault(x => x.Key.Value == symbol).Value;
            _benchmarkSymbol = security == null
                ? QuantConnect.Symbol.Create(symbol, SecurityType.Equity, Market.USA)
                : security.Symbol;
        }

        /// <summary>
        /// Sets the benchmark used for computing statistics of the algorithm to the specified symbol
        /// </summary>
        /// <param name="symbol">symbol to use as the benchmark</param>
        public void SetBenchmark(Symbol symbol)
        {
            _benchmarkSymbol = symbol;
        }

        /// <summary>
        /// Sets the specified function as the benchmark, this function provides the value of
        /// the benchmark at each date/time requested
        /// </summary>
        /// <param name="benchmark">The benchmark producing function</param>
        public void SetBenchmark(Func<DateTime, decimal> benchmark)
        {
            Benchmark = new FuncBenchmark(benchmark);
        }

        /// <summary>
        /// Benchmark
        /// </summary>
        /// <remarks>Use Benchmark to override default symbol based benchmark, and create your own benchmark. For example a custom moving average benchmark </remarks>
        ///
        public IBenchmark Benchmark
        {
            get;
            private set;
        }

        /// <summary>
        /// Set initial cash for the strategy while backtesting. During live mode this value is ignored
        /// and replaced with the actual cash of your brokerage account.
        /// </summary>
        /// <param name="startingCash">Starting cash for the strategy backtest</param>
        /// <remarks>Alias of SetCash(decimal)</remarks>
        public void SetCash(double startingCash)
        {
            SetCash((decimal)startingCash);
        }

        /// <summary>
        /// Set initial cash for the strategy while backtesting. During live mode this value is ignored
        /// and replaced with the actual cash of your brokerage account.
        /// </summary>
        /// <param name="startingCash">Starting cash for the strategy backtest</param>
        /// <remarks>Alias of SetCash(decimal)</remarks>
        public void SetCash(int startingCash)
        {
            SetCash((decimal)startingCash);
        }

        /// <summary>
        /// Set initial cash for the strategy while backtesting. During live mode this value is ignored
        /// and replaced with the actual cash of your brokerage account.
        /// </summary>
        /// <param name="startingCash">Starting cash for the strategy backtest</param>
        public void SetCash(decimal startingCash)
        {
            if (!_locked)
            {
                Portfolio.SetCash(startingCash);
            }
            else
            {
                throw new Exception("Algorithm.SetCash(): Cannot change cash available after algorithm initialized.");
            }
        }

        /// <summary>
        /// Set the cash for the specified symbol
        /// </summary>
        /// <param name="symbol">The cash symbol to set</param>
        /// <param name="startingCash">Decimal cash value of portfolio</param>
        /// <param name="conversionRate">The current conversion rate for the</param>
        public void SetCash(string symbol, decimal startingCash, decimal conversionRate)
        {
            if (!_locked)
            {
                Portfolio.SetCash(symbol, startingCash, conversionRate);
            }
            else
            {
                throw new Exception("Algorithm.SetCash(): Cannot change cash available after algorithm initialized.");
            }
        }

        /// <summary>
        /// Set the start date for backtest.
        /// </summary>
        /// <param name="day">Int starting date 1-30</param>
        /// <param name="month">Int month starting date</param>
        /// <param name="year">Int year starting date</param>
        /// <remarks>
        ///     Wrapper for SetStartDate(DateTime).
        ///     Must be less than end date.
        ///     Ignored in live trading mode.
        /// </remarks>
        public void SetStartDate(int year, int month, int day)
        {
            try
            {
                var start = new DateTime(year, month, day);

                // We really just want the date of the start, so it's 12am of the requested day (first moment of the day)
                start = start.Date;

                SetStartDate(start);
            }
            catch (Exception err)
            {
                throw new Exception("Date Invalid: " + err.Message);
            }
        }

        /// <summary>
        /// Set the end date for a backtest run
        /// </summary>
        /// <param name="day">Int end date 1-30</param>
        /// <param name="month">Int month end date</param>
        /// <param name="year">Int year end date</param>
        /// <remarks>Wrapper for SetEndDate(datetime).</remarks>
        /// <seealso cref="SetEndDate(DateTime)"/>
        public void SetEndDate(int year, int month, int day)
        {
            try
            {
                var end = new DateTime(year, month, day);

                // we want the end date to be just before the next day (last moment of the day)
                end = end.Date.AddDays(1).Subtract(TimeSpan.FromTicks(1));

                SetEndDate(end);
            }
            catch (Exception err)
            {
                throw new Exception("Date Invalid: " + err.Message);
            }
        }

        /// <summary>
        /// Set the algorithm id (backtestId or live deployId for the algorithmm).
        /// </summary>
        /// <param name="algorithmId">String Algorithm Id</param>
        /// <remarks>Intended for internal QC Lean Engine use only as a setter for AlgorihthmId</remarks>
        public void SetAlgorithmId(string algorithmId)
        {
            _algorithmId = algorithmId;
        }

        /// <summary>
        /// Set the start date for the backtest
        /// </summary>
        /// <param name="start">Datetime Start date for backtest</param>
        /// <remarks>Must be less than end date and within data available</remarks>
        /// <seealso cref="SetStartDate(DateTime)"/>
        public void SetStartDate(DateTime start)
        {
            // no need to set this value in live mode, will be set using the current time.
            if (_liveMode) return;

            //Validate the start date:
            //1. Check range;
            if (start < (new DateTime(1900, 01, 01)))
            {
                throw new Exception("Please select a start date after January 1st, 1900.");
            }

            //2. Check end date greater:
            if (_endDate != new DateTime())
            {
                if (start > _endDate)
                {
                    throw new Exception("Please select start date less than end date.");
                }
            }

            //3. Round up and subtract one tick:
            start = start.RoundDown(TimeSpan.FromDays(1));

            //3. Check not locked already:
            if (!_locked)
            {
                // this is only or backtesting
                if (!LiveMode)
                {
                    _startDate = start;
                    SetDateTime(_startDate.ConvertToUtc(TimeZone));
                }
            }
            else
            {
                throw new Exception("Algorithm.SetStartDate(): Cannot change start date after algorithm initialized.");
            }
        }

        /// <summary>
        /// Set the end date for a backtest.
        /// </summary>
        /// <param name="end">Datetime value for end date</param>
        /// <remarks>Must be greater than the start date</remarks>
        /// <seealso cref="SetEndDate(DateTime)"/>
        public void SetEndDate(DateTime end)
        {
            // no need to set this value in live mode, will be set using the current time.
            if (_liveMode) return;

            //Validate:
            //1. Check Range:
            if (end > DateTime.Now.Date.AddDays(-1))
            {
                end = DateTime.Now.Date.AddDays(-1);
            }

            //2. Check start date less:
            if (_startDate != new DateTime())
            {
                if (end < _startDate)
                {
                    throw new Exception("Please select end date greater than start date.");
                }
            }

            //3. Make this at the very end of the requested date
            end = end.RoundDown(TimeSpan.FromDays(1)).AddDays(1).AddTicks(-1);

            //4. Check not locked already:
            if (!_locked)
            {
                _endDate = end;
            }
            else
            {
                throw new Exception("Algorithm.SetEndDate(): Cannot change end date after algorithm initialized.");
            }
        }

        /// <summary>
        /// Lock the algorithm initialization to avoid user modifiying cash and data stream subscriptions
        /// </summary>
        /// <remarks>Intended for Internal QC Lean Engine use only to prevent accidental manipulation of important properties</remarks>
        public void SetLocked()
        {
            _locked = true;
        }

        /// <summary>
        /// Gets whether or not this algorithm has been locked and fully initialized
        /// </summary>
        public bool GetLocked()
        {
            return _locked;
        }

        /// <summary>
        /// Set live mode state of the algorithm run: Public setter for the algorithm property LiveMode.
        /// </summary>
        public void SetLiveMode(bool live)
        {
            if (!_locked)
            {
                _liveMode = live;
                Notify = new NotificationManager(live);
                TradeBuilder.SetLiveMode(live);

                if (live)
                {
                    _startDate = DateTime.Today;
                    _endDate = QuantConnect.Time.EndOfTime;
                }
            }
        }

        /// <summary>
        /// Set the <see cref="ITradeBuilder"/> implementation to generate trades from executions and market price updates
        /// </summary>
        public void SetTradeBuilder(ITradeBuilder tradeBuilder)
        {
            TradeBuilder = tradeBuilder;
            TradeBuilder.SetLiveMode(LiveMode);
        }

        /// <summary>
        /// Add specified data to our data subscriptions. QuantConnect will funnel this data to the handle data routine.
        /// </summary>
        /// <param name="securityType">MarketType Type: Equity, Commodity, Future, FOREX or Crypto</param>
        /// <param name="symbol">Symbol Reference for the MarketType</param>
        /// <param name="resolution">Resolution of the Data Required</param>
        /// <param name="fillDataForward">When no data available on a tradebar, return the last data that was generated</param>
        /// <param name="extendedMarketHours">Show the after market data as well</param>
        public Security AddSecurity(SecurityType securityType, string symbol, Resolution resolution = Resolution.Minute, bool fillDataForward = true, bool extendedMarketHours = false)
        {
            return AddSecurity(securityType, symbol, resolution, fillDataForward, 0, extendedMarketHours);
        }

        /// <summary>
        /// Add specified data to required list. QC will funnel this data to the handle data routine.
        /// </summary>
        /// <param name="securityType">MarketType Type: Equity, Commodity, Future, FOREX or Crypto</param>
        /// <param name="symbol">Symbol Reference for the MarketType</param>
        /// <param name="resolution">Resolution of the Data Required</param>
        /// <param name="fillDataForward">When no data available on a tradebar, return the last data that was generated</param>
        /// <param name="leverage">Custom leverage per security</param>
        /// <param name="extendedMarketHours">Extended market hours</param>
        /// <remarks> AddSecurity(SecurityType securityType, Symbol symbol, Resolution resolution, bool fillDataForward, decimal leverage, bool extendedMarketHours)</remarks>
        public Security AddSecurity(SecurityType securityType, string symbol, Resolution resolution, bool fillDataForward, decimal leverage, bool extendedMarketHours)
        {
            return AddSecurity(securityType, symbol, resolution, null, fillDataForward, leverage, extendedMarketHours);
        }

        /// <summary>
        /// Set a required SecurityType-symbol and resolution for algorithm
        /// </summary>
        /// <param name="securityType">MarketType Type: Equity, Commodity, Future, FOREX or Crypto</param>
        /// <param name="symbol">Symbol Representation of the MarketType, e.g. AAPL</param>
        /// <param name="resolution">Resolution of the MarketType required: MarketData, Second or Minute</param>
        /// <param name="market">The market the requested security belongs to, such as 'usa' or 'fxcm'</param>
        /// <param name="fillDataForward">If true, returns the last available data even if none in that timeslice.</param>
        /// <param name="leverage">leverage for this security</param>
        /// <param name="extendedMarketHours">ExtendedMarketHours send in data from 4am - 8pm, not used for FOREX</param>
        public Security AddSecurity(SecurityType securityType, string symbol, Resolution resolution, string market, bool fillDataForward, decimal leverage, bool extendedMarketHours)
        {
            // if AddSecurity method is called to add an option or a future, we delegate a call to respective methods
            if (securityType == SecurityType.Option)
            {
                return AddOption(symbol, resolution, market, fillDataForward, leverage);
            }

            if (securityType == SecurityType.Future)
            {
                return AddFuture(symbol, resolution, market, fillDataForward, leverage);
            }

            try
            {
                if (market == null)
                {
                    if (!BrokerageModel.DefaultMarkets.TryGetValue(securityType, out market))
                    {
                        throw new Exception("No default market set for security type: " + securityType);
                    }
                }

                Symbol symbolObject;
                if (!SymbolCache.TryGetSymbol(symbol, out symbolObject))
                {
                    symbolObject = QuantConnect.Symbol.Create(symbol, securityType, market);
                }

                var security = SecurityManager.CreateSecurity(Portfolio, SubscriptionManager, MarketHoursDatabase, _symbolPropertiesDatabase, SecurityInitializer,
                    symbolObject, resolution, fillDataForward, leverage, extendedMarketHours, false, false, LiveMode);

                AddToUserDefinedUniverse(security);
                return security;
            }
            catch (Exception err)
            {
                Error("Algorithm.AddSecurity(): " + err);
                return null;
            }
        }

        /// <summary>
        /// Creates and adds a new <see cref="Equity"/> security to the algorithm
        /// </summary>
        /// <param name="ticker">The equity ticker symbol</param>
        /// <param name="resolution">The <see cref="Resolution"/> of market data, Tick, Second, Minute, Hour, or Daily. Default is <see cref="Resolution.Minute"/></param>
        /// <param name="market">The equity's market, <seealso cref="Market"/>. Default value is null and looked up using BrokerageModel.DefaultMarkets in <see cref="AddSecurity{T}"/></param>
        /// <param name="fillDataForward">If true, returns the last available data even if none in that timeslice. Default is <value>true</value></param>
        /// <param name="leverage">The requested leverage for this equity. Default is set by <see cref="SecurityInitializer"/></param>
        /// <param name="extendedMarketHours">True to send data during pre and post market sessions. Default is <value>false</value></param>
        /// <returns>The new <see cref="Equity"/> security</returns>
        public Equity AddEquity(string ticker, Resolution resolution = Resolution.Minute, string market = null, bool fillDataForward = true, decimal leverage = 0m, bool extendedMarketHours = false)
        {
            return AddSecurity<Equity>(SecurityType.Equity, ticker, resolution, market, fillDataForward, leverage, extendedMarketHours);
        }

        /// <summary>
        /// Creates and adds a new equity <see cref="Option"/> security to the algorithm
        /// </summary>
        /// <param name="underlying">The underlying equity symbol</param>
        /// <param name="resolution">The <see cref="Resolution"/> of market data, Tick, Second, Minute, Hour, or Daily. Default is <see cref="Resolution.Minute"/></param>
        /// <param name="market">The equity's market, <seealso cref="Market"/>. Default is value null and looked up using BrokerageModel.DefaultMarkets in <see cref="AddSecurity{T}"/></param>
        /// <param name="fillDataForward">If true, returns the last available data even if none in that timeslice. Default is <value>true</value></param>
        /// <param name="leverage">The requested leverage for this equity. Default is set by <see cref="SecurityInitializer"/></param>
        /// <returns>The new <see cref="Option"/> security</returns>
        public Option AddOption(string underlying, Resolution resolution = Resolution.Minute, string market = null, bool fillDataForward = true, decimal leverage = 0m)
        {
            if (market == null)
            {
                if (!BrokerageModel.DefaultMarkets.TryGetValue(SecurityType.Option, out market))
                {
                    throw new Exception("No default market set for security type: " + SecurityType.Option);
                }
            }

            Symbol canonicalSymbol;
            var alias = "?" + underlying;
            if (!SymbolCache.TryGetSymbol(alias, out canonicalSymbol))
            {
                canonicalSymbol = QuantConnect.Symbol.Create(underlying, SecurityType.Option, market, alias);
            }

            var marketHoursEntry = MarketHoursDatabase.GetEntry(market, underlying, SecurityType.Option);
            var symbolProperties = _symbolPropertiesDatabase.GetSymbolProperties(market, underlying, SecurityType.Option, CashBook.AccountCurrency);
            var canonicalSecurity = (Option) SecurityManager.CreateSecurity(typeof(ZipEntryName), Portfolio, SubscriptionManager,
                marketHoursEntry.ExchangeHours, marketHoursEntry.DataTimeZone, symbolProperties, SecurityInitializer, canonicalSymbol, resolution,
                fillDataForward, leverage, false, false, false, LiveMode, true, false);
            canonicalSecurity.IsTradable = false;
            Securities.Add(canonicalSecurity);

            // add this security to the user defined universe
            Universe universe;
            if (!UniverseManager.TryGetValue(canonicalSymbol, out universe) && _pendingUniverseAdditions.All(u => u.Configuration.Symbol != canonicalSymbol))
            {
                var settings = new UniverseSettings(resolution, leverage, true, false, TimeSpan.Zero);
                universe = new OptionChainUniverse(canonicalSecurity, settings, SecurityInitializer, LiveMode);
                _pendingUniverseAdditions.Add(universe);
            }

            return canonicalSecurity;
        }

        /// <summary>
        /// Creates and adds a new <see cref="Future"/> security to the algorithm
        /// </summary>
        /// <param name="symbol">The futures contract symbol</param>
        /// <param name="resolution">The <see cref="Resolution"/> of market data, Tick, Second, Minute, Hour, or Daily. Default is <see cref="Resolution.Minute"/></param>
        /// <param name="market">The futures market, <seealso cref="Market"/>. Default is value null and looked up using BrokerageModel.DefaultMarkets in <see cref="AddSecurity{T}"/></param>
        /// <param name="fillDataForward">If true, returns the last available data even if none in that timeslice. Default is <value>true</value></param>
        /// <param name="leverage">The requested leverage for this equity. Default is set by <see cref="SecurityInitializer"/></param>
        /// <returns>The new <see cref="Future"/> security</returns>
        public Future AddFuture(string symbol, Resolution resolution = Resolution.Minute, string market = null, bool fillDataForward = true, decimal leverage = 0m)
        {
            if (market == null)
            {
                if (!BrokerageModel.DefaultMarkets.TryGetValue(SecurityType.Future, out market))
                {
                    throw new Exception("No default market set for security type: " + SecurityType.Future);
                }
            }

            Symbol canonicalSymbol;
            var alias = "/" + symbol;
            if (!SymbolCache.TryGetSymbol(alias, out canonicalSymbol))
            {
                canonicalSymbol = QuantConnect.Symbol.Create(symbol, SecurityType.Future, market, alias);
            }

            var marketHoursEntry = MarketHoursDatabase.GetEntry(market, symbol, SecurityType.Future);
            var symbolProperties = _symbolPropertiesDatabase.GetSymbolProperties(market, symbol, SecurityType.Future, CashBook.AccountCurrency);
            var canonicalSecurity = (Future)SecurityManager.CreateSecurity(typeof(ZipEntryName), Portfolio, SubscriptionManager,
                marketHoursEntry.ExchangeHours, marketHoursEntry.DataTimeZone, symbolProperties, SecurityInitializer, canonicalSymbol, resolution,
                fillDataForward, leverage, false, false, false, LiveMode, true, false);
            canonicalSecurity.IsTradable = false;
            Securities.Add(canonicalSecurity);

            // add this security to the user defined universe
            Universe universe;
            if (!UniverseManager.TryGetValue(canonicalSymbol, out universe) && _pendingUniverseAdditions.All(u => u.Configuration.Symbol != canonicalSymbol))
            {
                var settings = new UniverseSettings(resolution, leverage, true, false, TimeSpan.Zero);
                universe = new FuturesChainUniverse(canonicalSecurity, settings, SubscriptionManager, SecurityInitializer);
                _pendingUniverseAdditions.Add(universe);
            }

            return canonicalSecurity;
        }

        /// <summary>
        /// Creates and adds a new single <see cref="Future"/> contract to the algorithm
        /// </summary>
        /// <param name="symbol">The futures contract symbol</param>
        /// <param name="resolution">The <see cref="Resolution"/> of market data, Tick, Second, Minute, Hour, or Daily. Default is <see cref="Resolution.Minute"/></param>
        /// <param name="fillDataForward">If true, returns the last available data even if none in that timeslice. Default is <value>true</value></param>
        /// <param name="leverage">The requested leverage for this equity. Default is set by <see cref="SecurityInitializer"/></param>
        /// <returns>The new <see cref="Future"/> security</returns>
        public Future AddFutureContract(Symbol symbol, Resolution resolution = Resolution.Minute, bool fillDataForward = true, decimal leverage = 0m)
        {
            var future = (Future)SecurityManager.CreateSecurity(Portfolio, SubscriptionManager, MarketHoursDatabase, _symbolPropertiesDatabase, SecurityInitializer,
                symbol, resolution, fillDataForward, leverage, false, false, false, LiveMode);

            AddToUserDefinedUniverse(future);

            return future;
        }

        /// <summary>
        /// Creates and adds a new single <see cref="Option"/> contract to the algorithm
        /// </summary>
        /// <param name="symbol">The option contract symbol</param>
        /// <param name="resolution">The <see cref="Resolution"/> of market data, Tick, Second, Minute, Hour, or Daily. Default is <see cref="Resolution.Minute"/></param>
        /// <param name="fillDataForward">If true, returns the last available data even if none in that timeslice. Default is <value>true</value></param>
        /// <param name="leverage">The requested leverage for this equity. Default is set by <see cref="SecurityInitializer"/></param>
        /// <returns>The new <see cref="Option"/> security</returns>
        public Option AddOptionContract(Symbol symbol, Resolution resolution = Resolution.Minute, bool fillDataForward = true, decimal leverage = 0m)
        {
            var option = (Option)SecurityManager.CreateSecurity(Portfolio, SubscriptionManager, MarketHoursDatabase, _symbolPropertiesDatabase, SecurityInitializer,
                symbol, resolution, fillDataForward, leverage, false, false, false, LiveMode);

            // add underlying if not present
            var underlying = option.Symbol.Underlying;
            Security equity;
            if (!Securities.TryGetValue(underlying, out equity))
            {
                equity = AddEquity(underlying.Value, option.Resolution, underlying.ID.Market, false);
            }
            else if (equity.DataNormalizationMode != DataNormalizationMode.Raw)
            {
                Debug($"Warning: The {underlying.ToString()} equity security was set the raw price normalization mode to work with options.");
            }
            equity.SetDataNormalizationMode(DataNormalizationMode.Raw);

            option.Underlying = equity;

            AddToUserDefinedUniverse(option);

            return option;
        }

        /// <summary>
        /// Creates and adds a new <see cref="Forex"/> security to the algorithm
        /// </summary>
        /// <param name="ticker">The currency pair</param>
        /// <param name="resolution">The <see cref="Resolution"/> of market data, Tick, Second, Minute, Hour, or Daily. Default is <see cref="Resolution.Minute"/></param>
        /// <param name="market">The foreign exchange trading market, <seealso cref="Market"/>. Default value is null and looked up using BrokerageModel.DefaultMarkets in <see cref="AddSecurity{T}"/></param>
        /// <param name="fillDataForward">If true, returns the last available data even if none in that timeslice. Default is <value>true</value></param>
        /// <param name="leverage">The requested leverage for this equity. Default is set by <see cref="SecurityInitializer"/></param>
        /// <returns>The new <see cref="Forex"/> security</returns>
        public Forex AddForex(string ticker, Resolution resolution = Resolution.Minute, string market = null, bool fillDataForward = true, decimal leverage = 0m)
        {
            return AddSecurity<Forex>(SecurityType.Forex, ticker, resolution, market, fillDataForward, leverage, false);
        }

        /// <summary>
        /// Creates and adds a new <see cref="Cfd"/> security to the algorithm
        /// </summary>
        /// <param name="ticker">The currency pair</param>
        /// <param name="resolution">The <see cref="Resolution"/> of market data, Tick, Second, Minute, Hour, or Daily. Default is <see cref="Resolution.Minute"/></param>
        /// <param name="market">The cfd trading market, <seealso cref="Market"/>. Default value is null and looked up using BrokerageModel.DefaultMarkets in <see cref="AddSecurity{T}"/></param>
        /// <param name="fillDataForward">If true, returns the last available data even if none in that timeslice. Default is <value>true</value></param>
        /// <param name="leverage">The requested leverage for this equity. Default is set by <see cref="SecurityInitializer"/></param>
        /// <returns>The new <see cref="Cfd"/> security</returns>
        public Cfd AddCfd(string ticker, Resolution resolution = Resolution.Minute, string market = null, bool fillDataForward = true, decimal leverage = 0m)
        {
            return AddSecurity<Cfd>(SecurityType.Cfd, ticker, resolution, market, fillDataForward, leverage, false);
        }

        /// <summary>
        /// Creates and adds a new <see cref="Crypto"/> security to the algorithm
        /// </summary>
        /// <param name="ticker">The currency pair</param>
        /// <param name="resolution">The <see cref="Resolution"/> of market data, Tick, Second, Minute, Hour, or Daily. Default is <see cref="Resolution.Minute"/></param>
        /// <param name="market">The cfd trading market, <seealso cref="Market"/>. Default value is null and looked up using BrokerageModel.DefaultMarkets in <see cref="AddSecurity{T}"/></param>
        /// <param name="fillDataForward">If true, returns the last available data even if none in that timeslice. Default is <value>true</value></param>
        /// <param name="leverage">The requested leverage for this equity. Default is set by <see cref="SecurityInitializer"/></param>
        /// <returns>The new <see cref="Crypto"/> security</returns>
        public Crypto AddCrypto(string ticker, Resolution resolution = Resolution.Minute, string market = null, bool fillDataForward = true, decimal leverage = 0m)
        {
            return AddSecurity<Crypto>(SecurityType.Crypto, ticker, resolution, market, fillDataForward, leverage, false);
        }

        /// <summary>
        /// Removes the security with the specified symbol. This will cancel all
        /// open orders and then liquidate any existing holdings
        /// </summary>
        /// <param name="symbol">The symbol of the security to be removed</param>
        public bool RemoveSecurity(Symbol symbol)
        {
            Security security;
            if (Securities.TryGetValue(symbol, out security))
            {
                // cancel open orders
                Transactions.CancelOpenOrders(security.Symbol);

                // Clear cache
                security.Cache.Reset();

                // liquidate if invested
                if (security.Invested) Liquidate(security.Symbol);

                var universe = UniverseManager.Select(x => x.Value).OfType<UserDefinedUniverse>().FirstOrDefault(x => x.Members.ContainsKey(symbol));
                if (universe != null)
                {
                    var ret = universe.Remove(symbol);

                    // if we are removing the symbol which is also the benchmark, add it back as internal feed
                    if (symbol == _benchmarkSymbol)
                    {
                        Securities.Remove(symbol);

                        security = CreateBenchmarkSecurity();
                        AddToUserDefinedUniverse(security);
                    }

                    SubscriptionManager.HasCustomData = universe.Members.Any(x => x.Value.Subscriptions.Any(y => y.IsCustomData));

                    return ret;
                }
            }
            return false;
        }

        /// <summary>
        /// AddData<typeparam name="T"/> a new user defined data source, requiring only the minimum config options.
        /// The data is added with a default time zone of NewYork (Eastern Daylight Savings Time)
        /// </summary>
        /// <param name="symbol">Key/Symbol for data</param>
        /// <param name="resolution">Resolution of the data</param>
        /// <returns>The new <see cref="Security"/></returns>
        /// <remarks>Generic type T must implement base data</remarks>
        public Security AddData<T>(string symbol, Resolution resolution = Resolution.Minute)
            where T : IBaseData, new()
        {
            //Add this new generic data as a tradeable security:
            // Defaults:extended market hours"      = true because we want events 24 hours,
            //          fillforward                 = false because only want to trigger when there's new custom data.
            //          leverage                    = 1 because no leverage on nonmarket data?
            return AddData<T>(symbol, resolution, fillDataForward: false, leverage: 1m);
        }

        /// <summary>
        /// AddData<typeparam name="T"/> a new user defined data source, requiring only the minimum config options.
        /// The data is added with a default time zone of NewYork (Eastern Daylight Savings Time)
        /// </summary>
        /// <param name="symbol">Key/Symbol for data</param>
        /// <param name="resolution">Resolution of the Data Required</param>
        /// <param name="fillDataForward">When no data available on a tradebar, return the last data that was generated</param>
        /// <param name="leverage">Custom leverage per security</param>
        /// <returns>The new <see cref="Security"/></returns>
        /// <remarks>Generic type T must implement base data</remarks>
        public Security AddData<T>(string symbol, Resolution resolution, bool fillDataForward, decimal leverage = 1.0m)
            where T : IBaseData, new()
        {
            return AddData<T>(symbol, resolution, TimeZones.NewYork, fillDataForward, leverage);
        }

        /// <summary>
        /// AddData<typeparam name="T"/> a new user defined data source, requiring only the minimum config options.
        /// </summary>
        /// <param name="symbol">Key/Symbol for data</param>
        /// <param name="resolution">Resolution of the Data Required</param>
        /// <param name="timeZone">Specifies the time zone of the raw data</param>
        /// <param name="fillDataForward">When no data available on a tradebar, return the last data that was generated</param>
        /// <param name="leverage">Custom leverage per security</param>
        /// <returns>The new <see cref="Security"/></returns>
        /// <remarks>Generic type T must implement base data</remarks>
        public Security AddData<T>(string symbol, Resolution resolution, DateTimeZone timeZone, bool fillDataForward = false, decimal leverage = 1.0m)
            where T : IBaseData, new()
        {
            //Add this custom symbol to our market hours database
            var marketHoursDbEntry = MarketHoursDatabase.SetEntryAlwaysOpen(Market.USA, symbol, SecurityType.Base, timeZone);

            //Add this to the data-feed subscriptions
            var symbolObject = new Symbol(SecurityIdentifier.GenerateBase(symbol, Market.USA), symbol);
            var symbolProperties = _symbolPropertiesDatabase.GetSymbolProperties(Market.USA, symbol, SecurityType.Base, CashBook.AccountCurrency);

            //Add this new generic data as a tradeable security:
            var security = SecurityManager.CreateSecurity(typeof(T), Portfolio, SubscriptionManager, marketHoursDbEntry.ExchangeHours, marketHoursDbEntry.DataTimeZone,
                symbolProperties, SecurityInitializer, symbolObject, resolution, fillDataForward, leverage, true, false, true, LiveMode);

            AddToUserDefinedUniverse(security);
            return security;
        }

        /// <summary>
        /// Send a debug message to the web console:
        /// </summary>
        /// <param name="message">Message to send to debug console</param>
        /// <seealso cref="Log(string)"/>
        /// <seealso cref="Error(string)"/>
        public void Debug(string message)
        {
            if (!_liveMode && (message == "" || _previousDebugMessage == message)) return;
            _debugMessages.Enqueue(message);
            _previousDebugMessage = message;
        }

        /// <summary>
        /// Added another method for logging if user guessed.
        /// </summary>
        /// <param name="message">String message to log.</param>
        /// <seealso cref="Debug(string)"/>
        /// <seealso cref="Error(string)"/>
        public void Log(string message)
        {
            if (!_liveMode && message == "") return;
            _logMessages.Enqueue(message);
        }

        /// <summary>
        /// Send a string error message to the Console.
        /// </summary>
        /// <param name="message">Message to display in errors grid</param>
        /// <seealso cref="Debug(string)"/>
        /// <seealso cref="Log(string)"/>
        public void Error(string message)
        {
            if (!_liveMode && (message == "" || _previousErrorMessage == message)) return;
            _errorMessages.Enqueue(message);
            _previousErrorMessage = message;
        }

        /// <summary>
        /// Send a string error message to the Console.
        /// </summary>
        /// <param name="error">Exception object captured from a try catch loop</param>
        /// <seealso cref="Debug(string)"/>
        /// <seealso cref="Log(string)"/>
        public void Error(Exception error)
        {
            var message = error.Message;
            if (!_liveMode && (message == "" || _previousErrorMessage == message)) return;
            _errorMessages.Enqueue(message);
            _previousErrorMessage = message;
        }

        /// <summary>
        /// Terminate the algorithm after processing the current event handler.
        /// </summary>
        /// <param name="message">Exit message to display on quitting</param>
        public void Quit(string message = "")
        {
            Debug("Quit(): " + message);
            Status = AlgorithmStatus.Stopped;
        }

        /// <summary>
        /// Set the Quit flag property of the algorithm.
        /// </summary>
        /// <remarks>Intended for internal use by the QuantConnect Lean Engine only.</remarks>
        /// <param name="quit">Boolean quit state</param>
        /// <seealso cref="Quit"/>
        public void SetQuit(bool quit)
        {
            if (quit)
            {
                Status = AlgorithmStatus.Stopped;
            }
        }

        /// <summary>
        /// Converts the string 'ticker' symbol into a full <see cref="Symbol"/> object
        /// This requires that the string 'ticker' has been added to the algorithm
        /// </summary>
        /// <param name="ticker">The ticker symbol. This should be the ticker symbol
        /// as it was added to the algorithm</param>
        /// <returns>The symbol object mapped to the specified ticker</returns>
        public Symbol Symbol(string ticker)
        {
            return SymbolCache.GetSymbol(ticker);
        }

        /// <summary>
        /// Creates and adds a new <see cref="Security"/> to the algorithm
        /// </summary>
        private T AddSecurity<T>(SecurityType securityType, string ticker, Resolution resolution, string market, bool fillDataForward, decimal leverage, bool extendedMarketHours)
            where T : Security
        {
            if (market == null)
            {
                if (!BrokerageModel.DefaultMarkets.TryGetValue(securityType, out market))
                {
                    throw new Exception("No default market set for security type: " + securityType);
                }
            }

            Symbol symbol;
            if (!SymbolCache.TryGetSymbol(ticker, out symbol))
            {
                symbol = QuantConnect.Symbol.Create(ticker, securityType, market);
            }

            var security = SecurityManager.CreateSecurity(Portfolio, SubscriptionManager, MarketHoursDatabase, _symbolPropertiesDatabase, SecurityInitializer,
                symbol, resolution, fillDataForward, leverage, extendedMarketHours, false, false, LiveMode);
            AddToUserDefinedUniverse(security);
            return (T)security;
        }

        /// <summary>
        /// Creates and returns a <see cref="Security"/> object to be used as the benchmark
        /// </summary>
        private Security CreateBenchmarkSecurity()
        {
            // add the security as an internal feed so the algorithm doesn't receive the data
            Resolution resolution;
            if (_liveMode)
            {
                resolution = Resolution.Second;
            }
            else
            {
                // check to see if any universes arn't the ones added via AddSecurity
                var hasNonAddSecurityUniverses = (
                    from universe in UniverseManager.Select(kvp => kvp.Value).Union(_pendingUniverseAdditions)
                    let config = universe.Configuration
                    let symbol = UserDefinedUniverse.CreateSymbol(config.SecurityType, config.Market)
                    where config.Symbol != symbol
                    select universe).Any();

                resolution = hasNonAddSecurityUniverses ? UniverseSettings.Resolution : Resolution.Daily;
            }
            return SecurityManager.CreateSecurity(Portfolio, SubscriptionManager, MarketHoursDatabase, _symbolPropertiesDatabase, SecurityInitializer, _benchmarkSymbol, resolution, true, 1m, false, true, false, LiveMode);
        }

        /// <summary>
        /// Set the historical data provider
        /// </summary>
        /// <param name="historyProvider">Historical data provider</param>
        public void SetHistoryProvider(IHistoryProvider historyProvider)
        {
            if (historyProvider == null)
            {
                throw new ArgumentNullException("Algorithm.SetHistoryProvider(): Historical data provider cannot be null.");
            }
            HistoryProvider = historyProvider;
        }

        /// <summary>
        /// Set the runtime error
        /// </summary>
        /// <param name="exception">Represents error that occur during execution</param>
        public void SetRunTimeError(Exception exception)
        {
            if (exception == null)
            {
                throw new ArgumentNullException("Algorithm.SetRunTimeError(): Algorithm.RunTimeError cannot be set to null.");
            }

            RunTimeError = exception;
        }

        /// <summary>
        /// Set the state of a live deployment
        /// </summary>
        /// <param name="status">Live deployment status</param>
        public void SetStatus(AlgorithmStatus status)
        {
            Status = status;
        }

        /// <summary>
        /// Downloads the requested resource as a <see cref="string"/>.
        /// The resource to download is specified as a <see cref="string"/> containing the URI.
        /// </summary>
        /// <param name="address">A string containing the URI to download</param>
        /// <param name="headers">Defines header values to add to the request</param>
        /// <param name="userName">The user name associated with the credentials</param>
        /// <param name="password">The password for the user name associated with the credentials</param>
        /// <returns>The requested resource as a <see cref="string"/></returns>
        public string Download(string address, IEnumerable<KeyValuePair<string, string>> headers = null, string userName = null, string password = null)
        {
            using (var client = new WebClient { Credentials = new NetworkCredential(userName, password) })
            {
                client.Proxy = WebRequest.GetSystemWebProxy();
                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        client.Headers.Add(header.Key, header.Value);
                    }
                }
                // Add a user agent header in case the requested URI contains a query.
                client.Headers.Add("user-agent", "QCAlgorithm.Download(): User Agent Header");

                return client.DownloadString(address);
            }
        }

        /// <summary>
        /// Event invocator for the <see cref="InsightsGenerated"/> event
        /// </summary>
        /// <param name="insights">The collection of insights generaed at the current time step</param>
        protected void OnInsightsGenerated(IEnumerable<Insight> insights)
        {
            InsightsGenerated?.Invoke(this, new GeneratedInsightsCollection(UtcTime, insights));
        }
    }
}
