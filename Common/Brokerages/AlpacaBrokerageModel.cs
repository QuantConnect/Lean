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
using QuantConnect.Orders.TimeInForces;
using System.Linq;

namespace QuantConnect.Brokerages
{
    /// <summary>
    /// Provides an implementation of the <see cref="DefaultBrokerageModel"/> specific to Alpaca brokerage.
    /// </summary>
    public class AlpacaBrokerageModel : DefaultBrokerageModel
    {
        /// <summary>
        /// The default start time of the <see cref="OrderType.MarketOnOpen"/> order submission window.
        /// Example: 19:00 (7:00 PM).
        /// </summary>
        private static readonly TimeOnly _mooWindowStart = new(19, 0, 0);

        /// <summary>
        /// The contingency types supported by the brokerage: bracket, oco and oto order classes
        /// </summary>
        private readonly HashSet<ContingencyType> _supportedContingencyTypes = new()
        {
            ContingencyType.OneCancelsOther,
            ContingencyType.OneTriggersOther
        };

        /// <summary>
        /// A dictionary that maps each supported <see cref="SecurityType"/> to an array of <see cref="OrderType"/> supported by Alpaca brokerage.
        /// </summary>
        private readonly Dictionary<SecurityType, HashSet<OrderType>> _supportOrderTypeBySecurityType = new()
        {
            { SecurityType.Equity, new HashSet<OrderType> { OrderType.Market, OrderType.Limit, OrderType.StopMarket, OrderType.StopLimit,
                OrderType.TrailingStop, OrderType.MarketOnOpen, OrderType.MarketOnClose } },
            // Market and limit order types see https://docs.alpaca.markets/docs/options-trading-overview
            // Combo market and combo limit are the multi-leg option orders see https://docs.alpaca.markets/docs/options-level-3-trading
            { SecurityType.Option, new HashSet<OrderType> { OrderType.Market, OrderType.Limit, OrderType.ComboMarket, OrderType.ComboLimit } },
            { SecurityType.IndexOption, new HashSet<OrderType> { OrderType.Market, OrderType.Limit, OrderType.ComboMarket, OrderType.ComboLimit } },
            { SecurityType.Crypto, new HashSet<OrderType> { OrderType.Market, OrderType.Limit, OrderType.StopLimit }}
        };

        /// <summary>
        /// Defines the default set of <see cref="SecurityType"/> values that support <see cref="OrderType.MarketOnOpen"/> orders.
        /// </summary>
        private readonly IReadOnlySet<SecurityType> _defaultMarketOnOpenSupportedSecurityTypes;

        /// <summary>
        /// Initializes a new instance of the <see cref="AlpacaBrokerageModel"/> class
        /// </summary>
        /// <remarks>All Alpaca accounts are set up as margin accounts</remarks>
        public AlpacaBrokerageModel() : base(AccountType.Margin)
        {
            _defaultMarketOnOpenSupportedSecurityTypes = _supportOrderTypeBySecurityType.Where(x => x.Value.Contains(OrderType.MarketOnOpen)).Select(x => x.Key).ToHashSet();
        }

        /// <summary>
        /// Gets a new fee model that represents this brokerage's fee structure
        /// </summary>
        /// <param name="security">The security to get a fee model for</param>
        /// <returns>The new fee model for this brokerage</returns>
        public override IFeeModel GetFeeModel(Security security)
        {
            return new AlpacaFeeModel();
        }

        /// <summary>
        /// Validates contingent orders, Alpaca supports these order classes, always for a single equity symbol:
        ///  - bracket: an entry order which triggers a take profit limit order and a stop loss order, where one cancels the other
        ///  - oto: an entry order which triggers a single take profit limit order or stop loss order
        ///  - oco: a take profit limit order and a stop loss order where one cancels the other, to exit an existing position
        /// </summary>
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

            var isParent = order.GetContingencyLink(ContingencyRole.Parent) != null;
            var isChild = order.GetContingencyLink(ContingencyRole.Child) != null;
            var isMember = order.GetSiblingLink() != null;
            if (security.Type != SecurityType.Equity)
            {
                message = this.UnsupportedContingentOrdersShape("only equities are supported.");
            }
            else if (isParent && isMember)
            {
                message = this.UnsupportedContingentOrdersShape("the entry order can not be part of a one cancels other contingency.");
            }
            else if (!isParent && order.Type != OrderType.Limit && order.Type != OrderType.StopMarket && order.Type != OrderType.StopLimit)
            {
                message = this.UnsupportedContingentOrdersShape("the exit orders have to be a limit order (take profit) or a stop market/limit order (stop loss).");
            }
            else if (contingency.Count == 3 && !isParent && !(isChild && isMember))
            {
                message = this.UnsupportedContingentOrdersShape("3 orders are only supported as a bracket: an entry order which triggers a take profit and a stop loss where one cancels the other.");
            }
            else if (isMember && contingency.OrderTypes.Count > 0 && (!contingency.OrderTypes.Contains(OrderType.Limit)
                || !contingency.OrderTypes.Contains(OrderType.StopMarket) && !contingency.OrderTypes.Contains(OrderType.StopLimit)))
            {
                message = this.UnsupportedContingentOrdersShape("one cancels other requires a limit order (take profit) and a stop market/limit order (stop loss).");
            }
            else if (isMember && !isChild && contingency.Directions.Count > 1)
            {
                message = this.UnsupportedContingentOrdersShape("one cancels other orders have to be for the same side.");
            }
            return message == null;
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
            if (!_supportOrderTypeBySecurityType.TryGetValue(security.Type, out var supportOrderTypes))
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                    Messages.DefaultBrokerageModel.UnsupportedSecurityType(this, security));
                return false;
            }

            if (!supportOrderTypes.Contains(order.Type))
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                    Messages.DefaultBrokerageModel.UnsupportedOrderType(this, order, supportOrderTypes));
                return false;
            }

            // Every leg of a combo carries the group, so the number of legs is known from the first leg Lean sends.
            if (order.Type is OrderType.ComboMarket or OrderType.ComboLimit && order.GroupOrderManager.Count is < 2 or > 4)
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                    Messages.AlpacaBrokerageModel.UnsupportedComboOrderLegCount(this, order.GroupOrderManager.Count));
                return false;
            }

            var supportsOutsideTradingHours = (order.Properties as AlpacaOrderProperties)?.OutsideRegularTradingHours ?? false;
            if (supportsOutsideTradingHours && (order.Type != OrderType.Limit || order.TimeInForce is not DayTimeInForce))
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                    Messages.AlpacaBrokerageModel.TradingOutsideRegularHoursNotSupported(this, order.Type, order.TimeInForce));
                return false;
            }

            if (!CanSubmitContingentOrder(security, order, out message))
            {
                return false;
            }

            if (!BrokerageExtensions.ValidateCrossZeroOrder(this, security, order, out message))
            {
                return false;
            }

            if (!BrokerageExtensions.ValidateMarketOnOpenOrder(security, order, GetMarketOnOpenAllowedWindow, _defaultMarketOnOpenSupportedSecurityTypes, out message))
            {
                return false;
            }

            return base.CanSubmitOrder(security, order, out message);
        }

        /// <summary>
        /// Returns true if the brokerage would allow updating the order as specified by the request
        /// </summary>
        /// <param name="security">The security of the order</param>
        /// <param name="order">The order to be updated</param>
        /// <param name="request">The requested updated to be made to the order</param>
        /// <param name="message">If this function returns false, a brokerage message detailing why the order may not be updated</param>
        /// <returns>True if the brokerage would allow updating the order, false otherwise</returns>
        public override bool CanUpdateOrder(Security security, Order order, UpdateOrderRequest request, out BrokerageMessageEvent message)
        {
            message = null;
            if (order.Contingency != null && request.Quantity.HasValue && request.Quantity.Value != order.Quantity)
            {
                // the legs of bracket, oco and oto orders are sized by the brokerage
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                    Messages.DefaultBrokerageModel.UnsupportedContingentOrdersQuantityUpdate(this));
                return false;
            }
            return true;
        }

        /// <summary>
        /// Returns the allowed Market-on-Open submission window for Alpaca.
        /// </summary>
        /// <param name="marketHours">The market hours segment for the security.</param>
        /// <returns>
        /// A tuple with <c>MarketOnOpenWindowStart</c> (default 19:00 / 7:00 PM) and 
        /// <c>MarketOnOpenWindowEnd</c>, adjusted slightly before the market open to avoid rejection.
        /// </returns>
        private (TimeOnly MarketOnOpenWindowStart, TimeOnly MarketOnOpenWindowEnd) GetMarketOnOpenAllowedWindow(MarketHoursSegment marketHours)
        {
            return (_mooWindowStart, TimeOnly.FromTimeSpan(marketHours.Start.Add(-TimeSpan.FromMinutes(2))));
        }
    }
}
