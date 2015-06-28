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
using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Notifications;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Securities.Equity;
using QuantConnect.Securities.Forex;

namespace QuantConnect.Algorithm
{
    /// <summary>
    /// QC Algorithm Base Class - Handle the basic requirements of a trading algorithm, 
    /// allowing user to focus on event methods. The QCAlgorithm class implements Portfolio, 
    /// Securities, Transactions and Data Subscription Management.
    /// </summary>
    public partial class QCAlgorithm : MarshalByRefObject, IAlgorithm
    {
        private DateTime _time;
        private DateTime _startDate;   //Default start and end dates.
        private DateTime _endDate;     //Default end to yesterday
        private RunMode _runMode = RunMode.Series;
        private bool _locked;
        private bool _quit;
        private bool _liveMode;
        private string _algorithmId = "";
        private List<string> _debugMessages = new List<string>();
        private List<string> _logMessages = new List<string>();
        private List<string> _errorMessages = new List<string>();
        
        //Error tracking to avoid message flooding:
        private string _previousDebugMessage = "";
        private string _previousErrorMessage = "";
        private bool _sentNoDataError = false;

        /// <summary>
        /// QCAlgorithm Base Class Constructor - Initialize the underlying QCAlgorithm components.
        /// QCAlgorithm manages the transactions, portfolio, charting and security subscriptions for the users algorithms.
        /// </summary>
        public QCAlgorithm()
        {
            //Initialise the Algorithm Helper Classes:
            //- Note - ideally these wouldn't be here, but because of the DLL we need to make the classes shared across 
            //  the Worker & Algorithm, limiting ability to do anything else.

            //Initialise Data Manager 
            SubscriptionManager = new SubscriptionManager();

            Securities = new SecurityManager();
            Transactions = new SecurityTransactionManager(Securities);
            Portfolio = new SecurityPortfolioManager(Securities, Transactions);
            BrokerageModel = new DefaultBrokerageModel();
            Notify = new NotificationManager(false); // Notification manager defaults to disabled.

            //Initialise Algorithm RunMode to Series - Parallel Mode deprecated:
            _runMode = RunMode.Series;

            //Initialise to unlocked:
            _locked = false;

            //Initialise Start and End Dates:
            _startDate = new DateTime(1998, 01, 01);
            _endDate = DateTime.Now.AddDays(-1);
        }

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
        /// Read-only value for current time frontier of the algorithm and event horizon. 
        /// </summary>
        /// <remarks>During backtesting this is primarily sourced from the data feed. During live trading the time is updated from the system clock.</remarks>
        public DateTime Time 
        {
            get 
            {
                return _time;
            }
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
        public List<string> DebugMessages
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
        public List<string> LogMessages 
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
        public List<string> ErrorMessages
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
            throw new NotImplementedException("Please override the Intitialize() method");
        }

        /// <summary>
        /// Event handler for TradeBar data subscriptions packets. This method was deprecated June 2014 and replaced with OnData(TradeBars data)
        /// </summary>
        /// <param name="data">Dictionary of MarketData Objects</param>
        /// <obsolete>This method is obsolete, please use 'void OnData(TradeBars data)' instead</obsolete>
        [Obsolete("'override void OnTradeBar' method is obsolete, please use 'void OnData(TradeBars data)' instead")]
        public virtual void OnTradeBar(Dictionary<string, TradeBar> data)
        {
            //Algorithm Implementation
            //throw new NotImplementedException("OnTradeBar has been made obsolete. Please use OnData(TradeBars data) instead.");
        }

        /// <summary>
        /// Event handler for Tick data subscriptions. This method was deprecated June 2014 and replaced with OnData(Ticks data).
        /// </summary>
        /// <param name="data">Ticks arriving at the same moment come in a list. Because the "tick" data is actually list ordered within a second, you can get lots of ticks at once.</param>
        /// <obsolete>This method is obsolete, please use 'void OnData(Ticks data)' instead</obsolete>
        [Obsolete("'override void OnTick' method is obsolete, please use 'void OnData(Ticks data)' instead")]
        public virtual void OnTick(Dictionary<string, List<Tick>> data)
        {
            //Algorithm Implementation
            //throw new NotImplementedException("OnTick has been made obsolete. Please use OnData(Ticks data) instead.");
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
        // Event - v2.0 TICK EVENT HANDLER: (Pattern) Basic template for user to override when requesting tick data.
        // </summary>
        // <param name="data">List of Tick Data</param>
        //public void OnData(Ticks data)
        //{
        //
        //}

        // <summary>
        // Event - v2.0 SPLIT EVENT HANDLER: (Pattern) Basic template for user to override when requesting tick data.
        // </summary>
        // <param name="data">IDictionary of Splits Data Keyed by Symbol String</param>
        //public void OnData(Splits data)
        //{
        //
        //}

        // <summary>
        // Event - v2.0 DIVIDEND EVENT HANDLER: (Pattern) Basic template for user to override when requesting tick data.
        // </summary>
        // <param name="data">IDictionary of Dividend Data Keyed by Symbol String</param>
        //public void OnData(Dividends data)
        //{
        //
        //}

        /// <summary>
        /// Margin call event handler. This method is called right before the margin call orders are placed in the market.
        /// </summary>
        /// <param name="orders">The orders to be executed to bring this algorithm within margin limits</param>
        public virtual void OnMarginCall(List<Order> orders)
        {
        }

        /// <summary>
        /// Margin call warning event handler. This method is called when Portoflio.MarginRemaining is under 5% of your Portfolio.TotalPortfolioValue
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
        /// <param name="symbol">Asset symbol for this end of day event. Forex and equities have different closing hours.</param>
        public virtual void OnEndOfDay(string symbol) 
        {
            
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
        /// Update the interal algorithm time frontier.
        /// </summary>
        /// <remarks>For internal use only to advance time.</remarks>
        /// <param name="frontier">Current datetime.</param>
        public void SetDateTime(DateTime frontier) 
        {
            if (frontier > _time)
            {
                _time = frontier;
            }
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
        public void SetBrokerageModel(BrokerageName brokerage)
        {
            switch (brokerage)
            {
                case BrokerageName.Default:
                    BrokerageModel = new DefaultBrokerageModel();
                    break;
                case BrokerageName.InteractiveBrokersBrokerage:
                    BrokerageModel = new InteractiveBrokersBrokerageModel();
                    break;

                case BrokerageName.TradierBrokerage:
                    BrokerageModel = new TradierBrokerageModel();
                    break;

                default:
                    throw new ArgumentOutOfRangeException("brokerage", brokerage, null);
            }
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
                _startDate = start;
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
            }
        }

        /// <summary>
        /// Set the maximum number of assets allowable to ensure good memory usage / avoid linux killing job.
        /// </summary>
        /// <param name="minuteLimit">Maximum number of minute level assets the live mode can support with selected server</param>
        /// <param name="secondLimit">Maximum number of second level assets the live mode can support with selected server</param>
        /// /// <param name="tickLimit">Maximum number of tick level assets the live mode can support with selected server</param>
        /// <remarks>Sets the live behaviour of the algorithm including the selected server (ram) limits.</remarks>
        public void SetAssetLimits(int minuteLimit = 500, int secondLimit = 100, int tickLimit = 30)
        {
            if (!_locked)
            {
                Securities.SetLimits(minuteLimit, secondLimit, tickLimit);
            }
        }

        /// <summary>
        /// Add specified data to our data subscriptions. QuantConnect will funnel this data to the handle data routine.
        /// </summary>
        /// <param name="securityType">MarketType Type: Equity, Commodity, Future or FOREX</param>
        /// <param name="symbol">Symbol Reference for the MarketType</param>
        /// <param name="resolution">Resolution of the Data Required</param>
        /// <param name="fillDataForward">When no data available on a tradebar, return the last data that was generated</param>
        /// <param name="extendedMarketHours">Show the after market data as well</param>
        public void AddSecurity(SecurityType securityType, string symbol, Resolution resolution = Resolution.Minute, bool fillDataForward = true, bool extendedMarketHours = false)
        {
            AddSecurity(securityType, symbol, resolution, fillDataForward, 0, extendedMarketHours);
        }

        /// <summary>
        /// Add specified data to required list. QC will funnel this data to the handle data routine.
        /// </summary>
        /// <param name="securityType">MarketType Type: Equity, Commodity, Future or FOREX</param>
        /// <param name="symbol">Symbol Reference for the MarketType</param>
        /// <param name="resolution">Resolution of the Data Required</param>
        /// <param name="fillDataForward">When no data available on a tradebar, return the last data that was generated</param>
        /// <param name="leverage">Custom leverage per security</param>
        /// <param name="extendedMarketHours">Extended market hours</param>
        /// <remarks> AddSecurity(SecurityType securityType, string symbol, Resolution resolution, bool fillDataForward, decimal leverage, bool extendedMarketHours)</remarks>
        public void AddSecurity(SecurityType securityType, string symbol, Resolution resolution, bool fillDataForward, decimal leverage, bool extendedMarketHours) 
        {
            try
            {
                if (_locked)
                {
                    throw new Exception("Algorithm.AddSecurity(): Cannot add another security after algorithm running.");
                }
                
                symbol = symbol.ToUpper();
                //If it hasn't been set, use some defaults based on the portfolio type:
                if (leverage <= 0)
                {
                    switch (securityType)
                    {
                        case SecurityType.Equity:
                            leverage = 2; //Cash Ac. = 1, RegT Std = 2 or PDT = 4.
                            break;
                        case SecurityType.Forex:
                            leverage = 50;
                            break;
                    }
                }

                //Add the symbol to Data Manager -- generate unified data streams for algorithm events
                var config = SubscriptionManager.Add(securityType, symbol, resolution, fillDataForward, extendedMarketHours);

                Security security;
                switch (config.SecurityType)
                {
                    case SecurityType.Equity:
                        security = new Equity(config, leverage, false);
                        break;

                    case SecurityType.Forex:
                        // decompose the symbol into each currency pair
                        string baseCurrency, quoteCurrency;
                        QuantConnect.Securities.Forex.Forex.DecomposeCurrencyPair(symbol, out baseCurrency, out quoteCurrency);

                        if (!Portfolio.CashBook.ContainsKey(baseCurrency))
                        {
                            // since we have none it's safe to say the conversion is zero
                            Portfolio.CashBook.Add(baseCurrency, 0, 0);
                        }
                        if (!Portfolio.CashBook.ContainsKey(quoteCurrency))
                        {
                            // since we have none it's safe to say the conversion is zero
                            Portfolio.CashBook.Add(quoteCurrency, 0, 0);
                        }
                        security = new Forex(Portfolio.CashBook[quoteCurrency], config, leverage, false);
                        break;

                    default:
                    case SecurityType.Base:
                        security = new Security(config, leverage, false);
                        break;
                }

                //Add the symbol to Securities Manager -- manage collection of portfolio entities for easy access.
                Securities.Add(config.Symbol, security);
            }
            catch (Exception err) 
            {
                Error("Algorithm.AddSecurity(): " + err.Message);
            }
        }

        /// <summary>
        /// AddData<typeparam name="T"/> a new user defined data source, requiring only the minimum config options.
        /// </summary>
        /// <param name="symbol">Key/Symbol for data</param>
        /// <param name="resolution">Resolution of the data</param>
        /// <remarks>Generic type T must implement base data</remarks>
        public void AddData<T>(string symbol, Resolution resolution = Resolution.Minute)
        {
            if (_locked) return;

            //Add this new generic data as a tradeable security: 
            // Defaults:extended market hours"      = true because we want events 24 hours, 
            //          fillforward                 = false because only want to trigger when there's new custom data.
            //          leverage                    = 1 because no leverage on nonmarket data?
            AddData<T>(symbol, resolution, fillDataForward: false, leverage: 1m, isTradeBar: false, hasVolume: false);
        }

        /// <summary>
        /// AddData<typeparam name="T"/> a new user defined data source, requiring only the minimum config options.
        /// </summary>
        /// <param name="symbol">Key/Symbol for data</param>
        /// <param name="resolution">Resolution of the data</param>
        /// <param name="isTradeBar">Set to true if this data has Open, High, Low, and Close properties</param>
        /// <param name="hasVolume">Set to true if this data has a Volume property</param>
        /// <remarks>Generic type T must implement base data</remarks>
        public void AddData<T>(string symbol, Resolution resolution, bool isTradeBar, bool hasVolume)
        {
            if (_locked) return;

            AddData<T>(symbol, resolution, fillDataForward: false, leverage: 1m, isTradeBar: isTradeBar, hasVolume: hasVolume);
        }

        /// <summary>
        /// AddData<typeparam name="T"/> a new user defined data source, requiring only the minimum config options.
        /// </summary>
        /// <param name="symbol">Key/Symbol for data</param>
        /// <param name="resolution">Resolution of the Data Required</param>
        /// <param name="fillDataForward">When no data available on a tradebar, return the last data that was generated</param>
        /// <param name="leverage">Custom leverage per security</param>
        /// <param name="isTradeBar">Set to true if this data has Open, High, Low, and Close properties</param>
        /// <param name="hasVolume">Set to true if this data has a Volume property</param>
        /// <remarks>Generic type T must implement base data</remarks>
        public void AddData<T>(string symbol, Resolution resolution, bool fillDataForward, decimal leverage = 1.0m, bool isTradeBar = false, bool hasVolume = false)
        {
            if (_locked) return;

            symbol = symbol.ToUpper();

            //Add this to the data-feed subscriptions
            var config = SubscriptionManager.Add(typeof(T), SecurityType.Base, symbol, resolution, fillDataForward, extendedMarketHours: true, isTradeBar: isTradeBar, hasVolume: hasVolume);

            //Add this new generic data as a tradeable security: 
            var security = new Security(config, leverage,  true);
            Securities.Add(symbol, security);
        }

        /// <summary>
        /// Send a debug message to the web console:
        /// </summary>
        /// <param name="message">Message to send to debug console</param>
        /// <seealso cref="Log"/>
        /// <seealso cref="Error(string)"/>
        public void Debug(string message)
        {
            if (!_liveMode && (message == "" || _previousDebugMessage == message)) return;
            _debugMessages.Add(message);
            _previousDebugMessage = message;
        }

        /// <summary>
        /// Added another method for logging if user guessed.
        /// </summary>
        /// <param name="message">String message to log.</param>
        /// <seealso cref="Debug"/>
        /// <seealso cref="Error(string)"/>
        public void Log(string message) 
        {
            if (!_liveMode && message == "") return;
            _logMessages.Add(message);
        }

        /// <summary>
        /// Send a string error message to the Console.
        /// </summary>
        /// <param name="message">Message to display in errors grid</param>
        /// <seealso cref="Debug"/>
        /// <seealso cref="Log"/>
        public void Error(string message)
        {
            if (!_liveMode && (message == "" || _previousErrorMessage == message)) return;
            _errorMessages.Add(message);
            _previousErrorMessage = message;
        }

        /// <summary>
        /// Send a string error message to the Console.
        /// </summary>
        /// <param name="error">Exception object captured from a try catch loop</param>
        /// <seealso cref="Debug"/>
        /// <seealso cref="Log"/>
        public void Error(Exception error)
        {
            var message = error.Message;
            if (!_liveMode && (message == "" || _previousErrorMessage == message)) return;
            _errorMessages.Add(message);
            _previousErrorMessage = message;
        }

        /// <summary>
        /// Terminate the algorithm after processing the current event handler.
        /// </summary>
        /// <param name="message">Exit message to display on quitting</param>
        public void Quit(string message = "") 
        {
            Debug("Quit(): " + message);
            _quit = true;
        }

        /// <summary>
        /// Set the Quit flag property of the algorithm.
        /// </summary>
        /// <remarks>Intended for internal use by the QuantConnect Lean Engine only.</remarks>
        /// <param name="quit">Boolean quit state</param>
        /// <seealso cref="Quit"/>
        /// <seealso cref="GetQuit"/>
        public void SetQuit(bool quit) 
        {
            _quit = quit;
        }

        /// <summary>
        /// Get the quit state of the algorithm
        /// </summary>
        /// <returns>Boolean true if set to quit event loop.</returns>
        /// <remarks>Intended for internal use by the QuantConnect Lean Engine only.</remarks>
        /// <seealso cref="Quit"/>
        /// <seealso cref="SetQuit"/>
        public bool GetQuit() 
        {
            return _quit;
        }

    }
}
