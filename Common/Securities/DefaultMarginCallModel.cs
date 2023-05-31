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
using QuantConnect.Securities.Positions;

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
        /// The percent margin buffer to use when checking whether the total margin used is
        /// above the total portfolio value to generate margin call orders
        /// </summary>
        private readonly decimal _marginBuffer;

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
        /// <param name="marginBuffer">
        /// The percent margin buffer to use when checking whether the total margin used is
        /// above the total portfolio value to generate margin call orders
        /// </param>
        public DefaultMarginCallModel(SecurityPortfolioManager portfolio, IOrderProperties defaultOrderProperties, decimal marginBuffer = 0.10m)
        {
            Portfolio = portfolio;
            DefaultOrderProperties = defaultOrderProperties;
            _marginBuffer = marginBuffer;
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
                if (totalMarginUsed > totalPortfolioValue * (1 + _marginBuffer))
                {
                    foreach (var positionGroup in Portfolio.Positions.Groups)
                    {
                        var positionMarginCallOrders = GenerateMarginCallOrders(
                            new MarginCallOrdersParameters(positionGroup, totalPortfolioValue, totalMarginUsed)).ToList();
                        if (positionMarginCallOrders.Count > 0 && positionMarginCallOrders.All(x => x.Quantity != 0))
                        {
                            marginCallOrders.AddRange(positionMarginCallOrders);
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
        /// <param name="parameters">The set of parameters required to generate the margin call orders</param>
        /// <returns>An order object representing a liquidation order to be executed to bring the account within margin requirements</returns>
        protected virtual IEnumerable<SubmitOrderRequest> GenerateMarginCallOrders(MarginCallOrdersParameters parameters)
        {
            var positionGroup = parameters.PositionGroup;
            if (positionGroup.Positions.Any(position => Portfolio.Securities[position.Symbol].QuoteCurrency.ConversionRate == 0))
            {
                // check for div 0 - there's no conv rate, so we can't place an order
                return Enumerable.Empty<SubmitOrderRequest>();
            }

            // compute the amount of quote currency we need to liquidate in order to get within margin requirements
            var deltaAccountCurrency = parameters.TotalUsedMargin - parameters.TotalPortfolioValue;

            var currentlyUsedBuyingPower = positionGroup.BuyingPowerModel.GetReservedBuyingPowerForPositionGroup(Portfolio, positionGroup);

            // if currentlyUsedBuyingPower > deltaAccountCurrency, means we can keep using the diff in buying power
            var buyingPowerToKeep = Math.Max(0, currentlyUsedBuyingPower - deltaAccountCurrency);

            // we want a reduction so we send the inverse side of our position
            var deltaBuyingPower = (currentlyUsedBuyingPower - buyingPowerToKeep) * -Math.Sign(positionGroup.Quantity);

            var result = positionGroup.BuyingPowerModel.GetMaximumLotsForDeltaBuyingPower(new GetMaximumLotsForDeltaBuyingPowerParameters(
                Portfolio, positionGroup, deltaBuyingPower,
                // margin is negative, we need to reduce positions, no minimum
                minimumOrderMarginPortfolioPercentage: 0
            ));

            var absQuantity = Math.Abs(result.NumberOfLots);
            var orderType = positionGroup.Count > 1 ? OrderType.ComboMarket : OrderType.Market;

            GroupOrderManager groupOrderManager = null;
            if (orderType == OrderType.ComboMarket)
            {
                groupOrderManager = new GroupOrderManager(Portfolio.Transactions.GetIncrementGroupOrderManagerId(), positionGroup.Count,
                    absQuantity);
            }

            return positionGroup.Positions.Select(position =>
            {
                var security = Portfolio.Securities[position.Symbol];
                // Always reducing, so we take the absolute quantity times the opposite sign of the position
                var legQuantity = absQuantity * position.UnitQuantity * -Math.Sign(position.Quantity);

                return new SubmitOrderRequest(
                    orderType,
                    security.Type,
                    security.Symbol,
                    legQuantity.GetOrderLegGroupQuantity(groupOrderManager),
                    0,
                    0,
                    security.LocalTime.ConvertToUtc(security.Exchange.TimeZone),
                    Messages.DefaultMarginCallModel.MarginCallOrderTag,
                    DefaultOrderProperties?.Clone(),
                    groupOrderManager);
            });
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
            var groupManagerTemporalIds = -ordersWithSecurities.Count;
            var orderedByLosers = ordersWithSecurities
                // group orders by their group manager id so they are executed together
                .GroupBy(x => x.Key.GroupOrderManager?.Id ?? groupManagerTemporalIds++)
                .OrderBy(x => x.Sum(kvp => kvp.Value.UnrealizedProfit))
                .Select(x => x.Select(kvp => kvp.Key));
            foreach (var requests in orderedByLosers)
            {
                var tickets = new List<OrderTicket>();
                foreach (var request in requests)
                {
                    tickets.Add(Portfolio.Transactions.AddOrder(request));
                }

                foreach (var ticket in tickets)
                {
                    if (ticket.Status.IsOpen())
                    {
                        Portfolio.Transactions.WaitForOrder(ticket.OrderId);
                    }
                    executedOrders.Add(ticket);
                }

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
