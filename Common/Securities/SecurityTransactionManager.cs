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

/**********************************************************
* USING NAMESPACES
**********************************************************/
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using QuantConnect.Logging;
using QuantConnect.Orders;

namespace QuantConnect.Securities 
{
    /******************************************************** 
    * CLASS DEFINITIONS
    *********************************************************/
    /// <summary>
    /// Algorithm Transactions Manager - Recording Transactions
    /// </summary>
    public class SecurityTransactionManager 
    {
        /******************************************************** 
        * CLASS PRIVATE VARIABLES
        *********************************************************/
        private SecurityManager _securities;
        private ConcurrentDictionary<int, Order> _orders = new ConcurrentDictionary<int, Order>();
        private ConcurrentQueue<Order> _orderQueue = new ConcurrentQueue<Order>();
        private ConcurrentDictionary<int, List<OrderEvent>> _orderEvents = new ConcurrentDictionary<int, List<OrderEvent>>();
        private Dictionary<DateTime, decimal> _transactionRecord = new Dictionary<DateTime, decimal>();
        private int _orderId = 1;
        private decimal _minimumOrderSize = 0;
        private int _minimumOrderQuantity = 1;

        /******************************************************** 
        * CLASS PUBLIC VARIABLES
        *********************************************************/

        /******************************************************** 
        * CLASS CONSTRUCTOR
        *********************************************************/
        /// <summary>
        /// Initialise the transaction manager for holding and processing orders.
        /// </summary>
        public SecurityTransactionManager(SecurityManager security)
        {
            //Private reference for processing transactions
            _securities = security;

            //Initialise the Order Cache -- Its a mirror of the TransactionHandler.
            _orders = new ConcurrentDictionary<int, Order>();

            //Temporary Holding Queue of Orders to be Processed.
            _orderQueue = new ConcurrentQueue<Order>();

            // Internal order events storage.
            _orderEvents = new ConcurrentDictionary<int, List<OrderEvent>>();

            //Interal storage for transaction records:
            _transactionRecord = new Dictionary<DateTime, decimal>();
        }

        /******************************************************** 
        * CLASS PROPERTIES
        *********************************************************/
        /// <summary>
        /// Queue for holding all orders sent for processing.
        /// </summary>
        /// <remarks>Potentially for long term algorithms this will be a memory hog. Should consider dequeuing orders after a 1 day timeout</remarks>
        public ConcurrentDictionary<int, Order> Orders 
        {
            get 
            {
                return _orders;
            }
            set
            {
                _orders = value;
            }
        }

        /// <summary>
        /// Temporary storage for orders while waiting to process via transaction handler. Once processed they are added to the primary order queue.
        /// </summary>
        /// <seealso cref="Orders"/>
        public ConcurrentQueue<Order> OrderQueue
        {
            get
            {
                return _orderQueue;
            }
            set 
            {
                _orderQueue = value;
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

        /******************************************************** 
        * CLASS METHODS
        *********************************************************/
        /// <summary>
        /// Add an order to collection and return the unique order id or negative if an error.
        /// </summary>
        /// <param name="order">New order object to add to processing list</param>
        /// <returns>New unique, increasing orderid</returns>
        public virtual int AddOrder(Order order) 
        {
            try 
            {
                //Ensure its flagged as a new order for the transaction handler.
                order.Id = _orderId++;
                order.Status = OrderStatus.New;
                //Add the order to the cache to monitor
                OrderQueue.Enqueue(order);
            }
            catch (Exception err)
            {
                Log.Error("Algorithm.Transaction.AddOrder(): " + err.Message);
            }
            return order.Id;
        }

        /// <summary>
        /// Update an order yet to be filled such as stop or limit orders.
        /// </summary>
        /// <param name="order">Order to Update</param>
        /// <remarks>Does not apply if the order is already fully filled</remarks>
        /// <returns>
        ///     Id of the order we modified or 
        ///     -5 if the order was already filled or cancelled
        ///     -6 if the order was not found in the cache
        /// </returns>
        public int UpdateOrder(Order order) 
        {
            try 
            {
                //Update the order from the behaviour
                var id = order.Id;
                order.Time = _securities[order.Symbol].Time;

                //Validate order:
                if (order.Price == 0 || order.Quantity == 0) return -1;

                if (_orders.ContainsKey(id))
                {
                    //-> If its already filled return false; can't be updated
                    if (_orders[id].Status == OrderStatus.Filled || _orders[id].Status == OrderStatus.Canceled)
                    {
                        return -5;
                    }

                    //Flag the order to be resubmitted.
                    order.Status = OrderStatus.Update;
                    _orders[id] = order;

                    //Send the order to transaction handler for update to be processed.
                    OrderQueue.Enqueue(order);
                } 
                else 
                {
                    //-> Its not in the orders cache, shouldn't get here
                    return -6;
                }
            } 
            catch (Exception err) 
            {
                Log.Error("Algorithm.Transactions.UpdateOrder(): " + err.Message);
                return -7;
            }
            return 0;
        }


        /// <summary>
        /// Remove this order from outstanding queue: user is requesting a cancel.
        /// </summary>
        /// <param name="orderId">Specific order id to remove</param>
        public virtual void RemoveOrder(int orderId) 
        {
            try
            {
                //Error check
                if (!Orders.ContainsKey(orderId)) 
                {
                    Log.Error("Security.TransactionManager.RemoveOutstandingOrder(): Cannot find this id.");
                    return;
                }

                var order = Orders[orderId];
                if (order.Status != OrderStatus.Submitted && order.Type != OrderType.Market) 
                {
                    Log.Error("Security.TransactionManager.RemoveOutstandingOrder(): Order already filled");
                    return;
                }

                //Update the status of the order
                order.Status = OrderStatus.Canceled;

                //Send back to queue to be reprocessed with new status
                OrderQueue.Enqueue(order);
            }
            catch (Exception err)
            {
                Log.Error("TransactionManager.RemoveOrder(): " + err.Message);
            }
        }


        /// <summary>
        /// Get the order by its id
        /// </summary>
        /// <param name="orderId">Order id to fetch</param>
        /// <returns></returns>
        public Order GetOrderById(int orderId)
        {
            Order order = null;
            try
            {
                if (!Orders.TryGetValue(orderId, out order))
                {
                    var pending = OrderQueue.ToList();

                    var pendingOrder = (from o in pending 
                                        where o.Id == orderId 
                                        select o).FirstOrDefault();

                    return pendingOrder;
                }
                return order;
            }
            catch (Exception err)
            {
                Log.Error("TransactionManager.RemoveOrder(): " + err.Message);
            }
            return order;
        }

        /// <summary>
        /// Check if there is sufficient capital to execute this order.
        /// </summary>
        /// <param name="portfolio">Our portfolio</param>
        /// <param name="order">Order we're checking</param>
        /// <returns>True if suficient capital.</returns>
        public bool GetSufficientCapitalForOrder(SecurityPortfolioManager portfolio, Order order)
        {
            if (Math.Abs(GetOrderRequiredBuyingPower(order)) > portfolio.GetBuyingPower(order.Symbol, order.Direction)) 
            {
                //Log.Debug("Symbol: " + order.Symbol + " Direction: " + order.Direction.ToString() + " Quantity: " + order.Quantity);
                //Log.Debug("GetOrderRequiredBuyingPower(): " + Math.Abs(GetOrderRequiredBuyingPower(order)) + " PortfolioGetBuyingPower(): " + portfolio.GetBuyingPower(order.Symbol, order.Direction)); 
                return false;
            }
            return true;
        }

        /// <summary>
        /// Using leverage property of security find the required cash for this order.
        /// </summary>
        /// <param name="order">Order to check</param>
        /// <returns>decimal cash required to purchase order</returns>
        private decimal GetOrderRequiredBuyingPower(Order order)
        {
            try
            {
                //Get the order value from the non-abstract order classes (MarketOrder, LimitOrder, StopMarketOrder)
                //Market order is approximated from the current security price and set in the MarketOrder Method in QCAlgorithm.
                var orderFees = _securities[order.Symbol].Model.GetOrderFee(order.Quantity, order.Price);

                //Return the total buying power for the order, including fees:
                return (Math.Abs(order.Value) / _securities[order.Symbol].Leverage) + orderFees; 
            } 
            catch(Exception err)
            {
                Log.Error("Security.TransactionManager.GetOrderRequiredBuyingPower(): " + err.Message);
            }
            //Prevent all orders if leverage is 0.
            return decimal.MaxValue;
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
