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
using System.Linq;
using QuantConnect.Orders;
using QuantConnect.Securities;
using System.Collections.Generic;
using QuantConnect.Orders.TimeInForces;

namespace QuantConnect.Brokerages
{
    /// <summary>
    /// Provides properties specific to interactive brokers
    /// </summary>
    public class InteractiveBrokersFixModel : InteractiveBrokersBrokerageModel
    {
        /// <summary>
        /// Supported time in force
        /// </summary>
        protected override Type[] SupportedTimeInForces { get; } =
        {
            typeof(GoodTilCanceledTimeInForce),
            typeof(DayTimeInForce),
        };

        /// <summary>
        /// Supported order types
        /// </summary>
        protected override HashSet<OrderType> SupportedOrderTypes { get; } = new HashSet<OrderType>
        {
            OrderType.Market,
            OrderType.MarketOnOpen,
            OrderType.MarketOnClose,
            OrderType.Limit,
            OrderType.StopMarket,
            OrderType.StopLimit,
            OrderType.TrailingStop,
            OrderType.ComboMarket,
            OrderType.ComboLimit
        };

        private readonly GroupOrderCacheManager _groupOrderCacheManager = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="InteractiveBrokersFixModel"/> class
        /// </summary>
        /// <param name="accountType">The type of account to be modelled, defaults to
        /// <see cref="AccountType.Margin"/></param>
        public InteractiveBrokersFixModel(AccountType accountType = AccountType.Margin)
            : base(accountType)
        {
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
        public override bool CanSubmitOrder(Security security, Order order, out BrokerageMessageEvent message)
        {
            // only check supported combo order types
            if (order is ComboOrder && order.GroupOrderManager != null && SupportedOrderTypes.Contains(order.Type))
            {
                if (_groupOrderCacheManager.TryGetGroupCachedOrders(order, out var orders))
                {
                    // reject combos that mix FutureOption and Future legs
                    if (orders.Any(o => o.SecurityType == SecurityType.FutureOption) &&
                        orders.Any(o => o.SecurityType == SecurityType.Future))
                    {
                        message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                            Messages.InteractiveBrokersFixModel.UnsupportedFopFutureComboOrders(this, order));
                        return false;
                    }
                }
            }

            return base.CanSubmitOrder(security, order, out message);
        }
    }
}
