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
using QuantConnect.Orders.Fees;
using QuantConnect.Orders.Fills;
using QuantConnect.Orders.Slippage;
using QuantConnect.Securities.Option;

namespace QuantConnect.Securities.IndexOption
{
    /// <summary>
    /// Index Options security
    /// </summary>
    public class IndexOption : Option.Option
    {
        /// <summary>
        /// Constructor for the index option security
        /// </summary>
        /// <param name="symbol">Symbol of the index option</param>
        /// <param name="exchangeHours">Exchange hours of the index option</param>
        /// <param name="quoteCurrency">Quoted currency of the index option</param>
        /// <param name="symbolProperties">Symbol properties of the index option</param>
        /// <param name="currencyConverter">Currency converter</param>
        /// <param name="registeredTypes">Provides all data types registered to the algorithm</param>
        /// <param name="securityCache">Cache of security objects</param>
        /// <param name="underlying">Future underlying security</param>
        /// <param name="settlementType">Settlement type for the index option. Most index options are cash-settled.</param>
        public IndexOption(Symbol symbol,
            SecurityExchangeHours exchangeHours,
            Cash quoteCurrency,
            IndexOptionSymbolProperties symbolProperties,
            ICurrencyConverter currencyConverter,
            IRegisteredSecurityDataTypesProvider registeredTypes,
            SecurityCache securityCache,
            Security underlying,
            SettlementType settlementType = SettlementType.Cash)
            : base(symbol,
                quoteCurrency,
                symbolProperties,
                new OptionExchange(exchangeHours),
                securityCache,
                new OptionPortfolioModel(),
                new ImmediateFillModel(),
                new InteractiveBrokersFeeModel(),
                new ConstantSlippageModel(0),
                new ImmediateSettlementModel(),
                Securities.VolatilityModel.Null,
                new OptionMarginModel(),
                new OptionDataFilter(),
                new IndexOptionPriceVariationModel(),
                currencyConverter,
                registeredTypes,
                underlying
            )
        {
            ExerciseSettlement = settlementType;
        }

        /// <summary>
        /// Consumes market price data and updates the minimum price variation
        /// </summary>
        /// <param name="data">Market price data</param>
        /// <remarks>
        /// Index options have variable sized minimum price variations.
        /// For prices greater than or equal to $3.00 USD, the minimum price variation is $0.10 USD.
        /// For prices less than $3.00 USD, the minimum price variation is $0.05 USD.
        /// </remarks>
        protected override void UpdateConsumersMarketPrice(BaseData data)
        {
            base.UpdateConsumersMarketPrice(data);
            ((IndexOptionSymbolProperties)SymbolProperties).UpdateMarketPrice(data);
        }
    }
}
