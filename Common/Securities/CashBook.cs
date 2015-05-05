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
using System.Text;
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
        public const string AccountCurrency = "USD";

        private readonly Dictionary<string, Cash> _currencies;

        /// <summary>
        /// Gets the total value of the cash book in units of the base currency
        /// </summary>
        public decimal TotalValueInAccountCurrency
        {
            get { return _currencies.Values.Sum(x => x.ValueInAccountCurrency); }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CashBook"/> class.
        /// </summary>
        public CashBook()
        {
            _currencies = new Dictionary<string, Cash>();
            _currencies.Add(AccountCurrency, new Cash(AccountCurrency, 0, 1.0m));
        }

        /// <summary>
        /// Update the current conversion rate for each cash type
        /// </summary>
        /// <param name="data">The new, current data</param>
        public void Update(Dictionary<int, List<BaseData>> data)
        {
            foreach (var cash in _currencies.Values)
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
            var cash = new Cash(symbol, quantity, conversionRate);
            _currencies.Add(symbol, cash);
        }

        /// <summary>
        /// Checks the current subscriptions and adds necessary currency pair feeds to provide real time conversion data
        /// </summary>
        /// <param name="securities">The SecurityManager for the algorithm</param>
        /// <param name="subscriptions">The SubscriptionManager for the algorithm</param>
        public void EnsureCurrencyDataFeeds(SecurityManager securities, SubscriptionManager subscriptions)
        {
            foreach (var cash in _currencies.Values)
            {
                cash.EnsureCurrencyDataFeed(securities, subscriptions);
            }
        }

        /// <summary>
        /// Converts a quantity of source currency units into the specified destination currency
        /// </summary>
        /// <param name="sourceQuantity">The quantity of source currency to be converted</param>
        /// <param name="sourceCurrency">The source currency symbol</param>
        /// <param name="destinationCurrency">The destination currency symbol</param>
        /// <returns>The converted value</returns>
        public decimal Convert(decimal sourceQuantity, string sourceCurrency, string destinationCurrency)
        {
            var source = this[sourceCurrency];
            var destination = this[destinationCurrency];
            var conversionRate = source.ConversionRate*destination.ConversionRate;
            return sourceQuantity*conversionRate;
        }

        /// <summary>
        /// Converts a quantity of source currency units into the account currency
        /// </summary>
        /// <param name="sourceQuantity">The quantity of source currency to be converted</param>
        /// <param name="sourceCurrency">The source currency symbol</param>
        /// <returns>The converted value</returns>
        public decimal ConvertToAccountCurrency(decimal sourceQuantity, string sourceCurrency)
        {
            return Convert(sourceQuantity, sourceCurrency, AccountCurrency);
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendFormat("{0}: {1,10} @ ${2,10} = {3}", "Sym", "Quantity", "Conversion", "Value in {4}" + CashBook.AccountCurrency);
            foreach (var value in Values)
            {
                sb.AppendLine(value.ToString());
            }
            sb.AppendLine("-----------------------------------------");
            sb.AppendLine(string.Format("CashBook Total Value: {0, 30}", TotalValueInAccountCurrency.ToString("C")));
            return sb.ToString();
        }

        #region IDictionary Implementation

        public int Count
        {
            get { return _currencies.Count; }
        }

        public bool IsReadOnly
        {
            get { return ((IDictionary<string, Cash>) _currencies).IsReadOnly; }
        }

        public void Add(KeyValuePair<string, Cash> item)
        {
            _currencies.Add(item.Key, item.Value);
        }

        public void Add(string key, Cash value)
        {
            _currencies.Add(key, value);
        }

        public void Clear()
        {
            _currencies.Clear();
        }

        public bool Remove(string key)
        {
            return _currencies.Remove(key);
        }

        public bool Remove(KeyValuePair<string, Cash> item)
        {
            return _currencies.Remove(item.Key);
        }

        public bool ContainsKey(string key)
        {
            return _currencies.ContainsKey(key);
        }

        public bool TryGetValue(string key, out Cash value)
        {
            return _currencies.TryGetValue(key, out value);
        }

        public bool Contains(KeyValuePair<string, Cash> item)
        {
            return _currencies.Contains(item);
        }

        public void CopyTo(KeyValuePair<string, Cash>[] array, int arrayIndex)
        {
            ((IDictionary<string, Cash>) _currencies).CopyTo(array, arrayIndex);
        }

        public Cash this[string symbol]
        {
            get
            {
                Cash cash;
                if (!_currencies.TryGetValue(symbol, out cash))
                {
                    throw new Exception("This cash symbol (" + symbol + ") was not found in your cash book.");
                }
                return cash;
            }
            set { _currencies[symbol] = value; }
        }

        public ICollection<string> Keys
        {
            get { return _currencies.Keys; }
        }

        public ICollection<Cash> Values
        {
            get { return _currencies.Values; }
        }

        public IEnumerator<KeyValuePair<string, Cash>> GetEnumerator()
        {
            return _currencies.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable) _currencies).GetEnumerator();
        }

        #endregion
    }
}