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
using QuantConnect.Brokerages;
using QuantConnect.Configuration;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Notifications;
using QuantConnect.Orders;
using QuantConnect.Packets;
using QuantConnect.Securities;
using QuantConnect.Securities.Positions;
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
        private DateTime _nextInsightStoreUpdate;
        private DateTime _currentUtcDate;

        private readonly TimeSpan _storeInsightPeriod;

        private DateTime _nextPortfolioMarginUpdate;
        private DateTime _previousPortfolioMarginUpdate;
        private readonly TimeSpan _samplePortfolioPeriod;
        private readonly Chart _intradayPortfolioState = new(PortfolioMarginKey);

        /// <summary>
        /// The earliest time of next dump to the status file
        /// </summary>
        private DateTime _nextStatusUpdate;

        //Log Message Store:
        private DateTime _nextSample;
        private IApi _api;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly int _streamedChartLimit;
        private readonly int _streamedChartGroupSize;

        private bool _sampleChartAlways;
        private bool _userExchangeIsOpen;
        private ReferenceWrapper<decimal> _portfolioValue;
        private ReferenceWrapper<decimal> _benchmarkValue;
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
            _samplePortfolioPeriod = _storeInsightPeriod = TimeSpan.FromMinutes(10);
            _streamedChartLimit = Config.GetInt("streamed-chart-limit", 12);
            _streamedChartGroupSize = Config.GetInt("streamed-chart-group-size", 3);

            _portfolioValue = new ReferenceWrapper<decimal>(0);
            _benchmarkValue = new ReferenceWrapper<decimal>(0);
        }

        /// <summary>
        /// Initialize the result handler with this result packet.
        /// </summary>
        /// <param name="parameters">DTO parameters class to initialize a result handler</param>
        public override void Initialize(ResultHandlerInitializeParameters parameters)
        {
            _api = parameters.Api;
            _job = (LiveNodePacket)parameters.Job;
            if (_job == null) throw new Exception("LiveResultHandler.Constructor(): Submitted Job type invalid.");
            var utcNow = DateTime.UtcNow;
            _currentUtcDate = utcNow.Date;

            _nextPortfolioMarginUpdate = utcNow.RoundDown(_samplePortfolioPeriod).Add(_samplePortfolioPeriod);
            base.Initialize(parameters);
        }

        /// <summary>
        /// Live trading result handler thread.
        /// </summary>
        protected override void Run()
        {
            // give the algorithm time to initialize, else we will log an error right away
            ExitEvent.WaitOne(3000);

            // -> 1. Run Primary Sender Loop: Continually process messages from queue as soon as they arrive.
            while (!(ExitTriggered && Messages.IsEmpty))
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

                    if (Messages.IsEmpty)
                    {
                        // prevent thread lock/tight loop when there's no work to be done
                        ExitEvent.WaitOne(Time.GetSecondUnevenWait(1000));
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
                Log.Debug("LiveTradingResultHandler.Update(): Algorithm not yet initialized.");
                ExitEvent.WaitOne(1000);
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

                            if (chartUpdates.Name == PortfolioMarginKey)
                            {
                                PortfolioMarginChart.RemoveSinglePointSeries(chartUpdates);
                            }
                        }
                    }
                    Log.Debug("LiveTradingResultHandler.Update(): End build delta charts");

                    //Profit loss changes, get the banner statistics, summary information on the performance for the headers.
                    var serverStatistics = GetServerStatistics(utcNow);
                    var holdings = GetHoldings(Algorithm.Securities.Values, Algorithm.SubscriptionManager.SubscriptionDataConfigService);

                    //Add the algorithm statistics first.
                    Log.Debug("LiveTradingResultHandler.Update(): Build run time stats");

                    var summary = GenerateStatisticsResults(performanceCharts).Summary;
                    var runtimeStatistics = GetAlgorithmRuntimeStatistics(summary);
                    Log.Debug("LiveTradingResultHandler.Update(): End build run time stats");


                    // since we're sending multiple packets, let's do it async and forget about it
                    // chart data can get big so let's break them up into groups
                    var splitPackets = SplitPackets(deltaCharts, deltaOrders, holdings, Algorithm.Portfolio.CashBook, runtimeStatistics, serverStatistics, deltaOrderEvents);

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

                        var deltaStatistics = new Dictionary<string, string>();
                        var orders = new Dictionary<int, Order>(TransactionHandler.Orders);
                        var complete = new LiveResultPacket(_job, new LiveResult(new LiveResultParameters(chartComplete, orders, Algorithm.Transactions.TransactionRecord, holdings, Algorithm.Portfolio.CashBook, deltaStatistics, runtimeStatistics, orderEvents, serverStatistics, state: GetAlgorithmState())));
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
                                Algorithm.Portfolio.TotalNetProfit,
                                Algorithm.Portfolio.TotalHoldingsValue,
                                Algorithm.Portfolio.TotalPortfolioValue,
                                GetNetReturn(),
                                Algorithm.Portfolio.TotalSaleVolume,
                                TotalTradesCount(), 0);
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
                            // only store holdings we are invested in
                            holdings.Where(pair => pair.Value.Quantity != 0).ToDictionary(pair => pair.Key, pair => pair.Value),
                            chartComplete,
                            GetAlgorithmState(),
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
                        var timeLimitUtc = utcNow.AddDays(-2);
                        lock (ChartLock)
                        {
                            foreach (var chart in Charts)
                            {
                                foreach (var series in chart.Value.Series)
                                {
                                    // trim data that's older than 2 days
                                    series.Value.Values =
                                        (from v in series.Value.Values
                                         where v.Time > timeLimitUtc
                                         select v).ToList();
                                }
                            }
                        }
                        _nextChartTrimming = DateTime.UtcNow.AddMinutes(10);
                        Log.Debug("LiveTradingResultHandler.Update(): Finished trimming charts");
                    }

                    if (utcNow > _nextInsightStoreUpdate)
                    {
                        StoreInsights();

                        _nextInsightStoreUpdate = DateTime.UtcNow.Add(_storeInsightPeriod);
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
        /// Assigns the next earliest status update time
        /// </summary>
        protected virtual void SetNextStatusUpdate()
        {
            // Update the status json file every X
            _nextStatusUpdate = DateTime.UtcNow.AddMinutes(10);
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

            var data = JsonConvert.SerializeObject(orderEvents, Formatting.None, SerializerSettings);

            File.WriteAllText(path, data);
        }

        /// <summary>
        /// Gets the order events generated in '_currentUtcDate'
        /// </summary>
        private List<OrderEvent> GetOrderEventsToStore()
        {
            return TransactionHandler.OrderEvents.Where(orderEvent => orderEvent.UtcTime >= _currentUtcDate).ToList();
        }

        /// <summary>
        /// Will store the complete status of the algorithm in a single json file
        /// </summary>
        /// <remarks>Will sample charts every 12 hours, 2 data points per day at maximum,
        /// to reduce file size</remarks>
        private void StoreStatusFile(SortedDictionary<string, string> runtimeStatistics,
            Dictionary<string, Holding> holdings,
            Dictionary<string, Chart> chartComplete,
            Dictionary<string, string> algorithmState,
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
                chartComplete = dailySampler.SampleCharts(chartComplete, Time.Start, Time.EndOfTime);

                if (chartComplete.TryGetValue(PortfolioMarginKey, out var marginChart))
                {
                    PortfolioMarginChart.RemoveSinglePointSeries(marginChart);
                }

                var result = new LiveResult(new LiveResultParameters(chartComplete,
                    new Dictionary<int, Order>(TransactionHandler.Orders),
                    Algorithm?.Transactions.TransactionRecord ?? new(),
                    holdings,
                    Algorithm?.Portfolio.CashBook ?? new(),
                    statistics: statistics.Summary,
                    runtimeStatistics: runtimeStatistics,
                    orderEvents: null, // we stored order events separately
                    serverStatistics: serverStatistics,
                    state: algorithmState));

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
            SortedDictionary<string, string> runtimeStatistics,
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
                new LiveResultPacket(_job, new LiveResult { Holdings = holdings, CashBook = cashbook}),
                new LiveResultPacket(_job, new LiveResult
                {
                    RuntimeStatistics = runtimeStatistics,
                    ServerStatistics = serverStatistics
                })
            };

            var result = packets.Concat(chartPackets);

            // only send order and order event packet if there is actually any update
            if (deltaOrders.Count > 0 || deltaOrderEvents.Count > 0)
            {
                result = result.Concat(new[] { new LiveResultPacket(_job, new LiveResult { Orders = deltaOrders, OrderEvents = deltaOrderEvents }) });
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
            base.AddToLogStore(DateTime.Now.ToStringInvariant(DateFormat.UI) + " " + message);
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
        public virtual void RuntimeError(string message, string stacktrace = "")
        {
            Messages.Enqueue(new RuntimeErrorPacket(_job.UserId, AlgorithmId, message, stacktrace));
            AddToLogStore(message + (!string.IsNullOrEmpty(stacktrace) ? ": StackTrace: " + stacktrace : string.Empty));
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
        /// <param name="seriesName">Series name for the chart.</param>
        /// <param name="seriesIndex">Series chart index - which chart should this series belong</param>
        /// <param name="seriesType">Series type for the chart.</param>
        /// <param name="value">Value for the chart sample.</param>
        /// <param name="unit">Unit for the chart axis</param>
        /// <remarks>Sample can be used to create new charts or sample equity - daily performance.</remarks>
        protected override void Sample(string chartName, string seriesName, int seriesIndex, SeriesType seriesType, ISeriesPoint value,
            string unit = "$")
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
                if (!Charts.TryGetValue(chartName, out var chart))
                {
                    Charts.AddOrUpdate(chartName, new Chart(chartName));
                    chart = Charts[chartName];
                }

                //Add the sample to our chart:
                if (!chart.Series.TryGetValue(seriesName, out var series))
                {
                    series = BaseSeries.Create(seriesType, seriesName, seriesIndex, unit);
                    chart.Series.Add(seriesName, series);
                }

                //Add our value:
                series.Values.Add(value);
            }
            Log.Debug("LiveTradingResultHandler.Sample(): Done sampling " + chartName + "." + seriesName);
        }

        /// <summary>
        /// Add a range of samples from the users algorithms to the end of our current list.
        /// </summary>
        /// <param name="updates">Chart updates since the last request.</param>
        /// <seealso cref="Sample(string,string,int,SeriesType,ISeriesPoint,string)"/>
        protected void SampleRange(IEnumerable<Chart> updates)
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

                    //Add these samples to this chart.
                    foreach (BaseSeries series in update.Series.Values)
                    {
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
        public virtual void SetAlgorithm(IAlgorithm algorithm, decimal startingPortfolioValue)
        {
            Algorithm = algorithm;
            Algorithm.SetStatisticsService(this);
            DailyPortfolioValue = StartingPortfolioValue = startingPortfolioValue;
            _portfolioValue = new ReferenceWrapper<decimal>(startingPortfolioValue);
            CumulativeMaxPortfolioValue = StartingPortfolioValue;
            AlgorithmCurrencySymbol = Currencies.GetCurrencySymbol(Algorithm.AccountCurrency);

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

            // Wire algorithm name and tags updates
            algorithm.NameUpdated += (sender, name) => AlgorithmNameUpdated(name);
            algorithm.TagsUpdated += (sender, tags) => AlgorithmTagsUpdated(tags);
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
                var endTime = DateTime.UtcNow;
                var endState = GetAlgorithmState(endTime);
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
                    var holdings = GetHoldings(Algorithm.Securities.Values, Algorithm.SubscriptionManager.SubscriptionDataConfigService, onlyInvested: true);
                    var statisticsResults = GenerateStatisticsResults(charts, profitLoss);
                    var runtime = GetAlgorithmRuntimeStatistics(statisticsResults.Summary);

                    StoreStatusFile(runtime, holdings, charts, endState, profitLoss, statistics: statisticsResults);

                    //Create a packet:
                    result = new LiveResultPacket(_job,
                        new LiveResult(new LiveResultParameters(charts, orders, profitLoss, new Dictionary<string, Holding>(),
                            Algorithm.Portfolio.CashBook, statisticsResults.Summary, runtime, GetOrderEventsToStore(),
                            algorithmConfiguration: AlgorithmConfiguration.Create(Algorithm, null), state: endState)));
                }
                else
                {
                    StoreStatusFile(new(), new(), new(), endState, new());

                    result = LiveResultPacket.CreateEmpty(_job);
                    result.Results.State = endState;
                }

                StoreInsights();

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
                    minuteCharts.Remove(PortfolioMarginKey);
                    live.Results.Charts = minuteCharts;
                    SaveResults(CreateKey("minute"), live.Results);

                    // 10 minute resolution data, save today
                    var tenminuteSampler = new SeriesSampler(TimeSpan.FromMinutes(10));
                    var tenminuteCharts = tenminuteSampler.SampleCharts(live.Results.Charts, start, stop);
                    lock (_intradayPortfolioState)
                    {
                        var clone = _intradayPortfolioState.Clone();
                        PortfolioMarginChart.RemoveSinglePointSeries(clone);
                        tenminuteCharts[PortfolioMarginKey] = clone;
                    }

                    live.Results.Charts = tenminuteCharts;
                    SaveResults(CreateKey("10minute"), live.Results);

                    // high resolution data, we only want to save an hour
                    highResolutionCharts.Remove(PortfolioMarginKey);
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
                ExitEvent.Set();

                lock (LogStore)
                {
                    SaveLogs(AlgorithmId, LogStore);
                    LogStore.Clear();
                }

                StopUpdateRunner();

                SendFinalResult();

                base.Exit();

                _cancellationTokenSource.DisposeSafely();
            }
        }

        /// <summary>
        /// Truncates the chart and order data in the result packet to within the specified time frame
        /// </summary>
        private static void Truncate(LiveResult result, DateTime start, DateTime stop)
        {
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
                    var newSeries = series.Clone(empty: true);
                    newSeries.Values.AddRange(series.Values.Where(chartPoint => chartPoint.Time >= start && chartPoint.Time <= stop));
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
        public virtual void ProcessSynchronousEvents(bool forceProcess = false)
        {
            var time = DateTime.UtcNow;

            // Check to see if we should update stored portfolio values
            UpdatePortfolioValue(time, forceProcess);

            // Update the equity bar
            UpdateAlgorithmEquity();

            if (time > _nextPortfolioMarginUpdate || forceProcess)
            {
                _nextPortfolioMarginUpdate = time.RoundDown(_samplePortfolioPeriod).Add(_samplePortfolioPeriod);

                var newState = PortfolioState.Create(Algorithm.Portfolio, time, GetPortfolioValue());
                lock (_intradayPortfolioState)
                {
                    if (_previousPortfolioMarginUpdate.Date != time.Date)
                    {
                        // we crossed into a new day
                        _previousPortfolioMarginUpdate = time.Date;
                        _intradayPortfolioState.Series.Clear();
                    }

                    if (newState != null)
                    {
                        PortfolioMarginChart.AddSample(_intradayPortfolioState, newState, MapFileProvider, time);
                    }
                }
            }

            if (time > _nextSample || forceProcess)
            {
                Log.Debug("LiveTradingResultHandler.ProcessSynchronousEvents(): Enter");

                //Set next sample time: 4000 samples per backtest
                _nextSample = time.Add(ResamplePeriod);

                // Check to see if we should update stored bench values
                UpdateBenchmarkValue(time, forceProcess);

                //Sample the portfolio value over time for chart.
                SampleEquity(time);

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
            var timeout = DateTime.UtcNow.AddSeconds(1);
            while (!Algorithm.Notify.Messages.IsEmpty && (DateTime.UtcNow < timeout || forceProcess))
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
                        Algorithm.Debug(err.Message);
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

                // if the user added Crypto, Forex, Daily or an extended market hours subscription just sample always, one way trip.
                _sampleChartAlways = symbol.SecurityType == QuantConnect.SecurityType.Crypto
                                     || symbol.SecurityType == QuantConnect.SecurityType.Forex
                                     || Algorithm.SubscriptionManager.SubscriptionDataConfigService.GetSubscriptionDataConfigs(symbol)
                                         .Any(config => config.ExtendedMarketHours || config.Resolution == Resolution.Daily);
                if (_sampleChartAlways)
                {
                    // we set it once to true
                    return;
                }

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
        public void Sample(DateTime time)
        {
            // Force an update for our values before doing our daily sample
            UpdatePortfolioValue(time);
            UpdateBenchmarkValue(time);
            base.Sample(time);
        }

        /// <summary>
        /// Gets the current portfolio value
        /// </summary>
        /// <remarks>Useful so that live trading implementation can freeze the returned value if there is no user exchange open
        /// so we ignore extended market hours updates</remarks>
        protected override decimal GetPortfolioValue()
        {
            return _portfolioValue.Value;
        }

        /// <summary>
        /// Gets the current benchmark value
        /// </summary>
        /// <remarks>Useful so that live trading implementation can freeze the returned value if there is no user exchange open
        /// so we ignore extended market hours updates</remarks>
        /// <param name="time">Time to resolve benchmark value at</param>
        protected override decimal GetBenchmarkValue(DateTime time)
        {
            return _benchmarkValue.Value;
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
            if (!dictionary.TryAdd(key, value))
            {
                Log.Error($"LiveTradingResultHandler.DictionarySafeAdd(): dictionary {dictionaryName} already contains key {key}");
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
        private void UpdateBenchmarkValue(DateTime time, bool force = false)
        {
            if (force || UserExchangeIsOpen(time))
            {
                _benchmarkValue = new ReferenceWrapper<decimal>(base.GetBenchmarkValue(time));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdatePortfolioValue(DateTime time, bool force = false)
        {
            if (force || UserExchangeIsOpen(time))
            {
                _portfolioValue = new ReferenceWrapper<decimal>(base.GetPortfolioValue());
            }
        }

        /// <summary>
        /// Helper method to fetch the algorithm holdings
        /// </summary>
        public static Dictionary<string, Holding> GetHoldings(IEnumerable<Security> securities, ISubscriptionDataConfigService subscriptionDataConfigService, bool onlyInvested = false)
        {
            var holdings = new Dictionary<string, Holding>();

            foreach (var security in securities
                // If we are invested we send it always, if not, we send non internal, non canonical and tradable securities. When securities are removed they are marked as non tradable.
                .Where(s => s.Invested || !onlyInvested && (!s.IsInternalFeed() && s.IsTradable && !s.Symbol.IsCanonical()
                    // Continuous futures are different because it's mapped securities are internal and the continuous contract is canonical and non tradable but we want to send them anyways
                    // but we don't want to sent non canonical, non tradable futures, these would be the future chain assets, or continuous mapped contracts that have been removed
                    || s.Symbol.SecurityType == QuantConnect.SecurityType.Future && (s.IsTradable || s.Symbol.IsCanonical() && subscriptionDataConfigService.GetSubscriptionDataConfigs(s.Symbol).Any())))
                .OrderBy(x => x.Symbol.Value))
            {
                DictionarySafeAdd(holdings, security.Symbol.ID.ToString(), new Holding(security), "holdings");
            }

            return holdings;
        }

        /// <summary>
        /// Calculates and gets the current statistics for the algorithm
        /// </summary>
        /// <returns>The current statistics</returns>
        public StatisticsResults StatisticsResults()
        {
            return GenerateStatisticsResults();
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

        /// <summary>
        /// Handles updates to the algorithm's name
        /// </summary>
        /// <param name="name">The new name</param>
        public virtual void AlgorithmNameUpdated(string name)
        {
            Messages.Enqueue(new AlgorithmNameUpdatePacket(AlgorithmId, name));
        }

        /// <summary>
        /// Handles updates to the algorithm's tags
        /// </summary>
        /// <param name="tags">The new tags</param>
        public virtual void AlgorithmTagsUpdated(HashSet<string> tags)
        {
            Messages.Enqueue(new AlgorithmTagsUpdatePacket(AlgorithmId, tags));
        }
    }
}
