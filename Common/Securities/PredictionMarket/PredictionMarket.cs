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
using QuantConnect.Orders.Fees;
using QuantConnect.Orders.Fills;
using QuantConnect.Orders.Slippage;

namespace QuantConnect.Securities.PredictionMarket
{
    /// <summary>
    /// Prediction Market Security Object Implementation for prediction market contracts (e.g., Kalshi)
    /// </summary>
    /// <remarks>
    /// Prediction market contracts are binary outcome contracts priced between 0 and 1 (or 0-100 cents).
    /// They settle at either $0 or $1 depending on the outcome of the underlying event.
    /// Primary data type is QuoteBar representing bid/ask prices.
    /// </remarks>
    public class PredictionMarket : Security
    {
        /// <summary>
        /// Gets or sets the close time for this prediction market contract.
        /// This is when the market stops trading.
        /// </summary>
        public DateTime? CloseTime { get; set; }

        /// <summary>
        /// Gets or sets the expiration time for this prediction market contract.
        /// This is when the contract outcome is determined.
        /// </summary>
        public DateTime? ExpirationTime { get; set; }

        /// <summary>
        /// Gets or sets the settlement result for this prediction market contract.
        /// Default is Pending until the contract outcome is determined.
        /// </summary>
        public PredictionMarketSettlementResult SettlementResult { get; set; } = PredictionMarketSettlementResult.Pending;

        /// <summary>
        /// Constructor for the Prediction Market security
        /// </summary>
        /// <param name="symbol">The symbol</param>
        /// <param name="exchangeHours">Defines the hours this exchange is open</param>
        /// <param name="quoteCurrency">The cash object that represent the quote currency (typically USD)</param>
        /// <param name="symbolProperties">The symbol properties for this security</param>
        /// <param name="currencyConverter">Currency converter used to convert <see cref="CashAmount"/>
        /// instances into units of the account currency</param>
        /// <param name="registeredTypes">Provides all data types registered in the algorithm</param>
        /// <param name="cache">The security cache</param>
        public PredictionMarket(Symbol symbol,
            SecurityExchangeHours exchangeHours,
            Cash quoteCurrency,
            SymbolProperties symbolProperties,
            ICurrencyConverter currencyConverter,
            IRegisteredSecurityDataTypesProvider registeredTypes,
            SecurityCache cache)
            : base(symbol,
                quoteCurrency,
                symbolProperties,
                new PredictionMarketExchange(exchangeHours),
                cache,
                new PredictionMarketPortfolioModel(),
                new ImmediateFillModel(),
                new KalshiFeeModel(),
                NullSlippageModel.Instance,
                new ImmediateSettlementModel(),
                Securities.VolatilityModel.Null,
                new PredictionMarketBuyingPowerModel(),
                new SecurityDataFilter(),
                new SecurityPriceVariationModel(),
                currencyConverter,
                registeredTypes,
                Securities.MarginInterestRateModel.Null
                )
        {
            Holdings = new PredictionMarketHolding(this, currencyConverter);
        }

        /// <summary>
        /// Returns the securities symbol
        /// </summary>
        public static implicit operator Symbol(PredictionMarket security) => security.Symbol;
    }
}
