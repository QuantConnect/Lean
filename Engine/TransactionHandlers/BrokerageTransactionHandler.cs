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
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using QuantConnect.Algorithm;
using QuantConnect.Algorithm.Framework.Portfolio.SignalExports;
using QuantConnect.AlgorithmFactory.Python.Wrappers;
using QuantConnect.Brokerages;
using QuantConnect.Brokerages.Backtesting;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;
using QuantConnect.Securities.Option;
using QuantConnect.Util;

namespace QuantConnect.Lean.Engine.TransactionHandlers
{
    /// <summary>
    /// Transaction handler for all brokerages
    /// </summary>
    public class BrokerageTransactionHandler : ITransactionHandler
    {
        private IAlgorithm _algorithm;
        private SignalExportManager _signalExport;
        private IBrokerage _brokerage;
        private bool _brokerageIsBacktesting;
        private bool _loggedFeeAdjustmentWarning;

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
        protected IBusyCollection<OrderRequest> _orderRequestQueue { get; set; }

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
        /// Cache collection of price adjustment modes for each symbol
        /// </summary>
        private readonly Dictionary<Symbol, DataNormalizationMode> _priceAdjustmentModes = new Dictionary<Symbol, DataNormalizationMode>();

        /// <summary>
        /// The _cancelPendingOrders instance will help to keep track of CancelPending orders and their Status
        /// </summary>
        protected CancelPendingOrders _cancelPendingOrders { get; init; } = new CancelPendingOrders();

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
                throw new ArgumentNullException(nameof(brokerage));
            }
            // multi threaded queue, used for live deployments
            _orderRequestQueue = new BusyBlockingCollection<OrderRequest>();
            // we don't need to do this today because we just initialized/synced
            _resultHandler = resultHandler;

            _brokerage = brokerage;
            _brokerageIsBacktesting = brokerage is BacktestingBrokerage;

            _brokerage.OrdersStatusChanged += (sender, orderEvents) =>
            {
                HandleOrderEvents(orderEvents);
            };

            _brokerage.AccountChanged += (sender, account) =>
            {
                HandleAccountChanged(account);
            };

            _brokerage.OptionPositionAssigned += (sender, fill) =>
            {
                HandlePositionAssigned(fill);
            };

            _brokerage.OptionNotification += (sender, e) =>
            {
                HandleOptionNotification(e);
            };

            _brokerage.NewBrokerageOrderNotification += (sender, e) =>
            {
                HandleNewBrokerageSideOrder(e);
            };

            _brokerage.DelistingNotification += (sender, e) =>
            {
                HandleDelistingNotification(e);
            };

            _brokerage.OrderIdChanged += (sender, e) =>
            {
                HandlerBrokerageOrderIdChangedEvent(e);
            };

            _brokerage.OrderUpdated += (sender, e) =>
            {
                HandleOrderUpdated(e);
            };

            IsActive = true;

            _algorithm = algorithm;

            _signalExport = _algorithm is QCAlgorithm
                ? (_algorithm as QCAlgorithm).SignalExport
                : (_algorithm as AlgorithmPythonWrapper).SignalExport;

            NewOrderEvent += (s, e) => _signalExport.OnOrderEvent(e);
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

            var shortable = true;
            if (request.Quantity < 0)
            {
                shortable = _algorithm.Shortable(request.Symbol, request.Quantity);
            }

            if (!shortable)
            {
                var message = GetShortableErrorMessage(request.Symbol, request.Quantity);
                if (_algorithm.LiveMode)
                {
                    // in live mode we send a warning but we wont block the order being sent to the brokerage
                    _algorithm.Debug($"Warning: {message}");
                }
                else
                {
                    response = OrderResponse.Error(request, OrderResponseErrorCode.ExceedsShortableQuantity, message);
                }
            }

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
                var orderTag = response.ErrorCode == OrderResponseErrorCode.AlgorithmWarmingUp
                    ? "Algorithm warming up."
                    : response.ErrorMessage;

                // ensure the order is tagged with a currency
                var security = _algorithm.Securities[order.Symbol];
                order.PriceCurrency = security.SymbolProperties.QuoteCurrency;

                order.Status = OrderStatus.Invalid;
                order.Tag = orderTag;
                ticket.SetOrder(order);
                _completeOrderTickets.TryAdd(ticket.OrderId, ticket);
                _completeOrders.TryAdd(order.Id, order);

                HandleOrderEvent(new OrderEvent(order,
                    _algorithm.UtcTime,
                    OrderFee.Zero,
                    orderTag));
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
                var orderQuantity = request.Quantity ?? ticket.Quantity;

                var shortable = true;
                if (order?.Direction == OrderDirection.Sell || orderQuantity < 0)
                {
                    shortable = _algorithm.Shortable(ticket.Symbol, orderQuantity, order.Id);

                    if (_algorithm.LiveMode && !shortable)
                    {
                        // let's override and just send warning
                        shortable = true;

                        _algorithm.Debug($"Warning: {GetShortableErrorMessage(ticket.Symbol, ticket.Quantity)}");
                    }
                }

                if (order == null)
                {
                    // can't update an order that doesn't exist!
                    Log.Error("BrokerageTransactionHandler.Update(): Cannot update a null order");
                    request.SetResponse(OrderResponse.UnableToFindOrder(request));
                }
                else if (order.Status == OrderStatus.New)
                {
                    // can't update a pending submit order
                    Log.Error("BrokerageTransactionHandler.Update(): Cannot update a pending submit order with status " + order.Status);
                    request.SetResponse(OrderResponse.InvalidNewStatus(request, order));
                }
                else if (order.Status.IsClosed() && !request.IsAllowedForClosedOrder())
                {
                    // can't update a completed order
                    Log.Error("BrokerageTransactionHandler.Update(): Cannot update closed order with status " + order.Status);
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
                else if (!shortable)
                {
                    var shortableResponse = OrderResponse.Error(request, OrderResponseErrorCode.ExceedsShortableQuantity,
                        GetShortableErrorMessage(ticket.Symbol, ticket.Quantity));

                    request.SetResponse(shortableResponse);
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
                else if (order.Status == OrderStatus.New)
                {
                    Log.Error("BrokerageTransactionHandler.CancelOrder(): Cannot cancel order with status: " + order.Status);
                    request.SetResponse(OrderResponse.InvalidNewStatus(request, order));
                }
                else if (order.Status.IsClosed())
                {
                    Log.Error("BrokerageTransactionHandler.CancelOrder(): Cannot cancel order already " + order.Status);
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
        public List<Order> GetOrdersByBrokerageId(string brokerageId)
        {
            var openOrders = GetOrdersByBrokerageId(brokerageId, _openOrders);

            if (openOrders.Count > 0
                // if it's part of a group, some leg could be filled already, not part of open orders
                && (openOrders[0].GroupOrderManager == null || openOrders[0].GroupOrderManager.Count == openOrders.Count))
            {
                return openOrders;
            }

            return GetOrdersByBrokerageId(brokerageId, _completeOrders);
        }

        private static List<Order> GetOrdersByBrokerageId(string brokerageId, ConcurrentDictionary<int, Order> orders)
        {
            return orders
                .Where(x => x.Value.BrokerId.Contains(brokerageId))
                .Select(kvp => kvp.Value.Clone())
                .ToList();
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
                _algorithm.SetRuntimeError(err, "HandleOrderRequest");
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

            _signalExport.Flush(CurrentTimeUtc);

            // check if the brokerage should perform cash sync now
            if (!_algorithm.IsWarmingUp && _brokerage.ShouldPerformCashSync(CurrentTimeUtc))
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
                return;
            }

            Log.Debug("BrokerageTransactionHandler.ProcessSynchronousEvents(): Start removing old orders...");
            var max = _completeOrders.Max(x => x.Key);
            var lowestOrderIdToKeep = max - maxOrdersToKeep;
            foreach (var item in _completeOrders.Where(x => x.Key <= lowestOrderIdToKeep))
            {
                Order value;
                OrderTicket ticket;
                _completeOrders.TryRemove(item.Key, out value);
                _completeOrderTickets.TryRemove(item.Key, out ticket);
            }

            Log.Debug($"BrokerageTransactionHandler.ProcessSynchronousEvents(): New order count {_completeOrders.Count}. Exit");
        }

        /// <summary>
        /// Register an already open Order
        /// </summary>
        public void AddOpenOrder(Order order, IAlgorithm algorithm)
        {
            if (order.Status == OrderStatus.New || order.Status == OrderStatus.None)
            {
                // make sure we have a valid order status
                order.Status = OrderStatus.Submitted;
            }

            order.Id = algorithm.Transactions.GetIncrementOrderId();

            if (order.GroupOrderManager != null && order.GroupOrderManager.Id == 0)
            {
                order.GroupOrderManager.Id = algorithm.Transactions.GetIncrementGroupOrderManagerId();
            }

            var orderTicket = order.ToOrderTicket(algorithm.Transactions);

            SetPriceAdjustmentMode(order, algorithm);

            _openOrders.AddOrUpdate(order.Id, order, (i, o) => order);
            _completeOrders.AddOrUpdate(order.Id, order, (i, o) => order);
            _openOrderTickets.AddOrUpdate(order.Id, orderTicket);
            _completeOrderTickets.AddOrUpdate(order.Id, orderTicket);

            Interlocked.Increment(ref _totalOrderCount);
        }


        /// <summary>
        /// Signal a end of thread request to stop monitoring the transactions.
        /// </summary>
        public void Exit()
        {
            var timeout = TimeSpan.FromSeconds(60);
            if (_processingThread != null)
            {
                // only wait if the processing thread is running
                if (_orderRequestQueue.IsBusy && !_orderRequestQueue.WaitHandle.WaitOne(timeout))
                {
                    Log.Error("BrokerageTransactionHandler.Exit(): Exceed timeout: " + (int)(timeout.TotalSeconds) + " seconds.");
                }
            }

            _processingThread?.StopSafely(timeout, _cancellationTokenSource);
            IsActive = false;
            _cancellationTokenSource.DisposeSafely();
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
            if (string.IsNullOrEmpty(order.Tag))
            {
                order.Tag = order.GetDefaultTag();
            }

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

            var comboIsReady = order.TryGetGroupOrders(TryGetOrder, out var orders);
            var comboSecuritiesFound = orders.TryGetGroupOrdersSecurities(_algorithm.Portfolio, out var securities);

            // rounds the order prices
            RoundOrderPrices(order, security, comboIsReady, securities);

            // save current security prices
            order.OrderSubmissionData = new OrderSubmissionData(security.BidPrice, security.AskPrice, security.Close);

            // Set order price adjustment mode
            SetPriceAdjustmentMode(order, _algorithm);

            // update the ticket's internal storage with this new order reference
            ticket.SetOrder(order);

            if (!comboIsReady)
            {
                // an Order of the group is missing
                return OrderResponse.Success(request);
            }

            if (orders.Any(o => o.Quantity == 0))
            {
                var response = OrderResponse.ZeroQuantity(request);
                _algorithm.Error(response.ErrorMessage);

                InvalidateOrders(orders, response.ErrorMessage);
                return response;
            }

            if (!comboSecuritiesFound)
            {
                var response = OrderResponse.MissingSecurity(request);
                _algorithm.Error(response.ErrorMessage);

                InvalidateOrders(orders, response.ErrorMessage);
                return response;
            }

            // check to see if we have enough money to place the order
            if (!HasSufficientBuyingPowerForOrders(order, request, out var validationResult, orders, securities))
            {
                return validationResult;
            }

            // verify that our current brokerage can actually take the order
            foreach (var kvp in securities)
            {
                if (!_algorithm.BrokerageModel.CanSubmitOrder(kvp.Value, kvp.Key, out var message))
                {
                    var errorMessage = $"BrokerageModel declared unable to submit order: [{string.Join(",", orders.Select(o => o.Id))}]";

                    // if we couldn't actually process the order, mark it as invalid and bail
                    message ??= new BrokerageMessageEvent(BrokerageMessageType.Warning, "InvalidOrder", string.Empty);
                    var response = OrderResponse.Error(request, OrderResponseErrorCode.BrokerageModelRefusedToSubmitOrder, $"{errorMessage} {message}");

                    InvalidateOrders(orders, response.ErrorMessage);
                    _algorithm.Error(response.ErrorMessage);
                    return response;
                }
            }

            // set the order status based on whether or not we successfully submitted the order to the market
            bool orderPlaced;
            try
            {
                orderPlaced = orders.All(o => _brokerage.PlaceOrder(o));
            }
            catch (Exception err)
            {
                Log.Error(err);
                orderPlaced = false;
            }

            if (!orderPlaced)
            {
                // we failed to submit the order, invalidate it
                var errorMessage = $"Brokerage failed to place orders: [{string.Join(",", orders.Select(o => o.Id))}]";

                InvalidateOrders(orders, errorMessage);
                _algorithm.Error(errorMessage);
                return OrderResponse.Error(request, OrderResponseErrorCode.BrokerageFailedToSubmitOrder, errorMessage);
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

            if (order.Status == OrderStatus.New)
            {
                return OrderResponse.InvalidNewStatus(request, order);
            }

            var isClosedOrderUpdate = false;

            if (order.Status.IsClosed())
            {
                if (!request.IsAllowedForClosedOrder())
                {
                    return OrderResponse.InvalidStatus(request, order);
                }

                isClosedOrderUpdate = true;
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

            // If the order is not part of a ComboLegLimit update, validate sufficient buying power
            if (order.GroupOrderManager == null)
            {
                var updatedOrder = order.Clone();
                updatedOrder.ApplyUpdateOrderRequest(request);
                if (!HasSufficientBuyingPowerForOrders(updatedOrder, request, out var validationResult))
                {
                    return validationResult;
                }
            }

            // modify the values of the order object
            order.ApplyUpdateOrderRequest(request);

            // rounds the order prices
            RoundOrderPrices(order, security);

            ticket.SetOrder(order);

            bool orderUpdated;
            if (isClosedOrderUpdate)
            {
                orderUpdated = true;
            }
            else
            {
                try
                {
                    orderUpdated = _brokerage.UpdateOrder(order);
                }
                catch (Exception err)
                {
                    Log.Error(err);
                    orderUpdated = false;
                }
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

            if (order.Status == OrderStatus.New)
            {
                _cancelPendingOrders.RemoveAndFallback(order);
                return OrderResponse.InvalidNewStatus(request, order);
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

        /// <summary>
        /// Validates if there is sufficient buying power for the given order(s).
        /// Returns an error response if validation fails or an exception occurs.
        /// Returns null if validation passes.
        /// </summary>
        private bool HasSufficientBuyingPowerForOrders(Order order, OrderRequest request, out OrderResponse response, List<Order> orders = null, Dictionary<Order, Security> securities = null)
        {
            response = null;
            HasSufficientBuyingPowerForOrderResult hasSufficientBuyingPowerResult;
            try
            {
                hasSufficientBuyingPowerResult = _algorithm.Portfolio.HasSufficientBuyingPowerForOrder(orders ?? [order]);
            }
            catch (Exception err)
            {
                Log.Error(err);
                _algorithm.Error($"Order Error: id: {order.Id.ToStringInvariant()}, Error executing margin models: {err.Message}");
                HandleOrderEvent(new OrderEvent(order, _algorithm.UtcTime, OrderFee.Zero, "Error executing margin models"));

                response = OrderResponse.Error(request, OrderResponseErrorCode.ProcessingError, "An error occurred while checking sufficient buying power for the orders.");
                return false;
            }

            if (!hasSufficientBuyingPowerResult.IsSufficient)
            {
                var errorMessage = securities != null
                    ? securities.GetErrorMessage(hasSufficientBuyingPowerResult)
                    : $"Brokerage failed to update order with id: {order.Id.ToStringInvariant()}, Symbol: {order.Symbol.Value}, Insufficient buying power to complete order, Reason: {hasSufficientBuyingPowerResult.Reason}.";

                _algorithm.Error(errorMessage);

                if (request is UpdateOrderRequest)
                {
                    HandleOrderEvent(new OrderEvent(order, _algorithm.UtcTime, OrderFee.Zero, errorMessage));
                    response = OrderResponse.Error(request, OrderResponseErrorCode.BrokerageFailedToUpdateOrder, errorMessage);
                }
                else
                {
                    InvalidateOrders(orders, errorMessage);
                    response = OrderResponse.Error(request, OrderResponseErrorCode.InsufficientBuyingPower, errorMessage);
                }
                return false;
            }

            return true;
        }

        private void HandleOrderEvents(List<OrderEvent> orderEvents)
        {
            lock (_lockHandleOrderEvent)
            {
                // Get orders and tickets
                var orders = new List<Order>(orderEvents.Count);

                for (var i = 0; i < orderEvents.Count; i++)
                {
                    var orderEvent = orderEvents[i];

                    if (orderEvent.Status.IsClosed() && _openOrders.TryRemove(orderEvent.OrderId, out var order))
                    {
                        _completeOrders[orderEvent.OrderId] = order;
                    }
                    else if (!_completeOrders.TryGetValue(orderEvent.OrderId, out order))
                    {
                        Log.Error("BrokerageTransactionHandler.HandleOrderEvents(): Unable to locate open Combo Order with id " + orderEvent.OrderId);
                        LogOrderEvent(orderEvent);
                        return;
                    }
                    orders.Add(order);

                    if (orderEvent.Status.IsClosed() && _openOrderTickets.TryRemove(orderEvent.OrderId, out var ticket))
                    {
                        _completeOrderTickets[orderEvent.OrderId] = ticket;
                    }
                    else if (!_completeOrderTickets.TryGetValue(orderEvent.OrderId, out ticket))
                    {
                        Log.Error("BrokerageTransactionHandler.HandleOrderEvents(): Unable to resolve open ticket: " + orderEvent.OrderId);
                        LogOrderEvent(orderEvent);
                        return;
                    }
                    orderEvent.Ticket = ticket;
                }

                var fillsToProcess = new List<OrderEvent>(orderEvents.Count);

                // now lets update the orders
                for (var i = 0; i < orderEvents.Count; i++)
                {
                    var orderEvent = orderEvents[i];
                    var order = orders[i];
                    var ticket = orderEvent.Ticket;

                    _cancelPendingOrders.UpdateOrRemove(order.Id, orderEvent.Status);
                    // set the status of our order object based on the fill event except if the order status is filled/cancelled and the event is invalid
                    // in live trading it can happen that we submit an update which get's rejected by the brokerage because the order is already filled
                    // so we don't want the invalid update event to set the order status to invalid if it's already filled
                    if (order.Status != OrderStatus.Filled && order.Status != OrderStatus.Canceled || orderEvent.Status != OrderStatus.Invalid)
                    {
                        order.Status = orderEvent.Status;
                    }

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
                        case OrderType.ComboLegLimit:
                            var legLimitOrder = order as ComboLegLimitOrder;
                            orderEvent.LimitPrice = legLimitOrder.LimitPrice;
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
                        case OrderType.TrailingStop:
                            var trailingStopOrder = order as TrailingStopOrder;
                            orderEvent.StopPrice = trailingStopOrder.StopPrice;
                            orderEvent.TrailingAmount = trailingStopOrder.TrailingAmount;
                            break;
                        case OrderType.LimitIfTouched:
                            var limitIfTouchedOrder = order as LimitIfTouchedOrder;
                            orderEvent.LimitPrice = limitIfTouchedOrder.LimitPrice;
                            orderEvent.TriggerPrice = limitIfTouchedOrder.TriggerPrice;
                            break;
                    }

                    // check if the fill currency and the order currency match the symbol currency
                    if (orderEvent.Status == OrderStatus.Filled || orderEvent.Status == OrderStatus.PartiallyFilled)
                    {
                        fillsToProcess.Add(orderEvent);
                        Interlocked.Exchange(ref _lastFillTimeTicks, CurrentTimeUtc.Ticks);

                        var security = _algorithm.Securities[orderEvent.Symbol];

                        if (orderEvent.Symbol.SecurityType == SecurityType.Crypto
                            && order.Direction == OrderDirection.Buy
                            && CurrencyPairUtil.TryDecomposeCurrencyPair(orderEvent.Symbol, out var baseCurrency, out var quoteCurrency)
                            && orderEvent.OrderFee.Value.Currency == baseCurrency)
                        {
                            // fees are in the base currency, so we have to subtract them from the filled quantity
                            // else the virtual position will bigger than the real size and we might no be able to liquidate
                            orderEvent.FillQuantity -= orderEvent.OrderFee.Value.Amount;
                            orderEvent.OrderFee = new ModifiedFillQuantityOrderFee(orderEvent.OrderFee.Value, quoteCurrency, security.SymbolProperties.ContractMultiplier);

                            if (!_loggedFeeAdjustmentWarning)
                            {
                                _loggedFeeAdjustmentWarning = true;
                                const string message = "When buying currency pairs, using Cash account types, fees in base currency" +
                                    " will be deducted from the filled quantity so virtual positions reflect actual holdings.";
                                Log.Trace($"BrokerageTransactionHandler.HandleOrderEvent(): {message}");
                                _algorithm.Debug(message);
                            }
                        }
                    }
                }

                //Apply the filled orders to our portfolio:
                try
                {
                    _algorithm.Portfolio.ProcessFills(fillsToProcess);
                }
                catch (Exception err)
                {
                    Log.Error(err);
                    _algorithm.Error($"Fill error: error in TradeBuilder.ProcessFill: {err.Message}");
                }

                // Apply the filled orders to the trade builder
                for (var i = 0; i < orderEvents.Count; i++)
                {
                    var orderEvent = orderEvents[i];

                    if (orderEvent.Status == OrderStatus.Filled || orderEvent.Status == OrderStatus.PartiallyFilled)
                    {
                        var security = _algorithm.Securities[orderEvent.Symbol];

                        var multiplier = security.SymbolProperties.ContractMultiplier;
                        var securityConversionRate = security.QuoteCurrency.ConversionRate;
                        var feeInAccountCurrency = _algorithm.Portfolio.CashBook
                            .ConvertToAccountCurrency(orderEvent.OrderFee.Value).Amount;

                        try
                        {
                            _algorithm.TradeBuilder.ProcessFill(
                                orderEvent,
                                securityConversionRate,
                                feeInAccountCurrency,
                                multiplier);
                        }
                        catch (Exception err)
                        {
                            Log.Error(err);
                        }
                    }

                    // update the ticket after we've processed the fill, but before the event, this way everything is ready for user code
                    orderEvent.Ticket.AddOrderEvent(orderEvent);
                }
            }

            //We have the events! :) Orders filled, send them in to be handled by algorithm portfolio.
            for (var i = 0; i < orderEvents.Count; i++)
            {
                var orderEvent = orderEvents[i];

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
                        // unexpected error, we need to close down shop
                        _algorithm.SetRuntimeError(err, "Order Event Handler");
                    }
                }

                LogOrderEvent(orderEvent);
            }
        }

        private void HandleOrderEvent(OrderEvent orderEvent)
        {
            HandleOrderEvents(new List<OrderEvent> { orderEvent });
        }

        private void HandleOrderUpdated(OrderUpdateEvent e)
        {
            if (!_completeOrders.TryGetValue(e.OrderId, out var order))
            {
                Log.Error("BrokerageTransactionHandler.HandleOrderUpdated(): Unable to locate open order with id " + e.OrderId);
                return;
            }

            switch (order.Type)
            {
                case OrderType.TrailingStop:
                    ((TrailingStopOrder)order).StopPrice = e.TrailingStopPrice;
                    break;

                case OrderType.StopLimit:
                    ((StopLimitOrder)order).StopTriggered = e.StopTriggered;
                    break;
            }
        }

        /// <summary>
        /// Gets the price adjustment mode for the specified symbol from its subscription configurations
        /// </summary>
        private void SetPriceAdjustmentMode(Order order, IAlgorithm algorithm)
        {
            if (algorithm.LiveMode)
            {
                // live trading always uses raw prices
                order.PriceAdjustmentMode = DataNormalizationMode.Raw;
                return;
            }

            if (!_priceAdjustmentModes.TryGetValue(order.Symbol, out var mode))
            {
                var configs = algorithm.SubscriptionManager.SubscriptionDataConfigService
                    .GetSubscriptionDataConfigs(order.Symbol, includeInternalConfigs: true);
                if (configs.Count == 0)
                {
                    throw new InvalidOperationException($"Unable to locate subscription data config for {order.Symbol}");
                }

                mode = configs[0].DataNormalizationMode;
                _priceAdjustmentModes[order.Symbol] = mode;
            }

            order.PriceAdjustmentMode = mode;
        }

        /// <summary>
        /// Debug logging helper method, called after HandleOrderEvent has finished updating status, price and quantity
        /// </summary>
        /// <param name="e">The order event</param>
        private static void LogOrderEvent(OrderEvent e)
        {
            if (Log.DebuggingEnabled)
            {
                Log.Debug("BrokerageTransactionHandler.LogOrderEvent(): " + e);
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
            var existingCashBalance = _algorithm.Portfolio.CashBook[account.CurrencySymbol].Amount;
            if (existingCashBalance != account.CashBalance)
            {
                Log.Trace($"BrokerageTransactionHandler.HandleAccountChanged(): {account.CurrencySymbol} Cash Lean: {existingCashBalance} Brokerage: {account.CashBalance}. Will update: {_brokerage.AccountInstantlyUpdated}");
            }

            // maybe we don't actually want to do this, this data can be delayed. Must be explicitly supported by brokerage
            if (_brokerage.AccountInstantlyUpdated)
            {
                // override the current cash value so we're always guaranteed to be in sync with the brokerage's push updates
                _algorithm.Portfolio.CashBook[account.CurrencySymbol].SetAmount(account.CashBalance);
            }
        }

        /// <summary>
        /// Brokerage order id change is applied to the target order
        /// </summary>
        private void HandlerBrokerageOrderIdChangedEvent(BrokerageOrderIdChangedEvent brokerageOrderIdChangedEvent)
        {
            var originalOrder = GetOrderByIdInternal(brokerageOrderIdChangedEvent.OrderId);

            if (originalOrder == null)
            {
                // shouldn't happen but let's be careful
                Log.Error($"BrokerageTransactionHandler.HandlerBrokerageOrderIdChangedEvent(): Lean order id {brokerageOrderIdChangedEvent.OrderId} not found");
                return;
            }

            // we replace the whole collection
            originalOrder.BrokerId = brokerageOrderIdChangedEvent.BrokerId;
        }

        /// <summary>
        /// Option assignment/exercise event is received and propagated to the user algo
        /// </summary>
        private void HandlePositionAssigned(OrderEvent fill)
        {
            // informing user algorithm that option position has been assigned
            _algorithm.OnAssignmentOrderEvent(fill);
        }

        private void HandleDelistingNotification(DelistingNotificationEventArgs e)
        {
            if (_algorithm.Securities.TryGetValue(e.Symbol, out var security))
            {
                // only log always in live trading, in backtesting log if not 0 holdings
                if (_algorithm.LiveMode || security.Holdings.Quantity != 0)
                {
                    Log.Trace(
                        $"BrokerageTransactionHandler.HandleDelistingNotification(): UtcTime: {CurrentTimeUtc} clearing position for delisted holding: " +
                        $"Symbol: {e.Symbol.Value}, " +
                        $"Quantity: {security.Holdings.Quantity}");
                }

                // Only submit an order if we have holdings
                var quantity = -security.Holdings.Quantity;
                if (quantity != 0)
                {
                    var tag = "Liquidate from delisting";

                    // Create our order and add it
                    var order = new MarketOrder(security.Symbol, quantity, _algorithm.UtcTime, tag);
                    AddOpenOrder(order, _algorithm);

                    // Create our fill with the latest price
                    var fill = new OrderEvent(order, _algorithm.UtcTime, OrderFee.Zero)
                    {
                        FillPrice = security.Price,
                        Status = OrderStatus.Filled,
                        FillQuantity = order.Quantity
                    };

                    // Process this order event
                    HandleOrderEvent(fill);
                }
            }
        }

        /// <summary>
        /// Option notification event is received and new order events are generated
        /// </summary>
        private void HandleOptionNotification(OptionNotificationEventArgs e)
        {
            if (_algorithm.Securities.TryGetValue(e.Symbol, out var security))
            {
                // let's take the order event lock, we will be looking at orders and security holdings
                // and we don't want them changing mid processing because of an order event coming in at the same time
                // for example: DateTime/decimal order attributes are not thread safe by nature!
                lock (_lockHandleOrderEvent)
                {
                    if (OptionSymbol.IsOptionContractExpired(e.Symbol, CurrentTimeUtc))
                    {
                        if (e.Position == 0)
                        {
                            // only log always in live trading, in backtesting log if not 0 holdings
                            if (_algorithm.LiveMode || security.Holdings.Quantity != 0)
                            {
                                Log.Trace(
                                    $"BrokerageTransactionHandler.HandleOptionNotification(): UtcTime: {CurrentTimeUtc} clearing position for expired option holding: " +
                                    $"Symbol: {e.Symbol.Value}, " +
                                    $"Holdings: {security.Holdings.Quantity}");
                            }

                            var quantity = -security.Holdings.Quantity;

                            // If the quantity is already 0 for Lean and the brokerage there is nothing else todo here
                            if (quantity != 0)
                            {
                                var exerciseOrder = GenerateOptionExerciseOrder(security, quantity, e.Tag);

                                EmitOptionNotificationEvents(security, exerciseOrder);
                            }
                        }
                        else
                        {
                            Log.Error("BrokerageTransactionHandler.HandleOptionNotification(): " +
                                $"unexpected position ({e.Position} instead of zero) " +
                                $"for expired option contract: {e.Symbol.Value}");
                        }
                    }
                    else
                    {
                        // if position is reduced, could be an early exercise or early assignment
                        if (Math.Abs(e.Position) < security.Holdings.AbsoluteQuantity)
                        {
                            Log.Trace("BrokerageTransactionHandler.HandleOptionNotification(): " +
                                $"Symbol {e.Symbol.Value} EventQuantity {e.Position} Holdings {security.Holdings.Quantity}");

                            // if we are long the option and there is an open order, assume it's an early exercise
                            if (security.Holdings.IsLong)
                            {
                                // we only care about open option exercise orders, if it's closed it means we already
                                // processed it and we wouldn't have a need to handle it here
                                if (GetOpenOrders(x =>
                                        x.Symbol == e.Symbol &&
                                        x.Type == OrderType.OptionExercise)
                                    .FirstOrDefault() is OptionExerciseOrder exerciseOrder)
                                {
                                    EmitOptionNotificationEvents(security, exerciseOrder);
                                }
                            }

                            // if we are short the option and there are no buy orders (open or recently closed), assume it's an early assignment
                            else if (security.Holdings.IsShort)
                            {
                                var nowUtc = CurrentTimeUtc;
                                // for some brokerages (like IB) there might be a race condition between getting an option
                                // notification event and lean processing an order event. So if we are here, there are these options:
                                // A) holdings -10 position 5
                                //  1) WE just BOUGHT 15 and Lean doesn't know yet
                                //  2) WE just SOLD 15 and this notification is old
                                // B) holdings -10 position -5
                                //  1) WE just BOUGHT 5 and Lean doesn't know yet
                                //  2) WE just SOLD 5 more and this notification is old
                                //          - Seen this in production already
                                //  3) Brokerage triggered an early assignment

                                // so we get ALL orders for this symbol that were placed or got an update in the last 'orderWindowSeconds'

                                const int orderWindowSeconds = 10;
                                // NOTE: We do this checks for actual live trading only to handle the race condition stated above
                                // for actual brokerages (excluding paper trading with PaperBrokerage).
                                // TODO: If we confirm this race condition applies for IB only, we could move this to the brokerage itself.
                                if (_brokerageIsBacktesting ||
                                    !GetOrders(x =>
                                        x.Symbol == e.Symbol
                                        && (x.Status.IsOpen() || x.Status.IsFill() &&
                                            (Math.Abs((x.Time - nowUtc).TotalSeconds) < orderWindowSeconds
                                                || (x.LastUpdateTime.HasValue && Math.Abs((x.LastUpdateTime.Value - nowUtc).TotalSeconds) < orderWindowSeconds)
                                                || (x.LastFillTime.HasValue && Math.Abs((x.LastFillTime.Value - nowUtc).TotalSeconds) < orderWindowSeconds)))).Any())
                                {
                                    var quantity = e.Position - security.Holdings.Quantity;

                                    var exerciseOrder = GenerateOptionExerciseOrder(security, quantity, e.Tag);

                                    EmitOptionNotificationEvents(security, exerciseOrder);
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// New brokerage-side order event handler
        /// </summary>
        private void HandleNewBrokerageSideOrder(NewBrokerageOrderNotificationEventArgs e)
        {
            void onError(IReadOnlyCollection<SecurityType> supportedSecurityTypes) =>
                _algorithm.Debug($"Warning: New brokerage-side order could not be processed due to " +
                    $"it's security not being supported. Supported security types are {string.Join(", ", supportedSecurityTypes)}");

            if (_algorithm.BrokerageMessageHandler.HandleOrder(e) &&
                _algorithm.GetOrAddUnrequestedSecurity(e.Order.Symbol, out _, onError))
            {
                AddOpenOrder(e.Order, _algorithm);
            }
        }

        private OptionExerciseOrder GenerateOptionExerciseOrder(Security security, decimal quantity, string tag)
        {
            // generate new exercise order and ticket for the option
            var order = new OptionExerciseOrder(security.Symbol, quantity, CurrentTimeUtc, tag);

            // save current security prices
            order.OrderSubmissionData = new OrderSubmissionData(security.BidPrice, security.AskPrice, security.Close);
            order.PriceCurrency = security.SymbolProperties.QuoteCurrency;

            AddOpenOrder(order, _algorithm);
            return order;
        }

        private void EmitOptionNotificationEvents(Security security, OptionExerciseOrder order)
        {
            // generate the order events reusing the option exercise model
            var option = (Option)security;
            var orderEvents = option.OptionExerciseModel.OptionExercise(option, order);

            foreach (var orderEvent in orderEvents)
            {
                HandleOrderEvent(orderEvent);

                if (orderEvent.IsAssignment)
                {
                    orderEvent.Message = order.Tag;
                    HandlePositionAssigned(orderEvent);
                }
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
            var comboIsReady = order.TryGetGroupOrders(TryGetOrder, out var orders);
            orders.TryGetGroupOrdersSecurities(_algorithm.Portfolio, out var securities);

            RoundOrderPrices(order, security, comboIsReady, securities);
        }

        /// <summary>
        /// Rounds the order prices to its security minimum price variation.
        /// <remarks>
        /// This procedure is needed to meet brokerage precision requirements.
        /// </remarks>
        /// </summary>
        protected void RoundOrderPrices(Order order, Security security, bool comboIsReady, Dictionary<Order, Security> orders)
        {
            switch (order.Type)
            {
                case OrderType.Limit:
                    {
                        var limitOrder = (LimitOrder)order;
                        RoundOrderPrice(security, limitOrder.LimitPrice, "LimitPrice", (roundedPrice) => limitOrder.LimitPrice = roundedPrice);
                    }
                    break;

                case OrderType.StopMarket:
                    {
                        var stopMarketOrder = (StopMarketOrder)order;
                        RoundOrderPrice(security, stopMarketOrder.StopPrice, "StopPrice", (roundedPrice) => stopMarketOrder.StopPrice = roundedPrice);
                    }
                    break;

                case OrderType.StopLimit:
                    {
                        var stopLimitOrder = (StopLimitOrder)order;
                        RoundOrderPrice(security, stopLimitOrder.LimitPrice, "LimitPrice", (roundedPrice) => stopLimitOrder.LimitPrice = roundedPrice);
                        RoundOrderPrice(security, stopLimitOrder.StopPrice, "StopPrice", (roundedPrice) => stopLimitOrder.StopPrice = roundedPrice);
                    }
                    break;

                case OrderType.TrailingStop:
                    {
                        var trailingStopOrder = (TrailingStopOrder)order;
                        RoundOrderPrice(security, trailingStopOrder.StopPrice, "StopPrice",
                            (roundedPrice) => trailingStopOrder.StopPrice = roundedPrice);

                        if (!trailingStopOrder.TrailingAsPercentage)
                        {
                            RoundOrderPrice(security, trailingStopOrder.TrailingAmount, "TrailingAmount",
                                (roundedAmount) => trailingStopOrder.TrailingAmount = roundedAmount);
                        }
                    }
                    break;

                case OrderType.LimitIfTouched:
                    {
                        var limitIfTouchedOrder = (LimitIfTouchedOrder)order;
                        RoundOrderPrice(security, limitIfTouchedOrder.LimitPrice, "LimitPrice",
                            (roundedPrice) => limitIfTouchedOrder.LimitPrice = roundedPrice);
                        RoundOrderPrice(security, limitIfTouchedOrder.TriggerPrice, "TriggerPrice",
                            (roundedPrice) => limitIfTouchedOrder.TriggerPrice = roundedPrice);
                    }
                    break;

                case OrderType.ComboLegLimit:
                    {
                        var comboLegOrder = (ComboLegLimitOrder)order;
                        RoundOrderPrice(security, comboLegOrder.LimitPrice, "LimitPrice",
                            (roundedPrice) => comboLegOrder.LimitPrice = roundedPrice);
                    }
                    break;

                case OrderType.ComboLimit:
                    {
                        if (comboIsReady)
                        {
                            // all orders in the combo have been received.
                            // we can now round the limit price of the group order,
                            // for which we need to find the smallest price variation from each leg security
                            var groupOrderManager = order.GroupOrderManager;
                            var increment = 0m;
                            foreach (var (legOrder, legSecurity) in orders)
                            {
                                var legIncrement = legSecurity.PriceVariationModel.GetMinimumPriceVariation(
                                    new GetMinimumPriceVariationParameters(legSecurity, legOrder.Price));
                                if (legIncrement > 0 && (increment == 0 || legIncrement < increment))
                                {
                                    increment = legIncrement;
                                }
                            }

                            RoundOrderPrice(groupOrderManager.LimitPrice, increment, "LimitPrice",
                                (roundedPrice) => groupOrderManager.LimitPrice = roundedPrice);
                        }

                    }
                    break;
            }
        }

        private void RoundOrderPrice(Security security, decimal price, string priceType, Action<decimal> setPrice)
        {
            var increment = security.PriceVariationModel.GetMinimumPriceVariation(new GetMinimumPriceVariationParameters(security, price));
            RoundOrderPrice(price, increment, priceType, setPrice);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RoundOrderPrice(decimal price, decimal increment, string priceType, Action<decimal> setPrice)
        {
            if (increment > 0)
            {
                var roundedPrice = Math.Round(price / increment) * increment;
                setPrice(roundedPrice);
                SendWarningOnPriceChange(priceType, roundedPrice, price);
            }
        }

        private Order TryGetOrder(int orderId)
        {
            _completeOrders.TryGetValue(orderId, out var order);
            return order;
        }

        private void InvalidateOrders(List<Order> orders, string message)
        {
            for (var i = 0; i < orders.Count; i++)
            {
                var orderInGroup = orders[i];
                if (!orderInGroup.Status.IsClosed())
                {
                    orderInGroup.Status = OrderStatus.Invalid;
                }
                HandleOrderEvents(new List<OrderEvent> { new OrderEvent(orderInGroup, _algorithm.UtcTime, OrderFee.Zero, message) });
            }
        }

        private void SendWarningOnPriceChange(string priceType, decimal priceRound, decimal priceOriginal)
        {
            if (!priceOriginal.Equals(priceRound))
            {
                _algorithm.Error(
                    $"Warning: To meet brokerage precision requirements, order {priceType.ToStringInvariant()} was rounded to {priceRound.ToStringInvariant()} from {priceOriginal.ToStringInvariant()}"
                );
            }
        }

        private string GetShortableErrorMessage(Symbol symbol, decimal quantity)
        {
            var shortableQuantity = _algorithm.ShortableQuantity(symbol);
            return $"Order exceeds shortable quantity {shortableQuantity} for Symbol {symbol} requested {quantity})";
        }
    }
}

