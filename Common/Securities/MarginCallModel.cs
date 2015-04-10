using System.Collections.Generic;
using System.Linq;
using QuantConnect.Orders;

namespace QuantConnect.Securities
{
    /// <summary>
    /// Represents the model responsible for picking which orders should be executed during a margin call
    /// </summary>
    /// <remarks>
    /// This is a default implementation that orders the generated margin call orders by the unrealized
    /// profit (losers first) and executes each order synchronously until we're within the margin requirements
    /// </remarks>
    public class MarginCallModel
    {
        /// <summary>
        /// Gets the portfolio that margin calls will be transacted against
        /// </summary>
        protected SecurityPortfolioManager Portfolio { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MarginCallModel"/> class
        /// </summary>
        /// <param name="portfolio">The portfolio object to receive margin calls</param>
        public MarginCallModel(SecurityPortfolioManager portfolio)
        {
            Portfolio = portfolio;
        }

        /// <summary>
        /// Executes synchronous orders to bring the account within margin requirements.
        /// </summary>
        /// <param name="generatedMarginCallOrders">These are the margin call orders that were generated
        /// by individual security margin models.</param>
        public virtual void ExecuteMarginCall(IEnumerable<Order> generatedMarginCallOrders)
        {
            // order by losers first
            var ordersWithSecurities = generatedMarginCallOrders.ToDictionary(x => x, x => Portfolio[x.Symbol]);
            var orderedByLosers = ordersWithSecurities.OrderBy(x => x.Value.UnrealizedProfit).Select(x => x.Key);
            foreach (var order in orderedByLosers)
            {
                Portfolio.Transactions.AddOrder(order);
                Portfolio.Transactions.WaitForOrder(order.Id);

                // if our margin used is back under the portfolio value then we can stop liquidating
                if (Portfolio.TotalMarginUsed <= Portfolio.TotalPortfolioValue)
                {
                    break;
                }
            }
        }
    }
}