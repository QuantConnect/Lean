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
using static QuantConnect.StringExtensions;

namespace QuantConnect.Brokerages
{
    /// <summary>
    /// Provides properties specific to Trading Technologies
    /// </summary>
    public class TradingTechnologiesBrokerageModel : DefaultBrokerageModel
    {
        /// <summary>
        /// The default markets for Trading Technologies
        /// </summary>
        public new static readonly IReadOnlyDictionary<SecurityType, string> DefaultMarketMap = new Dictionary<SecurityType, string>
        {
            {SecurityType.Future, Market.CME}
        }.ToReadOnlyDictionary();

        private readonly Type[] _supportedTimeInForces =
        {
            typeof(GoodTilCanceledTimeInForce),
            typeof(DayTimeInForce)
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="TradingTechnologiesBrokerageModel"/> class
        /// </summary>
        /// <param name="accountType">The type of account to be modelled, defaults to
        /// <see cref="AccountType.Margin"/></param>
        public TradingTechnologiesBrokerageModel(AccountType accountType = AccountType.Margin)
            : base(accountType)
        {
        }

        /// <summary>
        /// Gets a map of the default markets to be used for each security type
        /// </summary>
        public override IReadOnlyDictionary<SecurityType, string> DefaultMarkets => DefaultMarketMap;

        /// <summary>
        /// Get the benchmark for this model
        /// </summary>
        /// <param name="securities">SecurityService to create the security with if needed</param>
        /// <returns>The benchmark for this brokerage</returns>
        public override IBenchmark GetBenchmark(SecurityManager securities)
        {
            // Equivalent to no benchmark
            return new FuncBenchmark(x => 0);
        }

        /// <summary>
        /// Gets a new fee model that represents this brokerage's fee structure
        /// </summary>
        /// <param name="security">The security to get a fee model for</param>
        /// <returns>The new fee model for this brokerage</returns>
        public override IFeeModel GetFeeModel(Security security)
        {
            return new ConstantFeeModel(0);
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
            message = null;

            // validate security type
            if (security.Type != SecurityType.Future)
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                    Invariant($"The {nameof(TradingTechnologiesBrokerageModel)} does not support {security.Type} security type.")
                );

                return false;
            }

            // validate order type
            if (order.Type != OrderType.Limit && order.Type != OrderType.Market && order.Type != OrderType.StopMarket && order.Type != OrderType.StopLimit)
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                    Invariant($"The {nameof(InteractiveBrokersBrokerageModel)} does not support {order.Type} order type.")
                );

                return false;
            }

            // validate time in force
            if (!_supportedTimeInForces.Contains(order.TimeInForce.GetType()))
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                    Invariant($"The {nameof(TradingTechnologiesBrokerageModel)} does not support {order.TimeInForce.GetType().Name} time in force.")
                );

                return false;
            }

            // validate stop orders prices
            var stopMarket = order as StopMarketOrder;
            if (stopMarket != null)
            {
                return IsValidOrderPrices(security, OrderType.StopMarket, stopMarket.Direction, stopMarket.StopPrice, security.Price, ref message);
            }

            var stopLimit = order as StopLimitOrder;
            if (stopLimit != null)
            {
                return IsValidOrderPrices(security, OrderType.StopMarket, stopLimit.Direction, stopLimit.StopPrice, stopLimit.LimitPrice, ref message);
            }

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

            return true;
        }

        /// <summary>
        /// Returns true if the brokerage would be able to execute this order at this time assuming
        /// market prices are sufficient for the fill to take place. This is used to emulate the
        /// brokerage fills in backtesting and paper trading. For example some brokerages may not perform
        /// executions during extended market hours. This is not intended to be checking whether or not
        /// the exchange is open, that is handled in the Security.Exchange property.
        /// </summary>
        /// <param name="security"></param>
        /// <param name="order">The order to test for execution</param>
        /// <returns>True if the brokerage would be able to perform the execution, false otherwise</returns>
        public override bool CanExecuteOrder(Security security, Order order)
        {
            return order.SecurityType == SecurityType.Future;
        }

        /// <summary>
        /// Validates stopmarket/stoplimit order prices, pass security.Price for limit/stop if n/a
        /// </summary>
        private static bool IsValidOrderPrices(
            Security security,
            OrderType orderType,
            OrderDirection orderDirection,
            decimal stopPrice,
            decimal limitPrice,
            ref BrokerageMessageEvent message
            )
        {
            // validate stop market order prices
            if (orderType == OrderType.StopMarket &&
                (orderDirection == OrderDirection.Buy && stopPrice <= security.Price ||
                    orderDirection == OrderDirection.Sell && stopPrice >= security.Price))
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                    "StopMarket Sell orders must be below market, StopMarket Buy orders must be above market."
                );

                return false;
            }

            // validate stop limit order prices
            if (orderType == OrderType.StopLimit)
            {
                if (orderDirection == OrderDirection.Buy && stopPrice <= security.Price ||
                    orderDirection == OrderDirection.Sell && stopPrice >= security.Price)
                {
                    message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                        "StopLimit Sell orders must be below market, StopLimit Buy orders must be above market."
                    );

                    return false;
                }

                if (orderDirection == OrderDirection.Buy && limitPrice < stopPrice ||
                    orderDirection == OrderDirection.Sell && limitPrice > stopPrice)
                {
                    message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                        "StopLimit Buy limit price must be greater than or equal to stop price, StopLimit Sell limit price must be smaller than or equal to stop price."
                    );

                    return false;
                }
            }

            return true;
        }
    }
}
