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

using QuantConnect.Orders.Fees;
using QuantConnect.Orders.Fills;
using QuantConnect.Orders.Slippage;

namespace QuantConnect.Securities.CryptoFuture
{
    /// <summary>
    /// Crypto Future Security Object Implementation for Crypto Future Assets
    /// </summary>
    public class CryptoFuture : Security, IBaseCurrencySymbol
    {
        /// <summary>
        /// Gets the currency acquired by going long this currency pair
        /// </summary>
        /// <remarks>
        /// For example, the EUR/USD has a base currency of the euro, and as a result
        /// of going long the EUR/USD a trader is acquiring euros in exchange for US dollars
        /// </remarks>
        public Cash BaseCurrency { get; protected set; }

        /// <summary>
        /// Constructor for the Crypto Future security
        /// </summary>
        /// <param name="symbol">The symbol</param>
        /// <param name="exchangeHours">Defines the hours this exchange is open</param>
        /// <param name="quoteCurrency">The cash object that represent the quote currency</param>
        /// <param name="baseCurrency">The cash object that represent the base currency</param>
        /// <param name="symbolProperties">The symbol properties for this security</param>
        /// <param name="currencyConverter">Currency converter used to convert <see cref="CashAmount"/>
        /// instances into units of the account currency</param>
        /// <param name="registeredTypes">Provides all data types registered in the algorithm</param>
        /// <param name="cache">The security cache</param>
        public CryptoFuture(Symbol symbol,
            SecurityExchangeHours exchangeHours,
            Cash quoteCurrency,
            Cash baseCurrency,
            SymbolProperties symbolProperties,
            ICurrencyConverter currencyConverter,
            IRegisteredSecurityDataTypesProvider registeredTypes,
            SecurityCache cache)
            : base(symbol,
                quoteCurrency,
                symbolProperties,
                new CryptoFutureExchange(exchangeHours),
                cache,
                new SecurityPortfolioModel(),
                new ImmediateFillModel(),
                IsCryptoCoinFuture(symbol.ID.Market, quoteCurrency.Symbol) ? new BinanceCoinFuturesFeeModel() : new BinanceFuturesFeeModel(),
                NullSlippageModel.Instance,
                new ImmediateSettlementModel(),
                Securities.VolatilityModel.Null,
                new CryptoFutureMarginModel(),
                new SecurityDataFilter(),
                new SecurityPriceVariationModel(),
                currencyConverter,
                registeredTypes,
                // only applies for perpetual futures
                symbol.ID.Date == SecurityIdentifier.DefaultDate ? new BinanceFutureMarginInterestRateModel() : Securities.MarginInterestRateModel.Null
                )
        {
            BaseCurrency = baseCurrency;
            Holdings = new CryptoFutureHolding(this, currencyConverter);
        }

        /// <summary>
        /// Checks whether the security is a crypto coin future
        /// </summary>
        /// <returns>True if the security is a crypto coin future</returns>
        public bool IsCryptoCoinFuture()
        {
            return IsCryptoCoinFuture(Symbol.ID.Market, QuoteCurrency.Symbol);
        }

        /// <summary>
        /// Checks whether the security is a crypto coin future
        /// </summary>
        /// <param name="market">The security market</param>
        /// <param name="quoteCurrency">The security quote currency</param>
        /// <returns>True if the security is a crypto coin future</returns>
        private static bool IsCryptoCoinFuture(string market, string quoteCurrency)
        {
            // Binance coin-margined futures are nominally quoted in USD (e.g. BTCUSD_PERP) but settle in the base
            // currency, so on Binance any quote currency other than one of its known USD-pegged stablecoins implies
            // a coin-margined contract. Other venues, notably EU MiCA-compliant exchanges, quote linear (USD-margined)
            // futures directly in fiat USD, so outside of Binance the quote currency alone can't be used this way.
            if (market != Market.Binance)
            {
                return false;
            }

            return quoteCurrency != "USDT" && quoteCurrency != "BUSD" && quoteCurrency != "USDC";
        }

        /// <summary>
        /// Returns the securities symbol
        /// </summary>
        public static implicit operator Symbol(CryptoFuture security) => security.Symbol;
    }
}
