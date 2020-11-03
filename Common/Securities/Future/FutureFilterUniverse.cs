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
            : base(allSymbols, underlying)
        {
        }

        /// <summary>
        /// Determine if the given Future contract symbol is standard
        /// </summary>
        /// <returns>True if contract is standard</returns>
        protected override bool IsStandard(Symbol symbol)
        {
            return FutureSymbol.IsStandard(symbol);
        }

        /// <summary>
        /// Applies filter selecting futures contracts based on expiration cycles. See <see cref="FutureExpirationCycles"/> for details
        /// </summary>
        /// <param name="months">Months to select contracts from</param>
        /// <returns>Universe with filter applied</returns>
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
        /// <param name="universe">Universe to apply the filter too</param>
        /// <param name="predicate">Bool function to determine which Symbol are filtered</param>
        /// <returns><see cref="FutureFilterUniverse"/> with filter applied</returns>
        public static FutureFilterUniverse Where(this FutureFilterUniverse universe, Func<Symbol, bool> predicate)
        {
            universe.AllSymbols = universe.AllSymbols.Where(predicate).ToList();
            universe.IsDynamicInternal = true;
            return universe;
        }

        /// <summary>
        /// Maps universe
        /// </summary>
        /// <param name="universe">Universe to apply the filter too</param>
        /// <param name="mapFunc">Symbol function to determine which Symbols are filtered</param>
        /// <returns><see cref="FutureFilterUniverse"/> with filter applied</returns>
        public static FutureFilterUniverse Select(this FutureFilterUniverse universe, Func<Symbol, Symbol> mapFunc)
        {
            universe.AllSymbols = universe.AllSymbols.Select(mapFunc).ToList();
            universe.IsDynamicInternal = true;
            return universe;
        }

        /// <summary>
        /// Binds universe
        /// </summary>
        /// <param name="universe">Universe to apply the filter too</param>
        /// <param name="mapFunc">Symbols function to determine which Symbols are filtered</param>
        /// <returns><see cref="FutureFilterUniverse"/> with filter applied</returns>
        public static FutureFilterUniverse SelectMany(this FutureFilterUniverse universe, Func<Symbol, IEnumerable<Symbol>> mapFunc)
        {
            universe.AllSymbols = universe.AllSymbols.SelectMany(mapFunc).ToList();
            universe.IsDynamicInternal = true;
            return universe;
        }
    }
}
