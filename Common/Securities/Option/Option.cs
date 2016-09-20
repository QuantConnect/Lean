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
using QuantConnect.Data.Market;
using QuantConnect.Orders.Fees;
using QuantConnect.Orders.Fills;
using QuantConnect.Orders.Slippage;

namespace QuantConnect.Securities.Option
{
    /// <summary>
    /// Option Security Object Implementation for Option Assets
    /// </summary>
    /// <seealso cref="Security"/>
    public class Option : Security
    {
        /// <summary>
        /// The default number of days required to settle an equity sale
        /// </summary>
        public const int DefaultSettlementDays = 1;

        /// <summary>
        /// The default time of day for settlement
        /// </summary>
        public static readonly TimeSpan DefaultSettlementTime = new TimeSpan(8, 0, 0);

        /// <summary>
        /// Constructor for the option security
        /// </summary>
        /// <param name="exchangeHours">Defines the hours this exchange is open</param>
        /// <param name="quoteCurrency">The cash object that represent the quote currency</param>
        /// <param name="config">The subscription configuration for this security</param>
        /// <param name="symbolProperties">The symbol properties for this security</param>
        public Option(SecurityExchangeHours exchangeHours, SubscriptionDataConfig config, Cash quoteCurrency, SymbolProperties symbolProperties)
            : base(config,
                quoteCurrency,
                symbolProperties,
                new OptionExchange(exchangeHours),
                new OptionCache(),
                new SecurityPortfolioModel(),
                new ImmediateFillModel(),
                new InteractiveBrokersFeeModel(),
                new SpreadSlippageModel(),
                new ImmediateSettlementModel(),
                Securities.VolatilityModel.Null,
                new SecurityMarginModel(2m),
                new OptionDataFilter(),
                new AdjustedPriceVariationModel()
                )
        {
            PriceModel = new CurrentPriceOptionPriceModel();
            ContractFilter = new StrikeExpiryOptionFilter(-5, 5, TimeSpan.Zero, TimeSpan.FromDays(35));
        }

        /// <summary>
        /// Gets or sets the underlying security object.
        /// </summary>
        public Security Underlying
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the price model for this option security
        /// </summary>
        public IOptionPriceModel PriceModel
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the contract filter
        /// </summary>
        public IDerivativeSecurityFilter ContractFilter
        {
            get; set;
        }

        /// <summary>
        /// Sets the <see cref="ContractFilter"/> to a new instance of the <see cref="StrikeExpiryOptionFilter"/>
        /// using the specified min and max strike values. Contracts with expirations further than 35
        /// days out will also be filtered.
        /// </summary>
        /// <param name="minStrike">The min strike rank relative to market price, for example, -1 would put
        /// a lower bound of one strike under market price, where a +1 would put a lower bound of one strike
        /// over market price</param>
        /// <param name="maxStrike">The max strike rank relative to market place, for example, -1 would put
        /// an upper bound of on strike under market price, where a +1 would be an upper bound of one strike
        /// over market price</param>
        public void SetFilter(int minStrike, int maxStrike)
        {
            SetFilter(minStrike, maxStrike, TimeSpan.Zero, TimeSpan.FromDays(35));
        }

        /// <summary>
        /// Sets the <see cref="ContractFilter"/> to a new instance of the <see cref="StrikeExpiryOptionFilter"/>
        /// using the specified min and max strike and expiration range alues
        /// </summary>
        /// <param name="minStrike">The min strike rank relative to market price, for example, -1 would put
        /// a lower bound of one strike under market price, where a +1 would put a lower bound of one strike
        /// over market price</param>
        /// <param name="maxStrike">The max strike rank relative to market place, for example, -1 would put
        /// an upper bound of on strike under market price, where a +1 would be an upper bound of one strike
        /// over market price</param>
        /// <param name="minExpiry">The minimum time until expiry to include, for example, TimeSpan.FromDays(10)
        /// would exclude contracts expiring in less than 10 days</param>
        /// <param name="maxExpiry">The maxmium time until expiry to include, for example, TimeSpan.FromDays(10)
        /// would exclude contracts expiring in more than 10 days</param>
        public void SetFilter(int minStrike, int maxStrike, TimeSpan minExpiry, TimeSpan maxExpiry)
        {
            ContractFilter = new StrikeExpiryOptionFilter(minStrike, maxStrike, minExpiry, maxExpiry);
        }
    }
}
