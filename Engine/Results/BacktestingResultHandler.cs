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
using System.Linq;
using System.Threading;
using QuantConnect.Configuration;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.Setup;
using QuantConnect.Lean.Engine.TransactionHandlers;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Packets;
using QuantConnect.Statistics;
using QuantConnect.Util;
using System.Diagnostics;
using QuantConnect.Securities;

namespace QuantConnect.Lean.Engine.Results
{
    /// <summary>
    /// Backtesting result handler passes messages back from the Lean to the User.
    /// </summary>
    public class BacktestingResultHandler : BaseResultsHandler, IResultHandler
    {
        private bool _exitTriggered = false;
        private BacktestNodePacket _job;
        private int _jobDays = 0;
        private string _compileId = "";
        private string _backtestId = "";
        private DateTime _nextUpdate = new DateTime();
        private DateTime _nextS3Update = new DateTime();
        DateTime _lastUpdate = new DateTime();
        private string _debugMessage = "";
        private List<string> _log = new List<string>();
        private string _errorMessage = "";
        private IAlgorithm _algorithm;
        private ConcurrentQueue<Packet> _messages;
        private ConcurrentDictionary<string, Chart> _charts;
        private bool _isActive = true;
        private object _chartLock = new Object();
        private object _runtimeLock = new Object();
        private readonly Dictionary<string, string> _runtimeStatistics = new Dictionary<string, string>();
        private double _daysProcessed = 0;
        private double _lastDaysProcessed = 1;
        private bool _processingFinalPacket = false;

        //Debug variables:
        private int _debugMessageCount = 0;
        private int _debugMessageMin = 100;
        private int _debugMessageMax = 10;
        private int _debugMessageLength = 200;
        private string _debugMessagePeriod = "day";

        //Sampling Periods:
        private TimeSpan _resamplePeriod = TimeSpan.FromMinutes(4);
        private TimeSpan _notificationPeriod = TimeSpan.FromSeconds(2);

        //Processing Time:
        private DateTime _startTime;
        private DateTime _nextSample;
        private IMessagingHandler _messagingHandler;
        private ITransactionHandler _transactionHandler;
        private ISetupHandler _setupHandler;
        private string _algorithmId;
        private int _projectId;

        private const double _samples = 4000;
        private const double _minimumSamplePeriod = 4;

        /// <summary>
        /// Packeting message queue to temporarily store packets and then pull for processing.
        /// </summary>
        public ConcurrentQueue<Packet> Messages
        {
            get
            {
                return _messages;
            }
            set
            {
                _messages = value;
            }
        }

        /// <summary>
        /// Local object access to the algorithm for the underlying Debug and Error messaging.
        /// </summary>
        public IAlgorithm Algorithm
        {
            get
            {
                return _algorithm;
            }
            set
            {
                _algorithm = value;
            }
        }

        /// <summary>
        /// Charts collection for storing the master copy of user charting data.
        /// </summary>
        public ConcurrentDictionary<string, Chart> Charts
        {
            get
            {
                return _charts;
            }
            set
            {
                _charts = value;
            }
        }

        /// <summary>
        /// Boolean flag indicating the result hander thread is completely finished and ready to dispose.
        /// </summary>
        public bool IsActive
        {
            get
            {
                return _isActive;
            }
        }



        /// <summary>
        /// Sampling period for timespans between resamples of the charting equity.
        /// </summary>
        /// <remarks>Specifically critical for backtesting since with such long timeframes the sampled data can get extreme.</remarks>
        public TimeSpan ResamplePeriod
        {
            get
            {
                return _resamplePeriod;
            }
        }

        /// <summary>
        /// How frequently the backtests push messages to the browser.
        /// </summary>
        /// <remarks>Update frequency of notification packets</remarks>
        public TimeSpan NotificationPeriod
        {
            get
            {
                return _notificationPeriod;
            }
        }

        /// <summary>
        /// A dictionary containing summary statistics
        /// </summary>
        public Dictionary<string, string> FinalStatistics { get; private set; }

        /// <summary>
        /// Default initializer for
        /// </summary>
        public BacktestingResultHandler()
        {
            //Initialize Properties:
            _messages = new ConcurrentQueue<Packet>();
            _charts = new ConcurrentDictionary<string, Chart>();
            _chartLock = new Object();
            _isActive = true;

            //Notification Period for Browser Pushes:
            _notificationPeriod = TimeSpan.FromSeconds(2);
            _exitTriggered = false;

            //Set the start time for the algorithm
            _startTime = DateTime.Now;

            //Default charts:
            Charts.AddOrUpdate("Strategy Equity", new Chart("Strategy Equity"));
            Charts["Strategy Equity"].Series.Add("Equity", new Series("Equity", SeriesType.Candle, 0, "$"));
            Charts["Strategy Equity"].Series.Add("Daily Performance", new Series("Daily Performance", SeriesType.Bar, 1, "%"));
        }

        /// <summary>
        /// Initialize the result handler with this result packet.
        /// </summary>
        /// <param name="job">Algorithm job packet for this result handler</param>
        /// <param name="messagingHandler">The handler responsible for communicating messages to listeners</param>
        /// <param name="api">The api instance used for handling logs</param>
        /// <param name="dataFeed"></param>
        /// <param name="setupHandler"></param>
        /// <param name="transactionHandler"></param>
        public virtual void Initialize(AlgorithmNodePacket job, IMessagingHandler messagingHandler, IApi api, IDataFeed dataFeed, ISetupHandler setupHandler, ITransactionHandler transactionHandler)
        {
            _algorithmId = job.AlgorithmId;
            _projectId = job.ProjectId;
            _messagingHandler = messagingHandler;
            _transactionHandler = transactionHandler;
            _setupHandler = setupHandler;
            _job = (BacktestNodePacket)job;
            if (_job == null) throw new Exception("BacktestingResultHandler.Constructor(): Submitted Job type invalid.");
            _compileId = _job.CompileId;
            _backtestId = _job.BacktestId;
        }

        /// <summary>
        /// The main processing method steps through the messaging queue and processes the messages one by one.
        /// </summary>
        public void Run()
        {
            //Initialize:
            var lastMessage = "";
            _lastDaysProcessed = 5;

            //Setup minimum result arrays:
            //SampleEquity(job.periodStart, job.startingCapital);
            //SamplePerformance(job.periodStart, 0);

            try
            {
                while (!(_exitTriggered && Messages.Count == 0))
                {
                    //While there's no work to do, go back to the algorithm:
                    if (Messages.Count == 0)
                    {
                        Thread.Sleep(50);
                    }
                    else
                    {
                        //1. Process Simple Messages in Queue
                        Packet packet;
                        if (Messages.TryDequeue(out packet))
                        {
                            _messagingHandler.Send(packet);
                        }
                    }

                    //2. Update the packet scanner:
                    Update();

                } // While !End.
            }
            catch (Exception err)
            {
                // unexpected error, we need to close down shop
                Log.Error(err);
                // quit the algorithm due to error
                _algorithm.RunTimeError = err;
            }

            Log.Trace("BacktestingResultHandler.Run(): Ending Thread...");
            _isActive = false;
        } // End Run();

        /// <summary>
        /// Send a backtest update to the browser taking a latest snapshot of the charting data.
        /// </summary>
        public void Update()
        {
            try
            {
                //Sometimes don't run the update, if not ready or we're ending.
                if (Algorithm == null || Algorithm.Transactions == null || _processingFinalPacket)
                {
                    return;
                }

                if (DateTime.Now <= _nextUpdate || !(_daysProcessed > (_lastDaysProcessed + 1))) return;

                //Extract the orders since last update
                var deltaOrders = new Dictionary<int, Order>();

                try
                {
                    deltaOrders = (from order in _transactionHandler.Orders
                        where order.Value.Time.Date >= _lastUpdate && order.Value.Status == OrderStatus.Filled
                        select order).ToDictionary(t => t.Key, t => t.Value);
                }
                catch (Exception err)
                {
                    Log.Error(err, "Transactions");
                }

                //Limit length of orders we pass back dynamically to avoid flooding.
                if (deltaOrders.Count > 50) deltaOrders.Clear();

                //Reset loop variables:
                try
                {
                    _lastUpdate = Algorithm.Time.Date;
                    _lastDaysProcessed = _daysProcessed;
                    _nextUpdate = DateTime.Now.AddSeconds(0.5);
                }
                catch (Exception err)
                {
                    Log.Error(err, "Can't update variables");
                }

                var deltaCharts = new Dictionary<string, Chart>();
                lock (_chartLock)
                {
                    //Get the updates since the last chart
                    foreach (var kvp in Charts)
                    {
                        var chart = kvp.Value;

                        deltaCharts.Add(chart.Name, chart.GetUpdates());
                    }
                }

                //Get the runtime statistics from the user algorithm:
                var runtimeStatistics = new Dictionary<string, string>();
                lock (_runtimeLock)
                {
                    foreach (var pair in _runtimeStatistics)
                    {
                        runtimeStatistics.Add(pair.Key, pair.Value);
                    }
                }
                runtimeStatistics.Add("Unrealized", "$" + _algorithm.Portfolio.TotalUnrealizedProfit.ToString("N2"));
                runtimeStatistics.Add("Fees", "-$" + _algorithm.Portfolio.TotalFees.ToString("N2"));
                runtimeStatistics.Add("Net Profit", "$" + (_algorithm.Portfolio.TotalProfit - _algorithm.Portfolio.TotalFees).ToString("N2"));
                runtimeStatistics.Add("Return", ((_algorithm.Portfolio.TotalPortfolioValue - _setupHandler.StartingPortfolioValue) / _setupHandler.StartingPortfolioValue).ToString("P"));
                runtimeStatistics.Add("Equity", "$" + _algorithm.Portfolio.TotalPortfolioValue.ToString("N2"));

                //Profit Loss Changes:
                var progress = Convert.ToDecimal(_daysProcessed / _jobDays);
                if (progress > 0.999m) progress = 0.999m;

                //1. Cloud Upload -> Upload the whole packet to S3  Immediately:
                var completeResult = new BacktestResult(_algorithm.IsFrameworkAlgorithm, Charts, _transactionHandler.Orders, Algorithm.Transactions.TransactionRecord, new Dictionary<string, string>(), runtimeStatistics, new Dictionary<string, AlgorithmPerformance>());
                var complete = new BacktestResultPacket(_job, completeResult, progress);

                if (DateTime.Now > _nextS3Update)
                {
                    _nextS3Update = DateTime.Now.AddSeconds(30);
                    StoreResult(complete, false);
                }

                //2. Backtest Update -> Send the truncated packet to the backtester:
                var splitPackets = SplitPackets(deltaCharts, deltaOrders, runtimeStatistics, progress);

                foreach (var backtestingPacket in splitPackets)
                {
                    _messagingHandler.Send(backtestingPacket);
                }
            }
            catch (Exception err)
            {
                Log.Error(err);
            }
        }

        /// <summary>
        /// Run over all the data and break it into smaller packets to ensure they all arrive at the terminal
        /// </summary>
        public IEnumerable<BacktestResultPacket> SplitPackets(Dictionary<string, Chart> deltaCharts, Dictionary<int, Order> deltaOrders, Dictionary<string,string> runtimeStatistics, decimal progress)
        {
            // break the charts into groups
            var splitPackets = new List<BacktestResultPacket>();
            foreach (var chart in deltaCharts.Values)
            {
                //Don't add packet if the series is empty:
                if (chart.Series.Values.Sum(x => x.Values.Count) == 0) continue;

                splitPackets.Add(new BacktestResultPacket(_job, new BacktestResult
                {
                    IsFrameworkAlgorithm = _algorithm.IsFrameworkAlgorithm,
                    Charts = new Dictionary<string, Chart>()
                    {
                        {chart.Name, chart}
                    }
                }, progress));
            }

            // Send alpha run time statistics
            splitPackets.Add(new BacktestResultPacket(_job, new BacktestResult {IsFrameworkAlgorithm = _algorithm.IsFrameworkAlgorithm, AlphaRuntimeStatistics = AlphaRuntimeStatistics}));

            // Add the orders into the charting packet:
            splitPackets.Add(new BacktestResultPacket(_job, new BacktestResult { IsFrameworkAlgorithm = _algorithm.IsFrameworkAlgorithm, Orders = deltaOrders }, progress));

            //Add any user runtime statistics into the backtest.
            splitPackets.Add(new BacktestResultPacket(_job, new BacktestResult { IsFrameworkAlgorithm = _algorithm.IsFrameworkAlgorithm, RuntimeStatistics = runtimeStatistics }, progress));

            return splitPackets;
        }

        /// <summary>
        /// Save the snapshot of the total results to storage.
        /// </summary>
        /// <param name="packet">Packet to store.</param>
        /// <param name="async">Store the packet asyncronously to speed up the thread.</param>
        /// <remarks>Async creates crashes in Mono 3.10 if the thread disappears before the upload is complete so it is disabled for now.</remarks>
        public void StoreResult(Packet packet, bool async = false)
        {
            try
            {
                lock (_chartLock)
                {
                    //1. Make sure this is the right type of packet:
                    if (packet.Type != PacketType.BacktestResult) return;

                    //2. Port to packet format:
                    var result = packet as BacktestResultPacket;

                    if (result != null)
                    {
                        //3. Set Alpha Runtime Statistics
                        result.Results.AlphaRuntimeStatistics = AlphaRuntimeStatistics;

                        //4. Get Storage Location:
                        var key = _job.BacktestId + ".json";

                        //5. Save results
                        SaveResults(key, result.Results);
                    }
                    else
                    {
                        Log.Error("BacktestingResultHandler.StoreResult(): Result Null.");
                    }
                }
            }
            catch (Exception err)
            {
                Log.Error(err);
            }
        }

        /// <summary>
        /// Send a final analysis result back to the IDE.
        /// </summary>
        /// <param name="job">Lean AlgorithmJob task</param>
        /// <param name="orders">Collection of orders from the algorithm</param>
        /// <param name="profitLoss">Collection of time-profit values for the algorithm</param>
        /// <param name="holdings">Current holdings state for the algorithm</param>
        /// <param name="cashbook">Cashbook for the holdingss</param>
        /// <param name="statisticsResults">Statistics information for the algorithm (empty if not finished)</param>
        /// <param name="banner">Runtime statistics banner information</param>
        public void SendFinalResult(AlgorithmNodePacket job, Dictionary<int, Order> orders, Dictionary<DateTime, decimal> profitLoss, Dictionary<string, Holding> holdings, CashBook cashbook, StatisticsResults statisticsResults, Dictionary<string, string> banner)
        {
            try
            {
                FinalStatistics = statisticsResults.Summary;

                //Convert local dictionary:
                var charts = new Dictionary<string, Chart>(Charts);
                _processingFinalPacket = true;

                // clear the trades collection before placing inside the backtest result
                foreach (var ap in statisticsResults.RollingPerformances.Values)
                {
                    ap.ClosedTrades.Clear();
                }

                //Create a result packet to send to the browser.
                var result = new BacktestResultPacket((BacktestNodePacket) job,
                    new BacktestResult(_algorithm.IsFrameworkAlgorithm, charts, orders, profitLoss, statisticsResults.Summary, banner, statisticsResults.RollingPerformances, statisticsResults.TotalPerformance), 1m)
                {
                    ProcessingTime = (DateTime.Now - _startTime).TotalSeconds,
                    DateFinished = DateTime.Now,
                    Progress = 1
                };

                //Place result into storage.
                StoreResult(result);

                //Second, send the truncated packet:
                _messagingHandler.Send(result);

                Log.Trace("BacktestingResultHandler.SendAnalysisResult(): Processed final packet");
            }
            catch (Exception err)
            {
                Log.Error(err);
            }
        }

        /// <summary>
        /// Set the Algorithm instance for ths result.
        /// </summary>
        /// <param name="algorithm">Algorithm we're working on.</param>
        /// <remarks>While setting the algorithm the backtest result handler.</remarks>
        public void SetAlgorithm(IAlgorithm algorithm)
        {
            _algorithm = algorithm;

            //Get the resample period:
            var totalMinutes = (_job.PeriodFinish - _job.PeriodStart).TotalMinutes;
            var resampleMinutes = (totalMinutes < (_minimumSamplePeriod * _samples)) ? _minimumSamplePeriod : (totalMinutes / _samples); // Space out the sampling every
            _resamplePeriod = TimeSpan.FromMinutes(resampleMinutes);
            Log.Trace("BacktestingResultHandler(): Sample Period Set: " + resampleMinutes.ToString("00.00"));

            //Setup the sampling periods:
            _jobDays = Algorithm.Securities.Count > 0
                ? Time.TradeableDates(Algorithm.Securities.Values, _job.PeriodStart, _job.PeriodFinish)
                : Convert.ToInt32((_job.PeriodFinish.Date - _job.PeriodStart.Date).TotalDays) + 1;

            //Setup Debug Messaging:
            _debugMessageMax = Convert.ToInt32(10 * _jobDays);
            //Minimum 100 messages per backtest:
            if (_debugMessageMax < _debugMessageMin) _debugMessageMax = _debugMessageMin;
            //Messaging for the log messages:
            _debugMessagePeriod = "backtest";

            //Set the security / market types.
            var types = new List<SecurityType>();
            foreach (var kvp in _algorithm.Securities)
            {
                var security = kvp.Value;

                if (!types.Contains(security.Type)) types.Add(security.Type);
            }
            SecurityType(types);

            if (Config.GetBool("forward-console-messages", true))
            {
                // we need to forward Console.Write messages to the algorithm's Debug function
                var debug = new FuncTextWriter(algorithm.Debug);
                var error = new FuncTextWriter(algorithm.Error);
                Console.SetOut(debug);
                Console.SetError(error);
            }
        }

        /// <summary>
        /// Send a debug message back to the browser console.
        /// </summary>
        /// <param name="message">Message we'd like shown in console.</param>
        public void DebugMessage(string message)
        {
            Messages.Enqueue(new DebugPacket(_projectId, _backtestId, _compileId, message));

            //Save last message sent:
            if (_algorithm != null)
            {
                _log.Add(_algorithm.Time.ToString(DateFormat.UI) + " " + message);
            }
            _debugMessage = message;
        }

        /// <summary>
        /// Send a system debug message back to the browser console.
        /// </summary>
        /// <param name="message">Message we'd like shown in console.</param>
        public void SystemDebugMessage(string message)
        {
            Messages.Enqueue(new SystemDebugPacket(_projectId, _backtestId, _compileId, message));

            //Save last message sent:
            if (_algorithm != null)
            {
                _log.Add(_algorithm.Time.ToString(DateFormat.UI) + " " + message);
            }
            _debugMessage = message;
        }

        /// <summary>
        /// Send a logging message to the log list for storage.
        /// </summary>
        /// <param name="message">Message we'd in the log.</param>
        public void LogMessage(string message)
        {
            Messages.Enqueue(new LogPacket(_backtestId, message));

            if (_algorithm != null)
            {
                _log.Add(_algorithm.Time.ToString(DateFormat.UI) + " " + message);
            }
        }

        /// <summary>
        /// Send list of security asset types the algortihm uses to browser.
        /// </summary>
        public void SecurityType(List<SecurityType> types)
        {
            var packet = new SecurityTypesPacket
            {
                Types = types
            };
            Messages.Enqueue(packet);
        }

        /// <summary>
        /// Send an error message back to the browser highlighted in red with a stacktrace.
        /// </summary>
        /// <param name="message">Error message we'd like shown in console.</param>
        /// <param name="stacktrace">Stacktrace information string</param>
        public void ErrorMessage(string message, string stacktrace = "")
        {
            if (message == _errorMessage) return;
            if (Messages.Count > 500) return;
            Messages.Enqueue(new HandledErrorPacket(_backtestId, message, stacktrace));
            _errorMessage = message;
        }

        /// <summary>
        /// Send a runtime error message back to the browser highlighted with in red
        /// </summary>
        /// <param name="message">Error message.</param>
        /// <param name="stacktrace">Stacktrace information string</param>
        public void RuntimeError(string message, string stacktrace = "")
        {
            PurgeQueue();
            Messages.Enqueue(new RuntimeErrorPacket(_job.UserId, _backtestId, message, stacktrace));
            _errorMessage = message;
        }

        /// <summary>
        /// Add a sample to the chart specified by the chartName, and seriesName.
        /// </summary>
        /// <param name="chartName">String chart name to place the sample.</param>
        /// <param name="seriesIndex">Type of chart we should create if it doesn't already exist.</param>
        /// <param name="seriesName">Series name for the chart.</param>
        /// <param name="seriesType">Series type for the chart.</param>
        /// <param name="time">Time for the sample</param>
        /// <param name="unit">Unit of the sample</param>
        /// <param name="value">Value for the chart sample.</param>
        public void Sample(string chartName, string seriesName, int seriesIndex, SeriesType seriesType, DateTime time, decimal value, string unit = "$")
        {
            lock (_chartLock)
            {
                //Add a copy locally:
                Chart chart;
                if (!Charts.TryGetValue(chartName, out chart))
                {
                    chart = new Chart(chartName);
                    Charts.AddOrUpdate(chartName, chart);
                }

                //Add the sample to our chart:
                Series series;
                if (!chart.Series.TryGetValue(seriesName, out series))
                {
                    series = new Series(seriesName, seriesType, seriesIndex, unit);
                    chart.Series.Add(seriesName, series);
                }

                //Add our value:
                if (series.Values.Count == 0 || time > Time.UnixTimeStampToDateTime(series.Values[series.Values.Count - 1].x))
                {
                    series.Values.Add(new ChartPoint(time, value));
                }
            }
        }

        /// <summary>
        /// Sample the current equity of the strategy directly with time-value pair.
        /// </summary>
        /// <param name="time">Current backtest time.</param>
        /// <param name="value">Current equity value.</param>
        public void SampleEquity(DateTime time, decimal value)
        {
            //Sample the Equity Value:
            Sample("Strategy Equity", "Equity", 0, SeriesType.Candle, time, value, "$");

            //Recalculate the days processed:
            _daysProcessed = (time - _algorithm.StartDate).TotalDays;
        }

        /// <summary>
        /// Sample the current daily performance directly with a time-value pair.
        /// </summary>
        /// <param name="time">Current backtest date.</param>
        /// <param name="value">Current daily performance value.</param>
        public void SamplePerformance(DateTime time, decimal value)
        {
            //Added a second chart to equity plot - daily perforamnce:
            Sample("Strategy Equity", "Daily Performance", 1, SeriesType.Bar, time, value, "%");
        }

        /// <summary>
        /// Sample the current benchmark performance directly with a time-value pair.
        /// </summary>
        /// <param name="time">Current backtest date.</param>
        /// <param name="value">Current benchmark value.</param>
        /// <seealso cref="IResultHandler.Sample"/>
        public void SampleBenchmark(DateTime time, decimal value)
        {
            Sample("Benchmark", "Benchmark", 0, SeriesType.Line, time, value, "$");
        }

        /// <summary>
        /// Add a range of samples from the users algorithms to the end of our current list.
        /// </summary>
        /// <param name="updates">Chart updates since the last request.</param>
        public void SampleRange(List<Chart> updates)
        {
            lock (_chartLock)
            {
                foreach (var update in updates)
                {
                    //Create the chart if it doesn't exist already:
                    if (!Charts.ContainsKey(update.Name))
                    {
                        Charts.AddOrUpdate(update.Name, new Chart(update.Name));
                    }

                    //Add these samples to this chart.
                    foreach (var series in update.Series.Values)
                    {
                        //If we don't already have this record, its the first packet
                        var chart = Charts[update.Name];
                        if (!chart.Series.ContainsKey(series.Name))
                        {
                            chart.Series.Add(series.Name, new Series(series.Name, series.SeriesType, series.Index, series.Unit)
                            {
                                Color = series.Color, ScatterMarkerSymbol = series.ScatterMarkerSymbol
                            });
                        }

                        var thisSeries = chart.Series[series.Name];
                        if (series.Values.Count > 0)
                        {
                            // only keep last point for pie charts
                            if (series.SeriesType == SeriesType.Pie)
                            {
                                var lastValue = series.Values.Last();
                                thisSeries.Purge();
                                thisSeries.Values.Add(lastValue);
                            }
                            else
                            {
                                //We already have this record, so just the new samples to the end:
                                chart.Series[series.Name].Values.AddRange(series.Values);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Terminate the result thread and apply any required exit procedures.
        /// </summary>
        public void Exit()
        {
            // Only process the logs once
            if (!_exitTriggered)
            {
                ProcessSynchronousEvents(true);
                var logLocation = SaveLogs(_algorithmId, _log);
                SystemDebugMessage("Your log was successfully created and can be retrieved from: " + logLocation);
            }

            //Set exit flag, and wait for the messages to send:
            _exitTriggered = true;
        }

        /// <summary>
        /// Send a new order event to the browser.
        /// </summary>
        /// <remarks>In backtesting the order events are not sent because it would generate a high load of messaging.</remarks>
        /// <param name="newEvent">New order event details</param>
        public void OrderEvent(OrderEvent newEvent)
        {
            // NOP. Don't do any order event processing for results in backtest mode.
        }


        /// <summary>
        /// Send an algorithm status update to the browser.
        /// </summary>
        /// <param name="status">Status enum value.</param>
        /// <param name="message">Additional optional status message.</param>
        public virtual void SendStatusUpdate(AlgorithmStatus status, string message = "")
        {
            var statusPacket = new AlgorithmStatusPacket(_algorithmId, _projectId, status, message);
            _messagingHandler.Send(statusPacket);
        }

        /// <summary>
        /// Sample the asset prices to generate plots.
        /// </summary>
        /// <param name="symbol">Symbol we're sampling.</param>
        /// <param name="time">Time of sample</param>
        /// <param name="value">Value of the asset price</param>
        public void SampleAssetPrices(Symbol symbol, DateTime time, decimal value)
        {
            //NOP. Don't sample asset prices in console.
        }

        /// <summary>
        /// Purge/clear any outstanding messages in message queue.
        /// </summary>
        public void PurgeQueue()
        {
            Messages.Clear();
        }

        /// <summary>
        /// Set the current runtime statistics of the algorithm.
        /// These are banner/title statistics which show at the top of the live trading results.
        /// </summary>
        /// <param name="key">Runtime headline statistic name</param>
        /// <param name="value">Runtime headline statistic value</param>
        public void RuntimeStatistic(string key, string value)
        {
            lock (_runtimeLock)
            {
                _runtimeStatistics[key] = value;
            }
        }

        /// <summary>
        /// Set the chart subscription we want data for. Not used in backtesting.
        /// </summary>
        public void SetChartSubscription(string symbol)
        {
            //NOP.
        }

        /// <summary>
        /// Process the synchronous result events, sampling and message reading.
        /// This method is triggered from the algorithm manager thread.
        /// </summary>
        /// <remarks>Prime candidate for putting into a base class. Is identical across all result handlers.</remarks>
        public void ProcessSynchronousEvents(bool forceProcess = false)
        {
            if (_algorithm == null) return;

            var time = _algorithm.UtcTime;

            if (time > _nextSample || forceProcess)
            {
                //Set next sample time: 4000 samples per backtest
                _nextSample = time.Add(ResamplePeriod);

                //Sample the portfolio value over time for chart.
                SampleEquity(time, Math.Round(_algorithm.Portfolio.TotalPortfolioValue, 4));

                //Also add the user samples / plots to the result handler tracking:
                SampleRange(_algorithm.GetChartUpdates());

                //Sample the asset pricing:
                foreach (var kvp in _algorithm.Securities)
                {
                    var security = kvp.Value;

                    SampleAssetPrices(security.Symbol, time, security.Price);
                }
            }

            //Send out the debug messages:
            var debugStopWatch = Stopwatch.StartNew();
            while (_algorithm.DebugMessages.Count > 0 && debugStopWatch.ElapsedMilliseconds < 250)
            {
                string message;
                if (_algorithm.DebugMessages.TryDequeue(out message))
                {
                    DebugMessage(message);
                }
            }

            //Send out the error messages:
            var errorStopWatch = Stopwatch.StartNew();
            while (_algorithm.ErrorMessages.Count > 0 && errorStopWatch.ElapsedMilliseconds < 250)
            {
                string message;
                if (_algorithm.ErrorMessages.TryDequeue(out message))
                {
                    ErrorMessage(message);
                }
            }

            //Send out the log messages:
            var logStopWatch = Stopwatch.StartNew();
            while (_algorithm.LogMessages.Count > 0 && logStopWatch.ElapsedMilliseconds < 250)
            {
                string message;
                if (_algorithm.LogMessages.TryDequeue(out message))
                {
                    LogMessage(message);
                }
            }

            //Set the running statistics:
            foreach (var pair in _algorithm.RuntimeStatistics)
            {
                RuntimeStatistic(pair.Key, pair.Value);
            }
        }
    }
}
