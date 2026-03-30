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
using QuantConnect.Orders.Fees;
using QuantConnect.Orders.TimeInForces;
using QuantConnect.Securities;

namespace QuantConnect.Brokerages
{
    /// <summary>
    /// Represents a brokerage model specific to Webull.
    /// </summary>
    public class WebullBrokerageModel : DefaultBrokerageModel
    {
        /// <summary>
        /// Maps each supported security type to the order types Webull allows for it.
        /// </summary>
        private static readonly Dictionary<SecurityType, HashSet<OrderType>> _supportedOrderTypesBySecurityType =
            new Dictionary<SecurityType, HashSet<OrderType>>
            {
                {
                    SecurityType.Equity, new HashSet<OrderType>
                    {
                        OrderType.Market,
                        OrderType.Limit,
                        OrderType.StopMarket,
                        OrderType.StopLimit,
                        OrderType.TrailingStop
                    }
                },
                {
                    SecurityType.Option, new HashSet<OrderType>
                    {
                        OrderType.Limit,
                        OrderType.StopMarket,
                        OrderType.StopLimit
                    }
                },
                {
                    SecurityType.IndexOption, new HashSet<OrderType>
                    {
                        OrderType.Limit,
                        OrderType.StopMarket,
                        OrderType.StopLimit
                    }
                },
                {
                    SecurityType.Future, new HashSet<OrderType>
                    {
                        OrderType.Market,
                        OrderType.Limit,
                        OrderType.StopMarket,
                        OrderType.StopLimit,
                        OrderType.TrailingStop
                    }
                },
                {
                    SecurityType.Crypto, new HashSet<OrderType>
                    {
                        OrderType.Market,
                        OrderType.Limit,
                        OrderType.StopLimit
                    }
                }
            };

        /// <summary>
        /// Constructor for Webull brokerage model.
        /// </summary>
        /// <param name="accountType">Cash or Margin</param>
        public WebullBrokerageModel(AccountType accountType = AccountType.Margin)
            : base(accountType)
        {
        }

        /// <summary>
        /// Provides the Webull fee model.
        /// </summary>
        /// <param name="security">Security</param>
        /// <returns>Webull fee model</returns>
        public override IFeeModel GetFeeModel(Security security)
        {
            return new WebullFeeModel();
        }

        /// <summary>
        /// Returns true if the brokerage could accept this order. This takes into account
        /// order type, security type, and order size limits.
        /// </summary>
        /// <remarks>
        /// For example, a brokerage may have no connectivity at certain times, or an order rate/size limit.
        /// </remarks>
        /// <param name="security">The security of the order</param>
        /// <param name="order">The order to be processed</param>
        /// <param name="message">If this function returns false, a brokerage message detailing why the order may not be submitted</param>
        /// <returns>True if the brokerage could process the order, false otherwise</returns>
        public override bool CanSubmitOrder(Security security, Order order, out BrokerageMessageEvent message)
        {
            message = default;

            if (!_supportedOrderTypesBySecurityType.TryGetValue(security.Type, out var supportedOrderTypes))
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                    Messages.DefaultBrokerageModel.UnsupportedSecurityType(this, security));
                return false;
            }

            if (!supportedOrderTypes.Contains(order.Type))
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                    Messages.DefaultBrokerageModel.UnsupportedOrderType(this, order, supportedOrderTypes));
                return false;
            }

            // Options and IndexOptions have per-direction TimeInForce restrictions.
            // https://developer.webull.com/apis/docs/trade-api/options#time-in-force
            // - Sell orders: Day only
            // - Buy orders: GoodTilCanceled only
            if (security.Type == SecurityType.Option || security.Type == SecurityType.IndexOption)
            {
                if (order.Direction == OrderDirection.Sell && order.TimeInForce is not DayTimeInForce)
                {
                    message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                        Messages.WebullBrokerageModel.InvalidTimeInForceForOptionSellOrder(order));
                    return false;
                }

                if (order.Direction == OrderDirection.Buy && order.TimeInForce is not GoodTilCanceledTimeInForce)
                {
                    message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                        Messages.WebullBrokerageModel.InvalidTimeInForceForOptionBuyOrder(order));
                    return false;
                }
            }

            if (order.Properties is WebullOrderProperties { OutsideRegularTradingHours: true } &&
                security.Type != SecurityType.Equity)
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                    Messages.WebullBrokerageModel.OutsideRegularTradingHoursNotSupportedForSecurityType(security));
                return false;
            }

            return base.CanSubmitOrder(security, order, out message);
        }
    }
}
