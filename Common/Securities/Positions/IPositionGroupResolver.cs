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

namespace QuantConnect.Securities.Positions
{
    /// <summary>
    /// Resolves position groups from a collection of positions.
    /// </summary>
    public interface IPositionGroupResolver
    {
        /// <summary>
        /// Attempts to group the specified positions into a new <see cref="IPositionGroup"/> using an
        /// appropriate <see cref="IPositionGroupBuyingPowerModel"/> for position groups created via this
        /// resolver.
        /// </summary>
        /// <param name="newPositions">The positions to be grouped</param>
        /// <param name="currentPositions">The currently grouped positions</param>
        /// <param name="group">The grouped positions when this resolver is able to, otherwise null</param>
        /// <returns>True if this resolver can group the specified positions, otherwise false</returns>
        bool TryGroup(IReadOnlyList<IPosition> newPositions, PositionGroupCollection currentPositions, out IPositionGroup group);

        /// <summary>
        /// Resolves the position groups that exist within the specified collection of positions.
        /// </summary>
        /// <param name="positions">The collection of positions</param>
        /// <returns>An enumerable of position groups</returns>
        PositionGroupCollection Resolve(PositionCollection positions);

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
        IEnumerable<IPositionGroup> GetImpactedGroups(
            PositionGroupCollection groups,
            IReadOnlyCollection<IPosition> positions
            );
    }
}
