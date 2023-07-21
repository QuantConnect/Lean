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
using System.Collections.Generic;

namespace QuantConnect.Securities.Positions
{
    /// <summary>
    /// Provides an implementation of <see cref="IPositionGroupResolver"/> that places all positions into a default group of one security.
    /// </summary>
    public class SecurityPositionGroupResolver : IPositionGroupResolver
    {
        private readonly IPositionGroupBuyingPowerModel _buyingPowerModel;

        /// <summary>
        /// Initializes a new instance of the <see cref="SecurityPositionGroupResolver"/> class
        /// </summary>
        /// <param name="buyingPowerModel">The buying power model to use for created groups</param>
        public SecurityPositionGroupResolver(IPositionGroupBuyingPowerModel buyingPowerModel)
        {
            _buyingPowerModel = buyingPowerModel;
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
        public bool TryGroup(IReadOnlyCollection<IPosition> newPositions, PositionGroupCollection currentPositions, out IPositionGroup group)
        {
            // we can only create default groupings containing a single security
            if (newPositions.Count != 1)
            {
                group = null;
                return false;
            }

            var key = new PositionGroupKey(_buyingPowerModel, newPositions);
            var position = newPositions.First();
            group = new PositionGroup(key, position.GetGroupQuantity(), newPositions.ToDictionary(p => p.Symbol));
            return true;
        }

        /// <summary>
        /// Resolves the position groups that exist within the specified collection of positions.
        /// </summary>
        /// <param name="positions">The collection of positions</param>
        /// <returns>An enumerable of position groups</returns>
        public PositionGroupCollection Resolve(PositionCollection positions)
        {
            var result = new PositionGroupCollection(positions
                .Select(position => new PositionGroup(_buyingPowerModel, position.GetGroupQuantity(), position)).ToList()
            );

            positions.Clear();
            return result;
        }

        /// <summary>
        /// Determines the position groups that would be evaluated for grouping of the specified
        /// positions were passed into the <see cref="IPositionGroupResolver.Resolve"/> method.
        /// </summary>
        /// <remarks>
        /// This function allows us to determine a set of impacted groups and run the resolver on just
        /// those groups in order to support what-if analysis
        /// </remarks>
        /// <param name="groups">The existing position groups</param>
        /// <param name="positions">The positions being changed</param>
        /// <returns>An enumerable containing the position groups that could be impacted by the specified position changes</returns>
        public IEnumerable<IPositionGroup> GetImpactedGroups(
            PositionGroupCollection groups,
            IReadOnlyCollection<IPosition> positions
            )
        {
            var seen = new HashSet<PositionGroupKey>();
            foreach (var position in positions)
            {
                IReadOnlyCollection<IPositionGroup> groupsForSymbol;
                if (!groups.TryGetGroups(position.Symbol, out groupsForSymbol))
                {
                    continue;
                }

                foreach (var group in groupsForSymbol)
                {
                    if (seen.Add(group.Key))
                    {
                        yield return group;
                    }
                }
            }
        }
    }
}
