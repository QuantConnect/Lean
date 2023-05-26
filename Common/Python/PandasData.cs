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
using QuantConnect.Util;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace QuantConnect.Python
{
    /// <summary>
    /// Organizes a list of data to create pandas.DataFrames
    /// </summary>
    public class PandasData
    {
        // we keep these so we don't need to ask for them each time
        private static PyString _empty;
        private static PyObject _pandas;
        private static PyObject _seriesFactory;
        private static PyObject _dataFrameFactory;
        private static PyObject _multiIndexFactory;

        private static PyList _defaultNames;
        private static PyList _level2Names;
        private static PyList _level3Names;

        private readonly static HashSet<string> _baseDataProperties = typeof(BaseData).GetProperties().ToHashSet(x => x.Name.ToLowerInvariant());
        private readonly static ConcurrentDictionary<Type, IEnumerable<MemberInfo>> _membersByType = new ();
        private readonly static IReadOnlyList<string> _standardColumns = new string []
        {
                "open",    "high",    "low",    "close", "lastprice",  "volume",
            "askopen", "askhigh", "asklow", "askclose",  "askprice", "asksize", "quantity", "suspicious",
            "bidopen", "bidhigh", "bidlow", "bidclose",  "bidprice", "bidsize", "exchange", "openinterest"
        };

        private readonly Symbol _symbol;
        private readonly Dictionary<string, Tuple<List<DateTime>, List<object>>> _series;

        private readonly IEnumerable<MemberInfo> _members = Enumerable.Empty<MemberInfo>();

        /// <summary>
        /// Gets true if this is a custom data request, false for normal QC data
        /// </summary>
        public bool IsCustomData { get; }

        /// <summary>
        /// Implied levels of a multi index pandas.Series (depends on the security type)
        /// </summary>
        public int Levels { get; } = 2;

        /// <summary>
        /// Initializes an instance of <see cref="PandasData"/>
        /// </summary>
        public PandasData(object data)
        {
            if (_pandas == null)
            {
                using (Py.GIL())
                {
                    // Use our PandasMapper class that modifies pandas indexing to support tickers, symbols and SIDs
                    _pandas = Py.Import("PandasMapper");
                    _seriesFactory = _pandas.GetAttr("Series");
                    _dataFrameFactory = _pandas.GetAttr("DataFrame");
                    using var multiIndex = _pandas.GetAttr("MultiIndex");
                    _multiIndexFactory = multiIndex.GetAttr("from_tuples");
                    _empty = new PyString(string.Empty);

                    var time = new PyString("time");
                    var symbol = new PyString("symbol");
                    var expiry = new PyString("expiry");
                    _defaultNames = new PyList(new PyObject[] { expiry, new PyString("strike"), new PyString("type"), symbol, time });
                    _level2Names = new PyList(new PyObject[] { symbol, time });
                    _level3Names = new PyList(new PyObject[] { expiry, symbol, time });
                }
            }

            // in the case we get a list/collection of data we take the first data point to determine the type
            // but it's also possible to get a data which supports enumerating we don't care about those cases
            if (data is not IBaseData && data is IEnumerable enumerable)
            {
                foreach (var item in enumerable)
                {
                    data = item;
                    break;
                }
            }

            var type = data.GetType();
            IsCustomData = type.Namespace != typeof(Bar).Namespace;
            _symbol = ((IBaseData)data).Symbol;

            if (_symbol.SecurityType == SecurityType.Future) Levels = 3;
            if (_symbol.SecurityType.IsOption()) Levels = 5;

            IEnumerable<string> columns = _standardColumns;

            if (IsCustomData)
            {
                var keys = (data as DynamicData)?.GetStorageDictionary().ToHashSet(x => x.Key);

                // C# types that are not DynamicData type
                if (keys == null)
                {
                    if (_membersByType.TryGetValue(type, out _members))
                    {
                        keys = _members.ToHashSet(x => x.Name.ToLowerInvariant());
                    }
                    else
                    {
                        var members = type.GetMembers().Where(x => x.MemberType == MemberTypes.Field || x.MemberType == MemberTypes.Property).ToList();

                        var duplicateKeys = members.GroupBy(x => x.Name.ToLowerInvariant()).Where(x => x.Count() > 1).Select(x => x.Key);
                        foreach (var duplicateKey in duplicateKeys)
                        {
                            throw new ArgumentException($"PandasData.ctor(): {Messages.PandasData.DuplicateKey(duplicateKey, type.FullName)}");
                        }

                        // If the custom data derives from a Market Data (e.g. Tick, TradeBar, QuoteBar), exclude its keys
                        keys = members.ToHashSet(x => x.Name.ToLowerInvariant());
                        keys.ExceptWith(_baseDataProperties);
                        keys.ExceptWith(GetPropertiesNames(typeof(QuoteBar), type));
                        keys.ExceptWith(GetPropertiesNames(typeof(TradeBar), type));
                        keys.ExceptWith(GetPropertiesNames(typeof(Tick), type));
                        keys.Add("value");

                        _members = members.Where(x => keys.Contains(x.Name.ToLowerInvariant())).ToList();
                        _membersByType.TryAdd(type, _members);
                    }
                }

                var customColumns = new HashSet<string>(columns);
                customColumns.Add("value");
                customColumns.UnionWith(keys);

                columns = customColumns;
            }

            _series = columns.ToDictionary(k => k, v => Tuple.Create(new List<DateTime>(), new List<object>()));
        }

        /// <summary>
        /// Adds security data object to the end of the lists
        /// </summary>
        /// <param name="baseData"><see cref="IBaseData"/> object that contains security data</param>
        public void Add(object baseData)
        {
            foreach (var member in _members)
            {
                var key = member.Name.ToLowerInvariant();
                var endTime = ((IBaseData)baseData).EndTime;
                var propertyMember = member as PropertyInfo;
                if (propertyMember != null)
                {
                    AddToSeries(key, endTime, propertyMember.GetValue(baseData));
                    continue;
                }
                var fieldMember = member as FieldInfo;
                if (fieldMember != null)
                {
                    AddToSeries(key, endTime, fieldMember.GetValue(baseData));
                }
            }

            var storage = (baseData as DynamicData)?.GetStorageDictionary();
            if (storage != null)
            {
                var endTime = ((IBaseData) baseData).EndTime;
                var value = ((IBaseData) baseData).Value;
                AddToSeries("value", endTime, value);

                foreach (var kvp in storage.Where(x => x.Key != "value"))
                {
                    AddToSeries(kvp.Key, endTime, kvp.Value);
                }
            }
            else
            {
                var tick = baseData as Tick;
                var tradeBar = baseData as TradeBar;
                var quoteBar = baseData as QuoteBar;
                Add(tick, tradeBar, quoteBar);
            }
        }

        /// <summary>
        /// Adds Lean data objects to the end of the lists
        /// </summary>
        /// <param name="tick"><see cref="Tick"/> object that contains tick information of the security</param>
        /// <param name="tradeBar"><see cref="TradeBar"/> object that contains trade bar information of the security</param>
        /// <param name="quoteBar"><see cref="QuoteBar"/> object that contains quote bar information of the security</param>
        public void Add(Tick tick, TradeBar tradeBar, QuoteBar quoteBar)
        {
            if (tradeBar != null)
            {
                var time = tradeBar.EndTime;
                AddToSeries("open", time, tradeBar.Open);
                AddToSeries("high", time, tradeBar.High);
                AddToSeries("low", time, tradeBar.Low);
                AddToSeries("close", time, tradeBar.Close);
                AddToSeries("volume", time, tradeBar.Volume);
            }
            if (quoteBar != null)
            {
                var time = quoteBar.EndTime;
                if (tradeBar == null)
                {
                    AddToSeries("open", time, quoteBar.Open);
                    AddToSeries("high", time, quoteBar.High);
                    AddToSeries("low", time, quoteBar.Low);
                    AddToSeries("close", time, quoteBar.Close);
                }
                if (quoteBar.Ask != null)
                {
                    AddToSeries("askopen", time, quoteBar.Ask.Open);
                    AddToSeries("askhigh", time, quoteBar.Ask.High);
                    AddToSeries("asklow", time, quoteBar.Ask.Low);
                    AddToSeries("askclose", time, quoteBar.Ask.Close);
                    AddToSeries("asksize", time, quoteBar.LastAskSize);
                }
                if (quoteBar.Bid != null)
                {
                    AddToSeries("bidopen", time, quoteBar.Bid.Open);
                    AddToSeries("bidhigh", time, quoteBar.Bid.High);
                    AddToSeries("bidlow", time, quoteBar.Bid.Low);
                    AddToSeries("bidclose", time, quoteBar.Bid.Close);
                    AddToSeries("bidsize", time, quoteBar.LastBidSize);
                }
            }
            if (tick != null)
            {
                var time = tick.EndTime;

                // We will fill some series with null for tick types that don't have a value for that series, so that we make sure
                // the indices are the same for every tick series.

                if (tick.TickType == TickType.Quote)
                {
                    AddToSeries("askprice", time, tick.AskPrice);
                    AddToSeries("asksize", time, tick.AskSize);
                    AddToSeries("bidprice", time, tick.BidPrice);
                    AddToSeries("bidsize", time, tick.BidSize);
                }
                else
                {
                    // Trade and open interest ticks don't have these values, so we'll fill them with null.
                    AddToSeries("askprice", time, null);
                    AddToSeries("asksize", time, null);
                    AddToSeries("bidprice", time, null);
                    AddToSeries("bidsize", time, null);
                }

                AddToSeries("exchange", time, tick.Exchange);
                AddToSeries("suspicious", time, tick.Suspicious);
                AddToSeries("quantity", time, tick.Quantity);

                if (tick.TickType == TickType.OpenInterest)
                {
                    AddToSeries("openinterest", time, tick.Value);
                    AddToSeries("lastprice", time, null);
                }
                else
                {
                    AddToSeries("lastprice", time, tick.Value);
                    AddToSeries("openinterest", time, null);
                }
            }
        }

        /// <summary>
        /// Get the pandas.DataFrame of the current <see cref="PandasData"/> state
        /// </summary>
        /// <param name="levels">Number of levels of the multi index</param>
        /// <returns>pandas.DataFrame object</returns>
        public PyObject ToPandasDataFrame(int levels = 2)
        {
            var list = Enumerable.Repeat<PyObject>(_empty, 5).ToList();
            list[3] = _symbol.ID.ToString().ToPython();

            if (_symbol.SecurityType == SecurityType.Future)
            {
                list[0] = _symbol.ID.Date.ToPython();
            }
            else if (_symbol.SecurityType.IsOption())
            {
                list[0] = _symbol.ID.Date.ToPython();
                list[1] = _symbol.ID.StrikePrice.ToPython();
                list[2] = _symbol.ID.OptionRight.ToString().ToPython();
            }

            // Create the index labels
            var names = _defaultNames;
            if (levels == 2)
            {
                names = _level2Names;
                for (int i = 0; i < 3; i++)
                {
                    // dispose of existing entry unless it's our static empty
                    DisposeIfNotEmpty(list[i]);
                }
                list.RemoveRange(0, 3);
            }
            if (levels == 3)
            {
                names = _level3Names;
                for (int i = 1; i < 2; i++)
                {
                    // dispose of existing entry unless it's our static empty
                    DisposeIfNotEmpty(list[i]);
                }
                list.RemoveRange(1, 2);
            }

            // creating the pandas MultiIndex is expensive so we keep a cash
            var indexCache = new Dictionary<List<DateTime>, PyObject>(new ListComparer<DateTime>());
            using (Py.GIL())
            {
                // Returns a dictionary keyed by column name where values are pandas.Series objects
                using var pyDict = new PyDict();
                foreach (var kvp in _series)
                {
                    var values = kvp.Value.Item2;
                    if (values.All(Filter)) continue;

                    if (!indexCache.TryGetValue(kvp.Value.Item1, out var index))
                    {
                        using var tuples = kvp.Value.Item1.Select(time => CreateTupleIndex(time, list)).ToPyList();
                        using var namesDic = Py.kw("names", names);

                        indexCache[kvp.Value.Item1] = index = _multiIndexFactory.Invoke(new[] { tuples }, namesDic);

                        foreach (var pyObject in tuples)
                        {
                            pyObject.Dispose();
                        }
                    }

                    // Adds pandas.Series value keyed by the column name
                    using var pyvalues = values.ToPyList();
                    using var series = _seriesFactory.Invoke(pyvalues, index);
                    pyDict.SetItem(kvp.Key, series);

                    foreach (var value in pyvalues)
                    {
                        value.Dispose();
                    }
                }
                _series.Clear();
                foreach (var kvp in indexCache)
                {
                    kvp.Value.Dispose();
                }

                for (var i = 0; i < list.Count; i++)
                {
                    DisposeIfNotEmpty(list[i]);
                }

                // Create the DataFrame
                var result = _dataFrameFactory.Invoke(pyDict);

                foreach (var item in pyDict)
                {
                    item.Dispose();
                }

                return result;
            }
        }

        /// <summary>
        /// Will determine if the given object should be used to create the pandas data frame or not
        /// </summary>
        private static bool Filter(object x)
        {
            var isNaNOrZero = x is double && ((double)x).IsNaNOrZero();
            var isNullOrWhiteSpace = x is string && string.IsNullOrWhiteSpace((string)x);
            var isFalse = x is bool && !(bool)x;
            return x == null || isNaNOrZero || isNullOrWhiteSpace || isFalse;
        }

        /// <summary>
        /// Only dipose of the PyObject if it was set to something different than empty
        /// </summary>
        private static void DisposeIfNotEmpty(PyObject pyObject)
        {
            if (!ReferenceEquals(pyObject, _empty))
            {
                pyObject.Dispose();
            }
        }

        /// <summary>
        /// Create a new tuple index
        /// </summary>
        private static PyTuple CreateTupleIndex(DateTime index, List<PyObject> list)
        {
            DisposeIfNotEmpty(list[list.Count - 1]);
            list[list.Count - 1] = index.ToPython();
            return new PyTuple(list.ToArray());
        }

        /// <summary>
        /// Adds data to dictionary
        /// </summary>
        /// <param name="key">The key of the value to get</param>
        /// <param name="time"><see cref="DateTime"/> object to add to the value associated with the specific key</param>
        /// <param name="input"><see cref="Object"/> to add to the value associated with the specific key. Can be null.</param>
        private void AddToSeries(string key, DateTime time, object input)
        {
            Tuple<List<DateTime>, List<object>> value;
            if (_series.TryGetValue(key, out value))
            {
                value.Item1.Add(time);
                value.Item2.Add(input is decimal ? input.ConvertInvariant<double>() : input);
            }
            else
            {
                throw new ArgumentException($"PandasData.AddToSeries(): {Messages.PandasData.KeyNotFoundInSeries(key)}");
            }
        }

        /// <summary>
        /// Get the lower-invariant name of properties of the type that a another type is assignable from
        /// </summary>
        /// <param name="baseType">The type that is assignable from</param>
        /// <param name="type">The type that is assignable by</param>
        /// <returns>List of string. Empty list if not assignable from</returns>
        private static IEnumerable<string> GetPropertiesNames(Type baseType, Type type)
        {
            return baseType.IsAssignableFrom(type)
                ? baseType.GetProperties().Select(x => x.Name.ToLowerInvariant())
                : Enumerable.Empty<string>();
        }
    }
}
