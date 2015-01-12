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
using QuantConnect.Brokerages;
using QuantConnect.Brokerages.Tradier;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Securities;

namespace QuantConnect.Lean.Engine.TransactionHandlers
{
    /******************************************************** 
    * QUANTCONNECT PROJECT LIBRARIES
    *********************************************************/
    /// <summary>
    /// Handle the Transactions Requests from Live Trading Cloud Algorithms.
    /// </summary>
    public class TradierTransactionHandler : ITransactionHandler
    {
        /******************************************************** 
        * CLASS VARIABLES
        *********************************************************/
        private TradierBrokerage _tradier = new TradierBrokerage();
        private bool _isActive = true;
        private bool _ready = false;
        private int _orderId = 0;
        private bool _exitTriggered = false;
        private int _accountId = 0;
        private DateTime _refreshOrders = new DateTime();
        private List<TradierOrder> _previousOrders = new List<TradierOrder>();
        private IAlgorithm _algorithm;
        private IResultHandler _results;

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
        /// Constructor for the tradier transaction handler
        /// </summary>
        /// <param name="algorithm">Algorithm instance</param>
        /// <param name="brokerage">Brokerage instance</param>
        /// <param name="results">Result handler </param>
        /// <param name="accountId">Tradier account id</param>
        public TradierTransactionHandler(IAlgorithm algorithm, IBrokerage brokerage, IResultHandler results, int accountId)
        {
            _algorithm = algorithm;
            _isActive = true;
            _ready = false;
            _accountId = accountId;

            //Connect with Tradier:
            _tradier = (TradierBrokerage)brokerage;
            _results = results;
        }


        /******************************************************** 
        * CLASS METHODS
        *********************************************************/
        /// <summary>
        /// Primary thread entry point to launch the transaction thread.
        /// </summary>
        public void Run()
        {
            while (!_exitTriggered)
            {
                try
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
                        //We're now processing an order: 
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

                                        //TRADIER Requires Processing Cross-Zero Orders in TWO Parts:
                                        //-> If Long Going Short, Break order up into two, process one as "Sell", other as "Short".

                                        //If neccessary divide the order into two components:
                                        var portfolio = _algorithm.Portfolio;
                                        if (portfolio.ContainsKey(order.Symbol))
                                        {
                                            var crossZero = DetectZeroCrossing(order, portfolio[order.Symbol]);
                                            var currentHoldings = portfolio[order.Symbol].Quantity;

                                            //If crossing zero, first order is to close out position 
                                            if (crossZero)
                                            {
                                                var firstOrderQuantity = 0;
                                                var secondOrderQuantity = 0;

                                                //Break into two orders, make second order contingent on first processing:
                                                //1. First order, close out to zero.
                                                if (currentHoldings > 0) 
                                                {
                                                    // First order close out to zero: a sell order:
                                                    firstOrderQuantity = -1 * currentHoldings;
                                                    secondOrderQuantity = -1 * Convert.ToInt32(order.AbsoluteQuantity - currentHoldings);

                                                }
                                                else if (currentHoldings < 0) 
                                                {
                                                    firstOrderQuantity = Math.Abs(currentHoldings);
                                                    secondOrderQuantity = order.Quantity - firstOrderQuantity;
                                                }

                                                //Set the first order quantity:
                                                order.Quantity = firstOrderQuantity;
                                                while (!Orders.TryAdd(order.Id, order)) { };

                                                //Create the second order: add to queue, make contingent on primary order.
                                                var secondOrder = new Order(order.Symbol, Convert.ToInt32(secondOrderQuantity), order.Type, order.Time, order.Price, order.Tag);
                                                secondOrder.Id = _algorithm.Transactions.GetIncrementOrderId();
                                                secondOrder.Status = OrderStatus.New;
                                                secondOrder.ContingentId = order.Id;
                                                while (!Orders.TryAdd(secondOrder.Id, order)) { };
                                            }
                                            else 
                                            {
                                                //If not zero crossing, simply add the order to the collection
                                                while (!Orders.TryAdd(order.Id, order)) { };
                                            }
                                        }
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
                    var keys = (from order in Orders
                                      where order.Value.Status != OrderStatus.Filled &&
                                            order.Value.Status != OrderStatus.Canceled &&
                                            order.Value.Status != OrderStatus.Invalid && 
                                            order.Value.Direction != OrderDirection.Hold
                                      select order.Key).ToList<int>();

                    //Now we have the list of keys; re-apply the order models to each order.
                    foreach (var id in keys)
                    {
                        //We're working...
                        _ready = false;
                        var order = Orders[id];
                        
                        //Make sure we have this in our portfolio:
                        if (!_algorithm.Portfolio.ContainsKey(order.Symbol)) continue;

                        //Don't process until contingent order completed:
                        if (order.ContingentId != 0 && Orders.ContainsKey(order.ContingentId)) 
                        {
                            if (Orders[order.ContingentId].Status != OrderStatus.Filled) continue;
                        }

                        //Make sure we have sufficient buying power:
                        var sufficientBuyingPower = _algorithm.Transactions.GetSufficientCapitalForOrder(_algorithm.Portfolio, order);

                        //Before we check this queued order make sure we have buying power:
                        if (sufficientBuyingPower)
                        {
                            var response = _tradier.PlaceOrder( 
                                accountId: _accountId, 
                                classification: TradierOrderClass.Equity,
                                direction: Direction(_algorithm.Portfolio[order.Symbol].Quantity, order), 
                                symbol: order.Symbol, 
                                quantity: Convert.ToDecimal(order.AbsoluteQuantity), 
                                price: order.Price, 
                                stop: order.Price, 
                                optionSymbol: "", 
                                type: OrderType(order.Type), 
                                duration: TradierOrderDuration.GTC);

                            if (response != null && response.Order != null && response.Errors.Errors.Count == 0)
                            {
                                //Save brokerage Id:
                                order.BrokerId.Add(response.Order.Id);
                                order.Tag = response.Order.Status;
                                //Set status as submitted, no more:
                                order.Status = OrderStatus.Submitted;
                            }
                        }
                        else
                        {
                            //Flag order as invalid and push off queue:
                            order.Status = OrderStatus.Invalid;
                            _algorithm.Error("Order Error: id: " + id + ": Insufficient buying power to complete order.");
                        }
                    }

                    // Check the key list: if more than 0-> there are orders pending:
                    if (keys.Count > 0 && DateTime.Now > _refreshOrders)
                    {
                        //Fetch orders and schedule for next refresh in 200ms.
                        var orderDetails = _tradier.FetchOrders(_accountId);
                        _refreshOrders = DateTime.Now.AddMilliseconds(200);

                        //Go through each submitted order, detect fills, process fills when delta from known fill.
                        foreach (var orderState in orderDetails)
                        {
                            //Process order: detect fills.
                            if (orderState.Class != TradierOrderClass.Equity) continue;

                            var deltaFilled = orderState.QuantityExecuted;
                            var status = OrderStatus.Filled;

                            //Look at fill prices as they happen, detect if Quantity changes from known quantity
                            var previousState = (from previous in _previousOrders
                                                 where previous.Id == orderState.Id
                                                 select previous).SingleOrDefault();

                            // Previous exists, find the delta between order - previous.
                            if (previousState != null)
                            {
                                deltaFilled = orderState.QuantityExecuted - previousState.QuantityExecuted;
                            }

                            //Set the state of the fill event:
                            if (orderState.RemainingQuantity > 0)
                            {
                                status = OrderStatus.PartiallyFilled;
                            }

                            // Generate the (Partial Fill) OrderEvent - 
                            var fillEvent = new OrderEvent(Convert.ToInt32((long) orderState.Id), orderState.Symbol, status, orderState.AverageFillPrice, Convert.ToInt32((decimal) deltaFilled), "Tradier Fill Event");

                            // Create (partial)fill Objects - 
                            _algorithm.Portfolio.ProcessFill(fillEvent);

                            try
                            {
                                // Fire Order Events
                                _algorithm.OnOrderEvent(fillEvent);
                            }
                            catch (Exception err)
                            {
                                _results.RuntimeError("Caught Error OnOrderEvent(): " + err.Message, err.StackTrace);
                            }
                        }

                        //Save the previous order information:
                        _previousOrders = orderDetails;
                    }
                }
                catch (Exception err)
                {
                    Log.Trace("TradierTransactionHandler.Run(): " + err.Message + " > > " + err.StackTrace );
                }
            }
            //Set flag thread ended.
            _isActive = false;
            Log.Trace("TradierTransactionHandler.Run(): Transaction Handler Thread Completed.");
        }


        /// <summary>
        /// Detect if this order will cross zero and needs to be split up.
        /// </summary>
        /// <param name="order">Order we're attempting to process</param>
        /// <param name="holding">Current holdings of this security</param>
        /// <returns>True when order will cross zero (e.g. long->short) and we need to split into two orders.</returns>
        private bool DetectZeroCrossing(Order order, SecurityHolding holding)
        {
            var holdings = holding.Quantity;
            //We're reducing position or flipping:
            if (holding.IsLong && order.Quantity < 0)
            {
                if ((holdings + order.Quantity) < 0)
                {
                    //We dont have enough holdings so will cross through zero:
                    return true;
                }
            }
            else if (holding.IsShort && order.Quantity > 0)
            { 
                if ((holdings + order.Quantity) > 0) 
                {
                    //Crossed zero: need to split into 2 orders:
                    return true;
                }
            }
            return false;
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
        /// Convert a QC Direction to Tradier Direction Enum
        /// </summary>
        /// <param name="holdingQuantity">Our current holdings quantity</param>
        /// <param name="order">Order we'd like to process</param>
        /// <returns>Tradier order direction for the new tradier order object</returns>
        private static TradierOrderDirection Direction(int holdingQuantity, Order order)
        {
            // Tradier has 4 types of orders for this: buy/sell/buy to close and sell short.
            // 2 of the types are specifically for opening, lets handle those first:
            if (holdingQuantity == 0)
            {
                //Open a position: Both open long and open short:
                switch (order.Direction)
                {
                    case OrderDirection.Buy:
                        return TradierOrderDirection.Buy;
                    case OrderDirection.Sell:
                        return TradierOrderDirection.SellShort;
                }
            }
            else if (holdingQuantity > 0)
            {
                switch (order.Direction)
                {
                    case OrderDirection.Buy:
                        //Increasing existing position:
                        return TradierOrderDirection.Buy;
                    case OrderDirection.Sell:
                        //Reducing existing position:
                        return TradierOrderDirection.Sell;
                }
            }
            else if (holdingQuantity < 0)
            {
                switch (order.Direction)
                {
                    case OrderDirection.Buy:
                        //Reducing existing short position:
                        return TradierOrderDirection.BuyToClose;
                    case OrderDirection.Sell:
                        //Increasing existing short position:
                        return TradierOrderDirection.SellShort;
                }
            }
            return TradierOrderDirection.None;
        }


        /// <summary>
        /// Convert the QuantConnect order type enum to a Tradier order type enum
        /// </summary>
        /// <param name="type">QuantConnect order type to convert.</param>
        /// <returns>Tradier OrderType enum</returns>
        private static TradierOrderType OrderType(OrderType type)
        {
            switch (type)
            { 
                case QuantConnect.Orders.OrderType.Market:
                    return TradierOrderType.Market;

                case QuantConnect.Orders.OrderType.Limit:
                    return TradierOrderType.Limit;

                case QuantConnect.Orders.OrderType.StopMarket:
                    return TradierOrderType.StopMarket;

                default:
                    return TradierOrderType.Market;
            }
        }

        /// <summary>
        /// Convert the QuantConnect order duration enum into a Tradier order duration enum
        /// </summary>
        /// <param name="duration">QuantConnect order duration enum</param>
        /// <returns>Tradier order duration enum</returns>
        private TradierOrderDuration OrderDuration(OrderDuration duration)
        {
            switch (duration)
            { 
                default:
                case QuantConnect.Orders.OrderDuration.GTC:
                    return TradierOrderDuration.GTC;
            }
        }

        /// <summary>
        /// Update and resubmit the order to the OrderQueue for processing.
        /// </summary>
        /// <param name="order">Order we'd like updated</param>
        /// <returns>True if successful, false if already cancelled or filled.</returns>
        public bool UpdateOrder(Order order)
        {
            //Filled or already cancelled, can't update:
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
            //Filled or already cancelled, can't recancel.
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
            _algorithm = algorithm;
        }

        /// <summary>
        /// Signal a end of thread request to stop montioring the transactions.
        /// </summary>
        public void Exit()
        {
            _exitTriggered = true;
        }
    } // End Live Cloud Transaction Handler Class:

} // End Namespace
