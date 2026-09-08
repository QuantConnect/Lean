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

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace QuantConnect.Data.Market
{
    /// <summary>
    /// The distinct strikes of a chain, ascending and read only, with helpers that return null (None in Python) when no strike matches
    /// </summary>
    public class StrikeList : ReadOnlyCollection<decimal>
    {
        private readonly List<decimal> _strikes;

        /// <summary>
        /// Creates the list from the given strikes, in any order, duplicates allowed
        /// </summary>
        /// <param name="strikes">The strike prices</param>
        public StrikeList(IEnumerable<decimal> strikes)
            : this(strikes.Distinct().OrderBy(strike => strike).ToList())
        {
        }

        private StrikeList(List<decimal> strikes)
            : base(strikes)
        {
            _strikes = strikes;
        }

        /// <summary>
        /// The strike closest to the price, the lower one on ties
        /// </summary>
        /// <param name="price">The reference price, e.g. the underlying price</param>
        /// <returns>The closest strike, or null when the list is empty</returns>
        public decimal? ClosestTo(decimal price)
        {
            if (_strikes.Count == 0)
            {
                return null;
            }

            var index = _strikes.BinarySearch(price);
            if (index >= 0)
            {
                return _strikes[index];
            }

            // the complement is the first strike above the price, so the candidates are it and the one before
            index = ~index;
            if (index == 0)
            {
                return _strikes[0];
            }
            if (index == _strikes.Count)
            {
                return _strikes[index - 1];
            }

            var below = _strikes[index - 1];
            var above = _strikes[index];
            return above - price < price - below ? above : below;
        }

        /// <summary>
        /// The lowest strike above the price
        /// </summary>
        /// <param name="price">The reference price, e.g. the underlying price</param>
        /// <returns>The first strike above the price, or null when there is none</returns>
        public decimal? FirstAbove(decimal price)
        {
            var index = _strikes.BinarySearch(price);
            index = index >= 0 ? index + 1 : ~index;
            return index < _strikes.Count ? _strikes[index] : null;
        }

        /// <summary>
        /// The highest strike below the price
        /// </summary>
        /// <param name="price">The reference price, e.g. the underlying price</param>
        /// <returns>The first strike below the price, or null when there is none</returns>
        public decimal? FirstBelow(decimal price)
        {
            var index = _strikes.BinarySearch(price);
            index = (index >= 0 ? index : ~index) - 1;
            return index >= 0 ? _strikes[index] : null;
        }
    }
}
