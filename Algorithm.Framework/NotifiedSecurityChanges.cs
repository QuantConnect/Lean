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
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Securities;
using QuantConnect.Util;

namespace QuantConnect.Algorithm.Framework
{
    /// <summary>
    /// Provides convenience methods for updating collections in responses to securities changed events
    /// </summary>
    public static class NotifiedSecurityChanges
    {
        /// <summary>
        /// Adds and removes the security changes to/from the collection
        /// </summary>
        /// <param name="securities">The securities collection to be updated with the changes</param>
        /// <param name="changes">The changes to be applied to the securities collection</param>
        public static void UpdateCollection(ICollection<Security> securities, SecurityChanges changes)
        {
            Update(changes, securities.Add, removed => securities.Remove(removed));
        }

        /// <summary>
        /// Adds and removes the security changes to/from the collection
        /// </summary>
        /// <param name="securities">The securities collection to be updated with the changes</param>
        /// <param name="changes">The changes to be applied to the securities collection</param>
        /// <param name="valueFactory">Delegate used to create instances of <typeparamref name="TValue"/> from a <see cref="Security"/> object</param>
        public static void UpdateCollection<TValue>(ICollection<TValue> securities, SecurityChanges changes, Func<Security, TValue> valueFactory)
        {
            Update(changes, added => securities.Add(valueFactory(added)), removed => securities.Remove(valueFactory(removed)));
        }

        /// <summary>
        /// Adds and removes the security changes to/from the collection
        /// </summary>
        /// <param name="dictionary">The securities collection to be updated with the changes</param>
        /// <param name="changes">The changes to be applied to the securities collection</param>
        /// <param name="valueFactory">Factory for creating dictonary values for a key</param>
        public static void UpdateDictionary<TValue>(
            IDictionary<Security, TValue> dictionary,
            SecurityChanges changes,
            Func<Security, TValue> valueFactory
            )
        {
            UpdateDictionary(dictionary, changes, security => security, valueFactory);
        }

        /// <summary>
        /// Adds and removes the security changes to/from the collection
        /// </summary>
        /// <param name="dictionary">The securities collection to be updated with the changes</param>
        /// <param name="changes">The changes to be applied to the securities collection</param>
        /// <param name="valueFactory">Factory for creating dictonary values for a key</param>
        public static void UpdateDictionary<TValue>(
            IDictionary<Symbol, TValue> dictionary,
            SecurityChanges changes,
            Func<Security, TValue> valueFactory
            )
        {
            UpdateDictionary(dictionary, changes, security => security.Symbol, valueFactory);
        }

        /// <summary>
        /// Most generic form of <see cref="UpdateCollection"/>
        /// </summary>
        /// <typeparam name="TKey">The dictionary's key type</typeparam>
        /// <typeparam name="TValue">The dictionary's value type</typeparam>
        /// <param name="dictionary">The dictionary to update</param>
        /// <param name="changes">The <seealso cref="SecurityChanges"/> to apply to the dictionary</param>
        /// <param name="keyFactory">Selector pulling <typeparamref name="TKey"/> from a <seealso cref="Security"/></param>
        /// <param name="valueFactory">Selector pulling <typeparamref name="TValue"/> from a <seealso cref="Security"/></param>
        public static void UpdateDictionary<TKey, TValue>(
            IDictionary<TKey, TValue> dictionary,
            SecurityChanges changes,
            Func<Security, TKey> keyFactory,
            Func<Security, TValue> valueFactory
            )
        {
            Update(changes,
                added => dictionary.Add(keyFactory(added), valueFactory(added)),
                removed =>
                {
                    var key = keyFactory(removed);
                    var entry = dictionary[key];
                    dictionary.Remove(key);

                    // give the entry a chance to clean up after itself
                    var disposable = entry as IDisposable;
                    disposable.DisposeSafely();
                });
        }

        /// <summary>
        /// Invokes the provided <paramref name="add"/> and <paramref name="remove"/> functions for each
        /// <seealso cref="SecurityChanges.Added"/> and <seealso cref="SecurityChanges.Removed"/>, respectively
        /// </summary>
        /// <param name="changes">The security changes to process</param>
        /// <param name="add">Function called for each added security</param>
        /// <param name="remove">Function called for each removed security</param>
        public static void Update(SecurityChanges changes, Action<Security> add, Action<Security> remove)
        {
            foreach (var added in changes.AddedSecurities)
            {
                add(added);
            }
            foreach (var removed in changes.RemovedSecurities)
            {
                remove(removed);
            }
        }
    }
}