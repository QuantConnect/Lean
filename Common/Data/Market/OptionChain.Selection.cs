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
        // Cached views, valid for the contract count they were built at
        private int _viewsContractsCount = -1;
        private IReadOnlyList<OptionContract> _calls;
        private IReadOnlyList<OptionContract> _puts;
        private StrikeList _strikePrices;
        private IReadOnlyList<DateTime> _expiries;

        /// <summary>
        /// The call contracts, sorted by expiration then strike
        /// </summary>
        [PandasIgnore]
        public IReadOnlyList<OptionContract> Calls => GetView(ref _calls, () => GetContracts(OptionRight.Call));

        /// <summary>
        /// The put contracts, sorted by expiration then strike
        /// </summary>
        [PandasIgnore]
        public IReadOnlyList<OptionContract> Puts => GetView(ref _puts, () => GetContracts(OptionRight.Put));

        /// <summary>
        /// The distinct strikes, ascending, with helpers to find the closest, next above or next below a price
        /// </summary>
        [PandasIgnore]
        public StrikeList StrikePrices => GetView(ref _strikePrices, () => new StrikeList(Contracts.Values.Select(contract => contract.Strike)));

        /// <summary>
        /// The distinct expiration dates, ascending
        /// </summary>
        [PandasIgnore]
        public IReadOnlyList<DateTime> Expiries => GetView(ref _expiries,
            () => Contracts.Values.Select(contract => contract.Expiry).Distinct().OrderBy(expiry => expiry).ToList());

        #region Selection helpers

        /// <summary>
        /// Selects the single contract closest to the criteria, e.g. <c>chain.select(OptionRight.PUT, target_dte=30, moneyness=-0.15)</c>.
        /// Returns null (None in Python) when nothing matches. Unlike the universe strategy filters,
        /// which take a minimum days to expiration, this takes a target and picks the closest expiration.
        /// See also <see cref="SelectByStrikeDistance"/> and <see cref="SelectByDelta"/>
        /// </summary>
        /// <param name="right">Only consider contracts of this right, any right when null</param>
        /// <param name="targetDte">Only consider the expiration closest to this many days out, see <see cref="ClosestExpiry"/></param>
        /// <param name="minDte">Exclude expirations closer than this many days</param>
        /// <param name="maxDte">Exclude expirations further than this many days</param>
        /// <param name="moneyness">Target strike as a signed fraction of the underlying price, e.g. -0.15 targets 85% of it. 0 is at the money</param>
        /// <returns>The contract with the strike closest to the target, or null</returns>
        public OptionContract Select(OptionRight? right = null, int? targetDte = null, int? minDte = null, int? maxDte = null, decimal moneyness = 0)
        {
            var candidates = GetCandidates(right, targetDte, minDte, maxDte, out var underlyingPrice);
            return underlyingPrice.HasValue ? GetClosestByStrike(candidates, underlyingPrice.Value * (1 + moneyness)) : null;
        }

        /// <summary>
        /// Synonym of <see cref="Select"/>
        /// </summary>
        /// <param name="right">Only consider contracts of this right, any right when null</param>
        /// <param name="targetDte">Only consider the expiration closest to this many days out</param>
        /// <param name="minDte">Exclude expirations closer than this many days</param>
        /// <param name="maxDte">Exclude expirations further than this many days</param>
        /// <param name="moneyness">Target strike as a signed fraction of the underlying price, 0 is at the money</param>
        /// <returns>The contract with the strike closest to the target, or null</returns>
        public OptionContract Pick(OptionRight? right = null, int? targetDte = null, int? minDte = null, int? maxDte = null, decimal moneyness = 0)
        {
            return Select(right, targetDte, minDte, maxDte, moneyness);
        }

        /// <summary>
        /// Like <see cref="Select"/>, with the target strike given as a distance from the underlying price in price units,
        /// as the universe strategy filters take it, e.g. <c>chain.select_by_strike_distance(-5, OptionRight.PUT, target_dte=30)</c>
        /// </summary>
        /// <param name="strikeFromAtm">Signed distance of the target strike from the underlying price</param>
        /// <param name="right">Only consider contracts of this right, any right when null</param>
        /// <param name="targetDte">Only consider the expiration closest to this many days out</param>
        /// <param name="minDte">Exclude expirations closer than this many days</param>
        /// <param name="maxDte">Exclude expirations further than this many days</param>
        /// <returns>The contract with the strike closest to the target, or null</returns>
        public OptionContract SelectByStrikeDistance(decimal strikeFromAtm, OptionRight? right = null, int? targetDte = null, int? minDte = null,
            int? maxDte = null)
        {
            var candidates = GetCandidates(right, targetDte, minDte, maxDte, out var underlyingPrice);
            return underlyingPrice.HasValue ? GetClosestByStrike(candidates, underlyingPrice.Value + strikeFromAtm) : null;
        }

        /// <summary>
        /// Like <see cref="Select"/>, targeting a delta instead of a strike: the contract whose absolute delta is closest to the
        /// absolute target, so a 30 delta put is 0.3 or -0.3, e.g. <c>chain.select_by_delta(0.3, OptionRight.PUT, target_dte=30)</c>.
        /// Contracts without greeks are ignored
        /// </summary>
        /// <param name="targetDelta">The target delta</param>
        /// <param name="right">Only consider contracts of this right, any right when null</param>
        /// <param name="targetDte">Only consider the expiration closest to this many days out</param>
        /// <param name="minDte">Exclude expirations closer than this many days</param>
        /// <param name="maxDte">Exclude expirations further than this many days</param>
        /// <returns>The contract with the delta closest to the target, or null</returns>
        public OptionContract SelectByDelta(decimal targetDelta, OptionRight? right = null, int? targetDte = null, int? minDte = null, int? maxDte = null)
        {
            var target = Math.Abs(targetDelta);
            // Contracts without greeks report a zero delta: they are excluded so a chain without greeks returns null
            return GetCandidates(right, targetDte, minDte, maxDte, out _)
                .Where(contract => contract.Greeks.Delta != 0)
                .OrderBy(contract => Math.Abs(Math.Abs(contract.Greeks.Delta) - target))
                .ThenBy(contract => contract.Expiry)
                .ThenBy(contract => contract.Strike)
                .ThenBy(contract => contract.Right)
                .FirstOrDefault();
        }

        /// <summary>
        /// Gets the expiration closest to the target days out. Returns null (None in Python) when none falls in the window
        /// </summary>
        /// <param name="targetDte">The target days to expiration, ties go to the earlier expiration. Defaults to minDte, else 0</param>
        /// <param name="minDte">Exclude expirations closer than this many days</param>
        /// <param name="maxDte">Exclude expirations further than this many days</param>
        /// <returns>The expiration date as stored in the contracts, or null</returns>
        public DateTime? ClosestExpiry(int? targetDte = null, int? minDte = null, int? maxDte = null)
        {
            return GetClosestExpiry(new OptionChainFilterUniverse(this), Expiries, targetDte, minDte, maxDte);
        }

        /// <summary>
        /// Gets a new chain with the contracts of the given expiration. Time of day is ignored
        /// </summary>
        /// <param name="expiry">The expiration date</param>
        /// <returns>A new chain, empty when nothing matches</returns>
        public OptionChain At(DateTime expiry)
        {
            var expiryDate = expiry.Date;
            return Filter(u =>
            {
                u.Data = u.Data.Where(contract => contract.Expiry.Date == expiryDate).ToList();
                return u;
            });
        }

        /// <summary>
        /// Gets the contract with the strike closest to the underlying price. Ties go to the lower strike,
        /// then the nearest expiration. Returns null (None in Python) when there is none
        /// </summary>
        /// <param name="right">Only consider contracts of this right, any right when null</param>
        /// <returns>The at-the-money contract, or null</returns>
        public OptionContract AtTheMoney(OptionRight? right = null)
        {
            return Select(right);
        }

        /// <summary>
        /// Gets a cached view of the contracts, recomputed when contracts have been added to the chain since it was built
        /// </summary>
        private T GetView<T>(ref T view, Func<T> compute)
            where T : class
        {
            // Slice chains are filled in as data arrives, so a new contract count invalidates every view.
            // Contracts are only ever added, never replaced, and the views derive from the contract symbols
            if (_viewsContractsCount != Contracts.Count)
            {
                _calls = null;
                _puts = null;
                _strikePrices = null;
                _expiries = null;
                _viewsContractsCount = Contracts.Count;
            }
            return view ??= compute();
        }

        /// <summary>
        /// Gets the contracts of the given right and of the expiration closest to the target, none when no expiration is in the window
        /// </summary>
        private IEnumerable<OptionContract> GetCandidates(OptionRight? right, int? targetDte, int? minDte, int? maxDte, out decimal? underlyingPrice)
        {
            var universe = new OptionChainFilterUniverse(this);
            underlyingPrice = universe.Underlying?.Price;

            IEnumerable<OptionContract> candidates = Contracts.Values;
            if (right.HasValue)
            {
                candidates = candidates.Where(contract => contract.Right == right.Value).ToList();
            }

            if (targetDte.HasValue || minDte.HasValue || maxDte.HasValue)
            {
                var expiries = candidates.Select(contract => contract.Expiry).Distinct().OrderBy(expiry => expiry).ToList();
                var expiry = GetClosestExpiry(universe, expiries, targetDte, minDte, maxDte);
                if (!expiry.HasValue)
                {
                    return Enumerable.Empty<OptionContract>();
                }
                candidates = candidates.Where(contract => contract.Expiry == expiry.Value).ToList();
            }

            return candidates;
        }

        private static OptionContract GetClosestByStrike(IEnumerable<OptionContract> contracts, decimal targetStrike)
        {
            // Scaled strikes are in underlying price units, see SymbolProperties.StrikeMultiplier.
            // Ties go to the lower strike, then the nearest expiration, then calls
            return contracts
                .OrderBy(contract => Math.Abs(contract.ScaledStrike - targetStrike))
                .ThenBy(contract => contract.Strike)
                .ThenBy(contract => contract.Expiry)
                .ThenBy(contract => contract.Right)
                .FirstOrDefault();
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
        /// Gets the expiration closest to the target days out among sorted distinct expirations, null when none is in the window
        /// </summary>
        private static DateTime? GetClosestExpiry(OptionChainFilterUniverse universe, IReadOnlyList<DateTime> expiries,
            int? targetDte, int? minDte, int? maxDte)
        {
            // Days to expiration grow with the expiration date, so the window and the target can be searched instead of scanned
            var low = minDte.HasValue ? FirstIndex(expiries, universe, 0, expiries.Count, dte => dte >= minDte.Value) : 0;
            var high = maxDte.HasValue ? FirstIndex(expiries, universe, low, expiries.Count, dte => dte > maxDte.Value) : expiries.Count;
            if (low >= high)
            {
                return null;
            }

            var target = targetDte ?? minDte ?? 0;
            // the first expiration at or beyond the target and the one before it are the only candidates, ties go to the earlier one
            var index = FirstIndex(expiries, universe, low, high, dte => dte >= target);
            if (index == high)
            {
                return expiries[high - 1];
            }
            if (index == low)
            {
                return expiries[low];
            }

            var before = expiries[index - 1];
            var after = expiries[index];
            return universe.GetDaysToExpiry(after) - target < target - universe.GetDaysToExpiry(before) ? after : before;
        }

        /// <summary>
        /// Gets the first index in [low, high) whose days to expiration satisfy the predicate, high when none does.
        /// The predicate must be false then true along the sorted expirations
        /// </summary>
        private static int FirstIndex(IReadOnlyList<DateTime> expiries, OptionChainFilterUniverse universe, int low, int high, Func<int, bool> predicate)
        {
            while (low < high)
            {
                var middle = low + (high - low) / 2;
                if (predicate(universe.GetDaysToExpiry(expiries[middle])))
                {
                    high = middle;
                }
                else
                {
                    low = middle + 1;
                }
            }
            return low;
        }

        #endregion
    }
}
