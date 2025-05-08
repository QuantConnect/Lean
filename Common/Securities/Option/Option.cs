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

using Python.Runtime;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Orders.Fills;
using QuantConnect.Orders.OptionExercise;
using QuantConnect.Orders.Slippage;
using QuantConnect.Python;
using QuantConnect.Securities.Interfaces;
using QuantConnect.Util;
using System;
using System.Collections.Generic;

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
        public static int DefaultSettlementDays { get; set; } = 1;

        /// <summary>
        /// The default time of day for settlement
        /// </summary>
        public static readonly TimeSpan DefaultSettlementTime = new (6, 0, 0);

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
        /// <remarks>Used in testing</remarks>
        public Option(SecurityExchangeHours exchangeHours,
            SubscriptionDataConfig config,
            Cash quoteCurrency,
            OptionSymbolProperties symbolProperties,
            ICurrencyConverter currencyConverter,
            IRegisteredSecurityDataTypesProvider registeredTypes)
            : this(config.Symbol,
                quoteCurrency,
                symbolProperties,
                new OptionExchange(exchangeHours),
                new OptionCache(),
                new OptionPortfolioModel(),
                new ImmediateFillModel(),
                new InteractiveBrokersFeeModel(),
                NullSlippageModel.Instance,
                new ImmediateSettlementModel(),
                Securities.VolatilityModel.Null,
                new OptionMarginModel(),
                new OptionDataFilter(),
                new SecurityPriceVariationModel(),
                currencyConverter,
                registeredTypes,
                null)
        {
            AddData(config);
            SetDataNormalizationMode(DataNormalizationMode.Raw);
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
        /// <param name="securityCache">Cache to store security information</param>
        /// <param name="underlying">Future underlying security</param>
        public Option(Symbol symbol,
            SecurityExchangeHours exchangeHours,
            Cash quoteCurrency,
            OptionSymbolProperties symbolProperties,
            ICurrencyConverter currencyConverter,
            IRegisteredSecurityDataTypesProvider registeredTypes,
            SecurityCache securityCache,
            Security underlying)
           : this(symbol,
               quoteCurrency,
               symbolProperties,
               new OptionExchange(exchangeHours),
               securityCache,
               new OptionPortfolioModel(),
               new ImmediateFillModel(),
               new InteractiveBrokersFeeModel(),
               NullSlippageModel.Instance,
               new ImmediateSettlementModel(),
               Securities.VolatilityModel.Null,
               new OptionMarginModel(),
               new OptionDataFilter(),
               new SecurityPriceVariationModel(),
               currencyConverter,
               registeredTypes,
               underlying)
        {
        }

        /// <summary>
        /// Creates instance of the Option class.
        /// </summary>
        /// <remarks>
        /// Allows for the forwarding of the security configuration to the
        /// base Security constructor
        /// </remarks>
        protected Option(Symbol symbol,
            Cash quoteCurrency,
            SymbolProperties symbolProperties,
            SecurityExchange exchange,
            SecurityCache cache,
            ISecurityPortfolioModel portfolioModel,
            IFillModel fillModel,
            IFeeModel feeModel,
            ISlippageModel slippageModel,
            ISettlementModel settlementModel,
            IVolatilityModel volatilityModel,
            IBuyingPowerModel buyingPowerModel,
            ISecurityDataFilter dataFilter,
            IPriceVariationModel priceVariationModel,
            ICurrencyConverter currencyConverter,
            IRegisteredSecurityDataTypesProvider registeredTypesProvider,
            Security underlying
        ) : base(
            symbol,
            quoteCurrency,
            symbolProperties,
            exchange,
            cache,
            portfolioModel,
            fillModel,
            feeModel,
            slippageModel,
            settlementModel,
            volatilityModel,
            buyingPowerModel,
            dataFilter,
            priceVariationModel,
            currencyConverter,
            registeredTypesProvider,
            Securities.MarginInterestRateModel.Null
        )
        {
            ExerciseSettlement = SettlementType.PhysicalDelivery;
            SetDataNormalizationMode(DataNormalizationMode.Raw);
            OptionExerciseModel = new DefaultExerciseModel();
            PriceModel = symbol.ID.OptionStyle switch
            {
                // CRR model has the best accuracy and speed suggested by
                // Branka, Zdravka & Tea (2014). Numerical Methods versus Bjerksund and Stensland Approximations for American Options Pricing.
                // International Journal of Economics and Management Engineering. 8:4.
                // Available via: https://downloads.dxfeed.com/specifications/dxLibOptions/Numerical-Methods-versus-Bjerksund-and-Stensland-Approximations-for-American-Options-Pricing-.pdf
                // Also refer to OptionPriceModelTests.MatchesIBGreeksBulk() test,
                // we select the most accurate and computational efficient model
                OptionStyle.American => OptionPriceModels.BinomialCoxRossRubinstein(),
                OptionStyle.European => OptionPriceModels.BlackScholes(),
                _ => throw new ArgumentException("Invalid OptionStyle")
            };
            Holdings = new OptionHolding(this, currencyConverter);
            _symbolProperties = (OptionSymbolProperties)symbolProperties;
            SetFilter(-1, 1, TimeSpan.Zero, TimeSpan.FromDays(35));
            Underlying = underlying;
            OptionAssignmentModel = new DefaultOptionAssignmentModel();
            ScaledStrikePrice = StrikePrice * SymbolProperties.StrikeMultiplier;
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
        public decimal StrikePrice => Symbol.ID.StrikePrice;

        /// <summary>
        /// Gets the strike price multiplied by the strike multiplier
        /// </summary>
        public decimal ScaledStrikePrice
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the expiration date
        /// </summary>
        public DateTime Expiry => Symbol.ID.Date;

        /// <summary>
        /// Gets the right being purchased (call [right to buy] or put [right to sell])
        /// </summary>
        public OptionRight Right => Symbol.ID.OptionRight;

        /// <summary>
        /// Gets the option style
        /// </summary>
        public OptionStyle Style => Symbol.ID.OptionStyle;

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
        /// Returns the directional quantity of underlying shares that are going to change hands on exercise/assignment of all
        /// contracts held by this account, taking into account the contract's <see cref="Right"/> as well as the contract's current
        /// <see cref="ContractUnitOfTrade"/>, which may have recently changed due to a split/reverse split in the underlying security.
        /// </summary>
        /// <remarks>
        /// Long option positions result in exercise while short option positions result in assignment. This function uses the term
        /// exercise loosely to refer to both situations.
        /// </remarks>
        public decimal GetExerciseQuantity()
        {
            // negate Holdings.Quantity to match an equivalent order
            return GetExerciseQuantity(-Holdings.Quantity);
        }

        /// <summary>
        /// Returns the directional quantity of underlying shares that are going to change hands on exercise/assignment of the
        /// specified <paramref name="exerciseOrderQuantity"/>, taking into account the contract's <see cref="Right"/> as well
        /// as the contract's current <see cref="ContractUnitOfTrade"/>, which may have recently changed due to a split/reverse
        /// split in the underlying security.
        /// </summary>
        /// <remarks>
        /// Long option positions result in exercise while short option positions result in assignment. This function uses the term
        /// exercise loosely to refer to both situations.
        /// </remarks>
        /// <paramref name="exerciseOrderQuantity">The quantity of contracts being exercised as provided by the <see cref="OptionExerciseOrder"/>.
        /// A negative value indicates exercise (we are long and the order quantity is negative to bring us (closer) to zero.
        /// A positive value indicates assignment (we are short and the order quantity is positive to bring us (closer) to zero.</paramref>
        public decimal GetExerciseQuantity(decimal exerciseOrderQuantity)
        {
            // when exerciseOrderQuantity > 0 [ we are short ]
            //      && right == call => we sell to contract holder  => negative
            //      && right == put  => we buy from contract holder => positive

            // when exerciseOrderQuantity < 0 [ we are long ]
            //      && right == call => we buy from contract holder => positive
            //      && right == put  => we sell to contract holder  => negative

            var sign = Right == OptionRight.Call ? -1 : 1;
            return sign * exerciseOrderQuantity * ContractUnitOfTrade;
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
            return OptionPayoff.GetIntrinsicValue(underlyingPrice, ScaledStrikePrice, Right);
        }

        /// <summary>
        /// Option payoff function at expiration time
        /// </summary>
        /// <param name="underlyingPrice">The price of the underlying</param>
        /// <returns></returns>
        public decimal GetPayOff(decimal underlyingPrice)
        {
            return OptionPayoff.GetPayOff(underlyingPrice, ScaledStrikePrice, Right);
        }

        /// <summary>
        /// Option out of the money function
        /// </summary>
        /// <param name="underlyingPrice">The price of the underlying</param>
        /// <returns></returns>
        public decimal OutOfTheMoneyAmount(decimal underlyingPrice)
        {
            return Math.Max(0, Right == OptionRight.Call ? ScaledStrikePrice - underlyingPrice : underlyingPrice - ScaledStrikePrice);
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
        /// The automatic option assignment model
        /// </summary>
        public IOptionAssignmentModel OptionAssignmentModel
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
        public IDerivativeSecurityFilter<OptionUniverse> ContractFilter
        {
            get; set;
        }

        /// <summary>
        /// Sets the automatic option assignment model
        /// </summary>
        /// <param name="pyObject">The option assignment model to use</param>
        public void SetOptionAssignmentModel(PyObject pyObject)
        {
            if (pyObject.TryConvert<IOptionAssignmentModel>(out var optionAssignmentModel))
            {
                // pure C# implementation
                SetOptionAssignmentModel(optionAssignmentModel);
            }
            else if (Extensions.TryConvert<IOptionAssignmentModel>(pyObject, out _, allowPythonDerivative: true))
            {
                SetOptionAssignmentModel(new OptionAssignmentModelPythonWrapper(pyObject));
            }
            else
            {
                using(Py.GIL())
                {
                    throw new ArgumentException($"SetOptionAssignmentModel: {pyObject.Repr()} is not a valid argument.");
                }
            }
        }

        /// <summary>
        /// Sets the automatic option assignment model
        /// </summary>
        /// <param name="optionAssignmentModel">The option assignment model to use</param>
        public void SetOptionAssignmentModel(IOptionAssignmentModel optionAssignmentModel)
        {
            OptionAssignmentModel = optionAssignmentModel;
        }

        /// <summary>
        /// Sets the option exercise model
        /// </summary>
        /// <param name="pyObject">The option exercise model to use</param>
        public void SetOptionExerciseModel(PyObject pyObject)
        {
            if (pyObject.TryConvert<IOptionExerciseModel>(out var optionExerciseModel))
            {
                // pure C# implementation
                SetOptionExerciseModel(optionExerciseModel);
            }
            else if (Extensions.TryConvert<IOptionExerciseModel>(pyObject, out _, allowPythonDerivative: true))
            {
                SetOptionExerciseModel(new OptionExerciseModelPythonWrapper(pyObject));
            }
            else
            {
                using (Py.GIL())
                {
                    throw new ArgumentException($"SetOptionExerciseModel: {pyObject.Repr()} is not a valid argument.");
                }
            }
        }

        /// <summary>
        /// Sets the option exercise model
        /// </summary>
        /// <param name="optionExerciseModel">The option exercise model to use</param>
        public void SetOptionExerciseModel(IOptionExerciseModel optionExerciseModel)
        {
            OptionExerciseModel = optionExerciseModel;
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
            SetFilterImp(universe => universe.Strikes(minStrike, maxStrike));
        }

        /// <summary>
        /// Sets the <see cref="ContractFilter"/> to a new instance of the filter
        /// using the specified min and max strike and expiration range values
        /// </summary>
        /// <param name="minExpiry">The minimum time until expiry to include, for example, TimeSpan.FromDays(10)
        /// would exclude contracts expiring in less than 10 days</param>
        /// <param name="maxExpiry">The maximum time until expiry to include, for example, TimeSpan.FromDays(10)
        /// would exclude contracts expiring in more than 10 days</param>
        public void SetFilter(TimeSpan minExpiry, TimeSpan maxExpiry)
        {
            SetFilterImp(universe => universe.Expiration(minExpiry, maxExpiry));
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
        /// <param name="maxExpiry">The maximum time until expiry to include, for example, TimeSpan.FromDays(10)
        /// would exclude contracts expiring in more than 10 days</param>
        public void SetFilter(int minStrike, int maxStrike, TimeSpan minExpiry, TimeSpan maxExpiry)
        {
            SetFilterImp(universe => universe
                .Strikes(minStrike, maxStrike)
                .Expiration(minExpiry, maxExpiry));
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
        /// <param name="minExpiryDays">The minimum time, expressed in days, until expiry to include, for example, 10
        /// would exclude contracts expiring in less than 10 days</param>
        /// <param name="maxExpiryDays">The maximum time, expressed in days, until expiry to include, for example, 10
        /// would exclude contracts expiring in more than 10 days</param>
        public void SetFilter(int minStrike, int maxStrike, int minExpiryDays, int maxExpiryDays)
        {
            SetFilterImp(universe => universe
                .Strikes(minStrike, maxStrike)
                .Expiration(minExpiryDays, maxExpiryDays));
        }

        /// <summary>
        /// Sets the <see cref="ContractFilter"/> to a new universe selection function
        /// </summary>
        /// <param name="universeFunc">new universe selection function</param>
        public void SetFilter(Func<OptionFilterUniverse, OptionFilterUniverse> universeFunc)
        {
            ContractFilter = new FuncSecurityDerivativeFilter<OptionUniverse>(universe =>
            {
                var optionUniverse = universe as OptionFilterUniverse;
                var result = universeFunc(optionUniverse);
                return result.ApplyTypesFilter();
            });
            ContractFilter.Asynchronous = false;
        }

        /// <summary>
        /// Sets the <see cref="ContractFilter"/> to a new universe selection function
        /// </summary>
        /// <param name="universeFunc">new universe selection function</param>
        public void SetFilter(PyObject universeFunc)
        {
            ContractFilter = new FuncSecurityDerivativeFilter<OptionUniverse>(universe =>
            {
                var optionUniverse = universe as OptionFilterUniverse;
                using (Py.GIL())
                {
                    PyObject result = (universeFunc as dynamic)(optionUniverse);

                    //Try to convert it to the possible outcomes and process it
                    //Must try filter first, if it is a filter and you try and convert it to
                    //list, TryConvert() with catch an exception. Later Python algo will break on
                    //this exception because we are using Py.GIL() and it will see the error set
                    OptionFilterUniverse filter;
                    List<Symbol> list;

                    if ((result).TryConvert(out filter))
                    {
                        optionUniverse = filter;
                    }
                    else if ((result).TryConvert(out list))
                    {
                        optionUniverse = optionUniverse.WhereContains(list);
                    }
                    else
                    {
                        throw new ArgumentException($"QCAlgorithm.SetFilter: result type {result.GetPythonType()} from " +
                            $"filter function is not a valid argument, please return either a OptionFilterUniverse or a list of symbols");
                    }
                }
                return optionUniverse.ApplyTypesFilter();
            });
            ContractFilter.Asynchronous = false;
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

        private void SetFilterImp(Func<OptionFilterUniverse, OptionFilterUniverse> universeFunc)
        {
            ContractFilter = new FuncSecurityDerivativeFilter<OptionUniverse>(universe =>
            {
                var optionUniverse = universe as OptionFilterUniverse;
                var result = universeFunc(optionUniverse);
                return result.ApplyTypesFilter();
            });
        }

        /// <summary>
        /// Updates the symbol properties of this security
        /// </summary>
        internal override void UpdateSymbolProperties(SymbolProperties symbolProperties)
        {
            if (symbolProperties != null)
            {
                SymbolProperties = new OptionSymbolProperties(symbolProperties);
            }
        }
    }
}
