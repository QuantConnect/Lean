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
using QuantConnect.Util;

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
        public static readonly SecurityChanges None = new SecurityChanges(new List<Security>(), new List<Security>());

        private readonly HashSet<Security> _addedSecurities;
        private readonly HashSet<Security> _removedSecurities;

        /// <summary>
        /// Gets the total count of added and removed securities
        /// </summary>
        public int Count => _addedSecurities.Count + _removedSecurities.Count;

        /// <summary>
        /// Gets the symbols that were added by universe selection
        /// </summary>
        public IReadOnlyList<Security> AddedSecurities
        {
            get { return _addedSecurities.OrderBy(x => x.Symbol.Value).ToList(); }
        }

        /// <summary>
        /// Gets the symbols that were removed by universe selection. This list may
        /// include symbols that were removed, but are still receiving data due to
        /// existing holdings or open orders
        /// </summary>
        public IReadOnlyList<Security> RemovedSecurities
        {
            get { return _removedSecurities.OrderBy(x => x.Symbol.Value).ToList(); }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SecurityChanges"/> class
        /// </summary>
        /// <param name="addedSecurities">Added symbols list</param>
        /// <param name="removedSecurities">Removed symbols list</param>
        public SecurityChanges(IEnumerable<Security> addedSecurities, IEnumerable<Security> removedSecurities)
        {
            _addedSecurities = addedSecurities.ToHashSet();
            _removedSecurities = removedSecurities.ToHashSet();
        }

        /// <summary>
        /// Returns a new instance of <see cref="SecurityChanges"/> with the specified securities marked as added
        /// </summary>
        /// <param name="securities">The added securities</param>
        /// <returns>A new security changes instance with the specified securities marked as added</returns>
        public static SecurityChanges Added(params Security[] securities)
        {
            if (securities == null || securities.Length == 0) return None;
            return new SecurityChanges(securities.ToList(), new List<Security>());
        }

        /// <summary>
        /// Returns a new instance of <see cref="SecurityChanges"/> with the specified securities marked as removed
        /// </summary>
        /// <param name="securities">The removed securities</param>
        /// <returns>A new security changes instance with the specified securities marked as removed</returns>
        public static SecurityChanges Removed(params Security[] securities)
        {
            if (securities == null || securities.Length == 0) return None;
            return new SecurityChanges(new List<Security>(), securities.ToList());
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
            if (left == None) return right;
            if (right == None) return left;

            var additions = left.AddedSecurities.Union(right.AddedSecurities).ToList();
            var removals = left.RemovedSecurities.Union(right.RemovedSecurities).Where(x => !additions.Contains(x)).ToList();
            return new SecurityChanges(additions, removals);
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
    }
}