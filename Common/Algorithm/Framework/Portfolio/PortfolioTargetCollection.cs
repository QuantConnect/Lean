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
using System.Collections;
using QuantConnect.Interfaces;
using System.Collections.Generic;

namespace QuantConnect.Algorithm.Framework.Portfolio
{
    /// <summary>
    /// Provides a collection for managing <see cref="IPortfolioTarget"/>s for each symbol
    /// </summary>
    public class PortfolioTargetCollection : ICollection<IPortfolioTarget>, IDictionary<Symbol, IPortfolioTarget>
    {
        private List<IPortfolioTarget> _enumerable;
        private List<KeyValuePair<Symbol, IPortfolioTarget>> _kvpEnumerable;
        private readonly Dictionary<Symbol, IPortfolioTarget> _targets = new ();

        /// <summary>
        /// Gets the number of targets in this collection
        /// </summary>
        public int Count
        {
            get
            {
                lock (_targets)
                {
                    return _targets.Count;
                }
            }
        }

        /// <summary>
        /// True if there is no target in the collection
        /// </summary>
        public bool IsEmpty
        {
            get
            {
                lock (_targets)
                {
                    return _targets.Count == 0;
                }
            }
        }

        /// <summary>
        /// Gets `false`. This collection is not read-only.
        /// </summary>
        public bool IsReadOnly => false;

        /// <summary>
        /// Gets the symbol keys for this collection
        /// </summary>
        public ICollection<Symbol> Keys
        {
            get
            {
                lock (_targets)
                {
                    return _targets.Keys.ToList();
                }
            }
        }

        /// <summary>
        /// Gets all portfolio targets in this collection
        /// Careful, will return targets for securities that might have no data yet.
        /// </summary>
        public ICollection<IPortfolioTarget> Values
        {
            get
            {
                var result = _enumerable;
                if (result == null)
                {
                    lock (_targets)
                    {
                        result = _enumerable = _targets.Values.ToList();
                    }
                }
                return result;
            }
        }

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

            lock (_targets)
            {
                _enumerable = null;
                _kvpEnumerable = null;
                _targets[target.Symbol] = target;
            }
        }

        /// <summary>
        /// Adds the specified target to the collection. If a target for the same symbol
        /// already exists it wil be overwritten.
        /// </summary>
        /// <param name="target">The portfolio target to add</param>
        public void Add(KeyValuePair<Symbol, IPortfolioTarget> target)
        {
            Add(target);
        }

        /// <summary>
        /// Adds the specified target to the collection. If a target for the same symbol
        /// already exists it wil be overwritten.
        /// </summary>
        /// <param name="symbol">The symbol key</param>
        /// <param name="target">The portfolio target to add</param>
        public void Add(Symbol symbol, IPortfolioTarget target)
        {
            Add(target);
        }

        /// <summary>
        /// Adds the specified targets to the collection. If a target for the same symbol
        /// already exists it will be overwritten.
        /// </summary>
        /// <param name="targets">The portfolio targets to add</param>
        public void AddRange(IEnumerable<IPortfolioTarget> targets)
        {
            lock (_targets)
            {
                _enumerable = null;
                _kvpEnumerable = null;
                foreach (var item in targets)
                {
                    _targets[item.Symbol] = item;
                }
            }
        }

        /// <summary>
        /// Adds the specified targets to the collection. If a target for the same symbol
        /// already exists it will be overwritten.
        /// </summary>
        /// <param name="targets">The portfolio targets to add</param>
        public void AddRange(IPortfolioTarget[] targets)
        {
            AddRange((IEnumerable<IPortfolioTarget>)targets);
        }

        /// <summary>
        /// Removes all portfolio targets from this collection
        /// </summary>
        public void Clear()
        {
            lock (_targets)
            {
                _enumerable = null;
                _kvpEnumerable = null;
                _targets.Clear();
            }
        }

        /// <summary>
        /// Removes fulfilled portfolio targets from this collection.
        /// Will only take into account actual holdings and ignore open orders.
        /// </summary>
        public void ClearFulfilled(IAlgorithm algorithm)
        {
            foreach (var target in this)
            {
                var security = algorithm.Securities[target.Symbol];
                var holdings = security.Holdings.Quantity;
                // check to see if we're done with this target
                if (Math.Abs(target.Quantity - holdings) < security.SymbolProperties.LotSize)
                {
                    Remove(target.Symbol);
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

            lock (_targets)
            {
                return _targets.ContainsKey(target.Symbol);
            }
        }

        /// <summary>
        /// Determines whether the specified symbol/target pair exists in this collection
        /// </summary>
        /// <param name="target">The symbol/target pair</param>
        /// <returns>True if the pair exists, false otherwise</returns>
        public bool Contains(KeyValuePair<Symbol, IPortfolioTarget> target)
        {
            return Contains(target);
        }

        /// <summary>
        /// Determines whether the specified symbol exists as a key in this collection
        /// </summary>
        /// <param name="symbol">The symbol key</param>
        /// <returns>True if the symbol exists in this collection, false otherwise</returns>
        public bool ContainsKey(Symbol symbol)
        {
            lock (_targets)
            {
                return _targets.ContainsKey(symbol);
            }
        }

        /// <summary>
        /// Copies the targets in this collection to the specified array
        /// </summary>
        /// <param name="array">The destination array to copy to</param>
        /// <param name="arrayIndex">The index in the array to start copying to</param>
        public void CopyTo(IPortfolioTarget[] array, int arrayIndex)
        {
            lock (_targets)
            {
                _targets.Values.CopyTo(array, arrayIndex);
            }
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
            lock (_targets)
            {
                if (_targets.Remove(symbol))
                {
                    _enumerable = null;
                    _kvpEnumerable = null;
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Removes the target for the specified symbol/target pair if it exists in this collection.
        /// </summary>
        /// <param name="target">The symbol/target pair to remove</param>
        /// <returns>True if the symbol's target was removed, false if it doesn't exist in the collection</returns>
        public bool Remove(KeyValuePair<Symbol, IPortfolioTarget> target)
        {
            return Remove(target.Value);
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

            lock (_targets)
            {
                IPortfolioTarget existing;
                if (_targets.TryGetValue(target.Symbol, out existing))
                {
                    // need to confirm that we're removing the requested target and not a different target w/ the same symbol key
                    if (existing.Equals(target))
                    {
                        return Remove(target.Symbol);
                    }
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
            lock (_targets)
            {
                return _targets.TryGetValue(symbol, out target);
            }
        }

        /// <summary>
        /// Gets or sets the portfolio target for the specified symbol
        /// </summary>
        /// <param name="symbol">The symbol</param>
        /// <returns>The symbol's portfolio target if it exists in this collection, if not a <see cref="KeyNotFoundException"/> will be thrown.</returns>
        public IPortfolioTarget this[Symbol symbol]
        {
            get
            {
                lock (_targets)
                {
                    return _targets[symbol];
                }
            }
            set
            {
                lock (_targets)
                {
                    _enumerable = null;
                    _kvpEnumerable = null;
                    _targets[symbol] = value;
                }
            }
        }

        /// <summary>
        /// Gets an enumerator to iterator over the symbol/target key value pairs in this collection.
        /// </summary>
        /// <returns>Symbol/target key value pair enumerator</returns>
        IEnumerator<KeyValuePair<Symbol, IPortfolioTarget>> IEnumerable<KeyValuePair<Symbol, IPortfolioTarget>>.GetEnumerator()
        {
            var result = _kvpEnumerable;
            if (result == null)
            {
                lock (_targets)
                {
                    _kvpEnumerable = result = _targets.ToList();
                }
            }
            return result.GetEnumerator();
        }

        /// <summary>
        /// Gets an enumerator to iterator over all portfolio targets in this collection.
        /// This is the default enumerator for this collection.
        /// </summary>
        /// <returns>Portfolio targets enumerator</returns>
        public IEnumerator<IPortfolioTarget> GetEnumerator()
        {
            var result = _enumerable;
            if (result == null)
            {
                lock (_targets)
                {
                    _enumerable = result = _targets.Values.ToList();
                }
            }
            return result.GetEnumerator();
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
            lock (_targets)
            {
                action(_targets);
            }
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
            if (IsEmpty)
            {
                return Enumerable.Empty<IPortfolioTarget>();
            }
            return this.OrderTargetsByMarginImpact(algorithm);
        }
    }
}
