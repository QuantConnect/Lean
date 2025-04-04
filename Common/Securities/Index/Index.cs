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

namespace QuantConnect.Securities.Index
{
    /// <summary>
    /// INDEX Security Object Implementation for INDEX Assets
    /// </summary>
    /// <seealso cref="Security"/>
    public class Index : Security
    {
        private bool _isTradable;

        /// <summary>
        /// Gets or sets whether or not this security should be considered tradable
        /// </summary>
        public override bool IsTradable {
            get => _isTradable;
            set
            {
                if (value) ManualSetIsTradable = true;
                _isTradable = value;
            }
        }

        /// <summary>
        /// Field to check if the user has manually set IsTradable field to true
        /// </summary>
        internal bool ManualSetIsTradable { get; set; }

        /// <summary>
        /// Constructor for the INDEX security
        /// </summary>
        /// <param name="exchangeHours">Defines the hours this exchange is open</param>
        /// <param name="quoteCurrency">The cash object that represent the quote currency</param>
        /// <param name="config">The subscription configuration for this security</param>
        /// <param name="symbolProperties">The symbol properties for this security</param>
        /// <param name="currencyConverter">Currency converter used to convert <see cref="CashAmount"/>
        /// instances into units of the account currency</param>
        /// <param name="registeredTypes">Provides all data types registered in the algorithm</param>
        public Index(SecurityExchangeHours exchangeHours,
            Cash quoteCurrency,
            SubscriptionDataConfig config,
            SymbolProperties symbolProperties,
            ICurrencyConverter currencyConverter,
            IRegisteredSecurityDataTypesProvider registeredTypes)
            : base(config,
                quoteCurrency,
                symbolProperties,
                new IndexExchange(exchangeHours),
                new IndexCache(),
                new SecurityPortfolioModel(),
                new ImmediateFillModel(),
                new ConstantFeeModel(0),
                NullSlippageModel.Instance,
                new ImmediateSettlementModel(),
                Securities.VolatilityModel.Null,
                new SecurityMarginModel(50m),
                new IndexDataFilter(),
                new SecurityPriceVariationModel(),
                currencyConverter,
                registeredTypes,
                Securities.MarginInterestRateModel.Null
                )
        {
            IsTradable = false;   //Index are non tradable by default
            Holdings = new IndexHolding(this, currencyConverter);
        }

        /// <summary>
        /// Constructor for the INDEX security
        /// </summary>
        /// <param name="symbol">The security's symbol</param>
        /// <param name="exchangeHours">Defines the hours this exchange is open</param>
        /// <param name="quoteCurrency">The cash object that represent the quote currency</param>
        /// <param name="symbolProperties">The symbol properties for this security</param>
        /// <param name="currencyConverter">Currency converter used to convert <see cref="CashAmount"/>
        /// instances into units of the account currency</param>
        /// <param name="registeredTypes">Provides all data types registered in the algorithm</param>
        /// <param name="securityCache">Cache to store security information</param>
        public Index(Symbol symbol,
            SecurityExchangeHours exchangeHours,
            Cash quoteCurrency,
            SymbolProperties symbolProperties,
            ICurrencyConverter currencyConverter,
            IRegisteredSecurityDataTypesProvider registeredTypes,
            SecurityCache securityCache)
            : base(symbol,
                quoteCurrency,
                symbolProperties,
                new IndexExchange(exchangeHours),
                securityCache,
                new SecurityPortfolioModel(),
                new ImmediateFillModel(),
                new ConstantFeeModel(0),
                NullSlippageModel.Instance,
                new ImmediateSettlementModel(),
                Securities.VolatilityModel.Null,
                new SecurityMarginModel(50m),
                new IndexDataFilter(),
                new SecurityPriceVariationModel(),
                currencyConverter,
                registeredTypes,
                Securities.MarginInterestRateModel.Null
                )
        {
            IsTradable = false;   //Index are non tradable by default
            Holdings = new IndexHolding(this, currencyConverter);
        }

        /// <summary>
        /// Resets the security to its initial state by marking it as uninitialized and non-tradable
        /// and clearing the subscriptions.
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            ManualSetIsTradable = false;
        }
    }
}
