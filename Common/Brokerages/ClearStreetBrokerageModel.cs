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

using QuantConnect.Util;
using QuantConnect.Orders;
using QuantConnect.Securities;
using System.Collections.Generic;

namespace QuantConnect.Brokerages
{
    /// <summary>
    /// Represents a brokerage model specific to Clear Street.
    /// </summary>
    public class ClearStreetBrokerageModel : DefaultBrokerageModel
    {
        /// <summary>
        /// The default markets for Clear Street.
        /// </summary>
        public new static readonly IReadOnlyDictionary<SecurityType, string> DefaultMarketMap = new Dictionary<SecurityType, string>
        {
            { SecurityType.Equity, Market.USA },
            { SecurityType.Option, Market.USA },
            { SecurityType.Index, Market.USA }
        }.ToReadOnlyDictionary();

        /// <summary>
        /// The security types Clear Street accepts orders for.
        /// <see cref="SecurityType.Index"/> is missing on purpose, see <see cref="CanSubmitOrder"/>.
        /// </summary>
        private readonly HashSet<SecurityType> _supportSecurityTypes = new(
            new[]
            {
                SecurityType.Equity,
                SecurityType.Option
            });

        /// <summary>
        /// The order types supported by the <see cref="CanSubmitOrder"/> operation in Clear Street.
        /// </summary>
        private readonly HashSet<OrderType> _supportOrderTypes = new(
            new[]
            {
                OrderType.Market,
                OrderType.Limit,
                OrderType.StopMarket,
                OrderType.StopLimit,
                OrderType.TrailingStop
            });

        /// <summary>
        /// Gets a map of the default markets to be used for each security type
        /// </summary>
        public override IReadOnlyDictionary<SecurityType, string> DefaultMarkets
        {
            get { return DefaultMarketMap; }
        }

        /// <summary>
        /// Constructor for Clear Street brokerage model
        /// </summary>
        /// <param name="accountType">Cash or Margin</param>
        public ClearStreetBrokerageModel(AccountType accountType = AccountType.Margin)
            : base(accountType)
        {
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
            message = default;

            // Clear Street serves index quotes, but it holds no index position and takes no index order.
            if (security.Type == SecurityType.Index)
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                    Messages.ClearStreetBrokerageModel.IndexIsNotTradable(security));
                return false;
            }

            if (!_supportSecurityTypes.Contains(security.Type))
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                    Messages.DefaultBrokerageModel.UnsupportedSecurityType(this, security));
                return false;
            }

            if (!_supportOrderTypes.Contains(order.Type))
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                    Messages.DefaultBrokerageModel.UnsupportedOrderType(this, order, _supportOrderTypes));
                return false;
            }

            // Clear Street works out the open or close effect of an order itself, so an order that
            // crosses a zero position is sent as one order and is not split here.
            return base.CanSubmitOrder(security, order, out message);
        }
    }
}
