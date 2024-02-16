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

namespace QuantConnect.Securities.Future
{
    /// <summary>
    /// Helpers for getting the futures contracts that are trading on a given date.
    /// This is a substitute for the BacktestingFutureChainProvider, but
    /// does not outright replace it because of missing entries. This will resolve
    /// the listed contracts without having any data in place. We follow the listing rules
    /// set forth by the exchange to get the <see cref="Symbol"/>s that are listed at a given date.
    /// </summary>
    public static class FuturesListings
    {
        private static readonly Symbol _zb = Symbol.Create("ZB", SecurityType.Future, Market.CBOT);
        private static readonly Symbol _zc = Symbol.Create("ZC", SecurityType.Future, Market.CBOT);
        private static readonly Symbol _zs = Symbol.Create("ZS", SecurityType.Future, Market.CBOT);
        private static readonly Symbol _zt = Symbol.Create("ZT", SecurityType.Future, Market.CBOT);
        private static readonly Symbol _zw = Symbol.Create("ZW", SecurityType.Future, Market.CBOT);
        private static readonly Symbol _zn = Symbol.Create("ZN", SecurityType.Future, Market.CBOT);

        private static Dictionary<string, Func<DateTime, List<Symbol>>> _futuresListingRules = new Dictionary<string, Func<DateTime, List<Symbol>>>
        {
            { "ZB", t => QuarterlyContracts(_zb, t, 3) },
            { "ZC", t => MonthlyContractListings(
                _zc,
                t,
                12,
                new FuturesListingCycles(new[] { 3, 5, 9 }, 9),
                new FuturesListingCycles(new[] { 7, 12 }, 8)) },
            { "ZN", t => QuarterlyContracts(_zt, t, 3) },
            { "ZS", t => MonthlyContractListings(
                _zs,
                t,
                11,
                new FuturesListingCycles(new[] { 1, 3, 5, 8, 9 }, 15),
                new FuturesListingCycles(new[] { 7, 11 }, 8)) },
            { "ZT", t => QuarterlyContracts(_zt, t, 3) },
            { "ZW", t => MonthlyContractListings(
                _zw,
                t,
                7,
                new FuturesListingCycles(new[] { 3, 5, 7, 9, 12 }, 15)) }
        };

        /// <summary>
        /// Gets the listed futures contracts on a given date
        /// </summary>
        /// <param name="futureTicker">Ticker of the future contract</param>
        /// <param name="time">Contracts to look up that are listed at that time</param>
        /// <returns>The currently trading contracts on the exchange</returns>
        public static List<Symbol> ListedContracts(string futureTicker, DateTime time)
        {
            if (!_futuresListingRules.ContainsKey(futureTicker))
            {
                // No entries found. This differs from entries being returned as an empty array, where
                // that would mean that no listings were found.
                return null;
            }

            return _futuresListingRules[futureTicker](time);
        }

        /// <summary>
        /// Gets contracts following a quarterly listing procedure, with a limit of
        /// how many contracts are listed at once.
        /// </summary>
        /// <param name="canonicalFuture">Canonical Futures Symbol</param>
        /// <param name="time">Contracts to look up that are listed at that time</param>
        /// <param name="limit">Number of Symbols we get back/are listed at a given time</param>
        /// <returns>Symbols that are listed at the given time</returns>
        private static List<Symbol> QuarterlyContracts(Symbol canonicalFuture, DateTime time, int limit)
        {
            var contractMonth = new DateTime(time.Year, time.Month, 1);
            var futureExpiry = DateTime.MinValue;
            var expiryFunc = FuturesExpiryFunctions.FuturesExpiryFunction(canonicalFuture);

            // Skip any contracts that have already expired.
            while (futureExpiry < time)
            {
                futureExpiry = FuturesExpiryFunctions.FuturesExpiryFunction(canonicalFuture)(contractMonth);
                contractMonth = contractMonth.AddMonths(1);
            }

            // Negate the last incrementation from the while loop to get the actual contract month of the future.
            var firstFutureContractMonth = contractMonth.AddMonths(-1);

            var quarterlyContracts = new List<Symbol>();
            // Gets the next closest month from the current month in multiples of 3
            var quarterlyContractMonth = (int)Math.Ceiling((double)firstFutureContractMonth.Month / 3) * 3;

            for (var i = 0; i < limit; i++)
            {
                // We're past the expiration frontier due to the while loop above, which means
                // that any contracts from here on out will be greater than the current time.
                var currentContractMonth = firstFutureContractMonth.AddMonths(-firstFutureContractMonth.Month + quarterlyContractMonth);
                var currentFutureExpiry = expiryFunc(currentContractMonth);

                quarterlyContracts.Add(Symbol.CreateFuture(canonicalFuture.ID.Symbol, canonicalFuture.ID.Market, currentFutureExpiry));
                quarterlyContractMonth += 3;
            }

            return quarterlyContracts;
        }

        /// <summary>
        /// Gets Futures contracts that follow a limited cyclical pattern
        /// </summary>
        /// <param name="canonicalFuture">Canonical Futures Symbol</param>
        /// <param name="time">Contracts to look up that are listed at that time</param>
        /// <param name="contractMonthForNewListings">Contract month that results in new listings after this contract's expiry</param>
        /// <param name="futureListingCycles">
        /// Cycles that define the number of contracts and the months the contracts are listed on, including
        /// the limit of how many contracts will be listed.
        /// </param>
        /// <returns>Symbols that are listed at the given time</returns>
        private static List<Symbol> MonthlyContractListings(
            Symbol canonicalFuture,
            DateTime time,
            int contractMonthForNewListings,
            params FuturesListingCycles[] futureListingCycles)
        {
            var listings = new List<Symbol>();
            var expiryFunc = FuturesExpiryFunctions.FuturesExpiryFunction(canonicalFuture);
            var yearDelta = 0;

            var contractMonthForNewListingCycle = new DateTime(time.Year, contractMonthForNewListings, 1);
            var contractMonthForNewListingCycleExpiry = expiryFunc(contractMonthForNewListingCycle);

            if (time <= contractMonthForNewListingCycleExpiry)
            {
                // Go back a year if we haven't yet crossed this year's contract renewal expiration date.
                contractMonthForNewListingCycleExpiry = expiryFunc(contractMonthForNewListingCycle.AddYears(-1));
                yearDelta = -1;
            }

            foreach (var listingCycle in futureListingCycles)
            {
                var year = yearDelta;
                var count = 0;
                var initialListings = true;

                while (count != listingCycle.Limit)
                {
                    var monthStartIndex = 0;
                    if (initialListings)
                    {
                        // For the initial listing, we want to start counting at some month that might not be the first
                        // index of the collection. The index is discovered here and used as the starting point for listed contracts.
                        monthStartIndex = listingCycle.Cycle.Length - listingCycle.Cycle.Count(c => c > contractMonthForNewListingCycleExpiry.Month);
                        initialListings = false;
                    }

                    for (var m = monthStartIndex; m < listingCycle.Cycle.Length; m++)
                    {
                        // Add the future's expiration to the listings
                        var currentContractMonth = new DateTime(time.Year + year, listingCycle.Cycle[m], 1);
                        var currentFutureExpiry = expiryFunc(currentContractMonth);
                        if (currentFutureExpiry >= time)
                        {
                            listings.Add(Symbol.CreateFuture(canonicalFuture.ID.Symbol, canonicalFuture.ID.Market, currentFutureExpiry));
                        }

                        if (++count == listingCycle.Limit)
                        {
                            break;
                        }
                    }

                    year++;
                }
            }

            return listings;
        }

        /// <summary>
        /// Listing Cycles, i.e. the months and number of contracts that are renewed whenever
        /// the specified renewal expiration contract expires.
        /// </summary>
        /// <remarks>
        /// Example:
        ///
        ///   (from: https://www.cmegroup.com/trading/agricultural/grain-and-oilseed/wheat_contract_specifications.html)
        ///   "15 monthly contracts of Mar, May, Jul, Sep, Dec listed annually following the termination of trading in the July contract of the current year."
        ///
        /// This would equate to a cycle of [3, 5, 7, 9, 12], a limit of 15, and the contract month == 7.
        /// </remarks>
        private class FuturesListingCycles
        {
            /// <summary>
            /// Monthly cycles that the futures listings rule follows
            /// </summary>
            public int[] Cycle { get; }

            /// <summary>
            /// Max number of contracts returned by this rule
            /// </summary>
            public int Limit { get; }


            /// <summary>
            /// Creates a listing cycle rule
            /// </summary>
            /// <param name="cycle">New contract listing cycles</param>
            /// <param name="limit">Max number of contracts to return in this rule</param>
            public FuturesListingCycles(int[] cycle, int limit)
            {
                Cycle = cycle;
                Limit = limit;
            }
        }
    }
}
