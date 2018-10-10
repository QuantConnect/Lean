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
using NodaTime;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Util;

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
                if (!_securityManager.ContainsKey(symbol))
                {
                    throw new Exception(string.Format("This asset symbol ({0}) was not found in your security list. Please add this security or check it exists before using it with 'Securities.ContainsKey(\"{1}\")'", symbol, SymbolCache.GetTicker(symbol)));
                }
                return _securityManager[symbol];
            }
            set
            {
                Security existing;
                if (_securityManager.TryGetValue(symbol, out existing) && existing != value)
                {
                    throw new ArgumentException("Unable to over write existing Security: " + symbol.ToString());
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
                    throw new Exception(string.Format("This asset symbol ({0}) was not found in your security list. Please add this security or check it exists before using it with 'Securities.ContainsKey(\"{0}\")'", ticker));
                }
                return this[symbol];
            }
            set
            {
                Symbol symbol;
                if (!SymbolCache.TryGetSymbol(ticker, out symbol))
                {
                    throw new Exception(string.Format("This asset symbol ({0}) was not found in your security list. Please add this security or check it exists before using it with 'Securities.ContainsKey(\"{0}\")'", ticker));
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
        /// Creates a security and matching configuration. This applies the default leverage if
        /// leverage is less than or equal to zero.
        /// This method also add the new symbol mapping to the <see cref="SymbolCache"/>
        /// </summary>
        public static Security CreateSecurity(List<Tuple<Type, TickType>> subscriptionDataTypes,
            SecurityPortfolioManager securityPortfolioManager,
            SubscriptionManager subscriptionManager,
            SecurityExchangeHours exchangeHours,
            DateTimeZone dataTimeZone,
            SymbolProperties symbolProperties,
            ISecurityInitializer securityInitializer,
            Symbol symbol,
            Resolution resolution,
            bool fillDataForward,
            decimal leverage,
            bool extendedMarketHours,
            bool isInternalFeed,
            bool isCustomData,
            bool isLiveMode,
            bool addToSymbolCache = true,
            bool isFilteredSubscription = true)
        {
            // add the symbol to our cache
            if (addToSymbolCache) SymbolCache.Set(symbol.Value, symbol);

            // Add the symbol to Data Manager -- generate unified data streams for algorithm events
            var configs = subscriptionManager.SubscriptionDataConfigService.Add(symbol, resolution, fillDataForward,
                                                                                extendedMarketHours, isFilteredSubscription, isInternalFeed,
                                                                                isCustomData, subscriptionDataTypes);
            var configList = new SubscriptionDataConfigList(symbol);
            configList.AddRange(configs);

            // verify the cash book is in a ready state
            var quoteCurrency = symbolProperties.QuoteCurrency;
            if (!securityPortfolioManager.CashBook.ContainsKey(quoteCurrency))
            {
                // since we have none it's safe to say the conversion is zero
                securityPortfolioManager.CashBook.Add(quoteCurrency, 0, 0);
            }
            if (symbol.ID.SecurityType == SecurityType.Forex || symbol.ID.SecurityType == SecurityType.Crypto)
            {
                // decompose the symbol into each currency pair
                string baseCurrency;
                Forex.Forex.DecomposeCurrencyPair(symbol.Value, out baseCurrency, out quoteCurrency);

                if (!securityPortfolioManager.CashBook.ContainsKey(baseCurrency))
                {
                    // since we have none it's safe to say the conversion is zero
                    securityPortfolioManager.CashBook.Add(baseCurrency, 0, 0);
                }
                if (!securityPortfolioManager.CashBook.ContainsKey(quoteCurrency))
                {
                    // since we have none it's safe to say the conversion is zero
                    securityPortfolioManager.CashBook.Add(quoteCurrency, 0, 0);
                }
            }

            var quoteCash = securityPortfolioManager.CashBook[symbolProperties.QuoteCurrency];

            Security security;
            switch (symbol.ID.SecurityType)
            {
                case SecurityType.Equity:
                    security = new Equity.Equity(symbol, exchangeHours, quoteCash, symbolProperties, securityPortfolioManager.CashBook);
                    break;

                case SecurityType.Option:
                    if (addToSymbolCache) SymbolCache.Set(symbol.Underlying.Value, symbol.Underlying);
                    security = new Option.Option(symbol, exchangeHours, securityPortfolioManager.CashBook[CashBook.AccountCurrency], new Option.OptionSymbolProperties(symbolProperties), securityPortfolioManager.CashBook);
                    break;

                case SecurityType.Future:
                    security = new Future.Future(symbol, exchangeHours, securityPortfolioManager.CashBook[CashBook.AccountCurrency], symbolProperties, securityPortfolioManager.CashBook);
                    break;

                case SecurityType.Forex:
                    security = new Forex.Forex(symbol, exchangeHours, quoteCash, symbolProperties, securityPortfolioManager.CashBook);
                    break;

                case SecurityType.Cfd:
                    security = new Cfd.Cfd(symbol, exchangeHours, quoteCash, symbolProperties, securityPortfolioManager.CashBook);
                    break;

                case SecurityType.Crypto:
                    security = new Crypto.Crypto(symbol, exchangeHours, quoteCash, symbolProperties, securityPortfolioManager.CashBook);
                    break;

                default:
                case SecurityType.Base:
                    security = new Security(symbol, exchangeHours, quoteCash, symbolProperties, securityPortfolioManager.CashBook);
                    break;
            }

            // if we're just creating this security and it only has an internal
            // feed, mark it as non-tradable since the user didn't request this data
            if (!isInternalFeed)
            {
                security.IsTradable = true;
            }

            security.AddData(configList);

            // invoke the security initializer
            securityInitializer.Initialize(security);

            // if leverage was specified then apply to security after the initializer has run, parameters of this
            // method take precedence over the intializer
            if (leverage > 0)
            {
                security.SetLeverage(leverage);
            }

            // In live mode, equity assumes specific price variation model
            if (isLiveMode && security.Type == SecurityType.Equity)
            {
                security.PriceVariationModel = new EquityPriceVariationModel();
            }

            return security;
        }

        /// <summary>
        /// Creates a security and matching configuration. This applies the default leverage if
        /// leverage is less than or equal to zero.
        /// This method also add the new symbol mapping to the <see cref="SymbolCache"/>
        /// </summary>
        public static Security CreateSecurity(Type dataType,
            SecurityPortfolioManager securityPortfolioManager,
            SubscriptionManager subscriptionManager,
            SecurityExchangeHours exchangeHours,
            DateTimeZone dataTimeZone,
            SymbolProperties symbolProperties,
            ISecurityInitializer securityInitializer,
            Symbol symbol,
            Resolution resolution,
            bool fillDataForward,
            decimal leverage,
            bool extendedMarketHours,
            bool isInternalFeed,
            bool isCustomData,
            bool isLiveMode,
            bool addToSymbolCache = true,
            bool isFilteredSubscription = true)
        {
            return CreateSecurity(
                new List<Tuple<Type, TickType>>
                {
                    new Tuple<Type, TickType>(dataType, LeanData.GetCommonTickTypeForCommonDataTypes(dataType, symbol.SecurityType))
                },
                securityPortfolioManager,
                subscriptionManager,
                exchangeHours,
                dataTimeZone,
                symbolProperties,
                securityInitializer,
                symbol,
                resolution,
                fillDataForward,
                leverage,
                extendedMarketHours,
                isInternalFeed,
                isCustomData,
                isLiveMode,
                addToSymbolCache,
                isFilteredSubscription);
        }

        /// <summary>
        /// Creates a security and matching configuration. This applies the default leverage if
        /// leverage is less than or equal to zero.
        /// This method also add the new symbol mapping to the <see cref="SymbolCache"/>
        /// </summary>
        public static Security CreateSecurity(SecurityPortfolioManager securityPortfolioManager,
            SubscriptionManager subscriptionManager,
            MarketHoursDatabase marketHoursDatabase,
            SymbolPropertiesDatabase symbolPropertiesDatabase,
            ISecurityInitializer securityInitializer,
            Symbol symbol,
            Resolution resolution,
            bool fillDataForward,
            decimal leverage,
            bool extendedMarketHours,
            bool isInternalFeed,
            bool isCustomData,
            bool isLiveMode,
            bool addToSymbolCache = true
            )
        {
            var marketHoursDbEntry = marketHoursDatabase.GetEntry(symbol.ID.Market, symbol, symbol.ID.SecurityType);
            var exchangeHours = marketHoursDbEntry.ExchangeHours;

            var defaultQuoteCurrency = CashBook.AccountCurrency;
            if (symbol.ID.SecurityType == SecurityType.Forex || symbol.ID.SecurityType == SecurityType.Crypto) defaultQuoteCurrency = symbol.Value.Substring(3);
            var symbolProperties = symbolPropertiesDatabase.GetSymbolProperties(symbol.ID.Market, symbol, symbol.ID.SecurityType, defaultQuoteCurrency);

            var types = subscriptionManager.LookupSubscriptionConfigDataTypes(symbol.SecurityType, resolution, symbol.IsCanonical());

            return CreateSecurity(types, securityPortfolioManager, subscriptionManager, exchangeHours, marketHoursDbEntry.DataTimeZone, symbolProperties, securityInitializer, symbol, resolution,
                fillDataForward, leverage, extendedMarketHours, isInternalFeed, isCustomData, isLiveMode, addToSymbolCache);
        }
    }
}
