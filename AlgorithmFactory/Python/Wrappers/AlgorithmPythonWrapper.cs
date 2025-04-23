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

using NodaTime;
using Python.Runtime;
using QuantConnect.Algorithm;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Benchmarks;
using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Exceptions;
using QuantConnect.Interfaces;
using QuantConnect.Notifications;
using QuantConnect.Orders;
using QuantConnect.Python;
using QuantConnect.Scheduling;
using QuantConnect.Securities;
using QuantConnect.Securities.Future;
using QuantConnect.Securities.Option;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using QuantConnect.Storage;
using QuantConnect.Statistics;
using QuantConnect.Data.Market;
using QuantConnect.Algorithm.Framework.Alphas.Analysis;
using QuantConnect.Commands;
using QuantConnect.Algorithm.Framework.Portfolio.SignalExports;

namespace QuantConnect.AlgorithmFactory.Python.Wrappers
{
    /// <summary>
    /// Creates and wraps the algorithm written in python.
    /// </summary>
    public class AlgorithmPythonWrapper : BasePythonWrapper<IAlgorithm>, IAlgorithm
    {
        private readonly dynamic _onData;
        private readonly dynamic _onMarginCall;
        private readonly IAlgorithm _baseAlgorithm;

        // QCAlgorithm methods that might be implemented in the python algorithm:
        // We keep them to avoid the BasePythonWrapper caching and eventual lookup overhead since these methods are called quite frequently
        private dynamic _onBrokerageDisconnect;
        private dynamic _onBrokerageMessage;
        private dynamic _onBrokerageReconnect;
        private dynamic _onSplits;
        private dynamic _onDividends;
        private dynamic _onDelistings;
        private dynamic _onSymbolChangedEvents;
        private dynamic _onEndOfDay;
        private dynamic _onMarginCallWarning;
        private dynamic _onOrderEvent;
        private dynamic _onCommand;
        private dynamic _onAssignmentOrderEvent;
        private dynamic _onSecuritiesChanged;
        private dynamic _onFrameworkSecuritiesChanged;

        /// <summary>
        /// True if the underlying python algorithm implements "OnEndOfDay"
        /// </summary>
        public bool IsOnEndOfDayImplemented { get; }

        /// <summary>
        /// True if the underlying python algorithm implements "OnEndOfDay(symbol)"
        /// </summary>
        public bool IsOnEndOfDaySymbolImplemented { get; }

        /// <summary>
        /// <see cref = "AlgorithmPythonWrapper"/> constructor.
        /// Creates and wraps the algorithm written in python.
        /// </summary>
        /// <param name="moduleName">Name of the module that can be found in the PYTHONPATH</param>
        public AlgorithmPythonWrapper(string moduleName)
            : base(false)
        {
            try
            {
                using (Py.GIL())
                {
                    Logging.Log.Trace($"AlgorithmPythonWrapper(): Python version {PythonEngine.Version}: Importing python module {moduleName}");

                    var module = Py.Import(moduleName);

                    Logging.Log.Trace($"AlgorithmPythonWrapper(): {moduleName} successfully imported.");

                    var pyList = module.Dir();
                    foreach (var name in pyList)
                    {
                        Type type;
                        var attr = module.GetAttr(name.ToString());
                        var repr = attr.Repr().GetStringBetweenChars('\'', '\'');

                        if (repr.StartsWith(moduleName) &&                // Must be defined in the module
                            attr.TryConvert(out type, true) &&                  // Must be a Type
                            typeof(QCAlgorithm).IsAssignableFrom(type))   // Must inherit from QCAlgorithm
                        {
                            Logging.Log.Trace("AlgorithmPythonWrapper(): Creating IAlgorithm instance.");

                            SetPythonInstance(attr.Invoke());
                            var dynAlgorithm = Instance as dynamic;

                            // Set pandas
                            dynAlgorithm.SetPandasConverter();

                            // IAlgorithm reference for LEAN internal C# calls (without going from C# to Python and back)
                            _baseAlgorithm = dynAlgorithm.AsManagedObject(type);

                            // determines whether OnData method was defined or inherits from QCAlgorithm
                            // If it is not, OnData from the base class will not be called
                            _onData = Instance.GetPythonMethod("OnData");

                            _onMarginCall = Instance.GetPythonMethod("OnMarginCall");

                            using PyObject endOfDayMethod = Instance.GetPythonMethod("OnEndOfDay");
                            if (endOfDayMethod != null)
                            {
                                // Since we have a EOD method implemented
                                // Determine which one it is by inspecting its arg count
                                var argCount = endOfDayMethod.GetPythonArgCount();
                                switch (argCount)
                                {
                                    case 0: // EOD()
                                        IsOnEndOfDayImplemented = true;
                                        break;
                                    case 1: // EOD(Symbol)
                                        IsOnEndOfDaySymbolImplemented = true;
                                        break;
                                }

                                // Its important to note that even if both are implemented
                                // python will only use the last implemented, meaning only one will
                                // be used and seen.
                            }

                            // Initialize the python methods
                            _onBrokerageDisconnect = Instance.GetMethod("OnBrokerageDisconnect");
                            _onBrokerageMessage = Instance.GetMethod("OnBrokerageMessage");
                            _onBrokerageReconnect = Instance.GetMethod("OnBrokerageReconnect");
                            _onSplits = Instance.GetMethod("OnSplits");
                            _onDividends = Instance.GetMethod("OnDividends");
                            _onDelistings = Instance.GetMethod("OnDelistings");
                            _onSymbolChangedEvents = Instance.GetMethod("OnSymbolChangedEvents");
                            _onEndOfDay = Instance.GetMethod("OnEndOfDay");
                            _onCommand = Instance.GetMethod("OnCommand");
                            _onMarginCallWarning = Instance.GetMethod("OnMarginCallWarning");
                            _onOrderEvent = Instance.GetMethod("OnOrderEvent");
                            _onAssignmentOrderEvent = Instance.GetMethod("OnAssignmentOrderEvent");
                            _onSecuritiesChanged = Instance.GetMethod("OnSecuritiesChanged");
                            _onFrameworkSecuritiesChanged = Instance.GetMethod("OnFrameworkSecuritiesChanged");
                        }
                        attr.Dispose();
                    }
                    module.Dispose();
                    pyList.Dispose();
                    // If _algorithm could not be set, throw exception
                    if (Instance == null)
                    {
                        throw new Exception("Please ensure that one class inherits from QCAlgorithm.");
                    }
                }
            }
            catch (Exception e)
            {
                // perform exception interpretation for error in module import
                var interpreter = StackExceptionInterpreter.CreateFromAssemblies(AppDomain.CurrentDomain.GetAssemblies());
                e = interpreter.Interpret(e, interpreter);

                throw new Exception($"AlgorithmPythonWrapper(): {interpreter.GetExceptionMessageHeader(e)}");
            }
        }

        /// <summary>
        /// AlgorithmId for the backtest
        /// </summary>
        public string AlgorithmId => _baseAlgorithm.AlgorithmId;

        /// <summary>
        /// Gets the function used to define the benchmark. This function will return
        /// the value of the benchmark at a requested date/time
        /// </summary>
        public IBenchmark Benchmark => _baseAlgorithm.Benchmark;

        /// <summary>
        /// Gets the brokerage message handler used to decide what to do
        /// with each message sent from the brokerage
        /// </summary>
        public IBrokerageMessageHandler BrokerageMessageHandler
        {
            get
            {
                return _baseAlgorithm.BrokerageMessageHandler;
            }

            set
            {
                SetBrokerageMessageHandler(value);
            }
        }

        /// <summary>
        /// Gets the brokerage model used to emulate a real brokerage
        /// </summary>
        public IBrokerageModel BrokerageModel => _baseAlgorithm.BrokerageModel;

        /// <summary>
        /// Gets the brokerage name.
        /// </summary>
        public BrokerageName BrokerageName => _baseAlgorithm.BrokerageName;

        /// <summary>
        /// Gets the risk free interest rate model used to get the interest rates
        /// </summary>
        public IRiskFreeInterestRateModel RiskFreeInterestRateModel => _baseAlgorithm.RiskFreeInterestRateModel;

        /// <summary>
        /// Debug messages from the strategy:
        /// </summary>
        public ConcurrentQueue<string> DebugMessages => _baseAlgorithm.DebugMessages;

        /// <summary>
        /// Get Requested Backtest End Date
        /// </summary>
        public DateTime EndDate => _baseAlgorithm.EndDate;

        /// <summary>
        /// Error messages from the strategy:
        /// </summary>
        public ConcurrentQueue<string> ErrorMessages => _baseAlgorithm.ErrorMessages;

        /// <summary>
        /// Gets or sets the history provider for the algorithm
        /// </summary>
        public IHistoryProvider HistoryProvider
        {
            get
            {
                return _baseAlgorithm.HistoryProvider;
            }

            set
            {
                SetHistoryProvider(value);
            }
        }

        /// <summary>
        /// Gets whether or not this algorithm is still warming up
        /// </summary>
        public bool IsWarmingUp => _baseAlgorithm.IsWarmingUp;

        /// <summary>
        /// Algorithm is running on a live server.
        /// </summary>
        public bool LiveMode => _baseAlgorithm.LiveMode;

        /// <summary>
        /// Algorithm running mode.
        /// </summary>
        public AlgorithmMode AlgorithmMode => _baseAlgorithm.AlgorithmMode;

        /// <summary>
        /// Deployment target, either local or cloud.
        /// </summary>
        public DeploymentTarget DeploymentTarget => _baseAlgorithm.DeploymentTarget;

        /// <summary>
        /// Log messages from the strategy:
        /// </summary>
        public ConcurrentQueue<string> LogMessages => _baseAlgorithm.LogMessages;

        /// <summary>
        /// Public name for the algorithm.
        /// </summary>
        /// <remarks>Not currently used but preserved for API integrity</remarks>
        public string Name
        {
            get
            {
                return _baseAlgorithm.Name;
            }
            set
            {
                _baseAlgorithm.Name = value;
            }
        }

        /// <summary>
        /// A list of tags associated with the algorithm or the backtest, useful for categorization
        /// </summary>
        public HashSet<string> Tags
        {
            get
            {
                return _baseAlgorithm.Tags;
            }
            set
            {
                _baseAlgorithm.Tags = value;
            }
        }

        /// <summary>
        /// Event fired algorithm's name is changed
        /// </summary>
        public event AlgorithmEvent<string> NameUpdated
        {
            add
            {
                _baseAlgorithm.NameUpdated += value;
            }

            remove
            {
                _baseAlgorithm.NameUpdated -= value;
            }
        }

        /// <summary>
        /// Event fired when the tag collection is updated
        /// </summary>
        public event AlgorithmEvent<HashSet<string>> TagsUpdated
        {
            add
            {
                _baseAlgorithm.TagsUpdated += value;
            }

            remove
            {
                _baseAlgorithm.TagsUpdated -= value;
            }
        }

        /// <summary>
        /// Notification manager for storing and processing live event messages
        /// </summary>
        public NotificationManager Notify => _baseAlgorithm.Notify;

        /// <summary>
        /// Security portfolio management class provides wrapper and helper methods for the Security.Holdings class such as
        /// IsLong, IsShort, TotalProfit
        /// </summary>
        /// <remarks>Portfolio is a wrapper and helper class encapsulating the Securities[].Holdings objects</remarks>
        public SecurityPortfolioManager Portfolio => _baseAlgorithm.Portfolio;

        /// <summary>
        /// Gets the run time error from the algorithm, or null if none was encountered.
        /// </summary>
        public Exception RunTimeError
        {
            get
            {
                return _baseAlgorithm.RunTimeError;
            }

            set
            {
                SetRunTimeError(value);
            }
        }

        /// <summary>
        /// Customizable dynamic statistics displayed during live trading:
        /// </summary>
        public ConcurrentDictionary<string, string> RuntimeStatistics => _baseAlgorithm.RuntimeStatistics;

        /// <summary>
        /// Gets schedule manager for adding/removing scheduled events
        /// </summary>
        public ScheduleManager Schedule => _baseAlgorithm.Schedule;

        /// <summary>
        /// Security object collection class stores an array of objects representing representing each security/asset
        /// we have a subscription for.
        /// </summary>
        /// <remarks>It is an IDictionary implementation and can be indexed by symbol</remarks>
        public SecurityManager Securities => _baseAlgorithm.Securities;

        /// <summary>
        /// Gets an instance that is to be used to initialize newly created securities.
        /// </summary>
        public ISecurityInitializer SecurityInitializer => _baseAlgorithm.SecurityInitializer;

        /// <summary>
        /// Gets the Trade Builder to generate trades from executions
        /// </summary>
        public ITradeBuilder TradeBuilder => _baseAlgorithm.TradeBuilder;

        /// <summary>
        /// Gets the user settings for the algorithm
        /// </summary>
        public IAlgorithmSettings Settings => _baseAlgorithm.Settings;

        /// <summary>
        /// Gets the option chain provider, used to get the list of option contracts for an underlying symbol
        /// </summary>
        public IOptionChainProvider OptionChainProvider => _baseAlgorithm.OptionChainProvider;

        /// <summary>
        /// Gets the future chain provider, used to get the list of future contracts for an underlying symbol
        /// </summary>
        public IFutureChainProvider FutureChainProvider => _baseAlgorithm.FutureChainProvider;

        /// <summary>
        /// Gets the object store, used for persistence
        /// </summary>
        public ObjectStore ObjectStore => _baseAlgorithm.ObjectStore;

        /// <summary>
        /// Returns the current Slice object
        /// </summary>
        public Slice CurrentSlice => _baseAlgorithm.CurrentSlice;

        /// <summary>
        /// Algorithm start date for backtesting, set by the SetStartDate methods.
        /// </summary>
        public DateTime StartDate => _baseAlgorithm.StartDate;

        /// <summary>
        /// Gets or sets the current status of the algorithm
        /// </summary>
        public AlgorithmStatus Status
        {
            get
            {
                return _baseAlgorithm.Status;
            }

            set
            {
                SetStatus(value);
            }
        }

        /// <summary>
        /// Set the state of a live deployment
        /// </summary>
        /// <param name="status">Live deployment status</param>
        public void SetStatus(AlgorithmStatus status) => _baseAlgorithm.SetStatus(status);

        /// <summary>
        /// Set the available <see cref="TickType"/> supported by each <see cref="SecurityType"/> in <see cref="SecurityManager"/>
        /// </summary>
        /// <param name="availableDataTypes">>The different <see cref="TickType"/> each <see cref="Security"/> supports</param>
        public void SetAvailableDataTypes(Dictionary<SecurityType, List<TickType>> availableDataTypes) => _baseAlgorithm.SetAvailableDataTypes(availableDataTypes);

        /// <summary>
        /// Sets the option chain provider, used to get the list of option contracts for an underlying symbol
        /// </summary>
        /// <param name="optionChainProvider">The option chain provider</param>
        public void SetOptionChainProvider(IOptionChainProvider optionChainProvider) => _baseAlgorithm.SetOptionChainProvider(optionChainProvider);

        /// <summary>
        /// Sets the future chain provider, used to get the list of future contracts for an underlying symbol
        /// </summary>
        /// <param name="futureChainProvider">The future chain provider</param>
        public void SetFutureChainProvider(IFutureChainProvider futureChainProvider) => _baseAlgorithm.SetFutureChainProvider(futureChainProvider);

        /// <summary>
        /// Event fired when an algorithm generates a insight
        /// </summary>
        public event AlgorithmEvent<GeneratedInsightsCollection> InsightsGenerated
        {
            add
            {
                _baseAlgorithm.InsightsGenerated += value;
            }

            remove
            {
                _baseAlgorithm.InsightsGenerated -= value;
            }
        }

        /// <summary>
        /// Gets the time keeper instance
        /// </summary>
        public ITimeKeeper TimeKeeper => _baseAlgorithm.TimeKeeper;

        /// <summary>
        /// Data subscription manager controls the information and subscriptions the algorithms recieves.
        /// Subscription configurations can be added through the Subscription Manager.
        /// </summary>
        public SubscriptionManager SubscriptionManager => _baseAlgorithm.SubscriptionManager;

        /// <summary>
        /// The project id associated with this algorithm if any
        /// </summary>
        public int ProjectId
        {
            set
            {
                _baseAlgorithm.ProjectId = value;
            }
            get
            {
                return _baseAlgorithm.ProjectId;
            }
        }

        /// <summary>
        /// Current date/time in the algorithm's local time zone
        /// </summary>
        public DateTime Time => _baseAlgorithm.Time;

        /// <summary>
        /// Gets the time zone of the algorithm
        /// </summary>
        public DateTimeZone TimeZone => _baseAlgorithm.TimeZone;

        /// <summary>
        /// Security transaction manager class controls the store and processing of orders.
        /// </summary>
        /// <remarks>The orders and their associated events are accessible here. When a new OrderEvent is recieved the algorithm portfolio is updated.</remarks>
        public SecurityTransactionManager Transactions => _baseAlgorithm.Transactions;

        /// <summary>
        /// Gets the collection of universes for the algorithm
        /// </summary>
        public UniverseManager UniverseManager => _baseAlgorithm.UniverseManager;

        /// <summary>
        /// Gets the subscription settings to be used when adding securities via universe selection
        /// </summary>
        public UniverseSettings UniverseSettings => _baseAlgorithm.UniverseSettings;

        /// <summary>
        /// Current date/time in UTC.
        /// </summary>
        public DateTime UtcTime => _baseAlgorithm.UtcTime;

        /// <summary>
        /// Gets the account currency
        /// </summary>
        public string AccountCurrency => _baseAlgorithm.AccountCurrency;

        /// <summary>
        /// Gets the insight manager
        /// </summary>
        public InsightManager Insights => _baseAlgorithm.Insights;

        /// <summary>
        /// Sets the statistics service instance to be used by the algorithm
        /// </summary>
        /// <param name="statisticsService">The statistics service instance</param>
        public void SetStatisticsService(IStatisticsService statisticsService) => _baseAlgorithm.SetStatisticsService(statisticsService);

        /// <summary>
        /// The current statistics for the running algorithm.
        /// </summary>
        public StatisticsResults Statistics => _baseAlgorithm.Statistics;

        /// <summary>
        /// SignalExport - Allows sending export signals to different 3rd party API's. For example, it allows to send signals
        /// to Collective2, CrunchDAO and Numerai API's
        /// </summary>
        public SignalExportManager SignalExport => ((QCAlgorithm)_baseAlgorithm).SignalExport;

        /// <summary>
        /// Set a required SecurityType-symbol and resolution for algorithm
        /// </summary>
        /// <param name="securityType">SecurityType Enum: Equity, Commodity, FOREX or Future</param>
        /// <param name="symbol">Symbol Representation of the MarketType, e.g. AAPL</param>
        /// <param name="resolution">The <see cref="Resolution"/> of market data, Tick, Second, Minute, Hour, or Daily.</param>
        /// <param name="market">The market the requested security belongs to, such as 'usa' or 'fxcm'</param>
        /// <param name="fillForward">If true, returns the last available data even if none in that timeslice.</param>
        /// <param name="leverage">leverage for this security</param>
        /// <param name="extendedMarketHours">Use extended market hours data</param>
        /// <param name="dataMappingMode">The contract mapping mode to use for the security</param>
        /// <param name="dataNormalizationMode">The price scaling mode to use for the security</param>
        public Security AddSecurity(SecurityType securityType, string symbol, Resolution? resolution, string market, bool fillForward, decimal leverage, bool extendedMarketHours,
            DataMappingMode? dataMappingMode = null, DataNormalizationMode? dataNormalizationMode = null)
            => _baseAlgorithm.AddSecurity(securityType, symbol, resolution, market, fillForward, leverage, extendedMarketHours, dataMappingMode, dataNormalizationMode);


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
        public Security AddSecurity(Symbol symbol, Resolution? resolution = null, bool fillForward = true, decimal leverage = Security.NullLeverage, bool extendedMarketHours = false,
            DataMappingMode? dataMappingMode = null, DataNormalizationMode? dataNormalizationMode = null, int contractDepthOffset = 0)
            => _baseAlgorithm.AddSecurity(symbol, resolution, fillForward, leverage, extendedMarketHours, dataMappingMode, dataNormalizationMode, contractDepthOffset);

        /// <summary>
        /// Creates and adds a new single <see cref="Future"/> contract to the algorithm
        /// </summary>
        /// <param name="symbol">The futures contract symbol</param>
        /// <param name="resolution">The <see cref="Resolution"/> of market data, Tick, Second, Minute, Hour, or Daily. Default is <see cref="Resolution.Minute"/></param>
        /// <param name="fillForward">If true, returns the last available data even if none in that timeslice. Default is <value>true</value></param>
        /// <param name="leverage">The requested leverage for this equity. Default is set by <see cref="SecurityInitializer"/></param>
        /// <param name="extendedMarketHours">Use extended market hours data</param>
        /// <returns>The new <see cref="Future"/> security</returns>
        public Future AddFutureContract(Symbol symbol, Resolution? resolution = null, bool fillForward = true, decimal leverage = 0m,
            bool extendedMarketHours = false)
            => _baseAlgorithm.AddFutureContract(symbol, resolution, fillForward, leverage, extendedMarketHours);

        /// <summary>
        /// Creates and adds a new single <see cref="Option"/> contract to the algorithm
        /// </summary>
        /// <param name="symbol">The option contract symbol</param>
        /// <param name="resolution">The <see cref="Resolution"/> of market data, Tick, Second, Minute, Hour, or Daily. Default is <see cref="Resolution.Minute"/></param>
        /// <param name="fillForward">If true, returns the last available data even if none in that timeslice. Default is <value>true</value></param>
        /// <param name="leverage">The requested leverage for this equity. Default is set by <see cref="SecurityInitializer"/></param>
        /// <param name="extendedMarketHours">Use extended market hours data</param>
        /// <returns>The new <see cref="Option"/> security</returns>
        public Option AddOptionContract(Symbol symbol, Resolution? resolution = null, bool fillForward = true, decimal leverage = 0m, bool extendedMarketHours = false)
            => _baseAlgorithm.AddOptionContract(symbol, resolution, fillForward, leverage, extendedMarketHours);

        /// <summary>
        /// Invoked at the end of every time step. This allows the algorithm
        /// to process events before advancing to the next time step.
        /// </summary>
        public void OnEndOfTimeStep()
        {
            _baseAlgorithm.OnEndOfTimeStep();
        }

        /// <summary>
        /// Send debug message
        /// </summary>
        /// <param name="message">String message</param>
        public void Debug(string message) => _baseAlgorithm.Debug(message);

        /// <summary>
        /// Send an error message for the algorithm
        /// </summary>
        /// <param name="message">String message</param>
        public void Error(string message) => _baseAlgorithm.Error(message);

        /// <summary>
        /// Add a Chart object to algorithm collection
        /// </summary>
        /// <param name="chart">Chart object to add to collection.</param>
        public void AddChart(Chart chart) => _baseAlgorithm.AddChart(chart);

        /// <summary>
        /// Get the chart updates since the last request:
        /// </summary>
        /// <param name="clearChartData"></param>
        /// <returns>List of Chart Updates</returns>
        public IEnumerable<Chart> GetChartUpdates(bool clearChartData = false) => _baseAlgorithm.GetChartUpdates(clearChartData);

        /// <summary>
        /// Gets whether or not this algorithm has been locked and fully initialized
        /// </summary>
        public bool GetLocked() => _baseAlgorithm.GetLocked();

        /// <summary>
        /// Gets a read-only dictionary with all current parameters
        /// </summary>
        public IReadOnlyDictionary<string, string> GetParameters() => _baseAlgorithm.GetParameters();

        /// <summary>
        /// Gets the parameter with the specified name. If a parameter with the specified name does not exist,
        /// the given default value is returned if any, else null
        /// </summary>
        /// <param name="name">The name of the parameter to get</param>
        /// <param name="defaultValue">The default value to return</param>
        /// <returns>The value of the specified parameter, or defaultValue if not found or null if there's no default value</returns>
        public string GetParameter(string name, string defaultValue = null) => _baseAlgorithm.GetParameter(name, defaultValue);

        /// <summary>
        /// Gets the parameter with the specified name parsed as an integer. If a parameter with the specified name does not exist,
        /// or the conversion is not possible, the given default value is returned
        /// </summary>
        /// <param name="name">The name of the parameter to get</param>
        /// <param name="defaultValue">The default value to return</param>
        /// <returns>The value of the specified parameter, or defaultValue if not found or null if there's no default value</returns>
        public int GetParameter(string name, int defaultValue) => _baseAlgorithm.GetParameter(name, defaultValue);

        /// <summary>
        /// Gets the parameter with the specified name parsed as a double. If a parameter with the specified name does not exist,
        /// or the conversion is not possible, the given default value is returned
        /// </summary>
        /// <param name="name">The name of the parameter to get</param>
        /// <param name="defaultValue">The default value to return</param>
        /// <returns>The value of the specified parameter, or defaultValue if not found or null if there's no default value</returns>
        public double GetParameter(string name, double defaultValue) => _baseAlgorithm.GetParameter(name, defaultValue);

        /// <summary>
        /// Gets the parameter with the specified name parsed as a decimal. If a parameter with the specified name does not exist,
        /// or the conversion is not possible, the given default value is returned
        /// </summary>
        /// <param name="name">The name of the parameter to get</param>
        /// <param name="defaultValue">The default value to return</param>
        /// <returns>The value of the specified parameter, or defaultValue if not found or null if there's no default value</returns>
        public decimal GetParameter(string name, decimal defaultValue) => _baseAlgorithm.GetParameter(name, defaultValue);

        /// <summary>
        /// Initialise the Algorithm and Prepare Required Data:
        /// </summary>
        public void Initialize()
        {
            InvokeMethod(nameof(Initialize));
        }

        /// <summary>
        /// Liquidate your portfolio holdings
        /// </summary>
        /// <param name="symbol">Specific asset to liquidate, defaults to all</param>
        /// <param name="asynchronous">Flag to indicate if the symbols should be liquidated asynchronously</param>
        /// <param name="tag">Custom tag to know who is calling this</param>
        /// <param name="orderProperties">Order properties to use</param>
        public List<OrderTicket> Liquidate(Symbol symbol = null, bool asynchronous = false, string tag = "Liquidated", IOrderProperties orderProperties = null) => _baseAlgorithm.Liquidate(symbol, asynchronous, tag, orderProperties);

        /// <summary>
        /// Save entry to the Log
        /// </summary>
        /// <param name="message">String message</param>
        public void Log(string message) => _baseAlgorithm.Log(message);

        /// <summary>
        /// Brokerage disconnected event handler. This method is called when the brokerage connection is lost.
        /// </summary>
        public void OnBrokerageDisconnect()
        {
            _onBrokerageDisconnect();
        }

        /// <summary>
        /// Brokerage message event handler. This method is called for all types of brokerage messages.
        /// </summary>
        public void OnBrokerageMessage(BrokerageMessageEvent messageEvent)
        {
            _onBrokerageMessage(messageEvent);
        }

        /// <summary>
        /// Brokerage reconnected event handler. This method is called when the brokerage connection is restored after a disconnection.
        /// </summary>
        public void OnBrokerageReconnect()
        {
            _onBrokerageReconnect();
        }

        /// <summary>
        /// v3.0 Handler for all data types
        /// </summary>
        /// <param name="slice">The current slice of data</param>
        public void OnData(Slice slice)
        {
            if (_onData != null)
            {
                using (Py.GIL())
                {
                    _onData(slice);
                }
            }
        }

        /// <summary>
        /// Used to send data updates to algorithm framework models
        /// </summary>
        /// <param name="slice">The current data slice</param>
        public void OnFrameworkData(Slice slice)
        {
            _baseAlgorithm.OnFrameworkData(slice);
        }

        /// <summary>
        /// Event handler to be called when there's been a split event
        /// </summary>
        /// <param name="splits">The current time slice splits</param>
        public void OnSplits(Splits splits)
        {
            _onSplits(splits);
        }

        /// <summary>
        /// Event handler to be called when there's been a dividend event
        /// </summary>
        /// <param name="dividends">The current time slice dividends</param>
        public void OnDividends(Dividends dividends)
        {
            _onDividends(dividends);
        }

        /// <summary>
        /// Event handler to be called when there's been a delistings event
        /// </summary>
        /// <param name="delistings">The current time slice delistings</param>
        public void OnDelistings(Delistings delistings)
        {
            _onDelistings(delistings);
        }

        /// <summary>
        /// Event handler to be called when there's been a symbol changed event
        /// </summary>
        /// <param name="symbolsChanged">The current time slice symbol changed events</param>
        public void OnSymbolChangedEvents(SymbolChangedEvents symbolsChanged)
        {
            _onSymbolChangedEvents(symbolsChanged);
        }

        /// <summary>
        /// Call this event at the end of the algorithm running.
        /// </summary>
        public void OnEndOfAlgorithm()
        {
            InvokeMethod(nameof(OnEndOfAlgorithm));
        }

        /// <summary>
        /// End of a trading day event handler. This method is called at the end of the algorithm day (or multiple times if trading multiple assets).
        /// </summary>
        /// <remarks>Method is called 10 minutes before closing to allow user to close out position.</remarks>
        /// <remarks>Deprecated because different assets have different market close times,
        /// and because Python does not support two methods with the same name</remarks>
        [Obsolete("This method is deprecated. Please use this overload: OnEndOfDay(Symbol symbol)")]
        [StubsIgnore]
        public void OnEndOfDay()
        {
            try
            {
                _onEndOfDay();
            }
            // If OnEndOfDay is not defined in the script, but OnEndOfDay(Symbol) is, a python exception occurs
            // Only throws if there is an error in its implementation body
            catch (PythonException exception)
            {
                if (!exception.Message.Contains("OnEndOfDay() missing 1 required positional argument"))
                {
                    _baseAlgorithm.SetRunTimeError(exception);
                }
            }
        }

        /// <summary>
        /// End of a trading day event handler. This method is called at the end of the algorithm day (or multiple times if trading multiple assets).
        /// </summary>
        /// <remarks>
        /// This method is left for backwards compatibility and is invoked via <see cref="OnEndOfDay(Symbol)"/>, if that method is
        /// override then this method will not be called without a called to base.OnEndOfDay(string)
        /// </remarks>
        /// <param name="symbol">Asset symbol for this end of day event. Forex and equities have different closing hours.</param>
        [StubsAvoidImplicits]
        public void OnEndOfDay(Symbol symbol)
        {
            try
            {
                _onEndOfDay(symbol);
            }
            // If OnEndOfDay(Symbol) is not defined in the script, but OnEndOfDay is, a python exception occurs
            // Only throws if there is an error in its implementation body
            catch (PythonException exception)
            {
                if (!exception.Message.Contains("OnEndOfDay() takes 1 positional argument but 2 were given"))
                {
                    _baseAlgorithm.SetRunTimeError(exception);
                }
            }
        }

        /// <summary>
        /// Margin call event handler. This method is called right before the margin call orders are placed in the market.
        /// </summary>
        /// <param name="requests">The orders to be executed to bring this algorithm within margin limits</param>
        public void OnMarginCall(List<SubmitOrderRequest> requests)
        {
            using (Py.GIL())
            {
                var result = InvokeMethod(nameof(OnMarginCall), requests);

                if (_onMarginCall != null)
                {
                    // If the method does not return or returns a non-iterable PyObject, throw an exception
                    if (result == null || !result.IsIterable())
                    {
                        throw new Exception("OnMarginCall must return a non-empty list of SubmitOrderRequest");
                    }

                    requests.Clear();

                    using var iterator = result.GetIterator();
                    foreach (PyObject pyRequest in iterator)
                    {
                        SubmitOrderRequest request;
                        if (TryConvert(pyRequest, out request))
                        {
                            requests.Add(request);
                        }
                    }

                    // If the PyObject is an empty list or its items are not SubmitOrderRequest objects, throw an exception
                    if (requests.Count == 0)
                    {
                        throw new Exception("OnMarginCall must return a non-empty list of SubmitOrderRequest");
                    }
                }
            }
        }

        /// <summary>
        /// Margin call warning event handler. This method is called when Portfolio.MarginRemaining is under 5% of your Portfolio.TotalPortfolioValue
        /// </summary>
        public void OnMarginCallWarning()
        {
            _onMarginCallWarning();
        }

        /// <summary>
        /// EXPERTS ONLY:: [-!-Async Code-!-]
        /// New order event handler: on order status changes (filled, partially filled, cancelled etc).
        /// </summary>
        /// <param name="newEvent">Event information</param>
        public void OnOrderEvent(OrderEvent newEvent)
        {
            _onOrderEvent(newEvent);
        }

        /// <summary>
        /// Generic untyped command call handler
        /// </summary>
        /// <param name="data">The associated data</param>
        /// <returns>True if success, false otherwise. Returning null will disable command feedback</returns>
        public bool? OnCommand(dynamic data)
        {
            return _onCommand(data);
        }

        /// <summary>
        /// Will submit an order request to the algorithm
        /// </summary>
        /// <param name="request">The request to submit</param>
        /// <remarks>Will run order prechecks, which include making sure the algorithm is not warming up, security is added and has data among others</remarks>
        /// <returns>The order ticket</returns>
        public OrderTicket SubmitOrderRequest(SubmitOrderRequest request)
        {
            return _baseAlgorithm.SubmitOrderRequest(request);
        }

        /// <summary>
        /// Option assignment event handler. On an option assignment event for short legs the resulting information is passed to this method.
        /// </summary>
        /// <param name="assignmentEvent">Option exercise event details containing details of the assignment</param>
        /// <remarks>This method can be called asynchronously and so should only be used by seasoned C# experts. Ensure you use proper locks on thread-unsafe objects</remarks>
        public void OnAssignmentOrderEvent(OrderEvent assignmentEvent)
        {
            _onAssignmentOrderEvent(assignmentEvent);
        }

        /// <summary>
        /// Event fired each time the we add/remove securities from the data feed
        /// </summary>
        /// <param name="changes">Security additions/removals for this time step</param>
        public void OnSecuritiesChanged(SecurityChanges changes)
        {
            _onSecuritiesChanged(changes);
        }

        /// <summary>
        /// Used to send security changes to algorithm framework models
        /// </summary>
        /// <param name="changes">Security additions/removals for this time step</param>
        public void OnFrameworkSecuritiesChanged(SecurityChanges changes)
        {
            _onFrameworkSecuritiesChanged(changes);
        }

        /// <summary>
        /// Called by setup handlers after Initialize and allows the algorithm a chance to organize
        /// the data gather in the Initialize method
        /// </summary>
        public void PostInitialize()
        {
            _baseAlgorithm.PostInitialize();
        }

        /// <summary>
        /// Called when the algorithm has completed initialization and warm up.
        /// </summary>
        public void OnWarmupFinished()
        {
            InvokeMethod(nameof(OnWarmupFinished));
        }

        /// <summary>
        /// Removes the security with the specified symbol. This will cancel all
        /// open orders and then liquidate any existing holdings
        /// </summary>
        /// <param name="symbol">The symbol of the security to be removed</param>
        public bool RemoveSecurity(Symbol symbol) => _baseAlgorithm.RemoveSecurity(symbol);

        /// <summary>
        /// Set the algorithm Id for this backtest or live run. This can be used to identify the order and equity records.
        /// </summary>
        /// <param name="algorithmId">unique 32 character identifier for backtest or live server</param>
        public void SetAlgorithmId(string algorithmId) => _baseAlgorithm.SetAlgorithmId(algorithmId);

        /// <summary>
        /// Sets the implementation used to handle messages from the brokerage.
        /// The default implementation will forward messages to debug or error
        /// and when a <see cref="BrokerageMessageType.Error"/> occurs, the algorithm
        /// is stopped.
        /// </summary>
        /// <param name="handler">The message handler to use</param>
        public void SetBrokerageMessageHandler(IBrokerageMessageHandler handler) => _baseAlgorithm.SetBrokerageMessageHandler(handler);

        /// <summary>
        /// Sets the brokerage model used to resolve transaction models, settlement models,
        /// and brokerage specified ordering behaviors.
        /// </summary>
        /// <param name="brokerageModel">The brokerage model used to emulate the real
        /// brokerage</param>
        public void SetBrokerageModel(IBrokerageModel brokerageModel) => _baseAlgorithm.SetBrokerageModel(brokerageModel);

        /// <summary>
        /// Sets the account currency cash symbol this algorithm is to manage, as well
        /// as the starting cash in this currency if given
        /// </summary>
        /// <remarks>Has to be called during <see cref="Initialize"/> before
        /// calling <see cref="SetCash(decimal)"/> or adding any <see cref="Security"/></remarks>
        /// <param name="accountCurrency">The account currency cash symbol to set</param>
        /// <param name="startingCash">The account currency starting cash to set</param>
        public void SetAccountCurrency(string accountCurrency, decimal? startingCash = null) => _baseAlgorithm.SetAccountCurrency(accountCurrency, startingCash);

        /// <summary>
        /// Set the starting capital for the strategy
        /// </summary>
        /// <param name="startingCash">decimal starting capital, default $100,000</param>
        public void SetCash(decimal startingCash) => _baseAlgorithm.SetCash(startingCash);

        /// <summary>
        /// Set the cash for the specified symbol
        /// </summary>
        /// <param name="symbol">The cash symbol to set</param>
        /// <param name="startingCash">Decimal cash value of portfolio</param>
        /// <param name="conversionRate">The current conversion rate for the</param>
        public void SetCash(string symbol, decimal startingCash, decimal conversionRate = 0) => _baseAlgorithm.SetCash(symbol, startingCash, conversionRate);

        /// <summary>
        /// Set the DateTime Frontier: This is the master time and is
        /// </summary>
        /// <param name="time"></param>
        public void SetDateTime(DateTime time) => _baseAlgorithm.SetDateTime(time);

        /// <summary>
        /// Set the start date for the backtest
        /// </summary>
        /// <param name="start">Datetime Start date for backtest</param>
        /// <remarks>Must be less than end date and within data available</remarks>
        public void SetStartDate(DateTime start) => _baseAlgorithm.SetStartDate(start);

        /// <summary>
        /// Set the end date for a backtest.
        /// </summary>
        /// <param name="end">Datetime value for end date</param>
        /// <remarks>Must be greater than the start date</remarks>
        public void SetEndDate(DateTime end) => _baseAlgorithm.SetEndDate(end);

        /// <summary>
        /// Get the last known price using the history provider.
        /// Useful for seeding securities with the correct price
        /// </summary>
        /// <param name="security"><see cref="Security"/> object for which to retrieve historical data</param>
        /// <returns>A single <see cref="BaseData"/> object with the last known price</returns>
        public BaseData GetLastKnownPrice(Security security) => _baseAlgorithm.GetLastKnownPrice(security);

        /// <summary>
        /// Set the runtime error
        /// </summary>
        /// <param name="exception">Represents error that occur during execution</param>
        public void SetRunTimeError(Exception exception) => _baseAlgorithm.SetRunTimeError(exception);

        /// <summary>
        /// Sets <see cref="IsWarmingUp"/> to false to indicate this algorithm has finished its warm up
        /// </summary>
        public void SetFinishedWarmingUp()
        {
            _baseAlgorithm.SetFinishedWarmingUp();

            // notify the algorithm
            OnWarmupFinished();
        }

        /// <summary>
        /// Set the historical data provider
        /// </summary>
        /// <param name="historyProvider">Historical data provider</param>
        public void SetHistoryProvider(IHistoryProvider historyProvider) => _baseAlgorithm.SetHistoryProvider(historyProvider);

        /// <summary>
        /// Set live mode state of the algorithm run: Public setter for the algorithm property LiveMode.
        /// </summary>
        /// <param name="live">Bool live mode flag</param>
        public void SetLiveMode(bool live) => _baseAlgorithm.SetLiveMode(live);

        /// <summary>
        /// Sets the algorithm running mode
        /// </summary>
        /// <param name="algorithmMode">Algorithm mode</param>
        public void SetAlgorithmMode(AlgorithmMode algorithmMode) => _baseAlgorithm.SetAlgorithmMode(algorithmMode);

        /// <summary>
        /// Sets the algorithm deployment target
        /// </summary>
        /// <param name="deploymentTarget">Deployment target</param>
        public void SetDeploymentTarget(DeploymentTarget deploymentTarget) => _baseAlgorithm.SetDeploymentTarget(deploymentTarget);

        /// <summary>
        /// Set the algorithm as initialized and locked. No more cash or security changes.
        /// </summary>
        public void SetLocked() => _baseAlgorithm.SetLocked();

        /// <summary>
        /// Set the maximum number of orders the algorithm is allowed to process.
        /// </summary>
        /// <param name="max">Maximum order count int</param>
        public void SetMaximumOrders(int max) => _baseAlgorithm.SetMaximumOrders(max);

        /// <summary>
        /// Sets the parameters from the dictionary
        /// </summary>
        /// <param name="parameters">Dictionary containing the parameter names to values</param>
        public void SetParameters(Dictionary<string, string> parameters) => _baseAlgorithm.SetParameters(parameters);

        /// <summary>
        /// Tries to convert a PyObject into a C# object
        /// </summary>
        /// <typeparam name="T">Type of the C# object</typeparam>
        /// <param name="pyObject">PyObject to be converted</param>
        /// <param name="result">C# object that of type T</param>
        /// <returns>True if successful conversion</returns>
        private bool TryConvert<T>(PyObject pyObject, out T result)
        {
            result = default(T);
            var type = (Type)pyObject.GetPythonType().AsManagedObject(typeof(Type));

            if (type == typeof(T))
            {
                result = (T)pyObject.AsManagedObject(typeof(T));
            }

            return type == typeof(T);
        }

        /// <summary>
        /// Returns a <see cref = "string"/> that represents the current <see cref = "AlgorithmPythonWrapper"/> object.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (Instance == null)
            {
                return base.ToString();
            }
            using (Py.GIL())
            {
                return Instance.Repr();
            }
        }

        /// <summary>
        /// Sets the current slice
        /// </summary>
        /// <param name="slice">The Slice object</param>
        public void SetCurrentSlice(Slice slice)
        {
            _baseAlgorithm.SetCurrentSlice(slice);
        }

        /// <summary>
        /// Provide the API for the algorithm.
        /// </summary>
        /// <param name="api">Initiated API</param>
        public void SetApi(IApi api) => _baseAlgorithm.SetApi(api);

        /// <summary>
        /// Sets the object store
        /// </summary>
        /// <param name="objectStore">The object store</param>
        public void SetObjectStore(IObjectStore objectStore) => _baseAlgorithm.SetObjectStore(objectStore);

        /// <summary>
        /// Determines if the Symbol is shortable at the brokerage
        /// </summary>
        /// <param name="symbol">Symbol to check if shortable</param>
        /// <param name="shortQuantity">Order's quantity to check if it is currently shortable, taking into account current holdings and open orders</param>
        /// <param name="updateOrderId">Optionally the id of the order being updated. When updating an order
        /// we want to ignore it's submitted short quantity and use the new provided quantity to determine if we
        /// can perform the update</param>
        /// <returns>True if the symbol can be shorted by the requested quantity</returns>
        public bool Shortable(Symbol symbol, decimal shortQuantity, int? updateOrderId = null)
        {
            return _baseAlgorithm.Shortable(symbol, shortQuantity, updateOrderId);
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
            return _baseAlgorithm.ShortableQuantity(symbol);
        }

        /// <summary>
        /// Converts the string 'ticker' symbol into a full <see cref="Symbol"/> object
        /// This requires that the string 'ticker' has been added to the algorithm
        /// </summary>
        /// <param name="ticker">The ticker symbol. This should be the ticker symbol
        /// as it was added to the algorithm</param>
        /// <returns>The symbol object mapped to the specified ticker</returns>
        public Symbol Symbol(string ticker) => _baseAlgorithm.Symbol(ticker);

        /// <summary>
        /// For the given symbol will resolve the ticker it used at the current algorithm date
        /// </summary>
        /// <param name="symbol">The symbol to get the ticker for</param>
        /// <returns>The mapped ticker for a symbol</returns>
        public string Ticker(Symbol symbol) => _baseAlgorithm.Ticker(symbol);

        /// <summary>
        /// Sets name to the currently running backtest
        /// </summary>
        /// <param name="name">The name for the backtest</param>
        public void SetName(string name)
        {
            _baseAlgorithm.SetName(name);
        }

        /// <summary>
        /// Adds a tag to the algorithm
        /// </summary>
        /// <param name="tag">The tag to add</param>
        public void AddTag(string tag)
        {
            _baseAlgorithm.AddTag(tag);
        }

        /// <summary>
        /// Sets the tags for the algorithm
        /// </summary>
        /// <param name="tags">The tags</param>
        public void SetTags(HashSet<string> tags)
        {
            _baseAlgorithm.SetTags(tags);
        }

        /// <summary>
        /// Run a callback command instance
        /// </summary>
        /// <param name="command">The callback command instance</param>
        /// <returns>The command result</returns>
        public CommandResultPacket RunCommand(CallbackCommand command) => _baseAlgorithm.RunCommand(command);

        /// <summary>
        /// Dispose of this instance
        /// </summary>
        public override void Dispose()
        {
            using var _ = Py.GIL();
            _onBrokerageDisconnect?.Dispose();
            _onBrokerageMessage?.Dispose();
            _onBrokerageReconnect?.Dispose();
            _onSplits?.Dispose();
            _onDividends?.Dispose();
            _onDelistings?.Dispose();
            _onSymbolChangedEvents?.Dispose();
            _onEndOfDay?.Dispose();
            _onMarginCallWarning?.Dispose();
            _onOrderEvent?.Dispose();
            _onCommand?.Dispose();
            _onAssignmentOrderEvent?.Dispose();
            _onSecuritiesChanged?.Dispose();
            _onFrameworkSecuritiesChanged?.Dispose();

            _onData?.Dispose();
            _onMarginCall?.Dispose();
            base.Dispose();
        }
    }
}
