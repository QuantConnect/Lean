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
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="dictionary"></param>
        /// <param name="changes"></param>
        /// <param name="keyFactory"></param>
        /// <param name="valueFactory"></param>
        public static void UpdateDictionary<TKey, TValue>(
            IDictionary<TKey, TValue> dictionary,
            SecurityChanges changes,
            Func<Security, TKey> keyFactory,
            Func<Security, TValue> valueFactory
            )
        {
            Update(changes,
                added => dictionary.Add(keyFactory(added), valueFactory(added)),
                removed => dictionary.Remove(keyFactory(removed))
            );
        }

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