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

namespace QuantConnect.Brokerages.Backtesting
{
    /// <summary>
    /// Represents a brokerage to be used during backtesting. This is intended to be only be used with the BacktestingTransactionHandler
    /// </summary>
    public class BacktestingBrokerage : Brokerage
    {
        // this is the algorithm under test
        private readonly IAlgorithm _algorithm;
        // this is the orders dictionary reference from the algorithm for convenence
        private readonly ConcurrentDictionary<int, Order> _orders;

        /// <summary>
        /// Creates a new BacktestingBrokerage for the specified algorithm
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        public BacktestingBrokerage(IAlgorithm algorithm)
            : base("Backtesting Brokerage")
        {
            _algorithm = algorithm;
            _orders = _algorithm.Transactions.Orders;
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
            _orders = _algorithm.Transactions.Orders;
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
            return (from order in _orders
                    where order.Value.Status != OrderStatus.Filled &&
                          order.Value.Status != OrderStatus.Canceled &&
                          order.Value.Status != OrderStatus.Invalid
                    orderby order.Value.Id
                    select order.Value).ToList();
        }

        /// <summary>
        /// Gets all holdings for the account
        /// </summary>
        /// <returns>The current holdings from the account</returns>
        public override List<Holding> GetAccountHoldings()
        {
            // grab everything from the portfolio with a non-zero absolute quantity
            return _algorithm.Portfolio.Values.Where(x => x.AbsoluteQuantity > 0).OrderBy(x => x.Symbol).Select(holding => new Holding(holding, holding.Type)).ToList();
        }

        /// <summary>
        /// Gets the current USD cash balance in the brokerage account
        /// </summary>
        /// <returns>The current USD cash balance available for trading</returns>
        public override decimal GetCashBalance()
        {
            return _algorithm.Portfolio.Cash;
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
                if (!order.BrokerId.Contains(order.Id)) order.BrokerId.Add(order.Id);
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
            if (order.Status == OrderStatus.Update)
            {
                if (!order.BrokerId.Contains(order.Id)) order.BrokerId.Add(order.Id);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Cancels the order with the specified ID
        /// </summary>
        /// <param name="order">The order to cancel</param>
        /// <returns>True if the request was made for the order to be canceled, false otherwise</returns>
        public override bool CancelOrder(Order order)
        {
            if (order.Status == OrderStatus.Canceled)
            {
                if (!order.BrokerId.Contains(order.Id)) order.BrokerId.Add(order.Id);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Scans all the outstanding orders and applies the algorithm model fills to generate the order events
        /// </summary>
        public void Scan()
        {
            //2. NOW ALL ORDERS IN ORDER DICTIONARY::> 
            //   Scan through Orders: Process fills. Trigger Events.
            //   Refresh the order model: look at the orders for ones - process every time.
            
            // find orders that still need to be processed, be sure to sort them by their id so we
            // fill them in the proper order
            var orders = (from order in _orders
                          where order.Value.Status != OrderStatus.Filled &&
                                order.Value.Status != OrderStatus.Canceled &&
                                order.Value.Status != OrderStatus.Invalid
                          orderby order.Value.Id ascending
                          select order.Value);

            //Now we have the orders; re-apply the order models to each order.
            foreach (var order in orders)
            {
                // verify sure we have enough cash to perform the fill
                var sufficientBuyingPower = _algorithm.Transactions.GetSufficientCapitalForOrder(_algorithm.Portfolio, order);

                var fill = new OrderEvent();
                fill.Symbol = order.Symbol;

                //Before we check this queued order make sure we have buying power:
                if (sufficientBuyingPower)
                {
                    //Model:
                    var model = _algorithm.Securities[order.Symbol].Model;

                    //Based on the order type: refresh its model to get fill price and quantity
                    try
                    {
                        switch (order.Type)
                        {
                            case OrderType.Limit:
                                fill = model.LimitFill(_algorithm.Securities[order.Symbol], order as LimitOrder);
                                break;
                            case OrderType.StopMarket:
                                fill = model.StopMarketFill(_algorithm.Securities[order.Symbol], order as StopMarketOrder);
                                break;
                            case OrderType.Market:
                                fill = model.MarketFill(_algorithm.Securities[order.Symbol], order as MarketOrder);
                                break;
                            case OrderType.StopLimit:
                                fill = model.StopLimitFill(_algorithm.Securities[order.Symbol], order as StopLimitOrder);
                                break;
                        }
                    }
                    catch (Exception err)
                    {
                        Log.Error("BacktestingBrokerage.Scan(): " + err.Message);
                        _algorithm.Error(string.Format("Order Error: id: {0}, Transaction model failed to fill for order type: {1} with error: {2}", order.Id, order.Type, err.Message));
                    }
                }
                else
                {
                    //Flag order as invalid and push off queue:
                    order.Status = OrderStatus.Invalid;
                    _algorithm.Error(string.Format("Order Error: id: {0}, Insufficient buying power to complete order (Value:{1}).", order.Id, order.Value));
                }

                if (order.Status != OrderStatus.None)
                {
                    //If the fill models come back suggesting filled, process the affects on portfolio
                    OnOrderEvent(fill);
                }
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