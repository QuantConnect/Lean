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
using QuantConnect.Orders.Fees;
using QuantConnect.Orders.Fills;
using QuantConnect.Orders.Slippage;
using Python.Runtime;
using QuantConnect.Util;

namespace QuantConnect.Securities.Future
{
    /// <summary>
    /// Futures Security Object Implementation for Futures Assets
    /// </summary>
    /// <seealso cref="Security"/>
    public class Future : Security, IDerivativeSecurity
    {
        /// <summary>
        /// The default number of days required to settle a futures sale
        /// </summary>
        public const int DefaultSettlementDays = 1;

        /// <summary>
        /// The default time of day for settlement
        /// </summary>
        public static readonly TimeSpan DefaultSettlementTime = new TimeSpan(8, 0, 0);

        /// <summary>
        /// Constructor for the Future security
        /// </summary>
        /// <param name="exchangeHours">Defines the hours this exchange is open</param>
        /// <param name="quoteCurrency">The cash object that represent the quote currency</param>
        /// <param name="config">The subscription configuration for this security</param>
        /// <param name="symbolProperties">The symbol properties for this security</param>
        /// <param name="currencyConverter">Currency converter used to convert <see cref="CashAmount"/>
        /// instances into units of the account currency</param>
        /// <param name="registeredTypes">Provides all data types registered in the algorithm</param>
        public Future(SecurityExchangeHours exchangeHours,
            SubscriptionDataConfig config,
            Cash quoteCurrency,
            SymbolProperties symbolProperties,
            ICurrencyConverter currencyConverter,
            IRegisteredSecurityDataTypesProvider registeredTypes
            )
            : base(config,
                quoteCurrency,
                symbolProperties,
                new FutureExchange(exchangeHours),
                new FutureCache(),
                new SecurityPortfolioModel(),
                new ImmediateFillModel(),
                new InteractiveBrokersFeeModel(),
                new ConstantSlippageModel(0),
                new ImmediateSettlementModel(),
                Securities.VolatilityModel.Null,
                new FutureMarginModel(),
                new SecurityDataFilter(),
                new SecurityPriceVariationModel(),
                currencyConverter,
                registeredTypes
                )
        {
            // for now all futures are cash settled as we don't allow underlying (Live Cattle?) to be posted on the account
            SettlementType = SettlementType.Cash;
            Holdings = new FutureHolding(this, currencyConverter);
            _symbolProperties = symbolProperties;
            SetFilter(TimeSpan.Zero, TimeSpan.FromDays(35));
        }

        /// <summary>
        /// Constructor for the Future security
        /// </summary>
        /// <param name="symbol">The subscription security symbol</param>
        /// <param name="exchangeHours">Defines the hours this exchange is open</param>
        /// <param name="quoteCurrency">The cash object that represent the quote currency</param>
        /// <param name="symbolProperties">The symbol properties for this security</param>
        /// <param name="currencyConverter">Currency converter used to convert <see cref="CashAmount"/>
        ///     instances into units of the account currency</param>
        /// <param name="registeredTypes">Provides all data types registered in the algorithm</param>
        public Future(Symbol symbol,
            SecurityExchangeHours exchangeHours,
            Cash quoteCurrency,
            SymbolProperties symbolProperties,
            ICurrencyConverter currencyConverter,
            IRegisteredSecurityDataTypesProvider registeredTypes,
            SecurityCache securityCache
            )
            : base(symbol,
                quoteCurrency,
                symbolProperties,
                new FutureExchange(exchangeHours),
                securityCache,
                new SecurityPortfolioModel(),
                new ImmediateFillModel(),
                new InteractiveBrokersFeeModel(),
                new ConstantSlippageModel(0),
                new ImmediateSettlementModel(),
                Securities.VolatilityModel.Null,
                new FutureMarginModel(),
                new SecurityDataFilter(),
                new SecurityPriceVariationModel(),
                currencyConverter,
                registeredTypes
                )
        {
            // for now all futures are cash settled as we don't allow underlying (Live Cattle?) to be posted on the account
            SettlementType = SettlementType.Cash;
            Holdings = new FutureHolding(this, currencyConverter);
            _symbolProperties = symbolProperties;
            SetFilter(TimeSpan.Zero, TimeSpan.FromDays(35));
        }

        // save off a strongly typed version of symbol properties
        private readonly SymbolProperties _symbolProperties;

        /// <summary>
        /// Returns true if this is the future chain security, false if it is a specific future contract
        /// </summary>
        public bool IsFutureChain => Symbol.IsCanonical();

        /// <summary>
        /// Returns true if this is a specific future contract security, false if it is the future chain security
        /// </summary>
        public bool IsFutureContract => !Symbol.IsCanonical();

        /// <summary>
        /// Gets the expiration date
        /// </summary>
        public DateTime Expiry
        {
            get { return Symbol.ID.Date; }
        }

        /// <summary>
        /// Specifies if futures contract has physical or cash settlement on settlement
        /// </summary>
        public SettlementType SettlementType
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the underlying security object.
        /// </summary>
        public Security Underlying
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
        /// Sets the <see cref="ContractFilter"/> to a new instance of the filter
        /// using the specified expiration range values
        /// </summary>
        /// <param name="minExpiry">The minimum time until expiry to include, for example, TimeSpan.FromDays(10)
        /// would exclude contracts expiring in more than 10 days</param>
        /// <param name="maxExpiry">The maximum time until expiry to include, for example, TimeSpan.FromDays(10)
        /// would exclude contracts expiring in less than 10 days</param>
        public void SetFilter(TimeSpan minExpiry, TimeSpan maxExpiry)
        {
            SetFilter(universe => universe.Expiration(minExpiry, maxExpiry));
        }

        /// <summary>
        /// Sets the <see cref="ContractFilter"/> to a new instance of the filter
        /// using the specified expiration range values
        /// </summary>
        /// <param name="minExpiryDays">The minimum time, expressed in days, until expiry to include, for example, 10
        /// would exclude contracts expiring in more than 10 days</param>
        /// <param name="maxExpiryDays">The maximum time, expressed in days, until expiry to include, for example, 10
        /// would exclude contracts expiring in less than 10 days</param>
        public void SetFilter(int minExpiryDays, int maxExpiryDays)
        {
            SetFilter(universe => universe.Expiration(minExpiryDays, maxExpiryDays));
        }

        /// <summary>
        /// Sets the <see cref="ContractFilter"/> to a new universe selection function
        /// </summary>
        /// <param name="universeFunc">new universe selection function</param>
        public void SetFilter(Func<FutureFilterUniverse, FutureFilterUniverse> universeFunc)
        {
            Func<IDerivativeSecurityFilterUniverse, IDerivativeSecurityFilterUniverse> func = universe =>
            {
                var futureUniverse = universe as FutureFilterUniverse;
                var result = universeFunc(futureUniverse);
                return result.ApplyTypesFilter();
            };

            ContractFilter = new FuncSecurityDerivativeFilter(func);
        }

        /// <summary>
        /// Sets the <see cref="ContractFilter"/> to a new universe selection function
        /// </summary>
        /// <param name="universeFunc">new universe selection function</param>
        public void SetFilter(PyObject universeFunc)
        {
            var pyUniverseFunc = PythonUtil.ToFunc<FutureFilterUniverse, FutureFilterUniverse>(universeFunc);
            SetFilter(pyUniverseFunc);
        }
    }
}
