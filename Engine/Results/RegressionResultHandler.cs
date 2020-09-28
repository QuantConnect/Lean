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
using Newtonsoft.Json;
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Util;

namespace QuantConnect.Lean.Engine.Results
{
    /// <summary>
    /// Provides a wrapper over the <see cref="BacktestingResultHandler"/> that logs all order events
    /// to a separate file
    /// </summary>
    public class RegressionResultHandler : BacktestingResultHandler
    {
        private Language Language => Config.GetValue<Language>("algorithm-language");

        private DateTime _lastAlphaRuntimeStatisticsDate;
        private DateTime _testStartTime;

        private StreamWriter _writer;
        private readonly object _sync = new object();
        private readonly Dictionary<string, string> _currentAlphaRuntimeStatistics;

        // this defaults to false since it can create massive files. a full regression run takes about 800MB
        // for each folder (800MB for ./passed and 800MB for ./regression)
        private static readonly bool HighFidelityLogging = Config.GetBool("regression-high-fidelity-logging", false);

        /// <summary>
        /// Gets the path used for logging all portfolio changing events, such as orders, TPV, daily holdings values
        /// </summary>
        public string LogFilePath => $"./regression/{AlgorithmId}.{Language.ToLower()}.details.log";

        /// <summary>
        /// Initializes a new instance of the <see cref="RegressionResultHandler"/> class
        /// </summary>
        public RegressionResultHandler()
        {
            _testStartTime = DateTime.UtcNow;
            _currentAlphaRuntimeStatistics = new Dictionary<string, string>();
        }

        /// <summary>
        /// Initializes the stream writer using the algorithm's id (name) in the file path
        /// </summary>
        public override void SetAlgorithm(IAlgorithm algorithm, decimal startingPortfolioValue)
        {
            base.SetAlgorithm(algorithm, startingPortfolioValue);

            var fileInfo = new FileInfo(LogFilePath);
            Directory.CreateDirectory(fileInfo.DirectoryName);
            if (fileInfo.Exists)
            {
                fileInfo.Delete();
            }

            _writer = new StreamWriter(LogFilePath);
            _writer.WriteLine($"{_testStartTime}: Starting regression test");
        }

        /// <summary>
        /// Runs on date changes, use this to log TPV and holdings values each day
        /// </summary>
        protected override void SamplePerformance(DateTime time, decimal value)
        {
            lock (_sync)
            {
                _writer.WriteLine($"{Algorithm.UtcTime}: Total Portfolio Value: {Algorithm.Portfolio.TotalPortfolioValue}");

                // write the entire cashbook each day, includes current conversion rates and total value of cash holdings
                _writer.WriteLine(Algorithm.Portfolio.CashBook);

                foreach (var kvp in Algorithm.Securities)
                {
                    var symbol = kvp.Key;
                    var security = kvp.Value;
                    if (!security.HoldStock)
                    {
                        continue;
                    }

                    // detailed logging of security holdings
                    _writer.WriteLine(
                        $"{Algorithm.UtcTime}: " +
                        $"Holdings: {symbol.Value} ({symbol.ID}): " +
                        $"Price: {security.Price} " +
                        $"Quantity: {security.Holdings.Quantity} " +
                        $"Value: {security.Holdings.HoldingsValue} " +
                        $"LastData: {security.GetLastData()}"
                    );
                }
            }

            base.SamplePerformance(time, value);
        }

        /// <summary>
        /// Log the order and order event to the dedicated log file for this regression algorithm
        /// </summary>
        /// <remarks>In backtesting the order events are not sent because it would generate a high load of messaging.</remarks>
        /// <param name="newEvent">New order event details</param>
        public override void OrderEvent(OrderEvent newEvent)
        {
            // log order events to a separate file for easier diffing of regression runs
            var order = Algorithm.Transactions.GetOrderById(newEvent.OrderId);

            lock (_sync)
            {
                _writer.WriteLine($"{Algorithm.UtcTime}: Order: {order}  OrderEvent: {newEvent}");
            }

            base.OrderEvent(newEvent);
        }

        /// <summary>
        /// Perform daily logging of the alpha runtime statistics
        /// </summary>
        public override void SetAlphaRuntimeStatistics(AlphaRuntimeStatistics statistics)
        {
            try
            {
                if (HighFidelityLogging || _lastAlphaRuntimeStatisticsDate != Algorithm.Time.Date)
                {
                    lock (_sync)
                    {
                        _lastAlphaRuntimeStatisticsDate = Algorithm.Time.Date;

                        foreach (var kvp in statistics.ToDictionary())
                        {
                            string value;
                            if (!_currentAlphaRuntimeStatistics.TryGetValue(kvp.Key, out value) || value != kvp.Value)
                            {
                                // only log new or updated values
                                _currentAlphaRuntimeStatistics[kvp.Key] = kvp.Value;
                                _writer.WriteLine($"{Algorithm.Time}: AlphaRuntimeStatistics: {kvp.Key}: {kvp.Value}");
                            }
                        }
                    }
                }

                base.SetAlphaRuntimeStatistics(statistics);
            }
            catch (Exception exception)
            {
                Log.Error(exception);
            }
        }

        /// <summary>
        /// Runs at the end of each time loop. When HighFidelityLogging is enabled, we'll
        /// log each piece of data to allow for faster determination of regression causes
        /// </summary>
        public override void ProcessSynchronousEvents(bool forceProcess = false)
        {
            if (HighFidelityLogging)
            {
                var slice = Algorithm.CurrentSlice;
                if (slice != null)
                {
                    lock (_sync)
                    {
                        // aggregate slice data
                        _writer.WriteLine($"{Algorithm.UtcTime}: Slice Time: {slice.Time:o} Slice Count: {slice.Count}");
                        var data = new Dictionary<Symbol, List<BaseData>>();
                        foreach (var kvp in slice.Bars)
                        {
                            data.Add(kvp.Key, (BaseData) kvp.Value);
                        }

                        foreach (var kvp in slice.QuoteBars)
                        {
                            data.Add(kvp.Key, (BaseData)kvp.Value);
                        }

                        foreach (var kvp in slice.Ticks)
                        {
                            foreach (var tick in kvp.Value)
                            {
                                data.Add(kvp.Key, (BaseData) tick);
                            }
                        }

                        foreach (var kvp in slice.Delistings)
                        {
                            data.Add(kvp.Key, (BaseData) kvp.Value);
                        }

                        foreach (var kvp in slice.Splits)
                        {
                            data.Add(kvp.Key, (BaseData) kvp.Value);
                        }

                        foreach (var kvp in slice.SymbolChangedEvents)
                        {
                            data.Add(kvp.Key, (BaseData) kvp.Value);
                        }

                        foreach (var kvp in slice.Dividends)
                        {
                            data.Add(kvp.Key, (BaseData) kvp.Value);
                        }

                        foreach (var kvp in data.OrderBy(kvp => kvp.Key))
                        {
                            foreach (var item in kvp.Value)
                            {
                                _writer.WriteLine($"{Algorithm.UtcTime}: Slice: DataTime: {item.EndTime} {item}");
                            }
                        }
                    }
                }
            }

            base.ProcessSynchronousEvents(forceProcess);
        }

        /// <summary>
        /// Save the results to disk
        /// </summary>
        public override void SaveResults(string name, Result result)
        {
            File.WriteAllText(GetResultsPath(name), JsonConvert.SerializeObject(result));
        }

        /// <summary>
        /// Terminate the result thread and apply any required exit procedures.
        /// Save orders log files to disk.
        /// </summary>
        public override void Exit()
        {
            base.Exit();
            lock (_sync)
            {
                if (_writer != null)
                {
                    var end = DateTime.UtcNow;
                    var delta = end - _testStartTime;
                    _writer.WriteLine($"{end}: Completed regression test, took: {delta.TotalSeconds:0.0} seconds");
                    _writer.DisposeSafely();
                    _writer = null;
                }
            }
        }
    }
}
