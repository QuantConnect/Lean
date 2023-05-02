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
using System.Globalization;
using System.Linq;
using System.Reflection;
using Fasterflect;
using QuantConnect.AlgorithmFactory;
using QuantConnect.Brokerages;
using QuantConnect.Configuration;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Lean.Engine.TransactionHandlers;
using QuantConnect.Logging;
using QuantConnect.Packets;
using QuantConnect.Securities;
using QuantConnect.Util;

namespace QuantConnect.Lean.Engine.Setup
{
    /// <summary>
    /// Defines a set up handler that initializes the algorithm instance using values retrieved from the user's brokerage account
    /// </summary>
    public class BrokerageSetupHandler : ISetupHandler
    {
        private bool _notifiedUniverseSettingsUsed;

        /// <summary>
        /// Max allocation limit configuration variable name
        /// </summary>
        public static string MaxAllocationLimitConfig = "max-allocation-limit";

        /// <summary>
        /// The worker thread instance the setup handler should use
        /// </summary>
        public WorkerThread WorkerThread { get; set; }

        /// <summary>
        /// Any errors from the initialization stored here:
        /// </summary>
        public List<Exception> Errors { get; set; }

        /// <summary>
        /// Get the maximum runtime for this algorithm job.
        /// </summary>
        public TimeSpan MaximumRuntime { get; }

        /// <summary>
        /// Algorithm starting capital for statistics calculations
        /// </summary>
        public decimal StartingPortfolioValue { get; private set; }

        /// <summary>
        /// Start date for analysis loops to search for data.
        /// </summary>
        public DateTime StartingDate { get; private set; }

        /// <summary>
        /// Maximum number of orders for the algorithm run -- applicable for backtests only.
        /// </summary>
        public int MaxOrders { get; }

        // saves ref to algo so we can call quit if runtime error encountered
        private IBrokerageFactory _factory;
        private IBrokerage _dataQueueHandlerBrokerage;

        /// <summary>
        /// Initializes a new BrokerageSetupHandler
        /// </summary>
        public BrokerageSetupHandler()
        {
            Errors = new List<Exception>();
            MaximumRuntime = TimeSpan.FromDays(10*365);
            MaxOrders = int.MaxValue;
        }

        /// <summary>
        /// Create a new instance of an algorithm from a physical dll path.
        /// </summary>
        /// <param name="assemblyPath">The path to the assembly's location</param>
        /// <param name="algorithmNodePacket">Details of the task required</param>
        /// <returns>A new instance of IAlgorithm, or throws an exception if there was an error</returns>
        public IAlgorithm CreateAlgorithmInstance(AlgorithmNodePacket algorithmNodePacket, string assemblyPath)
        {
            string error;
            IAlgorithm algorithm;

            // limit load times to 10 seconds and force the assembly to have exactly one derived type
            var loader = new Loader(false, algorithmNodePacket.Language, BaseSetupHandler.AlgorithmCreationTimeout, names => names.SingleOrAlgorithmTypeName(Config.Get("algorithm-type-name")), WorkerThread);
            var complete = loader.TryCreateAlgorithmInstanceWithIsolator(assemblyPath, algorithmNodePacket.RamAllocation, out algorithm, out error);
            if (!complete) throw new AlgorithmSetupException($"During the algorithm initialization, the following exception has occurred: {error}");

            return algorithm;
        }

        /// <summary>
        /// Creates the brokerage as specified by the job packet
        /// </summary>
        /// <param name="algorithmNodePacket">Job packet</param>
        /// <param name="uninitializedAlgorithm">The algorithm instance before Initialize has been called</param>
        /// <param name="factory">The brokerage factory</param>
        /// <returns>The brokerage instance, or throws if error creating instance</returns>
        public IBrokerage CreateBrokerage(AlgorithmNodePacket algorithmNodePacket, IAlgorithm uninitializedAlgorithm, out IBrokerageFactory factory)
        {
            var liveJob = algorithmNodePacket as LiveNodePacket;
            if (liveJob == null)
            {
                throw new ArgumentException("BrokerageSetupHandler.CreateBrokerage requires a live node packet");
            }

            // find the correct brokerage factory based on the specified brokerage in the live job packet
            _factory = Composer.Instance.Single<IBrokerageFactory>(brokerageFactory => brokerageFactory.BrokerageType.MatchesTypeName(liveJob.Brokerage));
            factory = _factory;

            PreloadDataQueueHandler(liveJob, uninitializedAlgorithm, factory);

            // initialize the correct brokerage using the resolved factory
            var brokerage = _factory.CreateBrokerage(liveJob, uninitializedAlgorithm);

            return brokerage;
        }

        /// <summary>
        /// Primary entry point to setup a new algorithm
        /// </summary>
        /// <param name="parameters">The parameters object to use</param>
        /// <returns>True on successfully setting up the algorithm state, or false on error.</returns>
        public bool Setup(SetupHandlerParameters parameters)
        {
            var algorithm = parameters.Algorithm;
            var brokerage = parameters.Brokerage;
            // verify we were given the correct job packet type
            var liveJob = parameters.AlgorithmNodePacket as LiveNodePacket;
            if (liveJob == null)
            {
                AddInitializationError("BrokerageSetupHandler requires a LiveNodePacket");
                return false;
            }

            algorithm.Name = liveJob.GetAlgorithmName();

            // verify the brokerage was specified
            if (string.IsNullOrWhiteSpace(liveJob.Brokerage))
            {
                AddInitializationError("A brokerage must be specified");
                return false;
            }


            // attach to the message event to relay brokerage specific initialization messages
            EventHandler<BrokerageMessageEvent> brokerageOnMessage = (sender, args) =>
            {
                if (args.Type == BrokerageMessageType.Error)
                {
                    AddInitializationError($"Brokerage Error Code: {args.Code} - {args.Message}");
                }
            };

            try
            {
                // let the world know what we're doing since logging in can take a minute
                parameters.ResultHandler.SendStatusUpdate(AlgorithmStatus.LoggingIn, "Logging into brokerage...");

                brokerage.Message += brokerageOnMessage;

                Log.Trace("BrokerageSetupHandler.Setup(): Connecting to brokerage...");
                try
                {
                    // this can fail for various reasons, such as already being logged in somewhere else
                    brokerage.Connect();
                }
                catch (Exception err)
                {
                    Log.Error(err);
                    AddInitializationError(
                        $"Error connecting to brokerage: {err.Message}. " +
                        "This may be caused by incorrect login credentials or an unsupported account type.", err);
                    return false;
                }

                if (!brokerage.IsConnected)
                {
                    // if we're reporting that we're not connected, bail
                    AddInitializationError("Unable to connect to brokerage.");
                    return false;
                }

                var message = $"{brokerage.Name} account base currency: {brokerage.AccountBaseCurrency ?? algorithm.AccountCurrency}";


                var accountCurrency = brokerage.AccountBaseCurrency;
                if (liveJob.BrokerageData.ContainsKey(MaxAllocationLimitConfig))
                {
                    accountCurrency = Currencies.USD;
                    message += ". Allocation limited, will use 'USD' account currency";
                }

                Log.Trace($"BrokerageSetupHandler.Setup(): {message}");

                algorithm.Debug(message);
                if (accountCurrency != null && accountCurrency != algorithm.AccountCurrency)
                {
                    algorithm.SetAccountCurrency(accountCurrency);
                }

                Log.Trace("BrokerageSetupHandler.Setup(): Initializing algorithm...");

                parameters.ResultHandler.SendStatusUpdate(AlgorithmStatus.Initializing, "Initializing algorithm...");

                //Execute the initialize code:
                var controls = liveJob.Controls;
                var isolator = new Isolator();
                var initializeComplete = isolator.ExecuteWithTimeLimit(TimeSpan.FromSeconds(300), () =>
                {
                    try
                    {
                        //Set the default brokerage model before initialize
                        algorithm.SetBrokerageModel(_factory.GetBrokerageModel(algorithm.Transactions));

                        //Margin calls are disabled by default in live mode
                        algorithm.Portfolio.MarginCallModel = MarginCallModel.Null;

                        //Set our parameters
                        algorithm.SetParameters(liveJob.Parameters);
                        algorithm.SetAvailableDataTypes(BaseSetupHandler.GetConfiguredDataFeeds());

                        //Algorithm is live, not backtesting:
                        algorithm.SetLiveMode(true);

                        //Initialize the algorithm's starting date
                        algorithm.SetDateTime(DateTime.UtcNow);

                        //Set the source impl for the event scheduling
                        algorithm.Schedule.SetEventSchedule(parameters.RealTimeHandler);

                        var optionChainProvider = Composer.Instance.GetPart<IOptionChainProvider>();
                        if (optionChainProvider == null)
                        {
                            optionChainProvider = new CachingOptionChainProvider(new LiveOptionChainProvider(parameters.DataCacheProvider, parameters.MapFileProvider));
                        }
                        // set the option chain provider
                        algorithm.SetOptionChainProvider(optionChainProvider);

                        var futureChainProvider = Composer.Instance.GetPart<IFutureChainProvider>();
                        if (futureChainProvider == null)
                        {
                            futureChainProvider = new CachingFutureChainProvider(new LiveFutureChainProvider(parameters.DataCacheProvider));
                        }
                        // set the future chain provider
                        algorithm.SetFutureChainProvider(futureChainProvider);

                        // set the object store
                        algorithm.SetObjectStore(parameters.ObjectStore);

                        // If we're going to receive market data from IB, set the default subscription limit to 100, algorithms can override this setting in the Initialize method
                        if (liveJob.DataQueueHandler.Contains("InteractiveBrokersBrokerage", StringComparison.InvariantCultureIgnoreCase))
                        {
                            algorithm.Settings.DataSubscriptionLimit = 100;
                            var message = $"Detected 'InteractiveBrokers' data feed. Adjusting algorithm Settings.DataSubscriptionLimit to {algorithm.Settings.DataSubscriptionLimit}." +
                            $" Can override this setting on Initialize.";
                            algorithm.Debug(message);
                            Log.Trace($"BrokerageSetupHandler.Setup(): {message}");
                        }

                        //Initialise the algorithm, get the required data:
                        algorithm.Initialize();

                        if (liveJob.Brokerage != "PaperBrokerage")
                        {
                            //Zero the CashBook - we'll populate directly from brokerage
                            foreach (var kvp in algorithm.Portfolio.CashBook)
                            {
                                kvp.Value.SetAmount(0);
                            }
                        }
                    }
                    catch (Exception err)
                    {
                        AddInitializationError(err.ToString(), err);
                    }
                }, controls.RamAllocation,
                    sleepIntervalMillis: 100); // entire system is waiting on this, so be as fast as possible

                if (Errors.Count != 0)
                {
                    // if we already got an error just exit right away
                    return false;
                }

                if (!initializeComplete)
                {
                    AddInitializationError("Initialization timed out.");
                    return false;
                }

                if (!LoadCashBalance(brokerage, algorithm))
                {
                    return false;
                }

                if (!LoadExistingHoldingsAndOrders(brokerage, algorithm, parameters))
                {
                    return false;
                }

                //Finalize Initialization
                algorithm.PostInitialize();

                BaseSetupHandler.SetupCurrencyConversions(algorithm, parameters.UniverseSelection);

                if (algorithm.Portfolio.TotalPortfolioValue == 0)
                {
                    algorithm.Debug("Warning: No cash balances or holdings were found in the brokerage account.");
                }

                string maxCashLimitStr;
                if (liveJob.BrokerageData.TryGetValue(MaxAllocationLimitConfig, out maxCashLimitStr))
                {
                    var maxCashLimit = decimal.Parse(maxCashLimitStr, NumberStyles.Any, CultureInfo.InvariantCulture);

                    // If allocation exceeded by more than $10,000; block deployment
                    if (algorithm.Portfolio.TotalPortfolioValue > (maxCashLimit + 10000m))
                    {
                        var exceptionMessage = $"TotalPortfolioValue '{algorithm.Portfolio.TotalPortfolioValue}' exceeds allocation limit '{maxCashLimit}'";
                        algorithm.Debug(exceptionMessage);
                        throw new ArgumentException(exceptionMessage);
                    }
                }

                //Set the starting portfolio value for the strategy to calculate performance:
                StartingPortfolioValue = algorithm.Portfolio.TotalPortfolioValue;
                StartingDate = DateTime.Now;

                // we set the free portfolio value based on the initial total value and the free percentage value
                algorithm.Settings.FreePortfolioValue =
                    algorithm.Portfolio.TotalPortfolioValue * algorithm.Settings.FreePortfolioValuePercentage;
            }
            catch (Exception err)
            {
                AddInitializationError(err.ToString(), err);
            }
            finally
            {
                if (brokerage != null)
                {
                    brokerage.Message -= brokerageOnMessage;
                }
            }

            return Errors.Count == 0;
        }

        private bool LoadCashBalance(IBrokerage brokerage, IAlgorithm algorithm)
        {
            Log.Trace("BrokerageSetupHandler.Setup(): Fetching cash balance from brokerage...");
            try
            {
                // set the algorithm's cash balance for each currency
                var cashBalance = brokerage.GetCashBalance();
                foreach (var cash in cashBalance)
                {
                    Log.Trace($"BrokerageSetupHandler.Setup(): Setting {cash.Currency} cash to {cash.Amount}");

                    algorithm.Portfolio.SetCash(cash.Currency, cash.Amount, 0);
                }
            }
            catch (Exception err)
            {
                Log.Error(err);
                AddInitializationError("Error getting cash balance from brokerage: " + err.Message, err);
                return false;
            }
            return true;
        }

        private bool LoadExistingHoldingsAndOrders(IBrokerage brokerage, IAlgorithm algorithm, SetupHandlerParameters parameters)
        {
            var supportedSecurityTypes = new HashSet<SecurityType>
            {
                SecurityType.Equity, SecurityType.Forex, SecurityType.Cfd, SecurityType.Option, SecurityType.Future, SecurityType.FutureOption, SecurityType.IndexOption, SecurityType.Crypto, SecurityType.CryptoFuture
            };

            Log.Trace("BrokerageSetupHandler.Setup(): Fetching open orders from brokerage...");
            try
            {
                GetOpenOrders(algorithm, parameters.ResultHandler, parameters.TransactionHandler, brokerage, supportedSecurityTypes);
            }
            catch (Exception err)
            {
                Log.Error(err);
                AddInitializationError("Error getting open orders from brokerage: " + err.Message, err);
                return false;
            }

            Log.Trace("BrokerageSetupHandler.Setup(): Fetching holdings from brokerage...");
            try
            {
                var utcNow = DateTime.UtcNow;

                // populate the algorithm with the account's current holdings
                var holdings = brokerage.GetAccountHoldings();

                // add options first to ensure raw data normalization mode is set on the equity underlyings
                foreach (var holding in holdings.OrderByDescending(x => x.Type))
                {
                    Log.Trace("BrokerageSetupHandler.Setup(): Has existing holding: " + holding);

                    // verify existing holding security type
                    if (!supportedSecurityTypes.Contains(holding.Type))
                    {
                        Log.Error("BrokerageSetupHandler.Setup(): Unsupported security type: " + holding.Type + "-" + holding.Symbol.Value);
                        AddInitializationError("Found unsupported security type in existing brokerage holdings: " + holding.Type + ". " +
                            "QuantConnect currently supports the following security types: " + string.Join(",", supportedSecurityTypes));

                        // keep aggregating these errors
                        continue;
                    }

                    AddUnrequestedSecurity(algorithm, holding.Symbol);

                    var security = algorithm.Securities[holding.Symbol];
                    var exchangeTime = utcNow.ConvertFromUtc(security.Exchange.TimeZone);

                    security.Holdings.SetHoldings(holding.AveragePrice, holding.Quantity);

                    if (holding.MarketPrice == 0)
                    {
                        // try warming current market price
                        holding.MarketPrice = algorithm.GetLastKnownPrice(security)?.Price ?? 0;
                    }

                    if (holding.MarketPrice != 0)
                    {
                        security.SetMarketPrice(new TradeBar
                        {
                            Time = exchangeTime,
                            Open = holding.MarketPrice,
                            High = holding.MarketPrice,
                            Low = holding.MarketPrice,
                            Close = holding.MarketPrice,
                            Volume = 0,
                            Symbol = holding.Symbol,
                            DataType = MarketDataType.TradeBar
                        });
                    }
                }
            }
            catch (Exception err)
            {
                Log.Error(err);
                AddInitializationError("Error getting account holdings from brokerage: " + err.Message, err);
                return false;
            }

            return true;
        }

        private Security AddUnrequestedSecurity(IAlgorithm algorithm, Symbol symbol)
        {
            if (!algorithm.Securities.TryGetValue(symbol, out Security security))
            {
                var resolution = algorithm.UniverseSettings.Resolution;
                var fillForward = algorithm.UniverseSettings.FillForward;
                var leverage = algorithm.UniverseSettings.Leverage;
                var extendedHours = algorithm.UniverseSettings.ExtendedMarketHours;

                if (!_notifiedUniverseSettingsUsed)
                {
                    // let's just send the message once
                    _notifiedUniverseSettingsUsed = true;
                    algorithm.Debug($"Will use UniverseSettings for automatically added securities for open orders and holdings. UniverseSettings:" +
                        $" Resolution = {resolution}; Leverage = {leverage}; FillForward = {fillForward}; ExtendedHours = {extendedHours}");
                }

                Log.Trace("BrokerageSetupHandler.Setup(): Adding unrequested security: " + symbol.Value);

                if (symbol.SecurityType.IsOption())
                {
                    // add current option contract to the system
                    security = algorithm.AddOptionContract(symbol, resolution, fillForward, leverage, extendedHours);
                }
                else if (symbol.SecurityType == SecurityType.Future)
                {
                    // add current future contract to the system
                    security = algorithm.AddFutureContract(symbol, resolution, fillForward, leverage, extendedHours);
                }
                else
                {
                    // for items not directly requested set leverage to 1 and at the min resolution
                    security = algorithm.AddSecurity(symbol.SecurityType, symbol.Value, resolution, symbol.ID.Market, fillForward, leverage, extendedHours);
                }
            }
            return security;
        }

        /// <summary>
        /// Get the open orders from a brokerage. Adds <see cref="Orders.Order"/> and <see cref="Orders.OrderTicket"/> to the transaction handler
        /// </summary>
        /// <param name="algorithm">Algorithm instance</param>
        /// <param name="resultHandler">The configured result handler</param>
        /// <param name="transactionHandler">The configurated transaction handler</param>
        /// <param name="brokerage">Brokerage output instance</param>
        /// <param name="supportedSecurityTypes">The list of supported security types</param>
        protected void GetOpenOrders(IAlgorithm algorithm, IResultHandler resultHandler, ITransactionHandler transactionHandler, IBrokerage brokerage,
            HashSet<SecurityType> supportedSecurityTypes)
        {
            // populate the algorithm with the account's outstanding orders
            var openOrders = brokerage.GetOpenOrders();

            // add options first to ensure raw data normalization mode is set on the equity underlyings
            foreach (var order in openOrders.OrderByDescending(x => x.SecurityType))
            {
                transactionHandler.AddOpenOrder(order, algorithm);

                Log.Trace($"BrokerageSetupHandler.Setup(): Has open order: {order}");
                resultHandler.DebugMessage($"BrokerageSetupHandler.Setup(): Open order detected.  Creating order tickets for open order {order.Symbol.Value} with quantity {order.Quantity}. Beware that this order ticket may not accurately reflect the quantity of the order if the open order is partially filled.");

                // verify existing holding security type
                if (!supportedSecurityTypes.Contains(order.SecurityType))
                {
                    Log.Error("BrokerageSetupHandler.Setup(): Unsupported security type: " + order.SecurityType + "-" + order.Symbol.Value);
                    AddInitializationError("Found unsupported security type in existing brokerage open orders: " + order.SecurityType + ". " +
                                           "QuantConnect currently supports the following security types: " + string.Join(",", supportedSecurityTypes));

                    // keep aggregating these errors
                    continue;
                }
                var security = AddUnrequestedSecurity(algorithm, order.Symbol);
                order.PriceCurrency = security?.SymbolProperties.QuoteCurrency;
            }
        }

        /// <summary>
        /// Adds initialization error to the Errors list
        /// </summary>
        /// <param name="message">The error message to be added</param>
        /// <param name="inner">The inner exception being wrapped</param>
        private void AddInitializationError(string message, Exception inner = null)
        {
            Errors.Add(new AlgorithmSetupException("During the algorithm initialization, the following exception has occurred: " + message, inner));
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            _factory?.DisposeSafely();

            if (_dataQueueHandlerBrokerage != null)
            {
                if (_dataQueueHandlerBrokerage.IsConnected)
                {
                    _dataQueueHandlerBrokerage.Disconnect();
                }
                _dataQueueHandlerBrokerage.DisposeSafely();
            }
            else
            {
                var dataQueueHandler = Composer.Instance.GetPart<IDataQueueHandler>();
                if (dataQueueHandler != null)
                {
                    Log.Trace($"BrokerageSetupHandler.Setup(): Found data queue handler to dispose: {dataQueueHandler.GetType()}");
                    dataQueueHandler.DisposeSafely();
                }
                else
                {
                    Log.Trace("BrokerageSetupHandler.Setup(): did not find any data queue handler to dispose");
                }
            }
        }

        private void PreloadDataQueueHandler(LiveNodePacket liveJob, IAlgorithm algorithm, IBrokerageFactory factory)
        {
            // preload the data queue handler using custom BrokerageFactory attribute
            var dataQueueHandlerType = Assembly.GetAssembly(typeof(Brokerage))
                .GetTypes()
                .FirstOrDefault(x =>
                    x.FullName != null &&
                    x.FullName.EndsWith(liveJob.DataQueueHandler) &&
                    x.HasAttribute(typeof(BrokerageFactoryAttribute)));

            if (dataQueueHandlerType != null)
            {
                var attribute = dataQueueHandlerType.GetCustomAttribute<BrokerageFactoryAttribute>();

                // only load the data queue handler if the factory is different from our brokerage factory
                if (attribute.Type != factory.GetType())
                {
                    var brokerageFactory = (BrokerageFactory)Activator.CreateInstance(attribute.Type);

                    // copy the brokerage data (usually credentials)
                    foreach (var kvp in brokerageFactory.BrokerageData)
                    {
                        if (!liveJob.BrokerageData.ContainsKey(kvp.Key))
                        {
                            liveJob.BrokerageData.Add(kvp.Key, kvp.Value);
                        }
                    }

                    // create the data queue handler and add it to composer
                    _dataQueueHandlerBrokerage = brokerageFactory.CreateBrokerage(liveJob, algorithm);

                    // open connection for subscriptions
                    _dataQueueHandlerBrokerage.Connect();
                }
            }
        }
    }
}
