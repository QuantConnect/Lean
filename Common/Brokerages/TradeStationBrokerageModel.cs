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
 *
*/

using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Orders.Fees;
using System.Collections.Generic;

namespace QuantConnect.Brokerages
{
    /// <summary>
    /// Represents a brokerage model specific to TradeStation.
    /// </summary>
    public class TradeStationBrokerageModel : DefaultBrokerageModel
    {
        /// <summary>
        /// HashSet containing the security types supported by TradeStation.
        /// </summary>
        private readonly HashSet<SecurityType> _supportSecurityTypes = new(
            new[]
            {
                SecurityType.Equity,
                SecurityType.Option,
                SecurityType.Future,
                SecurityType.IndexOption
            });

        /// <summary>
        /// HashSet containing the order types supported by the <see cref="CanSubmitOrder"/> operation in TradeStation.
        /// </summary>
        private readonly HashSet<OrderType> _supportOrderTypes = new(
            new[]
            {
                OrderType.Market,
                OrderType.Limit,
                OrderType.StopMarket,
                OrderType.StopLimit,
                OrderType.ComboMarket,
                OrderType.ComboLimit,
                OrderType.MarketOnOpen,
                OrderType.MarketOnClose,
                OrderType.TrailingStop
            });

        /// <summary>
        /// Constructor for TradeStation brokerage model
        /// </summary>
        /// <param name="accountType">Cash or Margin</param>
        public TradeStationBrokerageModel(AccountType accountType = AccountType.Margin)
            : base(accountType)
        {
        }

        /// <summary>
        /// Provides TradeStation fee model
        /// </summary>
        /// <param name="security">Security</param>
        /// <returns>TradeStation fee model</returns>
        public override IFeeModel GetFeeModel(Security security)
        {
            return new TradeStationFeeModel();
        }

        /// <summary>
        /// Returns true if the brokerage could accept this order. This takes into account
        /// order type, security type, and order size limits.
        /// </summary>
        /// <remarks>
        /// For example, a brokerage may have no connectivity at certain times, or an order rate/size limit
        /// </remarks>
        /// <param name="security">The security of the order</param>
        /// <param name="order">The order to be processed</param>
        /// <param name="message">If this function returns false, a brokerage message detailing why the order may not be submitted</param>
        /// <returns>True if the brokerage could process the order, false otherwise</returns>
        public override bool CanSubmitOrder(Security security, Order order, out BrokerageMessageEvent message)
        {
            message = default;

            var supportsOutsideTradingHours = (order.Properties as TradeStationOrderProperties)?.OutsideRegularTradingHours ?? false;
            if (supportsOutsideTradingHours && (order.Type != OrderType.Limit || order.SecurityType != SecurityType.Equity))
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupportedOutsideRegularMarketHours",
                    "To place an order outside regular trading hours, please use a limit order and ensure the security is an equity.");
                return false;
            }

            if (!_supportSecurityTypes.Contains(security.Type))
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                    Messages.DefaultBrokerageModel.UnsupportedSecurityType(this, security));

                return false;
            }

            if (!_supportOrderTypes.Contains(order.Type))
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported", Messages.DefaultBrokerageModel.UnsupportedOrderType(this, order, _supportOrderTypes));
                return false;
            }

            if (BrokerageExtensions.OrderCrossesZero(security.Holdings.Quantity, order.Quantity) && IsComboOrderType(order.Type))
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported", Messages.DefaultBrokerageModel.UnsupportedCrossZeroByOrderType(this, order.Type));
                return false;
            }

            return base.CanSubmitOrder(security, order, out message);
        }

        /// <summary>
        /// TradeStation support Update Order
        /// </summary>
        /// <param name="security">Security</param>
        /// <param name="order">Order that should be updated</param>
        /// <param name="request">Update request</param>
        /// <param name="message">Outgoing message</param>
        /// <returns>True if the brokerage would allow updating the order, false otherwise</returns>
        public override bool CanUpdateOrder(Security security, Order order, UpdateOrderRequest request, out BrokerageMessageEvent message)
        {
            message = null;

            if (BrokerageExtensions.OrderCrossesZero(security.Holdings.Quantity, order.Quantity)
                && request.Quantity != null && request.Quantity != order.Quantity)
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "UpdateRejected",
                    Messages.DefaultBrokerageModel.UnsupportedCrossZeroOrderUpdate(this));
                return false;
            }

            if (IsComboOrderType(order.Type) && request.Quantity != null && request.Quantity != order.Quantity)
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported", Messages.DefaultBrokerageModel.UnsupportedUpdateQuantityOrder(this, order.Type));
                return false;
            }

            return true;
        }

        /// <summary>
        /// Determines if the provided order type is a combo order.
        /// </summary>
        /// <param name="orderType">The order type to check.</param>
        /// <returns>True if the order type is a combo order; otherwise, false.</returns>
        private static bool IsComboOrderType(OrderType orderType)
        {
            return orderType == OrderType.ComboMarket || orderType == OrderType.ComboLimit;
        }
    }
}
