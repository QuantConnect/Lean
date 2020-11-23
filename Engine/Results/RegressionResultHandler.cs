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
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Securities;
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

        private DateTime _testStartTime;
        private DateTime _lastRuntimeStatisticsDate;
        private DateTime _lastAlphaRuntimeStatisticsDate;

        private TextWriter _writer;
        private readonly object _sync = new object();
        private readonly ConcurrentQueue<string> _preInitializeLines;
        private readonly Dictionary<string, string> _currentRuntimeStatistics;
        private readonly Dictionary<string, string> _currentAlphaRuntimeStatistics;

        // this defaults to false since it can create massive files. a full regression run takes about 800MB
        // for each folder (800MB for ./passed and 800MB for ./regression)
        private static readonly bool HighFidelityLogging = Config.GetBool("regression-high-fidelity-logging", false);

        private static readonly bool IsTest = !Process.GetCurrentProcess().ProcessName.Contains("Lean.Launcher");

        /// <summary>
        /// Gets the path used for logging all portfolio changing events, such as orders, TPV, daily holdings values
        /// </summary>
        public string LogFilePath => IsTest
            ? $"./regression/{AlgorithmId}.{Language.ToLower()}.details.log"
            : $"./{AlgorithmId}/{DateTime.Now:yyyy-MM-dd-hh-mm-ss}.{Language.ToLower()}.details.log";

        /// <summary>
        /// Initializes a new instance of the <see cref="RegressionResultHandler"/> class
        /// </summary>
        public RegressionResultHandler()
        {
            _testStartTime = DateTime.UtcNow;
            _preInitializeLines = new ConcurrentQueue<string>();
            _currentRuntimeStatistics = new Dictionary<string, string>();
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

            lock (_sync)
            {
                _writer = new StreamWriter(LogFilePath);
                WriteLine($"{_testStartTime}: Starting regression test");

                string line;
                while (_preInitializeLines.TryDequeue(out line))
                {
                    WriteLine(line);
                }
            }
        }

        /// <summary>
        /// Runs on date changes, use this to log TPV and holdings values each day
        /// </summary>
        protected override void SamplePerformance(DateTime time, decimal value)
        {
            lock (_sync)
            {
                WriteLine($"{Algorithm.UtcTime}: Total Portfolio Value: {Algorithm.Portfolio.TotalPortfolioValue}");

                // write the entire cashbook each day, includes current conversion rates and total value of cash holdings
                WriteLine($"{Environment.NewLine}{Algorithm.Portfolio.CashBook}");

                foreach (var kvp in Algorithm.Securities)
                {
                    var symbol = kvp.Key;
                    var security = kvp.Value;
                    if (!security.HoldStock)
                    {
                        continue;
                    }

                    // detailed logging of security holdings
                    WriteLine(
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
                WriteLine("==============================================================");
                WriteLine($"    Symbol: {order.Symbol}");
                WriteLine($"     Order: {order}");
                WriteLine($"     Event: {newEvent}");
                WriteLine($"  Position: {Algorithm.Portfolio[newEvent.Symbol].Quantity}");
                SecurityHolding underlyingHolding;
                if (newEvent.Symbol.HasUnderlying && Algorithm.Portfolio.TryGetValue(newEvent.Symbol.Underlying, out underlyingHolding))
                {
                    WriteLine($"Underlying: {underlyingHolding.Quantity}");
                }
                WriteLine($"      Cash: {Algorithm.Portfolio.Cash:0.00}");
                WriteLine($" Portfolio: {Algorithm.Portfolio.TotalPortfolioValue:0.00}");
                WriteLine("==============================================================");
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
                                WriteLine($"AlphaRuntimeStatistics: {kvp.Key}: {kvp.Value}");
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
        /// Send list of security asset types the algortihm uses to browser.
        /// </summary>
        public override void SecurityType(List<SecurityType> types)
        {
            base.SecurityType(types);

            var sorted = types.Select(type => type.ToString()).OrderBy(type => type);
            WriteLine($"SecurityTypes: {string.Join("|", sorted)}");
        }

        /// <summary>
        /// Send a debug message back to the browser console.
        /// </summary>
        /// <param name="message">Message we'd like shown in console.</param>
        public override void DebugMessage(string message)
        {
            base.DebugMessage(message);

            WriteLine($"DebugMessage: {message}");
        }

        /// <summary>
        /// Send an error message back to the browser highlighted in red with a stacktrace.
        /// </summary>
        /// <param name="message">Error message we'd like shown in console.</param>
        /// <param name="stacktrace">Stacktrace information string</param>
        public override void ErrorMessage(string message, string stacktrace = "")
        {
            base.ErrorMessage(message, stacktrace);

            stacktrace = string.IsNullOrEmpty(stacktrace) ? null : Environment.NewLine + stacktrace;
            WriteLine($"ErrorMessage: {message}{stacktrace}");
        }

        /// <summary>
        /// Send a logging message to the log list for storage.
        /// </summary>
        /// <param name="message">Message we'd in the log.</param>
        public override void LogMessage(string message)
        {
            base.LogMessage(message);

            WriteLine($"LogMessage: {message}");
        }

        /// <summary>
        /// Send a runtime error message back to the browser highlighted with in red
        /// </summary>
        /// <param name="message">Error message.</param>
        /// <param name="stacktrace">Stacktrace information string</param>
        public override void RuntimeError(string message, string stacktrace = "")
        {
            base.RuntimeError(message, stacktrace);

            stacktrace = string.IsNullOrEmpty(stacktrace) ? null : Environment.NewLine + stacktrace;
            WriteLine($"RuntimeError: {message}{stacktrace}");
        }

        /// <summary>
        /// Send a system debug message back to the browser console.
        /// </summary>
        /// <param name="message">Message we'd like shown in console.</param>
        public override void SystemDebugMessage(string message)
        {
            base.SystemDebugMessage(message);

            WriteLine($"SystemDebugMessage: {message}");
        }

        /// <summary>
        /// Set the current runtime statistics of the algorithm.
        /// These are banner/title statistics which show at the top of the live trading results.
        /// </summary>
        /// <param name="key">Runtime headline statistic name</param>
        /// <param name="value">Runtime headline statistic value</param>
        public override void RuntimeStatistic(string key, string value)
        {
            try
            {
                if (HighFidelityLogging || _lastRuntimeStatisticsDate != Algorithm.Time.Date)
                {
                    _lastRuntimeStatisticsDate = Algorithm.Time.Date;

                    string existingValue;
                    if (!_currentRuntimeStatistics.TryGetValue(key, out existingValue) || existingValue != value)
                    {
                        _currentRuntimeStatistics[key] = value;
                        WriteLine($"RuntimeStatistic: {key}: {value}");
                    }
                }

                base.RuntimeStatistic(key, value);
            }
            catch (Exception exception)
            {
                Log.Error(exception);
            }
        }

        /// <summary>
        /// Save an algorithm message to the log store. Uses a different timestamped method of adding messaging to interweve debug and logging messages.
        /// </summary>
        /// <param name="message">String message to store</param>
        protected override void AddToLogStore(string message)
        {
            base.AddToLogStore(message);

            WriteLine($"AddToLogStore: {message}");
        }

        /// <summary>
        /// Event fired each time that we add/remove securities from the data feed
        /// </summary>
        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            base.OnSecuritiesChanged(changes);

            if (changes.AddedSecurities.Count > 0)
            {
                var added = changes.AddedSecurities
                    .Select(security => security.Symbol.ToString())
                    .OrderBy(symbol => symbol);

                WriteLine($"OnSecuritiesChanged:ADD: {string.Join("|", added)}");
            }

            if (changes.RemovedSecurities.Count > 0)
            {
                var removed = changes.RemovedSecurities
                    .Select(security => security.Symbol.ToString())
                    .OrderBy(symbol => symbol);

                WriteLine($"OnSecuritiesChanged:REM: {string.Join("|", removed)}");
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
                        WriteLine($"Slice Time: {slice.Time:o} Slice Count: {slice.Count}");
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
                                WriteLine($"{Algorithm.UtcTime}: Slice: DataTime: {item.EndTime} {item}");
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
                    // only log final statistics and we want them to all be together
                    foreach (var kvp in RuntimeStatistics.OrderBy(kvp => kvp.Key))
                    {
                        WriteLine($"{kvp.Key,-15}\t{kvp.Value}");
                    }

                    var end = DateTime.UtcNow;
                    var delta = end - _testStartTime;
                    WriteLine($"{end}: Completed regression test, took: {delta.TotalSeconds:0.0} seconds");
                    _writer.DisposeSafely();
                    _writer = null;
                }
                else
                {
                    string line;
                    while (_preInitializeLines.TryDequeue(out line))
                    {
                        Console.WriteLine(line);
                    }
                }
            }
        }

        /// <summary>
        /// We want to make algorithm messages end up in both the standard regression log file {algorithm}.{language}.log
        /// as well as the details log {algorithm}.{language}.details.log. The details log is focused on providing a log
        /// dedicated solely to the algorithm's behavior, void of all <see cref="QuantConnect.Logging.Log"/> messages
        /// </summary>
        protected override void ConfigureConsoleTextWriter(IAlgorithm algorithm)
        {
            // configure Console.WriteLine and Console.Error.WriteLine to both logs, syslog and details.log
            // when 'forward-console-messages' is set to false, it guarantees synchronous logging of these messages

            if (Config.GetBool("forward-console-messages", true))
            {
                // we need to forward Console.Write messages to the algorithm's Debug function
                Console.SetOut(new FuncTextWriter(msg =>
                {
                    algorithm.Debug(msg);
                    WriteLine($"DEBUG: {msg}");
                }));
                Console.SetError(new FuncTextWriter(msg =>
                {
                    algorithm.Error(msg);
                    WriteLine($"ERROR: {msg}");
                }));
            }
            else
            {
                // we need to forward Console.Write messages to the standard Log functions
                Console.SetOut(new FuncTextWriter(msg =>
                {
                    Log.Trace(msg);
                    WriteLine($"DEBUG: {msg}");
                }));
                Console.SetError(new FuncTextWriter(msg =>
                {
                    Log.Error(msg);
                    WriteLine($"ERROR: {msg}");
                }));
            }
        }

        private void WriteLine(string message)
        {
            lock (_sync)
            {
                if (_writer == null)
                {
                    _preInitializeLines.Enqueue(message);
                }
                else
                {
                    _writer.WriteLine($"{Algorithm.Time:O}: {message}");
                }
            }
        }
    }
}
