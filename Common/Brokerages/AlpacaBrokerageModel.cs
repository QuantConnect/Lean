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
using System.Linq;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Orders.Slippage;
using QuantConnect.Securities;
using QuantConnect.Util;
using static QuantConnect.StringExtensions;

namespace QuantConnect.Brokerages
{
    /// <summary>
    /// Alpaca Brokerage Model Implementation for Back Testing.
    /// </summary>
    public class AlpacaBrokerageModel : DefaultBrokerageModel
    {
        private readonly IOrderProvider _orderProvider;

        /// <summary>
        /// The default markets for the alpaca brokerage
        /// </summary>
        public new static readonly IReadOnlyDictionary<SecurityType, string> DefaultMarketMap = new Dictionary<SecurityType, string>
        {
            {SecurityType.Base, Market.USA},
            {SecurityType.Equity, Market.USA}
        }.ToReadOnlyDictionary();

        /// <summary>
        /// Gets a map of the default markets to be used for each security type
        /// </summary>
        public override IReadOnlyDictionary<SecurityType, string> DefaultMarkets => DefaultMarketMap;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultBrokerageModel"/> class
        /// </summary>
        /// <param name="orderProvider">The order provider</param>
        /// <param name="accountType">The type of account to be modelled, defaults to
        /// <see cref="AccountType.Cash"/></param>
        public AlpacaBrokerageModel(IOrderProvider orderProvider, AccountType accountType = AccountType.Margin)
            : base(accountType)
        {
            _orderProvider = orderProvider;
        }

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
            if (security.Type != SecurityType.Equity)
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                    Invariant($"The {nameof(AlpacaBrokerageModel)} does not support {security.Type} security type.")
                );

                return false;
            }

            // validate order type
            if (order.Type != OrderType.Limit && order.Type != OrderType.Market && order.Type != OrderType.StopMarket && order.Type != OrderType.StopLimit && order.Type != OrderType.MarketOnOpen)
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                    Invariant($"The {nameof(AlpacaBrokerageModel)} does not support {order.Type} order type.")
                );

                return false;
            }

            // validate time in force
            if (order.TimeInForce != TimeInForce.GoodTilCanceled && order.TimeInForce != TimeInForce.Day)
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                    $"The {nameof(AlpacaBrokerageModel)} does not support {order.TimeInForce.GetType().Name} time in force."
                );

                return false;
            }

            var openOrders = _orderProvider.GetOpenOrders(x => x.Symbol == order.Symbol && x.Id != order.Id);

            if (security.Holdings.IsLong)
            {
                var openSellQuantity = openOrders.Where(x => x.Direction == OrderDirection.Sell).Sum(x => x.Quantity);
                var availableSellQuantity = -security.Holdings.Quantity - openSellQuantity;

                // cannot reverse position from long to short (open sell orders are taken into account)
                if (order.Direction == OrderDirection.Sell && order.Quantity < availableSellQuantity)
                {
                    message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                        (openSellQuantity == 0
                        ? $"The {nameof(AlpacaBrokerageModel)} does not support reversing the position (from long to short) with a single order"
                        : $"The {nameof(AlpacaBrokerageModel)} does not support submitting orders which could potentially reverse the position (from long to short)") +
                        $" [position:{security.Holdings.Quantity}, order quantity:{order.Quantity}, " +
                        $"open sell orders quantity:{openSellQuantity}, available sell quantity:{availableSellQuantity}]."
                    );

                    return false;
                }
            }
            else if (security.Holdings.IsShort)
            {
                var openBuyQuantity = openOrders.Where(x => x.Direction == OrderDirection.Buy).Sum(x => x.Quantity);
                var availableBuyQuantity = -security.Holdings.Quantity - openBuyQuantity;

                // cannot reverse position from short to long (open buy orders are taken into account)
                if (order.Direction == OrderDirection.Buy && order.Quantity > availableBuyQuantity)
                {
                    message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                        (openBuyQuantity == 0
                        ? $"The {nameof(AlpacaBrokerageModel)} does not support reversing the position (from short to long) with a single order"
                        : $"The {nameof(AlpacaBrokerageModel)} does not support submitting orders which could potentially reverse the position (from short to long)") +
                        $" [position:{security.Holdings.Quantity}, order quantity:{order.Quantity}, " +
                        $"open buy orders quantity:{openBuyQuantity}, available buy quantity:{availableBuyQuantity}]."
                    );

                    return false;
                }
            }
            else if (security.Holdings.Quantity == 0)
            {
                // cannot open a short sell while a long buy order is open
                if (order.Direction == OrderDirection.Sell && openOrders.Any(x => x.Direction == OrderDirection.Buy))
                {
                    var openBuyQuantity = openOrders.Where(x => x.Direction == OrderDirection.Buy).Sum(x => x.Quantity);

                    message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                        $"The {nameof(AlpacaBrokerageModel)} does not support submitting sell orders with open buy orders" +
                        $" [position:{security.Holdings.Quantity}, order quantity:{order.Quantity}, " +
                        $"open buy orders quantity:{openBuyQuantity}]."
                    );

                    return false;
                }

                // cannot open a long buy while a short sell order is open
                if (order.Direction == OrderDirection.Buy && openOrders.Any(x => x.Direction == OrderDirection.Sell))
                {
                    var openSellQuantity = openOrders.Where(x => x.Direction == OrderDirection.Sell).Sum(x => x.Quantity);

                    message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                        $"The {nameof(AlpacaBrokerageModel)} does not support submitting buy orders with open sell orders" +
                        $" [position:{security.Holdings.Quantity}, order quantity:{order.Quantity}, " +
                        $"open sell orders quantity:{openSellQuantity}]."
                    );

                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Gets a new fee model that represents this brokerage's fee structure
        /// </summary>
        /// <param name="security">The security to get a fee model for</param>
        /// <returns>The new fee model for this brokerage</returns>
        public override IFeeModel GetFeeModel(Security security)
        {
            return new ConstantFeeModel(0m);
        }

        /// <summary>
        /// Gets a new slippage model that represents this brokerage's fill slippage behavior
        /// </summary>
        /// <param name="security">The security to get a slippage model for</param>
        /// <returns>The new slippage model for this brokerage</returns>
        public override ISlippageModel GetSlippageModel(Security security)
        {
            return new ConstantSlippageModel(0);
        }
    }
}