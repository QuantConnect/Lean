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
using QuantConnect.Securities.Forex;
using QuantConnect.Securities.Interfaces;

namespace QuantConnect.Brokerages
{
    /// <summary>
    /// Provides FXCM specific properties
    /// </summary>
    public class FxcmBrokerageModel : DefaultBrokerageModel
    {
        /// <summary>
        /// Returns true if the brokerage could accept this order. This takes into account
        /// order type, security type, and order size limits.
        /// </summary>
        /// <remarks>
        /// For example, a brokerage may have no connectivity at certain times, or an order rate/size limit
        /// </remarks>
        /// <param name="security"></param>
        /// <param name="order">The order to be processed</param>
        /// <param name="message">If this function returns false, a brokerage message detailing why the order may not be submitted</param>
        /// <returns>True if the brokerage could process the order, false otherwise</returns>
        public override bool CanSubmitOrder(Security security, Order order, out BrokerageMessageEvent message)
        {
            message = null;

            // validate security type
            if (security.Type != SecurityType.Forex && security.Type != SecurityType.Cfd)
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                    "This model does not support " + security.Type + " security type."
                    );

                return false;
            }

            // validate order type
            if (order.Type != OrderType.Limit && order.Type != OrderType.Market && order.Type != OrderType.StopMarket)
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                    "This model does not support " + order.Type + " order type."
                    );

                return false;
            }

            // validate order quantity
            if (order.Quantity % 1000 != 0)
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                    "The order quantity must be a multiple of 1000."
                    );

                return false;
            }

            // validate stop/limit orders= prices
            var limit = order as LimitOrder;
            if (limit != null)
            {
                return IsValidOrderPrices(security, OrderType.Limit, limit.Direction, security.Price, limit.LimitPrice, ref message);
            }

            var stopMarket = order as StopMarketOrder;
            if (stopMarket != null)
            {
                return IsValidOrderPrices(security, OrderType.StopMarket, stopMarket.Direction, stopMarket.StopPrice, security.Price, ref message);
            }

            var stopLimit = order as StopLimitOrder;
            if (stopLimit != null)
            {
                return IsValidOrderPrices(security, OrderType.StopLimit, stopLimit.Direction, stopLimit.StopPrice, stopLimit.LimitPrice, ref message);
            }

            return true;
        }

        /// <summary>
        /// Returns true if the brokerage would allow updating the order as specified by the request
        /// </summary>
        /// <param name="security">The security of the order</param>
        /// <param name="order">The order to be updated</param>
        /// <param name="request">The requested update to be made to the order</param>
        /// <param name="message">If this function returns false, a brokerage message detailing why the order may not be updated</param>
        /// <returns>True if the brokerage would allow updating the order, false otherwise</returns>
        public override bool CanUpdateOrder(Security security, Order order, UpdateOrderRequest request, out BrokerageMessageEvent message)
        {
            message = null;

            // validate order quantity
            if (request.Quantity != null && request.Quantity % 1000 != 0)
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                    "The order quantity must be a multiple of 1000."
                    );

                return false;
            }
            
            // determine direction via the new, updated quantity
            var newQuantity = request.Quantity ?? order.Quantity;
            var direction = newQuantity > 0 ? OrderDirection.Buy : OrderDirection.Sell;

            // use security.Price if null, allows to pass checks
            var stopPrice = request.StopPrice ?? security.Price;
            var limitPrice = request.LimitPrice ?? security.Price;

            return IsValidOrderPrices(security, order.Type, direction, stopPrice, limitPrice, ref message);
        }

        /// <summary>
        /// Gets a new transaction model that represents this brokerage's fee structure and fill behavior
        /// </summary>
        /// <param name="security">The security to get a transaction model for</param>
        /// <returns>The transaction model for this brokerage</returns>
        public override ISecurityTransactionModel GetTransactionModel(Security security)
        {
            switch (security.Type)
            {
                case SecurityType.Forex:
                    return new FxcmTransactionModel();

                case SecurityType.Cfd:
                default:
                    throw new Exception("FXCM does not support the asset type " + security.Type + ". " +
                                        "Please ensure your algorithm only uses Forex (CFD assets are not currently supported).");
            }
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
