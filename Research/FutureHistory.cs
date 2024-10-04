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
using System.Linq;
using Python.Runtime;
using QuantConnect.Data;
using QuantConnect.Python;
using System.Collections.Generic;

namespace QuantConnect.Research
{
    /// <summary>
    /// Class to manage information from History Request of Futures
    /// </summary>
    public class FutureHistory : DataHistory<Slice>
    {
        /// <summary>
        /// Create a new instance of <see cref="FutureHistory"/>.
        /// </summary>
        /// <param name="data"></param>
        public FutureHistory(IEnumerable<Slice> data)
            : base(data, new Lazy<PyObject>(() => new PandasConverter().GetDataFrame(data), isThreadSafe: false))
        {
        }

        /// <summary>
        /// Gets all data from the History Request that are written in a pandas.DataFrame
        /// </summary>
        [Obsolete("Please use the 'DataFrame' property")]
        public PyObject GetAllData() => DataFrame;

        /// <summary>
        /// Gets all expity dates in the future history
        /// </summary>
        /// <returns></returns>
        public PyObject GetExpiryDates()
        {
            var expiry = Data.SelectMany(x => x.FuturesChains.SelectMany(y => y.Value.Contracts.Keys.Select(z => z.ID.Date).Distinct()));
            using (Py.GIL())
            {
                return expiry.Distinct().ToList().ToPython();
            }
        }
    }
}
