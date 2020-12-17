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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using QuantConnect.Configuration;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.TransactionHandlers;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Orders.Serialization;
using QuantConnect.Packets;
using QuantConnect.Statistics;

namespace QuantConnect.Lean.Engine.Results
{
    /// <summary>
    /// Provides base functionality to the implementations of <see cref="IResultHandler"/>
    /// </summary>
    public abstract class BaseResultsHandler
    {
        // used for resetting out/error upon completion
        private static readonly TextWriter StandardOut = Console.Out;
        private static readonly TextWriter StandardError = Console.Error;

        private string _hostName;

        /// <summary>
        /// The main loop update interval
        /// </summary>
        protected virtual TimeSpan MainUpdateInterval => TimeSpan.FromSeconds(3);

        /// <summary>
        /// The chart update interval
        /// </summary>
        protected TimeSpan ChartUpdateInterval = TimeSpan.FromMinutes(1);

        /// <summary>
        /// The last position consumed from the <see cref="ITransactionHandler.OrderEvents"/> by <see cref="GetDeltaOrders"/>
        /// </summary>
        protected int LastDeltaOrderPosition;

        /// <summary>
        /// The last position consumed from the <see cref="ITransactionHandler.OrderEvents"/> while determining delta order events
        /// </summary>
        protected int LastDeltaOrderEventsPosition;

        /// <summary>
        /// The task in charge of running the <see cref="Run"/> update method
        /// </summary>
        private Thread _updateRunner;

        /// <summary>
        /// Boolean flag indicating the thread is still active.
        /// </summary>
        public bool IsActive => _updateRunner != null && _updateRunner.IsAlive;

        /// <summary>
        /// Live packet messaging queue. Queue the messages here and send when the result queue is ready.
        /// </summary>
        public ConcurrentQueue<Packet> Messages { get; set; }

        /// <summary>
        /// Storage for the price and equity charts of the live results.
        /// </summary>
        public ConcurrentDictionary<string, Chart> Charts { get; set; }

        /// <summary>
        /// True if the exit has been triggered
        /// </summary>
        protected volatile bool ExitTriggered;

        /// <summary>
        /// The log store instance
        /// </summary>
        protected List<LogEntry> LogStore { get; }

        /// <summary>
        /// Algorithms performance related chart names
        /// </summary>
        /// <remarks>Used to calculate the probabilistic sharpe ratio</remarks>
        protected List<string> AlgorithmPerformanceCharts { get; } = new List<string> { "Strategy Equity", "Benchmark" };

        /// <summary>
        /// Lock to be used when accessing the chart collection
        /// </summary>
        protected object ChartLock { get; }

        /// <summary>
        /// The algorithm project id
        /// </summary>
        protected int ProjectId { get; set; }

        /// <summary>
        /// The maximum amount of RAM (in MB) this algorithm is allowed to utilize
        /// </summary>
        protected string RamAllocation { get; set; }

        /// <summary>
        /// The algorithm unique compilation id
        /// </summary>
        protected string CompileId { get; set; }

        /// <summary>
        /// The algorithm job id.
        /// This is the deploy id for live, backtesting id for backtesting
        /// </summary>
        protected string AlgorithmId { get; set; }

        /// <summary>
        /// The result handler start time
        /// </summary>
        protected DateTime StartTime { get; }

        /// <summary>
        /// Customizable dynamic statistics <see cref="IAlgorithm.RuntimeStatistics"/>
        /// </summary>
        protected Dictionary<string, string> RuntimeStatistics { get; }

        /// <summary>
        /// The handler responsible for communicating messages to listeners
        /// </summary>
        protected IMessagingHandler MessagingHandler;

        /// <summary>
        /// The transaction handler used to get the algorithms Orders information
        /// </summary>
        protected ITransactionHandler TransactionHandler;

        /// <summary>
        /// The algorithms starting portfolio value.
        /// Used to calculate the portfolio return
        /// </summary>
        protected decimal StartingPortfolioValue { get; set; }

        /// <summary>
        /// The algorithm instance
        /// </summary>
        protected IAlgorithm Algorithm { get; set; }

        /// <summary>
        /// Gets or sets the current alpha runtime statistics
        /// </summary>
        protected AlphaRuntimeStatistics AlphaRuntimeStatistics { get; set; }

        /// <summary>
        /// Closing portfolio value. Used to calculate daily performance.
        /// </summary>
        protected decimal DailyPortfolioValue;

        /// <summary>
        /// Last time the <see cref="IResultHandler.Sample(DateTime, bool)"/> method was called in UTC
        /// </summary>
        protected DateTime PreviousUtcSampleTime;

        /// <summary>
        /// Sampling period for timespans between resamples of the charting equity.
        /// </summary>
        /// <remarks>Specifically critical for backtesting since with such long timeframes the sampled data can get extreme.</remarks>
        protected TimeSpan ResamplePeriod { get; set; }

        /// <summary>
        /// How frequently the backtests push messages to the browser.
        /// </summary>
        /// <remarks>Update frequency of notification packets</remarks>
        protected TimeSpan NotificationPeriod { get; set; }

        /// <summary>
        /// Directory location to store results
        /// </summary>
        protected string ResultsDestinationFolder;

        /// <summary>
        /// The order event json converter instance to use
        /// </summary>
        protected OrderEventJsonConverter OrderEventJsonConverter { get; set; }

        /// <summary>
        /// Creates a new instance
        /// </summary>
        protected BaseResultsHandler()
        {
            Charts = new ConcurrentDictionary<string, Chart>();
            Messages = new ConcurrentQueue<Packet>();
            RuntimeStatistics = new Dictionary<string, string>();
            StartTime = DateTime.UtcNow;
            CompileId = "";
            AlgorithmId = "";
            ChartLock = new object();
            LogStore = new List<LogEntry>();
            ResultsDestinationFolder = Config.Get("results-destination-folder", Directory.GetCurrentDirectory());
        }

        /// <summary>
        /// New order event for the algorithm
        /// </summary>
        /// <param name="newEvent">New event details</param>
        public virtual void OrderEvent(OrderEvent newEvent)
        {
        }

        /// <summary>
        /// Terminate the result thread and apply any required exit procedures like sending final results
        /// </summary>
        public virtual void Exit()
        {
            // reset standard out/error
            Console.SetOut(StandardOut);
            Console.SetError(StandardError);
        }

        /// <summary>
        /// Gets the current Server statistics
        /// </summary>
        protected virtual Dictionary<string, string> GetServerStatistics(DateTime utcNow)
        {
            var serverStatistics = OS.GetServerStatistics();
            serverStatistics["Hostname"] = _hostName;
            var upTime = utcNow - StartTime;
            serverStatistics["Up Time"] = $"{upTime.Days}d {upTime:hh\\:mm\\:ss}";
            serverStatistics["Total RAM (MB)"] = RamAllocation;
            return serverStatistics;
        }

        /// <summary>
        /// Stores the order events
        /// </summary>
        /// <param name="utcTime">The utc date associated with these order events</param>
        /// <param name="orderEvents">The order events to store</param>
        protected virtual void StoreOrderEvents(DateTime utcTime, List<OrderEvent> orderEvents)
        {
            if (orderEvents.Count <= 0)
            {
                return;
            }

            var filename = $"{AlgorithmId}-order-events.json";
            var path = GetResultsPath(filename);

            var data = JsonConvert.SerializeObject(orderEvents, Formatting.None, OrderEventJsonConverter);

            File.WriteAllText(path, data);
        }

        /// <summary>
        /// Gets the orders generated starting from the provided <see cref="ITransactionHandler.OrderEvents"/> position
        /// </summary>
        /// <returns>The delta orders</returns>
        protected virtual Dictionary<int, Order> GetDeltaOrders(int orderEventsStartPosition, Func<int, bool> shouldStop)
        {
            var deltaOrders = new Dictionary<int, Order>();

            foreach (var orderId in TransactionHandler.OrderEvents.Skip(orderEventsStartPosition).Select(orderEvent => orderEvent.OrderId))
            {
                LastDeltaOrderPosition++;
                if (deltaOrders.ContainsKey(orderId))
                {
                    // we can have more than 1 order event per order id
                    continue;
                }

                var order = Algorithm.Transactions.GetOrderById(orderId);
                if (order == null)
                {
                    // this shouldn't happen but just in case
                    continue;
                }

                // for charting
                order.Price = order.Price.SmartRounding();

                deltaOrders[orderId] = order;

                if (shouldStop(deltaOrders.Count))
                {
                    break;
                }
            }

            return deltaOrders;
        }

        /// <summary>
        /// Initialize the result handler with this result packet.
        /// </summary>
        /// <param name="job">Algorithm job packet for this result handler</param>
        /// <param name="messagingHandler">The handler responsible for communicating messages to listeners</param>
        /// <param name="api">The api instance used for handling logs</param>
        /// <param name="transactionHandler">The transaction handler used to get the algorithms <see cref="Order"/> information</param>
        public virtual void Initialize(AlgorithmNodePacket job, IMessagingHandler messagingHandler, IApi api, ITransactionHandler transactionHandler)
        {
            _hostName = job.HostName ?? Environment.MachineName;
            MessagingHandler = messagingHandler;
            TransactionHandler = transactionHandler;
            CompileId = job.CompileId;
            AlgorithmId = job.AlgorithmId;
            ProjectId = job.ProjectId;
            RamAllocation = job.RamAllocation.ToStringInvariant();
            OrderEventJsonConverter = new OrderEventJsonConverter(AlgorithmId);
            _updateRunner = new Thread(Run, 0) { IsBackground = true, Name = "Result Thread" };
            _updateRunner.Start();
        }

        /// <summary>
        /// Result handler update method
        /// </summary>
        protected abstract void Run();

        /// <summary>
        /// Gets the full path for a results file
        /// </summary>
        /// <param name="filename">The filename to add to the path</param>
        /// <returns>The full path, including the filename</returns>
        protected string GetResultsPath(string filename)
        {
            return Path.Combine(ResultsDestinationFolder, filename);
        }

        /// <summary>
        /// Event fired each time that we add/remove securities from the data feed
        /// </summary>
        public virtual void OnSecuritiesChanged(SecurityChanges changes)
        {
        }

        /// <summary>
        /// Returns the location of the logs
        /// </summary>
        /// <param name="id">Id that will be incorporated into the algorithm log name</param>
        /// <param name="logs">The logs to save</param>
        /// <returns>The path to the logs</returns>
        public virtual string SaveLogs(string id, List<LogEntry> logs)
        {
            var filename = $"{id}-log.txt";
            var path = GetResultsPath(filename);
            var logLines = logs.Select(x => x.Message);
            File.WriteAllLines(path, logLines);
            return path;
        }

        /// <summary>
        /// Save the results to disk
        /// </summary>
        /// <param name="name">The name of the results</param>
        /// <param name="result">The results to save</param>
        public virtual void SaveResults(string name, Result result)
        {
            File.WriteAllText(GetResultsPath(name), JsonConvert.SerializeObject(result, Formatting.Indented));
        }

        /// <summary>
        /// Sets the current alpha runtime statistics
        /// </summary>
        /// <param name="statistics">The current alpha runtime statistics</param>
        public virtual void SetAlphaRuntimeStatistics(AlphaRuntimeStatistics statistics)
        {
            AlphaRuntimeStatistics = statistics;
        }

        /// <summary>
        /// Purge/clear any outstanding messages in message queue.
        /// </summary>
        protected void PurgeQueue()
        {
            Messages.Clear();
        }

        /// <summary>
        /// Stops the update runner task
        /// </summary>
        protected void StopUpdateRunner()
        {
            _updateRunner.StopSafely(TimeSpan.FromMinutes(10));
            _updateRunner = null;
        }

        /// <summary>
        /// Gets the algorithm net return
        /// </summary>
        protected decimal GetNetReturn()
        {
            //Some users have $0 in their brokerage account / starting cash of $0. Prevent divide by zero errors
            return StartingPortfolioValue > 0 ?
                (Algorithm.Portfolio.TotalPortfolioValue - StartingPortfolioValue) / StartingPortfolioValue
                : 0;
        }

        /// <summary>
        /// Save the snapshot of the total results to storage.
        /// </summary>
        /// <param name="packet">Packet to store.</param>
        protected abstract void StoreResult(Packet packet);

        /// <summary>
        /// Gets the current portfolio value
        /// </summary>
        /// <remarks>Useful so that live trading implementation can freeze the returned value if there is no user exchange open
        /// so we ignore extended market hours updates</remarks>
        protected virtual decimal GetPortfolioValue()
        {
            return Algorithm.Portfolio.TotalPortfolioValue;
        }

        /// <summary>
        /// Gets the current benchmark value
        /// </summary>
        /// <remarks>Useful so that live trading implementation can freeze the returned value if there is no user exchange open
        /// so we ignore extended market hours updates</remarks>
        protected virtual decimal GetBenchmarkValue()
        {
            return Algorithm.Benchmark.Evaluate(PreviousUtcSampleTime).SmartRounding();
        }

        /// <summary>
        /// Samples portfolio equity, benchmark, and daily performance
        /// </summary>
        /// <param name="time">Current UTC time in the AlgorithmManager loop</param>
        /// <param name="force">Force sampling of equity, benchmark, and performance to be </param>
        public virtual void Sample(DateTime time, bool force = false)
        {
            var dayChanged = PreviousUtcSampleTime.Date != time.Date;

            if (dayChanged || force)
            {
                if (force)
                {
                    // For any forced sampling, we need to sample at the time we provide to this method.
                    PreviousUtcSampleTime = time;
                }

                var currentPortfolioValue = GetPortfolioValue();
                var portfolioPerformance = DailyPortfolioValue == 0 ? 0 : Math.Round((currentPortfolioValue - DailyPortfolioValue) * 100 / DailyPortfolioValue, 10);

                SampleEquity(PreviousUtcSampleTime, currentPortfolioValue);
                SampleBenchmark(PreviousUtcSampleTime, GetBenchmarkValue());
                SamplePerformance(PreviousUtcSampleTime, portfolioPerformance);

                // If the day changed, set the closing portfolio value. Otherwise, we would end up
                // with skewed statistics if a processing event was forced.
                if (dayChanged)
                {
                    DailyPortfolioValue = currentPortfolioValue;
                }
            }

            // this time goes into the sample, we keep him updated because sample is called before we update anything, so the sampled values are from the last call
            PreviousUtcSampleTime = time;
        }

        /// <summary>
        /// Sample the current equity of the strategy directly with time-value pair.
        /// </summary>
        /// <param name="time">Time of the sample.</param>
        /// <param name="value">Current equity value.</param>
        protected virtual void SampleEquity(DateTime time, decimal value)
        {
            var accountCurrencySymbol = Currencies.GetCurrencySymbol(Algorithm.AccountCurrency);

            Sample("Strategy Equity", "Equity", 0, SeriesType.Candle, time, value, accountCurrencySymbol);
        }

        /// <summary>
        /// Sample the current daily performance directly with a time-value pair.
        /// </summary>
        /// <param name="time">Time of the sample.</param>
        /// <param name="value">Current daily performance value.</param>
        protected virtual void SamplePerformance(DateTime time, decimal value)
        {
            if (Log.DebuggingEnabled)
            {
                Log.Debug("BaseResultsHandler.SamplePerformance(): " + time.ToShortTimeString() + " >" + value);
            }
            Sample("Strategy Equity", "Daily Performance", 1, SeriesType.Bar, time, value, "%");
        }

        /// <summary>
        /// Sample the current benchmark performance directly with a time-value pair.
        /// </summary>
        /// <param name="time">Time of the sample.</param>
        /// <param name="value">Current benchmark value.</param>
        /// <seealso cref="IResultHandler.Sample"/>
        protected virtual void SampleBenchmark(DateTime time, decimal value)
        {
            Sample("Benchmark", "Benchmark", 0, SeriesType.Line, time, value);
        }

        /// <summary>
        /// Add a sample to the chart specified by the chartName, and seriesName.
        /// </summary>
        /// <param name="chartName">String chart name to place the sample.</param>
        /// <param name="seriesName">Series name for the chart.</param>
        /// <param name="seriesIndex">Series chart index - which chart should this series belong</param>
        /// <param name="seriesType">Series type for the chart.</param>
        /// <param name="time">Time for the sample</param>
        /// <param name="value">Value for the chart sample.</param>
        /// <param name="unit">Unit for the chart axis</param>
        /// <remarks>Sample can be used to create new charts or sample equity - daily performance.</remarks>
        protected abstract void Sample(string chartName,
            string seriesName,
            int seriesIndex,
            SeriesType seriesType,
            DateTime time,
            decimal value,
            string unit = "$");

        /// <summary>
        /// Gets the algorithm runtime statistics
        /// </summary>
        protected Dictionary<string, string> GetAlgorithmRuntimeStatistics(Dictionary<string, string> summary,
            Dictionary<string, string> runtimeStatistics = null)
        {
            if (runtimeStatistics == null)
            {
                runtimeStatistics = new Dictionary<string, string>();
            }

            if (summary.ContainsKey("Probabilistic Sharpe Ratio"))
            {
                runtimeStatistics["Probabilistic Sharpe Ratio"] = summary["Probabilistic Sharpe Ratio"];
            }
            else
            {
                runtimeStatistics["Probabilistic Sharpe Ratio"] = "0%";
            }

            var accountCurrencySymbol = Currencies.GetCurrencySymbol(Algorithm.AccountCurrency);

            runtimeStatistics["Unrealized"] = accountCurrencySymbol + Algorithm.Portfolio.TotalUnrealizedProfit.ToStringInvariant("N2");
            runtimeStatistics["Fees"] = $"-{accountCurrencySymbol}{Algorithm.Portfolio.TotalFees.ToStringInvariant("N2")}";
            runtimeStatistics["Net Profit"] = accountCurrencySymbol + Algorithm.Portfolio.TotalProfit.ToStringInvariant("N2");
            runtimeStatistics["Return"] = GetNetReturn().ToStringInvariant("P");
            runtimeStatistics["Equity"] = accountCurrencySymbol + Algorithm.Portfolio.TotalPortfolioValue.ToStringInvariant("N2");
            runtimeStatistics["Holdings"] = accountCurrencySymbol + Algorithm.Portfolio.TotalHoldingsValue.ToStringInvariant("N2");
            runtimeStatistics["Volume"] = accountCurrencySymbol + Algorithm.Portfolio.TotalSaleVolume.ToStringInvariant("N2");

            return runtimeStatistics;
        }

        /// <summary>
        /// Will generate the statistics results and update the provided runtime statistics
        /// </summary>
        protected StatisticsResults GenerateStatisticsResults(Dictionary<string, Chart> charts,
            SortedDictionary<DateTime, decimal> profitLoss = null)
        {
            var statisticsResults = new StatisticsResults();
            if (profitLoss == null)
            {
                profitLoss = new SortedDictionary<DateTime, decimal>();
            }

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
                    charts[strategyEquityKey].Series.ContainsKey(dailyPerformanceKey) &&
                    charts.ContainsKey(benchmarkKey) &&
                    charts[benchmarkKey].Series.ContainsKey(benchmarkKey))
                {
                    var equity = charts[strategyEquityKey].Series[equityKey].Values;
                    var performance = charts[strategyEquityKey].Series[dailyPerformanceKey].Values;
                    var totalTransactions = Algorithm.Transactions.GetOrders(x => x.Status.IsFill()).Count();
                    var benchmark = charts[benchmarkKey].Series[benchmarkKey].Values;

                    var trades = Algorithm.TradeBuilder.ClosedTrades;

                    statisticsResults = StatisticsBuilder.Generate(trades, profitLoss, equity, performance, benchmark,
                        StartingPortfolioValue, Algorithm.Portfolio.TotalFees, totalTransactions);
                }
            }
            catch (Exception err)
            {
                Log.Error(err, "BaseResultsHandler.GenerateStatisticsResults(): Error generating statistics packet");
            }

            return statisticsResults;
        }

        /// <summary>
        /// Save an algorithm message to the log store. Uses a different timestamped method of adding messaging to interweve debug and logging messages.
        /// </summary>
        /// <param name="message">String message to store</param>
        protected abstract void AddToLogStore(string message);

        /// <summary>
        /// Processes algorithm logs.
        /// Logs of the same type are batched together one per line and are sent out
        /// </summary>
        protected void ProcessAlgorithmLogs(int? messageQueueLimit = null)
        {
            ProcessAlgorithmLogsImpl(Algorithm.DebugMessages, PacketType.Debug, messageQueueLimit);
            ProcessAlgorithmLogsImpl(Algorithm.ErrorMessages, PacketType.HandledError, messageQueueLimit);
            ProcessAlgorithmLogsImpl(Algorithm.LogMessages, PacketType.Log, messageQueueLimit);
        }

        private void ProcessAlgorithmLogsImpl(ConcurrentQueue<string> concurrentQueue, PacketType packetType, int? messageQueueLimit = null)
        {
            if (concurrentQueue.Count <= 0)
            {
                return;
            }

            var result = new List<string>();
            var endTime = DateTime.UtcNow.AddMilliseconds(250).Ticks;
            string message;
            var currentMessageCount = -1;
            while (DateTime.UtcNow.Ticks < endTime && concurrentQueue.TryDequeue(out message))
            {
                if (messageQueueLimit.HasValue)
                {
                    if (currentMessageCount == -1)
                    {
                        // this is expensive, so let's get it once
                        currentMessageCount = Messages.Count;
                    }
                    if (currentMessageCount > messageQueueLimit)
                    {
                        //if too many in the queue already skip the logging and drop the messages
                        continue;
                    }
                }
                AddToLogStore(message);
                result.Add(message);
                // increase count after we add
                currentMessageCount++;
            }

            if (result.Count > 0)
            {
                message = string.Join(Environment.NewLine, result);
                if (packetType == PacketType.Debug)
                {
                    Messages.Enqueue(new DebugPacket(ProjectId, AlgorithmId, CompileId, message));
                }
                else if (packetType == PacketType.Log)
                {
                    Messages.Enqueue(new LogPacket(AlgorithmId, message));
                }
                else if (packetType == PacketType.HandledError)
                {
                    Messages.Enqueue(new HandledErrorPacket(AlgorithmId, message));
                }
            }
        }
    }
}
