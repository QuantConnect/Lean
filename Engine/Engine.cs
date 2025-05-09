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

using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using QuantConnect.AlgorithmFactory.Python.Wrappers;
using QuantConnect.Brokerages;
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Exceptions;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.HistoricalData;
using QuantConnect.Lean.Engine.Setup;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Packets;
using QuantConnect.Securities;
using QuantConnect.Util;
using static QuantConnect.StringExtensions;

namespace QuantConnect.Lean.Engine
{
    /// <summary>
    /// LEAN ALGORITHMIC TRADING ENGINE: ENTRY POINT.
    ///
    /// The engine loads new tasks, create the algorithms and threads, and sends them
    /// to Algorithm Manager to be executed. It is the primary operating loop.
    /// </summary>
    public class Engine
    {
        private bool _historyStartDateLimitedWarningEmitted;
        private bool _historyNumericalPrecisionLimitedWarningEmitted;
        private readonly bool _liveMode;
        private readonly Task<MarketHoursDatabase> _marketHoursDatabaseTask;

        /// <summary>
        /// Gets the configured system handlers for this engine instance
        /// </summary>
        public LeanEngineSystemHandlers SystemHandlers { get; }

        /// <summary>
        /// Gets the configured algorithm handlers for this engine instance
        /// </summary>
        public LeanEngineAlgorithmHandlers AlgorithmHandlers { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Engine"/> class using the specified handlers
        /// </summary>
        /// <param name="systemHandlers">The system handlers for controlling acquisition of jobs, messaging, and api calls</param>
        /// <param name="algorithmHandlers">The algorithm handlers for managing algorithm initialization, data, results, transaction, and real time events</param>
        /// <param name="liveMode">True when running in live mode, false otherwise</param>
        public Engine(LeanEngineSystemHandlers systemHandlers, LeanEngineAlgorithmHandlers algorithmHandlers, bool liveMode)
        {
            _liveMode = liveMode;
            SystemHandlers = systemHandlers;
            AlgorithmHandlers = algorithmHandlers;
            _marketHoursDatabaseTask = Task.Run(StaticInitializations);
        }

        /// <summary>
        /// Runs a single backtest/live job from the job queue
        /// </summary>
        /// <param name="job">The algorithm job to be processed</param>
        /// <param name="manager">The algorithm manager instance</param>
        /// <param name="assemblyPath">The path to the algorithm's assembly</param>
        /// <param name="workerThread">The worker thread instance</param>
        public void Run(AlgorithmNodePacket job, AlgorithmManager manager, string assemblyPath, WorkerThread workerThread)
        {
            var algorithm = default(IAlgorithm);
            var algorithmManager = manager;

            try
            {
                Log.Trace($"Engine.Run(): Resource limits '{job.Controls.CpuAllocation}' CPUs. {job.Controls.RamAllocation} MB RAM.");
                TextSubscriptionDataSourceReader.SetCacheSize((int) (job.RamAllocation * 0.4));

                //Reset thread holders.
                var initializeComplete = false;

                //-> Initialize messaging system
                SystemHandlers.Notify.SetAuthentication(job);

                //-> Set the result handler type for this algorithm job, and launch the associated result thread.
                AlgorithmHandlers.Results.Initialize(new (job, SystemHandlers.Notify, SystemHandlers.Api, AlgorithmHandlers.Transactions, AlgorithmHandlers.MapFileProvider));

                IBrokerage brokerage = null;
                DataManager dataManager = null;
                var synchronizer = _liveMode ? new LiveSynchronizer() : new Synchronizer();
                try
                {
                    // we get the mhdb before creating the algorithm instance,
                    // since the algorithm constructor will use it
                    var marketHoursDatabase = _marketHoursDatabaseTask.Result;

                    AlgorithmHandlers.Setup.WorkerThread = workerThread;

                    // Save algorithm to cache, load algorithm instance:
                    algorithm = AlgorithmHandlers.Setup.CreateAlgorithmInstance(job, assemblyPath);

                    algorithm.ProjectId = job.ProjectId;

                    // Set algorithm in ILeanManager
                    SystemHandlers.LeanManager.SetAlgorithm(algorithm);

                    // initialize the object store
                    AlgorithmHandlers.ObjectStore.Initialize(job.UserId, job.ProjectId, job.UserToken, job.Controls);

                    // initialize the data permission manager
                    AlgorithmHandlers.DataPermissionsManager.Initialize(job);

                    // notify the user of any errors w/ object store persistence
                    AlgorithmHandlers.ObjectStore.ErrorRaised += (sender, args) => algorithm.Debug($"ObjectStore Persistence Error: {args.Error.Message}");

                    // set the order processor on the transaction manager,needs to be done before initializing the brokerage which might start using it
                    algorithm.Transactions.SetOrderProcessor(AlgorithmHandlers.Transactions);

                    // Initialize the brokerage
                    IBrokerageFactory factory;
                    brokerage = AlgorithmHandlers.Setup.CreateBrokerage(job, algorithm, out factory);

                    // forward brokerage message events to the result handler
                    brokerage.Message += (_, e) => AlgorithmHandlers.Results.BrokerageMessage(e);

                    var symbolPropertiesDatabase = SymbolPropertiesDatabase.FromDataFolder();
                    var mapFilePrimaryExchangeProvider = new MapFilePrimaryExchangeProvider(AlgorithmHandlers.MapFileProvider);
                    var registeredTypesProvider = new RegisteredSecurityDataTypesProvider();
                    var securityService = new SecurityService(algorithm.Portfolio.CashBook,
                        marketHoursDatabase,
                        symbolPropertiesDatabase,
                        algorithm,
                        registeredTypesProvider,
                        new SecurityCacheProvider(algorithm.Portfolio),
                        mapFilePrimaryExchangeProvider,
                        algorithm);

                    algorithm.Securities.SetSecurityService(securityService);

                    dataManager = new DataManager(AlgorithmHandlers.DataFeed,
                        new UniverseSelection(
                            algorithm,
                            securityService,
                            AlgorithmHandlers.DataPermissionsManager,
                            AlgorithmHandlers.DataProvider),
                        algorithm,
                        algorithm.TimeKeeper,
                        marketHoursDatabase,
                        _liveMode,
                        registeredTypesProvider,
                        AlgorithmHandlers.DataPermissionsManager);

                    algorithm.SubscriptionManager.SetDataManager(dataManager);

                    synchronizer.Initialize(algorithm, dataManager);

                    // Set the algorithm's object store before initializing the data feed, which might use it
                    algorithm.SetObjectStore(AlgorithmHandlers.ObjectStore);

                    // Initialize the data feed before we initialize so he can intercept added securities/universes via events
                    AlgorithmHandlers.DataFeed.Initialize(
                        algorithm,
                        job,
                        AlgorithmHandlers.Results,
                        AlgorithmHandlers.MapFileProvider,
                        AlgorithmHandlers.FactorFileProvider,
                        AlgorithmHandlers.DataProvider,
                        dataManager,
                        (IDataFeedTimeProvider) synchronizer,
                        AlgorithmHandlers.DataPermissionsManager.DataChannelProvider);

                    // set the history provider before setting up the algorithm
                    var historyProvider = GetHistoryProvider();
                    historyProvider.SetBrokerage(brokerage);
                    historyProvider.Initialize(
                        new HistoryProviderInitializeParameters(
                            job,
                            SystemHandlers.Api,
                            AlgorithmHandlers.DataProvider,
                            AlgorithmHandlers.DataCacheProvider,
                            AlgorithmHandlers.MapFileProvider,
                            AlgorithmHandlers.FactorFileProvider,
                            progress =>
                            {
                                // send progress updates to the result handler only during initialization
                                if (!algorithm.GetLocked() || algorithm.IsWarmingUp)
                                {
                                    AlgorithmHandlers.Results.SendStatusUpdate(AlgorithmStatus.History,
                                        Invariant($"Processing history {progress}%..."));
                                }
                            },
                            // disable parallel history requests for live trading
                            parallelHistoryRequestsEnabled: !_liveMode,
                            dataPermissionManager: AlgorithmHandlers.DataPermissionsManager,
                            objectStore: algorithm.ObjectStore,
                            algorithmSettings: algorithm.Settings
                        )
                    );

                    historyProvider.InvalidConfigurationDetected += (sender, args) => { AlgorithmHandlers.Results.ErrorMessage(args.Message); };
                    historyProvider.DownloadFailed += (sender, args) => { AlgorithmHandlers.Results.ErrorMessage(args.Message, args.StackTrace); };
                    historyProvider.ReaderErrorDetected += (sender, args) => { AlgorithmHandlers.Results.RuntimeError(args.Message, args.StackTrace); };

                    Composer.Instance.AddPart(historyProvider);
                    algorithm.HistoryProvider = historyProvider;

                    // initialize the default brokerage message handler
                    algorithm.BrokerageMessageHandler = factory.CreateBrokerageMessageHandler(algorithm, job, SystemHandlers.Api);

                    //Initialize the internal state of algorithm and job: executes the algorithm.Initialize() method.
                    initializeComplete = AlgorithmHandlers.Setup.Setup(new SetupHandlerParameters(dataManager.UniverseSelection, algorithm,
                        brokerage, job, AlgorithmHandlers.Results, AlgorithmHandlers.Transactions, AlgorithmHandlers.RealTime,
                        AlgorithmHandlers.DataCacheProvider, AlgorithmHandlers.MapFileProvider));

                    // set this again now that we've actually added securities
                    AlgorithmHandlers.Results.SetAlgorithm(algorithm, AlgorithmHandlers.Setup.StartingPortfolioValue);

                    //If there are any reasons it failed, pass these back to the IDE.
                    if (!initializeComplete || AlgorithmHandlers.Setup.Errors.Count > 0)
                    {
                        initializeComplete = false;
                        //Get all the error messages: internal in algorithm and external in setup handler.
                        var errorMessage = string.Join(",", algorithm.ErrorMessages);
                        string stackTrace = "";
                        errorMessage += string.Join(",", AlgorithmHandlers.Setup.Errors.Select(e =>
                        {
                            var message = e.Message;
                            if (e.InnerException != null)
                            {
                                var interpreter = StackExceptionInterpreter.Instance.Value;
                                var err = interpreter.Interpret(e.InnerException);
                                var stackMessage = interpreter.GetExceptionMessageHeader(err);
                                message += stackMessage;
                                stackTrace += stackMessage;
                            }
                            return message;
                        }));
                        Log.Error("Engine.Run(): " + errorMessage);
                        AlgorithmHandlers.Results.RuntimeError(errorMessage, stackTrace);
                        SystemHandlers.Api.SetAlgorithmStatus(job.AlgorithmId, AlgorithmStatus.RuntimeError, errorMessage);
                    }
                }
                catch (Exception err)
                {
                    Log.Error(err);

                    // for python we don't add the ugly pythonNet stack trace
                    var stackTrace = job.Language == Language.Python ? err.Message : err.ToString();

                    var runtimeMessage = "Algorithm.Initialize() Error: " + err.Message + " Stack Trace: " + stackTrace;
                    AlgorithmHandlers.Results.RuntimeError(runtimeMessage, stackTrace);
                    SystemHandlers.Api.SetAlgorithmStatus(job.AlgorithmId, AlgorithmStatus.RuntimeError, runtimeMessage);
                }


                var historyProviderName = algorithm?.HistoryProvider != null ? algorithm.HistoryProvider.GetType().FullName : string.Empty;
                // log the job endpoints
                Log.Trace($"JOB HANDLERS:{Environment.NewLine}" +
                    $"         DataFeed:             {AlgorithmHandlers.DataFeed.GetType().FullName}{Environment.NewLine}" +
                    $"         Setup:                {AlgorithmHandlers.Setup.GetType().FullName}{Environment.NewLine}" +
                    $"         RealTime:             {AlgorithmHandlers.RealTime.GetType().FullName}{Environment.NewLine}" +
                    $"         Results:              {AlgorithmHandlers.Results.GetType().FullName}{Environment.NewLine}" +
                    $"         Transactions:         {AlgorithmHandlers.Transactions.GetType().FullName}{Environment.NewLine}" +
                    $"         Object Store:         {AlgorithmHandlers.ObjectStore.GetType().FullName}{Environment.NewLine}" +
                    $"         History Provider:     {historyProviderName}{Environment.NewLine}" +
                    $"         Brokerage:            {brokerage?.GetType().FullName}{Environment.NewLine}" +
                    $"         Data Provider:        {AlgorithmHandlers.DataProvider.GetType().FullName}{Environment.NewLine}");

                //-> Using the job + initialization: load the designated handlers:
                if (initializeComplete)
                {
                    // notify the LEAN manager that the algorithm is initialized and starting
                    SystemHandlers.LeanManager.OnAlgorithmStart();

                    //-> Reset the backtest stopwatch; we're now running the algorithm.
                    var startTime = DateTime.UtcNow;

                    //Set algorithm as locked; set it to live mode if we're trading live, and set it to locked for no further updates.
                    algorithm.SetAlgorithmId(job.AlgorithmId);
                    algorithm.SetLocked();

                    //Load the associated handlers for transaction and realtime events:
                    AlgorithmHandlers.Transactions.Initialize(algorithm, brokerage, AlgorithmHandlers.Results);
                    try
                    {
                        AlgorithmHandlers.RealTime.Setup(algorithm, job, AlgorithmHandlers.Results, SystemHandlers.Api, algorithmManager.TimeLimit);

                        // wire up the brokerage message handler
                        brokerage.Message += (sender, message) =>
                        {
                            algorithm.BrokerageMessageHandler.HandleMessage(message);

                            // fire brokerage message events
                            algorithm.OnBrokerageMessage(message);
                            switch (message.Type)
                            {
                                case BrokerageMessageType.Disconnect:
                                    algorithm.OnBrokerageDisconnect();
                                    break;
                                case BrokerageMessageType.Reconnect:
                                    algorithm.OnBrokerageReconnect();
                                    break;
                            }
                        };

                        // Result manager scanning message queue: (started earlier)
                        AlgorithmHandlers.Results.DebugMessage(
                            $"Launching analysis for {job.AlgorithmId} with LEAN Engine v{Globals.Version}");

                        //Create a new engine isolator class
                        var isolator = new Isolator();

                        // Execute the Algorithm Code:
                        var complete = isolator.ExecuteWithTimeLimit(AlgorithmHandlers.Setup.MaximumRuntime, algorithmManager.TimeLimit.IsWithinLimit, () =>
                        {
                            try
                            {
                                //Run Algorithm Job:
                                // -> Using this Data Feed,
                                // -> Send Orders to this TransactionHandler,
                                // -> Send Results to ResultHandler.
                                algorithmManager.Run(job, algorithm, synchronizer, AlgorithmHandlers.Transactions, AlgorithmHandlers.Results, AlgorithmHandlers.RealTime, SystemHandlers.LeanManager, isolator.CancellationTokenSource);
                            }
                            catch (Exception err)
                            {
                                algorithm.SetRuntimeError(err, "AlgorithmManager.Run");
                                return;
                            }

                            Log.Trace("Engine.Run(): Exiting Algorithm Manager");
                        }, job.Controls.RamAllocation, workerThread:workerThread, sleepIntervalMillis: algorithm.LiveMode ? 10000 : 1000);

                        if (!complete)
                        {
                            Log.Error("Engine.Main(): Failed to complete in time: " + AlgorithmHandlers.Setup.MaximumRuntime.ToStringInvariant("F"));
                            throw new Exception("Failed to complete algorithm within " + AlgorithmHandlers.Setup.MaximumRuntime.ToStringInvariant("F")
                                + " seconds. Please make it run faster.");
                        }
                    }
                    catch (Exception err)
                    {
                        //Error running the user algorithm: purge datafeed, send error messages, set algorithm status to failed.
                        algorithm.SetRuntimeError(err, "Engine Isolator");
                    }

                    // Algorithm runtime error:
                    if (algorithm.RunTimeError != null)
                    {
                        HandleAlgorithmError(job, algorithm.RunTimeError);
                    }

                    // notify the LEAN manager that the algorithm has finished
                    SystemHandlers.LeanManager.OnAlgorithmEnd();

                    try
                    {
                        var csvTransactionsFileName = Config.Get("transaction-log");
                        if (!string.IsNullOrEmpty(csvTransactionsFileName))
                        {
                            SaveListOfTrades(AlgorithmHandlers.Transactions, csvTransactionsFileName);
                        }

                        if (!_liveMode)
                        {
                            //Diagnostics Completed, Send Result Packet:
                            var totalSeconds = (DateTime.UtcNow - startTime).TotalSeconds;
                            var dataPoints = algorithmManager.DataPoints + algorithm.HistoryProvider.DataPointCount;
                            var kps = dataPoints / (double) 1000 / totalSeconds;
                            AlgorithmHandlers.Results.DebugMessage($"Algorithm Id:({job.AlgorithmId}) completed in {totalSeconds:F2} seconds at {kps:F0}k data points per second. Processing total of {dataPoints:N0} data points.");
                        }
                    }
                    catch (Exception err)
                    {
                        Log.Error(err, "Error sending analysis results");
                    }

                    //Before we return, send terminate commands to close up the threads
                    AlgorithmHandlers.Transactions.Exit();
                    AlgorithmHandlers.RealTime.Exit();
                    dataManager?.RemoveAllSubscriptions();
                    workerThread?.Dispose();
                }

                synchronizer.DisposeSafely();
                // Close data feed, alphas. Could be running even if algorithm initialization failed
                AlgorithmHandlers.DataFeed.Exit();

                //Close result handler:
                AlgorithmHandlers.Results.Exit();

                //Wait for the threads to complete:
                var millisecondInterval = 10;
                var millisecondTotalWait = 0;
                while ((AlgorithmHandlers.Results.IsActive
                    || (AlgorithmHandlers.Transactions != null && AlgorithmHandlers.Transactions.IsActive)
                    || (AlgorithmHandlers.DataFeed != null && AlgorithmHandlers.DataFeed.IsActive)
                    || (AlgorithmHandlers.RealTime != null && AlgorithmHandlers.RealTime.IsActive))
                    && millisecondTotalWait < 30*1000)
                {
                    Thread.Sleep(millisecondInterval);
                    if (millisecondTotalWait % (millisecondInterval * 10) == 0)
                    {
                        Log.Trace("Waiting for threads to exit...");
                    }
                    millisecondTotalWait += millisecondInterval;
                }

                if (brokerage != null)
                {
                    Log.Trace("Engine.Run(): Disconnecting from brokerage...");
                    brokerage.Disconnect();
                    brokerage.Dispose();
                }
                if (AlgorithmHandlers.Setup != null)
                {
                    Log.Trace("Engine.Run(): Disposing of setup handler...");
                    AlgorithmHandlers.Setup.Dispose();
                }

                Log.Trace("Engine.Main(): Analysis Completed and Results Posted.");
            }
            catch (Exception err)
            {
                Log.Error(err, "Error running algorithm");
            }
            finally
            {
                //No matter what for live mode; make sure we've set algorithm status in the API for "not running" conditions:
                if (_liveMode && algorithmManager.State != AlgorithmStatus.Running && algorithmManager.State != AlgorithmStatus.RuntimeError)
                    SystemHandlers.Api.SetAlgorithmStatus(job.AlgorithmId, algorithmManager.State);

                AlgorithmHandlers.Results.Exit();
                AlgorithmHandlers.DataFeed.Exit();
                AlgorithmHandlers.Transactions.Exit();
                AlgorithmHandlers.RealTime.Exit();
                AlgorithmHandlers.DataMonitor.Exit();
                (algorithm as AlgorithmPythonWrapper)?.DisposeSafely();
            }
        }

        /// <summary>
        /// Handle an error in the algorithm.Run method.
        /// </summary>
        /// <param name="job">Job we're processing</param>
        /// <param name="err">Error from algorithm stack</param>
        private void HandleAlgorithmError(AlgorithmNodePacket job, Exception err)
        {
            AlgorithmHandlers.DataFeed?.Exit();
            if (AlgorithmHandlers.Results != null)
            {
                var message = $"Runtime Error: {err.Message}";
                Log.Trace("Engine.Run(): Sending runtime error to user...");
                AlgorithmHandlers.Results.LogMessage(message);

                // for python we don't add the ugly pythonNet stack trace
                var stackTrace = job.Language == Language.Python ? err.Message : err.ToString();

                AlgorithmHandlers.Results.RuntimeError(message, stackTrace);
                SystemHandlers.Api.SetAlgorithmStatus(job.AlgorithmId, AlgorithmStatus.RuntimeError, $"{message} Stack Trace: {stackTrace}");
            }
        }

        /// <summary>
        /// Load the history provider from the Composer
        /// </summary>
        private HistoryProviderManager GetHistoryProvider()
        {
            var provider = new HistoryProviderManager();

            provider.InvalidConfigurationDetected += (sender, args) => { AlgorithmHandlers.Results.ErrorMessage(args.Message); };
            provider.NumericalPrecisionLimited += (sender, args) =>
            {
                if (!_historyNumericalPrecisionLimitedWarningEmitted)
                {
                    _historyNumericalPrecisionLimitedWarningEmitted = true;
                    AlgorithmHandlers.Results.DebugMessage("Warning: when performing history requests, the start date will be adjusted if there are numerical precision errors in the factor files.");
                }
            };
            provider.StartDateLimited += (sender, args) =>
            {
                if (!_historyStartDateLimitedWarningEmitted)
                {
                    _historyStartDateLimitedWarningEmitted = true;
                    AlgorithmHandlers.Results.DebugMessage("Warning: when performing history requests, the start date will be adjusted if it is before the first known date for the symbol.");
                }
            };
            provider.DownloadFailed += (sender, args) => { AlgorithmHandlers.Results.ErrorMessage(args.Message, args.StackTrace); };
            provider.ReaderErrorDetected += (sender, args) => { AlgorithmHandlers.Results.RuntimeError(args.Message, args.StackTrace); };

            return provider;
        }

        /// <summary>
        /// Save a list of trades to disk for a given path
        /// </summary>
        /// <param name="transactions">Transactions list via an OrderProvider</param>
        /// <param name="csvFileName">File path to create</param>
        private static void SaveListOfTrades(IOrderProvider transactions, string csvFileName)
        {
            var orders = transactions.GetOrders(x => x.Status.IsFill());

            var path = Path.GetDirectoryName(csvFileName);
            if (path != null && !Directory.Exists(path))
                Directory.CreateDirectory(path);

            using (var writer = new StreamWriter(csvFileName))
            {
                foreach (var order in orders)
                {
                    var line = Invariant($"{order.Time.ToStringInvariant("yyyy-MM-dd HH:mm:ss")},") +
                               Invariant($"{order.Symbol.Value},{order.Direction},{order.Quantity},{order.Price}");
                    writer.WriteLine(line);
                }
            }
        }

        /// <summary>
        /// Initialize slow static variables
        /// </summary>
        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        private static MarketHoursDatabase StaticInitializations()
        {
            SymbolPropertiesDatabase.FromDataFolder();
            // This is slow because it create all static timezones
            var nyTime = TimeZones.NewYork;
            // slow because if goes to disk and parses json
            return MarketHoursDatabase.FromDataFolder();
        }

    } // End Algorithm Node Core Thread
} // End Namespace
