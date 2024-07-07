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
    /// Specifies the impact on buying power from changing security holdings that affects current <see cref="IPositionGroup"/>,
    /// including the current reserved buying power, without the change, and a contemplate reserved buying power, which takes
    /// into account a contemplated change to the algorithm's positions that impacts current position groups.
    /// </summary>
    public class ReservedBuyingPowerImpact
    {
        /// <summary>
        /// Gets the current reserved buying power for the impacted groups
        /// </summary>
        public decimal Current { get; }

        /// <summary>
        /// Gets the reserved buying power for groups resolved after applying a contemplated change to the impacted groups
        /// </summary>
        public decimal Contemplated { get; }

        /// <summary>
        /// Gets the change in reserved buying power, <see cref="Current"/> minus <see cref="Contemplated"/>
        /// </summary>
        public decimal Delta { get; }

        /// <summary>
        /// Gets the impacted groups used as the basis for these reserved buying power numbers
        /// </summary>
        public IReadOnlyCollection<IPositionGroup> ImpactedGroups { get; }

        /// <summary>
        /// Gets the position changes being contemplated
        /// </summary>
        public IReadOnlyCollection<IPosition> ContemplatedChanges { get; }

        /// <summary>
        /// Gets the newly resolved groups resulting from applying the contemplated changes to the impacted groups
        /// </summary>
        public IReadOnlyCollection<IPositionGroup> ContemplatedGroups { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReservedBuyingPowerImpact"/> class
        /// </summary>
        /// <param name="current">The current reserved buying power for impacted groups</param>
        /// <param name="contemplated">The reserved buying power for impacted groups after applying the contemplated changes</param>
        /// <param name="impactedGroups">The groups impacted by the contemplated changes</param>
        /// <param name="contemplatedChanges">The position changes being contemplated</param>
        /// <param name="contemplatedGroups">The groups resulting from applying the contemplated changes</param>
        public ReservedBuyingPowerImpact(
            decimal current,
            decimal contemplated,
            IReadOnlyCollection<IPositionGroup> impactedGroups,
            IReadOnlyCollection<IPosition> contemplatedChanges,
            IReadOnlyCollection<IPositionGroup> contemplatedGroups
        )
        {
            Current = current;
            Contemplated = contemplated;
            Delta = Contemplated - Current;
            ImpactedGroups = impactedGroups;
            ContemplatedGroups = contemplatedGroups;
            ContemplatedChanges = contemplatedChanges;
        }
    }
}
