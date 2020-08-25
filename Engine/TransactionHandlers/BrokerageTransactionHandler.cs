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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using QuantConnect.Brokerages;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;
using QuantConnect.Util;

namespace QuantConnect.Lean.Engine.TransactionHandlers
{
    /// <summary>
    /// Transaction handler for all brokerages
    /// </summary>
    public class BrokerageTransactionHandler : ITransactionHandler
    {
        private IAlgorithm _algorithm;
        private IBrokerage _brokerage;

        // Counter to keep track of total amount of processed orders
        private int _totalOrderCount;

        // this bool is used to check if the warning message for the rounding of order quantity has been displayed for the first time
        private bool _firstRoundOffMessage = false;

        // this value is used for determining how confident we are in our cash balance update
        private long _lastFillTimeTicks;

        private const int MaxCashSyncAttempts = 5;
        private int _failedCashSyncAttempts;

        /// <summary>
        /// OrderQueue holds the newly updated orders from the user algorithm waiting to be processed. Once
        /// orders are processed they are moved into the Orders queue awaiting the brokerage response.
        /// </summary>
        protected IBusyCollection<OrderRequest> _orderRequestQueue;

        private Thread _processingThread;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private readonly ConcurrentQueue<OrderEvent> _orderEvents = new ConcurrentQueue<OrderEvent>();

        /// <summary>
        /// The _completeOrders dictionary holds all orders.
        /// Once the transaction thread has worked on them they get put here while witing for fill updates.
        /// </summary>
        private readonly ConcurrentDictionary<int, Order> _completeOrders = new ConcurrentDictionary<int, Order>();

        /// <summary>
        /// The orders dictionary holds orders which are open. Status: New, Submitted, PartiallyFilled, None, CancelPending
        /// Once the transaction thread has worked on them they get put here while witing for fill updates.
        /// </summary>
        private readonly ConcurrentDictionary<int, Order> _openOrders = new ConcurrentDictionary<int, Order>();

        /// <summary>
        /// The _openOrderTickets dictionary holds open order tickets that the algorithm can use to reference a specific order. This
        /// includes invoking update and cancel commands. In the future, we can add more features to the ticket, such as events
        /// and async events (such as run this code when this order fills)
        /// </summary>
        private readonly ConcurrentDictionary<int, OrderTicket> _openOrderTickets = new ConcurrentDictionary<int, OrderTicket>();

        /// <summary>
        /// The _completeOrderTickets dictionary holds all order tickets that the algorithm can use to reference a specific order. This
        /// includes invoking update and cancel commands. In the future, we can add more features to the ticket, such as events
        /// and async events (such as run this code when this order fills)
        /// </summary>
        private readonly ConcurrentDictionary<int, OrderTicket> _completeOrderTickets = new ConcurrentDictionary<int, OrderTicket>();

        /// <summary>
        /// The _cancelPendingOrders instance will help to keep track of CancelPending orders and their Status
        /// </summary>
        protected readonly CancelPendingOrders _cancelPendingOrders = new CancelPendingOrders();

        private IResultHandler _resultHandler;

        private readonly object _lockHandleOrderEvent = new object();

        /// <summary>
        /// Event fired when there is a new <see cref="OrderEvent"/>
        /// </summary>
        public event EventHandler<OrderEvent> NewOrderEvent;

        /// <summary>
        /// Gets the permanent storage for all orders
        /// </summary>
        public ConcurrentDictionary<int, Order> Orders
        {
            get
            {
                return _completeOrders;
            }
        }

        /// <summary>
        /// Gets all order events
        /// </summary>
        public IEnumerable<OrderEvent> OrderEvents => _orderEvents;

        /// <summary>
        /// Gets the permanent storage for all order tickets
        /// </summary>
        public ConcurrentDictionary<int, OrderTicket> OrderTickets
        {
            get
            {
                return _completeOrderTickets;
            }
        }

        /// <summary>
        /// Gets the current number of orders that have been processed
        /// </summary>
        public int OrdersCount => _totalOrderCount;

        /// <summary>
        /// Creates a new BrokerageTransactionHandler to process orders using the specified brokerage implementation
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="brokerage">The brokerage implementation to process orders and fire fill events</param>
        /// <param name="resultHandler"></param>
        public virtual void Initialize(IAlgorithm algorithm, IBrokerage brokerage, IResultHandler resultHandler)
        {
            if (brokerage == null)
            {
                throw new ArgumentNullException("brokerage");
            }
            // multi threaded queue, used for live deployments
            _orderRequestQueue = new BusyBlockingCollection<OrderRequest>();
            // we don't need to do this today because we just initialized/synced
            _resultHandler = resultHandler;

            _brokerage = brokerage;
            _brokerage.OrderStatusChanged += (sender, orderEvent) =>
            {
                HandleOrderEvent(orderEvent);
            };

            _brokerage.AccountChanged += (sender, account) =>
            {
                HandleAccountChanged(account);
            };

            _brokerage.OptionPositionAssigned += (sender, fill) =>
            {
                HandlePositionAssigned(fill);
            };

            IsActive = true;

            _algorithm = algorithm;
            InitializeTransactionThread();
        }

        /// <summary>
        /// Create and start the transaction thread, who will be in charge of processing
        /// the order requests
        /// </summary>
        protected virtual void InitializeTransactionThread()
        {
            _processingThread = new Thread(Run) { IsBackground = true, Name = "Transaction Thread" };
            _processingThread.Start();
        }

        /// <summary>
        /// Boolean flag indicating the Run thread method is busy.
        /// False indicates it is completely finished processing and ready to be terminated.
        /// </summary>
        public bool IsActive { get; private set; }

        #region Order Request Processing

        /// <summary>
        /// Adds the specified order to be processed
        /// </summary>
        /// <param name="request">The order to be processed</param>
        public OrderTicket Process(OrderRequest request)
        {
            if (_algorithm.LiveMode)
            {
                Log.Trace("BrokerageTransactionHandler.Process(): " + request);

                _algorithm.Portfolio.LogMarginInformation(request);
            }

            switch (request.OrderRequestType)
            {
                case OrderRequestType.Submit:
                    return AddOrder((SubmitOrderRequest)request);

                case OrderRequestType.Update:
                    return UpdateOrder((UpdateOrderRequest)request);

                case OrderRequestType.Cancel:
                    return CancelOrder((CancelOrderRequest)request);

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Add an order to collection and return the unique order id or negative if an error.
        /// </summary>
        /// <param name="request">A request detailing the order to be submitted</param>
        /// <returns>New unique, increasing orderid</returns>
        public OrderTicket AddOrder(SubmitOrderRequest request)
        {
            var response = !_algorithm.IsWarmingUp
                ? OrderResponse.Success(request)
                : OrderResponse.WarmingUp(request);

            request.SetResponse(response);
            var ticket = new OrderTicket(_algorithm.Transactions, request);

            Interlocked.Increment(ref _totalOrderCount);
            // send the order to be processed after creating the ticket
            if (response.IsSuccess)
            {
                _openOrderTickets.TryAdd(ticket.OrderId, ticket);
                _completeOrderTickets.TryAdd(ticket.OrderId, ticket);
                _orderRequestQueue.Add(request);

                // wait for the transaction handler to set the order reference into the new order ticket,
                // so we can ensure the order has already been added to the open orders,
                // before returning the ticket to the algorithm.
                WaitForOrderSubmission(ticket);
            }
            else
            {
                // add it to the orders collection for recall later
                var order = Order.CreateOrder(request);

                // ensure the order is tagged with a currency
                var security = _algorithm.Securities[order.Symbol];
                order.PriceCurrency = security.SymbolProperties.QuoteCurrency;

                order.Status = OrderStatus.Invalid;
                order.Tag = "Algorithm warming up.";
                ticket.SetOrder(order);
                _completeOrderTickets.TryAdd(ticket.OrderId, ticket);
                _completeOrders.TryAdd(order.Id, order);
            }
            return ticket;
        }

        /// <summary>
        /// Wait for the order to be handled by the <see cref="_processingThread"/>
        /// </summary>
        /// <param name="ticket">The <see cref="OrderTicket"/> expecting to be submitted</param>
        protected virtual void WaitForOrderSubmission(OrderTicket ticket)
        {
            var orderSetTimeout = Time.OneSecond;
            if (!ticket.OrderSet.WaitOne(orderSetTimeout))
            {
                Log.Error("BrokerageTransactionHandler.WaitForOrderSubmission(): " +
                    $"The order request (Id={ticket.OrderId}) was not submitted within {orderSetTimeout.TotalSeconds} second(s).");
            }
        }

        /// <summary>
        /// Update an order yet to be filled such as stop or limit orders.
        /// </summary>
        /// <param name="request">Request detailing how the order should be updated</param>
        /// <remarks>Does not apply if the order is already fully filled</remarks>
        public OrderTicket UpdateOrder(UpdateOrderRequest request)
        {
            OrderTicket ticket;
            if (!_completeOrderTickets.TryGetValue(request.OrderId, out ticket))
            {
                return OrderTicket.InvalidUpdateOrderId(_algorithm.Transactions, request);
            }

            ticket.AddUpdateRequest(request);

            try
            {
                //Update the order from the behaviour
                var order = GetOrderByIdInternal(request.OrderId);
                if (order == null)
                {
                    // can't update an order that doesn't exist!
                    request.SetResponse(OrderResponse.UnableToFindOrder(request));
                }
                else if (order.Status.IsClosed())
                {
                    // can't update a completed order
                    request.SetResponse(OrderResponse.InvalidStatus(request, order));
                }
                else if (request.Quantity.HasValue && request.Quantity.Value == 0)
                {
                    request.SetResponse(OrderResponse.ZeroQuantity(request));
                }
                else if (_algorithm.IsWarmingUp)
                {
                    request.SetResponse(OrderResponse.WarmingUp(request));
                }
                else
                {
                    request.SetResponse(OrderResponse.Success(request), OrderRequestStatus.Processing);
                    _orderRequestQueue.Add(request);
                }
            }
            catch (Exception err)
            {
                Log.Error(err);
                request.SetResponse(OrderResponse.Error(request, OrderResponseErrorCode.ProcessingError, err.Message));
            }

            return ticket;
        }

        /// <summary>
        /// Remove this order from outstanding queue: user is requesting a cancel.
        /// </summary>
        /// <param name="request">Request containing the specific order id to remove</param>
        public OrderTicket CancelOrder(CancelOrderRequest request)
        {
            OrderTicket ticket;
            if (!_completeOrderTickets.TryGetValue(request.OrderId, out ticket))
            {
                Log.Error("BrokerageTransactionHandler.CancelOrder(): Unable to locate ticket for order.");
                return OrderTicket.InvalidCancelOrderId(_algorithm.Transactions, request);
            }

            try
            {
                // if we couldn't set this request as the cancellation then another thread/someone
                // else is already doing it or it in fact has already been cancelled
                if (!ticket.TrySetCancelRequest(request))
                {
                    // the ticket has already been cancelled
                    request.SetResponse(OrderResponse.Error(request, OrderResponseErrorCode.InvalidRequest, "Cancellation is already in progress."));
                    return ticket;
                }

                //Error check
                var order = GetOrderByIdInternal(request.OrderId);
                if (order != null && request.Tag != null)
                {
                    order.Tag = request.Tag;
                }
                if (order == null)
                {
                    Log.Error("BrokerageTransactionHandler.CancelOrder(): Cannot find this id.");
                    request.SetResponse(OrderResponse.UnableToFindOrder(request));
                }
                else if (order.Status.IsClosed())
                {
                    Log.Error("BrokerageTransactionHandler.CancelOrder(): Order already " + order.Status);
                    request.SetResponse(OrderResponse.InvalidStatus(request, order));
                }
                else if (_algorithm.IsWarmingUp)
                {
                    request.SetResponse(OrderResponse.WarmingUp(request));
                }
                else
                {
                    _cancelPendingOrders.Set(order.Id, order.Status);
                    // update the order status
                    order.Status = OrderStatus.CancelPending;

                    // notify the algorithm with an order event
                    HandleOrderEvent(new OrderEvent(order,
                        _algorithm.UtcTime,
                        OrderFee.Zero));

                    // send the request to be processed
                    request.SetResponse(OrderResponse.Success(request), OrderRequestStatus.Processing);
                    _orderRequestQueue.Add(request);
                }
            }
            catch (Exception err)
            {
                Log.Error(err);
                request.SetResponse(OrderResponse.Error(request, OrderResponseErrorCode.ProcessingError, err.Message));
            }

            return ticket;
        }

        /// <summary>
        /// Gets and enumerable of <see cref="OrderTicket"/> matching the specified <paramref name="filter"/>
        /// </summary>
        /// <param name="filter">The filter predicate used to find the required order tickets</param>
        /// <returns>An enumerable of <see cref="OrderTicket"/> matching the specified <paramref name="filter"/></returns>
        public IEnumerable<OrderTicket> GetOrderTickets(Func<OrderTicket, bool> filter = null)
        {
            return _completeOrderTickets.Select(x => x.Value).Where(filter ?? (x => true));
        }

        /// <summary>
        /// Gets and enumerable of opened <see cref="OrderTicket"/> matching the specified <paramref name="filter"/>
        /// </summary>
        /// <param name="filter">The filter predicate used to find the required order tickets</param>
        /// <returns>An enumerable of opened <see cref="OrderTicket"/> matching the specified <paramref name="filter"/></returns>
        public IEnumerable<OrderTicket> GetOpenOrderTickets(Func<OrderTicket, bool> filter = null)
        {
            return _openOrderTickets.Select(x => x.Value).Where(filter ?? (x => true));
        }

        /// <summary>
        /// Gets the order ticket for the specified order id. Returns null if not found
        /// </summary>
        /// <param name="orderId">The order's id</param>
        /// <returns>The order ticket with the specified id, or null if not found</returns>
        public OrderTicket GetOrderTicket(int orderId)
        {
            OrderTicket ticket;
            _completeOrderTickets.TryGetValue(orderId, out ticket);
            return ticket;
        }

        #endregion

        /// <summary>
        /// Get the order by its id
        /// </summary>
        /// <param name="orderId">Order id to fetch</param>
        /// <returns>A clone of the order with the specified id, or null if no match is found</returns>
        public Order GetOrderById(int orderId)
        {
            Order order = GetOrderByIdInternal(orderId);
            return order?.Clone();
        }

        private Order GetOrderByIdInternal(int orderId)
        {
            Order order;
            return _completeOrders.TryGetValue(orderId, out order) ? order : null;
        }

        /// <summary>
        /// Gets the order by its brokerage id
        /// </summary>
        /// <param name="brokerageId">The brokerage id to fetch</param>
        /// <returns>The first order matching the brokerage id, or null if no match is found</returns>
        public Order GetOrderByBrokerageId(string brokerageId)
        {
            var order = _openOrders.FirstOrDefault(x => x.Value.BrokerId.Contains(brokerageId)).Value
                        ?? _completeOrders.FirstOrDefault(x => x.Value.BrokerId.Contains(brokerageId)).Value;
            return order?.Clone();
        }

        /// <summary>
        /// Gets all orders matching the specified filter. Specifying null will return an enumerable
        /// of all orders.
        /// </summary>
        /// <param name="filter">Delegate used to filter the orders</param>
        /// <returns>All orders this order provider currently holds by the specified filter</returns>
        public IEnumerable<Order> GetOrders(Func<Order, bool> filter = null)
        {
            if (filter != null)
            {
                // return a clone to prevent object reference shenanigans, you must submit a request to change the order
                return _completeOrders.Select(x => x.Value).Where(filter).Select(x => x.Clone());
            }
            return _completeOrders.Select(x => x.Value).Select(x => x.Clone());
        }

        /// <summary>
        /// Gets open orders matching the specified filter
        /// </summary>
        /// <param name="filter">Delegate used to filter the orders</param>
        /// <returns>All open orders this order provider currently holds</returns>
        public List<Order> GetOpenOrders(Func<Order, bool> filter = null)
        {
            if (filter != null)
            {
                // return a clone to prevent object reference shenanigans, you must submit a request to change the order
                return _openOrders.Select(x => x.Value).Where(filter).Select(x => x.Clone()).ToList();
            }
            return _openOrders.Select(x => x.Value).Select(x => x.Clone()).ToList();
        }

        /// <summary>
        /// Primary thread entry point to launch the transaction thread.
        /// </summary>
        protected void Run()
        {
            try
            {
                foreach (var request in _orderRequestQueue.GetConsumingEnumerable(_cancellationTokenSource.Token))
                {
                    HandleOrderRequest(request);
                    ProcessAsynchronousEvents();
                }
            }
            catch (Exception err)
            {
                // unexpected error, we need to close down shop
                Log.Error(err);
                // quit the algorithm due to error
                _algorithm.RunTimeError = err;
            }

            if (_processingThread != null)
            {
                Log.Trace("BrokerageTransactionHandler.Run(): Ending Thread...");
                IsActive = false;
            }
        }

        /// <summary>
        /// Processes asynchronous events on the transaction handler's thread
        /// </summary>
        public virtual void ProcessAsynchronousEvents()
        {
            // NOP
        }

        /// <summary>
        /// Processes all synchronous events that must take place before the next time loop for the algorithm
        /// </summary>
        public virtual void ProcessSynchronousEvents()
        {
            // how to do synchronous market orders for real brokerages?

            // in backtesting we need to wait for orders to be removed from the queue and finished processing
            if (!_algorithm.LiveMode)
            {
                if (_orderRequestQueue.IsBusy && !_orderRequestQueue.WaitHandle.WaitOne(Time.OneSecond, _cancellationTokenSource.Token))
                {
                    Log.Error("BrokerageTransactionHandler.ProcessSynchronousEvents(): Timed out waiting for request queue to finish processing.");
                }
                return;
            }

            Log.Debug("BrokerageTransactionHandler.ProcessSynchronousEvents(): Enter");

            // check if the brokerage should perform cash sync now
            if (_brokerage.ShouldPerformCashSync(CurrentTimeUtc))
            {
                // only perform cash syncs if we haven't had a fill for at least 10 seconds
                if (TimeSinceLastFill > TimeSpan.FromSeconds(10))
                {
                    if (!_brokerage.PerformCashSync(_algorithm, CurrentTimeUtc, () => TimeSinceLastFill))
                    {
                        if (++_failedCashSyncAttempts >= MaxCashSyncAttempts)
                        {
                            throw new Exception("The maximum number of attempts for brokerage cash sync has been reached.");
                        }
                    }
                }
            }

            // we want to remove orders older than 10k records, but only in live mode
            const int maxOrdersToKeep = 10000;
            if (_completeOrders.Count < maxOrdersToKeep + 1)
            {
                Log.Debug("BrokerageTransactionHandler.ProcessSynchronousEvents(): Exit");
                return;
            }

            int max = _completeOrders.Max(x => x.Key);
            int lowestOrderIdToKeep = max - maxOrdersToKeep;
            foreach (var item in _completeOrders.Where(x => x.Key <= lowestOrderIdToKeep))
            {
                Order value;
                OrderTicket ticket;
                _completeOrders.TryRemove(item.Key, out value);
                _completeOrderTickets.TryRemove(item.Key, out ticket);
            }

            Log.Debug("BrokerageTransactionHandler.ProcessSynchronousEvents(): Exit");
        }

        /// <summary>
        /// Register an already open Order
        /// </summary>
        public void AddOpenOrder(Order order, OrderTicket orderTicket)
        {
            _openOrders.AddOrUpdate(order.Id, order, (i, o) => order);
            _completeOrders.AddOrUpdate(order.Id, order, (i, o) => order);
            _openOrderTickets.AddOrUpdate(order.Id, orderTicket);
            _completeOrderTickets.AddOrUpdate(order.Id, orderTicket);
        }


        /// <summary>
        /// Signal a end of thread request to stop monitoring the transactions.
        /// </summary>
        public void Exit()
        {
            if (_processingThread != null)
            {
                // only wait if the processing thread is running
                var timeout = TimeSpan.FromSeconds(60);
                if (_orderRequestQueue.IsBusy && !_orderRequestQueue.WaitHandle.WaitOne(timeout))
                {
                    Log.Error("BrokerageTransactionHandler.Exit(): Exceed timeout: " + (int)(timeout.TotalSeconds) + " seconds.");
                }
            }
            _cancellationTokenSource.Cancel();
            if (_processingThread != null && _processingThread.IsAlive)
            {
                _processingThread.Abort();
            }
            IsActive = false;
        }

        /// <summary>
        /// Handles a generic order request
        /// </summary>
        /// <param name="request"><see cref="OrderRequest"/> to be handled</param>
        /// <returns><see cref="OrderResponse"/> for request</returns>
        public void HandleOrderRequest(OrderRequest request)
        {
            OrderResponse response;
            switch (request.OrderRequestType)
            {
                case OrderRequestType.Submit:
                    response = HandleSubmitOrderRequest((SubmitOrderRequest)request);
                    break;
                case OrderRequestType.Update:
                    response = HandleUpdateOrderRequest((UpdateOrderRequest)request);
                    break;
                case OrderRequestType.Cancel:
                    response = HandleCancelOrderRequest((CancelOrderRequest)request);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // mark request as processed
            request.SetResponse(response, OrderRequestStatus.Processed);
        }

        /// <summary>
        /// Handles a request to submit a new order
        /// </summary>
        private OrderResponse HandleSubmitOrderRequest(SubmitOrderRequest request)
        {
            OrderTicket ticket;
            var order = Order.CreateOrder(request);

            // ensure the order is tagged with a currency
            var security = _algorithm.Securities[order.Symbol];
            order.PriceCurrency = security.SymbolProperties.QuoteCurrency;

            // rounds off the order towards 0 to the nearest multiple of lot size
            order.Quantity = RoundOffOrder(order, security);

            if (!_openOrders.TryAdd(order.Id, order) || !_completeOrders.TryAdd(order.Id, order))
            {
                Log.Error("BrokerageTransactionHandler.HandleSubmitOrderRequest(): Unable to add new order, order not processed.");
                return OrderResponse.Error(request, OrderResponseErrorCode.OrderAlreadyExists, "Cannot process submit request because order with id {0} already exists");
            }
            if (!_completeOrderTickets.TryGetValue(order.Id, out ticket))
            {
                Log.Error("BrokerageTransactionHandler.HandleSubmitOrderRequest(): Unable to retrieve order ticket, order not processed.");
                return OrderResponse.UnableToFindOrder(request);
            }

            // rounds the order prices
            RoundOrderPrices(order, security);

            // save current security prices
            order.OrderSubmissionData = new OrderSubmissionData(security.BidPrice, security.AskPrice, security.Close);

            // update the ticket's internal storage with this new order reference
            ticket.SetOrder(order);

            if (order.Quantity == 0)
            {
                order.Status = OrderStatus.Invalid;
                var response = OrderResponse.ZeroQuantity(request);
                _algorithm.Error(response.ErrorMessage);
                HandleOrderEvent(new OrderEvent(order,
                    _algorithm.UtcTime,
                    OrderFee.Zero,
                    "Unable to add order for zero quantity"));
                return response;
            }

            // check to see if we have enough money to place the order
            HasSufficientBuyingPowerForOrderResult hasSufficientBuyingPowerResult;
            try
            {
                hasSufficientBuyingPowerResult = security.BuyingPowerModel.HasSufficientBuyingPowerForOrder(
                    new HasSufficientBuyingPowerForOrderParameters(_algorithm.Portfolio, security, order));
            }
            catch (Exception err)
            {
                Log.Error(err);
                _algorithm.Error($"Order Error: id: {order.Id.ToStringInvariant()}, Error executing margin models: {err.Message}");
                HandleOrderEvent(new OrderEvent(order,
                    _algorithm.UtcTime,
                    OrderFee.Zero,
                    "Error executing margin models"));
                return OrderResponse.Error(request, OrderResponseErrorCode.ProcessingError, "Error in GetSufficientCapitalForOrder");
            }

            if (!hasSufficientBuyingPowerResult.IsSufficient)
            {
                order.Status = OrderStatus.Invalid;
                var errorMessage = $"Order Error: id: {order.Id}, Insufficient buying power to complete order (Value:{order.GetValue(security).SmartRounding()}), Reason: {hasSufficientBuyingPowerResult.Reason}";
                var response = OrderResponse.Error(request, OrderResponseErrorCode.InsufficientBuyingPower, errorMessage);
                _algorithm.Error(response.ErrorMessage);
                HandleOrderEvent(new OrderEvent(order,
                    _algorithm.UtcTime,
                    OrderFee.Zero,
                    errorMessage));
                return response;
            }

            // verify that our current brokerage can actually take the order
            BrokerageMessageEvent message;
            if (!_algorithm.BrokerageModel.CanSubmitOrder(security, order, out message))
            {
                // if we couldn't actually process the order, mark it as invalid and bail
                order.Status = OrderStatus.Invalid;
                if (message == null) message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "InvalidOrder", "BrokerageModel declared unable to submit order: " + order.Id);
                var response = OrderResponse.Error(request, OrderResponseErrorCode.BrokerageModelRefusedToSubmitOrder, "OrderID: " + order.Id + " " + message);
                _algorithm.Error(response.ErrorMessage);
                HandleOrderEvent(new OrderEvent(order,
                    _algorithm.UtcTime,
                    OrderFee.Zero,
                    "BrokerageModel declared unable to submit order"));
                return response;
            }

            // set the order status based on whether or not we successfully submitted the order to the market
            bool orderPlaced;
            try
            {
                orderPlaced = _brokerage.PlaceOrder(order);
            }
            catch (Exception err)
            {
                Log.Error(err);
                orderPlaced = false;
            }

            if (!orderPlaced)
            {
                // we failed to submit the order, invalidate it
                order.Status = OrderStatus.Invalid;
                var errorMessage = "Brokerage failed to place order: " + order.Id;
                var response = OrderResponse.Error(request, OrderResponseErrorCode.BrokerageFailedToSubmitOrder, errorMessage);
                _algorithm.Error(response.ErrorMessage);
                HandleOrderEvent(new OrderEvent(order,
                    _algorithm.UtcTime,
                    OrderFee.Zero,
                    "Brokerage failed to place order"));
                return response;
            }

            return OrderResponse.Success(request);
        }

        /// <summary>
        /// Handles a request to update order properties
        /// </summary>
        private OrderResponse HandleUpdateOrderRequest(UpdateOrderRequest request)
        {
            Order order;
            OrderTicket ticket;
            if (!_completeOrders.TryGetValue(request.OrderId, out order) || !_completeOrderTickets.TryGetValue(request.OrderId, out ticket))
            {
                Log.Error("BrokerageTransactionHandler.HandleUpdateOrderRequest(): Unable to update order with ID " + request.OrderId);
                return OrderResponse.UnableToFindOrder(request);
            }

            if (!CanUpdateOrder(order))
            {
                return OrderResponse.InvalidStatus(request, order);
            }

            // rounds off the order towards 0 to the nearest multiple of lot size
            var security = _algorithm.Securities[order.Symbol];
            order.Quantity = RoundOffOrder(order, security);

            // verify that our current brokerage can actually update the order
            BrokerageMessageEvent message;
            if (!_algorithm.LiveMode && !_algorithm.BrokerageModel.CanUpdateOrder(_algorithm.Securities[order.Symbol], order, request, out message))
            {
                if (message == null) message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "InvalidRequest", "BrokerageModel declared unable to update order: " + order.Id);
                var response = OrderResponse.Error(request, OrderResponseErrorCode.BrokerageModelRefusedToUpdateOrder, "OrderID: " + order.Id + " " + message);
                _algorithm.Error(response.ErrorMessage);
                HandleOrderEvent(new OrderEvent(order,
                    _algorithm.UtcTime,
                    OrderFee.Zero,
                    "BrokerageModel declared unable to update order"));
                return response;
            }

            // modify the values of the order object
            order.ApplyUpdateOrderRequest(request);

            // rounds the order prices
            RoundOrderPrices(order, security);

            ticket.SetOrder(order);

            bool orderUpdated;
            try
            {
                orderUpdated = _brokerage.UpdateOrder(order);
            }
            catch (Exception err)
            {
                Log.Error(err);
                orderUpdated = false;
            }

            if (!orderUpdated)
            {
                // we failed to update the order for some reason
                var errorMessage = "Brokerage failed to update order with id " + request.OrderId;
                _algorithm.Error(errorMessage);
                HandleOrderEvent(new OrderEvent(order,
                    _algorithm.UtcTime,
                    OrderFee.Zero,
                    "Brokerage failed to update order"));
                return OrderResponse.Error(request, OrderResponseErrorCode.BrokerageFailedToUpdateOrder, errorMessage);
            }

            return OrderResponse.Success(request);
        }

        /// <summary>
        /// Returns true if the specified order can be updated
        /// </summary>
        /// <param name="order">The order to check if we can update</param>
        /// <returns>True if the order can be updated, false otherwise</returns>
        private bool CanUpdateOrder(Order order)
        {
            return order.Status != OrderStatus.Filled
                && order.Status != OrderStatus.Canceled
                && order.Status != OrderStatus.PartiallyFilled
                && order.Status != OrderStatus.Invalid;
        }

        /// <summary>
        /// Handles a request to cancel an order
        /// </summary>
        private OrderResponse HandleCancelOrderRequest(CancelOrderRequest request)
        {
            Order order;
            OrderTicket ticket;
            if (!_completeOrders.TryGetValue(request.OrderId, out order) || !_completeOrderTickets.TryGetValue(request.OrderId, out ticket))
            {
                Log.Error("BrokerageTransactionHandler.HandleCancelOrderRequest(): Unable to cancel order with ID " + request.OrderId + ".");
                _cancelPendingOrders.RemoveAndFallback(order);
                return OrderResponse.UnableToFindOrder(request);
            }

            if (order.Status.IsClosed())
            {
                _cancelPendingOrders.RemoveAndFallback(order);
                return OrderResponse.InvalidStatus(request, order);
            }

            ticket.SetOrder(order);

            bool orderCanceled;
            try
            {
                orderCanceled = _brokerage.CancelOrder(order);
            }
            catch (Exception err)
            {
                Log.Error(err);
                orderCanceled = false;
            }

            if (!orderCanceled)
            {
                // failed to cancel the order
                var message = "Brokerage failed to cancel order with id " + order.Id;
                _algorithm.Error(message);
                _cancelPendingOrders.RemoveAndFallback(order);
                return OrderResponse.Error(request, OrderResponseErrorCode.BrokerageFailedToCancelOrder, message);
            }

            if (request.Tag != null)
            {
                // update the tag, useful for 'why' we canceled the order
                order.Tag = request.Tag;
            }

            return OrderResponse.Success(request);
        }

        private void HandleOrderEvent(OrderEvent orderEvent)
        {
            lock (_lockHandleOrderEvent)
            {
                Order order;
                OrderTicket ticket;
                if (orderEvent.Status.IsClosed() && _openOrders.TryRemove(orderEvent.OrderId, out order))
                {
                    _completeOrders[orderEvent.OrderId] = order;
                }
                else if (!_completeOrders.TryGetValue(orderEvent.OrderId, out order))
                {
                    Log.Error("BrokerageTransactionHandler.HandleOrderEvent(): Unable to locate open Order with id " + orderEvent.OrderId);
                    return;
                }

                if (orderEvent.Status.IsClosed() && _openOrderTickets.TryRemove(orderEvent.OrderId, out ticket))
                {
                    _completeOrderTickets[orderEvent.OrderId] = ticket;
                }
                else if (!_completeOrderTickets.TryGetValue(orderEvent.OrderId, out ticket))
                {
                    Log.Error("BrokerageTransactionHandler.HandleOrderEvent(): Unable to resolve open ticket: " + orderEvent.OrderId);
                    return;
                }
                _cancelPendingOrders.UpdateOrRemove(order.Id, orderEvent.Status);
                // set the status of our order object based on the fill event
                order.Status = orderEvent.Status;

                orderEvent.Id = order.GetNewId();

                // set the modified time of the order to the fill's timestamp
                switch (orderEvent.Status)
                {
                    case OrderStatus.Canceled:
                        order.CanceledTime = orderEvent.UtcTime;
                        break;

                    case OrderStatus.PartiallyFilled:
                    case OrderStatus.Filled:
                        order.LastFillTime = orderEvent.UtcTime;

                        // append fill message to order tag, for additional information
                        if (orderEvent.Status == OrderStatus.Filled && !string.IsNullOrWhiteSpace(orderEvent.Message))
                        {
                            if (string.IsNullOrWhiteSpace(order.Tag))
                            {
                                order.Tag = orderEvent.Message;
                            }
                            else
                            {
                                order.Tag += " - " + orderEvent.Message;
                            }
                        }
                        break;

                    case OrderStatus.UpdateSubmitted:
                    case OrderStatus.Submitted:
                        // submit events after the initial submission are all order updates
                        if (ticket.UpdateRequests.Count > 0)
                        {
                            order.LastUpdateTime = orderEvent.UtcTime;
                        }
                        break;
                }

                // lets always set current Quantity, Limit and Stop prices in the order event so that it's easier for consumers
                // to know the current state and detect any update
                orderEvent.Quantity = order.Quantity;
                switch (order.Type)
                {
                    case OrderType.Limit:
                        var limit = order as LimitOrder;
                        orderEvent.LimitPrice = limit.LimitPrice;
                        break;
                    case OrderType.StopMarket:
                        var marketOrder = order as StopMarketOrder;
                        orderEvent.StopPrice = marketOrder.StopPrice;
                        break;
                    case OrderType.StopLimit:
                        var stopLimitOrder = order as StopLimitOrder;
                        orderEvent.LimitPrice = stopLimitOrder.LimitPrice;
                        orderEvent.StopPrice = stopLimitOrder.StopPrice;
                        break;
                }

                //Apply the filled order to our portfolio:
                if (orderEvent.Status == OrderStatus.Filled || orderEvent.Status == OrderStatus.PartiallyFilled)
                {
                    Interlocked.Exchange(ref _lastFillTimeTicks, CurrentTimeUtc.Ticks);

                    // check if the fill currency and the order currency match the symbol currency
                    var security = _algorithm.Securities[orderEvent.Symbol];
                    // Bug in FXCM API flipping the currencies -- disabling for now. 5/17/16 RFB
                    //if (fill.FillPriceCurrency != security.SymbolProperties.QuoteCurrency)
                    //{
                    //    Log.Error(string.Format("Currency mismatch: Fill currency: {0}, Symbol currency: {1}", fill.FillPriceCurrency, security.SymbolProperties.QuoteCurrency));
                    //}
                    //if (order.PriceCurrency != security.SymbolProperties.QuoteCurrency)
                    //{
                    //    Log.Error(string.Format("Currency mismatch: Order currency: {0}, Symbol currency: {1}", order.PriceCurrency, security.SymbolProperties.QuoteCurrency));
                    //}

                    var multiplier = security.SymbolProperties.ContractMultiplier;
                    var securityConversionRate = security.QuoteCurrency.ConversionRate;
                    var feeInAccountCurrency = _algorithm.Portfolio.CashBook
                        .ConvertToAccountCurrency(orderEvent.OrderFee.Value).Amount;

                    try
                    {
                        _algorithm.Portfolio.ProcessFill(orderEvent);
                        _algorithm.TradeBuilder.ProcessFill(
                            orderEvent,
                            securityConversionRate,
                            feeInAccountCurrency,
                            multiplier);
                    }
                    catch (Exception err)
                    {
                        Log.Error(err);
                        _algorithm.Error($"Order Error: id: {order.Id.ToStringInvariant()}, Error in Portfolio.ProcessFill: {err.Message}");
                    }
                }

                // update the ticket after we've processed the fill, but before the event, this way everything is ready for user code
                ticket.AddOrderEvent(orderEvent);
            }
            
            //We have an event! :) Order filled, send it in to be handled by algorithm portfolio.
            if (orderEvent.Status != OrderStatus.None) //order.Status != OrderStatus.Submitted
            {
                _orderEvents.Enqueue(orderEvent);

                //Create new order event:
                _resultHandler.OrderEvent(orderEvent);

                NewOrderEvent?.Invoke(this, orderEvent);

                try
                {
                    //Trigger our order event handler
                    _algorithm.OnOrderEvent(orderEvent);
                }
                catch (Exception err)
                {
                    _algorithm.Error("Order Event Handler Error: " + err.Message);
                    // kill the algorithm
                    _algorithm.RunTimeError = err;
                }
            }
        }

        /// <summary>
        /// Brokerages can send account updates, this include cash balance updates. Since it is of
        /// utmost important to always have an accurate picture of reality, we'll trust this information
        /// as truth
        /// </summary>
        private void HandleAccountChanged(AccountEvent account)
        {
            // how close are we?
            var delta = _algorithm.Portfolio.CashBook[account.CurrencySymbol].Amount - account.CashBalance;
            if (delta != 0)
            {
                Log.Trace($"BrokerageTransactionHandler.HandleAccountChanged(): {account.CurrencySymbol} Cash Delta: {delta}");
            }

            // maybe we don't actually want to do this, this data can be delayed. Must be explicitly supported by brokerage
            if (_brokerage.AccountInstantlyUpdated)
            {
                // override the current cash value so we're always guaranteed to be in sync with the brokerage's push updates
                _algorithm.Portfolio.CashBook[account.CurrencySymbol].SetAmount(account.CashBalance);
            }
        }

        /// <summary>
        /// Option assignment/exercise event is received and propagated to the user algo
        /// </summary>
        private void HandlePositionAssigned(OrderEvent fill)
        {
            // informing user algorithm that option position has been assigned
            if (fill.IsAssignment)
            {
                _algorithm.OnAssignmentOrderEvent(fill);
            }
        }

        /// <summary>
        /// Gets the amount of time since the last call to algorithm.Portfolio.ProcessFill(fill)
        /// </summary>
        protected virtual TimeSpan TimeSinceLastFill =>
            CurrentTimeUtc - new DateTime(Interlocked.Read(ref _lastFillTimeTicks));

        /// <summary>
        /// Gets current time UTC. This is here to facilitate testing
        /// </summary>
        protected virtual DateTime CurrentTimeUtc => DateTime.UtcNow;

        /// <summary>
        /// Rounds off the order towards 0 to the nearest multiple of Lot Size
        /// </summary>
        public decimal RoundOffOrder(Order order, Security security)
        {
            var orderLotMod = order.Quantity % security.SymbolProperties.LotSize;

            if (orderLotMod != 0)
            {
                order.Quantity = order.Quantity - orderLotMod;

                if (!_firstRoundOffMessage)
                {
                    _algorithm.Error("Warning: Due to brokerage limitations, orders will be rounded to " +
                        $"the nearest lot size of {security.SymbolProperties.LotSize.ToStringInvariant()}"
                    );
                    _firstRoundOffMessage = true;
                }
                return order.Quantity;
            }
            else
            {
                return order.Quantity;
            }
        }

        /// <summary>
        /// Rounds the order prices to its security minimum price variation.
        /// <remarks>
        /// This procedure is needed to meet brokerage precision requirements.
        /// </remarks>
        /// </summary>
        protected void RoundOrderPrices(Order order, Security security)
        {
            // Do not need to round market orders
            if (order.Type == OrderType.Market ||
                order.Type == OrderType.MarketOnOpen ||
                order.Type == OrderType.MarketOnClose)
            {
                return;
            }

            switch (order.Type)
            {
                case OrderType.Limit:
                    {
                        var limitPrice = ((LimitOrder) order).LimitPrice;
                        var increment = security.PriceVariationModel.GetMinimumPriceVariation(
                            new GetMinimumPriceVariationParameters(security, limitPrice));
                        if (increment > 0)
                        {
                            var limitRound = Math.Round(limitPrice / increment) * increment;
                            ((LimitOrder) order).LimitPrice = limitRound;
                            SendWarningOnPriceChange("Limit", limitRound, limitPrice);
                        }
                    }
                    break;

                case OrderType.StopMarket:
                    {
                        var stopPrice = ((StopMarketOrder) order).StopPrice;
                        var increment = security.PriceVariationModel.GetMinimumPriceVariation(
                            new GetMinimumPriceVariationParameters(security, stopPrice));
                        if (increment > 0)
                        {
                            var stopRound = Math.Round(stopPrice / increment) * increment;
                            ((StopMarketOrder) order).StopPrice = stopRound;
                            SendWarningOnPriceChange("Stop", stopRound, stopPrice);
                        }
                    }
                    break;

                case OrderType.StopLimit:
                    {
                        var limitPrice = ((StopLimitOrder) order).LimitPrice;
                        var increment = security.PriceVariationModel.GetMinimumPriceVariation(
                            new GetMinimumPriceVariationParameters(security, limitPrice));
                        if (increment > 0)
                        {
                            var limitRound = Math.Round(limitPrice / increment) * increment;
                            ((StopLimitOrder) order).LimitPrice = limitRound;
                            SendWarningOnPriceChange("Limit", limitRound, limitPrice);
                        }

                        var stopPrice = ((StopLimitOrder) order).StopPrice;
                        increment = security.PriceVariationModel.GetMinimumPriceVariation(
                            new GetMinimumPriceVariationParameters(security, stopPrice));
                        if (increment > 0)
                        {
                            var stopRound = Math.Round(stopPrice / increment) * increment;
                            ((StopLimitOrder) order).StopPrice = stopRound;
                            SendWarningOnPriceChange("Stop", stopRound, stopPrice);
                        }
                    }
                    break;
            }
        }

        private void SendWarningOnPriceChange(string priceType, decimal priceRound, decimal priceOriginal)
        {
            if (!priceOriginal.Equals(priceRound))
            {
                _algorithm.Error(
                    $"Warning: To meet brokerage precision requirements, order {priceType.ToStringInvariant()}Price was rounded to {priceRound.ToStringInvariant()} from {priceOriginal.ToStringInvariant()}"
                );
            }
        }
    }
}

