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

namespace QuantConnect.Securities.Option.StrategyMatcher
{
    /// <summary>
    /// Defines a match of <see cref="OptionPosition"/> to a <see cref="OptionStrategyDefinition"/>
    /// </summary>
    public class OptionStrategyDefinitionMatch : IEquatable<OptionStrategyDefinitionMatch>
    {
        /// <summary>
        /// The <see cref="OptionStrategyDefinition"/> matched
        /// </summary>
        public OptionStrategyDefinition Definition { get; }

        /// <summary>
        /// The number of times the definition is able to match the available positions.
        /// Since definitions are formed at the 'unit' level, such as having 1 contract,
        /// the multiplier defines how many times the definition matched. This multiplier
        /// is used to scale the quantity defined in each leg definition when creating the
        /// <see cref="OptionStrategy"/> objects.
        /// </summary>
        public int Multiplier { get; }

        /// <summary>
        /// The <see cref="OptionStrategyLegDefinitionMatch"/> instances matched to the definition.
        /// </summary>
        public IReadOnlyList<OptionStrategyLegDefinitionMatch> Legs { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="OptionStrategyDefinitionMatch"/> class
        /// </summary>
        public OptionStrategyDefinitionMatch(
            OptionStrategyDefinition definition,
            IReadOnlyList<OptionStrategyLegDefinitionMatch> legs,
            int multiplier
            )
        {
            Legs = legs;
            Multiplier = multiplier;
            Definition = definition;
        }

        /// <summary>
        /// Deducts the matched positions from the specified <paramref name="positions"/>
        /// </summary>
        public OptionPositionCollection RemoveFrom(OptionPositionCollection positions)
        {
            return positions.RemoveRange(Legs.Select(leg => leg.Position));
        }

        /// <summary>
        /// Creates the <see cref="OptionStrategy"/> instance this match represents
        /// </summary>
        public OptionStrategy CreateStrategy()
        {
            var legs = Legs.Select(leg => leg.CreateOptionStrategyLeg(Multiplier));
            var strategy = new OptionStrategy {Name = Definition.Name, Underlying = Legs[0].Position.Underlying};
            foreach (var leg in legs)
            {
                leg.Invoke(strategy.UnderlyingLegs.Add, strategy.OptionLegs.Add);
            }

            return strategy;
        }

        /// <summary>Indicates whether the current object is equal to another object of the same type.</summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>true if the current object is equal to the <paramref name="other" /> parameter; otherwise, false.</returns>
        public bool Equals(OptionStrategyDefinitionMatch other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (!Equals(Definition, other.Definition))
            {
                return false;
            }

            // index legs by OptionPosition so we can do the equality while ignoring ordering
            var positions = other.Legs.ToDictionary(leg => leg.Position, leg => leg.Multiplier);
            foreach (var leg in other.Legs)
            {
                int multiplier;
                if (!positions.TryGetValue(leg.Position, out multiplier))
                {
                    return false;
                }

                if (leg.Multiplier != multiplier)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Determines whether the specified object is equal to the current object.</summary>
        /// <param name="obj">The object to compare with the current object. </param>
        /// <returns>true if the specified object  is equal to the current object; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != GetType())
            {
                return false;
            }

            return Equals((OptionStrategyDefinitionMatch) obj);
        }

        /// <summary>Serves as the default hash function. </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                // we want to ensure that the ordering of legs does not impact equality operators in
                // pursuit of this, we compute the hash codes of each leg, placing them into an array
                // and then sort the array. using the sorted array, aggregates the hash codes

                var hashCode = Definition.GetHashCode();
                var arr = new int[Legs.Count];
                for (int i = 0; i < Legs.Count; i++)
                {
                    arr[i] = Legs[i].GetHashCode();
                }

                Array.Sort(arr);

                for (int i = 0; i < arr.Length; i++)
                {
                    hashCode = (hashCode * 397) ^ arr[i];
                }

                return hashCode;
            }
        }

        /// <summary>Returns a string that represents the current object.</summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            return $"{Definition.Name}: {string.Join("|", Legs.Select(leg => leg.Position))}";
        }

        public static bool operator ==(OptionStrategyDefinitionMatch left, OptionStrategyDefinitionMatch right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(OptionStrategyDefinitionMatch left, OptionStrategyDefinitionMatch right)
        {
            return !Equals(left, right);
        }
    }
}
