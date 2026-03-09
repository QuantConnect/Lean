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
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using QuantConnect.Util;

namespace QuantConnect.Securities.Option.StrategyMatcher
{
    /// <summary>
    /// Defines a condition under which a particular <see cref="OptionPosition"/> can be combined with
    /// a preceding list of leg (also of type <see cref="OptionPosition"/>) to achieve a particular
    /// option strategy.
    /// </summary>
    public class OptionStrategyLegPredicate
    {
        private readonly BinaryComparison _comparison;
        private readonly IOptionStrategyLegPredicateReferenceValue _reference;
        private readonly Func<IReadOnlyList<OptionPosition>, OptionPosition, bool> _predicate;
        private readonly Expression<Func<IReadOnlyList<OptionPosition>, OptionPosition, bool>> _expression;

        /// <summary>
        /// Determines whether or not this predicate is able to utilize <see cref="OptionPositionCollection"/> indexes.
        /// </summary>
        public bool IsIndexed => _comparison != null && _reference != null;

        /// <summary>
        /// Initializes a new instance of the <see cref="OptionStrategyLegPredicate"/> class
        /// </summary>
        /// <param name="comparison">The <see cref="BinaryComparison"/> invoked</param>
        /// <param name="reference">The reference value, such as a strike price, encapsulated within the
        /// <see cref="IOptionStrategyLegPredicateReferenceValue"/> to enable resolving the value from different potential sets.</param>
        /// <param name="predicate">The compiled predicate expression</param>
        /// <param name="expression">The predicate expression, from which, all other values were derived.</param>
        public OptionStrategyLegPredicate(
            BinaryComparison comparison,
            IOptionStrategyLegPredicateReferenceValue reference,
            Func<IReadOnlyList<OptionPosition>, OptionPosition, bool> predicate,
            Expression<Func<IReadOnlyList<OptionPosition>, OptionPosition, bool>> expression
            )
        {
            _reference = reference;
            _predicate = predicate;
            _comparison = comparison;
            _expression = expression;
        }

        /// <summary>
        /// Determines whether or not the provided combination of preceding <paramref name="legs"/>
        /// and current <paramref name="position"/> adhere to this predicate's requirements.
        /// </summary>
        public bool Matches(IReadOnlyList<OptionPosition> legs, OptionPosition position)
        {
            try
            {
                return _predicate(legs, position);
            }
            catch (InvalidOperationException)
            {
                // attempt to access option SecurityIdentifier values, such as strike, on the underlying
                // this simply means we don't match and can safely ignore this exception. now, this does
                // somewhat indicate a potential design flaw, but I content that this is better than having
                // to manage the underlying position separately throughout the entire matching process.
                return false;
            }
        }

        /// <summary>
        /// Filters the specified <paramref name="positions"/> by applying this predicate based on the referenced legs.
        /// </summary>
        public OptionPositionCollection Filter(IReadOnlyList<OptionPosition> legs, OptionPositionCollection positions, bool includeUnderlying)
        {
            if (!IsIndexed)
            {
                // if the predicate references non-indexed properties or contains complex/multiple conditions then
                // we'll need to do a full table scan. this is not always avoidable, but we should try to avoid it
                return OptionPositionCollection.Empty.AddRange(
                    positions.Where(position => _predicate(legs, position))
                );
            }

            var referenceValue = _reference.Resolve(legs);
            switch (_reference.Target)
            {
                case PredicateTargetValue.Right:        return positions.Slice((OptionRight) referenceValue, includeUnderlying);
                case PredicateTargetValue.Strike:       return positions.Slice(_comparison, (decimal) referenceValue, includeUnderlying);
                case PredicateTargetValue.Expiration:   return positions.Slice(_comparison, (DateTime) referenceValue, includeUnderlying);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Gets the underlying <see cref="IOptionStrategyLegPredicateReferenceValue"/> value used by this predicate.
        /// </summary>
        public IOptionStrategyLegPredicateReferenceValue GetReferenceValue()
        {
            return _reference;
        }

        /// <summary>
        /// Creates a new <see cref="OptionStrategyLegPredicate"/> from the specified predicate <paramref name="expression"/>
        /// </summary>
        public static OptionStrategyLegPredicate Create(
            Expression<Func<IReadOnlyList<OptionPosition>, OptionPosition, bool>> expression
            )
        {
            // expr must NOT include compound comparisons
            // expr is a lambda of one of the following forms:
            // (legs, position) => position.{target} {comparison} legs[i].{reference-target}
            // (legs, position) => legs[i].{reference-target} {comparison} position.{target}
            // (legs, position) => position.{target} {comparison} {literal-reference-target}
            // (legs, position) => {literal-reference-target} {comparison} position.{target}

            // we want to make the comparison of a common form, specifically:
            // position.{target} {comparison} {reference-target}
            // this is so when we invoke OptionPositionCollection we have the correct comparison type
            // for example, legs[0].Strike > position.Strike
            // needs to be inverted into position.Strike < legs[0].Strike
            // so we can call OptionPositionCollection.Slice(BinaryComparison.LessThan, legs[0].Strike);

            try
            {
                var legsParameter = expression.Parameters[0];
                var positionParameter = expression.Parameters[1];
                var binary = expression.OfType<BinaryExpression>().Single(e => e.NodeType.IsBinaryComparison());
                var comparison = BinaryComparison.FromExpressionType(binary.NodeType);
                var leftReference = CreateReferenceValue(legsParameter, positionParameter, binary.Left);
                var rightReference = CreateReferenceValue(legsParameter, positionParameter, binary.Right);
                if (leftReference != null && rightReference != null)
                {
                    throw new ArgumentException($"The provided expression is not of the required form: {expression}");
                }

                // we want the left side to be null, indicating position.{target}
                // if not, then we need to flip the comparison operand
                var reference = rightReference;
                if (rightReference == null)
                {
                    reference = leftReference;
                    comparison = comparison.FlipOperands();
                }

                return new OptionStrategyLegPredicate(comparison, reference, expression.Compile(), expression);
            }
            catch
            {
                // we can still handle arbitrary predicates, they just require a full search of the positions
                // as we're unable to leverage any of the pre-build indexes via Slice methods.
                return new OptionStrategyLegPredicate(null, null, expression.Compile(), expression);
            }
        }

        /// <summary>
        /// Creates a new <see cref="IOptionStrategyLegPredicateReferenceValue"/> from the specified lambda parameters
        /// and expression to be evaluated.
        /// </summary>
        private static IOptionStrategyLegPredicateReferenceValue CreateReferenceValue(
            Expression legsParameter,
            Expression positionParameter,
            Expression expression
            )
        {
            // if we're referencing the position parameter then this isn't a reference value
            // this 'value' is the positions being matched in OptionPositionCollection
            // verify the legs parameter doesn't appear in here either
            var expressions = expression.AsEnumerable().ToList();
            var containsLegParameter = expressions.Any(e => ReferenceEquals(e, legsParameter));
            var containsPositionParameter = expressions.Any(e => ReferenceEquals(e, positionParameter));
            if (containsPositionParameter)
            {
                if (containsLegParameter)
                {
                    throw new NotSupportedException("Expressions containing references to both parameters " +
                        "(legs and positions) on the same side of an equality operator are not supported."
                    );
                }

                // this expression is of the form position.Strike/position.Expiration/position.Right
                // and as such, is not a reference value, simply return null
                return null;
            }

            if (!containsLegParameter)
            {
                // this is a literal and we'll attempt to evaluate it.
                var value = Expression.Lambda(expression).Compile().DynamicInvoke();
                if (value == null)
                {
                    throw new ArgumentNullException($"Failed to evaluate expression literal: {expressions}");
                }

                return ConstantOptionStrategyLegReferenceValue.Create(value);
            }

            // we're looking for an array indexer into the legs list
            var methodCall = expressions.Single<MethodCallExpression>();
            Debug.Assert(methodCall.Method.Name == "get_Item");
            // compile and dynamically invoke the argument to get_Item(x) {legs[x]}
            var arrayIndex = (int) Expression.Lambda(methodCall.Arguments[0]).Compile().DynamicInvoke();

            // and then a member expression denoting the property (target)
            var member = expressions.Single<MemberExpression>().Member;
            var target = GetPredicateTargetValue(member.Name);

            return new OptionStrategyLegPredicateReferenceValue(arrayIndex, target);
        }

        private static PredicateTargetValue GetPredicateTargetValue(string memberName)
        {
            switch (memberName)
            {
                case nameof(OptionPosition.Right):      return PredicateTargetValue.Right;
                case nameof(OptionPosition.Strike):     return PredicateTargetValue.Strike;
                case nameof(OptionPosition.Expiration): return PredicateTargetValue.Expiration;
                default:
                    throw new NotImplementedException(
                        $"Failed to resolve member name '{memberName}' to {nameof(PredicateTargetValue)}"
                    );
            }
        }

        /// <summary>Returns a string that represents the current object.</summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            return _expression.ToString();
        }
    }
}