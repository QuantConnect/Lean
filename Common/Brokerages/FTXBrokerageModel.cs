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

using QuantConnect.Benchmarks;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;
using QuantConnect.Util;
using System.Collections.Generic;

namespace QuantConnect.Brokerages
{
    /// <summary>
    /// FTX Brokerage model
    /// </summary>
    public class FTXBrokerageModel : DefaultBrokerageModel
    {
        private const decimal _defaultLeverage = 3m;

        /// <summary>
        /// Creates an instance of <see cref="FTXBrokerageModel"/> class
        /// </summary>
        /// <param name="accountType">Cash or Margin</param>
        public FTXBrokerageModel(AccountType accountType = AccountType.Margin) : base(accountType)
        {
        }

        /// <summary>
        /// Gets a map of the default markets to be used for each security type
        /// </summary>
        public override IReadOnlyDictionary<SecurityType, string> DefaultMarkets { get; } = GetDefaultMarkets();


        /// <summary>
        /// Gets a new buying power model for the security, returning the default model with the security's configured leverage.
        /// For cash accounts, leverage = 1 is used.
        /// For margin trading, FTX supports up to 20x leverage, default is 3xs 
        /// </summary>
        /// <param name="security">The security to get a buying power model for</param>
        /// <returns>The buying power model for this brokerage/security</returns>
        public override IBuyingPowerModel GetBuyingPowerModel(Security security)
        {
            return AccountType == AccountType.Margin
                ? new SecurityMarginModel(GetLeverage(security))
                : new CashBuyingPowerModel();
        }

        /// <summary>
        /// Gets the brokerage's leverage for the specified security
        /// </summary>
        /// <param name="security">The security's whose leverage we seek</param>
        /// <returns>The leverage for the specified security</returns>
        public override decimal GetLeverage(Security security)
        {
            if (AccountType == AccountType.Cash)
            {
                return 1m;
            }

            return _defaultLeverage;
        }

        /// <summary>
        /// Provides FTX fee model
        /// </summary>
        /// <param name="security">The security to get a fee model for</param>
        /// <returns>The new fee model for this brokerage</returns>
        public override IFeeModel GetFeeModel(Security security)
            => new FTXFeeModel();

        /// <summary>
        /// Get the benchmark for this model
        /// </summary>
        /// <param name="securities">SecurityService to create the security with if needed</param>
        /// <returns>The benchmark for this brokerage</returns>
        public override IBenchmark GetBenchmark(SecurityManager securities)
        {
            var symbol = Symbol.Create("BTCUSD", SecurityType.Crypto, Market.FTX);
            return SecurityBenchmark.CreateInstance(securities, symbol);
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
            if (!IsValidOrderSize(security, order.Quantity, out message))
            {
                return false;
            }

            message = null;

            if (order.Type is OrderType.StopMarket or OrderType.StopLimit)
            {
                if (!security.HasData)
                {
                    message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                        "There is no data for this symbol yet, please check the security.HasData flag to ensure there is at least one data point."
                    );

                    return false;
                }

                var stopPrice = (order as StopMarketOrder)?.StopPrice;
                if (!stopPrice.HasValue)
                {
                    stopPrice = (order as StopLimitOrder)?.StopPrice;
                }

                switch (order.Direction)
                {
                    case OrderDirection.Sell:
                        if (stopPrice > security.BidPrice)
                        {
                            message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                                StringExtensions.Invariant($"Trigger price too high: must be below current market price.")
                            );
                        }
                        break;

                    case OrderDirection.Buy:
                        if (stopPrice < security.AskPrice)
                        {
                            message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                                StringExtensions.Invariant($"Trigger price too low: must be above current market price.")
                            );
                        }
                        break;
                }

                if (message != null)
                {
                    return false;
                }
            }

            if (security.Type != SecurityType.Crypto)
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                    StringExtensions.Invariant($"The {nameof(FTXBrokerageModel)} does not support {security.Type} security type.")
                );

                return false;
            }
            return base.CanSubmitOrder(security, order, out message);
        }

        /// <summary>
        /// Please note that the order's queue priority will be reset, and the order ID of the modified order will be different from that of the original order.
        /// Also note: this is implemented as cancelling and replacing your order.
        /// There's a chance that the order meant to be cancelled gets filled and its replacement still gets placed.
        /// </summary>
        /// <param name="security">The security of the order</param>
        /// <param name="order">The order to be updated</param>
        /// <param name="request">The requested update to be made to the order</param>
        /// <param name="message">If this function returns false, a brokerage message detailing why the order may not be updated</param>
        /// <returns>True if the brokerage would allow updating the order, false otherwise</returns>
        public override bool CanUpdateOrder(Security security, Order order, UpdateOrderRequest request, out BrokerageMessageEvent message)
        {
            message =
                new BrokerageMessageEvent(
                    BrokerageMessageType.Warning,
                    0,
                    "You must cancel and re-create instead.");
            return false;
        }

        private static IReadOnlyDictionary<SecurityType, string> GetDefaultMarkets()
        {
            var map = DefaultMarketMap.ToDictionary();
            map[SecurityType.Crypto] = Market.FTX;
            return map.ToReadOnlyDictionary();
        }
    }
}
