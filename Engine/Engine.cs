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
using System.Linq;
using System.Threading;
using QuantConnect.Configuration;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Packets;
using QuantConnect.Statistics;
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
        private readonly LeanEngineSystemHandlers _systemHandlers;
        private readonly LeanEngineAlgorithmHandlers _algorithmHandlers;

        /// <summary>
        /// Gets the configured system handlers for this engine instance
        /// </summary>
        public LeanEngineSystemHandlers SystemHandlers
        {
            get { return _systemHandlers; }
        }

        /// <summary>
        /// Gets the configured algorithm handlers for this engine instance
        /// </summary>
        public LeanEngineAlgorithmHandlers AlgorithmHandlers
        {
            get { return _algorithmHandlers;}
        }

        private readonly bool _liveMode;
        private const string _collapseMessage = "Unhandled exception breaking past controls and causing collapse of algorithm node. This is likely a memory leak of an external dependency or the underlying OS terminating the LEAN engine.";

        /// <summary>
        /// Primary Analysis Thread:
        /// </summary>
        public static void Main(string[] args)
        {
            Log.LogHandler = Composer.Instance.GetExportedValueByTypeName<ILogHandler>(Config.Get("log-handler", "CompositeLogHandler"));

            //Initialize:
            string mode = "RELEASE";
            var liveMode = Config.GetBool("live-mode");
            Log.DebuggingEnabled = Config.GetBool("debug-mode");

            #if DEBUG 
                mode = "DEBUG";
            #endif

            //Name thread for the profiler:
            Thread.CurrentThread.Name = "Algorithm Analysis Thread";
            Log.Trace("Engine.Main(): LEAN ALGORITHMIC TRADING ENGINE v" + Constants.Version + " Mode: " + mode);
            Log.Trace("Engine.Main(): Started " + DateTime.Now.ToShortTimeString());
            Log.Trace("Engine.Main(): Memory " + OS.ApplicationMemoryUsed + "Mb-App  " + +OS.TotalPhysicalMemoryUsed + "Mb-Used  " + OS.TotalPhysicalMemory + "Mb-Total");

            //Import external libraries specific to physical server location (cloud/local)
            LeanEngineSystemHandlers leanEngineSystemHandlers;
            try
            {
                leanEngineSystemHandlers = LeanEngineSystemHandlers.FromConfiguration(Composer.Instance);
            }
            catch (CompositionException compositionException)
            {
                Log.Error("Engine.Main(): Failed to load library: " + compositionException);
                throw;
            }

            //Setup packeting, queue and controls system: These don't do much locally.
            leanEngineSystemHandlers.Initialize();

            //-> Pull job from QuantConnect job queue, or, pull local build:
            string assemblyPath;
            var job = leanEngineSystemHandlers.JobQueue.NextJob(out assemblyPath);

            if (job == null)
            {
                throw new Exception("Engine.Main(): Job was null.");
            }

            LeanEngineAlgorithmHandlers leanEngineAlgorithmHandlers;
            try
            {
                leanEngineAlgorithmHandlers = LeanEngineAlgorithmHandlers.FromConfiguration(Composer.Instance);
            }
            catch (CompositionException compositionException)
            {
                Log.Error("Engine.Main(): Failed to load library: " + compositionException);
                throw;
            }

            // log the job endpoints
            Log.Trace("JOB HANDLERS: ");
            Log.Trace("         DataFeed:     " + leanEngineAlgorithmHandlers.DataFeed.GetType().FullName);
            Log.Trace("         Setup:        " + leanEngineAlgorithmHandlers.Setup.GetType().FullName);
            Log.Trace("         RealTime:     " + leanEngineAlgorithmHandlers.RealTime.GetType().FullName);
            Log.Trace("         Results:      " + leanEngineAlgorithmHandlers.Results.GetType().FullName);
            Log.Trace("         Transactions: " + leanEngineAlgorithmHandlers.Transactions.GetType().FullName);
            Log.Trace("         History:      " + leanEngineAlgorithmHandlers.HistoryProvider.GetType().FullName);

            // if the job version doesn't match this instance version then we can't process it
            // we also don't want to reprocess redelivered jobs
            if (job.Version != Constants.Version || job.Redelivered)
            {
                Log.Error("Engine.Run(): Job Version: " + job.Version + "  Deployed Version: " + Constants.Version + " Redelivered: " + job.Redelivered);
                //Tiny chance there was an uncontrolled collapse of a server, resulting in an old user task circulating.
                //In this event kill the old algorithm and leave a message so the user can later review.
                leanEngineSystemHandlers.Api.SetAlgorithmStatus(job.AlgorithmId, AlgorithmStatus.RuntimeError, _collapseMessage);
                leanEngineSystemHandlers.Notify.SetChannel(job.Channel);
                leanEngineSystemHandlers.Notify.RuntimeError(job.AlgorithmId, _collapseMessage);
                leanEngineSystemHandlers.JobQueue.AcknowledgeJob(job);
                return;
            }

            try
            {
                var engine = new Engine(leanEngineSystemHandlers, leanEngineAlgorithmHandlers, liveMode);
                engine.Run(job, assemblyPath);
            }
            finally
            {
                //Delete the message from the job queue:
                leanEngineSystemHandlers.JobQueue.AcknowledgeJob(job);
                Log.Trace("Engine.Main(): Packet removed from queue: " + job.AlgorithmId);

                // clean up resources
                leanEngineSystemHandlers.Dispose();
                leanEngineAlgorithmHandlers.Dispose();
                Log.LogHandler.Dispose();
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Engine"/> class using the specified handlers
        /// </summary>
        /// <param name="systemHandlers">The system handlers for controlling acquisition of jobs, messaging, and api calls</param>
        /// <param name="algorithmHandlers">The algorithm handlers for managing algorithm initialization, data, results, transaction, and real time events</param>
        /// <param name="liveMode">True when running in live mode, false otherwises</param>
        public Engine(LeanEngineSystemHandlers systemHandlers, LeanEngineAlgorithmHandlers algorithmHandlers, bool liveMode)
        {
            _liveMode = liveMode;
            _systemHandlers = systemHandlers;
            _algorithmHandlers = algorithmHandlers;
        }

        /// <summary>
        /// Runs a single backtest/live job from the job queue
        /// </summary>
        /// <param name="job">The algorithm job to be processed</param>
        /// <param name="assemblyPath">The path to the algorithm's assembly</param>
        public void Run(AlgorithmNodePacket job, string assemblyPath)
        {
            var algorithm = default(IAlgorithm);
            var algorithmManager = new AlgorithmManager(_liveMode);

            //Start monitoring the backtest active status:
            var statusPing = new StateCheck.Ping(algorithmManager, _systemHandlers.Api, _algorithmHandlers.Results, _systemHandlers.Notify, job);
            var statusPingThread = new Thread(statusPing.Run);
            statusPingThread.Start();

            try
            {
                //Reset thread holders.
                var initializeComplete = false;
                Thread threadFeed = null;
                Thread threadTransactions = null;
                Thread threadResults = null;
                Thread threadRealTime = null;

                //-> Initialize messaging system
                _systemHandlers.Notify.SetChannel(job.Channel);

                //-> Set the result handler type for this algorithm job, and launch the associated result thread.
                _algorithmHandlers.Results.Initialize(job, _systemHandlers.Notify, _systemHandlers.Api, _algorithmHandlers.DataFeed, _algorithmHandlers.Setup, _algorithmHandlers.Transactions);

                threadResults = new Thread(_algorithmHandlers.Results.Run, 0) {Name = "Result Thread"};
                threadResults.Start();

                IBrokerage brokerage = null;
                try
                {
                    // Save algorithm to cache, load algorithm instance:
                    algorithm = _algorithmHandlers.Setup.CreateAlgorithmInstance(assemblyPath, job.Language);

                    // set the history provider before setting up the algorithm
                    _algorithmHandlers.HistoryProvider.Initialize(job, progress =>
                    {
                        // send progress updates to the result handler only during initialization
                        if (!algorithm.GetLocked() || algorithm.IsWarmingUp)
                        {
                            _algorithmHandlers.Results.SendStatusUpdate(job.AlgorithmId, AlgorithmStatus.History, 
                                string.Format("Processing history {0}%...", progress));
                        }
                    });
                    algorithm.HistoryProvider = _algorithmHandlers.HistoryProvider;

                    //Initialize the internal state of algorithm and job: executes the algorithm.Initialize() method.
                    initializeComplete = _algorithmHandlers.Setup.Setup(algorithm, out brokerage, job, _algorithmHandlers.Results, _algorithmHandlers.Transactions, _algorithmHandlers.RealTime);

                    // set this again now that we've actually added securities
                    _algorithmHandlers.Results.SetAlgorithm(algorithm);

                    //If there are any reasons it failed, pass these back to the IDE.
                    if (!initializeComplete || algorithm.ErrorMessages.Count > 0 || _algorithmHandlers.Setup.Errors.Count > 0)
                    {
                        initializeComplete = false;
                        //Get all the error messages: internal in algorithm and external in setup handler.
                        var errorMessage = String.Join(",", algorithm.ErrorMessages);
                        errorMessage += String.Join(",", _algorithmHandlers.Setup.Errors);
                        _algorithmHandlers.Results.RuntimeError(errorMessage);
                        _systemHandlers.Api.SetAlgorithmStatus(job.AlgorithmId, AlgorithmStatus.RuntimeError, errorMessage);
                    }
                }
                catch (Exception err)
                {
                    var runtimeMessage = "Algorithm.Initialize() Error: " + err.Message + " Stack Trace: " + err.StackTrace;
                    _algorithmHandlers.Results.RuntimeError(runtimeMessage, err.StackTrace);
                    _systemHandlers.Api.SetAlgorithmStatus(job.AlgorithmId, AlgorithmStatus.RuntimeError, runtimeMessage);
                }

                //-> Using the job + initialization: load the designated handlers:
                if (initializeComplete)
                {
                    //-> Reset the backtest stopwatch; we're now running the algorithm.
                    var startTime = DateTime.Now;

                    //Set algorithm as locked; set it to live mode if we're trading live, and set it to locked for no further updates.
                    algorithm.SetAlgorithmId(job.AlgorithmId);
                    algorithm.SetLiveMode(_liveMode);
                    algorithm.SetLocked();

                    //Wire up the universe selection event handler before kicking off the data feed
                    var universeSelection = new UniverseSelection(_algorithmHandlers.DataFeed, algorithm, _liveMode);
                    _algorithmHandlers.DataFeed.UniverseSelection += (sender, args) => universeSelection.ApplyUniverseSelection(args);

                    //Load the associated handlers for data, transaction and realtime events:
                    _algorithmHandlers.DataFeed.Initialize(algorithm, job, _algorithmHandlers.Results);
                    _algorithmHandlers.Transactions.Initialize(algorithm, brokerage, _algorithmHandlers.Results);
                    _algorithmHandlers.RealTime.Setup(algorithm, job, _algorithmHandlers.Results, _systemHandlers.Api);

                    //Set the error handlers for the brokerage asynchronous errors.
                    _algorithmHandlers.Setup.SetupErrorHandler(_algorithmHandlers.Results, brokerage);

                    //Send status to user the algorithm is now executing.
                    _algorithmHandlers.Results.SendStatusUpdate(job.AlgorithmId, AlgorithmStatus.Running);

                    //Launch the data, transaction and realtime handlers into dedicated threads
                    threadFeed = new Thread(_algorithmHandlers.DataFeed.Run) {Name = "DataFeed Thread"};
                    threadTransactions = new Thread(_algorithmHandlers.Transactions.Run) {Name = "Transaction Thread"};
                    threadRealTime = new Thread(_algorithmHandlers.RealTime.Run) {Name = "RealTime Thread"};

                    //Launch the data feed, result sending, and transaction models/handlers in separate threads.
                    threadFeed.Start(); // Data feed pushing data packets into thread bridge; 
                    threadTransactions.Start(); // Transaction modeller scanning new order requests
                    threadRealTime.Start(); // RealTime scan time for time based events:

                    // Result manager scanning message queue: (started earlier)
                    _algorithmHandlers.Results.DebugMessage(string.Format("Launching analysis for {0} with LEAN Engine v{1}", job.AlgorithmId, Constants.Version));

                    try
                    {
                        //Create a new engine isolator class 
                        var isolator = new Isolator();

                        // Execute the Algorithm Code:
                        var complete = isolator.ExecuteWithTimeLimit(_algorithmHandlers.Setup.MaximumRuntime, algorithmManager.TimeLoopWithinLimits, () =>
                        {
                            try
                            {
                                //Run Algorithm Job:
                                // -> Using this Data Feed, 
                                // -> Send Orders to this TransactionHandler, 
                                // -> Send Results to ResultHandler.
                                algorithmManager.Run(job, algorithm, _algorithmHandlers.DataFeed, _algorithmHandlers.Transactions, _algorithmHandlers.Results, _algorithmHandlers.RealTime, isolator.CancellationToken);
                            }
                            catch (Exception err)
                            {
                                //Debugging at this level is difficult, stack trace needed.
                                Log.Error(err);
                                algorithm.RunTimeError = err;
                                algorithmManager.SetStatus(AlgorithmStatus.RuntimeError);
                                return;
                            }

                            Log.Trace("Engine.Run(): Exiting Algorithm Manager");
                        }, job.RamAllocation);

                        if (!complete)
                        {
                            Log.Error("Engine.Main(): Failed to complete in time: " + _algorithmHandlers.Setup.MaximumRuntime.ToString("F"));
                            throw new Exception("Failed to complete algorithm within " + _algorithmHandlers.Setup.MaximumRuntime.ToString("F")
                                + " seconds. Please make it run faster.");
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
                        if (_algorithmHandlers.DataFeed != null) _algorithmHandlers.DataFeed.Exit();
                        if (_algorithmHandlers.Results != null)
                        {
                            var message = "Runtime Error: " + err.Message;
                            Log.Trace("Engine.Run(): Sending runtime error to user...");
                            _algorithmHandlers.Results.LogMessage(message);
                            _algorithmHandlers.Results.RuntimeError(message, err.StackTrace);
                            _systemHandlers.Api.SetAlgorithmStatus(job.AlgorithmId, AlgorithmStatus.RuntimeError, message + " Stack Trace: " + err.StackTrace);
                        }
                    }

                    //Send result data back: this entire code block could be rewritten.
                    // todo: - Split up statistics class, its enormous. 
                    // todo: - Make a dedicated Statistics.Benchmark class.
                    // todo: - Move all creation and transmission of statistics out of primary engine loop.
                    // todo: - Statistics.Generate(algorithm, resulthandler, transactionhandler);

                    try
                    {
                        var trades = algorithm.TradeBuilder.ClosedTrades;
                        var charts = new Dictionary<string, Chart>(_algorithmHandlers.Results.Charts);
                        var orders = new Dictionary<int, Order>(_algorithmHandlers.Transactions.Orders);
                        var holdings = new Dictionary<string, Holding>();
                        var banner = new Dictionary<string, string>();
                        var statisticsResults = new StatisticsResults();

                        try
                        {
                            //Generates error when things don't exist (no charting logged, runtime errors in main algo execution)
                            const string strategyEquityKey = "Strategy Equity";
                            const string equityKey = "Equity";
                            const string dailyPerformanceKey = "Daily Performance";
                            const string benchmarkKey = "Benchmark";

                            // make sure we've taken samples for these series before just blindly requesting them
                            if (charts.ContainsKey(strategyEquityKey) &&
                                charts[strategyEquityKey].Series.ContainsKey(equityKey) &&
                                charts[strategyEquityKey].Series.ContainsKey(dailyPerformanceKey))
                            {
                                var equity = charts[strategyEquityKey].Series[equityKey].Values;
                                var performance = charts[strategyEquityKey].Series[dailyPerformanceKey].Values;
                                var profitLoss = new SortedDictionary<DateTime, decimal>(algorithm.Transactions.TransactionRecord);
                                var totalTransactions = algorithm.Transactions.GetOrders(x => x.Status.IsFill()).Count();
                                var benchmark = charts[benchmarkKey].Series[benchmarkKey].Values;

                                statisticsResults = StatisticsBuilder.Generate(trades, profitLoss, equity, performance, benchmark,
                                    _algorithmHandlers.Setup.StartingPortfolioValue, algorithm.Portfolio.TotalFees, totalTransactions);
                            }
                        }
                        catch (Exception err)
                        {
                            Log.Error("Algorithm.Node.Engine(): Error generating statistics packet: " + err.Message);
                        }

                        //Diagnostics Completed, Send Result Packet:
                        var totalSeconds = (DateTime.Now - startTime).TotalSeconds;
                        var dataPoints = algorithmManager.DataPoints + _algorithmHandlers.HistoryProvider.DataPointCount;
                        _algorithmHandlers.Results.DebugMessage(
                            string.Format("Algorithm Id:({0}) completed in {1} seconds at {2}k data points per second. Processing total of {3} data points.",
                                job.AlgorithmId, totalSeconds.ToString("F2"), ((dataPoints/(double) 1000)/totalSeconds).ToString("F0"),
                                dataPoints.ToString("N0")));

                        _algorithmHandlers.Results.SendFinalResult(job, orders, algorithm.Transactions.TransactionRecord, holdings, statisticsResults, banner);
                    }
                    catch (Exception err)
                    {
                        Log.Error("Engine.Main(): Error sending analysis result: " + err.Message + "  ST >> " + err.StackTrace);
                    }

                    //Before we return, send terminate commands to close up the threads
                    _algorithmHandlers.Transactions.Exit();
                    _algorithmHandlers.DataFeed.Exit();
                    _algorithmHandlers.RealTime.Exit();
                }

                //Close result handler:
                _algorithmHandlers.Results.Exit();
                statusPing.Exit();

                //Wait for the threads to complete:
                var ts = Stopwatch.StartNew();
                while ((_algorithmHandlers.Results.IsActive 
                    || (_algorithmHandlers.Transactions != null && _algorithmHandlers.Transactions.IsActive) 
                    || (_algorithmHandlers.DataFeed != null && _algorithmHandlers.DataFeed.IsActive)
                    || (_algorithmHandlers.RealTime != null && _algorithmHandlers.RealTime.IsActive))
                    && ts.ElapsedMilliseconds < 30*1000)
                {
                    Thread.Sleep(100);
                    Log.Trace("Waiting for threads to exit...");
                }

                //Terminate threads still in active state.
                if (threadFeed != null && threadFeed.IsAlive) threadFeed.Abort();
                if (threadTransactions != null && threadTransactions.IsAlive) threadTransactions.Abort();
                if (threadResults != null && threadResults.IsAlive) threadResults.Abort();
                if (statusPingThread != null && statusPingThread.IsAlive) statusPingThread.Abort();

                if (brokerage != null)
                {
                    brokerage.Disconnect();
                }
                if (_algorithmHandlers.Setup != null)
                {
                    _algorithmHandlers.Setup.Dispose();
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
                if (_liveMode && algorithmManager.State != AlgorithmStatus.Running && algorithmManager.State != AlgorithmStatus.RuntimeError)
                    _systemHandlers.Api.SetAlgorithmStatus(job.AlgorithmId, algorithmManager.State);

                _algorithmHandlers.Results.Exit();
                _algorithmHandlers.DataFeed.Exit();
                _algorithmHandlers.Transactions.Exit();
                _algorithmHandlers.RealTime.Exit();
            }
        }
    } // End Algorithm Node Core Thread
} // End Namespace
