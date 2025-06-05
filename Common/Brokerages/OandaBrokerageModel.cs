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
using QuantConnect.Benchmarks;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Orders.Slippage;
using QuantConnect.Securities;
using QuantConnect.Util;

namespace QuantConnect.Brokerages
{
    /// <summary>
    /// Oanda Brokerage Model Implementation for Back Testing.
    /// </summary>
    public class OandaBrokerageModel : DefaultBrokerageModel
    {
        /// <summary>
        /// The default markets for the fxcm brokerage
        /// </summary>
        public new static readonly IReadOnlyDictionary<SecurityType, string> DefaultMarketMap = new Dictionary<SecurityType, string>
        {
            {SecurityType.Base, Market.USA},
            {SecurityType.Equity, Market.USA},
            {SecurityType.Option, Market.USA},
            {SecurityType.Forex, Market.Oanda},
            {SecurityType.Cfd, Market.Oanda}
        }.ToReadOnlyDictionary();

        private readonly HashSet<OrderType> _supportedOrderTypes = new()
        {
            OrderType.Limit,
            OrderType.Market,
            OrderType.StopMarket,
            OrderType.StopLimit
        };

        /// <summary>
        /// Gets a map of the default markets to be used for each security type
        /// </summary>
        public override IReadOnlyDictionary<SecurityType, string> DefaultMarkets => DefaultMarketMap;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultBrokerageModel"/> class
        /// </summary>
        /// <param name="accountType">The type of account to be modelled, defaults to
        /// <see cref="AccountType.Margin"/></param>
        public OandaBrokerageModel(AccountType accountType = AccountType.Margin)
            : base(accountType)
        {
            if (accountType == AccountType.Cash)
            {
                throw new InvalidOperationException($"Oanda brokerage can only be used with a {AccountType.Margin} account type");
            }
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
            if (security.Type != SecurityType.Forex && security.Type != SecurityType.Cfd)
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                    Messages.DefaultBrokerageModel.UnsupportedSecurityType(this, security));

                return false;
            }

            // validate order type
            if (!_supportedOrderTypes.Contains(order.Type))
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                    Messages.DefaultBrokerageModel.UnsupportedOrderType(this, order, _supportedOrderTypes));

                return false;
            }

            // validate time in force
            if (order.TimeInForce != TimeInForce.GoodTilCanceled)
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                    Messages.DefaultBrokerageModel.UnsupportedTimeInForce(this, order));

                return false;
            }

            return true;
        }

        /// <summary>
        /// Get the benchmark for this model
        /// </summary>
        /// <param name="securities">SecurityService to create the security with if needed</param>
        /// <returns>The benchmark for this brokerage</returns>
        public override IBenchmark GetBenchmark(SecurityManager securities)
        {
            var symbol = Symbol.Create("EURUSD", SecurityType.Forex, Market.Oanda);
            return SecurityBenchmark.CreateInstance(securities, symbol);
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
        /// Gets a new settlement model for the security
        /// </summary>
        /// <param name="security">The security to get a settlement model for</param>
        /// <returns>The settlement model for this brokerage</returns>
        public override ISettlementModel GetSettlementModel(Security security)
        {
            return security.Type == SecurityType.Cfd
                ? new AccountCurrencyImmediateSettlementModel() :
                (ISettlementModel)new ImmediateSettlementModel();
        }
    }
}
