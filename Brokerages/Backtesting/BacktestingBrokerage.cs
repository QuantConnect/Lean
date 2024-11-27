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
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Orders.Fills;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;
using QuantConnect.Securities.Option;
using QuantConnect.Util;

namespace QuantConnect.Brokerages.Backtesting
{
    /// <summary>
    /// Represents a brokerage to be used during backtesting. This is intended to be only be used with the BacktestingTransactionHandler
    /// </summary>
    [BrokerageFactory(typeof(BacktestingBrokerageFactory))]
    public class BacktestingBrokerage : Brokerage
    {
        // flag used to indicate whether or not we need to scan for
        // fills, this is purely a performance concern is ConcurrentDictionary.IsEmpty
        // is not exactly the fastest operation and Scan gets called at least twice per
        // time loop
        private bool _needsScan;
        private DateTime _nextOptionAssignmentTime;
        private readonly ConcurrentDictionary<int, Order> _pending;
        private readonly object _needsScanLock = new object();
        private readonly HashSet<Symbol> _pendingOptionAssignments = new HashSet<Symbol>();

        /// <summary>
        /// This is the algorithm under test
        /// </summary>
        protected IAlgorithm Algorithm { get; init; }

        /// <summary>
        /// Creates a new BacktestingBrokerage for the specified algorithm
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        public BacktestingBrokerage(IAlgorithm algorithm)
            : this(algorithm, "Backtesting Brokerage")
        {
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

                AddBrokerageOrderId(order);

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

            AddBrokerageOrderId(order);

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

            if (!order.TryGetGroupOrders(TryGetOrder, out var orders))
            {
                return false;
            }

            var result = true;
            foreach (var orderInGroup in orders)
            {
                lock (_needsScanLock)
                {
                    if (!_pending.TryRemove(orderInGroup.Id, out var _))
                    {
                        // can't cancel something that isn't there,
                        // let's continue just in case some other order of the group has to be cancelled
                        result = false;
                    }
                }

                AddBrokerageOrderId(orderInGroup);

                // fire off the event that says this order has been canceled
                var canceled = new OrderEvent(orderInGroup,
                        Algorithm.UtcTime,
                        OrderFee.Zero)
                { Status = OrderStatus.Canceled };
                OnOrderEvent(canceled);
            }

            return result;
        }

        /// <summary>
        /// Scans all the outstanding orders and applies the algorithm model fills to generate the order events
        /// </summary>
        public virtual void Scan()
        {
            ProcessAssignmentOrders();

            lock (_needsScanLock)
            {
                // there's usually nothing in here
                if (!_needsScan)
                {
                    return;
                }

                var stillNeedsScan = false;

                // process each pending order to produce fills/fire events
                foreach (var kvp in _pending.OrderBySafe(x => x.Key))
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
                        _pending.TryRemove(order.Id, out var _);
                        continue;
                    }

                    // all order fills are processed on the next bar (except for market orders)
                    if (order.Time == Algorithm.UtcTime && order.Type != OrderType.Market && order.Type != OrderType.ComboMarket && order.Type != OrderType.OptionExercise)
                    {
                        stillNeedsScan = true;
                        continue;
                    }

                    if (!order.TryGetGroupOrders(TryGetOrder, out var orders))
                    {
                        // an Order of the group is missing
                        stillNeedsScan = true;
                        continue;
                    }

                    if(!orders.TryGetGroupOrdersSecurities(Algorithm.Portfolio, out var securities))
                    {
                        Log.Error($"BacktestingBrokerage.Scan(): Unable to process orders: [{string.Join(",", orders.Select(o => o.Id))}] The security no longer exists. UtcTime: {Algorithm.UtcTime}");
                        // invalidate the order in the algorithm before removing
                        RemoveOrders(orders, OrderStatus.Invalid);
                        continue;
                    }

                    if (!TryOrderPreChecks(securities, out stillNeedsScan))
                    {
                        continue;
                    }

                    // verify sure we have enough cash to perform the fill
                    HasSufficientBuyingPowerForOrderResult hasSufficientBuyingPowerResult;
                    try
                    {
                        hasSufficientBuyingPowerResult = Algorithm.Portfolio.HasSufficientBuyingPowerForOrder(orders);
                    }
                    catch (Exception err)
                    {
                        // if we threw an error just mark it as invalid and remove the order from our pending list
                        RemoveOrders(orders, OrderStatus.Invalid, err.Message);

                        Log.Error(err);
                        Algorithm.Error($"Order Error: ids: [{string.Join(",", orders.Select(o => o.Id))}], Error executing margin models: {err.Message}");
                        continue;
                    }

                    var fills = new List<OrderEvent>();
                    //Before we check this queued order make sure we have buying power:
                    if (hasSufficientBuyingPowerResult.IsSufficient)
                    {
                        //Model:
                        var security = securities[order];
                        var model = security.FillModel;

                        //Based on the order type: refresh its model to get fill price and quantity
                        try
                        {
                            if (order.Type == OrderType.OptionExercise)
                            {
                                var option = (Option)security;
                                fills.AddRange(option.OptionExerciseModel.OptionExercise(option, order as OptionExerciseOrder));
                            }
                            else
                            {
                                var context = new FillModelParameters(
                                    security,
                                    order,
                                    Algorithm.SubscriptionManager.SubscriptionDataConfigService,
                                    Algorithm.Settings.StalePriceTimeSpan,
                                    securities,
                                    OnOrderUpdated);

                                // check if the fill should be emitted
                                var fill = model.Fill(context);
                                if (fill.All(x => order.TimeInForce.IsFillValid(security, order, x)))
                                {
                                    fills.AddRange(fill);
                                }
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
                                        // It could be the case the order is a combo order, then it contains legs with different quantities and security types.
                                        // Therefore, we need to compute the fees based on the specific leg order and security
                                        var legKVP = securities.Where(x => x.Key.Id == fill.OrderId).Single();
                                        fill.OrderFee = legKVP.Value.FeeModel.GetOrderFee(new OrderFeeParameters(legKVP.Value, legKVP.Key));
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
                    else if (order.Status == OrderStatus.CancelPending)
                    {
                        // the pending CancelOrderRequest will be handled during the next transaction handler run
                        continue;
                    }
                    else
                    {
                        // invalidate the order in the algorithm before removing
                        var message = securities.GetErrorMessage(hasSufficientBuyingPowerResult);
                        RemoveOrders(orders, OrderStatus.Invalid, message);

                        Algorithm.Error(message);
                        continue;
                    }

                    if (fills.Count == 0)
                    {
                        continue;
                    }

                    List<OrderEvent> fillEvents = new(orders.Count);
                    List<Tuple<Order, OrderEvent>> positionAssignments = new(orders.Count);
                    foreach (var targetOrder in orders)
                    {
                        var orderFills = fills.Where(f => f.OrderId == targetOrder.Id);
                        foreach (var fill in orderFills)
                        {
                            // change in status or a new fill
                            if (targetOrder.Status != fill.Status || fill.FillQuantity != 0)
                            {
                                // we update the order status so we do not re process it if we re enter
                                // because of the call to OnOrderEvent.
                                // Note: this is done by the transaction handler but we have a clone of the order
                                targetOrder.Status = fill.Status;
                                fillEvents.Add(fill);
                            }

                            if (fill.IsAssignment)
                            {
                                positionAssignments.Add(Tuple.Create(targetOrder, fill));
                            }
                        }
                    }

                    OnOrderEvents(fillEvents);
                    foreach (var assignment in positionAssignments)
                    {
                        assignment.Item2.Message = assignment.Item1.Tag;
                        OnOptionPositionAssigned(assignment.Item2);
                    }

                    if (fills.All(x => x.Status.IsClosed()))
                    {
                        foreach (var o in orders)
                        {
                            _pending.TryRemove(o.Id, out var _);
                        }
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
        /// Invokes the <see cref="Brokerage.OnOrderUpdated(OrderUpdateEvent)" /> event with the given order updates.
        /// </summary>
        private void OnOrderUpdated(Order order)
        {
            switch (order.Type)
            {
                case OrderType.TrailingStop:
                    OnOrderUpdated(new OrderUpdateEvent { OrderId = order.Id, TrailingStopPrice = ((TrailingStopOrder)order).StopPrice });
                    break;

                case OrderType.StopLimit:
                    OnOrderUpdated(new OrderUpdateEvent { OrderId = order.Id, StopTriggered = ((StopLimitOrder)order).StopTriggered });
                    break;
            }
        }

        /// <summary>
        /// Helper method to drive option assignment models
        /// </summary>
        private void ProcessAssignmentOrders()
        {
            if (Algorithm.UtcTime >= _nextOptionAssignmentTime)
            {
                _nextOptionAssignmentTime = Algorithm.UtcTime.RoundDown(Time.OneHour) + Time.OneHour;

                foreach (var security in Algorithm.Securities.Values
                    .Where(security => security.Symbol.SecurityType.IsOption() && security.Holdings.IsShort)
                    .OrderBy(security => security.Symbol.ID.Symbol))
                {
                    var option = (Option)security;
                    var result = option.OptionAssignmentModel.GetAssignment(new OptionAssignmentParameters(option));
                    if (result != null && result.Quantity != 0)
                    {
                        if (!_pendingOptionAssignments.Add(option.Symbol))
                        {
                            throw new InvalidOperationException($"Duplicate option exercise order request for symbol {option.Symbol}. Please contact support");
                        }

                        OnOptionNotification(new OptionNotificationEventArgs(option.Symbol, 0, result.Tag));
                    }
                }
            }
        }

        /// <summary>
        /// Event invocator for the OrderFilled event
        /// </summary>
        /// <param name="orderEvents">The list of order events</param>
        protected override void OnOrderEvents(List<OrderEvent> orderEvents)
        {
            for (int i = 0; i < orderEvents.Count; i++)
            {
                _pendingOptionAssignments.Remove(orderEvents[i].Symbol);
            }
            base.OnOrderEvents(orderEvents);
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

        /// <summary>
        /// Process delistings
        /// </summary>
        /// <param name="delistings">Delistings to process</param>
        public void ProcessDelistings(Delistings delistings)
        {
            // Process our delistings, important to do options first because of possibility of having future options contracts
            // and underlying future delisting at the same time.
            foreach (var delisting in delistings?.Values.OrderBy(x => !x.Symbol.SecurityType.IsOption()))
            {
                Log.Debug($"BacktestingBrokerage.ProcessDelistings(): Delisting {delisting.Type}: {delisting.Symbol.Value}, UtcTime: {Algorithm.UtcTime}, DelistingTime: {delisting.Time}");
                if (delisting.Type == DelistingType.Warning)
                {
                    // We do nothing with warnings
                    continue;
                }

                var security = Algorithm.Securities[delisting.Symbol];

                if (security.Symbol.SecurityType.IsOption())
                {
                    // Process the option delisting
                    OnOptionNotification(new OptionNotificationEventArgs(delisting.Symbol, 0));
                }
                else
                {
                    // Any other type of delisting
                    OnDelistingNotification(new DelistingNotificationEventArgs(delisting.Symbol));
                }

                // the subscription are getting removed from the data feed because they end
                // remove security from all universes
                foreach (var ukvp in Algorithm.UniverseManager)
                {
                    var universe = ukvp.Value;
                    if (universe.ContainsMember(security.Symbol))
                    {
                        var userUniverse = universe as UserDefinedUniverse;
                        if (userUniverse != null)
                        {
                            userUniverse.Remove(security.Symbol);
                        }
                        else
                        {
                            universe.RemoveMember(Algorithm.UtcTime, security);
                        }
                    }
                }

                if (!Algorithm.IsWarmingUp)
                {
                    // Cancel any other orders
                    var cancelledOrders = Algorithm.Transactions.CancelOpenOrders(delisting.Symbol);
                    foreach (var cancelledOrder in cancelledOrders)
                    {
                        Log.Trace("AlgorithmManager.Run(): " + cancelledOrder);
                    }
                }
            }
        }

        private void RemoveOrders(List<Order> orders, OrderStatus orderStatus, string message = "")
        {
            var orderEvents = new List<OrderEvent>(orders.Count);
            for (var i = 0; i < orders.Count; i++)
            {
                var order = orders[i];
                orderEvents.Add(new OrderEvent(order, Algorithm.UtcTime, OrderFee.Zero, message) { Status = orderStatus });
                _pending.TryRemove(order.Id, out var _);
            }

            OnOrderEvents(orderEvents);
        }

        private bool TryOrderPreChecks(Dictionary<Order, Security> ordersSecurities, out bool stillNeedsScan)
        {
            var result = true;
            stillNeedsScan = false;

            var removedOrdersIds = new HashSet<int>();

            foreach (var kvp in ordersSecurities)
            {
                var order = kvp.Key;
                var security = kvp.Value;

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
                        result = false;
                        break;
                    }
                }

                // check if the time in force handler allows fills
                if (order.TimeInForce.IsOrderExpired(security, order))
                {
                    // We remove all orders in the combo
                    RemoveOrders(ordersSecurities.Select(kvp => kvp.Key).ToList(), OrderStatus.Canceled, "The order has expired.");
                    result = false;
                    break;
                }

                // check if we would actually be able to fill this
                if (!Algorithm.BrokerageModel.CanExecuteOrder(security, order))
                {
                    result = false;
                    break;
                }
            }

            return result;
        }

        private Order TryGetOrder(int orderId)
        {
            _pending.TryGetValue(orderId, out var order);
            return order;
        }

        private static void AddBrokerageOrderId(Order order)
        {
            var orderId = order.Id.ToStringInvariant();
            if (!order.BrokerId.Contains(orderId))
            {
                order.BrokerId.Add(orderId);
            }
        }
    }
}
