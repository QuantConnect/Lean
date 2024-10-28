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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;

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

        private readonly static ConcurrentDictionary<Type, IEnumerable<DataTypeMember>> _membersByType = new();

        private readonly static MemberInfo _tickLastPriceMember = typeof(Tick).GetProperty(nameof(Tick.LastPrice));
        private readonly static MemberInfo _openInterestLastPriceMember = typeof(OpenInterest).GetProperty(nameof(Tick.LastPrice));

        private readonly static string[] _quoteTickOnlyPropertes = new[] {
            nameof(Tick.AskPrice),
            nameof(Tick.AskSize),
            nameof(Tick.BidPrice),
            nameof(Tick.BidSize)
        };

        private static Type PandasNonExpandableAttribute = typeof(PandasNonExpandableAttribute);
        private static Type PandasIgnoreAttribute = typeof(PandasIgnoreAttribute);
        private static Type PandasIgnoreMembersAttribute = typeof(PandasIgnoreMembersAttribute);

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

            HashSet<string> columnNames;
            if (_isBaseData && !IsCustomData && baseData.DataType != MarketDataType.Auxiliary)
            {
                // We add columns for TradeBar, QuoteBar, Tick and OpenInterest data, they can all be added to the same data frame.
                // Also, we add openinterest key so the series is created: open interest tick LastPrice is renamed to OpenInterest
                columnNames = new HashSet<string>() { "openinterest" };
                foreach (var dataType in new[] { typeof(TradeBar), typeof(QuoteBar), typeof(Tick), typeof(OpenInterest) })
                {
                    columnNames.UnionWith(SetTypeMembers(dataType));
                }
            }
            else
            {
                columnNames = (data as DynamicData)?.GetStorageDictionary()
                    // if this is a PythonData instance we add in '__typename' which we don't want into the data frame
                    .Where(x => !x.Key.StartsWith("__", StringComparison.InvariantCulture)).ToHashSet(x => x.Key);

                // C# types that are not DynamicData type
                if (columnNames == null)
                {
                    columnNames = SetTypeMembers(type, nameof(BaseData.Value)).ToHashSet();
                }
                else
                {
                    // For dynamic data like PythonData
                    columnNames.Add("value");
                }
            }

            if (_timeAsColumn)
            {
                columnNames.Add("time");
            }

            _series = columnNames.ToDictionary(k => k, v => new Serie(withTimeIndex: !_timeAsColumn));
        }

        /// <summary>
        /// Adds security data object to the end of the lists
        /// </summary>
        /// <param name="data"><see cref="IBaseData"/> object that contains security data</param>
        public void Add(object data)
        {
            Add(data, false);
        }

        private void Add(object baseData, bool overrideValues)
        {
            if (baseData == null)
            {
                return;
            }

            var endTime = default(DateTime);
            if (_isBaseData)
            {
                endTime = ((IBaseData)baseData).EndTime;
                if (_timeAsColumn)
                {
                    AddToSeries("time", endTime, endTime, overrideValues);
                }
            }

            var dataType = baseData.GetType();
            if (_members.TryGetValue(dataType, out var typeMembers))
            {
                AddMembersData(baseData, typeMembers, endTime, overrideValues);
            }

            if (baseData is DynamicData dynamicData)
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

            if (member.IsProperty)
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
        /// Adds a tick data point to this pandas collection
        /// </summary>
        /// <param name="tick"><see cref="Tick"/> object that contains tick information of the security</param>
        public void AddTick(Tick tick)
        {
            Add(tick);
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
            var indexCache = new Dictionary<List<DateTime>, PyObject>(new ListComparer<DateTime>());
            // Returns a dictionary keyed by column name where values are pandas.Series objects
            using var pyDict = new PyDict();
            foreach (var (seriesName, serie) in _series)
            {
                if (filterMissingValueColumns && serie.ShouldFilter) continue;

                if (!indexCache.TryGetValue(serie.Times, out var index))
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

                    indexCache[serie.Times] = index;

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
        public static PyObject ToPandasDataFrame(IEnumerable<PandasData> pandasDatas)
        {
            using var _ = Py.GIL();

            using var list = pandasDatas.Select(x => x._symbol).ToPyListUnSafe();

            using var namesDic = Py.kw("name", _level1Names[0]);
            using var index = _indexFactory.Invoke(new[] { list }, namesDic);

            Dictionary<string, PyList> valuesPerSeries = new();
            foreach (var pandasData in pandasDatas)
            {
                foreach (var kvp in pandasData._series)
                {
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

        /// <summary>
        /// Gets or create/adds the <see cref="DataTypeMember"/> instances corresponding to the members of the given type,
        /// and returns the names of the members.
        /// </summary>
        private IEnumerable<string> SetTypeMembers(Type type, params string[] forcedInclusionMembers)
        {
            if (!_membersByType.TryGetValue(type, out var typeMembers))
            {
                typeMembers = GetDataTypeMembers(type, forcedInclusionMembers).ToList();
                _membersByType.TryAdd(type, typeMembers);
            }

            _members.Add(type, typeMembers);
            return typeMembers.SelectMany(x => x.GetMemberNames());
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
                    DataTypeMember dataTypeMember;
                    var memberType = DataTypeMember.GetMemberType(member);

                    // Should we unpack its properties into columns?
                    if (memberType.IsClass
                        && (memberType.Namespace == null
                            // We only expand members of types in the QuantConnect namespace,
                            // else we might be expanding types like System.String, NodaTime.DateTimeZone or any other external types
                            || (memberType.Namespace.StartsWith("QuantConnect.", StringComparison.InvariantCulture)
                                && !memberType.IsDefined(PandasNonExpandableAttribute)
                                && !member.IsDefined(PandasNonExpandableAttribute))))
                    {
                        dataTypeMember = new DataTypeMember(member, GetDataTypeMembers(memberType, forcedInclusionMembers).ToArray());
                    }
                    else
                    {
                        dataTypeMember = new DataTypeMember(member);
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
            var serie = GetSerie(key);
            serie.Add(time, input, overrideValues);
        }

        private Serie GetSerie(string key)
        {
            if (!_series.TryGetValue(key, out var value))
            {
                throw new ArgumentException($"PandasData.GetSerie(): {Messages.PandasData.KeyNotFoundInSeries(key)}");
            }
            return value;
        }

        private class Serie
        {
            private static readonly IFormatProvider InvariantCulture = CultureInfo.InvariantCulture;
            private bool _withTimeIndex;

            public bool ShouldFilter { get; set; } = true;
            public List<DateTime> Times { get; set; } = new();
            public List<object> Values { get; set; } = new();

            public Serie(bool withTimeIndex = true)
            {
                _withTimeIndex = withTimeIndex;
            }

            public void Add(DateTime time, object input, bool overrideValues)
            {
                var value = input is decimal ? Convert.ToDouble(input, InvariantCulture) : input;
                if (ShouldFilter)
                {
                    // we need at least 1 valid entry for the series not to get filtered
                    if (value is double)
                    {
                        if (!((double)value).IsNaNOrZero())
                        {
                            ShouldFilter = false;
                        }
                    }
                    else if (value is string)
                    {
                        if (!string.IsNullOrWhiteSpace((string)value))
                        {
                            ShouldFilter = false;
                        }
                    }
                    else if (value is bool)
                    {
                        if ((bool)value)
                        {
                            ShouldFilter = false;
                        }
                    }
                    else if (value != null)
                    {
                        ShouldFilter = false;
                    }
                }

                if (overrideValues && Times.Count > 0 && Times[^1] == time)
                {
                    // If the time is the same as the last one, we overwrite the value
                    Values[^1] = value;
                }
                else
                {
                    Values.Add(value);
                    if (_withTimeIndex)
                    {
                        Times.Add(time);
                    }
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

        private class DataTypeMember
        {
            private static readonly StringBuilder _stringBuilder = new StringBuilder();

            private PropertyInfo _property;
            private FieldInfo _field;

            private DataTypeMember _parent;
            private string _name;

            public MemberInfo Member { get; }

            public DataTypeMember[] Children { get; }

            public bool IsProperty => _property != null;

            public bool IsField => _field != null;

            /// <summary>
            /// The prefix to be used for the children members when a class being expanded has multiple properties/fields of the same type
            /// </summary>
            public string Prefix { get; private set; }

            public bool ShouldBeUnwrapped => Children != null && Children.Length > 0;

            /// <summary>
            /// Whether this member is Tick.LastPrice or OpenInterest.LastPrice.
            /// Saved to avoid MemberInfo comparisons in the future
            /// </summary>
            public bool IsTickLastPrice { get; }

            public bool IsTickProperty { get; }

            public DataTypeMember(MemberInfo member, DataTypeMember[] children = null)
            {
                Member = member;
                Children = children;

                _property = member as PropertyInfo;
                _field = member as FieldInfo;

                IsTickLastPrice = member == _tickLastPriceMember || member == _openInterestLastPriceMember;
                IsTickProperty = IsProperty && member.DeclaringType == typeof(Tick);

                if (Children != null)
                {
                    foreach (var child in Children)
                    {
                        child._parent = this;
                    }
                }
            }

            public PropertyInfo AsProperty()
            {
                return _property;
            }

            public FieldInfo AsField()
            {
                return _field;
            }

            public void SetPrefix()
            {
                Prefix = Member.Name.ToLowerInvariant();
            }

            /// <summary>
            /// Gets the member name, adding the parent prefixes if necessary.
            /// </summary>
            /// <param name="customName">If passed, it will be used instead of the <see cref="Member"/>'s name</param>
            public string GetMemberName(string customName = null)
            {
                if (ShouldBeUnwrapped)
                {
                    return string.Empty;
                }

                if (!string.IsNullOrEmpty(customName))
                {
                    return BuildMemberName(customName);
                }

                if (string.IsNullOrEmpty(_name))
                {
                    _name = BuildMemberName(GetBaseName());
                }

                return _name;
            }

            public IEnumerable<string> GetMemberNames()
            {
                return GetMemberNames(null);
            }

            public object GetValue(object instance)
            {
                if (IsProperty)
                {
                    return _property.GetValue(instance);
                }

                return _field.GetValue(instance);
            }

            public override string ToString()
            {
                return $"{GetMemberType(Member).Name} {Member.Name}";
            }

            public static Type GetMemberType(MemberInfo member)
            {
                return member switch
                {
                    PropertyInfo property => property.PropertyType,
                    FieldInfo field => field.FieldType,
                    // Should not happen
                    _ => throw new InvalidOperationException($"Unexpected member type: {member.MemberType}")
                };
            }

            private string BuildMemberName(string baseName)
            {
                _stringBuilder.Clear();
                while (_parent != null && _parent.ShouldBeUnwrapped)
                {
                    _stringBuilder.Insert(0, _parent.Prefix);
                    _parent = _parent._parent;
                }

                _stringBuilder.Append(baseName.ToLowerInvariant());
                return _stringBuilder.ToString();
            }

            private IEnumerable<string> GetMemberNames(string parentPrefix)
            {
                // If there are no children, return the name of the member. Else ignore the member and return the children names
                if (ShouldBeUnwrapped)
                {
                    var prefix = parentPrefix ?? string.Empty;
                    if (!string.IsNullOrEmpty(Prefix))
                    {
                        prefix += Prefix;
                    }

                    foreach (var child in Children)
                    {
                        foreach (var childName in child.GetMemberNames(prefix))
                        {
                            yield return childName;
                        }
                    }
                    yield break;
                }

                var memberName = GetBaseName();
                _name = string.IsNullOrEmpty(parentPrefix) ? memberName : $"{parentPrefix}{memberName}";
                yield return _name;
            }

            private string GetBaseName()
            {
                var baseName = Member.GetCustomAttribute<PandasColumnAttribute>()?.Name;
                if (string.IsNullOrEmpty(baseName))
                {
                    baseName = Member.Name;
                }

                return baseName.ToLowerInvariant();
            }
        }
    }
}
