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
using System.Collections.Specialized;

namespace QuantConnect.Util
{
    /// <summary>
    /// Provides a thread-safe set collection that mimics the behavior of <see cref="HashSet{T}"/>
    /// and will be keep insertion order
    /// </summary>
    /// <typeparam name="T">The item type</typeparam>
    public class ConcurrentSet<T> : ISet<T>
    {
        private readonly OrderedDictionary _set = new OrderedDictionary();
        // for performance we will keep a enumerator list which we will refresh only if required
        private readonly List<T> _enumerator = new List<T>();
        private bool _refreshEnumerator;

        /// <summary>Gets the number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1" />.</summary>
        /// <returns>The number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1" />.</returns>
        public int Count
        {
            get
            {
                lock (_set)
                {
                    return _set.Count;
                }
            }
        }

        /// <summary>Gets a value indicating whether the <see cref="T:System.Collections.Generic.ICollection`1" /> is read-only.</summary>
        /// <returns>true if the <see cref="T:System.Collections.Generic.ICollection`1" /> is read-only; otherwise, false.</returns>
        public bool IsReadOnly => false;

        /// <summary>Adds an item to the <see cref="T:System.Collections.Generic.ICollection`1" />.</summary>
        /// <param name="item">The object to add to the <see cref="T:System.Collections.Generic.ICollection`1" />.</param>
        /// <exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.ICollection`1" /> is read-only.</exception>
        public void Add(T item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            lock (_set)
            {
                AddImpl(item);
            }
        }

        /// <summary>Modifies the current set so that it contains all elements that are present in either the current set or the specified collection.</summary>
        /// <param name="other">The collection to compare to the current set.</param>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="other" /> is null.</exception>
        public void UnionWith(IEnumerable<T> other)
        {
            var otherSet = other.ToHashSet();

            lock (_set)
            {
                foreach (var item in otherSet)
                {
                    // wont overwrite existing references of same key
                    AddImpl(item);
                }
            }
        }

        /// <summary>Modifies the current set so that it contains only elements that are also in a specified collection.</summary>
        /// <param name="other">The collection to compare to the current set.</param>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="other" /> is null.</exception>
        public void IntersectWith(IEnumerable<T> other)
        {
            var otherSet = other.ToHashSet();

            lock (_set)
            {
                // remove items in '_set' that are not in 'other'
                // enumerating this and not '_set' so its safe to modify
                foreach (var item in this)
                {
                    if (!otherSet.Contains(item))
                    {
                        RemoveImpl(item);
                    }
                }
            }
        }

        /// <summary>Removes all elements in the specified collection from the current set.</summary>
        /// <param name="other">The collection of items to remove from the set.</param>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="other" /> is null.</exception>
        public void ExceptWith(IEnumerable<T> other)
        {
            var otherSet = other.ToHashSet();

            lock (_set)
            {
                // remove items from 'other'
                foreach (var item in otherSet)
                {
                    RemoveImpl(item);
                }
            }
        }

        /// <summary>Modifies the current set so that it contains only elements that are present either in the current set or in the specified collection, but not both. </summary>
        /// <param name="other">The collection to compare to the current set.</param>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="other" /> is null.</exception>
        public void SymmetricExceptWith(IEnumerable<T> other)
        {
            var otherSet = other.ToHashSet();

            lock (_set)
            {
                foreach (var item in otherSet)
                {
                    if (!AddImpl(item))
                    {
                        // remove items in both collections
                        RemoveImpl(item);
                    }
                }
            }
        }

        /// <summary>Determines whether a set is a subset of a specified collection.</summary>
        /// <returns>true if the current set is a subset of <paramref name="other" />; otherwise, false.</returns>
        /// <param name="other">The collection to compare to the current set.</param>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="other" /> is null.</exception>
        public bool IsSubsetOf(IEnumerable<T> other)
        {
            var otherSet = other.ToHashSet();
            lock (_set)
            {
                foreach (var item in otherSet)
                {
                    if (!_set.Contains(item))
                    {
                        return false;
                    }
                }

                // non-strict subset can be equal
                return _set.Keys.Count == 0;
            }
        }

        /// <summary>Determines whether the current set is a superset of a specified collection.</summary>
        /// <returns>true if the current set is a superset of <paramref name="other" />; otherwise, false.</returns>
        /// <param name="other">The collection to compare to the current set.</param>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="other" /> is null.</exception>
        public bool IsSupersetOf(IEnumerable<T> other)
        {
            var otherSet = other.ToHashSet();
            lock (_set)
            {
                foreach (DictionaryEntry item in _set)
                {
                    if (!otherSet.Remove((T)item.Key))
                    {
                        return false;
                    }
                }
            }

            // non-strict superset can be equal
            return true;
        }

        /// <summary>Determines whether the current set is a proper (strict) superset of a specified collection.</summary>
        /// <returns>true if the current set is a proper superset of <paramref name="other" />; otherwise, false.</returns>
        /// <param name="other">The collection to compare to the current set. </param>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="other" /> is null.</exception>
        public bool IsProperSupersetOf(IEnumerable<T> other)
        {
            var hasOther = false;
            var otherSet = other.ToHashSet();
            lock (_set)
            {
                foreach (DictionaryEntry item in _set)
                {
                    if (!otherSet.Remove((T)item.Key))
                    {
                        return false;
                    }

                    hasOther = true;
                }
            }

            // to be a strict superset, _set must contain extra elements and contain all of other
            return hasOther;
        }

        /// <summary>Determines whether the current set is a proper (strict) subset of a specified collection.</summary>
        /// <returns>true if the current set is a proper subset of <paramref name="other" />; otherwise, false.</returns>
        /// <param name="other">The collection to compare to the current set.</param>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="other" /> is null.</exception>
        public bool IsProperSubsetOf(IEnumerable<T> other)
        {
            var hasOther = false;
            var otherSet = other.ToHashSet();
            lock (_set)
            {
                foreach (var item in otherSet)
                {
                    if (!_set.Contains(item))
                    {
                        return false;
                    }

                    hasOther = true;
                }

                // to be a strict subset, other must contain extra elements and _set must contain all of other
                return hasOther && _set.Keys.Count == 0;
            }
        }

        /// <summary>Determines whether the current set overlaps with the specified collection.</summary>
        /// <returns>true if the current set and <paramref name="other" /> share at least one common element; otherwise, false.</returns>
        /// <param name="other">The collection to compare to the current set.</param>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="other" /> is null.</exception>
        public bool Overlaps(IEnumerable<T> other)
        {
            var otherSet = other.ToHashSet();

            lock (_set)
            {
                foreach (var item in otherSet)
                {
                    if (_set.Contains(item))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>Determines whether the current set and the specified collection contain the same elements.</summary>
        /// <returns>true if the current set is equal to <paramref name="other" />; otherwise, false.</returns>
        /// <param name="other">The collection to compare to the current set.</param>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="other" /> is null.</exception>
        public bool SetEquals(IEnumerable<T> other)
        {
            var otherSet = other.ToHashSet();
            lock (_set)
            {
                foreach (DictionaryEntry item in _set)
                {
                    if (!otherSet.Remove((T) item.Key))
                    {
                        return false;
                    }
                }
            }

            return otherSet.Count == 0;
        }

        /// <summary>Adds an element to the current set and returns a value to indicate if the element was successfully added. </summary>
        /// <returns>true if the element is added to the set; false if the element is already in the set.</returns>
        /// <param name="item">The element to add to the set.</param>
        bool ISet<T>.Add(T item)
        {
            lock (_set)
            {
                return AddImpl(item);
            }
        }

        /// <summary>Removes all items from the <see cref="T:System.Collections.Generic.ICollection`1" />.</summary>
        /// <exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.ICollection`1" /> is read-only. </exception>
        public void Clear()
        {
            lock (_set)
            {
                _refreshEnumerator = true;
                _set.Clear();
            }
        }

        /// <summary>Determines whether the <see cref="T:System.Collections.Generic.ICollection`1" /> contains a specific value.</summary>
        /// <returns>true if <paramref name="item" /> is found in the <see cref="T:System.Collections.Generic.ICollection`1" />; otherwise, false.</returns>
        /// <param name="item">The object to locate in the <see cref="T:System.Collections.Generic.ICollection`1" />.</param>
        public bool Contains(T item)
        {
            lock (_set)
            {
                return item != null && _set.Contains(item);
            }
        }

        /// <summary>Copies the elements of the <see cref="T:System.Collections.Generic.ICollection`1" /> to an <see cref="T:System.Array" />, starting at a particular <see cref="T:System.Array" /> index.</summary>
        /// <param name="array">The one-dimensional <see cref="T:System.Array" /> that is the destination of the elements copied from <see cref="T:System.Collections.Generic.ICollection`1" />. The <see cref="T:System.Array" /> must have zero-based indexing.</param>
        /// <param name="arrayIndex">The zero-based index in <paramref name="array" /> at which copying begins.</param>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="array" /> is null.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// <paramref name="arrayIndex" /> is less than 0.</exception>
        /// <exception cref="T:System.ArgumentException">The number of elements in the source <see cref="T:System.Collections.Generic.ICollection`1" /> is greater than the available space from <paramref name="arrayIndex" /> to the end of the destination <paramref name="array" />.</exception>
        public void CopyTo(T[] array, int arrayIndex)
        {
            lock (_set)
            {
                foreach (DictionaryEntry item in _set)
                {
                    array[arrayIndex++] = (T) item.Key;
                }
            }
        }

        /// <summary>Removes the first occurrence of a specific object from the <see cref="T:System.Collections.Generic.ICollection`1" />.</summary>
        /// <returns>true if <paramref name="item" /> was successfully removed from the <see cref="T:System.Collections.Generic.ICollection`1" />; otherwise, false. This method also returns false if <paramref name="item" /> is not found in the original <see cref="T:System.Collections.Generic.ICollection`1" />.</returns>
        /// <param name="item">The object to remove from the <see cref="T:System.Collections.Generic.ICollection`1" />.</param>
        /// <exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.ICollection`1" /> is read-only.</exception>
        public bool Remove(T item)
        {
            lock (_set)
            {
                if (item != null && _set.Contains(item))
                {
                    RemoveImpl(item);
                    return true;
                }
                return false;
            }
        }

        /// <summary>Returns an enumerator that iterates through the collection.</summary>
        /// <remarks>For thread safety will return a snapshot of the collection</remarks>
        /// <returns>A <see cref="T:System.Collections.Generic.IEnumerator`1" /> that can be used to iterate through the collection.</returns>
        /// <filterpriority>1</filterpriority>
        public IEnumerator<T> GetEnumerator()
        {
            lock (_set)
            {
                if (_refreshEnumerator)
                {
                    _enumerator.Clear();
                    foreach (DictionaryEntry item in _set)
                    {
                        _enumerator.Add((T)item.Key);
                    }

                    _refreshEnumerator = false;
                }
                return _enumerator.GetEnumerator();
            }
        }

        /// <summary>Returns an enumerator that iterates through a collection.</summary>
        /// <remarks>For thread safety will return a snapshot of the collection</remarks>
        /// <returns>An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.</returns>
        /// <filterpriority>2</filterpriority>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private bool AddImpl(T item)
        {
            if (!_set.Contains(item))
            {
                _refreshEnumerator = true;
                _set.Add(item, item);
                return true;
            }

            return false;
        }

        private void RemoveImpl(T item)
        {
            _refreshEnumerator = true;
            _set.Remove(item);
        }
    }
}
