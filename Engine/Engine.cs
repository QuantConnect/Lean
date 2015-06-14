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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
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
using QuantConnect.Util;

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
        private static bool _liveMode = Config.GetBool("live-mode");
        private static bool _local = Config.GetBool("local");
        private static IBrokerage _brokerage;
        private const string _collapseMessage = "Unhandled exception breaking past controls and causing collapse of algorithm node. This is likely a memory leak of an external dependency or the underlying OS terminating the LEAN engine.";

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
                //Total Physical Ram Available: 4gb max allocation per backtest
                var allocation = 4096;
                var ram = Convert.ToInt32(OS.TotalPhysicalMemory);
                if (ram < allocation)
                {
                    //If memory on machine less 100 allocation for OS: 
                    allocation = ram - 100;
                }
                Log.Trace("Engine.MaximumRamAllocation(): Allocated: " + allocation);
                return allocation;
            }
        }

        /// <summary>
        /// Primary Analysis Thread:
        /// </summary>
        public static void Main(string[] args) 
        {
            //Initialize:
            var algorithmPath = "";
            string mode = "RELEASE";
            AlgorithmNodePacket job = null;
            var algorithm = default(IAlgorithm);
            var startTime = DateTime.Now;
            Log.LogHandler = Composer.Instance.GetExportedValueByTypeName<ILogHandler>(Config.Get("log-handler", "CompositeLogHandler"));
       
            #if DEBUG 
                mode = "DEBUG";
            #endif

            //Name thread for the profiler:
            Thread.CurrentThread.Name = "Algorithm Analysis Thread";
            Log.Trace("Engine.Main(): LEAN ALGORITHMIC TRADING ENGINE v" + Constants.Version + " Mode: " + mode);
            Log.Trace("Engine.Main(): Started " + DateTime.Now.ToShortTimeString());
            Log.Trace("Engine.Main(): Memory " + OS.ApplicationMemoryUsed + "Mb-App  " + +OS.TotalPhysicalMemoryUsed + "Mb-Used  " + OS.TotalPhysicalMemory + "Mb-Total");

            //Import external libraries specific to physical server location (cloud/local)
            try
            {
                // grab the right export based on configuration
                Api = Composer.Instance.GetExportedValueByTypeName<IApi>(Config.Get("api-handler"));
                Notify = Composer.Instance.GetExportedValueByTypeName<IMessagingHandler>(Config.Get("messaging-handler"));
                JobQueue = Composer.Instance.GetExportedValueByTypeName<IJobQueueHandler>(Config.Get("job-queue-handler"));
            }
            catch (CompositionException compositionException)
            { Log.Error("Engine.Main(): Failed to load library: " + compositionException);
            }

            //Setup packeting, queue and controls system: These don't do much locally.
            Api.Initialize();
            Notify.Initialize();
            JobQueue.Initialize();

            //Start monitoring the backtest active status:
            var statusPingThread = new Thread(StateCheck.Ping.Run);
            statusPingThread.Start();

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

                    // if the job version doesn't match this instance version then we can't process it
                    // we also don't want to reprocess redelivered live jobs
                    if (job.Version != Constants.Version || (LiveMode && job.Redelivered))
                    {
                        Log.Error("Engine.Run(): Job Version: " + job.Version + "  Deployed Version: " + Constants.Version);

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
                        ResultHandler.RuntimeError(errorMessage);
                        Api.SetAlgorithmStatus(job.AlgorithmId, AlgorithmStatus.RuntimeError);
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
                    //-> Reset the backtest stopwatch; we're now running the algorithm.
                    startTime = DateTime.Now;
                        
                    //Set algorithm as locked; set it to live mode if we're trading live, and set it to locked for no further updates.
                    algorithm.SetAlgorithmId(job.AlgorithmId);
                    algorithm.SetLiveMode(LiveMode);
                    algorithm.SetLocked();

                    //Load the associated handlers for data, transaction and realtime events:
                    ResultHandler.SetAlgorithm(algorithm);
                    DataFeed = GetDataFeedHandler(algorithm, _brokerage, job);
                    TransactionHandler  = GetTransactionHandler(algorithm, _brokerage, ResultHandler, job);
                    RealTimeHandler     = GetRealTimeHandler(algorithm, _brokerage, DataFeed, ResultHandler, job);

                    //Set the error handlers for the brokerage asynchronous errors.
                    SetupHandler.SetupErrorHandler(ResultHandler, _brokerage);

                    //Send status to user the algorithm is now executing.
                    ResultHandler.SendStatusUpdate(job.AlgorithmId, AlgorithmStatus.Running);

                    //Launch the data, transaction and realtime handlers into dedicated threads
                    threadFeed = new Thread(DataFeed.Run) {Name = "DataFeed Thread"};
                    threadTransactions = new Thread(TransactionHandler.Run) {Name = "Transaction Thread"};
                    threadRealTime = new Thread(RealTimeHandler.Run) {Name = "RealTime Thread"};

                    //Launch the data feed, result sending, and transaction models/handlers in separate threads.
                    threadFeed.Start(); // Data feed pushing data packets into thread bridge; 
                    threadTransactions.Start(); // Transaction modeller scanning new order requests
                    threadRealTime.Start(); // RealTime scan time for time based events:

                    // Result manager scanning message queue: (started earlier)
                    ResultHandler.DebugMessage(string.Format("Launching analysis for {0} with LEAN Engine v{1}", job.AlgorithmId, Constants.Version));

                    try
                    {
                        //Create a new engine isolator class 
                        var isolator = new Isolator();

                        // Execute the Algorithm Code:
                        var complete = isolator.ExecuteWithTimeLimit(SetupHandler.MaximumRuntime, AlgorithmManager.TimeLoopWithinLimits, () =>
                        {
                            try
                            {
                                //Run Algorithm Job:
                                // -> Using this Data Feed, 
                                // -> Send Orders to this TransactionHandler, 
                                // -> Send Results to ResultHandler.
                                AlgorithmManager.Run(job, algorithm, DataFeed, TransactionHandler, ResultHandler, SetupHandler, RealTimeHandler, isolator.CancelToken);
                            }
                            catch (Exception err)
                            {
                                //Debugging at this level is difficult, stack trace needed.
                                Log.Error("Engine.Run", err);
                            }

                            Log.Trace("Engine.Run(): Exiting Algorithm Manager");

                        }, job.UserPlan == UserPlan.Free ? Math.Min(1024, MaximumRamAllocation) : MaximumRamAllocation);

                        if (!complete)
                        {
                            Log.Error("Engine.Main(): Failed to complete in time: " + SetupHandler.MaximumRuntime.ToString("F"));
                            throw new Exception("Failed to complete algorithm within " + SetupHandler.MaximumRuntime.ToString("F") + " seconds. Please make it run faster.");
                        }

                        // Algorithm runtime error:
                        if (algorithm.RunTimeError != null)
                        {
                            throw algorithm.RunTimeError;
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
                                    SetupHandler.StartingPortfolioValue, algorithm.Portfolio.TotalFees, 252);
                            }
                        }
                        catch (Exception err)
                        {
                            Log.Error("Algorithm.Node.Engine(): Error generating statistics packet: " + err.Message);
                        }

                        //Diagnostics Completed, Send Result Packet:
                        var totalSeconds = (DateTime.Now - startTime).TotalSeconds;
                        ResultHandler.DebugMessage(string.Format("Algorithm Id:({0}) completed in {1} seconds at {2}k data points per second. Processing total of {3} data points.",
                            job.AlgorithmId, totalSeconds.ToString("F2"), ((AlgorithmManager.DataPoints / (double)1000) / totalSeconds).ToString("F0"), AlgorithmManager.DataPoints.ToString("N0")));

                        ResultHandler.SendFinalResult(job, orders, algorithm.Transactions.TransactionRecord, holdings, statistics, banner);
                    }
                    catch (Exception err)
                    {
                        Log.Error("Engine.Main(): Error sending analysis result: " + err.Message + "  ST >> " + err.StackTrace);
                    }

                    //Before we return, send terminate commands to close up the threads
                    TransactionHandler.Exit();
                    DataFeed.Exit();
                    RealTimeHandler.Exit();
                }

                //Close result handler:
                ResultHandler.Exit();
                StateCheck.Ping.Exit();

                //Wait for the threads to complete:
                var ts = Stopwatch.StartNew();
                while ((ResultHandler.IsActive || (TransactionHandler != null && TransactionHandler.IsActive) || (DataFeed != null && DataFeed.IsActive)) && ts.ElapsedMilliseconds < 30 * 1000)
                {
                    Thread.Sleep(100); Log.Trace("Waiting for threads to exit...");
                }

                //Terminate threads still in active state.
                if (threadFeed != null && threadFeed.IsAlive) threadFeed.Abort();
                if (threadTransactions != null && threadTransactions.IsAlive) threadTransactions.Abort();
                if (threadResults != null && threadResults.IsAlive) threadResults.Abort();
                if (statusPingThread != null && statusPingThread.IsAlive) statusPingThread.Abort();

                if (_brokerage != null)
                {
                    _brokerage.Disconnect();
                }
                if (SetupHandler != null)
                {
                    SetupHandler.Dispose();
                }
                Log.Trace("Engine.Main(): Analysis Completed and Results Posted.");
            }
            catch (Exception err)
            {
                Log.Error("Engine.Main(): Error running algorithm: " + err.Message + " >> " + err.StackTrace);
            }
            finally
            {
                //No matter what for live mode; make sure we've set algorithm status in the API for "not running" conditions:
                if (LiveMode && AlgorithmManager.State != AlgorithmStatus.Running && AlgorithmManager.State != AlgorithmStatus.RuntimeError)
                    Api.SetAlgorithmStatus(job.AlgorithmId, AlgorithmManager.State);

                //Delete the message from the job queue:
                JobQueue.AcknowledgeJob(job);
                Log.Trace("Engine.Main(): Packet removed from queue: " + job.AlgorithmId);

                //Attempt to clean up ram usage:
                GC.Collect();
            }

            //Final disposals.
            Api.Dispose();
            
            // Make the console window pause so we can read log output before exiting and killing the application completely
            if (IsLocal)
            {
                Log.Trace("Engine.Main(): Analysis Complete. Press any key to continue.");
                Console.Read();
            }
            Log.LogHandler.Dispose();
        }

        /// <summary>
        /// Get an instance of the data feed handler we're requesting for this work.
        /// </summary>
        /// <param name="algorithm">User algorithm to scan for securities</param>
        /// <param name="job">Algorithm Node Packet</param>
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

                case DataFeedEndpoint.Database:
                    df = new DatabaseDataFeed(algorithm, (BacktestNodePacket)job);
                    Log.Trace("Engine.GetDataFeedHandler(): Selected Database Datafeed");
                    break;

                //Operation from local files:
                case DataFeedEndpoint.FileSystem:
                    df = new FileSystemDataFeed(algorithm, (BacktestNodePacket)job);
                    Log.Trace("Engine.GetDataFeedHandler(): Selected FileSystem Datafeed");
                    break;

                //Live Trading Data Source:
                case DataFeedEndpoint.LiveTrading:

                    var useBroker = Config.GetBool("use-broker-data-queue-handler", false);

                    var ds = useBroker == true ?  
                                brokerage as IDataQueueHandler:
                                Composer.Instance.GetExportedValueByTypeName<IDataQueueHandler>(Config.Get("data-queue-handler", "LiveDataQueue"));

                    df = new LiveTradingDataFeed(algorithm, (LiveNodePacket)job, ds);
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
                case TransactionHandlerEndpoint.Brokerage:
                    th = new BrokerageTransactionHandler(algorithm, brokerage);
                    Log.Trace("Engine.GetTransactionHandler(): Selected Brokerage Transaction Models.");
                    break;

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
            var resultHandler = Config.Get("result-handler", "ConsoleResultHandler");
            var rh = Composer.Instance.GetExportedValueByTypeName<IResultHandler>(resultHandler);
            
            rh.Initialize(job);
            Log.Trace("Engine.GetResultHandler(): Loaded and Initialized Result Handler: " + resultHandler + "  v" + Constants.Version);
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
                case SetupHandlerEndpoint.Brokerage:
                    sh = new BrokerageSetupHandler();
                    Log.Trace("Engine.GetSetupHandler(): Selected Brokerage Algorithm Setup Handler.");
                    break;
            }
            return sh;
        }
    } // End Algorithm Node Core Thread
    
} // End Namespace
