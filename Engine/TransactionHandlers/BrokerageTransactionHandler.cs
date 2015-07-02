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

namespace QuantConnect.Lean.Engine.TransactionHandlers
{
    /// <summary>
    /// Transaction handler for all brokerages
    /// </summary>
    public class BrokerageTransactionHandler : ITransactionHandler
    {
        private bool _exitTriggered;
        private IAlgorithm _algorithm;
        private IBrokerage _brokerage;
        private bool _syncedLiveBrokerageCashToday = false;

        // this value is used for determining how confident we are in our cash balance update
        private long _lastFillTimeTicks;
        private long _lastSyncTimeTicks;
        private readonly object _performCashSyncReentranceGuard = new object();
        private static readonly TimeSpan _liveBrokerageCashSyncTime = new TimeSpan(7, 45, 0); // 7:45 am

        // pulled directly from the algorithm

        /// <summary>
        /// The orders queue holds orders which are sent to exchange, partially filled, completely filled or cancelled.
        /// Once the transaction thread has worked on them they get put here while witing for fill updates.
        /// </summary>
        private ConcurrentDictionary<int, Order> _orders;

        private ConcurrentQueue<OrderEvent> _orderEventQueue;
        private ConcurrentQueue<SecurityEvent> _securityEventQueue;
        private ConcurrentQueue<AccountEvent> _accountEventQueue;
        private ConcurrentQueue<OrderRequest> _orderRequestQueue;
        /// <summary>
        /// OrderEvents is an orderid indexed collection of events attached to each order. Because an order might be filled in 
        /// multiple legs it is important to keep a record of each event.
        /// </summary>
        private ConcurrentDictionary<int, List<OrderEvent>> _orderEvents;

        private IResultHandler _resultHandler;

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
            _lastSyncTimeTicks = DateTime.Now.Ticks;

            _brokerage = brokerage;

            _brokerage.OrderStatusChanged += (sender, fill) =>
            {
                _orderEventQueue.Enqueue(fill);
            };

            _brokerage.SecurityHoldingUpdated += (sender, holding) =>
            {
                _securityEventQueue.Enqueue(holding);
            };

            _brokerage.AccountChanged += (sender, account) =>
            {
                _accountEventQueue.Enqueue(account);
            };

            IsActive = true;

            _algorithm = algorithm;

            // also save off the various order data structures locally
            _orders = new ConcurrentDictionary<int, Order>();
            _orderEvents = algorithm.Transactions.OrderEvents;
            _orderEventQueue = new ConcurrentQueue<OrderEvent>();
            _securityEventQueue = new ConcurrentQueue<SecurityEvent>();
            _orderRequestQueue = algorithm.Transactions.OrderRequestQueue;
            _accountEventQueue = new ConcurrentQueue<AccountEvent>();

        }

        /// <summary>
        /// Boolean flag indicating the Run thread method is busy. 
        /// False indicates it is completely finished processing and ready to be terminated.
        /// </summary>
        public bool IsActive { get; private set; }

        /// <summary>
        /// Boolean flag signalling the handler is ready and all orders have been processed.
        /// </summary>
        public bool Ready
        {
            get
            {
                return HasPendingItems == false
                    && !_algorithm.ProcessingEvents;
            }
        }

        public bool HasPendingItems
        {
            get
            {
                return !(_orderRequestQueue.IsEmpty
                     && _orderEventQueue.IsEmpty
                     && _securityEventQueue.IsEmpty
                     && _accountEventQueue.IsEmpty);
            }
        }
        /// <summary>
        /// Primary thread entry point to launch the transaction thread.
        /// </summary>
        public void Run()
        {
            while (!_exitTriggered)
            {
                // if it's empty just sleep this thread for a little bit

                bool working = false;

                if (HasPendingItems)
                {
                    _algorithm.ProcessingEvents = true;

                    working = ProcessAccountEvents();
                    working |= ProcessSecurityEvents();
                    working |= ProcessOrderEvents();
                    working |= ProcessOrderRequests();
                }

                if (working == false)
                {
                    _algorithm.ProcessingEvents = false;

                    Thread.Sleep(1);
                    continue;
                }

                ProcessAsynchronousEvents();
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
                // spin wait until the queue has finished processing
                while (!Ready)
                {
                    Thread.Sleep(1);
                }
                return;
            }

            // every morning flip this switch back
            if (_syncedLiveBrokerageCashToday && DateTime.Now.Date != LastSyncDate)
            {
                _syncedLiveBrokerageCashToday = false;
            }

            // we want to sync up our cash balance before market open
            if (_algorithm.LiveMode && !_syncedLiveBrokerageCashToday && DateTime.Now.TimeOfDay >= _liveBrokerageCashSyncTime)
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
            if (_orders.Count < maxOrdersToKeep + 1) return;

            int max = _orders.Max(x => x.Key);
            int lowestOrderIdToKeep = max - maxOrdersToKeep;
            foreach (var item in _orders.Where(x => x.Key <= lowestOrderIdToKeep))
            {
                Order value;
                _orders.TryRemove(item.Key, out value);
            }
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

                var balances = _brokerage.GetCashBalance();
                if (balances.Count > 0)
                {
                    // if we were returned our balances, update everything and flip our flag as having performed sync today
                    foreach (var balance in balances)
                    {
                        Cash cash;
                        if (_algorithm.Portfolio.CashBook.TryGetValue(balance.Symbol, out cash))
                        {
                            // compare in dollars
                            var delta = cash.Quantity - balance.Quantity;
                            if (Math.Abs(delta) > _algorithm.Portfolio.CashBook.ConvertToAccountCurrency(delta, cash.Symbol))
                            {
                                // log the delta between 
                                Log.LogHandler.Trace("BrokerageTransactionHandler.PerformCashSync(): {0} Delta: {1}", balance.Symbol,
                                    delta.ToString("0.00"));
                            }
                        }
                        _algorithm.Portfolio.SetCash(balance.Symbol, balance.Quantity, balance.ConversionRate);
                    }

                    _syncedLiveBrokerageCashToday = true;
                }
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
                    _lastSyncTimeTicks = DateTime.Now.Ticks;
                    Log.Trace("BrokerageTransactionHandler.PerformCashSync(): Verified cash sync.");
                }
            });
        }

        /// <summary>
        /// Signal a end of thread request to stop montioring the transactions.
        /// </summary>
        public void Exit()
        {
            _exitTriggered = true;
        }

        private Order GetOrderById(int id)
        {
            Order order = null;

            if (_orders.TryGetValue(id, out order) == false)
            {
                var clientOrder = _algorithm.Transactions.GetOrderById(id);

                if (clientOrder != null)
                {
                    order = clientOrder.Clone();
                    _orders[id] = order;
                }
            }

            return order;
        }

        /// <summary>
        /// Send order update to transaction manager.
        /// </summary>
        /// <param name="order"></param>
        private void SendOrder(Order order)
        {
            _algorithm.Transactions.Process(order.Clone());
        }

        /// <summary>
        /// Process Order Request Queue
        /// </summary>
        /// <returns></returns>
        private bool ProcessOrderRequests()
        {
            int remainingCount = _orderRequestQueue.Count;

            if (remainingCount == 0)
                return false;

            while (remainingCount-- > 0)
            {
                OrderRequest orderRequest;

                if (_orderRequestQueue.TryDequeue(out orderRequest) == false)
                    break;

                var response = new OrderResponse(orderRequest);
                response.Error(OrderResponseErrorCode.ProcessingError, "Processing did not complete");

                try
                {
                    HandleOrderRequest(orderRequest, response);
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                }
                finally
                {
                    _algorithm.Transactions.Process(response);
                }
            }

            return true;
        }

        /// <summary>
        /// Process an order request
        /// </summary>
        /// <param name="request">Order request</param>
        /// <param name="response">Order response</param>
        private void HandleOrderRequest(OrderRequest request, OrderResponse response)
        {

            if (request is SubmitOrderRequest)
            {
                HandleNewOrder((SubmitOrderRequest)request, response);
            }
            else if (request is UpdateOrderRequest)
            {
                HandleUpdatedOrder((UpdateOrderRequest)request, response);
            }
            else if (request is CancelOrderRequest)
            {
                HandleCancelledOrder((CancelOrderRequest)request, response);
            }
        }

        /// <summary>
        /// New order handler
        /// </summary>
        /// <param name="request">Submit order request</param>
        private void HandleNewOrder(SubmitOrderRequest request, OrderResponse response)
        {
            var order = GetOrderById(request.OrderId);

            if (order != null)
            {
                response.Error(OrderResponseErrorCode.OrderAlreadyExists, String.Format("Cannot process submit request because order with id [{0}] already exists", request.OrderId));
                _algorithm.Error(response.ErrorMessage);
                return;
            }

            order = Order.Create(request);
            order.Status = OrderStatus.New;

            _orders[order.Id] = order;

            // check to see if we have enough money to place the order
            if (!_algorithm.Transactions.GetSufficientCapitalForOrder(_algorithm.Portfolio, order))
            {
                order.Status = OrderStatus.Invalid;

                response.Error(OrderResponseErrorCode.InsufficientBuyingPower, string.Format("Order Error: id: {0}, Insufficient buying power to complete order (Value:{1}).", order.Id, order.Value));
                _algorithm.Error(response.ErrorMessage);
            }
            else
            {
                // verify that our current brokerage can actually take the order
                BrokerageMessageEvent message;
                if (!_algorithm.LiveMode && !_algorithm.BrokerageModel.CanSubmitOrder(_algorithm.Securities[order.Symbol], order, out message))
                {
                    // if we couldn't actually process the order, mark it as invalid and bail
                    order.Status = OrderStatus.Invalid;
                    if (message == null) message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "InvalidOrder", "BrokerageModel declared unable to submit order: " + order.Id);

                    response.Error(OrderResponseErrorCode.BrokerageModelRefusedToSubmitOrder, "OrderID:" + message);
                    _algorithm.Error(response.ErrorMessage);
                }
                else if (_brokerage.PlaceOrder(order))
                {
                    response.Processed();

                    order.Status = OrderStatus.Submitted;
                }
                else
                {
                    order.Status = OrderStatus.Invalid;

                    response.Error(OrderResponseErrorCode.BrokerageFailedToSubmitOrder, "Brokerage failed to place order: " + order.Id);
                    _algorithm.Error(response.ErrorMessage);
                }
            }


            SendOrder(order);
        }

        /// <summary>
        /// Update order handler
        /// </summary>
        /// <param name="request">The update order request</param>
        private void HandleUpdatedOrder(UpdateOrderRequest request, OrderResponse response)
        {
            Order order = GetOrderById(request.OrderId);

            if (order == null)
            {
                response.Error(OrderResponseErrorCode.UnableToFindOrder, String.Format("Missing order [{0}]", request.OrderId));
            }
            else if (order.Status != OrderStatus.Submitted)
            {
                response.Error(OrderResponseErrorCode.InvalidOrderStatus, String.Format("Invalid order status: {0}", order.Status));
            }
            else
            {
                var updatedOrder = order.Clone();
                updatedOrder.ApplyUpdate(request);

                if (!_algorithm.Transactions.GetSufficientCapitalForOrder(_algorithm.Portfolio, updatedOrder))
                {
                    response.Error(OrderResponseErrorCode.InsufficientBuyingPower, string.Format("Order Error: id: {0}, Insufficient buying power to complete order (Value:{1}).", order.Id, order.Value));
                    _algorithm.Error(response.ErrorMessage);
                }
                else if (CanUpdateOrder(updatedOrder) == false)
                {
                    response.Error(OrderResponseErrorCode.BrokerageHandlerRefusedToUpdateOrder, "Unable to update order with ID " + order.Id + ".");
                    Log.Error("BrokerageTransactionHandler.HandleUpdatedOrder(): " + response.ErrorMessage);
                }
                else if (!_brokerage.UpdateOrder(updatedOrder))
                {
                    response.Error(OrderResponseErrorCode.BrokerageFailedToUpdateOrder);
                }
                else
                {
                    response.Processed();

                    _orders[updatedOrder.Id] = updatedOrder;
                    SendOrder(updatedOrder);
                }
            }
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
        /// Cancel order handler
        /// </summary>
        /// <param name="request">Cancel order request</param>
        private void HandleCancelledOrder(CancelOrderRequest request, OrderResponse response)
        {
            Order order = GetOrderById(request.OrderId);
            if (order != null)
            {

                if (order.Status == OrderStatus.Submitted) //partially filled?
                {
                    order.Status = OrderStatus.Canceled;

                    if (!_brokerage.CancelOrder(order))
                    {
                        // we failed to cancel the order for some reason

                        response.Error(OrderResponseErrorCode.BrokerageFailedToCancelOrder);

                        order.Status = OrderStatus.Invalid;
                    }
                    else
                    {
                        response.Processed();
                    }

                    SendOrder(order);
                }
                else
                {
                    response.Error(OrderResponseErrorCode.InvalidOrderStatus, String.Format("Cannot cancel order [{0}] with status: {1}", order.Id, order.Status));
                    Log.Error("BrokerageTransactionHandler.HandleCancelledOrder(): Unable to cancel order with ID " + order.Id + ".");
                }
            }
            else
            {
                response.Error(OrderResponseErrorCode.UnableToFindOrder, String.Format("Missing order [{0}]", request.OrderId));
            }
        }

        private bool ProcessOrderEvents()
        {
            int remainingCount = _orderEventQueue.Count;

            if (remainingCount == 0) 
                return false;

            while (remainingCount-- > 0)
            {
                OrderEvent orderEvent;

                if (_orderEventQueue.TryDequeue(out orderEvent) == false)
                    break;

                try
                {
                    HandleOrderEvent(orderEvent);
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                }
            }

            return true;
        }

        private void HandleOrderEvent(OrderEvent fill)
        {
            // update the order status
            var order = GetOrderById(fill.OrderId);
            if (order == null)
            {
                Log.Error("BrokerageTransactionHandler.HandleOrderEvnt(): Unable to locate Order with id " + fill.OrderId);
                return;
            }

            // set the status of our order object based on the fill event
            order.Status = fill.Status;

            SendOrder(order);

            // save that the order event took place, we're initializing the list with a capacity of 2 to reduce number of mallocs
            //these hog memory
            //List<OrderEvent> orderEvents = _orderEvents.GetOrAdd(orderEvent.OrderId, i => new List<OrderEvent>(2));
            //orderEvents.Add(orderEvent);

            //Apply the filled order to our portfolio:
            if (fill.Status == OrderStatus.Filled || fill.Status == OrderStatus.PartiallyFilled)
            {
                Interlocked.Exchange(ref _lastFillTimeTicks, DateTime.Now.Ticks);
                _algorithm.Portfolio.ProcessFill(fill);
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
                }
            }
        }

        private bool ProcessAccountEvents()
        {
            int remainingCount = _accountEventQueue.Count;

            if (remainingCount == 0)
                return false;

            while (remainingCount-- > 0)
            {
                AccountEvent accountEvent;

                if (_accountEventQueue.TryDequeue(out accountEvent) == false)
                    break;

                try
                {
                    HandleAccountChanged(accountEvent);
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                }
            }

            return true;
        }

        /// <summary>
        /// Brokerages can send account updates, this include cash balance updates. Since it is of
        /// utmost important to always have an accurate picture of reality, we'll trust this information
        /// as truth
        /// </summary>
        private void HandleAccountChanged(AccountEvent account)
        {
            // how close are we?
            var delta = _algorithm.Portfolio.CashBook[account.CurrencySymbol].Quantity - account.CashBalance;
            if (delta != 0)
            {
                Log.Trace(string.Format("BrokerageTransactionHandler.HandleAccountChanged(): {0} Cash Delta: {1}", account.CurrencySymbol, delta));
            }

            // we don't actually want to do this, this data can be delayed
            // override the current cash value to we're always gauranted to be in sync with the brokerage's push updates
            //_algorithm.Portfolio.CashBook[account.CurrencySymbol].Quantity = account.CashBalance;
        }

        private bool ProcessSecurityEvents()
        {
            int remainingCount = _securityEventQueue.Count;

            if (remainingCount == 0)
                return false;

            while (remainingCount-- > 0)
            {
                SecurityEvent securityEvent;

                if (_securityEventQueue.TryDequeue(out securityEvent) == false)
                    break;

                try
                {
                    HandleSecurityHoldingUpdated(securityEvent);
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                }
            }

            return true;
        }

        /// <summary>
        /// Brokerages can send portfolio updates which should include average price of holdings and the
        /// quantity of holdings, we'll trust this information as truth and just set the portfolio with it
        /// </summary>
        private void HandleSecurityHoldingUpdated(SecurityEvent holding)
        {
            // how close are we?
            var securityHolding = _algorithm.Portfolio[holding.Symbol];
            var deltaQuantity = securityHolding.Quantity - holding.Quantity;
            var deltaAvgPrice = securityHolding.AveragePrice - holding.AveragePrice;
            if (deltaQuantity != 0 || deltaAvgPrice != 0)
            {
                Log.Trace(string.Format("BrokerageTransactionHandler.HandleSecurityHoldingUpdated(): {0} DeltaQuantity: {1} DeltaAvgPrice: {2}", holding.Symbol, deltaQuantity, deltaAvgPrice));
            }

            // we don't actually want to do this, this data can be delayed
            //securityHolding.SetHoldings(holding.AveragePrice, holding.Quantity);
        }

        /// <summary>
        /// Gets the amount of time since the last call to algorithm.Portfolio.ProcessFill(fill)
        /// </summary>
        private TimeSpan TimeSinceLastFill
        {
            get { return DateTime.Now - new DateTime(Interlocked.Read(ref _lastFillTimeTicks)); }
        }

        /// <summary>
        /// Gets the date of the last sync
        /// </summary>
        private DateTime LastSyncDate
        {
            get { return new DateTime(Interlocked.Read(ref _lastSyncTimeTicks)).Date; }
        }
    }
}