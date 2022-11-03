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
using QuantConnect.Interfaces;
using QuantConnect.Util;

namespace QuantConnect
{
    /// <summary>
    /// Monitors data requests and reports on missing data
    /// </summary>
    public class DataMonitor : IDataMonitor
    {
        private readonly HashSet<string> _succeededDataRequests = new();
        private readonly HashSet<string> _failedDataRequests = new();

        private readonly object _setsLock = new();
        
        private bool _initialized;
        private bool _exited;

        private readonly List<double> _requestRates = new();
        private int _prevRequestsCount;
        private DateTime _lastRequestRateCalculationTime;

        private Thread _requestRateCalculationThread;
        private CancellationTokenSource _cancellationTokenSource;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataMonitor"/> class
        /// </summary>
        public DataMonitor()
        {
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
                while (!_cancellationTokenSource.Token.WaitHandle.WaitOne(500))
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

            _requestRateCalculationThread.StopSafely(TimeSpan.FromSeconds(1), _cancellationTokenSource);
            _cancellationTokenSource.DisposeSafely();

            lock (_setsLock)
            {
                _succeededDataRequests.Clear();
                _failedDataRequests.Clear();
                _requestRates.Clear();
                _prevRequestsCount = 0;
                _initialized = false;
                _exited = true;
            }
        }

        /// <summary>
        /// Generates a report on missing data
        /// </summary>
        public DataMonitorReport GenerateReport()
        {
            return new DataMonitorReport(_succeededDataRequests, _failedDataRequests, _requestRates);
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

            lock (_setsLock)
            {
                Initialize();

                if (!e.Path.StartsWith(Globals.DataFolder, StringComparison.InvariantCulture))
                {
                    Logging.Log.Error($"DataMonitor.OnNewDataRequest(): Invalid data path '{e.Path}'. The path is not under the data folder '{Globals.DataFolder}'.");
                    return;
                }

                var path = e.Path.Substring(Globals.DataFolder.Length);

                if (e.Succeded)
                {
                    _succeededDataRequests.Add(path);
                }
                else
                {
                    _failedDataRequests.Add(path);

                    if (Logging.Log.DebuggingEnabled)
                    {
                        Logging.Log.Debug($"DataMonitor.GenerateReport(): Data from {path} could not be fetched");
                    }
                }
            }
        }

        private void ComputeFileRequestFrequency()
        {
            lock (_setsLock)
            {
                var requestsCount = _succeededDataRequests.Count + _failedDataRequests.Count;

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
        }
    }
}
