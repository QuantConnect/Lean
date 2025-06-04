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
using System.Collections.Generic;
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Orders.TimeInForces;
using QuantConnect.Securities;

namespace QuantConnect.Brokerages
{
    /// <summary>
    /// Provides tradier specific properties
    /// </summary>
    public class TradierBrokerageModel : DefaultBrokerageModel
    {
        private static readonly MarketHoursSegment PreMarketSession = new MarketHoursSegment(
            MarketHoursState.PreMarket,
            new TimeSpan(4, 0, 0),
            new TimeSpan(9, 24, 0));

        private static readonly MarketHoursSegment PostMarketSession = new MarketHoursSegment(
            MarketHoursState.PostMarket,
            new TimeSpan(16, 0, 0),
            new TimeSpan(19, 55, 0));

        private readonly HashSet<OrderType> _supportedOrderTypes = new HashSet<OrderType>
        {
            OrderType.Limit,
            OrderType.Market,
            OrderType.StopMarket,
            OrderType.StopLimit
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultBrokerageModel"/> class
        /// </summary>
        /// <param name="accountType">The type of account to be modeled, defaults to
        /// <see cref="QuantConnect.AccountType.Margin"/></param>
        public TradierBrokerageModel(AccountType accountType = AccountType.Margin)
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
        /// <param name="security">The security of the order</param>
        /// <param name="order">The order to be processed</param>
        /// <param name="message">If this function returns false, a brokerage message detailing why the order may not be submitted</param>
        /// <returns>True if the brokerage could process the order, false otherwise</returns>
        public override bool CanSubmitOrder(Security security, Order order, out BrokerageMessageEvent message)
        {
            message = null;

            if (!_supportedOrderTypes.Contains(order.Type))
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                    Messages.DefaultBrokerageModel.UnsupportedOrderType(this, order, _supportedOrderTypes));

                return false;
            }

            var securityType = order.SecurityType;
            if (securityType != SecurityType.Equity && securityType != SecurityType.Option)
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                    Messages.TradierBrokerageModel.UnsupportedSecurityType);

                return false;
            }

            if (order.TimeInForce is not GoodTilCanceledTimeInForce && order.TimeInForce is not DayTimeInForce)
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                    Messages.TradierBrokerageModel.UnsupportedTimeInForceType);

                return false;
            }

            if (security.Holdings.Quantity + order.Quantity < 0)
            {
                if (order.TimeInForce is GoodTilCanceledTimeInForce)
                {
                    message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "ShortOrderIsGtc", Messages.TradierBrokerageModel.ShortOrderIsGtc);

                    return false;
                }
                else if (security.Price < 5)
                {
                    message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "SellShortOrderLastPriceBelow5", Messages.TradierBrokerageModel.SellShortOrderLastPriceBelow5);

                    return false;
                }
            }

            if (order.AbsoluteQuantity < 1 || order.AbsoluteQuantity > 10000000)
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "IncorrectOrderQuantity", Messages.TradierBrokerageModel.IncorrectOrderQuantity);

                return false;
            }

            if (!CanExecuteOrderImpl(security, order, out var canSubmit))
            {
                if (!canSubmit)
                {
                    message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "ExtendedMarket",
                        Messages.TradierBrokerageModel.ExtendedMarketHoursTradingNotSupportedOutsideExtendedSession(PreMarketSession, PostMarketSession));
                    return false;
                }

                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "ExtendedMarket",
                    Messages.TradierBrokerageModel.ExtendedMarketHoursTradingNotSupported);
            }

            // tradier order limits
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

            // Tradier doesn't allow updating order quantities
            if (request.Quantity != null && request.Quantity != order.Quantity)
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "UpdateRejected",
                    Messages.TradierBrokerageModel.OrderQuantityUpdateNotSupported);

                return false;
            }

            return true;
        }

        private static bool CanExecuteOrderImpl(Security security, Order order, out bool canSubmit)
        {
            if (!security.Exchange.ExchangeOpen)
            {
                var tradeOnExtendedHours = (order.Properties as TradierOrderProperties)?.OutsideRegularTradingHours ?? false;
                if (!tradeOnExtendedHours ||
                    order.Type != OrderType.Limit ||
                    order.Symbol.SecurityType != SecurityType.Equity ||
                    !IsWithinTradierExtendedSession(security.LocalTime))
                {
                    // if OutsideRegularTradingHours is false, allow order submission since it will be processed on market open
                    canSubmit = !tradeOnExtendedHours;
                    return false;
                }

            }

            canSubmit = true;
            return true;
        }

        /// <summary>
        /// Returns true if the brokerage would be able to execute this order at this time assuming
        /// market prices are sufficient for the fill to take place. This is used to emulate the
        /// brokerage fills in backtesting and paper trading. For example some brokerages may not perform
        /// executions during extended market hours. This is not intended to be checking whether or not
        /// the exchange is open, that is handled in the Security.Exchange property.
        /// </summary>
        /// <param name="security">The security being ordered</param>
        /// <param name="order">The order to test for execution</param>
        /// <returns>True if the brokerage would be able to perform the execution, false otherwise</returns>
        public override bool CanExecuteOrder(Security security, Order order)
        {
            return CanExecuteOrderImpl(security, order, out _);
        }

        /// <summary>
        /// Applies the split to the specified order ticket
        /// </summary>
        /// <param name="tickets">The open tickets matching the split event</param>
        /// <param name="split">The split event data</param>
        public override void ApplySplit(List<OrderTicket> tickets, Split split)
        {
            // tradier cancels reverse splits
            var splitFactor = split.SplitFactor;
            if (splitFactor > 1.0m)
            {
                tickets.ForEach(ticket => ticket.Cancel(Messages.TradierBrokerageModel.OpenOrdersCancelOnReverseSplitSymbols));
            }
            else
            {
                base.ApplySplit(tickets, split);
            }
        }

        /// <summary>
        /// Gets a new fee model that represents this brokerage's fee structure
        /// </summary>
        /// <param name="security">The security to get a fee model for</param>
        /// <returns>The new fee model for this brokerage</returns>
        public override IFeeModel GetFeeModel(Security security)
        {
            // Trading stocks at Tradier Brokerage is free
            return new ConstantFeeModel(0m);
        }

        private static bool IsWithinTradierExtendedSession(DateTime localTime)
        {
            return PreMarketSession.Contains(localTime.TimeOfDay) || PostMarketSession.Contains(localTime.TimeOfDay);
        }
    }
}
