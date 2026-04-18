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
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Util;

namespace QuantConnect.Tests.Brokerages
{
    public abstract class BrokerageTests
    {
        // ideally this class would be abstract, but I wanted to keep the order test cases here which use the
        // various parameters required from derived types

        private IBrokerage _brokerage;
        private OrderProvider _orderProvider;
        private SecurityProvider _securityProvider;

        protected ManualResetEvent OrderFillEvent { get; } = new ManualResetEvent(false);

        protected ManualResetEvent OrderCancelledResetEvent { get; } = new(false);

        #region Test initialization and cleanup

        [SetUp]
        public void Setup()
        {
            Log.LogHandler = new NUnitLogHandler();

            Log.Trace("");
            Log.Trace("");
            Log.Trace("--- SETUP ---");
            Log.Trace("");
            Log.Trace("");
            // we want to regenerate these for each test
            _brokerage = null;
            _orderProvider = null;
            _securityProvider = null;
            Thread.Sleep(1000);
            CancelOpenOrders();
            LiquidateHoldings();
            Thread.Sleep(1000);
        }

        [TearDown]
        public void Teardown()
        {
            try
            {
                Log.Trace("");
                Log.Trace("");
                Log.Trace("--- TEARDOWN ---");
                Log.Trace("");
                Log.Trace("");
                Thread.Sleep(1000);
                CancelOpenOrders();
                LiquidateHoldings();
                Thread.Sleep(1000);
            }
            finally
            {
                if (_brokerage != null)
                {
                    DisposeBrokerage(_brokerage);
                }

                OrderFillEvent.Reset();
            }
        }

        public IBrokerage Brokerage
        {
            get
            {
                if (_brokerage == null)
                {
                    _brokerage = InitializeBrokerage();
                }
                return _brokerage;
            }
        }

        private IBrokerage InitializeBrokerage()
        {
            Log.Trace("");
            Log.Trace("- INITIALIZING BROKERAGE -");
            Log.Trace("");

            var brokerage = CreateBrokerage(OrderProvider, SecurityProvider);
            brokerage.Connect();

            if (!brokerage.IsConnected)
            {
                Assert.Fail("Failed to connect to brokerage");
            }

            Log.Trace("");
            Log.Trace("GET ACCOUNT HOLDINGS");
            Log.Trace("");
            var counter = 0;
            foreach (var accountHolding in brokerage.GetAccountHoldings())
            {
                // these securities don't need to be real, just used for the ISecurityProvider impl, required
                // by brokerages to track holdings
                var security = SecurityProvider.GetSecurity(accountHolding.Symbol);
                security.Holdings.SetHoldings(accountHolding.AveragePrice, accountHolding.Quantity);
                Log.Trace($"#{counter++}. {accountHolding}");
            }
            brokerage.OrdersStatusChanged += HandleEvents;
            brokerage.OrderIdChanged += HandleOrderIdChangedEvents;

            return brokerage;
        }

        /// <summary>
        /// Handles the event triggered when a brokerage order ID has changed.
        /// Logs the event and forwards it to the order provider for further processing.
        /// </summary>
        /// <param name="_">
        /// The sender of the event (unused).
        /// </param>
        /// <param name="brokerageOrderIdChangedEvent">
        /// The event data containing the updated order ID and brokerage IDs.
        /// </param>
        private void HandleOrderIdChangedEvents(object _, BrokerageOrderIdChangedEvent brokerageOrderIdChangedEvent)
        {
            Log.Trace("");
            Log.Trace($"ORDER ID CHANGED: {brokerageOrderIdChangedEvent}");
            Log.Trace("");

            OrderProvider.HandlerBrokerageOrderIdChangedEvent(brokerageOrderIdChangedEvent);
        }

        private void HandleEvents(object sender, List<OrderEvent> orderEvents)
        {
            foreach (var orderEvent in orderEvents)
            {
                var order = _orderProvider.GetOrderById(orderEvent.OrderId);
                order.Status = orderEvent.Status;

                Log.Trace("");
                Log.Trace($"ORDER STATUS CHANGED: {orderEvent}, Type: {order.Type}");
                Log.Trace("");

                switch (orderEvent.Status)
                {
                    case OrderStatus.Canceled:
                        SignalOrderStatusReached(order, OrderStatus.Canceled, OrderCancelledResetEvent);
                        break;

                }

                // we need to keep this maintained properly
                if (orderEvent.Status == OrderStatus.Filled || orderEvent.Status == OrderStatus.PartiallyFilled)
                {
                    Log.Trace("FILL EVENT: " + orderEvent.FillQuantity + " units of " + orderEvent.Symbol.ToString());

                    var eventFillPrice = orderEvent.FillPrice;
                    var eventFillQuantity = orderEvent.FillQuantity;

                    Assert.Greater(eventFillPrice, 0m);

                    switch (orderEvent.Direction)
                    {
                        case OrderDirection.Buy:
                            Assert.Greater(eventFillQuantity, 0m);
                            break;
                        case OrderDirection.Sell:
                            Assert.Less(eventFillQuantity, 0m);
                            break;
                        default:
                            throw new ArgumentException($"{nameof(BrokerageTests)}.{nameof(HandleEvents)}: Not Recognize order Event Direction = {orderEvent.Direction}");
                    }

                    var holding = SecurityProvider.GetSecurity(orderEvent.Symbol).Holdings;
                    holding.SetHoldings(eventFillPrice, holding.Quantity + eventFillQuantity);

                    Log.Trace("--HOLDINGS: " + _securityProvider[orderEvent.Symbol].Holdings);
                }

                if (orderEvent.Status == OrderStatus.Filled)
                {
                    SignalOrderStatusReached(order, OrderStatus.Filled, OrderFillEvent);
                }
            }
        }

        protected virtual BrokerageName BrokerageName { get; set; } = BrokerageName.Default;

        public OrderProvider OrderProvider
        {
            get { return _orderProvider ?? (_orderProvider = new OrderProvider()); }
        }

        public SecurityProvider SecurityProvider
        {
            get { return _securityProvider ?? (_securityProvider = new SecurityProvider(new(), BrokerageName, OrderProvider)); }
        }

        /// <summary>
        /// Creates the brokerage under test and connects it
        /// </summary>
        /// <returns>A connected brokerage instance</returns>
        protected abstract IBrokerage CreateBrokerage(IOrderProvider orderProvider, ISecurityProvider securityProvider);

        /// <summary>
        /// Disposes of the brokerage and any external resources started in order to create it
        /// </summary>
        /// <param name="brokerage">The brokerage instance to be disposed of</param>
        protected virtual void DisposeBrokerage(IBrokerage brokerage)
        {
            brokerage.OrdersStatusChanged -= HandleEvents;
            brokerage.OrderIdChanged -= HandleOrderIdChangedEvents;
            brokerage.Disconnect();
            brokerage.DisposeSafely();
        }

        /// <summary>
        /// This is used to ensure each test starts with a clean, known state.
        /// </summary>
        protected void LiquidateHoldings()
        {
            Log.Trace("");
            Log.Trace("LIQUIDATE HOLDINGS");
            Log.Trace("");

            var holdings = Brokerage.GetAccountHoldings();

            foreach (var holding in holdings)
            {
                if (holding.Quantity == 0) continue;
                Log.Trace("Liquidating: " + holding);
                var order = GetMarketOrder(holding.Symbol, -holding.Quantity);
                PlaceOrderWaitForStatus(order, OrderStatus.Filled);
            }
        }

        protected void CancelOpenOrders()
        {
            Log.Trace("");
            Log.Trace("CANCEL OPEN ORDERS");
            Log.Trace("");
            foreach (var openOrder in GetOpenOrders())
            {
                Log.Trace("Canceling: " + openOrder);
                Brokerage.CancelOrder(openOrder);
            }
        }

        private List<Order> GetOpenOrders()
        {
            Log.Trace("");
            Log.Trace("GET OPEN ORDERS");
            Log.Trace("");
            var orders = new List<Order>();
            foreach (var openOrder in Brokerage.GetOpenOrders())
            {
                var leanOrders = OrderProvider.GetOrdersByBrokerageId(openOrder.BrokerId.FirstOrDefault());
                // OrderType.Combo share the same BrokerId across LeanOrders
                if (leanOrders.Count == 0 || !leanOrders.Any(x => x.Symbol == openOrder.Symbol))
                {
                    OrderProvider.Add(openOrder);
                }
            }

            return OrderProvider.GetOpenOrders();
        }

        #endregion

        /// <summary>
        /// Gets the symbol to be traded, must be shortable
        /// </summary>
        protected abstract Symbol Symbol { get; }

        /// <summary>
        /// Gets the security type associated with the <see cref="Symbol"/>
        /// </summary>
        protected abstract SecurityType SecurityType { get; }

        /// <summary>
        /// Global order properties used to manage and properly liquidate positions.
        /// </summary>
        /// <remarks>
        /// This property is initialized before/after each brokerage test. It is used to
        /// ensure that any pre-existing positions are liquidated correctly and that
        /// new orders are created with the necessary properties (e.g., specific Route,
        /// Account) to ensure proper execution during the test.
        protected virtual OrderProperties OrderProperties { get; }

        /// <summary>
        /// Returns whether or not the brokers order methods implementation are async
        /// </summary>
        protected abstract bool IsAsync();

        /// <summary>
        /// Returns whether or not the brokers order cancel method implementation is async
        /// </summary>
        protected virtual bool IsCancelAsync()
        {
            return IsAsync();
        }

        /// <summary>
        /// Gets the current market price of the specified security
        /// </summary>
        protected abstract decimal GetAskPrice(Symbol symbol);

        /// <summary>
        /// Gets the default order quantity
        /// </summary>
        protected virtual decimal GetDefaultQuantity()
        {
            return 1;
        }

        [Test]
        public void IsConnected()
        {
            Assert.IsTrue(Brokerage.IsConnected);
        }

        public virtual void CancelComboOrders(ComboLimitOrderTestParameters parameters)
        {
            Log.Trace("");
            Log.Trace("CANCEL COMBO ORDERS");
            Log.Trace("");

            CancelOrders(parameters.CreateLongOrder(GetDefaultQuantity()), parameters.ExpectedStatus, parameters.ExpectedCancellationResult);
        }

        public virtual void CancelOrders(OrderTestParameters parameters)
        {
            Log.Trace("");
            Log.Trace("CANCEL ORDERS");
            Log.Trace("");

            CancelOrders([parameters.CreateLongOrder(GetDefaultQuantity())], parameters.ExpectedStatus, parameters.ExpectedCancellationResult);
        }

        public virtual void LongFromZero(OrderTestParameters parameters)
        {
            Log.Trace("");
            Log.Trace("LONG FROM ZERO");
            Log.Trace("");
            PlaceOrderWaitForStatus(parameters.CreateLongOrder(GetDefaultQuantity()), parameters.ExpectedStatus);
        }

        public virtual void CloseFromLong(OrderTestParameters parameters)
        {
            Log.Trace("");
            Log.Trace("CLOSE FROM LONG");
            Log.Trace("");
            // first go long
            PlaceOrderWaitForStatus(parameters.CreateLongMarketOrder(GetDefaultQuantity()), OrderStatus.Filled);

            // now close it
            PlaceOrderWaitForStatus(parameters.CreateShortOrder(GetDefaultQuantity()), parameters.ExpectedStatus);
        }

        public virtual void ShortFromZero(OrderTestParameters parameters)
        {
            Log.Trace("");
            Log.Trace("SHORT FROM ZERO");
            Log.Trace("");
            PlaceOrderWaitForStatus(parameters.CreateShortOrder(GetDefaultQuantity()), parameters.ExpectedStatus);
        }

        public virtual void CloseFromShort(OrderTestParameters parameters)
        {
            Log.Trace("");
            Log.Trace("CLOSE FROM SHORT");
            Log.Trace("");
            // first go short
            PlaceOrderWaitForStatus(parameters.CreateShortMarketOrder(GetDefaultQuantity()), OrderStatus.Filled);

            // now close it
            PlaceOrderWaitForStatus(parameters.CreateLongOrder(GetDefaultQuantity()), parameters.ExpectedStatus);
        }

        public virtual void ShortFromLong(OrderTestParameters parameters)
        {
            Log.Trace("");
            Log.Trace("SHORT FROM LONG");
            Log.Trace("");
            // first go long
            PlaceOrderWaitForStatus(parameters.CreateLongMarketOrder(GetDefaultQuantity()));

            // now go net short
            var order = PlaceOrderWaitForStatus(parameters.CreateShortOrder(2 * GetDefaultQuantity()), parameters.ExpectedStatus);

            if (parameters.ModifyUntilFilled)
            {
                ModifyOrderUntilFilled(order, parameters);
            }
        }

        public virtual void LongFromShort(OrderTestParameters parameters)
        {
            Log.Trace("");
            Log.Trace("LONG FROM SHORT");
            Log.Trace("");
            // first fo short
            PlaceOrderWaitForStatus(parameters.CreateShortMarketOrder(-GetDefaultQuantity()), OrderStatus.Filled);

            // now go long
            var order = PlaceOrderWaitForStatus(parameters.CreateLongOrder(2 * GetDefaultQuantity()), parameters.ExpectedStatus);

            if (parameters.ModifyUntilFilled)
            {
                ModifyOrderUntilFilled(order, parameters);
            }
        }

        public virtual void LongCombo(ComboLimitOrderTestParameters parameters)
        {
            Log.Trace("");
            Log.Trace($"LONG COMBO: " + parameters);
            Log.Trace("");

            var orders = PlaceOrderWaitForStatus(parameters.CreateLongOrder(GetDefaultQuantity()), parameters.ExpectedStatus);

            if (parameters.ModifyUntilFilled)
            {
                ModifyOrdersUntilFilled(orders, () => parameters.ModifyOrderToFill(orders, GetAskPrice));
            }
        }

        public virtual void ShortCombo(ComboLimitOrderTestParameters parameters)
        {
            Log.Trace("");
            Log.Trace($"SHORT COMBO: " + parameters);
            Log.Trace("");

            var orders = PlaceOrderWaitForStatus(parameters.CreateShortOrder(GetDefaultQuantity()), parameters.ExpectedStatus);

            if (parameters.ModifyUntilFilled)
            {
                ModifyOrdersUntilFilled(orders, () => parameters.ModifyOrderToFill(orders, GetAskPrice));
            }
        }

        /// <summary>
        /// Places a long order, updates it, and then cancels it. Verifies that each operation completes successfully.
        /// </summary>
        /// <param name="parameters">The parameters for creating and managing the order.</param>
        /// <param name="quantityIncrement">The increment to add to the order quantity during the update.</param>
        /// <param name="limitPriceIncrement">The increment to add to the order's limit price during the update.</param>
        /// <param name="stopPriceIncrement">The increment to add to the order's stop price during the update.</param>
        /// <exception cref="AssertFailedException">Thrown if the order fails to update or cancel as expected.</exception>
        public virtual void LongFromZeroUpdateAndCancel(OrderTestParameters parameters, decimal quantityIncrement = 1, decimal limitPriceIncrement = 0.01m, decimal stopPriceIncrement = 0.01m)
        {
            Log.Trace("");
            Log.Trace("LONG FROM ZERO THEN UPDATE AND CANCEL");
            Log.Trace("");

            var order = PlaceOrderWaitForStatus(parameters.CreateLongOrder(GetDefaultQuantity()), parameters.ExpectedStatus);

            using var updatedOrderStatusEvent = new AutoResetEvent(false);
            using var canceledOrderStatusEvent = new AutoResetEvent(false);

            EventHandler<List<OrderEvent>> brokerageOnOrdersStatusChanged = (_, orderEvents) =>
            {
                var eventOrderStatus = orderEvents[0].Status;

                order.Status = eventOrderStatus;

                switch (eventOrderStatus)
                {
                    case OrderStatus.UpdateSubmitted:
                        updatedOrderStatusEvent.Set();
                        break;
                    case OrderStatus.Canceled:
                        canceledOrderStatusEvent.Set();
                        break;
                }
            };

            Brokerage.OrdersStatusChanged += brokerageOnOrdersStatusChanged;

            var newQuantity = order.Quantity + quantityIncrement;

            decimal? limitPrice = order switch
            {
                LimitOrder lo => lo.LimitPrice,
                StopLimitOrder slo => slo.LimitPrice,
                LimitIfTouchedOrder lito => lito.LimitPrice,
                _ => null
            };

            decimal? stopPrice = order switch
            {
                StopMarketOrder smo => smo.StopPrice,
                StopLimitOrder slo => slo.StopPrice,
                _ => null
            };

            decimal? newLimitPrice = limitPrice.HasValue ? limitPrice.Value + limitPriceIncrement : null;
            decimal? newStopPrice = stopPrice.HasValue ? stopPrice.Value + stopPriceIncrement : null;

            Log.Trace("");
            Log.Trace($"UPDATE ORDER FIELDS: \n" +
                $"  oldQuantity = {order.Quantity}, newQuantity = {newQuantity}\n" +
                $"  oldLimitPrice = {limitPrice}, newLimitPrice = {newLimitPrice}\n" +
                $"  oldStopPrice = {stopPrice}, newStopPrice = {newStopPrice}");
            Log.Trace("");

            var updateOrderFields = new UpdateOrderFields()
            {
                Quantity = newQuantity,
                LimitPrice = newLimitPrice,
                StopPrice = newStopPrice
            };

            order.ApplyUpdateOrderRequest(new UpdateOrderRequest(DateTime.UtcNow, order.Id, updateOrderFields));

            if (!Brokerage.UpdateOrder(order) || !updatedOrderStatusEvent.WaitOne(TimeSpan.FromSeconds(5)))
            {
                Assert.Fail("Order is not updated well.");
            }

            if (!Brokerage.CancelOrder(order) || !canceledOrderStatusEvent.WaitOne(TimeSpan.FromSeconds(5)))
            {
                Assert.Fail("Order is not canceled well.");
            }

            Brokerage.OrdersStatusChanged -= brokerageOnOrdersStatusChanged;
        }

        [Test]
        public virtual void GetCashBalanceContainsSomething()
        {
            Log.Trace("");
            Log.Trace("GET CASH BALANCE");
            Log.Trace("");
            var balance = Brokerage.GetCashBalance();
            Assert.IsTrue(balance.Any());
        }

        [Test]
        public virtual void GetAccountHoldings()
        {
            Log.Trace("");
            Log.Trace("GET ACCOUNT HOLDINGS");
            Log.Trace("");
            var before = Brokerage.GetAccountHoldings();

            PlaceOrderWaitForStatus(GetMarketOrder(Symbol, GetDefaultQuantity()));

            Thread.Sleep(3000);

            var after = Brokerage.GetAccountHoldings();

            var beforeHoldings = before.FirstOrDefault(x => x.Symbol == Symbol);
            var afterHoldings = after.FirstOrDefault(x => x.Symbol == Symbol);

            var beforeQuantity = beforeHoldings == null ? 0 : beforeHoldings.Quantity;
            var afterQuantity = afterHoldings == null ? 0 : afterHoldings.Quantity;

            Assert.AreEqual(GetDefaultQuantity(), afterQuantity - beforeQuantity);
        }

        [Test, Explicit("This test requires reading the output and selection of a low volume security for the Brokerage")]
        public void PartialFills()
        {
            using var manualResetEvent = new ManualResetEvent(false);

            var qty = 1000000m;
            var remaining = qty;
            var sync = new object();
            Brokerage.OrdersStatusChanged += (sender, orderEvents) =>
            {
                lock (sync)
                {
                    var orderEvent = orderEvents[0];
                    remaining -= orderEvent.FillQuantity;
                    Log.Trace("Remaining: " + remaining + " FillQuantity: " + orderEvent.FillQuantity);
                    if (orderEvent.Status == OrderStatus.Filled)
                    {
                        manualResetEvent.Set();
                    }
                }
            };

            // pick a security with low, but some, volume
            var symbol = Symbols.EURUSD;
            var order = GetMarketOrder(symbol, qty);
            OrderProvider.Add(order);
            Brokerage.PlaceOrder(order);

            // pause for a while to wait for fills to come in
            manualResetEvent.WaitOne(2500);
            manualResetEvent.WaitOne(2500);
            manualResetEvent.WaitOne(2500);

            Log.Trace("Remaining: " + remaining);
            Assert.AreEqual(0, remaining);
        }

        /// <summary>
        /// Updates the specified order in the brokerage until it fills or reaches a timeout
        /// </summary>
        /// <param name="order">The order to be modified</param>
        /// <param name="parameters">The order test parameters that define how to modify the order</param>
        /// <param name="secondsTimeout">Maximum amount of time to wait until the order fills</param>
        protected virtual void ModifyOrderUntilFilled(Order order, OrderTestParameters parameters, double secondsTimeout = 90)
        {
            ModifyOrdersUntilFilled([order], () => parameters.ModifyOrderToFill(Brokerage, order, GetAskPrice(order.Symbol)), secondsTimeout);
        }

        protected virtual void ModifyOrdersUntilFilled(IReadOnlyCollection<Order> orders, Func<bool> modifyOrderToFill, double secondsTimeout = 90)
        {
            if (orders.All(o => o.Status == OrderStatus.Filled))
            {
                return;
            }

            EventHandler<List<OrderEvent>> brokerageOnOrdersStatusChanged = (sender, orderEvents) =>
            {
                foreach (var orderEvent in orderEvents)
                {
                    if (orderEvent.Status == OrderStatus.Canceled || orderEvent.Status == OrderStatus.Invalid)
                    {
                        var order = _orderProvider.GetOrderById(orderEvent.Id);
                        Log.Trace("");
                        Log.Trace($"{nameof(ModifyOrdersUntilFilled)}: " + order);
                        Log.Trace("");
                        Assert.Fail("Unexpected order status: " + orderEvent.Status);
                    }
                }
            };

            Brokerage.OrdersStatusChanged += brokerageOnOrdersStatusChanged;

            Log.Trace("");
            Log.Trace("MODIFY UNTIL FILLED: " + string.Join(Environment.NewLine, orders));
            Log.Trace("");

            var stopwatch = Stopwatch.StartNew();
            while (!orders.All(o => o.Status.IsClosed()) && !OrderFillEvent.WaitOne(TimeSpan.FromSeconds(3)) && stopwatch.Elapsed.TotalSeconds < secondsTimeout)
            {
                OrderFillEvent.Reset();
                if (orders.All(o => o.Status == OrderStatus.PartiallyFilled))
                {
                    continue;
                }

                if (modifyOrderToFill())
                {
                    if (orders.All(o => o.Status.IsClosed()))
                    {
                        break;
                    }

                    Log.Trace($"{nameof(BrokerageTests)}.{nameof(ModifyOrdersUntilFilled)}: " + string.Join(Environment.NewLine, orders));
                    foreach (var order in orders)
                    {
                        if (!Brokerage.UpdateOrder(order))
                        {
                            // could be filling already, partial fill
                        }
                    }
                }
            }
            Brokerage.OrdersStatusChanged -= brokerageOnOrdersStatusChanged;

            foreach (var order in orders)
            {
                Assert.AreEqual(OrderStatus.Filled, order.Status, $"Brokerage failed to update the order: Id = {order.Id} by Status = {order.Status}");
            }
        }

        /// <summary>
        /// Places the specified order with the brokerage and wait until we get the <paramref name="expectedStatus"/> back via an OrdersStatusChanged event.
        /// This function handles adding the order to the <see cref="IOrderProvider"/> instance as well as incrementing the order ID.
        /// </summary>
        /// <param name="order">The order to be submitted</param>
        /// <param name="expectedStatus">The status to wait for</param>
        /// <param name="secondsTimeout">Maximum amount of time to wait for <paramref name="expectedStatus"/></param>
        /// <param name="allowFailedSubmission">Allow failed order submission</param>
        /// <returns>The same order that was submitted.</returns>
        protected Order PlaceOrderWaitForStatus(Order order, OrderStatus expectedStatus = OrderStatus.Filled,
                                                double secondsTimeout = 30.0, bool allowFailedSubmission = false)
        {
            return PlaceOrderWaitForStatus([order], expectedStatus, secondsTimeout, allowFailedSubmission).First();
        }

        /// <summary>
        /// Places the specified order with the brokerage and wait until we get the <paramref name="expectedStatus"/> back via an OrdersStatusChanged event.
        /// This function handles adding the order to the <see cref="IOrderProvider"/> instance as well as incrementing the order ID.
        /// </summary>
        /// <param name="orders">The collection of orders to submitted.</param>
        /// <param name="expectedStatus">The status to wait for</param>
        /// <param name="secondsTimeout">Maximum amount of time to wait for <paramref name="expectedStatus"/></param>
        /// <param name="allowFailedSubmission">Allow failed order submission</param>
        /// <returns>The same order that was submitted.</returns>
        protected IReadOnlyCollection<Order> PlaceOrderWaitForStatus(IReadOnlyCollection<Order> orders, OrderStatus expectedStatus = OrderStatus.Filled,
                                                double secondsTimeout = 30.0, bool allowFailedSubmission = false)
        {
            using var requiredStatusEvent = new ManualResetEvent(false);
            using var desiredStatusEvent = new ManualResetEvent(false);
            EventHandler<List<OrderEvent>> brokerageOnOrdersStatusChanged = (sender, orderEvents) =>
            {
                foreach (var orderEvent in orderEvents)
                {
                    var order = _orderProvider.GetOrderById(orderEvent.OrderId);
                    order.Status = orderEvent.Status;
                    // no matter what, every order should fire at least one of these
                    if (orders.All(o => o.Status is OrderStatus.Submitted or OrderStatus.Invalid))
                    {
                        Log.Trace("");
                        Log.Trace("SUBMITTED: " + orderEvent);
                        Log.Trace("");
                        try
                        {
                            requiredStatusEvent.Set();
                        }
                        catch (ObjectDisposedException)
                        {
                        }
                    }
                    // make sure we fire the status we're expecting
                    if (orders.All(o => o.Status == expectedStatus))
                    {
                        Log.Trace("");
                        Log.Trace("EXPECTED: " + orderEvent);
                        Log.Trace("");
                        try
                        {
                            desiredStatusEvent.Set();
                        }
                        catch (ObjectDisposedException)
                        {
                        }
                    }
                }
            };

            Brokerage.OrdersStatusChanged += brokerageOnOrdersStatusChanged;

            OrderFillEvent.Reset();

            foreach (var order in orders)
            {
                OrderProvider.Add(order);
                if (!Brokerage.PlaceOrder(order) && !allowFailedSubmission)
                {
                    Assert.Fail("Brokerage failed to place the order: " + orders);
                }
            }

            // This is due to IB simulating stop orders https://www.interactivebrokers.com/en/trading/orders/stop.php
            // which causes the Status.Submitted order event to never be set
            var assertOrderEventStatus = true;
            if (Brokerage.Name == "Interactive Brokers Brokerage" && orders.Any(o => o.Type is OrderType.StopMarket or OrderType.StopLimit))
            {
                assertOrderEventStatus = false;
            }

            var delayMilliseconds = Convert.ToInt32(1000 * secondsTimeout);
            if (assertOrderEventStatus)
            {
                if (requiredStatusEvent.WaitOneAssertFail(delayMilliseconds, "Expected every order to fire a submitted or invalid status event"))
                {
                    desiredStatusEvent.WaitOneAssertFail(delayMilliseconds,
                        "OrderStatus " + expectedStatus + " was not encountered within the timeout." + string.Join("", orders.Select(x => " Order Id:" + x.Id)));
                }
            }
            else
            {
                requiredStatusEvent.WaitOne(delayMilliseconds);
            }

            Brokerage.OrdersStatusChanged -= brokerageOnOrdersStatusChanged;

            return orders;
        }

        protected static SubscriptionDataConfig GetSubscriptionDataConfig<T>(Symbol symbol, Resolution resolution)
        {
            return new SubscriptionDataConfig(
                typeof(T),
                symbol,
                resolution,
                TimeZones.Utc,
                TimeZones.Utc,
                true,
                true,
                false);
        }

        protected static void ProcessFeed(IEnumerator<BaseData> enumerator, CancellationTokenSource cancellationToken, Action<BaseData> callback = null)
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    while (enumerator.MoveNext() && !cancellationToken.IsCancellationRequested)
                    {
                        BaseData tick = enumerator.Current;
                        if (callback != null)
                        {
                            callback.Invoke(tick);
                        }
                    }
                }
                catch (AssertionException)
                {
                    throw;
                }
                catch (Exception err)
                {
                    Log.Error(err.Message);
                }
            }, cancellationToken.Token);
        }

        private Order GetMarketOrder(Symbol symbol, decimal quantity)
        {
            var mkt = new MarketOrderTestParameters(symbol, OrderProperties);
            return quantity > 0 ? mkt.CreateLongOrder(quantity) : mkt.CreateShortOrder(quantity);
        }

        /// <summary>
        /// Sets the given reset event when the order reaches the expected status.
        /// For combo orders, all legs must match the expected status.
        /// For simple orders, the event is set immediately.
        /// </summary>
        /// <param name="order">The order to check (simple or combo).</param>
        /// <param name="expectedStatus">The status to wait for before setting the event.</param>
        /// <param name="resetEvent">The reset event to signal.</param>
        private void SignalOrderStatusReached(Order order, OrderStatus expectedStatus, ManualResetEvent resetEvent)
        {
            if (GroupOrderExtensions.TryGetGroupOrders(order, _orderProvider.GetOrderById, out var orders))
            {
                // Combo order: set immediately if all legs match expected status
                if (orders.All(o => o.Status == expectedStatus))
                {
                    resetEvent.Set();
                }
            }
            else
            {
                // Simple order: set after its own status update
                resetEvent.Set();
            }
        }

        /// <summary>
        /// Cancels the specified <paramref name="orders"/> and waits until each order
        /// reaches the given <paramref name="expectedStatus"/> (via an <c>OrderStatusChanged</c> event),
        /// or until the timeout expires.
        /// <param name="orders">The collection of orders to cancel.</param>
        /// <param name="expectedStatus">The order status to wait for after cancellation.</param>
        /// <param name="expectedCancellationResult">Indicates whether the cancellation is expected to succeed.</param></param>
        /// <param name="secondsTimeout">The maximum number of seconds to wait for the expected cancellation result</param>
        private void CancelOrders(IReadOnlyCollection<Order> orders, OrderStatus expectedStatus = OrderStatus.Submitted, bool expectedCancellationResult = true, int secondsTimeout = 20)
        {
            var submittedOrders = PlaceOrderWaitForStatus(orders, expectedStatus);

            OrderCancelledResetEvent.Reset();

            var cancelResult = false;
            try
            {
                foreach (var order in submittedOrders)
                {
                    cancelResult = Brokerage.CancelOrder(order);
                }
            }
            catch (Exception exception)
            {
                Log.Error(exception);
            }

            Assert.AreEqual(IsCancelAsync() || expectedCancellationResult, cancelResult);

            if (expectedCancellationResult)
            {
                // We expect the OrderStatus.Canceled event
                OrderCancelledResetEvent.WaitOneAssertFail(1000 * secondsTimeout, "Order timeout to cancel");
            }

            var openIds = GetOpenOrders().Select(o => o.Id).ToHashSet();

            var isOrderStillOpen = false;
            foreach (var order in orders)
            {
                if (openIds.Contains(order.Id))
                {
                    isOrderStillOpen = true;
                }
            }

            Assert.IsFalse(isOrderStillOpen);

            OrderCancelledResetEvent.Reset();

            var cancelResultSecondTime = false;
            try
            {
                foreach (var order in submittedOrders)
                {
                    cancelResultSecondTime = Brokerage.CancelOrder(order);
                }
            }
            catch (Exception exception)
            {
                Log.Error(exception);
            }

            Assert.AreEqual(IsCancelAsync(), cancelResultSecondTime);
            // We do NOT expect the OrderStatus.Canceled event
            Assert.IsFalse(OrderCancelledResetEvent.WaitOne(TimeSpan.FromSeconds(10)));
        }
    }
}
