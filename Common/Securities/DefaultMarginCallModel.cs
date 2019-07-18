/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *
*/

using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Interfaces;
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
    public class DefaultMarginCallModel : IMarginCallModel
    {
        /// <summary>
        /// Gets the portfolio that margin calls will be transacted against
        /// </summary>
        protected SecurityPortfolioManager Portfolio { get; }

        /// <summary>
        /// Gets the default order properties to be used in margin call orders
        /// </summary>
        protected IOrderProperties DefaultOrderProperties { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultMarginCallModel"/> class
        /// </summary>
        /// <param name="portfolio">The portfolio object to receive margin calls</param>
        /// <param name="defaultOrderProperties">The default order properties to be used in margin call orders</param>
        public DefaultMarginCallModel(SecurityPortfolioManager portfolio, IOrderProperties defaultOrderProperties)
        {
            Portfolio = portfolio;
            DefaultOrderProperties = defaultOrderProperties;
        }

        /// <summary>
        /// Scan the portfolio and the updated data for a potential margin call situation which may get the holdings below zero!
        /// If there is a margin call, liquidate the portfolio immediately before the portfolio gets sub zero.
        /// </summary>
        /// <param name="issueMarginCallWarning">Set to true if a warning should be issued to the algorithm</param>
        /// <returns>True for a margin call on the holdings.</returns>
        public List<SubmitOrderRequest> GetMarginCallOrders(out bool issueMarginCallWarning)
        {
            issueMarginCallWarning = false;

            var totalMarginUsed = Portfolio.TotalMarginUsed;

            // don't issue a margin call if we're not using margin
            if (totalMarginUsed <= 0)
            {
                return new List<SubmitOrderRequest>();
            }

            // don't issue a margin call if we're under 1x implied leverage on the whole portfolio's holdings
            var averageHoldingsLeverage = Portfolio.TotalAbsoluteHoldingsCost / totalMarginUsed;
            if (averageHoldingsLeverage <= 1.0m)
            {
                return new List<SubmitOrderRequest>();
            }
            var totalPortfolioValue = Portfolio.TotalPortfolioValue;

            var marginRemaining = Portfolio.GetMarginRemaining(totalPortfolioValue);

            // issue a margin warning when we're down to 5% margin remaining
            if (marginRemaining <= totalPortfolioValue * 0.05m)
            {
                issueMarginCallWarning = true;
            }

            // generate a listing of margin call orders
            var marginCallOrders = new List<SubmitOrderRequest>();

            // if we still have margin remaining then there's no need for a margin call
            if (marginRemaining <= 0)
            {
                // skip securities that have no price data or no holdings, we can't liquidate nothingness
                foreach (var kvp in Portfolio.Securities)
                {
                    var security = kvp.Value;

                    if (security.Holdings.Quantity != 0 && security.Price != 0)
                    {
                        var buyingPowerModel = security.BuyingPowerModel as SecurityMarginModel;
                        if (buyingPowerModel != null)
                        {
                            var maintenanceMarginRequirement = buyingPowerModel.GetMaintenanceMarginRequirement(security);
                            var marginCallOrder = GenerateMarginCallOrder(security, totalPortfolioValue, totalMarginUsed, maintenanceMarginRequirement);
                            if (marginCallOrder != null && marginCallOrder.Quantity != 0)
                            {
                                marginCallOrders.Add(marginCallOrder);
                            }
                        }
                    }
                }
                issueMarginCallWarning = marginCallOrders.Count > 0;
            }

            return marginCallOrders;
        }

        /// <summary>
        /// Generates a new order for the specified security taking into account the total margin
        /// used by the account. Returns null when no margin call is to be issued.
        /// </summary>
        /// <param name="security">The security to generate a margin call order for</param>
        /// <param name="netLiquidationValue">The net liquidation value for the entire account</param>
        /// <param name="totalMargin">The total margin used by the account in units of base currency</param>
        /// <param name="maintenanceMarginRequirement">The percentage of the holding's absolute cost that must be held in free cash in order to avoid a margin call</param>
        /// <returns>An order object representing a liquidation order to be executed to bring the account within margin requirements</returns>
        public virtual SubmitOrderRequest GenerateMarginCallOrder(Security security, decimal netLiquidationValue, decimal totalMargin, decimal maintenanceMarginRequirement)
        {
            // leave a buffer in default implementation
            const decimal marginBuffer = 0.10m;

            if (totalMargin <= netLiquidationValue * (1 + marginBuffer))
            {
                return null;
            }

            if (!security.Holdings.Invested)
            {
                return null;
            }

            if (security.QuoteCurrency.ConversionRate == 0m)
            {
                // check for div 0 - there's no conv rate, so we can't place an order
                return null;
            }

            // compute the amount of quote currency we need to liquidate in order to get within margin requirements
            var deltaInQuoteCurrency = (totalMargin - netLiquidationValue) / security.QuoteCurrency.ConversionRate;

            // compute the number of shares required for the order, rounding up
            var unitPriceInQuoteCurrency = security.Price * security.SymbolProperties.ContractMultiplier;
            var quantity = Math.Round(deltaInQuoteCurrency / unitPriceInQuoteCurrency, MidpointRounding.AwayFromZero) / maintenanceMarginRequirement;

            // don't try and liquidate more share than we currently hold, minimum value of LotSize, maximum value for absolute quantity
            quantity = Math.Max(security.SymbolProperties.LotSize, Math.Min(security.Holdings.AbsoluteQuantity, quantity));
            if (security.Holdings.IsLong)
            {
                // adjust to a sell for long positions
                quantity *= -1;
            }

            return new SubmitOrderRequest(OrderType.Market, security.Type, security.Symbol, quantity, 0, 0, security.LocalTime.ConvertToUtc(security.Exchange.TimeZone), "Margin Call", DefaultOrderProperties?.Clone());
        }

        /// <summary>
        /// Executes synchronous orders to bring the account within margin requirements.
        /// </summary>
        /// <param name="generatedMarginCallOrders">These are the margin call orders that were generated
        /// by individual security margin models.</param>
        /// <returns>The list of orders that were actually executed</returns>
        public virtual List<OrderTicket> ExecuteMarginCall(IEnumerable<SubmitOrderRequest> generatedMarginCallOrders)
        {
            // if our margin used is back under the portfolio value then we can stop liquidating
            if (Portfolio.MarginRemaining >= 0)
            {
                return new List<OrderTicket>();
            }

            // order by losers first
            var executedOrders = new List<OrderTicket>();
            var ordersWithSecurities = generatedMarginCallOrders.ToDictionary(x => x, x => Portfolio[x.Symbol]);
            var orderedByLosers = ordersWithSecurities.OrderBy(x => x.Value.UnrealizedProfit).Select(x => x.Key);
            foreach (var request in orderedByLosers)
            {
                var ticket = Portfolio.Transactions.AddOrder(request);
                Portfolio.Transactions.WaitForOrder(request.OrderId);
                executedOrders.Add(ticket);

                // if our margin used is back under the portfolio value then we can stop liquidating
                if (Portfolio.MarginRemaining >= 0)
                {
                    break;
                }
            }
            return executedOrders;
        }
    }
}