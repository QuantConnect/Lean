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
using System.Collections.Generic;

namespace QuantConnect.Securities.Positions
{
    /// <summary>
    /// Provides an implementation of <see cref="IPositionGroupResolver"/> that invokes multiple wrapped implementations
    /// in succession. Each successive call to <see cref="IPositionGroupResolver.Resolve"/> will receive
    /// the remaining positions that have yet to be grouped. Any non-grouped positions are placed into identity groups.
    /// </summary>
    public class CompositePositionGroupResolver : IPositionGroupResolver
    {
        /// <summary>
        /// Gets the count of registered resolvers
        /// </summary>
        public int Count => _resolvers.Count;

        private readonly List<IPositionGroupResolver> _resolvers;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompositePositionGroupResolver"/> class
        /// </summary>
        /// <param name="resolvers">The position group resolvers to be invoked in order</param>
        public CompositePositionGroupResolver(params IPositionGroupResolver[] resolvers)
            : this((IEnumerable<IPositionGroupResolver>)resolvers)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CompositePositionGroupResolver"/> class
        /// </summary>
        /// <param name="resolvers">The position group resolvers to be invoked in order</param>
        public CompositePositionGroupResolver(IEnumerable<IPositionGroupResolver> resolvers)
        {
            _resolvers = resolvers.ToList();
        }

        /// <summary>
        /// Adds the specified <paramref name="resolver"/> to the end of the list of resolvers. This resolver will run last.
        /// </summary>
        /// <param name="resolver">The resolver to be added</param>
        public void Add(IPositionGroupResolver resolver)
        {
            _resolvers.Add(resolver);
        }

        /// <summary>
        /// Inserts the specified <paramref name="resolver"/> into the list of resolvers at the specified index.
        /// </summary>
        /// <param name="resolver">The resolver to be inserted</param>
        /// <param name="index">The zero based index indicating where to insert the resolver, zero inserts to the beginning
        /// of the list making this resolver un first and <see cref="Count"/> inserts the resolver to the end of the list
        /// making this resolver run last</param>
        public void Add(IPositionGroupResolver resolver, int index)
        {
            // insert handles bounds checking
            _resolvers.Insert(index, resolver);
        }

        /// <summary>
        /// Removes the specified <paramref name="resolver"/> from the list of resolvers
        /// </summary>
        /// <param name="resolver">The resolver to be removed</param>
        /// <returns>True if the resolver was removed, false if it wasn't found in the list</returns>
        public bool Remove(IPositionGroupResolver resolver)
        {
            return _resolvers.Remove(resolver);
        }

        /// <summary>
        /// Resolves the optimal set of <see cref="IPositionGroup"/> from the provided <paramref name="positions"/>.
        /// Implementations are required to deduct grouped positions from the <paramref name="positions"/> collection.
        /// </summary>
        public PositionGroupCollection Resolve(PositionCollection positions)
        {
            // we start with no groups, each resolver's result will get merged in
            var groups = PositionGroupCollection.Empty;

            // each call to ResolvePositionGroups is expected to deduct grouped positions from the PositionCollection
            foreach (var resolver in _resolvers)
            {
                var resolved = resolver.Resolve(positions);
                groups = groups.CombineWith(resolved);
            }

            if (positions.Count > 0)
            {
                throw new InvalidOperationException("All positions must be resolved into groups.");
            }

            return groups;
        }

        /// <summary>
        /// Attempts to group the specified positions into a new <see cref="IPositionGroup"/> using an
        /// appropriate <see cref="IPositionGroupBuyingPowerModel"/> for position groups created via this
        /// resolver.
        /// </summary>
        /// <param name="newPositions">The positions to be grouped</param>
        /// <param name="currentPositions">The currently grouped positions</param>
        /// <param name="group">The grouped positions when this resolver is able to, otherwise null</param>
        /// <returns>True if this resolver can group the specified positions, otherwise false</returns>
        public bool TryGroup(IReadOnlyList<IPosition> newPositions, PositionGroupCollection currentPositions, out IPositionGroup group)
        {
            foreach (var resolver in _resolvers)
            {
                if (resolver.TryGroup(newPositions, currentPositions, out group))
                {
                    return true;
                }
            }

            group = null;
            return false;
        }

        /// <summary>
        /// Determines the position groups that would be evaluated for grouping of the specified
        /// positions were passed into the <see cref="Resolve"/> method.
        /// </summary>
        /// <remarks>
        /// This function allows us to determine a set of impacted groups and run the resolver on just
        /// those groups in order to support what-if analysis
        /// </remarks>
        /// <param name="groups">The existing position groups</param>
        /// <param name="positions">The positions being changed</param>
        /// <returns>An enumerable containing the position groups that could be impacted by the specified position changes</returns>
        public IEnumerable<IPositionGroup> GetImpactedGroups(PositionGroupCollection groups, IReadOnlyCollection<IPosition> positions)
        {
            foreach (var resolver in _resolvers)
            {
                var seen = new HashSet<PositionGroupKey>();
                foreach (var group in resolver.GetImpactedGroups(groups, positions))
                {
                    if (seen.Add(group.Key))
                    {
                        yield return group;
                    }
                }
            }
        }
    }
}
