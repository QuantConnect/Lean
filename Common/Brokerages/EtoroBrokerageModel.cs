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
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Brokerages
{
    /// <summary>
    /// Represents a brokerage model specific to eToro.
    /// </summary>
    public class EtoroBrokerageModel : DefaultBrokerageModel
    {
        /// <summary>
        /// The least distance of a crypto stop loss from the price, in percent of the margin, which is the whole
        /// position at leverage 1, the one leverage of crypto at eToro. eToro moves a closer stop loss there without an error.
        /// It is the <c>minStopLossPercentage</c> in the <c>leverageConfigs</c> of every crypto instrument in the eligibility
        /// endpoint, https://api-portal.etoro.com/api-reference/trading--real/check-instrument-trading-eligibility.md;
        /// the other security types carry it per instrument, 0 on most.
        /// </summary>
        private const int CryptoMinimumStopLossPercent = 10;

        /// <summary>
        /// The security types supported by eToro: US stocks and ETFs, crypto and currency pairs.
        /// </summary>
        private readonly HashSet<SecurityType> _supportSecurityTypes = [SecurityType.Equity, SecurityType.Crypto, SecurityType.Forex];

        /// <summary>
        /// The order types supported by the <see cref="CanSubmitOrder"/> operation in eToro.
        /// </summary>
        private readonly HashSet<OrderType> _supportOrderTypes = [OrderType.Market];

        /// <summary>
        /// The order types of an exit order an entry triggers: a stop market order is the stop loss of the position the
        /// entry opens, a limit order its take profit.
        /// </summary>
        private readonly HashSet<OrderType> _supportContingentExitOrderTypes = [OrderType.StopMarket, OrderType.Limit];

        /// <summary>
        /// The contingency types supported: an entry which triggers its exits, and the two exits of a bracket where one
        /// cancels the other.
        /// </summary>
        private readonly HashSet<ContingencyType> _supportedContingencyTypes =
        [
            ContingencyType.OneTriggersOther,
            ContingencyType.OneCancelsOther
        ];

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
        /// <param name="accountType">Cash or Margin; a cash account gives every security leverage 1 and takes no other,
        /// a margin account starts at the leverage of <see cref="GetLeverage"/> and takes another with SetLeverage</param>
        public EtoroBrokerageModel(AccountType accountType = AccountType.Cash)
            : base(accountType)
        {
        }

        /// <summary>
        /// Gets the leverage a security starts at: 1 on a cash account; on a margin account 30 on a currency pair, the
        /// highest eToro offers, and Lean's default elsewhere, 2 on equities and 1 on crypto, inside what eToro offers.
        /// </summary>
        /// <param name="security">The security</param>
        /// <returns>The leverage the security starts at</returns>
        public override decimal GetLeverage(Security security)
        {
            if (AccountType == AccountType.Margin && security.Type == SecurityType.Forex)
            {
                // the highest eToro offers on a currency pair; a pair that takes 10 or 20 is refused at the order
                return 30m;
            }

            return base.GetLeverage(security);
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
            if (!CanSubmitContingentOrder(security, order, out message))
            {
                return false;
            }

            if (!_supportSecurityTypes.Contains(security.Type))
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                    Messages.DefaultBrokerageModel.UnsupportedSecurityType(this, security));
                return false;
            }

            var isContingentExitOrder = order.GetContingencyLink(ContingencyRole.Child) != null;
            var supportedOrderTypes = isContingentExitOrder ? _supportContingentExitOrderTypes : _supportOrderTypes;
            if (!supportedOrderTypes.Contains(order.Type))
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                    Messages.DefaultBrokerageModel.UnsupportedOrderType(this, order, supportedOrderTypes));
                return false;
            }

            if (isContingentExitOrder && !IsExitPriceSupported(security, order, order is StopMarketOrder stopLoss ? stopLoss.StopPrice : ((LimitOrder)order).LimitPrice, out message))
            {
                return false;
            }

            if (!isContingentExitOrder)
            {
                var orderPosition = BrokerageExtensions.GetOrderPosition(order.Direction, security.Holdings.Quantity);
                var hasStopLoss = order.Contingency?.OrderTypes.Contains(OrderType.StopMarket) == true;
                if (orderPosition == OrderPosition.SellToOpen && !hasStopLoss)
                {
                    message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                        Messages.EtoroBrokerageModel.ShortSaleRequiresStopLoss(order));
                    return false;
                }

                if ((orderPosition is OrderPosition.BuyToOpen or OrderPosition.SellToOpen) && security.Leverage > 1 && !hasStopLoss)
                {
                    message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                        Messages.EtoroBrokerageModel.LeverageRequiresStopLoss(order, security.Leverage));
                    return false;
                }
            }

            if (!isContingentExitOrder && !BrokerageExtensions.ValidateCrossZeroOrder(this, security, order, out message, NotSupportedCrossZeroOrderTypes))
            {
                return false;
            }

            return base.CanSubmitOrder(security, order, out message);
        }

        /// <summary>
        /// Validates a set of contingent orders: a market entry which once filled triggers a stop market order (the stop loss),
        /// a limit order (the take profit), or both where one cancels the other (a bracket). eToro sets them on the position
        /// the entry opens, so the exits are on the opposite side of the entry and close the position whole.
        /// </summary>
        /// <param name="security">The security of the order</param>
        /// <param name="order">The order to be processed</param>
        /// <param name="message">If this function returns false, a brokerage message detailing why the order may not be submitted</param>
        /// <returns>True if the order is not a contingent order or its set is supported</returns>
        private bool CanSubmitContingentOrder(Security security, Order order, out BrokerageMessageEvent message)
        {
            if (!this.ValidateContingentOrder(order, _supportedContingencyTypes, out message, supportsComboOrders: false,
                supportsMultipleSymbols: false, supportsNesting: false, maximumOrderCount: 3))
            {
                return false;
            }

            var contingency = order.Contingency;
            if (contingency == null)
            {
                return true;
            }

            var isEntry = order.GetContingencyLink(ContingencyRole.Parent) != null;
            var isExit = order.GetContingencyLink(ContingencyRole.Child) != null;
            var isMember = order.GetSiblingLink() != null;
            if (!isEntry && !isExit)
            {
                message = this.UnsupportedContingentOrdersShape("the exit orders have to be triggered by an entry order.");
            }
            else if (isEntry && isMember)
            {
                message = this.UnsupportedContingentOrdersShape("the entry order can not be part of a one cancels other contingency.");
            }
            else if (isEntry && BrokerageExtensions.GetOrderPosition(order.Direction, security.Holdings.Quantity) is not (OrderPosition.BuyToOpen or OrderPosition.SellToOpen))
            {
                message = this.UnsupportedContingentOrdersShape("the entry order has to open a position.");
            }
            else if (contingency.Directions.Count != 2 || isEntry && contingency.Quantities.Count(quantity => Math.Sign(quantity) == Math.Sign(order.Quantity)) != 1)
            {
                message = this.UnsupportedContingentOrdersShape("the exit orders have to be on the opposite side of the entry order.");
            }
            else if (contingency.Quantities.Select(Math.Abs).Distinct().Count() != 1)
            {
                message = this.UnsupportedContingentOrdersShape("the exit orders have to be for the quantity of the entry order: eToro closes the whole position at the stop loss or the take profit.");
            }
            else if (contingency.Count == 3 && (isExit && !isMember
                || !contingency.OrderTypes.Contains(OrderType.Limit) || !contingency.OrderTypes.Contains(OrderType.StopMarket)))
            {
                message = this.UnsupportedContingentOrdersShape("3 orders are only supported as a bracket: an entry order which triggers a take profit limit order and a stop loss stop market order where one cancels the other.");
            }
            return message == null;
        }

        /// <summary>
        /// Returns true for a price change of a stop market or a limit order, the stop loss or the take profit of a
        /// position, which eToro moves on the position; the order may come without its entry, read from the position
        /// after a restart. The new price is checked as at submission, see <see cref="IsExitPriceSupported"/>. Any
        /// other update is refused, eToro has no order update; an order is cancelled and placed again instead.
        /// </summary>
        /// <param name="security">The security of the order</param>
        /// <param name="order">The order to be updated</param>
        /// <param name="request">The requested update to be made to the order</param>
        /// <param name="message">If this function returns false, a brokerage message detailing why the order may not be updated</param>
        /// <returns>True for a price change of an exit order, false otherwise</returns>
        public override bool CanUpdateOrder(Security security, Order order, UpdateOrderRequest request, out BrokerageMessageEvent message)
        {
            message = null;
            if (order.Type is not (OrderType.StopMarket or OrderType.Limit))
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                    Messages.DefaultBrokerageModel.OrderUpdateNotSupported);
                return false;
            }

            if (request.Quantity.HasValue && request.Quantity.Value != order.Quantity)
            {
                // the whole position closes at the stop loss or the take profit
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                    Messages.DefaultBrokerageModel.UnsupportedContingentOrdersQuantityUpdate(this));
                return false;
            }

            var exitPrice = order.Type == OrderType.StopMarket ? request.StopPrice : request.LimitPrice;
            if (exitPrice.HasValue && !IsExitPriceSupported(security, order, exitPrice.Value, out message))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks the price of an exit order as eToro takes a rate of the position: the stop loss of a long position
        /// below the price and of a short position above it, the take profit the other side, and a crypto stop loss
        /// at least 10% away from the price. eToro refuses a rate on the wrong side of the price, where the fill model
        /// would fill the exit at once. A security without a price yet is not checked.
        /// </summary>
        /// <param name="security">The security of the order</param>
        /// <param name="order">The exit order: a sell closes a long position, a buy a short one</param>
        /// <param name="exitPrice">The stop price of a stop loss or the limit price of a take profit, asked or new</param>
        /// <param name="message">The refusal, null when eToro takes the price</param>
        /// <returns>True when eToro takes the price</returns>
        private static bool IsExitPriceSupported(Security security, Order order, decimal exitPrice, out BrokerageMessageEvent message)
        {
            message = null;
            if (security.Price == 0)
            {
                return true;
            }

            var closesLong = order.Direction == OrderDirection.Sell;
            switch (order.Type)
            {
                case OrderType.StopMarket:
                    if (closesLong ? exitPrice >= security.Price : exitPrice <= security.Price)
                    {
                        message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                            Messages.EtoroBrokerageModel.StopLossOnTheWrongSide(order, exitPrice, security.Price));
                    }
                    else if (security.Type == SecurityType.Crypto && Math.Abs(security.Price - exitPrice) < security.Price * CryptoMinimumStopLossPercent / 100)
                    {
                        message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                            Messages.EtoroBrokerageModel.CryptoStopLossTooClose(order, security.Price, CryptoMinimumStopLossPercent));
                    }
                    break;
                case OrderType.Limit:
                    if (closesLong ? exitPrice <= security.Price : exitPrice >= security.Price)
                    {
                        message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                            Messages.EtoroBrokerageModel.TakeProfitOnTheWrongSide(order, exitPrice, security.Price));
                    }
                    break;
            }

            return message == null;
        }
    }
}
