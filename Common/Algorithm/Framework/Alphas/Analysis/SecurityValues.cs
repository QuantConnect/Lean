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
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.Framework.Alphas.Analysis
{
    /// <summary>
    /// Contains security values required by insight analysis components
    /// </summary>
    /// <remarks>
    /// The main purpose here is providing an ACL against the algorithm to remove the dependencies
    /// </remarks>
    public class SecurityValues
    {
        /// <summary>
        /// Gets the symbol these values are for
        /// </summary>
        public Symbol Symbol { get; }

        /// <summary>
        /// Gets the utc time these values were sampled
        /// </summary>
        public DateTime TimeUtc { get; }

        /// <summary>
        /// Gets the security price as of <see cref="TimeUtc"/>
        /// </summary>
        public decimal Price { get; }

        /// <summary>
        /// Gets the security's volatility as of <see cref="TimeUtc"/>
        /// </summary>
        public decimal Volatility { get; }

        /// <summary>
        /// Gets the volume traded in the security during this time step
        /// </summary>
        public decimal Volume { get; }

        /// <summary>
        /// Gets the conversion rate for the quote currency of the security
        /// </summary>
        public decimal QuoteCurrencyConversionRate { get; }

        /// <summary>
        /// Gets the exchange hours for the security
        /// </summary>
        public SecurityExchangeHours ExchangeHours { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SecurityValues"/> class
        /// </summary>
        /// <param name="symbol">The symbol of the security</param>
        /// <param name="timeUtc">The time these values were sampled</param>
        /// <param name="exchangeHours">The security's exchange hours</param>
        /// <param name="price">The security price</param>
        /// <param name="volatility">The security's volatility</param>
        /// <param name="volume">The volume traded at this time step</param>
        /// <param name="quoteCurrencyConversionRate">The conversion rate for the quote currency of the security</param>
        public SecurityValues(Symbol symbol, DateTime timeUtc, SecurityExchangeHours exchangeHours, decimal price, decimal volatility, decimal volume, decimal quoteCurrencyConversionRate)
        {
            Symbol = symbol;
            Price = price;
            Volume = volume;
            TimeUtc = timeUtc;
            Volatility = volatility;
            ExchangeHours = exchangeHours;
            QuoteCurrencyConversionRate = quoteCurrencyConversionRate;
        }

        /// <summary>
        /// Gets the security value corresponding to the specified insight type
        /// </summary>
        /// <param name="type">The insight type</param>
        /// <returns>The security value for the specified insight type</returns>
        public decimal Get(InsightType type)
        {
            switch (type)
            {
                case InsightType.Price:
                    return Price;

                case InsightType.Volatility:
                    return Volatility;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
    }
}