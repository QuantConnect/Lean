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

using System.Collections.Generic;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Util;

namespace QuantConnect
{
    /// <summary>
    /// Monitors data requests and reports on missing data
    /// </summary>
    public class DataMonitor : IDataMonitor
    {
        private readonly HashSet<string> _fetchedData = new();
        private readonly HashSet<string> _missingData = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="DataMonitor"/> class using the specified data provider
        /// </summary>
        public DataMonitor()
        {
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
                    Logging.Log.Debug($"Data from {source} was not fetched");
                }
            }

            return new DataMonitorReport(_fetchedData, _missingData);
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
    }
}
