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
                    // this python Remapper class will work as a proxy and adjust the
                    // input to its methods using the provided 'mapper' callable object
                    _pandas = PythonEngine.ModuleFromString("remapper",
                        @"import pandas as pd
from pandas.core.resample import Resampler, DatetimeIndexResampler, PeriodIndexResampler, TimedeltaIndexResampler
from pandas.core.groupby.generic import DataFrameGroupBy, SeriesGroupBy
from pandas.core.indexes.frozen import FrozenList as pdFrozenList
from pandas.core.window import Expanding, EWM, Rolling, Window
from pandas.core.computation.ops import UndefinedVariableError
from inspect import getmembers, isfunction, isgenerator
from functools import partial
from sys import modules

from clr import AddReference
AddReference(""QuantConnect.Common"")
from QuantConnect import *

def mapper(key):
    '''Maps a Symbol object or a Symbol Ticker (string) to the string representation of
    Symbol SecurityIdentifier. If cannot map, returns the object
    '''
    keyType = type(key)
    if keyType is Symbol:
        return str(key.ID)
    if keyType is str:
        reserved = ['high', 'low', 'open', 'close']
        if key in reserved:
            return key
        kvp = SymbolCache.TryGetSymbol(key, None)
        if kvp[0]:
            return str(kvp[1].ID)
    if keyType is list:
        return [mapper(x) for x in key]
    if keyType is tuple:
        return tuple([mapper(x) for x in key])
    if keyType is dict:
        return {k:mapper(v) for k,v in key.items()}
    return key

def try_wrap_as_index(obj):
    '''Tries to wrap object if it is one of pandas' index objects.'''

    objType = type(obj)

    if objType is pd.Index:
        return True, Index(obj)

    if objType is pd.MultiIndex:
        result = object.__new__(MultiIndex)
        result._set_levels(obj.levels, copy=obj.copy, validate=False)
        result._set_codes(obj.codes, copy=obj.copy, validate=False)
        result._set_names(obj.names)
        result.sortorder = obj.sortorder
        return True, result

    if objType is pdFrozenList:
        return True, FrozenList(obj)

    return False, obj

def try_wrap_as_pandas(obj):
    '''Tries to wrap object if it is a pandas' object.'''

    success, obj = try_wrap_as_index(obj)
    if success:
        return success, obj

    objType = type(obj)

    if objType is pd.DataFrame:
        return True, DataFrame(data=obj)

    if objType is pd.Series:
        return True, Series(data=obj)

    if objType is tuple:
        anySuccess = False
        results = list()
        for item in obj:
            success, result = try_wrap_as_pandas(item)
            anySuccess |= success
            results.append(result)
        if anySuccess:
            return True, tuple(results)

    return False, obj

def try_wrap_resampler(obj, self):
    '''Tries to wrap object if it is a pandas' Resampler object.'''

    if not isinstance(obj, Resampler):
        return False, obj

    klass = CreateWrapperClass(type(obj))
    return True, klass(self, groupby=obj.groupby, kind=obj.kind, axis=obj.axis)

def wrap_function(f):
    '''Wraps function f with g.
    Function g converts the args/kwargs to use alternative index keys
    and the result of the f function call to the wrapper objects
    '''
    def g(*args, **kwargs):

        if len(args) > 1:
            args = mapper(args)
        if len(kwargs) > 0:
            kwargs = mapper(kwargs)

        try:
            result = f(*args, **kwargs)
        except UndefinedVariableError as e:
            # query/eval methods needs to look for a scope variable at a higher level
            # since the wrapper classes are children of pandas classes
            kwargs['level'] = kwargs.pop('level', 0) + 1
            result = f(*args, **kwargs)

        success, result = try_wrap_as_pandas(result)
        if success:
            return result

        success, result = try_wrap_resampler(result, args[0])
        if success:
            return result

        if isgenerator(result):
            return ( (k, try_wrap_as_pandas(v)[1]) for k, v in result)

        return result

    g.__name__ = f.__name__
    return g

def wrap_special_function(name, cls, fcls, gcls = None):
    '''Replaces the special function of a given class by g that wraps fcls
    This is how pandas implements them.
    gcls represents an alternative for fcls
    if the keyword argument has 'win_type' key for the Rolling/Window case
    '''
    fcls = CreateWrapperClass(fcls)
    if gcls is not None:
        gcls = CreateWrapperClass(fcls)

    def g(*args, **kwargs):
        if kwargs.get('win_type', None):
            return gcls(*args, **kwargs)
        return fcls(*args, **kwargs)
    g.__name__ = name
    setattr(cls, g.__name__, g)

def CreateWrapperClass(cls: type):
    '''Creates wrapper classes.
    Members of the original class are wrapped to allow alternative index look-up
    '''
    # Define a new class
    klass = type(f'{cls.__name__}', (cls,) + cls.__bases__, dict(cls.__dict__))

    def g(self, name):
        '''Wrap '__getattribute__' to handle indices
        Only need to wrap columns, index and levels attributes
        '''
        attr = object.__getattribute__(self, name)
        if name in ['columns', 'index', 'levels']:
            _, attr = try_wrap_as_index(attr)
        return attr
    g.__name__ =  '__getattribute__'
    g.__qualname__ =  g.__name__
    setattr(klass, g.__name__, g)

    def wrap_union(f):
        '''Wraps function f (union) with g.
        Special case: The union method from index objects needs to
        receive pandas' index objects to avoid infity recursion.
        Function g converts the args/kwargs objects to one of pandas index objects
        and the result of the f function call back to wrapper indexes objects
        '''
        def unwrap_index(obj):
            '''Tries to unwrap object if it is one of this module wrapper's index objects.'''
            objType = type(obj)

            if objType is Index:
                return pd.Index(obj)

            if objType is MultiIndex:
                result = object.__new__(pd.MultiIndex)
                result._set_levels(obj.levels, copy=obj.copy, validate=False)
                result._set_codes(obj.codes, copy=obj.copy, validate=False)
                result._set_names(obj.names)
                result.sortorder = obj.sortorder
                return result

            if objType is FrozenList:
                return pdFrozenList(obj)

            return obj

        def g(*args, **kwargs):

            args = tuple([unwrap_index(x) for x in args])
            result = f(*args, **kwargs)
            _, result = try_wrap_as_index(result)
            return result

        g.__name__ = f.__name__
        return g

    # We allow the wraopping of slot methods that are not inherited from object
    # It will include operation methods like __add__ and __contains__
    allow_list = set(x for x in dir(klass) if x.startswith('__')) - set(dir(object))

    # Wrap class members of the newly created class
    for name, member in getmembers(klass):
        if name.startswith('_') and name not in allow_list:
            continue

        if isfunction(member):
            if name == 'union':
                member = wrap_union(member)
            else:
                member = wrap_function(member)
            setattr(klass, name, member)

        elif type(member) is property:
            if type(member.fget) is partial:
                func = CreateWrapperClass(member.fget.func)
                fget = partial(func, name)
            else:
                fget = wrap_function(member.fget)
            member = property(fget, member.fset, member.fdel, member.__doc__)
            setattr(klass, name, member)

    return klass

FrozenList = CreateWrapperClass(pdFrozenList)
Index = CreateWrapperClass(pd.Index)
MultiIndex = CreateWrapperClass(pd.MultiIndex)
Series = CreateWrapperClass(pd.Series)
DataFrame = CreateWrapperClass(pd.DataFrame)

wrap_special_function('groupby', Series, SeriesGroupBy)
wrap_special_function('groupby', DataFrame, DataFrameGroupBy)
wrap_special_function('ewm', Series, EWM)
wrap_special_function('ewm', DataFrame, EWM)
wrap_special_function('expanding', Series, Expanding)
wrap_special_function('expanding', DataFrame, Expanding)
wrap_special_function('rolling', Series, Rolling, Window)
wrap_special_function('rolling', DataFrame, Rolling, Window)

CreateSeries = pd.Series

setattr(modules[__name__], 'concat', wrap_function(pd.concat))");
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
            if (_symbol.SecurityType == SecurityType.Option || _symbol.SecurityType == SecurityType.FutureOption) Levels = 5;

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
            if (_symbol.SecurityType == SecurityType.Option || _symbol.SecurityType == SecurityType.FutureOption)
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

                    // Adds pandas.Series value keyed by the column name
                    // CreateSeries will create an original pandas.Series
                    // We are not using the wrapper class to avoid unnecessary and expensive
                    // index wrapping operations when the Series are packed into a DataFrame
                    pyDict.SetItem(kvp.Key, _pandas.CreateSeries(values, index));
                }
                _series.Clear();

                // Create a DataFrame with wrapper class.
                // This is the starting point. The types of all DataFrame and Series that result from any operation will
                // be wrapper classes. Index and MultiIndex will be converted when required by index operations such as
                // stack, unstack, merge, union, etc.
                return _pandas.DataFrame(pyDict);
            }
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
    }
}
