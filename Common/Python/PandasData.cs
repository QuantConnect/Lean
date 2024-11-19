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
using QuantConnect.Data.Fundamental;
using QuantConnect.Data.Market;
using QuantConnect.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace QuantConnect.Python
{
    /// <summary>
    /// Organizes a list of data to create pandas.DataFrames
    /// </summary>
    public partial class PandasData
    {
        // we keep these so we don't need to ask for them each time
        private static PyString _empty;
        private static PyObject _pandas;
        private static PyObject _pandasColumn;
        private static PyObject _seriesFactory;
        private static PyObject _dataFrameFactory;
        private static PyObject _multiIndexFactory;
        private static PyObject _multiIndex;
        private static PyObject _indexFactory;

        private static PyList _defaultNames;
        private static PyList _level1Names;
        private static PyList _level2Names;
        private static PyList _level3Names;

        private readonly static Dictionary<Type, IEnumerable<DataTypeMember>> _membersCache = new();

        private readonly static MemberInfo _tickLastPriceMember = typeof(Tick).GetProperty(nameof(Tick.LastPrice));
        private readonly static MemberInfo _openInterestLastPriceMember = typeof(OpenInterest).GetProperty(nameof(Tick.LastPrice));

        private static readonly string[] _nonLeanDataTypeForcedMemberNames = new[] { nameof(BaseData.Value) };

        private readonly static string[] _quoteTickOnlyPropertes = new[] {
            nameof(Tick.AskPrice),
            nameof(Tick.AskSize),
            nameof(Tick.BidPrice),
            nameof(Tick.BidSize)
        };

        private static readonly Type PandasNonExpandableAttribute = typeof(PandasNonExpandableAttribute);
        private static readonly Type PandasIgnoreAttribute = typeof(PandasIgnoreAttribute);
        private static readonly Type PandasIgnoreMembersAttribute = typeof(PandasIgnoreMembersAttribute);

        private readonly Symbol _symbol;
        private readonly bool _isFundamentalType;
        private readonly bool _isBaseData;
        private readonly bool _timeAsColumn;
        private readonly Dictionary<string, Serie> _series;

        private readonly Dictionary<Type, IEnumerable<DataTypeMember>> _members = new();

        /// <summary>
        /// Gets true if this is a custom data request, false for normal QC data
        /// </summary>
        public bool IsCustomData { get; }

        /// <summary>
        /// Implied levels of a multi index pandas.Series (depends on the security type)
        /// </summary>
        public int Levels { get; } = 2;

        /// <summary>
        /// Initializes the static members of the <see cref="PandasData"/> class
        /// </summary>
        static PandasData()
        {
            using (Py.GIL())
            {
                // Use our PandasMapper class that modifies pandas indexing to support tickers, symbols and SIDs
                _pandas = Py.Import("PandasMapper");
                _pandasColumn = _pandas.GetAttr("PandasColumn");
                _seriesFactory = _pandas.GetAttr("Series");
                _dataFrameFactory = _pandas.GetAttr("DataFrame");
                _multiIndex = _pandas.GetAttr("MultiIndex");
                _multiIndexFactory = _multiIndex.GetAttr("from_tuples");
                _indexFactory = _pandas.GetAttr("Index");
                _empty = new PyString(string.Empty);

                var time = new PyString("time");
                var symbol = new PyString("symbol");
                var expiry = new PyString("expiry");
                _defaultNames = new PyList(new PyObject[] { expiry, new PyString("strike"), new PyString("type"), symbol, time });
                _level1Names = new PyList(new PyObject[] { symbol });
                _level2Names = new PyList(new PyObject[] { symbol, time });
                _level3Names = new PyList(new PyObject[] { expiry, symbol, time });
            }
        }

        /// <summary>
        /// Initializes an instance of <see cref="PandasData"/>
        /// </summary>
        public PandasData(object data, bool timeAsColumn = false)
        {
            _series = new();
            var baseData = data as IBaseData;

            // in the case we get a list/collection of data we take the first data point to determine the type
            // but it's also possible to get a data which supports enumerating we don't care about those cases
            if (baseData == null && data is IEnumerable enumerable)
            {
                foreach (var item in enumerable)
                {
                    data = item;
                    baseData = data as IBaseData;
                    break;
                }
            }

            var type = data.GetType();
            _isFundamentalType = type == typeof(Fundamental);
            _isBaseData = baseData != null;
            _timeAsColumn = timeAsColumn && _isBaseData;
            _symbol = _isBaseData ? baseData.Symbol : ((ISymbolProvider)data).Symbol;
            IsCustomData = Extensions.IsCustomDataType(_symbol, type);

            if (baseData == null)
            {
                Levels = 1;
            }
            else if (_symbol.SecurityType == SecurityType.Future)
            {
                Levels = 3;
            }
            else if (_symbol.SecurityType.IsOption())
            {
                Levels = 5;
            }
        }

        /// <summary>
        /// Adds security data object to the end of the lists
        /// </summary>
        /// <param name="data"><see cref="IBaseData"/> object that contains security data</param>
        public void Add(object data)
        {
            Add(data, false);
        }

        private void Add(object data, bool overrideValues)
        {
            if (data == null)
            {
                return;
            }

            var typeMembers = GetInstanceDataTypeMembers(data).ToList();

            var endTime = default(DateTime);
            if (_isBaseData)
            {
                endTime = ((IBaseData)data).EndTime;
                if (_timeAsColumn)
                {
                    AddToSeries("time", endTime, endTime, overrideValues);
                }
            }

            AddMembersData(data, typeMembers, endTime, overrideValues);

            if (data is DynamicData dynamicData)
            {
                var storage = dynamicData.GetStorageDictionary();
                var value = dynamicData.Value;
                AddToSeries("value", endTime, value, overrideValues);

                foreach (var kvp in storage.Where(x => x.Key != "value"
                    // if this is a PythonData instance we add in '__typename' which we don't want into the data frame
                    && !x.Key.StartsWith("__", StringComparison.InvariantCulture)))
                {
                    AddToSeries(kvp.Key, endTime, kvp.Value, overrideValues);
                }
            }
        }

        private void AddMemberToSeries(object instance, DateTime endTime, DataTypeMember member, bool overrideValues)
        {
            var baseName = (string)null;
            var tick = member.IsTickProperty ? instance as Tick : null;
            if (tick != null && member.IsTickLastPrice && tick.TickType == TickType.OpenInterest)
            {
                baseName = "OpenInterest";
            }

            // TODO field/property.GetValue is expensive
            var key = member.GetMemberName(baseName);
            var value = member.GetValue(instance);

            var memberType = member.GetMemberType();
            // For DataDictionary instances, we only want to add the values
            if (MemberIsDataDictionary(memberType))
            {
                value = memberType.GetProperty("Values").GetValue(value);
            }
            else if (member.IsProperty)
            {
                if (_isFundamentalType && value is FundamentalTimeDependentProperty timeDependentProperty)
                {
                    value = timeDependentProperty.Clone(new FixedTimeProvider(endTime));
                }
                else if (member.IsTickProperty && tick != null)
                {
                    if (tick.TickType != TickType.Quote && _quoteTickOnlyPropertes.Contains(member.Member.Name))
                    {
                        value = null;
                    }
                    else if (member.IsTickLastPrice)
                    {
                        var nullValueKey = tick.TickType != TickType.OpenInterest
                            ? member.GetMemberName("OpenInterest")
                            : member.GetMemberName();
                        AddToSeries(nullValueKey, endTime, null, overrideValues);
                    }
                }
            }

            AddToSeries(key, endTime, value, overrideValues);
        }

        /// <summary>
        /// Adds Lean data objects to the end of the lists
        /// </summary>
        /// <param name="tradeBar"><see cref="TradeBar"/> object that contains trade bar information of the security</param>
        /// <param name="quoteBar"><see cref="QuoteBar"/> object that contains quote bar information of the security</param>
        public void Add(TradeBar tradeBar, QuoteBar quoteBar)
        {
            // Quote bar first, so if there is a trade bar, OHLC will be overwritten
            Add(quoteBar);
            Add(tradeBar, overrideValues: true);
        }

        /// <summary>
        /// Get the pandas.DataFrame of the current <see cref="PandasData"/> state
        /// </summary>
        /// <param name="levels">Number of levels of the multi index</param>
        /// <param name="filterMissingValueColumns">If false, make sure columns with "missing" values only are still added to the dataframe</param>
        /// <returns>pandas.DataFrame object</returns>
        public PyObject ToPandasDataFrame(int levels = 2, bool filterMissingValueColumns = true)
        {
            using var _ = Py.GIL();

            PyObject[] indexTemplate;
            // Create the index labels
            var names = _defaultNames;

            if (levels == 1)
            {
                names = _level1Names;
                indexTemplate = GetIndexTemplate(_symbol);
            }
            else if (levels == 2)
            {
                // symbol, time
                names = _level2Names;
                indexTemplate = GetIndexTemplate(_symbol, null);
            }
            else if (levels == 3)
            {
                // expiry, symbol, time
                names = _level3Names;
                indexTemplate = GetIndexTemplate(_symbol.ID.Date, _symbol, null);
            }
            else
            {
                if (_symbol.SecurityType == SecurityType.Future)
                {
                    indexTemplate = GetIndexTemplate(_symbol.ID.Date, null, null, _symbol, null);
                }
                else if (_symbol.SecurityType.IsOption())
                {
                    indexTemplate = GetIndexTemplate(_symbol.ID.Date, _symbol.ID.StrikePrice, _symbol.ID.OptionRight, _symbol, null);
                }
                else
                {
                    indexTemplate = GetIndexTemplate(null, null, null, _symbol, null);
                }
            }

            names = new PyList(names.SkipLast(names.Count() > 1 && _timeAsColumn ? 1 : 0).ToArray());

            // creating the pandas MultiIndex is expensive so we keep a cash
            var indexCache = new Dictionary<IReadOnlyCollection<DateTime>, PyObject>(new ListComparer<DateTime>());
            // Returns a dictionary keyed by column name where values are pandas.Series objects
            using var pyDict = new PyDict();
            foreach (var (seriesName, serie) in _series)
            {
                if (filterMissingValueColumns && serie.ShouldFilter) continue;

                var key = serie.Times ?? new List<DateTime>();
                if (!indexCache.TryGetValue(key, out var index))
                {
                    PyList indexSource;
                    if (_timeAsColumn)
                    {
                        indexSource = serie.Values.Select(_ => CreateIndexSourceValue(DateTime.MinValue, indexTemplate)).ToPyListUnSafe();
                    }
                    else
                    {
                        indexSource = serie.Times.Select(time => CreateIndexSourceValue(time, indexTemplate)).ToPyListUnSafe();
                    }

                    if (indexTemplate.Length == 1)
                    {
                        using var nameDic = Py.kw("name", names[0]);
                        index = _indexFactory.Invoke(new[] { indexSource }, nameDic);
                    }
                    else
                    {
                        using var namesDic = Py.kw("names", names);
                        index = _multiIndexFactory.Invoke(new[] { indexSource }, namesDic);
                    }

                    indexCache[key] = index;

                    foreach (var pyObject in indexSource)
                    {
                        pyObject.Dispose();
                    }
                    indexSource.Dispose();
                }

                // Adds pandas.Series value keyed by the column name
                using var pyvalues = new PyList();
                for (var i = 0; i < serie.Values.Count; i++)
                {
                    using var pyObject = serie.Values[i].ToPython();
                    pyvalues.Append(pyObject);
                }
                using var series = _seriesFactory.Invoke(pyvalues, index);
                using var pyStrKey = seriesName.ToPython();
                using var pyKey = _pandasColumn.Invoke(pyStrKey);
                pyDict.SetItem(pyKey, series);
            }
            _series.Clear();
            foreach (var kvp in indexCache)
            {
                kvp.Value.Dispose();
            }

            for (var i = 0; i < indexTemplate.Length; i++)
            {
                DisposeIfNotEmpty(indexTemplate[i]);
            }
            names.Dispose();

            // Create the DataFrame
            var result = _dataFrameFactory.Invoke(pyDict);

            foreach (var item in pyDict)
            {
                item.Dispose();
            }

            return result;
        }

        /// <summary>
        /// Helper method to create a single pandas data frame indexed by symbol
        /// </summary>
        /// <remarks>Will add a single point per pandas data series (symbol)</remarks>
        public static PyObject ToPandasDataFrame(IEnumerable<PandasData> pandasDatas, bool skipTimesColumn = false)
        {
            using var _ = Py.GIL();

            using var list = pandasDatas.Select(x => x._symbol).ToPyListUnSafe();

            using var namesDic = Py.kw("name", _level1Names[0]);
            using var index = _indexFactory.Invoke(new[] { list }, namesDic);

            var valuesPerSeries = new Dictionary<string, PyList>();
            var seriesToSkip = new Dictionary<string, bool>();
            foreach (var pandasData in pandasDatas)
            {
                foreach (var kvp in pandasData._series)
                {
                    if (skipTimesColumn && kvp.Key == "time")
                    {
                        continue;
                    }

                    if (seriesToSkip.ContainsKey(kvp.Key))
                    {
                        seriesToSkip[kvp.Key] &= kvp.Value.ShouldFilter;
                    }
                    else
                    {
                        seriesToSkip[kvp.Key] = kvp.Value.ShouldFilter;
                    }

                    if (!valuesPerSeries.TryGetValue(kvp.Key, out PyList value))
                    {
                        // Adds pandas.Series value keyed by the column name
                        value = valuesPerSeries[kvp.Key] = new PyList();
                    }

                    if (kvp.Value.Values.Count > 0)
                    {
                        // taking only 1 value per symbol
                        using var valueOfSymbol = kvp.Value.Values[0].ToPython();
                        value.Append(valueOfSymbol);
                    }
                    else
                    {
                        value.Append(PyObject.None);
                    }
                }
            }

            using var pyDict = new PyDict();
            foreach (var kvp in valuesPerSeries)
            {
                if (seriesToSkip.TryGetValue(kvp.Key, out var skip) && skip)
                {
                    continue;
                }

                using var series = _seriesFactory.Invoke(kvp.Value, index);
                using var pyStrKey = kvp.Key.ToPython();
                using var pyKey = _pandasColumn.Invoke(pyStrKey);
                pyDict.SetItem(pyKey, series);

                kvp.Value.Dispose();
            }
            var result = _dataFrameFactory.Invoke(pyDict);

            // Drop columns with only NaN or None values
            using var dropnaKwargs = Py.kw("axis", 1, "inplace", true, "how", "all");
            result.GetAttr("dropna").Invoke(Array.Empty<PyObject>(), dropnaKwargs);

            return result;
        }

        private IEnumerable<DataTypeMember> GetInstanceDataTypeMembers(object data)
        {
            var type = data.GetType();
            if (!_members.TryGetValue(type, out var members))
            {
                HashSet<string> columnNames;

                if (data is DynamicData dynamicData)
                {
                    columnNames = (data as DynamicData)?.GetStorageDictionary()
                        // if this is a PythonData instance we add in '__typename' which we don't want into the data frame
                        .Where(x => !x.Key.StartsWith("__", StringComparison.InvariantCulture)).ToHashSet(x => x.Key);
                    columnNames.Add("value");
                    members = Enumerable.Empty<DataTypeMember>();
                }
                else
                {
                    members = GetTypeMembers(type);
                    columnNames = members.SelectMany(x => x.GetMemberNames()).ToHashSet();
                    // We add openinterest key so the series is created: open interest tick LastPrice is renamed to OpenInterest
                    if (data is Tick)
                    {
                        columnNames.Add("openinterest");
                    }
                }

                _members[type] = members;

                if (_timeAsColumn)
                {
                    columnNames.Add("time");
                }

                foreach (var columnName in columnNames)
                {
                    _series.TryAdd(columnName, new Serie(withTimeIndex: !_timeAsColumn));
                }
            }

            return members;
        }

        /// <summary>
        /// Gets or create/adds the <see cref="DataTypeMember"/> instances corresponding to the members of the given type,
        /// and returns the names of the members.
        /// </summary>
        private IEnumerable<DataTypeMember> GetTypeMembers(Type type)
        {
            IEnumerable<DataTypeMember> typeMembers;
            lock (_membersCache)
            {
                if (!_membersCache.TryGetValue(type, out typeMembers))
                {
                    var forcedInclusionMembers = LeanData.IsCommonLeanDataType(type)
                        ? Array.Empty<string>()
                        : _nonLeanDataTypeForcedMemberNames;
                    typeMembers = GetDataTypeMembers(type, forcedInclusionMembers).ToList();
                    _membersCache[type] = typeMembers;
                }
            }

            _members[type] = typeMembers;
            return typeMembers;
        }

        /// <summary>
        /// Gets the <see cref="DataTypeMember"/> instances corresponding to the members of the given type.
        /// It will try to unwrap properties which types are classes unless they are marked either to be ignored or to be added as a whole
        /// </summary>
        private static IEnumerable<DataTypeMember> GetDataTypeMembers(Type type, string[] forcedInclusionMembers)
        {
            var members = type
                .GetMembers(BindingFlags.Instance | BindingFlags.Public)
                .Where(x => x.MemberType == MemberTypes.Field || x.MemberType == MemberTypes.Property)
                .Where(x => forcedInclusionMembers.Contains(x.Name)
                    || (!x.IsDefined(PandasIgnoreAttribute) && !x.DeclaringType.IsDefined(PandasIgnoreMembersAttribute)));

            return members
                .Select(member =>
                {
                    var dataTypeMember = CreateDataTypeMember(member);
                    var memberType = dataTypeMember.GetMemberType();

                    // Should we unpack its properties into columns?
                    if (memberType.IsClass
                        && (memberType.Namespace == null
                            // We only expand members of types in the QuantConnect namespace,
                            // else we might be expanding types like System.String, NodaTime.DateTimeZone or any other external types
                            || (memberType.Namespace.StartsWith("QuantConnect.", StringComparison.InvariantCulture)
                                && !memberType.IsDefined(PandasNonExpandableAttribute)
                                && !member.IsDefined(PandasNonExpandableAttribute))))
                    {
                        dataTypeMember = CreateDataTypeMember(member, GetDataTypeMembers(memberType, forcedInclusionMembers).ToArray());
                    }

                    return (memberType, dataTypeMember);
                })
                // Check if there are multiple properties/fields of the same type,
                // in which case we add the property/field name as prefix for the inner members to avoid name conflicts
                .GroupBy(x => x.memberType, x => x.dataTypeMember)
                .SelectMany(grouping =>
                {
                    var typeProperties = grouping.ToList();
                    if (typeProperties.Count > 1)
                    {
                        var propertiesToExpand = typeProperties.Where(x => x.ShouldBeUnwrapped).ToList();
                        if (propertiesToExpand.Count > 1)
                        {
                            foreach (var property in propertiesToExpand)
                            {
                                property.SetPrefix();
                            }
                        }
                    }

                    return typeProperties;
                });
        }

        /// <summary>
        /// Adds the member value to the corresponding series, making sure unwrapped values a properly added
        /// by checking the children members and adding their values to their own series
        /// </summary>
        private void AddMembersData(object instance, IEnumerable<DataTypeMember> members, DateTime endTime, bool overrideValues)
        {
            foreach (var member in members)
            {
                if (!member.ShouldBeUnwrapped)
                {
                    AddMemberToSeries(instance, endTime, member, overrideValues);
                }
                else
                {
                    var memberValue = member.GetValue(instance);
                    if (memberValue != null)
                    {
                        AddMembersData(memberValue, member.Children, endTime, overrideValues);
                    }
                }
            }
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

        private static bool MemberIsDataDictionary(Type memberType)
        {
            while (memberType != null)
            {
                if (memberType.IsGenericType && memberType.GetGenericTypeDefinition() == typeof(DataDictionary<>))
                {
                    return true;
                }
                memberType = memberType.BaseType;
            }

            return false;
        }

        private PyObject[] GetIndexTemplate(params object[] args)
        {
            return args.SkipLast(args.Length > 1 && _timeAsColumn ? 1 : 0).Select(x => x?.ToPython() ?? _empty).ToArray();
        }

        /// <summary>
        /// Create a new tuple index
        /// </summary>
        private PyObject CreateIndexSourceValue(DateTime index, PyObject[] list)
        {
            if (!_timeAsColumn && list.Length > 1)
            {
                DisposeIfNotEmpty(list[^1]);
                list[^1] = index.ToPython();
            }

            if (list.Length > 1)
            {
                return new PyTuple(list.ToArray());
            }

            return list[0].ToPython();
        }

        /// <summary>
        /// Adds data to dictionary
        /// </summary>
        /// <param name="key">The key of the value to get</param>
        /// <param name="time"><see cref="DateTime"/> object to add to the value associated with the specific key</param>
        /// <param name="input"><see cref="Object"/> to add to the value associated with the specific key. Can be null.</param>
        private void AddToSeries(string key, DateTime time, object input, bool overrideValues)
        {
            if (!_series.TryGetValue(key, out var serie))
            {
                throw new ArgumentException($"PandasData.AddToSeries(): {Messages.PandasData.KeyNotFoundInSeries(key)}");
            }

            serie.Add(time, input, overrideValues);
        }

        private class Serie
        {
            private static readonly IFormatProvider InvariantCulture = CultureInfo.InvariantCulture;

            public bool ShouldFilter { get; private set; }
            public List<DateTime> Times { get; }
            public List<object> Values { get; }

            public Serie(bool withTimeIndex = true)
            {
                ShouldFilter = true;
                Values = new();
                if (withTimeIndex)
                {
                    Times = new();
                }
            }

            public void Add(DateTime time, object input, bool overrideValues)
            {
                var value = input is decimal ? Convert.ToDouble(input, InvariantCulture) : input;
                if (ShouldFilter)
                {
                    // we need at least 1 valid entry for the series not to get filtered
                    if (value is double doubleValue)
                    {
                        if (!doubleValue.IsNaNOrZero())
                        {
                            ShouldFilter = false;
                        }
                    }
                    else if (value is string stringValue)
                    {
                        if (!string.IsNullOrWhiteSpace(stringValue))
                        {
                            ShouldFilter = false;
                        }
                    }
                    else if (value is bool boolValue)
                    {
                        if (boolValue)
                        {
                            ShouldFilter = false;
                        }
                    }
                    else if (value != null)
                    {
                        ShouldFilter = false;
                    }
                }

                if (overrideValues && Times != null && Times.Count > 0 && Times[^1] == time)
                {
                    // If the time is the same as the last one, we overwrite the value
                    Values[^1] = value;
                }
                else
                {
                    Values.Add(value);
                    Times?.Add(time);
                }
            }
        }

        private class FixedTimeProvider : ITimeProvider
        {
            private readonly DateTime _time;
            public DateTime GetUtcNow() => _time;
            public FixedTimeProvider(DateTime time)
            {
                _time = time;
            }
        }
    }
}
