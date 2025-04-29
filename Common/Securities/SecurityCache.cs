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
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QuantConnect.Data.Fundamental;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Util;
using Python.Runtime;
using QuantConnect.Python;

namespace QuantConnect.Securities
{
    /// <summary>
    /// Base class caching spot for security data and any other temporary properties.
    /// </summary>
    public class SecurityCache
    {
        // let's share the empty readonly version, so we don't need null checks
        private static readonly IReadOnlyList<BaseData> _empty = new List<BaseData>();

        // this is used to prefer quote bar data over the tradebar data
        private DateTime _lastQuoteBarUpdate;
        private DateTime _lastOHLCUpdate;
        private BaseData _lastData;

        private readonly object _locker = new();
        private IReadOnlyList<BaseData> _lastTickQuotes = _empty;
        private IReadOnlyList<BaseData> _lastTickTrades = _empty;
        private Dictionary<Type, IReadOnlyList<BaseData>> _dataByType;

        private Dictionary<string, object> _properties;

        /// <summary>
        /// Gets the most recent price submitted to this cache
        /// </summary>
        public decimal Price { get; private set; }

        /// <summary>
        /// Gets the most recent open submitted to this cache
        /// </summary>
        public decimal Open { get; private set; }

        /// <summary>
        /// Gets the most recent high submitted to this cache
        /// </summary>
        public decimal High { get; private set; }

        /// <summary>
        /// Gets the most recent low submitted to this cache
        /// </summary>
        public decimal Low { get; private set; }

        /// <summary>
        /// Gets the most recent close submitted to this cache
        /// </summary>
        public decimal Close { get; private set; }

        /// <summary>
        /// Gets the most recent bid submitted to this cache
        /// </summary>
        public decimal BidPrice { get; private set; }

        /// <summary>
        /// Gets the most recent ask submitted to this cache
        /// </summary>
        public decimal AskPrice { get; private set; }

        /// <summary>
        /// Gets the most recent bid size submitted to this cache
        /// </summary>
        public decimal BidSize { get; private set; }

        /// <summary>
        /// Gets the most recent ask size submitted to this cache
        /// </summary>
        public decimal AskSize { get; private set; }

        /// <summary>
        /// Gets the most recent volume submitted to this cache
        /// </summary>
        public decimal Volume { get; private set; }

        /// <summary>
        /// Gets the most recent open interest submitted to this cache
        /// </summary>
        public long OpenInterest { get; private set; }

        /// <summary>
        /// Collection of keyed custom properties
        /// </summary>
        public Dictionary<string, object> Properties
        {
            get
            {
                if (_properties == null)
                {
                    _properties = new Dictionary<string, object>();
                }
                return _properties;
            }
        }

        /// <summary>
        /// Add a list of market data points to the local security cache for the current market price.
        /// </summary>
        /// <remarks>Internally uses <see cref="AddData"/> using the last data point of the provided list
        /// and it stores by type the non fill forward points using <see cref="StoreData"/></remarks>
        public void AddDataList(IReadOnlyList<BaseData> data, Type dataType, bool? containsFillForwardData = null)
        {
            var nonFillForwardData = data;
            // maintaining regression requires us to NOT cache FF data
            if (containsFillForwardData != false)
            {
                var dataFiltered = new List<BaseData>(data.Count);
                for (var i = 0; i < data.Count; i++)
                {
                    var dataPoint = data[i];
                    if (!dataPoint.IsFillForward)
                    {
                        dataFiltered.Add(dataPoint);
                    }
                }
                nonFillForwardData = dataFiltered;
            }
            if (nonFillForwardData.Count != 0)
            {
                StoreData(nonFillForwardData, dataType);
            }
            else if (dataType == typeof(OpenInterest))
            {
                StoreData(data, typeof(OpenInterest));
            }

            var last = data[data.Count - 1];

            ProcessDataPoint(last, cacheByType: false);
        }

        /// <summary>
        /// Add a new market data point to the local security cache for the current market price.
        /// Rules:
        ///     Don't cache fill forward data.
        ///     Always return the last observation.
        ///     If two consecutive data has the same time stamp and one is Quotebars and the other Tradebar, prioritize the Quotebar.
        /// </summary>
        public void AddData(BaseData data)
        {
            ProcessDataPoint(data, cacheByType: true);
        }

        /// <summary>
        /// Will consume the given data point updating the cache state and it's properties
        /// </summary>
        /// <param name="data">The data point to process</param>
        /// <param name="cacheByType">True if this data point should be cached by type</param>
        protected virtual void ProcessDataPoint(BaseData data, bool cacheByType)
        {
            var tick = data as Tick;
            if (tick?.TickType == TickType.OpenInterest)
            {
                if (cacheByType)
                {
                    StoreDataPoint(data);
                }
                OpenInterest = (long)tick.Value;
                return;
            }

            // Only cache non fill-forward data and non auxiliary
            if (data.IsFillForward) return;

            if (cacheByType)
            {
                StoreDataPoint(data);
            }

            // we store auxiliary data by type but we don't use it to set 'lastData' nor price information
            if (data.DataType == MarketDataType.Auxiliary) return;

            var isDefaultDataType = SubscriptionManager.IsDefaultDataType(data);

            // don't set _lastData if receive quotebar then tradebar w/ same end time. this
            // was implemented to grant preference towards using quote data in the fill
            // models and provide a level of determinism on the values exposed via the cache.
            if ((_lastData == null
              || _lastQuoteBarUpdate != data.EndTime
              || data.DataType != MarketDataType.TradeBar)
                // we will only set the default data type to preserve determinism and backwards compatibility
                && isDefaultDataType)
            {
                _lastData = data;
            }

            if (tick != null)
            {
                if (tick.Value != 0) Price = tick.Value;

                switch (tick.TickType)
                {
                    case TickType.Trade:
                        if (tick.Quantity != 0) Volume = tick.Quantity;
                        break;

                    case TickType.Quote:
                        if (tick.BidPrice != 0) BidPrice = tick.BidPrice;
                        if (tick.BidSize != 0) BidSize = tick.BidSize;

                        if (tick.AskPrice != 0) AskPrice = tick.AskPrice;
                        if (tick.AskSize != 0) AskSize = tick.AskSize;
                        break;
                }
                return;
            }

            var bar = data as IBar;
            if (bar != null)
            {
                // we will only set OHLC values using the default data type to preserve determinism and backwards compatibility.
                // Gives priority to QuoteBar over TradeBar, to be removed when default data type completely addressed GH issue 4196
                if ((_lastQuoteBarUpdate != data.EndTime || _lastOHLCUpdate != data.EndTime) && isDefaultDataType)
                {
                    _lastOHLCUpdate = data.EndTime;
                    if (bar.Open != 0) Open = bar.Open;
                    if (bar.High != 0) High = bar.High;
                    if (bar.Low != 0) Low = bar.Low;
                    if (bar.Close != 0)
                    {
                        Price = bar.Close;
                        Close = bar.Close;
                    }
                }

                var tradeBar = bar as TradeBar;
                if (tradeBar != null)
                {
                    if (tradeBar.Volume != 0) Volume = tradeBar.Volume;
                }

                var quoteBar = bar as QuoteBar;
                if (quoteBar != null)
                {
                    _lastQuoteBarUpdate = quoteBar.EndTime;
                    if (quoteBar.Ask != null && quoteBar.Ask.Close != 0) AskPrice = quoteBar.Ask.Close;
                    if (quoteBar.Bid != null && quoteBar.Bid.Close != 0) BidPrice = quoteBar.Bid.Close;
                    if (quoteBar.LastBidSize != 0) BidSize = quoteBar.LastBidSize;
                    if (quoteBar.LastAskSize != 0) AskSize = quoteBar.LastAskSize;
                }
            }
            else if (data.DataType != MarketDataType.Auxiliary)
            {
                if (data.DataType != MarketDataType.Base || data.Price != 0)
                {
                    Price = data.Price;
                }
            }
        }

        /// <summary>
        /// Stores the specified data list in the cache WITHOUT updating any of the cache properties, such as Price
        /// </summary>
        /// <param name="data">The collection of data to store in this cache</param>
        /// <param name="dataType">The data type</param>
        public void StoreData(IReadOnlyList<BaseData> data, Type dataType)
        {
            if (dataType == typeof(Tick))
            {
                var tick = data[data.Count - 1] as Tick;
                switch (tick?.TickType)
                {
                    case TickType.Trade:
                        _lastTickTrades = data;
                        return;
                    case TickType.Quote:
                        _lastTickQuotes = data;
                        return;
                }
            }

            lock (_locker)
            {
                _dataByType ??= new();
                _dataByType[dataType] = data;
            }
        }

        /// <summary>
        /// Get last data packet received for this security if any else null
        /// </summary>
        /// <returns>BaseData type of the security</returns>
        public BaseData GetData()
        {
            return _lastData;
        }

        /// <summary>
        /// Get last data packet received for this security of the specified type
        /// </summary>
        /// <typeparam name="T">The data type</typeparam>
        /// <returns>The last data packet, null if none received of type</returns>
        public T GetData<T>()
            where T : BaseData
        {
            return GetData(typeof(T)) as T;
        }

        /// <summary>
        /// Retrieves the last data packet of the specified Python type.
        /// </summary>
        /// <param name="pyType">The Python type to convert and match</param>
        /// <returns>The last data packet as a PyObject, or null if not found</returns>
        public PyObject GetData(PyObject pyType)
        {
            using var _ = Py.GIL();
            if (!pyType.TryCreateType(out var type))
            {
                return null;
            }
            type = typeof(PythonData).IsAssignableFrom(type) ? typeof(PythonData) : type;
            return GetData(type).ToPython();
        }

        /// <summary>
        /// Get the last data packet of the specified type
        /// </summary>
        /// <param name="type">The type of data to retrieve</param>
        /// <returns>The last data packet of the specified type, or null if none found</returns>
        private BaseData GetData(Type type)
        {
            IReadOnlyList<BaseData> list;
            if (!TryGetValue(type, out list) || list.Count == 0)
            {
                return null;
            }
            return list[list.Count - 1];
        }

        /// <summary>
        /// Gets all data points of the specified type from the most recent time step
        /// that produced data for that type
        /// </summary>
        public IEnumerable<T> GetAll<T>()
        {
            if (typeof(T) == typeof(Tick))
            {
                return _lastTickTrades.Concat(_lastTickQuotes).Cast<T>();
            }

            lock (_locker)
            {
                if (_dataByType == null || !_dataByType.TryGetValue(typeof(T), out var list))
                {
                    return new List<T>();
                }

                return list.Cast<T>();
            }
        }

        /// <summary>
        /// Reset cache storage and free memory
        /// </summary>
        public void Reset()
        {
            Price = 0;

            Open = 0;
            High = 0;
            Low = 0;
            Close = 0;

            BidPrice = 0;
            BidSize = 0;
            AskPrice = 0;
            AskSize = 0;

            Volume = 0;
            OpenInterest = 0;

            _lastData = null;
            _dataByType = null;
            _lastTickQuotes = _empty;
            _lastTickTrades = _empty;

            _lastOHLCUpdate = default;
            _lastQuoteBarUpdate = default;
        }

        /// <summary>
        /// Gets whether or not this dynamic data instance has data stored for the specified type
        /// </summary>
        public bool HasData(Type type)
        {
            return TryGetValue(type, out _);
        }

        /// <summary>
        /// Gets whether or not this dynamic data instance has data stored for the specified type
        /// </summary>
        public bool TryGetValue(Type type, out IReadOnlyList<BaseData> data)
        {
            if (type == typeof(Fundamentals))
            {
                // for backwards compatibility
                type = typeof(FundamentalUniverse);
            }
            else if (type == typeof(ETFConstituentData))
            {
                // for backwards compatibility
                type = typeof(ETFConstituentUniverse);
            }
            else if (type == typeof(Tick))
            {
                var quote = _lastTickQuotes.LastOrDefault();
                var trade = _lastTickTrades.LastOrDefault();
                var isQuoteDefaultDataType = quote != null && SubscriptionManager.IsDefaultDataType(quote);
                var isTradeDefaultDataType = trade != null && SubscriptionManager.IsDefaultDataType(trade);

                // Currently, IsDefaultDataType returns true for both cases,
                // So we will return the list with the tick with the most recent timestamp
                if (isQuoteDefaultDataType && isTradeDefaultDataType)
                {
                    data = quote.EndTime > trade.EndTime ? _lastTickQuotes : _lastTickTrades;
                    return true;
                }

                data = isQuoteDefaultDataType ? _lastTickQuotes : _lastTickTrades;
                return data?.Count > 0;
            }

            data = default;
            return _dataByType != null && _dataByType.TryGetValue(type, out data);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void StoreDataPoint(BaseData data)
        {
            if (data.GetType() == typeof(Tick))
            {
                var tick = data as Tick;
                switch (tick?.TickType)
                {
                    case TickType.Trade:
                        _lastTickTrades = new List<BaseData> { tick };
                        break;
                    case TickType.Quote:
                        _lastTickQuotes = new List<BaseData> { tick };
                        break;
                }
            }
            else
            {
                lock (_locker)
                {
                    _dataByType ??= new();
                    // Always keep track of the last observation
                    IReadOnlyList<BaseData> list;
                    if (!_dataByType.TryGetValue(data.GetType(), out list))
                    {
                        list = new List<BaseData> { data };
                        _dataByType[data.GetType()] = list;
                    }
                    else
                    {
                        // we KNOW this one is actually a list, so this is safe
                        // we overwrite the zero entry so we're not constantly newing up lists
                        ((List<BaseData>)list)[0] = data;
                    }
                }
            }
        }

        /// <summary>
        /// Helper method that modifies the target security cache instance to use the
        /// type cache of the source
        /// </summary>
        /// <remarks>Will set in the source cache any data already present in the target cache</remarks>
        /// <remarks>This is useful for custom data securities which also have an underlying security,
        /// will allow both securities to access the same data by type</remarks>
        /// <param name="sourceToShare">The source cache to use</param>
        /// <param name="targetToModify">The target security cache that will be modified</param>
        public static void ShareTypeCacheInstance(SecurityCache sourceToShare, SecurityCache targetToModify)
        {
            sourceToShare._dataByType ??= new();
            if (targetToModify._dataByType != null)
            {
                lock (targetToModify._locker)
                {
                    lock (sourceToShare._locker)
                    {
                        foreach (var kvp in targetToModify._dataByType)
                        {
                            sourceToShare._dataByType.TryAdd(kvp.Key, kvp.Value);
                        }
                    }
                }
            }
            targetToModify._dataByType = sourceToShare._dataByType;
            targetToModify._lastTickTrades = sourceToShare._lastTickTrades;
            targetToModify._lastTickQuotes = sourceToShare._lastTickQuotes;
        }

        /// <summary>
        /// Applies the split to the security cache values
        /// </summary>
        internal void ApplySplit(Split split)
        {
            Price *= split.SplitFactor;
            Open *= split.SplitFactor;
            High *= split.SplitFactor;
            Low *= split.SplitFactor;
            Close *= split.SplitFactor;
            Volume /= split.SplitFactor;
            BidPrice *= split.SplitFactor;
            AskPrice *= split.SplitFactor;
            AskSize /= split.SplitFactor;
            BidSize /= split.SplitFactor;

            // Adjust values for the last data we have cached
            Action<BaseData> scale = data => data.Scale((target, factor, _) => target * factor, 1 / split.SplitFactor, split.SplitFactor, decimal.Zero);
            _dataByType?.Values.DoForEach(x => x.DoForEach(scale));
            _lastTickQuotes.DoForEach(scale);
            _lastTickTrades.DoForEach(scale);
        }
    }
}
