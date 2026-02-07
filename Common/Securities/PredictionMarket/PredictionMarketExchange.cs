/*
 * CASCADELABS.IO
 * Cascade Labs LLC
 */

namespace QuantConnect.Securities.PredictionMarket
{
    /// <summary>
    /// Prediction Market exchange class - information and helper tools for prediction market exchange properties
    /// </summary>
    /// <seealso cref="SecurityExchange"/>
    public class PredictionMarketExchange : SecurityExchange
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PredictionMarketExchange"/> class using market hours
        /// derived from the market-hours-database for the prediction market
        /// </summary>
        public PredictionMarketExchange(string market)
            : base(MarketHoursDatabase.FromDataFolder().GetExchangeHours(market, null, SecurityType.PredictionMarket))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PredictionMarketExchange"/> class using the specified
        /// exchange hours to determine open/close times
        /// </summary>
        /// <param name="exchangeHours">Contains the weekly exchange schedule plus holidays</param>
        public PredictionMarketExchange(SecurityExchangeHours exchangeHours)
            : base(exchangeHours)
        {
        }
    }
}
