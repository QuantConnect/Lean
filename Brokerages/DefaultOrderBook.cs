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
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Brokerages
{
    /// <summary>
    /// Represents a full order book for a security.
    /// It contains prices and order sizes for each bid and ask level.
    /// The best bid and ask prices are also kept up to date.
    /// </summary>
    public class DefaultOrderBook : IOrderBook<decimal, decimal>
    {
        private readonly object _locker = new object();
        private readonly Symbol _symbol;
        protected readonly SortedDictionary<decimal, decimal> _bids = new SortedDictionary<decimal, decimal>();
        protected readonly SortedDictionary<decimal, decimal> _asks = new SortedDictionary<decimal, decimal>();

        /// <summary>
        /// Event fired each time <see cref="BestBidPrice"/> or <see cref="BestAskPrice"/> are changed
        /// </summary>
        public event EventHandler<BestBidAskUpdatedEventArgs> BestBidAskUpdated;

        /// <summary>
        /// The best bid price
        /// </summary>
        public decimal BestBidPrice { get; private set; }

        /// <summary>
        /// The best bid size
        /// </summary>
        public decimal BestBidSize { get; private set; }

        /// <summary>
        /// The best ask price
        /// </summary>
        public decimal BestAskPrice { get; private set; }

        /// <summary>
        /// The best ask size
        /// </summary>
        public decimal BestAskSize { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultOrderBook"/> class
        /// </summary>
        /// <param name="symbol">The symbol for the order book</param>
        public DefaultOrderBook(Symbol symbol)
        {
            _symbol = symbol;
        }

        /// <summary>
        /// Clears all bid/ask levels and prices.
        /// </summary>
        public void Clear()
        {
            lock (_locker)
            {
                _bids.Clear();
                _asks.Clear();
            }

            BestBidPrice = 0;
            BestBidSize = 0;
            BestAskPrice = 0;
            BestAskSize = 0;
        }

        /// <summary>
        /// Updates or inserts a bid price level in the order book
        /// </summary>
        /// <param name="price">The bid price level to be inserted or updated</param>
        /// <param name="size">The new size at the bid price level</param>
        public void UpdateBidRow(decimal price, decimal size)
        {
            lock (_locker)
            {
                _bids[price] = size;
            }

            if (BestBidPrice == 0 || price >= BestBidPrice)
            {
                BestBidPrice = price;
                BestBidSize = size;

                BestBidAskUpdated?.Invoke(this, new BestBidAskUpdatedEventArgs(_symbol, BestBidPrice, BestBidSize, BestAskPrice, BestAskSize));
            }
        }

        /// <summary>
        /// Updates or inserts an ask price level in the order book
        /// </summary>
        /// <param name="price">The ask price level to be inserted or updated</param>
        /// <param name="size">The new size at the ask price level</param>
        public void UpdateAskRow(decimal price, decimal size)
        {
            lock (_locker)
            {
                _asks[price] = size;
            }

            if (BestAskPrice == 0 || price <= BestAskPrice)
            {
                BestAskPrice = price;
                BestAskSize = size;

                BestBidAskUpdated?.Invoke(this, new BestBidAskUpdatedEventArgs(_symbol, BestBidPrice, BestBidSize, BestAskPrice, BestAskSize));
            }
        }

        /// <summary>
        /// Removes a bid price level from the order book
        /// </summary>
        /// <param name="price">The bid price level to be removed</param>
        public void RemoveBidRow(decimal price)
        {
            lock (_locker)
            {
                _bids.Remove(price);
            }

            if (price == BestBidPrice)
            {
                lock (_locker)
                {
                    BestBidPrice = _bids.Keys.LastOrDefault();
                    BestBidSize = BestBidPrice > 0 ? _bids[BestBidPrice] : 0;
                }

                BestBidAskUpdated?.Invoke(this, new BestBidAskUpdatedEventArgs(_symbol, BestBidPrice, BestBidSize, BestAskPrice, BestAskSize));
            }
        }

        /// <summary>
        /// Removes an ask price level from the order book
        /// </summary>
        /// <param name="price">The ask price level to be removed</param>
        public void RemoveAskRow(decimal price)
        {
            lock (_locker)
            {
                _asks.Remove(price);
            }

            if (price == BestAskPrice)
            {
                lock (_locker)
                {
                    BestAskPrice = _asks.Keys.FirstOrDefault();
                    BestAskSize = BestAskPrice > 0 ? _asks[BestAskPrice] : 0;
                }

                BestBidAskUpdated?.Invoke(this, new BestBidAskUpdatedEventArgs(_symbol, BestBidPrice, BestBidSize, BestAskPrice, BestAskSize));
            }
        }

        /// <summary>
        /// Common price level removal method
        /// </summary>
        /// <param name="priceLevel"></param>
        public void RemovePriceLevel(decimal priceLevel)
        {
            if (_asks.ContainsKey(priceLevel))
            {
                RemoveAskRow(priceLevel);
            }
            else if (_bids.ContainsKey(priceLevel))
            {
                RemoveBidRow(priceLevel);
            }
        }
    }
}
