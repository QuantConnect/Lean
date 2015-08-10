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
using QuantConnect.Data.Custom;
using QuantConnect.Data.Market;

namespace QuantConnect.Data
{
    /// <summary>
    /// Provides a data structure for all of an algorithm's data at a single time step
    /// </summary>
    public class Slice : IEnumerable<KeyValuePair<string, BaseData>>
    {
        private readonly Ticks _ticks; 
        private readonly TradeBars _bars;

        // aux data
        private readonly Splits _splits;
        private readonly Dividends _dividends;
        private readonly Delistings _delistings; 

        // string -> data   for non-tick data
        // string -> list{data} for tick data
        private readonly DataDictionary<SymbolData> _data;
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
        /// Gets the <see cref="TradeBars"/> for this slice of data
        /// </summary>
        public TradeBars Bars
        {
            get { return _bars; }
        }

        /// <summary>
        /// Gets the <see cref="Ticks"/> for this slice of data
        /// </summary>
        public Ticks Ticks
        {
            get { return _ticks; }
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
        /// Gets the number of symbols held in this slice
        /// </summary>
        public int Count
        {
            get { return _data.Count; }
        }

        /// <summary>
        /// Gets all the symbols in this slice
        /// </summary>
        public IReadOnlyList<string> Keys
        {
            get { return new List<string>(_data.Keys); }
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
            : this(time, data, null, null, null, null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Slice"/> class
        /// </summary>
        /// <param name="time">The timestamp for this slice of data</param>
        /// <param name="data">The raw data in this slice</param>
        /// <param name="tradeBars">The trade bars for this slice</param>
        /// <param name="ticks">This ticks for this slice</param>
        /// <param name="splits">The splits for this slice</param>
        /// <param name="dividends">The dividends for this slice</param>
        /// <param name="delistings">The delistings for this slice</param>
        public Slice(DateTime time, IEnumerable<BaseData> data, TradeBars tradeBars, Ticks ticks, Splits splits, Dividends dividends, Delistings delistings)
        {
            Time = time;

            _dataByType = new Dictionary<Type, Lazy<object>>();

            // market data
            _data = CreateDynamicDataDictionary(data);
            _ticks = CreateTicksCollection(ticks);
            _bars = CreateTradeBarsCollection(tradeBars);

            // auxiliary data
            _splits = CreateSplitsCollection(splits);
            _dividends = CreateDividendsCollection(dividends);
            _delistings = CreateDelistingsCollection(delistings);
        }

        /// <summary>
        /// Gets the data corresponding to the specified symbol. If the requested data
        /// is of <see cref="MarketDataType.Tick"/>, then a <see cref="List{Tick}"/> will
        /// be returned, otherwise, it will be the subscribed type, for example, <see cref="TradeBar"/>
        /// or event <see cref="Quandl"/> for custom data.
        /// </summary>
        /// <param name="symbol">The data's symbols</param>
        /// <returns>The data for the specified symbol</returns>
        public dynamic this[string symbol]
        {
            get
            {
                SymbolData value;
                if (_data.TryGetValue(symbol, out value))
                {
                    return value.GetData();
                }
                throw new KeyNotFoundException(string.Format("'{0}' wasn't found in the Slice object, likely because there was no-data at this moment in time and it wasn't possible to fillforward historical data. Please check the data exists before accessing it with data.ContainsKey(\"{0}\")", symbol));
            }
        }

        /// <summary>
        /// Gets the <see cref="DataDictionary{T}"/> for all data of the specified type
        /// </summary>
        /// <typeparam name="T">The type of data we want, for example, <see cref="TradeBar"/> or <see cref="Quandl"/>, ect...</typeparam>
        /// <returns>The <see cref="DataDictionary{T}"/> containing the data of the specified type</returns>
        public DataDictionary<T> Get<T>()
            where T : BaseData
        {
            Lazy<object> dictionary;
            if (!_dataByType.TryGetValue(typeof(T), out dictionary))
            {
                dictionary = new Lazy<object>(() => new DataDictionary<T>(_data.Values.Select(x => x.GetData()).OfType<T>(), x => x.Symbol));
                _dataByType[typeof (T)] = dictionary;
            }
            return (DataDictionary<T>) dictionary.Value;
        }

        /// <summary>
        /// Gets the data of the specified symbol and type.
        /// </summary>
        /// <typeparam name="T">The type of data we seek</typeparam>
        /// <param name="symbol">The specific symbol was seek</param>
        /// <returns>The data for the requested symbol</returns>
        public T Get<T>(string symbol)
            where T : BaseData
        {
            return Get<T>()[symbol];
        }

        /// <summary>
        /// Determines whether this instance contains data for the specified symbol
        /// </summary>
        /// <param name="symbol">The symbol we seek data for</param>
        /// <returns>True if this instance contains data for the symbol, false otherwise</returns>
        public bool ContainsKey(string symbol)
        {
            return _data.ContainsKey(symbol);
        }

        /// <summary>
        /// Gets the data associated with the specified symbol
        /// </summary>
        /// <param name="symbol">The symbol we want data for</param>
        /// <param name="data">The data for the specifed symbol, or null if no data was found</param>
        /// <returns>True if data was found, false otherwise</returns>
        public bool TryGetValue(string symbol, out dynamic data)
        {
            data = null;
            SymbolData symbolData;
            if (_data.TryGetValue(symbol, out symbolData))
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
                        symbolData.TradeBar = (TradeBar) datum;
                        break;

                    case MarketDataType.Tick:
                        symbolData.Type = SubscriptionType.Tick;
                        symbolData.Ticks.Add((Tick) datum);
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
            foreach (var listTicks in _data.Values.Select(x => x.GetData()).OfType<List<Tick>>().Where(x => x.Count != 0))
            {
                ticks[listTicks[0].Symbol] = listTicks;
            }
            return ticks;
        }

        /// <summary>
        /// Returns the input tradebars if non-null, otherwise produces one fom the dynamic data dictionary
        /// </summary>
        private TradeBars CreateTradeBarsCollection(TradeBars tradeBars)
        {
            if (tradeBars != null) return tradeBars;
            tradeBars = new TradeBars(Time);
            foreach (var bar in _data.Values.Select(x => x.GetData()).OfType<TradeBar>())
            {
                tradeBars[bar.Symbol] = bar;
            }
            return tradeBars;
        }

        /// <summary>
        /// Returns the input splits if non-null, otherwise produces one fom the dynamic data dictionary
        /// </summary>
        private Splits CreateSplitsCollection(Splits splits)
        {
            if (splits != null) return splits;
            splits = new Splits(Time);
            foreach (var split in _data.Values.Select(x => x.GetData()).OfType<Split>())
            {
                splits[split.Symbol] = split;
            }
            return splits;
        }

        /// <summary>
        /// Returns the input dividends if non-null, otherwise produces one fom the dynamic data dictionary
        /// </summary>
        private Dividends CreateDividendsCollection(Dividends dividends)
        {
            if (dividends != null) return dividends;
            dividends = new Dividends(Time);
            foreach (var dividend in _data.Values.Select(x => x.GetData()).OfType<Dividend>())
            {
                dividends[dividend.Symbol] = dividend;
            }
            return dividends;
        }

        /// <summary>
        /// Returns the input delistings if non-null, otherwise produces one from the dynamic data dictionary
        /// </summary>
        private Delistings CreateDelistingsCollection(Delistings delistings)
        {
            if (delistings != null) return delistings;
            delistings = new Delistings(Time);
            foreach (var delisting in _data.Values.Select(x => x.GetData()).OfType<Delisting>())
            {
                delistings[delisting.Symbol] = delisting;
            }
            return delistings;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
        /// </returns>
        /// <filterpriority>1</filterpriority>
        public IEnumerator<KeyValuePair<string, BaseData>> GetEnumerator()
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

        private IEnumerable<KeyValuePair<string, BaseData>> GetKeyValuePairEnumerable()
        {
            // this will not enumerate auxilliary data!
            return _data.Select(kvp => new KeyValuePair<string, BaseData>(kvp.Key, kvp.Value.GetData()));
        }

        private enum SubscriptionType { TradeBar, Tick, Custom };
        private class SymbolData
        {
            public SubscriptionType Type;
            public readonly string Symbol;

            // data
            public BaseData Custom;
            public TradeBar TradeBar;
            public readonly List<Tick> Ticks;
            public readonly List<BaseData> AuxilliaryData;

            public SymbolData(string symbol)
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
                    case SubscriptionType.Tick:
                        return Ticks;
                    case SubscriptionType.Custom:
                        return Custom;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

    }
}
