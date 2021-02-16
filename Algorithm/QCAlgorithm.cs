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
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Alphas.Analysis.Providers;
using QuantConnect.Algorithm.Framework.Execution;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Algorithm.Framework.Risk;
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Algorithm.Selection;
using QuantConnect.Storage;

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
        private bool _locked;
        private bool _liveMode;
        private string _algorithmId = "";
        private ConcurrentQueue<string> _debugMessages = new ConcurrentQueue<string>();
        private ConcurrentQueue<string> _logMessages = new ConcurrentQueue<string>();
        private ConcurrentQueue<string> _errorMessages = new ConcurrentQueue<string>();

        //Error tracking to avoid message flooding:
        private string _previousDebugMessage = "";
        private string _previousErrorMessage = "";

        /// <summary>
        /// Gets the market hours database in use by this algorithm
        /// </summary>
        protected MarketHoursDatabase MarketHoursDatabase { get; }

        /// <summary>
        /// Gets the symbol properties database in use by this algorithm
        /// </summary>
        protected SymbolPropertiesDatabase SymbolPropertiesDatabase { get; }

        // used for calling through to void OnData(Slice) if no override specified
        private bool _checkedForOnDataSlice;
        private Action<Slice> _onDataSlice;

        // flips to true when the user
        private bool _userSetSecurityInitializer = false;

        // warmup resolution variables
        private TimeSpan? _warmupTimeSpan;
        private int? _warmupBarCount;
        private Resolution? _warmupResolution;
        private Dictionary<string, string> _parameters = new Dictionary<string, string>();

        private readonly HistoryRequestFactory _historyRequestFactory;

        private IApi _api;

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
            DefaultOrderProperties = new OrderProperties();

            //Initialise Data Manager
            SubscriptionManager = new SubscriptionManager();

            Securities = new SecurityManager(_timeKeeper);
            Transactions = new SecurityTransactionManager(this, Securities);
            Portfolio = new SecurityPortfolioManager(Securities, Transactions, DefaultOrderProperties);
            BrokerageModel = new DefaultBrokerageModel();
            Notify = new NotificationManager(false); // Notification manager defaults to disabled.

            //Initialise to unlocked:
            _locked = false;

            // get exchange hours loaded from the market-hours-database.csv in /Data/market-hours
            MarketHoursDatabase = MarketHoursDatabase.FromDataFolder();
            SymbolPropertiesDatabase = SymbolPropertiesDatabase.FromDataFolder();

            // universe selection
            UniverseManager = new UniverseManager();
            Universe = new UniverseDefinitions(this);
            UniverseSettings = new UniverseSettings(Resolution.Minute, Security.NullLeverage, true, false, TimeSpan.FromDays(1));

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
            _historyRequestFactory = new HistoryRequestFactory(this);

            // Framework
            _securityValuesProvider = new AlgorithmSecurityValuesProvider(this);

            // set model defaults, universe selection set via PostInitialize
            SetAlpha(new NullAlphaModel());
            SetPortfolioConstruction(new NullPortfolioConstructionModel());
            SetExecution(new ImmediateExecutionModel());
            SetRiskManagement(new NullRiskManagementModel());
            SetUniverseSelection(new NullUniverseSelectionModel());
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
        /// Read-only dictionary containing all active securities. An active security is
        /// a security that is currently selected by the universe or has holdings or open orders.
        /// </summary>
        public IReadOnlyDictionary<Symbol, Security> ActiveSecurities => UniverseManager.ActiveSecurities;

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
        /// Gets the account currency
        /// </summary>
        public string AccountCurrency => Portfolio.CashBook.AccountCurrency;

        /// <summary>
        /// Gets the time keeper instance
        /// </summary>
        public ITimeKeeper TimeKeeper => _timeKeeper;

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
        public IAlgorithmSettings Settings
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
        /// Returns the current Slice object
        /// </summary>
        public Slice CurrentSlice { get; private set; }

        /// <summary>
        /// Gets the object store, used for persistence
        /// </summary>
        public ObjectStore ObjectStore { get; private set; }

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
            if (_endDate < _startDate)
            {
                throw new ArgumentException("Please select an algorithm end date greater than start date.");
            }

            var portfolioConstructionModel = PortfolioConstruction as PortfolioConstructionModel;
            if (portfolioConstructionModel != null)
            {
                // only override default values if user set the algorithm setting
                if (Settings.RebalancePortfolioOnSecurityChanges.HasValue)
                {
                    portfolioConstructionModel.RebalanceOnSecurityChanges
                        = Settings.RebalancePortfolioOnSecurityChanges.Value;
                }
                if (Settings.RebalancePortfolioOnInsightChanges.HasValue)
                {
                    portfolioConstructionModel.RebalanceOnInsightChanges
                        = Settings.RebalancePortfolioOnInsightChanges.Value;
                }
            }
            else
            {
                if (Settings.RebalancePortfolioOnInsightChanges.HasValue
                    || Settings.RebalancePortfolioOnSecurityChanges.HasValue)
                {
                    Debug("Warning: rebalance portfolio settings are set but not supported by the current IPortfolioConstructionModel type: " +
                          $"{PortfolioConstruction.GetType()}");
                }
            }

            FrameworkPostInitialize();

            // if the benchmark hasn't been set yet, load in the default from the brokerage model
            if (Benchmark == null)
            {
                Benchmark = BrokerageModel.GetBenchmark(Securities);
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
        /// Gets a read-only dictionary with all current parameters
        /// </summary>
        public IReadOnlyDictionary<string, string> GetParameters()
        {
            return _parameters.ToReadOnlyDictionary();
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
        /// Sets the security initializer, used to initialize/configure securities after creation.
        /// The initializer will be applied to all universes and manually added securities.
        /// </summary>
        /// <param name="securityInitializer">The security initializer</param>
        public void SetSecurityInitializer(ISecurityInitializer securityInitializer)
        {
            if (_locked)
            {
                throw new Exception("SetSecurityInitializer() cannot be called after algorithm initialization. " +
                                    "When you use the SetSecurityInitializer() method it will apply to all universes and manually added securities.");
            }

            if (_userSetSecurityInitializer)
            {
                Debug("Warning: SetSecurityInitializer() has already been called, existing security initializers in all universes will be overwritten.");
            }

            // this flag will prevent calls to SetBrokerageModel from overwriting this initializer
            _userSetSecurityInitializer = true;
            SecurityInitializer = securityInitializer;
        }

        /// <summary>
        /// Sets the security initializer function, used to initialize/configure securities after creation.
        /// The initializer will be applied to all universes and manually added securities.
        /// </summary>
        /// <param name="securityInitializer">The security initializer function</param>
        [Obsolete("This method is deprecated. Please use this overload: SetSecurityInitializer(Action<Security> securityInitializer)")]
        public void SetSecurityInitializer(Action<Security, bool> securityInitializer)
        {
            SetSecurityInitializer(new FuncSecurityInitializer(security => securityInitializer(security, false)));
        }

        /// <summary>
        /// Sets the security initializer function, used to initialize/configure securities after creation.
        /// The initializer will be applied to all universes and manually added securities.
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
        /// Event fired each time the we add/remove securities from the data feed
        /// </summary>
        /// <param name="changes">Security additions/removals for this time step</param>
        public virtual void OnSecuritiesChanged(SecurityChanges changes)
        {
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
        /// <remarks>Deprecated because different assets have different market close times,
        /// and because Python does not support two methods with the same name</remarks>
        [Obsolete("This method is deprecated. Please use this overload: OnEndOfDay(Symbol symbol)")]
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
        /// <param name="frontier">Current utc datetime.</param>
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
                throw new ArgumentException($"TimeZone with id '{timeZone}' was not found. For a complete list of time zones please visit: http://en.wikipedia.org/wiki/List_of_tz_database_time_zones");
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
                throw new InvalidOperationException("Algorithm.SetTimeZone(): Cannot change time zone after algorithm running.");
            }

            if (timeZone == null) throw new ArgumentNullException("timeZone");
            _timeKeeper.AddTimeZone(timeZone);
            _localTimeKeeper = _timeKeeper.GetLocalTimeKeeper(timeZone);

            // the time rules need to know the default time zone as well
            TimeRules.SetDefaultTimeZone(timeZone);

            // In BackTest mode we reset the Algorithm time to reflect the new timezone
            // startDate is set by the user so we expect it to be for their timezone already
            // so there is no need to update it.
            if (!LiveMode)
            {
                SetDateTime(_startDate.ConvertToUtc(TimeZone));
            }
            // In live mode we need to adjust startDate to reflect the new timezone
            // startDate is set by Lean to the default timezone (New York), so we must update it here
            else
            {
                _startDate = DateTime.UtcNow.ConvertFromUtc(TimeZone).Date;
            }
        }

        /// <summary>
        /// Sets the brokerage to emulate in backtesting or paper trading.
        /// This can be used for brokerages that have been implemented in LEAN
        /// </summary>
        /// <param name="brokerage">The brokerage to emulate</param>
        /// <param name="accountType">The account type (Cash or Margin)</param>
        public void SetBrokerageModel(BrokerageName brokerage, AccountType accountType = AccountType.Margin)
        {
            SetBrokerageModel(Brokerages.BrokerageModel.Create(Transactions, brokerage, accountType));
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
            if (_locked)
            {
                throw new InvalidOperationException("Algorithm.SetBenchmark(): Cannot change Benchmark after algorithm initialized.");
            }

            string market;
            if (!BrokerageModel.DefaultMarkets.TryGetValue(securityType, out market))
            {
                market = Market.USA;
            }

            var benchmarkSymbol = QuantConnect.Symbol.Create(symbol, securityType, market);
            SetBenchmark(benchmarkSymbol);
        }

        /// <summary>
        /// Sets the benchmark used for computing statistics of the algorithm to the specified ticker, defaulting to SecurityType.Equity
        /// if the ticker doesn't exist in the algorithm
        /// </summary>
        /// <param name="ticker">Ticker to use as the benchmark</param>
        /// <remarks>
        /// Overload to accept ticker without passing SecurityType. If ticker is in portfolio it will use that SecurityType, otherwise will default to SecurityType.Equity
        /// </remarks>
        public void SetBenchmark(string ticker)
        {
            Symbol symbol;

            // Check the cache for the symbol
            if (!SymbolCache.TryGetSymbol(ticker, out symbol))
            {
                // Check our securities for a symbol matched with this ticker
                symbol = Securities.FirstOrDefault(x => x.Key.Value == ticker).Key;

                // If we didn't find a symbol matching our ticker, create one.
                if (symbol == null)
                {
                    Debug($"Warning: SetBenchmark({ticker}): no existing symbol found, benchmark security will be added with {SecurityType.Equity} type.");
                    symbol = QuantConnect.Symbol.Create(ticker, SecurityType.Equity, Market.USA);
                }
            }

            // Send our symbol through
            SetBenchmark(symbol);
        }

        /// <summary>
        /// Sets the benchmark used for computing statistics of the algorithm to the specified symbol
        /// </summary>
        /// <param name="symbol">symbol to use as the benchmark</param>
        public void SetBenchmark(Symbol symbol)
        {
            if (_locked)
            {
                throw new InvalidOperationException("Algorithm.SetBenchmark(): Cannot change Benchmark after algorithm initialized.");
            }

            // Create our security benchmark
            Benchmark = SecurityBenchmark.CreateInstance(Securities, symbol);
        }

        /// <summary>
        /// Sets the specified function as the benchmark, this function provides the value of
        /// the benchmark at each date/time requested
        /// </summary>
        /// <param name="benchmark">The benchmark producing function</param>
        public void SetBenchmark(Func<DateTime, decimal> benchmark)
        {
            if (_locked)
            {
                throw new InvalidOperationException("Algorithm.SetBenchmark(): Cannot change Benchmark after algorithm initialized.");
            }

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
        /// Sets the account currency cash symbol this algorithm is to manage.
        /// </summary>
        /// <remarks>Has to be called during <see cref="Initialize"/> before
        /// calling <see cref="SetCash(decimal)"/> or adding any <see cref="Security"/></remarks>
        /// <param name="accountCurrency">The account currency cash symbol to set</param>
        public void SetAccountCurrency(string accountCurrency)
        {
            if (_locked)
            {
                throw new InvalidOperationException("Algorithm.SetAccountCurrency(): " +
                    "Cannot change AccountCurrency after algorithm initialized.");
            }

            Debug($"Changing account currency from {AccountCurrency} to {accountCurrency}...");

            Portfolio.SetAccountCurrency(accountCurrency);
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
                throw new InvalidOperationException("Algorithm.SetCash(): Cannot change cash available after algorithm initialized.");
            }
        }

        /// <summary>
        /// Set the cash for the specified symbol
        /// </summary>
        /// <param name="symbol">The cash symbol to set</param>
        /// <param name="startingCash">Decimal cash value of portfolio</param>
        /// <param name="conversionRate">The current conversion rate for the</param>
        public void SetCash(string symbol, decimal startingCash, decimal conversionRate = 0)
        {
            if (!_locked)
            {
                Portfolio.SetCash(symbol, startingCash, conversionRate);
            }
            else
            {
                throw new InvalidOperationException("Algorithm.SetCash(): Cannot change cash available after algorithm initialized.");
            }
        }

        /// <summary>
        /// Set the start date for backtest.
        /// </summary>
        /// <param name="day">Int starting date 1-30</param>
        /// <param name="month">Int month starting date</param>
        /// <param name="year">Int year starting date</param>
        /// <remarks>Wrapper for SetStartDate(DateTime).
        /// Must be less than end date.
        /// Ignored in live trading mode.</remarks>
        /// <seealso cref="SetStartDate(DateTime)"/>
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
                throw new ArgumentException($"Date Invalid: {err.Message}");
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
                throw new ArgumentException($"Date Invalid: {err.Message}");
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
        /// <seealso cref="SetStartDate(int, int, int)"/>
        public void SetStartDate(DateTime start)
        {
            // no need to set this value in live mode, will be set using the current time.
            if (_liveMode) return;

            //Round down
            start = start.RoundDown(TimeSpan.FromDays(1));

            //Validate the start date:
            //1. Check range;
            if (start < (new DateTime(1900, 01, 01)))
            {
                throw new ArgumentOutOfRangeException(nameof(start), "Please select a start date after January 1st, 1900.");
            }

            //2. Check future date
            var todayInAlgorithmTimeZone = DateTime.UtcNow.ConvertFromUtc(TimeZone).Date;
            if (start > todayInAlgorithmTimeZone)
            {
                throw new ArgumentOutOfRangeException(nameof(start), "Please select start date less than today");
            }

            //3. Check not locked already:
            if (!_locked)
            {
                _startDate = start;
                SetDateTime(_startDate.ConvertToUtc(TimeZone));
            }
            else
            {
                throw new InvalidOperationException("Algorithm.SetStartDate(): Cannot change start date after algorithm initialized.");
            }
        }

        /// <summary>
        /// Set the end date for a backtest.
        /// </summary>
        /// <param name="end">Datetime value for end date</param>
        /// <remarks>Must be greater than the start date</remarks>
        /// <seealso cref="SetEndDate(int, int, int)"/>
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

            //2. Make this at the very end of the requested date
            end = end.RoundDown(TimeSpan.FromDays(1)).AddDays(1).AddTicks(-1);

            //3. Check not locked already:
            if (!_locked)
            {
                _endDate = end;
            }
            else
            {
                throw new InvalidOperationException("Algorithm.SetEndDate(): Cannot change end date after algorithm initialized.");
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
                Securities.SetLiveMode(live);
                if (live)
                {
                    // startDate is set relative to the algorithm's timezone.
                    _startDate = DateTime.UtcNow.ConvertFromUtc(TimeZone).Date;
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
        /// <param name="ticker">The security ticker</param>
        /// <param name="resolution">Resolution of the Data Required</param>
        /// <param name="fillDataForward">When no data available on a tradebar, return the last data that was generated</param>
        /// <param name="extendedMarketHours">Show the after market data as well</param>
        public Security AddSecurity(SecurityType securityType, string ticker, Resolution? resolution = null, bool fillDataForward = true, bool extendedMarketHours = false)
        {
            return AddSecurity(securityType, ticker, resolution, fillDataForward, Security.NullLeverage, extendedMarketHours);
        }

        /// <summary>
        /// Add specified data to required list. QC will funnel this data to the handle data routine.
        /// </summary>
        /// <param name="securityType">MarketType Type: Equity, Commodity, Future, FOREX or Crypto</param>
        /// <param name="ticker">The security ticker</param>
        /// <param name="resolution">Resolution of the Data Required</param>
        /// <param name="fillDataForward">When no data available on a tradebar, return the last data that was generated</param>
        /// <param name="leverage">Custom leverage per security</param>
        /// <param name="extendedMarketHours">Extended market hours</param>
        /// <remarks> AddSecurity(SecurityType securityType, Symbol symbol, Resolution resolution, bool fillDataForward, decimal leverage, bool extendedMarketHours)</remarks>
        public Security AddSecurity(SecurityType securityType, string ticker, Resolution? resolution, bool fillDataForward, decimal leverage, bool extendedMarketHours)
        {
            return AddSecurity(securityType, ticker, resolution, null, fillDataForward, leverage, extendedMarketHours);
        }

        /// <summary>
        /// Set a required SecurityType-symbol and resolution for algorithm
        /// </summary>
        /// <param name="securityType">MarketType Type: Equity, Commodity, Future, FOREX or Crypto</param>
        /// <param name="ticker">The security ticker, e.g. AAPL</param>
        /// <param name="resolution">Resolution of the MarketType required: MarketData, Second or Minute</param>
        /// <param name="market">The market the requested security belongs to, such as 'usa' or 'fxcm'</param>
        /// <param name="fillDataForward">If true, returns the last available data even if none in that timeslice.</param>
        /// <param name="leverage">leverage for this security</param>
        /// <param name="extendedMarketHours">ExtendedMarketHours send in data from 4am - 8pm, not used for FOREX</param>
        public Security AddSecurity(SecurityType securityType, string ticker, Resolution? resolution, string market, bool fillDataForward, decimal leverage, bool extendedMarketHours)
        {
            // if AddSecurity method is called to add an option or a future, we delegate a call to respective methods
            if (securityType == SecurityType.Option)
            {
                return AddOption(ticker, resolution, market, fillDataForward, leverage);
            }

            if (securityType == SecurityType.Future)
            {
                return AddFuture(ticker, resolution, market, fillDataForward, leverage);
            }

            try
            {
                if (market == null)
                {
                    if (!BrokerageModel.DefaultMarkets.TryGetValue(securityType, out market))
                    {
                        throw new KeyNotFoundException($"No default market set for security type: {securityType}");
                    }
                }

                Symbol symbol;
                if (!SymbolCache.TryGetSymbol(ticker, out symbol) ||
                    symbol.ID.Market != market ||
                    symbol.SecurityType != securityType)
                {
                    symbol = QuantConnect.Symbol.Create(ticker, securityType, market);
                }

                return AddSecurity(symbol, resolution, fillDataForward, leverage, extendedMarketHours);
            }
            catch (Exception err)
            {
                Error("Algorithm.AddSecurity(): " + err);
                return null;
            }
        }

        /// <summary>
        /// Set a required SecurityType-symbol and resolution for algorithm
        /// </summary>
        /// <param name="symbol">The security Symbol</param>
        /// <param name="resolution">Resolution of the MarketType required: MarketData, Second or Minute</param>
        /// <param name="fillDataForward">If true, returns the last available data even if none in that timeslice.</param>
        /// <param name="leverage">leverage for this security</param>
        /// <param name="extendedMarketHours">ExtendedMarketHours send in data from 4am - 8pm, not used for FOREX</param>
        /// <returns>The new Security that was added to the algorithm</returns>
        public Security AddSecurity(Symbol symbol, Resolution? resolution = null, bool fillDataForward = true, decimal leverage = Security.NullLeverage, bool extendedMarketHours = false)
        {
            var isCanonical = symbol.IsCanonical();

            // Short-circuit to AddOptionContract because it will add the underlying if required
            if (!isCanonical && (symbol.SecurityType == SecurityType.Option || symbol.SecurityType == SecurityType.FutureOption))
            {
                return AddOptionContract(symbol, resolution, fillDataForward, leverage);
            }

            var configs = SubscriptionManager.SubscriptionDataConfigService.Add(symbol,
                resolution,
                fillDataForward,
                extendedMarketHours,
                isFilteredSubscription: !isCanonical);

            var security = Securities.CreateSecurity(symbol, configs, leverage);

            if (isCanonical)
            {
                security.IsTradable = false;
                Securities.Add(security);

                // add this security to the user defined universe
                Universe universe;
                if (!UniverseManager.TryGetValue(symbol, out universe) && _pendingUniverseAdditions.All(u => u.Configuration.Symbol != symbol))
                {
                    var settings = new UniverseSettings(configs.First().Resolution, leverage, true, false, TimeSpan.Zero);
                    if (symbol.SecurityType == SecurityType.Option || symbol.SecurityType == SecurityType.FutureOption)
                    {
                        universe = new OptionChainUniverse((Option)security, settings, LiveMode);
                    }
                    else
                    {
                        universe = new FuturesChainUniverse((Future)security, settings);
                    }

                    AddUniverse(universe);
                }
                return security;
            }

            AddToUserDefinedUniverse(security, configs);
            return security;
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
        public Equity AddEquity(string ticker, Resolution? resolution = null, string market = null, bool fillDataForward = true, decimal leverage = Security.NullLeverage, bool extendedMarketHours = false)
        {
            return AddSecurity<Equity>(SecurityType.Equity, ticker, resolution, market, fillDataForward, leverage, extendedMarketHours);
        }

        /// <summary>
        /// Creates and adds a new equity <see cref="Option"/> security to the algorithm
        /// </summary>
        /// <param name="underlying">The underlying equity ticker</param>
        /// <param name="resolution">The <see cref="Resolution"/> of market data, Tick, Second, Minute, Hour, or Daily. Default is <see cref="Resolution.Minute"/></param>
        /// <param name="market">The equity's market, <seealso cref="Market"/>. Default is value null and looked up using BrokerageModel.DefaultMarkets in <see cref="AddSecurity{T}"/></param>
        /// <param name="fillDataForward">If true, returns the last available data even if none in that timeslice. Default is <value>true</value></param>
        /// <param name="leverage">The requested leverage for this equity. Default is set by <see cref="SecurityInitializer"/></param>
        /// <returns>The new <see cref="Option"/> security</returns>
        public Option AddOption(string underlying, Resolution? resolution = null, string market = null, bool fillDataForward = true, decimal leverage = Security.NullLeverage)
        {
            if (market == null)
            {
                if (!BrokerageModel.DefaultMarkets.TryGetValue(SecurityType.Option, out market))
                {
                    throw new KeyNotFoundException($"No default market set for security type: {SecurityType.Option}");
                }
            }

            var underlyingSymbol = QuantConnect.Symbol.Create(underlying, SecurityType.Equity, market);
            return AddOption(underlyingSymbol, resolution, market, fillDataForward, leverage);
        }

        /// <summary>
        /// Creates and adds a new <see cref="Option"/> security to the algorithm.
        /// This method can be used to add options with non-equity asset classes
        /// to the algorithm (e.g. Future Options).
        /// </summary>
        /// <param name="underlying">Underlying asset Symbol to use as the option's underlying</param>
        /// <param name="resolution">The <see cref="Resolution"/> of market data, Tick, Second, Minute, Hour, or Daily. Default is <see cref="Resolution.Minute"/></param>
        /// <param name="market">The option's market, <seealso cref="Market"/>. Default value is null, but will be resolved using BrokerageModel.DefaultMarkets in <see cref="AddSecurity{T}"/></param>
        /// <param name="fillDataForward">If true, data will be provided to the algorithm every Second, Minute, Hour, or Day, while the asset is open and depending on the Resolution this option was configured to use.</param>
        /// <param name="leverage">The requested leverage for the </param>
        /// <returns></returns>
        /// <exception cref="KeyNotFoundException"></exception>
        public Option AddOption(Symbol underlying, Resolution? resolution = null, string market = null, bool fillDataForward = true, decimal leverage = Security.NullLeverage)
        {
            var optionType = SecurityType.Option;
            if (underlying.SecurityType == SecurityType.Future)
            {
                optionType = SecurityType.FutureOption;
            }

            if (market == null)
            {
                if (!BrokerageModel.DefaultMarkets.TryGetValue(optionType, out market))
                {
                    throw new KeyNotFoundException($"No default market set for security type: {optionType}");
                }
            }

            Symbol canonicalSymbol;
            var alias = "?" + underlying.Value;
            if (!SymbolCache.TryGetSymbol(alias, out canonicalSymbol) ||
                canonicalSymbol.ID.Market != market ||
                (canonicalSymbol.SecurityType != SecurityType.Option &&
                canonicalSymbol.SecurityType != SecurityType.FutureOption))
            {
                canonicalSymbol = QuantConnect.Symbol.CreateOption(
                    underlying,
                    underlying.ID.Market,
                    default(OptionStyle),
                    default(OptionRight),
                    0,
                    SecurityIdentifier.DefaultDate,
                    alias);
            }

            return (Option)AddSecurity(canonicalSymbol, resolution, fillDataForward, leverage);
        }

        /// <summary>
        /// Creates and adds a new <see cref="Future"/> security to the algorithm
        /// </summary>
        /// <param name="ticker">The future ticker</param>
        /// <param name="resolution">The <see cref="Resolution"/> of market data, Tick, Second, Minute, Hour, or Daily. Default is <see cref="Resolution.Minute"/></param>
        /// <param name="market">The futures market, <seealso cref="Market"/>. Default is value null and looked up using BrokerageModel.DefaultMarkets in <see cref="AddSecurity{T}"/></param>
        /// <param name="fillDataForward">If true, returns the last available data even if none in that timeslice. Default is <value>true</value></param>
        /// <param name="leverage">The requested leverage for this equity. Default is set by <see cref="SecurityInitializer"/></param>
        /// <returns>The new <see cref="Future"/> security</returns>
        public Future AddFuture(string ticker, Resolution? resolution = null, string market = null, bool fillDataForward = true, decimal leverage = Security.NullLeverage)
        {
            if (market == null)
            {
                if (!SymbolPropertiesDatabase.TryGetMarket(ticker, SecurityType.Future, out market)
                    && !BrokerageModel.DefaultMarkets.TryGetValue(SecurityType.Future, out market))
                {
                    throw new KeyNotFoundException($"No default market set for security type: {SecurityType.Future}");
                }
            }

            Symbol canonicalSymbol;
            var alias = "/" + ticker;
            if (!SymbolCache.TryGetSymbol(alias, out canonicalSymbol) ||
                canonicalSymbol.ID.Market != market ||
                canonicalSymbol.SecurityType != SecurityType.Future)
            {
                canonicalSymbol = QuantConnect.Symbol.Create(ticker, SecurityType.Future, market, alias);
            }

            return (Future)AddSecurity(canonicalSymbol, resolution, fillDataForward, leverage);
        }

        /// <summary>
        /// Creates and adds a new single <see cref="Future"/> contract to the algorithm
        /// </summary>
        /// <param name="symbol">The futures contract symbol</param>
        /// <param name="resolution">The <see cref="Resolution"/> of market data, Tick, Second, Minute, Hour, or Daily. Default is <see cref="Resolution.Minute"/></param>
        /// <param name="fillDataForward">If true, returns the last available data even if none in that timeslice. Default is <value>true</value></param>
        /// <param name="leverage">The requested leverage for this equity. Default is set by <see cref="SecurityInitializer"/></param>
        /// <returns>The new <see cref="Future"/> security</returns>
        public Future AddFutureContract(Symbol symbol, Resolution? resolution = null, bool fillDataForward = true, decimal leverage = Security.NullLeverage)
        {
            return (Future)AddSecurity(symbol, resolution, fillDataForward, leverage);
        }

        /// <summary>
        /// Creates and adds a new Future Option contract to the algorithm.
        /// </summary>
        /// <param name="symbol">The <see cref="Future"/> canonical symbol (i.e. Symbol returned from <see cref="AddFuture"/>)</param>
        /// <param name="optionFilter">Filter to apply to option contracts loaded as part of the universe</param>
        /// <returns>The new <see cref="Option"/> security, containing a <see cref="Future"/> as its underlying.</returns>
        /// <exception cref="ArgumentException">The symbol provided is not canonical.</exception>
        public void AddFutureOption(Symbol symbol, Func<OptionFilterUniverse, OptionFilterUniverse> optionFilter = null)
        {
            if (!symbol.IsCanonical())
            {
                throw new ArgumentException("Symbol provided must be canonical (i.e. the Symbol returned from AddFuture(), not AddFutureContract().");
            }

            AddUniverseOptions(symbol, optionFilter);
        }

        /// <summary>
        /// Adds a future option contract to the algorithm.
        /// </summary>
        /// <param name="symbol">Option contract Symbol</param>
        /// <param name="resolution">Resolution of the option contract, i.e. the granularity of the data</param>
        /// <param name="fillDataForward">If true, this will fill in missing data points with the previous data point</param>
        /// <param name="leverage">The leverage to apply to the option contract</param>
        /// <returns>Option security</returns>
        /// <exception cref="ArgumentException">Symbol is canonical (i.e. a generic Symbol returned from <see cref="AddFuture"/> or <see cref="AddOption"/>)</exception>
        public Option AddFutureOptionContract(Symbol symbol, Resolution? resolution = null, bool fillDataForward = true, decimal leverage = Security.NullLeverage)
        {
            if (symbol.IsCanonical())
            {
                throw new ArgumentException("Expected non-canonical Symbol (i.e. a Symbol representing a specific Future contract");
            }

            return AddOptionContract(symbol, resolution, fillDataForward, leverage);
        }

        /// <summary>
        /// Creates and adds a new single <see cref="Option"/> contract to the algorithm
        /// </summary>
        /// <param name="symbol">The option contract symbol</param>
        /// <param name="resolution">The <see cref="Resolution"/> of market data, Tick, Second, Minute, Hour, or Daily. Default is <see cref="Resolution.Minute"/></param>
        /// <param name="fillDataForward">If true, returns the last available data even if none in that timeslice. Default is <value>true</value></param>
        /// <param name="leverage">The requested leverage for this equity. Default is set by <see cref="SecurityInitializer"/></param>
        /// <returns>The new <see cref="Option"/> security</returns>
        public Option AddOptionContract(Symbol symbol, Resolution? resolution = null, bool fillDataForward = true, decimal leverage = Security.NullLeverage)
        {
            var configs = SubscriptionManager.SubscriptionDataConfigService.Add(symbol, resolution, fillDataForward, dataNormalizationMode:DataNormalizationMode.Raw);
            var option = (Option)Securities.CreateSecurity(symbol, configs, leverage);
            // add underlying if not present
            var underlying = option.Symbol.Underlying;
            Security underlyingSecurity;
            List<SubscriptionDataConfig> underlyingConfigs;
            if (!Securities.TryGetValue(underlying, out underlyingSecurity))
            {
                underlyingSecurity = AddSecurity(underlying, resolution, fillDataForward, leverage);
                underlyingConfigs = SubscriptionManager.SubscriptionDataConfigService
                    .GetSubscriptionDataConfigs(underlying);
            }
            else
            {
                underlyingConfigs = SubscriptionManager.SubscriptionDataConfigService
                    .GetSubscriptionDataConfigs(underlying);

                var dataNormalizationMode = underlyingConfigs.DataNormalizationMode();
                if (dataNormalizationMode != DataNormalizationMode.Raw && _locked)
                {
                    // We check the "locked" flag here because during initialization we need to load existing open orders and holdings from brokerages.
                    // There is no data streaming yet, so it is safe to change the data normalization mode to Raw.
                    throw new ArgumentException($"The underlying equity asset ({underlying.Value}) is set to " +
                        $"{dataNormalizationMode}, please change this to DataNormalizationMode.Raw with the " +
                        "SetDataNormalization() method"
                    );
                }
            }

            underlyingConfigs.SetDataNormalizationMode(DataNormalizationMode.Raw);
            // For backward compatibility we need to refresh the security DataNormalizationMode Property
            underlyingSecurity.RefreshDataNormalizationModeProperty();

            option.Underlying = underlyingSecurity;
            Securities.Add(option);

            // get or create the universe
            var universeSymbol = OptionContractUniverse.CreateSymbol(symbol.ID.Market, symbol.Underlying.SecurityType);
            Universe universe;
            if (!UniverseManager.TryGetValue(universeSymbol, out universe))
            {
                universe = _pendingUniverseAdditions.FirstOrDefault(u => u.Configuration.Symbol == universeSymbol)
                           ?? AddUniverse(new OptionContractUniverse(new SubscriptionDataConfig(configs.First(), symbol: universeSymbol), UniverseSettings));
            }

            // update the universe
            var optionUniverse = universe as OptionContractUniverse;
            if (optionUniverse != null)
            {
                foreach (var subscriptionDataConfig in configs.Concat(underlyingConfigs))
                {
                    optionUniverse.Add(subscriptionDataConfig);
                }
            }

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
        public Forex AddForex(string ticker, Resolution? resolution = null, string market = null, bool fillDataForward = true, decimal leverage = Security.NullLeverage)
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
        public Cfd AddCfd(string ticker, Resolution? resolution = null, string market = null, bool fillDataForward = true, decimal leverage = Security.NullLeverage)
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
        public Crypto AddCrypto(string ticker, Resolution? resolution = null, string market = null, bool fillDataForward = true, decimal leverage = Security.NullLeverage)
        {
            return AddSecurity<Crypto>(SecurityType.Crypto, ticker, resolution, market, fillDataForward, leverage, false);
        }

        /// <summary>
        /// Removes the security with the specified symbol. This will cancel all
        /// open orders and then liquidate any existing holdings
        /// </summary>
        /// <param name="symbol">The symbol of the security to be removed</param>
        /// <remarks>Sugar syntax for <see cref="AddOptionContract"/></remarks>
        public bool RemoveOptionContract(Symbol symbol)
        {
            return RemoveSecurity(symbol);
        }

        /// <summary>
        /// Removes the security with the specified symbol. This will cancel all
        /// open orders and then liquidate any existing holdings
        /// </summary>
        /// <param name="symbol">The symbol of the security to be removed</param>
        public bool RemoveSecurity(Symbol symbol)
        {
            Security security;
            if (!Securities.TryGetValue(symbol, out security))
            {
                return false;
            }

            // cancel open orders
            Transactions.CancelOpenOrders(security.Symbol);

            // liquidate if invested
            if (security.Invested)
            {
                Liquidate(security.Symbol);
            }

            // Clear cache
            security.Cache.Reset();

            // Mark security as not tradable
            security.IsTradable = false;
            if (symbol.IsCanonical())
            {
                // remove underlying equity data if it's marked as internal
                var universe = UniverseManager.Select(x => x.Value).FirstOrDefault(x => x.Configuration.Symbol == symbol);
                if (universe != null)
                {
                    // remove underlying if not used by other universes
                    var otherUniverses = UniverseManager.Select(ukvp => ukvp.Value).Where(u => !ReferenceEquals(u, universe)).ToList();
                    if (symbol.HasUnderlying)
                    {
                        var underlying = Securities[symbol.Underlying];
                        if (!otherUniverses.Any(u => u.Members.ContainsKey(underlying.Symbol)))
                        {
                            RemoveSecurity(underlying.Symbol);
                        }
                    }

                    // remove child securities (option contracts for option chain universes) if not used in other universes
                    foreach (var child in universe.Members.Values)
                    {
                        if (!otherUniverses.Any(u => u.Members.ContainsKey(child.Symbol)))
                        {
                            RemoveSecurity(child.Symbol);
                        }
                    }

                    // finally, dispose and remove the canonical security from the universe manager
                    UniverseManager.Remove(symbol);
                    _userAddedUniverses.Remove(symbol);
                }
            }
            else
            {
                var universe = UniverseManager.Select(x => x.Value).OfType<UserDefinedUniverse>().FirstOrDefault(x => x.Members.ContainsKey(symbol));
                universe?.Remove(symbol);
            }

            return true;
        }

        /// <summary>
        /// AddData<typeparam name="T"/> a new user defined data source, requiring only the minimum config options.
        /// The data is added with a default time zone of NewYork (Eastern Daylight Savings Time)
        /// </summary>
        /// <param name="ticker">Key/Ticker for data</param>
        /// <param name="resolution">Resolution of the data</param>
        /// <returns>The new <see cref="Security"/></returns>
        /// <remarks>Generic type T must implement base data</remarks>
        public Security AddData<T>(string ticker, Resolution? resolution = null)
            where T : IBaseData, new()
        {
            //Add this new generic data as a tradeable security:
            // Defaults:extended market hours"      = true because we want events 24 hours,
            //          fillforward                 = false because only want to trigger when there's new custom data.
            //          leverage                    = 1 because no leverage on nonmarket data?
            return AddData<T>(ticker, resolution, fillDataForward: false, leverage: 1m);
        }

        /// <summary>
        /// AddData<typeparam name="T"/> a new user defined data source, requiring only the minimum config options.
        /// The data is added with a default time zone of NewYork (Eastern Daylight Savings Time)
        /// </summary>
        /// <param name="underlying">The underlying symbol for the custom data</param>
        /// <param name="resolution">Resolution of the data</param>
        /// <returns>The new <see cref="Security"/></returns>
        /// <remarks>Generic type T must implement base data</remarks>
        public Security AddData<T>(Symbol underlying, Resolution? resolution = null)
            where T : IBaseData, new()
        {
            //Add this new generic data as a tradeable security:
            // Defaults:extended market hours"      = true because we want events 24 hours,
            //          fillforward                 = false because only want to trigger when there's new custom data.
            //          leverage                    = 1 because no leverage on nonmarket data?
            return AddData<T>(underlying, resolution, fillDataForward: false, leverage: 1m);
        }


        /// <summary>
        /// AddData<typeparam name="T"/> a new user defined data source, requiring only the minimum config options.
        /// The data is added with a default time zone of NewYork (Eastern Daylight Savings Time)
        /// </summary>
        /// <param name="ticker">Key/Ticker for data</param>
        /// <param name="resolution">Resolution of the Data Required</param>
        /// <param name="fillDataForward">When no data available on a tradebar, return the last data that was generated</param>
        /// <param name="leverage">Custom leverage per security</param>
        /// <returns>The new <see cref="Security"/></returns>
        /// <remarks>Generic type T must implement base data</remarks>
        public Security AddData<T>(string ticker, Resolution? resolution, bool fillDataForward, decimal leverage = 1.0m)
            where T : IBaseData, new()
        {
            return AddData<T>(ticker, resolution, null, fillDataForward, leverage);
        }

        /// <summary>
        /// AddData<typeparam name="T"/> a new user defined data source, requiring only the minimum config options.
        /// The data is added with a default time zone of NewYork (Eastern Daylight Savings Time)
        /// </summary>
        /// <param name="underlying">The underlying symbol for the custom data</param>
        /// <param name="resolution">Resolution of the Data Required</param>
        /// <param name="fillDataForward">When no data available on a tradebar, return the last data that was generated</param>
        /// <param name="leverage">Custom leverage per security</param>
        /// <returns>The new <see cref="Security"/></returns>
        /// <remarks>Generic type T must implement base data</remarks>
        public Security AddData<T>(Symbol underlying, Resolution? resolution, bool fillDataForward, decimal leverage = 1.0m)
            where T : IBaseData, new()
        {
            return AddData<T>(underlying, resolution, null, fillDataForward, leverage);
        }

        /// <summary>
        /// AddData<typeparam name="T"/> a new user defined data source, requiring only the minimum config options.
        /// </summary>
        /// <param name="ticker">Key/Ticker for data</param>
        /// <param name="resolution">Resolution of the Data Required</param>
        /// <param name="timeZone">Specifies the time zone of the raw data</param>
        /// <param name="fillDataForward">When no data available on a tradebar, return the last data that was generated</param>
        /// <param name="leverage">Custom leverage per security</param>
        /// <returns>The new <see cref="Security"/></returns>
        /// <remarks>Generic type T must implement base data</remarks>
        public Security AddData<T>(string ticker, Resolution? resolution, DateTimeZone timeZone, bool fillDataForward = false, decimal leverage = 1.0m)
            where T : IBaseData, new()
        {
            return AddData(typeof(T), ticker, resolution, timeZone, fillDataForward, leverage);
        }

        /// <summary>
        /// AddData<typeparam name="T"/> a new user defined data source, requiring only the minimum config options.
        /// </summary>
        /// <param name="underlying">The underlying symbol for the custom data</param>
        /// <param name="resolution">Resolution of the Data Required</param>
        /// <param name="timeZone">Specifies the time zone of the raw data</param>
        /// <param name="fillDataForward">When no data available on a tradebar, return the last data that was generated</param>
        /// <param name="leverage">Custom leverage per security</param>
        /// <returns>The new <see cref="Security"/></returns>
        /// <remarks>Generic type T must implement base data</remarks>
        public Security AddData<T>(Symbol underlying, Resolution? resolution, DateTimeZone timeZone, bool fillDataForward = false, decimal leverage = 1.0m)
            where T : IBaseData, new()
        {
            return AddData(typeof(T), underlying, resolution, timeZone, fillDataForward, leverage);
        }

        /// <summary>
        /// AddData<typeparam name="T"/> a new user defined data source including symbol properties and exchange hours,
        /// all other vars are not required and will use defaults.
        /// </summary>
        /// <param name="ticker">Key/Ticker for data</param>
        /// <param name="properties">The properties of this new custom data</param>
        /// <param name="exchangeHours">The Exchange hours of this symbol</param>
        /// <param name="resolution">Resolution of the Data Required</param>
        /// <param name="fillDataForward">When no data available on a tradebar, return the last data that was generated</param>
        /// <param name="leverage">Custom leverage per security</param>
        /// <returns>The new <see cref="Security"/></returns>
        public Security AddData<T>(string ticker, SymbolProperties properties, SecurityExchangeHours exchangeHours, Resolution? resolution = null, bool fillDataForward = false, decimal leverage = 1.0m)
            where T : IBaseData, new()
        {
            // Get the right key for storage of base type symbols
            var key = SecurityIdentifier.GenerateBaseSymbol(typeof(T), ticker);

            // Set our database entries for this data type
            SetDatabaseEntries(key, properties, exchangeHours);

            // Then add the data
            return AddData(typeof(T), ticker, resolution, null, fillDataForward, leverage);
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
        /// Send a debug message to the web console:
        /// </summary>
        /// <param name="message">Message to send to debug console</param>
        /// <seealso cref="Log(int)"/>
        /// <seealso cref="Error(int)"/>
        public void Debug(int message)
        {
            Debug(message.ToStringInvariant());
        }

        /// <summary>
        /// Send a debug message to the web console:
        /// </summary>
        /// <param name="message">Message to send to debug console</param>
        /// <seealso cref="Log(double)"/>
        /// <seealso cref="Error(double)"/>
        public void Debug(double message)
        {
            Debug(message.ToStringInvariant());
        }

        /// <summary>
        /// Send a debug message to the web console:
        /// </summary>
        /// <param name="message">Message to send to debug console</param>
        /// <seealso cref="Log(decimal)"/>
        /// <seealso cref="Error(decimal)"/>
        public void Debug(decimal message)
        {
            Debug(message.ToStringInvariant());
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
        /// Added another method for logging if user guessed.
        /// </summary>
        /// <param name="message">Int message to log.</param>
        /// <seealso cref="Debug(int)"/>
        /// <seealso cref="Error(int)"/>
        public void Log(int message)
        {
            Log(message.ToStringInvariant());
        }

        /// <summary>
        /// Added another method for logging if user guessed.
        /// </summary>
        /// <param name="message">Double message to log.</param>
        /// <seealso cref="Debug(double)"/>
        /// <seealso cref="Error(double)"/>
        public void Log(double message)
        {
            Log(message.ToStringInvariant());
        }

        /// <summary>
        /// Added another method for logging if user guessed.
        /// </summary>
        /// <param name="message">Decimal message to log.</param>
        /// <seealso cref="Debug(decimal)"/>
        /// <seealso cref="Error(decimal)"/>
        public void Log(decimal message)
        {
            Log(message.ToStringInvariant());
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
        /// Send a int error message to the Console.
        /// </summary>
        /// <param name="message">Message to display in errors grid</param>
        /// <seealso cref="Debug(int)"/>
        /// <seealso cref="Log(int)"/>
        public void Error(int message)
        {
            Error(message.ToStringInvariant());
        }

        /// <summary>
        /// Send a double error message to the Console.
        /// </summary>
        /// <param name="message">Message to display in errors grid</param>
        /// <seealso cref="Debug(double)"/>
        /// <seealso cref="Log(double)"/>
        public void Error(double message)
        {
            Error(message.ToStringInvariant());
        }

        /// <summary>
        /// Send a decimal error message to the Console.
        /// </summary>
        /// <param name="message">Message to display in errors grid</param>
        /// <seealso cref="Debug(decimal)"/>
        /// <seealso cref="Log(decimal)"/>
        public void Error(decimal message)
        {
            Error(message.ToStringInvariant());
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
        /// <seealso cref="Quit(String)"/>
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
        private T AddSecurity<T>(SecurityType securityType, string ticker, Resolution? resolution, string market, bool fillDataForward, decimal leverage, bool extendedMarketHours)
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
            if (!SymbolCache.TryGetSymbol(ticker, out symbol) ||
                symbol.ID.Market != market ||
                symbol.SecurityType != securityType)
            {
                symbol = QuantConnect.Symbol.Create(ticker, securityType, market);
            }

            var configs = SubscriptionManager.SubscriptionDataConfigService.Add(symbol, resolution, fillDataForward, extendedMarketHours);
            var security = Securities.CreateSecurity(symbol, configs, leverage);

            AddToUserDefinedUniverse(security, configs);
            return (T)security;
        }

        /// <summary>
        /// Set the historical data provider
        /// </summary>
        /// <param name="historyProvider">Historical data provider</param>
        public void SetHistoryProvider(IHistoryProvider historyProvider)
        {
            if (historyProvider == null)
            {
                throw new ArgumentNullException(nameof(historyProvider), "Algorithm.SetHistoryProvider(): Historical data provider cannot be null.");
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
                throw new ArgumentNullException(nameof(exception), "Algorithm.SetRunTimeError(): Algorithm.RunTimeError cannot be set to null.");
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
        /// <returns>The requested resource as a <see cref="string"/></returns>
        public string Download(string address) => Download(address, Enumerable.Empty<KeyValuePair<string, string>>());

        /// <summary>
        /// Downloads the requested resource as a <see cref="string"/>.
        /// The resource to download is specified as a <see cref="string"/> containing the URI.
        /// </summary>
        /// <param name="address">A string containing the URI to download</param>
        /// <param name="headers">Defines header values to add to the request</param>
        /// <returns>The requested resource as a <see cref="string"/></returns>
        public string Download(string address, IEnumerable<KeyValuePair<string, string>> headers) => Download(address, headers, null, null);

        /// <summary>
        /// Downloads the requested resource as a <see cref="string"/>.
        /// The resource to download is specified as a <see cref="string"/> containing the URI.
        /// </summary>
        /// <param name="address">A string containing the URI to download</param>
        /// <param name="headers">Defines header values to add to the request</param>
        /// <param name="userName">The user name associated with the credentials</param>
        /// <param name="password">The password for the user name associated with the credentials</param>
        /// <returns>The requested resource as a <see cref="string"/></returns>
        public string Download(string address, IEnumerable<KeyValuePair<string, string>> headers, string userName, string password)
        {
            return _api.Download(address, headers, userName, password);
        }

        /// <summary>
        /// Schedules the provided training code to execute immediately
        /// </summary>
        /// <param name="trainingCode">The training code to be invoked</param>
        public ScheduledEvent Train(Action trainingCode)
        {
            return Schedule.TrainingNow(trainingCode);
        }

        /// <summary>
        /// Schedules the training code to run using the specified date and time rules
        /// </summary>
        /// <param name="dateRule">Specifies what dates the event should run</param>
        /// <param name="timeRule">Specifies the times on those dates the event should run</param>
        /// <param name="trainingCode">The training code to be invoked</param>
        public ScheduledEvent Train(IDateRule dateRule, ITimeRule timeRule, Action trainingCode)
        {
            return Schedule.Training(dateRule, timeRule, trainingCode);
        }

        /// <summary>
        /// Event invocator for the <see cref="InsightsGenerated"/> event
        /// </summary>
        /// <param name="insights">The collection of insights generaed at the current time step</param>
        /// <param name="clone">Will emit a clone of the generated insights</param>
        private void OnInsightsGenerated(Insight[] insights, bool clone = true)
        {
            // debug printing of generated insights
            if (DebugMode)
            {
                Log($"{Time}: ALPHA: {string.Join(" | ", insights.Select(i => i.ToString()).OrderBy(i => i))}");
            }

            InsightsGenerated?.Invoke(this, new GeneratedInsightsCollection(UtcTime, insights, clone: clone));
        }

        /// <summary>
        /// Sets the current slice
        /// </summary>
        /// <param name="slice">The Slice object</param>
        public void SetCurrentSlice(Slice slice)
        {
            CurrentSlice = slice;
        }


        /// <summary>
        /// Provide the API for the algorithm.
        /// </summary>
        /// <param name="api">Initiated API</param>
        public void SetApi(IApi api)
        {
            _api = api;
        }

        /// <summary>
        /// Sets the object store
        /// </summary>
        /// <param name="objectStore">The object store</param>
        public void SetObjectStore(IObjectStore objectStore)
        {
            ObjectStore = new ObjectStore(objectStore);
        }

        /// <summary>
        /// Determines if the Symbol is shortable at the brokerage
        /// </summary>
        /// <param name="symbol">Symbol to check if shortable</param>
        /// <returns>True if shortable</returns>
        public bool Shortable(Symbol symbol)
        {
            return Shortable(symbol, 0);
        }

        /// <summary>
        /// Determines if the Symbol is shortable at the brokerage
        /// </summary>
        /// <param name="symbol">Symbol to check if shortable</param>
        /// <param name="shortQuantity">Order's quantity to check if it is currently shortable, taking into account current holdings and open orders</param>
        /// <returns>True if shortable</returns>
        public bool Shortable(Symbol symbol, decimal shortQuantity)
        {
            var shortableQuantity = BrokerageModel.GetShortableProvider().ShortableQuantity(symbol, Time);
            if (shortableQuantity == null)
            {
                return true;
            }

            var openOrderQuantity = Transactions.GetOpenOrdersRemainingQuantity(symbol);
            var portfolioQuantity = Portfolio.ContainsKey(symbol) ? Portfolio[symbol].Quantity : 0;
            // We check portfolio and open orders beforehand to ensure that orderQuantity == 0 case does not return
            // a true result whenever we have no more shares left to short.
            if (portfolioQuantity + openOrderQuantity <= -shortableQuantity)
            {
                return false;
            }

            shortQuantity = -Math.Abs(shortQuantity);
            return portfolioQuantity + shortQuantity + openOrderQuantity >= -shortableQuantity;
        }

        /// <summary>
        /// Gets the quantity shortable for the given asset
        /// </summary>
        /// <returns>
        /// Quantity shortable for the given asset. Zero if not
        /// shortable, or a number greater than zero if shortable.
        /// </returns>
        public long ShortableQuantity(Symbol symbol)
        {
            var shortableSymbols = AllShortableSymbols();
            return shortableSymbols.ContainsKey(symbol) ? shortableSymbols[symbol] : 0;
        }

        /// <summary>
        /// Gets all Symbols that are shortable, as well as the quantity shortable for them
        /// </summary>
        /// <returns>All shortable Symbols, null if all Symbols are shortable</returns>
        public Dictionary<Symbol, long> AllShortableSymbols()
        {
            return BrokerageModel.GetShortableProvider().AllShortableSymbols(Time);
        }
        
        /// Set the properties and exchange hours for a given key into our databases
        /// </summary>
        /// <param name="key">Key for database storage</param>
        /// <param name="properties">Properties to store</param>
        /// <param name="exchangeHours">Exchange hours to store</param>
        private void SetDatabaseEntries(string key, SymbolProperties properties, SecurityExchangeHours exchangeHours)
        {
            // Add entries to our Symbol Properties DB and MarketHours DB
            SymbolPropertiesDatabase.SetEntry(Market.USA, key, SecurityType.Base, properties);
            MarketHoursDatabase.SetEntry(Market.USA, key, SecurityType.Base, exchangeHours);
        }
    }
}
