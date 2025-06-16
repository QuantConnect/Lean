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
using System.Globalization;
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
using QuantConnect.Securities.IndexOption;
using QuantConnect.Securities.Option;
using QuantConnect.Statistics;
using QuantConnect.Util;
using QuantConnect.Data.Market;
using QuantConnect.Data.Fundamental;
using System.Collections.Concurrent;
using QuantConnect.Securities.Future;
using QuantConnect.Securities.Crypto;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Execution;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Algorithm.Framework.Risk;
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Algorithm.Selection;
using QuantConnect.Storage;
using Index = QuantConnect.Securities.Index.Index;
using QuantConnect.Securities.CryptoFuture;
using QuantConnect.Algorithm.Framework.Alphas.Analysis;
using QuantConnect.Algorithm.Framework.Portfolio.SignalExports;
using Python.Runtime;
using QuantConnect.Commands;
using Newtonsoft.Json;
using QuantConnect.Securities.Index;
using QuantConnect.Api;
using System.Threading.Tasks;

namespace QuantConnect.Algorithm
{
    /// <summary>
    /// QC Algorithm Base Class - Handle the basic requirements of a trading algorithm,
    /// allowing user to focus on event methods. The QCAlgorithm class implements Portfolio,
    /// Securities, Transactions and Data Subscription Management.
    /// </summary>
    public partial class QCAlgorithm : MarshalByRefObject, IAlgorithm
    {
        #region Documentation Attribute Categories
        const string AddingData = "Adding Data";
        const string AlgorithmFramework = "Algorithm Framework";
        const string Charting = "Charting";
        const string ConsolidatingData = "Consolidating Data";
        const string HandlingData = "Handling Data";
        const string HistoricalData = "Historical Data";
        const string Indicators = "Indicators";
        const string LiveTrading = "Live Trading";
        const string Logging = "Logging";
        const string MachineLearning = "Machine Learning";
        const string Modeling = "Modeling";
        const string ParameterAndOptimization = "Parameter and Optimization";
        const string ScheduledEvents = "Scheduled Events";
        const string SecuritiesAndPortfolio = "Securities and Portfolio";
        const string TradingAndOrders = "Trading and Orders";
        const string Universes = "Universes";
        const string StatisticsTag = "Statistics";
        #endregion

        /// <summary>
        /// Maximum length of the name or tags of a backtest
        /// </summary>
        protected const int MaxNameAndTagsLength = 200;

        /// <summary>
        /// Maximum number of tags allowed for a backtest
        /// </summary>
        protected const int MaxTagsCount = 100;

        private readonly TimeKeeper _timeKeeper;
        private LocalTimeKeeper _localTimeKeeper;

        private string _name;
        private HashSet<string> _tags;
        private bool _tagsLimitReachedLogSent;
        private bool _tagsCollectionTruncatedLogSent;
        private DateTime _start;
        private DateTime _startDate;   //Default start and end dates.
        private DateTime _endDate;     //Default end to yesterday
        private bool _locked;
        private bool _liveMode;
        private AlgorithmMode _algorithmMode;
        private DeploymentTarget _deploymentTarget;
        private string _algorithmId = "";
        private ConcurrentQueue<string> _debugMessages = new ConcurrentQueue<string>();
        private ConcurrentQueue<string> _logMessages = new ConcurrentQueue<string>();
        private ConcurrentQueue<string> _errorMessages = new ConcurrentQueue<string>();
        private IStatisticsService _statisticsService;
        private IBrokerageModel _brokerageModel;

        private bool _sentBroadcastCommandsDisabled;
        private readonly HashSet<string> _oneTimeCommandErrors = new();
        private readonly Dictionary<string, Func<CallbackCommand, bool?>> _registeredCommands = new(StringComparer.InvariantCultureIgnoreCase);

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
        private bool _userSetSecurityInitializer;

        // warmup resolution variables
        private TimeSpan? _warmupTimeSpan;
        private int? _warmupBarCount;
        private Dictionary<string, string> _parameters = new Dictionary<string, string>();
        private SecurityDefinitionSymbolResolver _securityDefinitionSymbolResolver;

        private SecurityDefinitionSymbolResolver SecurityDefinitionSymbolResolver
        {
            get
            {
                _securityDefinitionSymbolResolver ??= SecurityDefinitionSymbolResolver.GetInstance();
                return _securityDefinitionSymbolResolver;
            }
        }

        private readonly HistoryRequestFactory _historyRequestFactory;

        private IApi _api;

        /// <summary>
        /// QCAlgorithm Base Class Constructor - Initialize the underlying QCAlgorithm components.
        /// QCAlgorithm manages the transactions, portfolio, charting and security subscriptions for the users algorithms.
        /// </summary>
        public QCAlgorithm()
        {
            Name = GetType().Name;
            Tags = new();
            Status = AlgorithmStatus.Running;

            // AlgorithmManager will flip this when we're caught up with realtime
            IsWarmingUp = true;

            //Initialise the Algorithm Helper Classes:
            //- Note - ideally these wouldn't be here, but because of the DLL we need to make the classes shared across
            //  the Worker & Algorithm, limiting ability to do anything else.

            //Initialise Start Date:
            _startDate = new DateTime(1998, 01, 01);
            // intialize our time keeper with only new york
            _timeKeeper = new TimeKeeper(_startDate, new[] { TimeZones.NewYork });
            // set our local time zone
            _localTimeKeeper = _timeKeeper.GetLocalTimeKeeper(TimeZones.NewYork);
            //Initialise End Date:
            SetEndDate(DateTime.UtcNow.ConvertFromUtc(TimeZone));

            // Set default algorithm mode as backtesting
            _algorithmMode = AlgorithmMode.Backtesting;

            // Set default deployment target as local
            _deploymentTarget = DeploymentTarget.LocalPlatform;

            Settings = new AlgorithmSettings();
            DefaultOrderProperties = new OrderProperties();

            //Initialise Data Manager
            SubscriptionManager = new SubscriptionManager(_timeKeeper);

            Securities = new SecurityManager(_timeKeeper);
            Transactions = new SecurityTransactionManager(this, Securities);
            Portfolio = new SecurityPortfolioManager(Securities, Transactions, Settings, DefaultOrderProperties);
            SignalExport = new SignalExportManager(this);

            BrokerageModel = new DefaultBrokerageModel();
            RiskFreeInterestRateModel = new InterestRateProvider();
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
            Schedule = new ScheduleManager(Securities, TimeZone, MarketHoursDatabase);

            // initialize the trade builder
            SetTradeBuilder(new TradeBuilder(FillGroupingMethod.FillToFill, FillMatchingMethod.FIFO));

            SecurityInitializer = new BrokerageModelSecurityInitializer(BrokerageModel, SecuritySeeder.Null);

            CandlestickPatterns = new CandlestickPatterns(this);

            // initialize trading calendar
            TradingCalendar = new TradingCalendar(Securities, MarketHoursDatabase);

            OptionChainProvider = new EmptyOptionChainProvider();
            FutureChainProvider = new EmptyFutureChainProvider();
            _historyRequestFactory = new HistoryRequestFactory(this);

            // set model defaults, universe selection set via PostInitialize
            SetAlpha(new NullAlphaModel());
            SetPortfolioConstruction(new NullPortfolioConstructionModel());
            SetExecution(new ImmediateExecutionModel());
            SetRiskManagement(new NullRiskManagementModel());
            SetUniverseSelection(new NullUniverseSelectionModel());

            Insights = new InsightManager(this);
        }

        /// <summary>
        /// Event fired when the algorithm generates insights
        /// </summary>
        [DocumentationAttribute(AlgorithmFramework)]
        public event AlgorithmEvent<GeneratedInsightsCollection> InsightsGenerated;

        /// <summary>
        /// Security collection is an array of the security objects such as Equities and FOREX. Securities data
        /// manages the properties of tradeable assets such as price, open and close time and holdings information.
        /// </summary>
        [DocumentationAttribute(SecuritiesAndPortfolio)]
        public SecurityManager Securities
        {
            get;
            set;
        }

        /// <summary>
        /// Read-only dictionary containing all active securities. An active security is
        /// a security that is currently selected by the universe or has holdings or open orders.
        /// </summary>
        [DocumentationAttribute(SecuritiesAndPortfolio)]
        public IReadOnlyDictionary<Symbol, Security> ActiveSecurities => UniverseManager.ActiveSecurities;

        /// <summary>
        /// Portfolio object provieds easy access to the underlying security-holding properties; summed together in a way to make them useful.
        /// This saves the user time by providing common portfolio requests in a single
        /// </summary>
        [DocumentationAttribute(SecuritiesAndPortfolio)]
        public SecurityPortfolioManager Portfolio
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the account currency
        /// </summary>
        [DocumentationAttribute(SecuritiesAndPortfolio)]
        public string AccountCurrency => Portfolio.CashBook.AccountCurrency;

        /// <summary>
        /// Gets the time keeper instance
        /// </summary>
        public ITimeKeeper TimeKeeper => _timeKeeper;

        /// <summary>
        /// Generic Data Manager - Required for compiling all data feeds in order, and passing them into algorithm event methods.
        /// The subscription manager contains a list of the data feed's we're subscribed to and properties of each data feed.
        /// </summary>
        [DocumentationAttribute(HandlingData)]
        public SubscriptionManager SubscriptionManager
        {
            get;
            set;
        }

        /// <summary>
        /// SignalExport - Allows sending export signals to different 3rd party API's. For example, it allows to send signals
        /// to Collective2, CrunchDAO and Numerai API's
        /// </summary>
        [DocumentationAttribute(SecuritiesAndPortfolio)]
        public SignalExportManager SignalExport
        {
            get;
        }

        /// <summary>
        /// The project id associated with this algorithm if any
        /// </summary>
        public int ProjectId
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the brokerage model - used to model interactions with specific brokerages.
        /// </summary>
        [DocumentationAttribute(Modeling)]
        public IBrokerageModel BrokerageModel
        {
            get
            {
                return _brokerageModel;
            }
            private set
            {
                _brokerageModel = value;
                try
                {
                    BrokerageName = Brokerages.BrokerageModel.GetBrokerageName(_brokerageModel);
                }
                catch (ArgumentOutOfRangeException)
                {
                    // The brokerage model might be a custom one which has not a corresponding BrokerageName
                    BrokerageName = BrokerageName.Default;
                }
            }
        }

        /// <summary>
        /// Gets the brokerage name.
        /// </summary>
        [DocumentationAttribute(Modeling)]
        public BrokerageName BrokerageName
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the brokerage message handler used to decide what to do
        /// with each message sent from the brokerage
        /// </summary>
        [DocumentationAttribute(Modeling)]
        public IBrokerageMessageHandler BrokerageMessageHandler
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the risk free interest rate model used to get the interest rates
        /// </summary>
        [DocumentationAttribute(Modeling)]
        public IRiskFreeInterestRateModel RiskFreeInterestRateModel
        {
            get;
            private set;
        }

        /// <summary>
        /// Notification Manager for Sending Live Runtime Notifications to users about important events.
        /// </summary>
        [DocumentationAttribute(LiveTrading)]
        public NotificationManager Notify
        {
            get;
            set;
        }

        /// <summary>
        /// Gets schedule manager for adding/removing scheduled events
        /// </summary>
        [DocumentationAttribute(ScheduledEvents)]
        public ScheduleManager Schedule
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets the current status of the algorithm
        /// </summary>
        [DocumentationAttribute(HandlingData)]
        public AlgorithmStatus Status
        {
            get;
            set;
        }

        /// <summary>
        /// Gets an instance that is to be used to initialize newly created securities.
        /// </summary>
        [DocumentationAttribute(AddingData)]
        public ISecurityInitializer SecurityInitializer
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the Trade Builder to generate trades from executions
        /// </summary>
        [DocumentationAttribute(TradingAndOrders)]
        public ITradeBuilder TradeBuilder
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets an instance to access the candlestick pattern helper methods
        /// </summary>
        [DocumentationAttribute(Indicators)]
        public CandlestickPatterns CandlestickPatterns
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the date rules helper object to make specifying dates for events easier
        /// </summary>
        [DocumentationAttribute(ScheduledEvents)]
        public DateRules DateRules
        {
            get { return Schedule.DateRules; }
        }

        /// <summary>
        /// Gets the time rules helper object to make specifying times for events easier
        /// </summary>
        [DocumentationAttribute(ScheduledEvents)]
        public TimeRules TimeRules
        {
            get { return Schedule.TimeRules; }
        }

        /// <summary>
        /// Gets trading calendar populated with trading events
        /// </summary>
        [DocumentationAttribute(ScheduledEvents)]
        public TradingCalendar TradingCalendar
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the user settings for the algorithm
        /// </summary>
        [DocumentationAttribute(HandlingData)]
        public IAlgorithmSettings Settings
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the option chain provider, used to get the list of option contracts for an underlying symbol
        /// </summary>
        [DocumentationAttribute(AddingData)]
        [Obsolete("OptionChainProvider property is will soon be deprecated. " +
            "The new OptionChain() method should be used to fetch option chains, " +
            "which will contain additional data per contract, like daily price data, implied volatility and greeks.")]
        public IOptionChainProvider OptionChainProvider { get; private set; }

        /// <summary>
        /// Gets the future chain provider, used to get the list of future contracts for an underlying symbol
        /// </summary>
        [DocumentationAttribute(AddingData)]
        [Obsolete("FutureChainProvider property is will soon be deprecated. " +
            "The new FuturesChain() method should be used to fetch futures chains, " +
            "which will contain additional data per contract, like daily price data.")]
        public IFutureChainProvider FutureChainProvider { get; private set; }

        /// <summary>
        /// Gets the default order properties
        /// </summary>
        [DocumentationAttribute(TradingAndOrders)]
        public IOrderProperties DefaultOrderProperties { get; set; }

        /// <summary>
        /// Public name for the algorithm as automatically generated by the IDE. Intended for helping distinguish logs by noting
        /// the algorithm-id.
        /// </summary>
        /// <seealso cref="AlgorithmId"/>
        [DocumentationAttribute(HandlingData)]
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                if (_locked)
                {
                    throw new InvalidOperationException("Cannot set algorithm name after it is initialized.");
                }

                if (!string.IsNullOrEmpty(value))
                {
                    _name = value.Truncate(MaxNameAndTagsLength);
                }
            }
        }

        /// <summary>
        /// A list of tags associated with the algorithm or the backtest, useful for categorization
        /// </summary>
        [DocumentationAttribute(HandlingData)]
        public HashSet<string> Tags
        {
            get
            {
                return _tags;
            }
            set
            {
                if (value == null)
                {
                    return;
                }

                var tags = value.Where(x => !string.IsNullOrEmpty(x?.Trim())).ToList();

                if (tags.Count > MaxTagsCount && !_tagsCollectionTruncatedLogSent)
                {
                    Log($"Warning: The tags collection cannot contain more than {MaxTagsCount} items. It will be truncated.");
                    _tagsCollectionTruncatedLogSent = true;
                }

                _tags = tags.Take(MaxTagsCount).ToHashSet(tag => tag.Truncate(MaxNameAndTagsLength));
                if (_locked)
                {
                    TagsUpdated?.Invoke(this, Tags);
                }
            }
        }

        /// <summary>
        /// Event fired algorithm's name is changed
        /// </summary>
        [DocumentationAttribute(HandlingData)]
        public event AlgorithmEvent<string> NameUpdated;

        /// <summary>
        /// Event fired when the tag collection is updated
        /// </summary>
        [DocumentationAttribute(HandlingData)]
        public event AlgorithmEvent<HashSet<string>> TagsUpdated;

        /// <summary>
        /// Read-only value for current time frontier of the algorithm in terms of the <see cref="TimeZone"/>
        /// </summary>
        /// <remarks>During backtesting this is primarily sourced from the data feed. During live trading the time is updated from the system clock.</remarks>
        [DocumentationAttribute(HandlingData)]
        public DateTime Time
        {
            get { return _localTimeKeeper.LocalTime; }
        }

        /// <summary>
        /// Current date/time in UTC.
        /// </summary>
        [DocumentationAttribute(HandlingData)]
        public DateTime UtcTime
        {
            get { return _timeKeeper.UtcTime; }
        }

        /// <summary>
        /// Gets the time zone used for the <see cref="Time"/> property. The default value
        /// is <see cref="TimeZones.NewYork"/>
        /// </summary>
        [DocumentationAttribute(HandlingData)]
        public DateTimeZone TimeZone
        {
            get { return _localTimeKeeper.TimeZone; }
        }

        /// <summary>
        /// Value of the user set start-date from the backtest.
        /// </summary>
        /// <remarks>This property is set with SetStartDate() and defaults to the earliest QuantConnect data available - Jan 1st 1998. It is ignored during live trading </remarks>
        /// <seealso cref="SetStartDate(DateTime)"/>
        [DocumentationAttribute(HandlingData)]
        public DateTime StartDate => _startDate;

        /// <summary>
        /// Value of the user set start-date from the backtest. Controls the period of the backtest.
        /// </summary>
        /// <remarks> This property is set with SetEndDate() and defaults to today. It is ignored during live trading.</remarks>
        /// <seealso cref="SetEndDate(DateTime)"/>
        [DocumentationAttribute(HandlingData)]
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
        [DocumentationAttribute(HandlingData)]
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
        [DocumentationAttribute(LiveTrading)]
        public bool LiveMode
        {
            get
            {
                return _liveMode;
            }
        }

        /// <summary>
        /// Algorithm running mode.
        /// </summary>
        public AlgorithmMode AlgorithmMode
        {
            get
            {
                return _algorithmMode;
            }
        }

        /// <summary>
        /// Deployment target, either local or cloud.
        /// </summary>
        public DeploymentTarget DeploymentTarget
        {
            get
            {
                return _deploymentTarget;
            }
        }

        /// <summary>
        /// Storage for debugging messages before the event handler has passed control back to the Lean Engine.
        /// </summary>
        /// <seealso cref="Debug(string)"/>
        [DocumentationAttribute(Logging)]
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
        [DocumentationAttribute(Logging)]
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
        [DocumentationAttribute(Logging)]
        public Exception RunTimeError { get; set; }

        /// <summary>
        /// List of error messages generated by the user's code calling the "Error" function.
        /// </summary>
        /// <remarks>This method is best used within a try-catch bracket to handle any runtime errors from a user algorithm.</remarks>
        /// <see cref="Error(string)"/>
        [DocumentationAttribute(Logging)]
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
        [DocumentationAttribute(HandlingData)]
        public Slice CurrentSlice { get; private set; }

        /// <summary>
        /// Gets the object store, used for persistence
        /// </summary>
        [DocumentationAttribute(HandlingData)]
        [DocumentationAttribute(MachineLearning)]
        public ObjectStore ObjectStore { get; private set; }

        /// <summary>
        /// The current statistics for the running algorithm.
        /// </summary>
        [DocumentationAttribute(StatisticsTag)]
        public StatisticsResults Statistics
        {
            get
            {
                return _statisticsService?.StatisticsResults() ?? new StatisticsResults();
            }
        }

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        /// <seealso cref="SetStartDate(DateTime)"/>
        /// <seealso cref="SetEndDate(DateTime)"/>
        /// <seealso cref="SetCash(decimal)"/>
        [DocumentationAttribute(AlgorithmFramework)]
        [DocumentationAttribute(HandlingData)]
        public virtual void Initialize()
        {
            //Setup Required Data
            throw new NotImplementedException("Please override the Initialize() method");
        }

        /// <summary>
        /// Called by setup handlers after Initialize and allows the algorithm a chance to organize
        /// the data gather in the Initialize method
        /// </summary>
        [DocumentationAttribute(AlgorithmFramework)]
        [DocumentationAttribute(HandlingData)]
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

            // Check benchmark timezone against algorithm timezone to warn for misaligned statistics
            if (Benchmark is SecurityBenchmark securityBenchmark)
            {
                // Only warn on algorithms subscribed to daily resolution as its statistics will suffer the most
                var subscription = SubscriptionManager.Subscriptions.OrderByDescending(x => x.Resolution).FirstOrDefault();
                var benchmarkTimeZone = MarketHoursDatabase.GetDataTimeZone(securityBenchmark.Security.Symbol.ID.Market,
                    securityBenchmark.Security.Symbol, securityBenchmark.Security.Type);
                if ((subscription?.Resolution == Resolution.Daily || UniverseSettings.Resolution == Resolution.Daily) && benchmarkTimeZone != TimeZone)
                {
                    Log($"QCAlgorithm.PostInitialize(): Warning: Using a security benchmark of a different timezone ({benchmarkTimeZone})" +
                        $" than the algorithm TimeZone ({TimeZone}) may lead to skewed and incorrect statistics. Use a higher resolution than daily to minimize.");
                }
            }

            if (TryGetWarmupHistoryStartTime(out var result))
            {
                SetDateTime(result.ConvertToUtc(TimeZone));
            }
            else
            {
                SetFinishedWarmingUp();
            }

            if (Settings.DailyPreciseEndTime)
            {
                Debug("Accurate daily end-times now enabled by default. See more at https://qnt.co/3YHaWHL. To disable it and use legacy daily bars set self.settings.daily_precise_end_time = False.");
            }

            // perform end of time step checks, such as enforcing underlying securities are in raw data mode
            OnEndOfTimeStep();
        }

        /// <summary>
        /// Called when the algorithm has completed initialization and warm up.
        /// </summary>
        [DocumentationAttribute(HandlingData)]
        public virtual void OnWarmupFinished()
        {
        }

        /// <summary>
        /// Gets the parameter with the specified name. If a parameter with the specified name does not exist,
        /// the given default value is returned if any, else null
        /// </summary>
        /// <param name="name">The name of the parameter to get</param>
        /// <param name="defaultValue">The default value to return</param>
        /// <returns>The value of the specified parameter, or defaultValue if not found or null if there's no default value</returns>
        [DocumentationAttribute(ParameterAndOptimization)]
        public string GetParameter(string name, string defaultValue = null)
        {
            return _parameters.TryGetValue(name, out var value) ? value : defaultValue;
        }

        /// <summary>
        /// Gets the parameter with the specified name parsed as an integer. If a parameter with the specified name does not exist,
        /// or the conversion is not possible, the given default value is returned
        /// </summary>
        /// <param name="name">The name of the parameter to get</param>
        /// <param name="defaultValue">The default value to return</param>
        /// <returns>The value of the specified parameter, or defaultValue if not found or null if there's no default value</returns>
        [DocumentationAttribute(ParameterAndOptimization)]
        public int GetParameter(string name, int defaultValue)
        {
            return _parameters.TryGetValue(name, out var strValue) && int.TryParse(strValue, out var value) ? value : defaultValue;
        }

        /// <summary>
        /// Gets the parameter with the specified name parsed as a double. If a parameter with the specified name does not exist,
        /// or the conversion is not possible, the given default value is returned
        /// </summary>
        /// <param name="name">The name of the parameter to get</param>
        /// <param name="defaultValue">The default value to return</param>
        /// <returns>The value of the specified parameter, or defaultValue if not found or null if there's no default value</returns>
        [DocumentationAttribute(ParameterAndOptimization)]
        public double GetParameter(string name, double defaultValue)
        {
            return _parameters.TryGetValue(name, out var strValue) &&
                double.TryParse(strValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var value) ? value : defaultValue;
        }

        /// <summary>
        /// Gets the parameter with the specified name parsed as a decimal. If a parameter with the specified name does not exist,
        /// or the conversion is not possible, the given default value is returned
        /// </summary>
        /// <param name="name">The name of the parameter to get</param>
        /// <param name="defaultValue">The default value to return</param>
        /// <returns>The value of the specified parameter, or defaultValue if not found or null if there's no default value</returns>
        [DocumentationAttribute(ParameterAndOptimization)]
        public decimal GetParameter(string name, decimal defaultValue)
        {
            return _parameters.TryGetValue(name, out var strValue) &&
                decimal.TryParse(strValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var value) ? value : defaultValue;
        }

        /// <summary>
        /// Gets a read-only dictionary with all current parameters
        /// </summary>
        [DocumentationAttribute(ParameterAndOptimization)]
        public IReadOnlyDictionary<string, string> GetParameters()
        {
            return _parameters.ToReadOnlyDictionary();
        }

        /// <summary>
        /// Sets the parameters from the dictionary
        /// </summary>
        /// <param name="parameters">Dictionary containing the parameter names to values</param>
        [DocumentationAttribute(ParameterAndOptimization)]
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
        [DocumentationAttribute(HandlingData)]
        public void SetAvailableDataTypes(Dictionary<SecurityType, List<TickType>> availableDataTypes)
        {
            if (availableDataTypes == null)
            {
                return;
            }

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
        [DocumentationAttribute(AddingData)]
        [DocumentationAttribute(Modeling)]
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
        [DocumentationAttribute(AddingData)]
        [DocumentationAttribute(Modeling)]
        public void SetSecurityInitializer(Action<Security, bool> securityInitializer)
        {
            SetSecurityInitializer(new FuncSecurityInitializer(security => securityInitializer(security, false)));
        }

        /// <summary>
        /// Sets the security initializer function, used to initialize/configure securities after creation.
        /// The initializer will be applied to all universes and manually added securities.
        /// </summary>
        /// <param name="securityInitializer">The security initializer function</param>
        [DocumentationAttribute(AddingData)]
        [DocumentationAttribute(Modeling)]
        public void SetSecurityInitializer(Action<Security> securityInitializer)
        {
            SetSecurityInitializer(new FuncSecurityInitializer(securityInitializer));
        }

        /// <summary>
        /// Sets the option chain provider, used to get the list of option contracts for an underlying symbol
        /// </summary>
        /// <param name="optionChainProvider">The option chain provider</param>
        [DocumentationAttribute(AddingData)]
        public void SetOptionChainProvider(IOptionChainProvider optionChainProvider)
        {
            OptionChainProvider = optionChainProvider;
        }

        /// <summary>
        /// Sets the future chain provider, used to get the list of future contracts for an underlying symbol
        /// </summary>
        /// <param name="futureChainProvider">The future chain provider</param>
        [DocumentationAttribute(AddingData)]
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
        [DocumentationAttribute(HandlingData)]
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
                    .FirstOrDefault(x => x.GetParameters()[0].ParameterType == typeof(Slice));

                if (method == null)
                {
                    return;
                }

                var self = Expression.Constant(this);
                var parameter = Expression.Parameter(typeof(Slice), "data");
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
        /// Event handler to be called when there's been a split event
        /// </summary>
        /// <param name="splits">The current time slice splits</param>
        [DocumentationAttribute(HandlingData)]
        public virtual void OnSplits(Splits splits)
        {
        }

        /// <summary>
        /// Event handler to be called when there's been a dividend event
        /// </summary>
        /// <param name="dividends">The current time slice dividends</param>
        [DocumentationAttribute(HandlingData)]
        public virtual void OnDividends(Dividends dividends)
        {
        }

        /// <summary>
        /// Event handler to be called when there's been a delistings event
        /// </summary>
        /// <param name="delistings">The current time slice delistings</param>
        [DocumentationAttribute(HandlingData)]
        public virtual void OnDelistings(Delistings delistings)
        {
        }

        /// <summary>
        /// Event handler to be called when there's been a symbol changed event
        /// </summary>
        /// <param name="symbolsChanged">The current time slice symbol changed events</param>
        [DocumentationAttribute(HandlingData)]
        public virtual void OnSymbolChangedEvents(SymbolChangedEvents symbolsChanged)
        {
        }

        /// <summary>
        /// Event fired each time the we add/remove securities from the data feed
        /// </summary>
        /// <param name="changes">Security additions/removals for this time step</param>
        [DocumentationAttribute(AddingData)]
        [DocumentationAttribute(Universes)]
        public virtual void OnSecuritiesChanged(SecurityChanges changes)
        {
        }

        /// <summary>
        /// Margin call event handler. This method is called right before the margin call orders are placed in the market.
        /// </summary>
        /// <param name="requests">The orders to be executed to bring this algorithm within margin limits</param>
        [DocumentationAttribute(Modeling)]
        [DocumentationAttribute(TradingAndOrders)]
        public virtual void OnMarginCall(List<SubmitOrderRequest> requests)
        {
        }

        /// <summary>
        /// Margin call warning event handler. This method is called when Portfolio.MarginRemaining is under 5% of your Portfolio.TotalPortfolioValue
        /// </summary>
        [DocumentationAttribute(Modeling)]
        [DocumentationAttribute(TradingAndOrders)]
        public virtual void OnMarginCallWarning()
        {
        }

        /// <summary>
        /// End of a trading day event handler. This method is called at the end of the algorithm day (or multiple times if trading multiple assets).
        /// </summary>
        /// <remarks>Method is called 10 minutes before closing to allow user to close out position.</remarks>
        /// <remarks>Deprecated because different assets have different market close times,
        /// and because Python does not support two methods with the same name</remarks>
        [Obsolete("This method is deprecated and will be removed after August 2021. Please use this overload: OnEndOfDay(Symbol symbol)")]
        [DocumentationAttribute(HandlingData)]
        [StubsIgnore]
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
        [DocumentationAttribute(HandlingData)]
        [StubsIgnore]
        public virtual void OnEndOfDay(string symbol)
        {
        }

        /// <summary>
        /// End of a trading day event handler. This method is called at the end of the algorithm day (or multiple times if trading multiple assets).
        /// </summary>
        /// <param name="symbol">Asset symbol for this end of day event. Forex and equities have different closing hours.</param>
        [DocumentationAttribute(HandlingData)]
        [StubsAvoidImplicits]
        public virtual void OnEndOfDay(Symbol symbol)
        {
            OnEndOfDay(symbol.ToString());
        }

        /// <summary>
        /// End of algorithm run event handler. This method is called at the end of a backtest or live trading operation. Intended for closing out logs.
        /// </summary>
        [DocumentationAttribute(HandlingData)]
        public virtual void OnEndOfAlgorithm()
        {

        }

        /// <summary>
        /// Order fill event handler. On an order fill update the resulting information is passed to this method.
        /// </summary>
        /// <param name="orderEvent">Order event details containing details of the events</param>
        /// <remarks>This method can be called asynchronously and so should only be used by seasoned C# experts. Ensure you use proper locks on thread-unsafe objects</remarks>
        [DocumentationAttribute(TradingAndOrders)]
        public virtual void OnOrderEvent(OrderEvent orderEvent)
        {

        }

        /// <summary>
        /// Option assignment event handler. On an option assignment event for short legs the resulting information is passed to this method.
        /// </summary>
        /// <param name="assignmentEvent">Option exercise event details containing details of the assignment</param>
        /// <remarks>This method can be called asynchronously and so should only be used by seasoned C# experts. Ensure you use proper locks on thread-unsafe objects</remarks>
        [DocumentationAttribute(TradingAndOrders)]
        public virtual void OnAssignmentOrderEvent(OrderEvent assignmentEvent)
        {

        }

        /// <summary>
        /// Brokerage message event handler. This method is called for all types of brokerage messages.
        /// </summary>
        [DocumentationAttribute(LiveTrading)]
        [DocumentationAttribute(Modeling)]
        [DocumentationAttribute(TradingAndOrders)]
        public virtual void OnBrokerageMessage(BrokerageMessageEvent messageEvent)
        {

        }

        /// <summary>
        /// Brokerage disconnected event handler. This method is called when the brokerage connection is lost.
        /// </summary>
        [DocumentationAttribute(LiveTrading)]
        public virtual void OnBrokerageDisconnect()
        {

        }

        /// <summary>
        /// Brokerage reconnected event handler. This method is called when the brokerage connection is restored after a disconnection.
        /// </summary>
        [DocumentationAttribute(LiveTrading)]
        public virtual void OnBrokerageReconnect()
        {

        }

        /// <summary>
        /// Update the internal algorithm time frontier.
        /// </summary>
        /// <remarks>For internal use only to advance time.</remarks>
        /// <param name="frontier">Current utc datetime.</param>
        [DocumentationAttribute(HandlingData)]
        public void SetDateTime(DateTime frontier)
        {
            _timeKeeper.SetUtcDateTime(frontier);
            if (_locked && IsWarmingUp && Time >= _start)
            {
                SetFinishedWarmingUp();
            }
        }

        /// <summary>
        /// Sets the time zone of the <see cref="Time"/> property in the algorithm
        /// </summary>
        /// <param name="timeZone">The desired time zone</param>
        [DocumentationAttribute(HandlingData)]
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
        [DocumentationAttribute(HandlingData)]
        public void SetTimeZone(DateTimeZone timeZone)
        {
            if (_locked)
            {
                throw new InvalidOperationException("Algorithm.SetTimeZone(): Cannot change time zone after algorithm running.");
            }

            if (timeZone == null) throw new ArgumentNullException(nameof(timeZone));
            _timeKeeper.AddTimeZone(timeZone);
            _localTimeKeeper = _timeKeeper.GetLocalTimeKeeper(timeZone);

            // the time rules need to know the default time zone as well
            TimeRules.SetDefaultTimeZone(timeZone);
            DateRules.SetDefaultTimeZone(timeZone);

            // In BackTest mode we reset the Algorithm time to reflect the new timezone
            // startDate is set by the user so we expect it to be for their timezone already
            // so there is no need to update it.
            if (!LiveMode)
            {
                _start = _startDate;
                SetDateTime(_startDate.ConvertToUtc(TimeZone));
            }
            // In live mode we need to adjust startDate to reflect the new timezone
            // startDate is set by Lean to the default timezone (New York), so we must update it here
            else
            {
                SetLiveModeStartDate();
            }
        }

        /// <summary>
        /// Sets the brokerage to emulate in backtesting or paper trading.
        /// This can be used for brokerages that have been implemented in LEAN
        /// </summary>
        /// <param name="brokerage">The brokerage to emulate</param>
        /// <param name="accountType">The account type (Cash or Margin)</param>
        [DocumentationAttribute(Modeling)]
        public void SetBrokerageModel(BrokerageName brokerage, AccountType accountType = AccountType.Margin)
        {
            SetBrokerageModel(Brokerages.BrokerageModel.Create(Transactions, brokerage, accountType));
        }

        /// <summary>
        /// Sets the brokerage to emulate in backtesting or paper trading.
        /// This can be used to set a custom brokerage model.
        /// </summary>
        /// <param name="model">The brokerage model to use</param>
        [DocumentationAttribute(Modeling)]
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
        [DocumentationAttribute(Modeling)]
        [DocumentationAttribute(Logging)]
        public void SetBrokerageMessageHandler(IBrokerageMessageHandler handler)
        {
            BrokerageMessageHandler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        /// <summary>
        /// Sets the risk free interest rate model to be used in the algorithm
        /// </summary>
        /// <param name="model">The risk free interest rate model to use</param>
        [DocumentationAttribute(Modeling)]
        public void SetRiskFreeInterestRateModel(IRiskFreeInterestRateModel model)
        {
            RiskFreeInterestRateModel = model ?? throw new ArgumentNullException(nameof(model));
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
        [DocumentationAttribute(TradingAndOrders)]
        [DocumentationAttribute(SecuritiesAndPortfolio)]
        [DocumentationAttribute(Indicators)]
        public void SetBenchmark(SecurityType securityType, string symbol)
        {
            if (_locked)
            {
                throw new InvalidOperationException("Algorithm.SetBenchmark(): Cannot change Benchmark after algorithm initialized.");
            }

            var market = GetMarket(null, symbol, securityType, defaultMarket: Market.USA);

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
        [DocumentationAttribute(TradingAndOrders)]
        [DocumentationAttribute(SecuritiesAndPortfolio)]
        [DocumentationAttribute(Indicators)]
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
        [DocumentationAttribute(TradingAndOrders)]
        [DocumentationAttribute(SecuritiesAndPortfolio)]
        [DocumentationAttribute(Indicators)]
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
        [DocumentationAttribute(TradingAndOrders)]
        [DocumentationAttribute(SecuritiesAndPortfolio)]
        [DocumentationAttribute(Indicators)]
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
        [DocumentationAttribute(TradingAndOrders)]
        [DocumentationAttribute(SecuritiesAndPortfolio)]
        [DocumentationAttribute(Indicators)]
        public IBenchmark Benchmark
        {
            get;
            private set;
        }

        /// <summary>
        /// Sets name to the currently running backtest
        /// </summary>
        /// <param name="name">The name for the backtest</param>
        public void SetName(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Adds a tag to the algorithm
        /// </summary>
        /// <param name="tag">The tag to add</param>
        public void AddTag(string tag)
        {
            if (!string.IsNullOrEmpty(tag?.Trim()))
            {
                if (Tags.Count >= MaxTagsCount)
                {
                    if (!_tagsLimitReachedLogSent)
                    {
                        Log($"Warning: AddTag({tag}): Unable to add tag. Tags are limited to a maximum of {MaxTagsCount}.");
                        _tagsLimitReachedLogSent = true;
                    }
                    return;
                }

                // We'll only notify the tad update after the algorithm has been initialized
                if (Tags.Add(tag.Truncate(MaxNameAndTagsLength)) && _locked)
                {
                    TagsUpdated?.Invoke(this, Tags);
                }
            }
        }

        /// <summary>
        /// Sets the tags for the algorithm
        /// </summary>
        /// <param name="tags">The tags</param>
        public void SetTags(HashSet<string> tags)
        {
            Tags = tags;
        }

        /// <summary>
        /// Sets the account currency cash symbol this algorithm is to manage, as well as
        /// the starting cash in this currency if given
        /// </summary>
        /// <remarks>Has to be called during <see cref="Initialize"/> before
        /// calling <see cref="SetCash(decimal)"/> or adding any <see cref="Security"/></remarks>
        /// <param name="accountCurrency">The account currency cash symbol to set</param>
        /// <param name="startingCash">The account currency starting cash to set</param>
        [DocumentationAttribute(SecuritiesAndPortfolio)]
        public void SetAccountCurrency(string accountCurrency, decimal? startingCash = null)
        {
            if (_locked)
            {
                throw new InvalidOperationException("Algorithm.SetAccountCurrency(): " +
                    "Cannot change AccountCurrency after algorithm initialized.");
            }

            if (startingCash == null)
            {
                Debug($"Changing account currency from {AccountCurrency} to {accountCurrency}...");
            }
            else
            {
                Debug($"Changing account currency from {AccountCurrency} to {accountCurrency}, with a starting cash of {startingCash}...");
            }

            Portfolio.SetAccountCurrency(accountCurrency, startingCash);
        }

        /// <summary>
        /// Set initial cash for the strategy while backtesting. During live mode this value is ignored
        /// and replaced with the actual cash of your brokerage account.
        /// </summary>
        /// <param name="startingCash">Starting cash for the strategy backtest</param>
        /// <remarks>Alias of SetCash(decimal)</remarks>
        [DocumentationAttribute(SecuritiesAndPortfolio)]
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
        [DocumentationAttribute(SecuritiesAndPortfolio)]
        public void SetCash(int startingCash)
        {
            SetCash((decimal)startingCash);
        }

        /// <summary>
        /// Set initial cash for the strategy while backtesting. During live mode this value is ignored
        /// and replaced with the actual cash of your brokerage account.
        /// </summary>
        /// <param name="startingCash">Starting cash for the strategy backtest</param>
        [DocumentationAttribute(SecuritiesAndPortfolio)]
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
        [DocumentationAttribute(SecuritiesAndPortfolio)]
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
        [DocumentationAttribute(HandlingData)]
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
        [DocumentationAttribute(HandlingData)]
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
        /// Set the algorithm id (backtestId or live deployId for the algorithm).
        /// </summary>
        /// <param name="algorithmId">String Algorithm Id</param>
        /// <remarks>Intended for internal QC Lean Engine use only as a setter for AlgorithmId</remarks>
        [DocumentationAttribute(HandlingData)]
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
        [DocumentationAttribute(HandlingData)]
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
                _start = _startDate = start;
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
        [DocumentationAttribute(HandlingData)]
        public void SetEndDate(DateTime end)
        {
            // no need to set this value in live mode, will be set using the current time.
            if (_liveMode) return;

            //1. Check not locked already:
            if (_locked)
            {
                throw new InvalidOperationException("Algorithm.SetEndDate(): Cannot change end date after algorithm initialized.");
            }

            //Validate:
            //2. Check Range:
            var yesterdayInAlgorithmTimeZone = DateTime.UtcNow.ConvertFromUtc(TimeZone).Date.AddDays(-1);
            if (end > yesterdayInAlgorithmTimeZone)
            {
                end = yesterdayInAlgorithmTimeZone;
            }

            //3. Make this at the very end of the requested date
            _endDate = end.RoundDown(TimeSpan.FromDays(1)).AddDays(1).AddTicks(-1);
        }

        /// <summary>
        /// Lock the algorithm initialization to avoid user modifiying cash and data stream subscriptions
        /// </summary>
        /// <remarks>Intended for Internal QC Lean Engine use only to prevent accidental manipulation of important properties</remarks>
        [DocumentationAttribute(AlgorithmFramework)]
        public void SetLocked()
        {
            _locked = true;

            // The algorithm is initialized, we can now send the initial name and tags updates
            NameUpdated?.Invoke(this, Name);
            TagsUpdated?.Invoke(this, Tags);
        }

        /// <summary>
        /// Gets whether or not this algorithm has been locked and fully initialized
        /// </summary>
        [DocumentationAttribute(AlgorithmFramework)]
        public bool GetLocked()
        {
            return _locked;
        }

        /// <summary>
        /// Set live mode state of the algorithm run: Public setter for the algorithm property LiveMode.
        /// </summary>
        [DocumentationAttribute(LiveTrading)]
        public void SetLiveMode(bool live)
        {
            if (!_locked)
            {
                _liveMode = live;
                Notify = new NotificationManager(live);
                TradeBuilder.SetLiveMode(live);
                Securities.SetLiveMode(live);
                Transactions.SetLiveMode(live);
                if (live)
                {
                    SetLiveModeStartDate();
                    _algorithmMode = AlgorithmMode.Live;
                }
            }
        }

        /// <summary>
        /// Sets the algorithm running mode
        /// </summary>
        /// <param name="algorithmMode">Algorithm mode</param>
        public void SetAlgorithmMode(AlgorithmMode algorithmMode)
        {
            if (!_locked)
            {
                _algorithmMode = algorithmMode;
                SetLiveMode(_algorithmMode == AlgorithmMode.Live);
            }
        }

        /// <summary>
        /// Sets the algorithm deployment target
        /// </summary>
        /// <param name="deploymentTarget">Deployment target</param>
        public void SetDeploymentTarget(DeploymentTarget deploymentTarget)
        {
            if (!_locked)
            {
                _deploymentTarget = deploymentTarget;
            }
        }

        /// <summary>
        /// Set the <see cref="ITradeBuilder"/> implementation to generate trades from executions and market price updates
        /// </summary>
        [DocumentationAttribute(TradingAndOrders)]
        public void SetTradeBuilder(ITradeBuilder tradeBuilder)
        {
            TradeBuilder = tradeBuilder;
            TradeBuilder.SetLiveMode(LiveMode);
            TradeBuilder.SetSecurityManager(Securities);
        }

        /// <summary>
        /// Add specified data to our data subscriptions. QuantConnect will funnel this data to the handle data routine.
        /// </summary>
        /// <param name="securityType">MarketType Type: Equity, Commodity, Future, FOREX or Crypto</param>
        /// <param name="ticker">The security ticker</param>
        /// <param name="resolution">Resolution of the Data Required</param>
        /// <param name="fillForward">When no data available on a tradebar, return the last data that was generated</param>
        /// <param name="extendedMarketHours">Use extended market hours data</param>
        /// <param name="dataMappingMode">The contract mapping mode to use for the security</param>
        /// <param name="dataNormalizationMode">The price scaling mode to use for the security</param>
        [DocumentationAttribute(AddingData)]
        public Security AddSecurity(SecurityType securityType, string ticker, Resolution? resolution = null, bool fillForward = true, bool extendedMarketHours = false,
            DataMappingMode? dataMappingMode = null, DataNormalizationMode? dataNormalizationMode = null)
        {
            return AddSecurity(securityType, ticker, resolution, fillForward, Security.NullLeverage, extendedMarketHours, dataMappingMode, dataNormalizationMode);
        }

        /// <summary>
        /// Add specified data to required list. QC will funnel this data to the handle data routine.
        /// </summary>
        /// <param name="securityType">MarketType Type: Equity, Commodity, Future, FOREX or Crypto</param>
        /// <param name="ticker">The security ticker</param>
        /// <param name="resolution">Resolution of the Data Required</param>
        /// <param name="fillForward">When no data available on a tradebar, return the last data that was generated</param>
        /// <param name="leverage">Custom leverage per security</param>
        /// <param name="extendedMarketHours">Use extended market hours data</param>
        /// <param name="dataMappingMode">The contract mapping mode to use for the security</param>
        /// <param name="dataNormalizationMode">The price scaling mode to use for the security</param>
        /// <remarks> AddSecurity(SecurityType securityType, Symbol symbol, Resolution resolution, bool fillForward, decimal leverage, bool extendedMarketHours)</remarks>
        [DocumentationAttribute(AddingData)]
        public Security AddSecurity(SecurityType securityType, string ticker, Resolution? resolution, bool fillForward, decimal leverage, bool extendedMarketHours,
            DataMappingMode? dataMappingMode = null, DataNormalizationMode? dataNormalizationMode = null)
        {
            return AddSecurity(securityType, ticker, resolution, null, fillForward, leverage, extendedMarketHours, dataMappingMode, dataNormalizationMode);
        }

        /// <summary>
        /// Set a required SecurityType-symbol and resolution for algorithm
        /// </summary>
        /// <param name="securityType">MarketType Type: Equity, Commodity, Future, FOREX or Crypto</param>
        /// <param name="ticker">The security ticker, e.g. AAPL</param>
        /// <param name="resolution">Resolution of the MarketType required: MarketData, Second or Minute</param>
        /// <param name="market">The market the requested security belongs to, such as 'usa' or 'fxcm'</param>
        /// <param name="fillForward">If true, returns the last available data even if none in that timeslice.</param>
        /// <param name="leverage">leverage for this security</param>
        /// <param name="extendedMarketHours">Use extended market hours data</param>
        /// <param name="dataMappingMode">The contract mapping mode to use for the security</param>
        /// <param name="dataNormalizationMode">The price scaling mode to use for the security</param>
        [DocumentationAttribute(AddingData)]
        public Security AddSecurity(SecurityType securityType, string ticker, Resolution? resolution, string market, bool fillForward, decimal leverage, bool extendedMarketHours,
            DataMappingMode? dataMappingMode = null, DataNormalizationMode? dataNormalizationMode = null)
        {
            // if AddSecurity method is called to add an option or a future, we delegate a call to respective methods
            if (securityType == SecurityType.Option)
            {
                return AddOption(ticker, resolution, market, fillForward, leverage);
            }

            if (securityType == SecurityType.Future)
            {
                return AddFuture(ticker, resolution, market, fillForward, leverage, extendedMarketHours, dataMappingMode, dataNormalizationMode);
            }

            try
            {
                market = GetMarket(market, ticker, securityType);

                Symbol symbol;
                if (!SymbolCache.TryGetSymbol(ticker, out symbol) ||
                    symbol.ID.Market != market ||
                    symbol.SecurityType != securityType)
                {
                    symbol = QuantConnect.Symbol.Create(ticker, securityType, market);
                }

                return AddSecurity(symbol, resolution, fillForward, leverage, extendedMarketHours, dataMappingMode, dataNormalizationMode);
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
        /// <param name="fillForward">If true, returns the last available data even if none in that timeslice.</param>
        /// <param name="leverage">leverage for this security</param>
        /// <param name="extendedMarketHours">Use extended market hours data</param>
        /// <param name="dataMappingMode">The contract mapping mode to use for the security</param>
        /// <param name="dataNormalizationMode">The price scaling mode to use for the security</param>
        /// <param name="contractDepthOffset">The continuous contract desired offset from the current front month.
        /// For example, 0 (default) will use the front month, 1 will use the back month contract</param>
        /// <returns>The new Security that was added to the algorithm</returns>
        [DocumentationAttribute(AddingData)]
        public Security AddSecurity(Symbol symbol, Resolution? resolution = null, bool fillForward = true, decimal leverage = Security.NullLeverage, bool extendedMarketHours = false,
            DataMappingMode? dataMappingMode = null, DataNormalizationMode? dataNormalizationMode = null, int contractDepthOffset = 0)
        {
            // allow users to specify negative numbers, we get the abs of it
            var contractOffset = (uint)Math.Abs(contractDepthOffset);
            if (contractOffset > Futures.MaximumContractDepthOffset)
            {
                throw new ArgumentOutOfRangeException(nameof(contractDepthOffset), $"'contractDepthOffset' current maximum value is {Futures.MaximumContractDepthOffset}." +
                    $" Front month (0) and only {Futures.MaximumContractDepthOffset} back month contracts are currently supported.");
            }

            var isCanonical = symbol.IsCanonical();

            // Short-circuit to AddOptionContract because it will add the underlying if required
            if (!isCanonical && symbol.SecurityType.IsOption())
            {
                return AddOptionContract(symbol, resolution, fillForward, leverage, extendedMarketHours);
            }

            var securityResolution = resolution;
            var securityFillForward = fillForward;
            if (isCanonical)
            {
                // canonical options and futures are daily only
                securityResolution = Resolution.Daily;
                securityFillForward = false;
            }

            var isFilteredSubscription = !isCanonical;
            List<SubscriptionDataConfig> configs;
            // we pass dataNormalizationMode to SubscriptionManager.SubscriptionDataConfigService.Add conditionally,
            // so the default value for its argument is used when the it is null here.
            if (dataNormalizationMode.HasValue)
            {
                configs = SubscriptionManager.SubscriptionDataConfigService.Add(symbol,
                    securityResolution,
                    securityFillForward,
                    extendedMarketHours,
                    isFilteredSubscription,
                    dataNormalizationMode: dataNormalizationMode.Value,
                    contractDepthOffset: (uint)contractDepthOffset);
            }
            else
            {
                configs = SubscriptionManager.SubscriptionDataConfigService.Add(symbol,
                   securityResolution,
                   securityFillForward,
                   extendedMarketHours,
                   isFilteredSubscription,
                   contractDepthOffset: (uint)contractDepthOffset);
            }

            var security = Securities.CreateSecurity(symbol, configs, leverage);

            if (isCanonical)
            {
                security.IsTradable = false;
                Securities.Add(security);

                // add this security to the user defined universe
                Universe universe;
                if (!UniverseManager.ContainsKey(symbol))
                {
                    var canonicalConfig = configs.First();
                    var universeSettingsResolution = resolution ?? UniverseSettings.Resolution;
                    var settings = new UniverseSettings(universeSettingsResolution, leverage, fillForward, extendedMarketHours, UniverseSettings.MinimumTimeInUniverse)
                    {
                        Asynchronous = UniverseSettings.Asynchronous
                    };

                    if (symbol.SecurityType.IsOption())
                    {
                        universe = new OptionChainUniverse((Option)security, settings);
                    }
                    else
                    {
                        // add the expected configurations of the canonical symbol right away, will allow it to warmup and indicators register to them
                        var dataTypes = SubscriptionManager.LookupSubscriptionConfigDataTypes(SecurityType.Future,
                            GetResolution(symbol, resolution, null), isCanonical: false);
                        var continuousUniverseSettings = new UniverseSettings(settings)
                        {
                            ExtendedMarketHours = extendedMarketHours,
                            DataMappingMode = dataMappingMode ?? UniverseSettings.GetUniverseNormalizationModeOrDefault(symbol.SecurityType, symbol.ID.Market),
                            DataNormalizationMode = dataNormalizationMode ?? UniverseSettings.GetUniverseNormalizationModeOrDefault(symbol.SecurityType),
                            ContractDepthOffset = (int)contractOffset,
                            SubscriptionDataTypes = dataTypes,
                            Asynchronous = UniverseSettings.Asynchronous
                        };
                        ContinuousContractUniverse.AddConfigurations(SubscriptionManager.SubscriptionDataConfigService, continuousUniverseSettings, security.Symbol);

                        // let's add a MHDB entry for the continuous symbol using the associated security
                        var continuousContractSymbol = ContinuousContractUniverse.CreateSymbol(security.Symbol);
                        MarketHoursDatabase.SetEntry(continuousContractSymbol.ID.Market,
                            continuousContractSymbol.ID.Symbol,
                            continuousContractSymbol.ID.SecurityType,
                            security.Exchange.Hours);
                        AddUniverse(new ContinuousContractUniverse(security, continuousUniverseSettings, LiveMode,
                            new SubscriptionDataConfig(canonicalConfig, symbol: continuousContractSymbol,
                                // We can use any data type here, since we are not going to use the data.
                                // We just don't want to use the FutureUniverse type because it will force disable extended market hours
                                objectType: typeof(Tick), extendedHours: extendedMarketHours)));

                        universe = new FuturesChainUniverse((Future)security, settings);
                    }

                    AddUniverse(universe);
                }
                return security;
            }

            return AddToUserDefinedUniverse(security, configs);
        }

        /// <summary>
        /// Creates and adds a new <see cref="Equity"/> security to the algorithm
        /// </summary>
        /// <param name="ticker">The equity ticker symbol</param>
        /// <param name="resolution">The <see cref="Resolution"/> of market data, Tick, Second, Minute, Hour, or Daily. Default is <see cref="Resolution.Minute"/></param>
        /// <param name="market">The equity's market, <seealso cref="Market"/>. Default value is null and looked up using BrokerageModel.DefaultMarkets in <see cref="AddSecurity{T}"/></param>
        /// <param name="fillForward">If true, returns the last available data even if none in that timeslice. Default is <value>true</value></param>
        /// <param name="leverage">The requested leverage for this equity. Default is set by <see cref="SecurityInitializer"/></param>
        /// <param name="extendedMarketHours">True to send data during pre and post market sessions. Default is <value>false</value></param>
        /// <param name="dataNormalizationMode">The price scaling mode to use for the equity</param>
        /// <returns>The new <see cref="Equity"/> security</returns>
        [DocumentationAttribute(AddingData)]
        public Equity AddEquity(string ticker, Resolution? resolution = null, string market = null, bool fillForward = true,
            decimal leverage = Security.NullLeverage, bool extendedMarketHours = false, DataNormalizationMode? dataNormalizationMode = null)
        {
            return AddSecurity<Equity>(SecurityType.Equity, ticker, resolution, market, fillForward, leverage, extendedMarketHours, normalizationMode: dataNormalizationMode);
        }

        /// <summary>
        /// Creates and adds a new equity <see cref="Option"/> security to the algorithm
        /// </summary>
        /// <param name="underlying">The underlying equity ticker</param>
        /// <param name="resolution">The <see cref="Resolution"/> of market data, Tick, Second, Minute, Hour, or Daily. Default is <see cref="Resolution.Minute"/></param>
        /// <param name="market">The equity's market, <seealso cref="Market"/>. Default is value null and looked up using BrokerageModel.DefaultMarkets in <see cref="AddSecurity{T}"/></param>
        /// <param name="fillForward">If true, returns the last available data even if none in that timeslice. Default is <value>true</value></param>
        /// <param name="leverage">The requested leverage for this equity. Default is set by <see cref="SecurityInitializer"/></param>
        /// <returns>The new <see cref="Option"/> security</returns>
        [DocumentationAttribute(AddingData)]
        public Option AddOption(string underlying, Resolution? resolution = null, string market = null, bool fillForward = true, decimal leverage = Security.NullLeverage)
        {
            market = GetMarket(market, underlying, SecurityType.Option);

            var underlyingSymbol = QuantConnect.Symbol.Create(underlying, SecurityType.Equity, market);
            return AddOption(underlyingSymbol, resolution, market, fillForward, leverage);
        }

        /// <summary>
        /// Creates and adds a new <see cref="Option"/> security to the algorithm.
        /// This method can be used to add options with non-equity asset classes
        /// to the algorithm (e.g. Future Options).
        /// </summary>
        /// <param name="underlying">Underlying asset Symbol to use as the option's underlying</param>
        /// <param name="resolution">The <see cref="Resolution"/> of market data, Tick, Second, Minute, Hour, or Daily. Default is <see cref="Resolution.Minute"/></param>
        /// <param name="market">The option's market, <seealso cref="Market"/>. Default value is null, but will be resolved using BrokerageModel.DefaultMarkets in <see cref="AddSecurity{T}"/></param>
        /// <param name="fillForward">If true, data will be provided to the algorithm every Second, Minute, Hour, or Day, while the asset is open and depending on the Resolution this option was configured to use.</param>
        /// <param name="leverage">The requested leverage for the </param>
        /// <returns>The new option security instance</returns>
        /// <exception cref="KeyNotFoundException"></exception>
        [DocumentationAttribute(AddingData)]
        public Option AddOption(Symbol underlying, Resolution? resolution = null, string market = null, bool fillForward = true, decimal leverage = Security.NullLeverage)
        {
            return AddOption(underlying, null, resolution, market, fillForward, leverage);
        }

        /// <summary>
        /// Creates and adds a new <see cref="Option"/> security to the algorithm.
        /// This method can be used to add options with non-equity asset classes
        /// to the algorithm (e.g. Future Options).
        /// </summary>
        /// <param name="underlying">Underlying asset Symbol to use as the option's underlying</param>
        /// <param name="targetOption">The target option ticker. This is useful when the option ticker does not match the underlying, e.g. SPX index and the SPXW weekly option. If null is provided will use underlying</param>
        /// <param name="resolution">The <see cref="Resolution"/> of market data, Tick, Second, Minute, Hour, or Daily. Default is <see cref="Resolution.Minute"/></param>
        /// <param name="market">The option's market, <seealso cref="Market"/>. Default value is null, but will be resolved using BrokerageModel.DefaultMarkets in <see cref="AddSecurity{T}"/></param>
        /// <param name="fillForward">If true, data will be provided to the algorithm every Second, Minute, Hour, or Day, while the asset is open and depending on the Resolution this option was configured to use.</param>
        /// <param name="leverage">The requested leverage for the </param>
        /// <returns>The new option security instance</returns>
        /// <exception cref="KeyNotFoundException"></exception>
        [DocumentationAttribute(AddingData)]
        public Option AddOption(Symbol underlying, string targetOption, Resolution? resolution = null,
            string market = null, bool fillForward = true, decimal leverage = Security.NullLeverage)
        {
            var optionType = QuantConnect.Symbol.GetOptionTypeFromUnderlying(underlying);

            market = GetMarket(market, targetOption, optionType);

            Symbol canonicalSymbol;

            string alias;
            if (!string.IsNullOrEmpty(targetOption))
            {
                alias = $"?{targetOption}";
            }
            else
            {
                alias = $"?{underlying.Value}";
            }
            if (!SymbolCache.TryGetSymbol(alias, out canonicalSymbol) ||
                canonicalSymbol.ID.Market != market ||
                !canonicalSymbol.SecurityType.IsOption())
            {
                canonicalSymbol = QuantConnect.Symbol.CreateCanonicalOption(underlying, targetOption, market, alias);
            }

            return (Option)AddSecurity(canonicalSymbol, resolution, fillForward, leverage);
        }

        /// <summary>
        /// Creates and adds a new <see cref="Future"/> security to the algorithm
        /// </summary>
        /// <param name="ticker">The future ticker</param>
        /// <param name="resolution">The <see cref="Resolution"/> of market data, Tick, Second, Minute, Hour, or Daily. Default is <see cref="Resolution.Minute"/></param>
        /// <param name="market">The futures market, <seealso cref="Market"/>. Default is value null and looked up using BrokerageModel.DefaultMarkets in <see cref="AddSecurity{T}"/></param>
        /// <param name="fillForward">If true, returns the last available data even if none in that timeslice. Default is <value>true</value></param>
        /// <param name="leverage">The requested leverage for this equity. Default is set by <see cref="SecurityInitializer"/></param>
        /// <param name="extendedMarketHours">Use extended market hours data</param>
        /// <param name="dataMappingMode">The contract mapping mode to use for the continuous future contract</param>
        /// <param name="dataNormalizationMode">The price scaling mode to use for the continuous future contract</param>
        /// <param name="contractDepthOffset">The continuous future contract desired offset from the current front month.
        /// For example, 0 (default) will use the front month, 1 will use the back month contract</param>
        /// <returns>The new <see cref="Future"/> security</returns>
        [DocumentationAttribute(AddingData)]
        public Future AddFuture(string ticker, Resolution? resolution = null, string market = null,
            bool fillForward = true, decimal leverage = Security.NullLeverage, bool extendedMarketHours = false,
            DataMappingMode? dataMappingMode = null, DataNormalizationMode? dataNormalizationMode = null, int contractDepthOffset = 0)
        {
            market = GetMarket(market, ticker, SecurityType.Future);

            Symbol canonicalSymbol;
            var alias = "/" + ticker;
            if (!SymbolCache.TryGetSymbol(alias, out canonicalSymbol) ||
                canonicalSymbol.ID.Market != market ||
                canonicalSymbol.SecurityType != SecurityType.Future)
            {
                canonicalSymbol = QuantConnect.Symbol.Create(ticker, SecurityType.Future, market, alias);
            }

            return (Future)AddSecurity(canonicalSymbol, resolution, fillForward, leverage, extendedMarketHours, dataMappingMode: dataMappingMode,
                dataNormalizationMode: dataNormalizationMode, contractDepthOffset: contractDepthOffset);
        }

        /// <summary>
        /// Creates and adds a new single <see cref="Future"/> contract to the algorithm
        /// </summary>
        /// <param name="symbol">The futures contract symbol</param>
        /// <param name="resolution">The <see cref="Resolution"/> of market data, Tick, Second, Minute, Hour, or Daily. Default is <see cref="Resolution.Minute"/></param>
        /// <param name="fillForward">If true, returns the last available data even if none in that timeslice. Default is <value>true</value></param>
        /// <param name="leverage">The requested leverage for this equity. Default is set by <see cref="SecurityInitializer"/></param>
        /// <param name="extendedMarketHours">Use extended market hours data</param>
        /// <returns>The new <see cref="Future"/> security</returns>
        [DocumentationAttribute(AddingData)]
        public Future AddFutureContract(Symbol symbol, Resolution? resolution = null, bool fillForward = true,
            decimal leverage = Security.NullLeverage, bool extendedMarketHours = false)
        {
            return (Future)AddSecurity(symbol, resolution, fillForward, leverage, extendedMarketHours);
        }

        /// <summary>
        /// Creates and adds a new Future Option contract to the algorithm.
        /// </summary>
        /// <param name="symbol">The <see cref="Future"/> canonical symbol (i.e. Symbol returned from <see cref="AddFuture"/>)</param>
        /// <param name="optionFilter">Filter to apply to option contracts loaded as part of the universe</param>
        /// <returns>The new <see cref="Option"/> security, containing a <see cref="Future"/> as its underlying.</returns>
        /// <exception cref="ArgumentException">The symbol provided is not canonical.</exception>
        [DocumentationAttribute(AddingData)]
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
        /// <param name="fillForward">If true, this will fill in missing data points with the previous data point</param>
        /// <param name="leverage">The leverage to apply to the option contract</param>
        /// <param name="extendedMarketHours">Use extended market hours data</param>
        /// <returns>Option security</returns>
        /// <exception cref="ArgumentException">Symbol is canonical (i.e. a generic Symbol returned from <see cref="AddFuture"/> or <see cref="AddOption(string, Resolution?, string, bool, decimal)"/>)</exception>
        [DocumentationAttribute(AddingData)]
        public Option AddFutureOptionContract(Symbol symbol, Resolution? resolution = null, bool fillForward = true,
            decimal leverage = Security.NullLeverage, bool extendedMarketHours = false)
        {
            if (symbol.IsCanonical())
            {
                throw new ArgumentException("Expected non-canonical Symbol (i.e. a Symbol representing a specific Future contract");
            }

            return AddOptionContract(symbol, resolution, fillForward, leverage, extendedMarketHours);
        }

        /// <summary>
        /// Creates and adds index options to the algorithm.
        /// </summary>
        /// <param name="underlying">The underlying ticker of the Index Option</param>
        /// <param name="resolution">Resolution of the index option contracts, i.e. the granularity of the data</param>
        /// <param name="market">The foreign exchange trading market, <seealso cref="Market"/>. Default value is null and looked up using BrokerageModel.DefaultMarkets in <see cref="AddSecurity{T}"/></param>
        /// <param name="fillForward">If true, this will fill in missing data points with the previous data point</param>
        /// <returns>Canonical Option security</returns>
        [DocumentationAttribute(AddingData)]
        public IndexOption AddIndexOption(string underlying, Resolution? resolution = null, string market = null, bool fillForward = true)
        {
            return AddIndexOption(underlying, null, resolution, market, fillForward);
        }

        /// <summary>
        /// Creates and adds index options to the algorithm.
        /// </summary>
        /// <param name="symbol">The Symbol of the <see cref="Security"/> returned from <see cref="AddIndex"/></param>
        /// <param name="resolution">Resolution of the index option contracts, i.e. the granularity of the data</param>
        /// <param name="fillForward">If true, this will fill in missing data points with the previous data point</param>
        /// <returns>Canonical Option security</returns>
        [DocumentationAttribute(AddingData)]
        public IndexOption AddIndexOption(Symbol symbol, Resolution? resolution = null, bool fillForward = true)
        {
            return AddIndexOption(symbol, null, resolution, fillForward);
        }

        /// <summary>
        /// Creates and adds index options to the algorithm.
        /// </summary>
        /// <param name="symbol">The Symbol of the <see cref="Security"/> returned from <see cref="AddIndex"/></param>
        /// <param name="targetOption">The target option ticker. This is useful when the option ticker does not match the underlying, e.g. SPX index and the SPXW weekly option. If null is provided will use underlying</param>
        /// <param name="resolution">Resolution of the index option contracts, i.e. the granularity of the data</param>
        /// <param name="fillForward">If true, this will fill in missing data points with the previous data point</param>
        /// <returns>Canonical Option security</returns>
        [DocumentationAttribute(AddingData)]
        public IndexOption AddIndexOption(Symbol symbol, string targetOption, Resolution? resolution = null, bool fillForward = true)
        {
            if (symbol.SecurityType != SecurityType.Index)
            {
                throw new ArgumentException("Symbol provided must be of type SecurityType.Index");
            }

            return (IndexOption)AddOption(symbol, targetOption, resolution, symbol.ID.Market, fillForward);
        }

        /// <summary>
        /// Creates and adds index options to the algorithm.
        /// </summary>
        /// <param name="underlying">The underlying ticker of the Index Option</param>
        /// <param name="targetOption">The target option ticker. This is useful when the option ticker does not match the underlying, e.g. SPX index and the SPXW weekly option. If null is provided will use underlying</param>
        /// <param name="resolution">Resolution of the index option contracts, i.e. the granularity of the data</param>
        /// <param name="market">The foreign exchange trading market, <seealso cref="Market"/>. Default value is null and looked up using BrokerageModel.DefaultMarkets in <see cref="AddSecurity{T}"/></param>
        /// <param name="fillForward">If true, this will fill in missing data points with the previous data point</param>
        /// <returns>Canonical Option security</returns>
        [DocumentationAttribute(AddingData)]
        public IndexOption AddIndexOption(string underlying, string targetOption, Resolution? resolution = null, string market = null, bool fillForward = true)
        {
            return AddIndexOption(
                QuantConnect.Symbol.Create(underlying, SecurityType.Index, GetMarket(market, underlying, SecurityType.Index)),
                targetOption, resolution, fillForward);
        }

        /// <summary>
        /// Adds an index option contract to the algorithm.
        /// </summary>
        /// <param name="symbol">Symbol of the index option contract</param>
        /// <param name="resolution">Resolution of the index option contract, i.e. the granularity of the data</param>
        /// <param name="fillForward">If true, this will fill in missing data points with the previous data point</param>
        /// <returns>Index Option Contract</returns>
        /// <exception cref="ArgumentException">The provided Symbol is not an Index Option</exception>
        [DocumentationAttribute(AddingData)]
        public IndexOption AddIndexOptionContract(Symbol symbol, Resolution? resolution = null, bool fillForward = true)
        {
            if (symbol.SecurityType != SecurityType.IndexOption || symbol.IsCanonical())
            {
                throw new ArgumentException("Symbol provided must be non-canonical and of type SecurityType.IndexOption");
            }

            return (IndexOption)AddOptionContract(symbol, resolution, fillForward);
        }

        /// <summary>
        /// Creates and adds a new single <see cref="Option"/> contract to the algorithm
        /// </summary>
        /// <param name="symbol">The option contract symbol</param>
        /// <param name="resolution">The <see cref="Resolution"/> of market data, Tick, Second, Minute, Hour, or Daily. Default is <see cref="Resolution.Minute"/></param>
        /// <param name="fillForward">If true, returns the last available data even if none in that timeslice. Default is <value>true</value></param>
        /// <param name="leverage">The requested leverage for this equity. Default is set by <see cref="SecurityInitializer"/></param>
        /// <param name="extendedMarketHours">Use extended market hours data</param>
        /// <returns>The new <see cref="Option"/> security</returns>
        [DocumentationAttribute(AddingData)]
        public Option AddOptionContract(Symbol symbol, Resolution? resolution = null, bool fillForward = true,
            decimal leverage = Security.NullLeverage, bool extendedMarketHours = false)
        {
            if (symbol == null || !symbol.SecurityType.IsOption() || symbol.Underlying == null)
            {
                throw new ArgumentException($"Unexpected option symbol {symbol}. " +
                    $"Please provide a valid option contract with it's underlying symbol set.");
            }

            // add underlying if not present
            var underlying = symbol.Underlying;
            Security underlyingSecurity;
            List<SubscriptionDataConfig> underlyingConfigs;
            if (!Securities.TryGetValue(underlying, out underlyingSecurity) ||
                // The underlying might have been removed, let's see if there's already a subscription for it
                (!underlyingSecurity.IsTradable && SubscriptionManager.SubscriptionDataConfigService.GetSubscriptionDataConfigs(underlying).Count == 0))
            {
                underlyingSecurity = AddSecurity(underlying, resolution, fillForward, leverage, extendedMarketHours);
                underlyingConfigs = SubscriptionManager.SubscriptionDataConfigService
                    .GetSubscriptionDataConfigs(underlying);
            }
            else if (underlyingSecurity != null && underlyingSecurity.IsDelisted)
            {
                throw new ArgumentException($"The underlying {underlying.SecurityType} asset ({underlying.Value}) is delisted " +
                    $"(current time is {Time})");
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
                    throw new ArgumentException($"The underlying {underlying.SecurityType} asset ({underlying.Value}) is set to " +
                        $"{dataNormalizationMode}, please change this to DataNormalizationMode.Raw with the " +
                        "SetDataNormalization() method"
                    );
                }
            }

            var configs = SubscriptionManager.SubscriptionDataConfigService.Add(symbol, resolution, fillForward, extendedMarketHours,
                dataNormalizationMode: DataNormalizationMode.Raw);
            var option = (Option)Securities.CreateSecurity(symbol, configs, leverage, underlying: underlyingSecurity);

            underlyingConfigs.SetDataNormalizationMode(DataNormalizationMode.Raw);
            // For backward compatibility we need to refresh the security DataNormalizationMode Property
            underlyingSecurity.RefreshDataNormalizationModeProperty();

            Securities.Add(option);

            // get or create the universe
            var universeSymbol = OptionContractUniverse.CreateSymbol(symbol.ID.Market, symbol.Underlying.SecurityType);
            Universe universe;
            if (!UniverseManager.TryGetValue(universeSymbol, out universe))
            {
                var settings = new UniverseSettings(UniverseSettings)
                {
                    DataNormalizationMode = DataNormalizationMode.Raw,
                    Resolution = underlyingConfigs.GetHighestResolution(),
                    ExtendedMarketHours = extendedMarketHours
                };
                universe = AddUniverse(new OptionContractUniverse(new SubscriptionDataConfig(configs.First(),
                    // We can use any data type here, since we are not going to use the data.
                    // We just don't want to use the OptionUniverse type because it will force disable extended market hours
                    symbol: universeSymbol, objectType: typeof(Tick), extendedHours: extendedMarketHours), settings));
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
        /// <param name="fillForward">If true, returns the last available data even if none in that timeslice. Default is <value>true</value></param>
        /// <param name="leverage">The requested leverage for this equity. Default is set by <see cref="SecurityInitializer"/></param>
        /// <returns>The new <see cref="Forex"/> security</returns>
        [DocumentationAttribute(AddingData)]
        public Forex AddForex(string ticker, Resolution? resolution = null, string market = null, bool fillForward = true, decimal leverage = Security.NullLeverage)
        {
            return AddSecurity<Forex>(SecurityType.Forex, ticker, resolution, market, fillForward, leverage, false);
        }

        /// <summary>
        /// Creates and adds a new <see cref="Cfd"/> security to the algorithm
        /// </summary>
        /// <param name="ticker">The currency pair</param>
        /// <param name="resolution">The <see cref="Resolution"/> of market data, Tick, Second, Minute, Hour, or Daily. Default is <see cref="Resolution.Minute"/></param>
        /// <param name="market">The cfd trading market, <seealso cref="Market"/>. Default value is null and looked up using BrokerageModel.DefaultMarkets in <see cref="AddSecurity{T}"/></param>
        /// <param name="fillForward">If true, returns the last available data even if none in that timeslice. Default is <value>true</value></param>
        /// <param name="leverage">The requested leverage for this equity. Default is set by <see cref="SecurityInitializer"/></param>
        /// <returns>The new <see cref="Cfd"/> security</returns>
        [DocumentationAttribute(AddingData)]
        public Cfd AddCfd(string ticker, Resolution? resolution = null, string market = null, bool fillForward = true, decimal leverage = Security.NullLeverage)
        {
            return AddSecurity<Cfd>(SecurityType.Cfd, ticker, resolution, market, fillForward, leverage, false);
        }


        /// <summary>
        /// Creates and adds a new <see cref="Index"/> security to the algorithm
        /// </summary>
        /// <param name="ticker">The currency pair</param>
        /// <param name="resolution">The <see cref="Resolution"/> of market data, Tick, Second, Minute, Hour, or Daily. Default is <see cref="Resolution.Minute"/></param>
        /// <param name="market">The index trading market, <seealso cref="Market"/>. Default value is null and looked up using BrokerageModel.DefaultMarkets in <see cref="AddSecurity{T}"/></param>
        /// <param name="fillForward">If true, returns the last available data even if none in that timeslice. Default is <value>true</value></param>
        /// <returns>The new <see cref="Index"/> security</returns>
        [DocumentationAttribute(AddingData)]
        public Index AddIndex(string ticker, Resolution? resolution = null, string market = null, bool fillForward = true)
        {
            var index = AddSecurity<Index>(SecurityType.Index, ticker, resolution, market, fillForward, 1, false);
            return index;
        }

        /// <summary>
        /// Creates and adds a new <see cref="Crypto"/> security to the algorithm
        /// </summary>
        /// <param name="ticker">The currency pair</param>
        /// <param name="resolution">The <see cref="Resolution"/> of market data, Tick, Second, Minute, Hour, or Daily. Default is <see cref="Resolution.Minute"/></param>
        /// <param name="market">The cfd trading market, <seealso cref="Market"/>. Default value is null and looked up using BrokerageModel.DefaultMarkets in <see cref="AddSecurity{T}"/></param>
        /// <param name="fillForward">If true, returns the last available data even if none in that timeslice. Default is <value>true</value></param>
        /// <param name="leverage">The requested leverage for this equity. Default is set by <see cref="SecurityInitializer"/></param>
        /// <returns>The new <see cref="Crypto"/> security</returns>
        [DocumentationAttribute(AddingData)]
        public Crypto AddCrypto(string ticker, Resolution? resolution = null, string market = null, bool fillForward = true, decimal leverage = Security.NullLeverage)
        {
            return AddSecurity<Crypto>(SecurityType.Crypto, ticker, resolution, market, fillForward, leverage, false);
        }

        /// <summary>
        /// Creates and adds a new <see cref="CryptoFuture"/> security to the algorithm
        /// </summary>
        /// <param name="ticker">The currency pair</param>
        /// <param name="resolution">The <see cref="Resolution"/> of market data, Tick, Second, Minute, Hour, or Daily. Default is <see cref="Resolution.Minute"/></param>
        /// <param name="market">The cfd trading market, <seealso cref="Market"/>. Default value is null and looked up using BrokerageModel.DefaultMarkets in <see cref="AddSecurity{T}"/></param>
        /// <param name="fillForward">If true, returns the last available data even if none in that timeslice. Default is <value>true</value></param>
        /// <param name="leverage">The requested leverage for this equity. Default is set by <see cref="SecurityInitializer"/></param>
        /// <returns>The new <see cref="CryptoFuture"/> security</returns>
        [DocumentationAttribute(AddingData)]
        public CryptoFuture AddCryptoFuture(string ticker, Resolution? resolution = null, string market = null, bool fillForward = true, decimal leverage = Security.NullLeverage)
        {
            return AddSecurity<CryptoFuture>(SecurityType.CryptoFuture, ticker, resolution, market, fillForward, leverage, false);
        }

        /// <summary>
        /// Removes the security with the specified symbol. This will cancel all
        /// open orders and then liquidate any existing holdings
        /// </summary>
        /// <param name="symbol">The symbol of the security to be removed</param>
        /// <remarks>Sugar syntax for <see cref="AddOptionContract"/></remarks>
        [DocumentationAttribute(AddingData)]
        public bool RemoveOptionContract(Symbol symbol)
        {
            return RemoveSecurity(symbol);
        }

        /// <summary>
        /// Removes the security with the specified symbol. This will cancel all
        /// open orders and then liquidate any existing holdings
        /// </summary>
        /// <param name="symbol">The symbol of the security to be removed</param>
        [DocumentationAttribute(AddingData)]
        public bool RemoveSecurity(Symbol symbol)
        {
            Security security;
            if (!Securities.TryGetValue(symbol, out security))
            {
                return false;
            }

            if (!IsWarmingUp)
            {
                // cancel open orders
                Transactions.CancelOpenOrders(security.Symbol);
            }

            // liquidate if invested
            if (security.Invested)
            {
                Liquidate(security.Symbol);
            }

            // Mark security as not tradable
            security.Reset();
            if (symbol.IsCanonical())
            {
                // remove underlying equity data if it's marked as internal
                foreach (var kvp in UniverseManager.Where(x => x.Value.Configuration.Symbol == symbol
                    || x.Value.Configuration.Symbol == ContinuousContractUniverse.CreateSymbol(symbol)))
                {
                    var universe = kvp.Value;
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
                    // we order the securities so that the removal is deterministic, it will liquidate any holdings
                    foreach (var child in universe.Members.Values.OrderBy(security1 => security1.Symbol))
                    {
                        if (!otherUniverses.Any(u => u.Members.ContainsKey(child.Symbol)) && !child.Symbol.IsCanonical())
                        {
                            RemoveSecurity(child.Symbol);
                        }
                    }

                    // finally, dispose and remove the canonical security from the universe manager
                    UniverseManager.Remove(kvp.Key);
                    _universeSelectionUniverses.Remove(security.Symbol);
                }
            }
            else
            {
                lock (_pendingUniverseAdditionsLock)
                {
                    // we need to handle existing universes and pending to be added universes, that will be pushed
                    // at the end of this time step see OnEndOfTimeStep()
                    foreach (var universe in UniverseManager.Select(x => x.Value).OfType<UserDefinedUniverse>())
                    {
                        universe.Remove(symbol);
                    }
                    // for existing universes we need to purge pending additions too, also handled at OnEndOfTimeStep()
                    _pendingUserDefinedUniverseSecurityAdditions.RemoveAll(addition => addition.Security.Symbol == symbol);
                }
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
        [DocumentationAttribute(AddingData)]
        public Security AddData<T>(string ticker, Resolution? resolution = null)
            where T : IBaseData, new()
        {
            //Add this new generic data as a tradeable security:
            // Defaults:extended market hours"      = true because we want events 24 hours,
            //          fillforward                 = false because only want to trigger when there's new custom data.
            //          leverage                    = 1 because no leverage on nonmarket data?
            return AddData<T>(ticker, resolution, fillForward: false, leverage: 1m);
        }

        /// <summary>
        /// AddData<typeparam name="T"/> a new user defined data source, requiring only the minimum config options.
        /// The data is added with a default time zone of NewYork (Eastern Daylight Savings Time)
        /// </summary>
        /// <param name="underlying">The underlying symbol for the custom data</param>
        /// <param name="resolution">Resolution of the data</param>
        /// <returns>The new <see cref="Security"/></returns>
        /// <remarks>Generic type T must implement base data</remarks>
        [DocumentationAttribute(AddingData)]
        public Security AddData<T>(Symbol underlying, Resolution? resolution = null)
            where T : IBaseData, new()
        {
            //Add this new generic data as a tradeable security:
            // Defaults:extended market hours"      = true because we want events 24 hours,
            //          fillforward                 = false because only want to trigger when there's new custom data.
            //          leverage                    = 1 because no leverage on nonmarket data?
            return AddData<T>(underlying, resolution, fillForward: false, leverage: 1m);
        }


        /// <summary>
        /// AddData<typeparam name="T"/> a new user defined data source, requiring only the minimum config options.
        /// The data is added with a default time zone of NewYork (Eastern Daylight Savings Time)
        /// </summary>
        /// <param name="ticker">Key/Ticker for data</param>
        /// <param name="resolution">Resolution of the Data Required</param>
        /// <param name="fillForward">When no data available on a tradebar, return the last data that was generated</param>
        /// <param name="leverage">Custom leverage per security</param>
        /// <returns>The new <see cref="Security"/></returns>
        /// <remarks>Generic type T must implement base data</remarks>
        [DocumentationAttribute(AddingData)]
        public Security AddData<T>(string ticker, Resolution? resolution, bool fillForward, decimal leverage = 1.0m)
            where T : IBaseData, new()
        {
            return AddData<T>(ticker, resolution, null, fillForward, leverage);
        }

        /// <summary>
        /// AddData<typeparam name="T"/> a new user defined data source, requiring only the minimum config options.
        /// The data is added with a default time zone of NewYork (Eastern Daylight Savings Time)
        /// </summary>
        /// <param name="underlying">The underlying symbol for the custom data</param>
        /// <param name="resolution">Resolution of the Data Required</param>
        /// <param name="fillForward">When no data available on a tradebar, return the last data that was generated</param>
        /// <param name="leverage">Custom leverage per security</param>
        /// <returns>The new <see cref="Security"/></returns>
        /// <remarks>Generic type T must implement base data</remarks>
        [DocumentationAttribute(AddingData)]
        public Security AddData<T>(Symbol underlying, Resolution? resolution, bool fillForward, decimal leverage = 1.0m)
            where T : IBaseData, new()
        {
            return AddData<T>(underlying, resolution, null, fillForward, leverage);
        }

        /// <summary>
        /// AddData<typeparam name="T"/> a new user defined data source, requiring only the minimum config options.
        /// </summary>
        /// <param name="ticker">Key/Ticker for data</param>
        /// <param name="resolution">Resolution of the Data Required</param>
        /// <param name="timeZone">Specifies the time zone of the raw data</param>
        /// <param name="fillForward">When no data available on a tradebar, return the last data that was generated</param>
        /// <param name="leverage">Custom leverage per security</param>
        /// <returns>The new <see cref="Security"/></returns>
        /// <remarks>Generic type T must implement base data</remarks>
        [DocumentationAttribute(AddingData)]
        public Security AddData<T>(string ticker, Resolution? resolution, DateTimeZone timeZone, bool fillForward = false, decimal leverage = 1.0m)
            where T : IBaseData, new()
        {
            return AddData(typeof(T), ticker, resolution, timeZone, fillForward, leverage);
        }

        /// <summary>
        /// AddData<typeparam name="T"/> a new user defined data source, requiring only the minimum config options.
        /// </summary>
        /// <param name="underlying">The underlying symbol for the custom data</param>
        /// <param name="resolution">Resolution of the Data Required</param>
        /// <param name="timeZone">Specifies the time zone of the raw data</param>
        /// <param name="fillForward">When no data available on a tradebar, return the last data that was generated</param>
        /// <param name="leverage">Custom leverage per security</param>
        /// <returns>The new <see cref="Security"/></returns>
        /// <remarks>Generic type T must implement base data</remarks>
        [DocumentationAttribute(AddingData)]
        public Security AddData<T>(Symbol underlying, Resolution? resolution, DateTimeZone timeZone, bool fillForward = false, decimal leverage = 1.0m)
            where T : IBaseData, new()
        {
            return AddData(typeof(T), underlying, resolution, timeZone, fillForward, leverage);
        }

        /// <summary>
        /// AddData<typeparam name="T"/> a new user defined data source including symbol properties and exchange hours,
        /// all other vars are not required and will use defaults.
        /// </summary>
        /// <param name="ticker">Key/Ticker for data</param>
        /// <param name="properties">The properties of this new custom data</param>
        /// <param name="exchangeHours">The Exchange hours of this symbol</param>
        /// <param name="resolution">Resolution of the Data Required</param>
        /// <param name="fillForward">When no data available on a tradebar, return the last data that was generated</param>
        /// <param name="leverage">Custom leverage per security</param>
        /// <returns>The new <see cref="Security"/></returns>
        [DocumentationAttribute(AddingData)]
        public Security AddData<T>(string ticker, SymbolProperties properties, SecurityExchangeHours exchangeHours, Resolution? resolution = null, bool fillForward = false, decimal leverage = 1.0m)
            where T : IBaseData, new()
        {
            // Get the right key for storage of base type symbols
            var key = SecurityIdentifier.GenerateBaseSymbol(typeof(T), ticker);

            // Set our database entries for this data type
            SetDatabaseEntries(key, properties, exchangeHours);

            // Then add the data
            return AddData(typeof(T), ticker, resolution, null, fillForward, leverage);
        }

        /// <summary>
        /// Send a debug message to the web console:
        /// </summary>
        /// <param name="message">Message to send to debug console</param>
        /// <seealso cref="Log(string)"/>
        /// <seealso cref="Error(string)"/>
        [DocumentationAttribute(Logging)]
        public void Debug(string message)
        {
            if (!_liveMode && (string.IsNullOrEmpty(message) || _previousDebugMessage == message)) return;
            _debugMessages.Enqueue(message);
            _previousDebugMessage = message;
        }

        /// <summary>
        /// Send a debug message to the web console:
        /// </summary>
        /// <param name="message">Message to send to debug console</param>
        /// <seealso cref="Log(int)"/>
        /// <seealso cref="Error(int)"/>
        [DocumentationAttribute(Logging)]
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
        [DocumentationAttribute(Logging)]
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
        [DocumentationAttribute(Logging)]
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
        [DocumentationAttribute(Logging)]
        public void Log(string message)
        {
            if (!_liveMode && string.IsNullOrEmpty(message)) return;
            _logMessages.Enqueue(message);
        }

        /// <summary>
        /// Added another method for logging if user guessed.
        /// </summary>
        /// <param name="message">Int message to log.</param>
        /// <seealso cref="Debug(int)"/>
        /// <seealso cref="Error(int)"/>
        [DocumentationAttribute(Logging)]
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
        [DocumentationAttribute(Logging)]
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
        [DocumentationAttribute(Logging)]
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
        [DocumentationAttribute(Logging)]
        public void Error(string message)
        {
            if (!_liveMode && (string.IsNullOrEmpty(message) || _previousErrorMessage == message)) return;
            _errorMessages.Enqueue(message);
            _previousErrorMessage = message;
        }

        /// <summary>
        /// Send a int error message to the Console.
        /// </summary>
        /// <param name="message">Message to display in errors grid</param>
        /// <seealso cref="Debug(int)"/>
        /// <seealso cref="Log(int)"/>
        [DocumentationAttribute(Logging)]
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
        [DocumentationAttribute(Logging)]
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
        [DocumentationAttribute(Logging)]
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
        [DocumentationAttribute(Logging)]
        public void Error(Exception error)
        {
            var message = error.Message;
            if (!_liveMode && (string.IsNullOrEmpty(message) || _previousErrorMessage == message)) return;
            _errorMessages.Enqueue(message);
            _previousErrorMessage = message;
        }

        /// <summary>
        /// Terminate the algorithm after processing the current event handler.
        /// </summary>
        /// <param name="message">Exit message to display on quitting</param>
        [DocumentationAttribute(Logging)]
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
        [DocumentationAttribute(Logging)]
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
        [DocumentationAttribute(AddingData)]
        [DocumentationAttribute(HandlingData)]
        public Symbol Symbol(string ticker)
        {
            return SymbolCache.GetSymbol(ticker);
        }

        /// <summary>
        /// For the given symbol will resolve the ticker it used at the current algorithm date
        /// </summary>
        /// <param name="symbol">The symbol to get the ticker for</param>
        /// <returns>The mapped ticker for a symbol</returns>
        [DocumentationAttribute(AddingData)]
        [DocumentationAttribute(HandlingData)]
        public string Ticker(Symbol symbol)
        {
            return SecurityIdentifier.Ticker(symbol, Time);
        }

        /// <summary>
        /// Creates and adds a new <see cref="Security"/> to the algorithm
        /// </summary>
        [DocumentationAttribute(AddingData)]
        private T AddSecurity<T>(SecurityType securityType, string ticker, Resolution? resolution, string market, bool fillForward, decimal leverage, bool extendedMarketHours,
            DataMappingMode? mappingMode = null, DataNormalizationMode? normalizationMode = null)
            where T : Security
        {
            market = GetMarket(market, ticker, securityType);

            Symbol symbol;
            if (!SymbolCache.TryGetSymbol(ticker, out symbol) ||
                symbol.ID.Market != market ||
                symbol.SecurityType != securityType)
            {
                symbol = QuantConnect.Symbol.Create(ticker, securityType, market);
            }

            var configs = SubscriptionManager.SubscriptionDataConfigService.Add(symbol, resolution, fillForward, extendedMarketHours,
                dataNormalizationMode: normalizationMode ?? UniverseSettings.DataNormalizationMode,
                dataMappingMode: mappingMode ?? UniverseSettings.DataMappingMode);
            var security = Securities.CreateSecurity(symbol, configs, leverage);

            return (T)AddToUserDefinedUniverse(security, configs);
        }

        /// <summary>
        /// Set the historical data provider
        /// </summary>
        /// <param name="historyProvider">Historical data provider</param>
        [DocumentationAttribute(HistoricalData)]
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
        [DocumentationAttribute(HandlingData)]
        [DocumentationAttribute(LiveTrading)]
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
        [DocumentationAttribute(LiveTrading)]
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
        [DocumentationAttribute(AddingData)]
        [DocumentationAttribute(MachineLearning)]
        public string Download(string address) => Download(address, Enumerable.Empty<KeyValuePair<string, string>>());

        /// <summary>
        /// Downloads the requested resource as a <see cref="string"/>.
        /// The resource to download is specified as a <see cref="string"/> containing the URI.
        /// </summary>
        /// <param name="address">A string containing the URI to download</param>
        /// <param name="headers">Defines header values to add to the request</param>
        /// <returns>The requested resource as a <see cref="string"/></returns>
        [DocumentationAttribute(AddingData)]
        [DocumentationAttribute(MachineLearning)]
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
        [DocumentationAttribute(AddingData)]
        [DocumentationAttribute(MachineLearning)]
        public string Download(string address, IEnumerable<KeyValuePair<string, string>> headers, string userName, string password)
        {
            return _api.Download(address, headers, userName, password);
        }

        /// <summary>
        /// Schedules the provided training code to execute immediately
        /// </summary>
        /// <param name="trainingCode">The training code to be invoked</param>
        [DocumentationAttribute(MachineLearning)]
        [DocumentationAttribute(ScheduledEvents)]
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
        [DocumentationAttribute(MachineLearning)]
        [DocumentationAttribute(ScheduledEvents)]
        public ScheduledEvent Train(IDateRule dateRule, ITimeRule timeRule, Action trainingCode)
        {
            return Schedule.Training(dateRule, timeRule, trainingCode);
        }

        /// <summary>
        /// Event invocator for the <see cref="InsightsGenerated"/> event
        /// </summary>
        /// <param name="insights">The collection of insights generaed at the current time step</param>
        [DocumentationAttribute(AlgorithmFramework)]
        private void OnInsightsGenerated(Insight[] insights)
        {
            // debug printing of generated insights
            if (DebugMode)
            {
                Log($"{Time}: ALPHA: {string.Join(" | ", insights.Select(i => i.ToString()).OrderBy(i => i))}");
            }

            Insights.AddRange(insights);

            InsightsGenerated?.Invoke(this, new GeneratedInsightsCollection(UtcTime, insights));
        }

        /// <summary>
        /// Sets the current slice
        /// </summary>
        /// <param name="slice">The Slice object</param>
        [DocumentationAttribute(HandlingData)]
        public void SetCurrentSlice(Slice slice)
        {
            CurrentSlice = slice;
        }


        /// <summary>
        /// Provide the API for the algorithm.
        /// </summary>
        /// <param name="api">Initiated API</param>
        [DocumentationAttribute(HandlingData)]
        public void SetApi(IApi api)
        {
            _api = api;
        }

        /// <summary>
        /// Sets the object store
        /// </summary>
        /// <param name="objectStore">The object store</param>
        [DocumentationAttribute(HandlingData)]
        [DocumentationAttribute(MachineLearning)]
        public void SetObjectStore(IObjectStore objectStore)
        {
            ObjectStore = new ObjectStore(objectStore);
        }

        /// <summary>
        /// Determines if the Symbol is shortable at the brokerage
        /// </summary>
        /// <param name="symbol">Symbol to check if shortable</param>
        /// <returns>True if shortable</returns>
        [DocumentationAttribute(TradingAndOrders)]
        public bool Shortable(Symbol symbol)
        {
            return Shortable(symbol, 0);
        }

        /// <summary>
        /// Determines if the Symbol is shortable at the brokerage
        /// </summary>
        /// <param name="symbol">Symbol to check if shortable</param>
        /// <param name="shortQuantity">Order's quantity to check if it is currently shortable, taking into account current holdings and open orders</param>
        /// <param name="updateOrderId">Optionally the id of the order being updated. When updating an order
        /// we want to ignore it's submitted short quantity and use the new provided quantity to determine if we
        /// can perform the update</param>
        /// <returns>True if the symbol can be shorted by the requested quantity</returns>
        [DocumentationAttribute(TradingAndOrders)]
        public bool Shortable(Symbol symbol, decimal shortQuantity, int? updateOrderId = null)
        {
            var security = Securities[symbol];
            var shortableQuantity = security.ShortableProvider.ShortableQuantity(symbol, security.LocalTime);
            if (shortableQuantity == null)
            {
                return true;
            }

            var openOrderQuantity = Transactions.GetOpenOrdersRemainingQuantity(
                // if 'updateOrderId' was given, ignore that orders quantity
                order => order.Symbol == symbol && (!updateOrderId.HasValue || order.OrderId != updateOrderId.Value));

            var portfolioQuantity = security.Holdings.Quantity;
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
        [DocumentationAttribute(TradingAndOrders)]
        public long ShortableQuantity(Symbol symbol)
        {
            var security = Securities[symbol];
            return security.ShortableProvider.ShortableQuantity(symbol, security.LocalTime) ?? 0;
        }

        /// <summary>
        /// Converts an ISIN identifier into a <see cref="Symbol"/>
        /// </summary>
        /// <param name="isin">The International Securities Identification Number (ISIN) of an asset</param>
        /// <param name="tradingDate">
        /// The date that the stock being looked up is/was traded at.
        /// The date is used to create a Symbol with the ticker set to the ticker the asset traded under on the trading date.
        /// </param>
        /// <returns>Symbol corresponding to the ISIN. If no Symbol with a matching ISIN was found, returns null.</returns>
        [DocumentationAttribute(HandlingData)]
        [DocumentationAttribute(SecuritiesAndPortfolio)]
        public Symbol ISIN(string isin, DateTime? tradingDate = null)
        {
            return SecurityDefinitionSymbolResolver.ISIN(isin, GetVerifiedTradingDate(tradingDate));
        }

        /// <summary>
        /// Converts a <see cref="Symbol"/> into an ISIN identifier
        /// </summary>
        /// <param name="symbol">The <see cref="Symbol"/></param>
        /// <returns>ISIN corresponding to the Symbol. If no matching ISIN is found, returns null.</returns>
        [DocumentationAttribute(HandlingData)]
        [DocumentationAttribute(SecuritiesAndPortfolio)]
        public string ISIN(Symbol symbol)
        {
            return SecurityDefinitionSymbolResolver.ISIN(symbol);
        }

        /// <summary>
        /// Converts a composite FIGI identifier into a <see cref="Symbol"/>
        /// </summary>
        /// <param name="compositeFigi">The composite Financial Instrument Global Identifier (FIGI) of an asset</param>
        /// <param name="tradingDate">
        /// The date that the stock being looked up is/was traded at.
        /// The date is used to create a Symbol with the ticker set to the ticker the asset traded under on the trading date.
        /// </param>
        /// <returns>Symbol corresponding to the composite FIGI. If no Symbol with a matching composite FIGI was found, returns null.</returns>
        /// <remarks>
        /// The composite FIGI differs from an exchange-level FIGI, in that it identifies
        /// an asset across all exchanges in a single country that the asset trades in.
        /// </remarks>
        [DocumentationAttribute(HandlingData)]
        [DocumentationAttribute(SecuritiesAndPortfolio)]
        public Symbol CompositeFIGI(string compositeFigi, DateTime? tradingDate = null)
        {
            return SecurityDefinitionSymbolResolver.CompositeFIGI(compositeFigi, GetVerifiedTradingDate(tradingDate));
        }

        /// <summary>
        /// Converts a <see cref="Symbol"/> into a composite FIGI identifier
        /// </summary>
        /// <param name="symbol">The <see cref="Symbol"/></param>
        /// <returns>Composite FIGI corresponding to the Symbol. If no matching composite FIGI is found, returns null.</returns>
        [DocumentationAttribute(HandlingData)]
        [DocumentationAttribute(SecuritiesAndPortfolio)]
        public string CompositeFIGI(Symbol symbol)
        {
            return SecurityDefinitionSymbolResolver.CompositeFIGI(symbol);
        }

        /// <summary>
        /// Converts a CUSIP identifier into a <see cref="Symbol"/>
        /// </summary>
        /// <param name="cusip">The CUSIP number of an asset</param>
        /// <param name="tradingDate">
        /// The date that the stock being looked up is/was traded at.
        /// The date is used to create a Symbol with the ticker set to the ticker the asset traded under on the trading date.
        /// </param>
        /// <returns>Symbol corresponding to the CUSIP. If no Symbol with a matching CUSIP was found, returns null.</returns>
        [DocumentationAttribute(HandlingData)]
        [DocumentationAttribute(SecuritiesAndPortfolio)]
        public Symbol CUSIP(string cusip, DateTime? tradingDate = null)
        {
            return SecurityDefinitionSymbolResolver.CUSIP(cusip, GetVerifiedTradingDate(tradingDate));
        }

        /// <summary>
        /// Converts a <see cref="Symbol"/> into a CUSIP identifier
        /// </summary>
        /// <param name="symbol">The <see cref="Symbol"/></param>
        /// <returns>CUSIP corresponding to the Symbol. If no matching CUSIP is found, returns null.</returns>
        [DocumentationAttribute(HandlingData)]
        [DocumentationAttribute(SecuritiesAndPortfolio)]
        public string CUSIP(Symbol symbol)
        {
            return SecurityDefinitionSymbolResolver.CUSIP(symbol);
        }

        /// <summary>
        /// Converts a SEDOL identifier into a <see cref="Symbol"/>
        /// </summary>
        /// <param name="sedol">The SEDOL identifier of an asset</param>
        /// <param name="tradingDate">
        /// The date that the stock being looked up is/was traded at.
        /// The date is used to create a Symbol with the ticker set to the ticker the asset traded under on the trading date.
        /// </param>
        /// <returns>Symbol corresponding to the SEDOL. If no Symbol with a matching SEDOL was found, returns null.</returns>
        [DocumentationAttribute(HandlingData)]
        [DocumentationAttribute(SecuritiesAndPortfolio)]
        public Symbol SEDOL(string sedol, DateTime? tradingDate = null)
        {
            return SecurityDefinitionSymbolResolver.SEDOL(sedol, GetVerifiedTradingDate(tradingDate));
        }

        /// <summary>
        /// Converts a <see cref="Symbol"/> into a SEDOL identifier
        /// </summary>
        /// <param name="symbol">The <see cref="Symbol"/></param>
        /// <returns>SEDOL corresponding to the Symbol. If no matching SEDOL is found, returns null.</returns>
        [DocumentationAttribute(HandlingData)]
        [DocumentationAttribute(SecuritiesAndPortfolio)]
        public string SEDOL(Symbol symbol)
        {
            return SecurityDefinitionSymbolResolver.SEDOL(symbol);
        }

        /// <summary>
        /// Converts a CIK identifier into <see cref="Symbol"/> array
        /// </summary>
        /// <param name="cik">The CIK identifier of an asset</param>
        /// <param name="tradingDate">
        /// The date that the stock being looked up is/was traded at.
        /// The date is used to create a Symbol with the ticker set to the ticker the asset traded under on the trading date.
        /// </param>
        /// <returns>Symbols corresponding to the CIK. If no Symbol with a matching CIK was found, returns empty array.</returns>
        [DocumentationAttribute(HandlingData)]
        [DocumentationAttribute(SecuritiesAndPortfolio)]
        public Symbol[] CIK(int cik, DateTime? tradingDate = null)
        {
            return SecurityDefinitionSymbolResolver.CIK(cik, GetVerifiedTradingDate(tradingDate));
        }

        /// <summary>
        /// Converts a <see cref="Symbol"/> into a CIK identifier
        /// </summary>
        /// <param name="symbol">The <see cref="Symbol"/></param>
        /// <returns>CIK corresponding to the Symbol. If no matching CIK is found, returns null.</returns>
        [DocumentationAttribute(HandlingData)]
        [DocumentationAttribute(SecuritiesAndPortfolio)]
        public int? CIK(Symbol symbol)
        {
            return SecurityDefinitionSymbolResolver.CIK(symbol);
        }

        /// <summary>
        /// Get the fundamental data for the requested symbol at the current time
        /// </summary>
        /// <param name="symbol">The <see cref="Symbol"/></param>
        /// <returns>The fundamental data for the Symbol</returns>
        [DocumentationAttribute(HandlingData)]
        [DocumentationAttribute(SecuritiesAndPortfolio)]
        public Fundamental Fundamentals(Symbol symbol)
        {
            return new Fundamental(Time, symbol) { EndTime = Time };
        }

        /// <summary>
        /// Get the fundamental data for the requested symbols at the current time
        /// </summary>
        /// <param name="symbols">The <see cref="Symbol"/></param>
        /// <returns>The fundamental data for the symbols</returns>
        [DocumentationAttribute(HandlingData)]
        [DocumentationAttribute(SecuritiesAndPortfolio)]
        public List<Fundamental> Fundamentals(List<Symbol> symbols)
        {
            return symbols.Select(symbol => Fundamentals(symbol)).ToList();
        }

        /// <summary>
        /// Get the option chain for the specified symbol at the current time (<see cref="Time"/>)
        /// </summary>
        /// <param name="symbol">
        /// The symbol for which the option chain is asked for.
        /// It can be either the canonical option or the underlying symbol.
        /// </param>
        /// <param name="flatten">
        /// Whether to flatten the resulting data frame. Used from Python when accessing <see cref="OptionChain.DataFrame"/>.
        /// See <see cref="History(PyObject, int, Resolution?, bool?, bool?, DataMappingMode?, DataNormalizationMode?, int?, bool)"/>
        /// </param>
        /// <returns>The option chain</returns>
        /// <remarks>
        /// As of 2024/09/11, future options chain will not contain any additional data (e.g. daily price data, implied volatility and greeks),
        /// it will be populated with the contract symbol only. This is expected to change in the future.
        /// As of 2024/12/18, future options data will contain daily price data but not implied volatility and greeks.
        /// </remarks>
        [DocumentationAttribute(AddingData)]
        public OptionChain OptionChain(Symbol symbol, bool flatten = false)
        {
            return OptionChains(new[] { symbol }, flatten).Values.SingleOrDefault() ??
                new OptionChain(GetCanonicalOptionSymbol(symbol), Time.Date, flatten);
        }

        /// <summary>
        /// Get the option chains for the specified symbols at the current time (<see cref="Time"/>)
        /// </summary>
        /// <param name="symbols">
        /// The symbols for which the option chain is asked for.
        /// It can be either the canonical options or the underlying symbols.
        /// </param>
        /// <param name="flatten">
        /// Whether to flatten the resulting data frame. Used from Python when accessing <see cref="OptionChain.DataFrame"/>.
        /// See <see cref="History(PyObject, int, Resolution?, bool?, bool?, DataMappingMode?, DataNormalizationMode?, int?, bool)"/>
        /// </param>
        /// <returns>The option chains</returns>
        [DocumentationAttribute(AddingData)]
        public OptionChains OptionChains(IEnumerable<Symbol> symbols, bool flatten = false)
        {
            var canonicalSymbols = symbols.Select(GetCanonicalOptionSymbol).ToList();
            var optionChainsData = GetChainsData<OptionUniverse>(canonicalSymbols);

            var chains = new OptionChains(Time.Date, flatten);
            foreach (var (symbol, contracts) in optionChainsData)
            {
                var symbolProperties = SymbolPropertiesDatabase.GetSymbolProperties(symbol.ID.Market, symbol, symbol.SecurityType, AccountCurrency);
                var optionChain = new OptionChain(symbol, GetTimeInExchangeTimeZone(symbol).Date, contracts, symbolProperties, flatten);
                chains.Add(symbol, optionChain);
            }

            return chains;
        }

        /// <summary>
        /// Get the futures chain for the specified symbol at the current time (<see cref="Time"/>)
        /// </summary>
        /// <param name="symbol">
        /// The symbol for which the futures chain is asked for.
        /// It can be either the canonical future, a contract or an option symbol.
        /// </param>
        /// <param name="flatten">
        /// Whether to flatten the resulting data frame. Used from Python when accessing <see cref="FuturesChain.DataFrame"/>.
        /// See <see cref="History(PyObject, int, Resolution?, bool?, bool?, DataMappingMode?, DataNormalizationMode?, int?, bool)"/>
        /// </param>
        /// <returns>The futures chain</returns>
        [DocumentationAttribute(AddingData)]
        public FuturesChain FutureChain(Symbol symbol, bool flatten = false)
        {
            return FuturesChain(symbol, flatten);
        }

        /// <summary>
        /// Get the futures chain for the specified symbol at the current time (<see cref="Time"/>)
        /// </summary>
        /// <param name="symbol">
        /// The symbol for which the futures chain is asked for.
        /// It can be either the canonical future, a contract or an option symbol.
        /// </param>
        /// <param name="flatten">
        /// Whether to flatten the resulting data frame. Used from Python when accessing <see cref="FuturesChain.DataFrame"/>.
        /// See <see cref="History(PyObject, int, Resolution?, bool?, bool?, DataMappingMode?, DataNormalizationMode?, int?, bool)"/>
        /// </param>
        /// <returns>The futures chain</returns>
        [DocumentationAttribute(AddingData)]
        public FuturesChain FuturesChain(Symbol symbol, bool flatten = false)
        {
            return FuturesChains(new[] { symbol }, flatten).Values.SingleOrDefault() ??
                new FuturesChain(GetCanonicalFutureSymbol(symbol), Time.Date);
        }

        /// <summary>
        /// Get the futures chains for the specified symbols at the current time (<see cref="Time"/>)
        /// </summary>
        /// <param name="symbols">
        /// The symbols for which the futures chains are asked for.
        /// It can be either the canonical future, a contract or an option symbol.
        /// </param>
        /// <param name="flatten">
        /// Whether to flatten the resulting data frame. Used from Python when accessing <see cref="FuturesChains.DataFrame"/>.
        /// See <see cref="History(PyObject, int, Resolution?, bool?, bool?, DataMappingMode?, DataNormalizationMode?, int?, bool)"/>
        /// </param>
        /// <returns>The futures chains</returns>
        [DocumentationAttribute(AddingData)]
        public FuturesChains FutureChains(IEnumerable<Symbol> symbols, bool flatten = false)
        {
            return FuturesChains(symbols, flatten);
        }

        /// <summary>
        /// Get the futures chains for the specified symbols at the current time (<see cref="Time"/>)
        /// </summary>
        /// <param name="symbols">
        /// The symbols for which the futures chains are asked for.
        /// It can be either the canonical future, a contract or an option symbol.
        /// </param>
        /// <param name="flatten">
        /// Whether to flatten the resulting data frame. Used from Python when accessing <see cref="FuturesChains.DataFrame"/>.
        /// See <see cref="History(PyObject, int, Resolution?, bool?, bool?, DataMappingMode?, DataNormalizationMode?, int?, bool)"/>
        /// </param>
        /// <returns>The futures chains</returns>
        [DocumentationAttribute(AddingData)]
        public FuturesChains FuturesChains(IEnumerable<Symbol> symbols, bool flatten = false)
        {
            var canonicalSymbols = symbols.Select(GetCanonicalFutureSymbol).ToList();
            var futureChainsData = GetChainsData<FutureUniverse>(canonicalSymbols);

            var chains = new FuturesChains(Time.Date, flatten);

            if (futureChainsData != null)
            {
                foreach (var (symbol, contracts) in futureChainsData)
                {
                    var chain = new FuturesChain(symbol, GetTimeInExchangeTimeZone(symbol).Date, contracts, flatten);
                    chains.Add(symbol, chain);
                }
            }

            return chains;
        }

        /// <summary>
        /// Get an authenticated link to execute the given command instance
        /// </summary>
        /// <param name="command">The target command</param>
        /// <returns>The authenticated link</returns>
        public string Link(object command)
        {
            var typeName = command.GetType().Name;
            if (command is Command || typeName.Contains("AnonymousType", StringComparison.InvariantCultureIgnoreCase))
            {
                return CommandLink(typeName, command);
            }
            // this shouldn't happen but just in case
            throw new ArgumentException($"Unexpected command type: {typeName}");
        }

        /// <summary>
        /// Register a command type to be used
        /// </summary>
        /// <typeparam name="T">The command type</typeparam>
        public void AddCommand<T>() where T : Command
        {
            _registeredCommands[typeof(T).Name] = (CallbackCommand command) =>
            {
                var commandInstance = JsonConvert.DeserializeObject<T>(command.Payload);
                return commandInstance.Run(this);
            };
        }

        /// <summary>
        /// Broadcast a live command
        /// </summary>
        /// <param name="command">The target command</param>
        /// <returns><see cref="RestResponse"/></returns>
        public RestResponse BroadcastCommand(object command)
        {
            var typeName = command.GetType().Name;
            if (command is Command || typeName.Contains("AnonymousType", StringComparison.InvariantCultureIgnoreCase))
            {
                var serialized = JsonConvert.SerializeObject(command);
                var payload = JsonConvert.DeserializeObject<Dictionary<string, object>>(serialized);
                return SendBroadcast(typeName, payload);
            }
            // this shouldn't happen but just in case
            throw new ArgumentException($"Unexpected command type: {typeName}");
        }

        /// <summary>
        /// Run a callback command instance
        /// </summary>
        /// <param name="command">The callback command instance</param>
        /// <returns>The command result</returns>
        public CommandResultPacket RunCommand(CallbackCommand command)
        {
            bool? result = null;
            if (_registeredCommands.TryGetValue(command.Type, out var target))
            {
                try
                {
                    result = target.Invoke(command);
                }
                catch (Exception ex)
                {
                    QuantConnect.Logging.Log.Error(ex);
                    if (_oneTimeCommandErrors.Add(command.Type))
                    {
                        Log($"Unexpected error running command '{command.Type}' error: '{ex.Message}'");
                    }
                }
            }
            else
            {
                if (_oneTimeCommandErrors.Add(command.Type))
                {
                    Log($"Detected unregistered command type '{command.Type}', will be ignored");
                }
            }
            return new CommandResultPacket(command, result) { CommandName = command.Type };
        }

        /// <summary>
        /// Generic untyped command call handler
        /// </summary>
        /// <param name="data">The associated data</param>
        /// <returns>True if success, false otherwise. Returning null will disable command feedback</returns>
        public virtual bool? OnCommand(dynamic data)
        {
            return true;
        }

        /// <summary>
        /// Helper method to get a market for a given security type and ticker
        /// </summary>
        private string GetMarket(string market, string ticker, SecurityType securityType, string defaultMarket = null)
        {
            if (string.IsNullOrEmpty(market))
            {
                if (securityType == SecurityType.Index && IndexSymbol.TryGetIndexMarket(ticker, out market))
                {
                    return market;
                }

                if (securityType == SecurityType.Future && SymbolPropertiesDatabase.TryGetMarket(ticker, securityType, out market))
                {
                    return market;
                }

                if (!BrokerageModel.DefaultMarkets.TryGetValue(securityType, out market))
                {
                    if (string.IsNullOrEmpty(defaultMarket))
                    {
                        throw new KeyNotFoundException($"No default market set for security type: {securityType}");
                    }
                    return defaultMarket;
                }
            }
            return market;
        }

        private string CommandLink(string typeName, object command)
        {
            var payload = new Dictionary<string, dynamic> { { "projectId", ProjectId }, { "command", command } };
            if (_registeredCommands.ContainsKey(typeName))
            {
                payload["command[$type]"] = typeName;
            }
            return Authentication.Link("live/commands/create", payload);
        }

        private RestResponse SendBroadcast(string typeName, Dictionary<string, object> payload)
        {
            if (AlgorithmMode == AlgorithmMode.Backtesting)
            {
                if (!_sentBroadcastCommandsDisabled)
                {
                    _sentBroadcastCommandsDisabled = true;
                    Debug("Warning: sending broadcast commands is disabled in backtesting");
                }
                return null;
            }

            if (_registeredCommands.ContainsKey(typeName))
            {
                payload["$type"] = typeName;
            }
            return _api.BroadcastLiveCommand(Globals.OrganizationID,
                AlgorithmMode == AlgorithmMode.Live ? ProjectId : null,
                payload);
        }

        private static Symbol GetCanonicalOptionSymbol(Symbol symbol)
        {
            // We got the underlying
            if (symbol.SecurityType.HasOptions())
            {
                return QuantConnect.Symbol.CreateCanonicalOption(symbol);
            }

            if (symbol.SecurityType.IsOption())
            {
                return symbol.Canonical;
            }

            throw new ArgumentException($"The symbol {symbol} is not an option or an underlying symbol.");
        }

        private static Symbol GetCanonicalFutureSymbol(Symbol symbol)
        {
            // We got either a contract or the canonical itself
            if (symbol.SecurityType == SecurityType.Future)
            {
                return symbol.Canonical;
            }

            if (symbol.SecurityType == SecurityType.FutureOption)
            {
                return symbol.Underlying.Canonical;
            }

            throw new ArgumentException($"The symbol {symbol} is neither a future nor a future option symbol.");
        }

        /// <summary>
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

        /// <summary>
        /// Takes a date, and verifies that it is point-in-time. If null
        /// time is provided, algorithm time is returned instead.
        /// </summary>
        /// <param name="tradingDate">
        /// The trading date to verify that it is a point-in-time
        /// date, or before, relative to the algorithm's current trading date.
        /// </param>
        /// <returns>The date provided if not null, otherwise the algorithm's current trading date</returns>
        /// <exception cref="ArgumentException">
        /// The trading date provided is not null and it is after the algorithm's current trading date
        /// </exception>
        private DateTime GetVerifiedTradingDate(DateTime? tradingDate)
        {
            tradingDate ??= Time.Date;
            if (tradingDate > Time.Date)
            {
                throw new ArgumentException($"The trading date provided: \"{tradingDate:yyyy-MM-dd}\" is after the current algorithm's trading date: \"{Time:yyyy-MM-dd}\"");
            }

            return tradingDate.Value;
        }

        /// <summary>
        /// Helper method to set the start date during live trading
        /// </summary>
        private void SetLiveModeStartDate()
        {
            if (!LiveMode)
            {
                throw new InvalidOperationException("SetLiveModeStartDate should only be called during live trading!");
            }
            _start = DateTime.UtcNow.ConvertFromUtc(TimeZone);
            // startDate is set relative to the algorithm's timezone.
            _startDate = _start.Date;
            _endDate = QuantConnect.Time.EndOfTime;
        }

        /// <summary>
        /// Sets the statistics service instance to be used by the algorithm
        /// </summary>
        /// <param name="statisticsService">The statistics service instance</param>
        public void SetStatisticsService(IStatisticsService statisticsService)
        {
            if (_statisticsService == null)
            {
                _statisticsService = statisticsService;
            }
        }

        /// <summary>
        /// Makes a history request to get the option/future chain data for the specified symbols
        /// at the current algorithm time (<see cref="Time"/>)
        /// </summary>
        private IEnumerable<KeyValuePair<Symbol, IEnumerable<T>>> GetChainsData<T>(IEnumerable<Symbol> canonicalSymbols)
            where T : BaseChainUniverseData
        {
            foreach (var symbol in canonicalSymbols)
            {
                // We will add a safety measure in case the universe file for the current time is not available:
                // we will use the latest available universe file within the last 3 trading dates.
                // This is useful in cases like live trading when the algorithm is deployed at a time of day when
                // the universe file is not available yet.
                var history = (DataDictionary<T>)null;
                var periods = 1;
                while ((history == null || history.Count == 0) && periods <= 3)
                {
                    history = History<T>([symbol], periods++).FirstOrDefault();
                }

                var chain = history != null && history.Count > 0 ? history.Values.Single().Cast<T>() : Enumerable.Empty<T>();
                yield return KeyValuePair.Create(symbol, chain);
            }
        }

        /// <summary>
        /// Gets the current time in the exchange time zone for the given symbol
        /// </summary>
        private DateTime GetTimeInExchangeTimeZone(Symbol symbol)
        {
            var exchange = MarketHoursDatabase.GetExchangeHours(symbol.ID.Market, symbol, symbol.SecurityType);
            return UtcTime.ConvertFromUtc(exchange.TimeZone);
        }
    }
}
