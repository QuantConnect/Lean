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
    /// Represents a brokerage model specific to eToro.
    /// </summary>
    public class EtoroBrokerageModel : DefaultBrokerageModel
    {
        /// <summary>
        /// The security types supported by eToro: US stocks and ETFs, and crypto.
        /// </summary>
        private readonly HashSet<SecurityType> _supportSecurityTypes = new(
            new[]
            {
                SecurityType.Equity,
                SecurityType.Crypto
            });

        /// <summary>
        /// The order types supported by the <see cref="CanSubmitOrder"/> operation in eToro.
        /// </summary>
        private readonly HashSet<OrderType> _supportOrderTypes = new(
            new[]
            {
                OrderType.Market
            });

        /// <summary>
        /// The order types that cannot cross zero holdings.
        /// eToro closes a position by units, so a sell beyond the long holdings is refused.
        /// </summary>
        private static readonly IReadOnlySet<OrderType> NotSupportedCrossZeroOrderTypes = new HashSet<OrderType>
        {
            OrderType.Market
        };

        /// <summary>
        /// Constructor for eToro brokerage model
        /// </summary>
        /// <param name="accountType">Cash or Margin</param>
        public EtoroBrokerageModel(AccountType accountType = AccountType.Margin)
            : base(accountType)
        {
        }

        /// <summary>
        /// Provides the eToro fee model
        /// </summary>
        /// <param name="security">Security</param>
        /// <returns>eToro fee model</returns>
        public override IFeeModel GetFeeModel(Security security)
        {
            return new EtoroFeeModel();
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
            if (!this.ValidateContingentOrdersNotSupported(order, out message))
            {
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
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                    Messages.DefaultBrokerageModel.UnsupportedOrderType(this, order, _supportOrderTypes));
                return false;
            }

            if (BrokerageExtensions.GetOrderPosition(order.Direction, security.Holdings.Quantity) == OrderPosition.SellToOpen
                && order.Properties is not EtoroOrderProperties { StopLossRate: > 0 })
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                    Messages.EtoroBrokerageModel.ShortSaleRequiresStopLossRate(order));
                return false;
            }

            if (!BrokerageExtensions.ValidateCrossZeroOrder(this, security, order, out message, NotSupportedCrossZeroOrderTypes))
            {
                return false;
            }

            return base.CanSubmitOrder(security, order, out message);
        }

        /// <summary>
        /// Returns false: eToro has no order update, an order is cancelled and placed again instead.
        /// </summary>
        /// <param name="security">The security of the order</param>
        /// <param name="order">The order to be updated</param>
        /// <param name="request">The requested update to be made to the order</param>
        /// <param name="message">A brokerage message detailing why the order may not be updated</param>
        /// <returns>Always false</returns>
        public override bool CanUpdateOrder(Security security, Order order, UpdateOrderRequest request, out BrokerageMessageEvent message)
        {
            message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                Messages.DefaultBrokerageModel.OrderUpdateNotSupported);
            return false;
        }
    }
}
