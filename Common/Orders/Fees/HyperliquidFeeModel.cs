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
using QuantConnect.Securities;

namespace QuantConnect.Orders.Fees
{
    /// <summary>
    /// Represents a fee model specific to Hyperliquid DEX.
    /// </summary>
    /// <remarks>
    /// Hyperliquid fee structure:
    /// - Maker fee: 0.01% (0.0001)
    /// - Taker fee: 0.035% (0.00035)
    /// - Fees are charged in USDC
    /// </remarks>
    public class HyperliquidFeeModel : FeeModel
    {
        /// <summary>
        /// Default maker fee (0.01%)
        /// </summary>
        public const decimal DefaultMakerFee = 0.0001m;

        /// <summary>
        /// Default taker fee (0.035%)
        /// </summary>
        public const decimal DefaultTakerFee = 0.00035m;

        private readonly decimal _makerFee;
        private readonly decimal _takerFee;

        /// <summary>
        /// Initializes a new instance of the <see cref="HyperliquidFeeModel"/> class
        /// </summary>
        /// <param name="makerFee">The maker fee percentage (default 0.01%)</param>
        /// <param name="takerFee">The taker fee percentage (default 0.035%)</param>
        public HyperliquidFeeModel(decimal makerFee = DefaultMakerFee, decimal takerFee = DefaultTakerFee)
        {
            _makerFee = makerFee;
            _takerFee = takerFee;
        }

        /// <summary>
        /// Gets the order fee for a Hyperliquid order.
        /// </summary>
        /// <param name="parameters">The order fee parameters containing security and order info</param>
        /// <returns>The order fee in USDC</returns>
        public override OrderFee GetOrderFee(OrderFeeParameters parameters)
        {
            if (parameters == null)
            {
                throw new ArgumentNullException(nameof(parameters), "The 'parameters' argument cannot be null.");
            }

            var order = parameters.Order;
            var security = parameters.Security;

            // Determine if this is a maker or taker order
            var isMaker = order.Type == OrderType.Limit && !order.IsMarketable;
            var feeRate = isMaker ? _makerFee : _takerFee;

            // Get the notional value
            var unitPrice = order.Direction == OrderDirection.Buy ? security.AskPrice : security.BidPrice;
            var notionalValue = unitPrice * Math.Abs(order.Quantity);

            // Calculate fee
            var fee = notionalValue * feeRate;

            return new OrderFee(new CashAmount(fee, "USDC"));
        }
    }
}
