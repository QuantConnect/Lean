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
using QuantConnect.Data;
using QuantConnect.Orders.Fees;
using QuantConnect.Orders.Fills;
using QuantConnect.Orders.Slippage;
using System.Collections.Generic;

namespace QuantConnect.Securities.Forex
{
    /// <summary>
    /// FOREX Security Object Implementation for FOREX Assets
    /// </summary>
    /// <seealso cref="Security"/>
    public class Forex : Security, IBaseCurrencySymbol
    {
        /// <summary>
        /// Constructor for the forex security
        /// </summary>
        /// <param name="exchangeHours">Defines the hours this exchange is open</param>
        /// <param name="quoteCurrency">The cash object that represent the quote currency</param>
        /// <param name="config">The subscription configuration for this security</param>
        /// <param name="symbolProperties">The symbol properties for this security</param>
        public Forex(SecurityExchangeHours exchangeHours, Cash quoteCurrency, SubscriptionDataConfig config, SymbolProperties symbolProperties)
            : base(config,
                quoteCurrency,
                symbolProperties,
                new ForexExchange(exchangeHours),
                new ForexCache(),
                new SecurityPortfolioModel(),
                new ImmediateFillModel(),
                new InteractiveBrokersFeeModel(),
                new ConstantSlippageModel(0),
                new ImmediateSettlementModel(),
                Securities.VolatilityModel.Null,
                new SecurityMarginModel(50m),
                new ForexDataFilter(),
                new SecurityPriceVariationModel()
                )
        {
            Holdings = new ForexHolding(this);

            // decompose the symbol into each currency pair
            string baseCurrencySymbol, quoteCurrencySymbol;
            DecomposeCurrencyPair(config.Symbol.Value, out baseCurrencySymbol, out quoteCurrencySymbol);
            BaseCurrencySymbol = baseCurrencySymbol;
        }

        /// <summary>
        /// Constructor for the forex security
        /// </summary>
        /// <param name="symbol">The security's symbol</param>
        /// <param name="exchangeHours">Defines the hours this exchange is open</param>
        /// <param name="quoteCurrency">The cash object that represent the quote currency</param>
        /// <param name="symbolProperties">The symbol properties for this security</param>
        public Forex(Symbol symbol, SecurityExchangeHours exchangeHours, Cash quoteCurrency, SymbolProperties symbolProperties)
            : base(symbol,
                quoteCurrency,
                symbolProperties,
                new ForexExchange(exchangeHours),
                new ForexCache(),
                new SecurityPortfolioModel(),
                new ImmediateFillModel(),
                new InteractiveBrokersFeeModel(),
                new ConstantSlippageModel(0),
                new ImmediateSettlementModel(),
                Securities.VolatilityModel.Null,
                new SecurityMarginModel(50m),
                new ForexDataFilter(),
                new SecurityPriceVariationModel()
                )
        {
            Holdings = new ForexHolding(this);

            // decompose the symbol into each currency pair
            string baseCurrencySymbol, quoteCurrencySymbol;
            DecomposeCurrencyPair(symbol.Value, out baseCurrencySymbol, out quoteCurrencySymbol);
            BaseCurrencySymbol = baseCurrencySymbol;
        }

        /// <summary>
        /// Gets the currency acquired by going long this currency pair
        /// </summary>
        /// <remarks>
        /// For example, the EUR/USD has a base currency of the euro, and as a result
        /// of going long the EUR/USD a trader is acquiring euros in exchange for US dollars
        /// </remarks>
        public string BaseCurrencySymbol { get; private set; }

        /// <summary>
        /// Decomposes the specified currency pair into a base and quote currency provided as out parameters.
        /// Requires symbols in Currencies.CurrencySymbols dictionary to make accurate splits, important for crypto-currency symbols.
        /// </summary>
        /// <param name="currencyPair">The input currency pair to be decomposed, for example, "EURUSD"</param>
        /// <param name="baseCurrency">The output base currency</param>
        /// <param name="quoteCurrency">The output quote currency</param>
        public static void DecomposeCurrencyPair(string currencyPair, out string baseCurrency, out string quoteCurrency)
        {
            if (currencyPair == null || currencyPair.Length < 6 || currencyPair.Length > Currencies.MaxCharactersPerCurrencyPair)
            {
                throw new ArgumentException($"Currency pairs must not be null, length minimum of 6 and maximum of {Currencies.MaxCharactersPerCurrencyPair}. Problematic pair: {currencyPair}");
            }

            if (currencyPair.Length == 6)
            {

                // Old-code part for Forex (non-crypto) markets only. 
                baseCurrency = currencyPair.Substring(0, 3);
                quoteCurrency = currencyPair.Substring(3);
                return;
            }

            baseCurrency = null;
            quoteCurrency = null;

            List<string> bases = new List<string>();
            List<string> quotes = new List<string>();

            // Find bases
            foreach (var symbol in Currencies.CurrencySymbols.Keys)
            {
                if (currencyPair.IndexOf(symbol) == 0)
                {
                    bases.Add(symbol);
                }
            }

            // Find quotes
            foreach (var symbol in Currencies.CurrencySymbols.Keys)
            {
                if (currencyPair.Contains(symbol))
                {
                    int start = currencyPair.IndexOf(symbol, 3);

                    if (start >= 3 && start <= Currencies.MaxCharactersPerCurrencyCode)
                    {
                        quotes.Add(symbol);
                    }
                }
            }

            // Make combinations (combined) and compare to currencyPair
            // When 100% match found, break the loop.
            foreach (string b in bases)
            {
                foreach (string q in quotes)
                {
                    string combined = b + q;

                    if (combined.Equals(currencyPair))
                    {
                        baseCurrency = b;
                        quoteCurrency = q;
                        // Return, since if we came to this point, there was found atleast 1 base and 1 count, that matches original currencyPair
                        return;
                    }
                }
            }

            if (bases.Count == 0)
            {
                throw new ArgumentException($"No base currency found for the pair: {currencyPair}");
            }
            else if (quotes.Count == 0)
            {
                throw new ArgumentException($"No quote currency found for the pair: {currencyPair}");
            }

        }

        /// <summary>
        /// You have currencyPair AB and one known symbol (A or B). This function returns another one (B or A).
        /// </summary>
        /// <param name="currencyPair">Currency pair AB</param>
        /// <param name="knownSymbol">Known part of the currencyPair (either A or B)</param>
        /// <returns>Returns other part of currencyPair (either B or A)</returns>
        public static string CurrencyPairDual(string currencyPair, string knownSymbol)
        {
            string CurrencyA = null;
            string CurrencyB = null;

            DecomposeCurrencyPair(currencyPair, out CurrencyA, out CurrencyB);

            if (CurrencyA == knownSymbol)
            {
                return CurrencyB;
            }
            else if (CurrencyB == knownSymbol)
            {
                return CurrencyA;
            }
            else
            {
                throw new ArgumentException($"The knownSymbol {knownSymbol} isn't contained in currencyPair {currencyPair}.");
            }
        }
    }
}
