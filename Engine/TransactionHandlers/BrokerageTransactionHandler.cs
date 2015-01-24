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

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using QuantConnect.Brokerages;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Orders;

namespace QuantConnect.Lean.Engine.TransactionHandlers
{
    /// <summary>
    /// Transaction handler for all brokerages
    /// </summary>
    public class BrokerageTransactionHandler : ITransactionHandler
    {
        // order id counter
        private int _orderId;

        private bool _exitTriggered;
        private IAlgorithm _algorithm;
        private readonly IBrokerage _brokerage;
        private ConcurrentQueue<Order> _orderQueue;
        private ConcurrentDictionary<int, Order> _orders;
        private ConcurrentDictionary<int, List<OrderEvent>> _orderEvents;

        /// <summary>
        /// Creates a new BrokerageTransactionHandler to process orders using the specified brokerage implementation
        /// </summary>
        /// <param name="brokerage">The brokerage implementation to process orders and fire fill events</param>
        public BrokerageTransactionHandler(IBrokerage brokerage)
        {
            _brokerage = brokerage;
            _brokerage.OrderFilled += (sender, orderEvent) =>
            {
                // save that the order event took place
                List<OrderEvent> orderEvents = _orderEvents.GetOrAdd(orderEvent.OrderId, i => new List<OrderEvent>(2));
                orderEvents.Add(orderEvent);

                // update the order in our orders collection

                // update the algorithm with the order event
                _algorithm.Portfolio.ProcessFill(orderEvent);
            };

            //_brokerage.AccountChanged +=
            //_brokerage.PortfolioChanged +=

            IsActive = true;
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
            get { return _orderQueue.Count == 0; }
        }

        /// <summary>
        /// Primary thread entry point to launch the transaction thread.
        /// </summary>
        public void Run()
        {
            while (!_exitTriggered)
            {
                // if it's empty just sleep this thread for a little bit
                if (_orderQueue.Count == 0)
                {
                    _algorithm.ProcessingOrder = false;
                    Thread.Sleep(1);
                    continue;
                }

                // dequeue and process all orders in our queue

                Order order;
                while (_orderQueue.TryDequeue(out order))
                {
                    // we should never encounter a hold order direction, since it is the uninitialized state
                    if (order.Direction == OrderDirection.Hold)
                    {
                        Log.Error("BrokerageTransactionHandler.Run(): Encountered OrderDirection.Hold in OrderID: " + order.Id);
                        AddOrderToPermanentStorage(order);
                        continue;
                    }

                    // process the order properly depending on it's current status
                    switch (order.Status)
                    {
                        case OrderStatus.New:
                            HandleNewOrder(order); 
                            break;

                        case OrderStatus.Update:
                            HandleUpdatedOrder(order);
                            break;

                        case OrderStatus.Canceled:
                            HandleCancelledOrder(order);
                            break;

                        // we should not see orders with this status in the order queue
                        case OrderStatus.None:
                        case OrderStatus.Invalid:
                        case OrderStatus.PartiallyFilled:
                        case OrderStatus.Filled:
                        case OrderStatus.Submitted:
                            Log.Error("BrokerageTransactionHandler.Run(): Invalid order status found in order queue. OrderID: " + order.Id + " Status: " + order.Status);
                            break;
                    }

                    AddOrderToPermanentStorage(order);
                }
            }

            IsActive = false;
        }

        /// <summary>
        /// Set a local reference to the algorithm instance. This allows the transaction handler to update the algorithm with fill events
        /// </summary>
        /// <param name="algorithm">IAlgorithm object</param>
        public void SetAlgorithm(IAlgorithm algorithm)
        {
            _algorithm = algorithm;

            // also save off the various order data structures locally
            _orders = algorithm.Transactions.Orders;
            _orderEvents = algorithm.Transactions.OrderEvents;
            _orderQueue = algorithm.Transactions.OrderQueue;
        }

        /// <summary>
        /// Signal a end of thread request to stop montioring the transactions.
        /// </summary>
        public void Exit()
        {
            _exitTriggered = true;
        }

        /// <summary>
        /// New order handler
        /// </summary>
        /// <param name="order">The new order</param>
        private void HandleNewOrder(Order order)
        {
            // create new id in thread safe manner
            if (order.Id == 0) order.Id = Interlocked.Increment(ref _orderId);

            // tell algorithm to wait during scynchronous backtests
            _algorithm.ProcessingOrder = true;
            if (!_orders.TryAdd(order.Id, order))
            {
                Log.Error("BrokerageTransactionHandler.Run(): New: Unable to add new order, order not processed.");
            }
            else
            {
                // check for buying power
                bool sufficientBuyingPower = _algorithm.Transactions.GetSufficientCapitalForOrder(_algorithm.Portfolio, order);

                if (!sufficientBuyingPower)
                {
                    order.Status = OrderStatus.Invalid;
                    _algorithm.Error("Order Error: id: " + order.Id + ": Insufficient buying power to complete order.");
                    return;
                }

                // set the order status based on whether or not we successfully submitted the order to the market
                if (_brokerage.PlaceOrder(order))
                {
                    order.Status = OrderStatus.Submitted;
                }
                else
                {
                    order.Status = OrderStatus.Invalid;
                }
            }
        }

        /// <summary>
        /// Update order handler
        /// </summary>
        /// <param name="order">The updated order</param>
        private void HandleUpdatedOrder(Order order)
        {
            Order queued;
            if (_orders.TryGetValue(order.Id, out queued) && (queued.Status == OrderStatus.Submitted || queued.Status == OrderStatus.New)) //partially filled?
            {
                _orders[order.Id] = order;
                if (!_brokerage.UpdateOrder(order))
                {
                    // we failed to update the order for some reason
                    order.Status = OrderStatus.Invalid;
                }
            }
        }

        /// <summary>
        /// Cancel order handler
        /// </summary>
        /// <param name="order">The cancelled order</param>
        private void HandleCancelledOrder(Order order)
        {
            Order queued;
            if (_orders.TryGetValue(order.Id, out queued) && (queued.Status == OrderStatus.Submitted || queued.Status == OrderStatus.New)) //partially filled?
            {
                _orders[order.Id] = order;
                if (!_brokerage.CancelOrder(order))
                {
                    // we failed to cancel the order for some reason
                    order.Status = OrderStatus.Invalid;
                }
            }
        }

        /// <summary>
        /// Adds the order to the permanent storage dictionary
        /// </summary>
        /// <param name="order">The order to be stored</param>
        private void AddOrderToPermanentStorage(Order order)
        {
            // move all orders into permanent storage
            if (!_orders.TryAdd(order.Id, order))
            {
                Log.Error("BrokerageTransactionHandler.Run(): Unable to add order to permanent storage. OrderID: " + order.Id + " Status: " + order.Status);
            }
        }
    }
}