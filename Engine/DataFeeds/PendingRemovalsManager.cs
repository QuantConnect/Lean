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

using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Securities;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// Helper class used to managed pending security removals <see cref="UniverseSelection"/>
    /// </summary>
    public class PendingRemovalsManager
    {
        private readonly Dictionary<Universe, List<Security>> _pendingRemovals;
        private readonly IOrderProvider _orderProvider;

        /// <summary>
        /// Current pending removals
        /// </summary>
        public IReadOnlyDictionary<Universe, List<Security>> PendingRemovals => _pendingRemovals;

        /// <summary>
        /// Create a new instance
        /// </summary>
        /// <param name="orderProvider">The order provider used to determine if it is safe to remove a security</param>
        public PendingRemovalsManager(IOrderProvider orderProvider)
        {
            _orderProvider = orderProvider;
            _pendingRemovals = new Dictionary<Universe, List<Security>>();
        }

        /// <summary>
        /// Determines if we can safely remove the security member from a universe.
        /// We must ensure that we have zero holdings, no open orders, and no existing portfolio targets
        /// </summary>
        private bool IsSafeToRemove(Security member, Universe universe)
        {
            // but don't physically remove it from the algorithm if we hold stock or have open orders against it or an open target
            var openOrders = _orderProvider.GetOpenOrders(x => x.Symbol == member.Symbol);
            if (!member.HoldStock && !openOrders.Any() && (member.Holdings.Target == null || member.Holdings.Target.Quantity == 0))
            {
                if (universe.Securities.Any(pair =>
                    pair.Key.Underlying == member.Symbol && !IsSafeToRemove(pair.Value.Security, universe)))
                {
                    // don't remove if any member in the universe which uses this 'member' as underlying can't be removed
                    // covers the options use case
                    return false;
                }

                // don't remove if there are unsettled positions
                var unsettledCash = member.SettlementModel.GetUnsettledCash();
                if (unsettledCash != default && unsettledCash.Amount > 0)
                {
                    return false;
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Will determine if the <see cref="Security"/> can be removed.
        /// If it can be removed will add it to <see cref="PendingRemovals"/>
        /// </summary>
        /// <param name="member">The security to remove</param>
        /// <param name="universe">The universe which the security is a member of</param>
        /// <returns>The member to remove</returns>
        public List<RemovedMember> TryRemoveMember(Security member, Universe universe)
        {
            if (IsSafeToRemove(member, universe))
            {
                return new List<RemovedMember> {new RemovedMember(universe, member)};
            }

            if (_pendingRemovals.ContainsKey(universe))
            {
                if (!_pendingRemovals[universe].Contains(member))
                {
                    _pendingRemovals[universe].Add(member);
                }
            }
            else
            {
                _pendingRemovals.Add(universe, new List<Security> { member });
            }

            return null;
        }

        /// <summary>
        /// Will check pending security removals
        /// </summary>
        /// <param name="selectedSymbols">Currently selected symbols</param>
        /// <param name="currentUniverse">Current universe</param>
        /// <returns>The members to be removed</returns>
        public List<RemovedMember> CheckPendingRemovals(
            HashSet<Symbol> selectedSymbols,
            Universe currentUniverse)
        {
            var result = new List<RemovedMember>();
            // remove previously deselected members which were kept in the universe because of holdings or open orders
            foreach (var kvp in _pendingRemovals.ToList())
            {
                var universeRemoving = kvp.Key;
                foreach (var security in kvp.Value.ToList())
                {
                    var isSafeToRemove = IsSafeToRemove(security, universeRemoving);
                    if (isSafeToRemove
                        ||
                        // if we are re selecting it we remove it as a pending removal
                        // else we might remove it when we do not want to do so
                        universeRemoving == currentUniverse
                        && selectedSymbols.Contains(security.Symbol))
                    {
                        if (isSafeToRemove)
                        {
                            result.Add(new RemovedMember(universeRemoving, security));
                        }

                        _pendingRemovals[universeRemoving].Remove(security);

                        // if there are no more pending removals for this universe lets remove it
                        if (!_pendingRemovals[universeRemoving].Any())
                        {
                            _pendingRemovals.Remove(universeRemoving);
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Helper class used to report removed universe members
        /// </summary>
        public class RemovedMember
        {
            /// <summary>
            /// Universe the security was removed from
            /// </summary>
            public Universe Universe { get; }

            /// <summary>
            /// Security that is removed
            /// </summary>
            public Security Security { get; }

            /// <summary>
            /// Initialize a new instance of <see cref="RemovedMember"/>
            /// </summary>
            /// <param name="universe"><see cref="Universe"/> the security was removed from</param>
            /// <param name="security"><see cref="Security"/> that is removed</param>
            public RemovedMember(Universe universe, Security security)
            {
                Universe = universe;
                Security = security;
            }
        }
    }
}
