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
using QuantConnect.Securities.Option;

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
        private readonly ConcurrentDictionary<int, Order> _pending;
        private readonly object _needsScanLock = new object();
        private readonly HashSet<Symbol> _pendingOptionAssignments = new HashSet<Symbol>();

        /// <summary>
        /// This is the algorithm under test
        /// </summary>
        protected readonly IAlgorithm Algorithm;

        /// <summary>
        /// Creates a new BacktestingBrokerage for the specified algorithm
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        public BacktestingBrokerage(IAlgorithm algorithm)
            : base("Backtesting Brokerage")
        {
            Algorithm = algorithm;
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
            Algorithm = algorithm;
            _pending = new ConcurrentDictionary<int, Order>();
        }

        /// <summary>
        /// Creates a new BacktestingBrokerage for the specified algorithm. Adds market simulation to BacktestingBrokerage;
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="marketSimulation">The backtesting market simulation instance</param>
        public BacktestingBrokerage(IAlgorithm algorithm, IBacktestingMarketSimulation marketSimulation)
            : base("Backtesting Brokerage")
        {
            Algorithm = algorithm;
            MarketSimulation = marketSimulation;
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
            return Algorithm.Transactions.GetOpenOrders();
        }

        /// <summary>
        /// Gets all holdings for the account
        /// </summary>
        /// <returns>The current holdings from the account</returns>
        public override List<Holding> GetAccountHoldings()
        {
            // grab everything from the portfolio with a non-zero absolute quantity
            return (from security in Algorithm.Portfolio.Securities.Values.OrderBy(x => x.Symbol)
                    where security.Holdings.AbsoluteQuantity > 0
                    select new Holding(security)).ToList();
        }

        /// <summary>
        /// Gets the current cash balance for each currency held in the brokerage account
        /// </summary>
        /// <returns>The current cash balance for each currency available for trading</returns>
        public override List<Cash> GetCashBalance()
        {
            return Algorithm.Portfolio.CashBook.Values.ToList();
        }

        /// <summary>
        /// Places a new order and assigns a new broker ID to the order
        /// </summary>
        /// <param name="order">The order to be placed</param>
        /// <returns>True if the request for a new order has been placed, false otherwise</returns>
        public override bool PlaceOrder(Order order)
        {
            if (Algorithm.LiveMode)
            {
                Log.Trace("BacktestingBrokerage.PlaceOrder(): Type: " + order.Type + " Symbol: " + order.Symbol.Value + " Quantity: " + order.Quantity);
            }

            if (order.Status == OrderStatus.New)
            {
                lock (_needsScanLock)
                {
                    _needsScan = true;
                    SetPendingOrder(order);
                }

                var orderId = order.Id.ToString();
                if (!order.BrokerId.Contains(orderId)) order.BrokerId.Add(orderId);

                // fire off the event that says this order has been submitted
                const int orderFee = 0;
                var submitted = new OrderEvent(order, Algorithm.UtcTime, orderFee) { Status = OrderStatus.Submitted };
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
            if (Algorithm.LiveMode)
            {
                Log.Trace("BacktestingBrokerage.UpdateOrder(): Symbol: " + order.Symbol.Value + " Quantity: " + order.Quantity + " Status: " + order.Status);
            }

            lock (_needsScanLock)
            {
                Order pending;
                if (!_pending.TryGetValue(order.Id, out pending))
                {
                    // can't update something that isn't there
                    return false;
                }

                _needsScan = true;
                SetPendingOrder(order);
            }

            var orderId = order.Id.ToString();
            if (!order.BrokerId.Contains(orderId)) order.BrokerId.Add(orderId);

            // fire off the event that says this order has been updated
            const int orderFee = 0;
            var updated = new OrderEvent(order, Algorithm.UtcTime, orderFee) { Status = OrderStatus.Submitted };
            OnOrderEvent(updated);

            return true;
        }

        /// <summary>
        /// Cancels the order with the specified ID
        /// </summary>
        /// <param name="order">The order to cancel</param>
        /// <returns>True if the request was made for the order to be canceled, false otherwise</returns>
        public override bool CancelOrder(Order order)
        {
            if (Algorithm.LiveMode)
            {
                Log.Trace("BacktestingBrokerage.CancelOrder(): Symbol: " + order.Symbol.Value + " Quantity: " + order.Quantity);
            }

            lock (_needsScanLock)
            {
                Order pending;
                if (!_pending.TryRemove(order.Id, out pending))
                {
                    // can't cancel something that isn't there
                    return false;
                }
            }

            var orderId = order.Id.ToString();
            if (!order.BrokerId.Contains(orderId)) order.BrokerId.Add(order.Id.ToString());

            // fire off the event that says this order has been canceled
            const int orderFee = 0;
            var canceled = new OrderEvent(order, Algorithm.UtcTime, orderFee) { Status = OrderStatus.Canceled };
            OnOrderEvent(canceled);

            return true;
        }

        /// <summary>
        /// Market Simulation - simulates various market conditions in backtest
        /// </summary>
        public IBacktestingMarketSimulation MarketSimulation { get; set; }

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

                var stillNeedsScan = false;

                // process each pending order to produce fills/fire events
                foreach (var kvp in _pending.OrderBy(x => x.Key))
                {
                    var order = kvp.Value;
                    if (order == null)
                    {
                        Log.Error("BacktestingBrokerage.Scan(): Null pending order found: " + kvp.Key);
                        _pending.TryRemove(kvp.Key, out order);
                        continue;
                    }

                    if (order.Status.IsClosed())
                    {
                        // this should never actually happen as we always remove closed orders as they happen
                        _pending.TryRemove(order.Id, out order);
                        continue;
                    }

                    // all order fills are processed on the next bar (except for market orders)
                    if (order.Time == Algorithm.UtcTime && order.Type != OrderType.Market)
                    {
                        stillNeedsScan = true;
                        continue;
                    }

                    var fills = new[] { new OrderEvent(order, Algorithm.UtcTime, 0) };

                    Security security;
                    if (!Algorithm.Securities.TryGetValue(order.Symbol, out security))
                    {
                        Log.Error("BacktestingBrokerage.Scan(): Unable to process order: " + order.Id + ". The security no longer exists.");
                        // invalidate the order in the algorithm before removing
                        OnOrderEvent(new OrderEvent(order, Algorithm.UtcTime, 0m){Status = OrderStatus.Invalid});
                        _pending.TryRemove(order.Id, out order);
                        continue;
                    }

                    // check if we would actually be able to fill this
                    if (!Algorithm.BrokerageModel.CanExecuteOrder(security, order))
                    {
                        continue;
                    }

                    // verify sure we have enough cash to perform the fill
                    bool sufficientBuyingPower;
                    try
                    {
                        sufficientBuyingPower = Algorithm.Transactions.GetSufficientCapitalForOrder(Algorithm.Portfolio, order);
                    }
                    catch (Exception err)
                    {
                        // if we threw an error just mark it as invalid and remove the order from our pending list
                        Order pending;
                        _pending.TryRemove(order.Id, out pending);
                        order.Status = OrderStatus.Invalid;
                        OnOrderEvent(new OrderEvent(order, Algorithm.UtcTime, 0, "Error in GetSufficientCapitalForOrder"));

                        Log.Error(err);
                        Algorithm.Error(string.Format("Order Error: id: {0}, Error executing margin models: {1}", order.Id, err.Message));
                        continue;
                    }

                    //Before we check this queued order make sure we have buying power:
                    if (sufficientBuyingPower)
                    {
                        //Model:
                        var model = security.FillModel;

                        //Based on the order type: refresh its model to get fill price and quantity
                        try
                        {
                            switch (order.Type)
                            {
                                case OrderType.Limit:
                                    fills = new[] { model.LimitFill(security, order as LimitOrder) };
                                    break;

                                case OrderType.StopMarket:
                                    fills = new[] { model.StopMarketFill(security, order as StopMarketOrder) };
                                    break;

                                case OrderType.Market:
                                    fills = new[] { model.MarketFill(security, order as MarketOrder) };
                                    break;

                                case OrderType.StopLimit:
                                    fills = new[] { model.StopLimitFill(security, order as StopLimitOrder) };
                                    break;

                                case OrderType.MarketOnOpen:
                                    fills = new[] { model.MarketOnOpenFill(security, order as MarketOnOpenOrder) };
                                    break;

                                case OrderType.MarketOnClose:
                                    fills = new[] { model.MarketOnCloseFill(security, order as MarketOnCloseOrder) };
                                    break;

                                case OrderType.OptionExercise:
                                    var option = (Option)security;
                                    fills = option.OptionExerciseModel.OptionExercise(option, order as OptionExerciseOrder).ToArray();
                                    break;
                            }
                        }
                        catch (Exception err)
                        {
                            Log.Error(err);
                            Algorithm.Error(string.Format("Order Error: id: {0}, Transaction model failed to fill for order type: {1} with error: {2}",
                                order.Id, order.Type, err.Message));
                        }
                    }
                    else
                    {
                        //Flag order as invalid and push off queue:
                        order.Status = OrderStatus.Invalid;
                        Algorithm.Error(string.Format("Order Error: id: {0}, Insufficient buying power to complete order (Value:{1}).", order.Id,
                            order.GetValue(security).SmartRounding()));
                    }

                    foreach (var fill in fills)
                    {
                        // change in status or a new fill
                        if (order.Status != fill.Status || fill.FillQuantity != 0)
                        {
                            //If the fill models come back suggesting filled, process the affects on portfolio
                            OnOrderEvent(fill);
                        }

                        if (order.Type == OrderType.OptionExercise)
                        {
                            fill.Message = order.Tag;
                            OnOptionPositionAssigned(fill);
                        }
                    }

                    if (fills.All(x => x.Status.IsClosed()))
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
        /// Runs market simulation
        /// </summary>
        public void SimulateMarket()
        {
            // if simulator is installed, we run it
            MarketSimulation?.SimulateMarketConditions(this, Algorithm);
        }

        /// <summary>
        /// This method is called by market simulator in order to launch an assignment event
        /// </summary>
        /// <param name="option">Option security to assign</param>
        /// <param name="quantity">Quantity to assign</param>
        public virtual void ActivateOptionAssignment(Option option, int quantity)
        {
            // do not process the same assignment more than once
            if (_pendingOptionAssignments.Contains(option.Symbol)) return;

            _pendingOptionAssignments.Add(option.Symbol);

            var request = new SubmitOrderRequest(OrderType.OptionExercise, option.Type, option.Symbol, -quantity, 0m, 0m, Algorithm.UtcTime, "Simulated option assignment before expiration");

            var ticket = Algorithm.Transactions.ProcessRequest(request);
            Log.Trace($"BacktestingBrokerage.ActivateOptionAssignment(): OrderId: {ticket.OrderId}");
        }

        /// <summary>
        /// Event invocator for the OrderFilled event
        /// </summary>
        /// <param name="e">The OrderEvent</param>
        protected override void OnOrderEvent(OrderEvent e)
        {
            if (e.Status.IsClosed() && _pendingOptionAssignments.Contains(e.Symbol))
            {
                _pendingOptionAssignments.Remove(e.Symbol);
            }

            base.OnOrderEvent(e);
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

        /// <summary>
        /// Sets the pending order as a clone to prevent object reference nastiness
        /// </summary>
        /// <param name="order">The order to be added to the pending orders dictionary</param>
        /// <returns></returns>
        private void SetPendingOrder(Order order)
        {
            // only save off clones!
            _pending[order.Id] = order.Clone();
        }
    }
}