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
using QuantConnect.Python;
using QuantConnect.Securities;

namespace QuantConnect.Data.Market
{
    /// <summary>
    /// The option chain selection helpers: views of the contracts and single contract pickers,
    /// null-safe (None in Python) instead of raising when nothing matches
    /// </summary>
    public partial class OptionChain
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
        /// Gets the distinct strike prices in the chain, sorted in ascending order, with helpers to find the strike
        /// closest to, right above or right below a price. See <see cref="StrikeList"/>
        /// </summary>
        [PandasIgnore]
        public StrikeList StrikePrices => new(Contracts.Values.Select(contract => contract.Strike));

        /// <summary>
        /// Gets the distinct expiration dates in the chain, sorted in ascending order
        /// </summary>
        [PandasIgnore]
        public List<DateTime> Expiries => Contracts.Values.Select(contract => contract.Expiry).Distinct().OrderBy(expiry => expiry).ToList();

        #region Selection helpers

        /// <summary>
        /// Selects the single contract that best matches the given criteria, e.g.
        /// <c>chain.select(right=OptionRight.PUT, target_dte=30, moneyness=-0.15)</c> or <c>chain.select(OptionRight.CALL, 45, target_delta=0.3)</c>.
        /// Null-safe: returns null (None in Python) instead of throwing when nothing matches.
        /// Unlike the universe strategy filters, e.g. <see cref="BaseOptionFilterUniverse{TUniverse, TData}.NakedCall"/>, which take a minimum
        /// days to expiration and pick the first expiration at or after it, this takes a target and picks the expiration closest to it
        /// </summary>
        /// <param name="right">If set, only contracts of this right are considered</param>
        /// <param name="targetDte">If set, only the expiration closest to this many days from the chain date is considered. See <see cref="ClosestExpiry"/></param>
        /// <param name="minDte">If set, expirations closer than this many days are excluded</param>
        /// <param name="maxDte">If set, expirations further than this many days are excluded</param>
        /// <param name="moneyness">Signed distance of the strike from the underlying price as a fraction of it, regardless of right:
        /// negative values target strikes below the underlying price, positive values above, e.g. -0.15 targets the strike closest to 85% of the underlying price.
        /// Mutually exclusive with <paramref name="strikeFromAtm"/> and <paramref name="targetDelta"/></param>
        /// <param name="strikeFromAtm">Signed distance of the strike from the underlying price, in price units, like the universe strategy filters take.
        /// Mutually exclusive with <paramref name="moneyness"/> and <paramref name="targetDelta"/></param>
        /// <param name="targetDelta">If set, the contract whose absolute delta is closest to the absolute value of this target is selected,
        /// so a 30 delta put can be requested as either 0.3 or -0.3. Contracts without greeks are ignored.
        /// Mutually exclusive with <paramref name="moneyness"/> and <paramref name="strikeFromAtm"/></param>
        /// <returns>The best matching contract, or null if none matches. Without strike criteria the at-the-money contract is returned</returns>
        public OptionContract Select(OptionRight? right = null, int? targetDte = null, int? minDte = null, int? maxDte = null,
            decimal? moneyness = null, decimal? strikeFromAtm = null, decimal? targetDelta = null)
        {
            var strikeCriteria = (moneyness.HasValue ? 1 : 0) + (strikeFromAtm.HasValue ? 1 : 0) + (targetDelta.HasValue ? 1 : 0);
            if (strikeCriteria > 1)
            {
                throw new ArgumentException("OptionChain.Select(): moneyness, strikeFromAtm and targetDelta are mutually exclusive, please set only one of them.");
            }

            var universe = new OptionChainFilterUniverse(this);
            IEnumerable<OptionContract> candidates = Contracts.Values;
            if (right.HasValue)
            {
                candidates = candidates.Where(contract => contract.Right == right.Value).ToList();
            }

            if (targetDte.HasValue || minDte.HasValue || maxDte.HasValue)
            {
                var expiry = GetClosestExpiry(universe, candidates, targetDte, minDte, maxDte);
                if (!expiry.HasValue)
                {
                    return null;
                }
                candidates = candidates.Where(contract => contract.Expiry == expiry.Value).ToList();
            }

            if (targetDelta.HasValue)
            {
                var target = Math.Abs(targetDelta.Value);
                // Contracts without greeks report a zero delta: they are excluded so a chain without greeks returns null
                return candidates
                    .Where(contract => contract.Greeks.Delta != 0)
                    .OrderBy(contract => Math.Abs(Math.Abs(contract.Greeks.Delta) - target))
                    .ThenBy(contract => contract.Expiry)
                    .ThenBy(contract => contract.Strike)
                    .ThenBy(contract => contract.Right)
                    .FirstOrDefault();
            }

            var underlyingPrice = universe.Underlying?.Price;
            if (!underlyingPrice.HasValue)
            {
                return null;
            }
            var targetStrike = strikeFromAtm.HasValue
                ? underlyingPrice.Value + strikeFromAtm.Value
                : underlyingPrice.Value * (1 + (moneyness ?? 0));
            return GetClosestByStrike(candidates, targetStrike);
        }

        /// <summary>
        /// Selects the single contract that best matches the given criteria. Synonym of <see cref="Select"/>
        /// </summary>
        /// <param name="right">If set, only contracts of this right are considered</param>
        /// <param name="targetDte">If set, only the expiration closest to this many days from the chain date is considered</param>
        /// <param name="minDte">If set, expirations closer than this many days are excluded</param>
        /// <param name="maxDte">If set, expirations further than this many days are excluded</param>
        /// <param name="moneyness">Signed distance of the strike from the underlying price as a fraction of it</param>
        /// <param name="strikeFromAtm">Signed distance of the strike from the underlying price, in price units</param>
        /// <param name="targetDelta">If set, the contract whose absolute delta is closest to the absolute value of this target is selected</param>
        /// <returns>The best matching contract, or null if none matches</returns>
        public OptionContract Pick(OptionRight? right = null, int? targetDte = null, int? minDte = null, int? maxDte = null,
            decimal? moneyness = null, decimal? strikeFromAtm = null, decimal? targetDelta = null)
        {
            return Select(right, targetDte, minDte, maxDte, moneyness, strikeFromAtm, targetDelta);
        }

        /// <summary>
        /// Gets the expiration date in the chain closest to the target number of days from the chain date.
        /// Days to expiration are counted to the contract's last trading date, so Saturday expirations of equity options
        /// before February 2015 count on the preceding Friday.
        /// Null-safe: returns null (None in Python) when the chain is empty or no expiration falls within the requested window
        /// </summary>
        /// <param name="targetDte">The target days to expiration. When two expirations are equidistant the earlier one is returned.
        /// Defaults to minDte if set, else 0, i.e. the nearest expiration</param>
        /// <param name="minDte">If set, expirations closer than this many days are excluded</param>
        /// <param name="maxDte">If set, expirations further than this many days are excluded</param>
        /// <returns>The best matching expiration date as stored in the chain's contracts, or null if none matches</returns>
        public DateTime? ClosestExpiry(int? targetDte = null, int? minDte = null, int? maxDte = null)
        {
            return GetClosestExpiry(new OptionChainFilterUniverse(this), Contracts.Values, targetDte, minDte, maxDte);
        }

        /// <summary>
        /// Gets a new chain containing only the contracts with the given expiration date, e.g. <c>chain.at(expiry).puts</c>.
        /// Matching is done on the last trading date, so a chain of Saturday expiring contracts (equity options before February 2015)
        /// is also matched by the preceding Friday
        /// </summary>
        /// <param name="expiry">The expiration date, time of day is ignored</param>
        /// <returns>A new chain with only the matching contracts, empty if none matches</returns>
        public OptionChain At(DateTime expiry)
        {
            var universe = new OptionChainFilterUniverse(this);
            var expiryDate = universe.ToLastTradingDate(expiry);
            return Filter(u =>
            {
                u.Data = u.Data.Where(contract => universe.ToLastTradingDate(contract.Expiry) == expiryDate).ToList();
                return u;
            });
        }

        /// <summary>
        /// Gets the contract whose strike is closest to the current underlying price, of the given right if any.
        /// When two strikes are equidistant the lower one is returned, and among equal strikes the nearest expiration.
        /// Null-safe: returns null (None in Python) when the chain has no matching contracts or the underlying price is unavailable
        /// </summary>
        /// <param name="right">If set, only contracts of this right are considered</param>
        /// <returns>The at-the-money contract, or null if there is none</returns>
        public OptionContract AtTheMoney(OptionRight? right = null)
        {
            return Select(right);
        }

        private List<OptionContract> GetContracts(OptionRight right)
        {
            return Contracts.Values
                .Where(contract => contract.Right == right)
                .OrderBy(contract => contract.Expiry)
                .ThenBy(contract => contract.Strike)
                .ToList();
        }

        private static DateTime? GetClosestExpiry(OptionChainFilterUniverse universe, IEnumerable<OptionContract> contracts,
            int? targetDte, int? minDte, int? maxDte)
        {
            var target = targetDte ?? minDte ?? 0;
            DateTime? result = null;
            var resultDistance = int.MaxValue;
            foreach (var contract in contracts.DistinctBy(contract => contract.Expiry))
            {
                var dte = universe.GetDaysToExpiry(contract);
                // Lifted comparisons are false when the bound is null, i.e. unset bounds don't exclude anything
                if (dte < minDte || dte > maxDte)
                {
                    continue;
                }
                var distance = Math.Abs(dte - target);
                if (distance < resultDistance || (distance == resultDistance && contract.Expiry < result.Value))
                {
                    result = contract.Expiry;
                    resultDistance = distance;
                }
            }
            return result;
        }

        private static OptionContract GetClosestByStrike(IEnumerable<OptionContract> contracts, decimal targetStrike)
        {
            // Scaled strikes are in underlying price units, see SymbolProperties.StrikeMultiplier
            return contracts
                .OrderBy(contract => Math.Abs(contract.ScaledStrike - targetStrike))
                .ThenBy(contract => contract.Strike)
                .ThenBy(contract => contract.Expiry)
                .ThenBy(contract => contract.Right)
                .FirstOrDefault();
        }

        #endregion
    }
}
