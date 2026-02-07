/*
 * CASCADELABS.IO
 * Cascade Labs LLC
 */

namespace QuantConnect.Securities.PredictionMarket
{
    /// <summary>
    /// Prediction Market holdings implementation of the base securities class
    /// </summary>
    /// <remarks>
    /// Prediction market contracts have a price between 0 and 1 (representing $0.00-$1.00)
    /// and settle at either $0 or $1. Each contract represents a $1 max payout.
    /// Position value = quantity * current price (0-1 range)
    /// </remarks>
    /// <seealso cref="SecurityHolding"/>
    public class PredictionMarketHolding : SecurityHolding
    {
        /// <summary>
        /// Prediction Market Holding Class constructor
        /// </summary>
        /// <param name="security">The prediction market security being held</param>
        /// <param name="currencyConverter">A currency converter instance</param>
        public PredictionMarketHolding(Security security, ICurrencyConverter currencyConverter)
            : base(security, currencyConverter)
        {
        }
    }
}
