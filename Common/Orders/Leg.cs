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

namespace QuantConnect.Orders
{
    /// <summary>
    /// Basic order leg
    /// </summary>
    public class Leg
    {
        /// <summary>
        /// The legs symbol
        /// </summary>
        public Symbol Symbol { get; set; }

        /// <summary>
        /// Quantity multiplier used to specify proper scale (and direction) of the leg within the strategy
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Order limit price of the leg in case limit order is sent to the market on strategy execution
        /// </summary>
        public decimal? OrderPrice { get; set; }

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="symbol">The symbol</param>
        /// <param name="quantity">The quantity</param>
        /// <param name="limitPrice">Associated limit price if any</param>
        public static Leg Create(Symbol symbol, int quantity, decimal? limitPrice = null)
        {
            return new Leg { Symbol = symbol, Quantity = quantity, OrderPrice= limitPrice};
        }
    }
}
