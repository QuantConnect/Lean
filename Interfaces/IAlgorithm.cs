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
using System.Collections.Concurrent;
using System.Collections.Generic;
using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Notifications;
using QuantConnect.Orders;
using QuantConnect.Securities;

namespace QuantConnect.Interfaces
{
    /// <summary>
    /// Interface for QuantConnect algorithm implementations. All algorithms must implement these
    /// basic members to allow interaction with the Lean Backtesting Engine.
    /// </summary>
    public interface IAlgorithm
    {
        /// <summary>
        /// Data subscription manager controls the information and subscriptions the algorithms recieves.
        /// Subscription configurations can be added through the Subscription Manager.
        /// </summary>
        SubscriptionManager SubscriptionManager
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
            set;
        }

        /// <summary>
        /// Security portfolio management class provides wrapper and helper methods for the Security.Holdings class such as
        /// IsLong, IsShort, TotalProfit
        /// </summary>
        /// <remarks>Portfolio is a wrapper and helper class encapsulating the Securities[].Holdings objects</remarks>
        SecurityPortfolioManager Portfolio
        {
            get;
            set;
        }

        /// <summary>
        /// Security transaction manager class controls the store and processing of orders.
        /// </summary>
        /// <remarks>The orders and their associated events are accessible here. When a new OrderEvent is recieved the algorithm portfolio is updated.</remarks>
        SecurityTransactionManager Transactions
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the brokerage model used to emulate a real brokerage
        /// </summary>
        IBrokerageModel BrokerageModel { get; }

        /// <summary>
        /// Notification manager for storing and processing live event messages
        /// </summary>
        NotificationManager Notify
        {
            get;
            set;
        }

        /// <summary>
        /// Public name for the algorithm.
        /// </summary>
        /// <remarks>Not currently used but preserved for API integrity</remarks>
        string Name
        {
            get;
            set;
        }

        /// <summary>
        /// Property indicating the transaction handler is currently processing an order and the algorithm should wait (syncrhonous order processing).
        /// </summary>
        bool ProcessingOrder
        {
            get;
            set;
        }

        /// <summary>
        /// Current date/time.
        /// </summary>
        DateTime Time
        {
            get;
        }

        /// <summary>
        /// Algorithm start date for backtesting, set by the SetStartDate methods.
        /// </summary>
        /// <seealso cref="SetStartDate(DateTime)"/>
        /// <seealso cref="SetStartDate(int,int,int)"/>
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
        /// Accessor for Filled Orders:
        /// </summary>
        ConcurrentDictionary<int, Order> Orders
        {
            get;
        }

        /// <summary>
        /// Run Backtest Mode for the algorithm: Automatic, Parallel or Series.
        /// </summary>
        RunMode RunMode
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
        /// Debug messages from the strategy:
        /// </summary>
        List<string> DebugMessages
        {
            get;
            set;
        }

        /// <summary>
        /// Error messages from the strategy:
        /// </summary>
        List<string> ErrorMessages
        {
            get;
            set;
        }

        /// <summary>
        /// Log messages from the strategy:
        /// </summary>
        List<string> LogMessages
        {
            get;
            set;
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
        Dictionary<string, string> RuntimeStatistics
        {
            get;
        }

        /// <summary>
        /// Initialise the Algorithm and Prepare Required Data:
        /// </summary>
        void Initialize();

        // <summary>
        // v1.0 Handler for Tick Events [DEPRECATED June-2014]
        // </summary>
        // <param name="ticks">Tick Data Packet</param>
        //void OnTick(Dictionary<string, List<Tick>> ticks);

        // <summary>
        // v1.0 Handler for TradeBar Events [DEPRECATED June-2014]
        // </summary>
        // <param name="tradebars">TradeBar Data Packet</param>
        //void OnTradeBar(Dictionary<string, TradeBar> tradebars);

        // <summary>
        // v2.0 Handler for Generic Data Events
        // </summary>
        //void OnData(Ticks ticks);
        //void OnData(TradeBars tradebars);

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
        /// <param name="orders">The orders to be executed to bring this algorithm within margin limits</param>
        void OnMarginCall(List<Order> orders);

        /// <summary>
        /// Margin call warning event handler. This method is called when Portoflio.MarginRemaining is under 5% of your Portfolio.TotalPortfolioValue
        /// </summary>
        void OnMarginCallWarning();

        /// <summary>
        /// Call this method at the end of each day of data.
        /// </summary>
        void OnEndOfDay();

        /// <summary>
        /// Call this method at the end of each day of data.
        /// </summary>
        void OnEndOfDay(string symbol);

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
        /// Set the DateTime Frontier: This is the master time and is
        /// </summary>
        /// <param name="time"></param>
        void SetDateTime(DateTime time);

        /// <summary>
        /// Set the run mode of the algorithm: series, parallel or automatic.
        /// </summary>
        /// <param name="mode">Run mode to select, default automatic</param>
        /// <obsolete>The set runmode method is now obsolete and all algorithms are run in series mode.</obsolete>
        void SetRunMode(RunMode mode = RunMode.Automatic);

        /// <summary>
        /// Set the start date of the backtest period. This must be within available data.
        /// </summary>
        void SetStartDate(int year, int month, int day);

        /// <summary>
        /// Alias for SetStartDate() which accepts DateTime Class
        /// </summary>
        /// <param name="start">DateTime Object to Start the Algorithm</param>
        void SetStartDate(DateTime start);

        /// <summary>
        /// Set the end Backtest date for the algorithm. This must be within available data.
        /// </summary>
        void SetEndDate(int year, int month, int day);

        /// <summary>
        /// Alias for SetStartDate() which accepts DateTime Object
        /// </summary>
        /// <param name="end">DateTime End Date for Analysis</param>
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
        /// Get the chart updates since the last request:
        /// </summary>
        /// <param name="clearChartData"></param>
        /// <returns>List of Chart Updates</returns>
        List<Chart> GetChartUpdates(bool clearChartData = false);

        /// <summary>
        /// Add a chart to the internal algorithm list.
        /// </summary>
        /// <param name="chart">Chart object to add</param>
        void AddChart(Chart chart);

        /// <summary>
        /// Set a required SecurityType-symbol and resolution for algorithm
        /// </summary>
        /// <param name="securityType">SecurityType Enum: Equity, Commodity, FOREX or Future</param>
        /// <param name="symbol">Symbol Representation of the MarketType, e.g. AAPL</param>
        /// <param name="resolution">Resolution of the MarketType required: MarketData, Second or Minute</param>
        /// <param name="fillDataForward">If true, returns the last available data even if none in that timeslice.</param>
        /// <param name="leverage">leverage for this security</param>
        /// <param name="extendedMarketHours">ExtendedMarketHours send in data from 4am - 8pm, not used for FOREX</param>
        void AddSecurity(SecurityType securityType, string symbol, Resolution resolution, bool fillDataForward, decimal leverage, bool extendedMarketHours);

        /// <summary>
        /// AddData-typeparam name="T"- a new user defined data source, requiring only the minimum config options:
        /// </summary>
        /// <param name="symbol">Key/Symbol for data</param>
        /// <param name="resolution">Resolution of the data</param>
        /// <param name="isTradeBar">Set to true if this data has Open, High, Low, and Close properties</param>
        /// <param name="hasVolume">Set to true if this data has a Volume property</param>
        void AddData<T>(string symbol, Resolution resolution = Resolution.Second, bool isTradeBar = false, bool hasVolume = false);

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
        void SetCash(string symbol, decimal startingCash, decimal conversionRate);

        /// <summary>
        /// Send an order to the transaction manager.
        /// </summary>
        /// <param name="symbol">Symbol we want to purchase</param>
        /// <param name="quantity">Quantity to buy, + is long, - short.</param>
        /// <param name="asynchronous">Don't wait for the response, just submit order and move on.</param>
        /// <param name="tag">Custom data for this order</param>
        /// <returns>Integer Order ID.</returns>
        int Order(string symbol, int quantity, bool asynchronous = false, string tag = "");

        /// <summary>
        /// Liquidate your portfolio holdings:
        /// </summary>
        /// <param name="symbolToLiquidate">Specific asset to liquidate, defaults to all.</param>
        /// <returns>list of order ids</returns>
        List<int> Liquidate(string symbolToLiquidate = "");

        /// <summary>
        /// Terminate the algorithm on exiting the current event processor.
        /// If have holdings at the end of the algorithm/day they will be liquidated at market prices.
        /// If running a series analysis this command skips the current day (and doesn't liquidate).
        /// </summary>
        /// <param name="message">Exit message</param>
        void Quit(string message = "");

        /// <summary>
        /// Set the quit flag true / false.
        /// </summary>
        /// <param name="quit">When true quits the algorithm event loop for this day</param>
        void SetQuit(bool quit);

        /// <summary>
        /// Set live mode state of the algorithm run: Public setter for the algorithm property LiveMode.
        /// </summary>
        /// <param name="live">Bool live mode flag</param>
        void SetLiveMode(bool live);

        /// <summary>
        /// Set the maximum number of orders the algortihm is allowed to process.
        /// </summary>
        /// <param name="max">Maximum order count int</param>
        void SetMaximumOrders(int max);

        /// <summary>
        /// Set the maximum number of assets allowable to ensure good memory usage / avoid linux killing job.
        /// </summary>
        /// <param name="minuteLimit">Maximum number of minute level assets the live mode can support with selected server</param>
        /// <param name="secondLimit">Maximum number of second level assets the live mode can support with selected server</param>
        /// /// <param name="tickLimit">Maximum number of tick level assets the live mode can support with selected server</param>
        /// <remarks>Sets the live behaviour of the algorithm including the selected server (ram) limits.</remarks>
        void SetAssetLimits(int minuteLimit = 50, int secondLimit = 10, int tickLimit = 5);

        /// <summary>
        /// Set a runtime statistic for your algorithm- these are displayed on the IDE during live runmode.
        /// </summary>
        /// <param name="name">Key name for the statistic</param>
        /// <param name="value">String value for statistic</param>
        void SetRuntimeStatistic(string name, string value);

        /// <summary>
        /// Get the quit flag state.
        /// </summary>
        /// <returns>Boolean quit flag</returns>
        bool GetQuit();
}
}
