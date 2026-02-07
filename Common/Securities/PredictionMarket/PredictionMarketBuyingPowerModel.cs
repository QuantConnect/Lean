/*
 * CASCADELABS.IO
 * Cascade Labs LLC
 */

using System;
using QuantConnect.Orders;

namespace QuantConnect.Securities.PredictionMarket
{
    /// <summary>
    /// Buying power model for prediction market securities with cash-only (no margin) trading
    /// </summary>
    /// <remarks>
    /// Key characteristics:
    /// - Leverage is always 1 (no margin)
    /// - Buying YES at price P: requires P × quantity in cash (max loss = purchase price)
    /// - Selling/Shorting YES at price P: requires (1-P) × quantity in cash
    ///   (equivalent to buying NO at 1-P; max loss = 1-P per contract)
    /// - Selling to close an existing long position: allowed up to holdings quantity
    ///
    /// Key insight: Shorting a YES contract == buying a NO contract. Both are fully collateralized.
    /// A short YES at $0.60 risks $0.40 (if result=Yes, you pay $1 but received $0.60, net loss = $0.40).
    /// So collateral = (1 - 0.60) × contracts = $0.40 per contract.
    /// </remarks>
    public class PredictionMarketBuyingPowerModel : BuyingPowerModel
    {
        /// <summary>
        /// Initializes a new instance with leverage = 1 (no margin)
        /// </summary>
        public PredictionMarketBuyingPowerModel()
            : base(1m, 0m)
        {
        }

        /// <summary>
        /// Gets the leverage for the security (always 1)
        /// </summary>
        /// <param name="security">The security</param>
        /// <returns>1 (no leverage)</returns>
        public override decimal GetLeverage(Security security)
        {
            return 1m;
        }

        /// <summary>
        /// Sets the leverage for the security. Throws if leverage is not 1.
        /// </summary>
        /// <param name="security">The security</param>
        /// <param name="leverage">The desired leverage (must be 1)</param>
        public override void SetLeverage(Security security, decimal leverage)
        {
            if (leverage != 1m)
            {
                throw new InvalidOperationException(
                    "Prediction market securities do not support leverage. Leverage must be 1.");
            }
        }

        /// <summary>
        /// Gets the initial margin requirement for an order.
        /// For prediction markets:
        /// - Long positions: margin = price × quantity
        /// - Short positions: margin = (1 - price) × quantity
        /// </summary>
        /// <param name="parameters">The parameters containing security and quantity</param>
        /// <returns>The initial margin requirement</returns>
        public override InitialMargin GetInitialMarginRequirement(InitialMarginParameters parameters)
        {
            var security = parameters.Security;
            var quantity = parameters.Quantity;
            var price = security.Price;

            // For prediction markets priced between 0 and 1:
            // - Long: you pay price per contract, max loss = price
            // - Short: you risk (1 - price) per contract, max loss = 1 - price
            var currentHoldings = security.Holdings.Quantity;
            var newPosition = currentHoldings + quantity;

            decimal marginPerContract;

            if (newPosition >= 0)
            {
                // Going long or closing short: margin based on price
                marginPerContract = price;
            }
            else
            {
                // Going short: margin based on (1 - price)
                marginPerContract = 1m - price;
            }

            return security.QuoteCurrency.ConversionRate
                * security.SymbolProperties.ContractMultiplier
                * marginPerContract
                * Math.Abs(quantity);
        }

        /// <summary>
        /// Check if there is sufficient buying power to execute this order
        /// </summary>
        /// <param name="parameters">The parameters for the check</param>
        /// <returns>Returns buying power information for the order</returns>
        public override HasSufficientBuyingPowerForOrderResult HasSufficientBuyingPowerForOrder(
            HasSufficientBuyingPowerForOrderParameters parameters)
        {
            var order = parameters.Order;
            var security = parameters.Security;
            var holdings = security.Holdings;

            // Short circuit: zero quantity orders always have sufficient buying power
            if (order.Quantity == 0)
            {
                return parameters.Sufficient();
            }

            // Case 1: Selling to close a long position (no additional collateral needed)
            if (holdings.Quantity > 0 && order.Direction == OrderDirection.Sell)
            {
                // Selling up to current holdings quantity requires no additional collateral
                if (Math.Abs(order.Quantity) <= holdings.Quantity)
                {
                    return parameters.Sufficient();
                }
                // If selling more than holdings, we're going short - need collateral for the short portion
            }

            // Case 2: Buying to close a short position (no additional collateral needed)
            if (holdings.Quantity < 0 && order.Direction == OrderDirection.Buy)
            {
                // Buying up to current short quantity requires no additional collateral
                if (order.Quantity <= Math.Abs(holdings.Quantity))
                {
                    return parameters.Sufficient();
                }
                // If buying more than short holdings, we're going long - need collateral for the long portion
            }

            // For all other cases, use the standard buying power check with our margin calculation
            return base.HasSufficientBuyingPowerForOrder(parameters);
        }

        /// <summary>
        /// Gets the margin currently allocated to the specified holding.
        /// For prediction markets:
        /// - Long positions: margin = price × quantity
        /// - Short positions: margin = (1 - price) × quantity
        /// </summary>
        /// <param name="parameters">The parameters containing security and holdings info</param>
        /// <returns>The maintenance margin for the holdings</returns>
        public override MaintenanceMargin GetMaintenanceMargin(MaintenanceMarginParameters parameters)
        {
            var security = parameters.Security;
            var quantity = parameters.Quantity;
            var price = security.Price;

            if (quantity == 0)
            {
                return 0m;
            }

            decimal marginPerContract;
            if (quantity > 0)
            {
                // Long position: margin based on price
                marginPerContract = price;
            }
            else
            {
                // Short position: margin based on (1 - price)
                marginPerContract = 1m - price;
            }

            return security.QuoteCurrency.ConversionRate
                * security.SymbolProperties.ContractMultiplier
                * marginPerContract
                * Math.Abs(quantity);
        }
    }
}
