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
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Brokerages.Binance
{
    /// <summary>
    /// Represents a full order book for a security.
    /// It contains prices and order sizes for each bid and ask level.
    /// The best bid and ask prices are also kept up to date.
    /// </summary>
    public class BinanceOrderBook: DefaultOrderBook
    {
        /// <summary>
        /// Last update event
        /// </summary>
        public long LastUpdateId { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BinanceOrderBook"/> class
        /// </summary>
        /// <param name="symbol">The symbol for the order book</param>
        public BinanceOrderBook(Symbol symbol): base(symbol) { }

        /// <summary>
        /// Clears all bid/ask levels and prices.
        /// </summary>
        public void Reset()
        {
            Clear();
            LastUpdateId = 0;
        }
    }
}
