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
using System.Collections.Concurrent;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Data.Market;

namespace QuantConnect.Securities
{
    /// <summary>
    /// Base class caching caching spot for security data and any other temporary properties.
    /// </summary>
    /// <remarks>
    /// This class is virtually unused and will soon be made obsolete.
    /// This comment made in a remark to prevent obsolete errors in all users algorithms
    /// </remarks>
    public class SecurityCache
    {
        // this is used to prefer quote bar data over the tradebar data
        private DateTime _lastQuoteBarUpdate;
        private BaseData _lastData;
        private readonly ConcurrentDictionary<Type, BaseData> _dataByType = new ConcurrentDictionary<Type, BaseData>();

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
        /// Add a new market data point to the local security cache for the current market price.
        /// Rules:
        ///     Don't cache fill forward data.
        ///     Always return the last observation.
        ///     If two consecutive data has the same time stamp and one is Quotebars and the other Tradebar, prioritize the Quotebar.
        /// </summary>
        public void AddData(BaseData data)
        {
            var openInterest = data as OpenInterest;
            if (openInterest != null)
            {
                OpenInterest = (long)openInterest.Value;
                return;
            }

            // Only cache non fill-forward data.
            if (data.IsFillForward) return;

            // Always keep track of the last obesrvation
            _dataByType[data.GetType()] = data;

            // don't set _lastData if receive quotebar then tradebar w/ same end time. this
            // was implemented to grant preference towards using quote data in the fill
            // models and provide a level of determinism on the values exposed via the cache.
            if (_lastData == null
              || _lastQuoteBarUpdate != data.EndTime
              || data.DataType != MarketDataType.TradeBar )
            {
                _lastData = data;
            }


            var tick = data as Tick;
            if (tick != null)
            {
                if (tick.Value != 0) Price = tick.Value;

                if (tick.BidPrice != 0) BidPrice = tick.BidPrice;
                if (tick.BidSize != 0) BidSize = tick.BidSize;

                if (tick.AskPrice != 0) AskPrice = tick.AskPrice;
                if (tick.AskSize != 0) AskSize = tick.AskSize;

                if (tick.Quantity != 0) Volume = tick.Quantity;

                return;
            }
            var bar = data as IBar;
            if (bar != null)
            {
                if (_lastQuoteBarUpdate != data.EndTime)
                {
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
            else
            {
                Price = data.Price;
            }
        }

        /// <summary>
        /// Get last data packet recieved for this security
        /// </summary>
        /// <returns>BaseData type of the security</returns>
        public BaseData GetData()
        {
            return _lastData;
        }

        /// <summary>
        /// Get last data packet recieved for this security of the specified ty[e
        /// </summary>
        /// <typeparam name="T">The data type</typeparam>
        /// <returns>The last data packet, null if none received of type</returns>
        public T GetData<T>()
            where T : BaseData
        {
            BaseData data;
            _dataByType.TryGetValue(typeof(T), out data);
            return data as T;
        }

        /// <summary>
        /// Reset cache storage and free memory
        /// </summary>
        public void Reset()
        {
            _dataByType.Clear();
        }
    }
}
