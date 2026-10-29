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
using QuantConnect.Orders.TimeInForces;
using QuantConnect.Securities;
using System;
using System.Collections.Generic;

namespace QuantConnect.Brokerages
{
    /// <summary>
    /// Represents a brokerage model specific to Trading 212.
    /// </summary>
    /// <remarks>
    /// Trading 212 is commission-free and long only: the broker's equity accounts (Invest,
    /// Stocks ISA and SIPP) do not offer short selling or margin, and its public API accepts
    /// market, limit, stop and stop-limit orders for equities, with a DAY or GOOD_TILL_CANCEL time in force.
    /// <see cref="OrderType.MarketOnOpen"/> is accepted as well: a market order submitted while
    /// the market is closed is queued by Trading 212 and executed at the open, which is exactly
    /// what Lean's MarketOnOpen order models, and Lean converts such a market order into one
    /// before the brokerage model sees it. <see cref="OrderType.MarketOnClose"/> is not accepted,
    /// because Trading 212 has no order that fills at the close. The leveraged CFD product is a
    /// separate account type and is out of scope for this model.
    /// Sale proceeds settle immediately for equities, see <see cref="GetSettlementModel"/>.
    /// The long-only rule is evaluated against the security's holdings when the order is submitted,
    /// matching the other LEAN brokerage models, so it does not account for other pending unfilled orders.
    /// The broker rejects an over-sell with <c>SellingEquityNotOwned</c>, so a same-slice duplicate is
    /// not accepted as a short in a live account.
    /// </remarks>
    public class Trading212BrokerageModel : DefaultBrokerageModel
    {
        /// <summary>
        /// A dictionary that maps each supported <see cref="SecurityType"/> to the set of
        /// <see cref="OrderType"/> supported by Trading 212.
        /// </summary>
        /// <remarks>
        /// Trading 212's public API accepts MARKET, LIMIT, STOP and STOP_LIMIT orders, and a market
        /// order placed outside trading hours is executed at the open, so MARKET_ON_OPEN is accepted
        /// to model it. ETFs are equities in LEAN, so <see cref="SecurityType.Equity"/> covers the
        /// broker's US-listed shares and ETFs.
        /// </remarks>
        private readonly Dictionary<SecurityType, HashSet<OrderType>> _supportOrderTypeBySecurityType = new()
        {
            { SecurityType.Equity, new HashSet<OrderType> { OrderType.Market, OrderType.MarketOnOpen, OrderType.Limit, OrderType.StopMarket, OrderType.StopLimit } }
        };

        /// <summary>
        /// The time in force types accepted by Trading 212's order API
        /// </summary>
        private static readonly HashSet<Type> _supportTimeInForces = new()
        {
            typeof(DayTimeInForce),
            typeof(GoodTilCanceledTimeInForce)
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="Trading212BrokerageModel"/> class
        /// </summary>
        /// <param name="accountType">The type of account to be modelled, defaults to
        /// <see cref="QuantConnect.AccountType.Cash"/>, because Trading 212's equity accounts are
        /// cash accounts. <see cref="BrokerageModel.Create"/> forwards the account type it is
        /// given, so <c>SetBrokerageModel(BrokerageName.Trading212)</c> without an explicit
        /// <see cref="QuantConnect.AccountType.Cash"/> models a margin account. The account type does
        /// not change how this model behaves: leverage is always 1 and equity sale proceeds settle
        /// immediately, whichever account type is modelled.</param>
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
        /// Gets a new fee model that represents this brokerage's fee structure
        /// </summary>
        /// <param name="security">The security to get a fee model for</param>
        /// <returns>The new fee model for this brokerage</returns>
        public override IFeeModel GetFeeModel(Security security)
        {
            return new Trading212FeeModel();
        }

        /// <summary>
        /// Gets a new settlement model for the security
        /// </summary>
        /// <remarks>
        /// Trading 212's Invest Terms clause 8.3 allow sale proceeds to be used before the settlement date
        /// for the purpose of buying more shares; only withdrawals have to wait for settlement
        /// (https://www.trading212.com/legal-documentation/uk/invest/Invest-Terms_EN.pdf). Lean's
        /// settlement model controls buying power, not withdrawals, so equity proceeds settle immediately.
        /// This is returned for every account type because <see cref="BrokerageModel.Create"/> passes
        /// <see cref="QuantConnect.AccountType.Margin"/> when the model is selected with
        /// <c>SetBrokerageModel(BrokerageName.Trading212)</c> while the default constructor uses
        /// <see cref="QuantConnect.AccountType.Cash"/>: inheriting the account type based settlement would
        /// make settlement depend on how the model was selected.
        /// </remarks>
        /// <param name="security">The security to get a settlement model for</param>
        /// <returns>The settlement model for this brokerage</returns>
        public override ISettlementModel GetSettlementModel(Security security)
        {
            if (security.Type == SecurityType.Equity)
            {
                return new ImmediateSettlementModel();
            }

            return base.GetSettlementModel(security);
        }

        /// <summary>
        /// Returns true if the brokerage could accept this order. This takes into account the
        /// security type, the order type, the time in force, contingent orders, and whether the order
        /// would leave the account short.
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

            // Trading 212's order API only accepts DAY and GOOD_TILL_CANCEL orders.
            if (!_supportTimeInForces.Contains(order.TimeInForce.GetType()))
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                    Messages.DefaultBrokerageModel.UnsupportedTimeInForce(this, order));
                return false;
            }

            // Trading 212 is long only. Reject any sell that would leave the account short: an over-sell
            // from a long position, a sell from flat, or a sell while already short. Testing the resulting
            // position gives every long-only violation the same message and lets a buy that covers a short
            // position through.
            if (order.Quantity < 0 && security.Holdings.Quantity + order.Quantity < 0)
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                    Messages.Trading212BrokerageModel.ShortSellingNotSupported(order));
                return false;
            }

            return base.CanSubmitOrder(security, order, out message);
        }
    }
}
