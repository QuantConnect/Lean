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
using QuantConnect.Lean.Engine.TransactionHandlers;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Packets;
using QuantConnect.Statistics;
using QuantConnect.Util;
using System.IO;
using QuantConnect.Lean.Engine.Alphas;

namespace QuantConnect.Lean.Engine.Results
{
    /// <summary>
    /// Backtesting result handler passes messages back from the Lean to the User.
    /// </summary>
    public class BacktestingResultHandler : BaseResultsHandler, IResultHandler
    {
        // used for resetting out/error upon completion
        private static readonly TextWriter StandardOut = Console.Out;
        private static readonly TextWriter StandardError = Console.Error;
        private const double Samples = 4000;
        private const double MinimumSamplePeriod = 4;

        private bool _exitTriggered;
        private BacktestNodePacket _job;
        private int _jobDays;
        private DateTime _nextUpdate;
        private DateTime _nextS3Update;
        private DateTime _lastUpdate;
        private readonly List<string> _log;
        private string _errorMessage;
        private double _daysProcessed;
        private double _daysProcessedFrontier;
        private bool _processingFinalPacket;
        private readonly HashSet<string> _chartSeriesExceededDataPoints;

        //Processing Time:
        private DateTime _nextSample;
        private string _algorithmId;
        private int _projectId;

        /// <summary>
        /// Packeting message queue to temporarily store packets and then pull for processing.
        /// </summary>
        public ConcurrentQueue<Packet> Messages { get; set; }

        /// <summary>
        /// Charts collection for storing the master copy of user charting data.
        /// </summary>
        public ConcurrentDictionary<string, Chart> Charts { get; set; }

        /// <summary>
        /// Boolean flag indicating the result handler thread is completely finished and ready to dispose.
        /// </summary>
        public bool IsActive { get; private set; }

        /// <summary>
        /// Sampling period for timespans between resamples of the charting equity.
        /// </summary>
        /// <remarks>Specifically critical for backtesting since with such long timeframes the sampled data can get extreme.</remarks>
        public TimeSpan ResamplePeriod { get; private set; }

        /// <summary>
        /// How frequently the backtests push messages to the browser.
        /// </summary>
        /// <remarks>Update frequency of notification packets</remarks>
        public TimeSpan NotificationPeriod { get; }

        /// <summary>
        /// A dictionary containing summary statistics
        /// </summary>
        public Dictionary<string, string> FinalStatistics { get; private set; }

        /// <summary>
        /// Creates a new instance
        /// </summary>
        public BacktestingResultHandler()
        {
            Messages = new ConcurrentQueue<Packet>();
            Charts = new ConcurrentDictionary<string, Chart>();
            IsActive = true;
            ResamplePeriod = TimeSpan.FromMinutes(4);
            NotificationPeriod = TimeSpan.FromSeconds(2);

            _log = new List<string>();
            _chartSeriesExceededDataPoints = new HashSet<string>();

            // Delay uploading first packet
            _nextS3Update = StartTime.AddSeconds(30);

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
        /// <param name="transactionHandler">The transaction handler used to get the algorithms <see cref="Order"/> information</param>
        public virtual void Initialize(AlgorithmNodePacket job, IMessagingHandler messagingHandler, IApi api, ITransactionHandler transactionHandler)
        {
            _algorithmId = job.AlgorithmId;
            _projectId = job.ProjectId;
            MessagingHandler = messagingHandler;
            TransactionHandler = transactionHandler;
            _job = (BacktestNodePacket)job;
            if (_job == null) throw new Exception("BacktestingResultHandler.Constructor(): Submitted Job type invalid.");
            JobId = _job.BacktestId;
            CompileId = _job.CompileId;
        }

        /// <summary>
        /// The main processing method steps through the messaging queue and processes the messages one by one.
        /// </summary>
        public void Run()
        {
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
                Log.Error(err);
                // quit the algorithm due to error
                Algorithm.RunTimeError = err;
            }

            Log.Trace("BacktestingResultHandler.Run(): Ending Thread...");
            IsActive = false;

            // reset standard out/error
            Console.SetOut(StandardOut);
            Console.SetError(StandardError);
        } // End Run();

        /// <summary>
        /// Send a backtest update to the browser taking a latest snapshot of the charting data.
        /// </summary>
        public void Update()
        {
            try
            {
                //Sometimes don't run the update, if not ready or we're ending.
                if (Algorithm?.Transactions == null || _processingFinalPacket)
                {
                    return;
                }

                if (DateTime.UtcNow <= _nextUpdate || _daysProcessed < _daysProcessedFrontier) return;

                //Extract the orders since last update
                var deltaOrders = new Dictionary<int, Order>();

                try
                {
                    deltaOrders = (from order in TransactionHandler.Orders
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
                    _lastUpdate = Algorithm.UtcTime.Date;
                    _daysProcessedFrontier = _daysProcessed + 1;
                    _nextUpdate = DateTime.UtcNow.AddSeconds(2);
                }
                catch (Exception err)
                {
                    Log.Error(err, "Can't update variables");
                }

                var deltaCharts = new Dictionary<string, Chart>();
                lock (ChartLock)
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
                lock (RuntimeStatistics)
                {
                    foreach (var pair in RuntimeStatistics)
                    {
                        runtimeStatistics.Add(pair.Key, pair.Value);
                    }
                }
                GetAlgorithmRuntimeStatistics(runtimeStatistics);

                //Profit Loss Changes:
                var progress = Convert.ToDecimal(_daysProcessed / _jobDays);
                if (progress > 0.999m) progress = 0.999m;

                //1. Cloud Upload -> Upload the whole packet to S3  Immediately:
                if (DateTime.UtcNow > _nextS3Update)
                {
                    // For intermediate backtesting results, we truncate the order list to include only the last 100 orders
                    // The final packet will contain the full list of orders.
                    const int maxOrders = 100;
                    var orderCount = TransactionHandler.Orders.Count;

                    var completeResult = new BacktestResult(
                        Charts,
                        orderCount > maxOrders ? TransactionHandler.Orders.Skip(orderCount - maxOrders).ToDictionary() : TransactionHandler.Orders.ToDictionary(),
                        Algorithm.Transactions.TransactionRecord,
                        new Dictionary<string, string>(),
                        runtimeStatistics,
                        new Dictionary<string, AlgorithmPerformance>());

                    StoreResult(new BacktestResultPacket(_job, completeResult, progress));

                    _nextS3Update = DateTime.UtcNow.AddSeconds(30);
                }

                //2. Backtest Update -> Send the truncated packet to the backtester:
                var splitPackets = SplitPackets(deltaCharts, deltaOrders, runtimeStatistics, progress);

                foreach (var backtestingPacket in splitPackets)
                {
                    MessagingHandler.Send(backtestingPacket);
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
                if (chart.Series.Values.Aggregate(0, (i, x) => i + x.Values.Count) == 0) continue;

                splitPackets.Add(new BacktestResultPacket(_job, new BacktestResult
                {
                    Charts = new Dictionary<string, Chart>()
                    {
                        {chart.Name, chart}
                    }
                }, progress));
            }

            // Send alpha run time statistics
            splitPackets.Add(new BacktestResultPacket(_job, new BacktestResult { AlphaRuntimeStatistics = AlphaRuntimeStatistics}, progress));

            // Add the orders into the charting packet:
            splitPackets.Add(new BacktestResultPacket(_job, new BacktestResult { Orders = deltaOrders }, progress));

            //Add any user runtime statistics into the backtest.
            splitPackets.Add(new BacktestResultPacket(_job, new BacktestResult { RuntimeStatistics = runtimeStatistics }, progress));

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
                // Make sure this is the right type of packet:
                if (packet.Type != PacketType.BacktestResult) return;

                // Port to packet format:
                var result = packet as BacktestResultPacket;

                if (result != null)
                {
                    // Get Storage Location:
                    var key = _job.BacktestId + ".json";

                    BacktestResult results;
                    lock (ChartLock)
                    {
                        results = new BacktestResult(
                            result.Results.Charts.ToDictionary(x => x.Key, x => x.Value.Clone()),
                            result.Results.Orders,
                            result.Results.ProfitLoss,
                            result.Results.Statistics,
                            result.Results.RuntimeStatistics,
                            result.Results.RollingWindow,
                            result.Results.TotalPerformance
                        )
                        // Set Alpha Runtime Statistics
                        { AlphaRuntimeStatistics = result.Results.AlphaRuntimeStatistics };
                    }
                    // Save results
                    SaveResults(key, results);
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
        public void SendFinalResult()
        {
            try
            {
                _processingFinalPacket = true;

                //Convert local dictionary:
                var charts = new Dictionary<string, Chart>(Charts);
                var orders = new Dictionary<int, Order>(TransactionHandler.Orders);
                var profitLoss = new SortedDictionary<DateTime, decimal>(Algorithm.Transactions.TransactionRecord);
                var runtime = GetAlgorithmRuntimeStatistics();
                var statisticsResults = GenerateStatisticsResults(charts, profitLoss);

                FinalStatistics = statisticsResults.Summary;

                // clear the trades collection before placing inside the backtest result
                foreach (var ap in statisticsResults.RollingPerformances.Values)
                {
                    ap.ClosedTrades.Clear();
                }

                //Create a result packet to send to the browser.
                var result = new BacktestResultPacket(_job,
                    new BacktestResult(charts, orders, profitLoss, statisticsResults.Summary, runtime, statisticsResults.RollingPerformances, statisticsResults.TotalPerformance)
                        { AlphaRuntimeStatistics = AlphaRuntimeStatistics })
                {
                    ProcessingTime = (DateTime.UtcNow - StartTime).TotalSeconds,
                    DateFinished = DateTime.Now,
                    Progress = 1
                };

                //Place result into storage.
                StoreResult(result);

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
            StartingPortfolioValue = startingPortfolioValue;

            //Get the resample period:
            var totalMinutes = (_job.PeriodFinish - _job.PeriodStart).TotalMinutes;
            var resampleMinutes = totalMinutes < MinimumSamplePeriod * Samples ? MinimumSamplePeriod : totalMinutes / Samples; // Space out the sampling every
            ResamplePeriod = TimeSpan.FromMinutes(resampleMinutes);
            Log.Trace("BacktestingResultHandler(): Sample Period Set: " + resampleMinutes.ToStringInvariant("00.00"));

            //Setup the sampling periods:
            _jobDays = Algorithm.Securities.Count > 0
                ? Time.TradeableDates(Algorithm.Securities.Values, _job.PeriodStart, _job.PeriodFinish)
                : Convert.ToInt32((_job.PeriodFinish.Date - _job.PeriodStart.Date).TotalDays) + 1;

            //Set the security / market types.
            var types = new List<SecurityType>();
            foreach (var kvp in Algorithm.Securities)
            {
                var security = kvp.Value;

                if (!types.Contains(security.Type)) types.Add(security.Type);
            }
            SecurityType(types);

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
        /// Send a debug message back to the browser console.
        /// </summary>
        /// <param name="message">Message we'd like shown in console.</param>
        public void DebugMessage(string message)
        {
            Messages.Enqueue(new DebugPacket(_projectId, JobId, CompileId, message));

            //Save last message sent:
            if (Algorithm != null)
            {
                _log.Add(Algorithm.Time.ToStringInvariant(DateFormat.UI) + " " + message);
            }
        }

        /// <summary>
        /// Send a system debug message back to the browser console.
        /// </summary>
        /// <param name="message">Message we'd like shown in console.</param>
        public void SystemDebugMessage(string message)
        {
            Messages.Enqueue(new SystemDebugPacket(_projectId, JobId, CompileId, message));

            //Save last message sent:
            if (Algorithm != null)
            {
                _log.Add(Algorithm.Time.ToStringInvariant(DateFormat.UI) + " " + message);
            }
        }

        /// <summary>
        /// Send a logging message to the log list for storage.
        /// </summary>
        /// <param name="message">Message we'd in the log.</param>
        public void LogMessage(string message)
        {
            Messages.Enqueue(new LogPacket(JobId, message));

            if (Algorithm != null)
            {
                _log.Add(Algorithm.Time.ToStringInvariant(DateFormat.UI) + " " + message);
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
            Messages.Enqueue(new HandledErrorPacket(JobId, message, stacktrace));
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
            Messages.Enqueue(new RuntimeErrorPacket(_job.UserId, JobId, message, stacktrace));
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
            Sample("Strategy Equity", "Equity", 0, SeriesType.Candle, time, value);

            //Recalculate the days processed:
            _daysProcessed = (time - Algorithm.StartDate).TotalDays;
        }

        /// <summary>
        /// Sample the current daily performance directly with a time-value pair.
        /// </summary>
        /// <param name="time">Current backtest date.</param>
        /// <param name="value">Current daily performance value.</param>
        public virtual void SamplePerformance(DateTime time, decimal value)
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
            Sample("Benchmark", "Benchmark", 0, SeriesType.Line, time, value);
        }

        /// <summary>
        /// Add a range of samples from the users algorithms to the end of our current list.
        /// </summary>
        /// <param name="updates">Chart updates since the last request.</param>
        public void SampleRange(List<Chart> updates)
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

                    // for alpha assets chart, we always create a new series instance (step on previous value)
                    var forceNewSeries = update.Name == ChartingInsightManagerExtension.AlphaAssets;

                    //Add these samples to this chart.
                    foreach (var series in update.Series.Values)
                    {
                        if (series.Values.Count > 0)
                        {
                            var thisSeries = chart.TryAddAndGetSeries(series.Name, series.SeriesType, series.Index,
                                series.Unit, series.Color, series.ScatterMarkerSymbol,
                                forceNewSeries);
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
                                else if(!_chartSeriesExceededDataPoints.Contains(chart.Name + series.Name))
                                {
                                    _chartSeriesExceededDataPoints.Add(chart.Name + series.Name);
                                    DebugMessage($"Exceeded maximum data points per series, chart update skipped. Chart Name {update.Name}. Series name {series.Name}. " +
                                                 $"Limit is currently set at {_job.Controls.MaximumDataPointsPerChartSeries}");
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Terminate the result thread and apply any required exit procedures.
        /// </summary>
        public virtual void Exit()
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
        public virtual void OrderEvent(OrderEvent newEvent)
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
            MessagingHandler.Send(statusPacket);
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
            lock (RuntimeStatistics)
            {
                RuntimeStatistics[key] = value;
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
        public virtual void ProcessSynchronousEvents(bool forceProcess = false)
        {
            if (Algorithm == null) return;

            var time = Algorithm.UtcTime;

            if (time > _nextSample || forceProcess)
            {
                //Set next sample time: 4000 samples per backtest
                _nextSample = time.Add(ResamplePeriod);

                //Sample the portfolio value over time for chart.
                SampleEquity(time, Math.Round(Algorithm.Portfolio.TotalPortfolioValue, 4));

                //Also add the user samples / plots to the result handler tracking:
                SampleRange(Algorithm.GetChartUpdates());

                //Sample the asset pricing:
                foreach (var kvp in Algorithm.Securities)
                {
                    var security = kvp.Value;

                    SampleAssetPrices(security.Symbol, time, security.Price);
                }
            }

            long endTime;
            // avoid calling utcNow if not required
            if (Algorithm.DebugMessages.Count > 0)
            {
                //Send out the debug messages:
                endTime = DateTime.UtcNow.AddMilliseconds(250).Ticks;
                while (Algorithm.DebugMessages.Count > 0 && DateTime.UtcNow.Ticks < endTime)
                {
                    string message;
                    if (Algorithm.DebugMessages.TryDequeue(out message))
                    {
                        DebugMessage(message);
                    }
                }
            }

            // avoid calling utcNow if not required
            if (Algorithm.ErrorMessages.Count > 0)
            {
                //Send out the error messages:
                endTime = DateTime.UtcNow.AddMilliseconds(250).Ticks;
                while (Algorithm.ErrorMessages.Count > 0 && DateTime.UtcNow.Ticks < endTime)
                {
                    string message;
                    if (Algorithm.ErrorMessages.TryDequeue(out message))
                    {
                        ErrorMessage(message);
                    }
                }
            }

            // avoid calling utcNow if not required
            if (Algorithm.LogMessages.Count > 0)
            {
                //Send out the log messages:
                endTime = DateTime.UtcNow.AddMilliseconds(250).Ticks;
                while (Algorithm.LogMessages.Count > 0 && DateTime.UtcNow.Ticks < endTime)
                {
                    string message;
                    if (Algorithm.LogMessages.TryDequeue(out message))
                    {
                        LogMessage(message);
                    }
                }
            }

            //Set the running statistics:
            foreach (var pair in Algorithm.RuntimeStatistics)
            {
                RuntimeStatistic(pair.Key, pair.Value);
            }
        }
    }
}
