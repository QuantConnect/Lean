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

using System;
using QuantConnect.Orders;
using QuantConnect.Securities;
using System.Collections.Generic;

namespace QuantConnect.Brokerages
{
    /// <summary>
    /// Provides extension methods for handling brokerage operations.
    /// </summary>
    public static class BrokerageExtensions
    {
        /// <summary>
        /// The default set of order types that are not allowed to cross zero holdings.
        /// This is used by <see cref="ValidateCrossZeroOrder"/> when no custom set is provided.
        /// </summary>
        private static readonly IReadOnlySet<OrderType> DefaultNotSupportedCrossZeroOrderTypes = new HashSet<OrderType>
        {
            OrderType.MarketOnOpen,
            OrderType.MarketOnClose
        };

        /// <summary>
        /// Determines if executing the specified order will cross the zero holdings threshold.
        /// </summary>
        /// <param name="holdingQuantity">The current quantity of holdings.</param>
        /// <param name="orderQuantity">The quantity of the order to be evaluated.</param>
        /// <returns>
        /// <c>true</c> if the order will change the holdings from positive to negative or vice versa; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// This method checks if the order will result in a position change from positive to negative holdings or from negative to positive holdings.
        /// </remarks>
        public static bool OrderCrossesZero(decimal holdingQuantity, decimal orderQuantity)
        {
            //We're reducing position or flipping:
            if (holdingQuantity > 0 && orderQuantity < 0)
            {
                if ((holdingQuantity + orderQuantity) < 0)
                {
                    //We don't have enough holdings so will cross through zero:
                    return true;
                }
            }
            else if (holdingQuantity < 0 && orderQuantity > 0)
            {
                if ((holdingQuantity + orderQuantity) > 0)
                {
                    //Crossed zero: need to split into 2 orders:
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Determines whether an order that crosses zero holdings is permitted 
        /// for the specified brokerage model and order type.
        /// </summary>
        /// <param name="brokerageModel">The brokerage model performing the validation.</param>
        /// <param name="security">The security associated with the order.</param>
        /// <param name="order">The order to validate.</param>
        /// <param name="notSupportedTypes">The set of order types that cannot cross zero holdings.</param>
        /// <param name="message">
        /// When the method returns <c>false</c>, contains a <see cref="BrokerageMessageEvent"/> 
        /// explaining why the order is not supported; otherwise <c>null</c>.
        /// </param>
        /// <returns>
        /// <c>true</c> if the order is valid to submit; <c>false</c> if crossing zero is not supported 
        /// for the given order type.
        /// </returns>
        public static bool ValidateCrossZeroOrder(
            IBrokerageModel brokerageModel,
            Security security,
            Order order,
            out BrokerageMessageEvent message,
            IReadOnlySet<OrderType> notSupportedTypes = null)
        {
            message = null;
            notSupportedTypes ??= DefaultNotSupportedCrossZeroOrderTypes;

            if (OrderCrossesZero(security.Holdings.Quantity, order.Quantity) && notSupportedTypes.Contains(order.Type))
            {
                message = new BrokerageMessageEvent(
                    BrokerageMessageType.Warning,
                    "NotSupported",
                    Messages.DefaultBrokerageModel.UnsupportedCrossZeroByOrderType(brokerageModel, order.Type)
                );
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets the position that might result given the specified order direction and the current holdings quantity.
        /// This is useful for brokerages that require more specific direction information than provided by the OrderDirection enum
        /// (e.g. Tradier differentiates Buy/Sell and BuyToOpen/BuyToCover/SellShort/SellToClose)
        /// </summary>
        /// <param name="orderDirection">The order direction</param>
        /// <param name="holdingsQuantity">The current holdings quantity</param>
        /// <returns>The order position</returns>
        public static OrderPosition GetOrderPosition(OrderDirection orderDirection, decimal holdingsQuantity)
        {
            return orderDirection switch
            {
                OrderDirection.Buy => holdingsQuantity >= 0 ? OrderPosition.BuyToOpen : OrderPosition.BuyToClose,
                OrderDirection.Sell => holdingsQuantity <= 0 ? OrderPosition.SellToOpen : OrderPosition.SellToClose,
                _ => throw new ArgumentOutOfRangeException(nameof(orderDirection), orderDirection, "Invalid order direction")
            };
        }
    }
}
