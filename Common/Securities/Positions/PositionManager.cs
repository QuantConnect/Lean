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
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.Linq;
using QuantConnect.Orders;

namespace QuantConnect.Securities.Positions
{
    /// <summary>
    /// Responsible for managing the resolution of position groups for an algorithm
    /// </summary>
    public class PositionManager
    {
        /// <summary>
        /// Gets the set of currently resolved position groups
        /// </summary>
        public PositionGroupCollection Groups { get; private set; }

        /// <summary>
        /// Gets whether or not the algorithm is using only default position groups
        /// </summary>
        public bool IsOnlyDefaultGroups => Groups.IsOnlyDefaultGroups;

        private bool _requiresGroupResolution;

        private readonly SecurityManager _securities;
        private readonly SecurityPositionGroupResolver _resolver;
        private readonly SecurityPositionGroupBuyingPowerModel _defaultModel;

        /// <summary>
        /// Initializes a new instance of the <see cref="PositionManager"/> class
        /// </summary>
        /// <param name="securities">The algorithm's security manager</param>
        public PositionManager(SecurityManager securities)
        {
            _securities = securities;
            Groups = PositionGroupCollection.Empty;
            _defaultModel = new SecurityPositionGroupBuyingPowerModel();
            _resolver = new SecurityPositionGroupResolver(_defaultModel);

            // we must be notified each time our holdings change, so each time a security is added, we
            // want to bind to its SecurityHolding.QuantityChanged event so we can trigger the resolver

            securities.CollectionChanged += (sender, args) =>
            {
                var items = args.NewItems ?? new List<object>();
                if (args.OldItems != null)
                {
                    foreach (var item in args.OldItems)
                    {
                        items.Add(item);
                    }
                }

                foreach (Security security in items)
                {
                    var key = new PositionGroupKey(_defaultModel, security);
                    if (args.Action == NotifyCollectionChangedAction.Add)
                    {
                        if (!Groups.Contains(key))
                        {
                            // simply adding a security doesn't require group resolution until it has holdings
                            // all we need to do is make sure we add the default SecurityPosition
                            var position = new Position(security);
                            Groups = Groups.Add(new PositionGroup(key, position));
                            security.Holdings.QuantityChanged += HoldingsOnQuantityChanged;
                            if (security.Invested)
                            {
                                // if this security has holdings then we'll need to resolve position groups
                                _requiresGroupResolution = true;
                            }
                        }
                    }
                    else if (args.Action == NotifyCollectionChangedAction.Remove)
                    {
                        if (Groups.Contains(key))
                        {
                            security.Holdings.QuantityChanged -= HoldingsOnQuantityChanged;
                            if (security.Invested)
                            {
                                // only trigger group resolution if we had holdings in the removed security
                                _requiresGroupResolution = true;
                            }
                        }
                    }
                }
            };
        }

        /// <summary>
        /// Creates a position group for the specified order, pulling
        /// </summary>
        /// <param name="order">The order</param>
        /// <returns>A new position group matching the provided order</returns>
        public IPositionGroup CreatePositionGroup(Order order)
        {
            IPositionGroup group;
            var positions = order.CreatePositions(_securities).ToList();
            if (!_resolver.TryGroup(positions, out group))
            {
                throw new InvalidOperationException($"Unable to create group for order: {order.Id}");
            }

            return group;
        }

        /// <summary>
        /// Resolves the algorithm's position groups from all of its holdings
        /// </summary>
        public void ResolvePositionGroups()
        {
            if (_requiresGroupResolution)
            {
                _requiresGroupResolution = false;
                // TODO : Replace w/ special IPosition impl to always equal security.Quantity and we'll
                // use them explicitly for resolution collection so we don't do this each time

                Groups = ResolvePositionGroups(new PositionCollection(_securities.ToImmutableDictionary(
                    kvp => kvp.Key,
                    kvp => (IPosition) new Position(kvp.Value)
                )));
            }
        }

        /// <summary>
        /// Resolves position groups using the specified collection of positions
        /// </summary>
        /// <param name="positions">The positions to be grouped</param>
        /// <returns>A collection of position groups containing all of the provided positions</returns>
        public PositionGroupCollection ResolvePositionGroups(PositionCollection positions)
        {
            return _resolver.Resolve(positions);
        }

        /// <summary>
        /// Determines which position groups could be impacted by changes in the specified positions
        /// </summary>
        /// <param name="positions">The positions to be changed</param>
        /// <returns>All position groups that need to be re-evaluated due to changes in the positions</returns>
        public IEnumerable<IPositionGroup> GetImpactedGroups(IReadOnlyCollection<IPosition> positions)
        {
            return _resolver.GetImpactedGroups(Groups, positions);
        }

        /// <summary>
        /// Creates a <see cref="PositionGroupKey"/> for the security's default position group
        /// </summary>
        public PositionGroupKey CreateDefaultKey(Security security)
        {
            return new PositionGroupKey(_defaultModel, security);
        }

        private void HoldingsOnQuantityChanged(object sender, SecurityHoldingQuantityChangedEventArgs e)
        {
            _requiresGroupResolution = true;
        }
    }
}
