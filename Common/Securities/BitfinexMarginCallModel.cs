using System.Collections.Generic;
using System.Linq;
using QuantConnect.Orders;

namespace QuantConnect.Securities
{
    /// <summary>
    /// Represents the model responsible for picking which orders should be executed during a margin call
    /// </summary>
    public class BitfinexMarginCallModel : MarginCallModel
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="BitfinexMarginCallModel"/> class
        /// </summary>
        /// <param name="portfolio">The portfolio object to receive margin calls</param>
        public BitfinexMarginCallModel(SecurityPortfolioManager portfolio)
            : base(portfolio)
        {
        }

        /// <summary>
        /// Ignores automated margin call orders. Margin call warnings should be manually resolved.
        /// </summary>
        /// <param name="generatedMarginCallOrders">These are the margin call orders that were generated
        /// by individual security margin models.</param>
        /// <returns>Empty collection</returns>
        public virtual List<OrderTicket> ExecuteMarginCall(IEnumerable<SubmitOrderRequest> generatedMarginCallOrders)
        {
            return new List<OrderTicket>();
        }
    }
}