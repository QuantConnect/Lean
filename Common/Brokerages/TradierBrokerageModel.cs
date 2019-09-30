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
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Orders.Fills;
using QuantConnect.Orders.Slippage;
using QuantConnect.Securities;
using QuantConnect.Securities.Equity;

namespace QuantConnect.Brokerages
{
    /// <summary>
    /// Provides tradier specific properties
    /// </summary>
    public class TradierBrokerageModel : DefaultBrokerageModel
    {
        private static readonly EquityExchange EquityExchange =
            new EquityExchange(MarketHoursDatabase.FromDataFolder().GetExchangeHours(Market.USA, null, SecurityType.Equity));

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultBrokerageModel"/> class
        /// </summary>
        /// <param name="accountType">The type of account to be modelled, defaults to
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

            var securityType = order.SecurityType;
            if (securityType != SecurityType.Equity)
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                    "This model only supports equities."
                );

                return false;
            }

            if (order.Type == OrderType.MarketOnOpen || order.Type == OrderType.MarketOnClose)
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                    "Tradier brokerage only supports Market orders. MarketOnOpen and MarketOnClose orders not supported."
                );

                return false;
            }

            if (!CanExecuteOrder(security, order))
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "ExtendedMarket",
                    "Tradier does not support extended market hours trading.  Your order will be processed at market open."
                );
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
                    "Traider does not support updating order quantities."
                );

                return false;
            }

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
            EquityExchange.SetLocalDateTimeFrontier(security.Exchange.LocalTime);

            var cache = security.GetLastData();
            if (cache == null)
            {
                return false;
            }

            // tradier doesn't support after hours trading
            if (!EquityExchange.IsOpenDuringBar(cache.Time, cache.EndTime, false))
            {
                return false;
            }
            return true;
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
                tickets.ForEach(ticket => ticket.Cancel("Tradier Brokerage cancels open orders on reverse split symbols"));
            }
            else
            {
                base.ApplySplit(tickets, split);
            }
        }

        /// <summary>
        /// Gets a new fill model that represents this brokerage's fill behavior
        /// </summary>
        /// <param name="security">The security to get fill model for</param>
        /// <returns>The new fill model for this brokerage</returns>
        public override IFillModel GetFillModel(Security security)
        {
            return new ImmediateFillModel();
        }

        /// <summary>
        /// Gets a new fee model that represents this brokerage's fee structure
        /// </summary>
        /// <param name="security">The security to get a fee model for</param>
        /// <returns>The new fee model for this brokerage</returns>
        public override IFeeModel GetFeeModel(Security security)
        {
            return new ConstantFeeModel(1m);
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
