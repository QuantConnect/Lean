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
using QuantConnect.Securities.Future;

namespace QuantConnect.Securities.FutureOption
{
    /// <summary>
    /// Creates the underlying Symbol that corresponds to a futures options contract
    /// </summary>
    /// <remarks>
    /// Because there can exist futures options (FOP) contracts that have an underlying Future
    /// that does not have the same contract month as FOPs contract month, we need a way to resolve
    /// the underlying Symbol of the FOP to the specific future contract it belongs to.
    ///
    /// Luckily, these FOPs all follow a pattern as to how the underlying is determined. The
    /// method <see cref="GetUnderlyingFutureFromFutureOption"/> will automatically resolve the FOP contract's
    /// underlying Future, and will ensure that the rules of the underlying are being followed.
    ///
    /// An example of a contract that this happens to is Gold Futures (FUT=GC, FOP=OG). OG FOPs
    /// underlying Symbols are not determined by the contract month of the FOP itself, but rather
    /// by the closest contract to it in an even month.
    ///
    /// Examples:
    ///   OGH21 would have an underlying of GCJ21
    ///   OGJ21 would have an underlying of GCJ21
    ///   OGK21 would have an underlying of GCM21
    ///   OGM21 would have an underlying of GCM21...
    /// </remarks>
    public static class FuturesOptionsUnderlyingMapper
    {
        private static readonly Dictionary<string, Func<DateTime, DateTime?, DateTime?>> _underlyingFuturesOptionsRules = new Dictionary<string, Func<DateTime, DateTime?, DateTime?>>
        {
            // CBOT
            { "ZB", (d, ld) => ContractMonthSerialLookupRule(Symbol.Create("ZB", SecurityType.Future, Market.CBOT), d, ld.Value) },
            { "ZC", (d, ld) => ContractMonthSerialLookupRule(Symbol.Create("ZC", SecurityType.Future, Market.CBOT), d, ld.Value) },
            { "ZN", (d, ld) => ContractMonthSerialLookupRule(Symbol.Create("ZN", SecurityType.Future, Market.CBOT), d, ld.Value) },
            { "ZS", (d, ld) => ContractMonthSerialLookupRule(Symbol.Create("ZS", SecurityType.Future, Market.CBOT), d, ld.Value) },
            { "ZT", (d, ld) => ContractMonthSerialLookupRule(Symbol.Create("ZT", SecurityType.Future, Market.CBOT), d, ld.Value) },
            { "ZW", (d, ld) => ContractMonthSerialLookupRule(Symbol.Create("ZW", SecurityType.Future, Market.CBOT), d, ld.Value) },

            // COMEX
            { "HG", (d, _) => ContractMonthYearStartThreeMonthsThenEvenOddMonthsSkipRule(d, true) },
            { "SI", (d, _) => ContractMonthYearStartThreeMonthsThenEvenOddMonthsSkipRule(d, true) },
            { "GC", (d, _) => ContractMonthEvenOddMonth(d, false) }
        };

        /// <summary>
        /// The difference in months for the Futures expiry month minus the Futures Options expiry month. This assumes
        /// that the underlying Future follows a 1-1 mapping between the FOP and future, i.e. this will result in incorrect
        /// results, but is needed as an intermediate step to resolve the actual expiry.
        /// </summary>
        private static readonly IReadOnlyDictionary<string, int> _futuresOptionsExpiryDelta = new Dictionary<string, int>
        {
            { "ZB", 1 },
            { "ZC", 1 },
            { "ZN", 1 },
            { "ZS", 1 },
            { "ZT", 1 },
            { "ZW", 1 },
            { "HG", 1 },
            { "GC", 1 },
            { "SI", 1 }
        };

        /// <summary>
        /// Gets the FOP's underlying Future. The underlying Future's contract month might not match
        /// the contract month of the Future Option when providing CBOT or COMEX based FOPs contracts to this method.
        /// </summary>
        /// <param name="futureOptionTicker">Future option ticker</param>
        /// <param name="market">Market of the Future Option</param>
        /// <param name="futureOptionExpiration">Expiration date of the future option</param>
        /// <param name="date">Date to search the future chain provider with. Optional, but required for CBOT based contracts</param>
        /// <returns>Symbol if there is an underlying for the FOP, null if there's no underlying found for the Future Option</returns>
        public static Symbol GetUnderlyingFutureFromFutureOption(string futureOptionTicker, string market, DateTime futureOptionExpiration, DateTime? date = null)
        {
            var futureTicker = FuturesOptionsSymbolMappings.MapFromOption(futureOptionTicker);
            var canonicalFuture = Symbol.Create(futureTicker, SecurityType.Future, market);
            // Get the contract month of the FOP to use when searching for the underlying.
            // If the FOP and Future share the same contract month, this is reused as the future's
            // contract month so that we can resolve the Future's expiry.
            var contractMonth = GetFutureContractMonthNoRulesApplied(canonicalFuture, futureOptionExpiration);

            if (_underlyingFuturesOptionsRules.ContainsKey(futureTicker))
            {
                // The provided ticker follows some sort of rule. Let's figure out the underlying's contract month.
                var newFutureContractMonth = _underlyingFuturesOptionsRules[futureTicker](contractMonth, date);
                if (newFutureContractMonth == null)
                {
                    // This will only happen when we search the Futures chain for a given contract and no
                    // closest match could be made, i.e. there are no futures in the chain that come after the FOP's
                    // contract month.
                    return null;
                }

                contractMonth = newFutureContractMonth.Value;
            }

            var futureExpiry = FuturesExpiryFunctions.FuturesExpiryFunction(canonicalFuture)(contractMonth);
            return Symbol.CreateFuture(futureTicker, market, futureExpiry);
        }

        /// <summary>
        /// Searches the futures chain for the next matching futures contract, and resolves the underlying
        /// as the closest future we can find during or after the contract month.
        /// </summary>
        /// <param name="canonicalFutureSymbol">Canonical future Symbol</param>
        /// <param name="futureOptionContractMonth">Future option contract month. Note that this is not the expiry of the Future Option.</param>
        /// <param name="lookupDate">The date that we'll be using to look at the Future chain</param>
        /// <returns>The underlying future's contract month, or null if no closest contract was found</returns>
        private static DateTime? ContractMonthSerialLookupRule(Symbol canonicalFutureSymbol, DateTime futureOptionContractMonth, DateTime lookupDate)
        {
            var futureChain = FuturesListings.ListedContracts(canonicalFutureSymbol.ID.Symbol, lookupDate);
            if (futureChain == null)
            {
                // No matching contract listing rules entry was found
                return null;
            }

            foreach (var future in futureChain.OrderBy(s => s.ID.Date))
            {
                // Normalize by date first, normalize to a contract month date, then we want to get the contract
                // month of the Future contract so we normalize by getting the delta between the expiration
                // and the contract month.
                var futureContractMonth = future.ID.Date.Date
                    .AddDays(-future.ID.Date.Day + 1)
                    .AddMonths(FuturesExpiryUtilityFunctions.GetDeltaBetweenContractMonthAndContractExpiry(future.ID.Symbol, future.ID.Date));

                // We want a contract that is either the same as the contract month or greater
                if (futureContractMonth < futureOptionContractMonth)
                {
                    continue;
                }

                return futureContractMonth;
            }

            // No matching/closest contract was found in the futures chain.
            return null;
        }

        /// <summary>
        /// Searches for the closest future's contract month depending on whether the Future Option's contract month is
        /// on an even or odd month.
        /// </summary>
        /// <param name="futureOptionContractMonth">Future option contract month. Note that this is not the expiry of the Future Option.</param>
        /// <param name="oddMonths">True if the Future Option's underlying future contract month is on odd months, false if on even months</param>
        /// <returns>The underlying Future's contract month</returns>
        private static DateTime ContractMonthEvenOddMonth(DateTime futureOptionContractMonth, bool oddMonths)
        {
            var monthEven = futureOptionContractMonth.Month % 2 == 0;
            if (oddMonths && monthEven)
            {
                return futureOptionContractMonth.AddMonths(1);
            }
            if (!oddMonths && !monthEven)
            {
                return futureOptionContractMonth.AddMonths(1);
            }

            return futureOptionContractMonth;
        }

        /// <summary>
        /// Sets the contract month to the third month for the first 3 months, then begins using the <see cref="ContractMonthEvenOddMonth"/> rule.
        /// </summary>
        /// <param name="futureOptionContractMonth">Future option contract month. Note that this is not the expiry of the Future Option.</param>
        /// <param name="oddMonths">True if the Future Option's underlying future contract month is on odd months, false if on even months. Only used for months greater than 3 months</param>
        /// <returns></returns>
        private static DateTime ContractMonthYearStartThreeMonthsThenEvenOddMonthsSkipRule(DateTime futureOptionContractMonth, bool oddMonths)
        {
            if (futureOptionContractMonth.Month <= 3)
            {
                return new DateTime(futureOptionContractMonth.Year, 3, 1);
            }

            return ContractMonthEvenOddMonth(futureOptionContractMonth, oddMonths);
        }

        /// <summary>
        /// Gets the theoretical (i.e. intermediate/naive) future contract month if we assumed a 1-1 mapping
        /// between FOPs contract months and Futures contract months, i.e. they share the same contract month.
        /// </summary>
        /// <param name="canonicalFutureSymbol">Canonical future Symbol</param>
        /// <param name="futureOptionExpirationDate">Future Option Expiration Date</param>
        /// <returns>Contract month assuming that the Future Option and Future share the same contract month</returns>
        public static DateTime GetFutureContractMonthNoRulesApplied(Symbol canonicalFutureSymbol, DateTime futureOptionExpirationDate)
        {
            var baseOptionExpiryMonthDate = new DateTime(futureOptionExpirationDate.Year, futureOptionExpirationDate.Month, 1);
            if (!_futuresOptionsExpiryDelta.ContainsKey(canonicalFutureSymbol.ID.Symbol))
            {
                // For contracts like CL, they have no expiry delta between the Futures and FOPs, so we hit this path.
                // However, it does have a delta between its expiry and contract month, which we adjust here before
                // claiming that `baseOptionExpiryMonthDate` is the future's contract month.
                var futuresExpiry = FuturesExpiryFunctions.FuturesExpiryFunction(canonicalFutureSymbol)(baseOptionExpiryMonthDate);
                var futuresDelta = FuturesExpiryUtilityFunctions.GetDeltaBetweenContractMonthAndContractExpiry(canonicalFutureSymbol.ID.Symbol, futuresExpiry);

                return baseOptionExpiryMonthDate.AddMonths(futuresDelta);
            }

            return baseOptionExpiryMonthDate.AddMonths(_futuresOptionsExpiryDelta[canonicalFutureSymbol.ID.Symbol]);
        }
    }
}
