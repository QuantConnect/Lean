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
using QuantConnect.Securities.Option;
using QuantConnect.Util;

namespace QuantConnect.Securities.Positions
{
    /// <summary>
    /// Provides extension methods for <see cref="IPositionGroup"/>
    /// </summary>
    public static class PositionGroupExtensions
    {
        /// <summary>
        /// Gets the position in the <paramref name="group"/> matching the provided <param name="symbol"></param>
        /// </summary>
        public static IPosition GetPosition(this IPositionGroup group, Symbol symbol)
        {
            IPosition position;
            if (!group.TryGetPosition(symbol, out position))
            {
                throw new KeyNotFoundException($"No position with symbol '{symbol}' exists in the group: {group}");
            }

            return position;
        }

        /// <summary>
        /// Creates a new <see cref="IPositionGroup"/> with the specified <paramref name="groupQuantity"/>.
        /// If the quantity provided equals the template's quantity then the template is returned.
        /// </summary>
        /// <param name="template">The group template</param>
        /// <param name="groupQuantity">The quantity of the new group</param>
        /// <returns>A position group with the same position ratios as the template but with the specified group quantity</returns>
        public static IPositionGroup WithQuantity(this IPositionGroup template, decimal groupQuantity)
        {
            if (template.Quantity == groupQuantity)
            {
                return template;
            }

            var positions = template.ToArray(p => p.WithLots(groupQuantity));
            var quantity = template.Key.BuyingPowerModel is OptionStrategyPositionGroupBuyingPowerModel ? Math.Abs(groupQuantity) : groupQuantity;
            return new PositionGroup(template.Key, positions.Length > 0 ?Math.Abs(quantity) : quantity, positions);
        }

        /// <summary>
        /// Creates a new <see cref="IPositionGroup"/> with each position's quantity equaling it's unit quantity
        /// </summary>
        /// <param name="template">The group template</param>
        /// <returns>A position group with the same position ratios as the template but with the specified group quantity</returns>
        public static IPositionGroup CreateUnitGroup(this IPositionGroup template)
        {
            return template.WithQuantity(1);
        }

        /// <summary>
        /// Checks whether the provided groups are closing/reducing each other, that is, each of their positions are in opposite sides.
        /// </summary>
        /// <param name="finalGroup">Source position group</param>
        /// <param name="initialGroup">Target position group</param>
        /// <returns>Whether the position groups close/reduce each other</returns>
        public static bool Closes(this IPositionGroup finalGroup, IPositionGroup initialGroup)
        {
            if (finalGroup.Key != initialGroup.Key)
            {
                return false;
            }

            // Liquidating
            if (finalGroup.Quantity == 0)
            {
                return true;
            }

            // Each of the positions have opposite quantity signs
            if (finalGroup.All(position => Math.Sign(position.Quantity) == -Math.Sign(initialGroup.GetPosition(position.Symbol).Quantity)))
            {
                return true;
            }

            // The final group has a smaller quantity than the initial group
            return Math.Abs(finalGroup.Quantity) < Math.Abs(initialGroup.Quantity) &&
                finalGroup.All(position => Math.Sign(position.Quantity) == Math.Sign(initialGroup.GetPosition(position.Symbol).Quantity));
        }

        /// <summary>
        /// Gets a user friendly name for the provided <paramref name="group"/>
        /// </summary>
        public static string GetUserFriendlyName(this IPositionGroup group)
        {
            if (group.Count == 1)
            {
                return group.Single().Symbol.ToString();
            }

            return string.Join("|", group.Select(p => p.Symbol.ToString()));
        }
    }
}
