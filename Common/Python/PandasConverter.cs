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

using Apache.Arrow;
using Apache.Arrow.Ipc;
using Apache.Arrow.Memory;
using Python.Runtime;
using QuantConnect.Data;
using QuantConnect.Indicators;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IdentityModel.Protocols.WSTrust;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Configuration;
using System.Threading.Tasks;
using Apache.Arrow.Types;
using QuantConnect.Data.Market;
using QuantConnect.Securities;
using QuantConnect.Util;
using Array = System.Array;

namespace QuantConnect.Python
{
    /// <summary>
    /// Collection of methods that converts lists of objects in pandas.DataFrame
    /// </summary>
    public class PandasConverter
    {
        private static dynamic _pandas;
        private static dynamic _pa;
        private static HashSet<string> _baseDataProperties = typeof(BaseData).GetProperties().ToHashSet(x => x.Name.ToLowerInvariant());

        private PandasArrowMemoryAllocator allocator = new PandasArrowMemoryAllocator();

        private StringArray.Builder tradeBarSymbols = new StringArray.Builder();
        private TimestampArray.Builder tradeBarTimes = new TimestampArray.Builder();

        private TimestampArray.Builder tradeBarExpiry = new TimestampArray.Builder();
        private DoubleArray.Builder tradeBarStrike = new DoubleArray.Builder();
        private StringArray.Builder tradeBarRight = new StringArray.Builder();

        private DoubleArray.Builder tradeBarOpen = new DoubleArray.Builder();
        private DoubleArray.Builder tradeBarHigh = new DoubleArray.Builder();
        private DoubleArray.Builder tradeBarLow = new DoubleArray.Builder();
        private DoubleArray.Builder tradeBarClose = new DoubleArray.Builder();
        private DoubleArray.Builder tradeBarVolume = new DoubleArray.Builder();

        private StringArray.Builder quoteBarSymbols = new StringArray.Builder();
        private TimestampArray.Builder quoteBarTimes = new TimestampArray.Builder();

        private TimestampArray.Builder quoteBarExpiry = new TimestampArray.Builder();
        private DoubleArray.Builder quoteBarStrike = new DoubleArray.Builder();
        private StringArray.Builder quoteBarRight = new StringArray.Builder();

        private DoubleArray.Builder quoteBarBidOpen = new DoubleArray.Builder();
        private DoubleArray.Builder quoteBarBidHigh = new DoubleArray.Builder();
        private DoubleArray.Builder quoteBarBidLow = new DoubleArray.Builder();
        private DoubleArray.Builder quoteBarBidClose = new DoubleArray.Builder();
        private DoubleArray.Builder quoteBarBidVolume = new DoubleArray.Builder();
        private DoubleArray.Builder quoteBarAskOpen = new DoubleArray.Builder();
        private DoubleArray.Builder quoteBarAskHigh = new DoubleArray.Builder();
        private DoubleArray.Builder quoteBarAskLow = new DoubleArray.Builder();
        private DoubleArray.Builder quoteBarAskClose = new DoubleArray.Builder();
        private DoubleArray.Builder quoteBarAskVolume = new DoubleArray.Builder();

        private StringArray.Builder tickSymbols = new StringArray.Builder();
        private TimestampArray.Builder tickTimes = new TimestampArray.Builder();

        private TimestampArray.Builder tickExpiry = new TimestampArray.Builder();
        private DoubleArray.Builder tickStrike = new DoubleArray.Builder();
        private StringArray.Builder tickRight = new StringArray.Builder();

        private StringArray.Builder tickExchange = new StringArray.Builder();
        private BooleanArray.Builder tickSuspicious = new BooleanArray.Builder();

        private DoubleArray.Builder tickValue = new DoubleArray.Builder();
        private DoubleArray.Builder tickQuantity = new DoubleArray.Builder();
        private DoubleArray.Builder tickBidPrice = new DoubleArray.Builder();
        private DoubleArray.Builder tickBidSize = new DoubleArray.Builder();
        private DoubleArray.Builder tickAskPrice = new DoubleArray.Builder();
        private DoubleArray.Builder tickAskSize = new DoubleArray.Builder();

        private TimestampArray.Builder openInterestTimes = new TimestampArray.Builder();
        private StringArray.Builder openInterestSymbols = new StringArray.Builder();

        private TimestampArray.Builder openInterestExpiry = new TimestampArray.Builder();
        private DoubleArray.Builder openInterestStrike = new DoubleArray.Builder();
        private StringArray.Builder openInterestRight = new StringArray.Builder();

        private DoubleArray.Builder openInterestValue = new DoubleArray.Builder();

        private Dictionary<Type, List<MemberInfo>> _customDataMembers = new Dictionary<Type, List<MemberInfo>>();
        private Dictionary<string, IArrowArrayBuilder> _customDataBuilders = new Dictionary<string, IArrowArrayBuilder>();

        private List<string> _customDataSymbols = new List<string>();
        private List<DateTimeOffset> _customDataTimes = new List<DateTimeOffset>();
        private Dictionary<string, List<PyObject>> _customDataObjects = new Dictionary<string, List<PyObject>>();

        private bool hasExchange;
        private bool hasSuspicious;
        private bool hasExpiry;
        private bool hasStrike;
        private bool hasOptionRight;
        private bool tickHasTrades;
        private bool tickHasQuotes;

        /// <summary>
        /// Creates an instance of <see cref="PandasConverter"/>.
        /// </summary>
        public PandasConverter()
        {
            if (_pandas == null)
            {
                using (Py.GIL())
                {
                    // pyarrow is used to create a DataFrame without having to serialize the data.
                    // It also allows us to construct a DataFrame as a zero-copy operation.
                    _pa = PythonEngine.ImportModule("pyarrow");

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
        }

        /// <summary>
        /// Converts an enumerable of <see cref="Slice"/> in a pandas.DataFrame
        /// </summary>
        /// <param name="data">Enumerable of <see cref="Slice"/></param>
        /// <returns><see cref="PyObject"/> containing a pandas.DataFrame</returns>
        public PyObject GetDataFrame(IEnumerable<Slice> data)
        {
            ClearBuilders();

            foreach (var slice in data)
            {
                foreach (var symbol in slice.Keys)
                {
                    var tradeBar = slice.Bars.ContainsKey(symbol) ? slice.Bars[symbol] : null;
                    var quoteBar = slice.QuoteBars.ContainsKey(symbol) ? slice.QuoteBars[symbol] : null;
                    var ticks = slice.Ticks.ContainsKey(symbol) ? slice.Ticks[symbol] : null;
                    var sid = symbol.ID.ToString();

                    if (tradeBar != null)
                    {
                        tradeBarOpen.Append((double) tradeBar.Open);
                        tradeBarHigh.Append((double) tradeBar.High);
                        tradeBarLow.Append((double) tradeBar.Low);
                        tradeBarClose.Append((double) tradeBar.Close);
                        tradeBarVolume.Append((double) tradeBar.Volume);

                        tradeBarSymbols.Append(sid);
                        tradeBarTimes.Append(new DateTimeOffset(tradeBar.EndTime.Ticks, TimeSpan.Zero));

                        if (symbol.SecurityType == SecurityType.Future || symbol.SecurityType == SecurityType.Option)
                        {
                            hasExpiry = true;
                            tradeBarExpiry.Append(new DateTimeOffset(symbol.ID.Date.Ticks, TimeSpan.Zero));
                        }
                        else
                        {
                            tradeBarExpiry.AppendNull();
                        }
                        if (symbol.SecurityType == SecurityType.Option)
                        {
                            hasStrike = true;
                            hasOptionRight = true;
                            tradeBarStrike.Append((double) symbol.ID.StrikePrice);
                            tradeBarRight.Append(symbol.ID.OptionRight.ToString());
                        }
                        else
                        {
                            tradeBarStrike.AppendNull();
                            tradeBarRight.AppendNull();
                        }
                    }

                    if (quoteBar != null)
                    {
                        // To maintain old behavior and backwards compatibility, we will set the "OHLC" for TradeBars
                        // when no TradeBar exists in this timestep.
                        if (tradeBar == null)
                        {
                            tradeBarOpen.Append((double) quoteBar.Open);
                            tradeBarHigh.Append((double) quoteBar.High);
                            tradeBarLow.Append((double) quoteBar.Low);
                            tradeBarClose.Append((double) quoteBar.Close);
                            tradeBarVolume.AppendNull();

                            tradeBarSymbols.Append(sid);
                            tradeBarTimes.Append(new DateTimeOffset(quoteBar.EndTime.Ticks, TimeSpan.Zero));

                            if (symbol.SecurityType == SecurityType.Future || symbol.SecurityType == SecurityType.Option)
                            {
                                hasExpiry = true;
                                tradeBarExpiry.Append(new DateTimeOffset(symbol.ID.Date.Ticks, TimeSpan.Zero));
                            }
                            else
                            {
                                tradeBarExpiry.AppendNull();
                            }
                            if (symbol.SecurityType == SecurityType.Option)
                            {
                                hasStrike = true;
                                hasOptionRight = true;
                                tradeBarStrike.Append((double) symbol.ID.StrikePrice);
                                tradeBarRight.Append(symbol.ID.OptionRight.ToString());
                            }
                            else
                            {
                                tradeBarStrike.AppendNull();
                                tradeBarRight.AppendNull();
                            }
                        }
                        if (quoteBar.Bid != null)
                        {
                            quoteBarBidOpen.Append((double) quoteBar.Bid.Open);
                            quoteBarBidHigh.Append((double) quoteBar.Bid.High);
                            quoteBarBidLow.Append((double) quoteBar.Bid.Low);
                            quoteBarBidClose.Append((double) quoteBar.Bid.Close);
                            quoteBarBidVolume.Append((double) quoteBar.LastBidSize);
                        }
                        else
                        {
                            quoteBarBidOpen.AppendNull();
                            quoteBarBidHigh.AppendNull();
                            quoteBarBidLow.AppendNull();
                            quoteBarBidClose.AppendNull();
                            quoteBarBidVolume.AppendNull();
                        }

                        if (quoteBar.Ask != null)
                        {
                            quoteBarAskOpen.Append((double) quoteBar.Ask.Open);
                            quoteBarAskHigh.Append((double) quoteBar.Ask.High);
                            quoteBarAskLow.Append((double) quoteBar.Ask.Low);
                            quoteBarAskClose.Append((double) quoteBar.Ask.Close);
                            quoteBarAskVolume.Append((double) quoteBar.LastAskSize);
                        }
                        else
                        {
                            quoteBarAskOpen.AppendNull();
                            quoteBarAskHigh.AppendNull();
                            quoteBarAskLow.AppendNull();
                            quoteBarAskClose.AppendNull();
                            quoteBarAskVolume.AppendNull();
                        }

                        quoteBarSymbols.Append(quoteBar.Symbol.ID.ToString());
                        quoteBarTimes.Append(new DateTimeOffset(quoteBar.EndTime.Ticks, TimeSpan.Zero));

                        if (symbol.SecurityType == SecurityType.Future || symbol.SecurityType == SecurityType.Option)
                        {
                            hasExpiry = true;
                            quoteBarExpiry.Append(new DateTimeOffset(symbol.ID.Date.Ticks, TimeSpan.Zero));
                        }
                        else
                        {
                            quoteBarExpiry.AppendNull();
                        }
                        if (symbol.SecurityType == SecurityType.Option)
                        {
                            hasStrike = true;
                            hasOptionRight = true;
                            quoteBarStrike.Append((double) symbol.ID.StrikePrice);
                            quoteBarRight.Append(symbol.ID.OptionRight.ToString());
                        }
                        else
                        {
                            quoteBarStrike.AppendNull();
                            quoteBarRight.AppendNull();
                        }
                    }

                    if (ticks != null)
                    {
                        foreach (var tick in ticks)
                        {
                            if (tick.TickType == TickType.Trade || tick.TickType == TickType.Quote)
                            {
                                tickSymbols.Append(sid);
                                tickTimes.Append(new DateTimeOffset(tick.EndTime.Ticks, TimeSpan.Zero));
                                if (symbol.SecurityType == SecurityType.Future || symbol.SecurityType == SecurityType.Option)
                                {
                                    hasExpiry = true;
                                    tickExpiry.Append(new DateTimeOffset(symbol.ID.Date.Ticks, TimeSpan.Zero));
                                }
                                else
                                {
                                    tickExpiry.AppendNull();
                                }
                                if (symbol.SecurityType == SecurityType.Option)
                                {
                                    hasStrike = true;
                                    hasOptionRight = true;
                                    tickStrike.Append((double) symbol.ID.StrikePrice);
                                    tickRight.Append(symbol.ID.OptionRight.ToString());
                                }
                                else
                                {
                                    tickStrike.AppendNull();
                                    tickRight.AppendNull();
                                }
                            }

                            if (tick.TickType == TickType.Trade)
                            {
                                tickHasTrades = true;

                                tickValue.Append((double) tick.Value);
                                tickQuantity.Append((double) tick.Quantity);

                                if (tick.Suspicious && !hasSuspicious)
                                {
                                    hasSuspicious = true;
                                }
                                if (!string.IsNullOrWhiteSpace(tick.Exchange) && !hasExchange)
                                {
                                    hasExchange = true;
                                }

                                tickSuspicious.Append(tick.Suspicious);
                                tickExchange.Append(tick.Exchange);

                                tickBidPrice.AppendNull();
                                tickBidSize.AppendNull();
                                tickAskPrice.AppendNull();
                                tickAskSize.AppendNull();
                            }
                            else if (tick.TickType == TickType.Quote)
                            {
                                tickHasQuotes = true;

                                tickValue.AppendNull();
                                tickQuantity.AppendNull();

                                tickSuspicious.Append(tick.Suspicious);
                                tickExchange.Append(tick.Exchange);

                                tickBidPrice.Append((double) tick.BidPrice);
                                tickBidSize.Append((double) tick.BidSize);
                                tickAskPrice.Append((double) tick.AskPrice);
                                tickAskSize.Append((double) tick.AskSize);
                            }
                            else
                            {
                                openInterestTimes.Append(new DateTimeOffset(tick.EndTime.Ticks, TimeSpan.Zero));
                                openInterestSymbols.Append(sid);
                                openInterestValue.Append((double)tick.Value);

                                if (symbol.SecurityType == SecurityType.Future || symbol.SecurityType == SecurityType.Option)
                                {
                                    hasExpiry = true;
                                    openInterestExpiry.Append(new DateTimeOffset(symbol.ID.Date.Ticks, TimeSpan.Zero));
                                }
                                else
                                {
                                    openInterestExpiry.AppendNull();
                                }
                                if (symbol.SecurityType == SecurityType.Option)
                                {
                                    hasStrike = true;
                                    hasOptionRight = true;
                                    openInterestStrike.Append((double) symbol.ID.StrikePrice);
                                    openInterestRight.Append(symbol.ID.OptionRight.ToString());
                                }
                                else
                                {
                                    openInterestStrike.AppendNull();
                                    openInterestRight.AppendNull();
                                }
                            }
                        }
                    }

                    if (tradeBar == null && quoteBar == null && ticks == null)
                    {
                        // If we've made it this far, we're dealing with an instance of custom data.
                        var baseData = (BaseData)slice[symbol];
                        var baseDataType = baseData.GetType();

                        var dynamicData = (baseData as DynamicData)?.GetStorageDictionary();
                        var dynamicColumns = dynamicData?.Select(kvp => kvp.Key)
                            ?.ToHashSet();

                        List<MemberInfo> customMembers;
                        if (dynamicColumns == null && !_customDataMembers.TryGetValue(baseDataType, out customMembers))
                        {
                            var members = baseDataType.GetMembers()
                                .Where(x => x.MemberType == MemberTypes.Field || x.MemberType == MemberTypes.Property)
                                .ToList();

                            var duplicateKeys = members.GroupBy(x => x.Name.ToLowerInvariant())
                                .Where(x => x.Count() > 1)
                                .Select(x => x.Key)
                                .ToList();

                            if (duplicateKeys.Count != 0)
                            {
                                throw new ArgumentException($"PandasConverter.GetDataFrame(): Duplicate keys \"{string.Join(", ", duplicateKeys)}\" were found in the class {baseDataType.FullName}");
                            }

                            // If the custom data derives from market data (i.e. Tick, TradeBar, QuoteBar), exclude its keys
                            var columns = members.Select(x => x.Name.ToLowerInvariant()).ToHashSet().ToArray();
                            var columnsFiltered = columns.Except(_baseDataProperties)
                                .Except(GetPropertiesNames(typeof(QuoteBar), baseDataType))
                                .Except(GetPropertiesNames(typeof(TradeBar), baseDataType))
                                .Except(GetPropertiesNames(typeof(Tick), baseDataType))
                                .ToList();

                            columnsFiltered.Add("value");
                            columns = columnsFiltered.ToArray();
                            _customDataMembers[baseDataType] = members.Where(x => columns.Contains(x.Name.ToLowerInvariant())).ToList();
                        }
                        else
                        {
                            _customDataMembers[baseDataType] = new List<MemberInfo>();
                        }

                        foreach (var member in _customDataMembers[baseDataType])
                        {
                            var columnName = member.Name;
                            var property = member as PropertyInfo;
                            var field = member as FieldInfo;
                            var memberType = property?.PropertyType ?? field.FieldType;

                            IArrowArrayBuilder builder;
                            if (!_customDataBuilders.TryGetValue(columnName, out builder))
                            {
                                builder = CreateBuilder(memberType);
                                _customDataBuilders[columnName] = builder;
                            }

                            if (!AppendToBuilder(builder, baseData, memberType, property, field))
                            {
                                List<PyObject> customObjects;
                                if (!_customDataObjects.TryGetValue(columnName, out customObjects))
                                {
                                    customObjects = new List<PyObject>();
                                    _customDataObjects[columnName] = customObjects;
                                }

                                customObjects.Add((property?.GetValue(baseData) ?? field.GetValue(baseData)).ToPython());
                            }
                        }

                        if (dynamicColumns != null)
                        {
                            foreach (var columnName in dynamicColumns)
                            {
                                var columnEntry = dynamicData[columnName];
                                var entryType = columnEntry.GetType();

                                IArrowArrayBuilder builder;
                                if (!_customDataBuilders.TryGetValue(columnName, out builder))
                                {
                                    builder = CreateBuilder(entryType);
                                    _customDataBuilders[columnName] = builder;
                                }

                                if (!AppendToBuilder(builder, entryType, columnEntry))
                                {
                                    List<PyObject> customObjects;
                                    if (!_customDataObjects.TryGetValue(columnName, out customObjects))
                                    {
                                        customObjects = new List<PyObject>();
                                        _customDataObjects[columnName] = customObjects;
                                    }

                                    customObjects.Add(columnEntry.ToPython());
                                }
                            }
                        }

                        _customDataTimes.Add(new DateTimeOffset(baseData.EndTime.Ticks, TimeSpan.Zero));
                        _customDataSymbols.Add(sid);
                    }
                }
            }

            var recordBatches = new List<RecordBatch>();

            if (tradeBarTimes.Length != 0)
            {
                var tradeBarRecordBatchBuilder = new RecordBatch.Builder(allocator);

                tradeBarRecordBatchBuilder.Append("time", false, tradeBarTimes.Build(allocator));
                tradeBarRecordBatchBuilder.Append("symbol", false, tradeBarSymbols.Build(allocator));
                tradeBarRecordBatchBuilder.Append("open", false, tradeBarOpen.Build(allocator));
                tradeBarRecordBatchBuilder.Append("high", false, tradeBarHigh.Build(allocator));
                tradeBarRecordBatchBuilder.Append("low", false, tradeBarLow.Build(allocator));
                tradeBarRecordBatchBuilder.Append("close", false, tradeBarClose.Build(allocator));
                tradeBarRecordBatchBuilder.Append("volume", true, tradeBarVolume.Build(allocator));
                if (hasExpiry)
                {
                    tradeBarRecordBatchBuilder.Append("expiry", true, tradeBarExpiry.Build(allocator));
                    hasExpiry = true;
                }
                if (hasStrike)
                {
                    tradeBarRecordBatchBuilder.Append("strike", true, tradeBarStrike.Build(allocator));
                    tradeBarRecordBatchBuilder.Append("type", true, tradeBarRight.Build(allocator));
                }

                recordBatches.Add(tradeBarRecordBatchBuilder.Build());
            }

            if (quoteBarTimes.Length != 0)
            {
                var quoteBarRecordBatchBuilder = new RecordBatch.Builder(allocator);

                quoteBarRecordBatchBuilder.Append("time", false, quoteBarTimes.Build(allocator));
                quoteBarRecordBatchBuilder.Append("symbol", false, quoteBarSymbols.Build(allocator));
                quoteBarRecordBatchBuilder.Append("bidopen", true, quoteBarBidOpen.Build(allocator));
                quoteBarRecordBatchBuilder.Append("bidhigh", true, quoteBarBidHigh.Build(allocator));
                quoteBarRecordBatchBuilder.Append("bidlow", true, quoteBarBidLow.Build(allocator));
                quoteBarRecordBatchBuilder.Append("bidclose", true, quoteBarBidClose.Build(allocator));
                quoteBarRecordBatchBuilder.Append("bidsize", true, quoteBarBidVolume.Build(allocator));
                quoteBarRecordBatchBuilder.Append("askopen", true, quoteBarAskOpen.Build(allocator));
                quoteBarRecordBatchBuilder.Append("askhigh", true, quoteBarAskHigh.Build(allocator));
                quoteBarRecordBatchBuilder.Append("asklow", true, quoteBarAskLow.Build(allocator));
                quoteBarRecordBatchBuilder.Append("askclose", true, quoteBarAskClose.Build(allocator));
                quoteBarRecordBatchBuilder.Append("asksize", true, quoteBarAskVolume.Build(allocator));
                if (hasExpiry)
                {
                    quoteBarRecordBatchBuilder.Append("expiry", true, quoteBarExpiry.Build(allocator));
                    hasExpiry = true;
                }
                if (hasStrike)
                {
                    quoteBarRecordBatchBuilder.Append("strike", true, quoteBarStrike.Build(allocator));
                    quoteBarRecordBatchBuilder.Append("type", true, quoteBarRight.Build(allocator));
                }

                recordBatches.Add(quoteBarRecordBatchBuilder.Build());
            }

            if (tickTimes.Length != 0)
            {
                var tickRecordBatchBuilder = new RecordBatch.Builder(allocator);

                tickRecordBatchBuilder.Append("time", false, tickTimes.Build(allocator));
                tickRecordBatchBuilder.Append("symbol", false, tickSymbols.Build(allocator));

                if (hasSuspicious)
                {
                    tickRecordBatchBuilder.Append("suspicious", true, tickSuspicious.Build(allocator));
                }
                if (hasExchange)
                {
                    tickRecordBatchBuilder.Append("exchange", true, tickExchange.Build(allocator));
                }
                if (tickHasTrades)
                {
                    tickRecordBatchBuilder.Append("lastprice", tickHasQuotes, tickValue.Build(allocator));
                    tickRecordBatchBuilder.Append("quantity", tickHasQuotes, tickQuantity.Build(allocator));
                }
                if (tickHasQuotes)
                {
                    tickRecordBatchBuilder.Append("bidprice", tickHasTrades, tickBidPrice.Build(allocator));
                    tickRecordBatchBuilder.Append("bidsize", tickHasTrades, tickBidSize.Build(allocator));
                    tickRecordBatchBuilder.Append("askprice", tickHasTrades, tickAskPrice.Build(allocator));
                    tickRecordBatchBuilder.Append("asksize", tickHasTrades, tickAskSize.Build(allocator));
                }

                if (hasExpiry)
                {
                    tickRecordBatchBuilder.Append("expiry", true, tickExpiry.Build(allocator));
                    hasExpiry = true;
                }
                if (hasStrike)
                {
                    tickRecordBatchBuilder.Append("strike", true, tickStrike.Build(allocator));
                    tickRecordBatchBuilder.Append("type", true, tickRight.Build(allocator));
                }

                recordBatches.Add(tickRecordBatchBuilder.Build());
            }

            if (openInterestTimes.Length != 0)
            {
                var openInterestBatchBuilder = new RecordBatch.Builder(allocator);

                openInterestBatchBuilder.Append("time", false, openInterestTimes.Build(allocator));
                openInterestBatchBuilder.Append("symbol", false, openInterestSymbols.Build(allocator));
                openInterestBatchBuilder.Append("openinterest", false, openInterestValue.Build(allocator));
                if (hasExpiry)
                {
                    openInterestBatchBuilder.Append("expiry", true, openInterestExpiry.Build(allocator));
                }
                if (hasStrike)
                {
                    openInterestBatchBuilder.Append("strike", true, openInterestStrike.Build(allocator));
                    openInterestBatchBuilder.Append("type", true, openInterestRight.Build(allocator));
                }

                recordBatches.Add(openInterestBatchBuilder.Build());
            }

            if (_customDataSymbols.Count != 0)
            {
                var customDataRecordBatchBuilder = new RecordBatch.Builder(allocator);

                foreach (var kvp in _customDataMembers)
                {
                    var baseDataType = kvp.Key;
                    foreach (var member in kvp.Value)
                    {
                        var columnName = member.Name.ToLowerInvariant();
                        var property = member as PropertyInfo;
                        var field = member as FieldInfo;
                        var memberType = property?.PropertyType ?? field.FieldType;
                        var builder = _customDataBuilders[columnName];

                        AppendToRecordBatch(customDataRecordBatchBuilder, memberType, columnName, builder, allocator);
                    }
                }

                customDataRecordBatchBuilder.Append("time", false, action => action.Timestamp(builder => builder.AppendRange(_customDataTimes).Build(allocator)));
                customDataRecordBatchBuilder.Append("symbol", false, action => action.String(builder => builder.AppendRange(_customDataSymbols).Build(allocator)));

                recordBatches.Add(customDataRecordBatchBuilder.Build());
            }

            using (Py.GIL())
            {
                if (recordBatches.Count == 0)
                {
                    return _pandas.DataFrame();
                }

                var dataFrames = new List<dynamic>();
                foreach (var recordBatch in recordBatches)
                {
                    using (var ms = new MemoryStream())
                    using (var writer = new ArrowStreamWriter(ms, recordBatch.Schema))
                    {
                        writer.WriteRecordBatchAsync(recordBatch).SynchronouslyAwaitTask();
                        using (var arrowBuffer = new ArrowBuffer(ms.GetBuffer()))
                        {
                            unsafe
                            {
                                var pinned = arrowBuffer.Memory.Pin();

                                dynamic buf = _pa.foreign_buffer(
                                    (long) new IntPtr(pinned.Pointer),
                                    arrowBuffer.Length
                                );
                                dynamic df = _pa.ipc.open_stream(buf).read_pandas(
                                    split_blocks: true,
                                    self_destruct: true
                                );

                                var timeIdx = 1;
                                if (hasExpiry)
                                {
                                    df.set_index("expiry", inplace: true);
                                    df = df.tz_localize(null, copy: false);
                                    timeIdx++;
                                }
                                if (hasStrike)
                                {
                                    df["strike"] = df["strike"].fillna("");
                                    df.set_index("strike", append: hasExpiry, inplace: true);
                                    timeIdx++;
                                }
                                if (hasOptionRight)
                                {
                                    df["type"] = df["type"].fillna("");
                                    df.set_index("type", append: hasExpiry || hasStrike, inplace: true);
                                    timeIdx++;
                                }

                                df.set_index(new PyList(new []{ new PyString("symbol"), new PyString("time") }), append: hasExpiry || hasStrike || hasOptionRight, inplace: true);
                                if (_customDataSymbols.Count != 0)
                                {
                                    var series = new List<dynamic>();
                                    foreach (var kvp in _customDataObjects)
                                    {
                                        var columnName = kvp.Key;
                                        var values = kvp.Value;

                                        series.Add(_pandas.Series(values.ToArray(), name: columnName));
                                    }

                                    df = df.join(series.ToArray(), how: "outer");
                                }

                                dataFrames.Add(df.tz_localize(null, level: timeIdx, copy: false));
                            }
                        }
                    }
                }

                dynamic final_df = dataFrames[0];
                if (dataFrames.Count > 1)
                {
                    final_df = final_df.join(dataFrames.Skip(1).ToArray(), how: "outer");
                }

                final_df.sort_index(inplace: true);
                if (hasStrike)
                {
                    // Current version of pandas doesn't like whenever we join or append an index
                    // with no null time values (NaT). It resets any empty string to NaT and
                    // doesn't allow for indexing with '', which is required for backwards compatibility.
                    final_df.reset_index(inplace: true);
                    final_df["expiry"] = final_df["expiry"].fillna("");
                    final_df.set_index("expiry", inplace: true);
                    final_df.set_index(new PyList(new[]
                    {
                        new PyString("strike"),
                        new PyString("type"),
                        new PyString("symbol"),
                        new PyString("time")
                    }), append: true, inplace: true);
                }

                // Cleans up any resources we've used to allow for the next generation of DataFrames
                // to be created with potentially zero allocations.
                allocator.Free();

                return _pandas.DataFrame(final_df);
            }
        }

        private void ClearBuilders()
        {
            hasExchange = false;
            hasSuspicious = false;
            hasExpiry = false;
            hasStrike = false;
            hasOptionRight = false;
            tickHasTrades = false;
            tickHasQuotes = false;

            tradeBarSymbols.Clear();
            tradeBarTimes.Clear();

            tradeBarExpiry.Clear();
            tradeBarStrike.Clear();
            tradeBarRight.Clear();

            tradeBarOpen.Clear();
            tradeBarHigh.Clear();
            tradeBarLow.Clear();
            tradeBarClose.Clear();
            tradeBarVolume.Clear();

            quoteBarSymbols.Clear();
            quoteBarTimes.Clear();

            quoteBarExpiry.Clear();
            quoteBarStrike.Clear();
            quoteBarRight.Clear();

            quoteBarBidOpen.Clear();
            quoteBarBidHigh.Clear();
            quoteBarBidLow.Clear();
            quoteBarBidClose.Clear();
            quoteBarBidVolume.Clear();
            quoteBarAskOpen.Clear();
            quoteBarAskHigh.Clear();
            quoteBarAskLow.Clear();
            quoteBarAskClose.Clear();
            quoteBarAskVolume.Clear();

            tickSymbols.Clear();
            tickTimes.Clear();

            tickExpiry.Clear();
            tickStrike.Clear();
            tickRight.Clear();

            tickExchange.Clear();
            tickSuspicious.Clear();

            tickValue.Clear();
            tickQuantity.Clear();
            tickBidPrice.Clear();
            tickBidSize.Clear();
            tickAskPrice.Clear();
            tickAskSize.Clear();

            openInterestTimes.Clear();
            openInterestSymbols.Clear();

            openInterestExpiry.Clear();
            openInterestStrike.Clear();
            openInterestRight.Clear();

            openInterestValue.Clear();

            foreach (var kvp in _customDataMembers)
            {
                var memberType = kvp.Key;
                foreach (var member in kvp.Value)
                {
                    var objects = _customDataObjects[member.Name];
                    var builder = _customDataBuilders[member.Name];

                    ClearBuilder(builder, memberType);
                    objects.Clear();
                }
            }

            _customDataSymbols.Clear();
            _customDataTimes.Clear();
        }

        private IArrowArrayBuilder CreateBuilder(Type memberType)
        {
            if (memberType == typeof(byte))
            {
                return new UInt8Array.Builder();
            }
            if (memberType == typeof(sbyte))
            {
                return new Int8Array.Builder();
            }
            if (memberType == typeof(ushort))
            {
                return new UInt16Array.Builder();
            }
            if (memberType == typeof(short))
            {
                return new Int16Array.Builder();
            }
            if (memberType == typeof(int))
            {
                return new Int32Array.Builder();
            }
            if (memberType == typeof(uint))
            {
                return new UInt32Array.Builder();
            }
            if (memberType == typeof(long))
            {
                return new Int64Array.Builder();
            }
            if (memberType == typeof(ulong))
            {
                return new UInt64Array.Builder();
            }
            if (memberType == typeof(float))
            {
                return new FloatArray.Builder();
            }
            if (memberType == typeof(double))
            {
                return new DoubleArray.Builder();
            }
            if (memberType == typeof(decimal))
            {
                return new DoubleArray.Builder();
            }
            if (memberType == typeof(DateTime))
            {
                return new TimestampArray.Builder();
            }
            if (memberType == typeof(string))
            {
                return new StringArray.Builder();
            }

            return null;
        }

        private bool AppendToBuilder(IArrowArrayBuilder builder, BaseData baseData, Type memberType, PropertyInfo property, FieldInfo field)
        {
            return AppendToBuilder(builder, memberType, (property != null ? property.GetValue(baseData) : field.GetValue(baseData)));
        }

        private bool AppendToBuilder(IArrowArrayBuilder builder, Type memberType, object value)
        {
            if (memberType == typeof(byte))
            {
                ((UInt8Array.Builder)builder).Append((byte)value);
                return true;
            }
            if (memberType == typeof(sbyte))
            {
                ((Int8Array.Builder)builder).Append((sbyte)value);
                return true;
            }
            if (memberType == typeof(ushort))
            {
                ((UInt16Array.Builder)builder).Append((ushort)value);
                return true;
            }
            if (memberType == typeof(short))
            {
                ((Int16Array.Builder)builder).Append((short)value);
                return true;
            }
            if (memberType == typeof(int))
            {
                ((Int32Array.Builder)builder).Append((int)value);
                return true;
            }
            if (memberType == typeof(uint))
            {
                ((UInt32Array.Builder)builder).Append((uint)value);
                return true;
            }
            if (memberType == typeof(long))
            {
                ((Int64Array.Builder)builder).Append((long)value);
                return true;
            }
            if (memberType == typeof(ulong))
            {
                ((UInt64Array.Builder)builder).Append((ulong)value);
                return true;
            }
            if (memberType == typeof(float))
            {
                ((FloatArray.Builder)builder).Append((float)value);
                return true;
            }
            if (memberType == typeof(double))
            {
                ((DoubleArray.Builder)builder).Append((double)value);
                return true;
            }
            if (memberType == typeof(decimal))
            {
                ((DoubleArray.Builder)builder).Append((double)((decimal)value));
                return true;
            }
            if (memberType == typeof(DateTime))
            {
                ((TimestampArray.Builder)builder).Append(new DateTimeOffset(((DateTime)value).Ticks, TimeSpan.Zero));
                return true;
            }
            if (memberType == typeof(string))
            {
                ((StringArray.Builder)builder).Append((string)value);
                return true;
            }

            return false;
        }

        private bool AppendToRecordBatch(RecordBatch.Builder customDataRecordBatchBuilder, Type memberType, string columnName, IArrowArrayBuilder builder, MemoryAllocator allocator = null)
        {
            if (memberType == typeof(byte))
            {
                customDataRecordBatchBuilder.Append(columnName, true, ((UInt8Array.Builder)builder).Build(allocator));
                return true;
            }
            if (memberType == typeof(sbyte))
            {
                customDataRecordBatchBuilder.Append(columnName, true, ((Int8Array.Builder)builder).Build(allocator));
                return true;
            }
            if (memberType == typeof(ushort))
            {
                customDataRecordBatchBuilder.Append(columnName, true, ((UInt16Array.Builder)builder).Build(allocator));
                return true;
            }
            if (memberType == typeof(short))
            {
                customDataRecordBatchBuilder.Append(columnName, true, ((Int16Array.Builder)builder).Build(allocator));
                return true;
            }
            if (memberType == typeof(int))
            {
                customDataRecordBatchBuilder.Append(columnName, true, ((Int32Array.Builder)builder).Build(allocator));
                return true;
            }
            if (memberType == typeof(uint))
            {
                customDataRecordBatchBuilder.Append(columnName, true, ((UInt32Array.Builder)builder).Build(allocator));
                return true;
            }
            if (memberType == typeof(long))
            {
                customDataRecordBatchBuilder.Append(columnName, true, ((Int64Array.Builder)builder).Build(allocator));
                return true;
            }
            if (memberType == typeof(ulong))
            {
                customDataRecordBatchBuilder.Append(columnName, true, ((UInt64Array.Builder)builder).Build(allocator));
                return true;
            }
            if (memberType == typeof(float))
            {
                customDataRecordBatchBuilder.Append(columnName, true, ((FloatArray.Builder)builder).Build(allocator));
                return true;
            }
            if (memberType == typeof(double) || memberType == typeof(decimal))
            {
                customDataRecordBatchBuilder.Append(columnName, true, ((DoubleArray.Builder)builder).Build(allocator));
                return true;
            }
            if (memberType == typeof(DateTime))
            {
                customDataRecordBatchBuilder.Append(columnName, true, ((TimestampArray.Builder)builder).Build(allocator));
                return true;
            }
            if (memberType == typeof(string))
            {
                customDataRecordBatchBuilder.Append(columnName, true, ((StringArray.Builder)builder).Build(allocator));
                return true;
            }

            return false;
        }

        private void ClearBuilder(IArrowArrayBuilder builder, Type memberType)
        {
            if (memberType == typeof(byte))
            {
                ((UInt8Array.Builder)builder).Clear();
                return;
            }
            if (memberType == typeof(sbyte))
            {
                ((Int8Array.Builder)builder).Clear();
                return;
            }
            if (memberType == typeof(ushort))
            {
                ((UInt16Array.Builder)builder).Clear();
                return;
            }
            if (memberType == typeof(short))
            {
                ((Int16Array.Builder)builder).Clear();
                return;
            }
            if (memberType == typeof(int))
            {
                ((Int32Array.Builder)builder).Clear();
                return;
            }
            if (memberType == typeof(uint))
            {
                ((UInt32Array.Builder)builder).Clear();
                return;
            }
            if (memberType == typeof(long))
            {
                ((Int64Array.Builder)builder).Clear();
                return;
            }
            if (memberType == typeof(ulong))
            {
                ((UInt64Array.Builder)builder).Clear();
                return;
            }
            if (memberType == typeof(float))
            {
                ((FloatArray.Builder)builder).Clear();
                return;
            }
            if (memberType == typeof(double))
            {
                ((DoubleArray.Builder)builder).Clear();
                return;
            }
            if (memberType == typeof(decimal))
            {
                ((DoubleArray.Builder)builder).Clear();
                return;
            }
            if (memberType == typeof(DateTime))
            {
                ((TimestampArray.Builder)builder).Clear();
                return;
            }
            if (memberType == typeof(string))
            {
                ((StringArray.Builder)builder).Clear();
                return;
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

                sliceData.Add(datum);
            }

            using (Py.GIL())
            {
                // If sliceData is still null, data is an empty enumerable
                // returns an empty pandas.DataFrame
                if (sliceData == null)
                {
                    return _pandas.DataFrame();
                }
                return sliceData.ToPandasDataFrame();
            }
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
                    pyDict.SetItem(kvp.Key.ToLowerInvariant(), _pandas.Series(values, index));
                }

                return _pandas.DataFrame(pyDict, columns: data.Keys.Select(x => x.ToLowerInvariant()).OrderBy(x => x));
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
