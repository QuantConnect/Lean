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
using QuantConnect.Orders.OptionExercise;

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
        public Option(SecurityExchangeHours exchangeHours, SubscriptionDataConfig config, Cash quoteCurrency, OptionSymbolProperties symbolProperties)
            : base(config,
                quoteCurrency,
                symbolProperties,
                new OptionExchange(exchangeHours),
                new OptionCache(),
                new OptionPortfolioModel(),
                new ImmediateFillModel(),
                new InteractiveBrokersFeeModel(),
                new SpreadSlippageModel(),
                new ImmediateSettlementModel(),
                Securities.VolatilityModel.Null,
                new OptionMarginModel(),
                new OptionDataFilter(),
                new AdjustedPriceVariationModel()
                )
        {
            StrikePrice = Symbol.ID.StrikePrice;
            ExerciseSettlement = SettlementType.PhysicalDelivery;
            OptionExerciseModel = new DefaultExerciseModel();
            PriceModel = new CurrentPriceOptionPriceModel();
            ContractFilter = new StrikeExpiryOptionFilter(-5, 5, TimeSpan.Zero, TimeSpan.FromDays(35));
            Holdings = new OptionHolding(this);
            _symbolProperties = symbolProperties;
        }


        // save off a strongly typed version of symbol properties
        private readonly OptionSymbolProperties _symbolProperties;

        /// <summary>
        /// Gets the strike price
        /// </summary>
        public decimal StrikePrice
        {
            get; set; 
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
        public int GetExerciseQuantity(int quantity)
        {
            return (int)(quantity * ContractUnitOfTrade / ContractMultiplier);
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
