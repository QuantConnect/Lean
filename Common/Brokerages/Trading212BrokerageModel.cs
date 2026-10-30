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
using QuantConnect.Securities;
using System.Collections.Generic;

namespace QuantConnect.Brokerages
{
    /// <summary>
    /// Represents a brokerage model specific to Trading 212.
    /// </summary>
    /// <remarks>
    /// Trading 212 is commission-free and long only: the broker's equity accounts (Invest,
    /// Stocks ISA and SIPP) do not offer short selling or margin, and its public API accepts
    /// market, limit, stop and stop-limit orders for equities. The leveraged CFD product is a
    /// separate account type and is out of scope for this model.
    /// The long-only rule is evaluated against the security's holdings when the order is submitted,
    /// matching the other LEAN brokerage models, so it does not account for other pending unfilled orders.
    /// The broker rejects an over-sell with <c>SellingEquityNotOwned</c>, so a same-slice duplicate is
    /// not accepted as a short in a live account.
    /// This model only covers order submission rules.
    /// </remarks>
    public class Trading212BrokerageModel : DefaultBrokerageModel
    {
        /// <summary>
        /// A dictionary that maps each supported <see cref="SecurityType"/> to the set of
        /// <see cref="OrderType"/> supported by Trading 212.
        /// </summary>
        /// <remarks>
        /// Trading 212's public API accepts MARKET, LIMIT, STOP and STOP_LIMIT orders. ETFs are
        /// equities in LEAN, so <see cref="SecurityType.Equity"/> covers the broker's US-listed
        /// shares and ETFs.
        /// </remarks>
        private readonly Dictionary<SecurityType, HashSet<OrderType>> _supportOrderTypeBySecurityType = new()
        {
            { SecurityType.Equity, new HashSet<OrderType> { OrderType.Market, OrderType.Limit, OrderType.StopMarket, OrderType.StopLimit } }
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="Trading212BrokerageModel"/> class
        /// </summary>
        /// <param name="accountType">The type of account to be modelled, defaults to
        /// <see cref="QuantConnect.AccountType.Cash"/>, because Trading 212's equity accounts are
        /// cash accounts. <see cref="BrokerageModel.Create"/> forwards the account type it is
        /// given, so <c>SetBrokerageModel(BrokerageName.Trading212)</c> without an explicit
        /// <see cref="QuantConnect.AccountType.Cash"/> models a margin account; the model still
        /// returns no leverage and behaves as a cash account either way.</param>
        public Trading212BrokerageModel(AccountType accountType = AccountType.Cash)
            : base(accountType)
        {
        }

        /// <summary>
        /// Gets the brokerage's leverage for the specified security
        /// </summary>
        /// <remarks>
        /// Trading 212's equity accounts are long only and cannot use margin, so leverage is
        /// always 1 even when the model was created with <see cref="QuantConnect.AccountType.Margin"/>.
        /// The leveraged CFD product is a separate account type and is not modelled here.
        /// </remarks>
        /// <param name="security">The security's whose leverage we seek</param>
        /// <returns>The leverage for the specified security</returns>
        public override decimal GetLeverage(Security security)
        {
            return 1m;
        }

        /// <summary>
        /// Returns true if the brokerage could accept this order. This takes into account the
        /// security type, the order type, contingent orders, and whether the order would cross zero holdings.
        /// </summary>
        /// <param name="security">The security of the order</param>
        /// <param name="order">The order to be processed</param>
        /// <param name="message">If this function returns false, a brokerage message detailing why the order may not be submitted</param>
        /// <returns>True if the brokerage could process the order, false otherwise</returns>
        public override bool CanSubmitOrder(Security security, Order order, out BrokerageMessageEvent message)
        {
            // Trading 212 does not support contingent orders (OCO, OTO, OUO, brackets).
            if (!this.ValidateContingentOrdersNotSupported(order, out message))
            {
                return false;
            }

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

            // Trading 212 is long only. Reject any sell that would leave the account short: an over-sell
            // from a long position, a sell from flat, or a sell while already short. Testing the resulting
            // position gives every long-only violation the same message instead of letting an over-sell
            // fall through to the generic cross-zero message.
            if (order.Quantity < 0 && security.Holdings.Quantity + order.Quantity < 0)
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                    Messages.Trading212BrokerageModel.ShortSellingNotSupported(order));
                return false;
            }

            // Keep the cross-zero rule for any remaining supported order that would flip a position
            // through zero. Sells that cross zero are already rejected by the long-only check above.
            if (!BrokerageExtensions.ValidateCrossZeroOrder(this, security, order, out message, supportOrderTypes))
            {
                return false;
            }

            return base.CanSubmitOrder(security, order, out message);
        }
    }
}
