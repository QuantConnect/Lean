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

using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

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
        public static PositionGroupCollection Empty { get; } = new PositionGroupCollection(
            ImmutableDictionary<PositionGroupKey, IPositionGroup>.Empty,
            ImmutableDictionary<Symbol, ImmutableList<IPositionGroup>>.Empty
        );

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

        private bool? _hasNonDefaultGroups;
        private readonly ImmutableDictionary<PositionGroupKey, IPositionGroup> _groups;
        private readonly ImmutableDictionary<Symbol, ImmutableList<IPositionGroup>> _groupsBySymbol;

        /// <summary>
        /// Initializes a new instance of the <see cref="PositionGroupCollection"/> class
        /// </summary>
        /// <param name="groups">The position groups keyed by their group key</param>
        /// <param name="groupsBySymbol">The position groups keyed by the symbol of each position</param>
        public PositionGroupCollection(
            ImmutableDictionary<PositionGroupKey, IPositionGroup> groups,
            ImmutableDictionary<Symbol, ImmutableList<IPositionGroup>> groupsBySymbol
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
            _groups = groups.ToImmutableDictionary(g => g.Key);
            _groupsBySymbol = groups.SelectMany(group =>
                    group.Select(position => new {position.Symbol, group})
                )
                .GroupBy(item => item.Symbol)
                .ToImmutableDictionary(
                    item => item.Key,
                    item => item.Select(x => x.group).ToImmutableList()
                );
        }

        public PositionGroupCollection Add(IPositionGroup group)
        {
            var bySymbol = _groupsBySymbol;
            foreach (var position in group)
            {
                ImmutableList<IPositionGroup> groups;
                if (!_groupsBySymbol.TryGetValue(position.Symbol, out groups))
                {
                    groups = ImmutableList<IPositionGroup>.Empty;
                }

                bySymbol = _groupsBySymbol.SetItem(position.Symbol, groups.Add(group));
            }

            return new PositionGroupCollection(_groups.Add(group.Key, group), bySymbol);
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
            ImmutableList<IPositionGroup> list;
            if (_groupsBySymbol.TryGetValue(symbol, out list) && list?.IsEmpty == false)
            {
                groups = list;
                return true;
            }

            groups = ImmutableArray<IPositionGroup>.Empty;
            return false;
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
