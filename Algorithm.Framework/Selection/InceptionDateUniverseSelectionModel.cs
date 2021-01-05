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
using Python.Runtime;

namespace QuantConnect.Algorithm.Framework.Selection
{
    /// <summary>
    /// Inception Date Universe that accepts a Dictionary of DateTime keyed by String that represent
    /// the Inception date for each ticker
    /// </summary>
    public class InceptionDateUniverseSelectionModel : CustomUniverseSelectionModel
    {
        private readonly Queue<KeyValuePair<string, DateTime>> _queue;
        private readonly List<string> _symbols;

        /// <summary>
        /// Initializes a new instance of the <see cref="InceptionDateUniverseSelectionModel"/> class
        /// </summary>
        /// <param name="name">A unique name for this universe</param>
        /// <param name="tickersByDate">Dictionary of DateTime keyed by String that represent the Inception date for each ticker</param>
        public InceptionDateUniverseSelectionModel(string name, Dictionary<string, DateTime> tickersByDate) :
            base(name, (Func<DateTime, IEnumerable<string>>) null)
        {
            _queue = new Queue<KeyValuePair<string, DateTime>>(tickersByDate);
            _symbols = new List<string>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InceptionDateUniverseSelectionModel"/> class
        /// </summary>
        /// <param name="name">A unique name for this universe</param>
        /// <param name="tickersByDate">Dictionary of DateTime keyed by String that represent the Inception date for each ticker</param>
        public InceptionDateUniverseSelectionModel(string name, PyObject tickersByDate) :
            this(name, tickersByDate.ConvertToDictionary<string, DateTime>())
        {
        }

        /// <summary>
        /// Returns all tickers that are trading at current algorithm Time
        /// </summary>
        public override IEnumerable<string> Select(QCAlgorithm algorithm, DateTime date)
        {
            // Move Symbols that are trading from the queue to a list 
            var added = new List<string>();
            while (_queue.Count > 0 && _queue.First().Value <= date)
            {
                added.Add(_queue.Dequeue().Key);
            }

            // If no pending for addition found, return Universe Unchanged
            // Otherwise adds to list of current tickers and return it
            if (added.Count == 0)
            {
                return Universe.Unchanged;
            }

            _symbols.AddRange(added);
            return _symbols;
        }
    }
}