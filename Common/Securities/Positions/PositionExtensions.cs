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

namespace QuantConnect.Securities.Positions
{
    /// <summary>
    /// Provides extension methods for <see cref="IPosition"/>
    /// </summary>
    public static class PositionExtensions
    {
        /// <summary>
        /// Deducts the specified <paramref name="quantityToDeduct"/> from the specified <paramref name="position"/>
        /// </summary>
        /// <param name="position">The source position</param>
        /// <param name="quantityToDeduct">The quantity to deduct</param>
        /// <returns>A new position with the same properties but quantity reduced by the specified amount</returns>
        public static IPosition Deduct(this IPosition position, decimal quantityToDeduct)
        {
            var newQuantity = position.Quantity - quantityToDeduct;
            return new Position(position.Symbol, newQuantity, position.UnitQuantity);
        }
    }
}
