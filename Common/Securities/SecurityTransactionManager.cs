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
    public class SecurityTransactionManager : IOrderMapping
    {
        private int _orderId = 1;
        private readonly SecurityManager _securities;
        private const decimal _minimumOrderSize = 0;
        private const int _minimumOrderQuantity = 1;
        private ConcurrentQueue<OrderRequest> _orderRequestQueue;
        private ConcurrentDictionary<int, Order> _orders;
        private ConcurrentDictionary<int, List<OrderEvent>> _orderEvents;
        private Dictionary<DateTime, decimal> _transactionRecord;
        private Dictionary<Guid, TaskCompletionSource<OrderResponse>> _orderResponseCompletions;
        private ConcurrentDictionary<TaskCompletionSource<Order>, Predicate<Order>> _orderCompletions;

        /// <summary>
        /// Initialise the transaction manager for holding and processing orders.
        /// </summary>
        public SecurityTransactionManager(SecurityManager security)
        {
            //Private reference for processing transactions
            _securities = security;

            //Initialise the Order Cache -- Its a mirror of the TransactionHandler.
            _orders = new ConcurrentDictionary<int, Order>();

            // Internal order events storage.
            _orderEvents = new ConcurrentDictionary<int, List<OrderEvent>>();

            //Interal storage for transaction records:
            _transactionRecord = new Dictionary<DateTime, decimal>();

            _orderRequestQueue = new ConcurrentQueue<OrderRequest>();

            _orderResponseCompletions = new Dictionary<Guid, TaskCompletionSource<OrderResponse>>();

            _orderCompletions = new ConcurrentDictionary<TaskCompletionSource<Order>, Predicate<Order>>();
        }

        /// <summary>
        /// Count of currently cached orders.
        /// </summary>
        public int CachedOrderCount
        {
            get { return _orders.Count; }
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
        /// Order event storage - a list of the order events attached to each order
        /// </summary>
        /// <remarks>Seems like a huge memory hog and may be removed, leaving OrderEvents to be disposable classes with no track record.</remarks>
        /// <seealso cref="Orders"/>
        /// <seealso cref="OrderQueue"/>
        public ConcurrentDictionary<int, List<OrderEvent>> OrderEvents
        {
            get
            {
                return _orderEvents;
            }
            set 
            {
                _orderEvents = value;
            }
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
        /// Main entry for order requests. All requests are guaranteed a response.
        /// </summary>
        /// <param name="request">Order request</param>
        /// <returns></returns>
        public Task<OrderResponse> ProcessOrderRequest(OrderRequest request)
        {
            var taskCompletion = new TaskCompletionSource<OrderResponse>();

            var response = new OrderResponse(request);

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

            if (response.Type == OrderResponseType.Error)
            {
                taskCompletion.TrySetResult(response);
                return taskCompletion.Task;
            }

            _orderResponseCompletions[request.Id] = taskCompletion;

            OrderRequestQueue.Enqueue(request);

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
            //Wait for the market order to fill.
            //This is processed in a parallel thread.
            while (!_orders.ContainsKey(orderId) ||
                   (_orders[orderId].Status != OrderStatus.Filled &&
                    _orders[orderId].Status != OrderStatus.Invalid &&
                    _orders[orderId].Status != OrderStatus.Canceled))
            {
                Thread.Sleep(1);
            }
        }

        /// <summary>
        /// Wait for certain conditions on an order
        /// </summary>
        /// <param name="filter">Order predicate</param>
        /// <returns>Task that results in order or null if filter throws exception</returns>
        public Task<Order> GetOrderAsync(Predicate<Order> filter)
        {
            var orderCompletion = new TaskCompletionSource<Order>();

            _orderCompletions[orderCompletion] = filter;

            ScanOrderCompletions();

            return orderCompletion.Task;
        }

        /// <summary>
        /// Wait for order to reach final state.
        /// </summary>
        /// <param name="orderId"></param>
        /// <returns></returns>
        public Task<Order> GetFinalOrderAsync(int orderId)
        {
            return GetOrderAsync(o => o.Id == orderId && o.Status.IsFinal());
        }

        /// <summary>
        /// Wait for order to reach completed state.
        /// </summary>
        /// <param name="orderId"></param>
        /// <returns></returns>
        public Task<Order> GetCompletedlOrderAsync(int orderId)
        {
            return GetOrderAsync(o => o.Id == orderId && o.Status.IsCompleted());
        }

        private void ScanOrderCompletions(Order searchOrder = null)
        {
            foreach (var pair in _orderCompletions.ToList())
            {
                try
                {
                    Order order = null;

                    if (searchOrder != null)
                    {
                        if (pair.Value(searchOrder))
                        {
                            order = searchOrder;
                        }
                    }
                    else
                    {
                        order =_orders.Values.FirstOrDefault(o => pair.Value(o));
                    }

                    if (order != null)
                    {
                        Predicate<Order> predicate;

                        _orderCompletions.TryRemove(pair.Key, out predicate);

                        pair.Key.TrySetResult(order);
                    }
                }
                catch
                {
                    Predicate<Order> predicate;

                    _orderCompletions.TryRemove(pair.Key, out predicate);

                    pair.Key.TrySetResult(null);
                }
            }
        }

        /// <summary>
        /// Process order updates from the engine.
        /// </summary>
        /// <param name="update">updated order</param>
        public void Process(Order update)
        {
            _orders[update.Id] = update;

            ScanOrderCompletions(update);
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
            var openOrders = (from order in _orders.Values
                where (order.Status == OrderStatus.Submitted ||
                       order.Status == OrderStatus.New)
                select order).ToList();

            return openOrders;
        }

        /// <summary>
        /// Get a list of all open orders.
        /// </summary>
        /// <returns>List of open orders.</returns>
        public List<Order> GetOrders(Predicate<Order> filter = null)
        {
            var result = (from order in _orders.Values
                              where filter == null || filter(order) == true
                              select order).ToList();

            return result;
        } 

        /// <summary>
        /// Get the order by its id
        /// </summary>
        /// <param name="orderId">Order id to fetch</param>
        /// <returns>The order with the specified id, or null if no match is found</returns>
        public Order GetOrderById(int orderId)
        {
            try
            {
                Order order;
                // then check permanent storage
                if (_orders.TryGetValue(orderId, out order))
                {
                    return order;
                }
            }
            catch (Exception err)
            {
                Log.Error("TransactionManager.GetOrderById(): " + err.Message);
            }
            return null;
        }

        /// <summary>
        /// Gets the order by its brokerage id
        /// </summary>
        /// <param name="brokerageId">The brokerage id to fetch</param>
        /// <returns>The first order matching the brokerage id, or null if no match is found</returns>
        public Order GetOrderByBrokerageId(int brokerageId)
        {
            try
            {
                return _orders.FirstOrDefault(x => x.Value.BrokerId.Contains(brokerageId)).Value;
            }
            catch (Exception err)
            {
                Log.Error("TransactionManager.GetOrderByBrokerageId(): " + err.Message);
                return null;
            }
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
    } // End Algorithm Transaction Filling Classes
} // End QC Namespace
