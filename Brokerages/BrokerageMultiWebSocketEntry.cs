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

namespace QuantConnect.Brokerages
{
    /// <summary>
    /// Helper class for <see cref="BrokerageMultiWebSocketSubscriptionManager"/>
    /// </summary>
    public class BrokerageMultiWebSocketEntry
    {
        private readonly Dictionary<Symbol, int> _symbolWeights;
        private readonly List<Symbol> _symbols;
        private readonly object _locker = new();

        /// <summary>
        /// Gets the web socket instance
        /// </summary>
        public IWebSocket WebSocket { get; }

        /// <summary>
        /// Gets the sum of symbol weights for this web socket
        /// </summary>
        public int TotalWeight { get; private set; }

        /// <summary>
        /// Gets the number of symbols subscribed
        /// </summary>
        public int SymbolCount
        {
            get
            {
                lock (_locker)
                {
                    return _symbols.Count;
                }
            }
        }

        /// <summary>
        /// Returns whether the symbol is subscribed
        /// </summary>
        /// <param name="symbol"></param>
        /// <returns></returns>
        public bool Contains(Symbol symbol)
        {
            lock (_locker)
            {
                return _symbols.Contains(symbol);
            }
        }

        /// <summary>
        /// Returns the list of subscribed symbols
        /// </summary>
        /// <returns></returns>
        public IReadOnlyCollection<Symbol> Symbols
        {
            get
            {
                lock (_locker)
                {
                    return _symbols.ToList();
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BrokerageMultiWebSocketEntry"/> class
        /// </summary>
        /// <param name="symbolWeights">A dictionary of symbol weights</param>
        /// <param name="webSocket">The web socket instance</param>
        public BrokerageMultiWebSocketEntry(
            Dictionary<Symbol, int> symbolWeights,
            IWebSocket webSocket
        )
        {
            _symbolWeights = symbolWeights;
            _symbols = new List<Symbol>();

            WebSocket = webSocket;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BrokerageMultiWebSocketEntry"/> class
        /// </summary>
        /// <param name="webSocket">The web socket instance</param>
        public BrokerageMultiWebSocketEntry(IWebSocket webSocket)
            : this(null, webSocket) { }

        /// <summary>
        /// Adds a symbol to the entry
        /// </summary>
        /// <param name="symbol">The symbol to add</param>
        public void AddSymbol(Symbol symbol)
        {
            lock (_locker)
            {
                _symbols.Add(symbol);
            }

            if (_symbolWeights != null && _symbolWeights.TryGetValue(symbol, out var weight))
            {
                TotalWeight += weight;
            }
        }

        /// <summary>
        /// Removes a symbol from the entry
        /// </summary>
        /// <param name="symbol">The symbol to remove</param>
        public void RemoveSymbol(Symbol symbol)
        {
            lock (_locker)
            {
                _symbols.Remove(symbol);
            }

            if (_symbolWeights != null && _symbolWeights.TryGetValue(symbol, out var weight))
            {
                TotalWeight -= weight;
            }
        }
    }
}
