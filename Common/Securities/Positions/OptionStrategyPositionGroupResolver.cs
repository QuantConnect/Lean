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

using System.Linq;
using QuantConnect.Util;
using System.Collections.Generic;
using QuantConnect.Securities.Option;
using QuantConnect.Securities.Option.StrategyMatcher;

namespace QuantConnect.Securities.Positions
{
    /// <summary>
    /// Class in charge of resolving option strategy groups which will use the <see cref="OptionStrategyPositionGroupBuyingPowerModel"/>
    /// </summary>
    public class OptionStrategyPositionGroupResolver : IPositionGroupResolver
    {
        private readonly SecurityManager _securities;
        private readonly OptionStrategyMatcher _strategyMatcher;

        /// <summary>
        /// Creates the default option strategy group resolver for <see cref="OptionStrategyDefinitions.AllDefinitions"/>
        /// </summary>
        public OptionStrategyPositionGroupResolver(SecurityManager securities)
            : this(securities, OptionStrategyMatcherOptions.ForDefinitions(OptionStrategyDefinitions.AllDefinitions))
        {
        }

        /// <summary>
        /// Creates a custom option strategy group resolver
        /// </summary>
        /// <param name="strategyMatcherOptions">The option strategy matcher options instance to use</param>
        /// <param name="securities">The algorithms securities</param>
        public OptionStrategyPositionGroupResolver(SecurityManager securities, OptionStrategyMatcherOptions strategyMatcherOptions)
        {
            _securities = securities;
            _strategyMatcher = new OptionStrategyMatcher(strategyMatcherOptions);
        }

        /// <summary>
        /// Attempts to group the specified positions into a new <see cref="IPositionGroup"/> using an
        /// appropriate <see cref="IPositionGroupBuyingPowerModel"/> for position groups created via this
        /// resolver.
        /// </summary>
        /// <param name="newPositions">The positions to be grouped</param>
        /// <param name="currentPositions">The currently grouped positions</param>
        /// <param name="group">The grouped positions when this resolver is able to, otherwise null</param>
        /// <returns>True if this resolver can group the specified positions, otherwise false</returns>
        public bool TryGroup(IReadOnlyCollection<IPosition> newPositions, PositionGroupCollection currentPositions, out IPositionGroup @group)
        {
            IEnumerable<IPosition> positions;
            if (currentPositions.Count > 0)
            {
                var impactedGroups = GetImpactedGroups(currentPositions, newPositions);
                var positionsToConsiderInNewGroup = impactedGroups.SelectMany(positionGroup => positionGroup.Positions);
                positions = newPositions.Concat(positionsToConsiderInNewGroup);
            }
            else
            {
                if (newPositions.Count == 1)
                {
                    // there's no existing position and there's only a single position, no strategy will match
                    @group = null;
                    return false;
                }
                positions = newPositions;
            }

            @group = GetPositionGroups(positions)
                .Select(positionGroup =>
                {
                    if (positionGroup.Count == 0)
                    {
                        return positionGroup;
                    }

                    // from the resolved position groups we will take those which use our buying power model and which are related to the new positions to be executed
                    if (positionGroup.BuyingPowerModel.GetType() == typeof(OptionStrategyPositionGroupBuyingPowerModel))
                    {
                        if (newPositions.Any(position => positionGroup.TryGetPosition(position.Symbol, out position)))
                        {
                            return positionGroup;
                        }

                        // When none of the new positions are contained in the position group,
                        // it means that we are liquidating the assets in the new positions
                        // but some other existing positions were considered as impacted groups.
                        // Example:
                        //   Buy(OptionStrategies.BullCallSpread(...), 1);
                        //   Buy(OptionStrategies.BearPutSpread(...), 1);
                        //   ...
                        //   Sell(OptionStrategies.BullCallSpread(...), 1);
                        //   Sell(OptionStrategies.BearPutSpread(...), 1);
                        //   -----
                        //   When attempting revert the bull call position group, the bear put group
                        //   will be selected as impacted group, so the group will contain the put positions
                        //   but not the call ones. In this case, we return an valid empty group because the
                        //   liquidation is happening.
                        return new PositionGroup(new PositionGroupKey(new OptionStrategyPositionGroupBuyingPowerModel(null), new List<IPosition>()));
                    }

                    return null;
                })
                .Where(positionGroup => positionGroup != null)
                .FirstOrDefault();

            return @group != null;
        }

        /// <summary>
        /// Resolves the position groups that exist within the specified collection of positions.
        /// </summary>
        /// <param name="positions">The collection of positions</param>
        /// <returns>An enumerable of position groups</returns>
        public PositionGroupCollection Resolve(PositionCollection positions)
        {
            var result = PositionGroupCollection.Empty;

            var groups = GetPositionGroups(positions).ToList();
            if (groups.Count != 0)
            {
                result = new PositionGroupCollection(groups);

                // we are expected to remove any positions which we resolved into a position group
                positions.Remove(result);
            }

            return result;
        }

        /// <summary>
        /// Determines the position groups that would be evaluated for grouping of the specified
        /// positions were passed into the <see cref="Resolve"/> method.
        /// </summary>
        /// <remarks>
        /// This function allows us to determine a set of impacted groups and run the resolver on just
        /// those groups in order to support what-if analysis
        /// </remarks>
        /// <param name="groups">The existing position groups</param>
        /// <param name="positions">The positions being changed</param>
        /// <returns>An enumerable containing the position groups that could be impacted by the specified position changes</returns>
        public IEnumerable<IPositionGroup> GetImpactedGroups(PositionGroupCollection groups, IReadOnlyCollection<IPosition> positions)
        {
            if(groups.Count == 0)
            {
                // there's no existing groups, nothing to impact
                return Enumerable.Empty<IPositionGroup>();
            }

            var symbolsSet = positions.Where(position => position.Symbol.SecurityType.HasOptions() || position.Symbol.SecurityType.IsOption())
                .SelectMany(position =>
                {
                    return position.Symbol.HasUnderlying ? new[] { position.Symbol, position.Symbol.Underlying } : new[] { position.Symbol };
                })
                .ToHashSet();

            if (symbolsSet.Count == 0)
            {
                return Enumerable.Empty<IPositionGroup>();
            }

            // will select groups for which we actually hold some security quantity and any of the changed symbols or underlying are in it if they are options
            return groups.Where(group => group.Quantity != 0
                && group.Positions.Any(position1 => symbolsSet.Contains(position1.Symbol)
                    || position1.Symbol.HasUnderlying && position1.Symbol.SecurityType.IsOption() && symbolsSet.Contains(position1.Symbol.Underlying)));
        }

        private IEnumerable<IPositionGroup> GetPositionGroups(IEnumerable<IPosition> positions)
        {
            foreach (var positionsByUnderlying in positions
                .Where(position => position.Symbol.SecurityType.HasOptions() || position.Symbol.SecurityType.IsOption())
                .GroupBy(position => position.Symbol.HasUnderlying? position.Symbol.Underlying : position.Symbol))
            {
                var optionPosition = positionsByUnderlying.FirstOrDefault(position => position.Symbol.SecurityType.IsOption());
                if (optionPosition == null)
                {
                    // if there isn't any option position we aren't really interested, can't create any option strategy!
                    continue;
                }
                var contractMultiplier = (_securities[optionPosition.Symbol].SymbolProperties as OptionSymbolProperties)?.ContractUnitOfTrade ?? 100;

                var optionPositionCollection = OptionPositionCollection.FromPositions(positionsByUnderlying, contractMultiplier);

                if (optionPositionCollection.Count == 0 && positionsByUnderlying.Any())
                {
                    var resultingPositions = new List<IPosition>();
                    var key = new PositionGroupKey(new OptionStrategyPositionGroupBuyingPowerModel(null), resultingPositions);
                    // we could be liquidating there will be no position left!
                    yield return new PositionGroup(key, new Dictionary<Symbol, IPosition>());
                    yield break;
                }

                var matches = _strategyMatcher.MatchOnce(optionPositionCollection);
                if (matches.Strategies.Count == 0)
                {
                    continue;
                }

                foreach (var matchedStrategy in matches.Strategies)
                {
                    var positionsToGroup = matchedStrategy.OptionLegs
                        .Select(optionLeg => (IPosition)new Position(optionLeg.Symbol, optionLeg.Quantity, 1))
                        .Concat(matchedStrategy.UnderlyingLegs.Select(underlyingLeg => new Position(underlyingLeg.Symbol, underlyingLeg.Quantity * contractMultiplier, 1)))
                        .ToArray();

                    yield return new PositionGroup(new OptionStrategyPositionGroupBuyingPowerModel(matchedStrategy), positionsToGroup);
                }
            }
        }
    }
}
