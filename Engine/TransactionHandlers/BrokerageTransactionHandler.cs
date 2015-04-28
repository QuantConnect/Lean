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
using QuantConnect.Interfaces;
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
        private readonly IBrokerage _brokerage;

        // pulled directly from the algorithm

        /// <summary>
        /// OrderQueue holds the newly updated orders from the user algorithm waiting to be processed. Once
        /// orders are processed they are moved into the Orders queue awaiting the brokerage response.
        /// </summary>
        private ConcurrentQueue<Order> _orderQueue;

        /// <summary>
        /// The orders queue holds orders which are sent to exchange, partially filled, completely filled or cancelled.
        /// Once the transaction thread has worked on them they get put here while witing for fill updates.
        /// </summary>
        private ConcurrentDictionary<int, Order> _orders;

        /// <summary>
        /// OrderEvents is an orderid indexed collection of events attached to each order. Because an order might be filled in 
        /// multiple legs it is important to keep a record of each event.
        /// </summary>
        private ConcurrentDictionary<int, List<OrderEvent>> _orderEvents;

        /// <summary>
        /// Creates a new BrokerageTransactionHandler to process orders using the specified brokerage implementation
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="brokerage">The brokerage implementation to process orders and fire fill events</param>
        public BrokerageTransactionHandler(IAlgorithm algorithm, IBrokerage brokerage)
        {
            if (brokerage == null)
            {
                throw new ArgumentNullException("brokerage");
            }

            _brokerage = brokerage;
            _brokerage.OrderStatusChanged += (sender, fill) =>
            {
                HandleOrderEvent(fill);
            };

            _brokerage.SecurityHoldingUpdated += (sender, holding) =>
            {
                HandleSecurityHoldingUpdated(holding);
            };

            // maintain proper portfolio cash balance
            _brokerage.AccountChanged += (sender, account) =>
            {
                HandleAccountChanged(account);
            };

            IsActive = true;

            _algorithm = algorithm;

            // also save off the various order data structures locally
            _orders = algorithm.Transactions.Orders;
            _orderEvents = algorithm.Transactions.OrderEvents;
            _orderQueue = algorithm.Transactions.OrderQueue;
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
            get { return !_algorithm.ProcessingOrder; }
        }

        /// <summary>
        /// Primary thread entry point to launch the transaction thread.
        /// </summary>
        public void Run()
        {
            while (!_exitTriggered)
            {
                // if it's empty just sleep this thread for a little bit

                Order order;
                if (!_orderQueue.TryDequeue(out order))
                {
                    _algorithm.ProcessingOrder = false;
                    Thread.Sleep(1);
                    continue;
                }

                _algorithm.ProcessingOrder = true;

                // we should never encounter a hold order direction, since it is the uninitialized state
                if (order.Direction == OrderDirection.Hold)
                {
                    Log.Error("BrokerageTransactionHandler.Run(): Encountered OrderDirection.Hold in OrderID: " + order.Id);
                    
                    // move all orders into permanent storage
                    if (!_orders.TryAdd(order.Id, order))
                    {
                        Log.Error("BrokerageTransactionHandler.Run(): Unable to add order to permanent storage. OrderID: " + order.Id + " Status: " + order.Status);
                    }
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

                ProcessSynchronousEvents();
            }

            Log.Trace("BrokerageTransactionHandler.Run(): Ending Thread...");
            IsActive = false;
        }

        /// <summary>
        /// Processes all synchronous events that must take place before the next time loop for the algorithm
        /// </summary>
        public virtual void ProcessSynchronousEvents()
        {
            // how to do synchronous market orders for real brokerages?

            // we want to remove orders older than 10k records, but only in live mode
            if (!_algorithm.LiveMode) return;

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
            if (_orders.TryAdd(order.Id, order))
            {
                // check to see if we have enough money to place the order
                if (!_algorithm.Transactions.GetSufficientCapitalForOrder(_algorithm.Portfolio, order))
                {
                    order.Status = OrderStatus.Invalid;
                    _algorithm.Error(string.Format("Order Error: id: {0}, Insufficient buying power to complete order (Value:{1}).", order.Id, order.Value));
                    return;
                }

                // verify that our current brokerage can actually take the order
                if (!_brokerage.CanProcessOrder(order))
                {
                    // if we couldn't actually process the order, mark it as invalid and bail
                    order.Status = OrderStatus.Invalid;
                    _algorithm.Error(string.Format("Order Error: id: {0}, Brokerage {1} is unable to process order for {2} - {3}.", order.Id, _brokerage.Name, order.SecurityType, order.Symbol));
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
            else
            {
                Log.Error("BrokerageTransactionHandler.HandleNewOrder(): Unable to add new order, order not processed.");
            }
        }

        /// <summary>
        /// Update order handler
        /// </summary>
        /// <param name="order">The updated order</param>
        private void HandleUpdatedOrder(Order order)
        {
            Order queued;
            if (_orders.TryGetValue(order.Id, out queued) && (queued.Status == OrderStatus.Submitted)) //partially filled?
            {
                _orders[order.Id] = order;
                if (!_brokerage.UpdateOrder(order))
                {
                    // we failed to update the order for some reason
                    order.Status = OrderStatus.Invalid;
                }
            }
            else
            {
                Log.Error("BrokerageTransactionHandler.HandleUpdatedOrder(): Unable to update order with ID " + order.Id + ".");
            }
        }

        /// <summary>
        /// Cancel order handler
        /// </summary>
        /// <param name="order">The cancelled order</param>
        private void HandleCancelledOrder(Order order)
        {
            Order queued;
            if (_orders.TryGetValue(order.Id, out queued) && (queued.Status == OrderStatus.Submitted)) //partially filled?
            {
                _orders[order.Id] = order;

                if (!_brokerage.CancelOrder(order))
                {
                    // we failed to cancel the order for some reason
                    order.Status = OrderStatus.Invalid;
                }
            }
            else
            {
                Log.Error("BrokerageTransactionHandler.HandleCancelledOrder(): Unable to cancel order with ID " + order.Id + ".");
            }
        }

        private void HandleOrderEvent(OrderEvent fill)
        {
            // update the order status
            var order = _algorithm.Transactions.GetOrderById(fill.OrderId);
            if (order == null)
            {
                Log.Error("BrokerageTransactionHandler.HandleOrderEvnt(): Unable to locate Order with id " + fill.OrderId);
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
                _algorithm.Portfolio.ProcessFill(fill);
            }

            //We have an event! :) Order filled, send it in to be handled by algorithm portfolio.
            if (fill.Status != OrderStatus.None) //order.Status != OrderStatus.Submitted
            {
                //Create new order event:
                Engine.ResultHandler.OrderEvent(fill);
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

            // override the current cash value to we're always gauranted to be in sync with the brokerage's push updates
            _algorithm.Portfolio.CashBook[account.CurrencySymbol].Quantity = account.CashBalance;
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

            securityHolding.SetHoldings(holding.AveragePrice, holding.Quantity);
        }
    }
}