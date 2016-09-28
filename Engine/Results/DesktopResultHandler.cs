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
using System.Threading;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.Setup;
using QuantConnect.Lean.Engine.TransactionHandlers;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Packets;
using QuantConnect.Statistics;
using System.Diagnostics;

namespace QuantConnect.Lean.Engine.Results
{
    /// <summary>
    /// Desktop Result Handler - Desktop GUI Result Handler for Piping Results to WinForms:
    /// </summary>
    public class DesktopResultHandler : IResultHandler
    {
        private bool _isActive;
        private bool _exitTriggered;
        private IAlgorithm _algorithm;
        private readonly object _chartLock;
        private AlgorithmNodePacket _job;

        //Sampling Periods:
        private DateTime _nextSample;
        private readonly TimeSpan _resamplePeriod;
        private readonly TimeSpan _notificationPeriod;

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
        /// Desktop default constructor
        /// </summary>
        public DesktopResultHandler() 
        {
            FinalStatistics = new Dictionary<string, string>();
            Messages = new ConcurrentQueue<Packet>();
            Charts = new ConcurrentDictionary<string, Chart>();

            _chartLock = new Object();
            _isActive = true;
            _resamplePeriod = TimeSpan.FromSeconds(2);
            _notificationPeriod = TimeSpan.FromSeconds(2);
        }

        /// <summary>
        /// Initialize the result handler with this result packet.
        /// </summary>
        /// <param name="job">Algorithm job packet for this result handler</param>
        /// <param name="messagingHandler"></param>
        /// <param name="api"></param>
        /// <param name="dataFeed"></param>
        /// <param name="setupHandler"></param>
        /// <param name="transactionHandler"></param>
        public void Initialize(AlgorithmNodePacket job, IMessagingHandler messagingHandler, IApi api, IDataFeed dataFeed, ISetupHandler setupHandler, ITransactionHandler transactionHandler)
        {
            //Redirect the log messages here:
            _job = job;
            var desktopLogging = new FunctionalLogHandler(DebugMessage, DebugMessage, ErrorMessage);
            Log.LogHandler = new CompositeLogHandler(new[] { desktopLogging, Log.LogHandler });
        }

        /// <summary>
        /// Entry point for console result handler thread.
        /// </summary>
        public void Run()
        {
            while ( !_exitTriggered || Messages.Count > 0 ) 
            {
                Thread.Sleep(100);
            }
            DebugMessage("DesktopResultHandler: Ending Thread...");
            _isActive = false;
        }

        /// <summary>
        /// Send a debug message back to the browser console.
        /// </summary>
        /// <param name="message">Message we'd like shown in console.</param>
        public void DebugMessage(string message)
        {
            Messages.Enqueue(new DebugPacket(0, "", "", message));
        }

        /// <summary>
        /// Send a logging message to the log list for storage.
        /// </summary>
        /// <param name="message">Message we'd in the log.</param>
        public void LogMessage(string message)
        {
            Messages.Enqueue(new LogPacket("", message));
        }

        /// <summary>
        /// Send a runtime error message back to the browser highlighted with in red 
        /// </summary>
        /// <param name="message">Error message.</param>
        /// <param name="stacktrace">Stacktrace information string</param>
        public void RuntimeError(string message, string stacktrace = "")
        {
            Messages.Enqueue(new RuntimeErrorPacket("", message, stacktrace));
        }

        /// <summary>
        /// Send an error message back to the console highlighted in red with a stacktrace.
        /// </summary>
        /// <param name="message">Error message we'd like shown in console.</param>
        public void ErrorMessage(string message)
        {
            Messages.Enqueue(new HandledErrorPacket("", message, ""));
        }

        /// <summary>
        /// Send an error message back to the console highlighted in red with a stacktrace.
        /// </summary>
        /// <param name="message">Error message we'd like shown in console.</param>
        /// <param name="stacktrace">Stacktrace information string</param>
        public void ErrorMessage(string message, string stacktrace = "")
        {
            Messages.Enqueue(new HandledErrorPacket("", message, stacktrace));
        }

        /// <summary>
        /// Add a sample to the chart specified by the chartName, and seriesName.
        /// </summary>
        /// <param name="chartName">String chart name to place the sample.</param>
        /// <param name="seriesIndex">Type of chart we should create if it doesn't already exist.</param>
        /// <param name="seriesName">Series name for the chart.</param>
        /// <param name="seriesType">Series type for the chart.</param>
        /// <param name="time">Time for the sample</param>
        /// <param name="value">Value for the chart sample.</param>
        /// <param name="unit">Unit for the sample axis</param>
        /// <remarks>Sample can be used to create new charts or sample equity - daily performance.</remarks>
        public void Sample(string chartName, string seriesName, int seriesIndex, SeriesType seriesType, DateTime time, decimal value, string unit = "$")
        {
            lock (_chartLock)
            {
                //Add a copy locally:
                if (!Charts.ContainsKey(chartName))
                {
                    Charts.AddOrUpdate<string, Chart>(chartName, new Chart(chartName));
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
            Sample("Strategy Equity", "Equity", 0, SeriesType.Candle, time, value, "$");
        }

        /// <summary>
        /// Sample today's algorithm daily performance value.
        /// </summary>
        /// <param name="time">Current time.</param>
        /// <param name="value">Value of the daily performance.</param>
        public void SamplePerformance(DateTime time, decimal value)
        {
            Sample("Strategy Equity", "Daily Performance", 0, SeriesType.Line, time, value, "%");
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
            DebugMessage("DesktopResultHandler.SendStatusUpdate(): Algorithm Status: " + status + " : " + message);
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
            //Log.Trace("var statistics = new Dictionary<string, string>();");
            
            // Bleh. Nicely format statistical analysis on your algorithm results. Save to file etc.
            foreach (var pair in statisticsResults.Summary) 
            {
                DebugMessage("STATISTICS:: " + pair.Key + " " + pair.Value);
            }

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
            DebugMessage("DesktopResultHandler.OrderEvent(): id:" + newEvent.OrderId + " >> Status:" + newEvent.Status + " >> Fill Price: " + newEvent.FillPrice.ToString("C") + " >> Fill Quantity: " + newEvent.FillQuantity);
        }


        /// <summary>
        /// Set the current runtime statistics of the algorithm
        /// </summary>
        /// <param name="key">Runtime headline statistic name</param>
        /// <param name="value">Runtime headline statistic value</param>
        public void RuntimeStatistic(string key, string value)
        {
            DebugMessage("DesktopResultHandler.RuntimeStatistic(): " + key + " : " + value);
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

    } // End Result Handler Thread:

} // End Namespace
