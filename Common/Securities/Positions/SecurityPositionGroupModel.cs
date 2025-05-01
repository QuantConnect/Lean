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
using System.Linq;
using QuantConnect.Orders;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace QuantConnect.Securities.Positions
{
    /// <summary>
    /// Responsible for managing the resolution of position groups for an algorithm
    /// </summary>
    public class SecurityPositionGroupModel : ExtendedDictionary<PositionGroupKey, IPositionGroup>
    {
        /// <summary>
        /// Gets an implementation of <see cref="SecurityPositionGroupModel"/> that will not group multiple securities
        /// </summary>
        public static readonly SecurityPositionGroupModel Null = new NullSecurityPositionGroupModel();

        private bool _requiresGroupResolution;

        private SecurityManager _securities;
        private PositionGroupCollection _groups;
        private IPositionGroupResolver _resolver;

        /// <summary>
        /// Get's the single security position group buying power model to use
        /// </summary>
        protected virtual IPositionGroupBuyingPowerModel PositionGroupBuyingPowerModel { get; } = new SecurityPositionGroupBuyingPowerModel();

        /// <summary>
        /// Gets the set of currently resolved position groups
        /// </summary>
        public PositionGroupCollection Groups
        {
            get
            {
                ResolvePositionGroups();
                return _groups;
            }
            private set
            {
                _groups = value;
            }
        }

        /// <summary>
        /// Gets whether or not the algorithm is using only default position groups
        /// </summary>
        public bool IsOnlyDefaultGroups => Groups.IsOnlyDefaultGroups;

        /// <summary>
        /// Gets the number of position groups in this collection
        /// </summary>
        public override int Count => Groups.Count;

        /// <summary>
        /// Gets all the available position group keys
        /// </summary>
        protected override IEnumerable<PositionGroupKey> GetKeys => Groups.Keys;

        /// <summary>
        /// Gets all the available position groups
        /// </summary>
        protected override IEnumerable<IPositionGroup> GetValues => Groups.Values;

        /// <summary>
        /// Gets all the items in the dictionary
        /// </summary>
        /// <returns>All the items in the dictionary</returns>
        public override IEnumerable<KeyValuePair<PositionGroupKey, IPositionGroup>> GetItems() => Groups.GetGroups();

        /// <summary>
        /// Initializes a new instance of the <see cref="SecurityPositionGroupModel"/> class
        /// </summary>
        /// <param name="securities">The algorithm's security manager</param>
        public virtual void Initialize(SecurityManager securities)
        {
            _securities = securities;
            Groups = PositionGroupCollection.Empty;
            _resolver = GetPositionGroupResolver();

            foreach (var security in _securities.Values)
            {
                // if any security already present let's wire the holdings change event
                security.Holdings.QuantityChanged += HoldingsOnQuantityChanged;
            }

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
                    if (args.Action == NotifyCollectionChangedAction.Add)
                    {
                        security.Holdings.QuantityChanged += HoldingsOnQuantityChanged;
                        if (security.Invested)
                        {
                            // if this security has holdings then we'll need to resolve position groups
                            _requiresGroupResolution = true;
                        }
                    }
                    else if (args.Action == NotifyCollectionChangedAction.Remove)
                    {
                        security.Holdings.QuantityChanged -= HoldingsOnQuantityChanged;
                        if (security.Invested)
                        {
                            // only trigger group resolution if we had holdings in the removed security
                            _requiresGroupResolution = true;
                        }
                    }
                }
            };
        }

        /// <summary>
        /// Gets the <see cref="IPositionGroup"/> matching the specified <paramref name="key"/>. If one is not found,
        /// then a new empty position group is returned.
        /// </summary>
        public override IPositionGroup this[PositionGroupKey key]
        {
            get => Groups[key];
            set => throw new NotImplementedException("Read-only collection. Cannot set value.");
        }

        /// <summary>
        /// Creates a position group for the specified order, pulling
        /// </summary>
        /// <param name="orders">The order</param>
        /// <param name="group">The resulting position group</param>
        /// <returns>A new position group matching the provided order</returns>
        public bool TryCreatePositionGroup(List<Order> orders, out IPositionGroup group)
        {
            var newPositions = orders.Select(order => order.CreatePositions(_securities)).SelectMany(x => x).ToList();

            // We send new and current positions to try resolve any strategy being executed by multiple orders
            // else the PositionGroup we will get out here will just be the default in those cases
            if (!_resolver.TryGroup(newPositions, Groups, out group))
            {
                return false;
            }

            return true;
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
            return new PositionGroupKey(PositionGroupBuyingPowerModel, security);
        }

        /// <summary>
        /// Gets or creates the default position group for the specified <paramref name="security"/>
        /// </summary>
        /// <remarks>
        /// TODO: position group used here is the default, is this what callers want?
        /// </remarks>
        public IPositionGroup GetOrCreateDefaultGroup(Security security)
        {
            var key = CreateDefaultKey(security);
            return Groups[key];
        }

        /// <summary>
        /// Get the position group resolver instance to use
        /// </summary>
        /// <returns>The position group resolver instance</returns>
        protected virtual IPositionGroupResolver GetPositionGroupResolver()
        {
            return new CompositePositionGroupResolver(new OptionStrategyPositionGroupResolver(_securities), new SecurityPositionGroupResolver(PositionGroupBuyingPowerModel));
        }

        private void HoldingsOnQuantityChanged(object sender, SecurityHoldingQuantityChangedEventArgs e)
        {
            _requiresGroupResolution = true;
        }

        /// <summary>
        /// Resolves the algorithm's position groups from all of its holdings
        /// </summary>
        private void ResolvePositionGroups()
        {
            if (_requiresGroupResolution)
            {
                _requiresGroupResolution = false;
                // TODO : Replace w/ special IPosition impl to always equal security.Quantity and we'll
                // use them explicitly for resolution collection so we don't do this each time
                var investedPositions = _securities.Where(kvp => kvp.Value.Invested).Select(kvp => (IPosition)new Position(kvp.Value));
                var positionsCollection = new PositionCollection(investedPositions);
                Groups = ResolvePositionGroups(positionsCollection);
            }
        }

        /// <summary>
        /// Tries to get the position group matching the specified key
        /// </summary>
        /// <param name="key">The key to search for</param>
        /// <param name="value">The position group matching the specified key</param>
        /// <returns>True if a group with the specified key was found, false otherwise</returns>
        public override bool TryGetValue(PositionGroupKey key, out IPositionGroup value)
        {
            return Groups.TryGetGroup(key, out value);
        }
    }
}
