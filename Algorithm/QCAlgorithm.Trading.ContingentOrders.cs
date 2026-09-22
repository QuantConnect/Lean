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
*/

using System;
using System.Linq;
using QuantConnect.Orders;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using System.Collections.Generic;

namespace QuantConnect.Algorithm
{
    public partial class QCAlgorithm
    {
        /// <summary>
        /// Creates order requests to be submitted later through <see cref="Order(SubmitOrderRequest)"/>, so they can be composed into contingent
        /// orders before: orders which trigger other orders once filled (OTO), orders which cancel (OCO/OCA) or update (OUO) each other,
        /// and any composition of them like brackets (OTOCO)
        /// </summary>
        [DocumentationAttribute(TradingAndOrders)]
        public OrderFactory OrderFactory { get; private set; }

        /// <summary>
        /// Submits the given order request, built through <see cref="OrderFactory"/>, along with the set of contingent orders composed on it
        /// </summary>
        /// <param name="order">The order request to submit, see <see cref="OrderFactory"/></param>
        /// <returns>The tickets of all the submitted orders, parents before the orders they trigger, in the order they were composed</returns>
        /// <remarks>The orders triggered by another are held by the brokerage until then, see <see cref="OrderContingency.IsWaitingForTrigger"/>.
        /// The whole set of contingent orders the request belongs to is submitted</remarks>
        [DocumentationAttribute(TradingAndOrders)]
        public List<OrderTicket> Order(SubmitOrderRequest order)
        {
            return SubmitOrders(new[] { order });
        }

        /// <summary>
        /// Submits the given order requests, built through <see cref="OrderFactory"/>, along with the sets of contingent orders composed on them:
        /// the legs of a combo order, orders which cancel or update each other, each of them possibly triggering other orders once filled
        /// </summary>
        /// <param name="orders">The order requests to submit, see <see cref="OrderFactory"/></param>
        /// <returns>The tickets of all the submitted orders, parents first, in the order they were composed</returns>
        [DocumentationAttribute(TradingAndOrders)]
        public List<OrderTicket> Order(IEnumerable<SubmitOrderRequest> orders)
        {
            return SubmitOrders(orders);
        }

        /// <summary>
        /// Submits a bracket order (OTOCO): an entry order which once filled triggers a take profit limit order and a stop loss order of the
        /// opposite quantity, which are held until then. Once the take profit or the stop loss fills the other one is canceled.
        /// </summary>
        /// <param name="symbol">The symbol to trade</param>
        /// <param name="quantity">The quantity of the entry order</param>
        /// <param name="takeProfitPrice">The limit price of the take profit order</param>
        /// <param name="stopLossPrice">The stop price of the stop loss order</param>
        /// <param name="limitPrice">The limit price of the entry order, if not provided the entry is a market order</param>
        /// <param name="asynchronous">Send the order asynchronously (false). Otherwise we'll block until the market entry order fills</param>
        /// <param name="tag">String tag for the orders (optional)</param>
        /// <param name="orderProperties">The order properties to use. Defaults to <see cref="DefaultOrderProperties"/></param>
        /// <returns>The tickets of the entry, take profit and stop loss orders, in that order</returns>
        /// <remarks>For other entry or exit order types see <see cref="SubmitOrderRequest.Bracket"/> and <see cref="Order(SubmitOrderRequest)"/></remarks>
        [DocumentationAttribute(TradingAndOrders)]
        public List<OrderTicket> BracketOrder(Symbol symbol, decimal quantity, decimal takeProfitPrice, decimal stopLossPrice, decimal? limitPrice = null,
            bool asynchronous = false, string tag = "", IOrderProperties orderProperties = null)
        {
            var entry = limitPrice.HasValue
                ? OrderFactory.LimitOrder(symbol, quantity, limitPrice.Value, asynchronous, tag, orderProperties)
                : OrderFactory.MarketOrder(symbol, quantity, asynchronous, tag, orderProperties);
            return SubmitOrders(new[] { entry.Bracket(takeProfitPrice, stopLossPrice) });
        }

        /// <summary>
        /// Submits a set of orders where the first one to fill, even partially, cancels the rest (OCO/OCA)
        /// </summary>
        /// <param name="orders">The order requests, all the legs for combo orders, which can trigger other orders in turn</param>
        /// <returns>The tickets of all the submitted orders</returns>
        [DocumentationAttribute(TradingAndOrders)]
        public List<OrderTicket> OneCancelsOtherOrder(IEnumerable<SubmitOrderRequest> orders)
        {
            return SubmitOrders(OrderFactory.OneCancelsOther(orders));
        }

        /// <summary>
        /// Submits a set of orders where a partial fill of one of them reduces the remaining quantity of the rest proportionally,
        /// which are canceled once it completely fills (OUO)
        /// </summary>
        /// <param name="orders">The order requests, all the legs for combo orders, which can trigger other orders in turn</param>
        /// <returns>The tickets of all the submitted orders</returns>
        [DocumentationAttribute(TradingAndOrders)]
        public List<OrderTicket> OneUpdatesOtherOrder(IEnumerable<SubmitOrderRequest> orders)
        {
            return SubmitOrders(OrderFactory.OneUpdatesOther(orders));
        }

        /// <summary>
        /// Submits an order which once completely filled triggers others (OTO), they are held until then and canceled if the parent is canceled
        /// </summary>
        /// <param name="parent">The order request of the parent order</param>
        /// <param name="children">The order requests to trigger, all the legs for combo orders, independent of each other unless related</param>
        /// <returns>The tickets of all the submitted orders, the parent first</returns>
        [DocumentationAttribute(TradingAndOrders)]
        public List<OrderTicket> OneTriggersOtherOrder(SubmitOrderRequest parent, IEnumerable<SubmitOrderRequest> children)
        {
            return OneTriggersOtherOrder(new[] { parent }, children);
        }

        /// <summary>
        /// Submits a combo order which once all its legs fill triggers other orders (OTO), they are held until then and canceled if the parent is canceled
        /// </summary>
        /// <param name="parent">The order requests of the legs of the parent combo order</param>
        /// <param name="children">The order requests to trigger, all the legs for combo orders, independent of each other unless related</param>
        /// <returns>The tickets of all the submitted orders, the parent legs first</returns>
        [DocumentationAttribute(TradingAndOrders)]
        public List<OrderTicket> OneTriggersOtherOrder(IEnumerable<SubmitOrderRequest> parent, IEnumerable<SubmitOrderRequest> children)
        {
            var legs = parent?.ToList();
            OrderContingency.Trigger(legs, children);
            return SubmitOrders(legs);
        }

        /// <summary>
        /// Submits a single order request, along with its set of contingent orders if any, see <see cref="SubmitOrders"/>
        /// </summary>
        private OrderTicket SubmitOrder(SubmitOrderRequest order)
        {
            if (order.Contingency != null)
            {
                return SubmitOrders(new[] { order })[0];
            }

            Action conversionWarning = null;
            var response = PrepareRequest(order, ref conversionWarning);
            if (response.IsError)
            {
                return OrderTicket.InvalidSubmitRequest(Transactions, order, response);
            }
            var ticket = Transactions.AddOrder(order);
            if (order.Response.IsSuccess)
            {
                conversionWarning?.Invoke();
            }
            WaitForOrderIfRequired(order, ticket);
            return ticket;
        }

        /// <summary>
        /// Single entry point for submitting orders: single orders, combo orders and any set of contingent orders
        /// </summary>
        private List<OrderTicket> SubmitOrders(IEnumerable<SubmitOrderRequest> orders)
        {
            // the requests to submit, the sets of contingent orders as a whole: parents before the orders they trigger.
            // We execute pre order checks for all requests before submitting, so that if anything fails we are not left with half submitted orders
            var requests = new List<SubmitOrderRequest>();
            Action conversionWarning = null;
            // the legs of the combo orders which are not part of a set of contingent orders, all of them are required
            Dictionary<GroupOrderManager, int> comboLegs = null;
            foreach (var order in orders)
            {
                if (requests.Contains(order))
                {
                    // along with the rest of its set
                    continue;
                }
                if (order.Contingency == null)
                {
                    if (order.GroupOrderManager != null)
                    {
                        comboLegs ??= new();
                        comboLegs[order.GroupOrderManager] = comboLegs.GetValueOrDefault(order.GroupOrderManager) + 1;
                    }
                    var response = PrepareRequest(order, ref conversionWarning);
                    if (response.IsError)
                    {
                        return new List<OrderTicket> { OrderTicket.InvalidSubmitRequest(Transactions, order, response) };
                    }
                    requests.Add(order);
                    continue;
                }
                var setRequests = order.Contingency.Requests;
                for (var i = 0; i < setRequests.Count; i++)
                {
                    var request = setRequests[i];
                    var response = PrepareRequest(request, ref conversionWarning);
                    if (response.IsError)
                    {
                        return new List<OrderTicket> { OrderTicket.InvalidSubmitRequest(Transactions, request, response) };
                    }
                    requests.Add(request);
                }
            }
            if (comboLegs != null)
            {
                foreach (var (groupOrderManager, count) in comboLegs)
                {
                    if (count != groupOrderManager.Count)
                    {
                        throw new ArgumentException($"Expected all the {groupOrderManager.Count} legs of the combo order, got {count}", nameof(orders));
                    }
                }
            }

            // add the orders, creating their ids
            var tickets = new List<OrderTicket>(requests.Count);
            for (var i = 0; i < requests.Count; i++)
            {
                tickets.Add(Transactions.AddOrder(requests[i]));
            }
            if (requests.Count > 0 && requests[0].Response.IsSuccess)
            {
                conversionWarning?.Invoke();
            }

            for (var i = 0; i < requests.Count; i++)
            {
                WaitForOrderIfRequired(requests[i], tickets[i]);
            }
            return tickets;
        }

        /// <summary>
        /// Prepares a request for submission, converting the order type when required, and executes the pre order checks
        /// </summary>
        /// <param name="request">The request to prepare</param>
        /// <param name="conversionWarning">The warnings to send once the orders are submitted, when a market order is converted</param>
        private OrderResponse PrepareRequest(SubmitOrderRequest request, ref Action conversionWarning)
        {
            if (request.OrderId > 0)
            {
                throw new ArgumentException($"The order was already submitted, it can only be submitted once: {request}");
            }

            var security = GetSecurityForOrder(request.Symbol);
            var held = IsHeld(request);
            if (request.Contingency?.Id == 0)
            {
                // we create a unique Id so the algorithm and the brokerage can relate the contingent orders with each other
                request.Contingency.SetId(Transactions.GetIncrementContingentOrderSetId());
            }
            if (request.GroupOrderManager != null)
            {
                if (request.GroupOrderManager.Id == 0)
                {
                    // we create a unique Id so the algorithm and the brokerage can relate the combo orders with each other
                    request.GroupOrderManager.Id = Transactions.GetIncrementGroupOrderManagerId();
                }
            }
            else if (request.OrderType == OrderType.Market && !held)
            {
                conversionWarning += ConvertMarketOrderIfRequired(request, security);
            }
            else if (request.OrderType == OrderType.TrailingStop && request.StopPrice == 0 && !held)
            {
                // for held orders the brokerage will set it once it's triggered, from the market price at that time
                request.StopPrice = Orders.TrailingStopOrder.CalculateStopPrice(security.Price, request.TrailingAmount, request.TrailingAsPercentage,
                    request.Quantity > 0 ? OrderDirection.Buy : OrderDirection.Sell);
            }

            if (request.OrderType is OrderType.MarketOnOpen or OrderType.MarketOnClose)
            {
                InvalidateGoodTilDateTimeInForce(request.OrderProperties);
            }
            return PreOrderChecks(request);
        }

        /// <summary>
        /// Waits for the order to be processed, only for the orders which start working right away, not the ones held until another fills
        /// </summary>
        private void WaitForOrderIfRequired(SubmitOrderRequest request, OrderTicket ticket)
        {
            if (!request.Asynchronous && !IsHeld(request) && ticket.Status.IsOpen()
                && request.OrderType is OrderType.Market or OrderType.OptionExercise or OrderType.ComboMarket)
            {
                Transactions.WaitForOrder(ticket.OrderId);
            }
        }

        /// <summary>
        /// Whether the order is held by the brokerage until the order which triggers it fills
        /// </summary>
        private static bool IsHeld(SubmitOrderRequest request)
        {
            return request.Contingency?.IsWaitingForTrigger == true;
        }

        /// <summary>
        /// Converts a market order which would start working right away into a market on open/close order when required
        /// </summary>
        /// <returns>The warning to send once the converted order is submitted, null if it was not converted</returns>
        private Action ConvertMarketOrderIfRequired(SubmitOrderRequest request, Security security)
        {
            // For futures and FOPs, market orders can be submitted on extended hours, so we let them through.
            if (security.Type == SecurityType.Future || security.Type == SecurityType.FutureOption)
            {
                return null;
            }

            // When the market is closed the order is converted to fill at the next open (MarketOnOpen),
            // regardless of resolution.
            if (!security.Exchange.ExchangeOpen)
            {
                request.OrderType = OrderType.MarketOnOpen;
                return _isMarketOnOpenOrderWarningSent ? null : () =>
                {
                    if (!_isMarketOnOpenOrderWarningSent)
                    {
                        Debug("Warning: market orders submitted while the market is closed are automatically converted into MarketOnOpen orders to fill at the next market open.");
                        _isMarketOnOpenOrderWarningSent = true;
                    }
                };
            }

            // The market is open: only a security subscribed solely to daily resolution needs conversion, since
            // it has no fresh intraday price to fill against (it would otherwise fill at the stale previous
            // close). It is filled at today's close (MarketOnClose), or at the next open (MarketOnOpen) if we are
            // already within the MarketOnClose submission buffer.
            // This is only done in backtesting. In live trading an open-market market order fills at the current
            // market price, so we leave it as a regular market order. Markets that never close (e.g. crypto,
            // forex) have no open/close to convert to, so they are left as a regular market order too.
            if (!LiveMode && !security.Exchange.Hours.IsMarketAlwaysOpen && IsDailyResolutionOnly(security.Symbol))
            {
                request.OrderType = IsWithinMarketOnCloseSubmissionBuffer(security) ? OrderType.MarketOnOpen : OrderType.MarketOnClose;
                return _isDailyResolutionMarketOrderConversionWarningSent ? null : () =>
                {
                    if (!_isDailyResolutionMarketOrderConversionWarningSent)
                    {
                        Debug("Warning: market orders on daily resolution data sent during market hours are automatically converted into MarketOnClose orders (or MarketOnOpen near the close) to avoid filling at the stale previous close. Note: in live trading this conversion is not applied, as the order fills at the current market price.");
                        _isDailyResolutionMarketOrderConversionWarningSent = true;
                    }
                };
            }
            return null;
        }
    }
}
