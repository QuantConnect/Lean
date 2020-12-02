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
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;

namespace QuantConnect.Securities.Option.StrategyMatcher
{
    /// <summary>
    /// Provides a definitional object for an <see cref="OptionStrategy"/>. This definition is used to 'match' option
    /// positions via <see cref="OptionPositionCollection"/>. The <see cref="OptionStrategyMatcher"/> utilizes a full
    /// collection of these definitional objects in order to match an algorithm's option position holdings to the
    /// set of strategies in an effort to reduce the total margin required for holding the positions.
    /// </summary>
    public class OptionStrategyDefinition : IEnumerable<OptionStrategyLegDefinition>
    {
        /// <summary>
        /// Gets the definition's name
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the number of underlying lots required to match this definition. A lot size
        /// is equal to the contract's multiplier and is usually equal to 100.
        /// </summary>
        public int UnderlyingLots { get; }

        /// <summary>
        /// Gets the option leg definitions. This list does NOT contain a definition for the
        /// required underlying lots, due to its simplicity. Instead the required underlying
        /// lots are defined via the <see cref="UnderlyingLots"/> property of the definition.
        /// </summary>
        public IReadOnlyList<OptionStrategyLegDefinition> Legs { get; }

        /// <summary>
        /// Gets the total number of legs, INCLUDING the underlying leg if applicable. This
        /// is used to perform a coarse filter as the minimum number of unique positions in
        /// the positions collection.
        /// </summary>
        public int LegCount => Legs.Count + (UnderlyingLots == 0 ? 0 : 1);

        /// <summary>
        /// Initializes a new instance of the <see cref="OptionStrategyDefinition"/> class
        /// </summary>
        /// <param name="name">The definition's name</param>
        /// <param name="underlyingLots">The required number of underlying lots</param>
        /// <param name="legs">Definitions for each option leg</param>
        public OptionStrategyDefinition(string name, int underlyingLots, IEnumerable<OptionStrategyLegDefinition> legs)
        {
            Name = name;
            Legs = legs.ToList();
            UnderlyingLots = underlyingLots;
        }

        /// <summary>
        /// Creates the <see cref="OptionStrategy"/> instance using this definition and the provided leg matches
        /// </summary>
        public OptionStrategy CreateStrategy(IReadOnlyList<OptionStrategyLegDefinitionMatch> legs)
        {
            var underlying = legs[0].Position.Symbol;
            if (underlying.HasUnderlying)
            {
                underlying = underlying.Underlying;
            }

            var strategy = new OptionStrategy {Name = Name, Underlying = underlying};
            for (int i = 0; i < Math.Min(Legs.Count, legs.Count); i++)
            {
                var leg = Legs[i].CreateLegData(legs[i]);
                leg.Invoke(strategy.UnderlyingLegs.Add, strategy.OptionLegs.Add);
            }

            return strategy;
        }

        /// <summary>
        /// Attempts to match the positions to this definition exactly once, by evaluating the enumerable and
        /// taking the first entry matched. If not match is found, then false is returned and <paramref name="match"/>
        /// will be null.
        /// </summary>
        public bool TryMatchOnce(OptionStrategyMatcherOptions options, OptionPositionCollection positions, out OptionStrategyDefinitionMatch match)
        {
            match = Match(options, positions).FirstOrDefault();
            return match != null;
        }

        /// <summary>
        /// Determines all possible matches for this definition using the provided <paramref name="positions"/>.
        /// This includes OVERLAPPING matches. It's up to the actual matcher to make decisions based on which
        /// matches to accept. This allows the matcher to prioritize matching certain positions over others.
        /// </summary>
        public IEnumerable<OptionStrategyDefinitionMatch> Match(OptionPositionCollection positions)
        {
            return Match(OptionStrategyMatcherOptions.ForDefinitions(this), positions);
        }

        /// <summary>
        /// Determines all possible matches for this definition using the provided <paramref name="positions"/>.
        /// This includes OVERLAPPING matches. It's up to the actual matcher to make decisions based on which
        /// matches to accept. This allows the matcher to prioritize matching certain positions over others.
        /// </summary>
        public IEnumerable<OptionStrategyDefinitionMatch> Match(
            OptionStrategyMatcherOptions options,
            OptionPositionCollection positions
            )
        {
            // TODO : Pass OptionStrategyMatcherOptions in and respect applicable options
            if (positions.Count < LegCount)
            {
                return Enumerable.Empty<OptionStrategyDefinitionMatch>();
            }

            var multiplier = int.MaxValue;

            // first check underlying lots has correct sign and sufficient magnitude
            var underlyingLotsSign = Math.Sign(UnderlyingLots);
            if (underlyingLotsSign != 0)
            {
                var underlyingPositionSign = Math.Sign(positions.UnderlyingQuantity);
                if (underlyingLotsSign != underlyingPositionSign ||
                    Math.Abs(positions.UnderlyingQuantity) < Math.Abs(UnderlyingLots))
                {
                    return Enumerable.Empty<OptionStrategyDefinitionMatch>();
                }

                // set multiplier for underlying
                multiplier = positions.UnderlyingQuantity / UnderlyingLots;
            }

            // TODO : Consider add OptionStrategyLegDefinition for underlying for consistency purposes.
            //        Might want to enforce that it's always the first leg definition as well for easier slicing.
            return Match(options,
                ImmutableList<OptionStrategyLegDefinitionMatch>.Empty,
                ImmutableList<OptionPosition>.Empty,
                positions,
                multiplier
            ).Distinct();
        }

        private IEnumerable<OptionStrategyDefinitionMatch> Match(
            OptionStrategyMatcherOptions options,
            ImmutableList<OptionStrategyLegDefinitionMatch> legMatches,
            ImmutableList<OptionPosition> legPositions,
            OptionPositionCollection positions,
            int multiplier
            )
        {
            var nextLegIndex = legPositions.Count;
            if (nextLegIndex == Legs.Count)
            {
                if (nextLegIndex > 0)
                {
                    yield return new OptionStrategyDefinitionMatch(this, legMatches, multiplier);
                }
            }
            else if (positions.Count >= LegCount - nextLegIndex)
            {
                // grab the next leg definition and perform the match, restricting total to configured maximum per leg
                var nextLeg = Legs[nextLegIndex];
                var maxLegMatch = options.GetMaximumLegMatches(nextLegIndex);
                foreach (var legMatch in nextLeg.Match(options, legPositions, positions).Take(maxLegMatch))
                {
                    // add match to the match we're constructing and deduct matched position from positions collection
                    // we track the min multiplier in line so when we're done, we have the total number of matches for
                    // the matched set of positions in this 'thread' (OptionStrategy.Quantity)
                    foreach (var definitionMatch in Match(options,
                        legMatches.Add(legMatch),
                        legPositions.Add(legMatch.Position),
                        positions - legMatch.Position,
                        Math.Min(multiplier, legMatch.Multiplier)
                    ))
                    {
                        yield return definitionMatch;
                    }
                }
            }
            else
            {
                // positions.Count < LegsCount indicates a failed match

                // could include partial matches, would allow an algorithm to determine if adding a
                // new position could help reduce overall margin exposure by completing a strategy
            }
        }

        /// <summary>
        /// Attempts to exactly match the specified positions to this strategy definition with as much quantity as possible.
        /// </summary>
        public bool TryMatch(IReadOnlyList<OptionPosition> positions, out OptionStrategy strategy)
        {
            if (positions.Count == 0 || Legs.Count != positions.Count)
            {
                strategy = null;
                return false;
            }

            var underlying = positions[0].Symbol;
            if (underlying.SecurityType == SecurityType.Option)
            {
                underlying = underlying.Underlying;
            }

            var quantityMultiplier = int.MaxValue;
            var matches = new List<OptionStrategy.LegData>();
            for (int i = 0; i < Legs.Count; i++)
            {
                var leg = Legs[i];
                var position = positions[i];
                OptionStrategy.LegData match;
                if (!leg.TryMatch(position, out match))
                {
                    strategy = null;
                    return false;
                }

                matches.Add(match);
                var multiple = match.Quantity / leg.Quantity;
                quantityMultiplier = Math.Min(multiple, quantityMultiplier);
            }

            // separate matches into option/underlying legs and resize according to smallest quantity multipler
            var optionLegs = new List<OptionStrategy.OptionLegData>();
            var underlyingLegs = new List<OptionStrategy.UnderlyingLegData>();
            for (var i = 0; i < matches.Count; i++)
            {
                var match = matches[i];
                match.Invoke(underlyingLegs.Add, optionLegs.Add);
                match.Quantity = Legs[i].Quantity * quantityMultiplier;
            }

            strategy = new OptionStrategy
            {
                Name = Name,
                OptionLegs = optionLegs,
                Underlying =  underlying,
                UnderlyingLegs = underlyingLegs
            };

            return true;
        }

        /// <summary>Returns a string that represents the current object.</summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            return Name;
        }

        /// <summary>
        /// Factory function for creating definitions
        /// </summary>
        public static OptionStrategyDefinition Create(string name, int underlyingLots, params OptionStrategyLegDefinition[] legs)
        {
            return new OptionStrategyDefinition(name, underlyingLots, legs);
        }

        /// <summary>
        /// Factory function for creating definitions
        /// </summary>
        public static OptionStrategyDefinition Create(string name, params OptionStrategyLegDefinition[] legs)
        {
            return new OptionStrategyDefinition(name, 0, legs);
        }

        /// <summary>
        /// Factory function for creating definitions
        /// </summary>
        public static OptionStrategyDefinition Create(string name, params Func<Builder, Builder>[] predicates)
        {
            return predicates.Aggregate(new Builder(name),
                (builder, predicate) => predicate(builder)
            ).Build();
        }

        /// <summary>
        /// Factory function for creating a call leg definition
        /// </summary>
        public static OptionStrategyLegDefinition CallLeg(int quantity,
            params Expression<Func<IReadOnlyList<OptionPosition>, OptionPosition, bool>>[] predicates
            )
        {
            return OptionStrategyLegDefinition.Create(OptionRight.Call, quantity, predicates);
        }

        /// <summary>
        /// Factory function for creating a put leg definition
        /// </summary>
        public static OptionStrategyLegDefinition PutLeg(int quantity,
            params Expression<Func<IReadOnlyList<OptionPosition>, OptionPosition, bool>>[] predicates
            )
        {
            return OptionStrategyLegDefinition.Create(OptionRight.Put, quantity, predicates);
        }

        /// <summary>
        /// Builder class supporting fluent syntax in constructing <see cref="OptionStrategyDefinition"/>.
        /// </summary>
        public class Builder
        {
            private readonly string _name;

            private int _underlyingLots;
            private List<OptionStrategyLegDefinition> _legs;

            /// <summary>
            /// Initializes a new instance of the <see cref="Builder"/> class
            /// </summary>
            public Builder(string name)
            {
                _name = name;
                _legs = new List<OptionStrategyLegDefinition>();
            }

            /// <summary>
            /// Sets the required number of underlying lots
            /// </summary>
            public Builder WithUnderlyingLots(int lots)
            {
                if (_underlyingLots != 0)
                {
                    throw new InvalidOperationException("Underlying lots has already been set.");
                }

                _underlyingLots = lots;
                return this;
            }

            /// <summary>
            /// Adds a call leg
            /// </summary>
            public Builder WithCall(int quantity,
                params Expression<Func<IReadOnlyList<OptionPosition>, OptionPosition, bool>>[] predicates
                )
            {
                _legs.Add(OptionStrategyLegDefinition.Create(OptionRight.Call, quantity, predicates));
                return this;
            }

            /// <summary>
            /// Adds a put leg
            /// </summary>
            public Builder WithPut(int quantity,
                params Expression<Func<IReadOnlyList<OptionPosition>, OptionPosition, bool>>[] predicates
                )
            {
                _legs.Add(OptionStrategyLegDefinition.Create(OptionRight.Put, quantity, predicates));
                return this;
            }

            /// <summary>
            /// Builds the <see cref="OptionStrategyDefinition"/>
            /// </summary>
            public OptionStrategyDefinition Build()
            {
                return new OptionStrategyDefinition(_name, _underlyingLots, _legs);
            }
        }

        /// <summary>Returns an enumerator that iterates through the collection.</summary>
        /// <returns>An enumerator that can be used to iterate through the collection.</returns>
        public IEnumerator<OptionStrategyLegDefinition> GetEnumerator()
        {
            return Legs.GetEnumerator();
        }

        /// <summary>Returns an enumerator that iterates through a collection.</summary>
        /// <returns>An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
