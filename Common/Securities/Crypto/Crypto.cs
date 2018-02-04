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
        public Crypto(SecurityExchangeHours exchangeHours, Cash quoteCurrency, SubscriptionDataConfig config, SymbolProperties symbolProperties)
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
                new SecurityMarginModel(50m),
                new ForexDataFilter(),
                new SecurityPriceVariationModel()
                )
        {
            Holdings = new CryptoHolding(this);

            // decompose the symbol into each currency pair
            string baseCurrencySymbol, quoteCurrencySymbol;
            Forex.Forex.DecomposeCurrencyPair(config.Symbol.Value, out baseCurrencySymbol, out quoteCurrencySymbol);
            BaseCurrencySymbol = baseCurrencySymbol;
        }

        /// <summary>
        /// Constructor for the Crypto security
        /// </summary>
        /// <param name="symbol">The security's symbol</param>
        /// <param name="exchangeHours">Defines the hours this exchange is open</param>
        /// <param name="quoteCurrency">The cash object that represent the quote currency</param>
        /// <param name="symbolProperties">The symbol properties for this security</param>
        public Crypto(Symbol symbol, SecurityExchangeHours exchangeHours, Cash quoteCurrency, SymbolProperties symbolProperties)
            : base(symbol,
                quoteCurrency,
                symbolProperties,
                new CryptoExchange(exchangeHours),
                new ForexCache(),
                new SecurityPortfolioModel(),
                new ImmediateFillModel(),
                new ConstantFeeModel(0),
                new ConstantSlippageModel(0),
                new ImmediateSettlementModel(),
                Securities.VolatilityModel.Null,
                new SecurityMarginModel(50m),
                new ForexDataFilter(),
                new SecurityPriceVariationModel()
                )
        {
            Holdings = new CryptoHolding(this);

            // decompose the symbol into each currency pair
            string baseCurrencySymbol, quoteCurrencySymbol;
            Forex.Forex.DecomposeCurrencyPair(symbol.Value, out baseCurrencySymbol, out quoteCurrencySymbol);
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
    }
}
