/*
 * CASCADELABS.IO
 * Cascade Labs LLC
 */

using QuantConnect.Logging;
using QuantConnect.Orders;

namespace QuantConnect.Securities.PredictionMarket
{
    /// <summary>
    /// Portfolio model for prediction market securities that handles binary settlement
    /// </summary>
    /// <remarks>
    /// Core settlement logic:
    /// - Detects settlement fills (order tag = "Liquidate from delisting")
    /// - Overrides fill.FillPrice with the binary settlement price ($1 for Yes, $0 for No)
    /// - Delegates to base.ProcessFill() for standard P&L and cash accounting
    ///
    /// P&L examples:
    /// - Bought at $0.60, result=Yes → profit = ($1.00 - $0.60) × quantity
    /// - Bought at $0.60, result=No → profit = ($0.00 - $0.60) × quantity (total loss)
    /// - Short at $0.60, result=Yes → loss = ($1.00 - $0.60) × quantity
    /// - Short at $0.60, result=No → profit = ($0.60 - $0.00) × quantity
    /// </remarks>
    public class PredictionMarketPortfolioModel : SecurityPortfolioModel
    {
        /// <summary>
        /// The order tag used to identify settlement/delisting liquidation orders
        /// </summary>
        public const string DelistingOrderTag = "Liquidate from delisting";

        /// <summary>
        /// Processes a fill for a prediction market security, handling binary settlement
        /// </summary>
        /// <param name="portfolio">The algorithm's portfolio</param>
        /// <param name="security">The fill's security</param>
        /// <param name="fill">The order event fill object to be applied</param>
        public override void ProcessFill(SecurityPortfolioManager portfolio, Security security, OrderEvent fill)
        {
            // Check if this is a settlement fill (delisting liquidation)
            if (IsSettlementFill(fill))
            {
                var predictionMarket = security as PredictionMarket;
                if (predictionMarket != null)
                {
                    var settlementPrice = GetSettlementPrice(predictionMarket);

                    // Create a modified fill with the settlement price
                    // The fill price determines the P&L calculation in base.ProcessFill()
                    fill = new OrderEvent(
                        fill.OrderId,
                        fill.Symbol,
                        fill.UtcTime,
                        fill.Status,
                        fill.Direction,
                        settlementPrice,
                        fill.FillQuantity,
                        fill.OrderFee,
                        fill.Message)
                    {
                        Ticket = fill.Ticket
                    };
                }
            }

            base.ProcessFill(portfolio, security, fill);
        }

        /// <summary>
        /// Determines if an order event represents a settlement fill
        /// </summary>
        /// <param name="fill">The order event to check</param>
        /// <returns>True if this is a settlement/delisting liquidation fill</returns>
        private static bool IsSettlementFill(OrderEvent fill)
        {
            // Check if the order ticket has the delisting tag
            return fill.Ticket?.Tag == DelistingOrderTag;
        }

        /// <summary>
        /// Gets the binary settlement price based on the market's settlement result
        /// </summary>
        /// <param name="predictionMarket">The prediction market security</param>
        /// <returns>$1.00 for Yes, $0.00 for No, or last market price if Pending</returns>
        private static decimal GetSettlementPrice(PredictionMarket predictionMarket)
        {
            switch (predictionMarket.SettlementResult)
            {
                case PredictionMarketSettlementResult.Yes:
                    return 1.0m;

                case PredictionMarketSettlementResult.No:
                    return 0.0m;

                case PredictionMarketSettlementResult.Pending:
                default:
                    // Fallback to last market price with error log
                    Log.Error($"PredictionMarketPortfolioModel.GetSettlementPrice(): " +
                        $"Settlement result is Pending for {predictionMarket.Symbol}. " +
                        $"Falling back to last market price: {predictionMarket.Price}");
                    return predictionMarket.Price;
            }
        }
    }
}
