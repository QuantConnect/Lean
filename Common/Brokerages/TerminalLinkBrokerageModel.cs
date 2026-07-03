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

using System.Collections.Generic;
using QuantConnect.Orders;
using QuantConnect.Securities;

namespace QuantConnect.Brokerages
{
    /// <summary>
    /// Provides Bloomberg EMSX (TerminalLink) specific properties.
    /// </summary>
    public class TerminalLinkBrokerageModel : DefaultBrokerageModel
    {
        private readonly HashSet<SecurityType> _supportedSecurityTypes = new()
        {
            SecurityType.Equity,
            SecurityType.Option,
            SecurityType.Future,
        };

        private readonly HashSet<OrderType> _supportedOrderTypes = new()
        {
            OrderType.Market,
            OrderType.MarketOnOpen,
            OrderType.Limit,
            OrderType.StopMarket,
            OrderType.StopLimit,
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="TerminalLinkBrokerageModel"/> class.
        /// </summary>
        /// <param name="accountType">Cash or Margin</param>
        public TerminalLinkBrokerageModel(AccountType accountType = AccountType.Margin) : base(accountType)
        {
        }

        /// <summary>
        /// Returns true if the brokerage could accept this order.
        /// </summary>
        /// <param name="security">The security of the order</param>
        /// <param name="order">The order to be processed</param>
        /// <param name="message">If this function returns false, a brokerage message detailing why the order may not be submitted</param>
        public override bool CanSubmitOrder(Security security, Order order, out BrokerageMessageEvent message)
        {
            if (!_supportedSecurityTypes.Contains(security.Type))
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

            return base.CanSubmitOrder(security, order, out message);
        }

        /// <summary>
        /// TerminalLink does not allow modifying live orders; the EMSX brokerage rejects updates.
        /// </summary>
        public override bool CanUpdateOrder(Security security, Order order, UpdateOrderRequest request, out BrokerageMessageEvent message)
        {
            message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                Messages.DefaultBrokerageModel.OrderUpdateNotSupported);
            return false;
        }
    }
}
