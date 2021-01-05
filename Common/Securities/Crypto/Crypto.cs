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

using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Orders.Fees;
using QuantConnect.Orders.Fills;
using QuantConnect.Orders.Slippage;
using QuantConnect.Securities.Forex;
using System;

namespace QuantConnect.Securities.Crypto
{
    /// <summary>
    /// Crypto Security Object Implementation for Crypto Assets
    /// </summary>
    /// <seealso cref="Security"/>
    public class Crypto : Security, IBaseCurrencySymbol
    {
        /// <summary>
        /// Constructor for the Crypto security
        /// </summary>
        /// <param name="exchangeHours">Defines the hours this exchange is open</param>
        /// <param name="quoteCurrency">The cash object that represent the quote currency</param>
        /// <param name="config">The subscription configuration for this security</param>
        /// <param name="symbolProperties">The symbol properties for this security</param>
        /// <param name="currencyConverter">Currency converter used to convert <see cref="CashAmount"/>
        /// instances into units of the account currency</param>
        /// <param name="registeredTypes">Provides all data types registered in the algorithm</param>
        public Crypto(SecurityExchangeHours exchangeHours,
            Cash quoteCurrency,
            SubscriptionDataConfig config,
            SymbolProperties symbolProperties,
            ICurrencyConverter currencyConverter,
            IRegisteredSecurityDataTypesProvider registeredTypes)
            : base(config,
                quoteCurrency,
                symbolProperties,
                new CryptoExchange(exchangeHours),
                new ForexCache(),
                new SecurityPortfolioModel(),
                new ImmediateFillModel(),
                new GDAXFeeModel(),
                new ConstantSlippageModel(0),
                new ImmediateSettlementModel(),
                Securities.VolatilityModel.Null,
                new CashBuyingPowerModel(),
                new ForexDataFilter(),
                new SecurityPriceVariationModel(),
                currencyConverter,
                registeredTypes
                )
        {
            Holdings = new CryptoHolding(this, currencyConverter);

            // decompose the symbol into each currency pair
            string quoteCurrencySymbol, baseCurrencySymbol;
            DecomposeCurrencyPair(config.Symbol, symbolProperties, out baseCurrencySymbol, out quoteCurrencySymbol);
            BaseCurrencySymbol = baseCurrencySymbol;
        }

        /// <summary>
        /// Constructor for the Crypto security
        /// </summary>
        /// <param name="symbol">The security's symbol</param>
        /// <param name="exchangeHours">Defines the hours this exchange is open</param>
        /// <param name="quoteCurrency">The cash object that represent the quote currency</param>
        /// <param name="symbolProperties">The symbol properties for this security</param>
        /// <param name="currencyConverter">Currency converter used to convert <see cref="CashAmount"/>
        /// instances into units of the account currency</param>
        /// <param name="registeredTypes">Provides all data types registered in the algorithm</param>
        public Crypto(Symbol symbol,
            SecurityExchangeHours exchangeHours,
            Cash quoteCurrency,
            SymbolProperties symbolProperties,
            ICurrencyConverter currencyConverter,
            IRegisteredSecurityDataTypesProvider registeredTypes,
            SecurityCache securityCache)
            : base(symbol,
                quoteCurrency,
                symbolProperties,
                new CryptoExchange(exchangeHours),
                securityCache,
                new SecurityPortfolioModel(),
                new ImmediateFillModel(),
                new GDAXFeeModel(),
                new ConstantSlippageModel(0),
                new ImmediateSettlementModel(),
                Securities.VolatilityModel.Null,
                new CashBuyingPowerModel(),
                new ForexDataFilter(),
                new SecurityPriceVariationModel(),
                currencyConverter,
                registeredTypes
                )
        {
            Holdings = new CryptoHolding(this, currencyConverter);

            // decompose the symbol into each currency pair
            string quoteCurrencySymbol, baseCurrencySymbol;
            DecomposeCurrencyPair(symbol, symbolProperties, out baseCurrencySymbol, out quoteCurrencySymbol);
            BaseCurrencySymbol = baseCurrencySymbol;
        }

        /// <summary>
        /// Gets the currency acquired by going long this currency pair
        /// </summary>
        /// <remarks>
        /// For example, the EUR/USD has a base currency of the euro, and as a result
        /// of going long the EUR/USD a trader is acquiring euros in exchange for US dollars
        /// </remarks>
        public string BaseCurrencySymbol { get; protected set; }

        /// <summary>
        /// Get the current value of the security.
        /// </summary>
        public override decimal Price => Cache.GetData<TradeBar>()?.Close ?? Cache.Price;

        /// <summary>
        /// Decomposes the specified currency pair into a base and quote currency provided as out parameters
        /// </summary>
        /// <param name="symbol">The input symbol to be decomposed</param>
        /// <param name="symbolProperties">The symbol properties for this security</param>
        /// <param name="baseCurrency">The output base currency</param>
        /// <param name="quoteCurrency">The output quote currency</param>
        public static void DecomposeCurrencyPair(Symbol symbol, SymbolProperties symbolProperties, out string baseCurrency, out string quoteCurrency)
        {
            quoteCurrency = symbolProperties.QuoteCurrency;
            if (symbol.Value.EndsWith(quoteCurrency))
            {
                baseCurrency = symbol.Value.RemoveFromEnd(quoteCurrency);
            }
            else
            {
                throw new InvalidOperationException($"symbol doesn't end with {quoteCurrency}");
            }
        }
    }
}
