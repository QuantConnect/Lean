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
using System.Collections;
using QuantConnect.Util;
using QuantConnect.Securities.Option;

namespace QuantConnect.Securities
{
    /// <summary>
    /// Represents options symbols universe used in filtering.
    /// </summary>
    public class OptionFilterUniverse : IDerivativeSecurityFilterUniverse
    {
        /// <summary>
        /// Defines listed option types
        /// </summary>
        public enum Type : int
        {
            /// <summary>
            /// Listed stock options that expire 3rd Friday of the month
            /// </summary>
            Standard = 1,

            /// <summary>
            /// Weeklys options that expire every week
            /// These are options listed with approximately one week to expiration
            /// </summary>
            Weeklys = 2
        }

        internal IEnumerable<Symbol> _allSymbols;

        /// <summary>
        /// The underlying price data
        /// </summary>
        public BaseData Underlying
        {
            get
            {
                // underlying value changes over time, so accessing it makes universe dynamic
                _isDynamic = true;
                return _underlying;
            }
        }

        internal BaseData _underlying;

        /// <summary>
        /// True if the universe is dynamic and filter needs to be reapplied
        /// </summary>
        public bool IsDynamic
        {
            get
            {
                return _isDynamic;
            }
        }

        internal bool _isDynamic;
        internal Type _type = Type.Standard;

        // Fields used in relative strikes filter
        private List<decimal> _uniqueStrikes;
        private DateTime _uniqueStrikesResolveDate;

        /// <summary>
        /// Constructs OptionFilterUniverse
        /// </summary>
        public OptionFilterUniverse(IEnumerable<Symbol> allSymbols, BaseData underlying)
        {
            _allSymbols = allSymbols;
            _underlying = underlying;
            _type = Type.Standard;
            _isDynamic = false;
        }

        /// <summary>
        /// Includes universe of weeklys options (if any) into selection
        /// </summary>
        /// <returns></returns>
        public OptionFilterUniverse IncludeWeeklys()
        {
            _type |= Type.Weeklys;
            return this;
        }

        /// <summary>
        /// Sets universe of weeklys options (if any) as a selection
        /// </summary>
        /// <returns></returns>
        public OptionFilterUniverse WeeklysOnly()
        {
            _type = Type.Weeklys;
            return this;
        }

        /// <summary>
        /// Returns universe, filtered by option type
        /// </summary>
        /// <returns></returns>
        internal OptionFilterUniverse ApplyOptionTypesFilter()
        {
            // memoization map for ApplyOptionTypesFilter()
            var memoizedMap = new Dictionary<DateTime, bool>();

            Func<Symbol, bool> memoizedIsStandardType = symbol =>
            {
                var dt = symbol.ID.Date;

                if (memoizedMap.ContainsKey(dt))
                    return memoizedMap[dt];
                var res = OptionSymbol.IsStandardContract(symbol);
                memoizedMap[dt] = res;

                return res;
            };

            var filtered = _allSymbols.Where(x =>
            {
                switch (_type)
                {
                    case Type.Weeklys:
                        return !memoizedIsStandardType(x);
                    case Type.Standard:
                        return memoizedIsStandardType(x);
                    case Type.Standard | Type.Weeklys:
                        return true;
                    default:
                        return false;
                }
            });

            _allSymbols = filtered.ToList();
            return this;
        }

        /// <summary>
        /// Returns front month contract
        /// </summary>
        /// <returns></returns>
        public OptionFilterUniverse FrontMonth()
        {
            if (!_allSymbols.Any()) return this;
            var ordered = this.OrderBy(x => x.ID.Date).ToList();
            var frontMonth = ordered.TakeWhile(x => ordered[0].ID.Date == x.ID.Date);

            _allSymbols = frontMonth.ToList();
            return this;
        }

        /// <summary>
        /// Returns a list of back month contracts
        /// </summary>
        /// <returns></returns>
        public OptionFilterUniverse BackMonths()
        {
            if (!_allSymbols.Any()) return this;
            var ordered = this.OrderBy(x => x.ID.Date).ToList();
            var backMonths = ordered.SkipWhile(x => ordered[0].ID.Date == x.ID.Date);

            _allSymbols = backMonths.ToList();
            return this;
        }

        /// <summary>
        /// Returns first of back month contracts
        /// </summary>
        /// <returns></returns>
        public OptionFilterUniverse BackMonth()
        {
            return BackMonths().FrontMonth();
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

            if (_underlying.Time.Date != _uniqueStrikesResolveDate)
            {
                // each day we need to recompute the unique strikes list
                _uniqueStrikes = _allSymbols
                    .DistinctBy(x => x.ID.StrikePrice)
                    .OrderBy(x => x.ID.StrikePrice)
                    .ToList(symbol => symbol.ID.StrikePrice);

                _uniqueStrikesResolveDate = _underlying.Time.Date;
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
                .Where(symbol => symbol.ID.StrikePrice >= minPrice && symbol.ID.StrikePrice <= maxPrice)
                .ToList();

            return this;
        }

        /// <summary>
        /// Applies filter selecting options contracts based on a range of expiration dates relative to the current day
        /// </summary>
        /// <param name="minExpiry">The minimum time until expiry to include, for example, TimeSpan.FromDays(10)
        /// would exclude contracts expiring in less than 10 days</param>
        /// <param name="maxExpiry">The maxmium time until expiry to include, for example, TimeSpan.FromDays(10)
        /// would exclude contracts expiring in more than 10 days</param>
        /// <returns></returns>
        public OptionFilterUniverse Expiration(TimeSpan minExpiry, TimeSpan maxExpiry)
        {
            if (_underlying == null)
            {
                return this;
            }

            if (maxExpiry > Time.MaxTimeSpan) maxExpiry = Time.MaxTimeSpan;

            var minExpiryToDate = _underlying.Time.Date + minExpiry;
            var maxExpiryToDate = _underlying.Time.Date + maxExpiry;

            _allSymbols = _allSymbols
                .Where(symbol => symbol.ID.Date >= minExpiryToDate && symbol.ID.Date <= maxExpiryToDate)
                .ToList();

            return this;
        }

        /// <summary>
        /// Explicitly sets the selected contract symbols for this universe.
        /// This overrides and and all other methods of selecting symbols assuming it is called last.
        /// </summary>
        /// <param name="contracts">The option contract symbol objects to select</param>
        public OptionFilterUniverse Contracts(IEnumerable<Symbol> contracts)
        {
            _allSymbols = contracts.ToList();
            return this;
        }

        /// <summary>
        /// Sets a function used to filter the set of available contract filters. The input to the 'contractSelector'
        /// function will be the already filtered list if any other filters have already been applied.
        /// </summary>
        /// <param name="contractSelector">The option contract symbol objects to select</param>
        public OptionFilterUniverse Contracts(Func<IEnumerable<Symbol>, IEnumerable<Symbol>> contractSelector)
        {
            // force materialization using ToList
            _allSymbols = contractSelector(_allSymbols).ToList();
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

        /// <summary>
        /// Instructs the engine to only filter options contracts on the first time step of each market day.
        /// </summary>
        public OptionFilterUniverse OnlyApplyFilterAtMarketOpen()
        {
            _isDynamic = false;
            return this;
        }

        /// <summary>
        /// IEnumerable interface method implementation
        /// </summary>
        public IEnumerator<Symbol> GetEnumerator()
        {
            return _allSymbols.GetEnumerator();
        }

        /// <summary>
        /// IEnumerable interface method implementation
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _allSymbols.GetEnumerator();
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
    }
}
