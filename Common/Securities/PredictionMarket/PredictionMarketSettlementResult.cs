/*
 * CASCADELABS.IO
 * Cascade Labs LLC
 */

namespace QuantConnect.Securities.PredictionMarket
{
    /// <summary>
    /// Represents the settlement result of a prediction market contract
    /// </summary>
    public enum PredictionMarketSettlementResult
    {
        /// <summary>
        /// The contract has not yet settled - outcome is unknown
        /// </summary>
        Pending = 0,

        /// <summary>
        /// The contract settled with a "Yes" outcome - pays $1.00 per contract
        /// </summary>
        Yes = 1,

        /// <summary>
        /// The contract settled with a "No" outcome - pays $0.00 per contract
        /// </summary>
        No = 2
    }
}
