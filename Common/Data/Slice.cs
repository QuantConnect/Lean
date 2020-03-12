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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using QuantConnect.Data.Custom;
using QuantConnect.Data.Market;

namespace QuantConnect.Data
{
    /// <summary>
    /// Provides a data structure for all of an algorithm's data at a single time step
    /// </summary>
    public class Slice : IEnumerable<KeyValuePair<Symbol, BaseData>>
    {
        private readonly Ticks _ticks;
        private readonly TradeBars _bars;
        private readonly QuoteBars _quoteBars;
        private readonly OptionChains _optionChains;
        private readonly FuturesChains _futuresChains;

        // aux data
        private readonly Splits _splits;
        private readonly Dividends _dividends;
        private readonly Delistings _delistings;
        private readonly SymbolChangedEvents _symbolChangedEvents;

        // string -> data   for non-tick data
        // string -> list{data} for tick data
        private readonly Lazy<DataDictionary<SymbolData>> _data;
        // Quandl -> DataDictonary<Quandl>
        private readonly Dictionary<Type, Lazy<object>> _dataByType;

        /// <summary>
        /// Gets the timestamp for this slice of data
        /// </summary>
        public DateTime Time
        {
            get; private set;
        }

        /// <summary>
        /// Gets whether or not this slice has data
        /// </summary>
        public bool HasData
        {
            get; private set;
        }

        /// <summary>
        /// Gets the <see cref="TradeBars"/> for this slice of data
        /// </summary>
        public TradeBars Bars
        {
            get { return _bars; }
        }

        /// <summary>
        /// Gets the <see cref="QuoteBars"/> for this slice of data
        /// </summary>
        public QuoteBars QuoteBars
        {
            get { return _quoteBars; }
        }

        /// <summary>
        /// Gets the <see cref="Ticks"/> for this slice of data
        /// </summary>
        public Ticks Ticks
        {
            get { return _ticks; }
        }

        /// <summary>
        /// Gets the <see cref="OptionChains"/> for this slice of data
        /// </summary>
        public OptionChains OptionChains
        {
            get { return _optionChains; }
        }

        /// <summary>
        /// Gets the <see cref="FuturesChains"/> for this slice of data
        /// </summary>
        public FuturesChains FuturesChains
        {
            get { return _futuresChains; }
        }

        /// <summary>
        /// Gets the <see cref="FuturesChains"/> for this slice of data
        /// </summary>
        public FuturesChains FutureChains
        {
            get { return _futuresChains; }
        }

        /// <summary>
        /// Gets the <see cref="Splits"/> for this slice of data
        /// </summary>
        public Splits Splits
        {
            get { return _splits; }
        }

        /// <summary>
        /// Gets the <see cref="Dividends"/> for this slice of data
        /// </summary>
        public Dividends Dividends
        {
            get { return _dividends; }
        }

        /// <summary>
        /// Gets the <see cref="Delistings"/> for this slice of data
        /// </summary>
        public Delistings Delistings
        {
            get { return _delistings; }
        }

        /// <summary>
        /// Gets the <see cref="QuantConnect.Data.Market.SymbolChangedEvents"/> for this slice of data
        /// </summary>
        public SymbolChangedEvents SymbolChangedEvents
        {
            get { return _symbolChangedEvents; }
        }

        /// <summary>
        /// Gets the number of symbols held in this slice
        /// </summary>
        public int Count
        {
            get { return _data.Value.Count; }
        }

        /// <summary>
        /// Gets all the symbols in this slice
        /// </summary>
        public IReadOnlyList<Symbol> Keys
        {
            get { return new List<Symbol>(_data.Value.Keys); }
        }

        /// <summary>
        /// Gets a list of all the data in this slice
        /// </summary>
        public IReadOnlyList<BaseData> Values
        {
            get { return GetKeyValuePairEnumerable().Select(x => x.Value).ToList(); }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Slice"/> class, lazily
        /// instantiating the <see cref="Slice.Bars"/> and <see cref="Slice.Ticks"/>
        /// collections on demand
        /// </summary>
        /// <param name="time">The timestamp for this slice of data</param>
        /// <param name="data">The raw data in this slice</param>
        public Slice(DateTime time, IEnumerable<BaseData> data)
            : this(time, data, null, null, null, null, null, null, null, null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Slice"/> class
        /// </summary>
        /// <param name="time">The timestamp for this slice of data</param>
        /// <param name="data">The raw data in this slice</param>
        /// <param name="tradeBars">The trade bars for this slice</param>
        /// <param name="quoteBars">The quote bars for this slice</param>
        /// <param name="ticks">This ticks for this slice</param>
        /// <param name="optionChains">The option chains for this slice</param>
        /// <param name="futuresChains">The futures chains for this slice</param>
        /// <param name="splits">The splits for this slice</param>
        /// <param name="dividends">The dividends for this slice</param>
        /// <param name="delistings">The delistings for this slice</param>
        /// <param name="symbolChanges">The symbol changed events for this slice</param>
        /// <param name="hasData">true if this slice contains data</param>
        public Slice(DateTime time, IEnumerable<BaseData> data, TradeBars tradeBars, QuoteBars quoteBars, Ticks ticks, OptionChains optionChains, FuturesChains futuresChains, Splits splits, Dividends dividends, Delistings delistings, SymbolChangedEvents symbolChanges, bool? hasData = null)
        {
            Time = time;

            _dataByType = new Dictionary<Type, Lazy<object>>();

            // market data
            _data = new Lazy<DataDictionary<SymbolData>>(() => CreateDynamicDataDictionary(data));

            HasData = hasData ?? _data.Value.Count > 0;

            _ticks = CreateTicksCollection(ticks);
            _bars = CreateCollection<TradeBars, TradeBar>(tradeBars);
            _quoteBars = CreateCollection<QuoteBars, QuoteBar>(quoteBars);
            _optionChains = CreateCollection<OptionChains, OptionChain>(optionChains);
            _futuresChains = CreateCollection<FuturesChains, FuturesChain>(futuresChains);

            // auxiliary data
            _splits = CreateCollection<Splits, Split>(splits);
            _dividends = CreateCollection<Dividends, Dividend>(dividends);
            _delistings = CreateCollection<Delistings, Delisting>(delistings);
            _symbolChangedEvents = CreateCollection<SymbolChangedEvents, SymbolChangedEvent>(symbolChanges);
        }

        /// <summary>
        /// Gets the data corresponding to the specified symbol. If the requested data
        /// is of <see cref="MarketDataType.Tick"/>, then a <see cref="List{Tick}"/> will
        /// be returned, otherwise, it will be the subscribed type, for example, <see cref="TradeBar"/>
        /// or event <see cref="Quandl"/> for custom data.
        /// </summary>
        /// <param name="symbol">The data's symbols</param>
        /// <returns>The data for the specified symbol</returns>
        public dynamic this[Symbol symbol]
        {
            get
            {
                SymbolData value;
                if (_data.Value.TryGetValue(symbol, out value))
                {
                    return value.GetData();
                }
                throw new KeyNotFoundException($"'{symbol}' wasn't found in the Slice object, likely because there was no-data at this moment in time and it wasn't possible to fillforward historical data. Please check the data exists before accessing it with data.ContainsKey(\"{symbol}\")");
            }
        }

        /// <summary>
        /// Gets the <see cref="DataDictionary{T}"/> for all data of the specified type
        /// </summary>
        /// <typeparam name="T">The type of data we want, for example, <see cref="TradeBar"/> or <see cref="Quandl"/>, ect...</typeparam>
        /// <returns>The <see cref="DataDictionary{T}"/> containing the data of the specified type</returns>
        public DataDictionary<T> Get<T>()
            where T : IBaseData
        {
            return GetImpl(typeof(T), this);
        }

        /// <summary>
        /// Gets the data of the specified symbol and type.
        /// </summary>
        /// <remarks>Supports both C# and Python use cases</remarks>
        protected static dynamic GetImpl(Type type, Slice instance)
        {
            Lazy<object> dictionary;
            if (!instance._dataByType.TryGetValue(type, out dictionary))
            {
                if (type == typeof(Tick))
                {
                    dictionary = new Lazy<object>(() =>
                    {
                        var dataDictionaryCache = GenericDataDictionary.Get(type);
                        var dic = Activator.CreateInstance(dataDictionaryCache.GenericType);

                        foreach (var data in
                            instance._data.Value.Values.SelectMany<dynamic, dynamic>(x => x.GetData()).Where(o => o != null && (Type)o.GetType() == type))
                        {
                            dataDictionaryCache.MethodInfo.Invoke(dic, new[] { data.Symbol, data });
                        }
                        return dic;
                    }
                    );
                }
                else if (type == typeof(TradeBar))
                {
                    dictionary = new Lazy<object>(() => new DataDictionary<TradeBar>(
                        instance._data.Value.Values.Where(x => x.TradeBar != null).Select(x => x.TradeBar),
                        x => x.Symbol));
                }
                else if (type == typeof(QuoteBar))
                {
                    dictionary = new Lazy<object>(() => new DataDictionary<QuoteBar>(
                        instance._data.Value.Values.Where(x => x.QuoteBar != null).Select(x => x.QuoteBar),
                        x => x.Symbol));
                }
                else
                {
                    dictionary = new Lazy<object>(() =>
                    {
                        var dataDictionaryCache = GenericDataDictionary.Get(type);
                        var dic = Activator.CreateInstance(dataDictionaryCache.GenericType);

                        foreach (var data in instance._data.Value.Values.Select(x => x.GetData()).Where(o => o != null && (Type)o.GetType() == type))
                        {
                            dataDictionaryCache.MethodInfo.Invoke(dic, new[] { data.Symbol, data });
                        }
                        return dic;
                    }
                    );
                }

                instance._dataByType[type] = dictionary;
            }
            return dictionary.Value;
        }

        /// <summary>
        /// Gets the data of the specified symbol and type.
        /// </summary>
        /// <typeparam name="T">The type of data we seek</typeparam>
        /// <param name="symbol">The specific symbol was seek</param>
        /// <returns>The data for the requested symbol</returns>
        public T Get<T>(Symbol symbol)
            where T : BaseData
        {
            return Get<T>()[symbol];
        }

        /// <summary>
        /// Determines whether this instance contains data for the specified symbol
        /// </summary>
        /// <param name="symbol">The symbol we seek data for</param>
        /// <returns>True if this instance contains data for the symbol, false otherwise</returns>
        public bool ContainsKey(Symbol symbol)
        {
            return _data.Value.ContainsKey(symbol);
        }

        /// <summary>
        /// Gets the data associated with the specified symbol
        /// </summary>
        /// <param name="symbol">The symbol we want data for</param>
        /// <param name="data">The data for the specifed symbol, or null if no data was found</param>
        /// <returns>True if data was found, false otherwise</returns>
        public bool TryGetValue(Symbol symbol, out dynamic data)
        {
            data = null;
            SymbolData symbolData;
            if (_data.Value.TryGetValue(symbol, out symbolData))
            {
                data = symbolData.GetData();
                return data != null;
            }
            return false;
        }

        /// <summary>
        /// Produces the dynamic data dictionary from the input data
        /// </summary>
        private static DataDictionary<SymbolData> CreateDynamicDataDictionary(IEnumerable<BaseData> data)
        {
            var allData = new DataDictionary<SymbolData>();
            foreach (var datum in data)
            {
                SymbolData symbolData;
                if (!allData.TryGetValue(datum.Symbol, out symbolData))
                {
                    symbolData = new SymbolData(datum.Symbol);
                    allData[datum.Symbol] = symbolData;
                }

                switch (datum.DataType)
                {
                    case MarketDataType.Base:
                        symbolData.Type = SubscriptionType.Custom;
                        symbolData.Custom = datum;
                        break;

                    case MarketDataType.TradeBar:
                        symbolData.Type = SubscriptionType.TradeBar;
                        symbolData.TradeBar = (TradeBar)datum;
                        break;

                    case MarketDataType.QuoteBar:
                        symbolData.Type = SubscriptionType.QuoteBar;
                        symbolData.QuoteBar = (QuoteBar)datum;
                        break;

                    case MarketDataType.Tick:
                        symbolData.Type = SubscriptionType.Tick;
                        symbolData.Ticks.Add((Tick)datum);
                        break;

                    case MarketDataType.Auxiliary:
                        symbolData.AuxilliaryData.Add(datum);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            return allData;
        }

        /// <summary>
        /// Returns the input ticks if non-null, otherwise produces one fom the dynamic data dictionary
        /// </summary>
        private Ticks CreateTicksCollection(Ticks ticks)
        {
            if (ticks != null) return ticks;
            ticks = new Ticks(Time);
            foreach (var listTicks in _data.Value.Values.Select(x => x.GetData()).OfType<List<Tick>>().Where(x => x.Count != 0))
            {
                ticks[listTicks[0].Symbol] = listTicks;
            }
            return ticks;
        }

        /// <summary>
        /// Returns the input collection if onon-null, otherwise produces one from the dynamic data dictionary
        /// </summary>
        /// <typeparam name="T">The data dictionary type</typeparam>
        /// <typeparam name="TItem">The item type of the data dictionary</typeparam>
        /// <param name="collection">The input collection, if non-null, returned immediately</param>
        /// <returns>The data dictionary of <typeparamref name="TItem"/> containing all the data of that type in this slice</returns>
        private T CreateCollection<T, TItem>(T collection)
            where T : DataDictionary<TItem>, new()
            where TItem : BaseData
        {
            if (collection != null) return collection;
            collection = new T();
#pragma warning disable 618 // This assignment is left here until the Time property is removed.
            collection.Time = Time;
#pragma warning restore 618
            foreach (var item in _data.Value.Values.Select(x => x.GetData()).OfType<TItem>())
            {
                collection[item.Symbol] = item;
            }
            return collection;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
        /// </returns>
        /// <filterpriority>1</filterpriority>
        public IEnumerator<KeyValuePair<Symbol, BaseData>> GetEnumerator()
        {
            return GetKeyValuePairEnumerable().GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private IEnumerable<KeyValuePair<Symbol, BaseData>> GetKeyValuePairEnumerable()
        {
            // this will not enumerate auxilliary data!

            foreach (var kvp in _data.Value)
            {
                var data = kvp.Value.GetData();

                var dataPoints = data as IEnumerable<BaseData>;
                if (dataPoints != null)
                {
                    foreach (var dataPoint in dataPoints)
                    {
                        yield return new KeyValuePair<Symbol, BaseData>(kvp.Key, dataPoint);
                    }
                }
                else if (data != null)
                {
                    yield return new KeyValuePair<Symbol, BaseData>(kvp.Key, data);
                }
            }
        }

        private enum SubscriptionType { TradeBar, QuoteBar, Tick, Custom };
        private class SymbolData
        {
            public SubscriptionType Type;
            public readonly Symbol Symbol;

            // data
            public BaseData Custom;
            public TradeBar TradeBar;
            public QuoteBar QuoteBar;
            public readonly List<Tick> Ticks;
            public readonly List<BaseData> AuxilliaryData;

            public SymbolData(Symbol symbol)
            {
                Symbol = symbol;
                Ticks = new List<Tick>();
                AuxilliaryData = new List<BaseData>();
            }

            public dynamic GetData()
            {
                switch (Type)
                {
                    case SubscriptionType.TradeBar:
                        return TradeBar;
                    case SubscriptionType.QuoteBar:
                        return QuoteBar;
                    case SubscriptionType.Tick:
                        return Ticks;
                    case SubscriptionType.Custom:
                        return Custom;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        /// <summary>
        /// Helper class for generic <see cref="DataDictionary{T}"/>
        /// </summary>
        /// <remarks>The value of this class is primarily performance since it keeps a cache
        /// of the generic types instances and there add methods.</remarks>
        private class GenericDataDictionary
        {
            private static readonly Dictionary<Type, GenericDataDictionary> _genericCache = new Dictionary<Type, GenericDataDictionary>();

            /// <summary>
            /// The <see cref="DataDictionary{T}.Add(KeyValuePair{QuantConnect.Symbol,T})"/> method
            /// </summary>
            public MethodInfo MethodInfo { get; }

            /// <summary>
            /// The <see cref="DataDictionary{T}"/> type
            /// </summary>
            public Type GenericType { get; }

            private GenericDataDictionary(Type genericType, MethodInfo methodInfo)
            {
                GenericType = genericType;
                MethodInfo = methodInfo;
            }

            /// <summary>
            /// Provides a <see cref="GenericDataDictionary"/> instance for a given <see cref="Type"/>
            /// </summary>
            /// <param name="type"></param>
            /// <returns>A new instance or retrieved from the cache</returns>
            public static GenericDataDictionary Get(Type type)
            {
                GenericDataDictionary dataDictionaryCache;
                if (!_genericCache.TryGetValue(type, out dataDictionaryCache))
                {
                    var generic = typeof(DataDictionary<>).MakeGenericType(type);
                    var method = generic.GetMethod("Add", new[] { typeof(Symbol), type });
                    _genericCache[type] = dataDictionaryCache = new GenericDataDictionary(generic, method);
                }

                return dataDictionaryCache;
            }
        }
    }
}
