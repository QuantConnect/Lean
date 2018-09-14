using QuantConnect.Interfaces;

namespace QuantConnect.Orders
{
    /// <summary>
    /// Contains additional properties and settings for an order submitted to Binance brokerage
    /// </summary>
    public class BinanceOrderProperties : OrderProperties
    {
        /// <summary>
        /// This flag will ensure the order executes only as a maker (no fee) order.
        /// If part of the order results in taking liquidity rather than providing,
        /// it will be rejected and no part of the order will execute.
        /// Note: this flag is only applied to Limit orders.
        /// </summary>
        public bool PostOnly { get; set; }

        /// <summary>
        /// Returns a new instance clone of this object
        /// </summary>
        public override IOrderProperties Clone()
        {
            return (BinanceOrderProperties)MemberwiseClone();
        }
    }
}
