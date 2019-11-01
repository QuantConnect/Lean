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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.Framework.Portfolio
{
    /// <summary>
    /// Provides a collection for managing <see cref="IPortfolioTarget"/>s for each symbol
    /// </summary>
    public class PortfolioTargetCollection : ICollection<IPortfolioTarget>, IDictionary<Symbol, IPortfolioTarget>
    {
        private readonly ConcurrentDictionary<Symbol, IPortfolioTarget> _targets = new ConcurrentDictionary<Symbol, IPortfolioTarget>();

        /// <summary>
        /// Gets the number of targets in this collection
        /// </summary>
        public int Count => _targets.Skip(0).Count();

        /// <summary>
        /// Gets `false`. This collection is not read-only.
        /// </summary>
        public bool IsReadOnly => false;

        /// <summary>
        /// Gets the symbol keys for this collection
        /// </summary>
        public ICollection<Symbol> Keys => _targets.Keys;

        /// <summary>
        /// Gets all portfolio targets in this collection
        /// Careful, will return targets for securities that might have no data yet.
        /// </summary>
        public ICollection<IPortfolioTarget> Values => _targets.Values;

        /// <summary>
        /// Adds the specified target to the collection. If a target for the same symbol
        /// already exists it wil be overwritten.
        /// </summary>
        /// <param name="target">The portfolio target to add</param>
        public void Add(IPortfolioTarget target)
        {
            if (target == null)
            {
                return;
            }

            _targets[target.Symbol] = target;
        }

        /// <summary>
        /// Adds the specified target to the collection. If a target for the same symbol
        /// already exists it wil be overwritten.
        /// </summary>
        /// <param name="target">The portfolio target to add</param>
        public void Add(KeyValuePair<Symbol, IPortfolioTarget> target)
        {
            WithDictionary(d => d.Add(target));
        }

        /// <summary>
        /// Adds the specified target to the collection. If a target for the same symbol
        /// already exists it wil be overwritten.
        /// </summary>
        /// <param name="symbol">The symbol key</param>
        /// <param name="target">The portfolio target to add</param>
        public void Add(Symbol symbol, IPortfolioTarget target)
        {
            WithDictionary(d => d.Add(symbol, target));
        }

        /// <summary>
        /// Adds the specified targets to the collection. If a target for the same symbol
        /// already exists it will be overwritten.
        /// </summary>
        /// <param name="targets">The portfolio targets to add</param>
        public void AddRange(IEnumerable<IPortfolioTarget> targets)
        {
            foreach (var item in targets)
            {
                _targets[item.Symbol] = item;
            }
        }

        /// <summary>
        /// Adds the specified targets to the collection. If a target for the same symbol
        /// already exists it will be overwritten.
        /// </summary>
        /// <param name="targets">The portfolio targets to add</param>
        public void AddRange(IPortfolioTarget[] targets)
        {
            foreach (var item in targets)
            {
                _targets[item.Symbol] = item;
            }
        }

        /// <summary>
        /// Removes all portfolio targets from this collection
        /// </summary>
        public void Clear()
        {
            _targets.Clear();
        }

        /// <summary>
        /// Removes fulfilled portfolio targets from this collection.
        /// Will only take into account actual holdings and ignore open orders.
        /// </summary>
        public void ClearFulfilled(IAlgorithm algorithm)
        {
            foreach (var target in _targets)
            {
                var security = algorithm.Securities[target.Key];
                var holdings = security.Holdings.Quantity;
                // check to see if we're done with this target
                if (Math.Abs(target.Value.Quantity - holdings) < security.SymbolProperties.LotSize)
                {
                    Remove(target.Key);
                }
            }
        }

        /// <summary>
        /// Determines whether or not the specified target exists in this collection.
        /// NOTE: This checks for the exact specified target, not by symbol. Use ContainsKey
        /// to check by symbol.
        /// </summary>
        /// <param name="target">The portfolio target to check for existence.</param>
        /// <returns>True if the target exists, false otherwise</returns>
        public bool Contains(IPortfolioTarget target)
        {
            if (target == null)
            {
                return false;
            }

            return _targets.ContainsKey(target.Symbol);
        }

        /// <summary>
        /// Determines whether the specified symbol/target pair exists in this collection
        /// </summary>
        /// <param name="target">The symbol/target pair</param>
        /// <returns>True if the pair exists, false otherwise</returns>
        public bool Contains(KeyValuePair<Symbol, IPortfolioTarget> target)
        {
            return WithDictionary(d => d.Contains(target));
        }

        /// <summary>
        /// Determines whether the specified symbol exists as a key in this collection
        /// </summary>
        /// <param name="symbol">The symbol key</param>
        /// <returns>True if the symbol exists in this collection, false otherwise</returns>
        public bool ContainsKey(Symbol symbol)
        {
            return _targets.ContainsKey(symbol);
        }

        /// <summary>
        /// Copies the targets in this collection to the specified array
        /// </summary>
        /// <param name="array">The destination array to copy to</param>
        /// <param name="arrayIndex">The index in the array to start copying to</param>
        public void CopyTo(IPortfolioTarget[] array, int arrayIndex)
        {
            _targets.Values.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Copies the targets in this collection to the specified array
        /// </summary>
        /// <param name="array">The destination array to copy to</param>
        /// <param name="arrayIndex">The index in the array to start copying to</param>
        public void CopyTo(KeyValuePair<Symbol, IPortfolioTarget>[] array, int arrayIndex)
        {
            WithDictionary(d => d.CopyTo(array, arrayIndex));
        }

        /// <summary>
        /// Removes the target for the specified symbol if it exists in this collection.
        /// </summary>
        /// <param name="symbol">The symbol to remove</param>
        /// <returns>True if the symbol's target was removed, false if it doesn't exist in the collection</returns>
        public bool Remove(Symbol symbol)
        {
            return WithDictionary(d => d.Remove(symbol));
        }

        /// <summary>
        /// Removes the target for the specified symbol/target pair if it exists in this collection.
        /// </summary>
        /// <param name="target">The symbol/target pair to remove</param>
        /// <returns>True if the symbol's target was removed, false if it doesn't exist in the collection</returns>
        public bool Remove(KeyValuePair<Symbol, IPortfolioTarget> target)
        {
            return WithDictionary(d => d.Remove(target));
        }

        /// <summary>
        /// Removes the target if it exists in this collection.
        /// </summary>
        /// <param name="target">The target to remove</param>
        /// <returns>True if the target was removed, false if it doesn't exist in the collection</returns>
        public bool Remove(IPortfolioTarget target)
        {
            if (target == null)
            {
                return false;
            }

            IPortfolioTarget existing;
            if (_targets.TryGetValue(target.Symbol, out existing))
            {
                // need to confirm that we're removing the requested target and not a different target w/ the same symbol key
                if (existing.Equals(target))
                {
                    return Remove(target.Symbol);
                }
            }

            return false;
        }

        /// <summary>
        /// Attempts to retrieve the target for the specified symbol
        /// </summary>
        /// <param name="symbol">The symbol</param>
        /// <param name="target">The portfolio target for the symbol, or null if not found</param>
        /// <returns>True if the symbol's target was found, false if it does not exist in this collection</returns>
        public bool TryGetValue(Symbol symbol, out IPortfolioTarget target)
        {
            return _targets.TryGetValue(symbol, out target);
        }

        /// <summary>
        /// Gets or sets the portfolio target for the specified symbol
        /// </summary>
        /// <param name="symbol">The symbol</param>
        /// <returns>The symbol's portolio target if it exists in this collection, if not a <see cref="KeyNotFoundException"/> will be thrown.</returns>
        public IPortfolioTarget this[Symbol symbol]
        {
            get { return _targets[symbol]; }
            set { _targets[symbol] = value; }
        }

        /// <summary>
        /// Gets an enumerator to iterator over the symbol/target key value pairs in this collection.
        /// </summary>
        /// <returns>Symbol/target key value pair enumerator</returns>
        IEnumerator<KeyValuePair<Symbol, IPortfolioTarget>> IEnumerable<KeyValuePair<Symbol, IPortfolioTarget>>.GetEnumerator()
        {
            return _targets.GetEnumerator();
        }

        /// <summary>
        /// Gets an enumerator to iterator over all portfolio targets in this collection.
        /// This is the default enumerator for this collection.
        /// </summary>
        /// <returns>Portfolio targets enumerator</returns>
        public IEnumerator<IPortfolioTarget> GetEnumerator()
        {
            return _targets.Select(kvp => kvp.Value).GetEnumerator();
        }

        /// <summary>
        /// Gets an enumerator to iterator over all portfolio targets in this collection.
        /// This is the default enumerator for this collection.
        /// Careful, will return targets for securities that might have no data yet.
        /// </summary>
        /// <returns>Portfolio targets enumerator</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Helper function to easily access explicitly implemented interface methods against concurrent dictionary
        /// </summary>
        private void WithDictionary(Action<IDictionary<Symbol, IPortfolioTarget>> action)
        {
            action(_targets);
        }

        /// <summary>
        /// Helper function to easily access explicitly implemented interface methods against concurrent dictionary
        /// </summary>
        private T WithDictionary<T>(Func<IDictionary<Symbol, IPortfolioTarget>, T> func)
        {
            return func(_targets);
        }

        /// <summary>
        /// Returned an ordered enumerable where position reducing orders are executed first
        /// and the remaining orders are executed in decreasing order value.
        /// Will NOT return targets for securities that have no data yet.
        /// Will NOT return targets for which current holdings + open orders quantity, sum up to the target quantity
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        public IEnumerable<IPortfolioTarget> OrderByMarginImpact(IAlgorithm algorithm)
        {
            return this.OrderTargetsByMarginImpact(algorithm);
        }
    }
}