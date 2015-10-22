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
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Securities;

namespace QuantConnect.Brokerages.Backtesting
{
    /// <summary>
    /// Represents a brokerage to be used during backtesting. This is intended to be only be used with the BacktestingTransactionHandler
    /// </summary>
    public class BacktestingBrokerage : Brokerage
    {
        // flag used to indicate whether or not we need to scan for
        // fills, this is purely a performance concern is ConcurrentDictionary.IsEmpty
        // is not exactly the fastest operation and Scan gets called at least twice per
        // time loop
        private bool _needsScan;
        // this is the algorithm under test
        private readonly IAlgorithm _algorithm;
        private readonly ConcurrentDictionary<int, Order> _pending;
        private readonly object _needsScanLock = new object();

        /// <summary>
        /// Creates a new BacktestingBrokerage for the specified algorithm
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        public BacktestingBrokerage(IAlgorithm algorithm)
            : base("Backtesting Brokerage")
        {
            _algorithm = algorithm;
            _pending = new ConcurrentDictionary<int, Order>();
        }

        /// <summary>
        /// Creates a new BacktestingBrokerage for the specified algorithm
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="name">The name of the brokerage</param>
        protected BacktestingBrokerage(IAlgorithm algorithm, string name)
            : base(name)
        {
            _algorithm = algorithm;
            _pending = new ConcurrentDictionary<int, Order>();
        }

        /// <summary>
        /// Gets the connection status
        /// </summary>
        /// <remarks>
        /// The BacktestingBrokerage is always connected
        /// </remarks>
        public override bool IsConnected
        {
            get { return true; }
        }

        /// <summary>
        /// Gets all open orders on the account
        /// </summary>
        /// <returns>The open orders returned from IB</returns>
        public override List<Order> GetOpenOrders()
        {
            return _algorithm.Transactions.GetOpenOrders();
        }

        /// <summary>
        /// Gets all holdings for the account
        /// </summary>
        /// <returns>The current holdings from the account</returns>
        public override List<Holding> GetAccountHoldings()
        {
            // grab everything from the portfolio with a non-zero absolute quantity
            return _algorithm.Portfolio.Values.Where(x => x.AbsoluteQuantity > 0).OrderBy(x => x.Symbol).Select(holding => new Holding(holding)).ToList();
        }

        /// <summary>
        /// Gets the current cash balance for each currency held in the brokerage account
        /// </summary>
        /// <returns>The current cash balance for each currency available for trading</returns>
        public override List<Cash> GetCashBalance()
        {
            return _algorithm.Portfolio.CashBook.Values.ToList();
        }

        /// <summary>
        /// Places a new order and assigns a new broker ID to the order
        /// </summary>
        /// <param name="order">The order to be placed</param>
        /// <returns>True if the request for a new order has been placed, false otherwise</returns>
        public override bool PlaceOrder(Order order)
        {
            if (order.Status == OrderStatus.New)
            {
                lock (_needsScanLock)
                {
                    _needsScan = true;
                    _pending[order.Id] = order;
                }

                if (!order.BrokerId.Contains(order.Id)) order.BrokerId.Add(order.Id);

                // fire off the event that says this order has been submitted
                const int orderFee = 0;
                var submitted = new OrderEvent(order, _algorithm.UtcTime, orderFee) { Status = OrderStatus.Submitted };
                OnOrderEvent(submitted);

                return true;
            }
            return false;
        }

        /// <summary>
        /// Updates the order with the same ID
        /// </summary>
        /// <param name="order">The new order information</param>
        /// <returns>True if the request was made for the order to be updated, false otherwise</returns>
        public override bool UpdateOrder(Order order)
        {
            if (true)
            {
                Order pending;
                if (!_pending.TryGetValue(order.Id, out pending))
                {
                    // can't update something that isn't there
                    return false;
                }

                lock (_needsScanLock)
                {
                    _needsScan = true;
                    _pending[order.Id] = order;
                }

                if (!order.BrokerId.Contains(order.Id)) order.BrokerId.Add(order.Id);

                // fire off the event that says this order has been updated
                const int orderFee = 0;
                var updated = new OrderEvent(order, _algorithm.UtcTime, orderFee) { Status = OrderStatus.Submitted };
                OnOrderEvent(updated);

                return true;
            }
        }

        /// <summary>
        /// Cancels the order with the specified ID
        /// </summary>
        /// <param name="order">The order to cancel</param>
        /// <returns>True if the request was made for the order to be canceled, false otherwise</returns>
        public override bool CancelOrder(Order order)
        {
            Order pending;
            if (!_pending.TryRemove(order.Id, out pending))
            {
                // can't cancel something that isn't there
                return false;
            }

            if (!order.BrokerId.Contains(order.Id)) order.BrokerId.Add(order.Id);

            // fire off the event that says this order has been canceled
            const int orderFee = 0;
            var canceled = new OrderEvent(order, _algorithm.UtcTime, orderFee) { Status = OrderStatus.Canceled };
            OnOrderEvent(canceled);

            return true;
        }

        /// <summary>
        /// Scans all the outstanding orders and applies the algorithm model fills to generate the order events
        /// </summary>
        public void Scan()
        {
            lock (_needsScanLock)
            {
                // there's usually nothing in here
                if (!_needsScan)
                {
                    return;
                }

                //2. NOW ALL ORDERS IN ORDER DICTIONARY::> 
                //   Scan through Orders: Process fills. Trigger Events.
                //   Refresh the order model: look at the orders for ones - process every time.

                // find orders that still need to be processed, be sure to sort them by their id so we
                // fill them in the proper order
                var orders = (from order in _pending
                              where order.Value.Status != OrderStatus.Filled &&
                                    order.Value.Status != OrderStatus.Canceled &&
                                    order.Value.Status != OrderStatus.Invalid
                              orderby order.Value.Id ascending
                              select order);

                var stillNeedsScan = false;

                //Now we have the orders; re-apply the order models to each order.
                foreach (var kvp in orders)
                {
                    var order = kvp.Value;

                    Security security;
                    if (!_algorithm.Securities.TryGetValue(order.Symbol, out security))
                    {
                        Log.Error("BacktestingBrokerage.Scan(): Unable to process order: " + order.Id + ". The security no longer exists.");
                        // invalidate the order in the algorithm before removing
                        OnOrderEvent(new OrderEvent(order, _algorithm.UtcTime, 0m){Status = OrderStatus.Invalid});
                        _pending.TryRemove(order.Id, out order);
                        continue;
                    }

                    // check if we would actually be able to fill this
                    if (!_algorithm.BrokerageModel.CanExecuteOrder(security, order))
                    {
                        continue;
                    }

                    var orderFee = security.TransactionModel.GetOrderFee(security, order);
                    var fill = new OrderEvent(order, _algorithm.UtcTime, orderFee);

                    // verify sure we have enough cash to perform the fill
                    bool sufficientBuyingPower;
                    try
                    {
                        sufficientBuyingPower = _algorithm.Transactions.GetSufficientCapitalForOrder(_algorithm.Portfolio, order);
                    }
                    catch (Exception err)
                    {
                        // if we threw an error just mark it as invalid and remove the order from our pending list
                        Order pending;
                        _pending.TryRemove(order.Id, out pending);
                        order.Status = OrderStatus.Invalid;
                        OnOrderEvent(new OrderEvent(order, _algorithm.UtcTime, orderFee, "Error in GetSufficientCapitalForOrder"));

                        Log.Error(err);
                        _algorithm.Error(string.Format("Order Error: id: {0}, Error executing margin models: {1}", order.Id, err.Message));
                        continue;
                    }

                    //Before we check this queued order make sure we have buying power:
                    if (sufficientBuyingPower)
                    {
                        //Model:
                        var model = security.TransactionModel;

                        //Based on the order type: refresh its model to get fill price and quantity
                        try
                        {
                            switch (order.Type)
                            {
                                case OrderType.Limit:
                                    fill = model.LimitFill(security, order as LimitOrder);
                                    break;

                                case OrderType.StopMarket:
                                    fill = model.StopMarketFill(security, order as StopMarketOrder);
                                    break;

                                case OrderType.Market:
                                    fill = model.MarketFill(security, order as MarketOrder);
                                    break;

                                case OrderType.StopLimit:
                                    fill = model.StopLimitFill(security, order as StopLimitOrder);
                                    break;

                                case OrderType.MarketOnOpen:
                                    fill = model.MarketOnOpenFill(security, order as MarketOnOpenOrder);
                                    break;

                                case OrderType.MarketOnClose:
                                    fill = model.MarketOnCloseFill(security, order as MarketOnCloseOrder);
                                    break;
                            }
                        }
                        catch (Exception err)
                        {
                            Log.Error("BacktestingBrokerage.Scan(): " + err.Message);
                            _algorithm.Error(string.Format("Order Error: id: {0}, Transaction model failed to fill for order type: {1} with error: {2}",
                                order.Id, order.Type, err.Message));
                        }
                    }
                    else
                    {
                        //Flag order as invalid and push off queue:
                        order.Status = OrderStatus.Invalid;
                        _algorithm.Error(string.Format("Order Error: id: {0}, Insufficient buying power to complete order (Value:{1}).", order.Id,
                            order.GetValue(security.Price).SmartRounding()));
                    }

                    // change in status or a new fill
                    if (order.Status != fill.Status || fill.FillQuantity != 0)
                    {
                        //If the fill models come back suggesting filled, process the affects on portfolio
                        OnOrderEvent(fill);
                    }

                    if (order.Status == OrderStatus.Filled || order.Status == OrderStatus.Invalid || order.Status == OrderStatus.Canceled)
                    {
                        _pending.TryRemove(order.Id, out order);
                    }
                    else
                    {
                        stillNeedsScan = true;
                    }
                }
                
                // if we didn't fill then we need to continue to scan
                _needsScan = stillNeedsScan;
            }
        }

        /// <summary>
        /// The BacktestingBrokerage is always connected. This is a no-op.
        /// </summary>
        public override void Connect()
        {
            //NOP
        }

        /// <summary>
        /// The BacktestingBrokerage is always connected. This is a no-op.
        /// </summary>
        public override void Disconnect()
        {
            //NOP
        }
    }
}