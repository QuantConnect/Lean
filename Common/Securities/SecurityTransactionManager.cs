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
using static QuantConnect.StringExtensions;

namespace QuantConnect.Securities
{
    /// <summary>
    /// Algorithm Transactions Manager - Recording Transactions
    /// </summary>
    public class SecurityTransactionManager : IOrderProvider
    {
        private readonly Dictionary<DateTime, decimal> _transactionRecord;
        private readonly IAlgorithm _algorithm;
        private int _orderId;
        private readonly SecurityManager _securities;
        private const decimal _minimumOrderSize = 0;
        private const int _minimumOrderQuantity = 1;
        private TimeSpan _marketOrderFillTimeout = TimeSpan.FromSeconds(5);

        private IOrderProcessor _orderProcessor;

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
        /// <remarks>Will return a shallow copy, modifying the returned container
        /// will have no effect <see cref="AddTransactionRecord"/></remarks>
        public Dictionary<DateTime, decimal> TransactionRecord
        {
            get
            {
                lock (_transactionRecord)
                {
                    return new Dictionary<DateTime, decimal>(_transactionRecord);
                }
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
        /// Cancels all open orders for all symbols
        /// </summary>
        /// <returns>List containing the cancelled order tickets</returns>
        public List<OrderTicket> CancelOpenOrders()
        {
            if (_algorithm != null && _algorithm.IsWarmingUp)
            {
                throw new InvalidOperationException("This operation is not allowed in Initialize or during warm up: CancelOpenOrders. Please move this code to the OnWarmupFinished() method.");
            }

            var cancelledOrders = new List<OrderTicket>();
            foreach (var ticket in GetOpenOrderTickets())
            {
                ticket.Cancel($"Canceled by CancelOpenOrders() at {_algorithm.UtcTime:o}");
                cancelledOrders.Add(ticket);
            }
            return cancelledOrders;
        }

        /// <summary>
        /// Cancels all open orders for the specified symbol
        /// </summary>
        /// <param name="symbol">The symbol whose orders are to be cancelled</param>
        /// <param name="tag">Custom order tag</param>
        /// <returns>List containing the cancelled order tickets</returns>
        public List<OrderTicket> CancelOpenOrders(Symbol symbol, string tag = null)
        {
            if (_algorithm != null && _algorithm.IsWarmingUp)
            {
                throw new InvalidOperationException("This operation is not allowed in Initialize or during warm up: CancelOpenOrders. Please move this code to the OnWarmupFinished() method.");
            }

            var cancelledOrders = new List<OrderTicket>();
            foreach (var ticket in GetOpenOrderTickets(x => x.Symbol == symbol))
            {
                ticket.Cancel(tag);
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
        /// Gets an enumerable of <see cref="OrderTicket"/> matching the specified <paramref name="filter"/>
        /// </summary>
        /// <param name="filter">The filter predicate used to find the required order tickets</param>
        /// <returns>An enumerable of <see cref="OrderTicket"/> matching the specified <paramref name="filter"/></returns>
        public IEnumerable<OrderTicket> GetOrderTickets(Func<OrderTicket, bool> filter = null)
        {
            return _orderProcessor.GetOrderTickets(filter ?? (x => true));
        }

        /// <summary>
        /// Get an enumerable of open <see cref="OrderTicket"/> for the specified symbol
        /// </summary>
        /// <param name="symbol">The symbol for which to return the order tickets</param>
        /// <returns>An enumerable of open <see cref="OrderTicket"/>.</returns>
        public IEnumerable<OrderTicket> GetOpenOrderTickets(Symbol symbol)
        {
            return GetOpenOrderTickets(x => x.Symbol == symbol);
        }

        /// <summary>
        /// Gets an enumerable of opened <see cref="OrderTicket"/> matching the specified <paramref name="filter"/>
        /// </summary>
        /// <param name="filter">The filter predicate used to find the required order tickets</param>
        /// <returns>An enumerable of opened <see cref="OrderTicket"/> matching the specified <paramref name="filter"/></returns>
        public IEnumerable<OrderTicket> GetOpenOrderTickets(Func<OrderTicket, bool> filter = null)
        {
            return _orderProcessor.GetOpenOrderTickets(filter ?? (x => true));
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
                Log.Error(Invariant(
                    $"SecurityTransactionManager.WaitForOrder(): Unable to locate ticket for order: {orderId}"
                ));

                return false;
            }

            if (!orderTicket.OrderClosed.WaitOne(_marketOrderFillTimeout))
            {
                Log.Error(Invariant(
                    $"SecurityTransactionManager.WaitForOrder(): Order did not fill within {_marketOrderFillTimeout.TotalSeconds} seconds."
                ));

                return false;
            }

            return true;
        }

        /// <summary>
        /// Get a list of all open orders for a symbol.
        /// </summary>
        /// <param name="symbol">The symbol for which to return the orders</param>
        /// <returns>List of open orders.</returns>
        public List<Order> GetOpenOrders(Symbol symbol)
        {
            return GetOpenOrders(x => x.Symbol == symbol);
        }

        /// <summary>
        /// Gets open orders matching the specified filter. Specifying null will return an enumerable
        /// of all open orders.
        /// </summary>
        /// <param name="filter">Delegate used to filter the orders</param>
        /// <returns>All filtered open orders this order provider currently holds</returns>
        public List<Order> GetOpenOrders(Func<Order, bool> filter = null)
        {
            filter = filter ?? (x => true);
            return _orderProcessor.GetOpenOrders(x => filter(x));
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
        /// Gets all orders matching the specified filter. Specifying null will return an enumerable
        /// of all orders.
        /// </summary>
        /// <param name="filter">Delegate used to filter the orders</param>
        /// <returns>All orders this order provider currently holds by the specified filter</returns>
        public IEnumerable<Order> GetOrders(Func<Order, bool> filter)
        {
            return _orderProcessor.GetOrders(filter);
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
        /// Record the transaction value and time in a list to later be processed for statistics creation.
        /// </summary>
        /// <remarks>
        /// Bit of a hack -- but using datetime as dictionary key is dangerous as you can process multiple orders within a second.
        /// For the accounting / statistics generating purposes its not really critical to know the precise time, so just add a millisecond while there's an identical key.
        /// </remarks>
        /// <param name="time">Time of order processed </param>
        /// <param name="transactionProfitLoss">Profit Loss.</param>
        public void AddTransactionRecord(DateTime time, decimal transactionProfitLoss)
        {
            lock (_transactionRecord)
            {
                var clone = time;
                while (_transactionRecord.ContainsKey(clone))
                {
                    clone = clone.AddMilliseconds(1);
                }
                _transactionRecord.Add(clone, transactionProfitLoss);
            }
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
