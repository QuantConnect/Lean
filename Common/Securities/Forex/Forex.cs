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
using QuantConnect.Data;

namespace QuantConnect.Securities.Forex 
{
    /// <summary>
    /// FOREX Security Object Implementation for FOREX Assets
    /// </summary>
    /// <seealso cref="Security"/>
    public class Forex : Security
    {
        /// <summary>
        /// Constructor for the forex security
        /// </summary>
        /// <param name="quoteCurrency">The cash object that represent the quote currency</param>
        /// <param name="config">The subscription configuration for this security</param>
        /// <param name="leverage">The leverage used for this security</param>
        public Forex(Cash quoteCurrency, SubscriptionDataConfig config, decimal leverage)
            : this(MarketHoursDatabase.FromDataFolder().GetExchangeHours(config), quoteCurrency, config, leverage)
        {
            // this constructor is provided for backward compatibility

            // should we even keep this?
        }

        /// <summary>
        /// Constructor for the forex security
        /// </summary>
        /// <param name="exchangeHours">Defines the hours this exchange is open</param>
        /// <param name="quoteCurrency">The cash object that represent the quote currency</param>
        /// <param name="config">The subscription configuration for this security</param>
        /// <param name="leverage">The leverage used for this security</param>
        public Forex(SecurityExchangeHours exchangeHours, Cash quoteCurrency, SubscriptionDataConfig config, decimal leverage)
            : base(exchangeHours, config, leverage)
        {
            QuoteCurrency = quoteCurrency;
            //Holdings for new Vehicle:
            Cache = new ForexCache();
            Exchange = new ForexExchange(exchangeHours); 
            DataFilter = new ForexDataFilter();
            TransactionModel = new ForexTransactionModel();
            PortfolioModel = new ForexPortfolioModel();
            MarginModel = new ForexMarginModel(leverage);
            SettlementModel = new ImmediateSettlementModel();
            Holdings = new ForexHolding(this);

            // decompose the symbol into each currency pair
            string baseCurrencySymbol, quoteCurrencySymbol;
            DecomposeCurrencyPair(config.Symbol.Value, out baseCurrencySymbol, out quoteCurrencySymbol);
            BaseCurrencySymbol = baseCurrencySymbol;
            QuoteCurrencySymbol = quoteCurrencySymbol;
        }

        /// <summary>
        /// Gets the Cash object used for converting the quote currency to the account currency
        /// </summary>
        public Cash QuoteCurrency { get; private set; }

        /// <summary>
        /// Gets the currency acquired by going long this currency pair
        /// </summary>
        /// <remarks>
        /// For example, the EUR/USD has a base currency of the euro, and as a result
        /// of going long the EUR/USD a trader is acquiring euros in exchange for US dollars
        /// </remarks>
        public string BaseCurrencySymbol { get; private set; }

        /// <summary>
        /// Gets the currency spent by going long this currency pair
        /// </summary>
        /// <remarks>
        /// For example, the EUR/USD has a quote currency of US dollars, and as a result of
        /// going long the EUR/USD a trader is spending US dollars in order to acquire euros.
        /// </remarks>
        public string QuoteCurrencySymbol { get; private set; }

        /// <summary>
        /// Decomposes the specified currency pair into a base and quote currency provided as out parameters
        /// </summary>
        /// <param name="currencyPair">The input currency pair to be decomposed, for example, "EURUSD"</param>
        /// <param name="baseCurrency">The output base currency</param>
        /// <param name="quoteCurrency">The output quote currency</param>
        public static void DecomposeCurrencyPair(string currencyPair, out string baseCurrency, out string quoteCurrency)
        {
            if (currencyPair == null || currencyPair.Length != 6)
            {
                throw new ArgumentException("Currency pairs must be exactly 6 characters: " + currencyPair);
            }
            baseCurrency = currencyPair.Substring(0, 3);
            quoteCurrency = currencyPair.Substring(3);
        }

        /// <summary>
        /// Gets the listing of currently supported currency pairs.
        /// </summary>
        /// <remarks>
        /// This listing should be in sync with the data available at: https://www.quantconnect.com/data/FOREX#2.1.1
        /// </remarks>
        public static readonly IReadOnlyList<string> CurrencyPairs = new []
        {
            "AUDJPY", "AUDUSD", "EURCHF", "EURGBP", "EURJPY", "EURUSD", "GBPAUD", "GBPJPY", "GBPUSD", "NZDUSD", "USDCAD", "USDCHF", "USDJPY"
        };

        /// <summary>
        /// A mapping of currency codes to their display symbols
        /// </summary>
        public static readonly IReadOnlyDictionary<string, string> CurrencySymbols = new Dictionary<string, string>
        {
            {"USD", "$"},
            {"GBP", "₤"},
            {"JPY", "¥"},
            {"EUR", "€"},
            {"NZD", "$"},
            {"AUD", "$"},
            {"CAD", "$"},
            {"CHF", "Fr"}
        };
    }
}
