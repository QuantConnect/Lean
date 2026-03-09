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
*/

using QuantConnect.Util;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using QuantConnect.Optimizer.Parameters;
using Log = QuantConnect.Logging.Log;

namespace QuantConnect.Optimizer.Launcher
{
    /// <summary>
    /// Optimizer implementation that launches Lean as a local process
    /// </summary>
    public class ConsoleLeanOptimizer : LeanOptimizer
    {
        private readonly string _leanLocation;
        private readonly string _rootResultDirectory;
        private readonly string _extraLeanArguments;
        private readonly ConcurrentDictionary<string, Process> _processByBacktestId;

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="nodePacket">The optimization node packet to handle</param>
        public ConsoleLeanOptimizer(OptimizationNodePacket nodePacket) : base(nodePacket)
        {
            _processByBacktestId = new ConcurrentDictionary<string, Process>();

            _rootResultDirectory = Configuration.Config.Get("results-destination-folder",
                Path.Combine(Directory.GetCurrentDirectory(), $"opt-{nodePacket.OptimizationId}"));
            Directory.CreateDirectory(_rootResultDirectory);

            _leanLocation = Configuration.Config.Get("lean-binaries-location",
                Path.Combine(Directory.GetCurrentDirectory(), "../../../Launcher/bin/Debug/QuantConnect.Lean.Launcher"));

            var closeLeanAutomatically = Configuration.Config.GetBool("optimizer-close-automatically", true);
            _extraLeanArguments = $"--close-automatically {closeLeanAutomatically}";

            var algorithmTypeName = Configuration.Config.Get("algorithm-type-name");
            if (!string.IsNullOrEmpty(algorithmTypeName))
            {
                _extraLeanArguments += $" --algorithm-type-name \"{algorithmTypeName}\"";
            }

            var algorithmLanguage = Configuration.Config.Get("algorithm-language");
            if (!string.IsNullOrEmpty(algorithmLanguage))
            {
                _extraLeanArguments += $" --algorithm-language \"{algorithmLanguage}\"";
            }

            var algorithmLocation = Configuration.Config.Get("algorithm-location");
            if (!string.IsNullOrEmpty(algorithmLocation))
            {
                _extraLeanArguments += $" --algorithm-location \"{algorithmLocation}\"";
            }
        }

        /// <summary>
        /// Handles starting Lean for a given parameter set
        /// </summary>
        /// <param name="parameterSet">The parameter set for the backtest to run</param>
        /// <param name="backtestName">The backtest name to use</param>
        /// <returns>The new unique backtest id</returns>
        protected override string RunLean(ParameterSet parameterSet, string backtestName)
        {
            var backtestId = Guid.NewGuid().ToString();
            var optimizationId = NodePacket.OptimizationId;
            // start each lean instance in its own directory so they store their logs & results, else they fight for the log.txt file
            var resultDirectory = Path.Combine(_rootResultDirectory, backtestId);
            Directory.CreateDirectory(resultDirectory);

            // Use ProcessStartInfo class
            var startInfo = new ProcessStartInfo
            {
                FileName = _leanLocation,
                WorkingDirectory = Directory.GetParent(_leanLocation).FullName,
                Arguments = $"--results-destination-folder \"{resultDirectory}\" --algorithm-id \"{backtestId}\" --optimization-id \"{optimizationId}\" --parameters {parameterSet} --backtest-name \"{backtestName}\" {_extraLeanArguments}",
                WindowStyle = ProcessWindowStyle.Minimized
            };

            var process = new Process
            {
                StartInfo = startInfo,
                EnableRaisingEvents = true
            };
            _processByBacktestId[backtestId] = process;

            process.Exited += (sender, args) =>
            {
                if (Disposed)
                {
                    // handle abort
                    return;
                }

                _processByBacktestId.TryRemove(backtestId, out process);
                var backtestResult = $"{backtestId}.json";
                var resultJson = Path.Combine(_rootResultDirectory, backtestId, backtestResult);
                NewResult(File.Exists(resultJson) ? File.ReadAllText(resultJson) : null, backtestId);
                process.DisposeSafely();
            };

            process.Start();

            return backtestId;
        }

        /// <summary>
        /// Stops lean process
        /// </summary>
        /// <param name="backtestId">Specified backtest id</param>
        protected override void AbortLean(string backtestId)
        {
            Process process;
            if (_processByBacktestId.TryRemove(backtestId, out process))
            {
                process.Kill();
                process.DisposeSafely();
            }
        }

        /// <summary>
        /// Sends an update of the current optimization status to the user
        /// </summary>
        protected override void SendUpdate()
        {
            // end handler will already log a nice message on end
            if (Status != OptimizationStatus.Completed && Status != OptimizationStatus.Aborted)
            {
                var currentEstimate = GetCurrentEstimate();
                var stats = GetRuntimeStatistics();
                var message = $"ConsoleLeanOptimizer.SendUpdate(): {currentEstimate} {string.Join(", ", stats.Select(pair => $"{pair.Key}:{pair.Value}"))}";
                var currentBestBacktest = Strategy.Solution;
                if (currentBestBacktest != null)
                {
                    message += $". Best id:'{currentBestBacktest.BacktestId}'. {OptimizationTarget}. Parameters ({currentBestBacktest.ParameterSet})";
                }
                Log.Trace(message);
            }
        }
    }
}
