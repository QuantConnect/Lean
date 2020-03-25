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
    public class SecurityManager : BaseDictionary<Security>, INotifyCollectionChanged
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
            SetInternalDictionary(_securityManager);
        }

        /// <summary>
        /// Add a new security with this symbol to the collection.
        /// </summary>
        /// <remarks>IDictionary implementation</remarks>
        /// <param name="symbol">symbol for security we're trading</param>
        /// <param name="security">security object</param>
        /// <seealso cref="Add(Security)"/>
        public new void Add(Symbol symbol, Security security)
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
        public new void Add(KeyValuePair<Symbol, Security> pair)
        {
            Add(pair.Key, pair.Value);
        }

        /// <summary>
        /// Count of the number of securities in the collection.
        /// </summary>
        /// <remarks>IDictionary implementation</remarks>
        public new int Count => _securityManager.Skip(0).Count();

        /// <summary>
        /// Flag indicating if the internal arrray is read only.
        /// </summary>
        /// <remarks>IDictionary implementation</remarks>
        public new bool IsReadOnly => false;

        /// <summary>
        /// Remove this symbol security: Dictionary interface implementation.
        /// </summary>
        /// <param name="symbol">Symbol we're searching for</param>
        /// <returns>true success</returns>
        public new bool Remove(Symbol symbol)
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
        public new ICollection<Symbol> Keys => _securityManager.Select(x => x.Key).ToList();

        /// <summary>
        /// Get a list of the security objects for this collection.
        /// </summary>
        /// <remarks>IDictionary implementation</remarks>
        public new ICollection<Security> Values => _securityManager.Select(x => x.Value).ToList();

        /// <summary>
        /// Indexer method for the security manager to access the securities objects by their symbol.
        /// </summary>
        /// <remarks>IDictionary implementation</remarks>
        /// <param name="symbol">Symbol object indexer</param>
        /// <returns>Security</returns>
        public new Security this[Symbol symbol]
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
        public new Security this[string ticker]
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
            CollectionChanged?.Invoke(this, changedEventArgs);
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
