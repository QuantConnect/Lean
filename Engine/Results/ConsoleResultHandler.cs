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
using System.Globalization;
using System.IO;
using System.Threading;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.Setup;
using QuantConnect.Lean.Engine.TransactionHandlers;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Packets;
using QuantConnect.Notifications;  
using QuantConnect.Statistics;

namespace QuantConnect.Lean.Engine.Results
{
    /// <summary>
    /// Console local resulthandler passes messages back to the console/local GUI display.
    /// </summary>
    public class ConsoleResultHandler : IResultHandler
    {
        private bool _isActive;
        private bool _exitTriggered;
        private DateTime _updateTime;
        private DateTime _lastSampledTimed;
        private IAlgorithm _algorithm;
        private readonly object _chartLock;
        private IConsoleStatusHandler _algorithmNode;
        private IMessagingHandler _messagingHandler; 

        //Sampling Periods:
        private DateTime _nextSample;
        private TimeSpan _resamplePeriod;
        private readonly TimeSpan _notificationPeriod;
        private string _chartDirectory;
        private readonly Dictionary<string, List<string>> _equityResults;

        /// <summary>
        /// A dictionary containing summary statistics
        /// </summary>
        public Dictionary<string, string> FinalStatistics { get; private set; }

        /// <summary>
        /// Messaging to store notification messages for processing.
        /// </summary>
        public ConcurrentQueue<Packet> Messages 
        {
            get;
            set;
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
            get;
            set;
        }

        /// <summary>
        /// Boolean flag indicating the result hander thread is busy. 
        /// False means it has completely finished and ready to dispose.
        /// </summary>
        public bool IsActive {
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
        /// Console result handler constructor.
        /// </summary>
        public ConsoleResultHandler() 
        {
            Messages = new ConcurrentQueue<Packet>();
            Charts = new ConcurrentDictionary<string, Chart>();
            FinalStatistics = new Dictionary<string, string>();
            _chartLock = new Object();
            _isActive = true;
            _notificationPeriod = TimeSpan.FromSeconds(5);
            _equityResults = new Dictionary<string, List<string>>();
        }

        /// <summary>
        /// Initialize the result handler with this result packet.
        /// </summary>
        /// <param name="packet">Algorithm job packet for this result handler</param>
        /// <param name="messagingHandler"></param>
        /// <param name="api"></param>
        /// <param name="dataFeed"></param>
        /// <param name="setupHandler"></param>
        /// <param name="transactionHandler"></param>
        public void Initialize(AlgorithmNodePacket packet, IMessagingHandler messagingHandler, IApi api, IDataFeed dataFeed, ISetupHandler setupHandler, ITransactionHandler transactionHandler)
        {
            // we expect one of two types here, the backtest node packet or the live node packet
            var job = packet as BacktestNodePacket;
            if (job != null)
            {
                _algorithmNode = new BacktestConsoleStatusHandler(job);
            }
            else
            {
                var live = packet as LiveNodePacket;
                if (live == null)
                {
                    throw new ArgumentException("Unexpected AlgorithmNodeType: " + packet.GetType().Name);
                }
                _algorithmNode = new LiveConsoleStatusHandler(live);
            }
            _resamplePeriod = _algorithmNode.ComputeSampleEquityPeriod();

            var time = DateTime.Now.ToString("yyyy-MM-dd-HH-mm");
            _chartDirectory = Path.Combine("../../../Charts/", packet.AlgorithmId, time);
            if (Directory.Exists(_chartDirectory))
            {
                foreach (var file in Directory.EnumerateFiles(_chartDirectory, "*.csv", SearchOption.AllDirectories))
                {
                    File.Delete(file);
                }
                Directory.Delete(_chartDirectory, true);
            }
            Directory.CreateDirectory(_chartDirectory);
            _messagingHandler = messagingHandler; 

        }
        
        /// <summary>
        /// Entry point for console result handler thread.
        /// </summary>
        public void Run()
        {
            try
            {
                while ( !_exitTriggered || Messages.Count > 0 ) 
                {
                    Thread.Sleep(100);

                    var now = DateTime.UtcNow;
                    if (now > _updateTime)
                    {
                        _updateTime = now.AddSeconds(5);
                        _algorithmNode.LogAlgorithmStatus(_lastSampledTimed);
                    }
                }

                // Write Equity and EquityPerformance files in charts directory
                foreach (var fileName in _equityResults.Keys)
                {
                    File.WriteAllLines(fileName, _equityResults[fileName]);
                }
            }
            catch (Exception err)
            {
                // unexpected error, we need to close down shop
                Log.Error(err);
                // quit the algorithm due to error
                _algorithm.RunTimeError = err;
            }

            Log.Trace("ConsoleResultHandler: Ending Thread...");
            _isActive = false;
        }

        /// <summary>
        /// Send a debug message back to the browser console.
        /// </summary>
        /// <param name="message">Message we'd like shown in console.</param>
        public void DebugMessage(string message)
        {
            Log.Trace(_algorithm.Time + ": Debug >> " + message);
        }

        /// <summary>
        /// Send a logging message to the log list for storage.
        /// </summary>
        /// <param name="message">Message we'd in the log.</param>
        public void LogMessage(string message)
        {
            Log.Trace(_algorithm.Time + ": Log >> " + message);
        }

        /// <summary>
        /// Send a runtime error message back to the browser highlighted with in red 
        /// </summary>
        /// <param name="message">Error message.</param>
        /// <param name="stacktrace">Stacktrace information string</param>
        public void RuntimeError(string message, string stacktrace = "")
        {
            Log.Error(_algorithm.Time + ": Error >> " + message + (!string.IsNullOrEmpty(stacktrace) ? (" >> ST: " + stacktrace) : ""));
        }

        /// <summary>
        /// Send an error message back to the console highlighted in red with a stacktrace.
        /// </summary>
        /// <param name="message">Error message we'd like shown in console.</param>
        /// <param name="stacktrace">Stacktrace information string</param>
        public void ErrorMessage(string message, string stacktrace = "")
        {
            Log.Error(_algorithm.Time + ": Error >> " + message + (!string.IsNullOrEmpty(stacktrace) ? (" >> ST: " + stacktrace) : ""));
        }

        /// <summary>
        /// Add a sample to the chart specified by the chartName, and seriesName.
        /// </summary>
        /// <param name="chartName">String chart name to place the sample.</param>
        /// <param name="seriesName">Series name for the chart.</param>
        /// <param name="seriesType">Series type for the chart.</param>
        /// <param name="time">Time for the sample</param>
        /// <param name="value">Value for the chart sample.</param>
        /// <param name="unit">Unit for the sample axis</param>
        /// <param name="seriesIndex">Index of the series we're sampling</param>
        /// <remarks>Sample can be used to create new charts or sample equity - daily performance.</remarks>
        public void Sample(string chartName, string seriesName, int seriesIndex, SeriesType seriesType, DateTime time, decimal value, string unit = "$")
        {
            var chartFilename = Path.Combine(_chartDirectory, chartName + "-" + seriesName + ".csv");

            lock (_chartLock)
            {
                // Add line to list in dictionary, will be written to file at the end
                List<string> rows;
                if (!_equityResults.TryGetValue(chartFilename, out rows))
                {
                    rows = new List<string>();
                    _equityResults[chartFilename] = rows;
                }
                rows.Add(time + "," + value.ToString("F2", CultureInfo.InvariantCulture));

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
        }

        /// <summary>
        /// Sample the strategy equity at this moment in time.
        /// </summary>
        /// <param name="time">Current time</param>
        /// <param name="value">Current equity value</param>
        public void SampleEquity(DateTime time, decimal value)
        {
            Sample("Strategy Equity", "Equity", 0, SeriesType.Candle, time, value);
            _lastSampledTimed = time;
        }

        /// <summary>
        /// Sample today's algorithm daily performance value.
        /// </summary>
        /// <param name="time">Current time.</param>
        /// <param name="value">Value of the daily performance.</param>
        public void SamplePerformance(DateTime time, decimal value)
        {
            Sample("Strategy Equity", "Daily Performance", 1, SeriesType.Line, time, value, "%");
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
        /// Analyse the algorithm and determine its security types.
        /// </summary>
        /// <param name="types">List of security types in the algorithm</param>
        public void SecurityType(List<SecurityType> types)
        {
            //NOP
        }

        /// <summary>
        /// Send an algorithm status update to the browser.
        /// </summary>
        /// <param name="status">Status enum value.</param>
        /// <param name="message">Additional optional status message.</param>
        /// <remarks>In backtesting we do not send the algorithm status updates.</remarks>
        public void SendStatusUpdate(AlgorithmStatus status, string message = "")
        {
            Log.Trace("ConsoleResultHandler.SendStatusUpdate(): Algorithm Status: " + status + " : " + message);
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
        /// Add a range of samples to the store.
        /// </summary>
        /// <param name="updates">Charting updates since the last sample request.</param>
        public void SampleRange(List<Chart> updates)
        {
            lock (_chartLock)
            {
                foreach (var update in updates)
                {
                    //Create the chart if it doesn't exist already:
                    if (!Charts.ContainsKey(update.Name))
                    {
                        Charts.AddOrUpdate(update.Name, new Chart(update.Name, update.ChartType));
                    }

                    //Add these samples to this chart.
                    foreach (var series in update.Series.Values)
                    {
                        //If we don't already have this record, its the first packet
                        if (!Charts[update.Name].Series.ContainsKey(series.Name))
                        {
                            Charts[update.Name].Series.Add(series.Name, new Series(series.Name, series.SeriesType));
                        }

                        //We already have this record, so just the new samples to the end:
                        Charts[update.Name].Series[series.Name].Values.AddRange(series.Values);
                    }
                }
            }
        }

        /// <summary>
        /// Algorithm final analysis results dumped to the console.
        /// </summary>
        /// <param name="job">Lean AlgorithmJob task</param>
        /// <param name="orders">Collection of orders from the algorithm</param>
        /// <param name="profitLoss">Collection of time-profit values for the algorithm</param>
        /// <param name="holdings">Current holdings state for the algorithm</param>
        /// <param name="statisticsResults">Statistics information for the algorithm (empty if not finished)</param>
        /// <param name="banner">Runtime statistics banner information</param>
        public void SendFinalResult(AlgorithmNodePacket job, Dictionary<int, Order> orders, Dictionary<DateTime, decimal> profitLoss, Dictionary<string, Holding> holdings, StatisticsResults statisticsResults, Dictionary<string, string> banner)
        {
            // uncomment these code traces to help write regression tests
            //Console.WriteLine("var statistics = new Dictionary<string, string>();");
            
            // Bleh. Nicely format statistical analysis on your algorithm results. Save to file etc.
            foreach (var pair in statisticsResults.Summary) 
            {
                Log.Trace("STATISTICS:: " + pair.Key + " " + pair.Value);
                //Console.WriteLine(string.Format("statistics.Add(\"{0}\",\"{1}\");", pair.Key, pair.Value));
            }

            //foreach (var pair in statisticsResults.RollingPerformances) 
            //{
            //    Log.Trace("ROLLINGSTATS:: " + pair.Key + " SharpeRatio: " + Math.Round(pair.Value.PortfolioStatistics.SharpeRatio, 3));
            //}

            FinalStatistics = statisticsResults.Summary;
        }

        /// <summary>
        /// Set the Algorithm instance for ths result.
        /// </summary>
        /// <param name="algorithm">Algorithm we're working on.</param>
        /// <remarks>While setting the algorithm the backtest result handler.</remarks>
        public void SetAlgorithm(IAlgorithm algorithm) 
        {
            _algorithm = algorithm;
        }

        /// <summary>
        /// Terminate the result thread and apply any required exit proceedures.
        /// </summary>
        public void Exit()
        {
            _exitTriggered = true;
        }

        /// <summary>
        /// Send a new order event to the browser.
        /// </summary>
        /// <remarks>In backtesting the order events are not sent because it would generate a high load of messaging.</remarks>
        /// <param name="newEvent">New order event details</param>
        public void OrderEvent(OrderEvent newEvent)
        {
            Log.Debug("ConsoleResultHandler.OrderEvent(): id:" + newEvent.OrderId + " >> Status:" + newEvent.Status + " >> Fill Price: " + newEvent.FillPrice.ToString("C") + " >> Fill Quantity: " + newEvent.FillQuantity);
        }

        /// <summary>
        /// Set the current runtime statistics of the algorithm
        /// </summary>
        /// <param name="key">Runtime headline statistic name</param>
        /// <param name="value">Runtime headline statistic value</param>
        public void RuntimeStatistic(string key, string value)
        {
            Log.Trace("ConsoleResultHandler.RuntimeStatistic(): "  + key + " : " + value);
        }

        /// <summary>
        /// Clear the outstanding message queue to exit the thread.
        /// </summary>
        public void PurgeQueue() 
        {
            Messages.Clear();
        }

        /// <summary>
        /// Store result on desktop.
        /// </summary>
        /// <param name="packet">Packet of data to store.</param>
        /// <param name="async">Store the packet asyncronously to speed up the thread.</param>
        /// <remarks>Async creates crashes in Mono 3.10 if the thread disappears before the upload is complete so it is disabled for now.</remarks>
        public void StoreResult(Packet packet, bool async = false)
        {
            // Do nothing.
        }

        /// <summary>
        /// Provides an abstraction layer for live vs backtest packets to provide status/sampling to the AlgorithmManager
        /// </summary>
        /// <remarks>
        /// Since we can run both live and back test from the console, we need two implementations of what to do
        /// at certain times
        /// </remarks>
        private interface IConsoleStatusHandler
        {
            void LogAlgorithmStatus(DateTime current);
            TimeSpan ComputeSampleEquityPeriod();
        }

        // uses a const 2 second sample equity period and does nothing for logging algorithm status
        private class LiveConsoleStatusHandler : IConsoleStatusHandler
        {
            private readonly LiveNodePacket _job;
            public LiveConsoleStatusHandler(LiveNodePacket _job)
            {
                this._job = _job;
            }
            public void LogAlgorithmStatus(DateTime current)
            {
                // later we can log daily %Gain if possible
            }
            public TimeSpan ComputeSampleEquityPeriod()
            {
                return TimeSpan.FromSeconds(2);
            }
        }
        // computes sample equity period from 4000 samples evenly spaced over the backtest interval and logs %complete to log file
        private class BacktestConsoleStatusHandler : IConsoleStatusHandler
        {
            private readonly BacktestNodePacket _job;
            private double? _backtestSpanInDays;
            public BacktestConsoleStatusHandler(BacktestNodePacket _job)
            {
                this._job = _job;
            }
            public void LogAlgorithmStatus(DateTime current)
            {
                if (!_backtestSpanInDays.HasValue)
                {
                    _backtestSpanInDays = Math.Round((_job.PeriodFinish - _job.PeriodStart).TotalDays);
                    if (_backtestSpanInDays == 0.0)
                    {
                        _backtestSpanInDays = null;
                    }
                }

                // we need to wait until we've called initialize on the algorithm
                // this is not ideal at all
                if (_backtestSpanInDays.HasValue)
                {
                    var daysProcessed = (current - _job.PeriodStart).TotalDays;
                    if (daysProcessed < 0) daysProcessed = 0;
                    if (daysProcessed > _backtestSpanInDays.Value) daysProcessed = _backtestSpanInDays.Value;
                    Log.Trace("Progress: " + (daysProcessed * 100 / _backtestSpanInDays.Value).ToString("F2") + "% Processed: " + daysProcessed.ToString("0.000") + " days of total: " + (int)_backtestSpanInDays.Value);
                }
                else
                {
                    Log.Trace("Initializing...");
                }
            }

            public TimeSpan ComputeSampleEquityPeriod()
            {
                const double samples = 4000;
                const double minimumSamplePeriod = 4 * 60;
                double resampleMinutes = minimumSamplePeriod;

                var totalMinutes = (_job.PeriodFinish - _job.PeriodStart).TotalMinutes;

                // before initialize is called this will be zero
                if (totalMinutes > 0.0)
                {
                    resampleMinutes = (totalMinutes < (minimumSamplePeriod * samples)) ? minimumSamplePeriod : (totalMinutes / samples);
                }

                // set max value
                if (resampleMinutes < minimumSamplePeriod)
                {
                    resampleMinutes = minimumSamplePeriod;
                }
                    
                return TimeSpan.FromMinutes(resampleMinutes);
            }
        }

        /// <summary>
        /// Not used
        /// </summary>
        public void SetChartSubscription(string symbol)
        {
            //
        }

        /// <summary>
        /// Process the synchronous result events, sampling and message reading. 
        /// This method is triggered from the algorithm manager thread.
        /// </summary>
        /// <remarks>Prime candidate for putting into a base class. Is identical across all result handlers.</remarks>
        public void ProcessSynchronousEvents(bool forceProcess = false)
        {
            var time = _algorithm.Time;

            if (time > _nextSample || forceProcess)
            {
                //Set next sample time: 4000 samples per backtest
                _nextSample = time.Add(ResamplePeriod);

                //Sample the portfolio value over time for chart.
                SampleEquity(time, Math.Round(_algorithm.Portfolio.TotalPortfolioValue, 4));

                //Also add the user samples / plots to the result handler tracking:
                SampleRange(_algorithm.GetChartUpdates());

                //Sample the asset pricing:
                foreach (var security in _algorithm.Securities.Values) 
                {
                    SampleAssetPrices(security.Symbol, time, security.Price);
                }
            }

            //Send out the debug messages:
            _algorithm.DebugMessages.ForEach(x => DebugMessage(x));
            _algorithm.DebugMessages.Clear();

            //Send out the error messages:
            _algorithm.ErrorMessages.ForEach(x => ErrorMessage(x));
            _algorithm.ErrorMessages.Clear();

            //Send out the log messages:
            _algorithm.LogMessages.ForEach(x => LogMessage(x));
            _algorithm.LogMessages.Clear();

            //Set the running statistics:
            foreach (var pair in _algorithm.RuntimeStatistics)
            {
                RuntimeStatistic(pair.Key, pair.Value);
            }
            // Dequeue and processes notification messages
            //Send all the notification messages but timeout within a second
            var start = DateTime.UtcNow;
            while (_algorithm.Notify.Messages.Count > 0 && DateTime.UtcNow < start.AddSeconds(1))
            {
                Notification message;
                if (_algorithm.Notify.Messages.TryDequeue(out message))
                {
                    //Process the notification messages:
                    Log.Trace("ConsoleResultHandler.ProcessSynchronousEvents(): Processing Notification...");

                    switch (message.GetType().Name)
                    {
                        case "NotificationEmail":
                            _messagingHandler.Email(message as NotificationEmail);
                            break;

                        case "NotificationSms":
                            _messagingHandler.Sms(message as NotificationSms);
                            break;

                        case "NotificationWeb":
                            _messagingHandler.Web(message as NotificationWeb);
                            break;

                        default:
                            try
                            {
                                //User code.
                                message.Send();
                            }
                            catch (Exception err)
                            {
                                Log.Error(err, "Custom send notification:");
                                ErrorMessage("Custom send notification: " + err.Message, err.StackTrace);
                            }
                            break;
                    }
                }
            }
        }

    } // End Result Handler Thread:

} // End Namespace
