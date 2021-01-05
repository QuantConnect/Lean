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
using System.Collections.Concurrent;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Util;
using static QuantConnect.StringExtensions;

namespace QuantConnect.Securities
{
    /// <summary>
    /// Provides a means of keeping track of the different cash holdings of an algorithm
    /// </summary>
    public class CashBook : IDictionary<string, Cash>, ICurrencyConverter
    {
        private string _accountCurrency;

        /// <summary>
        /// Event fired when a <see cref="Cash"/> instance is added or removed, and when
        /// the <see cref="Cash.Updated"/> is triggered for the currently hold instances
        /// </summary>
        public event EventHandler<UpdateType> Updated;

        /// <summary>
        /// Gets the base currency used
        /// </summary>
        public string AccountCurrency
        {
            get { return _accountCurrency; }
            set
            {
                var amount = 0m;
                Cash accountCurrency;
                // remove previous account currency if any
                if (!_accountCurrency.IsNullOrEmpty()
                    && TryGetValue(_accountCurrency, out accountCurrency))
                {
                    amount = accountCurrency.Amount;
                    Remove(_accountCurrency);
                }

                // add new account currency using same amount as previous
                _accountCurrency = value.LazyToUpper();
                Add(_accountCurrency, new Cash(_accountCurrency, amount, 1.0m));
            }
        }

        private readonly ConcurrentDictionary<string, Cash> _currencies;

        /// <summary>
        /// Gets the total value of the cash book in units of the base currency
        /// </summary>
        public decimal TotalValueInAccountCurrency
        {
            get { return _currencies.Aggregate(0m, (d, pair) => d + pair.Value.ValueInAccountCurrency); }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CashBook"/> class.
        /// </summary>
        public CashBook()
        {
            _currencies = new ConcurrentDictionary<string, Cash>();
            AccountCurrency = Currencies.USD;
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
            Add(symbol, cash);
        }

        /// <summary>
        /// Checks the current subscriptions and adds necessary currency pair feeds to provide real time conversion data
        /// </summary>
        /// <param name="securities">The SecurityManager for the algorithm</param>
        /// <param name="subscriptions">The SubscriptionManager for the algorithm</param>
        /// <param name="marketMap">The market map that decides which market the new security should be in</param>
        /// <param name="changes">Will be used to consume <see cref="SecurityChanges.AddedSecurities"/></param>
        /// <param name="securityService">Will be used to create required new <see cref="Security"/></param>
        /// <param name="defaultResolution">The default resolution to use for the internal subscriptions</param>
        /// <returns>Returns a list of added currency <see cref="SubscriptionDataConfig"/></returns>
        public List<SubscriptionDataConfig> EnsureCurrencyDataFeeds(SecurityManager securities,
            SubscriptionManager subscriptions,
            IReadOnlyDictionary<SecurityType, string> marketMap,
            SecurityChanges changes,
            ISecurityService securityService,
            Resolution defaultResolution = Resolution.Minute)
        {
            var addedSubscriptionDataConfigs = new List<SubscriptionDataConfig>();
            foreach (var kvp in _currencies)
            {
                var cash = kvp.Value;

                var subscriptionDataConfig = cash.EnsureCurrencyDataFeed(
                    securities,
                    subscriptions,
                    marketMap,
                    changes,
                    securityService,
                    AccountCurrency,
                    defaultResolution);
                if (subscriptionDataConfig != null)
                {
                    addedSubscriptionDataConfigs.Add(subscriptionDataConfig);
                }
            }
            return addedSubscriptionDataConfigs;
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
            if (sourceQuantity == 0)
            {
                return 0;
            }

            var source = this[sourceCurrency];
            var destination = this[destinationCurrency];

            if (source.ConversionRate == 0)
            {
                throw new ArgumentException($"The conversion rate for {sourceCurrency} is not available.");
            }

            if (destination.ConversionRate == 0)
            {
                throw new ArgumentException($"The conversion rate for {destinationCurrency} is not available.");
            }

            var conversionRate = source.ConversionRate / destination.ConversionRate;
            return sourceQuantity * conversionRate;
        }

        /// <summary>
        /// Converts a quantity of source currency units into the account currency
        /// </summary>
        /// <param name="sourceQuantity">The quantity of source currency to be converted</param>
        /// <param name="sourceCurrency">The source currency symbol</param>
        /// <returns>The converted value</returns>
        public decimal ConvertToAccountCurrency(decimal sourceQuantity, string sourceCurrency)
        {
            if (sourceCurrency == AccountCurrency)
            {
                return sourceQuantity;
            }
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
            sb.AppendLine(Invariant($"Symbol {"Quantity",13}    {"Conversion",10} = Value in {AccountCurrency}"));
            foreach (var value in _currencies.Select(x => x.Value))
            {
                sb.AppendLine(value.ToString(AccountCurrency));
            }
            sb.AppendLine("-------------------------------------------------");
            sb.AppendLine("CashBook Total Value:                " +
                Invariant($"{Currencies.GetCurrencySymbol(AccountCurrency)}") +
                Invariant($"{Math.Round(TotalValueInAccountCurrency, 2).ToStringInvariant()}")
            );

            return sb.ToString();
        }

        #region IDictionary Implementation

        /// <summary>
        /// Gets the count of Cash items in this CashBook.
        /// </summary>
        /// <value>The count.</value>
        public int Count => _currencies.Skip(0).Count();

        /// <summary>
        /// Gets a value indicating whether this instance is read only.
        /// </summary>
        /// <value><c>true</c> if this instance is read only; otherwise, <c>false</c>.</value>
        public bool IsReadOnly
        {
            get { return ((IDictionary<string, Cash>) _currencies).IsReadOnly; }
        }

        /// <summary>
        /// Add the specified item to this CashBook.
        /// </summary>
        /// <param name="item">KeyValuePair of symbol -> Cash item</param>
        public void Add(KeyValuePair<string, Cash> item)
        {
            Add(item.Key, item.Value);
        }

        /// <summary>
        /// Add the specified key and value.
        /// </summary>
        /// <param name="symbol">The symbol of the Cash value.</param>
        /// <param name="value">Value.</param>
        public void Add(string symbol, Cash value)
        {
            if (symbol == Currencies.NullCurrency)
            {
                return;
            }
            // we link our Updated event with underlying cash instances
            // so interested listeners just subscribe to our event
            value.Updated += OnCashUpdate;

            var alreadyExisted = Remove(symbol, calledInternally: true);

            _currencies.AddOrUpdate(symbol, value);

            OnUpdate(alreadyExisted ? UpdateType.Updated : UpdateType.Added);
        }

        /// <summary>
        /// Clear this instance of all Cash entries.
        /// </summary>
        public void Clear()
        {
            _currencies.Clear();
            OnUpdate(UpdateType.Removed);
        }

        /// <summary>
        /// Remove the Cash item corresponding to the specified symbol
        /// </summary>
        /// <param name="symbol">The symbolto be removed</param>
        public bool Remove(string symbol)
        {
            return Remove(symbol, calledInternally: false);
        }

        /// <summary>
        /// Remove the specified item.
        /// </summary>
        /// <param name="item">Item.</param>
        public bool Remove(KeyValuePair<string, Cash> item)
        {
            return Remove(item.Key);
        }

        /// <summary>
        /// Determines whether the current instance contains an entry with the specified symbol.
        /// </summary>
        /// <returns><c>true</c>, if key was contained, <c>false</c> otherwise.</returns>
        /// <param name="symbol">Key.</param>
        public bool ContainsKey(string symbol)
        {
            return _currencies.ContainsKey(symbol);
        }

        /// <summary>
        /// Try to get the value.
        /// </summary>
        /// <remarks>To be added.</remarks>
        /// <returns><c>true</c>, if get value was tryed, <c>false</c> otherwise.</returns>
        /// <param name="symbol">The symbol.</param>
        /// <param name="value">Value.</param>
        public bool TryGetValue(string symbol, out Cash value)
        {
            return _currencies.TryGetValue(symbol, out value);
        }

        /// <summary>
        /// Determines whether the current collection contains the specified value.
        /// </summary>
        /// <param name="item">Item.</param>
        public bool Contains(KeyValuePair<string, Cash> item)
        {
            return _currencies.Contains(item);
        }

        /// <summary>
        /// Copies to the specified array.
        /// </summary>
        /// <param name="array">Array.</param>
        /// <param name="arrayIndex">Array index.</param>
        public void CopyTo(KeyValuePair<string, Cash>[] array, int arrayIndex)
        {
            ((IDictionary<string, Cash>) _currencies).CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Gets or sets the <see cref="QuantConnect.Securities.Cash"/> with the specified symbol.
        /// </summary>
        /// <param name="symbol">Symbol.</param>
        public Cash this[string symbol]
        {
            get
            {
                if (symbol == Currencies.NullCurrency)
                {
                    throw new InvalidOperationException(
                        "Unexpected request for NullCurrency Cash instance");
                }
                Cash cash;
                if (!_currencies.TryGetValue(symbol, out cash))
                {
                    throw new KeyNotFoundException($"This cash symbol ({symbol}) was not found in your cash book.");
                }
                return cash;
            }
            set
            {
                Add(symbol, value);
            }
        }

        /// <summary>
        /// Gets the keys.
        /// </summary>
        /// <value>The keys.</value>
        public ICollection<string> Keys => _currencies.Select(x => x.Key).ToList();

        /// <summary>
        /// Gets the values.
        /// </summary>
        /// <value>The values.</value>
        public ICollection<Cash> Values => _currencies.Select(x => x.Value).ToList();

        /// <summary>
        /// Gets the enumerator.
        /// </summary>
        /// <returns>The enumerator.</returns>
        public IEnumerator<KeyValuePair<string, Cash>> GetEnumerator()
        {
            return _currencies.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable) _currencies).GetEnumerator();
        }

        #endregion

        #region ICurrencyConverter Implementation

        /// <summary>
        /// Converts a cash amount to the account currency
        /// </summary>
        /// <param name="cashAmount">The <see cref="CashAmount"/> instance to convert</param>
        /// <returns>A new <see cref="CashAmount"/> instance denominated in the account currency</returns>
        public CashAmount ConvertToAccountCurrency(CashAmount cashAmount)
        {
            if (cashAmount.Currency == AccountCurrency)
            {
                return cashAmount;
            }

            var amount = Convert(cashAmount.Amount, cashAmount.Currency, AccountCurrency);
            return new CashAmount(amount, AccountCurrency);
        }

        #endregion

        private bool Remove(string symbol, bool calledInternally)
        {
            Cash cash = null;
            var removed = _currencies.TryRemove(symbol, out cash);
            if (!removed)
            {
                if (!calledInternally)
                {
                    Log.Error($"CashBook.Remove(): Failed to remove the cash book record for symbol {symbol}");
                }
            }
            else
            {
                cash.Updated -= OnCashUpdate;
                if (!calledInternally)
                {
                    OnUpdate(UpdateType.Removed);
                }
            }
            return removed;
        }

        private void OnCashUpdate(object sender, EventArgs eventArgs)
        {
            OnUpdate(UpdateType.Updated);
        }

        private void OnUpdate(UpdateType updateType)
        {
            Updated?.Invoke(this, updateType);
        }

        /// <summary>
        /// The different types of <see cref="Updated"/> events
        /// </summary>
        public enum UpdateType
        {
            /// <summary>
            /// A new <see cref="Cash.Symbol"/> was added
            /// </summary>
            Added,
            /// <summary>
            /// One or more <see cref="Cash"/> instances were removed
            /// </summary>
            Removed,
            /// <summary>
            /// An existing <see cref="Cash.Symbol"/> was updated
            /// </summary>
            Updated
        }
    }
}