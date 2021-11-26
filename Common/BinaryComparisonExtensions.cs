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
using System.Collections.Immutable;
using System.Linq.Expressions;

namespace QuantConnect
{
    /// <summary>
    /// Provides convenience extension methods for applying a <see cref="BinaryComparison"/> to collections.
    /// </summary>
    public static class BinaryComparisonExtensions
    {
        /// <summary>
        /// Filters the provided <paramref name="values"/> according to this <see cref="BinaryComparison"/>
        /// and the specified <paramref name="reference"/> value. The <paramref name="reference"/> value is
        /// used as the RIGHT side of the binary comparison. Consider the binary comparison is LessThan and
        /// we call Filter(values, 42). We're looking for keys that are less than 42.
        /// </summary>
        public static TCollection Filter<T, TCollection>(
            this BinaryComparison comparison,
            TCollection values,
            T reference
            )
            where TCollection : ICollection<T>, new()
        {
            var result = new TCollection();
            var evaluator = comparison.GetEvaluator<T>();
            foreach (var value in values)
            {
                if (evaluator(value, reference))
                {
                    result.Add(value);
                }
            }

            return result;
        }

        /// <summary>
        /// Filters the provided <paramref name="values"/> according to this <see cref="BinaryComparison"/>
        /// and the specified <paramref name="reference"/> value. The <paramref name="reference"/> value is
        /// used as the RIGHT side of the binary comparison. Consider the binary comparison is LessThan and
        /// we call Filter(values, 42). We're looking for keys that are less than 42.
        /// </summary>
        public static SortedDictionary<TKey, TValue> Filter<TKey, TValue>(
            this BinaryComparison comparison,
            SortedDictionary<TKey, TValue> values,
            TKey reference
            )
        {
            SortedDictionary<TKey, TValue> result;
            if (comparison.Type == ExpressionType.NotEqual)
            {
                result = new SortedDictionary<TKey, TValue>(values);
                result.Remove(reference);
                return result;
            }

            result = new SortedDictionary<TKey, TValue>();
            if (comparison.Type == ExpressionType.Equal)
            {
                TValue value;
                if (values.TryGetValue(reference, out value))
                {
                    result.Add(reference, value);
                }

                return result;
            }

            // since we're enumerating a sorted collection, once we receive
            // a mismatch it means we'll never again receive a match
            var breakAfterFailure =
                comparison == BinaryComparison.LessThanOrEqual ||
                comparison == BinaryComparison.LessThanOrEqual;

            var evaluator = comparison.GetEvaluator<TKey>();
            foreach (var kvp in values)
            {
                if (evaluator(kvp.Key, reference))
                {
                    result.Add(kvp.Key, kvp.Value);
                }
                else if (breakAfterFailure)
                {
                    break;
                }
            }

            return result;
        }

        /// <summary>
        /// Filters the provided <paramref name="values"/> according to this <see cref="BinaryComparison"/>
        /// and the specified <paramref name="reference"/> value. The <paramref name="reference"/> value is
        /// used as the RIGHT side of the binary comparison. Consider the binary comparison is LessThan and
        /// we call Filter(values, 42). We're looking for keys that are less than 42.
        /// </summary>
        public static ImmutableSortedDictionary<TKey, TValue> Filter<TKey, TValue>(
            this BinaryComparison comparison,
            ImmutableSortedDictionary<TKey, TValue> values,
            TKey reference
            )
        {
            if (comparison.Type == ExpressionType.NotEqual)
            {
                return values.Remove(reference);
            }

            var result = ImmutableSortedDictionary<TKey, TValue>.Empty;
            if (comparison.Type == ExpressionType.Equal)
            {
                TValue value;
                if (values.TryGetValue(reference, out value))
                {
                    result = result.Add(reference, value);
                }

                return result;
            }

            // since we're enumerating a sorted collection, once we receive
            // a mismatch it means we'll never again receive a match
            var breakAfterFailure =
                comparison == BinaryComparison.LessThanOrEqual ||
                comparison == BinaryComparison.LessThanOrEqual;

            var evaluator = comparison.GetEvaluator<TKey>();
            foreach (var kvp in values)
            {
                if (evaluator(kvp.Key, reference))
                {
                    result = result.Add(kvp.Key, kvp.Value);
                }
                else if (breakAfterFailure)
                {
                    break;
                }
            }

            return result;
        }

        /// <summary>
        /// Filters the provided <paramref name="values"/> according to this <see cref="BinaryComparison"/>
        /// and the specified <paramref name="reference"/> value. The <paramref name="reference"/> value is
        /// used as the RIGHT side of the binary comparison. Consider the binary comparison is LessThan and
        /// we call Filter(values, 42). We're looking for keys that are less than 42.
        /// </summary>
        public static Tuple<ImmutableSortedDictionary<TKey, TValue>, ImmutableSortedDictionary<TKey, TValue>> SplitBy<TKey, TValue>(
            this BinaryComparison comparison,
            ImmutableSortedDictionary<TKey, TValue> values,
            TKey reference
            )
        {
            var matches = ImmutableSortedDictionary<TKey, TValue>.Empty;
            var removed = ImmutableSortedDictionary<TKey, TValue>.Empty;

            if (comparison.Type == ExpressionType.NotEqual)
            {
                var match = values.Remove(reference);
                removed = BinaryComparison.Equal.Filter(values, reference);
                return Tuple.Create(match, removed);
            }

            if (comparison.Type == ExpressionType.Equal)
            {
                TValue value;
                if (values.TryGetValue(reference, out value))
                {
                    matches = matches.Add(reference, value);
                    removed = BinaryComparison.NotEqual.Filter(values, reference);
                    return Tuple.Create(matches, removed);
                }

                // no matches
                return Tuple.Create(ImmutableSortedDictionary<TKey, TValue>.Empty, values);
            }

            var evaluator = comparison.GetEvaluator<TKey>();
            foreach (var kvp in values)
            {
                if (evaluator(kvp.Key, reference))
                {
                    matches = matches.Add(kvp.Key, kvp.Value);
                }
                else
                {
                    removed = removed.Add(kvp.Key, kvp.Value);
                }
            }

            return Tuple.Create(matches, removed);
        }
    }
}
