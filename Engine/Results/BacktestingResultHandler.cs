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
using System.IO;
using System.Linq;
using QuantConnect.Brokerages;
using QuantConnect.Configuration;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Packets;
using QuantConnect.Securities.Positions;
using QuantConnect.Statistics;
using QuantConnect.Util;

namespace QuantConnect.Lean.Engine.Results
{
    /// <summary>
    /// Backtesting result handler passes messages back from the Lean to the User.
    /// </summary>
    public class BacktestingResultHandler : BaseResultsHandler, IResultHandler
    {
        private const double Samples = 4000;
        private const double MinimumSamplePeriod = 4;

        private BacktestNodePacket _job;
        private DateTime _nextUpdate;
        private DateTime _nextS3Update;
        private string _errorMessage;
        private int _daysProcessedFrontier;
        private readonly HashSet<string> _chartSeriesExceededDataPoints;
        private readonly HashSet<string> _chartSeriesCount;
        private bool _chartSeriesCountExceededError;

        private BacktestProgressMonitor _progressMonitor;

        /// <summary>
        /// Calculates the capacity of a strategy per Symbol in real-time
        /// </summary>
        private CapacityEstimate _capacityEstimate;

        //Processing Time:
        private DateTime _nextSample;
        private string _algorithmId;
        private int _projectId;

        /// <summary>
        /// A dictionary containing summary statistics
        /// </summary>
        public Dictionary<string, string> FinalStatistics { get; private set; }

        /// <summary>
        /// Creates a new instance
        /// </summary>
        public BacktestingResultHandler()
        {
            ResamplePeriod = TimeSpan.FromMinutes(4);
            NotificationPeriod = TimeSpan.FromSeconds(2);

            _chartSeriesExceededDataPoints = new();
            _chartSeriesCount = new();

            // Delay uploading first packet
            _nextS3Update = StartTime.AddSeconds(5);
        }

        /// <summary>
        /// Initialize the result handler with this result packet.
        /// </summary>
        public override void Initialize(ResultHandlerInitializeParameters parameters)
        {
            _job = (BacktestNodePacket)parameters.Job;
            State["Name"] = _job.Name;
            _algorithmId = _job.AlgorithmId;
            _projectId = _job.ProjectId;
            if (_job == null) throw new Exception("BacktestingResultHandler.Constructor(): Submitted Job type invalid.");
            base.Initialize(parameters);
            if (!string.IsNullOrEmpty(_job.OptimizationId))
            {
                State["OptimizationId"] = _job.OptimizationId;
            }
        }

        /// <summary>
        /// The main processing method steps through the messaging queue and processes the messages one by one.
        /// </summary>
        protected override void Run()
        {
            try
            {
                while (!(ExitTriggered && Messages.IsEmpty))
                {
                    //While there's no work to do, go back to the algorithm:
                    if (Messages.IsEmpty)
                    {
                        ExitEvent.WaitOne(50);
                    }
                    else
                    {
                        //1. Process Simple Messages in Queue
                        Packet packet;
                        if (Messages.TryDequeue(out packet))
                        {
                            MessagingHandler.Send(packet);
                        }
                    }

                    //2. Update the packet scanner:
                    Update();

                } // While !End.
            }
            catch (Exception err)
            {
                // unexpected error, we need to close down shop
                Algorithm.SetRuntimeError(err, "ResultHandler");
            }

            Log.Trace("BacktestingResultHandler.Run(): Ending Thread...");
        } // End Run();

        /// <summary>
        /// Send a backtest update to the browser taking a latest snapshot of the charting data.
        /// </summary>
        private void Update()
        {
            try
            {
                //Sometimes don't run the update, if not ready or we're ending.
                if (Algorithm?.Transactions == null || ExitTriggered || !Algorithm.GetLocked())
                {
                    return;
                }

                var utcNow = DateTime.UtcNow;
                if (utcNow <= _nextUpdate || _progressMonitor.ProcessedDays < _daysProcessedFrontier) return;

                var deltaOrders = GetDeltaOrders(LastDeltaOrderPosition, shouldStop: orderCount => orderCount >= 50);
                // Deliberately skip to the end of order event collection to prevent overloading backtesting UX
                LastDeltaOrderPosition = TransactionHandler.OrderEvents.Count();

                //Reset loop variables:
                try
                {
                    _daysProcessedFrontier = _progressMonitor.ProcessedDays + 1;
                    _nextUpdate = utcNow.AddSeconds(3);
                }
                catch (Exception err)
                {
                    Log.Error(err, "Can't update variables");
                }

                var deltaCharts = new Dictionary<string, Chart>();
                var serverStatistics = GetServerStatistics(utcNow);
                var performanceCharts = new Dictionary<string, Chart>();

                // Process our charts updates
                lock (ChartLock)
                {
                    foreach (var kvp in Charts)
                    {
                        var chart = kvp.Value;

                        // Get a copy of this chart with updates only since last request
                        var updates = chart.GetUpdates();
                        if (!updates.IsEmpty())
                        {
                            deltaCharts.Add(chart.Name, updates);
                        }

                        // Update our algorithm performance charts
                        if (AlgorithmPerformanceCharts.Contains(kvp.Key))
                        {
                            performanceCharts[kvp.Key] = chart.Clone();
                        }

                        if (updates.Name == PortfolioMarginKey)
                        {
                            PortfolioMarginChart.RemoveSinglePointSeries(updates);
                        }
                    }
                }

                //Get the runtime statistics from the user algorithm:
                var summary = GenerateStatisticsResults(performanceCharts, estimatedStrategyCapacity: _capacityEstimate).Summary;
                var runtimeStatistics = GetAlgorithmRuntimeStatistics(summary, _capacityEstimate);

                var progress = _progressMonitor.Progress;

                //1. Cloud Upload -> Upload the whole packet to S3  Immediately:
                if (utcNow > _nextS3Update)
                {
                    // For intermediate backtesting results, we truncate the order list to include only the last 100 orders
                    // The final packet will contain the full list of orders.
                    const int maxOrders = 100;
                    var orderCount = TransactionHandler.Orders.Count;

                    var completeResult = new BacktestResult(new BacktestResultParameters(
                        Charts,
                        orderCount > maxOrders ? TransactionHandler.Orders.Skip(orderCount - maxOrders).ToDictionary() : TransactionHandler.Orders.ToDictionary(),
                        Algorithm.Transactions.TransactionRecord,
                        new Dictionary<string, string>(),
                        runtimeStatistics,
                        new Dictionary<string, AlgorithmPerformance>(),
                        // we store the last 100 order events, the final packet will contain the full list
                        TransactionHandler.OrderEvents.Reverse().Take(100).ToList(), state: GetAlgorithmState()));

                    StoreResult(new BacktestResultPacket(_job, completeResult, Algorithm.EndDate, Algorithm.StartDate, progress));

                    _nextS3Update = DateTime.UtcNow.AddSeconds(30);
                }

                //2. Backtest Update -> Send the truncated packet to the backtester:
                var splitPackets = SplitPackets(deltaCharts, deltaOrders, runtimeStatistics, progress, serverStatistics);

                foreach (var backtestingPacket in splitPackets)
                {
                    MessagingHandler.Send(backtestingPacket);
                }

                // let's re update this value after we finish just in case, so we don't re enter in the next loop
                _nextUpdate = DateTime.UtcNow.Add(MainUpdateInterval);
            }
            catch (Exception err)
            {
                Log.Error(err);
            }
        }

        /// <summary>
        /// Run over all the data and break it into smaller packets to ensure they all arrive at the terminal
        /// </summary>
        public virtual IEnumerable<BacktestResultPacket> SplitPackets(Dictionary<string, Chart> deltaCharts, Dictionary<int, Order> deltaOrders, SortedDictionary<string, string> runtimeStatistics, decimal progress, Dictionary<string, string> serverStatistics)
        {
            // break the charts into groups
            var splitPackets = new List<BacktestResultPacket>();
            foreach (var chart in deltaCharts.Values)
            {
                splitPackets.Add(new BacktestResultPacket(_job, new BacktestResult
                {
                    Charts = new Dictionary<string, Chart>
                    {
                        {chart.Name, chart}
                    }
                }, Algorithm.EndDate, Algorithm.StartDate, progress));
            }

            // only send orders if there is actually any update
            if (deltaOrders.Count > 0)
            {
                // Add the orders into the charting packet:
                splitPackets.Add(new BacktestResultPacket(_job, new BacktestResult { Orders = deltaOrders }, Algorithm.EndDate, Algorithm.StartDate, progress));
            }

            //Add any user runtime statistics into the backtest.
            splitPackets.Add(new BacktestResultPacket(_job, new BacktestResult { ServerStatistics = serverStatistics, RuntimeStatistics = runtimeStatistics }, Algorithm.EndDate, Algorithm.StartDate, progress));


            return splitPackets;
        }

        /// <summary>
        /// Save the snapshot of the total results to storage.
        /// </summary>
        /// <param name="packet">Packet to store.</param>
        protected override void StoreResult(Packet packet)
        {
            try
            {
                // Make sure this is the right type of packet:
                if (packet.Type != PacketType.BacktestResult) return;

                // Port to packet format:
                var result = packet as BacktestResultPacket;

                if (result != null)
                {
                    // Get Storage Location:
                    var key = $"{AlgorithmId}.json";

                    BacktestResult results;
                    lock (ChartLock)
                    {
                        results = new BacktestResult(new BacktestResultParameters(
                            result.Results.Charts.ToDictionary(x => x.Key, x => x.Value.Clone()),
                            result.Results.Orders,
                            result.Results.ProfitLoss,
                            result.Results.Statistics,
                            result.Results.RuntimeStatistics,
                            result.Results.RollingWindow,
                            null, // null order events, we store them separately
                            result.Results.TotalPerformance,
                            result.Results.AlgorithmConfiguration,
                            result.Results.State));

                        if (result.Results.Charts.TryGetValue(PortfolioMarginKey, out var marginChart))
                        {
                            PortfolioMarginChart.RemoveSinglePointSeries(marginChart);
                        }
                    }
                    // Save results
                    SaveResults(key, results);

                    // Store Order Events in a separate file
                    StoreOrderEvents(Algorithm?.UtcTime ?? DateTime.UtcNow, result.Results.OrderEvents);
                }
                else
                {
                    Log.Error("BacktestingResultHandler.StoreResult(): Result Null.");
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
        protected void SendFinalResult()
        {
            try
            {
                var endTime = DateTime.UtcNow;
                BacktestResultPacket result;
                // could happen if algorithm failed to init
                if (Algorithm != null)
                {
                    //Convert local dictionary:
                    var charts = new Dictionary<string, Chart>(Charts);
                    var orders = new Dictionary<int, Order>(TransactionHandler.Orders);
                    var profitLoss = new SortedDictionary<DateTime, decimal>(Algorithm.Transactions.TransactionRecord);
                    var statisticsResults = GenerateStatisticsResults(charts, profitLoss, _capacityEstimate);
                    var runtime = GetAlgorithmRuntimeStatistics(statisticsResults.Summary, capacityEstimate: _capacityEstimate);

                    FinalStatistics = statisticsResults.Summary;

                    // clear the trades collection before placing inside the backtest result
                    foreach (var ap in statisticsResults.RollingPerformances.Values)
                    {
                        ap.ClosedTrades.Clear();
                    }
                    var orderEvents = TransactionHandler.OrderEvents.ToList();
                    //Create a result packet to send to the browser.
                    result = new BacktestResultPacket(_job,
                        new BacktestResult(new BacktestResultParameters(charts, orders, profitLoss, statisticsResults.Summary, runtime,
                            statisticsResults.RollingPerformances, orderEvents, statisticsResults.TotalPerformance,
                            AlgorithmConfiguration.Create(Algorithm, _job), GetAlgorithmState(endTime))),
                        Algorithm.EndDate, Algorithm.StartDate);
                }
                else
                {
                    result = BacktestResultPacket.CreateEmpty(_job);
                    result.Results.State = GetAlgorithmState(endTime);
                }

                result.ProcessingTime = (endTime - StartTime).TotalSeconds;
                result.DateFinished = DateTime.Now;
                result.Progress = 1;

                StoreInsights();

                //Place result into storage.
                StoreResult(result);

                result.Results.ServerStatistics = GetServerStatistics(endTime);
                //Second, send the truncated packet:
                MessagingHandler.Send(result);

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
        /// <param name="startingPortfolioValue">Algorithm starting capital for statistics calculations</param>
        /// <remarks>While setting the algorithm the backtest result handler.</remarks>
        public virtual void SetAlgorithm(IAlgorithm algorithm, decimal startingPortfolioValue)
        {
            Algorithm = algorithm;
            Algorithm.SetStatisticsService(this);
            State["Name"] = Algorithm.Name;
            StartingPortfolioValue = startingPortfolioValue;
            DailyPortfolioValue = StartingPortfolioValue;
            CumulativeMaxPortfolioValue = StartingPortfolioValue;
            AlgorithmCurrencySymbol = Currencies.GetCurrencySymbol(Algorithm.AccountCurrency);
            _capacityEstimate = new CapacityEstimate(Algorithm);
            _progressMonitor = new BacktestProgressMonitor(Algorithm.TimeKeeper, Algorithm.EndDate);

            //Get the resample period:
            var totalMinutes = (algorithm.EndDate - algorithm.StartDate).TotalMinutes;
            var resampleMinutes = totalMinutes < MinimumSamplePeriod * Samples ? MinimumSamplePeriod : totalMinutes / Samples; // Space out the sampling every
            ResamplePeriod = TimeSpan.FromMinutes(resampleMinutes);
            Log.Trace("BacktestingResultHandler(): Sample Period Set: " + resampleMinutes.ToStringInvariant("00.00"));

            //Set the security / market types.
            var types = new List<SecurityType>();
            foreach (var kvp in Algorithm.Securities)
            {
                var security = kvp.Value;

                if (!types.Contains(security.Type)) types.Add(security.Type);
            }
            SecurityType(types);

            ConfigureConsoleTextWriter(algorithm);

            // Wire algorithm name and tags updates
            algorithm.NameUpdated += (sender, name) => AlgorithmNameUpdated(name);
            algorithm.TagsUpdated += (sender, tags) => AlgorithmTagsUpdated(tags);
        }

        /// <summary>
        /// Handles updates to the algorithm's name
        /// </summary>
        /// <param name="name">The new name</param>
        public virtual void AlgorithmNameUpdated(string name)
        {
            Messages.Enqueue(new AlgorithmNameUpdatePacket(AlgorithmId, name));
        }

        /// <summary>
        /// Sends a packet communicating an update to the algorithm's tags
        /// </summary>
        /// <param name="tags">The new tags</param>
        public virtual void AlgorithmTagsUpdated(HashSet<string> tags)
        {
            Messages.Enqueue(new AlgorithmTagsUpdatePacket(AlgorithmId, tags));
        }

        /// <summary>
        /// Send a debug message back to the browser console.
        /// </summary>
        /// <param name="message">Message we'd like shown in console.</param>
        public virtual void DebugMessage(string message)
        {
            Messages.Enqueue(new DebugPacket(_projectId, AlgorithmId, CompileId, message));
            AddToLogStore(message);
        }

        /// <summary>
        /// Send a system debug message back to the browser console.
        /// </summary>
        /// <param name="message">Message we'd like shown in console.</param>
        public virtual void SystemDebugMessage(string message)
        {
            Messages.Enqueue(new SystemDebugPacket(_projectId, AlgorithmId, CompileId, message));
            AddToLogStore(message);
        }

        /// <summary>
        /// Send a logging message to the log list for storage.
        /// </summary>
        /// <param name="message">Message we'd in the log.</param>
        public virtual void LogMessage(string message)
        {
            Messages.Enqueue(new LogPacket(AlgorithmId, message));
            AddToLogStore(message);
        }

        /// <summary>
        /// Add message to LogStore
        /// </summary>
        /// <param name="message">Message to add</param>
        protected override void AddToLogStore(string message)
        {
            var messageToLog = Algorithm != null
                ? Algorithm.Time.ToStringInvariant(DateFormat.UI) + " " + message
                : "Algorithm Initialization: " + message;

            base.AddToLogStore(messageToLog);
        }

        /// <summary>
        /// Send list of security asset types the algorithm uses to browser.
        /// </summary>
        public virtual void SecurityType(List<SecurityType> types)
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
        public virtual void ErrorMessage(string message, string stacktrace = "")
        {
            if (message == _errorMessage) return;
            if (Messages.Count > 500) return;
            Messages.Enqueue(new HandledErrorPacket(AlgorithmId, message, stacktrace));
            _errorMessage = message;
        }

        /// <summary>
        /// Send a runtime error message back to the browser highlighted with in red
        /// </summary>
        /// <param name="message">Error message.</param>
        /// <param name="stacktrace">Stacktrace information string</param>
        public virtual void RuntimeError(string message, string stacktrace = "")
        {
            PurgeQueue();
            Messages.Enqueue(new RuntimeErrorPacket(_job.UserId, AlgorithmId, message, stacktrace));
            _errorMessage = message;
            SetAlgorithmState(message, stacktrace);
        }

        /// <summary>
        /// Process brokerage message events
        /// </summary>
        /// <param name="brokerageMessageEvent">The brokerage message event</param>
        public virtual void BrokerageMessage(BrokerageMessageEvent brokerageMessageEvent)
        {
            // NOP
        }

        /// <summary>
        /// Add a sample to the chart specified by the chartName, and seriesName.
        /// </summary>
        /// <param name="chartName">String chart name to place the sample.</param>
        /// <param name="seriesIndex">Type of chart we should create if it doesn't already exist.</param>
        /// <param name="seriesName">Series name for the chart.</param>
        /// <param name="seriesType">Series type for the chart.</param>
        /// <param name="value">Value for the chart sample.</param>
        /// <param name="unit">Unit of the sample</param>
        protected override void Sample(string chartName, string seriesName, int seriesIndex, SeriesType seriesType, ISeriesPoint value,
            string unit = "$")
        {
            // Sampling during warming up period skews statistics
            if (Algorithm.IsWarmingUp)
            {
                return;
            }

            lock (ChartLock)
            {
                //Add a copy locally:
                Chart chart;
                if (!Charts.TryGetValue(chartName, out chart))
                {
                    chart = new Chart(chartName);
                    Charts.AddOrUpdate(chartName, chart);
                }

                //Add the sample to our chart:
                BaseSeries series;
                if (!chart.Series.TryGetValue(seriesName, out series))
                {
                    series = BaseSeries.Create(seriesType, seriesName, seriesIndex, unit);
                    chart.Series.Add(seriesName, series);
                }

                //Add our value:
                if (series.Values.Count == 0 || value.Time > series.Values[series.Values.Count - 1].Time
                    // always sample portfolio turnover and use latest value
                    || chartName == PortfolioTurnoverKey)
                {
                    series.AddPoint(value);
                }
            }
        }

        /// <summary>
        /// Sample estimated strategy capacity
        /// </summary>
        /// <param name="time">Time of the sample</param>
        protected override void SampleCapacity(DateTime time)
        {
            // Sample strategy capacity, round to 1k
            var roundedCapacity = _capacityEstimate.Capacity;
            Sample("Capacity", "Strategy Capacity", 0, SeriesType.Line, new ChartPoint(time, roundedCapacity), AlgorithmCurrencySymbol);
        }

        /// <summary>
        /// Add a range of samples from the users algorithms to the end of our current list.
        /// </summary>
        /// <param name="updates">Chart updates since the last request.</param>
        protected void SampleRange(IEnumerable<Chart> updates)
        {
            lock (ChartLock)
            {
                foreach (var update in updates)
                {
                    //Create the chart if it doesn't exist already:
                    Chart chart;
                    if (!Charts.TryGetValue(update.Name, out chart))
                    {
                        chart = new Chart(update.Name);
                        Charts.AddOrUpdate(update.Name, chart);
                    }

                    //Add these samples to this chart.
                    foreach (var series in update.Series.Values)
                    {
                        // let's assert we are within series count limit
                        if (_chartSeriesCount.Count < _job.Controls.MaximumChartSeries)
                        {
                            _chartSeriesCount.Add(series.Name);
                        }
                        else if (!_chartSeriesCount.Contains(series.Name))
                        {
                            // above the limit and this is a new series
                            if(!_chartSeriesCountExceededError)
                            {
                                _chartSeriesCountExceededError = true;
                                DebugMessage($"Exceeded maximum chart series count for organization tier, new series will be ignored. Limit is currently set at {_job.Controls.MaximumChartSeries}. https://qnt.co/docs-charting-quotas");
                            }
                            continue;
                        }

                        if (series.Values.Count > 0)
                        {
                            var thisSeries = chart.TryAddAndGetSeries(series.Name, series, forceAddNew: false);
                            if (series.SeriesType == SeriesType.Pie)
                            {
                                var dataPoint = series.ConsolidateChartPoints();
                                if (dataPoint != null)
                                {
                                    thisSeries.AddPoint(dataPoint);
                                }
                            }
                            else
                            {
                                var values = thisSeries.Values;
                                if ((values.Count + series.Values.Count) <= _job.Controls.MaximumDataPointsPerChartSeries) // check chart data point limit first
                                {
                                    //We already have this record, so just the new samples to the end:
                                    values.AddRange(series.Values);
                                }
                                else if (!_chartSeriesExceededDataPoints.Contains(chart.Name + series.Name))
                                {
                                    _chartSeriesExceededDataPoints.Add(chart.Name + series.Name);
                                    DebugMessage($"Exceeded maximum data points per series for organization tier, chart update skipped. Chart Name {update.Name}. Series name {series.Name}. https://qnt.co/docs-charting-quotas" +
                                                 $"Limit is currently set at {_job.Controls.MaximumDataPointsPerChartSeries}");
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Terminate the result thread and apply any required exit procedures like sending final results.
        /// </summary>
        public override void Exit()
        {
            // Only process the logs once
            if (!ExitTriggered)
            {
                Log.Trace("BacktestingResultHandler.Exit(): starting...");
                List<LogEntry> copy;
                lock (LogStore)
                {
                    copy = LogStore.ToList();
                }
                ProcessSynchronousEvents(true);
                Log.Trace("BacktestingResultHandler.Exit(): Saving logs...");
                var logLocation = SaveLogs(_algorithmId, copy);
                SystemDebugMessage("Your log was successfully created and can be retrieved from: " + logLocation);

                // Set exit flag, update task will send any message before stopping
                ExitTriggered = true;
                ExitEvent.Set();

                StopUpdateRunner();

                SendFinalResult();

                base.Exit();
            }
        }

        /// <summary>
        /// Send an algorithm status update to the browser.
        /// </summary>
        /// <param name="status">Status enum value.</param>
        /// <param name="message">Additional optional status message.</param>
        public virtual void SendStatusUpdate(AlgorithmStatus status, string message = "")
        {
            var statusPacket = new AlgorithmStatusPacket(_algorithmId, _projectId, status, message) { OptimizationId = _job.OptimizationId };
            MessagingHandler.Send(statusPacket);
        }

        /// <summary>
        /// Set the current runtime statistics of the algorithm.
        /// These are banner/title statistics which show at the top of the live trading results.
        /// </summary>
        /// <param name="key">Runtime headline statistic name</param>
        /// <param name="value">Runtime headline statistic value</param>
        public virtual void RuntimeStatistic(string key, string value)
        {
            lock (RuntimeStatistics)
            {
                RuntimeStatistics[key] = value;
            }
        }

        /// <summary>
        /// Handle order event
        /// </summary>
        /// <param name="newEvent">Event to process</param>
        public override void OrderEvent(OrderEvent newEvent)
        {
            _capacityEstimate?.OnOrderEvent(newEvent);
        }

        /// <summary>
        /// Process the synchronous result events, sampling and message reading.
        /// This method is triggered from the algorithm manager thread.
        /// </summary>
        /// <remarks>Prime candidate for putting into a base class. Is identical across all result handlers.</remarks>
        public virtual void ProcessSynchronousEvents(bool forceProcess = false)
        {
            if (Algorithm == null) return;

            _capacityEstimate.UpdateMarketCapacity(forceProcess);

            // Invalidate the processed days count so it gets recalculated
            _progressMonitor.InvalidateProcessedDays();

            // Update the equity bar
            UpdateAlgorithmEquity();

            var time = Algorithm.UtcTime;
            if (time > _nextSample || forceProcess)
            {
                //Set next sample time: 4000 samples per backtest
                _nextSample = time.Add(ResamplePeriod);

                //Sample the portfolio value over time for chart.
                SampleEquity(time);

                //Also add the user samples / plots to the result handler tracking:
                SampleRange(Algorithm.GetChartUpdates());
            }

            ProcessAlgorithmLogs();

            //Set the running statistics:
            foreach (var pair in Algorithm.RuntimeStatistics)
            {
                RuntimeStatistic(pair.Key, pair.Value);
            }
        }

        /// <summary>
        /// Configures the <see cref="Console.Out"/> and <see cref="Console.Error"/> <see cref="TextWriter"/>
        /// instances. By default, we forward <see cref="Console.WriteLine(string)"/> to <see cref="IAlgorithm.Debug"/>.
        /// This is perfect for running in the cloud, but since they're processed asynchronously, the ordering of these
        /// messages with respect to <see cref="Log"/> messages is broken. This can lead to differences in regression
        /// test logs based solely on the ordering of messages. To disable this forwarding, set <code>"forward-console-messages"</code>
        /// to <code>false</code> in the configuration.
        /// </summary>
        protected virtual void ConfigureConsoleTextWriter(IAlgorithm algorithm)
        {
            if (Config.GetBool("forward-console-messages", true))
            {
                // we need to forward Console.Write messages to the algorithm's Debug function
                Console.SetOut(new FuncTextWriter(algorithm.Debug));
                Console.SetError(new FuncTextWriter(algorithm.Error));
            }
            else
            {
                // we need to forward Console.Write messages to the standard Log functions
                Console.SetOut(new FuncTextWriter(msg => Log.Trace(msg)));
                Console.SetError(new FuncTextWriter(msg => Log.Error(msg)));
            }
        }

        /// <summary>
        /// Calculates and gets the current statistics for the algorithm
        /// </summary>
        /// <returns>The current statistics</returns>
        public StatisticsResults StatisticsResults()
        {
            return GenerateStatisticsResults(_capacityEstimate);
        }

        /// <summary>
        /// Sets or updates a custom summary statistic
        /// </summary>
        /// <param name="name">The statistic name</param>
        /// <param name="value">The statistic value</param>
        public void SetSummaryStatistic(string name, string value)
        {
            SummaryStatistic(name, value);
        }
    }
}
