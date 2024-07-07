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
using System.Linq;
using QuantConnect.Benchmarks;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Orders.TimeInForces;
using QuantConnect.Securities;
using QuantConnect.Util;

namespace QuantConnect.Brokerages
{
    /// <summary>
    /// Brokerage Model implementation for Samco
    /// </summary>
    public class SamcoBrokerageModel : DefaultBrokerageModel
    {
        private readonly HashSet<Type> _supportedTimeInForces =
            new()
            {
                typeof(GoodTilCanceledTimeInForce),
                typeof(DayTimeInForce),
                typeof(GoodTilDateTimeInForce)
            };

        private readonly HashSet<OrderType> _supportedOrderTypes =
            new() { OrderType.Market, OrderType.Limit, OrderType.StopMarket };

        private const decimal _maxLeverage = 5m;

        /// <summary>
        /// Initializes a new instance of the <see cref="SamcoBrokerageModel"/> class
        /// </summary>
        /// <param name="accountType">The type of account to be modelled, defaults to <see cref="AccountType.Margin"/></param>
        public SamcoBrokerageModel(AccountType accountType = AccountType.Margin)
            : base(accountType) { }

        /// <summary>
        /// Returns true if the brokerage would be able to execute this order at this time assuming
        /// market prices are sufficient for the fill to take place. This is used to emulate the
        /// brokerage fills in backtesting and paper trading. For example some brokerages may not
        /// perform executions during extended market hours. This is not intended to be checking
        /// whether or not the exchange is open, that is handled in the Security.Exchange property.
        /// </summary>
        /// <param name="security"></param>
        /// <param name="order">The order to test for execution</param>
        /// <returns>True if the brokerage would be able to perform the execution, false otherwise</returns>
        public override bool CanExecuteOrder(Security security, Order order)
        {
            // validate security type
            if (
                security.Type != SecurityType.Equity
                && security.Type != SecurityType.Option
                && security.Type != SecurityType.Future
            )
            {
                return false;
            }

            // validate time in force
            if (!_supportedTimeInForces.Contains(order.TimeInForce.GetType()))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Returns true if the brokerage could accept this order. This takes into account order
        /// type, security type, and order size limits.
        /// </summary>
        /// <remarks>
        /// For example, a brokerage may have no connectivity at certain times, or an order
        /// rate/size limit
        /// </remarks>
        /// <param name="security">The security being ordered</param>
        /// <param name="order">The order to be processed</param>
        /// <param name="message">
        /// If this function returns false, a brokerage message detailing why the order may not be submitted
        /// </param>
        /// <returns>True if the brokerage could process the order, false otherwise</returns>
        public override bool CanSubmitOrder(
            Security security,
            Order order,
            out BrokerageMessageEvent message
        )
        {
            message = null;

            // validate security type
            if (
                security.Type != SecurityType.Equity
                && security.Type != SecurityType.Option
                && security.Type != SecurityType.Future
            )
            {
                message = new BrokerageMessageEvent(
                    BrokerageMessageType.Warning,
                    "NotSupported",
                    Messages.DefaultBrokerageModel.UnsupportedSecurityType(this, security)
                );

                return false;
            }

            // validate time in force
            if (!_supportedTimeInForces.Contains(order.TimeInForce.GetType()))
            {
                message = new BrokerageMessageEvent(
                    BrokerageMessageType.Warning,
                    "NotSupported",
                    Messages.DefaultBrokerageModel.UnsupportedTimeInForce(this, order)
                );

                return false;
            }

            // validate order type
            if (!_supportedOrderTypes.Contains(order.Type))
            {
                message = new BrokerageMessageEvent(
                    BrokerageMessageType.Warning,
                    "NotSupported",
                    Messages.DefaultBrokerageModel.UnsupportedOrderType(
                        this,
                        order,
                        _supportedOrderTypes
                    )
                );

                return false;
            }

            return true;
        }

        /// <summary>
        /// Returns true if the brokerage would allow updating the order as specified by the request
        /// </summary>
        /// <param name="security">The security of the order</param>
        /// <param name="order">The order to be updated</param>
        /// <param name="request">The requested update to be made to the order</param>
        /// <param name="message">
        /// If this function returns false, a brokerage message detailing why the order may not be updated
        /// </param>
        /// <returns>True if the brokerage would allow updating the order, false otherwise</returns>
        public override bool CanUpdateOrder(
            Security security,
            Order order,
            UpdateOrderRequest request,
            out BrokerageMessageEvent message
        )
        {
            message = null;
            return true;
        }

        /// <summary>
        /// Gets a map of the default markets to be used for each security type
        /// </summary>
        public override IReadOnlyDictionary<SecurityType, string> DefaultMarkets { get; } =
            GetDefaultMarkets();

        /// <summary>
        /// Samco global leverage rule
        /// </summary>
        /// <param name="security"></param>
        /// <returns></returns>
        public override decimal GetLeverage(Security security)
        {
            if (
                AccountType == AccountType.Cash
                || security.IsInternalFeed()
                || security.Type == SecurityType.Base
            )
            {
                return 1m;
            }

            if (
                security.Type == SecurityType.Equity
                || security.Type == SecurityType.Future
                || security.Type == SecurityType.Option
                || security.Type == SecurityType.Index
            )
            {
                return _maxLeverage;
            }

            throw new ArgumentException(
                Messages.DefaultBrokerageModel.InvalidSecurityTypeForLeverage(security),
                nameof(security)
            );
        }

        /// <summary>
        /// Get the benchmark for this model
        /// </summary>
        /// <param name="securities">SecurityService to create the security with if needed</param>
        /// <returns>The benchmark for this brokerage</returns>
        public override IBenchmark GetBenchmark(SecurityManager securities)
        {
            var symbol = Symbol.Create("NIFTYBEES", SecurityType.Equity, Market.India);
            return SecurityBenchmark.CreateInstance(securities, symbol);
        }

        /// <summary>
        /// Provides Samco fee model
        /// </summary>
        /// <param name="security"></param>
        /// <returns></returns>
        public override IFeeModel GetFeeModel(Security security)
        {
            return new SamcoFeeModel();
        }

        private static IReadOnlyDictionary<SecurityType, string> GetDefaultMarkets()
        {
            var map = DefaultMarketMap.ToDictionary();
            map[SecurityType.Equity] = Market.India;
            map[SecurityType.Future] = Market.India;
            map[SecurityType.Option] = Market.India;
            return map.ToReadOnlyDictionary();
        }
    }
}
