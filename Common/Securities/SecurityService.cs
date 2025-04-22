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
using System.Linq;
using QuantConnect.Util;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using System.Collections.Generic;

namespace QuantConnect.Securities
{
    /// <summary>
    /// This class implements interface <see cref="ISecurityService"/> providing methods for creating new <see cref="Security"/>
    /// </summary>
    public class SecurityService : ISecurityService
    {
        private readonly CashBook _cashBook;
        private readonly MarketHoursDatabase _marketHoursDatabase;
        private readonly SymbolPropertiesDatabase _symbolPropertiesDatabase;
        private readonly IRegisteredSecurityDataTypesProvider _registeredTypes;
        private readonly ISecurityInitializerProvider _securityInitializerProvider;
        private readonly SecurityCacheProvider _cacheProvider;
        private readonly IPrimaryExchangeProvider _primaryExchangeProvider;
        private readonly IAlgorithm _algorithm;
        private bool _isLiveMode;
        private bool _modelsMismatchWarningSent;

        /// <summary>
        /// Creates a new instance of the SecurityService class
        /// </summary>
        public SecurityService(CashBook cashBook,
            MarketHoursDatabase marketHoursDatabase,
            SymbolPropertiesDatabase symbolPropertiesDatabase,
            ISecurityInitializerProvider securityInitializerProvider,
            IRegisteredSecurityDataTypesProvider registeredTypes,
            SecurityCacheProvider cacheProvider,
            IPrimaryExchangeProvider primaryExchangeProvider = null,
            IAlgorithm algorithm = null)
        {
            _cashBook = cashBook;
            _registeredTypes = registeredTypes;
            _marketHoursDatabase = marketHoursDatabase;
            _symbolPropertiesDatabase = symbolPropertiesDatabase;
            _securityInitializerProvider = securityInitializerProvider;
            _cacheProvider = cacheProvider;
            _primaryExchangeProvider = primaryExchangeProvider;
            _algorithm = algorithm;
        }

        /// <summary>
        /// Creates a new security
        /// </summary>
        /// <remarks>Following the obsoletion of Security.Subscriptions,
        /// both overloads will be merged removing <see cref="SubscriptionDataConfig"/> arguments</remarks>
        private Security CreateSecurity(Symbol symbol,
            List<SubscriptionDataConfig> subscriptionDataConfigList,
            decimal leverage,
            bool addToSymbolCache,
            Security underlying,
            bool initializeSecurity,
            bool reCreateSecurity)
        {
            var configList = new SubscriptionDataConfigList(symbol);
            configList.AddRange(subscriptionDataConfigList);

            if (!reCreateSecurity && _algorithm != null && _algorithm.Securities.TryGetValue(symbol, out var existingSecurity))
            {
                existingSecurity.AddData(configList);

                // If non-internal, mark as tradable if it was not already since this is an existing security but might include new subscriptions
                if (!configList.IsInternalFeed)
                {
                    existingSecurity.MakeTradable();
                }

                InitializeSecurity(initializeSecurity, existingSecurity);

                return existingSecurity;
            }

            var dataTypes = Enumerable.Empty<Type>();
            if(symbol.SecurityType == SecurityType.Base && SecurityIdentifier.TryGetCustomDataTypeInstance(symbol.ID.Symbol, out var type))
            {
                dataTypes = new[] { type };
            }
            var exchangeHours = _marketHoursDatabase.GetEntry(symbol, dataTypes).ExchangeHours;

            var defaultQuoteCurrency = _cashBook.AccountCurrency;
            if (symbol.ID.SecurityType == SecurityType.Forex)
            {
                defaultQuoteCurrency = symbol.Value.Substring(3);
            }

            if (symbol.ID.SecurityType == SecurityType.Crypto && !_symbolPropertiesDatabase.ContainsKey(symbol.ID.Market, symbol, symbol.ID.SecurityType))
            {
                throw new ArgumentException(Messages.SecurityService.SymbolNotFoundInSymbolPropertiesDatabase(symbol));
            }

            // For Futures Options that don't have a SPDB entry, the futures entry will be used instead.
            var symbolProperties = _symbolPropertiesDatabase.GetSymbolProperties(
                symbol.ID.Market,
                symbol,
                symbol.SecurityType,
                defaultQuoteCurrency);

            // add the symbol to our cache
            if (addToSymbolCache)
            {
                SymbolCache.Set(symbol.Value, symbol);
            }

            // verify the cash book is in a ready state
            var quoteCurrency = symbolProperties.QuoteCurrency;
            if (!_cashBook.TryGetValue(quoteCurrency, out var quoteCash))
            {
                // since we have none it's safe to say the conversion is zero
                quoteCash = _cashBook.Add(quoteCurrency, 0, 0);
            }

            Cash baseCash = null;
            // we skip cfd because we don't need to add the base cash
            if (symbol.SecurityType != SecurityType.Cfd)
            {
                if (CurrencyPairUtil.TryDecomposeCurrencyPair(symbol, out var baseCurrencySymbol, out _))
                {
                    if (!_cashBook.TryGetValue(baseCurrencySymbol, out baseCash))
                    {
                        // since we have none it's safe to say the conversion is zero
                        baseCash = _cashBook.Add(baseCurrencySymbol, 0, 0);
                    }
                }
                else if (CurrencyPairUtil.IsValidSecurityType(symbol.SecurityType, false))
                {
                    throw new ArgumentException($"Failed to resolve base currency for '{symbol.ID.Symbol}', it might be missing from the Symbol database or market '{symbol.ID.Market}' could be wrong");
                }
            }

            var cache = _cacheProvider.GetSecurityCache(symbol);

            Security security;
            switch (symbol.ID.SecurityType)
            {
                case SecurityType.Equity:
                    var primaryExchange =
                        _primaryExchangeProvider?.GetPrimaryExchange(symbol.ID) ??
                        Exchange.UNKNOWN;
                    security = new Equity.Equity(symbol, exchangeHours, quoteCash, symbolProperties, _cashBook, _registeredTypes, cache, primaryExchange);
                    break;

                case SecurityType.Option:
                    if (addToSymbolCache) SymbolCache.Set(symbol.Underlying.Value, symbol.Underlying);
                    security = new Option.Option(symbol, exchangeHours, quoteCash, new Option.OptionSymbolProperties(symbolProperties), _cashBook, _registeredTypes, cache, underlying);
                    break;

                case SecurityType.IndexOption:
                    if (addToSymbolCache) SymbolCache.Set(symbol.Underlying.Value, symbol.Underlying);
                    security = new IndexOption.IndexOption(symbol, exchangeHours, quoteCash, new IndexOption.IndexOptionSymbolProperties(symbolProperties), _cashBook, _registeredTypes, cache, underlying);
                    break;

                case SecurityType.FutureOption:
                    if (addToSymbolCache) SymbolCache.Set(symbol.Underlying.Value, symbol.Underlying);
                    var optionSymbolProperties = new Option.OptionSymbolProperties(symbolProperties);

                    // Future options exercised only gives us one contract back, rather than the
                    // 100x seen in equities.
                    optionSymbolProperties.SetContractUnitOfTrade(1);

                    security = new FutureOption.FutureOption(symbol, exchangeHours, quoteCash, optionSymbolProperties, _cashBook, _registeredTypes, cache, underlying);
                    break;

                case SecurityType.Future:
                    security = new Future.Future(symbol, exchangeHours, quoteCash, symbolProperties, _cashBook, _registeredTypes, cache);
                    break;

                case SecurityType.Forex:
                    security = new Forex.Forex(symbol, exchangeHours, quoteCash, baseCash, symbolProperties, _cashBook, _registeredTypes, cache);
                    break;

                case SecurityType.Cfd:
                    security = new Cfd.Cfd(symbol, exchangeHours, quoteCash, symbolProperties, _cashBook, _registeredTypes, cache);
                    break;

                case SecurityType.Index:
                    security = new Index.Index(symbol, exchangeHours, quoteCash, symbolProperties, _cashBook, _registeredTypes, cache);
                    break;

                case SecurityType.Crypto:
                    security = new Crypto.Crypto(symbol, exchangeHours, quoteCash, baseCash, symbolProperties, _cashBook, _registeredTypes, cache);
                    break;

                case SecurityType.CryptoFuture:
                    security = new CryptoFuture.CryptoFuture(symbol, exchangeHours, quoteCash, baseCash, symbolProperties, _cashBook, _registeredTypes, cache);
                    break;

                default:
                case SecurityType.Base:
                    security = new Security(symbol, exchangeHours, quoteCash, symbolProperties, _cashBook, _registeredTypes, cache);
                    break;
            }

            // if we're just creating this security and it only has an internal
            // feed, mark it as non-tradable since the user didn't request this data
            if (security.IsTradable)
            {
                security.IsTradable = !configList.IsInternalFeed;
            }

            security.AddData(configList);

            // invoke the security initializer
            InitializeSecurity(initializeSecurity, security);

            CheckCanonicalSecurityModels(security);

            // if leverage was specified then apply to security after the initializer has run, parameters of this
            // method take precedence over the intializer
            if (leverage != Security.NullLeverage)
            {
                security.SetLeverage(leverage);
            }

            var isNotNormalized = configList.DataNormalizationMode() == DataNormalizationMode.Raw;

            // In live mode and non normalized data, equity assumes specific price variation model
            if ((_isLiveMode || isNotNormalized) && security.Type == SecurityType.Equity)
            {
                security.PriceVariationModel = new EquityPriceVariationModel();
            }

            return security;
        }

        /// <summary>
        /// Creates a new security
        /// </summary>
        /// <remarks>Following the obsoletion of Security.Subscriptions,
        /// both overloads will be merged removing <see cref="SubscriptionDataConfig"/> arguments</remarks>
        public Security CreateSecurity(Symbol symbol,
            List<SubscriptionDataConfig> subscriptionDataConfigList,
            decimal leverage = 0,
            bool addToSymbolCache = true,
            Security underlying = null)
        {
            return CreateSecurity(symbol, subscriptionDataConfigList, leverage, addToSymbolCache, underlying,
                initializeSecurity: true, reCreateSecurity: false);
        }

        /// <summary>
        /// Creates a new security
        /// </summary>
        /// <remarks>Following the obsoletion of Security.Subscriptions,
        /// both overloads will be merged removing <see cref="SubscriptionDataConfig"/> arguments</remarks>
        public Security CreateSecurity(Symbol symbol, SubscriptionDataConfig subscriptionDataConfig, decimal leverage = 0, bool addToSymbolCache = true, Security underlying = null)
        {
            return CreateSecurity(symbol, new List<SubscriptionDataConfig> { subscriptionDataConfig }, leverage, addToSymbolCache, underlying);
        }

        /// <summary>
        /// Creates a new security
        /// </summary>
        /// <remarks>Following the obsoletion of Security.Subscriptions,
        /// both overloads will be merged removing <see cref="SubscriptionDataConfig"/> arguments</remarks>
        public Security CreateBenchmarkSecurity(Symbol symbol)
        {
            return CreateSecurity(symbol,
                new List<SubscriptionDataConfig>(),
                leverage: 1,
                addToSymbolCache: false,
                underlying: null,
                initializeSecurity: false,
                reCreateSecurity: true);
        }

        /// <summary>
        /// Set live mode state of the algorithm
        /// </summary>
        /// <param name="isLiveMode">True, live mode is enabled</param>
        public void SetLiveMode(bool isLiveMode)
        {
            _isLiveMode = isLiveMode;
        }

        /// <summary>
        /// Checks whether the created security has the same models as its canonical security (in case it has one)
        /// and sends a one-time warning if it doesn't.
        /// </summary>
        private void CheckCanonicalSecurityModels(Security security)
        {
            if (!_modelsMismatchWarningSent &&
                _algorithm != null &&
                security.Symbol.HasCanonical() &&
                _algorithm.Securities.TryGetValue(security.Symbol.Canonical, out var canonicalSecurity))
            {
                if (security.FillModel.GetType() != canonicalSecurity.FillModel.GetType() ||
                    security.FeeModel.GetType() != canonicalSecurity.FeeModel.GetType() ||
                    security.BuyingPowerModel.GetType() != canonicalSecurity.BuyingPowerModel.GetType() ||
                    security.MarginInterestRateModel.GetType() != canonicalSecurity.MarginInterestRateModel.GetType() ||
                    security.SlippageModel.GetType() != canonicalSecurity.SlippageModel.GetType() ||
                    security.VolatilityModel.GetType() != canonicalSecurity.VolatilityModel.GetType() ||
                    security.SettlementModel.GetType() != canonicalSecurity.SettlementModel.GetType())
                {
                    _modelsMismatchWarningSent = true;
                    _algorithm.Debug($"Warning: Security {security.Symbol} its canonical security {security.Symbol.Canonical} have at least one model of different types (fill, fee, buying power, margin interest rate, slippage, volatility, settlement). To avoid this, consider using a security initializer to set the right models to each security type according to your algorithm's requirements.");
                }
            }
        }

        private void InitializeSecurity(bool initializeSecurity, Security security)
        {
            if (initializeSecurity && !security.IsInitialized)
            {
                _securityInitializerProvider.SecurityInitializer.Initialize(security);
                security.IsInitialized = true;
            }
        }
    }
}
