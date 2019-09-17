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
using QuantConnect.Data;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using static QuantConnect.StringExtensions;

namespace QuantConnect.Securities
{
    /// <summary>
    /// Represents a holding of a currency in cash.
    /// </summary>
    public class Cash
    {
        private decimal _conversionRate;
        private bool _isBaseCurrency;
        private bool _invertRealTimePrice;

        private readonly object _locker = new object();

        /// <summary>
        /// Event fired when this instance is updated
        /// <see cref="AddAmount"/>, <see cref="SetAmount"/>, <see cref="Update"/>
        /// </summary>
        public event EventHandler Updated;

        /// <summary>
        /// Gets the symbol of the security required to provide conversion rates.
        /// If this cash represents the account currency, then <see cref="QuantConnect.Symbol.Empty"/>
        /// is returned
        /// </summary>
        public Symbol SecuritySymbol => ConversionRateSecurity?.Symbol ?? QuantConnect.Symbol.Empty;

        /// <summary>
        /// Gets the security used to apply conversion rates.
        /// If this cash represents the account currency, then null is returned.
        /// </summary>
        [JsonIgnore]
        public Security ConversionRateSecurity { get; private set; }

        /// <summary>
        /// Gets the symbol used to represent this cash
        /// </summary>
        public string Symbol { get; }

        /// <summary>
        /// Gets or sets the amount of cash held
        /// </summary>
        public decimal Amount { get; private set; }

        /// <summary>
        /// Gets the conversion rate into account currency
        /// </summary>
        public decimal ConversionRate
        {
            get
            {
                return _conversionRate;
            }
            internal set
            {
                _conversionRate = value;
                OnUpdate();
            }
        }

        /// <summary>
        /// The symbol of the currency, such as $
        /// </summary>
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
        /// <param name="conversionRate">The initial conversion rate of this currency into the <see cref="AccountCurrency"/></param>
        public Cash(string symbol, decimal amount, decimal conversionRate)
        {
            if (string.IsNullOrEmpty(symbol))
            {
                throw new ArgumentException("Cash symbols cannot be null or empty.");
            }
            Amount = amount;
            ConversionRate = conversionRate;
            Symbol = symbol.LazyToUpper();
            CurrencySymbol = Currencies.GetCurrencySymbol(Symbol);
        }

        /// <summary>
        /// Updates this cash object with the specified data
        /// </summary>
        /// <param name="data">The new data for this cash object</param>
        public void Update(BaseData data)
        {
            if (_isBaseCurrency) return;

            var rate = data.Value;
            if (_invertRealTimePrice)
            {
                rate = 1/rate;
            }
            ConversionRate = rate;
            OnUpdate();
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
            lock (_locker)
            {
                Amount = amount;
            }
            OnUpdate();
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
        /// <returns>Returns the added <see cref="SubscriptionDataConfig"/>, otherwise null</returns>
        public SubscriptionDataConfig EnsureCurrencyDataFeed(SecurityManager securities,
            SubscriptionManager subscriptions,
            IReadOnlyDictionary<SecurityType, string> marketMap,
            SecurityChanges changes,
            ISecurityService securityService,
            string accountCurrency
            )
        {
            // this gets called every time we add securities using universe selection,
            // so must of the time we've already resolved the value and don't need to again
            if (ConversionRateSecurity != null)
            {
                return null;
            }

            if (Symbol == accountCurrency)
            {
                ConversionRateSecurity = null;
                _isBaseCurrency = true;
                ConversionRate = 1.0m;
                return null;
            }

            // we require a security that converts this into the base currency
            string normal = Symbol + accountCurrency;
            string invert = accountCurrency + Symbol;
            var securitiesToSearch = securities.Select(kvp => kvp.Value)
                .Concat(changes.AddedSecurities)
                .Where(s => s.Type == SecurityType.Forex || s.Type == SecurityType.Cfd || s.Type == SecurityType.Crypto);

            foreach (var security in securitiesToSearch)
            {
                if (security.Symbol.Value == normal)
                {
                    ConversionRateSecurity = security;
                    return null;
                }
                if (security.Symbol.Value == invert)
                {
                    ConversionRateSecurity = security;
                    _invertRealTimePrice = true;
                    return null;
                }
            }
            // if we've made it here we didn't find a security, so we'll need to add one

            // Create a SecurityType to Market mapping with the markets from SecurityManager members
            var markets = securities.Select(x => x.Key).GroupBy(x => x.SecurityType).ToDictionary(x => x.Key, y => y.First().ID.Market);
            if (markets.ContainsKey(SecurityType.Cfd) && !markets.ContainsKey(SecurityType.Forex))
            {
                markets.Add(SecurityType.Forex, markets[SecurityType.Cfd]);
            }
            if (markets.ContainsKey(SecurityType.Forex) && !markets.ContainsKey(SecurityType.Cfd))
            {
                markets.Add(SecurityType.Cfd, markets[SecurityType.Forex]);
            }

            var potentials = Currencies.CurrencyPairs.Select(fx => CreateSymbol(marketMap, fx, markets, SecurityType.Forex))
                .Concat(Currencies.CfdCurrencyPairs.Select(cfd => CreateSymbol(marketMap, cfd, markets, SecurityType.Cfd)))
                .Concat(Currencies.CryptoCurrencyPairs.Select(crypto => CreateSymbol(marketMap, crypto, markets, SecurityType.Crypto)));

            var minimumResolution = subscriptions.Subscriptions.Select(x => x.Resolution).DefaultIfEmpty(Resolution.Minute).Min();

            foreach (var symbol in potentials)
            {
                if (symbol.Value == normal || symbol.Value == invert)
                {
                    _invertRealTimePrice = symbol.Value == invert;
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
                        subscriptionDataTypes: new List<Tuple<Type, TickType>> { new Tuple<Type, TickType>(objectType, tickType) }).First();

                    var security = securityService.CreateSecurity(symbol,
                        config,
                        addToSymbolCache: false);

                    ConversionRateSecurity = security;
                    securities.Add(config.Symbol, security);
                    Log.Trace($"Cash.EnsureCurrencyDataFeed(): Adding {symbol.Value} for cash {Symbol} currency feed");
                    return config;
                }
            }

            // if this still hasn't been set then it's an error condition
            throw new ArgumentException($"In order to maintain cash in {Symbol} you are required to add a " +
                $"subscription for Forex pair {Symbol}{accountCurrency} or {accountCurrency}{Symbol}"
            );
        }

        /// <summary>
        /// Returns a <see cref="string"/> that represents the current <see cref="Cash"/>.
        /// </summary>
        /// <returns>A <see cref="string"/> that represents the current <see cref="Cash"/>.</returns>
        public override string ToString()
        {
            // round the conversion rate for output
            var rate = ConversionRate;
            rate = rate < 1000 ? rate.RoundToSignificantDigits(5) : Math.Round(rate, 2);
            return Invariant($"{Symbol}: {CurrencySymbol}{Amount,15:0.00} @ {rate,10:0.00####} = ${Math.Round(ValueInAccountCurrency, 2)}");
        }

        private static Symbol CreateSymbol(IReadOnlyDictionary<SecurityType, string> marketMap, string crypto, Dictionary<SecurityType, string> markets, SecurityType securityType)
        {
            string market;
            if (!markets.TryGetValue(securityType, out market))
            {
                market = marketMap[securityType];
            }

            return QuantConnect.Symbol.Create(crypto, securityType, market);
        }

        private void OnUpdate()
        {
            Updated?.Invoke(this, EventArgs.Empty);
        }
    }
}