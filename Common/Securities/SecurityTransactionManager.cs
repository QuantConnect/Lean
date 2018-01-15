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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Orders;

namespace QuantConnect.Securities
{
    /// <summary>
    /// Algorithm Transactions Manager - Recording Transactions
    /// </summary>
    public class SecurityTransactionManager : IOrderProvider
    {
        private readonly IAlgorithm _algorithm;
        private int _orderId;
        private readonly SecurityManager _securities;
        private const decimal _minimumOrderSize = 0;
        private const int _minimumOrderQuantity = 1;
        private TimeSpan _marketOrderFillTimeout = TimeSpan.FromSeconds(5);

        private IOrderProcessor _orderProcessor;
        private Dictionary<DateTime, decimal> _transactionRecord;

        /// <summary>
        /// Gets the time the security information was last updated
        /// </summary>
        public DateTime UtcTime
        {
            get { return _securities.UtcTime; }
        }

        /// <summary>
        /// Initialise the transaction manager for holding and processing orders.
        /// </summary>
        public SecurityTransactionManager(IAlgorithm algorithm, SecurityManager security)
        {
            _algorithm = algorithm;

            //Private reference for processing transactions
            _securities = security;

            //Internal storage for transaction records:
            _transactionRecord = new Dictionary<DateTime, decimal>();
        }

        /// <summary>
        /// Trade record of profits and losses for each trade statistics calculations
        /// </summary>
        public Dictionary<DateTime, decimal> TransactionRecord
        {
            get
            {
                return _transactionRecord;
            }
            set
            {
                _transactionRecord = value;
            }
        }

        /// <summary>
        /// Configurable minimum order value to ignore bad orders, or orders with unrealistic sizes
        /// </summary>
        /// <remarks>Default minimum order size is $0 value</remarks>
        public decimal MinimumOrderSize
        {
            get
            {
                return _minimumOrderSize;
            }
        }

        /// <summary>
        /// Configurable minimum order size to ignore bad orders, or orders with unrealistic sizes
        /// </summary>
        /// <remarks>Default minimum order size is 0 shares</remarks>
        public int MinimumOrderQuantity
        {
            get
            {
                return _minimumOrderQuantity;
            }
        }

        /// <summary>
        /// Get the last order id.
        /// </summary>
        public int LastOrderId
        {
            get
            {
                return _orderId;
            }
        }

        /// <summary>
        /// Configurable timeout for market order fills
        /// </summary>
        /// <remarks>Default value is 5 seconds</remarks>
        public TimeSpan MarketOrderFillTimeout
        {
            get
            {
                return _marketOrderFillTimeout;
            }
            set
            {
                _marketOrderFillTimeout = value;
            }
        }

        /// <summary>
        /// Processes the order request
        /// </summary>
        /// <param name="request">The request to be processed</param>
        /// <returns>The order ticket for the request</returns>
        public OrderTicket ProcessRequest(OrderRequest request)
        {
            if (_algorithm != null && _algorithm.IsWarmingUp)
            {
                throw new Exception(OrderResponse.WarmingUp(request).ToString());
            }

            var submit = request as SubmitOrderRequest;
            if (submit != null)
            {
                submit.SetOrderId(GetIncrementOrderId());
            }
            return _orderProcessor.Process(request);
        }

        /// <summary>
        /// Add an order to collection and return the unique order id or negative if an error.
        /// </summary>
        /// <param name="request">A request detailing the order to be submitted</param>
        /// <returns>New unique, increasing orderid</returns>
        public OrderTicket AddOrder(SubmitOrderRequest request)
        {
            return ProcessRequest(request);
        }

        /// <summary>
        /// Update an order yet to be filled such as stop or limit orders.
        /// </summary>
        /// <param name="request">Request detailing how the order should be updated</param>
        /// <remarks>Does not apply if the order is already fully filled</remarks>
        public OrderTicket UpdateOrder(UpdateOrderRequest request)
        {
            return ProcessRequest(request);
        }

        /// <summary>
        /// Added alias for RemoveOrder -
        /// </summary>
        /// <param name="orderId">Order id we wish to cancel</param>
        /// <param name="orderTag">Tag to indicate from where this method was called</param>
        public OrderTicket CancelOrder(int orderId, string orderTag = null)
        {
            return RemoveOrder(orderId, orderTag);
        }

        /// <summary>
        /// Cancels all open orders for the specified symbol
        /// </summary>
        /// <param name="symbol">The symbol whose orders are to be cancelled</param>
        /// <returns>List containing the cancelled order tickets</returns>
        public List<OrderTicket> CancelOpenOrders(Symbol symbol)
        {
            if (_algorithm != null && _algorithm.IsWarmingUp)
            {
                throw new Exception("This operation is not allowed in Initialize or during warm up: CancelOpenOrders. Please move this code to the OnWarmupFinished() method.");
            }

            var cancelledOrders = new List<OrderTicket>();
            foreach (var ticket in GetOrderTickets(x => x.Symbol == symbol && x.Status.IsOpen()))
            {
                ticket.Cancel();
                cancelledOrders.Add(ticket);
            }
            return cancelledOrders;
        }

        /// <summary>
        /// Remove this order from outstanding queue: user is requesting a cancel.
        /// </summary>
        /// <param name="orderId">Specific order id to remove</param>
        /// <param name="tag">Tag request</param>
        public OrderTicket RemoveOrder(int orderId, string tag = null)
        {
            return ProcessRequest(new CancelOrderRequest(_securities.UtcTime, orderId, tag ?? string.Empty));
        }

        /// <summary>
        /// Gets and enumerable of <see cref="OrderTicket"/> matching the specified <paramref name="filter"/>
        /// </summary>
        /// <param name="filter">The filter predicate used to find the required order tickets</param>
        /// <returns>An enumerable of <see cref="OrderTicket"/> matching the specified <paramref name="filter"/></returns>
        public IEnumerable<OrderTicket> GetOrderTickets(Func<OrderTicket, bool> filter = null)
        {
            return _orderProcessor.GetOrderTickets(filter ?? (x => true));
        }

        /// <summary>
        /// Gets the order ticket for the specified order id. Returns null if not found
        /// </summary>
        /// <param name="orderId">The order's id</param>
        /// <returns>The order ticket with the specified id, or null if not found</returns>
        public OrderTicket GetOrderTicket(int orderId)
        {
            return _orderProcessor.GetOrderTicket(orderId);
        }

        /// <summary>
        /// Wait for a specific order to be either Filled, Invalid or Canceled
        /// </summary>
        /// <param name="orderId">The id of the order to wait for</param>
        /// <returns>True if we successfully wait for the fill, false if we were unable
        /// to wait. This may be because it is not a market order or because the timeout
        /// was reached</returns>
        public bool WaitForOrder(int orderId)
        {
            var orderTicket = GetOrderTicket(orderId);
            if (orderTicket == null)
            {
                Log.Error("SecurityTransactionManager.WaitForOrder(): Unable to locate ticket for order: " + orderId);
                return false;
            }

            if (!orderTicket.OrderClosed.WaitOne(_marketOrderFillTimeout))
            {
                Log.Error("SecurityTransactionManager.WaitForOrder(): Order did not fill within {0} seconds.", _marketOrderFillTimeout.TotalSeconds);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Get a list of all open orders.
        /// </summary>
        /// <returns>List of open orders.</returns>
        public List<Order> GetOpenOrders()
        {
            return _orderProcessor.GetOrders(x => x.Status.IsOpen()).ToList();
        }

        /// <summary>
        /// Get a list of all open orders for a symbol.
        /// </summary>
        /// <param name="symbol">The symbol for which to return the orders</param>
        /// <returns>List of open orders.</returns>
        public List<Order> GetOpenOrders(Symbol symbol)
        {
            return _orderProcessor.GetOrders(x => x.Symbol == symbol && x.Status.IsOpen()).ToList();
        }

        /// <summary>
        /// Gets the current number of orders that have been processed
        /// </summary>
        public int OrdersCount
        {
            get { return _orderProcessor.OrdersCount; }
        }

        /// <summary>
        /// Get the order by its id
        /// </summary>
        /// <param name="orderId">Order id to fetch</param>
        /// <returns>The order with the specified id, or null if no match is found</returns>
        public Order GetOrderById(int orderId)
        {
            return _orderProcessor.GetOrderById(orderId);
        }

        /// <summary>
        /// Gets the order by its brokerage id
        /// </summary>
        /// <param name="brokerageId">The brokerage id to fetch</param>
        /// <returns>The first order matching the brokerage id, or null if no match is found</returns>
        public Order GetOrderByBrokerageId(string brokerageId)
        {
            return _orderProcessor.GetOrderByBrokerageId(brokerageId);
        }

        /// <summary>
        /// Gets all orders matching the specified filter
        /// </summary>
        /// <param name="filter">Delegate used to filter the orders</param>
        /// <returns>All open orders this order provider currently holds</returns>
        public IEnumerable<Order> GetOrders(Func<Order, bool> filter)
        {
            return _orderProcessor.GetOrders(filter);
        }

        /// <summary>
        /// Check if there is sufficient capital to execute this order.
        /// </summary>
        /// <param name="portfolio">Our portfolio</param>
        /// <param name="order">Order we're checking</param>
        /// <returns>True if sufficient capital.</returns>
        public bool GetSufficientCapitalForOrder(SecurityPortfolioManager portfolio, Order order)
        {
            // short circuit the div 0 case
            if (order.Quantity == 0) return true;

            var security = _securities[order.Symbol];

            var ticket = GetOrderTicket(order.Id);
            if (ticket == null)
            {
                Log.Error("SecurityTransactionManager.GetSufficientCapitalForOrder(): Null order ticket for id: " + order.Id);
                return false;
            }

            if (order.Type == OrderType.OptionExercise)
            {
                // for option assignment and exercise orders we look into the requirements to process the underlying security transaction
                var option = (Option.Option)security;
                var underlying = option.Underlying;

                if (option.IsAutoExercised(underlying.Close))
                {
                    var quantity = option.GetExerciseQuantity(order.Quantity);

                    var newOrder = new LimitOrder
                    {
                        Id = order.Id,
                        Time = order.Time,
                        LimitPrice = option.StrikePrice,
                        Symbol = underlying.Symbol,
                        Quantity = option.Symbol.ID.OptionRight == OptionRight.Call ? quantity : -quantity
                    };

                    // we continue with this call for underlying
                    return GetSufficientCapitalForOrder(portfolio, newOrder);
                }

                return true;
            }

            // When order only reduces or closes a security position, capital is always sufficient
            if (security.Holdings.Quantity * order.Quantity < 0 && Math.Abs(security.Holdings.Quantity) >= Math.Abs(order.Quantity)) return true;

            var freeMargin = security.MarginModel.GetMarginRemaining(portfolio, security, order.Direction);
            var initialMarginRequiredForOrder = security.MarginModel.GetInitialMarginRequiredForOrder(security, order);

            // pro-rate the initial margin required for order based on how much has already been filled
            var percentUnfilled = (Math.Abs(order.Quantity) - Math.Abs(ticket.QuantityFilled))/Math.Abs(order.Quantity);
            var initialMarginRequiredForRemainderOfOrder = percentUnfilled*initialMarginRequiredForOrder;

            if (Math.Abs(initialMarginRequiredForRemainderOfOrder) > freeMargin)
            {
                Log.Error(string.Format("SecurityTransactionManager.GetSufficientCapitalForOrder(): Id: {0}, Initial Margin: {1}, Free Margin: {2}", order.Id, initialMarginRequiredForOrder, freeMargin));
                return false;
            }
            return true;
        }

        /// <summary>
        /// Get a new order id, and increment the internal counter.
        /// </summary>
        /// <returns>New unique int order id.</returns>
        public int GetIncrementOrderId()
        {
            return Interlocked.Increment(ref _orderId);
        }

        /// <summary>
        /// Sets the <see cref="IOrderProvider"/> used for fetching orders for the algorithm
        /// </summary>
        /// <param name="orderProvider">The <see cref="IOrderProvider"/> to be used to manage fetching orders</param>
        public void SetOrderProcessor(IOrderProcessor orderProvider)
        {
            _orderProcessor = orderProvider;
        }

        /// <summary>
        /// Returns true when the specified order is in a completed state
        /// </summary>
        private static bool Completed(Order order)
        {
            return order.Status == OrderStatus.Filled || order.Status == OrderStatus.PartiallyFilled || order.Status == OrderStatus.Invalid || order.Status == OrderStatus.Canceled;
        }
    }
}
