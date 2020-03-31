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

using Python.Runtime;

namespace QuantConnect.Interfaces
{
    /// <summary>
    /// Represents a generic collection of key/value pairs that implements python dictionary methods.
    /// </summary>
    public interface IExtendedDictionary<TKey, TValue>
    {
        /// <summary>
        /// Removes all keys and values from the <see cref="IExtendedDictionary{TKey, TValue}"/>.
        /// </summary>
        void clear();

        /// <summary>
        /// Creates a shallow copy of the <see cref="IExtendedDictionary{TKey, TValue}"/>.
        /// </summary>
        /// <returns>Returns a shallow copy of the dictionary. It doesn't modify the original dictionary.</returns>
        PyDict copy();

        /// <summary>
        /// Creates a new dictionary from the given sequence of elements.
        /// </summary>
        /// <param name="sequence">Sequence of elements which is to be used as keys for the new dictionary</param>
        /// <returns>Returns a new dictionary with the given sequence of elements as the keys of the dictionary.</returns>
        PyDict fromkeys(TKey[] sequence);

        /// <summary>
        /// Creates a new dictionary from the given sequence of elements with a value provided by the user.
        /// </summary>
        /// <param name="sequence">Sequence of elements which is to be used as keys for the new dictionary</param>
        /// <param name="value">Value which is set to each each element of the dictionary</param>
        /// <returns>Returns a new dictionary with the given sequence of elements as the keys of the dictionary.
        /// Each element of the newly created dictionary is set to the provided value.</returns>
        PyDict fromkeys(TKey[] sequence, TValue value);

        /// <summary>
        /// Returns the value for the specified key if key is in dictionary.
        /// </summary>
        /// <param name="key">Key to be searched in the dictionary</param>
        /// <returns>The value for the specified key if key is in dictionary.
        /// None if the key is not found and value is not specified.</returns>
        TValue get(TKey key);

        /// <summary>
        /// Returns the value for the specified key if key is in dictionary.
        /// </summary>
        /// <param name="key">Key to be searched in the dictionary</param>
        /// <param name="value">Value to be returned if the key is not found. The default value is null.</param>
        /// <returns>The value for the specified key if key is in dictionary.
        /// value if the key is not found and value is specified.</returns>
        TValue get(TKey key, TValue value);

        /// <summary>
        /// Returns a view object that displays a list of dictionary's (key, value) tuple pairs.
        /// </summary>
        /// <returns>Returns a view object that displays a list of a given dictionary's (key, value) tuple pair.</returns>
        PyList items();

        /// <summary>
        /// Returns a view object that displays a list of all the keys in the dictionary
        /// </summary>
        /// <returns>Returns a view object that displays a list of all the keys.
        /// When the dictionary is changed, the view object also reflect these changes.</returns>
        PyList keys();

        /// <summary>
        /// Returns and removes an arbitrary element (key, value) pair from the dictionary.
        /// </summary>
        /// <returns>Returns an arbitrary element (key, value) pair from the dictionary
        /// removes an arbitrary element(the same element which is returned) from the dictionary.
        /// Note: Arbitrary elements and random elements are not same.The popitem() doesn't return a random element.</returns>
        PyTuple popitem();

        /// <summary>
        /// Returns the value of a key (if the key is in dictionary). If not, it inserts key with a value to the dictionary.
        /// </summary>
        /// <param name="key">Key with null/None value is inserted to the dictionary if key is not in the dictionary.</param>
        /// <returns>The value of the key if it is in the dictionary
        /// None if key is not in the dictionary</returns>
        TValue setdefault(TKey key);

        /// <summary>
        /// Returns the value of a key (if the key is in dictionary). If not, it inserts key with a value to the dictionary.
        /// </summary>
        /// <param name="key">Key with a value default_value is inserted to the dictionary if key is not in the dictionary.</param>
        /// <param name="default_value">Default value</param>
        /// <returns>The value of the key if it is in the dictionary
        /// default_value if key is not in the dictionary and default_value is specified</returns>
        TValue setdefault(TKey key, TValue default_value);

        /// <summary>
        /// Removes and returns an element from a dictionary having the given key.
        /// </summary>
        /// <param name="key">Key which is to be searched for removal</param>
        /// <returns>If key is found - removed/popped element from the dictionary
        /// If key is not found - KeyError exception is raised</returns>
        TValue pop(TKey key);

        /// <summary>
        /// Removes and returns an element from a dictionary having the given key.
        /// </summary>
        /// <param name="key">Key which is to be searched for removal</param>
        /// <param name="default_value">Value which is to be returned when the key is not in the dictionary</param>
        /// <returns>If key is found - removed/popped element from the dictionary
        /// If key is not found - value specified as the second argument(default)</returns>
        TValue pop(TKey key, TValue default_value);

        /// <summary>
        /// Updates the dictionary with the elements from the another dictionary object or from an iterable of key/value pairs.
        /// The update() method adds element(s) to the dictionary if the key is not in the dictionary.If the key is in the dictionary, it updates the key with the new value.
        /// </summary>
        /// <param name="other">Takes either a dictionary or an iterable object of key/value pairs (generally tuples).</param>
        void update(PyDict other);

        /// <summary>
        /// Returns a view object that displays a list of all the values in the dictionary.
        /// </summary>
        /// <returns>Returns a view object that displays a list of all values in a given dictionary.</returns>
        PyList values();
    }
}