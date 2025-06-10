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
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using ProtoBuf;
using QuantConnect.Data;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Securities.CurrencyConversion;

namespace QuantConnect.Securities
{
    /// <summary>
    /// Represents a holding of a currency in cash.
    /// </summary>
    [ProtoContract(SkipConstructor = true)]
    public class Cash
    {
        private ICurrencyConversion _currencyConversion;

        private readonly object _locker = new object();

        /// <summary>
        /// Event fired when this instance is updated
        /// <see cref="AddAmount"/>, <see cref="SetAmount"/>, <see cref="Update"/>
        /// </summary>
        public event EventHandler Updated;

        /// <summary>
        /// Event fired when this instance's <see cref="CurrencyConversion"/> is set/updated
        /// </summary>
        public event EventHandler CurrencyConversionUpdated;

        /// <summary>
        /// Gets the symbols of the securities required to provide conversion rates.
        /// If this cash represents the account currency, then an empty enumerable is returned.
        /// </summary>
        public IEnumerable<Symbol> SecuritySymbols => CurrencyConversion.ConversionRateSecurities.Any()
            ? CurrencyConversion.ConversionRateSecurities.Select(x => x.Symbol)
            // we do this only because Newtonsoft.Json complains about empty enumerables
            : new List<Symbol>(0);

        /// <summary>
        /// Gets the object that calculates the conversion rate to account currency
        /// </summary>
        [JsonIgnore]
        public ICurrencyConversion CurrencyConversion
        {
            get
            {
                return _currencyConversion;
            }
            internal set
            {

                var lastConversionRate = 0m;
                if (_currencyConversion != null)
                {
                    lastConversionRate = _currencyConversion.ConversionRate;
                    _currencyConversion.ConversionRateUpdated -= OnConversionRateUpdated;
                }

                _currencyConversion = value;
                if (_currencyConversion != null)
                {
                    if (lastConversionRate != 0m)
                    {
                        // If a user adds cash with an initial conversion rate and then this is overriden to a SecurityCurrencyConversion,
                        // we want to keep the previous rate until the new one is updated.
                        _currencyConversion.ConversionRate = lastConversionRate;
                    }
                    _currencyConversion.ConversionRateUpdated += OnConversionRateUpdated;
                }
                CurrencyConversionUpdated?.Invoke(this, EventArgs.Empty);
            }
        }

        private void OnConversionRateUpdated(object sender, decimal e)
        {
            OnUpdate();
        }

        /// <summary>
        /// Gets the symbol used to represent this cash
        /// </summary>
        [ProtoMember(1)]
        public string Symbol { get; }

        /// <summary>
        /// Gets or sets the amount of cash held
        /// </summary>
        [ProtoMember(2)]
        public decimal Amount { get; private set; }

        /// <summary>
        /// Gets the conversion rate into account currency
        /// </summary>
        [ProtoMember(3)]
        public decimal ConversionRate
        {
            get
            {
                return _currencyConversion.ConversionRate;
            }
            internal set
            {
                if (_currencyConversion == null)
                {
                    CurrencyConversion = new ConstantCurrencyConversion(Symbol, null, value);
                }

                _currencyConversion.ConversionRate = value;
            }
        }

        /// <summary>
        /// The symbol of the currency, such as $
        /// </summary>
        [ProtoMember(4)]
        public string CurrencySymbol { get; }

        /// <summary>
        /// Gets the value of this cash in the account currency
        /// </summary>
        public decimal ValueInAccountCurrency => Amount * ConversionRate;

        /// <summary>
        /// Initializes a new instance of the <see cref="Cash"/> class
        /// </summary>
        /// <param name="symbol">The symbol used to represent this cash</param>
        /// <param name="amount">The amount of this currency held</param>
        /// <param name="conversionRate">The initial conversion rate of this currency into the <see cref="CashBook.AccountCurrency"/></param>
        public Cash(string symbol, decimal amount, decimal conversionRate)
        {
            if (string.IsNullOrEmpty(symbol))
            {
                throw new ArgumentException(Messages.Cash.NullOrEmptyCashSymbol);
            }
            Amount = amount;
            Symbol = symbol.LazyToUpper();
            CurrencySymbol = Currencies.GetCurrencySymbol(Symbol);
            CurrencyConversion = new ConstantCurrencyConversion(Symbol, null, conversionRate);
        }

        /// <summary>
        /// Marks this cash object's conversion rate as being potentially outdated
        /// </summary>
        public void Update()
        {
            _currencyConversion.Update();
        }

        /// <summary>
        /// Adds the specified amount of currency to this Cash instance and returns the new total.
        /// This operation is thread-safe
        /// </summary>
        /// <param name="amount">The amount of currency to be added</param>
        /// <returns>The amount of currency directly after the addition</returns>
        public decimal AddAmount(decimal amount)
        {
            lock (_locker)
            {
                Amount += amount;
            }
            OnUpdate();
            return Amount;
        }

        /// <summary>
        /// Sets the Quantity to the specified amount
        /// </summary>
        /// <param name="amount">The amount to set the quantity to</param>
        public void SetAmount(decimal amount)
        {
            var updated = false;
            // lock can be null when proto deserializing this instance
            lock (_locker ?? new object())
            {
                if (Amount != amount)
                {
                    Amount = amount;
                    // only update if there was actually one
                    updated = true;
                }
            }

            if (updated)
            {
                OnUpdate();
            }
        }

        /// <summary>
        /// Ensures that we have a data feed to convert this currency into the base currency.
        /// This will add a <see cref="SubscriptionDataConfig"/> and create a <see cref="Security"/> at the lowest resolution if one is not found.
        /// </summary>
        /// <param name="securities">The security manager</param>
        /// <param name="subscriptions">The subscription manager used for searching and adding subscriptions</param>
        /// <param name="marketMap">The market map that decides which market the new security should be in</param>
        /// <param name="changes">Will be used to consume <see cref="SecurityChanges.AddedSecurities"/></param>
        /// <param name="securityService">Will be used to create required new <see cref="Security"/></param>
        /// <param name="accountCurrency">The account currency</param>
        /// <param name="defaultResolution">The default resolution to use for the internal subscriptions</param>
        /// <returns>Returns the added <see cref="SubscriptionDataConfig"/>, otherwise null</returns>
        public List<SubscriptionDataConfig> EnsureCurrencyDataFeed(SecurityManager securities,
            SubscriptionManager subscriptions,
            IReadOnlyDictionary<SecurityType, string> marketMap,
            SecurityChanges changes,
            ISecurityService securityService,
            string accountCurrency,
            Resolution defaultResolution = Resolution.Minute
            )
        {
            // this gets called every time we add securities using universe selection,
            // so must of the time we've already resolved the value and don't need to again
            if (CurrencyConversion.DestinationCurrency != null)
            {
                return null;
            }

            if (Symbol == accountCurrency)
            {
                CurrencyConversion = ConstantCurrencyConversion.Identity(accountCurrency);
                return null;
            }

            // existing securities
            var securitiesToSearch = securities.Select(kvp => kvp.Value)
                .Concat(changes.AddedSecurities)
                .Where(s => ProvidesConversionRate(s.Type));

            // Create a SecurityType to Market mapping with the markets from SecurityManager members
            var markets = securities.Select(x => x.Key)
                .GroupBy(x => x.SecurityType)
                .ToDictionary(x => x.Key, y => y.Select(symbol => symbol.ID.Market).ToHashSet());
            if (markets.ContainsKey(SecurityType.Cfd) && !markets.ContainsKey(SecurityType.Forex))
            {
                markets.Add(SecurityType.Forex, markets[SecurityType.Cfd]);
            }
            if (markets.ContainsKey(SecurityType.Forex) && !markets.ContainsKey(SecurityType.Cfd))
            {
                markets.Add(SecurityType.Cfd, markets[SecurityType.Forex]);
            }

            var forexEntries = GetAvailableSymbolPropertiesDatabaseEntries(SecurityType.Forex, marketMap, markets);
            var cfdEntries = GetAvailableSymbolPropertiesDatabaseEntries(SecurityType.Cfd, marketMap, markets);
            var cryptoEntries = GetAvailableSymbolPropertiesDatabaseEntries(SecurityType.Crypto, marketMap, markets);

            var potentialEntries = forexEntries
                .Concat(cfdEntries)
                .Concat(cryptoEntries)
                .ToList();

            if (!potentialEntries.Any(x =>
                    Symbol == x.Key.Symbol.Substring(0, x.Key.Symbol.Length - x.Value.QuoteCurrency.Length) ||
                    Symbol == x.Value.QuoteCurrency))
            {
                // currency not found in any tradeable pair
                Log.Error(Messages.Cash.NoTradablePairFoundForCurrencyConversion(Symbol, accountCurrency, marketMap.Where(kvp => ProvidesConversionRate(kvp.Key))));
                CurrencyConversion = ConstantCurrencyConversion.Null(accountCurrency, Symbol);
                return null;
            }

            // Special case for crypto markets without direct pairs (They wont be found by the above)
            // This allows us to add cash for "StableCoins" that are 1-1 with our account currency without needing a conversion security.
            // Check out the StableCoinsWithoutPairs static var for those that are missing their 1-1 conversion pairs
            if (marketMap.TryGetValue(SecurityType.Crypto, out var market)
                &&
                (Currencies.IsStableCoinWithoutPair(Symbol + accountCurrency, market)
                || Currencies.IsStableCoinWithoutPair(accountCurrency + Symbol, market)))
            {
                CurrencyConversion = ConstantCurrencyConversion.Identity(accountCurrency, Symbol);
                return null;
            }

            var requiredSecurities = new List<SubscriptionDataConfig>();

            var potentials = potentialEntries
                .Select(x => QuantConnect.Symbol.Create(x.Key.Symbol, x.Key.SecurityType, x.Key.Market));

            var minimumResolution = subscriptions.Subscriptions.Select(x => x.Resolution).DefaultIfEmpty(defaultResolution).Min();

            var makeNewSecurity = new Func<Symbol, Security>(symbol =>
            {
                var securityType = symbol.ID.SecurityType;

                // use the first subscription defined in the subscription manager
                var type = subscriptions.LookupSubscriptionConfigDataTypes(securityType, minimumResolution, false).First();
                var objectType = type.Item1;
                var tickType = type.Item2;

                // set this as an internal feed so that the data doesn't get sent into the algorithm's OnData events
                var config = subscriptions.SubscriptionDataConfigService.Add(symbol,
                    minimumResolution,
                    fillForward: true,
                    extendedMarketHours: false,
                    isInternalFeed: true,
                    subscriptionDataTypes: new List<Tuple<Type, TickType>>
                        {new Tuple<Type, TickType>(objectType, tickType)}).First();

                var newSecurity = securityService.CreateSecurity(symbol,
                    config,
                    addToSymbolCache: false);

                Log.Trace("Cash.EnsureCurrencyDataFeed(): " + Messages.Cash.AddingSecuritySymbolForCashCurrencyFeed(symbol, Symbol));

                securities.Add(symbol, newSecurity);
                requiredSecurities.Add(config);

                return newSecurity;
            });

            CurrencyConversion = SecurityCurrencyConversion.LinearSearch(Symbol,
                accountCurrency,
                securitiesToSearch.ToList(),
                potentials,
                makeNewSecurity);

            return requiredSecurities;
        }

        /// <summary>
        /// Returns a <see cref="string"/> that represents the current <see cref="Cash"/>.
        /// </summary>
        /// <returns>A <see cref="string"/> that represents the current <see cref="Cash"/>.</returns>
        public override string ToString()
        {
            return ToString(Currencies.USD);
        }

        /// <summary>
        /// Returns a <see cref="string"/> that represents the current <see cref="Cash"/>.
        /// </summary>
        /// <returns>A <see cref="string"/> that represents the current <see cref="Cash"/>.</returns>
        public string ToString(string accountCurrency)
        {
            return Messages.Cash.ToString(this, accountCurrency);
        }

        private static IEnumerable<KeyValuePair<SecurityDatabaseKey, SymbolProperties>> GetAvailableSymbolPropertiesDatabaseEntries(
            SecurityType securityType,
            IReadOnlyDictionary<SecurityType, string> marketMap,
            IReadOnlyDictionary<SecurityType, HashSet<string>> markets
            )
        {
            var marketJoin = new HashSet<string>();
            {
                string market;
                if (marketMap.TryGetValue(securityType, out market))
                {
                    marketJoin.Add(market);
                }
                HashSet<string> existingMarkets;
                if (markets.TryGetValue(securityType, out existingMarkets))
                {
                    foreach (var existingMarket in existingMarkets)
                    {
                        marketJoin.Add(existingMarket);
                    }
                }
            }

            return marketJoin.SelectMany(market => SymbolPropertiesDatabase.FromDataFolder()
                .GetSymbolPropertiesList(market, securityType));
        }

        private static bool ProvidesConversionRate(SecurityType securityType)
        {
            return securityType == SecurityType.Forex || securityType == SecurityType.Crypto || securityType == SecurityType.Cfd;
        }

        private void OnUpdate()
        {
            Updated?.Invoke(this, EventArgs.Empty);
        }
    }
}
