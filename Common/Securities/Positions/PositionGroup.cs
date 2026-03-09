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
using System;
using QuantConnect.Securities.Option;

namespace QuantConnect.Securities.Positions
{
    /// <summary>
    /// Provides a default implementation of <see cref="IPositionGroup"/>
    /// </summary>
    public class PositionGroup : IPositionGroup
    {
        /// <summary>
        /// Gets the number of positions in the group
        /// </summary>
        public int Count => _positions.Count;

        /// <summary>
        /// Gets the key identifying this group
        /// </summary>
        public PositionGroupKey Key { get; }

        /// <summary>
        /// Gets the whole number of units in this position group
        /// </summary>
        public decimal Quantity { get; }

        /// <summary>
        /// Gets the positions in this group
        /// </summary>
        public IEnumerable<IPosition> Positions => _positions.Values;

        /// <summary>
        /// Gets the buying power model defining how margin works in this group
        /// </summary>
        public IPositionGroupBuyingPowerModel BuyingPowerModel => Key.BuyingPowerModel;

        private readonly Dictionary<Symbol, IPosition> _positions;

        /// <summary>
        /// Initializes a new instance of the <see cref="PositionGroup"/> class
        /// </summary>
        /// <param name="buyingPowerModel">The buying power model to use for this group</param>
        /// <param name="quantity">The group quantity, which must be the ratio of quantity to unit quantity of each position</param>
        /// <param name="positions">The positions comprising this group</param>
        /// <exception cref="ArgumentException">Thrown when the quantity is not the ratio of quantity to unit quantity of each position</exception>
        public PositionGroup(IPositionGroupBuyingPowerModel buyingPowerModel, decimal quantity, params IPosition[] positions)
            : this(new PositionGroupKey(buyingPowerModel, positions), quantity, positions.ToDictionary(p => p.Symbol))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PositionGroup"/> class
        /// </summary>
        /// <param name="key">The deterministic key for this group</param>
        /// <param name="quantity">The group quantity, which must be the ratio of quantity to unit quantity of each position</param>
        /// <param name="positions">The positions comprising this group</param>
        /// <exception cref="ArgumentException">Thrown when the quantity is not the ratio of quantity to unit quantity of each position</exception>
        public PositionGroup(PositionGroupKey key, decimal quantity, params IPosition[] positions)
            : this(key, quantity, positions.ToDictionary(p => p.Symbol))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PositionGroup"/> class
        /// </summary>
        /// <param name="key">The deterministic key for this group</param>
        /// <param name="quantity">The group quantity, which must be the ratio of quantity to unit quantity of each position</param>
        /// <param name="positions">The positions comprising this group</param>
        /// <exception cref="ArgumentException">Thrown when the quantity is not the ratio of quantity to unit quantity of each position</exception>
        public PositionGroup(PositionGroupKey key, decimal quantity, Dictionary<Symbol, IPosition> positions)
        {
            Key = key;
            Quantity = quantity;
            _positions = positions;

#if DEBUG
            if (positions.Any(kvp => Math.Abs(kvp.Value.Quantity / kvp.Value.UnitQuantity) != Math.Abs(Quantity)))
            {
                throw new ArgumentException(Messages.PositionGroup.InvalidQuantity(Quantity, positions.Values));
            }
#endif
        }

        /// <summary>
        /// Attempts to retrieve the position with the specified symbol
        /// </summary>
        /// <param name="symbol">The symbol</param>
        /// <param name="position">The position, if found</param>
        /// <returns>True if the position was found, otherwise false</returns>
        public bool TryGetPosition(Symbol symbol, out IPosition position)
        {
            return _positions.TryGetValue(symbol, out position);
        }

        /// <summary>Returns a string that represents the current object.</summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            return $"{Key}: {Quantity}";
        }

        /// <summary>Returns an enumerator that iterates through the collection.</summary>
        /// <returns>An enumerator that can be used to iterate through the collection.</returns>
        public IEnumerator<IPosition> GetEnumerator()
        {
            return Positions.GetEnumerator();
        }

        /// <summary>Returns an enumerator that iterates through a collection.</summary>
        /// <returns>An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Instantiates a default empty position group instance
        /// </summary>
        /// <param name="buyingPowerModel">The buying power model to use for this group</param>
        public static PositionGroup Empty(IPositionGroupBuyingPowerModel buyingPowerModel)
        {
            return new PositionGroup(new PositionGroupKey(buyingPowerModel, new List<IPosition>()), 0m);
        }
    }
}
