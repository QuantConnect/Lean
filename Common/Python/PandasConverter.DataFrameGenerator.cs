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
using QuantConnect.Util;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Python
{
    public partial class PandasConverter
    {
        /// <summary>
        /// Helper class to generate data frames from slices
        /// </summary>
        private class DataFrameGenerator
        {
            private static readonly string[] MultiBaseDataCollectionDataFrameNames = new[] { "collection_symbol", "time" };
            private static readonly string[] SingleBaseDataCollectionDataFrameNames = new[] { "time" };

            private readonly Type _dataType;
            private readonly bool _requestedTick;
            private readonly bool _requestedQuoteBar;
            private readonly bool _requestedTradeBar;
            private readonly bool _timeAsColumn;

            /// <summary>
            /// PandasData instances for each symbol. Does not hold BaseDataCollection instances.
            /// </summary>
            private Dictionary<Symbol, PandasData> _pandasData;
            private List<BaseDataCollection> _collections;

            private int _maxLevels;
            private bool _shouldUseSymbolOnlyIndex;

            protected DataFrameGenerator(Type dataType = null, bool timeAsColumn = false)
            {
                _dataType = dataType;
                // if no data type is requested we check all
                _requestedTick = dataType == null || dataType == typeof(Tick) || dataType == typeof(OpenInterest);
                _requestedTradeBar = dataType == null || dataType == typeof(TradeBar);
                _requestedQuoteBar = dataType == null || dataType == typeof(QuoteBar);
                _timeAsColumn = timeAsColumn;
            }

            public DataFrameGenerator(IEnumerable<Slice> slices, Type dataType = null)
                : this(dataType)
            {
                AddData(slices);
            }

            /// <summary>
            /// Extracts the data from the slices and prepares it for DataFrame generation.
            /// If the slices contain BaseDataCollection instances, they are added to the collections list for proper handling.
            /// For the rest of the data, PandasData instances are created for each symbol and the data is added to them for later processing.
            /// </summary>
            protected void AddData(IEnumerable<Slice> slices)
            {
                HashSet<SecurityIdentifier> addedData = null;

                foreach (var slice in slices)
                {
                    foreach (var data in slice.AllData)
                    {
                        if (data is BaseDataCollection collection)
                        {
                            AddCollection(collection);
                            continue;
                        }

                        var pandasData = GetPandasData(data);
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

            /// <summary>
            /// Adds a collection of data and prepares it for DataFrame generation.
            /// If the collection holds BaseDataCollection instances, they are added to the collections list for proper handling.
            /// For the rest of the data, PandasData instances are created for each symbol and the data is added to them for later processing.
            /// </summary>
            protected void AddData<T>(IEnumerable<T> data)
                where T : ISymbolProvider
            {
                var type = typeof(T);

                if (type.IsAssignableTo(typeof(BaseDataCollection)))
                {
                    foreach (var collection in data)
                    {
                        AddCollection(collection as BaseDataCollection);
                    }
                }
                else
                {
                    Symbol prevSymbol = null;
                    PandasData prevPandasData = null;
                    foreach (var item in data)
                    {
                        var pandasData = prevSymbol != null && item.Symbol == prevSymbol ? prevPandasData : GetPandasData(item);
                        pandasData.Add(item);
                        prevSymbol = item.Symbol;
                        prevPandasData = pandasData;
                    }

                    // Multiple symbols detected, use symbol only indexing for performance reasons
                    if (_pandasData != null && _pandasData.Count > 1)
                    {
                        _shouldUseSymbolOnlyIndex = true;
                    }
                }
            }

            /// <summary>
            /// Generates the data frame
            /// </summary>
            /// <param name="levels">The number of level the index should have. If not provided, it will be inferred from the data</param>
            /// <param name="sort">Whether to sort the data frames on concatenation</param>
            /// <param name="filterMissingValueColumns">Whether to filter missing values. See <see cref="PandasData.ToPandasDataFrame(int, bool)"/></param>
            /// <param name="symbolOnlyIndex">Whether to assume the data has multiple symbols and also one data point per symbol.
            /// This is used for performance purposes</param>
            /// <param name="forceMultiValueSymbol">Useful when the data contains points for multiple symbols.
            /// If false and <paramref name="symbolOnlyIndex"/> is true, it will assume there is a single point for each symbol,
            /// and will apply performance improvements for the data frame generation.</param>
            public PyObject GenerateDataFrame(int? levels = null, bool sort = true, bool filterMissingValueColumns = true,
                bool symbolOnlyIndex = false, bool forceMultiValueSymbol = false)
            {
                using var _ = Py.GIL();

                var pandasDataDataFrames = GetPandasDataDataFrames(levels, filterMissingValueColumns, symbolOnlyIndex, forceMultiValueSymbol).ToList();
                var collectionsDataFrames = GetCollectionsDataFrames(symbolOnlyIndex, forceMultiValueSymbol).ToList();

                if (collectionsDataFrames.Count == 0)
                {
                    return ConcatDataFrames(pandasDataDataFrames, sort, dropna: true);
                }

                var dataFrames = collectionsDataFrames.Select(x => x.Item3).Concat(pandasDataDataFrames);

                if (_collections.DistinctBy(x => x.Symbol).Count() > 1)
                {
                    var keys = collectionsDataFrames
                        .Select(x => new object[] { x.Item1, x.Item2 })
                        .Concat(pandasDataDataFrames.Select(x => new object[] { x, DateTime.MinValue }));

                    return ConcatDataFrames(dataFrames, keys, MultiBaseDataCollectionDataFrameNames, sort, dropna: true);
                }
                else
                {
                    var keys = collectionsDataFrames
                        .Select(x => new object[] { x.Item2 })
                        .Concat(pandasDataDataFrames.Select(x => new object[] { DateTime.MinValue }));

                    return ConcatDataFrames(dataFrames, keys, SingleBaseDataCollectionDataFrameNames, sort, dropna: true);
                }
            }

            /// <summary>
            /// Creates the data frames for the data stored in the <see cref="_pandasData"/> dictionary
            /// </summary>
            private IEnumerable<PyObject> GetPandasDataDataFrames(int? levels, bool filterMissingValueColumns, bool symbolOnlyIndex, bool forceMultiValueSymbol)
            {
                if (_pandasData is null || _pandasData.Count == 0)
                {
                    yield break;
                }

                if (!forceMultiValueSymbol && (symbolOnlyIndex || _shouldUseSymbolOnlyIndex))
                {
                    yield return PandasData.ToPandasDataFrame(_pandasData.Values, skipTimesColumn: true);
                    yield break;
                }

                foreach (var data in _pandasData.Values)
                {
                    yield return data.ToPandasDataFrame(levels ?? _maxLevels, filterMissingValueColumns);
                }
            }

            /// <summary>
            /// Generates the data frames for the base data collections
            /// </summary>
            private IEnumerable<(Symbol, DateTime, PyObject)> GetCollectionsDataFrames(bool symbolOnlyIndex, bool forceMultiValueSymbol)
            {
                if (_collections is null || _collections.Count == 0)
                {
                    yield break;
                }

                foreach (var collection in _collections.GroupBy(x => x.Symbol).SelectMany(x => x))
                {
                    var generator = new DataFrameGenerator(_dataType, timeAsColumn: !symbolOnlyIndex);
                    generator.AddData(collection.Data);
                    var dataFrame = generator.GenerateDataFrame(symbolOnlyIndex: symbolOnlyIndex, forceMultiValueSymbol: forceMultiValueSymbol);

                    yield return (collection.Symbol, collection.EndTime, dataFrame);
                }
            }

            private PandasData GetPandasData(ISymbolProvider data)
            {
                _pandasData ??= new();
                if (!_pandasData.TryGetValue(data.Symbol, out var pandasData))
                {
                    pandasData = new PandasData(data, _timeAsColumn);
                    _pandasData[data.Symbol] = pandasData;
                    _maxLevels = Math.Max(_maxLevels, pandasData.Levels);
                }

                return pandasData;
            }

            private void AddCollection(BaseDataCollection collection)
            {
                _collections ??= new();
                _collections.Add(collection);
            }
        }
    }
}