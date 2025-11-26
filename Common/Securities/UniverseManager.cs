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
using System.Collections.Specialized;
using System.Linq;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Util;
using Common.Util;

namespace QuantConnect.Securities
{
    /// <summary>
    /// Manages the algorithm's collection of universes
    /// </summary>
    public class UniverseManager : BaseExtendedDictionary<Symbol, Universe, ConcurrentDictionary<Symbol, Universe>>
    {
        private readonly Queue<UniverseManagerChanged> _pendingChanges = new();

        /// <summary>
        /// Event fired when a universe is added or removed
        /// </summary>
        public event EventHandler<UniverseManagerChanged> CollectionChanged;

        /// <summary>
        /// Gets the number of elements contained in the dictionary
        /// </summary>
        public override int Count => TypedDictionary.Skip(0).Count();

        /// <summary>
        /// Gets the keys of the dictionary
        /// </summary>
        public override ICollection<Symbol> Keys => TypedDictionary.Select(x => x.Key).ToList();

        /// <summary>
        /// Gets the values of the dictionary
        /// </summary>
        public override ICollection<Universe> Values => TypedDictionary.Select(x => x.Value).ToList();

        /// <summary>
        /// Read-only dictionary containing all active securities. An active security is
        /// a security that is currently selected by the universe or has holdings or open orders.
        /// </summary>
        public IReadOnlyDictionary<Symbol, Security> ActiveSecurities => this
            .SelectMany(ukvp => ukvp.Value.Members.Select(mkvp => mkvp.Value))
            .DistinctBy(s => s.Symbol)
            .ToReadOnlyExtendedDictionary(s => s.Symbol);

        /// <summary>
        /// Initializes a new instance of the <see cref="UniverseManager"/> class
        /// </summary>
        public UniverseManager() : base(new ConcurrentDictionary<Symbol, Universe>())
        {
        }

        /// <summary>
        /// Adds an element with the provided key and value to the dictionary
        /// </summary>
        public override void Add(Symbol key, Universe value)
        {
            if (TypedDictionary.TryAdd(key, value))
            {
                lock (_pendingChanges)
                {
                    _pendingChanges.Enqueue(new UniverseManagerChanged(NotifyCollectionChangedAction.Add, value));
                }
            }
        }

        /// <summary>
        /// Updates an element with the provided key and value to the dictionary
        /// </summary>
        public void Update(Symbol key, Universe value, NotifyCollectionChangedAction action)
        {
            if (TypedDictionary.ContainsKey(key) && !_pendingChanges.Any(x => x.Value == value))
            {
                lock (_pendingChanges)
                {
                    _pendingChanges.Enqueue(new UniverseManagerChanged(action, value));
                }
            }
        }

        /// <summary>
        /// Will trigger collection changed event if required
        /// </summary>
        public void ProcessChanges()
        {
            UniverseManagerChanged universeChange;
            do
            {
                lock (_pendingChanges)
                {
                    _pendingChanges.TryDequeue(out universeChange);
                }

                if (universeChange != null)
                {
                    OnCollectionChanged(universeChange);
                }
            }
            while (universeChange != null);
        }

        /// <summary>
        /// Removes the element with the specified key from the dictionary
        /// </summary>
        public override bool Remove(Symbol key)
        {
            Universe universe;
            if (TypedDictionary.TryRemove(key, out universe))
            {
                universe.Dispose();
                OnCollectionChanged(new UniverseManagerChanged(NotifyCollectionChangedAction.Remove, universe));
                return true;
            }
            return false;
        }

        /// <summary>
        /// Gets or sets the element with the specified key
        /// </summary>
        public override Universe this[Symbol symbol]
        {
            get
            {
                if (!TypedDictionary.ContainsKey(symbol))
                {
                    throw new KeyNotFoundException($"This universe symbol ({symbol}) was not found in your universe list. Please add this security or check it exists before using it with 'Universes.ContainsKey(\"{SymbolCache.GetTicker(symbol)}\")'");
                }
                return TypedDictionary[symbol];
            }
            set
            {
                Universe existing;
                if (TypedDictionary.TryGetValue(symbol, out existing) && existing != value)
                {
                    throw new ArgumentException($"Unable to over write existing Universe: {symbol.Value}");
                }

                // no security exists for the specified symbol key, add it now
                if (existing == null)
                {
                    Add(symbol, value);
                }
            }
        }

        /// <summary>
        /// Event invocator for the <see cref="CollectionChanged"/> event
        /// </summary>
        protected virtual void OnCollectionChanged(UniverseManagerChanged e)
        {
            CollectionChanged?.Invoke(this, e);
        }
    }
}