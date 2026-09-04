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

namespace QuantConnect.Data.Market
{
    /// <summary>
    /// The distinct strikes of a chain, ascending, with helpers that return null (None in Python) when no strike matches
    /// </summary>
    public class StrikeList : List<decimal>
    {
        /// <summary>
        /// Creates the list from the given strikes, in any order, duplicates allowed
        /// </summary>
        /// <param name="strikes">The strike prices</param>
        public StrikeList(IEnumerable<decimal> strikes)
            : base(strikes.Distinct().OrderBy(strike => strike))
        {
        }

        /// <summary>
        /// The strike closest to the price, the lower one on ties
        /// </summary>
        /// <param name="price">The reference price, e.g. the underlying price</param>
        /// <returns>The closest strike, or null when the list is empty</returns>
        public decimal? ClosestTo(decimal price)
        {
            decimal? closest = null;
            foreach (var strike in this)
            {
                // ascending order plus strict comparison keeps the lower strike on ties
                if (closest == null || Math.Abs(strike - price) < Math.Abs(closest.Value - price))
                {
                    closest = strike;
                }
            }
            return closest;
        }

        /// <summary>
        /// The lowest strike above the price
        /// </summary>
        /// <param name="price">The reference price, e.g. the underlying price</param>
        /// <returns>The first strike above the price, or null when there is none</returns>
        public decimal? FirstAbove(decimal price)
        {
            foreach (var strike in this)
            {
                if (strike > price)
                {
                    return strike;
                }
            }
            return null;
        }

        /// <summary>
        /// The highest strike below the price
        /// </summary>
        /// <param name="price">The reference price, e.g. the underlying price</param>
        /// <returns>The first strike below the price, or null when there is none</returns>
        public decimal? FirstBelow(decimal price)
        {
            for (var i = Count - 1; i >= 0; i--)
            {
                if (this[i] < price)
                {
                    return this[i];
                }
            }
            return null;
        }
    }
}
