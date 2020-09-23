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
using QuantConnect.Orders.Fills;
using QuantConnect.Orders.Fees;
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
        public override bool IsConnected => true;

        /// <summary>
        /// Gets all open orders on the account
        /// </summary>
        /// <returns>The open orders returned from IB</returns>
        public override List<Order> GetOpenOrders()
        {
            return Algorithm.Transactions.GetOpenOrders().ToList();
        }

        /// <summary>
        /// Gets all holdings for the account
        /// </summary>
        /// <returns>The current holdings from the account</returns>
        public override List<Holding> GetAccountHoldings()
        {
            // grab everything from the portfolio with a non-zero absolute quantity
            return (from kvp in Algorithm.Portfolio.Securities.OrderBy(x => x.Value.Symbol)
                    where kvp.Value.Holdings.AbsoluteQuantity > 0
                    select new Holding(kvp.Value)).ToList();
        }

        /// <summary>
        /// Gets the current cash balance for each currency held in the brokerage account
        /// </summary>
        /// <returns>The current cash balance for each currency available for trading</returns>
        public override List<CashAmount> GetCashBalance()
        {
            return Algorithm.Portfolio.CashBook.Select(x => new CashAmount(x.Value.Amount, x.Value.Symbol)).ToList();
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

                var orderId = order.Id.ToStringInvariant();
                if (!order.BrokerId.Contains(orderId)) order.BrokerId.Add(orderId);

                // fire off the event that says this order has been submitted
                var submitted = new OrderEvent(order,
                        Algorithm.UtcTime,
                        OrderFee.Zero)
                    { Status = OrderStatus.Submitted };
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

            var orderId = order.Id.ToStringInvariant();
            if (!order.BrokerId.Contains(orderId)) order.BrokerId.Add(orderId);

            // fire off the event that says this order has been updated
            var updated = new OrderEvent(order,
                    Algorithm.UtcTime,
                    OrderFee.Zero)
            {
                Status = OrderStatus.UpdateSubmitted
            };
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

            var orderId = order.Id.ToStringInvariant();
            if (!order.BrokerId.Contains(orderId)) order.BrokerId.Add(order.Id.ToStringInvariant());

            // fire off the event that says this order has been canceled
            var canceled = new OrderEvent(order,
                    Algorithm.UtcTime,
                    OrderFee.Zero)
                { Status = OrderStatus.Canceled };
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
        public virtual void Scan()
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

                    var fills = new OrderEvent[0];

                    Security security;
                    if (!Algorithm.Securities.TryGetValue(order.Symbol, out security))
                    {
                        Log.Error("BacktestingBrokerage.Scan(): Unable to process order: " + order.Id + ". The security no longer exists.");
                        // invalidate the order in the algorithm before removing
                        OnOrderEvent(new OrderEvent(order,
                                Algorithm.UtcTime,
                                OrderFee.Zero)
                        {Status = OrderStatus.Invalid});
                        _pending.TryRemove(order.Id, out order);
                        continue;
                    }

                    if (order.Type == OrderType.MarketOnOpen)
                    {
                        // This is a performance improvement:
                        // Since MOO should never fill on the same bar or on stale data (see FillModel)
                        // the order can remain unfilled for multiple 'scans', so we want to avoid
                        // margin and portfolio calculations since they are expensive
                        var currentBar = security.GetLastData();
                        var localOrderTime = order.Time.ConvertFromUtc(security.Exchange.TimeZone);
                        if (currentBar == null || localOrderTime >= currentBar.EndTime)
                        {
                            stillNeedsScan = true;
                            continue;
                        }
                    }

                    // check if the time in force handler allows fills
                    if (order.TimeInForce.IsOrderExpired(security, order))
                    {
                        OnOrderEvent(new OrderEvent(order,
                            Algorithm.UtcTime,
                            OrderFee.Zero)
                        {
                            Status = OrderStatus.Canceled,
                            Message = "The order has expired."
                        });
                        _pending.TryRemove(order.Id, out order);
                        continue;
                    }

                    // check if we would actually be able to fill this
                    if (!Algorithm.BrokerageModel.CanExecuteOrder(security, order))
                    {
                        continue;
                    }

                    // verify sure we have enough cash to perform the fill
                    HasSufficientBuyingPowerForOrderResult hasSufficientBuyingPowerResult;
                    try
                    {
                        hasSufficientBuyingPowerResult = security.BuyingPowerModel.HasSufficientBuyingPowerForOrder(Algorithm.Portfolio, security, order);
                    }
                    catch (Exception err)
                    {
                        // if we threw an error just mark it as invalid and remove the order from our pending list
                        OnOrderEvent(new OrderEvent(order,
                                Algorithm.UtcTime,
                                OrderFee.Zero,
                                err.Message)
                            { Status = OrderStatus.Invalid });
                        Order pending;
                        _pending.TryRemove(order.Id, out pending);

                        Log.Error(err);
                        Algorithm.Error($"Order Error: id: {order.Id}, Error executing margin models: {err.Message}");
                        continue;
                    }

                    //Before we check this queued order make sure we have buying power:
                    if (hasSufficientBuyingPowerResult.IsSufficient)
                    {
                        //Model:
                        var model = security.FillModel;

                        //Based on the order type: refresh its model to get fill price and quantity
                        try
                        {
                            if (order.Type == OrderType.OptionExercise)
                            {
                                var option = (Option)security;
                                fills = option.OptionExerciseModel.OptionExercise(option, order as OptionExerciseOrder).ToArray();
                            }
                            else
                            {
                                var context = new FillModelParameters(
                                    security,
                                    order,
                                    Algorithm.SubscriptionManager.SubscriptionDataConfigService,
                                    Algorithm.Settings.StalePriceTimeSpan);
                                fills = new[] { model.Fill(context).OrderEvent };
                            }

                            // invoke fee models for completely filled order events
                            foreach (var fill in fills)
                            {
                                if (fill.Status == OrderStatus.Filled)
                                {
                                    // this check is provided for backwards compatibility of older user-defined fill models
                                    // that may be performing fee computation inside the fill model w/out invoking the fee model
                                    // TODO : This check can be removed in April, 2019 -- a 6-month window to upgrade (also, suspect small % of users, if any are impacted)
                                    if (fill.OrderFee.Value.Amount == 0m)
                                    {
                                        fill.OrderFee = security.FeeModel.GetOrderFee(
                                            new OrderFeeParameters(security,
                                                order));
                                    }
                                }
                            }
                        }
                        catch (Exception err)
                        {
                            Log.Error(err);
                            Algorithm.Error($"Order Error: id: {order.Id}, Transaction model failed to fill for order type: {order.Type} with error: {err.Message}");
                        }
                    }
                    else
                    {
                        // invalidate the order in the algorithm before removing
                        var message = $"Insufficient buying power to complete order (Value:{order.GetValue(security).SmartRounding()}), Reason: {hasSufficientBuyingPowerResult.Reason}.";
                        OnOrderEvent(new OrderEvent(order,
                                Algorithm.UtcTime,
                                OrderFee.Zero,
                                message)
                            { Status = OrderStatus.Invalid });
                        Order pending;
                        _pending.TryRemove(order.Id, out pending);

                        Algorithm.Error($"Order Error: id: {order.Id}, {message}");
                        continue;
                    }

                    foreach (var fill in fills)
                    {
                        // check if the fill should be emitted
                        if (!order.TimeInForce.IsFillValid(security, order, fill))
                        {
                            break;
                        }

                        // change in status or a new fill
                        if (order.Status != fill.Status || fill.FillQuantity != 0)
                        {
                            // we update the order status so we do not re process it if we re enter
                            // because of the call to OnOrderEvent.
                            // Note: this is done by the transaction handler but we have a clone of the order
                            order.Status = fill.Status;

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

                // if we didn't fill then we need to continue to scan or
                // if there are still pending orders
                _needsScan = stillNeedsScan || !_pending.IsEmpty;
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
            _pending[order.Id] = order;
        }
    }
}