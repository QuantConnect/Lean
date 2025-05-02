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
    /// Represents a brokerage model specific to Charles Schwab.
    /// </summary>
    public class CharlesSchwabBrokerageModel : DefaultBrokerageModel
    {
        /// <summary>
        /// HashSet containing the security types supported by TradeStation.
        /// </summary>
        private readonly HashSet<SecurityType> _supportSecurityTypes = new(
            new[]
            {
                SecurityType.Equity,
                SecurityType.Option,
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
                OrderType.ComboMarket,
                OrderType.ComboLimit,
                OrderType.MarketOnClose,
                OrderType.MarketOnOpen
            });

        /// <summary>
        /// Constructor for Charles Schwab brokerage model
        /// </summary>
        /// <param name="accountType">Cash or Margin</param>
        public CharlesSchwabBrokerageModel(AccountType accountType = AccountType.Margin)
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
            return new CharlesSchwabFeeModel();
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

            return base.CanSubmitOrder(security, order, out message);
        }
    }
}
