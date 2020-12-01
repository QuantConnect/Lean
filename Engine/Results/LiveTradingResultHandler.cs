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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using QuantConnect.Configuration;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.Alphas;
using QuantConnect.Lean.Engine.TransactionHandlers;
using QuantConnect.Logging;
using QuantConnect.Notifications;
using QuantConnect.Orders;
using QuantConnect.Packets;
using QuantConnect.Securities;
using QuantConnect.Statistics;
using QuantConnect.Util;

namespace QuantConnect.Lean.Engine.Results
{
    /// <summary>
    /// Live trading result handler implementation passes the messages to the QC live trading interface.
    /// </summary>
    /// <remarks>Live trading result handler is quite busy. It sends constant price updates, equity updates and order/holdings updates.</remarks>
    public class LiveTradingResultHandler : BaseResultsHandler, IResultHandler
    {
        // Required properties for the cloud app.
        private LiveNodePacket _job;

        //Update loop:
        private DateTime _nextUpdate;
        private DateTime _nextChartsUpdate;
        private DateTime _nextChartTrimming;
        private DateTime _nextLogStoreUpdate;
        private DateTime _nextStatisticsUpdate;
        private DateTime _nextStatusUpdate;
        private DateTime _currentUtcDate;

        //Log Message Store:
        private DateTime _nextSample;
        private IApi _api;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly int _streamedChartLimit;
        private readonly int _streamedChartGroupSize;

        private bool _sampleChartAlways;
        private bool _userExchangeIsOpen;
        private decimal _portfolioValue;
        private decimal _benchmarkValue;
        private DateTime _lastChartSampleLogicCheck;
        private readonly Dictionary<string, SecurityExchangeHours> _exchangeHours;

        /// <summary>
        /// Creates a new instance
        /// </summary>
        public LiveTradingResultHandler()
        {
            _exchangeHours = new Dictionary<string, SecurityExchangeHours>();
            _cancellationTokenSource = new CancellationTokenSource();
            ResamplePeriod = TimeSpan.FromSeconds(2);
            NotificationPeriod = TimeSpan.FromSeconds(1);
            SetNextStatusUpdate();
            _streamedChartLimit = Config.GetInt("streamed-chart-limit", 12);
            _streamedChartGroupSize = Config.GetInt("streamed-chart-group-size", 3);
        }

        /// <summary>
        /// Initialize the result handler with this result packet.
        /// </summary>
        /// <param name="job">Algorithm job packet for this result handler</param>
        /// <param name="messagingHandler">The handler responsible for communicating messages to listeners</param>
        /// <param name="api">The api instance used for handling logs</param>
        /// <param name="transactionHandler">The transaction handler used to get the algorithms Orders information</param>
        public override void Initialize(AlgorithmNodePacket job, IMessagingHandler messagingHandler, IApi api, ITransactionHandler transactionHandler)
        {
            _api = api;
            _job = (LiveNodePacket)job;
            if (_job == null) throw new Exception("LiveResultHandler.Constructor(): Submitted Job type invalid.");
            PreviousUtcSampleTime = DateTime.UtcNow;
            _currentUtcDate = PreviousUtcSampleTime.Date;
            base.Initialize(job, messagingHandler, api, transactionHandler);
        }

        /// <summary>
        /// Live trading result handler thread.
        /// </summary>
        protected override void Run()
        {
            var sleepSpan = TimeSpan.FromMilliseconds(100);
            for (var i = 0; i < 30; i++)
            {
                // let's check exit flag while sleeping so we reduce latency on shutdown
                if (ExitTriggered)
                {
                    break;
                }
                // give the algorithm time to initialize, else we will log an error right away
                Thread.Sleep(sleepSpan);
            }

            // -> 1. Run Primary Sender Loop: Continually process messages from queue as soon as they arrive.
            while (!(ExitTriggered && Messages.Count == 0))
            {
                try
                {
                    //1. Process Simple Messages in Queue
                    Packet packet;
                    if (Messages.TryDequeue(out packet))
                    {
                        MessagingHandler.Send(packet);
                    }

                    //2. Update the packet scanner:
                    Update();

                    if (Messages.Count == 0)
                    {
                        // prevent thread lock/tight loop when there's no work to be done
                        Thread.Sleep(100);
                    }
                }
                catch (Exception err)
                {
                    Log.Error(err);
                }
            } // While !End.

            Log.Trace("LiveTradingResultHandler.Run(): Ending Thread...");
        } // End Run();


        /// <summary>
        /// Every so often send an update to the browser with the current state of the algorithm.
        /// </summary>
        private void Update()
        {
            //Error checks if the algorithm & threads have not loaded yet, or are closing down.
            if (Algorithm?.Transactions == null || TransactionHandler.Orders == null || !Algorithm.GetLocked())
            {
                Log.Error("LiveTradingResultHandler.Update(): Algorithm not yet initialized.");
                return;
            }

            if (ExitTriggered)
            {
                return;
            }

            var utcNow = DateTime.UtcNow;
            if (utcNow > _nextUpdate)
            {
                try
                {
                    Dictionary<int, Order> deltaOrders;
                    {
                        var stopwatch = Stopwatch.StartNew();
                        deltaOrders = GetDeltaOrders(LastDeltaOrderPosition, shouldStop: orderCount => stopwatch.ElapsedMilliseconds > 15);
                    }
                    var deltaOrderEvents = TransactionHandler.OrderEvents.Skip(LastDeltaOrderEventsPosition).Take(50).ToList();
                    LastDeltaOrderEventsPosition += deltaOrderEvents.Count;

                    //Create and send back the changes in chart since the algorithm started.
                    var deltaCharts = new Dictionary<string, Chart>();
                    Log.Debug("LiveTradingResultHandler.Update(): Build delta charts");
                    var performanceCharts = new Dictionary<string, Chart>();
                    lock (ChartLock)
                    {
                        //Get the updates since the last chart
                        foreach (var chart in Charts)
                        {
                            var chartUpdates = chart.Value.GetUpdates();
                            // we only want to stream charts that have new updates
                            if (!chartUpdates.IsEmpty())
                            {
                                // remove directory pathing characters from chart names
                                var safeName = chart.Value.Name.Replace('/', '-');
                                DictionarySafeAdd(deltaCharts, safeName, chartUpdates, "deltaCharts");
                            }

                            if (AlgorithmPerformanceCharts.Contains(chart.Key))
                            {
                                performanceCharts[chart.Key] = chart.Value.Clone();
                            }
                        }
                    }
                    Log.Debug("LiveTradingResultHandler.Update(): End build delta charts");

                    //Profit loss changes, get the banner statistics, summary information on the performance for the headers.
                    var holdings = new Dictionary<string, Holding>();
                    var deltaStatistics = new Dictionary<string, string>();
                    var runtimeStatistics = new Dictionary<string, string>();
                    var serverStatistics = GetServerStatistics(utcNow);

                    // Only send holdings updates when we have changes in orders, except for first time, then we want to send all
                    foreach (var kvp in Algorithm.Securities
                        // we send non internal, non canonical and tradable securities. When securities are removed they are marked as non tradable
                        .Where(pair => pair.Value.IsTradable && !pair.Value.IsInternalFeed() && !pair.Key.IsCanonical())
                        .OrderBy(x => x.Key.Value))
                    {
                        var security = kvp.Value;
                        DictionarySafeAdd(holdings, security.Symbol.Value, new Holding(security), "holdings");
                    }

                    //Add the algorithm statistics first.
                    Log.Debug("LiveTradingResultHandler.Update(): Build run time stats");
                    lock (RuntimeStatistics)
                    {
                        foreach (var pair in RuntimeStatistics)
                        {
                            runtimeStatistics.Add(pair.Key, pair.Value);
                        }
                    }
                    Log.Debug("LiveTradingResultHandler.Update(): End build run time stats");

                    //Add other fixed parameters.
                    var summary = GenerateStatisticsResults(performanceCharts).Summary;
                    GetAlgorithmRuntimeStatistics(summary, runtimeStatistics);

                    // since we're sending multiple packets, let's do it async and forget about it
                    // chart data can get big so let's break them up into groups
                    var splitPackets = SplitPackets(deltaCharts, deltaOrders, holdings, Algorithm.Portfolio.CashBook, deltaStatistics, runtimeStatistics, serverStatistics, deltaOrderEvents);

                    foreach (var liveResultPacket in splitPackets)
                    {
                        MessagingHandler.Send(liveResultPacket);
                    }

                    //Send full packet to storage.
                    if (utcNow > _nextChartsUpdate)
                    {
                        Log.Debug("LiveTradingResultHandler.Update(): Pre-store result");
                        var chartComplete = new Dictionary<string, Chart>();
                        lock (ChartLock)
                        {
                            foreach (var chart in Charts)
                            {
                                // remove directory pathing characters from chart names
                                var safeName = chart.Value.Name.Replace('/', '-');
                                DictionarySafeAdd(chartComplete, safeName, chart.Value.Clone(), "chartComplete");
                            }
                        }

                        var orderEvents = GetOrderEventsToStore();

                        var orders = new Dictionary<int, Order>(TransactionHandler.Orders);
                        var complete = new LiveResultPacket(_job, new LiveResult(new LiveResultParameters(chartComplete, orders, Algorithm.Transactions.TransactionRecord, holdings, Algorithm.Portfolio.CashBook, deltaStatistics, runtimeStatistics, orderEvents, serverStatistics)));
                        StoreResult(complete);
                        _nextChartsUpdate = DateTime.UtcNow.Add(ChartUpdateInterval);
                        Log.Debug("LiveTradingResultHandler.Update(): End-store result");
                    }

                    // Upload the logs every 1-2 minutes; this can be a heavy operation depending on amount of live logging and should probably be done asynchronously.
                    if (utcNow > _nextLogStoreUpdate)
                    {
                        List<LogEntry> logs;
                        Log.Debug("LiveTradingResultHandler.Update(): Storing log...");
                        lock (LogStore)
                        {
                            // we need a new container instance so we can store the logs outside the lock
                            logs = new List<LogEntry>(LogStore);
                            LogStore.Clear();
                        }
                        SaveLogs(AlgorithmId, logs);

                        _nextLogStoreUpdate = DateTime.UtcNow.AddMinutes(2);
                        Log.Debug("LiveTradingResultHandler.Update(): Finished storing log");
                    }

                    // Every minute send usage statistics:
                    if (utcNow > _nextStatisticsUpdate)
                    {
                        try
                        {
                            _api.SendStatistics(
                                _job.AlgorithmId,
                                Algorithm.Portfolio.TotalUnrealizedProfit,
                                Algorithm.Portfolio.TotalFees,
                                Algorithm.Portfolio.TotalProfit,
                                Algorithm.Portfolio.TotalHoldingsValue,
                                Algorithm.Portfolio.TotalPortfolioValue,
                                GetNetReturn(),
                                Algorithm.Portfolio.TotalSaleVolume,
                                TransactionHandler.OrdersCount, 0);
                        }
                        catch (Exception err)
                        {
                            Log.Error(err, "Error sending statistics:");
                        }
                        _nextStatisticsUpdate = utcNow.AddMinutes(1);
                    }

                    if (utcNow > _nextStatusUpdate)
                    {
                        var chartComplete = new Dictionary<string, Chart>();
                        lock (ChartLock)
                        {
                            foreach (var chart in Charts)
                            {
                                // remove directory pathing characters from chart names
                                var safeName = chart.Value.Name.Replace('/', '-');
                                DictionarySafeAdd(chartComplete, safeName, chart.Value.Clone(), "chartComplete");
                            }
                        }
                        StoreStatusFile(
                            runtimeStatistics,
                            holdings,
                            chartComplete,
                            new SortedDictionary<DateTime, decimal>(Algorithm.Transactions.TransactionRecord),
                            serverStatistics);

                        SetNextStatusUpdate();
                    }

                    if (_currentUtcDate != utcNow.Date)
                    {
                        StoreOrderEvents(_currentUtcDate, GetOrderEventsToStore());
                        // start storing in a new date file
                        _currentUtcDate = utcNow.Date;
                    }

                    if (utcNow > _nextChartTrimming)
                    {
                        Log.Debug("LiveTradingResultHandler.Update(): Trimming charts");
                        var timeLimitUtc = Time.DateTimeToUnixTimeStamp(utcNow.AddDays(-2));
                        lock (ChartLock)
                        {
                            foreach (var chart in Charts)
                            {
                                foreach (var series in chart.Value.Series)
                                {
                                    // trim data that's older than 2 days
                                    series.Value.Values =
                                        (from v in series.Value.Values
                                         where v.x > timeLimitUtc
                                         select v).ToList();
                                }
                            }
                        }
                        _nextChartTrimming = DateTime.UtcNow.AddMinutes(10);
                        Log.Debug("LiveTradingResultHandler.Update(): Finished trimming charts");
                    }
                }
                catch (Exception err)
                {
                    Log.Error(err, "LiveTradingResultHandler().Update(): ", true);
                }

                //Set the new update time after we've finished processing.
                // The processing can takes time depending on how large the packets are.
                _nextUpdate = DateTime.UtcNow.Add(MainUpdateInterval);
            } // End Update Charts:
        }

        /// <summary>
        /// Stores the order events
        /// </summary>
        /// <param name="utcTime">The utc date associated with these order events</param>
        /// <param name="orderEvents">The order events to store</param>
        protected override void StoreOrderEvents(DateTime utcTime, List<OrderEvent> orderEvents)
        {
            if (orderEvents.Count <= 0)
            {
                return;
            }

            var filename = $"{AlgorithmId}-{utcTime:yyyy-MM-dd}-order-events.json";
            var path = GetResultsPath(filename);

            var data = JsonConvert.SerializeObject(orderEvents, Formatting.None);

            File.WriteAllText(path, data);
        }

        /// <summary>
        /// Gets the order events generated in '_currentUtcDate'
        /// </summary>
        private List<OrderEvent> GetOrderEventsToStore()
        {
            return TransactionHandler.OrderEvents.Where(orderEvent => orderEvent.UtcTime >= _currentUtcDate).ToList();
        }

        private void SetNextStatusUpdate()
        {
            // Update the status json file each day at 1am UTC
            // after the daily performance has been sampled
            _nextStatusUpdate = DateTime.UtcNow.Date.AddDays(1).AddHours(1);
        }

        /// <summary>
        /// Will store the complete status of the algorithm in a single json file
        /// </summary>
        /// <remarks>Will sample charts every 12 hours, 2 data points per day at maximum,
        /// to reduce file size</remarks>
        private void StoreStatusFile(Dictionary<string, string> runtimeStatistics,
            Dictionary<string, Holding> holdings,
            Dictionary<string, Chart> chartComplete,
            SortedDictionary<DateTime, decimal> profitLoss,
            Dictionary<string, string> serverStatistics = null,
            StatisticsResults statistics = null)
        {
            try
            {
                Log.Debug("LiveTradingResultHandler.Update(): status update start...");

                if (statistics == null)
                {
                    statistics = GenerateStatisticsResults(chartComplete, profitLoss);
                }

                // sample the entire charts with a 12 hours resolution
                var dailySampler = new SeriesSampler(TimeSpan.FromHours(12));
                chartComplete = dailySampler.SampleCharts(chartComplete, Time.BeginningOfTime, Time.EndOfTime);

                var result = new LiveResult(new LiveResultParameters(chartComplete,
                    new Dictionary<int, Order>(TransactionHandler.Orders),
                    Algorithm.Transactions.TransactionRecord,
                    holdings,
                    Algorithm.Portfolio.CashBook,
                    statistics: statistics.Summary,
                    runtimeStatistics: runtimeStatistics,
                    orderEvents: null, // we stored order events separately
                    serverStatistics: serverStatistics,
                    alphaRuntimeStatistics: AlphaRuntimeStatistics));

                SaveResults($"{AlgorithmId}.json", result);
                Log.Debug("LiveTradingResultHandler.Update(): status update end.");
            }
            catch (Exception err)
            {
                Log.Error(err, "Error storing status update");
            }
        }

        /// <summary>
        /// Run over all the data and break it into smaller packets to ensure they all arrive at the terminal
        /// </summary>
        private IEnumerable<LiveResultPacket> SplitPackets(Dictionary<string, Chart> deltaCharts,
            Dictionary<int, Order> deltaOrders,
            Dictionary<string, Holding> holdings,
            CashBook cashbook,
            Dictionary<string, string> deltaStatistics,
            Dictionary<string, string> runtimeStatistics,
            Dictionary<string, string> serverStatistics,
            List<OrderEvent> deltaOrderEvents)
        {
            // break the charts into groups
            var current = new Dictionary<string, Chart>();
            var chartPackets = new List<LiveResultPacket>();

            // First add send charts

            // Loop through all the charts, add them to packets to be sent.
            // Group three charts per packet
            foreach (var deltaChart in deltaCharts.Values)
            {
                current.Add(deltaChart.Name, deltaChart);

                if (current.Count >= _streamedChartGroupSize)
                {
                    // Add the micro packet to transport.
                    chartPackets.Add(new LiveResultPacket(_job, new LiveResult { Charts = current }));

                    // Reset the carrier variable.
                    current = new Dictionary<string, Chart>();
                    if (chartPackets.Count * _streamedChartGroupSize >= _streamedChartLimit)
                    {
                        // stream a maximum number of charts
                        break;
                    }
                }
            }

            // Add whatever is left over here too
            // unless it is a wildcard subscription
            if (current.Count > 0)
            {
                chartPackets.Add(new LiveResultPacket(_job, new LiveResult { Charts = current }));
            }

            // these are easier to split up, not as big as the chart objects
            var packets = new[]
            {
                new LiveResultPacket(_job, new LiveResult { Holdings = holdings, Cash = cashbook}),
                new LiveResultPacket(_job, new LiveResult
                {
                    Statistics = deltaStatistics,
                    RuntimeStatistics = runtimeStatistics,
                    ServerStatistics = serverStatistics,
                    AlphaRuntimeStatistics = AlphaRuntimeStatistics
                })
            };

            var result = packets.Concat(chartPackets);

            // only send order and order event packet if there is actually any update
            if (deltaOrders.Count > 0 || deltaOrderEvents.Count > 0)
            {
                result= result.Concat(new []{ new LiveResultPacket(_job, new LiveResult { Orders = deltaOrders, OrderEvents = deltaOrderEvents }) });
            }

            return result;
        }


        /// <summary>
        /// Send a live trading debug message to the live console.
        /// </summary>
        /// <param name="message">Message we'd like shown in console.</param>
        /// <remarks>When there are already 500 messages in the queue it stops adding new messages.</remarks>
        public void DebugMessage(string message)
        {
            if (Messages.Count > 500) return; //if too many in the queue already skip the logging.
            Messages.Enqueue(new DebugPacket(_job.ProjectId, AlgorithmId, CompileId, message));
            AddToLogStore(message);
        }

        /// <summary>
        /// Send a live trading system debug message to the live console.
        /// </summary>
        /// <param name="message">Message we'd like shown in console.</param>
        public void SystemDebugMessage(string message)
        {
            Messages.Enqueue(new SystemDebugPacket(_job.ProjectId, AlgorithmId, CompileId, message));
            AddToLogStore(message);
        }


        /// <summary>
        /// Log string messages and send them to the console.
        /// </summary>
        /// <param name="message">String message wed like logged.</param>
        /// <remarks>When there are already 500 messages in the queue it stops adding new messages.</remarks>
        public void LogMessage(string message)
        {
            //Send the logging messages out immediately for live trading:
            if (Messages.Count > 500) return;
            Messages.Enqueue(new LogPacket(AlgorithmId, message));
            AddToLogStore(message);
        }

        /// <summary>
        /// Save an algorithm message to the log store. Uses a different timestamped method of adding messaging to interweve debug and logging messages.
        /// </summary>
        /// <param name="message">String message to send to browser.</param>
        protected override void AddToLogStore(string message)
        {
            Log.Debug("LiveTradingResultHandler.AddToLogStore(): Adding");
            lock (LogStore)
            {
                LogStore.Add(new LogEntry(DateTime.Now.ToStringInvariant(DateFormat.UI) + " " + message));
            }
            Log.Debug("LiveTradingResultHandler.AddToLogStore(): Finished adding");
        }

        /// <summary>
        /// Send an error message back to the browser console and highlight it read.
        /// </summary>
        /// <param name="message">Message we'd like shown in console.</param>
        /// <param name="stacktrace">Stacktrace to show in the console.</param>
        public void ErrorMessage(string message, string stacktrace = "")
        {
            if (Messages.Count > 500) return;
            Messages.Enqueue(new HandledErrorPacket(AlgorithmId, message, stacktrace));
            AddToLogStore(message + (!string.IsNullOrEmpty(stacktrace) ? ": StackTrace: " + stacktrace : string.Empty));
        }

        /// <summary>
        /// Send a list of secutity types that the algorithm trades to the browser to show the market clock - is this market open or closed!
        /// </summary>
        /// <param name="types">List of security types</param>
        public void SecurityType(List<SecurityType> types)
        {
            var packet = new SecurityTypesPacket { Types = types };
            Messages.Enqueue(packet);
        }

        /// <summary>
        /// Send a runtime error back to the users browser and highlight it red.
        /// </summary>
        /// <param name="message">Runtime error message</param>
        /// <param name="stacktrace">Associated error stack trace.</param>
        public void RuntimeError(string message, string stacktrace = "")
        {
            Messages.Enqueue(new RuntimeErrorPacket(_job.UserId, AlgorithmId, message, stacktrace));
            AddToLogStore(message + (!string.IsNullOrEmpty(stacktrace) ? ": StackTrace: " + stacktrace : string.Empty));
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
        protected override void Sample(string chartName, string seriesName, int seriesIndex, SeriesType seriesType, DateTime time, decimal value, string unit = "$")
        {
            // Sampling during warming up period skews statistics
            if (Algorithm.IsWarmingUp)
            {
                return;
            }

            Log.Debug("LiveTradingResultHandler.Sample(): Sampling " + chartName + "." + seriesName);
            lock (ChartLock)
            {
                //Add a copy locally:
                if (!Charts.ContainsKey(chartName))
                {
                    Charts.AddOrUpdate(chartName, new Chart(chartName));
                }

                //Add the sample to our chart:
                if (!Charts[chartName].Series.ContainsKey(seriesName))
                {
                    Charts[chartName].Series.Add(seriesName, new Series(seriesName, seriesType, seriesIndex, unit));
                }

                //Add our value:
                Charts[chartName].Series[seriesName].Values.Add(new ChartPoint(time, value));
            }
            Log.Debug("LiveTradingResultHandler.Sample(): Done sampling " + chartName + "." + seriesName);
        }

        /// <summary>
        /// Wrapper methond on sample to create the equity chart.
        /// </summary>
        /// <param name="time">Time of the sample.</param>
        /// <param name="value">Equity value at this moment in time.</param>
        /// <seealso cref="Sample(string,string,int,SeriesType,DateTime,decimal,string)"/>
        protected override void SampleEquity(DateTime time, decimal value)
        {
            if (value > 0)
            {
                Log.Debug("LiveTradingResultHandler.SampleEquity(): " + time.ToShortTimeString() + " >" + value);

                base.SampleEquity(time, value);
            }
        }

        /// <summary>
        /// Add a range of samples from the users algorithms to the end of our current list.
        /// </summary>
        /// <param name="updates">Chart updates since the last request.</param>
        /// <seealso cref="Sample(string,string,int,SeriesType,DateTime,decimal,string)"/>
        protected void SampleRange(List<Chart> updates)
        {
            Log.Debug("LiveTradingResultHandler.SampleRange(): Begin sampling");
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
                                //We already have this record, so just the new samples to the end:
                                thisSeries.Values.AddRange(series.Values);
                            }
                        }
                    }
                }
            }
            Log.Debug("LiveTradingResultHandler.SampleRange(): Finished sampling");
        }

        /// <summary>
        /// Set the algorithm of the result handler after its been initialized.
        /// </summary>
        /// <param name="algorithm">Algorithm object matching IAlgorithm interface</param>
        /// <param name="startingPortfolioValue">Algorithm starting capital for statistics calculations</param>
        public void SetAlgorithm(IAlgorithm algorithm, decimal startingPortfolioValue)
        {
            Algorithm = algorithm;
            _portfolioValue = DailyPortfolioValue = StartingPortfolioValue = startingPortfolioValue;

            var types = new List<SecurityType>();
            foreach (var kvp in Algorithm.Securities)
            {
                var security = kvp.Value;

                if (!types.Contains(security.Type)) types.Add(security.Type);
            }
            SecurityType(types);

            // we need to forward Console.Write messages to the algorithm's Debug function
            var debug = new FuncTextWriter(algorithm.Debug);
            var error = new FuncTextWriter(algorithm.Error);
            Console.SetOut(debug);
            Console.SetError(error);

            UpdateAlgorithmStatus();
        }


        /// <summary>
        /// Send a algorithm status update to the user of the algorithms running state.
        /// </summary>
        /// <param name="status">Status enum of the algorithm.</param>
        /// <param name="message">Optional string message describing reason for status change.</param>
        public void SendStatusUpdate(AlgorithmStatus status, string message = "")
        {
            Log.Trace($"LiveTradingResultHandler.SendStatusUpdate(): status: '{status}'. {(string.IsNullOrEmpty(message) ? string.Empty : " " + message)}");
            var packet = new AlgorithmStatusPacket(_job.AlgorithmId, _job.ProjectId, status, message);
            Messages.Enqueue(packet);
        }


        /// <summary>
        /// Set a dynamic runtime statistic to show in the (live) algorithm header
        /// </summary>
        /// <param name="key">Runtime headline statistic name</param>
        /// <param name="value">Runtime headline statistic value</param>
        public void RuntimeStatistic(string key, string value)
        {
            Log.Debug("LiveTradingResultHandler.RuntimeStatistic(): Begin setting statistic");
            lock (RuntimeStatistics)
            {
                if (!RuntimeStatistics.ContainsKey(key))
                {
                    RuntimeStatistics.Add(key, value);
                }
                RuntimeStatistics[key] = value;
            }
            Log.Debug("LiveTradingResultHandler.RuntimeStatistic(): End setting statistic");
        }

        /// <summary>
        /// Send a final analysis result back to the IDE.
        /// </summary>
        protected void SendFinalResult()
        {
            Log.Trace("LiveTradingResultHandler.SendFinalResult(): Starting...");
            try
            {
                LiveResultPacket result;
                // could happen if algorithm failed to init
                if (Algorithm != null)
                {
                    //Convert local dictionary:
                    var charts = new Dictionary<string, Chart>();
                    lock (ChartLock)
                    {
                        foreach (var kvp in Charts)
                        {
                            charts.Add(kvp.Key, kvp.Value.Clone());
                        }
                    }

                    var orders = new Dictionary<int, Order>(TransactionHandler.Orders);
                    var profitLoss = new SortedDictionary<DateTime, decimal>(Algorithm.Transactions.TransactionRecord);
                    var holdings = new Dictionary<string, Holding>();
                    var statisticsResults = GenerateStatisticsResults(charts, profitLoss);
                    var runtime = GetAlgorithmRuntimeStatistics(statisticsResults.Summary);

                    StoreStatusFile(runtime, holdings, charts, profitLoss, statistics: statisticsResults);

                    //Create a packet:
                    result = new LiveResultPacket(_job,
                        new LiveResult(new LiveResultParameters(charts, orders, profitLoss, holdings, Algorithm.Portfolio.CashBook, statisticsResults.Summary, runtime, GetOrderEventsToStore())));
                }
                else
                {
                    result = LiveResultPacket.CreateEmpty(_job);
                }
                result.ProcessingTime = (DateTime.UtcNow - StartTime).TotalSeconds;

                //Store to S3:
                StoreResult(result);
                Log.Trace("LiveTradingResultHandler.SendFinalResult(): Finished storing results. Start sending...");
                //Truncate packet to fit within 32kb:
                result.Results = new LiveResult();

                //Send the truncated packet:
                MessagingHandler.Send(result);
            }
            catch (Exception err)
            {
                Log.Error(err);
            }
            Log.Trace("LiveTradingResultHandler.SendFinalResult(): Ended");
        }

        /// <summary>
        /// Process the log entries and save it to permanent storage
        /// </summary>
        /// <param name="id">Id that will be incorporated into the algorithm log name</param>
        /// <param name="logs">Log list</param>
        /// <returns>Returns the location of the logs</returns>
        public override string SaveLogs(string id, List<LogEntry> logs)
        {
            try
            {
                var logLines = logs.Select(x => x.Message);
                var filename = $"{id}-log.txt";
                var path = GetResultsPath(filename);
                File.AppendAllLines(path, logLines);
                return path;
            }
            catch (Exception err)
            {
                Log.Error(err);
            }
            return "";
        }

        /// <summary>
        /// Save the snapshot of the total results to storage.
        /// </summary>
        /// <param name="packet">Packet to store.</param>
        protected override void StoreResult(Packet packet)
        {
            try
            {
                Log.Debug("LiveTradingResultHandler.StoreResult(): Begin store result sampling");

                // Make sure this is the right type of packet:
                if (packet.Type != PacketType.LiveResult) return;

                // Port to packet format:
                var live = packet as LiveResultPacket;

                if (live != null)
                {
                    if (live.Results.OrderEvents != null)
                    {
                        // we store order events separately
                        StoreOrderEvents(_currentUtcDate, live.Results.OrderEvents);
                        // lets null the orders events so that they aren't stored again and generate a giant file
                        live.Results.OrderEvents = null;
                    }

                    live.Results.AlphaRuntimeStatistics = AlphaRuntimeStatistics;

                    // we need to down sample
                    var start = DateTime.UtcNow.Date;
                    var stop = start.AddDays(1);

                    // truncate to just today, we don't need more than this for anyone
                    Truncate(live.Results, start, stop);

                    var highResolutionCharts = new Dictionary<string, Chart>(live.Results.Charts);

                    // minute resolution data, save today
                    var minuteSampler = new SeriesSampler(TimeSpan.FromMinutes(1));
                    var minuteCharts = minuteSampler.SampleCharts(live.Results.Charts, start, stop);

                    // swap out our charts with the sampled data
                    live.Results.Charts = minuteCharts;
                    SaveResults(CreateKey("minute"), live.Results);

                    // 10 minute resolution data, save today
                    var tenminuteSampler = new SeriesSampler(TimeSpan.FromMinutes(10));
                    var tenminuteCharts = tenminuteSampler.SampleCharts(live.Results.Charts, start, stop);

                    live.Results.Charts = tenminuteCharts;
                    SaveResults(CreateKey("10minute"), live.Results);

                    // high resolution data, we only want to save an hour
                    live.Results.Charts = highResolutionCharts;
                    start = DateTime.UtcNow.RoundDown(TimeSpan.FromHours(1));
                    stop = DateTime.UtcNow.RoundUp(TimeSpan.FromHours(1));

                    Truncate(live.Results, start, stop);

                    foreach (var name in live.Results.Charts.Keys)
                    {
                        var result = new LiveResult
                        {
                            Orders = new Dictionary<int, Order>(live.Results.Orders),
                            Holdings = new Dictionary<string, Holding>(live.Results.Holdings),
                            Charts = new Dictionary<string, Chart> { { name, live.Results.Charts[name] } }
                        };

                        SaveResults(CreateKey("second_" + CreateSafeChartName(name), "yyyy-MM-dd-HH"), result);
                    }
                }
                else
                {
                    Log.Error("LiveResultHandler.StoreResult(): Result Null.");
                }

                Log.Debug("LiveTradingResultHandler.StoreResult(): End store result sampling");
            }
            catch (Exception err)
            {
                Log.Error(err);
            }
        }

        /// <summary>
        /// New order event for the algorithm
        /// </summary>
        /// <param name="newEvent">New event details</param>
        public override void OrderEvent(OrderEvent newEvent)
        {
            var brokerIds = string.Empty;
            var order = TransactionHandler.GetOrderById(newEvent.OrderId);
            if (order != null && order.BrokerId.Count > 0) brokerIds = string.Join(", ", order.BrokerId);

            //Send the message to frontend as packet:
            Log.Trace("LiveTradingResultHandler.OrderEvent(): " + newEvent + " BrokerId: " + brokerIds, true);
            Messages.Enqueue(new OrderEventPacket(AlgorithmId, newEvent));

            var message = "New Order Event: " + newEvent;
            DebugMessage(message);
        }

        /// <summary>
        /// Terminate the result thread and apply any required exit procedures like sending final results
        /// </summary>
        public override void Exit()
        {
            if (!ExitTriggered)
            {
                _cancellationTokenSource.Cancel();

                if (Algorithm != null)
                {
                    // first process synchronous events so we add any new message or log
                    ProcessSynchronousEvents(true);
                }

                // Set exit flag, update task will send any message before stopping
                ExitTriggered = true;

                lock (LogStore)
                {
                    SaveLogs(AlgorithmId, LogStore);
                    LogStore.Clear();
                }

                StopUpdateRunner();

                SendFinalResult();

                base.Exit();
            }
        }

        /// <summary>
        /// Truncates the chart and order data in the result packet to within the specified time frame
        /// </summary>
        private static void Truncate(LiveResult result, DateTime start, DateTime stop)
        {
            var unixDateStart = Time.DateTimeToUnixTimeStamp(start);
            var unixDateStop = Time.DateTimeToUnixTimeStamp(stop);

            //Log.Trace("LiveTradingResultHandler.Truncate: Start: " + start.ToString("u") + " Stop : " + stop.ToString("u"));
            //Log.Trace("LiveTradingResultHandler.Truncate: Truncate Delta: " + (unixDateStop - unixDateStart) + " Incoming Points: " + result.Charts["Strategy Equity"].Series["Equity"].Values.Count);

            var charts = new Dictionary<string, Chart>();
            foreach (var kvp in result.Charts)
            {
                var chart = kvp.Value;
                var newChart = new Chart(chart.Name);
                charts.Add(kvp.Key, newChart);
                foreach (var series in chart.Series.Values)
                {
                    var newSeries = new Series(series.Name, series.SeriesType, series.Unit, series.Color);
                    newSeries.Values.AddRange(series.Values.Where(chartPoint => chartPoint.x >= unixDateStart && chartPoint.x <= unixDateStop));
                    newChart.AddSeries(newSeries);
                }
            }
            result.Charts = charts;
            result.Orders = result.Orders.Values.Where(x =>
                (x.Time >= start && x.Time <= stop) ||
                (x.LastFillTime != null && x.LastFillTime >= start && x.LastFillTime <= stop) ||
                (x.LastUpdateTime != null && x.LastUpdateTime >= start && x.LastUpdateTime <= stop)
            ).ToDictionary(x => x.Id);

            //Log.Trace("LiveTradingResultHandler.Truncate: Truncate Outgoing: " + result.Charts["Strategy Equity"].Series["Equity"].Values.Count);
        }

        private string CreateKey(string suffix, string dateFormat = "yyyy-MM-dd")
        {
            return $"{AlgorithmId}-{DateTime.UtcNow.ToStringInvariant(dateFormat)}_{suffix}.json";
        }

        /// <summary>
        /// Escape the chartname so that it can be saved to a file system
        /// </summary>
        /// <param name="chartName">The name of a chart</param>
        /// <returns>The name of the chart will all escape all characters except RFC 2396 unreserved characters</returns>
        protected virtual string CreateSafeChartName(string chartName)
        {
            return Uri.EscapeDataString(chartName);
        }

        /// <summary>
        /// Process the synchronous result events, sampling and message reading.
        /// This method is triggered from the algorithm manager thread.
        /// </summary>
        /// <remarks>Prime candidate for putting into a base class. Is identical across all result handlers.</remarks>
        public void ProcessSynchronousEvents(bool forceProcess = false)
        {
            var time = DateTime.UtcNow;

            if (time > _nextSample || forceProcess)
            {
                Log.Debug("LiveTradingResultHandler.ProcessSynchronousEvents(): Enter");

                //Set next sample time: 4000 samples per backtest
                _nextSample = time.Add(ResamplePeriod);

                //Sample the portfolio value over time for chart.
                SampleEquity(time, Math.Round(GetPortfolioValue(), 4));

                //Also add the user samples / plots to the result handler tracking:
                SampleRange(Algorithm.GetChartUpdates(true));
            }

            ProcessAlgorithmLogs(messageQueueLimit: 500);

            //Set the running statistics:
            foreach (var pair in Algorithm.RuntimeStatistics)
            {
                RuntimeStatistic(pair.Key, pair.Value);
            }

            //Send all the notification messages but timeout within a second, or if this is a force process, wait till its done.
            var start = DateTime.UtcNow;
            while (Algorithm.Notify.Messages.Count > 0 && (DateTime.UtcNow < start.AddSeconds(1) || forceProcess))
            {
                Notification message;
                if (Algorithm.Notify.Messages.TryDequeue(out message))
                {
                    //Process the notification messages:
                    Log.Trace("LiveTradingResultHandler.ProcessSynchronousEvents(): Processing Notification...");
                    try
                    {
                        MessagingHandler.SendNotification(message);
                    }
                    catch (Exception err)
                    {
                        Log.Error(err, "Sending notification: " + message.GetType().FullName);
                    }
                }
            }

            Log.Debug("LiveTradingResultHandler.ProcessSynchronousEvents(): Exit");
        }

        /// <summary>
        /// Event fired each time that we add/remove securities from the data feed.
        /// On Security change we re determine when should we sample charts, if the user added Crypto, Forex or an extended market hours subscription
        /// we will always sample charts. Else, we will keep the exchange per market to query later on demand
        /// </summary>
        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            if (_sampleChartAlways)
            {
                return;
            }
            foreach (var securityChange in changes.AddedSecurities)
            {
                var symbol = securityChange.Symbol;
                if (symbol.SecurityType == QuantConnect.SecurityType.Base)
                {
                    // ignore custom data
                    continue;
                }

                // if the user added Crypto, Forex or an extended market hours subscription just sample always, one way trip.
                _sampleChartAlways = symbol.SecurityType == QuantConnect.SecurityType.Crypto
                                     || symbol.SecurityType == QuantConnect.SecurityType.Forex
                                     || Algorithm.SubscriptionManager.SubscriptionDataConfigService.GetSubscriptionDataConfigs(symbol).Any(config => config.ExtendedMarketHours);

                if (!_exchangeHours.ContainsKey(securityChange.Symbol.ID.Market))
                {
                    // per market we keep track of the exchange hours
                    _exchangeHours[securityChange.Symbol.ID.Market] = securityChange.Exchange.Hours;
                }
            }
        }

        /// <summary>
        /// Samples portfolio equity, benchmark, and daily performance
        /// </summary>
        /// <param name="time">Current UTC time in the AlgorithmManager loop</param>
        /// <param name="force">Force sampling of equity, benchmark, and performance to be </param>
        public override void Sample(DateTime time, bool force = false)
        {
            UpdatePortfolioValue(time, force);
            UpdateBenchmarkValue(time, force);
            base.Sample(time, force);
        }

        /// <summary>
        /// Gets the current portfolio value
        /// </summary>
        /// <remarks>Useful so that live trading implementation can freeze the returned value if there is no user exchange open
        /// so we ignore extended market hours updates</remarks>
        protected override decimal GetPortfolioValue()
        {
            return _portfolioValue;
        }

        /// <summary>
        /// Gets the current benchmark value
        /// </summary>
        /// <remarks>Useful so that live trading implementation can freeze the returned value if there is no user exchange open
        /// so we ignore extended market hours updates</remarks>
        protected override decimal GetBenchmarkValue()
        {
            return _benchmarkValue;
        }

        /// <summary>
        /// True if user exchange are open and we should update portfolio and benchmark value
        /// </summary>
        /// <remarks>Useful so that live trading implementation can freeze the returned value if there is no user exchange open
        /// so we ignore extended market hours updates</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool UserExchangeIsOpen(DateTime utcDateTime)
        {
            if (_sampleChartAlways || _exchangeHours.Count == 0)
            {
                return true;
            }

            if (_lastChartSampleLogicCheck.Day == utcDateTime.Day
                && _lastChartSampleLogicCheck.Hour == utcDateTime.Hour
                && _lastChartSampleLogicCheck.Minute == utcDateTime.Minute)
            {
                // we cache the value for a minute
                return _userExchangeIsOpen;
            }
            _lastChartSampleLogicCheck = utcDateTime;

            foreach (var exchangeHour in _exchangeHours.Values)
            {
                if (exchangeHour.IsOpen(utcDateTime.ConvertFromUtc(exchangeHour.TimeZone), false))
                {
                    // one of the users exchanges is open
                    _userExchangeIsOpen = true;
                    return true;
                }
            }

            // no user exchange is open
            _userExchangeIsOpen = false;
            return false;
        }

        private static void DictionarySafeAdd<T>(Dictionary<string, T> dictionary, string key, T value, string dictionaryName)
        {
            if (dictionary.ContainsKey(key))
            {
                // TODO: GH issue 3609
                Log.Debug($"LiveTradingResultHandler.DictionarySafeAdd(): dictionary {dictionaryName} already contains key {key}");
            }
            else
            {
                dictionary.Add(key, value);
            }
        }

        /// <summary>
        /// Will launch a task which will call the API and update the algorithm status every minute
        /// </summary>
        private void UpdateAlgorithmStatus()
        {
            if (!ExitTriggered
                && !_cancellationTokenSource.IsCancellationRequested) // just in case
            {
                // wait until after we're warmed up to start sending running status each minute
                if (!Algorithm.IsWarmingUp)
                {
                    _api.SetAlgorithmStatus(_job.AlgorithmId, AlgorithmStatus.Running);
                }
                Task.Delay(TimeSpan.FromMinutes(1), _cancellationTokenSource.Token).ContinueWith(_ => UpdateAlgorithmStatus());
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateBenchmarkValue(DateTime time, bool force)
        {
            if (force || UserExchangeIsOpen(time))
            {
                _benchmarkValue = base.GetBenchmarkValue();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdatePortfolioValue(DateTime time, bool force)
        {
            if (force || UserExchangeIsOpen(time))
            {
                _portfolioValue = base.GetPortfolioValue();
            }
        }
    }
}
