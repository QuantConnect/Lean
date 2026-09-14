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
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Securities.Future;
using QuantConnect.Util;

namespace QuantConnect.Securities
{
    /// <summary>
    /// Base future contracts filter, shared by the futures universe selection filter (<see cref="FutureFilterUniverse"/>)
    /// and the futures chain filters (<see cref="Data.Market.FuturesChain"/>) so both offer the same filters with the same semantics
    /// </summary>
    /// <typeparam name="TUniverse">The concrete filter universe type</typeparam>
    /// <typeparam name="TData">The future contract data type</typeparam>
    public abstract class BaseFutureFilterUniverse<TUniverse, TData> : ContractSecurityFilterUniverse<TUniverse, TData>, IFutureContractFilters<TUniverse>
        where TUniverse : BaseFutureFilterUniverse<TUniverse, TData>
        where TData : ISymbolProvider
    {
        /// <summary>
        /// Constructs BaseFutureFilterUniverse
        /// </summary>
        /// <param name="allData">All data for the future contracts</param>
        /// <param name="localTime">The current local time</param>
        protected BaseFutureFilterUniverse(IReadOnlyList<TData> allData, DateTime localTime)
            : base(allData, localTime)
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
        public TUniverse ExpirationCycle(IEnumerable<int> months)
        {
            var monthHashSet = months.ToHashSet();
            return Contracts(contracts => contracts.Where(x => monthHashSet.Contains(x.Symbol.ID.Date.Month)));
        }

        /// <summary>
        /// Selects the contracts whose contract month is any of the given months of the year, see <see cref="FutureExpirationCycles"/>.
        /// Like <see cref="ExpirationCycle"/> but by the contract month, the month the contract is named after, which for some products,
        /// e.g. crude oil, is the month after the expiration month, see <see cref="FuturesExpiryUtilityFunctions.GetFutureContractMonth"/>
        /// </summary>
        /// <param name="months">Months of the year to select contracts from</param>
        /// <returns>Universe with filter applied</returns>
        public TUniverse ContractMonths(IEnumerable<int> months)
        {
            var monthHashSet = months.ToHashSet();
            return Contracts(contracts => contracts.Where(x => monthHashSet.Contains(FuturesExpiryUtilityFunctions.GetFutureContractMonth(x.Symbol).Month)));
        }
    }

    /// <summary>
    /// Represents futures symbols universe used in filtering.
    /// </summary>
    public class FutureFilterUniverse : BaseFutureFilterUniverse<FutureFilterUniverse, FutureUniverse>
    {
        /// <summary>
        /// Constructs FutureFilterUniverse
        /// </summary>
        public FutureFilterUniverse(IReadOnlyList<FutureUniverse> allData, DateTime localTime)
            : base(allData, localTime)
        {
        }

        /// <summary>
        /// Creates a new instance of the data type for the given symbol
        /// </summary>
        /// <returns>A data instance for the given symbol, which is just the symbol itself</returns>
        protected override FutureUniverse CreateDataInstance(Symbol symbol)
        {
            return new FutureUniverse()
            {
                Symbol = symbol,
                Time = LocalTime
            };
        }

        /// <summary>
        /// Gets the open interest of the given contract
        /// </summary>
        protected override decimal GetOpenInterest(FutureUniverse contract) => contract.OpenInterest;

        /// <summary>
        /// Gets the volume of the given contract
        /// </summary>
        protected override decimal GetVolume(FutureUniverse contract) => contract.Volume;
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
        public static FutureFilterUniverse Where(this FutureFilterUniverse universe, Func<FutureUniverse, bool> predicate)
        {
            universe.Data = universe.Data.Where(predicate).ToList();
            return universe;
        }

        /// <summary>
        /// Maps universe
        /// </summary>
        /// <param name="universe">Universe to apply the filter too</param>
        /// <param name="mapFunc">Symbol function to determine which Symbols are filtered</param>
        /// <returns><see cref="FutureFilterUniverse"/> with filter applied</returns>
        public static FutureFilterUniverse Select(this FutureFilterUniverse universe, Func<FutureUniverse, Symbol> mapFunc)
        {
            universe.AllSymbols = universe.Data.Select(mapFunc).ToList();
            return universe;
        }

        /// <summary>
        /// Binds universe
        /// </summary>
        /// <param name="universe">Universe to apply the filter too</param>
        /// <param name="mapFunc">Symbols function to determine which Symbols are filtered</param>
        /// <returns><see cref="FutureFilterUniverse"/> with filter applied</returns>
        public static FutureFilterUniverse SelectMany(this FutureFilterUniverse universe, Func<FutureUniverse, IEnumerable<Symbol>> mapFunc)
        {
            universe.AllSymbols = universe.Data.SelectMany(mapFunc).ToList();
            return universe;
        }
    }
}
