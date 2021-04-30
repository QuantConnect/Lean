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
using static QuantConnect.StringExtensions;

namespace QuantConnect.Brokerages
{
    /// <summary>
    /// Provides FXCM specific properties
    /// </summary>
    public class FxcmBrokerageModel : DefaultBrokerageModel
    {
        /// <summary>
        /// The default markets for the fxcm brokerage
        /// </summary>
        public new static readonly IReadOnlyDictionary<SecurityType, string> DefaultMarketMap = new Dictionary<SecurityType, string>
        {
            {SecurityType.Base, Market.USA},
            {SecurityType.Equity, Market.USA},
            {SecurityType.Option, Market.USA},
            {SecurityType.Forex, Market.FXCM},
            {SecurityType.Cfd, Market.FXCM}
        }.ToReadOnlyDictionary();

        /// <summary>
        /// Gets a map of the default markets to be used for each security type
        /// </summary>
        public override IReadOnlyDictionary<SecurityType, string> DefaultMarkets => DefaultMarketMap;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultBrokerageModel"/> class
        /// </summary>
        /// <param name="accountType">The type of account to be modelled, defaults to
        /// <see cref="AccountType.Margin"/></param>
        public FxcmBrokerageModel(AccountType accountType = AccountType.Margin)
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
                    Invariant($"The {nameof(FxcmBrokerageModel)} does not support {security.Type} security type.")
                );

                return false;
            }

            // validate order type
            if (order.Type != OrderType.Limit && order.Type != OrderType.Market && order.Type != OrderType.StopMarket)
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                    Invariant($"The {nameof(FxcmBrokerageModel)} does not support {order.Type} order type.")
                );

                return false;
            }

            // validate order quantity
            if (order.Quantity % security.SymbolProperties.LotSize != 0)
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                    Invariant($"The order quantity must be a multiple of LotSize: [{security.SymbolProperties.LotSize}].")
                );

                return false;
            }

            // validate stop/limit orders prices
            var limit = order as LimitOrder;
            if (limit != null)
            {
                return IsValidOrderPrices(security, OrderType.Limit, limit.Direction, security.Price, limit.LimitPrice, ref message);
            }

            var stopMarket = order as StopMarketOrder;
            if (stopMarket != null)
            {
                return IsValidOrderPrices(security, OrderType.StopMarket, stopMarket.Direction, stopMarket.StopPrice, security.Price, ref message);
            }

            // validate time in force
            if (order.TimeInForce != TimeInForce.GoodTilCanceled)
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                    Invariant($"The {nameof(FxcmBrokerageModel)} does not support {order.TimeInForce.GetType().Name} time in force.")
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
        /// <param name="message">If this function returns false, a brokerage message detailing why the order may not be updated</param>
        /// <returns>True if the brokerage would allow updating the order, false otherwise</returns>
        public override bool CanUpdateOrder(Security security, Order order, UpdateOrderRequest request, out BrokerageMessageEvent message)
        {
            message = null;

            // validate order quantity
            if (request.Quantity != null && request.Quantity % security.SymbolProperties.LotSize != 0)
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                    Invariant($"The order quantity must be a multiple of LotSize: [{security.SymbolProperties.LotSize}].")
                );

                return false;
            }

            // determine direction via the new, updated quantity
            var newQuantity = request.Quantity ?? order.Quantity;
            var direction = newQuantity > 0 ? OrderDirection.Buy : OrderDirection.Sell;

            // use security.Price if null, allows to pass checks
            var stopPrice = request.StopPrice ?? security.Price;
            var limitPrice = request.LimitPrice ?? security.Price;

            return IsValidOrderPrices(security, order.Type, direction, stopPrice, limitPrice, ref message);
        }

        /// <summary>
        /// Get the benchmark for this model
        /// </summary>
        /// <param name="securities">SecurityService to create the security with if needed</param>
        /// <returns>The benchmark for this brokerage</returns>
        public override IBenchmark GetBenchmark(SecurityManager securities)
        {
            var symbol = Symbol.Create("EURUSD", SecurityType.Forex, Market.FXCM);
            return SecurityBenchmark.CreateInstance(securities, symbol);
        }

        /// <summary>
        /// Gets a new fee model that represents this brokerage's fee structure
        /// </summary>
        /// <param name="security">The security to get a fee model for</param>
        /// <returns>The new fee model for this brokerage</returns>
        public override IFeeModel GetFeeModel(Security security)
        {
            return new FxcmFeeModel();
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

        /// <summary>
        /// Validates limit/stopmarket order prices, pass security.Price for limit/stop if n/a
        /// </summary>
        private static bool IsValidOrderPrices(Security security, OrderType orderType, OrderDirection orderDirection, decimal stopPrice, decimal limitPrice, ref BrokerageMessageEvent message)
        {
            // validate order price
            var invalidPrice = orderType == OrderType.Limit && orderDirection == OrderDirection.Buy && limitPrice > security.Price ||
                orderType == OrderType.Limit && orderDirection == OrderDirection.Sell && limitPrice < security.Price ||
                orderType == OrderType.StopMarket && orderDirection == OrderDirection.Buy && stopPrice < security.Price ||
                orderType == OrderType.StopMarket && orderDirection == OrderDirection.Sell && stopPrice > security.Price;

            if (invalidPrice)
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                    "Limit Buy orders and Stop Sell orders must be below market, Limit Sell orders and Stop Buy orders must be above market."
                );

                return false;
            }

            // Validate FXCM maximum distance for limit and stop orders:
            // there are two different Max Limits, 15000 pips and 50% rule,
            // whichever comes first (for most pairs, 50% rule comes first)
            var maxDistance = Math.Min(
                // MinimumPriceVariation is 1/10th of a pip
                security.SymbolProperties.MinimumPriceVariation * 10 * 15000,
                security.Price / 2);
            var currentPrice = security.Price;
            var minPrice = currentPrice - maxDistance;
            var maxPrice = currentPrice + maxDistance;

            var outOfRangePrice = orderType == OrderType.Limit && orderDirection == OrderDirection.Buy && limitPrice < minPrice ||
                                  orderType == OrderType.Limit && orderDirection == OrderDirection.Sell && limitPrice > maxPrice ||
                                  orderType == OrderType.StopMarket && orderDirection == OrderDirection.Buy && stopPrice > maxPrice ||
                                  orderType == OrderType.StopMarket && orderDirection == OrderDirection.Sell && stopPrice < minPrice;

            if (outOfRangePrice)
            {
                var orderPrice = orderType == OrderType.Limit ? limitPrice : stopPrice;

                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                    Invariant($"The {orderType} {orderDirection} order price ({orderPrice}) is too far from the current market price ({currentPrice}).")
                );

                return false;
            }

            return true;
        }
    }
}
