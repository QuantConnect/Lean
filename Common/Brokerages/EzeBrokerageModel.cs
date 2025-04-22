/*
* QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
* Lean Algorithmic Trading Engine v2.0. Copyright 2023 QuantConnect Corporation.
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

using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;
using System;
using System.Collections.Generic;

namespace QuantConnect.Brokerages
{
    /// <summary>
    /// Provides Eze specific properties
    /// </summary>
    public class EzeBrokerageModel : DefaultBrokerageModel
    {
        /// <summary>
        /// Array's Eze supports security types
        /// </summary>
        private readonly HashSet<SecurityType> _supportSecurityTypes = new(
            new[]
            {
                SecurityType.Equity,
                SecurityType.Option,
                SecurityType.Future,
                SecurityType.FutureOption,
                SecurityType.Index,
                SecurityType.IndexOption
            });

        /// <summary>
        /// Array's Eze supports order types
        /// </summary>
        private readonly HashSet<OrderType> _supportOrderTypes = new(
            new[]
            {
                OrderType.Market,
                OrderType.Limit,
                OrderType.StopMarket,
                OrderType.StopLimit,
                OrderType.MarketOnOpen,
                OrderType.MarketOnClose,
            });

        /// <summary>
        /// Constructor for Eze brokerage model
        /// </summary>
        /// <param name="accountType">Cash or Margin</param>
        public EzeBrokerageModel(AccountType accountType = AccountType.Margin) : base(accountType)
        {
            if (accountType == AccountType.Cash)
            {
                throw new NotSupportedException($"Eze brokerage can only be used with a {AccountType.Margin} account type");
            }
        }

        /// <summary>
        /// Provides Eze fee model
        /// </summary>
        /// <param name="security">Security</param>
        /// <returns>Eze Fee model</returns>
        public override IFeeModel GetFeeModel(Security security)
        {
            return new EzeFeeModel();
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
        /// <param name="message">>If this function returns false, a brokerage message detailing why the order may not be submitted</param>
        /// <returns>True if the brokerage could process the order, false otherwise</returns>
        public override bool CanSubmitOrder(Security security, Order order, out BrokerageMessageEvent message)
        {
            if (!_supportSecurityTypes.Contains(security.Type))
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                    Messages.DefaultBrokerageModel.UnsupportedSecurityType(this, security));

                return false;
            }

            if (!_supportOrderTypes.Contains(order.Type))
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                    Messages.DefaultBrokerageModel.UnsupportedOrderType(this, order, _supportOrderTypes));

                return false;
            }

            if (order.AbsoluteQuantity % 1 != 0)
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                    $"Order Quantity must be Integer, but provided {order.AbsoluteQuantity}.");

                return false;
            }

            return base.CanSubmitOrder(security, order, out message);
        }

        /// <summary>
        /// Returns true if the brokerage could accept this order update. This takes into account
        /// order type, security type, and order size limits.
        /// </summary>
        /// <param name="security">The security of the order</param>
        /// <param name="order">The order to be updated</param>
        /// <param name="request">The requested update to be made to the order</param>
        /// <param name="message">If this function returns false, a brokerage message detailing why the order may not be updated</param>
        /// <returns>True if the brokerage could update the order, false otherwise</returns>
        /// <remarks>
        /// The Eze supports update:
        /// - quantity <see cref="Order.Quantity"/>
        /// - LimitPrice <see cref="LimitOrder.LimitPrice"/>
        /// - StopPrice <see cref="StopLimitOrder.StopPrice"/>
        /// - OrderType <seealso cref="OrderType"/>
        /// - Time In Force <see cref="Order.TimeInForce"/>
        /// </remarks>
        public override bool CanUpdateOrder(Security security, Order order, UpdateOrderRequest request, out BrokerageMessageEvent message)
        {
            message = null;
            return true;
        }
    }
}
