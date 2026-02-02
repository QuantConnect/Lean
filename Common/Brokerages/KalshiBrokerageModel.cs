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
using QuantConnect.Securities;
using QuantConnect.Util;

namespace QuantConnect.Brokerages
{
    /// <summary>
    /// Represents a brokerage model for interacting with the Kalshi prediction market exchange.
    /// Kalshi is a CFTC-regulated prediction market for binary outcome contracts.
    /// </summary>
    /// <remarks>
    /// Kalshi key characteristics:
    /// - Binary contracts priced 0-100 cents (settle at $0 or $1)
    /// - Cash account only (no margin)
    /// - Supports Limit and Market orders
    /// - Trading hours vary by contract series
    /// </remarks>
    public class KalshiBrokerageModel : DefaultBrokerageModel
    {
        /// <summary>
        /// Supported order types for Kalshi
        /// </summary>
        private readonly HashSet<OrderType> _supportedOrderTypes = new()
        {
            OrderType.Limit,
            OrderType.Market
        };

        /// <summary>
        /// Gets a map of the default markets to be used for each security type
        /// </summary>
        public override IReadOnlyDictionary<SecurityType, string> DefaultMarkets
        {
            get
            {
                var map = DefaultMarketMap.ToDictionary();
                map[SecurityType.PredictionMarket] = Market.Kalshi;
                return map.ToReadOnlyDictionary();
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KalshiBrokerageModel"/> class
        /// </summary>
        /// <param name="accountType">The type of account to be modeled, defaults to Cash</param>
        /// <remarks>Kalshi only supports cash accounts - no margin trading</remarks>
        public KalshiBrokerageModel(AccountType accountType = AccountType.Cash)
            : base(accountType)
        {
            if (accountType == AccountType.Margin)
            {
                throw new ArgumentException(
                    "Kalshi does not support margin accounts. Please use AccountType.Cash.",
                    nameof(accountType));
            }
        }

        /// <summary>
        /// Gets the leverage for Kalshi securities (always 1x, no margin)
        /// </summary>
        /// <param name="security">The security to get leverage for</param>
        /// <returns>1 (no leverage)</returns>
        public override decimal GetLeverage(Security security)
        {
            return 1m;
        }

        /// <summary>
        /// Gets the benchmark for this brokerage model
        /// </summary>
        /// <param name="securities">The security manager</param>
        /// <returns>A constant benchmark (no standard benchmark for prediction markets)</returns>
        public override IBenchmark GetBenchmark(SecurityManager securities)
        {
            // No standard benchmark for prediction markets
            return new FuncBenchmark(dt => 0m);
        }

        /// <summary>
        /// Gets the fee model for Kalshi
        /// </summary>
        /// <param name="security">The security</param>
        /// <returns>The Kalshi fee model</returns>
        public override IFeeModel GetFeeModel(Security security)
        {
            return new KalshiFeeModel();
        }

        /// <summary>
        /// Determines if the brokerage can update an existing order
        /// </summary>
        /// <param name="security">The security of the order</param>
        /// <param name="order">The order to update</param>
        /// <param name="request">The update request</param>
        /// <param name="message">Output message if update is not allowed</param>
        /// <returns>True if order can be updated</returns>
        public override bool CanUpdateOrder(Security security, Order order, UpdateOrderRequest request, out BrokerageMessageEvent message)
        {
            // Kalshi supports order modifications for limit orders
            if (order.Type != OrderType.Limit)
            {
                message = new BrokerageMessageEvent(
                    BrokerageMessageType.Warning,
                    "NotSupported",
                    "Only limit orders can be modified on Kalshi.");
                return false;
            }

            message = null;
            return true;
        }

        /// <summary>
        /// Evaluates whether the brokerage can accept the given order
        /// </summary>
        /// <param name="security">The security of the order</param>
        /// <param name="order">The order to validate</param>
        /// <param name="message">Output message if order cannot be submitted</param>
        /// <returns>True if the order can be submitted</returns>
        public override bool CanSubmitOrder(Security security, Order order, out BrokerageMessageEvent message)
        {
            if (order == null || security == null)
            {
                var parameter = order == null ? nameof(order) : nameof(security);
                throw new ArgumentNullException(parameter,
                    $"{parameter} parameter cannot be null. Please provide a valid {parameter} for submission.");
            }

            // Validate security type
            if (security.Type != SecurityType.PredictionMarket)
            {
                message = new BrokerageMessageEvent(
                    BrokerageMessageType.Warning,
                    "NotSupported",
                    $"Kalshi only supports PredictionMarket securities. Got: {security.Type}");
                return false;
            }

            // Validate order type
            if (!_supportedOrderTypes.Contains(order.Type))
            {
                message = new BrokerageMessageEvent(
                    BrokerageMessageType.Warning,
                    "NotSupported",
                    $"Order type {order.Type} is not supported by Kalshi. Supported types: Limit, Market");
                return false;
            }

            // Validate order size (Kalshi has minimum 1 contract)
            if (Math.Abs(order.Quantity) < 1)
            {
                message = new BrokerageMessageEvent(
                    BrokerageMessageType.Warning,
                    "InvalidQuantity",
                    "Kalshi requires a minimum order size of 1 contract.");
                return false;
            }

            // Kalshi contracts must be traded in whole numbers
            if (order.Quantity != Math.Floor(order.Quantity))
            {
                message = new BrokerageMessageEvent(
                    BrokerageMessageType.Warning,
                    "InvalidQuantity",
                    "Kalshi contracts must be traded in whole numbers (no fractional contracts).");
                return false;
            }

            return base.CanSubmitOrder(security, order, out message);
        }

        /// <summary>
        /// Gets the buying power model for Kalshi (cash only)
        /// </summary>
        /// <param name="security">The security</param>
        /// <returns>Cash buying power model</returns>
        public override IBuyingPowerModel GetBuyingPowerModel(Security security)
        {
            return new CashBuyingPowerModel();
        }
    }
}
