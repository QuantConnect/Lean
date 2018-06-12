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
        public IReadOnlyList<ConversionSecurity> ConversionRateSecurity { get; private set; }

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
        public List<Security> EnsureCurrencyDataFeed(SecurityManager securities,
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
            //string normal = Symbol + CashBook.AccountCurrency;
            //string invert = CashBook.AccountCurrency + Symbol;

            //!EXISTING SECURITIES WITH DATAFEED
            var securitiesToSearch = securities.Select(kvp => kvp.Value)
                .Concat(changes.AddedSecurities)
                .Where(s => s.Type == SecurityType.Forex || s.Type == SecurityType.Cfd || s.Type == SecurityType.Crypto);

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

            var minimumResolution = subscriptions.Subscriptions.Select(x => x.Resolution).DefaultIfEmpty(Resolution.Minute).Min();

            //!TODO

            List<ConversionSecurity> conversionSecuritiesList = new List<ConversionSecurity>();

            List<Security> requiredSecurities = new List<Security>();

            // return needed pairs for full conversion from one currency to another
            CurrencyPath shortestPath = Currencies.Graph.FindShortedPath(Symbol, CashBook.AccountCurrency);

            foreach (var step in shortestPath.Steps)
            {
                // required symbol
                Symbol symbol = CreateSymbol(marketMap, step.Edge.PairSymbol, markets, step.Edge.Type);

                Security security;
                try
                {
                    //check if it already exists
                    security = securitiesToSearch.Where(sec => sec.Symbol.Value == step.Edge.PairSymbol).First();
                }
                catch
                {
                    var symbolProperties = symbolPropertiesDatabase.GetSymbolProperties(symbol.ID.Market, symbol.Value, symbol.SecurityType, Symbol);

                    Cash quoteCash;
                    if (!cashBook.TryGetValue(symbolProperties.QuoteCurrency, out quoteCash))
                    {
                        throw new Exception("Unable to resolve quote cash: " + symbolProperties.QuoteCurrency + ". This is required to add conversion feed: " + symbol.Value);
                    }

                    var marketHoursDbEntry = marketHoursDatabase.GetEntry(symbol.ID.Market, symbol.Value, symbol.ID.SecurityType);
                    var exchangeHours = marketHoursDbEntry.ExchangeHours;

                    // use the first subscription defined in the subscription manager
                    var type = subscriptions.LookupSubscriptionConfigDataTypes(symbol.SecurityType, minimumResolution, false).First();
                    var objectType = type.Item1;
                    var tickType = type.Item2;

                    // set this as an internal feed so that the data doesn't get sent into the algorithm's OnData events
                    var config = subscriptions.Add(objectType, tickType, symbol, minimumResolution, marketHoursDbEntry.DataTimeZone, exchangeHours.TimeZone, false, true, false, true);

                    switch (symbol.SecurityType)
                    {
                        case SecurityType.Cfd:
                            security = new Cfd.Cfd(exchangeHours, quoteCash, config, symbolProperties);
                            break;
                        case SecurityType.Crypto:
                            security = new Crypto.Crypto(exchangeHours, quoteCash, config, symbolProperties);
                            break;
                        case SecurityType.Forex:
                            security = new Forex.Forex(exchangeHours, quoteCash, config, symbolProperties);
                            break;
                        default:
                            throw new ArgumentException("Unknown security type");
                    }

                    securities.Add(config.Symbol, security);

                    Log.Trace("Cash.EnsureCurrencyDataFeed(): Adding " + symbol.Value + " for cash " + Symbol + " currency feed");
                }

                conversionSecuritiesList.Add(new ConversionSecurity(security, step.Inverted));
            }

            if (shortestPath.Length == 0)
                throw new ArgumentException(string.Format("No path found for converting {0} to {1}, so cannot calculate {0}{1}.", Symbol, CashBook.AccountCurrency));

            return requiredSecurities;
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