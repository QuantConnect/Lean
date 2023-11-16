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
using QuantConnect.Securities.Option;

namespace QuantConnect.Securities.FutureOption
{
    /// <summary>
    /// Futures Options security
    /// </summary>
    public class FutureOption : Option.Option
    {
        /// <summary>
        /// Constructor for the future option security
        /// </summary>
        /// <param name="symbol">Symbol of the future option</param>
        /// <param name="exchangeHours">Exchange hours of the future option</param>
        /// <param name="quoteCurrency">Quoted currency of the future option</param>
        /// <param name="symbolProperties">Symbol properties of the future option</param>
        /// <param name="currencyConverter">Currency converter</param>
        /// <param name="registeredTypes">Provides all data types registered to the algorithm</param>
        /// <param name="securityCache">Cache of security objects</param>
        /// <param name="underlying">Future underlying security</param>
        public FutureOption(Symbol symbol,
            SecurityExchangeHours exchangeHours,
            Cash quoteCurrency,
            OptionSymbolProperties symbolProperties,
            ICurrencyConverter currencyConverter,
            IRegisteredSecurityDataTypesProvider registeredTypes,
            SecurityCache securityCache,
            Security underlying)
            : base(symbol,
                quoteCurrency,
                symbolProperties,
                new OptionExchange(exchangeHours),
                securityCache,
                new OptionPortfolioModel(),
                new FutureOptionFillModel(),
                new InteractiveBrokersFeeModel(),
                NullSlippageModel.Instance,
                new ImmediateSettlementModel(),
                Securities.VolatilityModel.Null,
                null,
                new OptionDataFilter(),
                new SecurityPriceVariationModel(),
                currencyConverter,
                registeredTypes,
                underlying
        )
        {
            BuyingPowerModel = new FuturesOptionsMarginModel(0, this);
        }
    }
}
