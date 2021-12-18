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

using System.Collections.Generic;
using System.Linq;
using QuantConnect.Securities;

namespace QuantConnect.Data.UniverseSelection
{
    /// <summary>
    /// Defines the additions and subtractions to the algorithm's security subscriptions
    /// </summary>
    public class SecurityChanges
    {
        /// <summary>
        /// Gets an instance that represents no changes have been made
        /// </summary>
        public static readonly SecurityChanges None = new (new Dictionary<Security, bool>(), new Dictionary<Security, bool>());

        private readonly Dictionary<Security, bool> _addedSecurities;
        private readonly Dictionary<Security, bool> _removedSecurities;

        /// <summary>
        /// Gets the total count of added and removed securities
        /// </summary>
        public int Count => _addedSecurities.Count + _removedSecurities.Count;

        /// <summary>
        /// True will filter out custom securities from the
        /// <see cref="AddedSecurities"/> and <see cref="RemovedSecurities"/> properties
        /// </summary>
        /// <remarks>This allows us to filter but also to disable
        /// the filtering if desired</remarks>
        public bool FilterCustomSecurities { get; set; }

        /// <summary>
        /// True will filter out internal securities from the
        /// <see cref="AddedSecurities"/> and <see cref="RemovedSecurities"/> properties
        /// </summary>
        /// <remarks>This allows us to filter but also to disable
        /// the filtering if desired</remarks>
        public bool FilterInternalSecurities { get; set; }

        /// <summary>
        /// Gets the symbols that were added by universe selection
        /// </summary>
        /// <remarks>Will use <see cref="FilterCustomSecurities"/> value
        /// to determine if custom securities should be filtered</remarks>
        /// <remarks>Will use <see cref="FilterInternalSecurities"/> value
        /// to determine if internal securities should be filtered</remarks>
        public IReadOnlyList<Security> AddedSecurities => GetFilteredList(_addedSecurities);

        /// <summary>
        /// Gets the symbols that were removed by universe selection. This list may
        /// include symbols that were removed, but are still receiving data due to
        /// existing holdings or open orders
        /// </summary>
        /// <remarks>Will use <see cref="FilterCustomSecurities"/> value
        /// to determine if custom securities should be filtered</remarks>
        /// <remarks>Will use <see cref="FilterInternalSecurities"/> value
        /// to determine if internal securities should be filtered</remarks>
        public IReadOnlyList<Security> RemovedSecurities => GetFilteredList(_removedSecurities);

        /// <summary>
        /// Initializes a new instance of the <see cref="SecurityChanges"/> class
        /// </summary>
        /// <param name="addedSecurities">Added symbols list</param>
        /// <param name="removedSecurities">Removed symbols list</param>
        public SecurityChanges(Dictionary<Security, bool> addedSecurities, Dictionary<Security, bool> removedSecurities)
        {
            _addedSecurities = addedSecurities;
            _removedSecurities = removedSecurities;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SecurityChanges"/> class
        /// as a shallow clone of a given instance, sharing the same collections
        /// </summary>
        /// <param name="changes">The instance to clone</param>
        public SecurityChanges(SecurityChanges changes)
        {
            _addedSecurities = changes._addedSecurities;
            _removedSecurities = changes._removedSecurities;
        }

        /// <summary>
        /// Returns a new instance of <see cref="SecurityChanges"/> with the specified securities marked as added
        /// </summary>
        /// <param name="securities">The added securities</param>
        /// <remarks>Useful for testing</remarks>
        /// <returns>A new security changes instance with the specified securities marked as added</returns>
        public static SecurityChanges AddedNonInternal(params Security[] securities)
        {
            if (securities == null || securities.Length == 0) return None;
            return CreateNonInternal(securities, Enumerable.Empty<Security>());
        }

        /// <summary>
        /// Returns a new instance of <see cref="SecurityChanges"/> with the specified securities marked as removed
        /// </summary>
        /// <param name="securities">The removed securities</param>
        /// <remarks>Useful for testing</remarks>
        /// <returns>A new security changes instance with the specified securities marked as removed</returns>
        public static SecurityChanges RemovedNonInternal(params Security[] securities)
        {
            if (securities == null || securities.Length == 0) return None;
            return CreateNonInternal(Enumerable.Empty<Security>(), securities);
        }

        /// <summary>
        /// Combines the results of two <see cref="SecurityChanges"/>
        /// </summary>
        /// <param name="left">The left side of the operand</param>
        /// <param name="right">The right side of the operand</param>
        /// <returns>Adds the additions together and removes any removals found in the additions, that is, additions take precendence</returns>
        public static SecurityChanges operator +(SecurityChanges left, SecurityChanges right)
        {
            // common case is adding something to nothing, shortcut these to prevent linqness
            if (left == None || left.Count == 0) return right;
            if (right == None || right.Count == 0) return left;

            var additions = Merge(left._addedSecurities, right._addedSecurities);
            var removals = Merge(left._removedSecurities, right._removedSecurities);
            if (additions.Count > 0 && removals.Count > 0)
            {
                removals = new Dictionary<Security, bool>(
                    removals.Where(x => !additions.ContainsKey(x.Key)));
            }

            return new SecurityChanges(additions, removals);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SecurityChanges"/> class all none internal
        /// </summary>
        /// <param name="addedSecurities">Added symbols list</param>
        /// <param name="removedSecurities">Removed symbols list</param>
        /// <remarks>Useful for testing</remarks>
        public static SecurityChanges CreateNonInternal(IEnumerable<Security> addedSecurities, IEnumerable<Security> removedSecurities)
        {
            return new SecurityChanges(
                new Dictionary<Security, bool>(addedSecurities.Select(security =>
                    new KeyValuePair<Security, bool>(security, false))),
                new Dictionary<Security, bool>(removedSecurities.Select(security =>
                    new KeyValuePair<Security, bool>(security, false))));
        }

        #region Overrides of Object

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public override string ToString()
        {
            if (Count == 0)
            {
                return "SecurityChanges: None";
            }

            var added = string.Empty;
            if (AddedSecurities.Count != 0)
            {
                added = $" Added: {string.Join(",", AddedSecurities.Select(x => x.Symbol.ID))}";
            }
            var removed = string.Empty;
            if (RemovedSecurities.Count != 0)
            {
                removed = $" Removed: {string.Join(",", RemovedSecurities.Select(x => x.Symbol.ID))}";
            }

            return $"SecurityChanges: {added}{removed}";
        }

        #endregion

        /// <summary>
        /// Helper method to filter added and removed securities based on current settings
        /// </summary>
        private IReadOnlyList<Security> GetFilteredList(IReadOnlyDictionary<Security, bool> source)
        {
            return source.Where(kvp => !FilterCustomSecurities || kvp.Key.Type != SecurityType.Base)
            .Where(kvp => !FilterInternalSecurities || !kvp.Value)
            .Select(kvp => kvp.Key)
            .OrderBy(security => security.Symbol.Value)
            .ToList();
        }

        /// <summary>
        /// Helper method that will merge two security change collections
        /// </summary>
        /// <returns>Will return merged dictionary</returns>
        private static Dictionary<Security, bool> Merge(Dictionary<Security, bool> left, Dictionary<Security, bool> right)
        {
            // if right is emtpy we just use left
            var result = left;
            if (right.Count != 0)
            {
                if (left.Count == 0)
                {
                    // left is emtpy so let's just use right
                    result = right;
                }
                else
                {
                    // merge, both are not empty
                    result = new Dictionary<Security, bool>(result);
                    foreach (var kvp in right)
                    {
                        if (!result.TryGetValue(kvp.Key, out var existingIsInternal) || existingIsInternal && !kvp.Value)
                        {
                            // let's add it if not present or override it if was internal and new the other isn't
                            result[kvp.Key] = kvp.Value;
                        }
                    }

                    return result;
                }
            }
            return new Dictionary<Security, bool>(result);
        }
    }
}
