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
    /// Represents a fee model specific to Kalshi prediction market exchange.
    /// Kalshi charges fees based on contract value with a taker fee structure.
    /// </summary>
    /// <remarks>
    /// Kalshi fee structure (as of 2024):
    /// - Taker fee: 7% of potential profit (capped at contract price)
    /// - No maker fees for resting limit orders
    /// - Fees are charged in USD
    /// See: https://kalshi.com/docs/kalshi-fee-schedule.pdf
    /// </remarks>
    public class KalshiFeeModel : FeeModel
    {
        /// <summary>
        /// Default taker fee percentage (7% of potential profit)
        /// </summary>
        public const decimal DefaultTakerFee = 0.07m;

        /// <summary>
        /// Default maker fee (currently 0 for resting limit orders)
        /// </summary>
        public const decimal DefaultMakerFee = 0m;

        private readonly decimal _takerFee;
        private readonly decimal _makerFee;

        /// <summary>
        /// Initializes a new instance of the <see cref="KalshiFeeModel"/> class
        /// </summary>
        /// <param name="takerFee">The taker fee percentage (default 7%)</param>
        /// <param name="makerFee">The maker fee percentage (default 0%)</param>
        public KalshiFeeModel(decimal takerFee = DefaultTakerFee, decimal makerFee = DefaultMakerFee)
        {
            _takerFee = takerFee;
            _makerFee = makerFee;
        }

        /// <summary>
        /// Gets the order fee for a Kalshi prediction market order.
        /// </summary>
        /// <param name="parameters">The order fee parameters containing security and order info</param>
        /// <returns>The order fee in USD</returns>
        /// <remarks>
        /// Kalshi contracts are priced 0-100 cents ($0.00 to $1.00).
        /// Fee is calculated as: fee_rate * min(price, 100 - price) * quantity
        /// This represents the fee on potential profit (the lesser of YES or NO side value).
        /// </remarks>
        public override OrderFee GetOrderFee(OrderFeeParameters parameters)
        {
            if (parameters == null)
            {
                throw new ArgumentNullException(nameof(parameters), "The 'parameters' argument cannot be null.");
            }

            var order = parameters.Order;
            var security = parameters.Security;

            // Determine if this is a maker or taker order
            // Limit orders that don't cross the spread are maker orders
            var isMaker = order.Type == OrderType.Limit && !order.IsMarketable;
            var feeRate = isMaker ? _makerFee : _takerFee;

            // Get the contract price (0-100 cents, represented as 0.00-1.00 in LEAN)
            var unitPrice = order.Direction == OrderDirection.Buy ? security.AskPrice : security.BidPrice;

            // Kalshi fee is based on potential profit, which is min(price, 1-price)
            // For a YES contract at $0.70, potential profit is $0.30 (if it settles YES)
            // For a YES contract at $0.30, potential profit is $0.30 (if it settles YES)
            var potentialProfit = Math.Min(unitPrice, 1m - unitPrice);

            // Calculate fee: fee_rate * potential_profit * quantity
            var fee = feeRate * potentialProfit * Math.Abs(order.Quantity);

            return new OrderFee(new CashAmount(fee, Currencies.USD));
        }
    }
}
