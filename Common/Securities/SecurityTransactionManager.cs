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
using QuantConnect.Logging;
using QuantConnect.Orders;

namespace QuantConnect.Securities 
{
    /// <summary>
    /// Algorithm Transactions Manager - Recording Transactions
    /// </summary>
    public class SecurityTransactionManager : IOrderProvider
    {
        private int _orderId;
        private readonly SecurityManager _securities;
        private const decimal _minimumOrderSize = 0;
        private const int _minimumOrderQuantity = 1;

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
        public SecurityTransactionManager(SecurityManager security)
        {
            //Private reference for processing transactions
            _securities = security;

            //Interal storage for transaction records:
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
        /// Processes the order request
        /// </summary>
        /// <param name="request">The request to be processed</param>
        /// <returns>The order ticket for the request</returns>
        public OrderTicket ProcessRequest(OrderRequest request)
        {
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
        public OrderTicket CancelOrder(int orderId)
        {
            return RemoveOrder(orderId);
        }

        /// <summary>
        /// Remove this order from outstanding queue: user is requesting a cancel.
        /// </summary>
        /// <param name="orderId">Specific order id to remove</param>
        public OrderTicket RemoveOrder(int orderId)
        {
            return ProcessRequest(new CancelOrderRequest(_securities.UtcTime, orderId, string.Empty));
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
        /// Wait for a specific order to be either Filled, Invalid or Canceled
        /// </summary>
        /// <param name="orderId">The id of the order to wait for</param>
        public void WaitForOrder(int orderId)
        {
            // wait for the processor to finish processing his orders
            while(true)
            {
                var order = GetOrderById(orderId);
                if (order == null || !Completed(order))
                {
                    if (order != null && order.Type != OrderType.Market)
                    {
                        // can't wait for non-market orders to fill
                        return;
                    }
                    Thread.Sleep(1);
                }
                else
                {
                    break;
                }
            }

            // wait for the processor to finish processing the order
            _orderProcessor.ProcessingCompletedEvent.Wait();
        }

        /// <summary>
        /// Get a list of all open orders.
        /// </summary>
        /// <returns>List of open orders.</returns>
        public List<Order> GetOpenOrders()
        {
            return _orderProcessor.GetOrders(x => x.Status == OrderStatus.Submitted || x.Status == OrderStatus.New).ToList();
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
        public Order GetOrderByBrokerageId(int brokerageId)
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
        /// <returns>True if suficient capital.</returns>
        public bool GetSufficientCapitalForOrder(SecurityPortfolioManager portfolio, Order order)
        {
            var security = _securities[order.Symbol];
            
            var freeMargin = security.MarginModel.GetMarginRemaining(portfolio, security, order.Direction);
            var initialMarginRequiredForOrder = security.MarginModel.GetInitialMarginRequiredForOrder(security, order);
            if (Math.Abs(initialMarginRequiredForOrder) > freeMargin)
            {
                Log.Error(string.Format("Transactions.GetSufficientCapitalForOrder(): Id: {0}, Initial Margin: {1}, Free Margin: {2}", order.Id, initialMarginRequiredForOrder, freeMargin));
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
            return order.Status == OrderStatus.Filled || order.Status == OrderStatus.Invalid || order.Status == OrderStatus.Canceled;
        }
    }
}
