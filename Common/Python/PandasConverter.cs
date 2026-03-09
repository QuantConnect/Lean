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
using QuantConnect.Indicators;
using QuantConnect.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Python
{
    /// <summary>
    /// Collection of methods that converts lists of objects in pandas.DataFrame
    /// </summary>
    public partial class PandasConverter
    {
        private static dynamic _pandas;
        private static PyObject _concat;

        /// <summary>
        /// Initializes the <see cref="PandasConverter"/> class
        /// </summary>
        static PandasConverter()
        {
            using (Py.GIL())
            {
                var pandas = Py.Import("pandas");
                _pandas = pandas;
                // keep it so we don't need to ask for it each time
                _concat = pandas.GetAttr("concat");
            }
        }

        /// <summary>
        /// Converts an enumerable of <see cref="Slice"/> in a pandas.DataFrame
        /// </summary>
        /// <param name="data">Enumerable of <see cref="Slice"/></param>
        /// <param name="flatten">Whether to flatten collections into rows and columns</param>
        /// <param name="dataType">Optional type of bars to add to the data frame
        /// If true, the base data items time will be ignored and only the base data collection time will be used in the index</param>
        /// <returns><see cref="PyObject"/> containing a pandas.DataFrame</returns>
        public PyObject GetDataFrame(IEnumerable<Slice> data, bool flatten = false, Type dataType = null)
        {
            var generator = new DataFrameGenerator(data, flatten, dataType);
            return generator.GenerateDataFrame();
        }

        /// <summary>
        /// Converts an enumerable of <see cref="IBaseData"/> in a pandas.DataFrame
        /// </summary>
        /// <param name="data">Enumerable of <see cref="Slice"/></param>
        /// <param name="symbolOnlyIndex">Whether to make the index only the symbol, without time or any other index levels</param>
        /// <param name="forceMultiValueSymbol">Useful when the data contains points for multiple symbols.
        /// If false and <paramref name="symbolOnlyIndex"/> is true, it will assume there is a single point for each symbol,
        /// and will apply performance improvements for the data frame generation.</param>
        /// <param name="flatten">Whether to flatten collections into rows and columns</param>
        /// <returns><see cref="PyObject"/> containing a pandas.DataFrame</returns>
        /// <remarks>Helper method for testing</remarks>
        public PyObject GetDataFrame<T>(IEnumerable<T> data, bool symbolOnlyIndex = false, bool forceMultiValueSymbol = false, bool flatten = false)
            where T : ISymbolProvider
        {
            var generator = new DataFrameGenerator<T>(data, flatten);
            return generator.GenerateDataFrame(
                // Use 2 instead of maxLevels for backwards compatibility
                levels: symbolOnlyIndex ? 1 : 2,
                sort: false,
                symbolOnlyIndex: symbolOnlyIndex,
                forceMultiValueSymbol: forceMultiValueSymbol);
        }

        /// <summary>
        /// Converts a dictionary with a list of <see cref="IndicatorDataPoint"/> in a pandas.DataFrame
        /// </summary>
        /// <param name="data">Dictionary with a list of <see cref="IndicatorDataPoint"/></param>
        /// <param name="extraData">Optional dynamic properties to include in the DataFrame.</param>
        /// <returns><see cref="PyObject"/> containing a pandas.DataFrame</returns>
        public PyObject GetIndicatorDataFrame(IEnumerable<KeyValuePair<string, List<IndicatorDataPoint>>> data, IEnumerable<KeyValuePair<string, List<(DateTime, object)>>> extraData = null)
        {
            using (Py.GIL())
            {
                using var pyDict = new PyDict();

                foreach (var kvp in data)
                {
                    AddSeriesToPyDict(kvp.Key, kvp.Value, pyDict);
                }

                if (extraData != null)
                {
                    foreach (var kvp in extraData)
                    {
                        AddDynamicSeriesToPyDict(kvp.Key, kvp.Value, pyDict);
                    }
                }

                return MakeIndicatorDataFrame(pyDict);
            }
        }

        /// <summary>
        /// Converts a dictionary with a list of <see cref="IndicatorDataPoint"/> in a pandas.DataFrame
        /// </summary>
        /// <param name="data"><see cref="PyObject"/> that should be a dictionary (convertible to PyDict) of string to list of <see cref="IndicatorDataPoint"/></param>
        /// <returns><see cref="PyObject"/> containing a pandas.DataFrame</returns>
        public PyObject GetIndicatorDataFrame(PyObject data)
        {
            using (Py.GIL())
            {
                using var inputPythonType = data.GetPythonType();
                var inputTypeStr = inputPythonType.ToString();
                var targetTypeStr = nameof(PyDict);
                PyObject currentKvp = null;

                try
                {
                    using var pyDictData = new PyDict(data);
                    using var seriesPyDict = new PyDict();

                    targetTypeStr = $"{nameof(String)}: {nameof(List<IndicatorDataPoint>)}";

                    foreach (var kvp in pyDictData.Items())
                    {
                        currentKvp = kvp;
                        AddSeriesToPyDict(kvp[0].As<string>(), kvp[1].As<List<IndicatorDataPoint>>(), seriesPyDict);
                    }

                    return MakeIndicatorDataFrame(seriesPyDict);
                }
                catch (Exception e)
                {
                    if (currentKvp != null)
                    {
                        inputTypeStr = $"{currentKvp[0].GetPythonType()}: {currentKvp[1].GetPythonType()}";
                    }

                    throw new ArgumentException(Messages.PandasConverter.ConvertToDictionaryFailed(inputTypeStr, targetTypeStr, e.Message), e);
                }
            }
        }

        /// <summary>
        /// Returns a string that represent the current object
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (_pandas == null)
            {
                return Messages.PandasConverter.PandasModuleNotImported;
            }

            using (Py.GIL())
            {
                return _pandas.Repr();
            }
        }

        /// <summary>
        /// Concatenates multiple data frames
        /// </summary>
        /// <param name="dataFrames">The data frames to concatenate</param>
        /// <param name="keys">
        /// Optional new keys for a new multi-index level that would be added
        /// to index each individual data frame in the resulting one
        /// </param>
        /// <param name="names">The optional names of the new index level (and the existing ones if they need to be changed)</param>
        /// <param name="sort">Whether to sort the resulting data frame</param>
        /// <param name="dropna">Whether to drop columns containing NA values only (Nan, None, etc)</param>
        /// <returns>A new data frame result from concatenating the input</returns>
        public static PyObject ConcatDataFrames<T>(IEnumerable<PyObject> dataFrames, IEnumerable<T> keys, IEnumerable<string> names,
            bool sort = true, bool dropna = true)
        {
            using (Py.GIL())
            {
                using var pyDataFrames = dataFrames.ToPyListUnSafe();

                if (pyDataFrames.Length() == 0)
                {
                    return _pandas.DataFrame();
                }

                using var kwargs = Py.kw("sort", sort);
                PyList pyKeys = null;
                PyList pyNames = null;

                try
                {
                    if (keys != null && names != null)
                    {
                        pyNames = names.ToPyListUnSafe();
                        pyKeys = ConvertConcatKeys(keys);
                        using var pyFalse = false.ToPython();

                        kwargs.SetItem("keys", pyKeys);
                        kwargs.SetItem("names", pyNames);
                        kwargs.SetItem("copy", pyFalse);
                    }

                    var result = _concat.Invoke(new[] { pyDataFrames }, kwargs);

                    // Drop columns with only NaN or None values
                    if (dropna)
                    {
                        using var dropnaKwargs = Py.kw("axis", 1, "inplace", true, "how", "all");
                        result.GetAttr("dropna").Invoke(Array.Empty<PyObject>(), dropnaKwargs);
                    }

                    return result;
                }
                finally
                {
                    pyKeys?.Dispose();
                    pyNames?.Dispose();
                }
            }
        }

        public static PyObject ConcatDataFrames(IEnumerable<PyObject> dataFrames, bool sort = true, bool dropna = true)
        {
            return ConcatDataFrames<string>(dataFrames, null, null, sort, dropna);
        }

        /// <summary>
        /// Creates the list of keys required for the pd.concat method, making sure that if the items are enumerables,
        /// they are converted to Python tuples so that they are used as levels for a multi index
        /// </summary>
        private static PyList ConvertConcatKeys(IEnumerable<IEnumerable<object>> keys)
        {
            var keyTuples = keys.Select(x => new PyTuple(x.Select(y => y.ToPython()).ToArray()));
            try
            {
                return keyTuples.ToPyListUnSafe();
            }
            finally
            {
                foreach (var tuple in keyTuples)
                {
                    foreach (var x in tuple)
                    {
                        x.DisposeSafely();
                    }
                    tuple.DisposeSafely();
                }
            }
        }

        private static PyList ConvertConcatKeys<T>(IEnumerable<T> keys)
        {
            if ((typeof(T).IsAssignableTo(typeof(IEnumerable)) && !typeof(T).IsAssignableTo(typeof(string))))
            {
                return ConvertConcatKeys(keys.Cast<IEnumerable<object>>());
            }

            return keys.ToPyListUnSafe();
        }

        /// <summary>
        /// Creates a series from a list of <see cref="IndicatorDataPoint"/> and adds it to the
        /// <see cref="PyDict"/> as the value of the given <paramref name="key"/>
        /// </summary>
        /// <param name="key">Key to insert in the <see cref="PyDict"/></param>
        /// <param name="points">List of <see cref="IndicatorDataPoint"/> that will make up the resulting series</param>
        /// <param name="pyDict"><see cref="PyDict"/> where the resulting key-value pair will be inserted into</param>
        private void AddSeriesToPyDict(string key, List<IndicatorDataPoint> points, PyDict pyDict)
        {
            var index = new List<DateTime>();
            var values = new List<double>();

            foreach (var point in points)
            {
                if (point.EndTime != default)
                {
                    index.Add(point.EndTime);
                    values.Add((double)point.Value);
                }
            }
            pyDict.SetItem(key.ToLowerInvariant(), _pandas.Series(values, index));
        }

        /// <summary>
        /// Builds a time‑indexed pandas <see cref="Series"/> from a collection of 
        /// heterogeneous data (numbers, enums, strings, etc.) and inserts it into the
        /// specified <see cref="PyDict"/> under the given <paramref name="key"/>.
        /// </summary>
        /// <param name="key">Key to insert in the <see cref="PyDict"/></param>
        /// <param name="entries">A list of tuples whose first item is the timestamp and whose second item is the value associated with that timestamp.</param>
        /// <param name="pyDict"><see cref="PyDict"/> where the resulting key-value pair will be inserted into</param>
        private void AddDynamicSeriesToPyDict(string key, List<(DateTime Timestamp, object Value)> entries, PyDict pyDict)
        {
            var index = new List<DateTime>();
            var values = new List<object>();

            foreach (var (timestamp, value) in entries)
            {
                if (timestamp != default)
                {
                    index.Add(timestamp);
                    values.Add(value is Enum e ? e.ToString() : value);
                }
            }
            pyDict.SetItem(key.ToLowerInvariant(), _pandas.Series(values, index));
        }

        /// <summary>
        /// Converts a <see cref="PyDict"/> of string to pandas.Series in a pandas.DataFrame
        /// </summary>
        /// <param name="pyDict"><see cref="PyDict"/> of string to pandas.Series</param>
        /// <returns><see cref="PyObject"/> containing a pandas.DataFrame</returns>
        private PyObject MakeIndicatorDataFrame(PyDict pyDict)
        {
            return _pandas.DataFrame(pyDict, columns: pyDict.Keys().Select(x => x.As<string>().ToLowerInvariant()).OrderBy(x => x));
        }
    }
}
