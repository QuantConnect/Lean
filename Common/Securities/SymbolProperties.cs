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

namespace QuantConnect.Securities
{
    /// <summary>
    /// Represents common properties for a specific security, uniquely identified by market, symbol and security type
    /// </summary>
    public class SymbolProperties
    {
        /// <summary>
        /// The description of the security
        /// </summary>
        public string Description
        {
            get;
        }

        /// <summary>
        /// The quote currency of the security
        /// </summary>
        public string QuoteCurrency
        {
            get;
        }

        /// <summary>
        /// The contract multiplier for the security
        /// </summary>
        public decimal ContractMultiplier
        {
            get;
            protected set;
        }

        /// <summary>
        /// The minimum price variation (tick size) for the security
        /// </summary>
        public virtual decimal MinimumPriceVariation
        {
            get;
            protected set;
        }

        /// <summary>
        /// The lot size (lot size of the order) for the security
        /// </summary>
        public decimal LotSize
        {
            get;
        }

        /// <summary>
        /// The market ticker
        /// </summary>
        public string MarketTicker
        {
            get;
        }

        /// <summary>
        /// The minimum order size allowed
        /// For crypto/forex pairs it's expected to be expressed in base or quote currency
        /// i.e For BTC/USD the minimum order size allowed with Coinbase is 0.0001 BTC
        /// while on Binance the minimum order size allowed is 10 USD
        /// </summary>
        public decimal? MinimumOrderSize
        {
            get;
        }

        /// <summary>
        /// Allows normalizing live asset prices to US Dollars for Lean consumption. In some exchanges,
        /// for some securities, data is expressed in cents like for example for corn futures ('ZC').
        /// </summary>
        /// <remarks>Default value is 1 but for some futures in cents it's 100</remarks>
        public decimal PriceMagnifier
        {
            get;
        }

        /// <summary>
        /// Scale factor for option's strike price. For some options, such as NQX, the strike price
        /// is based on a fraction of the underlying, thus this paramater scales the strike price so
        /// that it can be used in comparation with the underlying such as
        /// in <see cref="OptionFilterUniverse.Strikes(int, int)"/>
        /// </summary>
        public decimal StrikeMultiplier
        {
            get;
        }

        /// <summary>
        /// Creates an instance of the <see cref="SymbolProperties"/> class
        /// </summary>
        public SymbolProperties(string description, string quoteCurrency, decimal contractMultiplier, decimal minimumPriceVariation, decimal lotSize, string marketTicker, decimal? minimumOrderSize = null, decimal priceMagnifier = 1, decimal strikeMultiplier = 1)
        {
            Description = description;
            QuoteCurrency = quoteCurrency;
            ContractMultiplier = contractMultiplier;
            MinimumPriceVariation = minimumPriceVariation;
            LotSize = lotSize;

            if (LotSize <= 0)
            {
                throw new ArgumentException(Messages.SymbolProperties.InvalidLotSize);
            }

            MarketTicker = marketTicker;
            MinimumOrderSize = minimumOrderSize;

            PriceMagnifier = priceMagnifier;
            if (PriceMagnifier <= 0)
            {
                throw new ArgumentException(Messages.SymbolProperties.InvalidPriceMagnifier);
            }

            StrikeMultiplier = strikeMultiplier;
            if (strikeMultiplier <= 0)
            {
                throw new ArgumentException(Messages.SymbolProperties.InvalidStrikeMultiplier);
            }
        }

        /// <summary>
        /// The string representation of these symbol properties
        /// </summary>
        public override string ToString()
        {
            return Messages.SymbolProperties.ToString(this);
        }

        /// <summary>
        /// Gets a default instance of the <see cref="SymbolProperties"/> class for the specified <paramref name="quoteCurrency"/>
        /// </summary>
        /// <param name="quoteCurrency">The quote currency of the symbol</param>
        /// <returns>A default instance of the<see cref="SymbolProperties"/> class</returns>
        public static SymbolProperties GetDefault(string quoteCurrency)
        {
            return new SymbolProperties(string.Empty, quoteCurrency.LazyToUpper(), 1, 0.01m, 1, string.Empty);
        }
    }
}
