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
using System.Collections.Generic;

namespace QuantConnect.Data.Market
{
    /// <summary>
    /// Collection of <see cref="OptionChain"/> keyed by canonical option symbol
    /// </summary>
    public class OptionChains : BaseChains<OptionChain, OptionContract, OptionContracts>
    {
        /// <summary>
        /// Creates a new instance of the <see cref="OptionChains"/> dictionary
        /// </summary>
        public OptionChains() : base()
        {
        }

        /// <summary>
        /// Creates a new instance of the <see cref="OptionChains"/> dictionary
        /// </summary>
        public OptionChains(bool flatten)
            : base(flatten)
        {
        }

        /// <summary>
        /// Creates a new instance of the <see cref="OptionChains"/> dictionary
        /// </summary>
        public OptionChains(DateTime time, bool flatten = true)
            : base(time, flatten)
        {
        }

        /// <summary>
        /// Gets or sets the <see cref="OptionChain"/> for the symbol, converting to canonical if needed.
        /// </summary>
        public override OptionChain this[Symbol symbol]
        {
            get => base[GetCanonicalOptionSymbol(symbol)];
            set => base[GetCanonicalOptionSymbol(symbol)] = value;
        }

        /// <summary>
        /// Tries to get the <see cref="OptionChain"/> for the given symbol.
        /// Converts to the canonical option symbol if needed before attempting retrieval.
        /// </summary>
        public override bool TryGetValue(Symbol key, out OptionChain value)
        {
            var canonicalSymbol = GetCanonicalOptionSymbol(key);
            return base.TryGetValue(canonicalSymbol, out value);
        }

        /// <summary>
        /// Checks if an <see cref="OptionChain"/> exists for the given symbol.
        /// Converts to the canonical option symbol first if needed.
        /// </summary>
        public override bool ContainsKey(Symbol key)
        {
            var canonicalSymbol = GetCanonicalOptionSymbol(key);
            return base.ContainsKey(canonicalSymbol);
        }

        /// <summary>
        /// Adds the specified symbol and chain to the dictionary, converting to canonical if needed.
        /// </summary>
        public override void Add(Symbol key, OptionChain value)
        {
            var canonicalSymbol = GetCanonicalOptionSymbol(key);
            base.Add(canonicalSymbol, value);
        }

        /// <summary>
        /// Removes the element with the specified key, converting to canonical if needed.
        /// </summary>
        public override bool Remove(Symbol key)
        {
            var canonicalSymbol = GetCanonicalOptionSymbol(key);
            return base.Remove(canonicalSymbol);
        }

        /// <summary>
        /// Determines if the dictionary contains the specific key-value pair, converting key to canonical if needed.
        /// </summary>
        public override bool Contains(KeyValuePair<Symbol, OptionChain> item)
        {
            var canonicalSymbol = GetCanonicalOptionSymbol(item.Key);
            return base.Contains(new KeyValuePair<Symbol, OptionChain>(canonicalSymbol, item.Value));
        }

        /// <summary>
        /// Removes the specific key-value pair, converting key to canonical if needed.
        /// </summary>
        public override bool Remove(KeyValuePair<Symbol, OptionChain> item)
        {
            var canonicalSymbol = GetCanonicalOptionSymbol(item.Key);
            return base.Remove(new KeyValuePair<Symbol, OptionChain>(canonicalSymbol, item.Value));
        }

        private static Symbol GetCanonicalOptionSymbol(Symbol symbol)
        {
            if (symbol.SecurityType.HasOptions())
            {
                return Symbol.CreateCanonicalOption(symbol);
            }

            if (symbol.SecurityType.IsOption())
            {
                return symbol.Canonical;
            }

            return symbol;
        }
    }
}
