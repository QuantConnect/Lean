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

namespace QuantConnect.Util
{
    /// <summary>
    /// Provides more extension methods for the enumerable types
    /// </summary>
    public static class LinqExtensions
    {
        /// <summary>
        /// Creates a dictionary multimap from the lookup.
        /// </summary>
        /// <typeparam name="K">The key type</typeparam>
        /// <typeparam name="V">The value type</typeparam>
        /// <param name="lookup">The ILookup instance to convert to a dictionary</param>
        /// <returns>A dictionary holding the same data as 'lookup'</returns>
        public static Dictionary<K, List<V>> ToDictionary<K, V>(this ILookup<K, V> lookup)
        {
            return lookup.ToDictionary(grouping => grouping.Key, grouping => grouping.ToList());
        }

        /// <summary>
        /// Creates a dictionary enumerable of key value pairs
        /// </summary>
        /// <typeparam name="K">The key type</typeparam>
        /// <typeparam name="V">The value type</typeparam>
        /// <param name="lookup">The IEnumerable of KeyValuePair instance to convert to a dictionary</param>
        /// <returns>A dictionary holding the same data as 'lookup'</returns>
        public static Dictionary<K, V> ToDictionary<K, V>(this IEnumerable<KeyValuePair<K, V>> lookup)
        {
            return lookup.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        /// <summary>
        /// Creates a new <see cref="HashSet{T}"/> from the elements in the specified enumerable
        /// </summary>
        /// <typeparam name="T">The item type in the hash set</typeparam>
        /// <param name="enumerable">The items to be placed into the enumerable</param>
        /// <returns>A new <see cref="HashSet{T}"/> containing the items in the enumerable</returns>
        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> enumerable)
        {
            return new HashSet<T>(enumerable);
        }

        /// <summary>
        /// Creates a new <see cref="HashSet{T}"/> from the elements in the specified enumerable
        /// </summary>
        /// <typeparam name="T">The item type of the source enumerable</typeparam>
        /// <typeparam name="TResult">The type of the items in the output <see cref="HashSet{T}"/></typeparam>
        /// <param name="enumerable">The items to be placed into the enumerable</param>
        /// <param name="selector">Selects items from the enumerable to be placed into the <see cref="HashSet{T}"/></param>
        /// <returns>A new <see cref="HashSet{T}"/> containing the items in the enumerable</returns>
        public static HashSet<TResult> ToHashSet<T, TResult>(this IEnumerable<T> enumerable, Func<T, TResult> selector)
        {
            return new HashSet<TResult>(enumerable.Select(selector));
        }

        /// <summary>
        /// Returns true if the specified enumerable is null or has no elements
        /// </summary>
        /// <typeparam name="T">The enumerable's item type</typeparam>
        /// <param name="enumerable">The enumerable to check for a value</param>
        /// <returns>True if the enumerable has elements, false otherwise</returns>
        public static bool IsNullOrEmpty<T>(this IEnumerable<T> enumerable)
        {
            return enumerable == null || !enumerable.Any();
        }

        /// <summary>
        /// Performs the specified selector before calling DefaultIfEmpty. This is just short hand for Select(selector).DefaultIfEmpty(defaultValue)
        /// </summary>
        public static IEnumerable<TResult> DefaultIfEmpty<T, TResult>(this IEnumerable<T> enumerable, Func<T, TResult> selector, TResult defaultValue = default(TResult))
        {
            return enumerable.Select(selector).DefaultIfEmpty(defaultValue);
        }

        /// <summary>
        /// Gets the median value in the collection
        /// </summary>
        /// <typeparam name="T">The item type in the collection</typeparam>
        /// <param name="enumerable">The enumerable of items to search</param>
        /// <returns>The median value, throws InvalidOperationException if no items are present</returns>
        public static T Median<T>(this IEnumerable<T> enumerable)
        {
            var collection = enumerable.ToList();
            return collection.OrderBy(x => x).Skip(collection.Count/2).First();
        }

        /// <summary>
        /// Gets the median value in the collection
        /// </summary>
        /// <typeparam name="T">The item type in the collection</typeparam>
        /// <typeparam name="TProperty">The type of the value selected</typeparam>
        /// <param name="collection">The collection of items to search</param>
        /// <param name="selector">Function used to select a value from collection items</param>
        /// <returns>The median value, throws InvalidOperationException if no items are present</returns>
        public static TProperty Median<T, TProperty>(this IEnumerable<T> collection, Func<T, TProperty> selector)
        {
            return collection.Select(selector).Median();
        }
    }
}
