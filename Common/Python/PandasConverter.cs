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
using QuantConnect.Data.UniverseSelection;
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
    public class PandasConverter
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
        /// <param name="dataType">Optional type of bars to add to the data frame
        /// If true, the base data items time will be ignored and only the base data collection time will be used in the index</param>
        /// <returns><see cref="PyObject"/> containing a pandas.DataFrame</returns>
        public static PyObject GetDataFrame(IEnumerable<Slice> data, Type dataType = null)
        {
            var joiner = new DataFrameGenerator(dataType);
            joiner.AddData(data);

            return joiner.GenerateDataFrame();
        }

        /// <summary>
        /// Converts an enumerable of <see cref="IBaseData"/> in a pandas.DataFrame
        /// </summary>
        /// <param name="data">Enumerable of <see cref="Slice"/></param>
        /// <param name="symbolOnlyIndex">Whether to make the index only the symbol, without time or any other index levels</param>
        /// <returns><see cref="PyObject"/> containing a pandas.DataFrame</returns>
        /// <remarks>Helper method for testing</remarks>
        public static PyObject GetDataFrame<T>(IEnumerable<T> data, bool symbolOnlyIndex = false)
            where T : ISymbolProvider
        {
            var pandasDataBySymbol = new Dictionary<SecurityIdentifier, PandasData>();
            var maxLevels = 0;
            foreach (var datum in data)
            {
                var pandasData = GetPandasDataValue(pandasDataBySymbol, datum.Symbol, datum, ref maxLevels);
                pandasData.Add(datum);
            }

            if (symbolOnlyIndex)
            {
                return PandasData.ToPandasDataFrame(pandasDataBySymbol.Values);
            }
            return CreateDataFrame(pandasDataBySymbol,
                // Use 2 instead of maxLevels for backwards compatibility
                maxLevels: symbolOnlyIndex ? 1 : 2,
                sort: false,
                // Multiple data frames (one for each symbol) will be concatenated,
                // so make sure rows with missing values only are not filtered out before concatenation
                filterMissingValueColumns: pandasDataBySymbol.Count <= 1);
        }

        /// <summary>
        /// Converts a dictionary with a list of <see cref="IndicatorDataPoint"/> in a pandas.DataFrame
        /// </summary>
        /// <param name="data">Dictionary with a list of <see cref="IndicatorDataPoint"/></param>
        /// <returns><see cref="PyObject"/> containing a pandas.DataFrame</returns>
        public PyObject GetIndicatorDataFrame(IEnumerable<KeyValuePair<string, List<IndicatorDataPoint>>> data)
        {
            using (Py.GIL())
            {
                using var pyDict = new PyDict();

                foreach (var kvp in data)
                {
                    AddSeriesToPyDict(kvp.Key, kvp.Value, pyDict);
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
        /// Create a data frame by concatenated the resulting data frames from the given data
        /// </summary>
        private static PyObject CreateDataFrame(Dictionary<SecurityIdentifier, PandasData> dataBySymbol, int maxLevels = 2, bool sort = true,
            bool filterMissingValueColumns = true)
        {
            using (Py.GIL())
            {
                if (dataBySymbol.Count == 0)
                {
                    return _pandas.DataFrame();
                }

                var dataFrames = dataBySymbol.Select(x => x.Value.ToPandasDataFrame(maxLevels, filterMissingValueColumns));
                var result = ConcatDataFrames(dataFrames, sort: sort, dropna: true);

                foreach (var df in dataFrames)
                {
                    df.Dispose();
                }

                return result;
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
                var dataFramesList = dataFrames.ToList();
                if (dataFramesList.Count == 0)
                {
                    return _pandas.DataFrame();
                }

                using var pyDataFrames = dataFramesList.ToPyListUnSafe();
                using var kwargs = Py.kw("sort", sort);
                PyList pyKeys = null;
                PyList pyNames = null;

                try
                {
                    if (keys != null && names != null)
                    {
                        pyNames = names.ToPyListUnSafe();
                        pyKeys = ConvertConcatKeys(keys);

                        kwargs.SetItem("keys", pyKeys);
                        kwargs.SetItem("names", pyNames);
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
            if (typeof(T).IsAssignableTo(typeof(IEnumerable)) && !typeof(T).IsAssignableTo(typeof(string)))
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
                index.Add(point.EndTime);
                values.Add((double) point.Value);
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

        /// <summary>
        /// Gets the <see cref="PandasData"/> for the given symbol if it exists in the dictionary, otherwise it creates a new instance with the
        /// given base data and adds it to the dictionary
        /// </summary>
        private static PandasData GetPandasDataValue(IDictionary<SecurityIdentifier, PandasData> sliceDataDict, Symbol symbol, object data, ref int maxLevels)
        {
            PandasData value;
            if (!sliceDataDict.TryGetValue(symbol.ID, out value))
            {
                sliceDataDict[symbol.ID] = value = new PandasData(data);
                maxLevels = Math.Max(maxLevels, value.Levels);
            }

            return value;
        }

        /// <summary>
        /// Adds each slice data corresponding to the requested data type to the pandas data dictionary
        /// </summary>
        private void AddSliceDataTypeDataToDict(Slice slice, bool requestedTick, bool requestedTradeBar, bool requestedQuoteBar, IDictionary<SecurityIdentifier, PandasData> sliceDataDict, ref int maxLevels, Type dataType = null)
        {
            HashSet<SecurityIdentifier> _addedData = null;

            for (int i = 0; i < slice.AllData.Count; i++)
            {
                var baseData = slice.AllData[i];
                var value = GetPandasDataValue(sliceDataDict, baseData.Symbol, baseData, ref maxLevels);

                if (value.IsCustomData)
                {
                    value.Add(baseData);
                }
                else
                {
                    var tick = requestedTick ? baseData as Tick : null;
                    if(tick == null)
                    {
                        if (!requestedTradeBar && !requestedQuoteBar && dataType != null && baseData.GetType().IsAssignableTo(dataType))
                        {
                            // support for auxiliary data history requests
                            value.Add(baseData);
                            continue;
                        }

                        // we add both quote and trade bars for each symbol at the same time, because they share the row in the data frame else it will generate 2 rows per series
                        if (requestedTradeBar && requestedQuoteBar)
                        {
                            _addedData ??= new();
                            if (!_addedData.Add(baseData.Symbol.ID))
                            {
                                continue;
                            }
                        }

                        // the slice already has the data organized by symbol so let's take advantage of it using Bars/QuoteBars collections
                        QuoteBar quoteBar = null;
                        var tradeBar = requestedTradeBar ? baseData as TradeBar : null;
                        if (tradeBar != null)
                        {
                            slice.QuoteBars.TryGetValue(tradeBar.Symbol, out quoteBar);
                        }
                        else
                        {
                            quoteBar = requestedQuoteBar ? baseData as QuoteBar : null;
                            if (quoteBar != null)
                            {
                                slice.Bars.TryGetValue(quoteBar.Symbol, out tradeBar);
                            }
                        }
                        value.Add(tradeBar, quoteBar);
                    }
                    else
                    {
                        value.AddTick(tick);
                    }
                }
            }
        }

        private class DataFrameGenerator
        {
            private readonly Type _dataType;
            private readonly bool _requestedTick;
            private readonly bool _requestedQuoteBar;
            private readonly bool _requestedTradeBar;

            private Dictionary<Symbol, PandasData> _pandasData;
            private List<BaseDataCollection> _collections;
            private int _maxLevels;

            public DataFrameGenerator(Type dataType)
            {
                _dataType = dataType;
                // if no data type is requested we check all
                _requestedTick = dataType == null || dataType == typeof(Tick) || dataType == typeof(OpenInterest);
                _requestedTradeBar = dataType == null || dataType == typeof(TradeBar);
                _requestedQuoteBar = dataType == null || dataType == typeof(QuoteBar);
            }

            public void AddData(IEnumerable<Slice> slices)
            {
                HashSet<SecurityIdentifier> addedData = null;

                foreach (var slice in slices)
                {
                    foreach (var data in slice.AllData)
                    {
                        if (data is BaseDataCollection collection)
                        {
                            _collections ??= new();
                            _collections.Add(collection);
                            continue;
                        }

                        _pandasData ??= new();
                        if (!_pandasData.TryGetValue(data.Symbol, out var pandasData))
                        {
                            pandasData = new PandasData(data);
                            _pandasData[data.Symbol] = pandasData;
                            _maxLevels = Math.Max(_maxLevels, pandasData.Levels);
                        }

                        if (pandasData.IsCustomData)
                        {
                            pandasData.Add(data);
                        }
                        else
                        {
                            var tick = _requestedTick ? data as Tick : null;
                            if (tick == null)
                            {
                                if (!_requestedTradeBar && !_requestedQuoteBar && _dataType != null && data.GetType().IsAssignableTo(_dataType))
                                {
                                    // support for auxiliary data history requests
                                    pandasData.Add(data);
                                    continue;
                                }

                                // we add both quote and trade bars for each symbol at the same time, because they share the row in the data frame else it will generate 2 rows per series
                                if (_requestedTradeBar && _requestedQuoteBar)
                                {
                                    addedData ??= new();
                                    if (!addedData.Add(data.Symbol.ID))
                                    {
                                        continue;
                                    }
                                }

                                // the slice already has the data organized by symbol so let's take advantage of it using Bars/QuoteBars collections
                                QuoteBar quoteBar;
                                var tradeBar = _requestedTradeBar ? data as TradeBar : null;
                                if (tradeBar != null)
                                {
                                    slice.QuoteBars.TryGetValue(tradeBar.Symbol, out quoteBar);
                                }
                                else
                                {
                                    quoteBar = _requestedQuoteBar ? data as QuoteBar : null;
                                    if (quoteBar != null)
                                    {
                                        slice.Bars.TryGetValue(quoteBar.Symbol, out tradeBar);
                                    }
                                }
                                pandasData.Add(tradeBar, quoteBar);
                            }
                            else
                            {
                                pandasData.AddTick(tick);
                            }
                        }
                    }

                    addedData?.Clear();
                }
            }

            public PyObject GenerateDataFrame(bool sort = true, bool filterMissingValueColumns = true)
            {
                using var _ = Py.GIL();

                var pandasDataDataFrames = GetPandasDataDataFrames(filterMissingValueColumns).ToList();
                var collectionsDataFrames = GetCollectionsDataFrames().ToList();

                if (collectionsDataFrames.Count == 0)
                {
                    return ConcatDataFrames(pandasDataDataFrames.Select(x => x.Item2), sort, dropna: true);
                }

                var dataFrames = collectionsDataFrames.Select(x => x.Item3).Concat(pandasDataDataFrames.Select(x => x.Item2));
                var keys = collectionsDataFrames
                    .Select(x => new object[] { x.Item1, x.Item2 })
                    .Concat(pandasDataDataFrames.Select(x => new object[] { x.Item2, DateTime.MinValue }));
                var names = new[] { "collection_symbol", "time" }; // TODO: Make it a static property

                return ConcatDataFrames(dataFrames, keys, names, sort, dropna: true);
            }

            private IEnumerable<(Symbol, PyObject)> GetPandasDataDataFrames(bool filterMissingValueColumns)
            {
                if (_pandasData is null || _pandasData.Count == 0)
                {
                    yield break;
                }

                foreach (var (symbol, data) in _pandasData)
                {
                    yield return (symbol, data.ToPandasDataFrame(_maxLevels, filterMissingValueColumns));
                }
            }

            private IEnumerable<(Symbol, DateTime, PyObject)> GetCollectionsDataFrames()
            {
                if (_collections is null || _collections.Count == 0)
                {
                    yield break;
                }

                foreach (var collection in _collections)
                {
                    var dataFrame = GetDataFrame(collection.Data, true);
                    yield return (collection.Symbol, collection.EndTime, dataFrame);
                }
            }
        }
    }
}
