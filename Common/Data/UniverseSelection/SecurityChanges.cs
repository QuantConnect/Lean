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
using System.Linq;
using QuantConnect.Securities;
using System.Collections.Generic;

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
        public static readonly SecurityChanges None = new (Enumerable.Empty<Security>(), Enumerable.Empty<Security>(),
            Enumerable.Empty<Security>(), Enumerable.Empty<Security>());

        private readonly IReadOnlySet<Security> _addedSecurities;
        private readonly IReadOnlySet<Security> _removedSecurities;
        private readonly IReadOnlySet<Security> _internalAddedSecurities;
        private readonly IReadOnlySet<Security> _internalRemovedSecurities;

        /// <summary>
        /// Gets the total count of added and removed securities
        /// </summary>
        public int Count => _addedSecurities.Count + _removedSecurities.Count
            + _internalAddedSecurities.Count + _internalRemovedSecurities.Count;

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
        public IReadOnlyList<Security> AddedSecurities => GetFilteredList(_addedSecurities,
            !FilterInternalSecurities ? _internalAddedSecurities : null);

        /// <summary>
        /// Gets the symbols that were removed by universe selection. This list may
        /// include symbols that were removed, but are still receiving data due to
        /// existing holdings or open orders
        /// </summary>
        /// <remarks>Will use <see cref="FilterCustomSecurities"/> value
        /// to determine if custom securities should be filtered</remarks>
        /// <remarks>Will use <see cref="FilterInternalSecurities"/> value
        /// to determine if internal securities should be filtered</remarks>
        public IReadOnlyList<Security> RemovedSecurities => GetFilteredList(_removedSecurities,
            !FilterInternalSecurities ? _internalRemovedSecurities : null);

        /// <summary>
        /// Initializes a new instance of the <see cref="SecurityChanges"/> class
        /// </summary>
        /// <param name="additions">Added symbols list</param>
        /// <param name="removals">Removed symbols list</param>
        /// <param name="internalAdditions">Internal added symbols list</param>
        /// <param name="internalRemovals">Internal removed symbols list</param>
        private SecurityChanges(IEnumerable<Security> additions, IEnumerable<Security> removals,
            IEnumerable<Security> internalAdditions, IEnumerable<Security> internalRemovals)
        {
            _addedSecurities = additions.ToHashSet();
            _removedSecurities = removals.ToHashSet();
            _internalAddedSecurities = internalAdditions.ToHashSet();
            _internalRemovedSecurities = internalRemovals.ToHashSet();
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
            _internalAddedSecurities = changes._internalAddedSecurities;
            _internalRemovedSecurities = changes._internalRemovedSecurities;
        }

        /// <summary>
        /// Combines the results of two <see cref="SecurityChanges"/>
        /// </summary>
        /// <param name="left">The left side of the operand</param>
        /// <param name="right">The right side of the operand</param>
        /// <returns>Adds the additions together and removes any removals found in the additions, that is, additions take precedence</returns>
        public static SecurityChanges operator +(SecurityChanges left, SecurityChanges right)
        {
            // common case is adding something to nothing, shortcut these to prevent linqness
            if (left == None || left.Count == 0) return right;
            if (right == None || right.Count == 0) return left;

            var additions = Merge(left._addedSecurities, right._addedSecurities);
            var internalAdditions = Merge(left._internalAddedSecurities, right._internalAddedSecurities);

            var removals = Merge(left._removedSecurities, right._removedSecurities,
                security => !additions.Contains(security) && !internalAdditions.Contains(security));
            var internalRemovals = Merge(left._internalRemovedSecurities, right._internalRemovedSecurities,
                security => !additions.Contains(security) && !internalAdditions.Contains(security));

            return new SecurityChanges(additions, removals, internalAdditions, internalRemovals);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SecurityChanges"/> class all none internal
        /// </summary>
        /// <param name="additions">Added symbols list</param>
        /// <param name="removals">Removed symbols list</param>
        /// <param name="internalAdditions">Internal added symbols list</param>
        /// <param name="internalRemovals">Internal removed symbols list</param>
        /// <remarks>Useful for testing</remarks>
        public static SecurityChanges Create(IReadOnlyCollection<Security> additions, IReadOnlyCollection<Security> removals,
            IReadOnlyCollection<Security> internalAdditions, IReadOnlyCollection<Security> internalRemovals)
        {
            // return None if there's no changes, otherwise return what we've modified
            return additions?.Count + removals?.Count + internalAdditions?.Count + internalRemovals?.Count > 0
                ? new SecurityChanges(additions, removals, internalAdditions, internalRemovals)
                : None;
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
        private IReadOnlyList<Security> GetFilteredList(IReadOnlySet<Security> source, IReadOnlySet<Security> secondSource = null)
        {
            IEnumerable<Security> enumerable = source;
            if (secondSource != null && secondSource.Count > 0)
            {
                enumerable = enumerable.Union(secondSource);
            }
            return enumerable.Where(kvp => !FilterCustomSecurities || kvp.Type != SecurityType.Base)
                .Select(kvp => kvp)
                .OrderBy(security => security.Symbol.Value)
                .ToList();
        }

        /// <summary>
        /// Helper method that will merge two security sets, taken into account an optional filter
        /// </summary>
        /// <returns>Will return merged set</returns>
        private static HashSet<Security> Merge(IReadOnlyCollection<Security> left, IReadOnlyCollection<Security> right, Func<Security, bool> filter = null)
        {
            // if right is emtpy we just use left
            IEnumerable<Security> result = left;
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
                    result = result.Concat(right);
                }
            }

            if (filter != null)
            {
                result = result.Where(filter.Invoke);
            }

            return new HashSet<Security>(result);
        }
    }

    /// <summary>
    /// Helper method to create security changes
    /// </summary>
    public class SecurityChangesConstructor
    {
        private readonly List<Security> _internalAdditions =  new();
        private readonly List<Security> _internalRemovals =  new();
        private readonly List<Security> _additions = new();
        private readonly List<Security> _removals = new();

        /// <summary>
        /// Inserts a security addition change
        /// </summary>
        public void Add(Security security, bool isInternal)
        {
            if (isInternal)
            {
                _internalAdditions.Add(security);
            }
            else
            {
                _additions.Add(security);
            }
        }

        /// <summary>
        /// Inserts a security removal change
        /// </summary>
        public void Remove(Security security, bool isInternal)
        {
            if (isInternal)
            {
                _internalRemovals.Add(security);
            }
            else
            {
                _removals.Add(security);
            }
        }

        /// <summary>
        /// Get the current security changes clearing state
        /// </summary>
        public SecurityChanges Flush()
        {
            var result = SecurityChanges.Create(_additions, _removals, _internalAdditions, _internalRemovals);

            _internalAdditions.Clear();
            _removals.Clear();
            _internalRemovals.Clear();
            _additions.Clear();

            return result;
        }
    }
}
