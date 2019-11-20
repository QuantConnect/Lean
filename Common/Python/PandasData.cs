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
        private static dynamic _pandas;
        private static dynamic _remapperFactory;
        private readonly static HashSet<string> _baseDataProperties = typeof(BaseData).GetProperties().ToHashSet(x => x.Name.ToLowerInvariant());
        private readonly static ConcurrentDictionary<Type, List<MemberInfo>> _membersByType = new ConcurrentDictionary<Type, List<MemberInfo>>();

        private readonly Symbol _symbol;
        private readonly Dictionary<string, Tuple<List<DateTime>, List<object>>> _series;

        private readonly List<MemberInfo> _members;

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
                    _pandas = Py.Import("pandas");

                    // this python Remapper class will work as a proxy and adjust the
                    // input to its methods using the provided 'mapper' callable object
                    _remapperFactory = PythonEngine.ModuleFromString("remapper",
                        @"import wrapt
import pandas
from clr import AddReference
AddReference(""QuantConnect.Common"")
from QuantConnect import *

originalConcat = pandas.concat

def PandasConcatWrapper(objs, axis=0, join='outer', join_axes=None, ignore_index=False, keys=None, levels=None, names=None, verify_integrity=False, sort=None, copy=True):
    return Remapper(originalConcat(objs, axis, join, join_axes, ignore_index, keys, levels, names, verify_integrity, sort, copy))

pandas.concat = PandasConcatWrapper

class Remapper(wrapt.ObjectProxy):
    def __init__(self, wrapped):
        super(Remapper, self).__init__(wrapped)

    # Our remapping method. Originally implemented in C# but some cases were not working
    # correctly and using Py did the trick
    def _self_mapper(self, key):
        # this is to improve user experience, they can use Symbol
        # as key and we convert it to string for pandas
        if isinstance(key, Symbol):
            key = str(key.ID)
        # this is the most normal use case
        elif isinstance(key, str):
            tupleResult = SymbolCache.TryGetSymbol(key, None)
            if tupleResult[0]:
                return str(tupleResult[1].ID)

        # this case is required to cover 'df.at[]'
        elif isinstance(key, tuple) and 2 >= len(key) >= 1:
            keyElement = key[0]

            if isinstance(keyElement, tuple) and 2 >= len(keyElement) >= 1:
                keyElement = keyElement[0]

                if isinstance(keyElement, str):
                    tupleResult = SymbolCache.TryGetSymbol(keyElement, None)
                    if tupleResult[0]:
                        result = str(tupleResult[1].ID)
                        # tuples can not be modified in Py so we generate new ones
                        if len(key[0]) == 1:
                            firstTuple = (result,)
                        else:
                            firstTuple = (result, key[0][1])
                        if len(key[1]) == 1:
                            return (firstTuple,)
                        else:
                            return (firstTuple, key[1])
        return key

    def __contains__(self, key):
        key = self._self_mapper(key)
        return self.__wrapped__.__contains__(key)

    def __getitem__(self, name):
        name = self._self_mapper(name)
        result = self.__wrapped__.__getitem__(name)

        if isinstance(result, (pandas.Series, pandas.Index)):
            # For these cases we wrap the result too. Can't apply the wrap around all
            # results because it causes issues in pandas for some of our use cases
            # specifically pandas timestamp type
            return Remapper(result)
        return result

    def __setitem__(self, name, value):
        name = self._self_mapper(name)
        return self.__wrapped__.__setitem__(name, value)

    def __delitem__(self, name):
        name = self._self_mapper(name)
        return self.__wrapped__.__delitem__(name)

    # we wrap the result and input of 'xs'
    def xs(self, key, axis=0, level=None, drop_level=True):
        key = self._self_mapper(key)
        result = self.__wrapped__.xs(key=key, axis=axis, level=level, drop_level=drop_level)
        return Remapper(result)

    def get(self, key, default=None):
        key = self._self_mapper(key)
        return self.__wrapped__.get(key=key, default=default)

    # we wrap the result of 'unstack'
    def unstack(self, level=-1, fill_value=None):
        result = self.__wrapped__.unstack(level=level, fill_value=fill_value)
        return Remapper(result)

    def join(self, other, on=None, how='left', lsuffix='', rsuffix='', sort=False):
        result = self.__wrapped__.join(other=other, on=on, how=how, lsuffix=lsuffix, rsuffix=rsuffix, sort=sort)
        return Remapper(result)

    def append(self, other, ignore_index=False, verify_integrity=False, sort=None):
        result = self.__wrapped__.append(other=other, ignore_index=ignore_index, verify_integrity=verify_integrity, sort=sort)
        return Remapper(result)

    def merge(self, right, how='inner', on=None, left_on=None, right_on=None, left_index=False, right_index=False, sort=False, suffixes=('_x', '_y'), copy=True, indicator=False, validate=None):
        result = self.__wrapped__.merge(right=right, how=how, on=on, left_on=left_on, right_on=right_on, left_index=left_index, right_index=right_index, sort=sort, suffixes=suffixes, copy=copy, indicator=indicator, validate=validate)
        return Remapper(result)

    # we wrap 'loc' to cover the: df.loc['SPY'] case
    @property
    def loc(self):
        return Remapper(self.__wrapped__.loc)

    @property
    def ix(self):
        return Remapper(self.__wrapped__.ix)

    @property
    def iloc(self):
        return Remapper(self.__wrapped__.iloc)

    @property
    def at(self):
        return Remapper(self.__wrapped__.at)

    @property
    def index(self):
        return Remapper(self.__wrapped__.index)

    @property
    def levels(self):
        return Remapper(self.__wrapped__.levels)

    # we wrap the following properties so that when 'unstack', 'loc' are called we wrap them
    @property
    def open(self):
        return Remapper(self.__wrapped__.open)
    @property
    def high(self):
        return Remapper(self.__wrapped__.high)
    @property
    def close(self):
        return Remapper(self.__wrapped__.close)
    @property
    def low(self):
        return Remapper(self.__wrapped__.low)
    @property
    def lastprice(self):
        return Remapper(self.__wrapped__.lastprice)
    @property
    def volume(self):
        return Remapper(self.__wrapped__.volume)
    @property
    def askopen(self):
        return Remapper(self.__wrapped__.askopen)
    @property
    def askhigh(self):
        return Remapper(self.__wrapped__.askhigh)
    @property
    def asklow(self):
        return Remapper(self.__wrapped__.asklow)
    @property
    def askclose(self):
        return Remapper(self.__wrapped__.askclose)
    @property
    def askprice(self):
        return Remapper(self.__wrapped__.askprice)
    @property
    def asksize(self):
        return Remapper(self.__wrapped__.asksize)
    @property
    def quantity(self):
        return Remapper(self.__wrapped__.quantity)
    @property
    def suspicious(self):
        return Remapper(self.__wrapped__.suspicious)
    @property
    def bidopen(self):
        return Remapper(self.__wrapped__.bidopen)
    @property
    def bidhigh(self):
        return Remapper(self.__wrapped__.bidhigh)
    @property
    def bidlow(self):
        return Remapper(self.__wrapped__.bidlow)
    @property
    def bidclose(self):
        return Remapper(self.__wrapped__.bidclose)
    @property
    def bidprice(self):
        return Remapper(self.__wrapped__.bidprice)
    @property
    def bidsize(self):
        return Remapper(self.__wrapped__.bidsize)
    @property
    def exchange(self):
        return Remapper(self.__wrapped__.exchange)
    @property
    def openinterest(self):
        return Remapper(self.__wrapped__.openinterest)
").GetAttr("Remapper");
                }
            }

            var enumerable = data as IEnumerable;
            if (enumerable != null)
            {
                foreach (var item in enumerable)
                {
                    data = item;
                }
            }

            var type = data.GetType();
            IsCustomData = type.Namespace != typeof(Bar).Namespace;
            _members = new List<MemberInfo>();
            _symbol = ((IBaseData)data).Symbol;

            if (_symbol.SecurityType == SecurityType.Future) Levels = 3;
            if (_symbol.SecurityType == SecurityType.Option) Levels = 5;

            var columns = new HashSet<string>
            {
                   "open",    "high",    "low",    "close", "lastprice",  "volume",
                "askopen", "askhigh", "asklow", "askclose",  "askprice", "asksize", "quantity", "suspicious",
                "bidopen", "bidhigh", "bidlow", "bidclose",  "bidprice", "bidsize", "exchange", "openinterest"
            };

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
                            throw new ArgumentException($"PandasData.ctor(): More than one \'{duplicateKey}\' member was found in \'{type.FullName}\' class.");
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

                columns.Add("value");
                columns.UnionWith(keys);
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
                var endTime = ((IBaseData) baseData).EndTime;
                AddToSeries(key, endTime, (member as FieldInfo)?.GetValue(baseData));
                AddToSeries(key, endTime, (member as PropertyInfo)?.GetValue(baseData));
            }

            var storage = (baseData as DynamicData)?.GetStorageDictionary();
            if (storage != null)
            {
                var endTime = ((IBaseData) baseData).EndTime;
                var value = ((IBaseData) baseData).Value;
                AddToSeries("value", endTime, value);

                foreach (var kvp in storage)
                {
                    AddToSeries(kvp.Key, endTime, kvp.Value);
                }
            }
            else
            {
                var ticks = new List<Tick> { baseData as Tick };
                var tradeBar = baseData as TradeBar;
                var quoteBar = baseData as QuoteBar;
                Add(ticks, tradeBar, quoteBar);
            }
        }

        /// <summary>
        /// Adds Lean data objects to the end of the lists
        /// </summary>
        /// <param name="ticks">List of <see cref="Tick"/> object that contains tick information of the security</param>
        /// <param name="tradeBar"><see cref="TradeBar"/> object that contains trade bar information of the security</param>
        /// <param name="quoteBar"><see cref="QuoteBar"/> object that contains quote bar information of the security</param>
        public void Add(IEnumerable<Tick> ticks, TradeBar tradeBar, QuoteBar quoteBar)
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
            if (ticks != null)
            {
                foreach (var tick in ticks)
                {
                    if (tick == null) continue;

                    var time = tick.EndTime;
                    var column = tick.TickType == TickType.OpenInterest
                        ? "openinterest"
                        : "lastprice";

                    if (tick.TickType == TickType.Quote)
                    {
                        AddToSeries("askprice", time, tick.AskPrice);
                        AddToSeries("asksize", time, tick.AskSize);
                        AddToSeries("bidprice", time, tick.BidPrice);
                        AddToSeries("bidsize", time, tick.BidSize);
                    }
                    AddToSeries("exchange", time, tick.Exchange);
                    AddToSeries("suspicious", time, tick.Suspicious);
                    AddToSeries("quantity", time, tick.Quantity);
                    AddToSeries(column, time, tick.LastPrice);
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
            var empty = new PyString(string.Empty);
            var list = Enumerable.Repeat<PyObject>(empty, 5).ToList();
            list[3] = _symbol.ID.ToString().ToPython();

            if (_symbol.SecurityType == SecurityType.Future)
            {
                list[0] = _symbol.ID.Date.ToPython();
                list[3] = _symbol.ID.ToString().ToPython();
            }
            if (_symbol.SecurityType == SecurityType.Option)
            {
                list[0] = _symbol.ID.Date.ToPython();
                list[1] = _symbol.ID.StrikePrice.ToPython();
                list[2] = _symbol.ID.OptionRight.ToString().ToPython();
                list[3] = _symbol.ID.ToString().ToPython();
            }

            // Create the index labels
            var names = "expiry,strike,type,symbol,time";
            if (levels == 2)
            {
                names = "symbol,time";
                list.RemoveRange(0, 3);
            }
            if (levels == 3)
            {
                names = "expiry,symbol,time";
                list.RemoveRange(1, 2);
            }

            Func<object, bool> filter = x =>
            {
                var isNaNOrZero = x is double && ((double)x).IsNaNOrZero();
                var isNullOrWhiteSpace = x is string && string.IsNullOrWhiteSpace((string)x);
                var isFalse = x is bool && !(bool)x;
                return x == null || isNaNOrZero || isNullOrWhiteSpace || isFalse;
            };
            Func<DateTime, PyTuple> selector = x =>
            {
                list[list.Count - 1] = x.ToPython();
                return new PyTuple(list.ToArray());
            };
            // creating the pandas MultiIndex is expensive so we keep a cash
            var indexCache = new Dictionary<List<DateTime>, dynamic>(new ListComparer<DateTime>());
            using (Py.GIL())
            {
                // Returns a dictionary keyed by column name where values are pandas.Series objects
                var pyDict = new PyDict();
                var splitNames = names.Split(',');
                foreach (var kvp in _series)
                {
                    var values = kvp.Value.Item2;
                    if (values.All(filter)) continue;

                    dynamic index;
                    if (!indexCache.TryGetValue(kvp.Value.Item1, out index))
                    {
                        var tuples = kvp.Value.Item1.Select(selector).ToArray();
                        index = _pandas.MultiIndex.from_tuples(tuples, names: splitNames);
                        indexCache[kvp.Value.Item1] = index;
                    }

                    pyDict.SetItem(kvp.Key, _pandas.Series(values, index));
                }
                _series.Clear();
                return ApplySymbolMapper(_pandas.DataFrame(pyDict));
            }
        }

        /// <summary>
        /// Adds data to dictionary
        /// </summary>
        /// <param name="key">The key of the value to get</param>
        /// <param name="time"><see cref="DateTime"/> object to add to the value associated with the specific key</param>
        /// <param name="input"><see cref="Object"/> to add to the value associated with the specific key</param>
        private void AddToSeries(string key, DateTime time, object input)
        {
            if (input == null) return;

            Tuple<List<DateTime>, List<object>> value;
            if (_series.TryGetValue(key, out value))
            {
                value.Item1.Add(time);
                value.Item2.Add(input is decimal ? input.ConvertInvariant<double>() : input);
            }
            else
            {
                throw new ArgumentException($"PandasData.AddToSeries(): {key} key does not exist in series dictionary.");
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

        /// <summary>
        /// Will wrap the provided pandas data frame using the <see cref="_remapperFactory"/>
        /// </summary>
        internal static dynamic ApplySymbolMapper(dynamic pandasDataFrame)
        {
            return _remapperFactory.Invoke(pandasDataFrame);
        }
    }
}