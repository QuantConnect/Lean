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
using QuantConnect.Data;
using System.Collections;
using QuantConnect.Interfaces;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace QuantConnect.Securities
{
    /// <summary>
    /// Enumerable security management class for grouping security objects into an array and providing any common properties.
    /// </summary>
    /// <remarks>Implements IDictionary for the index searching of securities by symbol</remarks>
    public class SecurityManager : ExtendedDictionary<Security>, IDictionary<Symbol, Security>, INotifyCollectionChanged
    {
        /// <summary>
        /// Event fired when a security is added or removed from this collection
        /// </summary>
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        private readonly ITimeKeeper _timeKeeper;

        //Internal dictionary implementation:
        private readonly Dictionary<Symbol, Security> _securityManager;
        private readonly Dictionary<Symbol, Security> _completeSecuritiesCollection;
        // let's keep ah thread safe enumerator created which we reset and recreate if required
        private List<Symbol> _enumeratorKeys;
        private List<Security> _enumeratorValues;
        private List<KeyValuePair<Symbol, Security>> _enumerator;
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
            _securityManager = new();
            _completeSecuritiesCollection = new();
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
            bool changed;
            lock (_securityManager)
            {
                changed = _securityManager.TryAdd(symbol, security);
                if (changed)
                {
                    _completeSecuritiesCollection[symbol] = security;
                }
            }

            if (changed)
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
        public override void Clear()
        {
            lock (_securityManager)
            {
                _enumerator = null;
                _enumeratorKeys = null;
                _enumeratorValues = null;
                _securityManager.Clear();
                _completeSecuritiesCollection.Clear();
            }
        }

        /// <summary>
        /// Check if this collection contains this key value pair.
        /// </summary>
        /// <param name="pair">Search key-value pair</param>
        /// <remarks>IDictionary implementation</remarks>
        /// <returns>Bool true if contains this key-value pair</returns>
        public bool Contains(KeyValuePair<Symbol, Security> pair)
        {
            lock (_securityManager)
            {
                return _completeSecuritiesCollection.Contains(pair);
            }
        }

        /// <summary>
        /// Check if this collection contains this symbol.
        /// </summary>
        /// <param name="symbol">Symbol we're checking for.</param>
        /// <remarks>IDictionary implementation</remarks>
        /// <returns>Bool true if contains this symbol pair</returns>
        public bool ContainsKey(Symbol symbol)
        {
            lock (_securityManager)
            {
                return _completeSecuritiesCollection.ContainsKey(symbol);
            }
        }

        /// <summary>
        /// Copy from the internal array to an external array.
        /// </summary>
        /// <param name="array">Array we're outputting to</param>
        /// <param name="number">Starting index of array</param>
        /// <remarks>IDictionary implementation</remarks>
        public void CopyTo(KeyValuePair<Symbol, Security>[] array, int number)
        {
            lock (_securityManager)
            {
                ((IDictionary<Symbol, Security>)_securityManager).CopyTo(array, number);
            }
        }

        /// <summary>
        /// Count of the number of securities in the collection.
        /// </summary>
        /// <remarks>IDictionary implementation</remarks>
        public int Count
        {
            get
            {
                lock (_securityManager)
                {
                    return _securityManager.Count;
                }
            }
        }

        /// <summary>
        /// Flag indicating if the internal array is read only.
        /// </summary>
        /// <remarks>IDictionary implementation</remarks>
        public override bool IsReadOnly
        {
            get { return false; }
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
        public override bool Remove(Symbol symbol)
        {
            Security security;
            lock (_securityManager)
            {
                _securityManager.Remove(symbol, out security);
            }

            if (security != null)
            {
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, security));
                return true;
            }
            return false;
        }

        /// <summary>
        /// List of the symbol-keys in the collection of securities.
        /// </summary>
        /// <remarks>Excludes non active or delisted securities</remarks>
        public ICollection<Symbol> Keys
        {
            get
            {
                var result = _enumeratorKeys;
                if (result == null)
                {
                    lock (_securityManager)
                    {
                        _enumeratorKeys = result = _securityManager.Keys.ToList();
                    }
                }
                return result;
            }
        }

        /// <summary>
        /// Try and get this security object with matching symbol and return true on success.
        /// </summary>
        /// <param name="symbol">String search symbol</param>
        /// <param name="security">Output Security object</param>
        /// <remarks>IDictionary implementation</remarks>
        /// <returns>True on successfully locating the security object</returns>
        public override bool TryGetValue(Symbol symbol, out Security security)
        {
            lock (_securityManager)
            {
                return _completeSecuritiesCollection.TryGetValue(symbol, out security);
            }
        }

        /// <summary>
        /// Gets an <see cref="T:System.Collections.Generic.ICollection`1"/> containing the Symbol objects of the <see cref="T:System.Collections.Generic.IDictionary`2"/>.
        /// </summary>
        /// <remarks>Excludes non active or delisted securities</remarks>
        /// <returns>
        /// An <see cref="T:System.Collections.Generic.ICollection`1"/> containing the Symbol objects of the object that implements <see cref="T:System.Collections.Generic.IDictionary`2"/>.
        /// </returns>
        protected override IEnumerable<Symbol> GetKeys => Keys;

        /// <summary>
        /// Gets an <see cref="T:System.Collections.Generic.ICollection`1"/> containing the values in the <see cref="T:System.Collections.Generic.IDictionary`2"/>.
        /// </summary>
        /// <remarks>Excludes non active or delisted securities</remarks>
        /// <returns>
        /// An <see cref="T:System.Collections.Generic.ICollection`1"/> containing the values in the object that implements <see cref="T:System.Collections.Generic.IDictionary`2"/>.
        /// </returns>
        protected override IEnumerable<Security> GetValues => Values;

        /// <summary>
        /// Get a list of the security objects for this collection.
        /// </summary>
        /// <remarks>Excludes non active or delisted securities</remarks>
        public ICollection<Security> Values
        {
            get
            {
                var result = _enumeratorValues;
                if (result == null)
                {
                    lock (_securityManager)
                    {
                        _enumeratorValues = result = _securityManager.Values.ToList();
                    }
                }
                return result;
            }
        }

        /// <summary>
        /// Get a list of the complete security objects for this collection, including non active or delisted securities
        /// </summary>
        public ICollection<Security> Total
        {
            get
            {
                ICollection<Security> result;
                lock (_securityManager)
                {
                    result = _completeSecuritiesCollection.Values.ToList();
                }
                return result;
            }
        }

        /// <summary>
        /// Get the enumerator for this security collection.
        /// </summary>
        /// <remarks>IDictionary implementation</remarks>
        /// <returns>Enumerable key value pair</returns>
        IEnumerator<KeyValuePair<Symbol, Security>> IEnumerable<KeyValuePair<Symbol, Security>>.GetEnumerator()
        {
            return GetEnumeratorImplementation();
        }

        /// <summary>
        /// Get the enumerator for this securities collection.
        /// </summary>
        /// <remarks>IDictionary implementation</remarks>
        /// <returns>Enumerator.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumeratorImplementation();
        }

        private List<KeyValuePair<Symbol, Security>>.Enumerator GetEnumeratorImplementation()
        {
            var result = _enumerator;
            if (result == null)
            {
                lock (_securityManager)
                {
                    _enumerator = result = _securityManager.ToList();
                }
            }
            return result.GetEnumerator();
        }

        /// <summary>
        /// Indexer method for the security manager to access the securities objects by their symbol.
        /// </summary>
        /// <remarks>IDictionary implementation</remarks>
        /// <param name="symbol">Symbol object indexer</param>
        /// <returns>Security</returns>
        public override Security this[Symbol symbol]
        {
            get
            {
                Security security;
                lock (_securityManager)
                {
                    if (!_completeSecuritiesCollection.TryGetValue(symbol, out security))
                    {
                        throw new KeyNotFoundException(Messages.SecurityManager.SymbolNotFoundInSecurities(symbol));
                    }
                }
                return security;
            }
            set
            {
                Security existing;
                lock (_securityManager)
                {
                    if (_securityManager.TryGetValue(symbol, out existing) && existing != value)
                    {
                        throw new ArgumentException(Messages.SecurityManager.UnableToOverwriteSecurity(symbol));
                    }
                }

                // no security exists for the specified symbol key, add it now
                if (existing == null)
                {
                    Add(symbol, value);
                }
            }
        }

        /// <summary>
        /// Event invocator for the <see cref="CollectionChanged"/> event
        /// </summary>
        /// <param name="changedEventArgs">Event arguments for the <see cref="CollectionChanged"/> event</param>
        protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs changedEventArgs)
        {
            _enumerator = null;
            _enumeratorKeys = null;
            _enumeratorValues = null;
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
            bool addToSymbolCache = true,
            Security underlying = null)
        {
            return _securityService.CreateSecurity(symbol, subscriptionDataConfigList, leverage, addToSymbolCache, underlying);
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
            bool addToSymbolCache = true,
            Security underlying = null
            )
        {
            return _securityService.CreateSecurity(symbol, subscriptionDataConfig, leverage, addToSymbolCache, underlying);
        }

        /// <summary>
        /// Creates a new benchmark security
        /// </summary>
        public Security CreateBenchmarkSecurity(Symbol symbol)
        {
            return _securityService.CreateBenchmarkSecurity(symbol);
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
