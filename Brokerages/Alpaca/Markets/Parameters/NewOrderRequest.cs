/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.5.5
*/

using System;
using System.Collections.Generic;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    /// <summary>
    /// Encapsulates request parameters for <see cref="AlpacaTradingClient.PostOrderAsync(NewOrderRequest,System.Threading.CancellationToken)"/> call.
    /// </summary>
    public sealed class NewOrderRequest : Validation.IRequest
    {
        /// <summary>
        /// Creates new instance of <see cref="NewOrderRequest"/> object.
        /// </summary>
        /// <param name="symbol">Order asset name.</param>
        /// <param name="quantity">Order quantity.</param>
        /// <param name="side">Order side (buy or sell).</param>
        /// <param name="type">Order type.</param>
        /// <param name="duration">Order duration.</param>
        public NewOrderRequest(
            String symbol,
            Int64 quantity,
            OrderSide side,
            OrderType type,
            TimeInForce duration)
        {
            Symbol = symbol;
            Quantity = quantity;
            Side = side;
            Type = type;
            Duration = duration;
        }

        /// <summary>
        /// Gets the new order asset name.
        /// </summary>
        public String Symbol { get;  }

        /// <summary>
        /// Gets the new order quantity.
        /// </summary>
        public Int64 Quantity { get; }

        /// <summary>
        /// Gets the new order side (buy or sell).
        /// </summary>
        public OrderSide Side { get; }

        /// <summary>
        /// Gets the new order type.
        /// </summary>
        public OrderType Type { get; }

        /// <summary>
        /// Gets the new order duration.
        /// </summary>
        public TimeInForce Duration { get;  }

        /// <summary>
        /// Gets or sets the new order limit price.
        /// </summary>
        public Decimal? LimitPrice { get; set; }

        /// <summary>
        /// Gets or sets the new order stop price.
        /// </summary>
        public Decimal? StopPrice { get; set; }

        /// <summary>
        /// Gets or sets the client order ID.
        /// </summary>
        public String ClientOrderId { get; set; }

        /// <summary>
        /// Gets or sets flag indicating that order should be allowed to execute during extended hours trading.
        /// </summary>
        public Boolean? ExtendedHours { get; set; }

        /// <summary>
        /// Gets or sets the order class for advanced order types.
        /// </summary>
        public OrderClass? OrderClass { get; set; }

        /// <summary>
        /// Gets or sets the profit taking limit price for advanced order types.
        /// </summary>
        public Decimal? TakeProfitLimitPrice { get; set; }

        /// <summary>
        /// Gets or sets the stop loss stop price for advanced order types.
        /// </summary>
        public Decimal? StopLossStopPrice { get; set; }

        /// <summary>
        /// Gets or sets the stop loss limit price for advanced order types.
        /// </summary>
        public Decimal? StopLossLimitPrice { get; set; }

        /// <summary>
        /// Gets or sets flag indicated that child orders should be listed as 'legs' of parent orders.
        /// </summary>
        public Boolean? Nested { get; set; }

        IEnumerable<RequestValidationException> Validation.IRequest.GetExceptions()
        {
            if (ClientOrderId?.Length > 48)
            {
                ClientOrderId = ClientOrderId.Substring(0, 48);
            }

            // TODO: olegra - add more validations here

            yield break;
        }
    }
}
