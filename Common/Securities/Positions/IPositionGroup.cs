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
    /// Defines a group of positions allowing for more efficient use of portfolio margin
    /// </summary>
    public interface IPositionGroup : IReadOnlyCollection<IPosition>
    {
        /// <summary>
        /// Gets the key identifying this group
        /// </summary>
        PositionGroupKey Key { get; }

        /// <summary>
        /// Gets the whole number of units in this position group
        /// </summary>
        decimal Quantity { get; }

        /// <summary>
        /// Gets the positions in this group
        /// </summary>
        IEnumerable<IPosition> Positions { get; }

        /// <summary>
        /// Gets the buying power model defining how margin works in this group
        /// </summary>
        IPositionGroupBuyingPowerModel BuyingPowerModel { get; }

        /// <summary>
        /// Attempts to retrieve the position with the specified symbol
        /// </summary>
        /// <param name="symbol">The symbol</param>
        /// <param name="position">The position, if found</param>
        /// <returns>True if the position was found, otherwise false</returns>
        bool TryGetPosition(Symbol symbol, out IPosition position);
    }
}
