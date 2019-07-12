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
    /// Represents an orderbook updater interface for a security.
    /// Provides the ability to update orderbook price level and to be alerted about updates
    /// </summary>
    /// <typeparam name="K">Price level identifier</typeparam>
    /// <typeparam name="V">Size at the price level</typeparam>
    public interface IOrderBookUpdater<K, V>
    {
        /// <summary>
        /// Event fired each time <see cref="BestBidPrice"/> or <see cref="BestAskPrice"/> are changed
        /// </summary>
        event EventHandler<BestBidAskUpdatedEventArgs> BestBidAskUpdated;

        /// <summary>
        /// Updates or inserts a bid price level in the order book
        /// </summary>
        /// <param name="price">The bid price level to be inserted or updated</param>
        /// <param name="size">The new size at the bid price level</param>
        void UpdateBidRow(K price, V size);

        /// <summary>
        /// Updates or inserts an ask price level in the order book
        /// </summary>
        /// <param name="price">The ask price level to be inserted or updated</param>
        /// <param name="size">The new size at the ask price level</param>
        void UpdateAskRow(K price, V size);

        /// <summary>
        /// Removes a bid price level from the order book
        /// </summary>
        /// <param name="price">The bid price level to be removed</param>
        void RemoveBidRow(K price);

        /// <summary>
        /// Removes an ask price level from the order book
        /// </summary>
        /// <param name="price">The ask price level to be removed</param>
        void RemoveAskRow(K price);
    }
}
