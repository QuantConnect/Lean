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

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Newtonsoft.Json;
using QuantConnect.Configuration;
using QuantConnect.Interfaces;
using QuantConnect.Util;

namespace QuantConnect.Data
{
    /// <summary>
    /// Monitors data requests and reports on missing data
    /// </summary>
    public class DataMonitor : IDataMonitor
    {
        private bool _exited;

        private TextWriter _succeededDataRequestsWriter;
        private TextWriter _failedDataRequestsWriter;

        private long _succeededDataRequestsCount;
        private long _failedDataRequestsCount;

        private long _succeededUniverseDataRequestsCount;
        private long _failedUniverseDataRequestsCount;

        private readonly List<double> _requestRates = new();
        private long _prevRequestsCount;
        private DateTime _lastRequestRateCalculationTime;

        private Thread _requestRateCalculationThread;
        private CancellationTokenSource _cancellationTokenSource;

        private readonly string _succeededDataRequestsFileName;
        private readonly string _failedDataRequestsFileName;
        private readonly string _resultsDestinationFolder;

        private readonly object _threadLock = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="DataMonitor"/> class
        /// </summary>
        public DataMonitor()
        {
            _resultsDestinationFolder = Globals.ResultsDestinationFolder;
            _succeededDataRequestsFileName = GetFilePath("succeeded-data-requests.txt");
            _failedDataRequestsFileName = GetFilePath("failed-data-requests.txt");
        }

        /// <summary>
        /// Event handler for the <see cref="IDataProvider.NewDataRequest"/> event
        /// </summary>
        public void OnNewDataRequest(object sender, DataProviderNewDataRequestEventArgs e)
        {
            if (_exited)
            {
                return;
            }

            Initialize();

            if (e.Path.Contains("map_files", StringComparison.OrdinalIgnoreCase) ||
                e.Path.Contains("factor_files", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var path = StripDataFolder(e.Path);
            var isUniverseData = path.Contains("coarse", StringComparison.OrdinalIgnoreCase) ||
                path.Contains("universe", StringComparison.OrdinalIgnoreCase);

            if (e.Succeded)
            {
                WriteLineToFile(_succeededDataRequestsWriter, path, _succeededDataRequestsFileName);
                Interlocked.Increment(ref _succeededDataRequestsCount);
                if (isUniverseData)
                {
                    Interlocked.Increment(ref _succeededUniverseDataRequestsCount);
                }
            }
            else
            {
                WriteLineToFile(_failedDataRequestsWriter, path, _failedDataRequestsFileName);
                Interlocked.Increment(ref _failedDataRequestsCount);
                if (isUniverseData)
                {
                    Interlocked.Increment(ref _failedUniverseDataRequestsCount);
                }

                if (Logging.Log.DebuggingEnabled)
                {
                    Logging.Log.Debug($"DataMonitor.OnNewDataRequest(): Data from {path} could not be fetched");
                }
            }
        }

        /// <summary>
        /// Terminates the data monitor generating a final report
        /// </summary>
        public void Exit()
        {
            if (_exited || _requestRateCalculationThread == null)
            {
                return;
            }
            _exited = true;

            _requestRateCalculationThread.StopSafely(TimeSpan.FromSeconds(5), _cancellationTokenSource);
            _succeededDataRequestsWriter?.Close();
            _failedDataRequestsWriter?.Close();

            StoreDataMonitorReport(GenerateReport());

            _succeededDataRequestsWriter.DisposeSafely();
            _failedDataRequestsWriter.DisposeSafely();
            _cancellationTokenSource.DisposeSafely();
        }

        public void Dispose()
        {
            Exit();
        }

        protected virtual string StripDataFolder(string path)
        {
            if (path.StartsWith(Globals.DataFolder, StringComparison.OrdinalIgnoreCase))
            {
                return path.Substring(Globals.DataFolder.Length);
            }

            return path;
        }

        /// <summary>
        /// Initializes the <see cref="DataMonitor"/> instance
        /// </summary>
        private void Initialize()
        {
            if (_requestRateCalculationThread != null)
            {
                return;
            }
            lock (_threadLock)
            {
                if (_requestRateCalculationThread != null)
                {
                    return;
                }
                // we create the files on demand
                _succeededDataRequestsWriter = OpenStream(_succeededDataRequestsFileName);
                _failedDataRequestsWriter = OpenStream(_failedDataRequestsFileName);

                _cancellationTokenSource = new CancellationTokenSource();

                _requestRateCalculationThread = new Thread(() =>
                {
                    while (!_cancellationTokenSource.Token.WaitHandle.WaitOne(3000))
                    {
                        ComputeFileRequestFrequency();
                    }
                })
                { IsBackground = true };
                _requestRateCalculationThread.Start();
            }
        }

        private DataMonitorReport GenerateReport()
        {
            var report = new DataMonitorReport(_succeededDataRequestsCount,
                _failedDataRequestsCount,
                _succeededUniverseDataRequestsCount,
                _failedUniverseDataRequestsCount,
                _requestRates);

            Logging.Log.Trace($"DataMonitor.GenerateReport():{Environment.NewLine}" +
                $"DATA USAGE:: Total data requests {report.TotalRequestsCount}{Environment.NewLine}" +
                $"DATA USAGE:: Succeeded data requests {report.SucceededDataRequestsCount}{Environment.NewLine}" +
                $"DATA USAGE:: Failed data requests {report.FailedDataRequestsCount}{Environment.NewLine}" +
                $"DATA USAGE:: Failed data requests percentage {report.FailedDataRequestsPercentage}%{Environment.NewLine}" +
                $"DATA USAGE:: Total universe data requests {report.TotalUniverseDataRequestsCount}{Environment.NewLine}" +
                $"DATA USAGE:: Succeeded universe data requests {report.SucceededUniverseDataRequestsCount}{Environment.NewLine}" +
                $"DATA USAGE:: Failed universe data requests {report.FailedUniverseDataRequestsCount}{Environment.NewLine}" +
                $"DATA USAGE:: Failed universe data requests percentage {report.FailedUniverseDataRequestsPercentage}%");

            return report;
        }

        private void ComputeFileRequestFrequency()
        {
            var requestsCount = _succeededDataRequestsCount + _failedDataRequestsCount;

            if (_lastRequestRateCalculationTime == default)
            {
                // First time we calculate the request rate.
                // We don't have a previous value to compare to so we just store the current value.
                _lastRequestRateCalculationTime = DateTime.UtcNow;
                _prevRequestsCount = requestsCount;
                return;
            }

            var requestsCountDelta = requestsCount - _prevRequestsCount;
            var now = DateTime.UtcNow;
            var timeDelta = now - _lastRequestRateCalculationTime;

            _requestRates.Add(Math.Round(requestsCountDelta / timeDelta.TotalSeconds));
            _prevRequestsCount = requestsCount;
            _lastRequestRateCalculationTime = now;
        }

        /// <summary>
        /// Stores the data monitor report
        /// </summary>
        /// <param name="report">The data monitor report to be stored<param>
        private void StoreDataMonitorReport(DataMonitorReport report)
        {
            if (report == null)
            {
                return;
            }

            var path = GetFilePath("data-monitor-report.json");
            var data = JsonConvert.SerializeObject(report, Formatting.None);
            File.WriteAllText(path, data);
        }

        private string GetFilePath(string filename)
        {
            var baseFilename = Path.GetFileNameWithoutExtension(filename);
            var timestamp = DateTime.UtcNow.ToStringInvariant("yyyyMMddHHmmssfff");
            var extension = Path.GetExtension(filename);
            return Path.Combine(_resultsDestinationFolder, $"{baseFilename}-{timestamp}{extension}");
        }

        private static TextWriter OpenStream(string filename)
        {
            var writer = new StreamWriter(filename);
            return TextWriter.Synchronized(writer);
        }

        private static void WriteLineToFile(TextWriter writer, string line, string filename)
        {
            try
            {
                writer.WriteLine(line);
            }
            catch (IOException exception)
            {
                Logging.Log.Error($"DataMonitor.OnNewDataRequest(): Failed to write to file {filename}: {exception.Message}");
            }
        }
    }
}
