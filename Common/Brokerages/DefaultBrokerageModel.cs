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
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Benchmarks;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Data.Shortable;
using QuantConnect.Interfaces;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Orders.Fills;
using QuantConnect.Orders.Slippage;
using QuantConnect.Securities;
using QuantConnect.Securities.Equity;
using QuantConnect.Securities.Future;
using QuantConnect.Securities.Option;
using QuantConnect.Util;

namespace QuantConnect.Brokerages
{
    /// <summary>
    /// Provides a default implementation of <see cref="IBrokerageModel"/> that allows all orders and uses
    /// the default transaction models
    /// </summary>
    public class DefaultBrokerageModel : IBrokerageModel
    {
        /// <summary>
        /// The default markets for the backtesting brokerage
        /// </summary>
        public static readonly IReadOnlyDictionary<SecurityType, string> DefaultMarketMap = new Dictionary<SecurityType, string>
        {
            {SecurityType.Base, Market.USA},
            {SecurityType.Equity, Market.USA},
            {SecurityType.Option, Market.USA},
            {SecurityType.Future, Market.CME},
            {SecurityType.FutureOption, Market.CME},
            {SecurityType.Forex, Market.Oanda},
            {SecurityType.Cfd, Market.FXCM},
            {SecurityType.Crypto, Market.GDAX}
        }.ToReadOnlyDictionary();

        /// <summary>
        /// Determines whether the asset you want to short is shortable.
        /// The default is set to <see cref="NullShortableProvider"/>,
        /// which allows for infinite shorting of any asset. You can limit the
        /// quantity you can short for an asset class by setting this variable to
        /// your own implementation of <see cref="IShortableProvider"/>.
        /// </summary>
        protected IShortableProvider ShortableProvider { get; set; }

        /// <summary>
        /// Gets or sets the account type used by this model
        /// </summary>
        public virtual AccountType AccountType
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the brokerages model percentage factor used to determine the required unused buying power for the account.
        /// From 1 to 0. Example: 0 means no unused buying power is required. 0.5 means 50% of the buying power should be left unused.
        /// </summary>
        public virtual decimal RequiredFreeBuyingPowerPercent => 0m;

        /// <summary>
        /// Gets a map of the default markets to be used for each security type
        /// </summary>
        public virtual IReadOnlyDictionary<SecurityType, string> DefaultMarkets
        {
            get { return DefaultMarketMap; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultBrokerageModel"/> class
        /// </summary>
        /// <param name="accountType">The type of account to be modelled, defaults to
        /// <see cref="QuantConnect.AccountType.Margin"/></param>
        public DefaultBrokerageModel(AccountType accountType = AccountType.Margin)
        {
            AccountType = accountType;

            // Shortable provider, responsible for loading the data that indicates how much
            // quantity we can short for a given asset. The NullShortableProvider default will
            // allow for infinite quantities of any asset to be shorted.
            ShortableProvider = new NullShortableProvider();
        }

        /// <summary>
        /// Returns true if the brokerage could accept this order. This takes into account
        /// order type, security type, and order size limits.
        /// </summary>
        /// <remarks>
        /// For example, a brokerage may have no connectivity at certain times, or an order rate/size limit
        /// </remarks>
        /// <param name="security">The security being ordered</param>
        /// <param name="order">The order to be processed</param>
        /// <param name="message">If this function returns false, a brokerage message detailing why the order may not be submitted</param>
        /// <returns>True if the brokerage could process the order, false otherwise</returns>
        public virtual bool CanSubmitOrder(Security security, Order order, out BrokerageMessageEvent message)
        {
            message = null;
            return true;
        }

        /// <summary>
        /// Returns true if the brokerage would allow updating the order as specified by the request
        /// </summary>
        /// <param name="security">The security of the order</param>
        /// <param name="order">The order to be updated</param>
        /// <param name="request">The requested update to be made to the order</param>
        /// <param name="message">If this function returns false, a brokerage message detailing why the order may not be updated</param>
        /// <returns>True if the brokerage would allow updating the order, false otherwise</returns>
        public virtual bool CanUpdateOrder(Security security, Order order, UpdateOrderRequest request, out BrokerageMessageEvent message)
        {
            message = null;
            return true;
        }

        /// <summary>
        /// Returns true if the brokerage would be able to execute this order at this time assuming
        /// market prices are sufficient for the fill to take place. This is used to emulate the
        /// brokerage fills in backtesting and paper trading. For example some brokerages may not perform
        /// executions during extended market hours. This is not intended to be checking whether or not
        /// the exchange is open, that is handled in the Security.Exchange property.
        /// </summary>
        /// <param name="security">The security being traded</param>
        /// <param name="order">The order to test for execution</param>
        /// <returns>True if the brokerage would be able to perform the execution, false otherwise</returns>
        public virtual bool CanExecuteOrder(Security security, Order order)
        {
            return true;
        }

        /// <summary>
        /// Applies the split to the specified order ticket
        /// </summary>
        /// <remarks>
        /// This default implementation will update the orders to maintain a similar market value
        /// </remarks>
        /// <param name="tickets">The open tickets matching the split event</param>
        /// <param name="split">The split event data</param>
        public virtual void ApplySplit(List<OrderTicket> tickets, Split split)
        {
            // by default we'll just update the orders to have the same notional value
            var splitFactor = split.SplitFactor;
            tickets.ForEach(ticket => ticket.Update(new UpdateOrderFields
            {
                Quantity = (int?) (ticket.Quantity/splitFactor),
                LimitPrice = ticket.OrderType.IsLimitOrder() ? ticket.Get(OrderField.LimitPrice)*splitFactor : (decimal?) null,
                StopPrice = ticket.OrderType.IsStopOrder() ? ticket.Get(OrderField.StopPrice)*splitFactor : (decimal?) null
            }));
        }

        /// <summary>
        /// Gets the brokerage's leverage for the specified security
        /// </summary>
        /// <param name="security">The security's whose leverage we seek</param>
        /// <returns>The leverage for the specified security</returns>
        public virtual decimal GetLeverage(Security security)
        {
            if (AccountType == AccountType.Cash)
            {
                return 1m;
            }

            switch (security.Type)
            {
                case SecurityType.Equity:
                    return 2m;

                case SecurityType.Forex:
                case SecurityType.Cfd:
                    return 50m;

                case SecurityType.Crypto:
                    return 1m;

                case SecurityType.Base:
                case SecurityType.Commodity:
                case SecurityType.Option:
                case SecurityType.FutureOption:
                case SecurityType.Future:
                default:
                    return 1m;
            }
        }

        /// <summary>
        /// Get the benchmark for this model
        /// </summary>
        /// <param name="securities">SecurityService to create the security with if needed</param>
        /// <returns>The benchmark for this brokerage</returns>
        public virtual IBenchmark GetBenchmark(SecurityManager securities)
        {
            var symbol = Symbol.Create("SPY", SecurityType.Equity, Market.USA);
            return SecurityBenchmark.CreateInstance(securities, symbol);
        }

        /// <summary>
        /// Gets a new fill model that represents this brokerage's fill behavior
        /// </summary>
        /// <param name="security">The security to get fill model for</param>
        /// <returns>The new fill model for this brokerage</returns>
        public virtual IFillModel GetFillModel(Security security)
        {
            switch (security.Type)
            {
                case SecurityType.Base:
                    break;
                case SecurityType.Equity:
                    return new EquityFillModel();
                case SecurityType.Option:
                    break;
                case SecurityType.FutureOption:
                    break;
                case SecurityType.Commodity:
                    break;
                case SecurityType.Forex:
                    break;
                case SecurityType.Future:
                    break;
                case SecurityType.Cfd:
                    break;
                case SecurityType.Crypto:
                    break;
                default:
                    throw new ArgumentOutOfRangeException($"{GetType().Name}.GetFillModel: Invalid security type {security.Type}");
            }

            return new ImmediateFillModel();
        }

        /// <summary>
        /// Gets a new fee model that represents this brokerage's fee structure
        /// </summary>
        /// <param name="security">The security to get a fee model for</param>
        /// <returns>The new fee model for this brokerage</returns>
        public virtual IFeeModel GetFeeModel(Security security)
        {
            switch (security.Type)
            {
                case SecurityType.Base:
                case SecurityType.Forex:
                case SecurityType.Cfd:
                case SecurityType.Crypto:
                    return new ConstantFeeModel(0m);

                case SecurityType.Equity:
                case SecurityType.Option:
                case SecurityType.Future:
                case SecurityType.FutureOption:
                    return new InteractiveBrokersFeeModel();

                case SecurityType.Commodity:
                default:
                    return new ConstantFeeModel(0m);
            }
        }

        /// <summary>
        /// Gets a new slippage model that represents this brokerage's fill slippage behavior
        /// </summary>
        /// <param name="security">The security to get a slippage model for</param>
        /// <returns>The new slippage model for this brokerage</returns>
        public virtual ISlippageModel GetSlippageModel(Security security)
        {
            switch (security.Type)
            {
                case SecurityType.Base:
                case SecurityType.Equity:
                    return new ConstantSlippageModel(0);

                case SecurityType.Forex:
                case SecurityType.Cfd:
                case SecurityType.Crypto:
                    return new ConstantSlippageModel(0);

                case SecurityType.Commodity:
                case SecurityType.Option:
                case SecurityType.FutureOption:
                case SecurityType.Future:
                default:
                    return new ConstantSlippageModel(0);
            }
        }

        /// <summary>
        /// Gets a new settlement model for the security
        /// </summary>
        /// <param name="security">The security to get a settlement model for</param>
        /// <returns>The settlement model for this brokerage</returns>
        public virtual ISettlementModel GetSettlementModel(Security security)
        {
            if (AccountType == AccountType.Cash)
            {
                switch (security.Type)
                {
                    case SecurityType.Equity:
                        return new DelayedSettlementModel(Equity.DefaultSettlementDays, Equity.DefaultSettlementTime);

                    case SecurityType.Option:
                        return new DelayedSettlementModel(Option.DefaultSettlementDays, Option.DefaultSettlementTime);
                }
            }

            return new ImmediateSettlementModel();
        }

        /// <summary>
        /// Gets a new settlement model for the security
        /// </summary>
        /// <param name="security">The security to get a settlement model for</param>
        /// <param name="accountType">The account type</param>
        /// <returns>The settlement model for this brokerage</returns>
        [Obsolete("Flagged deprecated and will remove December 1st 2018")]
        public ISettlementModel GetSettlementModel(Security security, AccountType accountType)
        {
            return GetSettlementModel(security);
        }

        /// <summary>
        /// Gets a new buying power model for the security, returning the default model with the security's configured leverage.
        /// For cash accounts, leverage = 1 is used.
        /// </summary>
        /// <param name="security">The security to get a buying power model for</param>
        /// <returns>The buying power model for this brokerage/security</returns>
        public virtual IBuyingPowerModel GetBuyingPowerModel(Security security)
        {
            var leverage = GetLeverage(security);
            IBuyingPowerModel model;

            switch (security.Type)
            {
                case SecurityType.Crypto:
                    model = new CashBuyingPowerModel();
                    break;
                case SecurityType.Forex:
                case SecurityType.Cfd:
                    model = new SecurityMarginModel(leverage, RequiredFreeBuyingPowerPercent);
                    break;
                case SecurityType.Option:
                    model = new OptionMarginModel(RequiredFreeBuyingPowerPercent);
                    break;
                case SecurityType.FutureOption:
                    model = new FuturesOptionsMarginModel(RequiredFreeBuyingPowerPercent, (Option)security);
                    break;
                case SecurityType.Future:
                    model = new FutureMarginModel(RequiredFreeBuyingPowerPercent, security);
                    break;
                default:
                    model = new SecurityMarginModel(leverage, RequiredFreeBuyingPowerPercent);
                    break;
            }
            return model;
        }

        /// <summary>
        /// Gets the shortable provider
        /// </summary>
        /// <returns>Shortable provider</returns>
        public virtual IShortableProvider GetShortableProvider()
        {
            return ShortableProvider;
        }

        /// <summary>
        /// Gets a new buying power model for the security
        /// </summary>
        /// <param name="security">The security to get a buying power model for</param>
        /// <param name="accountType">The account type</param>
        /// <returns>The buying power model for this brokerage/security</returns>
        [Obsolete("Flagged deprecated and will remove December 1st 2018")]
        public IBuyingPowerModel GetBuyingPowerModel(Security security, AccountType accountType)
        {
            return GetBuyingPowerModel(security);
        }
    }
}
