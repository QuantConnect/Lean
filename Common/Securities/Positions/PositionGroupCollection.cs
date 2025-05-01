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

using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace QuantConnect.Securities.Positions
{
    /// <summary>
    /// Provides a collection type for <see cref="IPositionGroup"/>
    /// </summary>
    public class PositionGroupCollection : IReadOnlyCollection<IPositionGroup>
    {
        /// <summary>
        /// Gets an empty instance of the <see cref="PositionGroupCollection"/> class
        /// </summary>
        public static PositionGroupCollection Empty => new(new Dictionary<PositionGroupKey, IPositionGroup>(), new Dictionary<Symbol, HashSet<IPositionGroup>>());

        /// <summary>
        /// Gets the number of positions in this group
        /// </summary>
        public int Count => _groups.Count;

        /// <summary>
        /// Gets whether or not this collection contains only default position groups
        /// </summary>
        public bool IsOnlyDefaultGroups
        {
            get
            {
                if (_hasNonDefaultGroups == null)
                {
                    _hasNonDefaultGroups = _groups.Count == 0 || _groups.All(grp => grp.Key.IsDefaultGroup);
                }

                return _hasNonDefaultGroups.Value;
            }
        }

        /// <summary>
        /// Gets the position groups keys in this collection
        /// </summary>
        public IReadOnlyCollection<PositionGroupKey> Keys => _groups.Keys;

        /// <summary>
        /// Gets the position groups in this collection
        /// </summary>
        public IReadOnlyCollection<IPositionGroup> Values => _groups.Values;

        private bool? _hasNonDefaultGroups;
        private readonly Dictionary<PositionGroupKey, IPositionGroup> _groups;
        private readonly Dictionary<Symbol, HashSet<IPositionGroup>> _groupsBySymbol;

        internal IEnumerable<KeyValuePair<PositionGroupKey, IPositionGroup>> GetGroups() => _groups;

        /// <summary>
        /// Initializes a new instance of the <see cref="PositionGroupCollection"/> class
        /// </summary>
        /// <param name="groups">The position groups keyed by their group key</param>
        /// <param name="groupsBySymbol">The position groups keyed by the symbol of each position</param>
        public PositionGroupCollection(
            Dictionary<PositionGroupKey, IPositionGroup> groups,
            Dictionary<Symbol, HashSet<IPositionGroup>> groupsBySymbol
            )
        {
            _groups = groups;
            _groupsBySymbol = groupsBySymbol;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PositionGroupCollection"/> class
        /// </summary>
        /// <param name="groups">The position groups</param>
        public PositionGroupCollection(IReadOnlyCollection<IPositionGroup> groups)
        {
            _groups = new();
            _groupsBySymbol = new();
            foreach (var group in groups)
            {
                Add(group);
            }
        }

        /// <summary>
        /// Creates a new <see cref="PositionGroupCollection"/> that contains all of the position groups
        /// in this collection in addition to the specified <paramref name="group"/>. If a group with the
        /// same key already exists then it is overwritten.
        /// </summary>
        public PositionGroupCollection Add(IPositionGroup group)
        {
            foreach (var position in group)
            {
                if (!_groupsBySymbol.TryGetValue(position.Symbol, out var groups))
                {
                    _groupsBySymbol[position.Symbol] = groups = new();
                }
                groups.Add(group);
            }
            _groups[group.Key] = group;

            return this;
        }

        /// <summary>
        /// Determines whether or not a group with the specified key exists in this collection
        /// </summary>
        /// <param name="key">The group key to search for</param>
        /// <returns>True if a group with the specified key was found, false otherwise</returns>
        public bool Contains(PositionGroupKey key)
        {
            return _groups.ContainsKey(key);
        }

        /// <summary>
        /// Gets the <see cref="IPositionGroup"/> matching the specified key. If one does not exist, then an empty
        /// group is returned matching the unit quantities defined in the <paramref name="key"/>
        /// </summary>
        /// <param name="key">The position group key to search for</param>
        /// <returns>The position group matching the specified key, or a new empty group if no matching group is found.</returns>
        public IPositionGroup this[PositionGroupKey key]
        {
            get
            {
                IPositionGroup group;
                if (!TryGetGroup(key, out group))
                {
                    return new PositionGroup(key, 0m, key.CreateEmptyPositions());
                }

                return group;
            }
        }

        /// <summary>
        /// Attempts to retrieve the group with the specified key
        /// </summary>
        /// <param name="key">The group key to search for</param>
        /// <param name="group">The position group</param>
        /// <returns>True if group with key found, otherwise false</returns>
        public bool TryGetGroup(PositionGroupKey key, out IPositionGroup group)
        {
            return _groups.TryGetValue(key, out group);
        }

        /// <summary>
        /// Attempts to retrieve all groups that contain the provided symbol
        /// </summary>
        /// <param name="symbol">The symbol</param>
        /// <param name="groups">The groups if any were found, otherwise null</param>
        /// <returns>True if groups were found for the specified symbol, otherwise false</returns>
        public bool TryGetGroups(Symbol symbol, out IReadOnlyCollection<IPositionGroup> groups)
        {
            HashSet<IPositionGroup> list;
            if (_groupsBySymbol.TryGetValue(symbol, out list) && list?.Count > 0)
            {
                groups = list;
                return true;
            }

            groups = null;
            return false;
        }

        /// <summary>
        /// Merges this position group collection with the provided <paramref name="other"/> collection.
        /// </summary>
        public PositionGroupCollection CombineWith(PositionGroupCollection other)
        {
            if(other.Count == 0)
            {
                return this;
            }
            if (Count == 0)
            {
                return other;
            }

            var result = this;
            foreach (var positionGroup in other)
            {
                result = result.Add(positionGroup);
            }

            return result;
        }

        /// <summary>Returns an enumerator that iterates through the collection.</summary>
        /// <returns>An enumerator that can be used to iterate through the collection.</returns>
        public IEnumerator<IPositionGroup> GetEnumerator()
        {
            return _groups.Values.GetEnumerator();
        }

        /// <summary>Returns an enumerator that iterates through a collection.</summary>
        /// <returns>An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
