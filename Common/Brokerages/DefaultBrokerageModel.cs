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
using QuantConnect.Benchmarks;
using QuantConnect.Data.Market;
using QuantConnect.Data.Shortable;
using QuantConnect.Interfaces;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Orders.Fills;
using QuantConnect.Orders.Slippage;
using QuantConnect.Securities;
using QuantConnect.Securities.CryptoFuture;
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
            {SecurityType.Cfd, Market.Oanda},
            {SecurityType.Crypto, Market.Coinbase},
            {SecurityType.CryptoFuture, Market.Binance},
            {SecurityType.Index, Market.USA},
            {SecurityType.IndexOption, Market.USA}
        }.ToReadOnlyDictionary();

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
            if ((security.Type == SecurityType.Future || security.Type == SecurityType.FutureOption) && order.Type == OrderType.MarketOnOpen)
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                    Messages.DefaultBrokerageModel.UnsupportedMarketOnOpenOrdersForFuturesAndFutureOptions);
                return false;
            }

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
                StopPrice = ticket.OrderType.IsStopOrder() ? ticket.Get(OrderField.StopPrice)*splitFactor : (decimal?) null,
                TriggerPrice = ticket.OrderType == OrderType.LimitIfTouched ? ticket.Get(OrderField.TriggerPrice) * splitFactor : (decimal?) null,
                TrailingAmount = ticket.OrderType == OrderType.TrailingStop && !ticket.Get<bool>(OrderField.TrailingAsPercentage) ? ticket.Get(OrderField.TrailingAmount) * splitFactor : (decimal?) null
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
                case SecurityType.CryptoFuture:
                    return 25m;

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
                case SecurityType.Index:
                case SecurityType.IndexOption:
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
                case SecurityType.Equity:
                    return new EquityFillModel();
                case SecurityType.FutureOption:
                    return new FutureOptionFillModel();
                case SecurityType.Future:
                    return new FutureFillModel();
                case SecurityType.Base:
                case SecurityType.Option:
                case SecurityType.Commodity:
                case SecurityType.Forex:
                case SecurityType.Cfd:
                case SecurityType.Crypto:
                case SecurityType.CryptoFuture:
                case SecurityType.Index:
                case SecurityType.IndexOption:
                    return new ImmediateFillModel();
                default:
                    throw new ArgumentOutOfRangeException(Messages.DefaultBrokerageModel.InvalidSecurityTypeToGetFillModel(this, security));
            }
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
                case SecurityType.CryptoFuture:
                case SecurityType.Index:
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
            return NullSlippageModel.Instance;
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

            if(security.Symbol.SecurityType == SecurityType.Future)
            {
                return new FutureSettlementModel();
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
            IBuyingPowerModel getCurrencyBuyingPowerModel() =>
                AccountType == AccountType.Cash
                    ? new CashBuyingPowerModel()
                    : new SecurityMarginModel(GetLeverage(security), RequiredFreeBuyingPowerPercent);

            return security?.Type switch
            {
                SecurityType.Crypto => getCurrencyBuyingPowerModel(),
                SecurityType.Forex => getCurrencyBuyingPowerModel(),
                SecurityType.CryptoFuture => new CryptoFutureMarginModel(GetLeverage(security)),
                SecurityType.Future => new FutureMarginModel(RequiredFreeBuyingPowerPercent, security),
                SecurityType.FutureOption => new FuturesOptionsMarginModel(RequiredFreeBuyingPowerPercent, (Option)security),
                SecurityType.IndexOption => new OptionMarginModel(RequiredFreeBuyingPowerPercent),
                SecurityType.Option => new OptionMarginModel(RequiredFreeBuyingPowerPercent),
                _ => new SecurityMarginModel(GetLeverage(security), RequiredFreeBuyingPowerPercent)
            };
        }

        /// <summary>
        /// Gets the shortable provider
        /// </summary>
        /// <returns>Shortable provider</returns>
        public virtual IShortableProvider GetShortableProvider(Security security)
        {
            // Shortable provider, responsible for loading the data that indicates how much
            // quantity we can short for a given asset. The NullShortableProvider default will
            // allow for infinite quantities of any asset to be shorted.
            return NullShortableProvider.Instance;
        }

        /// <summary>
        /// Gets a new margin interest rate model for the security
        /// </summary>
        /// <param name="security">The security to get a margin interest rate model for</param>
        /// <returns>The margin interest rate model for this brokerage</returns>
        public virtual IMarginInterestRateModel GetMarginInterestRateModel(Security security)
        {
            return MarginInterestRateModel.Null;
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

        /// <summary>
        /// Checks if the order quantity is valid, it means, the order size is bigger than the minimum size allowed
        /// </summary>
        /// <param name="security">The security of the order</param>
        /// <param name="orderQuantity">The quantity of the order to be processed</param>
        /// <param name="message">If this function returns false, a brokerage message detailing why the order may be invalid</param>
        /// <returns>True if the order quantity is bigger than the minimum allowed, false otherwise</returns>
        public static bool IsValidOrderSize(Security security, decimal orderQuantity, out BrokerageMessageEvent message)
        {
            var minimumOrderSize = security.SymbolProperties.MinimumOrderSize;
            if (minimumOrderSize != null && Math.Abs(orderQuantity) < minimumOrderSize)
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                    Messages.DefaultBrokerageModel.InvalidOrderQuantity(security, orderQuantity));

                return false;
            }

            message = null;
            return true;
        }
    }
}
