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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Apache.Arrow.Types;
using QuantConnect.Securities;

namespace QuantConnect.Python
{
    /// <summary>
    /// Collection of methods that converts lists of objects in pandas.DataFrame
    /// </summary>
    public class PandasConverter
    {
        private static dynamic _pandas;

        /// <summary>
        /// Creates an instance of <see cref="PandasConverter"/>.
        /// </summary>
        public PandasConverter()
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
            /*
            var maxLevels = 0;
            var sliceDataDict = new Dictionary<Symbol, PandasData>();

            foreach (var slice in data)
            {
                foreach (var key in slice.Keys)
                {
                    var baseData = slice[key];

                    PandasData value;
                    if (!sliceDataDict.TryGetValue(key, out value))
                    {
                        sliceDataDict.Add(key, value = new PandasData(baseData));
                        maxLevels = Math.Max(maxLevels, value.Levels);
                    }

                    if (value.IsCustomData)
                    {
                        value.Add(baseData);
                    }
                    else
                    {
                        var ticks = slice.Ticks.ContainsKey(key) ? slice.Ticks[key] : null;
                        var tradeBars = slice.Bars.ContainsKey(key) ? slice.Bars[key] : null;
                        var quoteBars = slice.QuoteBars.ContainsKey(key) ? slice.QuoteBars[key] : null;
                        value.Add(ticks, tradeBars, quoteBars);
                    }
                }
            }

            using (Py.GIL())
            {
                if (sliceDataDict.Count == 0)
                {
                    return _pandas.DataFrame();
                }
                var dataFrames = sliceDataDict.Select(x => x.Value.ToPandasDataFrame(maxLevels));
                return _pandas.concat(dataFrames.ToArray(), Py.kw("sort", true));
            }
            /**/

            var allocator = new NativeMemoryAllocator();

            var tradeBarSymbols = new StringArray.Builder();
            var tradeBarTimes = new TimestampArray.Builder();

            var tradeBarExpiry = new TimestampArray.Builder();
            var tradeBarStrike = new DoubleArray.Builder();
            var tradeBarRight = new StringArray.Builder();

            var tradeBarOpen = new DoubleArray.Builder();
            var tradeBarHigh = new DoubleArray.Builder();
            var tradeBarLow = new DoubleArray.Builder();
            var tradeBarClose = new DoubleArray.Builder();
            var tradeBarVolume = new DoubleArray.Builder();

            var quoteBarSymbols = new StringArray.Builder();
            var quoteBarTimes = new TimestampArray.Builder();

            var quoteBarExpiry = new TimestampArray.Builder();
            var quoteBarStrike = new DoubleArray.Builder();
            var quoteBarRight = new StringArray.Builder();

            var quoteBarBidOpen = new DoubleArray.Builder();
            var quoteBarBidHigh = new DoubleArray.Builder();
            var quoteBarBidLow = new DoubleArray.Builder();
            var quoteBarBidClose = new DoubleArray.Builder();
            var quoteBarBidVolume = new DoubleArray.Builder();
            var quoteBarAskOpen = new DoubleArray.Builder();
            var quoteBarAskHigh = new DoubleArray.Builder();
            var quoteBarAskLow = new DoubleArray.Builder();
            var quoteBarAskClose = new DoubleArray.Builder();
            var quoteBarAskVolume = new DoubleArray.Builder();

            var tickSymbols = new StringArray.Builder();
            var tickTimes = new TimestampArray.Builder();

            var tickExpiry = new TimestampArray.Builder();
            var tickStrike = new DoubleArray.Builder();
            var tickRight = new StringArray.Builder();

            var tickExchange = new StringArray.Builder();
            var hasExchange = false;
            var tickSuspicious = new BooleanArray.Builder();
            var hasSuspicious = false;

            var tickHasTrades = false;
            var tickValue = new DoubleArray.Builder();
            var tickQuantity = new DoubleArray.Builder();
            var tickHasQuotes = false;
            var tickBidPrice = new DoubleArray.Builder();
            var tickBidSize = new DoubleArray.Builder();
            var tickAskPrice = new DoubleArray.Builder();
            var tickAskSize = new DoubleArray.Builder();

            var openInterestTimes = new TimestampArray.Builder();
            var openInterestSymbols = new StringArray.Builder();

            var openInterestExpiry = new TimestampArray.Builder();
            var openInterestStrike = new DoubleArray.Builder();
            var openInterestRight = new StringArray.Builder();

            var openInterestValue = new DoubleArray.Builder();

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
                            tradeBarExpiry.Append(new DateTimeOffset(symbol.ID.Date.Ticks, TimeSpan.Zero));
                        }
                        if (symbol.SecurityType == SecurityType.Option)
                        {
                            tradeBarStrike.Append((double) symbol.ID.StrikePrice);
                            tradeBarRight.Append(symbol.ID.OptionRight.ToString());
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
                            tradeBarVolume.Append(double.NaN);

                            tradeBarSymbols.Append(sid);
                            tradeBarTimes.Append(new DateTimeOffset(quoteBar.EndTime.Ticks, TimeSpan.Zero));

                            if (symbol.SecurityType == SecurityType.Future || symbol.SecurityType == SecurityType.Option)
                            {
                                tradeBarExpiry.Append(new DateTimeOffset(symbol.ID.Date.Ticks, TimeSpan.Zero));
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
                            quoteBarBidOpen.Append(double.NaN);
                            quoteBarBidHigh.Append(double.NaN);
                            quoteBarBidLow.Append(double.NaN);
                            quoteBarBidClose.Append(double.NaN);
                            quoteBarBidVolume.Append(double.NaN);
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
                            quoteBarAskOpen.Append(double.NaN);
                            quoteBarAskHigh.Append(double.NaN);
                            quoteBarAskLow.Append(double.NaN);
                            quoteBarAskClose.Append(double.NaN);
                            quoteBarAskVolume.Append(double.NaN);
                        }

                        quoteBarSymbols.Append(quoteBar.Symbol.ID.ToString());
                        quoteBarTimes.Append(new DateTimeOffset(quoteBar.EndTime.Ticks, TimeSpan.Zero));

                        if (symbol.SecurityType == SecurityType.Future || symbol.SecurityType == SecurityType.Option)
                        {
                            quoteBarExpiry.Append(new DateTimeOffset(symbol.ID.Date.Ticks, TimeSpan.Zero));
                        }
                        if (symbol.SecurityType == SecurityType.Option)
                        {
                            quoteBarStrike.Append((double) symbol.ID.StrikePrice);
                            quoteBarRight.Append(symbol.ID.OptionRight.ToString());
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
                                    tickExpiry.Append(new DateTimeOffset(symbol.ID.Date.Ticks, TimeSpan.Zero));
                                }
                                if (symbol.SecurityType == SecurityType.Option)
                                {
                                    tickStrike.Append((double) symbol.ID.StrikePrice);
                                    tickRight.Append(symbol.ID.OptionRight.ToString());
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

                                tickBidPrice.Append(double.NaN);
                                tickBidSize.Append(double.NaN);
                                tickAskPrice.Append(double.NaN);
                                tickAskSize.Append(double.NaN);
                            }
                            else if (tick.TickType == TickType.Quote)
                            {
                                tickHasQuotes = true;

                                tickValue.Append(double.NaN);
                                tickQuantity.Append(double.NaN);

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
                                    openInterestExpiry.Append(new DateTimeOffset(symbol.ID.Date.Ticks, TimeSpan.Zero));
                                }
                                if (symbol.SecurityType == SecurityType.Option)
                                {
                                    openInterestStrike.Append((double) symbol.ID.StrikePrice);
                                    openInterestRight.Append(symbol.ID.OptionRight.ToString());
                                }
                            }
                        }
                    }
                }
            }

            var recordBatches = new List<RecordBatch>();
            var hasExpiry = false;
            var hasStrike = false;
            var hasOptionRight = false;

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
                if (tradeBarExpiry.Length != 0)
                {
                    tradeBarRecordBatchBuilder.Append("expiry", true, tradeBarExpiry.Build(allocator));
                    hasExpiry = true;
                }
                if (tradeBarStrike.Length != 0)
                {
                    tradeBarRecordBatchBuilder.Append("strike", false, tradeBarStrike.Build(allocator));
                    tradeBarRecordBatchBuilder.Append("right", false, tradeBarRight.Build(allocator));
                }

                recordBatches.Add(tradeBarRecordBatchBuilder.Build());
            }

            if (quoteBarTimes.Length != 0)
            {
                var quoteBarRecordBatchBuilder = new RecordBatch.Builder(allocator);

                quoteBarRecordBatchBuilder.Append("time", false, quoteBarTimes.Build());
                quoteBarRecordBatchBuilder.Append("symbol", false, quoteBarSymbols.Build());
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
                if (quoteBarExpiry.Length != 0)
                {
                    quoteBarRecordBatchBuilder.Append("expiry", true, quoteBarExpiry.Build(allocator));
                    hasExpiry = true;
                }
                if (quoteBarStrike.Length != 0)
                {
                    quoteBarRecordBatchBuilder.Append("strike", false, quoteBarStrike.Build(allocator));
                    quoteBarRecordBatchBuilder.Append("right", false, quoteBarRight.Build(allocator));
                }

                recordBatches.Add(quoteBarRecordBatchBuilder.Build());
            }

            if (tickTimes.Length != 0)
            {
                var tickRecordBatchBuilder = new RecordBatch.Builder();

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

                if (tickExpiry.Length != 0)
                {
                    tickRecordBatchBuilder.Append("expiry", true, tickExpiry.Build(allocator));
                    hasExpiry = true;
                }
                if (tickStrike.Length != 0)
                {
                    tickRecordBatchBuilder.Append("strike", false, tickStrike.Build(allocator));
                    tickRecordBatchBuilder.Append("right", false, tickRight.Build(allocator));
                }

                recordBatches.Add(tickRecordBatchBuilder.Build());
            }

            if (openInterestTimes.Length != 0)
            {
                var openInterestBatchBuilder = new RecordBatch.Builder();

                openInterestBatchBuilder.Append("time", false, openInterestTimes.Build(allocator));
                openInterestBatchBuilder.Append("symbol", false, openInterestSymbols.Build(allocator));
                openInterestBatchBuilder.Append("openinterest", false, openInterestValue.Build(allocator));
                if (openInterestExpiry.Length != 0)
                {
                    openInterestBatchBuilder.Append("expiry", false, openInterestExpiry.Build(allocator));
                    hasExpiry = true;
                }
                if (openInterestStrike.Length != 0)
                {
                    openInterestBatchBuilder.Append("strike", false, openInterestStrike.Build(allocator));
                    openInterestBatchBuilder.Append("right", false, openInterestRight.Build(allocator));
                }

                recordBatches.Add(openInterestBatchBuilder.Build());
            }

            using (Py.GIL())
            {
                if (recordBatches.Count == 0)
                {
                    return _pandas.DataFrame();
                }

                dynamic pa = PythonEngine.ImportModule("pyarrow");
                var dataFrames = new List<dynamic>();
                foreach (var recordBatch in recordBatches)
                {
                    using (var ms = new MemoryStream(0))
                    using (var writer = new ArrowStreamWriter(ms, recordBatch.Schema))
                    {
                        writer.WriteRecordBatchAsync(recordBatch).SynchronouslyAwaitTask();
                        using (var arrowBuffer = new ArrowBuffer(ms.GetBuffer()))
                        {
                            unsafe
                            {
                                var pinned = arrowBuffer.Memory.Pin();

                                dynamic buf = pa.foreign_buffer(
                                    ((long) new IntPtr(pinned.Pointer)).ToPython(),
                                    arrowBuffer.Length.ToPython()
                                );
                                dynamic df = pa.ipc.open_stream(buf).read_pandas(
                                    Py.kw("split_blocks", true),
                                    Py.kw("self_destruct", true)
                                );

                                var timeIdx = 1;
                                if (hasExpiry)
                                {
                                    df.set_index("expiry", Py.kw("inplace", true));
                                    df = df.tz_localize(null, Py.kw("copy", false));
                                    timeIdx++;
                                }

                                df.set_index("symbol", Py.kw("append", hasExpiry), Py.kw("inplace", true));
                                df.set_index("time", Py.kw("append", true), Py.kw("inplace", true));
                                dataFrames.Add(df.tz_localize(null, Py.kw("level", timeIdx.ToPython()), Py.kw("copy", false)));
                            }
                        }
                    }
                }

                dynamic final_df = null;
                foreach (var dataFrame in dataFrames)
                {
                    if (final_df == null)
                    {
                        final_df = dataFrame;
                        continue;
                    }

                    final_df = final_df.join(dataFrame, Py.kw("how", "outer"));
                }

                final_df.sort_index(Py.kw("inplace", true));
                return _pandas.DataFrame(final_df);
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
