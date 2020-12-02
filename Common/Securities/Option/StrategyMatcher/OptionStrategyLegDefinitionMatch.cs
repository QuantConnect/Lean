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

namespace QuantConnect.Securities.Option.StrategyMatcher
{
    /// <summary>
    /// Defines the item result type of <see cref="OptionStrategyLegDefinition.Match"/>, containing the number of
    /// times the leg definition matched the position (<see cref="Multiplier"/>) and applicable portion of the position.
    /// </summary>
    public struct OptionStrategyLegDefinitionMatch : IEquatable<OptionStrategyLegDefinitionMatch>
    {
        /// <summary>
        /// The number of times the definition is able to match the position. For example,
        /// if the definition requires +2 contracts and the algorithm's position has +5
        /// contracts, then this multiplier would equal 2.
        /// </summary>
        public int Multiplier { get; }

        /// <summary>
        /// The position that was successfully matched with the total quantity matched. For example,
        /// if the definition requires +2 contracts and this multiplier equals 2, then this position
        /// would have a quantity of 4. This may be different than the remaining/total quantity
        /// available in the positions collection.
        /// </summary>
        public OptionPosition Position { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="OptionStrategyLegDefinitionMatch"/> struct
        /// </summary>
        /// <param name="multiplier">The number of times the positions matched the leg definition</param>
        /// <param name="position">The position that matched the leg definition</param>
        public OptionStrategyLegDefinitionMatch(int multiplier, OptionPosition position)
        {
            Position = position;
            Multiplier = multiplier;
        }

        /// <summary>
        /// Creates the appropriate type of <see cref="OptionStrategy.LegData"/> for this matched position
        /// </summary>
        /// <param name="multiplier">The multiplier to use for creating the leg data. This multiplier will be
        /// the minimum multiplier of all legs within a strategy definition match. Each leg defines its own
        /// multiplier which is the max matches for that leg and the strategy definition's multiplier is the
        /// min of the individual legs.</param>
        public OptionStrategy.LegData CreateOptionStrategyLeg(int multiplier)
        {
            var quantity = Position.Quantity;
            if (Multiplier != multiplier)
            {
                if (multiplier > Multiplier)
                {
                    throw new ArgumentOutOfRangeException(nameof(multiplier), "Unable to create strategy leg with a larger multiplier than matched.");
                }

                // back out the unit quantity and scale it up to the requested multiplier
                var unit = Position.Quantity / Multiplier;
                quantity = unit * multiplier;
            }

            return Position.IsUnderlying
                ? (OptionStrategy.LegData) OptionStrategy.UnderlyingLegData.Create(quantity, Position.Symbol)
                : OptionStrategy.OptionLegData.Create(quantity, Position.Symbol);
        }

        /// <summary>Indicates whether the current object is equal to another object of the same type.</summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>true if the current object is equal to the <paramref name="other" /> parameter; otherwise, false.</returns>
        public bool Equals(OptionStrategyLegDefinitionMatch other)
        {
            return Multiplier == other.Multiplier && Position.Equals(other.Position);
        }

        /// <summary>Indicates whether this instance and a specified object are equal.</summary>
        /// <param name="obj">The object to compare with the current instance. </param>
        /// <returns>true if <paramref name="obj" /> and this instance are the same type and represent the same value; otherwise, false. </returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            return obj is OptionStrategyLegDefinitionMatch && Equals((OptionStrategyLegDefinitionMatch) obj);
        }

        /// <summary>Returns the hash code for this instance.</summary>
        /// <returns>A 32-bit signed integer that is the hash code for this instance.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                return (Multiplier * 397) ^ Position.GetHashCode();
            }
        }

        /// <summary>Returns the fully qualified type name of this instance.</summary>
        /// <returns>The fully qualified type name.</returns>
        public override string ToString()
        {
            return $"{Multiplier} Matches|{Position}";
        }

        public static bool operator ==(OptionStrategyLegDefinitionMatch left, OptionStrategyLegDefinitionMatch right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(OptionStrategyLegDefinitionMatch left, OptionStrategyLegDefinitionMatch right)
        {
            return !left.Equals(right);
        }
    }
}
