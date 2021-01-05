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

namespace QuantConnect.Data.Market
{
    /// <summary>
    /// Collection of TradeBars to create a data type for generic data handler:
    /// </summary>
    public class TradeBars : DataDictionary<TradeBar>
    {
        /// <summary>
        /// Creates a new instance of the <see cref="TradeBars"/> dictionary
        /// </summary>
        public TradeBars()
        {
        }

        /// <summary>
        /// Creates a new instance of the <see cref="TradeBars"/> dictionary
        /// </summary>
        /// <param name="frontier">The time associated with the data in this dictionary</param>
        public TradeBars(DateTime frontier)
            : base(frontier)
        {
        }

        /// <summary>
        /// Gets or sets the TradeBar with the specified ticker.
        /// </summary>
        /// <returns>
        /// The TradeBar with the specified ticker.
        /// </returns>
        /// <param name="ticker">The ticker of the element to get or set.</param>
        /// <remarks>Wraps the base implementation to enable indexing in python algorithms due to pythonnet limitations</remarks>
        public new TradeBar this[string ticker] { get { return base[ticker]; } set { base[ticker] = value; } }

        /// <summary>
        /// Gets or sets the TradeBar with the specified Symbol.
        /// </summary>
        /// <returns>
        /// The TradeBar with the specified Symbol.
        /// </returns>
        /// <param name="symbol">The Symbol of the element to get or set.</param>
        /// <remarks>Wraps the base implementation to enable indexing in python algorithms due to pythonnet limitations</remarks>
        public new TradeBar this[Symbol symbol] { get { return base[symbol]; } set { base[symbol] = value; } }
    }
}