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
 *
*/

using System;
using System.Collections.Generic;
using QuantConnect.Data;
using System.Linq;
using QuantConnect.Securities.Option;

namespace QuantConnect.Securities
{
    /// <summary>
    /// Represents options symbols universe used in filtering.
    /// </summary>
    public class OptionFilterUniverse : ContractSecurityFilterUniverse<OptionFilterUniverse>
    {
        // Fields used in relative strikes filter
        private List<decimal> _uniqueStrikes;
        private bool _refreshUniqueStrikes;

        /// <summary>
        /// Constructs OptionFilterUniverse
        /// </summary>
        public OptionFilterUniverse()
        {
        }

        /// <summary>
        /// Constructs OptionFilterUniverse
        /// </summary>
        public OptionFilterUniverse(IEnumerable<Symbol> allSymbols, BaseData underlying)
        {
            Refresh(allSymbols, underlying, exchangeDateChange: true);
        }

        /// <summary>
        /// Refreshes this option filter universe and allows specifying if the exchange date changed from last call
        /// </summary>
        /// <param name="allSymbols">All the options contract symbols</param>
        /// <param name="underlying">The current underlying last data point</param>
        /// <param name="exchangeDateChange">True if the exchange data has chanced since the last call or construction</param>
        public void Refresh(IEnumerable<Symbol> allSymbols, BaseData underlying, bool exchangeDateChange = true)
        {
            _allSymbols = allSymbols;
            _underlying = underlying;
            _type = Type.Standard;
            _isDynamic = false;
            _refreshUniqueStrikes = exchangeDateChange;
        }

        /// <summary>
        /// Determine if the given Option contract symbol is standard
        /// </summary>
        /// <returns></returns>
        protected override bool IsStandard(Symbol symbol)
        {
            return OptionSymbol.IsStandard(symbol);
        }

        /// <summary>
        /// Applies filter selecting options contracts based on a range of strikes in relative terms
        /// </summary>
        /// <param name="minStrike">The minimum strike relative to the underlying price, for example, -1 would filter out contracts further than 1 strike below market price</param>
        /// <param name="maxStrike">The maximum strike relative to the underlying price, for example, +1 would filter out contracts further than 1 strike above market price</param>
        /// <returns></returns>
        public OptionFilterUniverse Strikes(int minStrike, int maxStrike)
        {
            if (_underlying == null)
            {
                return this;
            }

            if (_refreshUniqueStrikes || _uniqueStrikes == null)
            {
                // each day we need to recompute the unique strikes list
                _uniqueStrikes = _allSymbols.Select(x => x.ID.StrikePrice)
                    .Distinct()
                    .OrderBy(strikePrice => strikePrice)
                    .ToList();
                _refreshUniqueStrikes = false;
            }

            // new universe is dynamic
            _isDynamic = true;

            // find the current price in the list of strikes
            var exactPriceFound = true;
            var index = _uniqueStrikes.BinarySearch(_underlying.Price);

            // Return value of BinarySearch (from MSDN):
            // The zero-based index of item in the sorted List<T>, if item is found;
            // otherwise, a negative number that is the bitwise complement of the index of the next element that is larger than item
            // or, if there is no larger element, the bitwise complement of Count.
            if (index < 0)
            {
                // exact price not found
                exactPriceFound = false;

                if (index == ~_uniqueStrikes.Count)
                {
                    // there is no greater price, return empty
                    _allSymbols = Enumerable.Empty<Symbol>();
                    return this;
                }

                index = ~index;
            }

            // compute the bounds, no need to worry about rounding and such
            var indexMinPrice = index + minStrike;
            var indexMaxPrice = index + maxStrike;
            if (!exactPriceFound)
            {
                if (minStrike < 0 && maxStrike > 0)
                {
                    indexMaxPrice--;
                }
                else if (minStrike > 0)
                {
                    indexMinPrice--;
                    indexMaxPrice--;
                }
            }

            if (indexMinPrice < 0)
            {
                indexMinPrice = 0;
            }
            else if (indexMinPrice >= _uniqueStrikes.Count)
            {
                // price out of range: return empty
                _allSymbols = Enumerable.Empty<Symbol>();
                return this;
            }

            if (indexMaxPrice < 0)
            {
                // price out of range: return empty
                _allSymbols = Enumerable.Empty<Symbol>();
                return this;
            }
            if (indexMaxPrice >= _uniqueStrikes.Count)
            {
                indexMaxPrice = _uniqueStrikes.Count - 1;
            }

            var minPrice = _uniqueStrikes[indexMinPrice];
            var maxPrice = _uniqueStrikes[indexMaxPrice];

            _allSymbols = _allSymbols
                .Where(symbol =>
                    {
                        var price = symbol.ID.StrikePrice;
                        return price >= minPrice && price <= maxPrice;
                    }
                ).ToList();

            return this;
        }


        /// <summary>
        /// Sets universe of call options (if any) as a selection
        /// </summary>
        public OptionFilterUniverse CallsOnly()
        {
            return Contracts(contracts => contracts.Where(x => x.ID.OptionRight == OptionRight.Call));
        }

        /// <summary>
        /// Sets universe of put options (if any) as a selection
        /// </summary>
        public OptionFilterUniverse PutsOnly()
        {
            return Contracts(contracts => contracts.Where(x => x.ID.OptionRight == OptionRight.Put));
        }
    }

    /// <summary>
    /// Extensions for Linq support
    /// </summary>
    public static class OptionFilterUniverseEx
    {
        /// <summary>
        /// Filters universe
        /// </summary>
        /// <param name="universe"></param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public static OptionFilterUniverse Where(this OptionFilterUniverse universe, Func<Symbol, bool> predicate)
        {
            universe._allSymbols = universe._allSymbols.Where(predicate).ToList();
            universe._isDynamic = true;
            return universe;
        }

        /// <summary>
        /// Maps universe
        /// </summary>
        public static OptionFilterUniverse Select(this OptionFilterUniverse universe, Func<Symbol, Symbol> mapFunc)
        {
            universe._allSymbols = universe._allSymbols.Select(mapFunc).ToList();
            universe._isDynamic = true;
            return universe;
        }

        /// <summary>
        /// Binds universe
        /// </summary>
        public static OptionFilterUniverse SelectMany(this OptionFilterUniverse universe, Func<Symbol, IEnumerable<Symbol>> mapFunc)
        {
            universe._allSymbols = universe._allSymbols.SelectMany(mapFunc).ToList();
            universe._isDynamic = true;
            return universe;
        }

        /// <summary>
        /// Updates universe to only contain the symbols in the list
        /// </summary>
        public static OptionFilterUniverse WhereContains(this OptionFilterUniverse universe, List<Symbol> filterList)
        {
            universe._allSymbols = universe._allSymbols.Where(filterList.Contains).ToList();
            universe._isDynamic = true;
            return universe;
        }
    }
}
