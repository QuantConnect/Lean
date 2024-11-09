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
using System.Linq;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace QuantConnect
{
    /// <summary>
    /// Provides a string->Symbol mapping to allow for user defined strings to be lifted into a Symbol
    /// This is mainly used via the Symbol implicit operator, but also functions that create securities
    /// should also call Set to add new mappings
    /// </summary>
    public static class SymbolCache
    {
        // we aggregate the two maps into a class so we can assign a new one as an atomic operation
        private static readonly Dictionary<string, Symbol> Symbols = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<Symbol, string> Tickers = new();

        /// <summary>
        /// Adds a mapping for the specified ticker
        /// </summary>
        /// <param name="ticker">The string ticker symbol</param>
        /// <param name="symbol">The symbol object that maps to the string ticker symbol</param>
        public static void Set(string ticker, Symbol symbol)
        {
            lock (Symbols)
            {
                Symbols[ticker] = symbol;
                Tickers[symbol] = ticker;

                var index = ticker.IndexOf('.');
                if (index != -1)
                {
                    var related = ticker.Substring(0, index);
                    if (Symbols.TryGetValue(related, out symbol) && symbol is null)
                    {
                        Symbols.Remove(related);
                    }
                }
            }
        }

        /// <summary>
        /// Gets the Symbol object that is mapped to the specified string ticker symbol
        /// </summary>
        /// <param name="ticker">The string ticker symbol</param>
        /// <returns>The symbol object that maps to the specified string ticker symbol</returns>
        public static Symbol GetSymbol(string ticker)
        {
            var result = TryGetSymbol(ticker);
            if (!result.Item1)
            {
                throw result.Item3 ?? throw new InvalidOperationException(Messages.SymbolCache.UnableToLocateTicker(ticker));
            }
            return result.Item2;
        }

        /// <summary>
        /// Gets the Symbol object that is mapped to the specified string ticker symbol
        /// </summary>
        /// <param name="ticker">The string ticker symbol</param>
        /// <param name="symbol">The output symbol object</param>
        /// <returns>The symbol object that maps to the specified string ticker symbol</returns>
        public static bool TryGetSymbol(string ticker, out Symbol symbol)
        {
            var result = TryGetSymbol(ticker);
            symbol = result.Item2;
            return result.Item1;
        }

        /// <summary>
        /// Gets the string ticker symbol that is mapped to the specified Symbol
        /// </summary>
        /// <param name="symbol">The symbol object</param>
        /// <returns>The string ticker symbol that maps to the specified symbol object</returns>
        public static string GetTicker(Symbol symbol)
        {
            lock (Symbols)
            {
                return Tickers.TryGetValue(symbol, out var ticker) ? ticker : symbol.ID.ToString();
            }
        }

        /// <summary>
        /// Gets the string ticker symbol that is mapped to the specified Symbol
        /// </summary>
        /// <param name="symbol">The symbol object</param>
        /// <param name="ticker">The output string ticker symbol</param>
        /// <returns>The string ticker symbol that maps to the specified symbol object</returns>
        public static bool TryGetTicker(Symbol symbol, out string ticker)
        {
            lock (Symbols)
            {
                return Tickers.TryGetValue(symbol, out ticker);
            }
        }

        /// <summary>
        /// Removes the mapping for the specified symbol from the cache
        /// </summary>
        /// <param name="symbol">The symbol whose mappings are to be removed</param>
        /// <returns>True if the symbol mapping were removed from the cache</returns>
        /// <remarks>Just used for testing</remarks>
        public static bool TryRemove(Symbol symbol)
        {
            lock (Symbols)
            {
                return Tickers.Remove(symbol, out var ticker) && Symbols.Remove(ticker, out symbol);
            }
        }

        /// <summary>
        /// Removes the mapping for the specified symbol from the cache
        /// </summary>
        /// <param name="ticker">The ticker whose mappings are to be removed</param>
        /// <returns>True if the symbol mapping were removed from the cache</returns>
        /// <remarks>Just used for testing</remarks>
        public static bool TryRemove(string ticker)
        {
            lock (Symbols)
            {
                return Symbols.Remove(ticker, out var symbol) && Tickers.Remove(symbol, out ticker);
            }
        }

        /// <summary>
        /// Clears the current caches
        /// </summary>
        /// <remarks>Just used for testing</remarks>
        public static void Clear()
        {
            lock (Symbols)
            {
                Symbols.Clear();
                Tickers.Clear();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Tuple<bool, Symbol, InvalidOperationException> TryGetSymbol(string ticker)
        {
            lock (Symbols)
            {
                if (!TryGetSymbolCached(ticker, out var symbol))
                {
                    // fall-back full-text search as a back-shim for custom data symbols.
                    // permitting a user to use BTC to resolve to BTC.Bitcoin
                    var search = $"{ticker}.";
                    var match = Symbols.Where(kvp => kvp.Key.StartsWith(search, StringComparison.InvariantCultureIgnoreCase) && kvp.Value is not null).ToList();

                    if (match.Count == 0)
                    {
                        // no matches, cache the miss! else it will get expensive
                        Symbols[ticker] = null;
                        return new(false, null, null);
                    }
                    else if (match.Count == 1)
                    {
                        // exactly one match
                        Symbols[ticker] = match[0].Value;
                        return new(true, match[0].Value, null);
                    }
                    else if (match.Count > 1)
                    {
                        // too many matches
                        return new(false, null, new InvalidOperationException(
                            Messages.SymbolCache.MultipleMatchingTickersLocated(match.Select(kvp => kvp.Key))));
                    }
                }
                return new(symbol is not null, symbol, null);
            }
        }

        /// <summary>
        /// Attempts to resolve the ticker to a Symbol via the cache. If not found in the
        /// cache then
        /// </summary>
        /// <param name="ticker">The ticker to resolver to a symbol</param>
        /// <param name="symbol">The resolves symbol</param>
        /// <returns>True if we successfully resolved a symbol, false otherwise</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryGetSymbolCached(string ticker, out Symbol symbol)
        {
            if (Symbols.TryGetValue(ticker, out symbol))
            {
                return true;
            }
            if (SecurityIdentifier.TryParse(ticker, out var sid))
            {
                symbol = new Symbol(sid, sid.Symbol);
                return true;
            }
            return false;
        }
    }
}
