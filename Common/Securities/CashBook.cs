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
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Util;

namespace QuantConnect.Securities
{
    /// <summary>
    /// Provides a means of keeping track of the different cash holdings of an algorithm
    /// </summary>
    public class CashBook : ExtendedDictionary<string, Cash>, IDictionary<string, Cash>, ICurrencyConverter
    {
        private string _accountCurrency;

        /// <summary>
        /// Event fired when a <see cref="Cash"/> instance is added or removed, and when
        /// the <see cref="Cash.Updated"/> is triggered for the currently hold instances
        /// </summary>
        public event EventHandler<CashBookUpdatedEventArgs> Updated;

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

        /// <summary>
        /// No need for concurrent collection, they are expensive. Currencies barely change and only on the start
        /// by the main thread, so if they do we will just create a new collection, reference change is atomic
        /// </summary>
        private Dictionary<string, Cash> _currencies;

        /// <summary>
        /// Gets the total value of the cash book in units of the base currency
        /// </summary>
        public decimal TotalValueInAccountCurrency
        {
            get
            {
                return this.Aggregate(0m, (d, pair) => d + pair.Value.ValueInAccountCurrency);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CashBook"/> class.
        /// </summary>
        public CashBook()
        {
            _currencies = new();
            AccountCurrency = Currencies.USD;
        }

        /// <summary>
        /// Adds a new cash of the specified symbol and quantity
        /// </summary>
        /// <param name="symbol">The symbol used to reference the new cash</param>
        /// <param name="quantity">The amount of new cash to start</param>
        /// <param name="conversionRate">The conversion rate used to determine the initial
        /// portfolio value/starting capital impact caused by this currency position.</param>
        /// <returns>The added cash instance</returns>
        public Cash Add(string symbol, decimal quantity, decimal conversionRate)
        {
            var cash = new Cash(symbol, quantity, conversionRate);
            // let's return the cash instance we are using
            return AddIternal(symbol, cash);
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

                var subscriptionDataConfigs = cash.EnsureCurrencyDataFeed(
                    securities,
                    subscriptions,
                    marketMap,
                    changes,
                    securityService,
                    AccountCurrency,
                    defaultResolution);
                if (subscriptionDataConfigs != null)
                {
                    foreach (var subscriptionDataConfig in subscriptionDataConfigs)
                    {
                        addedSubscriptionDataConfigs.Add(subscriptionDataConfig);
                    }
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
                throw new ArgumentException(Messages.CashBook.ConversionRateNotFound(sourceCurrency));
            }

            if (destination.ConversionRate == 0)
            {
                throw new ArgumentException(Messages.CashBook.ConversionRateNotFound(destinationCurrency));
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
            return Messages.CashBook.ToString(this);
        }

        #region IDictionary Implementation

        /// <summary>
        /// Gets the count of Cash items in this CashBook.
        /// </summary>
        /// <value>The count.</value>
        public override int Count
        {
            get
            {
                return _currencies.Count;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is read only.
        /// </summary>
        /// <value><c>true</c> if this instance is read only; otherwise, <c>false</c>.</value>
        public override bool IsReadOnly
        {
            get { return false; }
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
            AddIternal(symbol, value);
        }

        /// <summary>
        /// Clear this instance of all Cash entries.
        /// </summary>
        public override void Clear()
        {
            _currencies = new();
            OnUpdate(CashBookUpdateType.Removed, null);
        }

        /// <summary>
        /// Remove the Cash item corresponding to the specified symbol
        /// </summary>
        /// <param name="symbol">The symbolto be removed</param>
        public override bool Remove(string symbol)
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
        public override bool ContainsKey(string symbol)
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
        public override bool TryGetValue(string symbol, out Cash value)
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
        public override Cash this[string symbol]
        {
            get
            {
                if (symbol == Currencies.NullCurrency)
                {
                    throw new InvalidOperationException(Messages.CashBook.UnexpectedRequestForNullCurrency);
                }
                Cash cash;
                if (!_currencies.TryGetValue(symbol, out cash))
                {
                    throw new KeyNotFoundException(Messages.CashBook.CashSymbolNotFound(symbol));
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
        public ICollection<string> Keys => _currencies.Keys;

        /// <summary>
        /// Gets the values.
        /// </summary>
        /// <value>The values.</value>
        public ICollection<Cash> Values => _currencies.Values;

        /// <summary>
        /// Gets the keys.
        /// </summary>
        /// <value>The keys.</value>
        protected override IEnumerable<string> GetKeys => Keys;

        /// <summary>
        /// Gets the values.
        /// </summary>
        /// <value>The values.</value>
        protected override IEnumerable<Cash> GetValues => Values;

        /// <summary>
        /// Gets all the items in the dictionary
        /// </summary>
        /// <returns>All the items in the dictionary</returns>
        public override IEnumerable<KeyValuePair<string, Cash>> GetItems() => _currencies;

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
            return _currencies.GetEnumerator();
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

        private Cash AddIternal(string symbol, Cash value)
        {
            if (symbol == Currencies.NullCurrency)
            {
                return null;
            }

            if (!_currencies.TryGetValue(symbol, out var cash))
            {
                // we link our Updated event with underlying cash instances
                // so interested listeners just subscribe to our event
                value.Updated += OnCashUpdate;
                var newCurrencies = new Dictionary<string, Cash>(_currencies)
                {
                    [symbol] = value
                };
                _currencies = newCurrencies;

                OnUpdate(CashBookUpdateType.Added, value);

                return value;
            }
            else
            {
                // override the values, it will trigger an update event already
                // we keep the instance because it might be used by securities already
                cash.ConversionRate = value.ConversionRate;
                cash.SetAmount(value.Amount);

                return cash;
            }
        }

        private bool Remove(string symbol, bool calledInternally)
        {
            Cash cash = null;
            var newCurrencies = new Dictionary<string, Cash>(_currencies);
            var removed = newCurrencies.Remove(symbol, out cash);
            _currencies = newCurrencies;
            if (!removed)
            {
                if (!calledInternally)
                {
                    Log.Error("CashBook.Remove(): " + Messages.CashBook.FailedToRemoveRecord(symbol));
                }
            }
            else
            {
                cash.Updated -= OnCashUpdate;
                if (!calledInternally)
                {
                    OnUpdate(CashBookUpdateType.Removed, cash);
                }
            }
            return removed;
        }

        private void OnCashUpdate(object sender, EventArgs eventArgs)
        {
            OnUpdate(CashBookUpdateType.Updated, sender as Cash);
        }

        private void OnUpdate(CashBookUpdateType updateType, Cash cash)
        {
            Updated?.Invoke(this, new CashBookUpdatedEventArgs(updateType, cash));
        }
    }
}
