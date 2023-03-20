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
using Python.Runtime;
using System.Collections;
using System.Collections.Generic;

namespace QuantConnect.Algorithm.Framework.Alphas
{
    /// <summary>
    /// Provides a collection for managing insights. This type provides collection access semantics
    /// as well as dictionary access semantics through TryGetValue, ContainsKey, and this[symbol]
    /// </summary>
    public class InsightCollection : IEnumerable<Insight>
    {
        private int _totalInsightCount;
        private int _openInsightCount;
        private readonly List<Insight> _insightsComplete = new();
        private readonly Dictionary<Symbol, List<Insight>> _insights = new();

        /// <summary>
        /// The open insight count
        /// </summary>
        public int Count
        {
            get
            {
                lock (_insights)
                {
                    return _openInsightCount;
                }
            }
        }

        /// <summary>
        /// The total insight count
        /// </summary>
        public int TotalCount
        {
            get
            {
                lock (_insights)
                {
                    return _totalInsightCount;
                }
            }
        }

        /// <summary>Adds an item to the <see cref="T:System.Collections.Generic.ICollection`1" />.</summary>
        /// <param name="item">The object to add to the <see cref="T:System.Collections.Generic.ICollection`1" />.</param>
        /// <exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.ICollection`1" /> is read-only.</exception>
        public void Add(Insight item)
        {
            lock (_insights)
            {
                _openInsightCount++;
                _totalInsightCount++;

                _insightsComplete.Add(item);

                if (!_insights.TryGetValue(item.Symbol, out var existingInsights))
                {
                    _insights[item.Symbol] = existingInsights = new();
                }
                existingInsights.Add(item);
            }
        }

        /// <summary>
        /// Adds each item in the specified enumerable of insights to this collection
        /// </summary>
        /// <param name="insights">The insights to add to this collection</param>
        public void AddRange(IEnumerable<Insight> insights)
        {
            foreach (var insight in insights)
            {
                Add(insight);
            }
        }

        /// <summary>Determines whether the <see cref="T:System.Collections.Generic.ICollection`1" /> contains a specific value.</summary>
        /// <returns>true if <paramref name="item" /> is found in the <see cref="T:System.Collections.Generic.ICollection`1" />; otherwise, false.</returns>
        /// <param name="item">The object to locate in the <see cref="T:System.Collections.Generic.ICollection`1" />.</param>
        public bool Contains(Insight item)
        {
            lock(_insights)
            {
                return _insights.TryGetValue(item.Symbol, out var symbolInsights)
                    && symbolInsights.Contains(item);
            }
        }

        /// <summary>
        /// Determines whether insights exist in this collection for the specified symbol
        /// </summary>
        /// <param name="symbol">The symbol key</param>
        /// <returns>True if there are insights for the symbol in this collection</returns>
        public bool ContainsKey(Symbol symbol)
        {
            lock (_insights)
            {
                return _insights.TryGetValue(symbol, out var symbolInsights)
                    && symbolInsights.Count > 0;
            }
        }

        /// <summary>Removes the first occurrence of a specific object from the <see cref="T:System.Collections.Generic.ICollection`1" />.</summary>
        /// <returns>true if <paramref name="item" /> was successfully removed from the <see cref="T:System.Collections.Generic.ICollection`1" />; otherwise, false. This method also returns false if <paramref name="item" /> is not found in the original <see cref="T:System.Collections.Generic.ICollection`1" />.</returns>
        /// <param name="item">The object to remove from the <see cref="T:System.Collections.Generic.ICollection`1" />.</param>
        /// <exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.ICollection`1" /> is read-only.</exception>
        public bool Remove(Insight item)
        {
            lock (_insights)
            {
                if (_insights.TryGetValue(item.Symbol, out var symbolInsights))
                {
                    if (symbolInsights.Remove(item))
                    {
                        _openInsightCount--;

                        // remove empty list from dictionary
                        if (symbolInsights.Count == 0)
                        {
                            _insights.Remove(item.Symbol);
                        }
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Dictionary accessor returns a list of insights for the specified symbol
        /// </summary>
        /// <param name="symbol">The symbol key</param>
        /// <returns>List of insights for the symbol</returns>
        public List<Insight> this[Symbol symbol]
        {
            get
            {
                lock(_insights)
                {
                    return _insights[symbol]?.ToList();
                }
            }
            set
            {
                lock (_insights)
                {
                    if (_insights.TryGetValue(symbol, out var existingInsights))
                    {
                        _openInsightCount -= existingInsights?.Count ?? 0;
                    }

                    if (value != null)
                    {
                        _openInsightCount += value.Count;
                        _totalInsightCount += value.Count;
                    }
                    _insights[symbol] = value;
                }
            }
        }

        /// <summary>
        /// Attempts to get the list of insights with the specified symbol key
        /// </summary>
        /// <param name="symbol">The symbol key</param>
        /// <param name="insights">The insights for the specified symbol, or null if not found</param>
        /// <returns>True if insights for the specified symbol were found, false otherwise</returns>
        public bool TryGetValue(Symbol symbol, out List<Insight> insights)
        {
            lock (_insights)
            {
                var result = _insights.TryGetValue(symbol, out insights);
                if (result)
                {
                    // for thread safety we need to return a copy of the collection
                    insights = insights.ToList();
                }
                return result;
            }
        }

        /// <summary>Returns an enumerator that iterates through the collection.</summary>
        /// <returns>A <see cref="T:System.Collections.Generic.IEnumerator`1" /> that can be used to iterate through the collection.</returns>
        /// <filterpriority>1</filterpriority>
        public IEnumerator<Insight> GetEnumerator()
        {
            lock (_insights)
            {
                return _insights.SelectMany(kvp => kvp.Value).ToList().GetEnumerator();
            }
        }

        /// <summary>Returns an enumerator that iterates through a collection.</summary>
        /// <returns>An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.</returns>
        /// <filterpriority>2</filterpriority>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Removes the symbol and its insights
        /// </summary>
        /// <param name="symbols">List of symbols that will be removed</param>
        public void Clear(Symbol[] symbols)
        {
            lock (_insights)
            {
                foreach (var symbol in symbols)
                {
                    if (_insights.Remove(symbol, out var existingInsights))
                    {
                        _openInsightCount -= existingInsights.Count;
                    }
                }
            }
        }

        /// <summary>
        /// Gets the next expiry time UTC
        /// </summary>
        public DateTime? GetNextExpiryTime()
        {
            lock(_insights)
            {
                if (_openInsightCount == 0)
                {
                    return null;
                }

                // we can't store expiration time because it can change
                return _insights.Min(x => x.Value.Min(i => i.CloseTimeUtc));
            }
        }

        /// <summary>
        /// Gets the last generated active insight
        /// </summary>
        /// <returns>Collection of insights that are active</returns>
        public ICollection<Insight> GetActiveInsights(DateTime utcTime)
        {
            var activeInsights = new List<Insight>();
            lock (_insights)
            {
                foreach (var kvp in _insights)
                {
                    foreach (var insight in kvp.Value)
                    {
                        if (insight.IsActive(utcTime))
                        {
                            activeInsights.Add(insight);
                        }
                    }
                }
                return activeInsights;
            }
        }

        /// <summary>
        /// Returns true if there are active insights for a given symbol and time
        /// </summary>
        /// <param name="symbol">The symbol key</param>
        /// <param name="utcTime">Time that determines whether the insight has expired</param>
        /// <returns></returns>
        public bool HasActiveInsights(Symbol symbol, DateTime utcTime)
        {
            lock (_insights)
            {
                if(_insights.TryGetValue(symbol, out var existingInsights))
                {
                    return existingInsights.Any(i => i.IsActive(utcTime));
                }
            }
            return false;
        }

        /// <summary>
        /// Remove all expired insights from the collection and retuns them
        /// </summary>
        /// <param name="utcTime">Time that determines whether the insight has expired</param>
        /// <returns>Expired insights that were removed</returns>
        public ICollection<Insight> RemoveExpiredInsights(DateTime utcTime)
        {
            var removedInsights = new List<Insight>();
            lock (_insights)
            {
                foreach (var kvp in _insights)
                {
                    foreach (var insight in kvp.Value)
                    {
                        if (insight.IsExpired(utcTime))
                        {
                            removedInsights.Add(insight);
                        }
                    }
                }
                foreach (var insight in removedInsights)
                {
                    Remove(insight);
                }
            }
            return removedInsights;
        }

        /// <summary>
        /// Will remove insights from the complete insight collection
        /// </summary>
        /// <param name="filter">The function that will determine which insight to remove</param>
        public void RemoveInsights(Func<Insight, bool> filter)
        {
            lock (_insights)
            {
                _insightsComplete.RemoveAll(insight => filter(insight));

                // for consistentcy remove from open insights just in case
                List<Insight> insightsToRemove = null;
                foreach (var insights in _insights.Values)
                {
                    foreach (var insight in insights)
                    {
                        if (filter(insight))
                        {
                            insightsToRemove ??= new ();
                            insightsToRemove.Add(insight);
                        }
                    }
                }
                if(insightsToRemove != null)
                {
                    foreach (var insight in insightsToRemove)
                    {
                        Remove(insight);
                    }
                }
            }
        }

        /// <summary>
        /// Will return insights from the complete insight collection
        /// </summary>
        /// <param name="filter">The function that will determine which insight to return</param>
        /// <returns>A new list containing the selected insights</returns>
        public List<Insight> GetInsights(Func<Insight, bool> filter = null)
        {
            lock (_insights)
            {
                if(filter == null)
                {
                    return _insightsComplete.ToList();
                }
                return _insightsComplete.Where(filter).ToList();
            }
        }

        /// <summary>
        /// Will return insights from the complete insight collection
        /// </summary>
        /// <param name="filter">The function that will determine which insight to return</param>
        /// <returns>A new list containing the selected insights</returns>
        public List<Insight> GetInsights(PyObject filter)
        {
            Func<Insight, bool> convertedFilter;
            if (filter.TryConvertToDelegate(out convertedFilter))
            {
                return GetInsights(convertedFilter);
            }
            else
            {
                using (Py.GIL())
                {
                    throw new ArgumentException($"InsightCollection.GetInsights: {filter.Repr()} is not a valid argument.");
                }
            }
        }
    }
}
