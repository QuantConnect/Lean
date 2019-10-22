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
using QuantConnect.Orders.OptionExercise;
using Python.Runtime;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Util;

namespace QuantConnect.Securities.Option
{
    /// <summary>
    /// Option Security Object Implementation for Option Assets
    /// </summary>
    /// <seealso cref="Security"/>
    public class Option : Security, IDerivativeSecurity, IOptionPrice
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
        /// <param name="currencyConverter">Currency converter used to convert <see cref="CashAmount"/>
        /// instances into units of the account currency</param>
        /// <param name="registeredTypes">Provides all data types registered in the algorithm</param>
        public Option(SecurityExchangeHours exchangeHours,
            SubscriptionDataConfig config,
            Cash quoteCurrency,
            OptionSymbolProperties symbolProperties,
            ICurrencyConverter currencyConverter,
            IRegisteredSecurityDataTypesProvider registeredTypes)
            : base(config,
                quoteCurrency,
                symbolProperties,
                new OptionExchange(exchangeHours),
                new OptionCache(),
                new OptionPortfolioModel(),
                new ImmediateFillModel(),
                new InteractiveBrokersFeeModel(),
                new ConstantSlippageModel(0),
                new ImmediateSettlementModel(),
                Securities.VolatilityModel.Null,
                new OptionMarginModel(),
                new OptionDataFilter(),
                new SecurityPriceVariationModel(),
                currencyConverter,
                registeredTypes
                )
        {
            ExerciseSettlement = SettlementType.PhysicalDelivery;
            SetDataNormalizationMode(DataNormalizationMode.Raw);
            OptionExerciseModel = new DefaultExerciseModel();
            PriceModel = new CurrentPriceOptionPriceModel();
            Holdings = new OptionHolding(this, currencyConverter);
            _symbolProperties = symbolProperties;
            SetFilter(-1, 1, TimeSpan.Zero, TimeSpan.FromDays(35));
        }

        /// <summary>
        /// Constructor for the option security
        /// </summary>
        /// <param name="symbol">The symbol of the security</param>
        /// <param name="exchangeHours">Defines the hours this exchange is open</param>
        /// <param name="quoteCurrency">The cash object that represent the quote currency</param>
        /// <param name="symbolProperties">The symbol properties for this security</param>
        /// <param name="currencyConverter">Currency converter used to convert <see cref="CashAmount"/>
        /// instances into units of the account currency</param>
        /// <param name="registeredTypes">Provides all data types registered in the algorithm</param>
        public Option(Symbol symbol,
            SecurityExchangeHours exchangeHours,
            Cash quoteCurrency,
            OptionSymbolProperties symbolProperties,
            ICurrencyConverter currencyConverter,
            IRegisteredSecurityDataTypesProvider registeredTypes,
            SecurityCache securityCache)
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
               new SecurityPriceVariationModel(),
               currencyConverter,
               registeredTypes
               )
        {
            ExerciseSettlement = SettlementType.PhysicalDelivery;
            SetDataNormalizationMode(DataNormalizationMode.Raw);
            OptionExerciseModel = new DefaultExerciseModel();
            PriceModel = new CurrentPriceOptionPriceModel();
            Holdings = new OptionHolding(this, currencyConverter);
            _symbolProperties = symbolProperties;
            SetFilter(-1, 1, TimeSpan.Zero, TimeSpan.FromDays(35));
        }

        // save off a strongly typed version of symbol properties
        private readonly OptionSymbolProperties _symbolProperties;

        /// <summary>
        /// Returns true if this is the option chain security, false if it is a specific option contract
        /// </summary>
        public bool IsOptionChain => Symbol.IsCanonical();

        /// <summary>
        /// Returns true if this is a specific option contract security, false if it is the option chain security
        /// </summary>
        public bool IsOptionContract => !Symbol.IsCanonical();

        /// <summary>
        /// Gets the strike price
        /// </summary>
        public decimal StrikePrice
        {
            get { return Symbol.ID.StrikePrice; }
        }

        /// <summary>
        /// Gets the expiration date
        /// </summary>
        public DateTime Expiry
        {
            get { return Symbol.ID.Date; }
        }

        /// <summary>
        /// Gets the right being purchased (call [right to buy] or put [right to sell])
        /// </summary>
        public OptionRight Right
        {
            get { return Symbol.ID.OptionRight; }
        }

        /// <summary>
        /// Gets the option style
        /// </summary>
        public OptionStyle Style
        {
            get { return Symbol.ID.OptionStyle;  }
        }

        /// <summary>
        /// Gets the most recent bid price if available
        /// </summary>
        public override decimal BidPrice => Cache.BidPrice;

        /// <summary>
        /// Gets the most recent ask price if available
        /// </summary>
        public override decimal AskPrice => Cache.AskPrice;

        /// <summary>
        /// When the holder of an equity option exercises one contract, or when the writer of an equity option is assigned
        /// an exercise notice on one contract, this unit of trade, usually 100 shares of the underlying security, changes hands.
        /// </summary>
        public int ContractUnitOfTrade
        {
            get
            {
                return _symbolProperties.ContractUnitOfTrade;
            }
            set
            {
                _symbolProperties.SetContractUnitOfTrade(value);
            }
        }

        /// <summary>
        /// The contract multiplier for the option security
        /// </summary>
        public int ContractMultiplier
        {
            get
            {
                return (int)_symbolProperties.ContractMultiplier;
            }
            set
            {
                _symbolProperties.SetContractMultiplier(value);
            }
        }

        /// <summary>
        /// Aggregate exercise amount or aggregate contract value. It is the total amount of cash one will pay (or receive) for the shares of the
        /// underlying stock if he/she decides to exercise (or is assigned an exercise notice). This amount is not the premium paid or received for an equity option.
        /// </summary>
        public decimal GetAggregateExerciseAmount()
        {
            return StrikePrice * ContractMultiplier;
        }

        /// <summary>
        /// Returns the actual number of the underlying shares that are going to change hands on exercise. For instance, after reverse split
        /// we may have 1 option contract with multiplier of 100 with right to buy/sell only 50 shares of underlying stock.
        /// </summary>
        /// <returns></returns>
        public decimal GetExerciseQuantity(decimal quantity)
        {
            return quantity * ContractUnitOfTrade;
        }

        /// <summary>
        /// Checks if option is eligible for automatic exercise on expiration
        /// </summary>
        public bool IsAutoExercised(decimal underlyingPrice)
        {
            return GetIntrinsicValue(underlyingPrice) >= 0.01m;
        }

        /// <summary>
        /// Intrinsic value function of the option
        /// </summary>
        public decimal GetIntrinsicValue(decimal underlyingPrice)
        {
            return Math.Max(0.0m, GetPayOff(underlyingPrice));
        }
        /// <summary>
        /// Option payoff function at expiration time
        /// </summary>
        /// <param name="underlyingPrice">The price of the underlying</param>
        /// <returns></returns>
        public decimal GetPayOff(decimal underlyingPrice)
        {
            return Right == OptionRight.Call ? underlyingPrice - StrikePrice : StrikePrice - underlyingPrice;
        }

        /// <summary>
        /// Specifies if option contract has physical or cash settlement on exercise
        /// </summary>
        public SettlementType ExerciseSettlement
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
        /// Gets a reduced interface of the underlying security object.
        /// </summary>
        ISecurityPrice IOptionPrice.Underlying => Underlying;

        /// <summary>
        /// For this option security object, evaluates the specified option
        /// contract to compute a theoretical price, IV and greeks
        /// </summary>
        /// <param name="slice">The current data slice. This can be used to access other information
        /// available to the algorithm</param>
        /// <param name="contract">The option contract to evaluate</param>
        /// <returns>An instance of <see cref="OptionPriceModelResult"/> containing the theoretical
        /// price of the specified option contract</returns>
        public OptionPriceModelResult EvaluatePriceModel(Slice slice, OptionContract contract)
        {
            return PriceModel.Evaluate(this, slice, contract);
        }

        /// <summary>
        /// Gets or sets the price model for this option security
        /// </summary>
        public IOptionPriceModel PriceModel
        {
            get; set;
        }

        /// <summary>
        /// Fill model used to produce fill events for this security
        /// </summary>
        public IOptionExerciseModel OptionExerciseModel
        {
            get; set;
        }

        /// <summary>
        /// When enabled, approximates Greeks if corresponding pricing model didn't calculate exact numbers
        /// </summary>
        [Obsolete("This property has been deprecated. Please use QLOptionPriceModel.EnableGreekApproximation instead.")]
        public bool EnableGreekApproximation
        {
            get
            {
                var model = PriceModel as QLOptionPriceModel;
                if (model != null)
                {
                    return model.EnableGreekApproximation;
                }
                return false;
            }

            set
            {
                var model = PriceModel as QLOptionPriceModel;
                if (model != null)
                {
                   model.EnableGreekApproximation = value;
                }
            }
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
            SetFilter(universe => universe.Strikes(minStrike, maxStrike));
        }

        /// <summary>
        /// Sets the <see cref="ContractFilter"/> to a new instance of the filter
        /// using the specified min and max strike and expiration range values
        /// </summary>
        /// <param name="minExpiry">The minimum time until expiry to include, for example, TimeSpan.FromDays(10)
        /// would exclude contracts expiring in less than 10 days</param>
        /// <param name="maxExpiry">The maxmium time until expiry to include, for example, TimeSpan.FromDays(10)
        /// would exclude contracts expiring in more than 10 days</param>
        public void SetFilter(TimeSpan minExpiry, TimeSpan maxExpiry)
        {
            SetFilter(universe => universe.Expiration(minExpiry, maxExpiry));
        }

        /// <summary>
        /// Sets the <see cref="ContractFilter"/> to a new instance of the filter
        /// using the specified min and max strike and expiration range values
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
            SetFilter(universe => universe
                .Strikes(minStrike, maxStrike)
                .Expiration(minExpiry, maxExpiry));
        }

        /// <summary>
        /// Sets the <see cref="ContractFilter"/> to a new universe selection function
        /// </summary>
        /// <param name="universeFunc">new universe selection function</param>
        public void SetFilter(Func<OptionFilterUniverse, OptionFilterUniverse> universeFunc)
        {
            ContractFilter = new FuncSecurityDerivativeFilter(universe =>
            {
                var optionUniverse = universe as OptionFilterUniverse;
                var result = universeFunc(optionUniverse);
                return result.ApplyOptionTypesFilter();
            });
        }

        /// <summary>
        /// Sets the <see cref="ContractFilter"/> to a new universe selection function
        /// </summary>
        /// <param name="universeFunc">new universe selection function</param>
        public void SetFilter(PyObject universeFunc)
        {
            var pyUniverseFunc = PythonUtil.ToFunc<OptionFilterUniverse, OptionFilterUniverse>(universeFunc);
            SetFilter(pyUniverseFunc);
        }

        /// <summary>
        /// Sets the data normalization mode to be used by this security
        /// </summary>
        public override void SetDataNormalizationMode(DataNormalizationMode mode)
        {
            if (mode != DataNormalizationMode.Raw)
            {
                throw new ArgumentException("DataNormalizationMode.Raw must be used with options");
            }

            base.SetDataNormalizationMode(mode);
        }
    }
}
