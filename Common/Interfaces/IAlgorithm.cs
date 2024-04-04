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
using NodaTime;
using QuantConnect.Data;
using QuantConnect.Orders;
using QuantConnect.Storage;
using QuantConnect.Benchmarks;
using QuantConnect.Brokerages;
using QuantConnect.Scheduling;
using QuantConnect.Securities;
using QuantConnect.Statistics;
using QuantConnect.Data.Market;
using QuantConnect.Notifications;
using System.Collections.Generic;
using System.Collections.Concurrent;
using QuantConnect.Securities.Future;
using QuantConnect.Securities.Option;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Alphas.Analysis;

namespace QuantConnect.Interfaces
{
    /// <summary>
    /// Defines an event fired from within an algorithm instance.
    /// </summary>
    /// <typeparam name="T">The event type</typeparam>
    /// <param name="algorithm">The algorithm that fired the event</param>
    /// <param name="eventData">The event data</param>
    public delegate void AlgorithmEvent<in T>(IAlgorithm algorithm, T eventData);

    /// <summary>
    /// Interface for QuantConnect algorithm implementations. All algorithms must implement these
    /// basic members to allow interaction with the Lean Backtesting Engine.
    /// </summary>
    public interface IAlgorithm : ISecurityInitializerProvider, IAccountCurrencyProvider
    {
        /// <summary>
        /// Event fired when an algorithm generates a insight
        /// </summary>
        event AlgorithmEvent<GeneratedInsightsCollection> InsightsGenerated;

        /// <summary>
        /// Gets the time keeper instance
        /// </summary>
        ITimeKeeper TimeKeeper
        {
            get;
        }

        /// <summary>
        /// Data subscription manager controls the information and subscriptions the algorithms recieves.
        /// Subscription configurations can be added through the Subscription Manager.
        /// </summary>
        SubscriptionManager SubscriptionManager
        {
            get;
        }

        /// <summary>
        /// The project id associated with this algorithm if any
        /// </summary>
        int ProjectId
        {
            get;
            set;
        }

        /// <summary>
        /// Security object collection class stores an array of objects representing representing each security/asset
        /// we have a subscription for.
        /// </summary>
        /// <remarks>It is an IDictionary implementation and can be indexed by symbol</remarks>
        SecurityManager Securities
        {
            get;
        }

        /// <summary>
        /// Gets the collection of universes for the algorithm
        /// </summary>
        UniverseManager UniverseManager
        {
            get;
        }

        /// <summary>
        /// Security portfolio management class provides wrapper and helper methods for the Security.Holdings class such as
        /// IsLong, IsShort, TotalProfit
        /// </summary>
        /// <remarks>Portfolio is a wrapper and helper class encapsulating the Securities[].Holdings objects</remarks>
        SecurityPortfolioManager Portfolio
        {
            get;
        }

        /// <summary>
        /// Security transaction manager class controls the store and processing of orders.
        /// </summary>
        /// <remarks>The orders and their associated events are accessible here. When a new OrderEvent is recieved the algorithm portfolio is updated.</remarks>
        SecurityTransactionManager Transactions
        {
            get;
        }

        /// <summary>
        /// Gets the brokerage model used to emulate a real brokerage
        /// </summary>
        IBrokerageModel BrokerageModel
        {
            get;
        }

        /// <summary>
        /// Gets the brokerage name.
        /// </summary>
        BrokerageName BrokerageName
        {
            get;
        }

        /// <summary>
        /// Gets the risk free interest rate model used to get the interest rates
        /// </summary>
        IRiskFreeInterestRateModel RiskFreeInterestRateModel
        {
            get;
        }

        /// <summary>
        /// Gets the brokerage message handler used to decide what to do
        /// with each message sent from the brokerage
        /// </summary>
        IBrokerageMessageHandler BrokerageMessageHandler
        {
            get;
            set;
        }

        /// <summary>
        /// Notification manager for storing and processing live event messages
        /// </summary>
        NotificationManager Notify
        {
            get;
        }

        /// <summary>
        /// Gets schedule manager for adding/removing scheduled events
        /// </summary>
        ScheduleManager Schedule
        {
            get;
        }

        /// <summary>
        /// Gets or sets the history provider for the algorithm
        /// </summary>
        IHistoryProvider HistoryProvider
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the current status of the algorithm
        /// </summary>
        AlgorithmStatus Status
        {
            get;
            set;
        }

        /// <summary>
        /// Gets whether or not this algorithm is still warming up
        /// </summary>
        bool IsWarmingUp
        {
            get;
        }

        /// <summary>
        /// Public name for the algorithm.
        /// </summary>
        string Name
        {
            get;
            set;
        }

        /// <summary>
        /// A list of tags associated with the algorithm or the backtest, useful for categorization
        /// </summary>
        HashSet<string> Tags
        {
            get;
            set;
        }

        /// <summary>
        /// Event fired algorithm's name is changed
        /// </summary>
        event AlgorithmEvent<string> NameUpdated;

        /// <summary>
        /// Event fired when the tag collection is updated
        /// </summary>
        event AlgorithmEvent<HashSet<string>> TagsUpdated;

        /// <summary>
        /// Current date/time in the algorithm's local time zone
        /// </summary>
        DateTime Time
        {
            get;
        }

        /// <summary>
        /// Gets the time zone of the algorithm
        /// </summary>
        DateTimeZone TimeZone
        {
            get;
        }

        /// <summary>
        /// Current date/time in UTC.
        /// </summary>
        DateTime UtcTime
        {
            get;
        }

        /// <summary>
        /// Algorithm start date for backtesting, set by the SetStartDate methods.
        /// </summary>
        DateTime StartDate
        {
            get;
        }

        /// <summary>
        /// Get Requested Backtest End Date
        /// </summary>
        DateTime EndDate
        {
            get;
        }

        /// <summary>
        /// AlgorithmId for the backtest
        /// </summary>
        string AlgorithmId
        {
            get;
        }

        /// <summary>
        /// Algorithm is running on a live server.
        /// </summary>
        bool LiveMode
        {
            get;
        }

        /// <summary>
        /// Algorithm running mode.
        /// </summary>
        AlgorithmMode AlgorithmMode
        {
            get;
        }

        /// <summary>
        /// Deployment target, either local or cloud.
        /// </summary>
        DeploymentTarget DeploymentTarget
        {
            get;
        }

        /// <summary>
        /// Gets the subscription settings to be used when adding securities via universe selection
        /// </summary>
        UniverseSettings UniverseSettings
        {
            get;
        }

        /// <summary>
        /// Debug messages from the strategy:
        /// </summary>
        ConcurrentQueue<string> DebugMessages
        {
            get;
        }

        /// <summary>
        /// Error messages from the strategy:
        /// </summary>
        ConcurrentQueue<string> ErrorMessages
        {
            get;
        }

        /// <summary>
        /// Log messages from the strategy:
        /// </summary>
        ConcurrentQueue<string> LogMessages
        {
            get;
        }

        /// <summary>
        /// Gets the run time error from the algorithm, or null if none was encountered.
        /// </summary>
        Exception RunTimeError
        {
            get;
            set;
        }

        /// <summary>
        /// Customizable dynamic statistics displayed during live trading:
        /// </summary>
        ConcurrentDictionary<string, string> RuntimeStatistics
        {
            get;
        }

        /// <summary>
        /// The current algorithm statistics for the running algorithm.
        /// </summary>
        StatisticsResults Statistics
        {
            get;
        }

        /// <summary>
        /// Gets the function used to define the benchmark. This function will return
        /// the value of the benchmark at a requested date/time
        /// </summary>
        IBenchmark Benchmark
        {
            get;
        }

        /// <summary>
        /// Gets the Trade Builder to generate trades from executions
        /// </summary>
        ITradeBuilder TradeBuilder
        {
            get;
        }

        /// <summary>
        /// Gets the user settings for the algorithm
        /// </summary>
        IAlgorithmSettings Settings
        {
            get;
        }

        /// <summary>
        /// Gets the option chain provider, used to get the list of option contracts for an underlying symbol
        /// </summary>
        IOptionChainProvider OptionChainProvider
        {
            get;
        }

        /// <summary>
        /// Gets the future chain provider, used to get the list of future contracts for an underlying symbol
        /// </summary>
        IFutureChainProvider FutureChainProvider
        {
            get;
        }

        /// <summary>
        /// Gets the insight manager
        /// </summary>
        InsightManager Insights
        {
            get;
        }

        /// <summary>
        /// Gets the object store, used for persistence
        /// </summary>
        ObjectStore ObjectStore { get; }

        /// <summary>
        /// Returns the current Slice object
        /// </summary>
        Slice CurrentSlice { get; }

        /// <summary>
        /// Initialise the Algorithm and Prepare Required Data:
        /// </summary>
        void Initialize();

        /// <summary>
        /// Called by setup handlers after Initialize and allows the algorithm a chance to organize
        /// the data gather in the Initialize method
        /// </summary>
        void PostInitialize();

        /// <summary>
        /// Called when the algorithm has completed initialization and warm up.
        /// </summary>
        void OnWarmupFinished();

        /// <summary>
        /// Gets a read-only dictionary with all current parameters
        /// </summary>
        IReadOnlyDictionary<string, string> GetParameters();

        /// <summary>
        /// Gets the parameter with the specified name. If a parameter with the specified name does not exist,
        /// the given default value is returned if any, else null
        /// </summary>
        /// <param name="name">The name of the parameter to get</param>
        /// <param name="defaultValue">The default value to return</param>
        /// <returns>The value of the specified parameter, or defaultValue if not found or null if there's no default value</returns>
        string GetParameter(string name, string defaultValue = null);

        /// <summary>
        /// Gets the parameter with the specified name parsed as an integer. If a parameter with the specified name does not exist,
        /// or the conversion is not possible, the given default value is returned
        /// </summary>
        /// <param name="name">The name of the parameter to get</param>
        /// <param name="defaultValue">The default value to return</param>
        /// <returns>The value of the specified parameter, or defaultValue if not found or null if there's no default value</returns>
        int GetParameter(string name, int defaultValue);

        /// <summary>
        /// Gets the parameter with the specified name parsed as a double. If a parameter with the specified name does not exist,
        /// or the conversion is not possible, the given default value is returned
        /// </summary>
        /// <param name="name">The name of the parameter to get</param>
        /// <param name="defaultValue">The default value to return</param>
        /// <returns>The value of the specified parameter, or defaultValue if not found or null if there's no default value</returns>
        double GetParameter(string name, double defaultValue);

        /// <summary>
        /// Gets the parameter with the specified name parsed as a decimal. If a parameter with the specified name does not exist,
        /// or the conversion is not possible, the given default value is returned
        /// </summary>
        /// <param name="name">The name of the parameter to get</param>
        /// <param name="defaultValue">The default value to return</param>
        /// <returns>The value of the specified parameter, or defaultValue if not found or null if there's no default value</returns>
        decimal GetParameter(string name, decimal defaultValue);

        /// <summary>
        /// Sets the parameters from the dictionary
        /// </summary>
        /// <param name="parameters">Dictionary containing the parameter names to values</param>
        void SetParameters(Dictionary<string, string> parameters);

        /// <summary>
        /// Determines if the Symbol is shortable at the brokerage
        /// </summary>
        /// <param name="symbol">Symbol to check if shortable</param>
        /// <param name="shortQuantity">Order's quantity to check if it is currently shortable, taking into account current holdings and open orders</param>
        /// <param name="updateOrderId">Optionally the id of the order being updated. When updating an order
        /// we want to ignore it's submitted short quantity and use the new provided quantity to determine if we
        /// can perform the update</param>
        /// <returns>True if the symbol can be shorted by the requested quantity</returns>
        bool Shortable(Symbol symbol, decimal shortQuantity, int? updateOrderId = null);

        /// <summary>
        /// Gets the quantity shortable for the given asset
        /// </summary>
        /// <returns>
        /// Quantity shortable for the given asset. Zero if not
        /// shortable, or a number greater than zero if shortable.
        /// </returns>
        long ShortableQuantity(Symbol symbol);

        /// <summary>
        /// Sets the brokerage model used to resolve transaction models, settlement models,
        /// and brokerage specified ordering behaviors.
        /// </summary>
        /// <param name="brokerageModel">The brokerage model used to emulate the real
        /// brokerage</param>
        void SetBrokerageModel(IBrokerageModel brokerageModel);

        /// <summary>
        /// v3.0 Handler for all data types
        /// </summary>
        /// <param name="slice">The current slice of data</param>
        void OnData(Slice slice);

        /// <summary>
        /// Used to send data updates to algorithm framework models
        /// </summary>
        /// <param name="slice">The current data slice</param>
        void OnFrameworkData(Slice slice);

        /// <summary>
        /// Event handler to be called when there's been a split event
        /// </summary>
        /// <param name="splits">The current time slice splits</param>
        void OnSplits(Splits splits);

        /// <summary>
        /// Event handler to be called when there's been a dividend event
        /// </summary>
        /// <param name="dividends">The current time slice dividends</param>
        void OnDividends(Dividends dividends);

        /// <summary>
        /// Event handler to be called when there's been a delistings event
        /// </summary>
        /// <param name="delistings">The current time slice delistings</param>
        void OnDelistings(Delistings delistings);

        /// <summary>
        /// Event handler to be called when there's been a symbol changed event
        /// </summary>
        /// <param name="symbolsChanged">The current time slice symbol changed events</param>
        void OnSymbolChangedEvents(SymbolChangedEvents symbolsChanged);

        /// <summary>
        /// Event fired each time that we add/remove securities from the data feed
        /// </summary>
        /// <param name="changes">Security additions/removals for this time step</param>
        void OnSecuritiesChanged(SecurityChanges changes);

        /// <summary>
        /// Used to send security changes to algorithm framework models
        /// </summary>
        /// <param name="changes">Security additions/removals for this time step</param>
        void OnFrameworkSecuritiesChanged(SecurityChanges changes);

        /// <summary>
        /// Invoked at the end of every time step. This allows the algorithm
        /// to process events before advancing to the next time step.
        /// </summary>
        void OnEndOfTimeStep();

        /// <summary>
        /// Send debug message
        /// </summary>
        /// <param name="message"></param>
        void Debug(string message);

        /// <summary>
        /// Save entry to the Log
        /// </summary>
        /// <param name="message">String message</param>
        void Log(string message);

        /// <summary>
        /// Send an error message for the algorithm
        /// </summary>
        /// <param name="message">String message</param>
        void Error(string message);

        /// <summary>
        /// Margin call event handler. This method is called right before the margin call orders are placed in the market.
        /// </summary>
        /// <param name="requests">The orders to be executed to bring this algorithm within margin limits</param>
        void OnMarginCall(List<SubmitOrderRequest> requests);

        /// <summary>
        /// Margin call warning event handler. This method is called when Portfolio.MarginRemaining is under 5% of your Portfolio.TotalPortfolioValue
        /// </summary>
        void OnMarginCallWarning();

        /// <summary>
        /// Call this method at the end of each day of data.
        /// </summary>
        /// <remarks>Deprecated because different assets have different market close times,
        /// and because Python does not support two methods with the same name</remarks>
        [Obsolete("This method is deprecated. Please use this overload: OnEndOfDay(Symbol symbol)")]
        void OnEndOfDay();

        /// <summary>
        /// Call this method at the end of each day of data.
        /// </summary>
        void OnEndOfDay(Symbol symbol);

        /// <summary>
        /// Call this event at the end of the algorithm running.
        /// </summary>
        void OnEndOfAlgorithm();

        /// <summary>
        /// EXPERTS ONLY:: [-!-Async Code-!-]
        /// New order event handler: on order status changes (filled, partially filled, cancelled etc).
        /// </summary>
        /// <param name="newEvent">Event information</param>
        void OnOrderEvent(OrderEvent newEvent);

        /// <summary>
        /// Will submit an order request to the algorithm
        /// </summary>
        /// <param name="request">The request to submit</param>
        /// <remarks>Will run order prechecks, which include making sure the algorithm is not warming up, security is added and has data among others</remarks>
        /// <returns>The order ticket</returns>
        OrderTicket SubmitOrderRequest(SubmitOrderRequest request);

        /// <summary>
        /// Option assignment event handler. On an option assignment event for short legs the resulting information is passed to this method.
        /// </summary>
        /// <param name="assignmentEvent">Option exercise event details containing details of the assignment</param>
        /// <remarks>This method can be called asynchronously and so should only be used by seasoned C# experts. Ensure you use proper locks on thread-unsafe objects</remarks>
        void OnAssignmentOrderEvent(OrderEvent assignmentEvent);

        /// <summary>
        /// Brokerage message event handler. This method is called for all types of brokerage messages.
        /// </summary>
        void OnBrokerageMessage(BrokerageMessageEvent messageEvent);

        /// <summary>
        /// Brokerage disconnected event handler. This method is called when the brokerage connection is lost.
        /// </summary>
        void OnBrokerageDisconnect();

        /// <summary>
        /// Brokerage reconnected event handler. This method is called when the brokerage connection is restored after a disconnection.
        /// </summary>
        void OnBrokerageReconnect();

        /// <summary>
        /// Set the DateTime Frontier: This is the master time and is
        /// </summary>
        /// <param name="time"></param>
        void SetDateTime(DateTime time);

        /// <summary>
        /// Set the start date for the backtest
        /// </summary>
        /// <param name="start">Datetime Start date for backtest</param>
        /// <remarks>Must be less than end date and within data available</remarks>
        void SetStartDate(DateTime start);

        /// <summary>
        /// Set the end date for a backtest.
        /// </summary>
        /// <param name="end">Datetime value for end date</param>
        /// <remarks>Must be greater than the start date</remarks>
        void SetEndDate(DateTime end);

        /// <summary>
        /// Set the algorithm Id for this backtest or live run. This can be used to identify the order and equity records.
        /// </summary>
        /// <param name="algorithmId">unique 32 character identifier for backtest or live server</param>
        void SetAlgorithmId(string algorithmId);

        /// <summary>
        /// Set the algorithm as initialized and locked. No more cash or security changes.
        /// </summary>
        void SetLocked();

        /// <summary>
        /// Gets whether or not this algorithm has been locked and fully initialized
        /// </summary>
        bool GetLocked();

        /// <summary>
        /// Add a Chart object to algorithm collection
        /// </summary>
        /// <param name="chart">Chart object to add to collection.</param>
        void AddChart(Chart chart);

        /// <summary>
        /// Get the chart updates since the last request:
        /// </summary>
        /// <param name="clearChartData"></param>
        /// <returns>List of Chart Updates</returns>
        IEnumerable<Chart> GetChartUpdates(bool clearChartData = false);

        /// <summary>
        /// Set a required SecurityType-symbol and resolution for algorithm
        /// </summary>
        /// <param name="securityType">SecurityType Enum: Equity, Commodity, FOREX or Future</param>
        /// <param name="symbol">Symbol Representation of the MarketType, e.g. AAPL</param>
        /// <param name="resolution">Resolution of the MarketType required: MarketData, Second or Minute</param>
        /// <param name="market">The market the requested security belongs to, such as 'usa' or 'fxcm'</param>
        /// <param name="fillForward">If true, returns the last available data even if none in that timeslice.</param>
        /// <param name="leverage">leverage for this security</param>
        /// <param name="extendedMarketHours">ExtendedMarketHours send in data from 4am - 8pm, not used for FOREX</param>
        /// <param name="dataMappingMode">The contract mapping mode to use for the security</param>
        /// <param name="dataNormalizationMode">The price scaling mode to use for the security</param>
        Security AddSecurity(SecurityType securityType, string symbol, Resolution? resolution, string market, bool fillForward, decimal leverage, bool extendedMarketHours,
            DataMappingMode? dataMappingMode = null, DataNormalizationMode? dataNormalizationMode = null);

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
        Security AddSecurity(Symbol symbol, Resolution? resolution = null, bool fillForward = true, decimal leverage = Security.NullLeverage, bool extendedMarketHours = false,
            DataMappingMode? dataMappingMode = null, DataNormalizationMode? dataNormalizationMode = null, int contractDepthOffset = 0);

        /// <summary>
        /// Creates and adds a new single <see cref="Future"/> contract to the algorithm
        /// </summary>
        /// <param name="symbol">The futures contract symbol</param>
        /// <param name="resolution">The <see cref="Resolution"/> of market data, Tick, Second, Minute, Hour, or Daily. Default is <see cref="Resolution.Minute"/></param>
        /// <param name="fillForward">If true, returns the last available data even if none in that timeslice. Default is <value>true</value></param>
        /// <param name="leverage">The requested leverage for this equity. Default is set by <see cref="SecurityInitializer"/></param>
        /// <param name="extendedMarketHours">Show the after market data as well</param>
        /// <returns>The new <see cref="Future"/> security</returns>
        Future AddFutureContract(Symbol symbol, Resolution? resolution = null, bool fillForward = true, decimal leverage = 0m, bool extendedMarketHours = false);

        /// <summary>
        /// Creates and adds a new single <see cref="Option"/> contract to the algorithm
        /// </summary>
        /// <param name="symbol">The option contract symbol</param>
        /// <param name="resolution">The <see cref="Resolution"/> of market data, Tick, Second, Minute, Hour, or Daily. Default is <see cref="Resolution.Minute"/></param>
        /// <param name="fillForward">If true, returns the last available data even if none in that timeslice. Default is <value>true</value></param>
        /// <param name="leverage">The requested leverage for this equity. Default is set by <see cref="SecurityInitializer"/></param>
        /// <param name="extendedMarketHours">Show the after market data as well</param>
        /// <returns>The new <see cref="Option"/> security</returns>
        Option AddOptionContract(Symbol symbol, Resolution? resolution = null, bool fillForward = true, decimal leverage = 0m, bool extendedMarketHours = false);

        /// <summary>
        /// Removes the security with the specified symbol. This will cancel all
        /// open orders and then liquidate any existing holdings
        /// </summary>
        /// <param name="symbol">The symbol of the security to be removed</param>
        bool RemoveSecurity(Symbol symbol);

        /// <summary>
        /// Sets the account currency cash symbol this algorithm is to manage, as well as
        /// the starting cash in this currency if given
        /// </summary>
        /// <remarks>Has to be called during <see cref="Initialize"/> before
        /// calling <see cref="SetCash(decimal)"/> or adding any <see cref="Security"/></remarks>
        /// <param name="accountCurrency">The account currency cash symbol to set</param>
        /// <param name="startingCash">The account currency starting cash to set</param>
        void SetAccountCurrency(string accountCurrency, decimal? startingCash = null);

        /// <summary>
        /// Set the starting capital for the strategy
        /// </summary>
        /// <param name="startingCash">decimal starting capital, default $100,000</param>
        void SetCash(decimal startingCash);

        /// <summary>
        /// Set the cash for the specified symbol
        /// </summary>
        /// <param name="symbol">The cash symbol to set</param>
        /// <param name="startingCash">Decimal cash value of portfolio</param>
        /// <param name="conversionRate">The current conversion rate for the</param>
        void SetCash(string symbol, decimal startingCash, decimal conversionRate = 0);

        /// <summary>
        /// Liquidate your portfolio holdings:
        /// </summary>
        /// <param name="symbolToLiquidate">Specific asset to liquidate, defaults to all.</param>
        /// <param name="tag">Custom tag to know who is calling this.</param>
        /// <returns>list of order ids</returns>
        List<int> Liquidate(Symbol symbolToLiquidate = null, string tag = "Liquidated");

        /// <summary>
        /// Set live mode state of the algorithm run: Public setter for the algorithm property LiveMode.
        /// </summary>
        /// <param name="live">Bool live mode flag</param>
        void SetLiveMode(bool live);

        /// <summary>
        /// Sets the algorithm running mode
        /// </summary>
        /// <param name="algorithmMode">Algorithm mode</param>
        void SetAlgorithmMode(AlgorithmMode algorithmMode);

        /// <summary>
        /// Sets the algorithm deployment target
        /// </summary>
        /// <param name="deploymentTarget">Deployment target</param>
        void SetDeploymentTarget(DeploymentTarget deploymentTarget);

        /// <summary>
        /// Sets <see cref="IsWarmingUp"/> to false to indicate this algorithm has finished its warm up
        /// </summary>
        void SetFinishedWarmingUp();

        /// <summary>
        /// Set the maximum number of orders the algorithm is allowed to process.
        /// </summary>
        /// <param name="max">Maximum order count int</param>
        void SetMaximumOrders(int max);

        /// <summary>
        /// Sets the implementation used to handle messages from the brokerage.
        /// The default implementation will forward messages to debug or error
        /// and when a <see cref="BrokerageMessageType.Error"/> occurs, the algorithm
        /// is stopped.
        /// </summary>
        /// <param name="handler">The message handler to use</param>
        void SetBrokerageMessageHandler(IBrokerageMessageHandler handler);

        /// <summary>
        /// Set the historical data provider
        /// </summary>
        /// <param name="historyProvider">Historical data provider</param>
        void SetHistoryProvider(IHistoryProvider historyProvider);

        /// <summary>
        /// Get the last known price using the history provider.
        /// Useful for seeding securities with the correct price
        /// </summary>
        /// <param name="security"><see cref="Security"/> object for which to retrieve historical data</param>
        /// <returns>A single <see cref="BaseData"/> object with the last known price</returns>
        BaseData GetLastKnownPrice(Security security);

        /// <summary>
        /// Set the runtime error
        /// </summary>
        /// <param name="exception">Represents error that occur during execution</param>
        void SetRunTimeError(Exception exception);

        /// <summary>
        /// Set the state of a live deployment
        /// </summary>
        /// <param name="status">Live deployment status</param>
        void SetStatus(AlgorithmStatus status);

        /// <summary>
        /// Set the available <see cref="TickType"/> supported by each <see cref="SecurityType"/> in <see cref="SecurityManager"/>
        /// </summary>
        /// <param name="availableDataTypes">>The different <see cref="TickType"/> each <see cref="Security"/> supports</param>
        void SetAvailableDataTypes(Dictionary<SecurityType, List<TickType>> availableDataTypes);

        /// <summary>
        /// Sets the option chain provider, used to get the list of option contracts for an underlying symbol
        /// </summary>
        /// <param name="optionChainProvider">The option chain provider</param>
        void SetOptionChainProvider(IOptionChainProvider optionChainProvider);

        /// <summary>
        /// Sets the future chain provider, used to get the list of future contracts for an underlying symbol
        /// </summary>
        /// <param name="futureChainProvider">The future chain provider</param>
        void SetFutureChainProvider(IFutureChainProvider futureChainProvider);

        /// <summary>
        /// Sets the current slice
        /// </summary>
        /// <param name="slice">The Slice object</param>
        void SetCurrentSlice(Slice slice);

        /// <summary>
        /// Provide the API for the algorithm.
        /// </summary>
        /// <param name="api">Initiated API</param>
        void SetApi(IApi api);

        /// <summary>
        /// Sets the object store
        /// </summary>
        /// <param name="objectStore">The object store</param>
        void SetObjectStore(IObjectStore objectStore);

        /// <summary>
        /// Converts the string 'ticker' symbol into a full <see cref="Symbol"/> object
        /// This requires that the string 'ticker' has been added to the algorithm
        /// </summary>
        /// <param name="ticker">The ticker symbol. This should be the ticker symbol
        /// as it was added to the algorithm</param>
        /// <returns>The symbol object mapped to the specified ticker</returns>
        Symbol Symbol(string ticker);

        /// <summary>
        /// For the given symbol will resolve the ticker it used at the current algorithm date
        /// </summary>
        /// <param name="symbol">The symbol to get the ticker for</param>
        /// <returns>The mapped ticker for a symbol</returns>
        string Ticker(Symbol symbol);

        /// <summary>
        /// Sets the statistics service instance to be used by the algorithm
        /// </summary>
        /// <param name="statisticsService">The statistics service instance</param>
        void SetStatisticsService(IStatisticsService statisticsService);

        /// <summary>
        /// Sets name to the currently running backtest
        /// </summary>
        /// <param name="name">The name for the backtest</param>
        void SetName(string name);

        /// <summary>
        /// Adds a tag to the algorithm
        /// </summary>
        /// <param name="tag">The tag to add</param>
        void AddTag(string tag);

        /// <summary>
        /// Sets the tags for the algorithm
        /// </summary>
        /// <param name="tags">The tags</param>
        void SetTags(HashSet<string> tags);
    }
}
