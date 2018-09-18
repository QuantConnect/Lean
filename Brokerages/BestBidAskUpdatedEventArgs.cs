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

namespace QuantConnect.Brokerages
{
    /// <summary>
    /// Event arguments class for the <see cref="DefaultOrderBook.BestBidAskUpdated"/> event
    /// </summary>
    public sealed class BestBidAskUpdatedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the new best bid price
        /// </summary>
        public Symbol Symbol { get; }

        /// <summary>
        /// Gets the new best bid price
        /// </summary>
        public decimal BestBidPrice { get; }

        /// <summary>
        /// Gets the new best bid size
        /// </summary>
        public decimal BestBidSize { get; }

        /// <summary>
        /// Gets the new best ask price
        /// </summary>
        public decimal BestAskPrice { get; }

        /// <summary>
        /// Gets the new best ask size
        /// </summary>
        public decimal BestAskSize { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BestBidAskUpdatedEventArgs"/> class
        /// </summary>
        /// <param name="symbol">The symbol</param>
        /// <param name="bestBidPrice">The newly updated best bid price</param>
        /// <param name="bestBidSize">>The newly updated best bid size</param>
        /// <param name="bestAskPrice">The newly updated best ask price</param>
        /// <param name="bestAskSize">The newly updated best ask size</param>
        public BestBidAskUpdatedEventArgs(Symbol symbol, decimal bestBidPrice, decimal bestBidSize, decimal bestAskPrice, decimal bestAskSize)
        {
            Symbol = symbol;
            BestBidPrice = bestBidPrice;
            BestBidSize = bestBidSize;
            BestAskPrice = bestAskPrice;
            BestAskSize = bestAskSize;
        }
    }
}
