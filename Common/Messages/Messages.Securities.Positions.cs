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

using QuantConnect.Securities.Positions;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

using static QuantConnect.StringExtensions;

namespace QuantConnect
{
    /// <summary>
    /// Provides user-facing message construction methods and static messages for the <see cref="Securities.Positions"/> namespace
    /// </summary>
    public static partial class Messages
    {
        /// <summary>
        /// Provides user-facing messages for the <see cref="Securities.Positions.PositionGroup"/> class and its consumers or related classes
        /// </summary>
        public static class PositionGroup
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string InvalidQuantity(decimal quantity, IEnumerable<IPosition> positions)
            {
                return Invariant($@"The given quantity {quantity
                    } must be equal to the ratio between the quantity and unit quantity for each position. Quantities were {
                    string.Join(", ", positions.Select(position => position.Quantity))}. Unit quantities were {
                    string.Join(", ", positions.Select(position => position.UnitQuantity))}.");
            }
        }
    }
}
