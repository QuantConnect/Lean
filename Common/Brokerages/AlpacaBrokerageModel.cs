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
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Orders.Fees;
using System.Collections.Generic;

namespace QuantConnect.Brokerages
{
    /// <summary>
    /// Provides an implementation of the <see cref="DefaultBrokerageModel"/> specific to Alpaca brokerage.
    /// </summary>
    public class AlpacaBrokerageModel : DefaultBrokerageModel
    {
        /// <summary>
        /// A dictionary that maps each supported <see cref="SecurityType"/> to an array of <see cref="OrderType"/> supported by Alpaca brokerage.
        /// </summary>
        private readonly Dictionary<SecurityType, HashSet<OrderType>> _supportOrderTypeBySecurityType = new()
        {
            { SecurityType.Equity, new HashSet<OrderType> { OrderType.Market, OrderType.Limit, OrderType.StopMarket, OrderType.StopLimit, OrderType.TrailingStop } },
            { SecurityType.Option, new HashSet<OrderType> { OrderType.Market, OrderType.Limit, OrderType.StopMarket, OrderType.StopLimit, OrderType.TrailingStop } },
            { SecurityType.Crypto, new HashSet<OrderType> { OrderType.Market, OrderType.Limit, OrderType.StopLimit }}
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="AlpacaBrokerageModel"/> class with the specified account type.
        /// </summary>
        /// <param name="accountType">The type of account, which can be either Cash or Margin. Defaults to Cash if not specified.</param>
        public AlpacaBrokerageModel(AccountType accountType = AccountType.Cash) : base(accountType)
        { }

        /// <inheritdoc cref="IBrokerageModel.GetFeeModel(Security)" />
        public override IFeeModel GetFeeModel(Security security)
        {
            if (security == null)
            {
                throw new ArgumentNullException(nameof(security), "The security parameter cannot be null.");
            }

            return security.Type switch
            {
                SecurityType.Crypto => new AlpacaFeeModel(),
                SecurityType.Base => base.GetFeeModel(security),
                _ => throw new ArgumentOutOfRangeException(nameof(security), security, $"Not supported security type {security.Type}")
            };
        }

        /// <inheritdoc cref="IBrokerageModel.CanSubmitOrder(Security, Order, out BrokerageMessageEvent)"/>
        public override bool CanSubmitOrder(Security security, Order order, out BrokerageMessageEvent message)
        {
            if (security == null)
            {
                throw new ArgumentNullException(nameof(security), "The security cannot be null. Please provide a valid security.");
            }
            else if (order == null)
            {
                throw new ArgumentNullException(nameof(order), "The order cannot be null. Please provide a valid order.");
            }

            if (!_supportOrderTypeBySecurityType.ContainsKey(security.Type))
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                    Messages.DefaultBrokerageModel.UnsupportedSecurityType(this, security));
                return false;
            }

            if (!_supportOrderTypeBySecurityType.TryGetValue(security.Type, out var supportOrderTypes) && supportOrderTypes.Contains(order.Type))
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                    Messages.DefaultBrokerageModel.UnsupportedOrderType(this, order, supportOrderTypes));
                return false;
            }

            return base.CanSubmitOrder(security, order, out message);
        }

        /// <inheritdoc cref="IBrokerageModel.CanUpdateOrder(Security, Order, UpdateOrderRequest, out BrokerageMessageEvent)"/>
        public override bool CanUpdateOrder(Security security, Order order, UpdateOrderRequest request, out BrokerageMessageEvent message)
        {
            message = null;
            return true;
        }
    }
}
