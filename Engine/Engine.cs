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

/**********************************************************
* USING NAMESPACES
**********************************************************/
using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using QuantConnect.Brokerages;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.RealTime;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Lean.Engine.Setup;
using QuantConnect.Lean.Engine.TransactionHandlers;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Packets;
using QuantConnect.Configuration;

namespace QuantConnect.Lean.Engine 
{
    /******************************************************** 
    * CLASS DEFINITIONS
    *********************************************************/
    /// <summary>
    /// LEAN ALGORITHMIC TRADING ENGINE: ENTRY POINT.
    /// 
    /// The engine loads new tasks, create the algorithms and threads, and sends them 
    /// to Algorithm Manager to be executed. It is the primary operating loop.
    /// </summary>
    public class Engine 
    {
        /******************************************************** 
        * CLASS PRIVATE VARIABLES
        *********************************************************/
        private static bool _liveMode = Config.GetBool("livemode");
        private static bool _local = Config.GetBool("local");
        private static IBrokerage _brokerage = new Brokerage();

        /******************************************************** 
        * CLASS PUBLIC VARIABLES
        *********************************************************/
        /// <summary>
        /// Datafeed handler creates local, live, historical data feed management all through specific dedicated DLL's.
        /// </summary>
        public static IDataFeed DataFeed;

        /// <summary>
        /// Result handler pushes result messages to live API, backtesting API or console for local.
        /// </summary>
        public static IResultHandler ResultHandler;

        /// <summary>
        /// Transaction handler pushes trades to historical models, or live market with brokerage.
        /// </summary>
        public static ITransactionHandler TransactionHandler;

        /// <summary>
        /// Setup handler initializes all backtest requirements and sets up the algorithms internal state.
        /// </summary>
        public static ISetupHandler SetupHandler;

        /// <summary>
        /// RealTime events handlers trigger function callbacks at specific times during the day for the algorithms. 
        /// Works for backtests and live trading.
        /// </summary>
        public static IRealTimeHandler RealTimeHandler;

        /// <summary>
        /// Brokerage class holds manages the connection, transaction processing and data retrieval from specific broker endpoints.
        /// </summary>
        public static IBrokerage Brokerage
        {
            get
            {
                return _brokerage;
            }
        }

        /// <summary>
        /// Notification/messaging handler for pushing messages to the proper endpoint.
        /// </summary>
        public static IMessagingHandler Notify;

        /// <summary>
        /// Task requester / job queue handler for running the next algorithm task.
        /// </summary>
        public static IQueueHandler Queue;

        /// <summary>
        /// Algorithm API handler for setting the per user restrictions on algorithm behaviour where applicable.
        /// </summary>
        public static IApi Api;


        /******************************************************** 
        * CLASS PROPERTIES
        *********************************************************/
        /// <summary>
        /// Are we operating this as a local independent node, independent of the cloud.
        /// Running on a local algorithm, and local datasources.
        /// </summary>
        public static bool IsLocal
        {
            get
            {
                return _local;
            }
        }

        /// <summary>
        /// Instance is a micro tagged qc.live.v3 for live use only:
        /// -> Monitor the live job queue, not the backtest queue:
        /// </summary>
        public static bool LiveMode
        {
            get
            {
                return _liveMode;
            }
        }

        /// <summary>
        /// Maximum allowable ram for an algorithm
        /// </summary>
        public static int MaximumRamAllocation
        {
            get
            {
                //Total Physical Ram Available:
                var allocation = 1024;
                var ram = Convert.ToInt32(OS.TotalPhysicalMemory);
                if (ram < allocation)
                {
                    allocation = ram - 200;
                }

                Log.Trace("Engine.MaximumRamAllocation(): Allocated: " + allocation);

                return allocation;
            }
        }

        /******************************************************** 
        * CLASS METHODS
        *********************************************************/
        /// <summary>
        /// Primary Analysis Thread:
        /// </summary>
        public static void Main(string[] args) 
        {
            //Initialize:
            AlgorithmNodePacket job = null;
            var timer = Stopwatch.StartNew();
            var algorithm = default(IAlgorithm);
            
            //Name thread for the profiler:
            Thread.CurrentThread.Name = "Algorithm Analysis Thread";
            Log.Trace("Engine.Main(): Started " + DateTime.Now.ToShortTimeString());
            Log.Trace("Engine.Main(): Memory " + OS.ApplicationMemoryUsed + "Mb-App  " + +OS.TotalPhysicalMemoryUsed + "Mb-Used  " + OS.TotalPhysicalMemory + "Mb-Total");

            //Import external libraries specific to physical server location (cloud/local)
            var catalog = new AggregateCatalog();
            catalog.Catalogs.Add(new DirectoryCatalog(@"../../Extensions"));
            var container = new CompositionContainer(catalog);
            try
            {
                // grab the right export based on configuration
                Notify = container.GetExportedValueByTypeName<IMessagingHandler>(Config.Get("messaging-handler"));
                Queue = container.GetExportedValueByTypeName<IQueueHandler>(Config.Get("queue-handler"));
                Api = container.GetExportedValueByTypeName<IApi>(Config.Get("api-handler")); 
            } 
            catch (CompositionException compositionException)
            { Log.Error("Engine.Main(): Failed to load library: " + compositionException); 
            }

            //Setup packeting, queue and controls system: These don't do much locally.
            Api.Initialize();
            Notify.Initialize();
            Queue.Initialize(_liveMode);

            //Start monitoring the backtest active status:
            var statusPingThread = new Thread(StateCheck.Ping.Run);
            statusPingThread.Start();

            do 
            {
                try
                {
                    //Clean up cache directories:
                    CleanUpDirectories();

                    //Reset algo manager internal variables preparing for a new algorithm.
                    AlgorithmManager.ResetManager();

                    //Reset thread holders.
                    var initializeComplete = false;
                    Thread threadFeed = null;
                    Thread threadTransactions = null;
                    Thread threadResults = null;
                    Thread threadRealTime = null;

                    //-> Pull job from QuantConnect job queue, or, pull local build:
                    var algorithmPath = "";
                    job = Queue.NextJob(out algorithmPath); // Blocking.

                    //-> Initialize messaging system
                    Notify.SetChannel(job.Channel);

                    //-> Reset the backtest stopwatch; we're now running the algorithm.
                    timer.Restart();

                    //-> Create SetupHandler to configure internal algorithm state:
                    SetupHandler = GetSetupHandler(job.SetupEndpoint);

                    //-> Set the result handler type for this algorithm job, and launch the associated result thread.
                    ResultHandler = GetResultHandler(job);
                    threadResults = new Thread(ResultHandler.Run, 0) {Name = "Result Thread"};
                    threadResults.Start();

                    try
                    {
                        // Save algorithm to cache, load algorithm instance:
                        algorithm = SetupHandler.CreateAlgorithmInstance(algorithmPath);

                        //Initialize the internal state of algorithm and job: executes the algorithm.Initialize() method.
                        initializeComplete = SetupHandler.Setup(algorithm, out _brokerage, job);

                        //If there are any reasons it failed, pass these back to the IDE.
                        if (!initializeComplete || algorithm.ErrorMessages.Count > 0 || SetupHandler.Errors.Count > 0)
                        {
                            initializeComplete = false;
                            //Get all the error messages: internal in algorithm and external in setup handler.
                            var errorMessage = String.Join(",", algorithm.ErrorMessages);
                            errorMessage += String.Join(",", SetupHandler.Errors);
                            throw new Exception(errorMessage);
                        }
                    }
                    catch (Exception err)
                    {
                        ResultHandler.RuntimeError("Algorithm.Initialize() Error: " + err.Message, err.StackTrace);
                    }

                    //-> Using the job + initialization: load the designated handlers:
                    if (initializeComplete)
                    {
                        //Set algorithm as locked; set it to live mode if we're trading live, and set it to locked for no further updates.
                        algorithm.SetAlgorithmId(job.AlgorithmId);
                        algorithm.SetLiveMode(LiveMode);
                        algorithm.SetLocked();
                        
                        //Load the associated handlers for data, transaction and realtime events:
                        ResultHandler.SetAlgorithm(algorithm);
                        DataFeed = GetDataFeedHandler(algorithm, _brokerage, job);
                        TransactionHandler = GetTransactionHandler(algorithm, _brokerage, ResultHandler, job);
                        RealTimeHandler = GetRealTimeHandler(algorithm, _brokerage, DataFeed, ResultHandler, job);

                        //Set the error handlers for the brokerage asynchronous errors.
                        SetupHandler.SetupErrorHandler(ResultHandler, _brokerage);

                        //Send status to user the algorithm is now executing.
                        ResultHandler.SendStatusUpdate(job.AlgorithmId, AlgorithmStatus.Running);

                        //Launch the data, transaction and realtime handlers into dedicated threads
                        threadFeed = new Thread(DataFeed.Run, 0) {Name = "DataFeed Thread"};
                        threadTransactions = new Thread(TransactionHandler.Run, 0) { Name = "Transaction Thread" };
                        threadRealTime = new Thread(RealTimeHandler.Run, 0) {Name = "RealTime Thread"};

                        //Launch the data feed, result sending, and transaction models/handlers in separate threads.
                        threadFeed.Start();         // Data feed pushing data packets into thread bridge; 
                        threadTransactions.Start(); // Transaction modeller scanning new order requests
                        threadRealTime.Start();     // RealTime scan time for time based events:
                                                    // Result manager scanning message queue: (started earlier)

                        try
                        {
                            // Execute the Algorithm Code:
                            var complete = Isolator.ExecuteWithTimeLimit(SetupHandler.MaximumRuntime, () =>
                            {
                                try
                                {
                                    //Run Algorithm Job:
                                    // -> Using this Data Feed, 
                                    // -> Send Orders to this TransactionHandler, 
                                    // -> Send Results to ResultHandler.
                                    AlgorithmManager.Run(job, algorithm, DataFeed, TransactionHandler, ResultHandler, SetupHandler, RealTimeHandler);
                                }
                                catch (Exception err)
                                {
                                    //Debugging at this level is difficult, stack trace needed.
                                    Log.Error("Engine.Run(): Error in Algo Manager: " + err.Message + " ST >> " + err.StackTrace);
                                }

                                Log.Trace("Engine.Run(): Exiting Algorithm Manager");

                            }, MaximumRamAllocation);

                            if (!complete)
                            {
                                Log.Error("Engine.Main(): Failed to complete in time: " + SetupHandler.MaximumRuntime.ToString("F"));
                                throw new Exception("Failed to complete algorithm within " + SetupHandler.MaximumRuntime.ToString("F") + " seconds. Please make it run faster.");
                            }

                            // Algorithm runtime error:
                            if (AlgorithmManager.RunTimeError != null)
                            {
                                throw AlgorithmManager.RunTimeError;
                            }
                        } 
                        catch (Exception err) 
                        {
                            //Error running the user algorithm: purge datafeed, send error messages, set algorithm status to failed.
                            Log.Error("Engine.Run(): Breaking out of parent try-catch: " + err.Message + " " + err.StackTrace);
                            if (DataFeed != null) DataFeed.Exit();
                            if (ResultHandler != null)
                            {
                                ResultHandler.RuntimeError("Runtime Error: " + err.Message, err.StackTrace);
                                Api.SetAlgorithmStatus(job.AlgorithmId, AlgorithmStatus.RuntimeError);
                            }
                        }

                        //Send result data back: this entire code block could be rewritten.
                        // todo: - Split up statistics class, its enormous. 
                        // todo: - Make a dedicated Statistics.Benchmark class.
                        // todo: - Elegently manage failure scenarios where no equity present.
                        // todo: - Move all creation and transmission of statistics out of primary engine loop.
                        // todo: - Statistics.Generate(algorithm, resulthandler, transactionhandler);

                        try
                        {
                            var charts = new Dictionary<string, Chart>(ResultHandler.Charts);
                            var orders = new Dictionary<int, Order>(TransactionHandler.Orders);
                            var holdings = new Dictionary<string, Holding>();
                            var statistics = new Dictionary<string, string>();
                            var banner = new Dictionary<string, string>();

                            try
                            {
                                //Generates error when things don't exist (no charting logged, runtime errors in main algo execution)
                                var equity = charts["Strategy Equity"].Series["Equity"].Values;
                                var performance = charts["Strategy Equity"].Series["Daily Performance"].Values;
                                var profitLoss = new SortedDictionary<DateTime,decimal>(algorithm.Transactions.TransactionRecord);
                                statistics = Statistics.Statistics.Generate(equity, profitLoss, performance, SetupHandler.StartingCapital, 252);
                            }
                            catch (Exception err) {
                                Log.Error("Algorithm.Node.Engine(): Error generating result packet: " + err.Message);
                            }

                            //Diagnostics Completed:
                            ResultHandler.DebugMessage("Algorithm Id:(" + job.AlgorithmId + ") completed analysis in " + timer.Elapsed.TotalSeconds.ToString("F2") + " seconds");

                            //Send the result packet:
                            ResultHandler.SendFinalResult(job, orders, algorithm.Transactions.TransactionRecord, holdings, statistics, banner);
                        }
                        catch (Exception err)
                        {
                            Log.Error("Engine.Main(): Error sending analysis result: " + err.Message + "  ST >> " + err.StackTrace);
                        }

                        //Before we return, send terminate commands to close up the threads 
                        timer.Stop();               //Algorithm finished running.
                        TransactionHandler.Exit();
                        DataFeed.Exit();
                        RealTimeHandler.Exit();
                        AlgorithmManager.ResetManager();
                    }

                    //Close result handler:
                    ResultHandler.Exit();

                    //Wait for the threads to complete:
                    Log.Trace("Engine.Main(): Waiting for threads to deactivate...");
                    var ts = Stopwatch.StartNew();
                    while ((ResultHandler.IsActive || (TransactionHandler != null && TransactionHandler.IsActive) || (DataFeed != null && DataFeed.IsActive)) && ts.ElapsedMilliseconds < 60 * 1000)
                    {
                        Thread.Sleep(100); 
                        DataFeed.Exit();
                        Log.Trace("WAITING >> Result: " + ResultHandler.IsActive + " Transaction: " + TransactionHandler.IsActive + " DataFeed: " + DataFeed.IsActive + " RealTime: " + RealTimeHandler.IsActive);
                    }

                    Log.Trace("Engine.Main(): Closing Threads...");
                    if (threadFeed != null && threadFeed.IsAlive) threadFeed.Abort();
                    if (threadTransactions != null && threadTransactions.IsAlive) threadTransactions.Abort();
                    if (threadResults != null && threadResults.IsAlive) threadResults.Abort();
                    Log.Trace("Engine.Main(): Analysis Completed and Results Posted.");
                }
                catch (Exception err)
                {
                    Log.Error("Engine.Main(): Error running algorithm: " + err.Message + " >> " + err.StackTrace);
                }
                finally 
                {
                    //Delete the message from the queue before another worker picks it up:
                    Queue.AcknowledgeJob(job);
                    Log.Trace("Engine.Main(): Packet removed from queue: " + job.AlgorithmId);
                    GC.Collect();
                }

                //If we're running locally will execute just once.
            } while (!IsLocal);

            // Send the exit signal and then kill the thread
            StateCheck.Ping.Exit();
            
            // Make the console window pause so we can read log output before exiting and killing the application completely
            Console.ReadKey();

            //Finally if ping thread still not complete, kill.
            if (statusPingThread != null && statusPingThread.IsAlive) statusPingThread.Abort();
        }



        /// <summary>
        /// Algorithm status monitor reads the central command directive for this algorithm/backtest. When it detects
        /// the backtest has been deleted or cancelled the backtest is aborted.
        /// </summary>
        public static class StateCheck
        {
            /// DB Ping Class
            public static class Ping
            {
                // set to true to break while loop in Run()
                private static bool _exitTriggered;

                /// DB Ping Run Method:
                public static void Run()
                {
                    //Don't run at all if local.
                    if (_local) return;

                    while (!_exitTriggered)
                    {
                        if (AlgorithmManager.AlgorithmId != "" && AlgorithmManager.QuitState == false)
                        {
                            try
                            {
                                //Get the state from the central server:
                                var state = Api.GetAlgorithmStatus(AlgorithmManager.AlgorithmId);
                                AlgorithmManager.SetStatus(state);

                                Log.Debug("StateCheck.Ping.Run(): Algorithm Status: " + state);
                            }
                            catch
                            {
                                Log.Debug("StateCheck.Run(): Error in state check.");
                            }
                        }
                        else
                        {
                            Log.Debug("StateCheck.Ping.Run(): Opted to not ping: " + AlgorithmManager.AlgorithmId + " " + AlgorithmManager.QuitState);
                        }
                        Thread.Sleep(1000);
                    }
                }

                /// <summary>
                /// Send an exit signal to the thread
                /// </summary>
                public static void Exit()
                {
                    _exitTriggered = true;
                }
            }
        }


        /// <summary>
        /// Get an instance of the data feed handler we're requesting for this work.
        /// </summary>
        /// <param name="algorithm">User algorithm to scan for securities</param>
        /// <param name="job">Algorithm Node Packet</param>
        /// <param name="brokerage">Brokerage instance to avoid access token duplication</param>
        /// <returns>Class matching IDataFeed Interface</returns>
        private static IDataFeed GetDataFeedHandler(IAlgorithm algorithm, IBrokerage brokerage, AlgorithmNodePacket job)
        {
            var df = default(IDataFeed);
            switch (job.DataEndpoint) 
            {
                //default:
                ////Backtesting:
                case DataFeedEndpoint.Backtesting:
                    df = new BacktestingDataFeed(algorithm, (BacktestNodePacket)job);
                    Log.Trace("Engine.GetDataFeedHandler(): Selected Backtesting Datafeed");
                    break;

                //Operation from local files:
                case DataFeedEndpoint.FileSystem:
                    df = new FileSystemDataFeed(algorithm, (BacktestNodePacket)job);
                    Log.Trace("Engine.GetDataFeedHandler(): Selected FileSystem Datafeed");
                    break;

                //Live Trading Data Source:
                case DataFeedEndpoint.LiveTrading:
                    df = new PaperTradingDataFeed(algorithm, (LiveNodePacket)job);
                    Log.Trace("Engine.GetDataFeedHandler(): Selected LiveTrading Datafeed");
                    break;

                case DataFeedEndpoint.Test:
                    int fastForward = 100;
                    df = new TestLiveTradingDataFeed(algorithm, (LiveNodePacket) job, fastForward: fastForward);
                    Log.Trace("Engine.GetDataFeedHandler(): Selected Test Datafeed at " + fastForward + "x");
                    break;
            }
            return df;
        }

        /// <summary>
        /// Select the realtime event handler set in the job.
        /// </summary>
        private static IRealTimeHandler GetRealTimeHandler(IAlgorithm algorithm, IBrokerage brokerage, IDataFeed feed, IResultHandler results, AlgorithmNodePacket job)
        {
            var rth = default(IRealTimeHandler);
            switch (job.RealTimeEndpoint)
            { 
                //Don't fire based on system time but virtualized backtesting time.
                case RealTimeEndpoint.Backtesting:
                    Log.Trace("Engine.GetRealTimeHandler(): Selected Backtesting RealTimeEvent Handler");
                    rth = new BacktestingRealTimeHandler(algorithm, job);
                    break;

                // Fire events based on real system clock time.
                case RealTimeEndpoint.LiveTrading:
                    Log.Trace("Engine.GetRealTimeHandler(): Selected LiveTrading RealTimeEvent Handler");
                    rth = new LiveTradingRealTimeHandler(algorithm, feed, results, brokerage, job);
                    break;
            }
            return rth;
        }


        /// <summary>
        /// Get an instance of the transaction handler set by the task.
        /// </summary>
        /// <param name="algorithm">Algorithm instance</param>
        /// <param name="job">Algorithm job packet</param>
        /// <param name="brokerage">Brokerage instance to avoid access token duplication</param>
        /// <param name="results">Results array for sending order events.</param>
        /// <returns>Class matching ITransactionHandler interface</returns>
        private static ITransactionHandler GetTransactionHandler(IAlgorithm algorithm, IBrokerage brokerage, IResultHandler results, AlgorithmNodePacket job)
        {
            ITransactionHandler th;
            switch (job.TransactionEndpoint)
            {
                //Operation from local files:
                default:
                    th = new BacktestingTransactionHandler(algorithm);
                    Log.Trace("Engine.GetTransactionHandler(): Selected Backtesting Transaction Models.");
                    break;

                case TransactionHandlerEndpoint.Tradier:
                    var live = job as LiveNodePacket;
                    Log.Trace("Engine.GetTransactionHandler(): Selected Live Transaction Fills.");
                    th = new TradierTransactionHandler(algorithm, brokerage, results, live.AccountId);
                    break;
            }
            return th;
        }

        /// <summary>
        /// Get an instance of the data feed handler we're requesting for this work.
        /// </summary>
        /// <param name="job">Algorithm Node Packet</param>
        /// <returns>Class Matching IResultHandler Inteface</returns>
        private static IResultHandler GetResultHandler(AlgorithmNodePacket job)
        {
            var rh = default(IResultHandler);
            if (IsLocal) return new ConsoleResultHandler(job);

            switch (job.ResultEndpoint)
            {
                //Local backtesting and live trading result handler route messages to the local console.
                case ResultHandlerEndpoint.Console:
                    Log.Trace("Engine.GetResultHandler(): Selected Console Output.");
                    rh = new ConsoleResultHandler((BacktestNodePacket)job);
                    break;


                // Backtesting route messages to user browser.
                case ResultHandlerEndpoint.Backtesting:
                    Log.Trace("Engine.GetResultHandler(): Selected Backtesting API Result Endpoint.");
                    rh = new BacktestingResultHandler((BacktestNodePacket)job);
                    break;

                // Live trading route messages to user's browser.
                case ResultHandlerEndpoint.LiveTrading:
                    Log.Trace("Engine.GetResultHandler(): Selected Live Trading API Result Endpoint.");
                    rh = new LiveTradingResultHandler((LiveNodePacket)job);
                    break;
            }
            return rh;
        }


        /// <summary>
        /// Get the setup handler for this algorithm, depending on its use case.
        /// </summary>
        /// <param name="setupMethod">Setup handler</param>
        /// <returns>Instance of a setup handler:</returns>
        private static ISetupHandler GetSetupHandler(SetupHandlerEndpoint setupMethod)
        {
            var sh = default(ISetupHandler);
            if (IsLocal) return new ConsoleSetupHandler();

            switch (setupMethod)
            {
                //Setup console handler:
                case SetupHandlerEndpoint.Console:
                    sh = new ConsoleSetupHandler();
                    Log.Trace("Engine.GetSetupHandler(): Selected Console Algorithm Setup Handler.");
                    break;
                //Default, backtesting result handler:
                case SetupHandlerEndpoint.Backtesting:
                    sh = new BacktestingSetupHandler();
                    Log.Trace("Engine.GetSetupHandler(): Selected Backtesting Algorithm Setup Handler.");
                    break;
                case SetupHandlerEndpoint.PaperTrading:
                    sh = new PaperTradingSetupHandler();
                    Log.Trace("Engine.GetSetupHandler(): Selected PaperTrading Algorithm Setup Handler.");
                    break;
                case SetupHandlerEndpoint.Tradier:
                    sh = new TradierSetupHandler();
                    Log.Trace("Engine.GetSetupHandler(): Selected Tradier Algorithm Setup Handler.");
                    break;
            }
            return sh;
        }

        /// <summary>
        /// Setup a private directory structure, clean the caches:
        /// </summary>
        private static void CleanUpDirectories() 
        {
            var baseCache = Directory.GetCurrentDirectory() + "/cache";
            string[] resolutions = { "minute", "second", "tick" };
            var days = 30;
            var filesDeleted = 0;

            try
            {
                if (!Directory.Exists(baseCache)) {
                    //Create new directory structure fresh:
                    Directory.CreateDirectory(baseCache + @"/algorithm");
                    Directory.CreateDirectory(baseCache + @"/data");
                }

                while (OS.DriveSpaceRemaining < 3000 && !IsLocal && days-- >= 0)
                {
                    //Clean out the current structure each loop:
                    var files = Directory.GetFiles(baseCache + @"/algorithm");
                    foreach (var file in files)
                    {
                        File.Delete(file);
                        filesDeleted++;
                    }

                    //Delete the user custom data:
                    files = Directory.GetFiles(baseCache + @"/data");
                    foreach (var file in files)
                    {
                        var info = new FileInfo(file);
                        if (info.LastAccessTime < DateTime.Now.AddDays(days)) File.Delete(file);
                    }

                    //Go through all the resolutions and symbols, and 
                    foreach (var resolution in resolutions)
                    {
                        var directoryLocation = baseCache + @"/data/equity/" + resolution + @"/";
                        if (!Directory.Exists(directoryLocation)) Directory.CreateDirectory(directoryLocation);

                        var directories = Directory.GetDirectories(directoryLocation);
                        foreach (var directory in directories)
                        {
                            files = Directory.GetFiles(directory);
                            foreach (var file in files)
                            {
                                var info = new FileInfo(file);
                                if (info.LastAccessTime < DateTime.Now.AddDays(-days))
                                {
                                    File.Delete(file);
                                    filesDeleted++;
                                }
                            }
                        }
                    }
                }
                Log.Trace("Engine.CleanUpDirectory(): Cleared " + filesDeleted + " files from cache. " + OS.DriveSpaceRemaining + " MB Disk Remaining");
            }
            catch (Exception err)
            {
                //Error cleaning up the directories 
                Log.Error("Engine.CleanUpDirectories(): " + err.Message);
            }
        }

    } // End Algorithm Node Core Thread
    
} // End Namespace
