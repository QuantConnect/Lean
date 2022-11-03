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

namespace QuantConnect
{
    /// <summary>
    /// Monitors data requests and reports on missing data
    /// </summary>
    public class DataMonitor : IDataMonitor
    {
        private bool _initialized;
        private bool _exited;

        private readonly TextWriter _succeededDataRequestsWriter;
        private readonly TextWriter _failedDataRequestsWriter;

        private long _succeededDataRequestsCount;
        private long _failedDataRequestsCount;

        private long _succeededUniverseDataRequestsCount;
        private long _failedUniverseDataRequestsCount;

        private readonly List<double> _requestRates = new();
        private long _prevRequestsCount;
        private DateTime _lastRequestRateCalculationTime;

        private Thread _requestRateCalculationThread;
        private CancellationTokenSource _cancellationTokenSource;

        /// <summary>
        /// Directory location to store results
        /// </summary>
        protected string ResultsDestinationFolder { get; set; }

        /// <summary>
        /// The algorithm id, which is the algorithm name
        /// </summary>
        protected string AlgorithmId { get; set; }

        /// <summary>
        /// Name of the file to store succeeded data requests
        /// </summary>
        protected string SucceededDataRequestsFileName { get; set; }

        /// <summary>
        /// Name of the file to store failed data requests
        /// </summary>
        protected string FailedDataRequestsFileName { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataMonitor"/> class
        /// </summary>
        public DataMonitor()
        {
            ResultsDestinationFolder = Config.Get("results-destination-folder", Directory.GetCurrentDirectory());
            AlgorithmId = Config.Get("algorithm-id", Config.Get("algorithm-type-name"));
            SucceededDataRequestsFileName = GetResultsPath("succeeded-data-requests.txt");
            FailedDataRequestsFileName = GetResultsPath("failed-data-requests.txt");

            _succeededDataRequestsWriter = OpenStream(SucceededDataRequestsFileName);            
            _failedDataRequestsWriter = OpenStream(FailedDataRequestsFileName);
        }

        private TextWriter OpenStream(string filename)
        {
            var writer = new StreamWriter(filename);
            return TextWriter.Synchronized(writer);
        }

        /// <summary>
        /// Initializes the <see cref="DataMonitor"/> instance
        /// </summary>
        private void Initialize()
        {
            if (_initialized || _exited)
            {
                return;
            }

            _initialized = true;
            _cancellationTokenSource = new CancellationTokenSource();

            _requestRateCalculationThread = new Thread(() =>
            {
                while (!_cancellationTokenSource.Token.WaitHandle.WaitOne(3000))
                {
                    ComputeFileRequestFrequency();
                }
            }) { IsBackground = true };
            _requestRateCalculationThread.Start();
        }

        /// <summary>
        /// Terminates the data monitor generating a final report
        /// </summary>
        public void Exit()
        {
            if (!_initialized || _exited)
            {
                return;
            }

            _requestRateCalculationThread.StopSafely(TimeSpan.FromSeconds(5), _cancellationTokenSource);
            _succeededDataRequestsWriter.Close();
            _failedDataRequestsWriter.Close();
            _initialized = false;
            _exited = true;

            StoreDataMonitorReport(GenerateReport());
            
            _succeededDataRequestsCount = 0;
            _failedDataRequestsCount = 0;
            _requestRates.Clear();
            _prevRequestsCount = 0;
            _lastRequestRateCalculationTime = default;
        }

        /// <summary>
        /// Generates a report on missing data
        /// </summary>
        public DataMonitorReport GenerateReport()
        {
            return new DataMonitorReport(_succeededDataRequestsCount, 
                _failedDataRequestsCount, 
                _succeededUniverseDataRequestsCount, 
                _failedUniverseDataRequestsCount, 
                _requestRates);
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

            if (!e.Path.StartsWith(Globals.DataFolder, StringComparison.InvariantCulture))
            {
                Logging.Log.Error($"DataMonitor.OnNewDataRequest(): Invalid data path '{e.Path}'. The path is not under the data folder '{Globals.DataFolder}'.");
                return;
            }

            var path = e.Path.Substring(Globals.DataFolder.Length);
            var isUniverseData = path.Contains("coarse", StringComparison.OrdinalIgnoreCase) || path.Contains("universe", StringComparison.OrdinalIgnoreCase);

            if (e.Succeded)
            {
                if (TryWriteLineToFile(_succeededDataRequestsWriter, path, SucceededDataRequestsFileName))
                {
                    Interlocked.Increment(ref _succeededDataRequestsCount);
                    if (isUniverseData)
                    {
                        Interlocked.Increment(ref _succeededUniverseDataRequestsCount);
                    }
                }
            }
            else if (TryWriteLineToFile(_failedDataRequestsWriter, path, FailedDataRequestsFileName))
            {
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

        private static bool TryWriteLineToFile(TextWriter writer, string line, string filename)
        {
            try
            {
                writer.WriteLine(line);
            }
            catch (IOException exception)
            {
                Logging.Log.Error($"DataMonitor.OnNewDataRequest(): Failed to write to file {filename}: {exception.Message}");
                return false;
            }
            
            return true;
        }

        private void ComputeFileRequestFrequency()
        {
            var requestsCount = _succeededDataRequestsCount + _failedDataRequestsCount;

            if (_lastRequestRateCalculationTime == default)
            {
                _lastRequestRateCalculationTime = DateTime.UtcNow;
                _prevRequestsCount = requestsCount;
                return;
            }

            var requestsCountDelta = requestsCount - _prevRequestsCount;
            var now = DateTime.UtcNow;
            var timeDelta = now - _lastRequestRateCalculationTime;

            _requestRates.Add(requestsCountDelta / timeDelta.TotalSeconds);
            _prevRequestsCount = requestsCount;
            _lastRequestRateCalculationTime = now;
        }
        
        private string GetResultsPath(string filename)
        {
            return Path.Combine(ResultsDestinationFolder, $"{AlgorithmId}-{filename}");
        }

        /// <summary>
        /// Stores the data monitor report
        /// </summary>
        /// <param name="report">The data monitor report to be stored<param>
        protected virtual void StoreDataMonitorReport(DataMonitorReport report)
        {
            if (report == null)
            {
                return;
            }

            var path = GetResultsPath("data-monitor-report.json");
            var data = JsonConvert.SerializeObject(report, Formatting.Indented);
            File.WriteAllText(path, data);
        }

        public void Dispose()
        {
            _succeededDataRequestsWriter.Close();
            _succeededDataRequestsWriter.DisposeSafely();
            _failedDataRequestsWriter.Close();
            _failedDataRequestsWriter.DisposeSafely();
            _cancellationTokenSource?.DisposeSafely();
        }
    }
}
