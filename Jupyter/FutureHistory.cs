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

using Python.Runtime;
using QuantConnect.Data;
using QuantConnect.Python;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Jupyter
{
    /// <summary>
    /// Class to manage information from History Request of Futures
    /// </summary>
    public class FutureHistory
    {
        private IEnumerable<Slice> _data;
        private PandasConverter _converter;
        private PyObject _dataframe;

        /// <summary>
        /// Create a new instance of <see cref="FutureHistory"/>.
        /// </summary>
        /// <param name="data"></param>
        public FutureHistory(IEnumerable<Slice> data)
        {
            _data = data;
            _converter = new PandasConverter();
            _dataframe = _converter.GetDataFrame(_data);
        }

        /// <summary>
        /// Gets all data from the History Request that are written in a pandas.DataFrame
        /// </summary>
        /// <returns></returns>
        public PyObject GetAllData()
        {
            return _dataframe;
        }

        /// <summary>
        /// Gets all expity dates in the future history
        /// </summary>
        /// <returns></returns>
        public PyObject GetExpiryDates()
        {
            var expiry = _data.SelectMany(x => x.FuturesChains.SelectMany(y => y.Value.Contracts.Keys.Select(z => z.ID.Date).Distinct()));
            using (Py.GIL())
            {
                return expiry.Distinct().ToList().ToPython();
            }
        }

        /// <summary>
        /// Returns a string that represent the current object
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return _converter.ToString();
        }
    }
}