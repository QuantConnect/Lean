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
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Orders.TimeInForces;
using QuantConnect.Securities;
using QuantConnect.Util;

namespace QuantConnect.Brokerages
{
    /// <summary>
    /// Provides an implementation of <see cref="IBrokerageModel"/> specific to Tradovate brokerage.
    /// Tradovate is a futures-focused brokerage supporting CME Group products.
    /// </summary>
    public class TradovateBrokerageModel : DefaultBrokerageModel
    {
        /// <summary>
        /// The default markets for Tradovate brokerage
        /// </summary>
        public new static readonly IReadOnlyDictionary<SecurityType, string> DefaultMarketMap = new Dictionary<SecurityType, string>
        {
            { SecurityType.Future, Market.CME },
            { SecurityType.FutureOption, Market.CME }
        }.ToReadOnlyDictionary();

        /// <summary>
        /// Order types supported by Tradovate
        /// </summary>
        protected virtual HashSet<OrderType> SupportedOrderTypes { get; } = new HashSet<OrderType>
        {
            OrderType.Market,
            OrderType.Limit,
            OrderType.StopMarket,
            OrderType.StopLimit,
            OrderType.TrailingStop
        };

        /// <summary>
        /// Time in force types supported by Tradovate
        /// </summary>
        protected virtual Type[] SupportedTimeInForces { get; } =
        {
            typeof(GoodTilCanceledTimeInForce),
            typeof(DayTimeInForce)
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="TradovateBrokerageModel"/> class
        /// </summary>
        /// <param name="accountType">The type of account to be modeled, defaults to <see cref="AccountType.Margin"/></param>
        public TradovateBrokerageModel(AccountType accountType = AccountType.Margin)
            : base(accountType)
        {
        }

        /// <summary>
        /// Gets a map of the default markets to be used for each security type
        /// </summary>
        public override IReadOnlyDictionary<SecurityType, string> DefaultMarkets => DefaultMarketMap;

        /// <summary>
        /// Returns true if the brokerage could accept this order. This takes into account
        /// order type, security type, and order size limits.
        /// </summary>
        /// <param name="security">The security being ordered</param>
        /// <param name="order">The order to be processed</param>
        /// <param name="message">If this function returns false, a brokerage message detailing why the order may not be submitted</param>
        /// <returns>True if the brokerage could process the order, false otherwise</returns>
        public override bool CanSubmitOrder(Security security, Order order, out BrokerageMessageEvent message)
        {
            message = null;

            // Validate security type - Tradovate only supports futures
            if (security.Type != SecurityType.Future && security.Type != SecurityType.FutureOption)
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                    $"Tradovate does not support {security.Type} security type. Only Future and FutureOption are supported.");
                return false;
            }

            // Validate order type
            if (!SupportedOrderTypes.Contains(order.Type))
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                    $"Tradovate does not support {order.Type} order type. " +
                    $"Supported order types are: {string.Join(", ", SupportedOrderTypes)}");
                return false;
            }

            // Validate time in force
            if (!IsValidTimeInForce(order.TimeInForce))
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                    $"Tradovate does not support {order.TimeInForce.GetType().Name} time in force. " +
                    "Supported time in force types are: Day, GoodTilCanceled");
                return false;
            }

            return base.CanSubmitOrder(security, order, out message);
        }

        /// <summary>
        /// Returns true if the brokerage would allow updating the order as specified by the request.
        /// Tradovate supports order modifications via the order/modifyorder endpoint.
        /// </summary>
        /// <param name="security">The security of the order</param>
        /// <param name="order">The order to be updated</param>
        /// <param name="request">The requested update to be made to the order</param>
        /// <param name="message">If this function returns false, a brokerage message detailing why the order may not be updated</param>
        /// <returns>True if the brokerage would allow updating the order, false otherwise</returns>
        public override bool CanUpdateOrder(Security security, Order order, UpdateOrderRequest request, out BrokerageMessageEvent message)
        {
            message = null;

            // Tradovate supports order modifications
            // Note: When modifying orders, the timeInForce must match the existing order's setting
            return true;
        }

        /// <summary>
        /// Gets a new fee model that represents Tradovate's fee structure
        /// </summary>
        /// <param name="security">The security to get a fee model for</param>
        /// <returns>The new fee model for this brokerage</returns>
        public override IFeeModel GetFeeModel(Security security)
        {
            return new TradovateFeeModel();
        }

        /// <summary>
        /// Validates that the time in force is supported by Tradovate
        /// </summary>
        private bool IsValidTimeInForce(TimeInForce timeInForce)
        {
            var timeInForceType = timeInForce.GetType();
            foreach (var supportedType in SupportedTimeInForces)
            {
                if (supportedType == timeInForceType)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
