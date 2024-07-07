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
using System.Linq;

namespace QuantConnect.Securities.Positions
{
    /// <summary>
    /// Provides a collection type for <see cref="IPosition"/> aimed at providing indexing for
    /// common operations required by the resolver implementations.
    /// </summary>
    public class PositionCollection : IEnumerable<IPosition>
    {
        private Dictionary<Symbol, IPosition> _positions;

        /// <summary>Gets the number of elements in the collection.</summary>
        /// <returns>The number of elements in the collection. </returns>
        public int Count => _positions.Count;

        /// <summary>
        /// Initializes a new instance of the <see cref="PositionCollection"/> class
        /// </summary>
        /// <param name="positions">The positions to include in this collection</param>
        public PositionCollection(Dictionary<Symbol, IPosition> positions)
        {
            _positions = positions;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PositionCollection"/> class
        /// </summary>
        /// <param name="positions">The positions to include in this collection</param>
        public PositionCollection(IEnumerable<IPosition> positions)
            : this(positions.ToDictionary(p => p.Symbol)) { }

        /// <summary>
        /// Removes the quantities in the provided groups from this position collection.
        /// This should be called following <see cref="IPositionGroupResolver"/> has resolved
        /// position groups in order to update the collection of positions for the next resolver,
        /// if one exists.
        /// </summary>
        /// <param name="groups">The resolved position groups</param>
        /// <returns></returns>
        public void Remove(IEnumerable<IPositionGroup> groups)
        {
            foreach (var group in groups)
            {
                foreach (var position in group.Positions)
                {
                    IPosition existing;
                    if (!_positions.TryGetValue(position.Symbol, out existing))
                    {
                        throw new InvalidOperationException(
                            $"Position with symbol {position.Symbol} not found."
                        );
                    }

                    var resultingPosition = existing.Deduct(position.Quantity);
                    // directly remove positions hows quantity is 0
                    if (resultingPosition.Quantity == 0)
                    {
                        _positions.Remove(position.Symbol);
                    }
                    else
                    {
                        _positions[position.Symbol] = resultingPosition;
                    }
                }
            }
        }

        /// <summary>
        /// Clears this collection of all positions
        /// </summary>
        public void Clear()
        {
            _positions.Clear();
        }

        /// <summary>
        /// Attempts to retrieve the position with the specified symbol from this collection
        /// </summary>
        /// <param name="symbol">The symbol</param>
        /// <param name="position">The position</param>
        /// <returns>True if the position is found, otherwise false</returns>
        public bool TryGetPosition(Symbol symbol, out IPosition position)
        {
            return _positions.TryGetValue(symbol, out position);
        }

        /// <summary>Returns an enumerator that iterates through the collection.</summary>
        /// <returns>An enumerator that can be used to iterate through the collection.</returns>
        public IEnumerator<IPosition> GetEnumerator()
        {
            return _positions.Values.GetEnumerator();
        }

        /// <summary>Returns an enumerator that iterates through a collection.</summary>
        /// <returns>An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
