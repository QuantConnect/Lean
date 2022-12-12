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
using QuantConnect.Util;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Brokerages
{
    /// <summary>
    /// TDAmeritrade
    /// </summary>
    public class TDAmeritradeBrokerageModel : DefaultBrokerageModel
    {
        /// <summary>
        /// Array's TD Ameritrade supports security types 
        /// </summary>
        private readonly SecurityType[] _supportSecurityTypes = new[] { SecurityType.Equity, SecurityType.Option, SecurityType.Future };

        /// <summary>
        /// Array's TD Ameritrade supports order types 
        /// </summary>
        private readonly OrderType[] _supportOrderTypes = new[] { OrderType.Market, OrderType.Limit, OrderType.StopMarket, OrderType.StopLimit };

        /// <summary>
        /// Constructor for TDAmeritrade brokerage model
        /// </summary>
        /// <param name="accountType">Cash or Margin</param>
        public TDAmeritradeBrokerageModel(AccountType accountType = AccountType.Margin) : base(accountType)
        {

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
            if (!IsValidOrderSize(security, order.Quantity, out message))
            {
                return false;
            }

            message = null;

            if (!_supportSecurityTypes.Contains(security.Type))
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                    StringExtensions.Invariant($"The {nameof(TDAmeritradeBrokerageModel)} does not support {security.Type} security type.")
                );

                return false;
            }

            if (!_supportOrderTypes.Contains(order.Type))
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                    StringExtensions.Invariant($"{order.Type} order is not supported by TDAmeritrade. Currently, only Market Order is supported.")
                );

                return false;
            }

            return base.CanSubmitOrder(security, order, out message);
        }

        /// <summary>
        /// TDAmeritrade support Update Order
        /// </summary>
        /// <param name="security">Security</param>
        /// <param name="order">Order that should be updated</param>
        /// <param name="request">Update request</param>
        /// <param name="message">Outgoing message</param>
        /// <returns>True if the brokerage would allow updating the order, false otherwise</returns>
        public override bool CanUpdateOrder(Security security, Order order, UpdateOrderRequest request, out BrokerageMessageEvent message)
        {
            message = null;

            // determine direction via the new, updated quantity
            var newQuantity = request.Quantity ?? order.Quantity;
            var direction = newQuantity > 0 ? OrderDirection.Buy : OrderDirection.Sell;

            // use security.Price if null, allows to pass checks
            var stopPrice = request.StopPrice ?? security.Price;
            var limitPrice = request.LimitPrice ?? security.Price;

            return IsValidOrderPrices(security, order.Type, direction, stopPrice, limitPrice, ref message);
        }

        /// <summary>
        /// Provides TDAmeritrade fee model
        /// </summary>
        /// <param name="security">Security</param>
        /// <returns>TDAmeritrade fee model</returns>
        public override IFeeModel GetFeeModel(Security security)
        {
            return new TDAmeritradeFeeModel();
        }

        /// <summary>
        /// Validates limit/stopmarket order prices, pass security.Price for limit/stop if n/a
        /// </summary>
        private static bool IsValidOrderPrices(Security security, OrderType orderType, OrderDirection orderDirection, decimal stopPrice, decimal limitPrice, ref BrokerageMessageEvent message)
        {
            // validate order price
            var invalidPrice = orderType == OrderType.Limit && orderDirection == OrderDirection.Buy && limitPrice > security.Price ||
                orderType == OrderType.Limit && orderDirection == OrderDirection.Sell && limitPrice < security.Price ||
                orderType == OrderType.StopMarket && orderDirection == OrderDirection.Buy && stopPrice < security.Price ||
                orderType == OrderType.StopMarket && orderDirection == OrderDirection.Sell && stopPrice > security.Price;

            if (invalidPrice)
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                    "Limit Buy orders and Stop Sell orders must be below market, Limit Sell orders and Stop Buy orders must be above market."
                );

                return false;
            }

            return true;
        }
    }
}
