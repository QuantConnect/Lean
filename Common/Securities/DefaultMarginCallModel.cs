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
using QuantConnect.Brokerages;
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
        /// The submitted margin call orders that might still be open, and those of the current margin call that closed without filling
        /// </summary>
        private readonly List<OrderTicket> _marginCallTickets = new();

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

            // the margin call orders that failed are kept until the margin call ends, so their position groups are liquidated last
            _marginCallTickets.RemoveAll(ticket => ticket.Status == OrderStatus.Filled);

            // don't issue a margin call if we're not using margin
            if (totalMarginUsed <= 0)
            {
                _marginCallTickets.RemoveAll(ticket => ticket.Status.IsClosed());
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

            // the margin call ended: the margin call orders that failed are forgotten
            if (marginRemaining > 0)
            {
                _marginCallTickets.RemoveAll(ticket => ticket.Status.IsClosed());
            }

            // if we still have margin remaining then there's no need for a margin call
            if (marginRemaining <= 0)
            {
                // margin call orders can stay open for a while, such as market orders waiting for the next daily bar in backtesting:
                // the margin they will release is accounted for, and the holdings they're liquidating aren't liquidated again
                var pendingQuantities = GetOpenMarginCallOrderQuantities();
                var marginUsedAfterPendingOrders = totalMarginUsed - GetMarginReleased(pendingQuantities);

                if (marginUsedAfterPendingOrders > totalPortfolioValue * (1 + _marginBuffer))
                {
                    // the position groups are sized from the holdings the open margin call orders leave
                    var regroupedPositionGroups = ResolvePositionGroupsAfterFills(pendingQuantities, out var impactedGroups);
                    foreach (var positionGroup in Portfolio.Positions.Groups.Except(impactedGroups).Concat(regroupedPositionGroups))
                    {
                        var positionMarginCallOrders = GenerateMarginCallOrders(
                            new MarginCallOrdersParameters(positionGroup, totalPortfolioValue, marginUsedAfterPendingOrders)).ToList();
                        // holdings that open margin call orders sell beyond what's held aren't bought back
                        if (positionMarginCallOrders.Count > 0 && positionMarginCallOrders.All(x => x.Quantity != 0
                            && Math.Sign(x.Quantity) != Math.Sign(Portfolio[x.Symbol].Quantity)
                            && Math.Abs(pendingQuantities.GetValueOrDefault(x.Symbol) + x.Quantity) <= Math.Abs(Portfolio[x.Symbol].Quantity)))
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
        /// Gets the quantities that the open margin call orders have yet to fill, by symbol
        /// </summary>
        private Dictionary<Symbol, decimal> GetOpenMarginCallOrderQuantities()
        {
            return _marginCallTickets
                .Where(ticket => ticket.Status.IsOpen())
                .GroupBy(ticket => ticket.Symbol)
                .ToDictionary(tickets => tickets.Key, tickets => tickets.Sum(ticket => ticket.QuantityRemaining));
        }

        /// <summary>
        /// Gets the margin that filling the specified quantities at the current prices would release, regrouping the impacted positions
        /// </summary>
        private decimal GetMarginReleased(Dictionary<Symbol, decimal> quantities)
        {
            if (quantities.Count == 0)
            {
                return 0;
            }

            var groupsAfterFills = ResolvePositionGroupsAfterFills(quantities, out var impactedGroups);

            return impactedGroups.Sum(positionGroup => positionGroup.BuyingPowerModel.GetReservedBuyingPowerForPositionGroup(Portfolio, positionGroup))
                - groupsAfterFills.Sum(positionGroup => positionGroup.BuyingPowerModel.GetReservedBuyingPowerForPositionGroup(Portfolio, positionGroup));
        }

        /// <summary>
        /// Resolves the position groups the impacted positions form once the specified quantities are filled
        /// </summary>
        private PositionGroupCollection ResolvePositionGroupsAfterFills(Dictionary<Symbol, decimal> quantities, out List<IPositionGroup> impactedGroups)
        {
            var positions = quantities.Select(kvp => (IPosition)new Position(Portfolio.Securities[kvp.Key], kvp.Value)).ToList();
            impactedGroups = Portfolio.Positions.GetImpactedGroups(positions).ToList();
            var positionsAfterFills = impactedGroups
                .SelectMany(positionGroup => positionGroup.Positions)
                .Concat(positions)
                .Consolidate()
                .Values
                .Where(position => position.Quantity != 0);

            return Portfolio.Positions.ResolvePositionGroups(new PositionCollection(positionsAfterFills));
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

            decimal absQuantity;
            if (Portfolio.Positions.Groups.TryGetGroup(positionGroup.Key, out var heldPositionGroup) && heldPositionGroup.Quantity == positionGroup.Quantity)
            {
                // if currentlyUsedBuyingPower > deltaAccountCurrency, means we can keep using the diff in buying power
                var buyingPowerToKeep = Math.Max(0, currentlyUsedBuyingPower - deltaAccountCurrency);

                // we want a reduction so we send the inverse side of our position
                var deltaBuyingPower = (currentlyUsedBuyingPower - buyingPowerToKeep) * -Math.Sign(positionGroup.Quantity);

                var result = positionGroup.BuyingPowerModel.GetMaximumLotsForDeltaBuyingPower(new GetMaximumLotsForDeltaBuyingPowerParameters(
                    Portfolio, positionGroup, deltaBuyingPower,
                    // margin is negative, we need to reduce positions, no minimum
                    minimumOrderMarginPortfolioPercentage: 0
                ));
                // the security's buying power model sizes from all of its holdings, which other position groups can share
                absQuantity = Math.Min(Math.Abs(result.NumberOfLots), Math.Abs(positionGroup.Quantity));
            }
            else
            {
                // the holdings left after other liquidations can form a group that isn't held, which the buying power model
                // sizes from the held one: the fewest lots of this group whose margin covers the deficit are liquidated instead
                var heldLots = Math.Abs(positionGroup.Quantity);
                var lowerLots = 1m;
                var upperLots = Math.Ceiling(heldLots);
                while (lowerLots < upperLots)
                {
                    var lots = Math.Floor((lowerLots + upperLots) / 2);
                    var positionGroupLeft = positionGroup.WithQuantity(heldLots - lots, Portfolio.Positions);
                    var releasedBuyingPower = currentlyUsedBuyingPower
                        - positionGroupLeft.BuyingPowerModel.GetReservedBuyingPowerForPositionGroup(Portfolio, positionGroupLeft);
                    if (releasedBuyingPower >= deltaAccountCurrency)
                    {
                        upperLots = lots;
                    }
                    else
                    {
                        lowerLots = lots + 1;
                    }
                }
                absQuantity = Math.Min(lowerLots, heldLots);
            }

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
                // Always reducing, so we take the absolute quantity times the opposite sign of the position.
                // This is the leg's total quantity, which is what combo order requests expect too
                var legQuantity = absQuantity * position.UnitQuantity * -Math.Sign(position.Quantity);

                return new SubmitOrderRequest(
                    orderType,
                    security.Type,
                    security.Symbol,
                    legQuantity,
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

            // the symbols of each margin call order of this margin call that failed, so the groups whose orders failed the most go last
            var failedOrdersSymbols = _marginCallTickets
                .Where(ticket => ticket.Status.IsClosed())
                .GroupBy(ticket => (object)ticket.SubmitRequest.GroupOrderManager ?? ticket)
                .Select(tickets => tickets.Select(ticket => ticket.Symbol).ToHashSet())
                .ToList();

            // order by losers first
            var executedOrders = new List<OrderTicket>();
            var ordersWithSecurities = generatedMarginCallOrders.ToDictionary(x => x, x => Portfolio[x.Symbol]);
            var groupManagerTemporalIds = -ordersWithSecurities.Count;
            var orderedByLosers = ordersWithSecurities
                // group orders by their group manager id so they are executed together
                .GroupBy(x => x.Key.GroupOrderManager?.Id ?? groupManagerTemporalIds++)
                .OrderBy(x => failedOrdersSymbols.Count(symbols => symbols.SetEquals(x.Select(kvp => kvp.Key.Symbol))))
                .ThenBy(x => x.Sum(kvp => kvp.Value.UnrealizedProfit))
                .Select(x => x.Select(kvp => kvp.Key).ToList());
            foreach (var generatedRequests in orderedByLosers)
            {
                var requests = generatedRequests;
                var groupOrderManager = requests[0].GroupOrderManager;
                // a failed order releases no margin, so after failed ones alone the requests are executed as generated for the whole deficit
                if (executedOrders.Any(ticket => ticket.Status != OrderStatus.Invalid && ticket.Status != OrderStatus.Canceled)
                    && requests.All(request => request.Quantity != 0) && groupOrderManager?.Quantity != 0)
                {
                    // the earlier orders cover part of the deficit: the fewest lots of these requests that cover what they leave
                    // are executed, never more than requested
                    var lotQuantities = requests.Select(request => groupOrderManager == null
                        ? Math.Sign(request.Quantity) * Portfolio.Securities[request.Symbol].SymbolProperties.LotSize
                        : request.Quantity / Math.Abs(groupOrderManager.Quantity)).ToList();
                    decimal GetQuantity(int index, decimal lots) =>
                        Math.Sign(requests[index].Quantity) * Math.Min(Math.Abs(lotQuantities[index] * lots), Math.Abs(requests[index].Quantity));

                    var marginRemaining = Portfolio.MarginRemaining;
                    var openQuantities = GetOpenMarginCallOrderQuantities();
                    var requestedLots = Math.Ceiling(Math.Abs(requests[0].Quantity / lotQuantities[0]));
                    var lowerLots = 1m;
                    var upperLots = requestedLots;
                    while (lowerLots < upperLots)
                    {
                        var lots = Math.Floor((lowerLots + upperLots) / 2);
                        var quantities = new Dictionary<Symbol, decimal>(openQuantities);
                        for (var i = 0; i < requests.Count; i++)
                        {
                            quantities[requests[i].Symbol] = quantities.GetValueOrDefault(requests[i].Symbol) + GetQuantity(i, lots);
                        }
                        if (marginRemaining + GetMarginReleased(quantities) >= 0)
                        {
                            upperLots = lots;
                        }
                        else
                        {
                            lowerLots = lots + 1;
                        }
                    }

                    // an order below the minimum order size would be rejected: the requested one is executed instead
                    if (lowerLots < requestedLots && Enumerable.Range(0, requests.Count).All(i =>
                        DefaultBrokerageModel.IsValidOrderSize(Portfolio.Securities[requests[i].Symbol], GetQuantity(i, lowerLots), out _)))
                    {
                        var scaledGroupOrderManager = groupOrderManager == null ? null : new GroupOrderManager(
                            Portfolio.Transactions.GetIncrementGroupOrderManagerId(), groupOrderManager.Count,
                            Math.Sign(groupOrderManager.Quantity) * lowerLots, groupOrderManager.LimitPrice);
                        requests = requests.Select((request, i) => new SubmitOrderRequest(request.OrderType, request.SecurityType,
                            request.Symbol, GetQuantity(i, lowerLots), request.StopPrice, request.LimitPrice, request.TriggerPrice,
                            request.TrailingAmount, request.TrailingAsPercentage, request.Time, request.Tag, request.OrderProperties,
                            scaledGroupOrderManager, request.Asynchronous, request.Contingency)).ToList();
                    }
                }

                var tickets = new List<OrderTicket>();
                foreach (var request in requests)
                {
                    var ticket = Portfolio.Transactions.AddOrder(request);
                    tickets.Add(ticket);
                    _marginCallTickets.Add(ticket);
                }

                foreach (var ticket in tickets)
                {
                    if (ticket.Status.IsOpen())
                    {
                        Portfolio.Transactions.WaitForOrder(ticket.OrderId);
                    }
                    executedOrders.Add(ticket);
                }

                // if our margin used, once the margin call orders still open are filled, is back under the portfolio value
                // then we can stop liquidating
                if (Portfolio.MarginRemaining + GetMarginReleased(GetOpenMarginCallOrderQuantities()) >= 0)
                {
                    break;
                }
            }
            return executedOrders;
        }
    }
}
