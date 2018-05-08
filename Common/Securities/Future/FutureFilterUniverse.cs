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

namespace QuantConnect.Securities
{
    /// <summary>
    /// Represents futures symbols universe used in filtering.
    /// </summary>
    public class FutureFilterUniverse : IDerivativeSecurityFilterUniverse
    {
        internal IEnumerable<Symbol> _allSymbols;

        /// <summary>
        /// The underlying price data
        /// </summary>
        public BaseData Underlying
        {
            get { return _underlying; }
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

        /// <summary>
        /// Constructs FutureFilterUniverse
        /// </summary>
        public FutureFilterUniverse(IEnumerable<Symbol> allSymbols, BaseData underlying)
        {
            _allSymbols = allSymbols;
            _underlying = underlying;
            _isDynamic = false;
        }

        /// <summary>
        /// Returns front month contract
        /// </summary>
        /// <returns></returns>
        public FutureFilterUniverse FrontMonth()
        {
            if (_allSymbols.Count() == 0) return this;
            var ordered = this.OrderBy(x => x.ID.Date).ToList();
            var frontMonth = ordered.TakeWhile(x => ordered[0].ID.Date == x.ID.Date);

            _allSymbols = frontMonth.ToList();
            return this;
        }

        /// <summary>
        /// Returns a list of back month contracts
        /// </summary>
        /// <returns></returns>
        public FutureFilterUniverse BackMonths()
        {
            if (_allSymbols.Count() == 0) return this;
            var ordered = this.OrderBy(x => x.ID.Date).ToList();
            var backMonths = ordered.SkipWhile(x => ordered[0].ID.Date == x.ID.Date);

            _allSymbols = backMonths.ToList();
            return this;
        }

        /// <summary>
        /// Returns first of back month contracts
        /// </summary>
        /// <returns></returns>
        public FutureFilterUniverse BackMonth()
        {
            return BackMonths().FrontMonth();
        }

        /// <summary>
        /// Applies filter selecting futures contracts based on a range of expiration dates relative to the current day
        /// </summary>
        /// <param name="minExpiry">The minimum time until expiry to include, for example, TimeSpan.FromDays(10)
        /// would exclude contracts expiring in less than 10 days</param>
        /// <param name="maxExpiry">The maxmium time until expiry to include, for example, TimeSpan.FromDays(10)
        /// would exclude contracts expiring in more than 10 days</param>
        /// <returns></returns>
        public FutureFilterUniverse Expiration(TimeSpan minExpiry, TimeSpan maxExpiry)
        {
            if (_underlying == null)
            {
                return this;
            }

            if (maxExpiry > Time.MaxTimeSpan) maxExpiry = Time.MaxTimeSpan;

            var minExpiryToDate = _underlying.Time.Date + minExpiry;
            var maxExpiryToDate = _underlying.Time.Date + maxExpiry;

            var filtered =
                   from symbol in _allSymbols
                   let contract = symbol.ID
                   where contract.Date >= minExpiryToDate
                      && contract.Date <= maxExpiryToDate
                   select symbol;

            _allSymbols = filtered.ToList();
            return this;
        }

        /// <summary>
        /// Applies filter selecting futures contracts based on expiration cycles. See <see cref="FutureExpirationCycles"/> for details
        /// </summary>
        /// <param name="months"></param>
        /// <returns></returns>
        public FutureFilterUniverse ExpirationCycle(int[] months)
        {
            var monthHashSet = months.ToHashSet();
            return this.Where(x => monthHashSet.Contains(x.ID.Date.Month));
        }

        /// <summary>
        /// Explicitly sets the selected contract symbols for this universe.
        /// This overrides and and all other methods of selecting symbols assuming it is called last.
        /// </summary>
        /// <param name="contracts">The future contract symbol objects to select</param>
        public FutureFilterUniverse Contracts(IEnumerable<Symbol> contracts)
        {
            _allSymbols = contracts.ToList();
            return this;
        }

        /// <summary>
        /// Sets a function used to filter the set of available contract filters. The input to the 'contractSelector'
        /// function will be the already filtered list if any other filters have already been applied.
        /// </summary>
        /// <param name="contractSelector">The future contract symbol objects to select</param>
        public FutureFilterUniverse Contracts(Func<IEnumerable<Symbol>, IEnumerable<Symbol>> contractSelector)
        {
            // force materialization using ToList
            _allSymbols = contractSelector(_allSymbols).ToList();
            return this;
        }

        /// <summary>
        /// Instructs the engine to only filter options contracts on the first time step of each market day.
        /// </summary>
        public FutureFilterUniverse OnlyApplyFilterAtMarketOpen()
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
    public static class FutureFilterUniverseEx
    {
        /// <summary>
        /// Filters universe
        /// </summary>
        /// <param name="universe"></param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public static FutureFilterUniverse Where(this FutureFilterUniverse universe, Func<Symbol, bool> predicate)
        {
            universe._allSymbols = universe._allSymbols.Where(predicate).ToList();
            universe._isDynamic = true;
            return universe;
        }

        /// <summary>
        /// Maps universe
        /// </summary>
        public static FutureFilterUniverse Select(this FutureFilterUniverse universe, Func<Symbol, Symbol> mapFunc)
        {
            universe._allSymbols = universe._allSymbols.Select(mapFunc).ToList();
            universe._isDynamic = true;
            return universe;
        }

        /// <summary>
        /// Binds universe
        /// </summary>
        public static FutureFilterUniverse SelectMany(this FutureFilterUniverse universe, Func<Symbol, IEnumerable<Symbol>> mapFunc)
        {
            universe._allSymbols = universe._allSymbols.SelectMany(mapFunc).ToList();
            universe._isDynamic = true;
            return universe;
        }
    }
}
