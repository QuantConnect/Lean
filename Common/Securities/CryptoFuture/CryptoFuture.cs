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
    /// 
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
        /// 
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="exchangeHours"></param>
        /// <param name="quoteCurrency"></param>
        /// <param name="baseCurrency"></param>
        /// <param name="symbolProperties"></param>
        /// <param name="currencyConverter"></param>
        /// <param name="registeredTypes"></param>
        /// <param name="cache"></param>
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
                new BinanceFeeModel(),
                new ConstantSlippageModel(0),
                new ImmediateSettlementModel(),
                Securities.VolatilityModel.Null,
                new CryptoFutureMarginModel(10, 0.02m, 2),
                new SecurityDataFilter(),
                new SecurityPriceVariationModel(),
                currencyConverter,
                registeredTypes
                )
        {
            BaseCurrency = baseCurrency;
            Holdings = new CryptoFutureHolding(this, currencyConverter);
        }
    }
}
