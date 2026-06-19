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
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;
using System.Collections.Generic;

namespace QuantConnect.Brokerages
{
    /// <summary>
    /// Represents a brokerage model specific to Public.com.
    /// </summary>
    public class PublicBrokerageModel : DefaultBrokerageModel
    {
        /// <summary>
        /// The security types supported by Public.com.
        /// </summary>
        private readonly HashSet<SecurityType> _supportSecurityTypes = new(
            new[]
            {
                SecurityType.Equity,
                SecurityType.Option,
                SecurityType.IndexOption,
                SecurityType.Crypto
            });

        /// <summary>
        /// The order types supported by the <see cref="CanSubmitOrder"/> operation in Public.com.
        /// Multi-leg combos are limit only.
        /// </summary>
        private readonly HashSet<OrderType> _supportOrderTypes = new(
            new[]
            {
                OrderType.Market,
                OrderType.Limit,
                OrderType.StopMarket,
                OrderType.StopLimit,
                OrderType.ComboLimit
            });

        /// <summary>
        /// Constructor for Public.com brokerage model
        /// </summary>
        /// <param name="accountType">Cash or Margin</param>
        public PublicBrokerageModel(AccountType accountType = AccountType.Margin)
            : base(accountType)
        {
        }

        /// <summary>
        /// Provides the Public.com fee model
        /// </summary>
        /// <param name="security">Security</param>
        /// <returns>Public.com fee model</returns>
        public override IFeeModel GetFeeModel(Security security)
        {
            return new PublicFeeModel();
        }

        /// <summary>
        /// Returns true if the brokerage could accept this order. This takes into account order type, security type.
        /// </summary>
        /// <param name="security">The security of the order</param>
        /// <param name="order">The order to be processed</param>
        /// <param name="message">If this function returns false, a brokerage message detailing why the order may not be submitted</param>
        /// <returns>True if the brokerage could process the order, false otherwise</returns>
        public override bool CanSubmitOrder(Security security, Order order, out BrokerageMessageEvent message)
        {
            message = default;

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

            // Public.com only accepts Limit orders in the extended (outside regular trading hours) session.
            if (order.Properties is PublicOrderProperties { OutsideRegularTradingHours: true } && order.Type != OrderType.Limit)
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                    Messages.PublicBrokerageModel.ExtendedMarketOrderMustBeLimit(order));
                return false;
            }

            if (order.Properties is PublicOrderProperties publicOrderProperties)
            {
                // A cash account has no margin buying power, so margin is always off there.
                // On a margin account, keep an explicit choice and otherwise use margin by default.
                publicOrderProperties.UseMargin = AccountType != AccountType.Cash && (publicOrderProperties.UseMargin ?? true);
            }

            // Public.com handles crossing a zero position natively, so the order is not split or rejected here.
            return base.CanSubmitOrder(security, order, out message);
        }

        /// <summary>
        /// Returns true if the brokerage would allow updating the order as specified by the request.
        /// Public.com has no multi-leg replace endpoint, so combo orders cannot be updated.
        /// </summary>
        /// <param name="security">The security of the order</param>
        /// <param name="order">The order to be updated</param>
        /// <param name="request">The requested update to be made to the order</param>
        /// <param name="message">If this function returns false, a brokerage message detailing why the order may not be updated</param>
        /// <returns>True if the brokerage would allow updating the order, false otherwise</returns>
        public override bool CanUpdateOrder(Security security, Order order, UpdateOrderRequest request, out BrokerageMessageEvent message)
        {
            if (order.GroupOrderManager != null)
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                    "Public.com does not support updating combo (multi-leg) orders.");
                return false;
            }

            message = null;
            return true;
        }
    }
}
