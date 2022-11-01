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
using System.Threading;
using System.Threading.Tasks;
using QuantConnect.Interfaces;
using QuantConnect.Util;

namespace QuantConnect
{
    /// <summary>
    /// Monitors data requests and reports on missing data
    /// </summary>
    public class DataMonitor : IDataMonitor
    {
        private readonly ConcurrentSet<string> _fetchedData = new();
        private readonly ConcurrentSet<string> _missingData = new();

        private CancellationTokenSource _cancellationTokenSource;
        private bool _initialized;

        private readonly List<double> _dataRequestRates = new();
        private int _prevDataRequestsCount;
        private DateTime _lastDataRequestRateCalculationTime;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataMonitor"/> class
        /// </summary>
        public DataMonitor()
        {
        }

        /// <summary>
        /// Initializes the <see cref="DataMonitor"/> instance
        /// </summary>
        public void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;

            _lastDataRequestRateCalculationTime = DateTime.UtcNow;

            Task.Run(async () => {
                while (!token.IsCancellationRequested)
                {
                    await Task.Delay(Time.GetSecondUnevenWait(1000), token).ConfigureAwait(false);
                    ComputeFileRequestFrequency();
                }
            }, token);
        }

        /// <summary>
        /// Terminates the data monitor generating a final report
        /// </summary>
        public void Exit()
        {
            if (!_initialized)
            {
                return;
            }

            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.DisposeSafely();
            _fetchedData.Clear();
            _missingData.Clear();
            _dataRequestRates.Clear();
            _prevDataRequestsCount = 0;
            _initialized = false;
        }

        /// <summary>
        /// Generates a report on missing data
        /// </summary>
        public DataMonitorReport GenerateReport()
        {
            if (Logging.Log.DebuggingEnabled)
            {
                foreach (string path in _missingData)
                {
                    string source = path;
                    if (LeanData.TryParsePath(source, out var symbol, out var date, out var resolution, out var tickType, out var dataType))
                    {
                        source = $"{symbol}|{date:yyyyMMdd}|{resolution}|{tickType}|{dataType.Name}";
                    }
                    Logging.Log.Debug($"DataMonitor.GenerateReport(): Data from {source} could not be fetched");
                }
            }

            return new DataMonitorReport(_fetchedData, _missingData, _dataRequestRates);
        }
        
        /// <summary>
        /// Event handler for the <see cref="IDataProvider.NewDataRequest"/> event
        /// </summary>
        public void OnNewDataRequest(object sender, DataProviderNewDataRequestEventArgs e)
        {
            if (e.Succeded)
            {
                _fetchedData.Add(e.Path);
            }
            else
            {
                _missingData.Add(e.Path);
            }
        }

        private void ComputeFileRequestFrequency()
        {
            var requestsCount = _fetchedData.Count + _missingData.Count;
            var requestsCountDelta = requestsCount - _prevDataRequestsCount;
            var now = DateTime.UtcNow;
            var timeDelta = now - _lastDataRequestRateCalculationTime;

            _dataRequestRates.Add(requestsCountDelta / timeDelta.TotalSeconds);
            _prevDataRequestsCount = requestsCount;
            _lastDataRequestRateCalculationTime = now;
        }
    }
}
