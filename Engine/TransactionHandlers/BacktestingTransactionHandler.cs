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
 *
*/

/**********************************************************
* USING NAMESPACES
**********************************************************/

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Orders;

namespace QuantConnect.Lean.Engine.TransactionHandlers
{
    /// <summary>
    /// Backtesting transaction handler class for modelling the order fills and portfolio impact when in a backtest.
    /// </summary>
    public class BacktestingTransactionHandler : ITransactionHandler
    {
        /******************************************************** 
        * CLASS VARIABLES
        *********************************************************/
        private int _orderId = 1;
        private bool _exitTriggered = false;
        private bool _ready = false;
        private bool _isActive = false;
        private IAlgorithm _algorithm;

        /******************************************************** 
        * CLASS PROPERTIES
        *********************************************************/
        /// <summary>
        /// The orders queue holds orders which are sent to exchange, partially filled, completely filled or cancelled.
        /// Once the transaction thread has worked on them they get put here while witing for fill updates.
        /// </summary>
        public ConcurrentDictionary<int, Order> Orders 
        {
            get
            {
                return _algorithm.Transactions.Orders;
            }
            set
            {
                _algorithm.Transactions.Orders = value;
            }
        }

        /// <summary>
        /// OrderEvents is an orderid indexed collection of events attached to each order. Because an order might be filled in 
        /// multiple legs it is important to keep a record of each event.
        /// </summary>
        public ConcurrentDictionary<int, List<OrderEvent>> OrderEvents
        {
            get
            {
                return _algorithm.Transactions.OrderEvents;
            }
            set
            {
                _algorithm.Transactions.OrderEvents = value;
            }
        }

        /// <summary>
        /// OrderQueue holds the newly updated orders from the user algorithm waiting to be processed. Once
        /// orders are processed they are moved into the Orders queue awaiting the brokerage response.
        /// </summary>
        public ConcurrentQueue<Order> OrderQueue 
        {
            get
            {
                return _algorithm.Transactions.OrderQueue;
            }
            set
            {
                _algorithm.Transactions.OrderQueue = value;
            }
        }

        /// <summary>
        /// Boolean flag signalling the handler is ready and all orders have been processed.
        /// </summary>
        public bool Ready 
        {
            get 
            {
                return _ready;
            }
        }

        /// <summary>
        /// Boolean flag indicating the thread is busy. 
        /// False indicates it is completely finished processing and ready to be terminated.
        /// </summary>
        public bool IsActive 
        { 
            get
            {
                return _isActive;
            }
        }

        /******************************************************** 
        * CONSTRUCTOR
        *********************************************************/
        /// <summary>
        /// Constructor for the backtesting transaction handler.
        /// </summary>
        /// <param name="algorithm">Algorithm instance</param>
        public BacktestingTransactionHandler(IAlgorithm algorithm)
        {
            _algorithm = algorithm;
            _isActive = true;
            _ready = false;
        }

        /******************************************************** 
        * CLASS METHODS
        *********************************************************/
        /// <summary>
        /// Primary thread entry point to launch the transaction thread.
        /// </summary>
        public void Run() 
        {
            //Run until the end of the algorithm.
            while (!_exitTriggered)
            {
                //1. Add order commands from queue to primary order list.
                if (OrderQueue.Count == 0)
                {
                    //We've processed all the orders in queue.Allow interruption of thread if nothing to do (99.99% of time).
                    _ready = true;  
                    //Set finished processing flag:
                    _algorithm.ProcessingOrder = false;
                    //NOP.
                    Thread.Sleep(1);
                }
                else 
                {
                    //We're working...
                    _ready = false;

                    //Scan jobs in the new orders queue: 
                    Order order;
                    if (OrderQueue.TryDequeue(out order))
                    {
                        switch (order.Status)
                        {
                            case OrderStatus.New:
                                //If we don't have this key, add it to the dictionary
                                if (!Orders.ContainsKey(order.Id))
                                {
                                    //Tell algorithm to wait:
                                    _algorithm.ProcessingOrder = true;

                                    //Add the order to the collection
                                    Orders.TryAdd(order.Id, order);
                                }
                                break;

                            case OrderStatus.Canceled:
                                if (Orders.ContainsKey(order.Id) && Orders[order.Id].Status == OrderStatus.Submitted)
                                {
                                    //Just set the master dictionary to a cancelled order, only IF we've only been submitted and no further processing.
                                    Orders[order.Id] = order;
                                }
                                break;

                            case OrderStatus.Update:
                                if (Orders.ContainsKey(order.Id) && Orders[order.Id].Status == OrderStatus.Submitted)
                                {
                                    //Just set the master dictionary to a updated order, only IF we've only been submitted and no further processing.
                                    Orders[order.Id] = order;
                                }
                                break;
                        }
                    }
                }

                //2. NOW ALL ORDERS IN ORDER DICTIONARY::> 
                //   Scan through Orders: Process fills. Trigger Events.
                //   Refresh the order model: look at the orders for ones - process every time.
                var keys =    (from order in Orders
                               where  order.Value.Status != OrderStatus.Filled && 
                                      order.Value.Status != OrderStatus.Canceled && 
                                      order.Value.Status != OrderStatus.Invalid
                               select order.Key).ToList<int>();

                //Now we have the list of keys; re-apply the order models to each order.
                foreach (var id in keys)
                {
                    //We're working...
                    var fill = new OrderEvent();
                    _ready = false;

                    var order = Orders[id];
                    var sufficientBuyingPower = _algorithm.Transactions.GetSufficientCapitalForOrder(_algorithm.Portfolio, order);

                    //Before we check this queued order make sure we have buying power:
                    if (sufficientBuyingPower)
                    {
                        //Based on the order type: refresh its model to get fill price and quantity
                        fill = _algorithm.Securities[order.Symbol].Model.Fill(_algorithm.Securities[order.Symbol], order);

                        //Apply the filled order to our portfolio:
                        if (fill.Status == OrderStatus.Filled || fill.Status == OrderStatus.PartiallyFilled)
                        {
                            //If the fill models come back suggesting filled, process the affects on portfolio
                            _algorithm.Portfolio.ProcessFill(fill);
                        }
                    }
                    else 
                    { 
                        //Flag order as invalid and push off queue:
                        order.Status = OrderStatus.Invalid;
                        _algorithm.Error("Order Error: id: " + id + ": Insufficient buying power to complete order.");
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

            } // End While.

            Log.Trace("BacktestingTransactionHandler.Run(): Ending Thread...");
            _isActive = false;
        }

        /// <summary>
        /// Submit a new order to be processed.
        /// </summary>
        /// <param name="order">New order object</param>
        /// <returns>New unique quantconnect order id</returns>
        public int NewOrder(Order order) 
        {
            //If this is a new order (with no id) set it:
            if (order.Id == 0) order.Id = _orderId++;

            //Submit to queue
            order.Status = OrderStatus.New;
            OrderQueue.Enqueue(order);
            _ready = false;

            return order.Id;
        }

        /// <summary>
        /// Update and resubmit the order to the OrderQueue for processing.
        /// </summary>
        /// <param name="order">Order we'd like updated</param>
        /// <returns>True if successful, false if already cancelled or filled.</returns>
        public bool UpdateOrder(Order order) 
        {
            //Failed.
            if (Orders[order.Id].Status == OrderStatus.Filled || Orders[order.Id].Status == OrderStatus.Canceled) 
            {
                return false;
            }

            //Flag the order as new, send it to the queue:
            order.Status = OrderStatus.Update;
            OrderQueue.Enqueue(order);
            _ready = false;

            return true;
        }

        /// <summary>
        /// Cancel the order specified
        /// </summary>
        /// <param name="order">Order we'd like to cancel.</param>
        /// <returns>True if successful, false if its already been cancelled or filled.</returns>
        public bool CancelOrder(Order order)
        {
            //Failed.
            if (Orders[order.Id].Status == OrderStatus.Filled || Orders[order.Id].Status == OrderStatus.Canceled)
            {
                return false;
            }

            //Flag the order as new, send it to the queue:
            order.Status = OrderStatus.Canceled;
            OrderQueue.Enqueue(order);
            _ready = false;

            return true;
        }

        /// <summary>
        /// Set a local reference to the algorithm instance.
        /// </summary>
        /// <param name="algorithm">IAlgorithm object</param>
        public void SetAlgorithm(IAlgorithm algorithm)
        {
            //Access to the algorithm 
            _algorithm = algorithm;
        }

        /// <summary>
        /// Signal a end of thread request to stop montioring the transactions.
        /// </summary>
        public void Exit() 
        {
            _exitTriggered = true;
        }

    } // End Algorithm Class:

} // End Namespace
