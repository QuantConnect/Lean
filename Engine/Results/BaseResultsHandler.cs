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
        private bool _packetDroppedWarning;
        // used for resetting out/error upon completion
        private static readonly TextWriter StandardOut = Console.Out;
        private static readonly TextWriter StandardError = Console.Error;

        private string _hostName;

        /// <summary>
        /// The main loop update interval
        /// </summary>
        protected virtual TimeSpan MainUpdateInterval { get; } = TimeSpan.FromSeconds(3);

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
        /// Event set when exit is triggered
        /// </summary>
        protected ManualResetEvent ExitEvent { get; }

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
        /// State of the result packet
        /// </summary>
        protected Dictionary<string, string> State { get; }

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
        /// Algorithm currency symbol, used in charting
        /// </summary>
        protected string AlgorithmCurrencySymbol { get; set; }

        /// <summary>
        /// Closing portfolio value. Used to calculate daily performance.
        /// </summary>
        protected decimal DailyPortfolioValue;

        /// <summary>
        /// Cumulative max portfolio value. Used to calculate drawdown underwater.
        /// </summary>
        protected decimal CumulativeMaxPortfolioValue;

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
            ExitEvent = new ManualResetEvent(false);
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
        /// <param name="time">Time to resolve benchmark value at</param>
        protected virtual decimal GetBenchmarkValue(DateTime time)
        {
            if(Algorithm == null || Algorithm.Benchmark == null)
            {
                // this could happen if the algorithm exploded mid initialization
                return 0;
            }
            return Algorithm.Benchmark.Evaluate(time).SmartRounding();
        }

        /// <summary>
        /// Samples portfolio equity, benchmark, and daily performance
        /// Called by scheduled event every night at midnight algorithm time
        /// </summary>
        /// <param name="time">Current UTC time in the AlgorithmManager loop</param>
        public virtual void Sample(DateTime time)
        {
            var currentPortfolioValue = GetPortfolioValue();
            var portfolioPerformance = DailyPortfolioValue == 0 ? 0 : Math.Round((currentPortfolioValue - DailyPortfolioValue) * 100 / DailyPortfolioValue, 10);

            // Update our max portfolio value
            CumulativeMaxPortfolioValue = Math.Max(currentPortfolioValue, CumulativeMaxPortfolioValue);

            // Sample all our default charts
            SampleEquity(time, currentPortfolioValue);
            SampleBenchmark(time, GetBenchmarkValue(time));
            SamplePerformance(time, portfolioPerformance);
            SampleDrawdown(time, currentPortfolioValue);
            SampleSalesVolume(time);
            SampleExposure(time, currentPortfolioValue);
            SampleCapacity(time);

            // Update daily portfolio value; works because we only call sample once a day
            DailyPortfolioValue = currentPortfolioValue;
        }

        /// <summary>
        /// Sample the current equity of the strategy directly with time-value pair.
        /// </summary>
        /// <param name="time">Time of the sample.</param>
        /// <param name="value">Current equity value.</param>
        protected virtual void SampleEquity(DateTime time, decimal value)
        {
            Sample("Strategy Equity", "Equity", 0, SeriesType.Candle, time, value, AlgorithmCurrencySymbol);
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
        /// Sample drawdown of equity of the strategy
        /// </summary>
        /// <param name="time">Time of the sample</param>
        /// <param name="currentPortfolioValue">Current equity value</param>
        protected virtual void SampleDrawdown(DateTime time, decimal currentPortfolioValue)
        {
            // This will throw otherwise, in this case just don't sample
            if (CumulativeMaxPortfolioValue != 0)
            {
                // Calculate our drawdown and sample it
                var drawdown = Statistics.Statistics.DrawdownPercent(currentPortfolioValue, CumulativeMaxPortfolioValue);
                Sample("Drawdown", "Equity Drawdown", 0, SeriesType.Line, time, drawdown, "%");
            }
        }

        /// <summary>
        /// Sample assets sales volume
        /// </summary>
        /// <param name="time">Time of the sample</param>
        protected virtual void SampleSalesVolume(DateTime time)
        {
            // Sample top 30 holdings by sales volume
            foreach (var holding in Algorithm.Portfolio.Values.Where(y => y.TotalSaleVolume != 0)
                .OrderByDescending(x => x.TotalSaleVolume).Take(30))
            {
                Sample("Assets Sales Volume", $"{holding.Symbol.Value}", 0, SeriesType.Treemap, time,
                    holding.TotalSaleVolume, AlgorithmCurrencySymbol);
            }
        }

        /// <summary>
        /// Sample portfolio exposure long/short ratios by security type
        /// </summary>
        /// <param name="time">Time of the sample</param>
        /// <param name="currentPortfolioValue">Current value of the portfolio</param>
        protected virtual void SampleExposure(DateTime time, decimal currentPortfolioValue)
        {
            // Will throw in this case, just return without sampling
            if (currentPortfolioValue == 0)
            {
                return;
            }

            // Split up our holdings in one enumeration into long and shorts holding values
            // only process those that we hold stock in.
            var shortHoldings = new Dictionary<SecurityType, decimal>();
            var longHoldings = new Dictionary<SecurityType, decimal>();
            foreach (var holding in Algorithm.Portfolio.Values.Where(x => x.HoldStock))
            {
                // Ensure we have a value for this security type in both our dictionaries
                if (!longHoldings.ContainsKey(holding.Symbol.SecurityType))
                {
                    longHoldings.Add(holding.Symbol.SecurityType, 0);
                    shortHoldings.Add(holding.Symbol.SecurityType, 0);
                }

                // Long Position
                if (holding.HoldingsValue > 0)
                {
                    longHoldings[holding.Symbol.SecurityType] += holding.HoldingsValue;
                }
                // Short Position
                else
                {
                    shortHoldings[holding.Symbol.SecurityType] += holding.HoldingsValue;
                }
            }

            // Sample our long and short positions
            SampleExposureHelper(PositionSide.Long, time, currentPortfolioValue, longHoldings);
            SampleExposureHelper(PositionSide.Short, time, currentPortfolioValue, shortHoldings);
        }

        /// <summary>
        /// Helper method for SampleExposure, samples our holdings value to
        /// our exposure chart by their position side and security type
        /// </summary>
        /// <param name="type">Side to sample from portfolio</param>
        /// <param name="time">Time of the sample</param>
        /// <param name="currentPortfolioValue">Current value of the portfolio</param>
        /// <param name="holdings">Enumerable of holdings to sample</param>
        private void SampleExposureHelper(PositionSide type, DateTime time, decimal currentPortfolioValue, Dictionary<SecurityType, decimal> holdings)
        {
            foreach (var kvp in holdings)
            {
                var ratio = Math.Round(kvp.Value / currentPortfolioValue, 4);
                Sample("Exposure", $"{kvp.Key} - {type} Ratio", 0, SeriesType.Line, time,
                    ratio, "");
            }
        }

        /// <summary>
        /// Sample estimated strategy capacity
        /// </summary>
        /// <param name="time">Time of the sample</param>
        protected virtual void SampleCapacity(DateTime time)
        {
            // NOP; Used only by BacktestingResultHandler because he owns a CapacityEstimate
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
        protected SortedDictionary<string, string> GetAlgorithmRuntimeStatistics(Dictionary<string, string> summary, CapacityEstimate capacityEstimate = null)
        {
            var runtimeStatistics = new SortedDictionary<string, string>();
            lock (RuntimeStatistics)
            {
                foreach (var pair in RuntimeStatistics)
                {
                    runtimeStatistics.Add(pair.Key, pair.Value);
                }
            }

            if (summary.ContainsKey("Probabilistic Sharpe Ratio"))
            {
                runtimeStatistics["Probabilistic Sharpe Ratio"] = summary["Probabilistic Sharpe Ratio"];
            }
            else
            {
                runtimeStatistics["Probabilistic Sharpe Ratio"] = "0%";
            }

            runtimeStatistics["Unrealized"] = AlgorithmCurrencySymbol + Algorithm.Portfolio.TotalUnrealizedProfit.ToStringInvariant("N2");
            runtimeStatistics["Fees"] = $"-{AlgorithmCurrencySymbol}{Algorithm.Portfolio.TotalFees.ToStringInvariant("N2")}";
            runtimeStatistics["Net Profit"] = AlgorithmCurrencySymbol + Algorithm.Portfolio.TotalNetProfit.ToStringInvariant("N2");
            runtimeStatistics["Return"] = GetNetReturn().ToStringInvariant("P");
            runtimeStatistics["Equity"] = AlgorithmCurrencySymbol + Algorithm.Portfolio.TotalPortfolioValue.ToStringInvariant("N2");
            runtimeStatistics["Holdings"] = AlgorithmCurrencySymbol + Algorithm.Portfolio.TotalHoldingsValue.ToStringInvariant("N2");
            runtimeStatistics["Volume"] = AlgorithmCurrencySymbol + Algorithm.Portfolio.TotalSaleVolume.ToStringInvariant("N2");
            if (capacityEstimate != null)
            {
                runtimeStatistics["Capacity"] = AlgorithmCurrencySymbol + capacityEstimate.Capacity.RoundToSignificantDigits(2).ToFinancialFigures();
            }

            return runtimeStatistics;
        }

        /// <summary>
        /// Gets the algorithm state data
        /// </summary>
        protected Dictionary<string, string> GetAlgorithmState(string endTime = "")
        {
            var state = new Dictionary<string, string>
            {
                ["Status"] = Algorithm.Status.ToStringInvariant(),
                ["StartTime"] = StartTime.ToStringInvariant(),
                ["Hostname"] = _hostName,
                ["EndTime"] = endTime,
                ["RuntimeError"] = "",
                ["StackTrace"] = "",
            };
            if (Algorithm?.RunTimeError != null)
            {
                state["RuntimeError"] = Algorithm.RunTimeError.ToString();
                state["StackTrace"] = Algorithm.RunTimeError.StackTrace;
            }
            return state;
        }

        /// <summary>
        /// Will generate the statistics results and update the provided runtime statistics
        /// </summary>
        protected StatisticsResults GenerateStatisticsResults(Dictionary<string, Chart> charts, 
            SortedDictionary<DateTime, decimal> profitLoss = null, CapacityEstimate estimatedStrategyCapacity = null)
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
                        StartingPortfolioValue, Algorithm.Portfolio.TotalFees, totalTransactions, estimatedStrategyCapacity, AlgorithmCurrencySymbol);
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
            if (concurrentQueue.IsEmpty)
            {
                return;
            }

            var endTime = DateTime.UtcNow.AddMilliseconds(250).Ticks;
            var currentMessageCount = -1;
            while (DateTime.UtcNow.Ticks < endTime && concurrentQueue.TryDequeue(out var message))
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
                        if (!_packetDroppedWarning)
                        {
                            _packetDroppedWarning = true;
                            // this shouldn't happen in most cases, queue limit is high and consumed often but just in case let's not silently drop packets without a warning
                            Messages.Enqueue(new HandledErrorPacket(AlgorithmId, "Your algorithm messaging has been rate limited to prevent browser flooding."));
                        }
                        //if too many in the queue already skip the logging and drop the messages
                        continue;
                    }
                }

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
                AddToLogStore(message);

                // increase count after we add
                currentMessageCount++;
            }
        }
    }
}
