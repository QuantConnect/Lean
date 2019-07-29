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
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Interfaces;

namespace QuantConnect.Securities
{
    /// <summary>
    /// Enumerable security management class for grouping security objects into an array and providing any common properties.
    /// </summary>
    /// <remarks>Implements IDictionary for the index searching of securities by symbol</remarks>
    public class SecurityManager : IDictionary<Symbol, Security>, INotifyCollectionChanged
    {
        /// <summary>
        /// Event fired when a security is added or removed from this collection
        /// </summary>
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        private readonly ITimeKeeper _timeKeeper;

        //Internal dictionary implementation:
        private readonly ConcurrentDictionary<Symbol, Security> _securityManager;
        private SecurityService _securityService;

        /// <summary>
        /// Gets the most recent time this manager was updated
        /// </summary>
        public DateTime UtcTime
        {
            get { return _timeKeeper.UtcTime; }
        }

        /// <summary>
        /// Initialise the algorithm security manager with two empty dictionaries
        /// </summary>
        /// <param name="timeKeeper"></param>
        public SecurityManager(ITimeKeeper timeKeeper)
        {
            _timeKeeper = timeKeeper;
            _securityManager = new ConcurrentDictionary<Symbol, Security>();
        }

        /// <summary>
        /// Add a new security with this symbol to the collection.
        /// </summary>
        /// <remarks>IDictionary implementation</remarks>
        /// <param name="symbol">symbol for security we're trading</param>
        /// <param name="security">security object</param>
        /// <seealso cref="Add(Security)"/>
        public void Add(Symbol symbol, Security security)
        {
            if (_securityManager.TryAdd(symbol, security))
            {
                security.SetLocalTimeKeeper(_timeKeeper.GetLocalTimeKeeper(security.Exchange.TimeZone));
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, security));
            }
        }

        /// <summary>
        /// Add a new security with this symbol to the collection.
        /// </summary>
        /// <param name="security">security object</param>
        public void Add(Security security)
        {
            Add(security.Symbol, security);
        }

        /// <summary>
        /// Add a symbol-security by its key value pair.
        /// </summary>
        /// <remarks>IDictionary implementation</remarks>
        /// <param name="pair"></param>
        public void Add(KeyValuePair<Symbol, Security> pair)
        {
            Add(pair.Key, pair.Value);
        }

        /// <summary>
        /// Clear the securities array to delete all the portfolio and asset information.
        /// </summary>
        /// <remarks>IDictionary implementation</remarks>
        public void Clear()
        {
            _securityManager.Clear();
        }

        /// <summary>
        /// Check if this collection contains this key value pair.
        /// </summary>
        /// <param name="pair">Search key-value pair</param>
        /// <remarks>IDictionary implementation</remarks>
        /// <returns>Bool true if contains this key-value pair</returns>
        public bool Contains(KeyValuePair<Symbol, Security> pair)
        {
            return _securityManager.Contains(pair);
        }

        /// <summary>
        /// Check if this collection contains this symbol.
        /// </summary>
        /// <param name="symbol">Symbol we're checking for.</param>
        /// <remarks>IDictionary implementation</remarks>
        /// <returns>Bool true if contains this symbol pair</returns>
        public bool ContainsKey(Symbol symbol)
        {
            return _securityManager.ContainsKey(symbol);
        }

        /// <summary>
        /// Copy from the internal array to an external array.
        /// </summary>
        /// <param name="array">Array we're outputting to</param>
        /// <param name="number">Starting index of array</param>
        /// <remarks>IDictionary implementation</remarks>
        public void CopyTo(KeyValuePair<Symbol, Security>[] array, int number)
        {
            ((IDictionary<Symbol, Security>)_securityManager).CopyTo(array, number);
        }

        /// <summary>
        /// Count of the number of securities in the collection.
        /// </summary>
        /// <remarks>IDictionary implementation</remarks>
        public int Count => _securityManager.Skip(0).Count();

        /// <summary>
        /// Flag indicating if the internal arrray is read only.
        /// </summary>
        /// <remarks>IDictionary implementation</remarks>
        public bool IsReadOnly
        {
            get { return false;  }
        }

        /// <summary>
        /// Remove a key value of of symbol-securities from the collections.
        /// </summary>
        /// <remarks>IDictionary implementation</remarks>
        /// <param name="pair">Key Value pair of symbol-security to remove</param>
        /// <returns>Boolean true on success</returns>
        public bool Remove(KeyValuePair<Symbol, Security> pair)
        {
            return Remove(pair.Key);
        }

        /// <summary>
        /// Remove this symbol security: Dictionary interface implementation.
        /// </summary>
        /// <param name="symbol">Symbol we're searching for</param>
        /// <returns>true success</returns>
        public bool Remove(Symbol symbol)
        {
            Security security;
            if (_securityManager.TryRemove(symbol, out security))
            {
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, security));
                return true;
            }
            return false;
        }

        /// <summary>
        /// List of the symbol-keys in the collection of securities.
        /// </summary>
        /// <remarks>IDictionary implementation</remarks>
        public ICollection<Symbol> Keys => _securityManager.Select(x => x.Key).ToList();

        /// <summary>
        /// Try and get this security object with matching symbol and return true on success.
        /// </summary>
        /// <param name="symbol">String search symbol</param>
        /// <param name="security">Output Security object</param>
        /// <remarks>IDictionary implementation</remarks>
        /// <returns>True on successfully locating the security object</returns>
        public bool TryGetValue(Symbol symbol, out Security security)
        {
            return _securityManager.TryGetValue(symbol, out security);
        }

        /// <summary>
        /// Get a list of the security objects for this collection.
        /// </summary>
        /// <remarks>IDictionary implementation</remarks>
        public ICollection<Security> Values => _securityManager.Select(x => x.Value).ToList();

        /// <summary>
        /// Get the enumerator for this security collection.
        /// </summary>
        /// <remarks>IDictionary implementation</remarks>
        /// <returns>Enumerable key value pair</returns>
        IEnumerator<KeyValuePair<Symbol, Security>> IEnumerable<KeyValuePair<Symbol, Security>>.GetEnumerator()
        {
            return _securityManager.GetEnumerator();
        }

        /// <summary>
        /// Get the enumerator for this securities collection.
        /// </summary>
        /// <remarks>IDictionary implementation</remarks>
        /// <returns>Enumerator.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _securityManager.GetEnumerator();
        }

        /// <summary>
        /// Indexer method for the security manager to access the securities objects by their symbol.
        /// </summary>
        /// <remarks>IDictionary implementation</remarks>
        /// <param name="symbol">Symbol object indexer</param>
        /// <returns>Security</returns>
        public Security this[Symbol symbol]
        {
            get
            {
                Security security;
                if (!_securityManager.TryGetValue(symbol, out security))
                {
                    throw new KeyNotFoundException($"This asset symbol ({symbol}) was not found in your security list. Please add this security or check it exists before using it with 'Securities.ContainsKey(\"{SymbolCache.GetTicker(symbol)}\")'");
                }
                return security;
            }
            set
            {
                Security existing;
                if (_securityManager.TryGetValue(symbol, out existing) && existing != value)
                {
                    throw new ArgumentException($"Unable to over write existing Security: {symbol}");
                }

                // no security exists for the specified symbol key, add it now
                if (existing == null)
                {
                    Add(symbol, value);
                }
            }
        }

        /// <summary>
        /// Indexer method for the security manager to access the securities objects by their symbol.
        /// </summary>
        /// <remarks>IDictionary implementation</remarks>
        /// <param name="ticker">string ticker symbol indexer</param>
        /// <returns>Security</returns>
        public Security this[string ticker]
        {
            get
            {
                Symbol symbol;
                if (!SymbolCache.TryGetSymbol(ticker, out symbol))
                {
                    throw new KeyNotFoundException($"This asset symbol ({ticker}) was not found in your security list. Please add this security or check it exists before using it with 'Securities.ContainsKey(\"{ticker}\")'");
                }
                return this[symbol];
            }
            set
            {
                Symbol symbol;
                if (!SymbolCache.TryGetSymbol(ticker, out symbol))
                {
                    throw new KeyNotFoundException($"This asset symbol ({ticker}) was not found in your security list. Please add this security or check it exists before using it with 'Securities.ContainsKey(\"{ticker}\")'");
                }
                this[symbol] = value;
            }
        }

        /// <summary>
        /// Event invocator for the <see cref="CollectionChanged"/> event
        /// </summary>
        /// <param name="changedEventArgs">Event arguments for the <see cref="CollectionChanged"/> event</param>
        protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs changedEventArgs)
        {
            var handler = CollectionChanged;
            if (handler != null) handler(this, changedEventArgs);
        }

        /// <summary>
        /// Sets the Security Service to be used
        /// </summary>
        public void SetSecurityService(SecurityService securityService)
        {
            _securityService = securityService;
        }

        /// <summary>
        /// Creates a new security
        /// </summary>
        /// <remarks>Following the obsoletion of Security.Subscriptions,
        /// both overloads will be merged removing <see cref="SubscriptionDataConfig"/> arguments</remarks>
        public Security CreateSecurity(
            Symbol symbol,
            List<SubscriptionDataConfig> subscriptionDataConfigList,
            decimal leverage = 0,
            bool addToSymbolCache = true)
        {
            return _securityService.CreateSecurity(symbol, subscriptionDataConfigList, leverage, addToSymbolCache);
        }


        /// <summary>
        /// Creates a new security
        /// </summary>
        /// <remarks>Following the obsoletion of Security.Subscriptions,
        /// both overloads will be merged removing <see cref="SubscriptionDataConfig"/> arguments</remarks>
        public Security CreateSecurity(
            Symbol symbol,
            SubscriptionDataConfig subscriptionDataConfig,
            decimal leverage = 0,
            bool addToSymbolCache = true
            )
        {
            return _securityService.CreateSecurity(symbol, subscriptionDataConfig, leverage, addToSymbolCache);
        }

        /// <summary>
        /// Set live mode state of the algorithm
        /// </summary>
        /// <param name="isLiveMode">True, live mode is enabled</param>
        public void SetLiveMode(bool isLiveMode)
        {
            _securityService.SetLiveMode(isLiveMode);
        }
    }
}
