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
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using QuantConnect.Logging;
using QuantConnect.Orders;

namespace QuantConnect.Securities
{
    /// <summary>
    /// Algorithm Transactions Manager - Recording Transactions
    /// </summary>
    public class SecurityTransactionManager : IOrderProvider
    {
        private int _orderId = 1;
        private readonly SecurityManager _securities;
        private const decimal _minimumOrderSize = 0;
        private const int _minimumOrderQuantity = 1;

        private IOrderProcessor _orderProcessor;
        private ConcurrentQueue<OrderRequest> _orderRequestQueue;

        private Dictionary<DateTime, decimal> _transactionRecord;
        private Dictionary<Guid, TaskCompletionSource<OrderResponse>> _orderResponseCompletions;

        /// <summary>
        /// Initialise the transaction manager for holding and processing orders.
        /// </summary>
        public SecurityTransactionManager(SecurityManager security)
        {
            //Private reference for processing transactions
            _securities = security;

            //Interal storage for transaction records:
            _transactionRecord = new Dictionary<DateTime, decimal>();

            _orderRequestQueue = new ConcurrentQueue<OrderRequest>();

            _orderResponseCompletions = new Dictionary<Guid, TaskCompletionSource<OrderResponse>>();
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
        /// Temporary storage for orders requests while waiting to process via transaction handler. Once processed, orders are updated at transaction manager.
        /// </summary>
        /// <seealso cref="Orders"/>
        public ConcurrentQueue<OrderRequest> OrderRequestQueue
        {
            get
            {
                return _orderRequestQueue;
            }
            set
            {
                _orderRequestQueue = value;
            }
        }

        /// <summary>
        /// Main entry for order requests. All requests are guaranteed a response.
        /// </summary>
        /// <param name="request">Order request</param>
        /// <returns>Order response. Check error for details.</returns>
        public Task<OrderResponse> ProcessOrderRequest(OrderRequest request)
        {
            var taskCompletion = new TaskCompletionSource<OrderResponse>();

            var response = new OrderResponse(request);

            if (response.IsError == false)
            {
                if (request is UpdateOrderRequest)
                {
                    var updateRequest = (UpdateOrderRequest)request;

                    var order = GetOrderById(updateRequest.OrderId);

                    if (order == null)
                    {
                        response.Error(OrderResponseErrorCode.UnableToFindOrder);
                    }
                    else if (updateRequest.Quantity == 0)
                    {
                        response.Error(OrderResponseErrorCode.OrderQuantityZero);
                    }
                    else
                    {
                        updateRequest.Created = _securities[order.Symbol].Time;
                    }

                }
                else if (request is SubmitOrderRequest)
                {
                    var submitRequest = (SubmitOrderRequest)request;

                    if (submitRequest.Quantity == 0)
                    {
                        response.Error(OrderResponseErrorCode.OrderQuantityZero);
                    }
                    else
                    {
                        submitRequest.OrderId = _orderId++;
                        submitRequest.Created = _securities[submitRequest.Symbol].Time;
                    }
                }
                else if (request is CancelOrderRequest)
                {
                    var order = GetOrderById(request.OrderId);

                    if (order == null)
                    {
                        response.Error(OrderResponseErrorCode.UnableToFindOrder);
                    }
                    else if (order.Status.IsOpen() == false)
                    {
                        response.Error(OrderResponseErrorCode.InvalidOrderStatus);
                    }
                    else
                    {
                        request.Created = _securities[order.Symbol].Time;
                    }
                }
                else
                {
                    response.Error(OrderResponseErrorCode.UnsupportedRequestType);
                }

            }

            if (response.IsError)
            {
                taskCompletion.TrySetResult(response);
                return taskCompletion.Task;
            }

            _orderResponseCompletions[request.Id] = taskCompletion;

            _orderRequestQueue.Enqueue(request);

            return taskCompletion.Task;
        }

        /// <summary>
        /// Add submit order request to queue and return the unique order id or negative if an error.
        /// </summary>
        /// <param name="request">Submit order request to add to processing list</param>
        /// <returns>OrderResponse. Check ErrorCode for details.</returns>
        public OrderResponse SubmitOrder(SubmitOrderRequest request)
        {
            return ProcessOrderRequest(request).Result;
        }

        /// <summary>
        /// Update an order yet to be filled such as stop or limit orders.
        /// </summary>
        /// <param name="request">Order to Update</param>
        /// <remarks>Does not apply if the order is already fully filled</remarks>
        /// <returns>
        ///     OrderResponse. Check ErrorCode for details.
        /// </returns>
        public OrderResponse UpdateOrder(UpdateOrderRequest request)
        {
            return ProcessOrderRequest(request).Result;
        }

        /// <summary>
        /// Cancel Order
        /// </summary>
        /// <param name="orderId">Order id we wish to cancel</param>
        public OrderResponse CancelOrder(int orderId)
        {
            var request = new CancelOrderRequest
            {
                Id = Guid.NewGuid(),
                OrderId = orderId
            };

            return ProcessOrderRequest(request).Result;
        }

        /// <summary>
        /// Wait for a specific order to be either Filled, Invalid or Canceled
        /// </summary>
        /// <param name="orderId">The id of the order to wait for</param>
        public void WaitForOrder(int orderId)
        {
            // wait for the processor to finish processing his orders
            while (true)
            {
                var order = GetOrderById(orderId);
                if (order == null || !Completed(order))
                {
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
        /// Process order responses from the engine
        /// </summary>
        /// <param name="response"></param>
        public void Process(OrderResponse response)
        {
            TaskCompletionSource<OrderResponse> taskCompletion;

            if (_orderResponseCompletions.TryGetValue(response.Id, out taskCompletion) == true)
            {
                _orderResponseCompletions.Remove(response.Id);

                taskCompletion.TrySetResult(response);
            }
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
            return _orderId++;
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
