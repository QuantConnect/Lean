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
using System.IO;
using QuantConnect.Interfaces;
using QuantConnect.Logging;

namespace QuantConnect.Data.UniverseSelection
{
    /// <summary>
    /// Data provider wrapper that falls back to the backup universe file ("*.backup"), if any,
    /// when the expected universe file can't be fetched, as a last resort
    /// </summary>
    public class BackupUniverseFileDataProvider : IDataProvider
    {
        // the fallback is retried on every universe refresh for as long as the expected file is missing,
        // so the fallback trace is paced to avoid flooding the logs
        private const int MaximumLogsPerWindow = 30;
        private static readonly TimeSpan LogWindow = TimeSpan.FromMinutes(5);
        private static readonly object LogLock = new();
        private DateTime _logWindowStartUtc;
        private int _logCount;

        private IDataProvider _dataProvider;

        /// <summary>
        /// Event raised each time data fetch is finished (successfully or not)
        /// </summary>
        public event EventHandler<DataProviderNewDataRequestEventArgs> NewDataRequest
        {
            add => _dataProvider?.NewDataRequest += value;
            remove => _dataProvider?.NewDataRequest -= value;
        }

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="dataProvider">The data provider to wrap, can be set later with <see cref="SetDataProvider"/></param>
        public BackupUniverseFileDataProvider(IDataProvider dataProvider = null)
        {
            _dataProvider = dataProvider;
        }

        /// <summary>
        /// Sets the data provider to wrap, forwarding its <see cref="IDataProvider.NewDataRequest"/> events
        /// </summary>
        /// <param name="dataProvider">The data provider to wrap</param>
        public void SetDataProvider(IDataProvider dataProvider)
        {
            _dataProvider = dataProvider;
        }

        /// <summary>
        /// Retrieves data to be used in an algorithm, falling back to the backup universe file, if any,
        /// when the requested file is not available
        /// </summary>
        /// <param name="key">A string representing where the data is stored</param>
        /// <returns>A <see cref="Stream"/> of the data requested, or null if none is available</returns>
        public Stream Fetch(string key)
        {
            var stream = _dataProvider.Fetch(key);
            if (stream != null)
            {
                return stream;
            }

            var backupKey = key + ".backup";
            stream = _dataProvider.Fetch(backupKey);
            if (stream != null && ShouldLog())
            {
                Log.Trace($"BackupUniverseFileDataProvider.Fetch(): universe file '{key}' is not available, " +
                    $"falling back to backup universe file '{backupKey}'");
            }

            return stream;
        }

        /// <summary>
        /// Determines whether the fallback should be logged, allowing up to <see cref="MaximumLogsPerWindow"/> logs per <see cref="LogWindow"/>
        /// </summary>
        private bool ShouldLog()
        {
            lock (LogLock)
            {
                var utcNow = DateTime.UtcNow;
                if (utcNow - _logWindowStartUtc >= LogWindow)
                {
                    _logWindowStartUtc = utcNow;
                    _logCount = 0;
                }

                return _logCount++ < MaximumLogsPerWindow;
            }
        }
    }
}
