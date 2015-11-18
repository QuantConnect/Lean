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
 *
*/

using System;
using System.Collections.Concurrent;

namespace QuantConnect
{
    /// <summary>
    /// Provides a string->Symbol mapping to allow for user defined strings to be lifted into a Symbol
    /// This is mainly used via the Symbol implicit operator, but also functions that create securities
    /// should also call Set to add new mappings
    /// </summary>
    public static class SymbolCache
    {
        private static readonly ConcurrentDictionary<string, Symbol> Symbols = new ConcurrentDictionary<string, Symbol>();
        private static readonly ConcurrentDictionary<Symbol, string> Tickers = new ConcurrentDictionary<Symbol, string>();

        /// <summary>
        /// Adds a mapping for the specified ticker
        /// </summary>
        /// <param name="ticker">The string ticker symbol</param>
        /// <param name="symbol">The symbol object that maps to the string ticker symbol</param>
        public static void Set(string ticker, Symbol symbol)
        {
            Symbols[ticker] = symbol;
            Tickers[symbol] = ticker;
        }

        /// <summary>
        /// Gets the Symbol object that is mapped to the specified string ticker symbol
        /// </summary>
        /// <param name="ticker">The string ticker symbol</param>
        /// <returns>The symbol object that maps to the specified string ticker symbol</returns>
        public static Symbol Get(string ticker)
        {
            Symbol symbol;
            if (Symbols.TryGetValue(ticker, out symbol)) return symbol;
            throw new Exception("Unable to resolve sid from ticker: " + ticker);
        }

        /// <summary>
        /// Gets the string ticker symbol that is mapped to the specified Symbol
        /// </summary>
        /// <param name="symbol">The symbol object</param>
        /// <returns>The string ticker symbol that maps to the specified symbol object</returns>
        public static string GetTicker(Symbol symbol)
        {
            string ticker;
            if (Tickers.TryGetValue(symbol, out ticker)) return ticker;
            throw new Exception("Unable to resolve ticker from sid: " + symbol);
        }
    }
}