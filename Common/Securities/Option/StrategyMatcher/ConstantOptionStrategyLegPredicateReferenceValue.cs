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

namespace QuantConnect.Securities.Option.StrategyMatcher
{
    /// <summary>
    /// Provides an implementation of <see cref="IOptionStrategyLegPredicateReferenceValue"/> that represents a constant value.
    /// </summary>
    public class ConstantOptionStrategyLegPredicateReferenceValue<T> : IOptionStrategyLegPredicateReferenceValue
    {
        private readonly T _value;

        /// <summary>
        /// Gets the target of this value
        /// </summary>
        public PredicateTargetValue Target { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConstantOptionStrategyLegPredicateReferenceValue{T}"/> class
        /// </summary>
        /// <param name="value">The constant reference value</param>
        /// <param name="target">The value target in relation to the <see cref="OptionPosition"/></param>
        public ConstantOptionStrategyLegPredicateReferenceValue(T value, PredicateTargetValue target)
        {
            _value = value;
            Target = target;
        }

        /// <summary>
        /// Returns the constant value provided at initialization
        /// </summary>
        public object Resolve(IReadOnlyList<OptionPosition> legs)
        {
            return _value;
        }
    }

    /// <summary>
    /// Provides methods for easily creating instances of <see cref="ConstantOptionStrategyLegPredicateReferenceValue{T}"/>
    /// </summary>
    public static class ConstantOptionStrategyLegReferenceValue
    {
        /// <summary>
        /// Creates a new instance of the <see cref="ConstantOptionStrategyLegPredicateReferenceValue{T}"/> class for
        /// the specified <paramref name="value"/>
        /// </summary>
        public static IOptionStrategyLegPredicateReferenceValue Create(object value)
        {
            if (value is DateTime)
            {
                return Create((DateTime) value);
            }

            if (value is decimal)
            {
                return Create((decimal) value);
            }

            if (value is OptionRight)
            {
                return Create((OptionRight) value);
            }

            throw new NotSupportedException($"{value?.GetType().GetBetterTypeName()} is not supported.");
        }

        /// <summary>
        /// Creates a new instance of the <see cref="ConstantOptionStrategyLegPredicateReferenceValue{T}"/> class for
        /// the specified <paramref name="expiration"/>
        /// </summary>
        public static IOptionStrategyLegPredicateReferenceValue Create(DateTime expiration)
        {
            return new ConstantOptionStrategyLegPredicateReferenceValue<DateTime>(expiration, PredicateTargetValue.Expiration);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="ConstantOptionStrategyLegPredicateReferenceValue{T}"/> class for
        /// the specified <paramref name="strike"/>
        /// </summary>
        public static IOptionStrategyLegPredicateReferenceValue Create(decimal strike)
        {
            return new ConstantOptionStrategyLegPredicateReferenceValue<decimal>(strike, PredicateTargetValue.Strike);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="ConstantOptionStrategyLegPredicateReferenceValue{T}"/> class for
        /// the specified <paramref name="right"/>
        /// </summary>
        public static IOptionStrategyLegPredicateReferenceValue Create(OptionRight right)
        {
            return new ConstantOptionStrategyLegPredicateReferenceValue<OptionRight>(right, PredicateTargetValue.Right);
        }
    }
}