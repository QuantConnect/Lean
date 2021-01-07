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

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.Linq;

namespace QuantConnect.Securities.Positions
{
    /// <summary>
    /// Responsible for managing the resolution of position groups for an algorithm
    /// </summary>
    public class PositionManager
    {
        private bool _requiresGroupResolution;
        private Dictionary<PositionGroupKey, IPositionGroup> _groups;

        private readonly SecurityManager _securities;
        private readonly SecurityPositionGroupResolver _resolver;
        private readonly IPositionGroupBuyingPowerModel _defaultBuyingPowerModel;

        /// <summary>
        /// Initializes a new instance of the <see cref="PositionManager"/> class
        /// </summary>
        /// <param name="securities">The algorithm's security manager</param>
        public PositionManager(SecurityManager securities)
        {
            _securities = securities;
            _groups = new Dictionary<PositionGroupKey, IPositionGroup>();
            _defaultBuyingPowerModel = new SecurityPositionGroupBuyingPowerModel();
            _resolver = new SecurityPositionGroupResolver(_defaultBuyingPowerModel);

            // we must be notified each time our holdings change, so each time a security is added, we
            // want to bind to its SecurityHolding.QuantityChanged event so we can trigger the resolver

            securities.CollectionChanged += (sender, args) =>
            {
                foreach (Security security in args.NewItems)
                {
                    IPositionGroup group;
                    var key = new PositionGroupKey(_defaultBuyingPowerModel, security);
                    if (args.Action == NotifyCollectionChangedAction.Add)
                    {
                        if (!_groups.TryGetValue(key, out group))
                        {
                            // simply adding a security doesn't require group resolution until it has holdings
                            // all we need to do is make sure we add the default SecurityPosition
                            var position = new Position(security);
                            _groups[key] = new PositionGroup(key, position);;
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
                        if (_groups.TryGetValue(key, out group))
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
        /// Resolves the algorithm's position groups from all of its holdings
        /// </summary>
        public void ResolvePositionGroups()
        {
            if (_requiresGroupResolution)
            {
                _requiresGroupResolution = false;
                var positions = new PositionCollection(_securities.ToImmutableDictionary(
                    kvp => kvp.Key,
                    kvp => (IPosition) new Position(kvp.Value)
                ));

                _groups = _resolver.Resolve(positions).ToDictionary(grp => grp.Key);
            }
        }

        private void HoldingsOnQuantityChanged(object sender, SecurityHoldingQuantityChangedEventArgs e)
        {
            _requiresGroupResolution = true;
        }
    }
}
