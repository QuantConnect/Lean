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
using Newtonsoft.Json.Serialization;
using QuantConnect.Configuration;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Indicators;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.TransactionHandlers;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Orders.Serialization;
using QuantConnect.Packets;
using QuantConnect.Securities.Positions;
using QuantConnect.Statistics;

namespace QuantConnect.Lean.Engine.Results
{
    /// <summary>
    /// Provides base functionality to the implementations of <see cref="IResultHandler"/>
    /// </summary>
    public abstract class BaseResultsHandler
    {
        private RollingWindow<decimal> _previousSalesVolume;
        private DateTime _previousPortfolioTurnoverSample;
        private bool _packetDroppedWarning;
        private int _logCount;
        private ConcurrentDictionary<string, string> _customSummaryStatistics;
        // used for resetting out/error upon completion
        private static readonly TextWriter StandardOut = Console.Out;
        private static readonly TextWriter StandardError = Console.Error;

        private string _hostName;

        private Bar _currentAlgorithmEquity;

        public const string StrategyEquityKey = "Strategy Equity";
        public const string EquityKey = "Equity";
        public const string ReturnKey = "Return";
        public const string BenchmarkKey = "Benchmark";
        public const string DrawdownKey = "Drawdown";
        public const string PortfolioTurnoverKey = "Portfolio Turnover";
        public const string PortfolioMarginKey = "Portfolio Margin";

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
        /// Serializer settings to use
        /// </summary>
        protected JsonSerializerSettings SerializerSettings { get; set; } = new ()
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy
                {
                    ProcessDictionaryKeys = false,
                    OverrideSpecifiedNames = true
                }
            }
        };

        /// <summary>
        /// The current aggregated equity bar for sampling.
        /// It will be aggregated with values from the <see cref="GetPortfolioValue"/>
        /// </summary>
        protected Bar CurrentAlgorithmEquity
        {
            get
            {
                if (_currentAlgorithmEquity == null)
                {
                    _currentAlgorithmEquity = new Bar();
                    UpdateAlgorithmEquity(_currentAlgorithmEquity);
                }
                return _currentAlgorithmEquity;
            }
            set
            {
                _currentAlgorithmEquity = value;
            }
        }

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
        protected List<string> AlgorithmPerformanceCharts { get; } = new List<string> { StrategyEquityKey, BenchmarkKey };

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
        /// State of the algorithm
        /// </summary>
        protected Dictionary<string, string> State { get; set; }

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
        protected virtual IAlgorithm Algorithm { get; set; }

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
        /// The map file provider instance to use
        /// </summary>
        protected IMapFileProvider MapFileProvider { get; set; }

        /// <summary>
        /// Creates a new instance
        /// </summary>
        protected BaseResultsHandler()
        {
            ExitEvent = new ManualResetEvent(false);
            Charts = new ConcurrentDictionary<string, Chart>();
            //Default charts:
            var equityChart = Charts[StrategyEquityKey] = new Chart(StrategyEquityKey);
            equityChart.Series.Add(EquityKey, new CandlestickSeries(EquityKey, 0, "$"));
            equityChart.Series.Add(ReturnKey, new Series(ReturnKey, SeriesType.Bar, 1, "%"));

            Messages = new ConcurrentQueue<Packet>();
            RuntimeStatistics = new Dictionary<string, string>();
            StartTime = DateTime.UtcNow;
            CompileId = "";
            AlgorithmId = "";
            ChartLock = new object();
            LogStore = new List<LogEntry>();
            ResultsDestinationFolder = Globals.ResultsDestinationFolder;
            State = new Dictionary<string, string>
            {
                ["StartTime"] = StartTime.ToStringInvariant(DateFormat.UI),
                ["EndTime"] = string.Empty,
                ["RuntimeError"] = string.Empty,
                ["StackTrace"] = string.Empty,
                ["LogCount"] = "0",
                ["OrderCount"] = "0",
                ["InsightCount"] = "0"
            };
            _previousSalesVolume = new(2);
            _previousSalesVolume.Add(0);
            _customSummaryStatistics = new();
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

            var data = JsonConvert.SerializeObject(orderEvents, Formatting.None, SerializerSettings);

            File.WriteAllText(path, data);
        }

        /// <summary>
        /// Save insight results to persistent storage
        /// </summary>
        /// <remarks>Method called by the storing timer and on exit</remarks>
        protected virtual void StoreInsights()
        {
            if (Algorithm?.Insights == null)
            {
                // could be null if we are not initialized and exit is called
                return;
            }
            // default save all results to disk and don't remove any from memory
            // this will result in one file with all of the insights/results in it
            var allInsights = Algorithm.Insights.GetInsights();
            if (allInsights.Count > 0)
            {
                var alphaResultsPath = GetResultsPath(Path.Combine(AlgorithmId, "alpha-results.json"));
                var directory = Directory.GetParent(alphaResultsPath);
                if (!directory.Exists)
                {
                    directory.Create();
                }
                var orderedInsights = allInsights.OrderBy(insight => insight.GeneratedTimeUtc);
                File.WriteAllText(alphaResultsPath, JsonConvert.SerializeObject(orderedInsights, Formatting.Indented, SerializerSettings));
            }
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
        /// <param name="parameters">DTO parameters class to initialize a result handler</param>
        public virtual void Initialize(ResultHandlerInitializeParameters parameters)
        {
            _hostName = parameters.Job.HostName ?? Environment.MachineName;
            MessagingHandler = parameters.MessagingHandler;
            TransactionHandler = parameters.TransactionHandler;
            CompileId = parameters.Job.CompileId;
            AlgorithmId = parameters.Job.AlgorithmId;
            ProjectId = parameters.Job.ProjectId;
            RamAllocation = parameters.Job.RamAllocation.ToStringInvariant();
            _updateRunner = new Thread(Run, 0) { IsBackground = true, Name = "Result Thread" };
            _updateRunner.Start();
            State["Hostname"] = _hostName;
            MapFileProvider = parameters.MapFileProvider;

            SerializerSettings = new()
            {
                Converters = new [] { new OrderEventJsonConverter(AlgorithmId) },
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new CamelCaseNamingStrategy
                    {
                        ProcessDictionaryKeys = false,
                        OverrideSpecifiedNames = true
                    }
                }
            };
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
            File.WriteAllText(GetResultsPath(name), JsonConvert.SerializeObject(result, Formatting.Indented, SerializerSettings));
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
            if (Algorithm == null || Algorithm.Benchmark == null)
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
            UpdateAlgorithmEquity();
            SampleEquity(time);
            SampleBenchmark(time, GetBenchmarkValue(time));
            SamplePerformance(time, portfolioPerformance);
            SampleDrawdown(time, currentPortfolioValue);
            SampleSalesVolume(time);
            SampleExposure(time, currentPortfolioValue);
            SampleCapacity(time);
            SamplePortfolioTurnover(time, currentPortfolioValue);
            SamplePortfolioMargin(time, currentPortfolioValue);

            // Update daily portfolio value; works because we only call sample once a day
            DailyPortfolioValue = currentPortfolioValue;
        }

        private void SamplePortfolioMargin(DateTime algorithmUtcTime, decimal currentPortfolioValue)
        {
            var state = PortfolioState.Create(Algorithm.Portfolio, algorithmUtcTime, currentPortfolioValue);

            lock (ChartLock)
            {
                if (!Charts.TryGetValue(PortfolioMarginKey, out var chart))
                {
                    chart = new Chart(PortfolioMarginKey);
                    Charts.AddOrUpdate(PortfolioMarginKey, chart);
                }
                PortfolioMarginChart.AddSample(chart, state, MapFileProvider, DateTime.UtcNow.Date);
            }
        }

        /// <summary>
        /// Sample the current equity of the strategy directly with time and using
        /// the current algorithm equity value in <see cref="CurrentAlgorithmEquity"/>
        /// </summary>
        /// <param name="time">Equity candlestick end time</param>
        protected virtual void SampleEquity(DateTime time)
        {
            Sample(StrategyEquityKey, EquityKey, 0, SeriesType.Candle, new Candlestick(time, CurrentAlgorithmEquity), AlgorithmCurrencySymbol);

            // Reset the current algorithm equity object so another bar is create on the next sample
            CurrentAlgorithmEquity = null;
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
            Sample(StrategyEquityKey, ReturnKey, 1, SeriesType.Bar, new ChartPoint(time, value), "%");
        }

        /// <summary>
        /// Sample the current benchmark performance directly with a time-value pair.
        /// </summary>
        /// <param name="time">Time of the sample.</param>
        /// <param name="value">Current benchmark value.</param>
        /// <seealso cref="IResultHandler.Sample"/>
        protected virtual void SampleBenchmark(DateTime time, decimal value)
        {
            Sample(BenchmarkKey, BenchmarkKey, 0, SeriesType.Line, new ChartPoint(time, value));
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
                Sample(DrawdownKey, "Equity Drawdown", 0, SeriesType.Line, new ChartPoint(time, drawdown), "%");
            }
        }

        /// <summary>
        /// Sample portfolio turn over of the strategy
        /// </summary>
        /// <param name="time">Time of the sample</param>
        /// <param name="currentPortfolioValue">Current equity value</param>
        protected virtual void SamplePortfolioTurnover(DateTime time, decimal currentPortfolioValue)
        {
            if (currentPortfolioValue != 0)
            {
                if (Algorithm.StartDate == time.ConvertFromUtc(Algorithm.TimeZone))
                {
                    // the first sample in backtesting is at start, we only want to sample after a full algorithm execution date
                    return;
                }
                var currentTotalSaleVolume = Algorithm.Portfolio.TotalSaleVolume;

                decimal todayPortfolioTurnOver;
                if (_previousPortfolioTurnoverSample == time)
                {
                    // we are sampling the same time twice, this can happen if we sample at the start of the portfolio loop
                    // and the algorithm happen to end at the same time and we trigger the final sample to take into account that last loop
                    // this new sample will overwrite the previous, so we resample using T-2 sales volume
                    todayPortfolioTurnOver = (currentTotalSaleVolume - _previousSalesVolume[1]) / currentPortfolioValue;
                }
                else
                {
                    todayPortfolioTurnOver = (currentTotalSaleVolume - _previousSalesVolume[0]) / currentPortfolioValue;
                }

                _previousSalesVolume.Add(currentTotalSaleVolume);
                _previousPortfolioTurnoverSample = time;

                Sample(PortfolioTurnoverKey, PortfolioTurnoverKey, 0, SeriesType.Line, new ChartPoint(time, todayPortfolioTurnOver), "%");
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
                Sample("Assets Sales Volume", $"{holding.Symbol.Value}", 0, SeriesType.Treemap, new ChartPoint(time, holding.TotalSaleVolume),
                    AlgorithmCurrencySymbol);
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
            foreach (var holding in Algorithm.Portfolio.Values)
            {
                // Ensure we have a value for this security type in both our dictionaries
                if (!longHoldings.ContainsKey(holding.Symbol.SecurityType))
                {
                    longHoldings.Add(holding.Symbol.SecurityType, 0);
                    shortHoldings.Add(holding.Symbol.SecurityType, 0);
                }

                var holdingsValue = holding.HoldingsValue;
                if (holdingsValue == 0)
                {
                    continue;
                }

                // Long Position
                if (holdingsValue > 0)
                {
                    longHoldings[holding.Symbol.SecurityType] += holdingsValue;
                }
                // Short Position
                else
                {
                    shortHoldings[holding.Symbol.SecurityType] += holdingsValue;
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
                Sample("Exposure", $"{kvp.Key} - {type} Ratio", 0, SeriesType.Line, new ChartPoint(time, ratio),
                    "");
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
        /// <param name="value">Value for the chart sample.</param>
        /// <param name="unit">Unit for the chart axis</param>
        /// <remarks>Sample can be used to create new charts or sample equity - daily performance.</remarks>
        protected abstract void Sample(string chartName,
            string seriesName,
            int seriesIndex,
            SeriesType seriesType,
            ISeriesPoint value,
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

            return runtimeStatistics;
        }

        /// <summary>
        /// Sets the algorithm state data
        /// </summary>
        protected void SetAlgorithmState(string error, string stack)
        {
            State["RuntimeError"] = error;
            State["StackTrace"] = stack;
        }

        /// <summary>
        /// Gets the algorithm state data
        /// </summary>
        protected Dictionary<string, string> GetAlgorithmState(DateTime? endTime = null)
        {
            if (Algorithm == null || !string.IsNullOrEmpty(State["RuntimeError"]))
            {
                State["Status"] = AlgorithmStatus.RuntimeError.ToStringInvariant();
            }
            else
            {
                State["Status"] = Algorithm.Status.ToStringInvariant();
            }
            State["EndTime"] = endTime != null ? endTime.ToStringInvariant(DateFormat.UI) : string.Empty;

            lock (LogStore)
            {
                State["LogCount"] = _logCount.ToStringInvariant();
            }
            State["OrderCount"] = Algorithm?.Transactions?.OrdersCount.ToStringInvariant() ?? "0";
            State["InsightCount"] = Algorithm?.Insights.TotalCount.ToStringInvariant() ?? "0";

            return State;
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

                // make sure we've taken samples for these series before just blindly requesting them
                if (charts.TryGetValue(StrategyEquityKey, out var strategyEquity) &&
                    strategyEquity.Series.TryGetValue(EquityKey, out var equity) &&
                    strategyEquity.Series.TryGetValue(ReturnKey, out var performance) &&
                    charts.TryGetValue(BenchmarkKey, out var benchmarkChart) &&
                    benchmarkChart.Series.TryGetValue(BenchmarkKey, out var benchmark))
                {
                    var trades = Algorithm.TradeBuilder.ClosedTrades;

                    BaseSeries portfolioTurnover;
                    if (charts.TryGetValue(PortfolioTurnoverKey, out var portfolioTurnoverChart))
                    {
                        portfolioTurnoverChart.Series.TryGetValue(PortfolioTurnoverKey, out portfolioTurnover);
                    }
                    else
                    {
                        portfolioTurnover = new Series();
                    }

                    statisticsResults = StatisticsBuilder.Generate(trades, profitLoss, equity.Values, performance.Values, benchmark.Values,
                        portfolioTurnover.Values, StartingPortfolioValue, Algorithm.Portfolio.TotalFees, TotalTradesCount(),
                        estimatedStrategyCapacity, AlgorithmCurrencySymbol, Algorithm.Transactions, Algorithm.RiskFreeInterestRateModel,
                        Algorithm.Settings.TradingDaysPerYear.Value // already set in Brokerage|Backtesting-SetupHandler classes
                        );
                }

                statisticsResults.AddCustomSummaryStatistics(_customSummaryStatistics);
            }
            catch (Exception err)
            {
                Log.Error(err, "BaseResultsHandler.GenerateStatisticsResults(): Error generating statistics packet");
            }

            return statisticsResults;
        }

        /// <summary>
        /// Helper method to get the total trade count statistic
        /// </summary>
        protected int TotalTradesCount()
        {
            return TransactionHandler?.OrdersCount ?? 0;
        }

        /// <summary>
        /// Calculates and gets the current statistics for the algorithm.
        /// It will use the current <see cref="Charts"/> and profit loss information calculated from the current transaction record
        /// to generate the results.
        /// </summary>
        /// <returns>The current statistics</returns>
        protected StatisticsResults GenerateStatisticsResults(CapacityEstimate estimatedStrategyCapacity = null)
        {
            // could happen if algorithm failed to init
            if (Algorithm == null)
            {
                return new StatisticsResults();
            }

            Dictionary<string, Chart> charts;
            lock (ChartLock)
            {
                charts = new(Charts);
            }
            var profitLoss = new SortedDictionary<DateTime, decimal>(Algorithm.Transactions.TransactionRecord);

            return GenerateStatisticsResults(charts, profitLoss, estimatedStrategyCapacity);
        }

        /// <summary>
        /// Save an algorithm message to the log store. Uses a different timestamped method of adding messaging to interweve debug and logging messages.
        /// </summary>
        /// <param name="message">String message to store</param>
        protected virtual void AddToLogStore(string message)
        {
            lock (LogStore)
            {
                LogStore.Add(new LogEntry(message));
                _logCount++;
            }
        }

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

        /// <summary>
        /// Sets or updates a custom summary statistic
        /// </summary>
        /// <param name="name">The statistic name</param>
        /// <param name="value">The statistic value</param>
        protected void SummaryStatistic(string name, string value)
        {
            _customSummaryStatistics.AddOrUpdate(name, value);
        }

        /// <summary>
        /// Updates the current equity bar with the current equity value from <see cref="GetPortfolioValue"/>
        /// </summary>
        /// <remarks>
        /// This is required in order to update the <see cref="CurrentAlgorithmEquity"/> bar without using the getter,
        /// which would cause the bar to be created if it doesn't exist.
        /// </remarks>
        private void UpdateAlgorithmEquity(Bar equity)
        {
            equity.Update(Math.Round(GetPortfolioValue(), 4));
        }

        /// <summary>
        /// Updates the current equity bar with the current equity value from <see cref="GetPortfolioValue"/>
        /// </summary>
        protected void UpdateAlgorithmEquity()
        {
            UpdateAlgorithmEquity(CurrentAlgorithmEquity);
        }
    }
}
