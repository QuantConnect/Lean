/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014-2023 QuantConnect Corporation.
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
using System.Linq;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Benchmarks;
using QuantConnect.Orders.Fees;
using System.Collections.Generic;
using QuantConnect.Util;

namespace QuantConnect.Brokerages
{
    /// <summary>
    /// Represents a brokerage model for interacting with the Coinbase exchange.
    /// This class extends the default brokerage model.
    /// </summary>
    public class CoinbaseBrokerageModel : DefaultBrokerageModel
    {
        /// <summary>
        /// Marks the end of stop market order support on Coinbase Pro.
        /// For backtesting purposes, this field '_stopMarketOrderSupportEndDate' specifies the date when the
        /// market structure update was applied, affecting the handling of historical data or simulations
        /// involving stop market orders. Details: https://blog.coinbase.com/coinbase-pro-market-structure-update-fbd9d49f43d7
        /// </summary>
        private readonly DateTime _stopMarketOrderSupportEndDate = new DateTime(2019, 3, 23, 1, 0, 0);

        /// <summary>
        /// Notifies users that order updates are not supported by the current brokerage model.
        /// </summary>
        private readonly BrokerageMessageEvent _message = new(BrokerageMessageType.Warning, 0, Messages.DefaultBrokerageModel.OrderUpdateNotSupported);

        /// <summary>
        /// Represents a set of order types supported by the current brokerage model.
        /// </summary>
        private readonly HashSet<OrderType> _supportedOrderTypes = new()
        {
            OrderType.Limit,
            OrderType.Market,
            OrderType.StopLimit,
            OrderType.StopMarket
        };

        /// <summary>
        /// Gets a map of the default markets to be used for each security type
        /// </summary>
        public override IReadOnlyDictionary<SecurityType, string> DefaultMarkets => GetDefaultMarkets(Market.Coinbase);

        /// <summary>
        /// Initializes a new instance of the <see cref="CoinbaseBrokerageModel"/> class
        /// </summary>
        /// <param name="accountType">The type of account to be modelled, defaults to <see cref="AccountType.Cash"/></param>
        public CoinbaseBrokerageModel(AccountType accountType = AccountType.Cash)
            : base(accountType)
        {
            if (accountType == AccountType.Margin)
            {
                throw new ArgumentException(Messages.CoinbaseBrokerageModel.UnsupportedAccountType, nameof(accountType));
            }
        }

        /// <summary>
        /// Coinbase global leverage rule
        /// </summary>
        /// <param name="security"></param>
        /// <returns></returns>
        public override decimal GetLeverage(Security security)
        {
            // margin trading is not currently supported by Coinbase
            return 1m;
        }

        /// <summary>
        /// Get the benchmark for this model
        /// </summary>
        /// <param name="securities">SecurityService to create the security with if needed</param>
        /// <returns>The benchmark for this brokerage</returns>
        public override IBenchmark GetBenchmark(SecurityManager securities)
        {
            var symbol = Symbol.Create("BTCUSD", SecurityType.Crypto, Market.Coinbase);
            return SecurityBenchmark.CreateInstance(securities, symbol);
        }

        /// <summary>
        /// Provides Coinbase fee model
        /// </summary>
        /// <param name="security"></param>
        /// <returns></returns>
        public override IFeeModel GetFeeModel(Security security)
        {
            return new CoinbaseFeeModel();
        }

        /// <summary>
        /// Determines whether the brokerage supports updating an existing order for the specified security.
        /// </summary>
        /// <param name="security">The security of the order</param>
        /// <param name="order">The order to be updated</param>
        /// <param name="request">The requested update to be made to the order</param>
        /// <param name="message">If this function returns false, a brokerage message detailing why the order may not be updated</param>
        /// <returns><c>true</c> if the brokerage supports updating orders; otherwise, <c>false</c>.</returns>
        /// <remarks>Coinbase: Only limit order types, with time in force type of good-till-cancelled can be edited.</remarks>
        public override bool CanUpdateOrder(Security security, Order order, UpdateOrderRequest request, out BrokerageMessageEvent message)
        {
            if (order == null || security == null || request == null)
            {
                var parameter = order == null ? nameof(order) : nameof(security);
                throw new ArgumentNullException(parameter, $"{parameter} parameter cannot be null. Please provide a valid {parameter} for submission.");
            }

            if (order.Type != OrderType.Limit)
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported", 
                    $"Order with type {order.Type} can't be modified, only LIMIT.");
                return false;
            }

            if (order.TimeInForce != TimeInForce.GoodTilCanceled)
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported", 
                    $"Order's parameter 'TimeInForce' is not instance of Good Til Cancelled class.");
                return false;
            }

            if (order.Status is not (OrderStatus.New or OrderStatus.PartiallyFilled or OrderStatus.Submitted or OrderStatus.UpdateSubmitted))
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                    $"Order with status {order.Status} can't be modified");
                return false;
            }

            if (request.Quantity.HasValue && !IsOrderSizeLargeEnough(security, Math.Abs(request.Quantity.Value)))
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                    Messages.DefaultBrokerageModel.InvalidOrderQuantity(security, request.Quantity.Value));
                return false;
            }

            message = null;
            return true;
        }

        /// <summary>
        /// Evaluates whether exchange will accept order. Will reject order update
        /// </summary>
        /// <param name="security">The security of the order</param>
        /// <param name="order">The order to be processed</param>
        /// <param name="message">If this function returns false, a brokerage message detailing why the order may not be submitted</param>
        /// <returns>True if the brokerage could process the order, false otherwise</returns>
        public override bool CanSubmitOrder(Security security, Order order, out BrokerageMessageEvent message)
        {
            if(order == null || security == null)
            {
                var parameter = order == null ? nameof(order) : nameof(security);
                throw new ArgumentNullException(parameter, $"{parameter} parameter cannot be null. Please provide a valid {parameter} for submission.");
            }
            
            if (order.BrokerId != null && order.BrokerId.Any())
            {
                message = _message;
                return false;
            }

            if (!IsValidOrderSize(security, order.Quantity, out message))
            {
                return false;
            }

            if (security.Type != SecurityType.Crypto)
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported", 
                    Messages.DefaultBrokerageModel.UnsupportedSecurityType(this, security));
                return false;
            }

            if (!_supportedOrderTypes.Contains(order.Type))
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                    Messages.DefaultBrokerageModel.UnsupportedOrderType(this, order, _supportedOrderTypes));
                return false;
            }
            
            if (order.Type == OrderType.StopMarket && order.Time >= _stopMarketOrderSupportEndDate)
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                    Messages.CoinbaseBrokerageModel.StopMarketOrdersNoLongerSupported(_stopMarketOrderSupportEndDate));

                return false;
            }

            if (!IsOrderSizeLargeEnough(security, Math.Abs(order.Quantity)))
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                    Messages.DefaultBrokerageModel.InvalidOrderQuantity(security, order.Quantity));
                return false;
            }

            return base.CanSubmitOrder(security, order, out message);
        }

        /// <summary>
        /// Gets a new buying power model for the security, returning the default model with the security's configured leverage.
        /// For cash accounts, leverage = 1 is used.
        /// </summary>
        /// <param name="security">The security to get a buying power model for</param>
        /// <returns>The buying power model for this brokerage/security</returns>
        public override IBuyingPowerModel GetBuyingPowerModel(Security security)
        {
            // margin trading is not currently supported by Coinbase
            return new CashBuyingPowerModel();
        }

        /// <summary>
        /// Returns true if the order size is large enough for the given security.
        /// </summary>
        /// <param name="security">The security of the order</param>
        /// <param name="orderQuantity">The order quantity</param>
        /// <returns>True if the order size is large enough, false otherwise</returns>
        protected virtual bool IsOrderSizeLargeEnough(Security security, decimal orderQuantity)
        {
#pragma warning disable CA1062
            return !security!.SymbolProperties.MinimumOrderSize.HasValue ||
                   orderQuantity >= security.SymbolProperties.MinimumOrderSize;
#pragma warning restore CA1062
        }

        /// <summary>
        /// Gets the default markets for different security types, with an option to override the market name for Crypto securities.
        /// </summary>
        /// <param name="marketName">The default market name for Crypto securities.</param>
        /// <returns>
        /// A read-only dictionary where the keys are <see cref="SecurityType"/> and the values are market names.
        /// </returns>
        protected static IReadOnlyDictionary<SecurityType, string> GetDefaultMarkets(string marketName)
        {
            var map = DefaultMarketMap.ToDictionary();
            map[SecurityType.Crypto] = marketName;
            return map.ToReadOnlyDictionary();
        }
    }
}
