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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Interfaces;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// An implementation of <see cref="IFutureChainProvider"/> that will cache by date future contracts returned by another future chain provider.
    /// </summary>
    public class CachingFutureChainProvider : IFutureChainProvider
    {
        private readonly ConcurrentDictionary<Symbol, FutureChainCacheEntry> _cache = new ConcurrentDictionary<Symbol, FutureChainCacheEntry>();
        private readonly IFutureChainProvider _futureChainProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="CachingFutureChainProvider"/> class
        /// </summary>
        /// <param name="futureChainProvider"></param>
        public CachingFutureChainProvider(IFutureChainProvider futureChainProvider)
        {
            _futureChainProvider = futureChainProvider;
        }

        /// <summary>
        /// Gets the list of future contracts for a given underlying symbol
        /// </summary>
        /// <param name="symbol">The underlying symbol</param>
        /// <param name="date">The date for which to request the future chain (only used in backtesting)</param>
        /// <returns>The list of future contracts</returns>
        public IEnumerable<Symbol> GetFutureContractList(Symbol symbol, DateTime date)
        {
            List<Symbol> symbols;

            FutureChainCacheEntry entry;
            if (!_cache.TryGetValue(symbol, out entry) || date.Date != entry.Date)
            {
                symbols = _futureChainProvider.GetFutureContractList(symbol, date.Date).ToList();
                _cache[symbol] = new FutureChainCacheEntry(date.Date, symbols);
            }
            else
            {
                symbols = entry.Symbols;
            }

            return symbols;
        }

        private class FutureChainCacheEntry
        {
            public DateTime Date { get; }
            public List<Symbol> Symbols { get; }

            public FutureChainCacheEntry(DateTime date, List<Symbol> symbols)
            {
                Date = date;
                Symbols = symbols;
            }
        }
    }
}