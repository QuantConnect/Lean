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

using QuantConnect.Data.UniverseSelection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Algorithm.Framework.Selection
{
    /// <summary>
    /// Base ETF Universe that accepts a Dictionary of DateTime keyed by String that represent
    /// the Inception date for each symbol
    /// </summary>
    public class BaseETFUniverseSelectionModel : FundamentalUniverseSelectionModel
    {
        private readonly Queue<Inception> _queue;
        private readonly List<Symbol> _symbols;

        /// <summary>
        /// Initializes a new instance of the BaseETFUniverse class
        /// </summary>
        /// <param name="tickersByDate">Dictionary of DateTime keyed by String that represent the Inception date for each symbol</param>
        public BaseETFUniverseSelectionModel(Dictionary<string, DateTime> tickersByDate) :
            base(false)
        {
            _queue = new Queue<Inception>(tickersByDate.Select(Inception.Create));
            _symbols = new List<Symbol>();
        }

        /// <summary>
        /// Returns all ETF that are trading at current algorithm Time
        /// </summary>
        public override IEnumerable<Symbol> SelectCoarse(QCAlgorithm algorithm, IEnumerable<CoarseFundamental> coarse)
        {
            var date = algorithm.Time;

            // Move Symbols that are trading from the queue to a list 
            var added = new List<Symbol>();
            while (_queue.Count > 0 && _queue.First().Date <= date)
            {
                var inception = _queue.Dequeue();
                added.Add(inception.Symbol);
            }

            // If no pending for addition found, return Universe Unchanged
            // Otherwise adds to list of current symbols and return it
            if (added.Count == 0)
            {
                return Universe.Unchanged;
            }

            _symbols.AddRange(added);
            return _symbols;
        }

        internal class Inception
        {
            public DateTime Date;
            public Symbol Symbol;

            public static Inception Create(KeyValuePair<string, DateTime> kvp)
            {
                return new Inception()
                {
                    Date = kvp.Value,
                    Symbol = Symbol.Create(kvp.Key, SecurityType.Equity, Market.USA)
                };
            }
        }
    }
}