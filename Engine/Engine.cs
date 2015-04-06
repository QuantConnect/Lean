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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using QuantConnect.Brokerages.Backtesting;
using QuantConnect.Configuration;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.RealTime;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Lean.Engine.Setup;
using QuantConnect.Lean.Engine.TransactionHandlers;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Packets;

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
        private static bool _liveMode = Config.GetBool("live-mode");
        private static bool _local = Config.GetBool("local");
        private static DateTime _version;
        private static IBrokerage _brokerage;
        private const string _collapseMessage = "Unhandled exception breaking past controls and causing collapse of algorithm node. This is likely a memory leak of an external dependency or the underlying OS terminating the LEAN engine.";

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
        public static IJobQueueHandler JobQueue;

        /// <summary>
        /// Algorithm API handler for setting the per user restrictions on algorithm behaviour where applicable.
        /// </summary>
        public static IApi Api;

        /// <summary>
        /// Version of the engine that is running. This is required for retiring old processing 
        /// and live trading nodes during live trading.
        /// </summary>
        public static DateTime Version
        {
            get { return _version; }
        }
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

                if (_liveMode) allocation -= 50;

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
            // Pick an implementation of ILogHandler for the application
            // Using file log handler
            Log.LogHandler = IsLocal
                ? (ILogHandler)new ConsoleLogHandler() 
                : new FileLogHandler("log.txt");

            //Initialize:
            var algorithmPath = "";
            AlgorithmNodePacket job = null;
            var timer = Stopwatch.StartNew();
            var algorithm = default(IAlgorithm);
            _version = DateTime.ParseExact(Config.Get("version", DateTime.Now.ToString(DateFormat.UI)), DateFormat.UI, CultureInfo.InvariantCulture);
            
            //Name thread for the profiler:
            Thread.CurrentThread.Name = "Algorithm Analysis Thread";
            Log.Trace("Engine.Main(): LEAN ALGORITHMIC TRADING ENGINE v" + _version);
            Log.Trace("Engine.Main(): Started " + DateTime.Now.ToShortTimeString());
            Log.Trace("Engine.Main(): Memory " + OS.ApplicationMemoryUsed + "Mb-App  " + +OS.TotalPhysicalMemoryUsed + "Mb-Used  " + OS.TotalPhysicalMemory + "Mb-Total");

            //Import external libraries specific to physical server location (cloud/local)
            var catalog = new AggregateCatalog();
            catalog.Catalogs.Add(new DirectoryCatalog(AppDomain.CurrentDomain.BaseDirectory));
            var container = new CompositionContainer(catalog);
            try
            {
                // grab the right export based on configuration
                Notify = container.GetExportedValueByTypeName<IMessagingHandler>(Config.Get("messaging-handler"));
                JobQueue = container.GetExportedValueByTypeName<IJobQueueHandler>(Config.Get("job-queue-handler"));
                Api = container.GetExportedValueByTypeName<IApi>(Config.Get("api-handler")); 
            } 
            catch (CompositionException compositionException)
            { Log.Error("Engine.Main(): Failed to load library: " + compositionException); 
            }

            //Setup packeting, queue and controls system: These don't do much locally.
            Api.Initialize();
            Notify.Initialize();

            //Start monitoring the backtest active status:
            var statusPingThread = new Thread(StateCheck.Ping.Run);
            statusPingThread.Start();

            do 
            {
                try
                {
                    //Reset algo manager internal variables preparing for a new algorithm.
                    AlgorithmManager.ResetManager();

                    //Reset thread holders.
                    var initializeComplete = false;
                    Thread threadFeed = null;
                    Thread threadTransactions = null;
                    Thread threadResults = null;
                    Thread threadRealTime = null;

                    do
                    {
                        //-> Pull job from QuantConnect job queue, or, pull local build:
                        job = JobQueue.NextJob(out algorithmPath); // Blocking.

                        if (!IsLocal && LiveMode && (job.Version < Version || (job.Version == Version && job.Redelivered)))
                        {
                            //Tiny chance there was an uncontrolled collapse of a server, resulting in an old user task circulating.
                            //In this event kill the old algorithm and leave a message so the user can later review.
                            JobQueue.AcknowledgeJob(job);
                            Api.SetAlgorithmStatus(job.AlgorithmId, AlgorithmStatus.RuntimeError, _collapseMessage);
                            Notify.SetChannel(job.Channel);
                            Notify.RuntimeError(job.AlgorithmId, _collapseMessage);
                            job = null;
                        }
                    } while (job == null);
                    

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
                        var runtimeMessage = "Algorithm.Initialize() Error: " + err.Message + " Stack Trace: " + err.StackTrace;
                        ResultHandler.RuntimeError(runtimeMessage, err.StackTrace);
                        Api.SetAlgorithmStatus(job.AlgorithmId, AlgorithmStatus.RuntimeError, runtimeMessage);
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
                        DataFeed            = GetDataFeedHandler(algorithm, job);
                        TransactionHandler  = GetTransactionHandler(algorithm, _brokerage, ResultHandler, job);
                        RealTimeHandler     = GetRealTimeHandler(algorithm, _brokerage, DataFeed, ResultHandler, job);

                        //Set the error handlers for the brokerage asynchronous errors.
                        SetupHandler.SetupErrorHandler(ResultHandler, _brokerage);

                        //Send status to user the algorithm is now executing.
                        ResultHandler.SendStatusUpdate(job.AlgorithmId, AlgorithmStatus.Running);

                        //Launch the data, transaction and realtime handlers into dedicated threads
                        threadFeed = new Thread(DataFeed.Run, 0) {Name = "DataFeed Thread"};
                        threadTransactions = new Thread(TransactionHandler.Run, 0) {Name = "Transaction Thread"};
                        threadRealTime = new Thread(RealTimeHandler.Run, 0) {Name = "RealTime Thread"};

                        //Launch the data feed, result sending, and transaction models/handlers in separate threads.
                        threadFeed.Start(); // Data feed pushing data packets into thread bridge; 
                        threadTransactions.Start(); // Transaction modeller scanning new order requests
                        threadRealTime.Start(); // RealTime scan time for time based events:
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
                                var message = "Runtime Error: " + err.Message;
                                Log.Trace("Engine.Run(): Sending runtime error to user...");
                                ResultHandler.LogMessage(message);
                                ResultHandler.RuntimeError(message, err.StackTrace);
                                Api.SetAlgorithmStatus(job.AlgorithmId, AlgorithmStatus.RuntimeError, message + " Stack Trace: " + err.StackTrace);
                            }
                        }

                        //Send result data back: this entire code block could be rewritten.
                        // todo: - Split up statistics class, its enormous. 
                        // todo: - Make a dedicated Statistics.Benchmark class.
                        // todo: - Move all creation and transmission of statistics out of primary engine loop.
                        // todo: - Statistics.Generate(algorithm, resulthandler, transactionhandler);

                        try
                        {
                            var charts = new Dictionary<string, Chart>(ResultHandler.Charts);
                            var orders = new Dictionary<int, Order>(algorithm.Transactions.Orders);
                            var holdings = new Dictionary<string, Holding>();
                            var statistics = new Dictionary<string, string>();
                            var banner = new Dictionary<string, string>();

                            try
                            {
                                //Generates error when things don't exist (no charting logged, runtime errors in main algo execution)
                                const string strategyEquityKey = "Strategy Equity";
                                const string equityKey = "Equity";
                                const string dailyPerformanceKey = "Daily Performance";

                                // make sure we've taken samples for these series before just blindly requesting them
                                if (charts.ContainsKey(strategyEquityKey) &&
                                    charts[strategyEquityKey].Series.ContainsKey(equityKey) &&
                                    charts[strategyEquityKey].Series.ContainsKey(dailyPerformanceKey))
                                {
                                    var equity = charts[strategyEquityKey].Series[equityKey].Values;
                                    var performance = charts[strategyEquityKey].Series[dailyPerformanceKey].Values;
                                    var profitLoss =
                                        new SortedDictionary<DateTime, decimal>(algorithm.Transactions.TransactionRecord);
                                    statistics = Statistics.Statistics.Generate(equity, profitLoss, performance,
                                        SetupHandler.StartingCapital, 252);
                                }
                            }
                            catch (Exception err)
                            {
                                Log.Error("Algorithm.Node.Engine(): Error generating statistics packet: " + err.Message);
                            }

                            //Diagnostics Completed, Send Result Packet:
                            ResultHandler.DebugMessage("Algorithm Id:(" + job.AlgorithmId + ") completed analysis in " + timer.Elapsed.TotalSeconds.ToString("F2") + " seconds");
                            ResultHandler.SendFinalResult(job, orders, algorithm.Transactions.TransactionRecord, holdings, statistics, banner);
                        }
                        catch (Exception err)
                        {
                            Log.Error("Engine.Main(): Error sending analysis result: " + err.Message + "  ST >> " + err.StackTrace);
                        }

                        //Before we return, send terminate commands to close up the threads 
                        timer.Stop(); //Algorithm finished running.
                        TransactionHandler.Exit();
                        DataFeed.Exit();
                        RealTimeHandler.Exit();
                    }

                    //Close result handler:
                    ResultHandler.Exit();

                    //Wait for the threads to complete:
                    var ts = Stopwatch.StartNew();
                    while ((ResultHandler.IsActive || (TransactionHandler != null && TransactionHandler.IsActive) || (DataFeed != null && DataFeed.IsActive)) && ts.ElapsedMilliseconds < 30 * 1000)
                    {
                        Thread.Sleep(100); Log.Trace("Waiting for threads to exit...");
                    }
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
                    //Delete the message from the job queue:
                    JobQueue.AcknowledgeJob(job);
                    Log.Trace("Engine.Main(): Packet removed from queue: " + job.AlgorithmId);

                    //No matter what for live mode; make sure we've set algorithm status in the API for "not running" conditions:
                    if (LiveMode && AlgorithmManager.State != AlgorithmStatus.Running && AlgorithmManager.State != AlgorithmStatus.RuntimeError) 
                        Api.SetAlgorithmStatus(job.AlgorithmId, AlgorithmManager.State);
                    
                    //Attempt to clean up ram usage:
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

            if (Log.LogHandler != null)
            {
                Log.LogHandler.Dispose();
            }
        }



        /// <summary>
        /// Get an instance of the data feed handler we're requesting for this work.
        /// </summary>
        /// <param name="algorithm">User algorithm to scan for securities</param>
        /// <param name="job">Algorithm Node Packet</param>
        /// <returns>Class matching IDataFeed Interface</returns>
        private static IDataFeed GetDataFeedHandler(IAlgorithm algorithm, AlgorithmNodePacket job)
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
                    var ds = Composer.Instance.GetExportedValueByTypeName<IDataQueueHandler>(Config.Get("data-queue-handler"));
                    df = new PaperTradingDataFeed(algorithm, ds, (LiveNodePacket)job);
                    Log.Trace("Engine.GetDataFeedHandler(): Selected LiveTrading Datafeed");
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
                    rth = new LiveTradingRealTimeHandler(algorithm, feed, results);
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
                    th = new BacktestingTransactionHandler(algorithm, brokerage as BacktestingBrokerage);
                    Log.Trace("Engine.GetTransactionHandler(): Selected Backtesting Transaction Models.");
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
            }
            return sh;
        }
    } // End Algorithm Node Core Thread
    
} // End Namespace
