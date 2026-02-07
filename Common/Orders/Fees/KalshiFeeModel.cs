/*
 * CASCADELABS.IO
 * Cascade Labs LLC
 */

using System;
using QuantConnect.Securities;

namespace QuantConnect.Orders.Fees
{
    /// <summary>
    /// Represents a fee model specific to Kalshi prediction market exchange.
    /// Kalshi charges fees based on expected earnings with a taker fee structure.
    /// </summary>
    /// <remarks>
    /// Kalshi fee formula: fees = round_up(0.07 × C × P × (1-P))
    /// Where:
    ///   P = price of contract in dollars (0.00 to 1.00, where 50 cents = 0.50)
    ///   C = number of contracts being traded
    ///   round_up = rounds to the next cent
    ///
    /// - Taker fee: 7% of expected earnings (P × (1-P))
    /// - Maker fee: 0% for resting limit orders
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
        /// Kalshi fee formula: fees = round_up(0.07 × C × P × (1-P))
        /// Where:
        ///   P = price of contract in dollars (0.00 to 1.00)
        ///   C = number of contracts being traded
        ///   round_up = rounds to the next cent
        ///
        /// The fee is based on P × (1-P), which represents expected earnings:
        /// the price times the probability of not winning (implied by 1-price).
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
            var price = order.Direction == OrderDirection.Buy ? security.AskPrice : security.BidPrice;

            // Number of contracts
            var contracts = Math.Abs(order.Quantity);

            // Kalshi fee formula: 0.07 × C × P × (1-P)
            // P × (1-P) represents the expected earnings component
            var expectedEarnings = price * (1m - price);
            var fee = feeRate * contracts * expectedEarnings;

            // Round up to the next cent
            fee = Math.Ceiling(fee * 100m) / 100m;

            return new OrderFee(new CashAmount(fee, Currencies.USD));
        }
    }
}
