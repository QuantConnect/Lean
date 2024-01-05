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
    public class DefaultOrderBook : IOrderBookUpdater<decimal, decimal>
    {
        private decimal _bestBidPrice;
        private decimal _bestBidSize;
        private decimal _bestAskPrice;
        private decimal _bestAskSize;
        private readonly object _locker = new object();

        /// <summary>
        /// Represents bid prices and sizes
        /// </summary>
        protected readonly SortedDictionary<decimal, decimal> Bids = new SortedDictionary<decimal, decimal>();

        /// <summary>
        /// Represents ask prices and sizes
        /// </summary>
        protected readonly SortedDictionary<decimal, decimal> Asks = new SortedDictionary<decimal, decimal>();

        /// <summary>
        /// Represents a unique security identifier of current Order Book
        /// </summary>
        public Symbol Symbol { get; }

        /// <summary>
        /// Event fired each time <see cref="BestBidPrice"/> or <see cref="BestAskPrice"/> are changed
        /// </summary>
        public event EventHandler<BestBidAskUpdatedEventArgs> BestBidAskUpdated;

        /// <summary>
        /// The best bid price
        /// </summary>
        public decimal BestBidPrice
        {
            get
            {
                lock (_locker)
                {
                    return _bestBidPrice;
                }
            }
        }

        /// <summary>
        /// The best bid size
        /// </summary>
        public decimal BestBidSize
        {
            get
            {
                lock (_locker)
                {
                    return _bestBidSize;
                }
            }
        }

        /// <summary>
        /// The best ask price
        /// </summary>
        public decimal BestAskPrice
        {
            get
            {
                lock (_locker)
                {
                    return _bestAskPrice;
                }
            }
        }

        /// <summary>
        /// The best ask size
        /// </summary>
        public decimal BestAskSize
        {
            get
            {
                lock (_locker)
                {
                    return _bestAskSize;
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultOrderBook"/> class
        /// </summary>
        /// <param name="symbol">The symbol for the order book</param>
        public DefaultOrderBook(Symbol symbol)
        {
            Symbol = symbol;
        }

        /// <summary>
        /// Clears all bid/ask levels and prices.
        /// </summary>
        public void Clear()
        {
            lock (_locker)
            {
                _bestBidPrice = 0;
                _bestBidSize = 0;
                _bestAskPrice = 0;
                _bestAskSize = 0;

                Bids.Clear();
                Asks.Clear();
            }
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
                Bids[price] = size;

                if (_bestBidPrice == 0 || price >= _bestBidPrice)
                {
                    _bestBidPrice = price;
                    _bestBidSize = size;

                    BestBidAskUpdated?.Invoke(this, new BestBidAskUpdatedEventArgs(Symbol, _bestBidPrice, _bestBidSize, _bestAskPrice, _bestAskSize));
                }
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
                Asks[price] = size;

                if (_bestAskPrice == 0 || price <= _bestAskPrice)
                {
                    _bestAskPrice = price;
                    _bestAskSize = size;

                    BestBidAskUpdated?.Invoke(this, new BestBidAskUpdatedEventArgs(Symbol, _bestBidPrice, _bestBidSize, _bestAskPrice, _bestAskSize));
                }
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
                Bids.Remove(price);

                if (price == _bestBidPrice)
                {
                    var priceLevel = Bids.LastOrDefault();
                    _bestBidPrice = priceLevel.Key;
                    _bestBidSize = priceLevel.Value;

                    BestBidAskUpdated?.Invoke(this, new BestBidAskUpdatedEventArgs(Symbol, _bestBidPrice, _bestBidSize, _bestAskPrice, _bestAskSize));
                }
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
                Asks.Remove(price);

                if (price == _bestAskPrice)
                {
                    var priceLevel = Asks.FirstOrDefault();
                    _bestAskPrice = priceLevel.Key;
                    _bestAskSize = priceLevel.Value;

                    BestBidAskUpdated?.Invoke(this, new BestBidAskUpdatedEventArgs(Symbol, _bestBidPrice, _bestBidSize, _bestAskPrice, _bestAskSize));
                }
            }
        }

        /// <summary>
        /// Common price level removal method
        /// </summary>
        /// <param name="priceLevel"></param>
        public void RemovePriceLevel(decimal priceLevel)
        {
            lock (_locker)
            {
                if (Asks.ContainsKey(priceLevel))
                {
                    RemoveAskRow(priceLevel);
                }
                else if (Bids.ContainsKey(priceLevel))
                {
                    RemoveBidRow(priceLevel);
                }
            }
        }
    }
}
