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

using NodaTime;
using Python.Runtime;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Packets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Python.Wrappers
{
    /// <summary>
    /// Wrapper for an <see cref = "IHistoryProvider"/> instance created in Python.
    /// All calls to python should be inside a "using (Py.GIL()) {/* Your code here */}" block.
    /// </summary>
    public class HistoryProviderPythonWrapper : IHistoryProvider
    {
        private IHistoryProvider _historyProvider;

        /// <summary>
        /// <see cref = "HistoryProviderPythonWrapper"/> constructor.
        /// Wraps the <see cref = "IHistoryProvider"/> object.  
        /// </summary>
        /// <param name="historyProvider"><see cref = "IHistoryProvider"/> object to be wrapped</param>
        public HistoryProviderPythonWrapper(IHistoryProvider historyProvider)
        {
            _historyProvider = historyProvider;
        }

        /// <summary>
        /// Wrapper for <see cref = "IHistoryProvider.DataPointCount" /> in Python
        /// </summary>
        public int DataPointCount
        {
            get
            {
                using (Py.GIL())
                {
                    return _historyProvider.DataPointCount;
                }
            }
        }

        /// <summary>
        /// Wrapper for <see cref = "IHistoryProvider.GetHistory" /> in Python
        /// </summary>
        /// <param name="requests">The historical data requests</param>
        /// <param name="sliceTimeZone">The time zone used when time stamping the slice instances</param>
        /// <returns>An enumerable of the slices of data covering the span specified in each request</returns>
        public IEnumerable<Slice> GetHistory(IEnumerable<Data.HistoryRequest> requests, DateTimeZone sliceTimeZone)
        {
            using (Py.GIL())
            {
                return _historyProvider.GetHistory(requests, sliceTimeZone).ToList();
            }
        }

        /// <summary>
        /// Wrapper for <see cref = "IHistoryProvider.Initialize" /> in Python
        /// </summary>
        /// <param name="job">The job</param>
        /// <param name="dataCacheProvider">Cache system for the history request</param>
        /// <param name="mapFileProvider">Provider used to get a map file resolver to handle equity mapping</param>
        /// <param name="factorFileProvider">Provider used to get factor files to handle equity price scaling</param>
        /// <param name="dataProvider">Provider used to get data when it is not present on disk</param>
        /// <param name="statusUpdate">Function used to send status updates</param>
        public void Initialize(AlgorithmNodePacket job, IDataProvider dataProvider, IDataCacheProvider dataCacheProvider, IMapFileProvider mapFileProvider, IFactorFileProvider factorFileProvider, Action<int> statusUpdate)
        {
            using (Py.GIL())
            {
                _historyProvider.Initialize(job, dataProvider, dataCacheProvider, mapFileProvider, factorFileProvider, statusUpdate);
            }
        }
    }
}