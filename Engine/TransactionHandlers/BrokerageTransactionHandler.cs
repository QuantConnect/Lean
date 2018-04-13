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
using System.Threading;
using System.Threading.Tasks;
using QuantConnect.Brokerages;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Logging;
using QuantConnect.Orders;
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
        private bool _syncedLiveBrokerageCashToday;

        // this bool is used to check if the warning message for the rounding of order quantity has been displayed for the first time
        private bool _firstRoundOffMessage = false;

        // this value is used for determining how confident we are in our cash balance update
        private long _lastFillTimeTicks;
        private long _lastSyncTimeTicks;
        private readonly object _performCashSyncReentranceGuard = new object();

        // 7:45 AM (New York time zone)
        private static readonly TimeSpan LiveBrokerageCashSyncTime = new TimeSpan(7, 45, 0);

        /// <summary>
        /// OrderQueue holds the newly updated orders from the user algorithm waiting to be processed. Once
        /// orders are processed they are moved into the Orders queue awaiting the brokerage response.
        /// </summary>
        private readonly BusyBlockingCollection<OrderRequest> _orderRequestQueue = new BusyBlockingCollection<OrderRequest>();
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        /// <summary>
        /// The orders dictionary holds orders which are sent to exchange, partially filled, completely filled or cancelled.
        /// Once the transaction thread has worked on them they get put here while witing for fill updates.
        /// </summary>
        private readonly ConcurrentDictionary<int, Order> _orders = new ConcurrentDictionary<int, Order>();

        /// <summary>
        /// The orders tickets dictionary holds order tickets that the algorithm can use to reference a specific order. This
        /// includes invoking update and cancel commands. In the future, we can add more features to the ticket, such as events
        /// and async events (such as run this code when this order fills)
        /// </summary>
        private readonly ConcurrentDictionary<int, OrderTicket> _orderTickets = new ConcurrentDictionary<int, OrderTicket>();

        private IResultHandler _resultHandler;

        /// <summary>
        /// Gets the permanent storage for all orders
        /// </summary>
        public ConcurrentDictionary<int, Order> Orders
        {
            get { return _orders; }
        }

        /// <summary>
        /// Gets the permanent storage for all order tickets
        /// </summary>
        public ConcurrentDictionary<int, OrderTicket> OrderTickets
        {
            get { return _orderTickets; }
        }

        /// <summary>
        /// Gets the current number of orders that have been processed
        /// </summary>
        public int OrdersCount
        {
            get { return _orders.Count; }
        }

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

            // we don't need to do this today because we just initialized/synced
            _resultHandler = resultHandler;
            _syncedLiveBrokerageCashToday = true;
            _lastSyncTimeTicks = DateTime.UtcNow.Ticks;

            _brokerage = brokerage;
            _brokerage.OrderStatusChanged += (sender, fill) =>
            {
                // log every fill in live mode
                if (algorithm.LiveMode)
                {
                    var brokerIds = string.Empty;
                    var order = GetOrderById(fill.OrderId);
                    if (order != null && order.BrokerId.Count > 0) brokerIds = string.Join(", ", order.BrokerId);
                    Log.Trace("BrokerageTransactionHandler.OrderStatusChanged(): " + fill + " BrokerId: " + brokerIds);
                }

                HandleOrderEvent(fill);
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
            }

            switch (request.OrderRequestType)
            {
                case OrderRequestType.Submit:
                    return AddOrder((SubmitOrderRequest) request);

                case OrderRequestType.Update:
                    return UpdateOrder((UpdateOrderRequest) request);

                case OrderRequestType.Cancel:
                    return CancelOrder((CancelOrderRequest) request);

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
            _orderTickets.TryAdd(ticket.OrderId, ticket);

            // send the order to be processed after creating the ticket
            if (response.IsSuccess)
            {
                _orderRequestQueue.Add(request);
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
                _orders.TryAdd(request.OrderId, order);
            }
            return ticket;
        }

        /// <summary>
        /// Update an order yet to be filled such as stop or limit orders.
        /// </summary>
        /// <param name="request">Request detailing how the order should be updated</param>
        /// <remarks>Does not apply if the order is already fully filled</remarks>
        public OrderTicket UpdateOrder(UpdateOrderRequest request)
        {
            OrderTicket ticket;
            if (!_orderTickets.TryGetValue(request.OrderId, out ticket))
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
            if (!_orderTickets.TryGetValue(request.OrderId, out ticket))
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
                    // update the order status
                    order.Status = OrderStatus.CancelPending;

                    // notify the algorithm with an order event
                    HandleOrderEvent(new OrderEvent(order, _algorithm.UtcTime, 0m));

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
            return _orderTickets.Select(x => x.Value).Where(filter ?? (x => true));
        }

        /// <summary>
        /// Gets the order ticket for the specified order id. Returns null if not found
        /// </summary>
        /// <param name="orderId">The order's id</param>
        /// <returns>The order ticket with the specified id, or null if not found</returns>
        public OrderTicket GetOrderTicket(int orderId)
        {
            OrderTicket ticket;
            _orderTickets.TryGetValue(orderId, out ticket);
            return ticket;
        }

        #endregion

        /// <summary>
        /// Get the order by its id
        /// </summary>
        /// <param name="orderId">Order id to fetch</param>
        /// <returns>The order with the specified id, or null if no match is found</returns>
        public Order GetOrderById(int orderId)
        {
            Order order = GetOrderByIdInternal(orderId);
            return order != null ? order.Clone() : null;
        }

        private Order GetOrderByIdInternal(int orderId)
        {
            // this function can be invoked by brokerages when getting open orders, guard against null ref
            if (_orders == null) return null;

            Order order;
            return _orders.TryGetValue(orderId, out order) ? order : null;
        }

        /// <summary>
        /// Gets the order by its brokerage id
        /// </summary>
        /// <param name="brokerageId">The brokerage id to fetch</param>
        /// <returns>The first order matching the brokerage id, or null if no match is found</returns>
        public Order GetOrderByBrokerageId(string brokerageId)
        {
            // this function can be invoked by brokerages when getting open orders, guard against null ref
            if (_orders == null) return null;

            var order = _orders.FirstOrDefault(x => x.Value.BrokerId.Contains(brokerageId)).Value;
            return order != null ? order.Clone() : null;
        }

        /// <summary>
        /// Gets all orders matching the specified filter
        /// </summary>
        /// <param name="filter">Delegate used to filter the orders</param>
        /// <returns>All open orders this order provider currently holds</returns>
        public IEnumerable<Order> GetOrders(Func<Order, bool> filter = null)
        {
            if (_orders == null)
            {
                // this is the case when we haven't initialize yet, backtesting brokerage
                // will end up calling this through the transaction manager
                return Enumerable.Empty<Order>();
            }

            if (filter != null)
            {
                // return a clone to prevent object reference shenanigans, you must submit a request to change the order
                return _orders.Select(x => x.Value).Where(filter).Select(x => x.Clone());
            }
            return _orders.Select(x => x.Value).Select(x => x.Clone());
        }

        /// <summary>
        /// Primary thread entry point to launch the transaction thread.
        /// </summary>
        public void Run()
        {
            try
            {
                foreach(var request in _orderRequestQueue.GetConsumingEnumerable(_cancellationTokenSource.Token))
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

            Log.Trace("BrokerageTransactionHandler.Run(): Ending Thread...");
            IsActive = false;
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

            // every morning flip this switch back
            var currentTimeNewYork = DateTime.UtcNow.ConvertFromUtc(TimeZones.NewYork);
            if (_syncedLiveBrokerageCashToday && currentTimeNewYork.Date != LastSyncDate)
            {
                _syncedLiveBrokerageCashToday = false;
            }

            // we want to sync up our cash balance before market open
            if (_algorithm.LiveMode && !_syncedLiveBrokerageCashToday && currentTimeNewYork.TimeOfDay >= LiveBrokerageCashSyncTime)
            {
                try
                {
                    // only perform cash syncs if we haven't had a fill for at least 10 seconds
                    if (TimeSinceLastFill > TimeSpan.FromSeconds(10))
                    {
                        PerformCashSync();
                    }
                }
                catch (Exception err)
                {
                    Log.Error(err, "Updating cash balances");
                }
            }

            // we want to remove orders older than 10k records, but only in live mode
            const int maxOrdersToKeep = 10000;
            if (_orders.Count < maxOrdersToKeep + 1)
            {
                Log.Debug("BrokerageTransactionHandler.ProcessSynchronousEvents(): Exit");
                return;
            }

            int max = _orders.Max(x => x.Key);
            int lowestOrderIdToKeep = max - maxOrdersToKeep;
            foreach (var item in _orders.Where(x => x.Key <= lowestOrderIdToKeep))
            {
                Order value;
                OrderTicket ticket;
                _orders.TryRemove(item.Key, out value);
                _orderTickets.TryRemove(item.Key, out ticket);
            }

            Log.Debug("BrokerageTransactionHandler.ProcessSynchronousEvents(): Exit");
        }

        /// <summary>
        /// Syncs cash from brokerage with portfolio object
        /// </summary>
        private void PerformCashSync()
        {
            try
            {
                // prevent reentrance in this method
                if (!Monitor.TryEnter(_performCashSyncReentranceGuard))
                {
                    return;
                }

                Log.Trace("BrokerageTransactionHandler.PerformCashSync(): Sync cash balance");

                var balances = new List<Cash>();
                try
                {
                    balances = _brokerage.GetCashBalance();
                }
                catch (Exception err)
                {
                    Log.Error(err);
                }

                if (balances.Count == 0)
                {
                    return;
                }

                //Adds currency to the cashbook that the user might have deposited
                foreach (var balance in balances)
                {
                    Cash cash;
                    if (!_algorithm.Portfolio.CashBook.TryGetValue(balance.Symbol, out cash))
                    {
                        Log.LogHandler.Trace("BrokerageTransactionHandler.PerformCashSync(): Unexpected cash found {0} {1}", balance.Amount, balance.Symbol);
                        _algorithm.Portfolio.SetCash(balance.Symbol, balance.Amount, balance.ConversionRate);
                    }
                }

                // if we were returned our balances, update everything and flip our flag as having performed sync today
                foreach (var kvp in _algorithm.Portfolio.CashBook)
                {
                    var cash = kvp.Value;

                    var balanceCash = balances.FirstOrDefault(balance => balance.Symbol == cash.Symbol);
                    //update the cash if the entry if found in the balances
                    if (balanceCash != null)
                    {
                        // compare in dollars
                        var delta = cash.Amount - balanceCash.Amount;
                        if (Math.Abs(_algorithm.Portfolio.CashBook.ConvertToAccountCurrency(delta, cash.Symbol)) > 5)
                        {
                            // log the delta between
                            Log.LogHandler.Trace("BrokerageTransactionHandler.PerformCashSync(): {0} Delta: {1}", balanceCash.Symbol,
                                delta.ToString("0.00"));
                        }
                        _algorithm.Portfolio.SetCash(balanceCash.Symbol, balanceCash.Amount, balanceCash.ConversionRate);
                    }
                    else
                    {
                        //Set the cash amount to zero if cash entry not found in the balances
                        _algorithm.Portfolio.SetCash(cash.Symbol, 0, cash.ConversionRate);
                    }
                }
                _syncedLiveBrokerageCashToday = true;
            }
            finally
            {
                Monitor.Exit(_performCashSyncReentranceGuard);
            }

            // fire off this task to check if we've had recent fills, if we have then we'll invalidate the cash sync
            // and do it again until we're confident in it
            Task.Delay(TimeSpan.FromSeconds(10)).ContinueWith(_ =>
            {
                // we want to make sure this is a good value, so check for any recent fills
                if (TimeSinceLastFill <= TimeSpan.FromSeconds(20))
                {
                    // this will cause us to come back in and reset cash again until we
                    // haven't processed a fill for +- 10 seconds of the set cash time
                    _syncedLiveBrokerageCashToday = false;
                    Log.Trace("BrokerageTransactionHandler.PerformCashSync(): Unverified cash sync - resync required.");
                }
                else
                {
                    _lastSyncTimeTicks = DateTime.UtcNow.Ticks;
                    Log.Trace("BrokerageTransactionHandler.PerformCashSync(): Verified cash sync.");
                }
            });
        }

        /// <summary>
        /// Signal a end of thread request to stop montioring the transactions.
        /// </summary>
        public void Exit()
        {
            var timeout = TimeSpan.FromSeconds(60);
            if (!_orderRequestQueue.WaitHandle.WaitOne(timeout))
            {
                Log.Error("BrokerageTransactionHandler.Exit(): Exceed timeout: " + (int)(timeout.TotalSeconds) + " seconds.");
            }
            _cancellationTokenSource.Cancel();
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

            if (!_orders.TryAdd(order.Id, order))
            {
                Log.Error("BrokerageTransactionHandler.HandleSubmitOrderRequest(): Unable to add new order, order not processed.");
                return OrderResponse.Error(request, OrderResponseErrorCode.OrderAlreadyExists, "Cannot process submit request because order with id {0} already exists");
            }
            if (!_orderTickets.TryGetValue(order.Id, out ticket))
            {
                Log.Error("BrokerageTransactionHandler.HandleSubmitOrderRequest(): Unable to retrieve order ticket, order not processed.");
                return OrderResponse.UnableToFindOrder(request);
            }

            // rounds the order prices
            RoundOrderPrices(order, security);

            // save current security time and prices
            order.OrderSubmissionData = new OrderSubmissionData(security.GetLastData());

            // update the ticket's internal storage with this new order reference
            ticket.SetOrder(order);

            if (order.Quantity == 0)
            {
                order.Status = OrderStatus.Invalid;
                var response = OrderResponse.ZeroQuantity(request);
                _algorithm.Error(response.ErrorMessage);
                HandleOrderEvent(new OrderEvent(order, _algorithm.UtcTime, 0m, "Unable to add order for zero quantity"));
                return response;
            }

            // check to see if we have enough money to place the order
            HasSufficientBuyingPowerForOrderResult hasSufficientBuyingPowerResult;
            try
            {
                hasSufficientBuyingPowerResult = security.BuyingPowerModel.HasSufficientBuyingPowerForOrder(_algorithm.Portfolio, security, order);
            }
            catch (Exception err)
            {
                Log.Error(err);
                _algorithm.Error(string.Format("Order Error: id: {0}, Error executing margin models: {1}", order.Id, err.Message));
                HandleOrderEvent(new OrderEvent(order, _algorithm.UtcTime, 0m, "Error executing margin models"));
                return OrderResponse.Error(request, OrderResponseErrorCode.ProcessingError, "Error in GetSufficientCapitalForOrder");
            }

            if (!hasSufficientBuyingPowerResult.IsSufficient)
            {
                order.Status = OrderStatus.Invalid;
                var errorMessage = $"Order Error: id: {order.Id}, Insufficient buying power to complete order (Value:{order.GetValue(security).SmartRounding()}), Reason: {hasSufficientBuyingPowerResult.Reason}";
                var response = OrderResponse.Error(request, OrderResponseErrorCode.InsufficientBuyingPower, errorMessage);
                _algorithm.Error(response.ErrorMessage);
                HandleOrderEvent(new OrderEvent(order, _algorithm.UtcTime, 0m, errorMessage));
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
                HandleOrderEvent(new OrderEvent(order, _algorithm.UtcTime, 0m, "BrokerageModel declared unable to submit order"));
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
                HandleOrderEvent(new OrderEvent(order, _algorithm.UtcTime, 0m, "Brokerage failed to place order"));
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
            if (!_orders.TryGetValue(request.OrderId, out order) || !_orderTickets.TryGetValue(request.OrderId, out ticket))
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
                // if we couldn't actually process the order, mark it as invalid and bail
                order.Status = OrderStatus.Invalid;
                if (message == null) message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "InvalidOrder", "BrokerageModel declared unable to update order: " + order.Id);
                var response = OrderResponse.Error(request, OrderResponseErrorCode.BrokerageModelRefusedToUpdateOrder, "OrderID: " + order.Id + " " + message);
                _algorithm.Error(response.ErrorMessage);
                HandleOrderEvent(new OrderEvent(order, _algorithm.UtcTime, 0m, "BrokerageModel declared unable to update order"));
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
                HandleOrderEvent(new OrderEvent(order, _algorithm.UtcTime, 0m, "Brokerage failed to update order"));
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
            if (!_orders.TryGetValue(request.OrderId, out order) || !_orderTickets.TryGetValue(request.OrderId, out ticket))
            {
                Log.Error("BrokerageTransactionHandler.HandleCancelOrderRequest(): Unable to cancel order with ID " + request.OrderId + ".");
                return OrderResponse.UnableToFindOrder(request);
            }

            if (order.Status.IsClosed())
            {
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
                HandleOrderEvent(new OrderEvent(order, _algorithm.UtcTime, 0m, "Brokerage failed to cancel order"));
                return OrderResponse.Error(request, OrderResponseErrorCode.BrokerageFailedToCancelOrder, message);
            }

            // we succeeded to cancel the order
            order.Status = OrderStatus.Canceled;

            if (request.Tag != null)
            {
                // update the tag, useful for 'why' we canceled the order
                order.Tag = request.Tag;
            }

            return OrderResponse.Success(request);
        }

        private void HandleOrderEvent(OrderEvent fill)
        {
            // update the order status
            var order = GetOrderByIdInternal(fill.OrderId);
            if (order == null)
            {
                Log.Error("BrokerageTransactionHandler.HandleOrderEvent(): Unable to locate Order with id " + fill.OrderId);
                return;
            }

            // set the status of our order object based on the fill event
            order.Status = fill.Status;

            // save that the order event took place, we're initializing the list with a capacity of 2 to reduce number of mallocs
            //these hog memory
            //List<OrderEvent> orderEvents = _orderEvents.GetOrAdd(orderEvent.OrderId, i => new List<OrderEvent>(2));
            //orderEvents.Add(orderEvent);

            //Apply the filled order to our portfolio:
            if (fill.Status == OrderStatus.Filled || fill.Status == OrderStatus.PartiallyFilled)
            {
                Interlocked.Exchange(ref _lastFillTimeTicks, DateTime.UtcNow.Ticks);

                // check if the fill currency and the order currency match the symbol currency
                var security = _algorithm.Securities[fill.Symbol];
                // Bug in FXCM API flipping the currencies -- disabling for now. 5/17/16 RFB
                //if (fill.FillPriceCurrency != security.SymbolProperties.QuoteCurrency)
                //{
                //    Log.Error(string.Format("Currency mismatch: Fill currency: {0}, Symbol currency: {1}", fill.FillPriceCurrency, security.SymbolProperties.QuoteCurrency));
                //}
                //if (order.PriceCurrency != security.SymbolProperties.QuoteCurrency)
                //{
                //    Log.Error(string.Format("Currency mismatch: Order currency: {0}, Symbol currency: {1}", order.PriceCurrency, security.SymbolProperties.QuoteCurrency));
                //}

                try
                {
                    _algorithm.Portfolio.ProcessFill(fill);

                    var conversionRate = security.QuoteCurrency.ConversionRate;
                    var multiplier = security.SymbolProperties.ContractMultiplier;

                    _algorithm.TradeBuilder.ProcessFill(fill, conversionRate, multiplier);
                }
                catch (Exception err)
                {
                    Log.Error(err);
                    _algorithm.Error(string.Format("Order Error: id: {0}, Error in Portfolio.ProcessFill: {1}", order.Id, err.Message));
                }
            }

            // update the ticket and order after we've processed the fill, but before the event, this way everything is ready for user code
            OrderTicket ticket;
            if (_orderTickets.TryGetValue(fill.OrderId, out ticket))
            {
                ticket.AddOrderEvent(fill);
                order.Price = ticket.AverageFillPrice;
            }
            else
            {
                Log.Error("BrokerageTransactionHandler.HandleOrderEvent(): Unable to resolve ticket: " + fill.OrderId);
            }

            //We have an event! :) Order filled, send it in to be handled by algorithm portfolio.
            if (fill.Status != OrderStatus.None) //order.Status != OrderStatus.Submitted
            {
                //Create new order event:
                _resultHandler.OrderEvent(fill);
                try
                {
                    //Trigger our order event handler
                    _algorithm.OnOrderEvent(fill);
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
                Log.Trace(string.Format("BrokerageTransactionHandler.HandleAccountChanged(): {0} Cash Delta: {1}", account.CurrencySymbol, delta));
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
        private TimeSpan TimeSinceLastFill
        {
            get { return DateTime.UtcNow - new DateTime(Interlocked.Read(ref _lastFillTimeTicks)); }
        }

        /// <summary>
        /// Gets the date of the last sync (New York time zone)
        /// </summary>
        private DateTime LastSyncDate
        {
            get { return new DateTime(Interlocked.Read(ref _lastSyncTimeTicks)).ConvertFromUtc(TimeZones.NewYork).Date; }
        }

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
                    _algorithm.Error(
                        string.Format(
                            "Warning: Due to brokerage limitations, orders will be rounded to the nearest lot size of {0}",
                            security.SymbolProperties.LotSize));
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
        private void RoundOrderPrices(Order order, Security security)
        {
            // Do not need to round market orders
            if (order.Type == OrderType.Market ||
                order.Type == OrderType.MarketOnOpen ||
                order.Type == OrderType.MarketOnClose)
            {
                return;
            }

            var increment = security.PriceVariationModel.GetMinimumPriceVariation(security);
            if (increment == 0) return;

            var limitPrice = 0m;
            var limitRound = 0m;
            var stopPrice = 0m;
            var stopRound = 0m;

            switch (order.Type)
            {
                case OrderType.Limit:
                    limitPrice = ((LimitOrder)order).LimitPrice;
                    limitRound = Math.Round(limitPrice / increment) * increment;
                    ((LimitOrder)order).LimitPrice = limitRound;
                    break;
                case OrderType.StopMarket:
                    stopPrice = ((StopMarketOrder)order).StopPrice;
                    stopRound = Math.Round(stopPrice / increment) * increment;
                    ((StopMarketOrder)order).StopPrice = stopRound;
                    break;
                case OrderType.StopLimit:
                    limitPrice = ((StopLimitOrder)order).LimitPrice;
                    limitRound = Math.Round(limitPrice / increment) * increment;
                    ((StopLimitOrder)order).LimitPrice = limitRound;
                    stopPrice = ((StopLimitOrder)order).StopPrice;
                    stopRound = Math.Round(stopPrice / increment) * increment;
                    ((StopLimitOrder)order).StopPrice = stopRound;
                    break;
                default:
                    break;
            }

            var format = "Warning: To meet brokerage precision requirements, order {0}Price was rounded to {1} from {2}";

            if (!limitPrice.Equals(limitRound))
            {
                _algorithm.Error(string.Format(format, "Limit", limitRound, limitPrice));
            }
            if (!stopPrice.Equals(stopRound))
            {
                _algorithm.Error(string.Format(format, "Stop", stopRound, stopPrice));
            }
        }
    }
}
