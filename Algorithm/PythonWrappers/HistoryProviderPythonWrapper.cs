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

namespace QuantConnect.Algorithm
{
    /// <summary>
    /// Wrapper for an IHistoryProvider instance created in Python.
    /// All calls to python should be inside a "using (Py.GIL()) {/* Your code here */}" block.
    /// </summary>
    public class HistoryProviderPythonWrapper : IHistoryProvider
    {
        IHistoryProvider _historyProvider;

        public HistoryProviderPythonWrapper(IHistoryProvider historyProvider)
        {
            _historyProvider = historyProvider;
        }

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

        public IEnumerable<Slice> GetHistory(IEnumerable<Data.HistoryRequest> requests, DateTimeZone sliceTimeZone)
        {
            using (Py.GIL())
            {
                return _historyProvider.GetHistory(requests, sliceTimeZone).ToList();
            }
        }

        public void Initialize(AlgorithmNodePacket job, IMapFileProvider mapFileProvider, IFactorFileProvider factorFileProvider, IDataFileProvider dataFileProvider, Action<int> statusUpdate)
        {
            using (Py.GIL())
            {
                _historyProvider.Initialize(job, mapFileProvider, factorFileProvider, dataFileProvider, statusUpdate);
            }
        }
    }
}