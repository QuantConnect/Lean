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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data;

namespace QuantConnect.Securities
{
    /// <summary>
    /// Provides a means of keeping track of the different cash holdings of an algorithm
    /// </summary>
    public class CashBook : IDictionary<string, Cash>
    {
        /// <summary>
        /// Gets the base currency used
        /// </summary>
        public const string BaseCurrency = "USD";

        private readonly Dictionary<string, Cash> _storage;
        private readonly SubscriptionManager _subscriptionManager;

        /// <summary>
        /// Gets the total value of the cash book in units of the base currency
        /// </summary>
        public decimal ValueInBaseCurrency
        {
            get { return _storage.Values.Sum(x => x.ValueInBaseCurrency); }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CashBook"/> class.
        /// </summary>
        /// <param name="subscriptionManager">The subscription manager used to look up subscription configurations</param>
        public CashBook(SubscriptionManager subscriptionManager)
        {
            _subscriptionManager = subscriptionManager;
            _storage = new Dictionary<string, Cash>();
            _storage.Add(BaseCurrency, new Cash(BaseCurrency, subscriptionManager));
        }

        /// <summary>
        /// Update the current conversion rate for each cash type
        /// </summary>
        /// <param name="data">The new, current data</param>
        public void Update(Dictionary<int, List<BaseData>> data)
        {
            foreach (var cash in _storage.Values)
            {
                cash.Update(data);
            }
        }

        /// <summary>
        /// Adds a new cash of the specified symbol and quantity
        /// </summary>
        /// <param name="symbol">The symbol used to reference the new cash</param>
        /// <param name="quantity">The amount of new cash to start</param>
        /// <param name="conversionRate">The conversion rate used to determine the initial
        /// portfolio value/starting capital impact caused by this currency position.</param>
        public void Add(string symbol, decimal quantity, decimal conversionRate)
        {
            var cash = new Cash(symbol, _subscriptionManager) {Quantity = quantity};
            cash.SetConversionRate(conversionRate);
            _storage.Add(symbol, cash);
        }

        #region IDictionary Implementation

        public int Count
        {
            get { return _storage.Count; }
        }

        public bool IsReadOnly
        {
            get { return ((IDictionary<string, Cash>) _storage).IsReadOnly; }
        }

        public void Add(KeyValuePair<string, Cash> item)
        {
            _storage.Add(item.Key, item.Value);
        }

        public void Add(string key, Cash value)
        {
            _storage.Add(key, value);
        }

        public void Clear()
        {
            _storage.Clear();
        }

        public bool Remove(string key)
        {
            return _storage.Remove(key);
        }

        public bool Remove(KeyValuePair<string, Cash> item)
        {
            return _storage.Remove(item.Key);
        }

        public bool ContainsKey(string key)
        {
            return _storage.ContainsKey(key);
        }

        public bool TryGetValue(string key, out Cash value)
        {
            return _storage.TryGetValue(key, out value);
        }

        public bool Contains(KeyValuePair<string, Cash> item)
        {
            return _storage.Contains(item);
        }

        public void CopyTo(KeyValuePair<string, Cash>[] array, int arrayIndex)
        {
            ((IDictionary<string, Cash>) _storage).CopyTo(array, arrayIndex);
        }

        public Cash this[string symbol]
        {
            get
            {
                Cash cash;
                if (!_storage.TryGetValue(symbol, out cash))
                {
                    throw new Exception("This cash symbol (" + symbol + ") was not found in your cash book.");
                }
                return cash;
            }
            set { _storage[symbol] = value; }
        }

        public ICollection<string> Keys
        {
            get { return _storage.Keys; }
        }

        public ICollection<Cash> Values
        {
            get { return _storage.Values; }
        }

        public IEnumerator<KeyValuePair<string, Cash>> GetEnumerator()
        {
            return _storage.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable) _storage).GetEnumerator();
        }

        #endregion
    }
}