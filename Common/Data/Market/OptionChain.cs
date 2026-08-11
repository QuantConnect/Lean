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
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Python;
using QuantConnect.Securities;

namespace QuantConnect.Data.Market
{
    /// <summary>
    /// Represents an entire chain of option contracts for a single underlying security.
    /// This type is <see cref="IEnumerable{OptionContract}"/>
    /// </summary>
    public class OptionChain : BaseChain<OptionContract, OptionContracts>
    {
        /// <summary>
        /// Gets all call contracts in the chain, sorted by expiration and strike
        /// </summary>
        [PandasIgnore]
        public List<OptionContract> Calls => GetContracts(OptionRight.Call);

        /// <summary>
        /// Gets all put contracts in the chain, sorted by expiration and strike
        /// </summary>
        [PandasIgnore]
        public List<OptionContract> Puts => GetContracts(OptionRight.Put);

        /// <summary>
        /// Gets the distinct strike prices in the chain, sorted in ascending order.
        /// See <see cref="StrikeList.ClosestTo"/>, <see cref="StrikeList.FirstAbove"/> and <see cref="StrikeList.FirstBelow"/>
        /// </summary>
        [PandasIgnore]
        public StrikeList Strikes => new(Contracts.Values.Select(contract => contract.Strike));

        /// <summary>
        /// Initializes a new instance of the <see cref="OptionChain"/> class
        /// </summary>
        /// <param name="canonicalOptionSymbol">The symbol for this chain.</param>
        /// <param name="time">The time of this chain</param>
        /// <param name="flatten">Whether to flatten the data frame</param>
        public OptionChain(Symbol canonicalOptionSymbol, DateTime time, bool flatten = true)
            : base(canonicalOptionSymbol, time, MarketDataType.OptionChain, flatten)
        {
        }

        /// <summary>
        /// Initializes a new option chain for a list of contracts as <see cref="OptionUniverse"/> instances
        /// </summary>
        /// <param name="canonicalOptionSymbol">The canonical option symbol</param>
        /// <param name="time">The time of this chain</param>
        /// <param name="contracts">The list of contracts data</param>
        /// <param name="symbolProperties">The option symbol properties</param>
        /// <param name="flatten">Whether to flatten the data frame</param>
        public OptionChain(Symbol canonicalOptionSymbol, DateTime time, IEnumerable<OptionUniverse> contracts, SymbolProperties symbolProperties,
            bool flatten = true)
            : this(canonicalOptionSymbol, time, flatten)
        {
            var underlyingSet = false;
            foreach (var contractData in contracts)
            {
                if (!underlyingSet && contractData.Underlying != null)
                {
                    // The base constructor initializes Underlying to an empty QuoteBar,
                    // so a "??=" here would never assign and the chain would report a zero underlying price
                    Underlying = contractData.Underlying;
                    underlyingSet = true;
                }
                if (contractData.Symbol.ID.Date.Date < time.Date) continue;
                Contracts[contractData.Symbol] = OptionContract.Create(contractData, symbolProperties);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OptionChain"/> class as a clone of the specified instance
        /// </summary>
        private OptionChain(OptionChain other)
            : base(other)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OptionChain"/> class as a copy of the specified chain,
        /// but containing only the given subset of its contracts
        /// </summary>
        private OptionChain(OptionChain other, IEnumerable<OptionContract> contracts)
            : base(other, contracts)
        {
        }

        /// <summary>
        /// Return a new instance clone of this object, used in fill forward
        /// </summary>
        /// <returns>A clone of the current object</returns>
        public override BaseData Clone()
        {
            return new OptionChain(this);
        }

        /// <summary>
        /// Selects the single contract that best matches the given criteria, replacing the usual
        /// sorted-comprehension ceremony with a single call, e.g.
        /// <c>chain.select(right=OptionRight.PUT, target_dte=30, moneyness=-0.15)</c>.
        /// Null-safe: returns null (None in Python) instead of throwing when the chain is empty,
        /// no expiration falls within the requested window or the underlying price is unavailable.
        /// </summary>
        /// <param name="right">If set, only contracts of this right are considered</param>
        /// <param name="targetDte">If set, only contracts of the expiration closest to this many days from the
        /// chain's current date are considered. See <see cref="ClosestExpiry"/></param>
        /// <param name="minDte">If set, expirations closer than this many days are excluded</param>
        /// <param name="maxDte">If set, expirations further than this many days are excluded</param>
        /// <param name="moneyness">Signed distance from the underlying price as a fraction of it, regardless of right:
        /// negative values target strikes below the underlying price, positive values above.
        /// e.g. -0.15 targets the strike closest to 85% of the underlying price.
        /// When neither moneyness nor targetDelta are set, the at-the-money contract (moneyness 0) is selected.
        /// Mutually exclusive with <paramref name="targetDelta"/></param>
        /// <param name="targetDelta">If set, the contract whose absolute delta is closest to the absolute value of this
        /// target is selected, so a "30 delta put" can be requested as either 0.3 or -0.3.
        /// Contracts without greeks data are ignored. Mutually exclusive with <paramref name="moneyness"/></param>
        /// <returns>The best matching contract, or null if no contract matches</returns>
        public OptionContract Select(OptionRight? right = null, int? targetDte = null, int? minDte = null, int? maxDte = null,
            decimal? moneyness = null, decimal? targetDelta = null)
        {
            if (moneyness.HasValue && targetDelta.HasValue)
            {
                throw new ArgumentException("OptionChain.Select(): moneyness and targetDelta are mutually exclusive, please set only one of them.");
            }

            IEnumerable<OptionContract> candidates = right.HasValue
                ? Contracts.Values.Where(contract => contract.Right == right.Value).ToList()
                : Contracts.Values;

            if (targetDte.HasValue || minDte.HasValue || maxDte.HasValue)
            {
                var expiry = GetClosestExpiry(candidates, targetDte, minDte, maxDte);
                if (!expiry.HasValue)
                {
                    return null;
                }
                candidates = candidates.Where(contract => contract.Expiry == expiry.Value).ToList();
            }

            if (targetDelta.HasValue)
            {
                var target = Math.Abs(targetDelta.Value);
                // Contracts without greeks data report a flat zero delta: exclude them so a chain without
                // greeks returns null instead of silently picking an arbitrary contract
                return candidates
                    .Where(contract => contract.Greeks.Delta != 0)
                    .OrderBy(contract => Math.Abs(Math.Abs(contract.Greeks.Delta) - target))
                    .ThenBy(contract => contract.Strike)
                    .ThenBy(contract => contract.Right)
                    .FirstOrDefault();
            }

            var underlyingPrice = GetUnderlyingPrice();
            if (!underlyingPrice.HasValue)
            {
                return null;
            }

            var targetStrike = underlyingPrice.Value * (1 + (moneyness ?? 0));
            return GetClosestByStrike(candidates, targetStrike);
        }

        /// <summary>
        /// Gets the expiration date in the chain closest to the target number of days from the chain's current date.
        /// Null-safe: returns null (None in Python) when the chain is empty or no expiration falls within the requested window.
        /// </summary>
        /// <param name="targetDte">The target days to expiration. When two expirations are equidistant the earlier one is returned.
        /// Defaults to minDte if set, else 0 (the nearest expiration)</param>
        /// <param name="minDte">If set, expirations closer than this many days are excluded</param>
        /// <param name="maxDte">If set, expirations further than this many days are excluded</param>
        /// <returns>The best matching expiration date as stored in the chain's contracts, or null if none matches</returns>
        /// <remarks>Days to expiration are measured on the contract's last trading date: pre-2015 equity option metadata
        /// uses the OCC Saturday expiration convention, which is counted as the preceding Friday</remarks>
        public DateTime? ClosestExpiry(int? targetDte = null, int? minDte = null, int? maxDte = null)
        {
            return GetClosestExpiry(Contracts.Values, targetDte, minDte, maxDte);
        }

        /// <summary>
        /// Gets a new chain containing only the contracts with the given expiration date, so contracts
        /// for a single expiration can be selected with <c>chain.at(expiry).calls</c> or <c>chain.at(expiry).puts</c>.
        /// Matching is date-tolerant: pre-2015 equity option metadata uses the OCC Saturday expiration convention,
        /// so a chain whose contracts expire e.g. Saturday 2012-02-18 is also matched by the last trading
        /// date, Friday 2012-02-17, which would otherwise silently match zero contracts.
        /// </summary>
        /// <param name="expiry">The expiration date, time of day is ignored</param>
        /// <returns>A new chain with only the matching contracts, empty if none matches</returns>
        public OptionChain At(DateTime expiry)
        {
            var expiryDate = NormalizeExpiry(expiry);
            return new OptionChain(this, Contracts.Values.Where(contract => NormalizeExpiry(contract.Expiry) == expiryDate));
        }

        /// <summary>
        /// Gets the contract of the given right whose strike is closest to the current underlying price.
        /// When two strikes are equidistant the lower one is returned.
        /// Null-safe: returns null (None in Python) when the chain has no contracts of the given right
        /// or the underlying price is unavailable.
        /// </summary>
        /// <param name="right">The contract right to search for</param>
        /// <returns>The at-the-money contract, or null if there is none</returns>
        public OptionContract AtTheMoney(OptionRight right)
        {
            var underlyingPrice = GetUnderlyingPrice();
            if (!underlyingPrice.HasValue)
            {
                return null;
            }
            return GetClosestByStrike(Contracts.Values.Where(contract => contract.Right == right), underlyingPrice.Value);
        }

        private List<OptionContract> GetContracts(OptionRight right)
        {
            return Contracts.Values
                .Where(contract => contract.Right == right)
                .OrderBy(contract => contract.Expiry)
                .ThenBy(contract => contract.Strike)
                .ToList();
        }

        /// <summary>
        /// Gets the underlying price for moneyness calculations. Chains built from universe data
        /// might not have the chain-level underlying data populated, but their contracts carry it.
        /// Returns null when unavailable so selection helpers can be null-safe instead of
        /// silently treating the underlying price as zero.
        /// </summary>
        private decimal? GetUnderlyingPrice()
        {
            var price = Underlying?.Price ?? decimal.Zero;
            if (price == decimal.Zero)
            {
                price = Contracts.Values.Select(contract => contract.UnderlyingLastPrice).FirstOrDefault(x => x != decimal.Zero);
            }
            return price == decimal.Zero ? null : price;
        }

        /// <summary>
        /// Normalizes an expiration date to the contract's last trading date for comparisons:
        /// equity option metadata prior to February 2015 uses the OCC Saturday expiration convention,
        /// while the contract actually stops trading the preceding Friday.
        /// </summary>
        private static DateTime NormalizeExpiry(DateTime expiry)
        {
            var date = expiry.Date;
            return date.DayOfWeek == DayOfWeek.Saturday ? date.AddDays(-1) : date;
        }

        private DateTime? GetClosestExpiry(IEnumerable<OptionContract> contracts, int? targetDte, int? minDte, int? maxDte)
        {
            var target = targetDte ?? minDte ?? 0;
            DateTime? result = null;
            var resultDistance = int.MaxValue;
            foreach (var expiry in contracts.Select(contract => contract.Expiry).Distinct())
            {
                // Days to expiration measured against the chain's own date, so results are not
                // affected by the time zone difference between the algorithm and the exchange
                var dte = (NormalizeExpiry(expiry) - EndTime.Date).Days;
                // Lifted comparisons are false when the bound is null, i.e. unset bounds don't exclude anything
                if (dte < minDte || dte > maxDte)
                {
                    continue;
                }
                var distance = Math.Abs(dte - target);
                if (distance < resultDistance || (distance == resultDistance && expiry < result.Value))
                {
                    result = expiry;
                    resultDistance = distance;
                }
            }
            return result;
        }

        private static OptionContract GetClosestByStrike(IEnumerable<OptionContract> contracts, decimal targetStrike)
        {
            return contracts
                .OrderBy(contract => Math.Abs(contract.Strike - targetStrike))
                .ThenBy(contract => contract.Strike)
                .ThenBy(contract => contract.Right)
                .FirstOrDefault();
        }
    }
}
