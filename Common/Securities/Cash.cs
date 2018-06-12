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
using QuantConnect.Logging;
using QuantConnect.Securities.Graph;

namespace QuantConnect.Securities
{
    /// <summary>
    /// Represents a holding of a currency in cash.
    /// </summary>
    public class Cash
    {
        /// <summary>
        /// Class that holds Security for conversion and bool, which tells if rate should be inverted (1/rate)
        /// </summary>
        public class ConversionSecurity
        {
            public Security RateSecurity;
            public bool inverted = false;
            public decimal ConversionRate; 

            public ConversionSecurity(Security RateSecurity, bool inverted = false)
            {
                this.RateSecurity = RateSecurity;
                this.inverted = inverted;

                if (RateSecurity.Price != 0)
                {
                    if (inverted == false)
                        ConversionRate = RateSecurity.Price;
                    else
                        ConversionRate = 1m / RateSecurity.Price;
                }
            }
        }

        private bool _isBaseCurrency;
        
        private readonly object _locker = new object();

        /// <summary>
        /// Gets the symbol of the security required to provide conversion rates.
        /// If this cash represents the account currency, then <see cref="QuantConnect.Symbol.Empty"/>
        /// is returned
        /// </summary>
        public List<Symbol> SecuritySymbol 
        {
            get
            {
                return (ConversionRateSecurity == null? new List<Symbol>() : ConversionRateSecurity.Select(conSec => conSec.RateSecurity.Symbol).ToList());
            }
        }
        
        /// <summary>
        /// Gets the security used to apply conversion rates.
        /// If this cash represents the account currency, then null is returned.
        /// </summary>
        [JsonIgnore]
        public List<ConversionSecurity> ConversionRateSecurity { get; private set; }

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
        public decimal ConversionRate { get; internal set; }

        /// <summary>y
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
        /// <param name="conversionRate">The initial conversion rate of this currency into the <see cref="CashBook.AccountCurrency"/></param>
        public Cash(string symbol, decimal amount, decimal conversionRate)
        {
            if (symbol == null || symbol.Length < 3 || symbol.Length > Currencies.MaxCharactersPerCurrencyCode)
            {
                throw new ArgumentException($"Cash symbols must have atleast 3 characters and at most {Currencies.MaxCharactersPerCurrencyCode} characters.");
            }

            Amount = amount;
            ConversionRate = conversionRate;
            Symbol = symbol.ToUpper();
            CurrencySymbol = Currencies.GetCurrencySymbol(Symbol);
        }

        /// <summary>
        /// Updates this cash object with the specified data
        /// </summary>
        /// <param name="data">The new data for this cash object</param>
        public void Update(BaseData data)
        {
            if (_isBaseCurrency) return;

            foreach(ConversionSecurity conSec in ConversionRateSecurity)
            {
                if (conSec.RateSecurity.Symbol == data.Symbol)
                {
                    var rate = data.Value;

                    if (conSec.inverted)
                        rate = 1 / rate;

                    conSec.ConversionRate = rate;
                }
            }

            ConversionRate = 1m;
            foreach (ConversionSecurity conSec in ConversionRateSecurity)
            {
                ConversionRate *= conSec.ConversionRate;
            }

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
                return Amount;
            }
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
        }

        /// <summary>
        /// Ensures that we have a data feed to convert this currency into the base currency.
        /// This will add a subscription at the lowest resolution if one is not found.
        /// </summary>
        /// <param name="securities">The security manager</param>
        /// <param name="subscriptions">The subscription manager used for searching and adding subscriptions</param>
        /// <param name="marketHoursDatabase">A security exchange hours provider instance used to resolve exchange hours for new subscriptions</param>
        /// <param name="symbolPropertiesDatabase">A symbol properties database instance</param>
        /// <param name="marketMap">The market map that decides which market the new security should be in</param>
        /// <param name="cashBook">The cash book - used for resolving quote currencies for created conversion securities</param>
        /// <param name="changes"></param>
        /// <returns>Returns the added currency security if needed, otherwise null</returns>
        public List<ConversionSecurity> EnsureCurrencyDataFeed(SecurityManager securities,
            SubscriptionManager subscriptions,
            MarketHoursDatabase marketHoursDatabase,
            SymbolPropertiesDatabase symbolPropertiesDatabase,
            IReadOnlyDictionary<SecurityType, string> marketMap,
            CashBook cashBook,
            SecurityChanges changes
            )
        {
            // this gets called every time we add securities using universe selection,
            // so must of the time we've already resolved the value and don't need to again
            if (ConversionRateSecurity != null)
            {
                return null;
            }

            if (Symbol == CashBook.AccountCurrency)
            {
                ConversionRateSecurity = null;
                _isBaseCurrency = true;
                ConversionRate = 1.0m;
                return null;
            }

            // we require a security that converts this into the base currency
            string normal = Symbol + CashBook.AccountCurrency;
            string invert = CashBook.AccountCurrency + Symbol;
            var securitiesToSearch = securities.Select(kvp => kvp.Value)
                .Concat(changes.AddedSecurities)
                .Where(s => s.Type == SecurityType.Forex || s.Type == SecurityType.Cfd || s.Type == SecurityType.Crypto);

            foreach (var security in securitiesToSearch)
            {
                if(security.Symbol.Value == normal)
                {
                    ConversionRateSecurity = new List<ConversionSecurity>() { new ConversionSecurity(security, false) };
                    return null;
                }

                if (security.Symbol.Value == invert)
                {
                    ConversionRateSecurity = new List<ConversionSecurity>() { new ConversionSecurity(security, true) };
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

            CurrencyGraph graph = new CurrencyGraph();
            
            //!TODO

            var minimumResolution = subscriptions.Subscriptions.Select(x => x.Resolution).DefaultIfEmpty(Resolution.Minute).Min();

            foreach (var symbol in potentials)
            {
                if (symbol.Value == normal || symbol.Value == invert)
                {
                    bool invertPrice = symbol.Value == invert;

                    var securityType = symbol.ID.SecurityType;
                    var symbolProperties = symbolPropertiesDatabase.GetSymbolProperties(symbol.ID.Market, symbol.Value, securityType, Symbol);
                    Cash quoteCash;
                    if (!cashBook.TryGetValue(symbolProperties.QuoteCurrency, out quoteCash))
                    {
                        throw new Exception("Unable to resolve quote cash: " + symbolProperties.QuoteCurrency + ". This is required to add conversion feed: " + symbol.Value);
                    }
                    var marketHoursDbEntry = marketHoursDatabase.GetEntry(symbol.ID.Market, symbol.Value, symbol.ID.SecurityType);
                    var exchangeHours = marketHoursDbEntry.ExchangeHours;

                    // use the first subscription defined in the subscription manager
                    var type = subscriptions.LookupSubscriptionConfigDataTypes(securityType, minimumResolution, false).First();
                    var objectType = type.Item1;
                    var tickType = type.Item2;

                    // set this as an internal feed so that the data doesn't get sent into the algorithm's OnData events
                    var config = subscriptions.Add(objectType, tickType, symbol, minimumResolution, marketHoursDbEntry.DataTimeZone, exchangeHours.TimeZone, false, true, false, true);

                    Security security;
                    if (securityType == SecurityType.Cfd)
                    {
                        security = new Cfd.Cfd(exchangeHours, quoteCash, config, symbolProperties);
                    }
                    else if (securityType == SecurityType.Crypto)
                    {
                        security = new Crypto.Crypto(exchangeHours, quoteCash, config, symbolProperties);
                    }
                    else
                    {
                        security = new Forex.Forex(exchangeHours, quoteCash, config, symbolProperties);
                    }

                    ConversionRateSecurity = new List<ConversionSecurity>() { new ConversionSecurity(security, invertPrice) };

                    securities.Add(config.Symbol, security);
                    Log.Trace("Cash.EnsureCurrencyDataFeed(): Adding " + symbol.Value + " for cash " + Symbol + " currency feed");
                    return ConversionRateSecurity;
                }
            }

            // No direct conversion rate pair is found, check for secondary conversion pair
            // Common to cryptocurrencies where there are no direct pairings with USD, but there is intermediary such as BTC or ETH
            // Example #1: RENUSD doesn't exist, but there is RENETH  and ETHUSD,  from which we can calculate RENUSD
            // Example #2: RENUSD doesn't exist, but there is RENUSDT and USDTUSD, from which we can calculate RENUSD

            

            // Make a copy
            var existingPotentials = Currencies.CryptoCurrencyPairs.Select(x => x);
           
            // Order secondaryPotentials by whenever they are already contained in securities object.
            // This is must; if you use AddCrypto(RENETH), currency conversion will use ETHUSD as a conversion pair for USD, and not BTCUSD
            existingPotentials = existingPotentials
            .OrderByDescending(cryptoPair =>
            {
                foreach (var s in securities.Keys)
                    if (cryptoPair == s.Value)
                        return 1;

                return 0;
            });

            // RENUSD     = RENETH * ETHUSD
            // calculated = main   * linking
            ConversionSecurity mainConSec    = null;
            ConversionSecurity linkingConSec = null;

            List<ConversionSecurity> conversionSecuritiesList = new List<ConversionSecurity>();

            string baseCode = null;
            string quoteCode = null;

            // find main pair, such as RENETH
            foreach (var mainPair in existingPotentials)
            {
                Forex.Forex.DecomposeCurrencyPair(mainPair, out baseCode, out quoteCode);

                // found RENETH
                if(baseCode == this.Symbol || quoteCode == this.Symbol)
                {
                    // ETH
                    string secondCode = Forex.Forex.CurrencyPairDual(mainPair, this.Symbol);

                    bool mainInvert = mainPair.IndexOf(this.Symbol) != 0;

                    // ETHUSD
                    string linkingNormal = secondCode + CashBook.AccountCurrency;
                    // USDETH
                    string linkingInvert = CashBook.AccountCurrency + secondCode;

                    // search for ETHUSD or USDETH
                    foreach(var linkingPair in existingPotentials)
                    {
                        // found
                        if(linkingPair == linkingNormal || linkingPair == linkingInvert)
                        {
                            
                            var securityType = SecurityType.Crypto;

                            Symbol MainSymbol = CreateSymbol(marketMap, mainPair, markets, SecurityType.Crypto);
                            Symbol LinkingSymbol = CreateSymbol(marketMap, linkingPair, markets, SecurityType.Crypto);

                            var MainSymbolProperties    = symbolPropertiesDatabase.GetSymbolProperties(MainSymbol.ID.Market, MainSymbol.Value, securityType, secondCode);
                            var LinkingSymbolProperties = symbolPropertiesDatabase.GetSymbolProperties(LinkingSymbol.ID.Market, LinkingSymbol.Value, securityType, CashBook.AccountCurrency);

                            Cash MainQuoteCash;
                            if (!cashBook.TryGetValue(MainSymbolProperties.QuoteCurrency, out MainQuoteCash))
                            {
                                throw new Exception("Unable to resolve main quote cash: " + MainSymbolProperties.QuoteCurrency + ". This is required to add conversion feed: " + MainSymbol.Value);
                            }

                            Cash LinkingQuoteCash;
                            if (!cashBook.TryGetValue(LinkingSymbolProperties.QuoteCurrency, out LinkingQuoteCash))
                            {
                                throw new Exception("Unable to resolve linking quote cash: " + LinkingSymbolProperties.QuoteCurrency + ". This is required to add conversion feed: " + LinkingSymbol.Value);
                            }

                            var MainMarketHoursDbEntry = marketHoursDatabase.GetEntry(MainSymbol.ID.Market, MainSymbol.Value, MainSymbol.ID.SecurityType);
                            var LinkingMarketHoursDbEntry = marketHoursDatabase.GetEntry(LinkingSymbol.ID.Market, LinkingSymbol.Value, LinkingSymbol.ID.SecurityType);

                            var MainExchangeHours = MainMarketHoursDbEntry.ExchangeHours;
                            var LinkingExchangeHours = LinkingMarketHoursDbEntry.ExchangeHours;

                            // use the first subscription defined in the subscription manager
                            var MainType = subscriptions.LookupSubscriptionConfigDataTypes(securityType, minimumResolution, false).First();
                            var MainObjectType = MainType.Item1;
                            var MainTickType = MainType.Item2;

                            // use the first subscription defined in the subscription manager
                            var LinkingType = subscriptions.LookupSubscriptionConfigDataTypes(securityType, minimumResolution, false).First();
                            var LinkingObjectType = LinkingType.Item1;
                            var LinkingTickType = LinkingType.Item2;

                            // set this as an internal feed so that the data doesn't get sent into the algorithm's OnData events
                            var MainConfig = subscriptions.Add(MainObjectType, MainTickType, MainSymbol,   minimumResolution, MainMarketHoursDbEntry.DataTimeZone, MainExchangeHours.TimeZone, false, true, false, true);
                            var LinkingConfig = subscriptions.Add(LinkingObjectType, LinkingTickType, LinkingSymbol, minimumResolution, LinkingMarketHoursDbEntry.DataTimeZone, LinkingExchangeHours.TimeZone, false, true, false, true);

                            Security MainSecurity    = new Crypto.Crypto(MainExchangeHours,    MainQuoteCash,    MainConfig,    MainSymbolProperties);
                            Security LinkingSecurity = new Crypto.Crypto(LinkingExchangeHours, LinkingQuoteCash, LinkingConfig, LinkingSymbolProperties);

                            Log.Trace("Cash.EnsureCurrencyDataFeed(): Adding linking pair " + LinkingSymbol.Value + " for cash " + Symbol + " currency feed");
                            securities.Add(LinkingConfig.Symbol, LinkingSecurity);
                            linkingConSec = new ConversionSecurity(LinkingSecurity, linkingPair == linkingInvert);

                            Log.Trace("Cash.EnsureCurrencyDataFeed(): Adding main pair " + MainSymbol.Value + " for cash " + Symbol + " currency feed");
                            securities.Add(MainConfig.Symbol, MainSecurity);
                            mainConSec = new ConversionSecurity(MainSecurity, mainInvert);


                            ConversionRateSecurity = new List<ConversionSecurity>() { mainConSec, linkingConSec };
                            return ConversionRateSecurity;                               
                        }
                    }
                }
            }

            // if this still hasn't been set then it's an error condition
            throw new ArgumentException(string.Format("In order to maintain cash in {0} you are required to add a subscription for Forex pair {0}{1} or {1}{0}", Symbol, CashBook.AccountCurrency));
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
            return $"{Symbol}: {CurrencySymbol}{Amount,15:0.00} @ {rate,10:0.00####} = ${Math.Round(ValueInAccountCurrency, 2)}";
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
    }
}