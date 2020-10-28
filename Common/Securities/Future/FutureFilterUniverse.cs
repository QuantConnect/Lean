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
using QuantConnect.Securities.Future;
using QuantConnect.Util;

namespace QuantConnect.Securities
{
    /// <summary>
    /// Represents futures symbols universe used in filtering.
    /// </summary>
    public class FutureFilterUniverse : ContractSecurityFilterUniverse<FutureFilterUniverse>
    {
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
        /// Determine if the given Future contract symbol is standard
        /// </summary>
        /// <returns></returns>
        protected override bool IsStandard(Symbol symbol)
        {
            return FutureSymbol.IsStandard(symbol);
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
