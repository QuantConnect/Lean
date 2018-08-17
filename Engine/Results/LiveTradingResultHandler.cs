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
using System.Diagnostics;
using System.Linq;
using System.Threading;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.Setup;
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
        private readonly DateTime _launchTimeUtc = DateTime.UtcNow;

        // Required properties for the cloud app.
        private string _compileId;
        private string _deployId;
        private LiveNodePacket _job;
        private readonly ConcurrentQueue<OrderEvent> _orderEvents = new ConcurrentQueue<OrderEvent>();
        private IAlgorithm _algorithm;
        private volatile bool _exitTriggered;
        private readonly DateTime _startTime = DateTime.Now;
        private readonly Dictionary<string, string> _runtimeStatistics = new Dictionary<string, string>();

        //Update loop:
        private DateTime _nextUpdate;
        private DateTime _nextChartsUpdate;
        private DateTime _nextRunningStatus;
        private DateTime _nextLogStoreUpdate;
        private DateTime _nextStatisticsUpdate;
        private int _lastOrderId;
        private readonly object _chartLock = new object();
        private readonly object _runtimeLock = new object();
        private string _subscription = "Strategy Equity";

        //Log Message Store:
        private readonly object _logStoreLock = new object();
        private List<LogEntry> _logStore = new List<LogEntry>();
        private DateTime _nextSample;
        private IMessagingHandler _messagingHandler;
        private IApi _api;
        private IDataFeed _dataFeed;
        private ISetupHandler _setupHandler;
        private ITransactionHandler _transactionHandler;

        /// <summary>
        /// Live packet messaging queue. Queue the messages here and send when the result queue is ready.
        /// </summary>
        public ConcurrentQueue<Packet> Messages { get; set; } = new ConcurrentQueue<Packet>();

        /// <summary>
        /// Storage for the price and equity charts of the live results.
        /// </summary>
        /// <remarks>
        ///     Potential memory leak when the algorithm has been running for a long time. Infinitely storing the results isn't wise.
        ///     The results should be stored to disk daily, and then the caches reset.
        /// </remarks>
        public ConcurrentDictionary<string, Chart> Charts { get; set; } = new ConcurrentDictionary<string, Chart>();

        /// <summary>
        /// Boolean flag indicating the thread is still active.
        /// </summary>
        public bool IsActive { get; private set; } = true;

        /// <summary>
        /// Equity resampling period for the charting.
        /// </summary>
        /// <remarks>Live trading can resample at much higher frequencies (every 1-2 seconds)</remarks>
        public TimeSpan ResamplePeriod { get; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Notification periods set how frequently we push updates to the browser.
        /// </summary>
        /// <remarks>Live trading resamples - sends updates at high frequencies(every 1-2 seconds)</remarks>
        public TimeSpan NotificationPeriod { get; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Initialize the result handler with this result packet.
        /// </summary>
        /// <param name="job">Algorithm job packet for this result handler</param>
        /// <param name="messagingHandler"></param>
        /// <param name="api"></param>
        /// <param name="dataFeed"></param>
        /// <param name="setupHandler"></param>
        /// <param name="transactionHandler"></param>
        public virtual void Initialize(AlgorithmNodePacket job, IMessagingHandler messagingHandler, IApi api, IDataFeed dataFeed, ISetupHandler setupHandler, ITransactionHandler transactionHandler)
        {
            _api = api;
            _dataFeed = dataFeed;
            _messagingHandler = messagingHandler;
            _setupHandler = setupHandler;
            _transactionHandler = transactionHandler;
            _job = (LiveNodePacket)job;
            if (_job == null) throw new Exception("LiveResultHandler.Constructor(): Submitted Job type invalid.");
            _deployId = _job.DeployId;
            _compileId = _job.CompileId;
        }

        /// <summary>
        /// Live trading result handler thread.
        /// </summary>
        public void Run()
        {
            // -> 1. Run Primary Sender Loop: Continually process messages from queue as soon as they arrive.
            while (!(_exitTriggered && Messages.Count == 0))
            {
                try
                {
                    //1. Process Simple Messages in Queue
                    Packet packet;
                    if (Messages.TryDequeue(out packet))
                    {
                        _messagingHandler.Send(packet);
                    }

                    //2. Update the packet scanner:
                    Update();

                    if (Messages.Count == 0)
                    {
                        // prevent thread lock/tight loop when there's no work to be done
                        Thread.Sleep(10);
                    }
                }
                catch (Exception err)
                {
                    Log.Error(err);
                }
            } // While !End.

            Log.Trace("LiveTradingResultHandler.Run(): Ending Thread...");
            IsActive = false;
        } // End Run();


        /// <summary>
        /// Every so often send an update to the browser with the current state of the algorithm.
        /// </summary>
        public void Update()
        {
            //Error checks if the algorithm & threads have not loaded yet, or are closing down.
            if (_algorithm?.Transactions == null || _transactionHandler.Orders == null || !_algorithm.GetLocked())
            {
                Log.Error("LiveTradingResultHandler.Update(): Algorithm not yet initialized.");
                return;
            }

            try
            {
                if (DateTime.Now > _nextUpdate || _exitTriggered)
                {
                    //Extract the orders created since last update
                    OrderEvent orderEvent;
                    var deltaOrders = new Dictionary<int, Order>();

                    var stopwatch = Stopwatch.StartNew();
                    while (_orderEvents.TryDequeue(out orderEvent) && stopwatch.ElapsedMilliseconds < 15)
                    {
                        var order = _algorithm.Transactions.GetOrderById(orderEvent.OrderId);
                        deltaOrders[orderEvent.OrderId] = order.Clone();
                    }

                    //For charting convert to UTC
                    foreach (var order in deltaOrders)
                    {
                        order.Value.Price = order.Value.Price.SmartRounding();
                        order.Value.Time = order.Value.Time.ToUniversalTime();
                    }

                    //Reset loop variables:
                    _lastOrderId = (from order in deltaOrders.Values select order.Id).DefaultIfEmpty(_lastOrderId).Max();

                    //Limit length of orders we pass back dynamically to avoid flooding.
                    //if (deltaOrders.Count > 50) deltaOrders.Clear();

                    //Create and send back the changes in chart since the algorithm started.
                    var deltaCharts = new Dictionary<string, Chart>();
                    Log.Debug("LiveTradingResultHandler.Update(): Build delta charts");
                    lock (_chartLock)
                    {
                        //Get the updates since the last chart
                        foreach (var chart in Charts)
                        {
                            // remove directory pathing characters from chart names
                            var safeName = chart.Value.Name.Replace('/', '-');
                            deltaCharts.Add(safeName, chart.Value.GetUpdates());
                        }
                    }
                    Log.Debug("LiveTradingResultHandler.Update(): End build delta charts");

                    //Profit loss changes, get the banner statistics, summary information on the performance for the headers.
                    var holdings = new Dictionary<string, Holding>();
                    var deltaStatistics = new Dictionary<string, string>();
                    var runtimeStatistics = new Dictionary<string, string>();
                    var serverStatistics = OS.GetServerStatistics();
                    var upTime = DateTime.UtcNow - _launchTimeUtc;
                    serverStatistics["Up Time"] = $"{upTime.Days}d {upTime:hh\\:mm\\:ss}";
                    serverStatistics["Total RAM (MB)"] = _job.Controls.RamAllocation.ToString();

                    // Only send holdings updates when we have changes in orders, except for first time, then we want to send all
                    foreach (var kvp in _algorithm.Securities.OrderBy(x => x.Key.Value))
                    {
                        var security = kvp.Value;

                        if (!security.IsInternalFeed() && !security.Symbol.IsCanonical())
                        {
                            holdings.Add(security.Symbol.Value, new Holding(security));
                        }
                    }

                    //Add the algorithm statistics first.
                    Log.Debug("LiveTradingResultHandler.Update(): Build run time stats");
                    lock (_runtimeLock)
                    {
                        foreach (var pair in _runtimeStatistics)
                        {
                            runtimeStatistics.Add(pair.Key, pair.Value);
                        }
                    }
                    Log.Debug("LiveTradingResultHandler.Update(): End build run time stats");

                    //Some users have $0 in their brokerage account / starting cash of $0. Prevent divide by zero errors
                    var netReturn = _setupHandler.StartingPortfolioValue > 0 ?
                                    (_algorithm.Portfolio.TotalPortfolioValue - _setupHandler.StartingPortfolioValue) / _setupHandler.StartingPortfolioValue
                                    : 0;

                    //Add other fixed parameters.
                    runtimeStatistics.Add("Unrealized:", "$" + _algorithm.Portfolio.TotalUnrealizedProfit.ToString("N2"));
                    runtimeStatistics.Add("Fees:", "-$" + _algorithm.Portfolio.TotalFees.ToString("N2"));
                    runtimeStatistics.Add("Net Profit:", "$" + (_algorithm.Portfolio.TotalProfit - _algorithm.Portfolio.TotalFees).ToString("N2"));
                    runtimeStatistics.Add("Return:", netReturn.ToString("P"));
                    runtimeStatistics.Add("Equity:", "$" + _algorithm.Portfolio.TotalPortfolioValue.ToString("N2"));
                    runtimeStatistics.Add("Holdings:", "$" + _algorithm.Portfolio.TotalHoldingsValue.ToString("N2"));
                    runtimeStatistics.Add("Volume:", "$" + _algorithm.Portfolio.TotalSaleVolume.ToString("N2"));

                    // since we're sending multiple packets, let's do it async and forget about it
                    // chart data can get big so let's break them up into groups
                    var splitPackets = SplitPackets(deltaCharts, deltaOrders, holdings, _algorithm.Portfolio.CashBook, deltaStatistics, runtimeStatistics, serverStatistics);

                    foreach (var liveResultPacket in splitPackets)
                    {
                        _messagingHandler.Send(liveResultPacket);
                    }

                    //Send full packet to storage.
                    if (DateTime.Now > _nextChartsUpdate || _exitTriggered)
                    {
                        Log.Debug("LiveTradingResultHandler.Update(): Pre-store result");
                        _nextChartsUpdate = DateTime.Now.AddMinutes(1);
                        var chartComplete = new Dictionary<string, Chart>();
                        lock (_chartLock)
                        {
                            foreach (var chart in Charts)
                            {
                                // remove directory pathing characters from chart names
                                var safeName = chart.Value.Name.Replace('/', '-');
                                chartComplete.Add(safeName, chart.Value.Clone());
                            }
                        }
                        var orders = new Dictionary<int, Order>(_transactionHandler.Orders);
                        var complete = new LiveResultPacket(_job, new LiveResult(_algorithm.IsFrameworkAlgorithm, chartComplete, orders, _algorithm.Transactions.TransactionRecord, holdings, _algorithm.Portfolio.CashBook, deltaStatistics, runtimeStatistics, serverStatistics));
                        StoreResult(complete);
                        Log.Debug("LiveTradingResultHandler.Update(): End-store result");
                    }

                    // Upload the logs every 1-2 minutes; this can be a heavy operation depending on amount of live logging and should probably be done asynchronously.
                    if (DateTime.Now > _nextLogStoreUpdate || _exitTriggered)
                    {
                        List<LogEntry> logs;
                        Log.Debug("LiveTradingResultHandler.Update(): Storing log...");
                        lock (_logStoreLock)
                        {
                            var utc = DateTime.UtcNow;
                            logs = (from log in _logStore
                                    where log.Time >= utc.RoundDown(TimeSpan.FromHours(1))
                                    select log).ToList();
                            //Override the log master to delete the old entries and prevent memory creep.
                            _logStore = logs;
                        }
                        StoreLog(logs);
                        _nextLogStoreUpdate = DateTime.Now.AddMinutes(2);
                        Log.Debug("LiveTradingResultHandler.Update(): Finished storing log");
                    }

                    // Every minute send usage statistics:
                    if (DateTime.Now > _nextStatisticsUpdate || _exitTriggered)
                    {
                        try
                        {
                            _api.SendStatistics(
                                _job.AlgorithmId,
                                _algorithm.Portfolio.TotalUnrealizedProfit,
                                _algorithm.Portfolio.TotalFees,
                                _algorithm.Portfolio.TotalProfit,
                                _algorithm.Portfolio.TotalHoldingsValue,
                                _algorithm.Portfolio.TotalPortfolioValue,
                                netReturn,
                                _algorithm.Portfolio.TotalSaleVolume,
                                _lastOrderId, 0);
                        }
                        catch (Exception err)
                        {
                            Log.Error(err, "Error sending statistics:");
                        }
                        _nextStatisticsUpdate = DateTime.Now.AddMinutes(1);
                    }


                    Log.Debug("LiveTradingResultHandler.Update(): Trimming charts");
                    lock (_chartLock)
                    {
                        foreach (var chart in Charts)
                        {
                            foreach (var series in chart.Value.Series)
                            {
                                // trim data that's older than 2 days
                                series.Value.Values =
                                    (from v in series.Value.Values
                                     where v.x > Time.DateTimeToUnixTimeStamp(DateTime.UtcNow.AddDays(-2))
                                     select v).ToList();
                            }
                        }
                    }
                    Log.Debug("LiveTradingResultHandler.Update(): Finished trimming charts");


                    //Set the new update time after we've finished processing.
                    // The processing can takes time depending on how large the packets are.
                    _nextUpdate = DateTime.Now.AddSeconds(2);

                } // End Update Charts:
            }
            catch (Exception err)
            {
                Log.Error(err, "LiveTradingResultHandler().Update(): ", true);
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
            Dictionary<string, string> serverStatistics)
        {
            // break the charts into groups

            var groupSize = 3;
            var current = new Dictionary<string, Chart>();
            var chartPackets = new List<LiveResultPacket>();

            // First add send charts

            // Loop through all the charts, add them to packets to be sent.
            // Group three charts to a packets, and add in the data to the chart depending on the subscription.

            foreach (var deltaChart in deltaCharts.Values)
            {
                var chart = new Chart(deltaChart.Name);
                current.Add(deltaChart.Name, chart);

                if (deltaChart.Name == _subscription || (_subscription == "*" && deltaChart.Name == "Strategy Equity"))
                {
                    chart.Series = deltaChart.Series;
                }

                // If there is room left in the group. add the subscription
                // to the packet unless it is a wildcard subscription
                if (current.Count >= groupSize && _subscription != "*")
                {
                    // Add the micro packet to transport.
                    chartPackets.Add(new LiveResultPacket(_job, new LiveResult { IsFrameworkAlgorithm = _algorithm.IsFrameworkAlgorithm, Charts = current }));
                    // Reset the carrier variable.
                    current = new Dictionary<string, Chart>();
                }
            }

            // Add whatever is left over here too
            // unless it is a wildcard subscription
            if (current.Count > 0 && _subscription != "*")
            {
                chartPackets.Add(new LiveResultPacket(_job, new LiveResult {IsFrameworkAlgorithm = _algorithm.IsFrameworkAlgorithm, Charts = current}));
            }

            // these are easier to split up, not as big as the chart objects
            var packets = new[]
            {
                new LiveResultPacket(_job, new LiveResult {IsFrameworkAlgorithm = _algorithm.IsFrameworkAlgorithm, Orders = deltaOrders}),
                new LiveResultPacket(_job, new LiveResult {IsFrameworkAlgorithm = _algorithm.IsFrameworkAlgorithm, Holdings = holdings, Cash = cashbook}),
                new LiveResultPacket(_job, new LiveResult
                {
                    IsFrameworkAlgorithm = _algorithm.IsFrameworkAlgorithm,
                    Statistics = deltaStatistics,
                    RuntimeStatistics = runtimeStatistics,
                    ServerStatistics = serverStatistics,
                    AlphaRuntimeStatistics = AlphaRuntimeStatistics
                })
            };

            return packets.Concat(chartPackets);
        }


        /// <summary>
        /// Send a live trading debug message to the live console.
        /// </summary>
        /// <param name="message">Message we'd like shown in console.</param>
        /// <remarks>When there are already 500 messages in the queue it stops adding new messages.</remarks>
        public void DebugMessage(string message)
        {
            if (Messages.Count > 500) return; //if too many in the queue already skip the logging.
            Messages.Enqueue(new DebugPacket(_job.ProjectId, _deployId, _compileId, message));
            AddToLogStore(message);
        }

        /// <summary>
        /// Send a live trading system debug message to the live console.
        /// </summary>
        /// <param name="message">Message we'd like shown in console.</param>
        public void SystemDebugMessage(string message)
        {
            Messages.Enqueue(new SystemDebugPacket(_job.ProjectId, _deployId, _compileId, message));
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
            Messages.Enqueue(new LogPacket(_deployId, message));
            AddToLogStore(message);
        }

        /// <summary>
        /// Save an algorithm message to the log store. Uses a different timestamped method of adding messaging to interweve debug and logging messages.
        /// </summary>
        /// <param name="message">String message to send to browser.</param>
        private void AddToLogStore(string message)
        {
            Log.Debug("LiveTradingResultHandler.AddToLogStore(): Adding");
            lock (_logStoreLock)
            {
                _logStore.Add(new LogEntry(DateTime.Now.ToString(DateFormat.UI) + " " + message));
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
            Messages.Enqueue(new HandledErrorPacket(_deployId, message, stacktrace));
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
            Messages.Enqueue(new RuntimeErrorPacket(_job.UserId, _deployId, message, stacktrace));
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
        public void Sample(string chartName, string seriesName, int seriesIndex, SeriesType seriesType, DateTime time, decimal value, string unit = "$")
        {
            // Sampling during warming up period skews statistics
            if (_algorithm.IsWarmingUp)
            {
                return;
            }

            Log.Debug("LiveTradingResultHandler.Sample(): Sampling " + chartName + "." + seriesName);
            lock (_chartLock)
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
        public void SampleEquity(DateTime time, decimal value)
        {
            if (value > 0)
            {
                Log.Debug("LiveTradingResultHandler.SampleEquity(): " + time.ToShortTimeString() + " >" + value);
                Sample("Strategy Equity", "Equity", 0, SeriesType.Candle, time, value);
            }
        }

        /// <summary>
        /// Sample the asset prices to generate plots.
        /// </summary>
        /// <param name="symbol">Symbol we're sampling.</param>
        /// <param name="time">Time of sample</param>
        /// <param name="value">Value of the asset price</param>
        /// <seealso cref="Sample(string,string,int,SeriesType,DateTime,decimal,string)"/>
        public void SampleAssetPrices(Symbol symbol, DateTime time, decimal value)
        {
            // don't send stockplots for internal feeds
            Security security;
            if (_algorithm.Securities.TryGetValue(symbol, out security) && !security.IsInternalFeed() && value > 0)
            {
                var now = DateTime.UtcNow.ConvertFromUtc(security.Exchange.TimeZone);
                if (security.Exchange.Hours.IsOpen(now, security.IsExtendedMarketHours))
                {
                    Sample("Stockplot: " + symbol.Value, "Stockplot: " + symbol.Value, 0, SeriesType.Line, time, value);
                }
            }
        }

        /// <summary>
        /// Sample the current daily performance directly with a time-value pair.
        /// </summary>
        /// <param name="time">Current backtest date.</param>
        /// <param name="value">Current daily performance value.</param>
        /// <seealso cref="Sample(string,string,int,SeriesType,DateTime,decimal,string)"/>
        public void SamplePerformance(DateTime time, decimal value)
        {
            //No "daily performance" sampling for live trading yet.
            //Log.Debug("LiveTradingResultHandler.SamplePerformance(): " + time.ToShortTimeString() + " >" + value);
            //Sample("Strategy Equity", ChartType.Overlay, "Daily Performance", SeriesType.Line, time, value, "%");
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
        /// <seealso cref="Sample(string,string,int,SeriesType,DateTime,decimal,string)"/>
        public void SampleRange(List<Chart> updates)
        {
            Log.Debug("LiveTradingResultHandler.SampleRange(): Begin sampling");
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
            Log.Debug("LiveTradingResultHandler.SampleRange(): Finished sampling");
        }

        /// <summary>
        /// Set the algorithm of the result handler after its been initialized.
        /// </summary>
        /// <param name="algorithm">Algorithm object matching IAlgorithm interface</param>
        public void SetAlgorithm(IAlgorithm algorithm)
        {
            _algorithm = algorithm;

            var types = new List<SecurityType>();
            foreach (var kvp in _algorithm.Securities)
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
        }


        /// <summary>
        /// Send a algorithm status update to the user of the algorithms running state.
        /// </summary>
        /// <param name="status">Status enum of the algorithm.</param>
        /// <param name="message">Optional string message describing reason for status change.</param>
        public void SendStatusUpdate(AlgorithmStatus status, string message = "")
        {
            var msg = status + (string.IsNullOrEmpty(message) ? string.Empty : " " + message);
            Log.Trace("LiveTradingResultHandler.SendStatusUpdate(): " + msg);
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
            lock (_runtimeLock)
            {
                if (!_runtimeStatistics.ContainsKey(key))
                {
                    _runtimeStatistics.Add(key, value);
                }
                _runtimeStatistics[key] = value;
            }
            Log.Debug("LiveTradingResultHandler.RuntimeStatistic(): End setting statistic");
        }

        /// <summary>
        /// Send a final analysis result back to the IDE.
        /// </summary>
        /// <param name="job">Lean AlgorithmJob task</param>
        /// <param name="orders">Collection of orders from the algorithm</param>
        /// <param name="profitLoss">Collection of time-profit values for the algorithm</param>
        /// <param name="holdings">Current holdings state for the algorithm</param>
        /// <param name="cashbook">Cashbook of the current cash of the algorithm</param>
        /// <param name="statisticsResults">Statistics information for the algorithm (empty if not finished)</param>
        /// <param name="runtime">Runtime statistics banner information</param>
        public void SendFinalResult(AlgorithmNodePacket job, Dictionary<int, Order> orders, Dictionary<DateTime, decimal> profitLoss, Dictionary<string, Holding> holdings, CashBook cashbook, StatisticsResults statisticsResults, Dictionary<string, string> runtime)
        {
            try
            {
                //Convert local dictionary:
                var charts = new Dictionary<string, Chart>();
                lock (_chartLock)
                {
                    foreach (var kvp in Charts)
                    {
                        charts.Add(kvp.Key, kvp.Value.Clone());
                    }
                }

                //Create a packet:
                var result = new LiveResultPacket((LiveNodePacket) job,
                    new LiveResult(_algorithm.IsFrameworkAlgorithm, charts, orders, profitLoss, holdings, cashbook, statisticsResults.Summary, runtime))
                {
                    ProcessingTime = (DateTime.Now - _startTime).TotalSeconds
                };

                //Save the processing time:

                //Store to S3:
                StoreResult(result, false);

                //Truncate packet to fit within 32kb:
                result.Results = new LiveResult{IsFrameworkAlgorithm = _algorithm.IsFrameworkAlgorithm};

                //Send the truncated packet:
                _messagingHandler.Send(result);
            }
            catch (Exception err)
            {
                Log.Error(err);
            }
        }


        /// <summary>
        /// Process the log entries and save it to permanent storage
        /// </summary>
        /// <param name="logs">Log list</param>
        public void StoreLog(IEnumerable<LogEntry> logs)
        {
            try
            {
                SaveLogs(_job.DeployId, logs.Select(x => x.Message));
            }
            catch (Exception err)
            {
                Log.Error(err);
            }
        }

        /// <summary>
        /// Save the snapshot of the total results to storage.
        /// </summary>
        /// <param name="packet">Packet to store.</param>
        /// <param name="async">Store the packet asyncronously to speed up the thread.</param>
        /// <remarks>
        ///     Async creates crashes in Mono 3.10 if the thread disappears before the upload is complete so it is disabled for now.
        ///     For live trading we're making assumption its a long running task and safe to async save large files.
        /// </remarks>
        public void StoreResult(Packet packet, bool async = true)
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
                    // Set

                    // we need to down sample
                    var start = DateTime.UtcNow.Date;
                    var stop = start.AddDays(1);

                    // truncate to just today, we don't need more than this for anyone
                    Truncate(live.Results, start, stop);

                    var highResolutionCharts = new Dictionary<string, Chart>(live.Results.Charts);

                    // minute resoluton data, save today
                    var minuteSampler = new SeriesSampler(TimeSpan.FromMinutes(1));
                    var minuteCharts = minuteSampler.SampleCharts(live.Results.Charts, start, stop);

                    // swap out our charts with the sampeld data
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
                            IsFrameworkAlgorithm = _algorithm.IsFrameworkAlgorithm,
                            Orders = new Dictionary<int, Order>(live.Results.Orders),
                            Holdings = new Dictionary<string, Holding>(live.Results.Holdings),
                            Charts = new Dictionary<string, Chart> {{name, live.Results.Charts[name]}}
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
        /// New order event for the algorithm backtest: send event to browser.
        /// </summary>
        /// <param name="newEvent">New event details</param>
        public void OrderEvent(OrderEvent newEvent)
        {
            // we'll pull these out for the deltaOrders
            _orderEvents.Enqueue(newEvent);

            //Send the message to frontend as packet:
            Log.Trace("LiveTradingResultHandler.OrderEvent(): " + newEvent, true);
            Messages.Enqueue(new OrderEventPacket(_deployId, newEvent));

            var message = "New Order Event: " + newEvent;
            DebugMessage(message);
        }

        /// <summary>
        /// Terminate the result thread and apply any required exit procedures.
        /// </summary>
        public void Exit()
        {
            // If the algorithm was not successfully initialized, be sure to store the logs
            // Update() will be unable to store the logs if the algorithm never full initialized
            if (!_exitTriggered && _algorithm != null && !_algorithm.GetLocked())
            {
                ProcessSynchronousEvents(true);
                StoreLog(_logStore);
            }

            _exitTriggered = true;

            Update();
        }

        /// <summary>
        /// Purge/clear any outstanding messages in message queue.
        /// </summary>
        public void PurgeQueue()
        {
            Messages.Clear();
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
                var newChart = new Chart(chart.Name, chart.ChartType);
                charts.Add(kvp.Key, newChart);
                foreach (var series in chart.Series.Values)
                {
                    var newSeries = new Series(series.Name, series.SeriesType);
                    newSeries.Values.AddRange(series.Values.Where(chartPoint => chartPoint.x >= unixDateStart && chartPoint.x <= unixDateStop));
                    newChart.AddSeries(newSeries);
                }
            }
            result.Charts = charts;
            result.Orders = result.Orders.Values.Where(x => x.Time >= start && x.Time <= stop).ToDictionary(x => x.Id);

            //Log.Trace("LiveTradingResultHandler.Truncate: Truncate Outgoing: " + result.Charts["Strategy Equity"].Series["Equity"].Values.Count);

            //For live charting convert to UTC
            foreach (var order in result.Orders)
            {
                order.Value.Time = order.Value.Time.ToUniversalTime();
            }
        }

        private string CreateKey(string suffix, string dateFormat = "yyyy-MM-dd")
        {
            return $"{_job.DeployId}-{DateTime.UtcNow.ToString(dateFormat)}_{suffix}.json";
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
        /// Set the chart name that we want data from.
        /// </summary>
        public void SetChartSubscription(string symbol)
        {
            _subscription = symbol;
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

                //Update the asset prices to take a real time sample of the market price even though we're using minute bars
                if (_dataFeed != null)
                {
                    foreach (var subscription in _dataFeed.Subscriptions)
                    {
                        // OI subscription doesn't contain asset market prices
                        if (subscription.Configuration.TickType == TickType.OpenInterest)
                            continue;

                        Security security;
                        if (_algorithm.Securities.TryGetValue(subscription.Configuration.Symbol, out security))
                        {
                            //Sample Portfolio Value:
                            var price = subscription.RealtimePrice;

                            var last = security.GetLastData();
                            if (last != null && price > 0)
                            {
                                // Prevents changes in previous bar
                                last = last.Clone(last.IsFillForward);

                                last.Value = price;
                                security.SetRealTimePrice(last);

                                // Update CashBook for Forex securities
                                var cash = (from c in _algorithm.Portfolio.CashBook
                                    where c.Value.SecuritySymbol == last.Symbol
                                    select c.Value).SingleOrDefault();

                                cash?.Update(last);
                            }
                            else
                            {
                                // we haven't gotten data yet so just spoof a tick to push through the system to start with
                                if (price > 0)
                                {
                                    security.SetMarketPrice(new Tick(time, subscription.Configuration.Symbol, price, price));
                                }
                            }

                            //Sample Asset Pricing:
                            SampleAssetPrices(subscription.Configuration.Symbol, time, price);
                        }
                    }
                }

                //Sample the portfolio value over time for chart.
                SampleEquity(time, Math.Round(_algorithm.Portfolio.TotalPortfolioValue, 4));

                //Also add the user samples / plots to the result handler tracking:
                SampleRange(_algorithm.GetChartUpdates(true));
            }

            // wait until after we're warmed up to start sending running status each minute
            if (!_algorithm.IsWarmingUp && time > _nextRunningStatus)
            {
                _nextRunningStatus = time.Add(TimeSpan.FromMinutes(1));
                _api.SetAlgorithmStatus(_job.AlgorithmId, AlgorithmStatus.Running);
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

            //Send all the notification messages but timeout within a second, or if this is a force process, wait till its done.
            var start = DateTime.Now;
            while (_algorithm.Notify.Messages.Count > 0 && (DateTime.Now < start.AddSeconds(1) || forceProcess))
            {
                Notification message;
                if (_algorithm.Notify.Messages.TryDequeue(out message))
                {
                    //Process the notification messages:
                    Log.Trace("LiveTradingResultHandler.ProcessSynchronousEvents(): Processing Notification...");
                    try
                    {
                        _messagingHandler.SendNotification(message);
                    }
                    catch (Exception err)
                    {
                        Log.Error(err, "Sending notification: " + message.GetType().FullName);
                    }
                }
            }

            Log.Debug("LiveTradingResultHandler.ProcessSynchronousEvents(): Exit");
        }
    }
}
