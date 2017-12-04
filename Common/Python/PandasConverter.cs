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
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Python
{
    /// <summary>
    /// Collection of methods that converts lists of objects in pandas.DataFrame
    /// </summary>
    public class PandasConverter
    {
        dynamic _pandas;

        /// <summary>
        /// Creates an instance of <see cref="PandasConverter"/>.
        /// </summary>
        /// <param name="pandas"></param>
        public PandasConverter(PyObject pandas = null)
        {
            try
            {
                if (pandas == null)
                {
                    using (Py.GIL())
                    {
                        pandas = Py.Import("pandas");
                    }
                }

                _pandas = pandas;
            }
            catch (PythonException pythonException)
            {
                Logging.Log.Error($"PandasConverter: Failed to import pandas module: {pythonException}");
            }
        }

        /// <summary>
        /// Converts an enumerable of <see cref="Slice"/> in a pandas.DataFrame
        /// </summary>
        /// <param name="data">Enumerable of <see cref="Slice"/></param>
        /// <returns><see cref="PyObject"/> containing a pandas.DataFrame</returns>
        public PyObject GetDataFrame(IEnumerable<Slice> data)
        {
            var maxLevels = 0;
            var sliceDataDict = new Dictionary<Symbol, PandasData>();

            foreach (var slice in data)
            {
                foreach (var key in slice.Keys)
                {
                    PandasData value;
                    if (!sliceDataDict.TryGetValue(key, out value))
                    {
                        sliceDataDict.Add(key, value = new PandasData(slice[key]));
                        maxLevels = Math.Max(maxLevels, value.Levels);
                    }
                    else
                    {
                        value.Add(slice[key]);
                    }
                }
            }

            using (Py.GIL())
            {
                if (sliceDataDict.Count == 0)
                {
                    return _pandas.DataFrame();
                }
                var dataFrames = sliceDataDict.Select(x => x.Value.ToPandasDataFrame(_pandas, maxLevels));
                return _pandas.concat(dataFrames.ToArray());
            }
        }

        /// <summary>
        /// Converts an enumerable of <see cref="IBaseData"/> in a pandas.DataFrame
        /// </summary>
        /// <param name="data">Enumerable of <see cref="Slice"/></param>
        /// <returns><see cref="PyObject"/> containing a pandas.DataFrame</returns>
        public PyObject GetDataFrame<T>(IEnumerable<T> data)
            where T : IBaseData
        {
            PandasData sliceData = null;
            foreach (var datum in data)
            {
                if (sliceData == null)
                {
                    sliceData = new PandasData(datum);
                }
                else
                {
                    sliceData.Add(datum);
                }
            }

            // If sliceData is still null, data is an empty enumerable
            // returns an empty pandas.DataFrame
            if (sliceData == null)
            {
                using (Py.GIL())
                {
                    return _pandas.DataFrame();
                }
            }
            return sliceData.ToPandasDataFrame(_pandas);
        }

        /// <summary>
        /// Converts a dictionary with a list of <see cref="IndicatorDataPoint"/> in a pandas.DataFrame
        /// </summary>
        /// <param name="data">Dictionary with a list of <see cref="IndicatorDataPoint"/></param>
        /// <returns><see cref="PyObject"/> containing a pandas.DataFrame</returns>
        public PyObject GetIndicatorDataFrame(IDictionary<string, List<IndicatorDataPoint>> data)
        {
            using (Py.GIL())
            {
                var pyDict = new PyDict();

                foreach (var kvp in data)
                {
                    var index = new List<DateTime>();
                    var values = new List<double>();

                    foreach (var item in kvp.Value)
                    {
                        index.Add(item.EndTime);
                        values.Add((double)item.Value);
                    }
                    pyDict.SetItem(kvp.Key.ToLower(), _pandas.Series(values, index));
                }

                return _pandas.DataFrame(pyDict, columns: data.Keys.Select(x => x.ToLower()).OrderBy(x => x));
            }
        }

        /// <summary>
        /// Returns a string that represent the current object
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return _pandas == null
                ? "pandas module was not imported."
                : _pandas.Repr();
        }
    }
}