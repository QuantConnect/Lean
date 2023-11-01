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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Market;
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
            Log.Trace("GET OPEN ORDERS");
            Log.Trace("");
            foreach (var openOrder in brokerage.GetOpenOrders())
            {
                OrderProvider.Add(openOrder);
            }

            Log.Trace("");
            Log.Trace("GET ACCOUNT HOLDINGS");
            Log.Trace("");
            foreach (var accountHolding in brokerage.GetAccountHoldings())
            {
                // these securities don't need to be real, just used for the ISecurityProvider impl, required
                // by brokerages to track holdings
                SecurityProvider[accountHolding.Symbol] = CreateSecurity(accountHolding.Symbol);
                SecurityProvider[accountHolding.Symbol].Holdings.SetHoldings(accountHolding.AveragePrice, accountHolding.Quantity);
            }
            brokerage.OrdersStatusChanged += HandleFillEvents;

            return brokerage;
        }

        private void HandleFillEvents(object sender, List<OrderEvent> ordeEvents)
        {
            Log.Trace("");
            Log.Trace($"ORDER STATUS CHANGED: {string.Join(",", ordeEvents.Select(x => x.ToString()))}");
            Log.Trace("");

            var orderEvent = ordeEvents[0];

            // we need to keep this maintained properly
            if (orderEvent.Status == OrderStatus.Filled || orderEvent.Status == OrderStatus.PartiallyFilled)
            {
                Log.Trace("FILL EVENT: " + orderEvent.FillQuantity + " units of " + orderEvent.Symbol.ToString());

                if (orderEvent.Status == OrderStatus.Filled)
                {
                    OrderFillEvent.Set();
                }

                Security security;
                if (_securityProvider.TryGetValue(orderEvent.Symbol, out security))
                {
                    var holding = _securityProvider[orderEvent.Symbol].Holdings;
                    holding.SetHoldings(orderEvent.FillPrice, holding.Quantity + orderEvent.FillQuantity);
                }
                else
                {
                    _securityProvider[orderEvent.Symbol] = CreateSecurity(orderEvent.Symbol);
                    _securityProvider[orderEvent.Symbol].Holdings.SetHoldings(orderEvent.FillPrice, orderEvent.FillQuantity);
                }

                Log.Trace("--HOLDINGS: " + _securityProvider[orderEvent.Symbol]);

                // update order mapping
                var order = _orderProvider.GetOrderById(orderEvent.OrderId);
                order.Status = orderEvent.Status;
            }
        }

        public static Security CreateSecurity(Symbol symbol)
        {
            return new Security(
                SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                new SubscriptionDataConfig(
                    typeof(TradeBar),
                    symbol,
                    Resolution.Minute,
                    TimeZones.NewYork,
                    TimeZones.NewYork,
                    false,
                    false,
                    false
                ),
                new Cash(Currencies.USD, 0, 1m),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );
        }

        public OrderProvider OrderProvider
        {
            get { return _orderProvider ?? (_orderProvider = new OrderProvider()); }
        }

        public SecurityProvider SecurityProvider
        {
            get { return _securityProvider ?? (_securityProvider = new SecurityProvider()); }
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
            brokerage.OrdersStatusChanged -= HandleFillEvents;
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
                var order = new MarketOrder(holding.Symbol, -holding.Quantity, DateTime.UtcNow);
                PlaceOrderWaitForStatus(order, OrderStatus.Filled);
            }
        }

        protected void CancelOpenOrders()
        {
            Log.Trace("");
            Log.Trace("CANCEL OPEN ORDERS");
            Log.Trace("");
            var openOrders = Brokerage.GetOpenOrders();
            foreach (var openOrder in openOrders)
            {
                Log.Trace("Canceling: " + openOrder);
                Brokerage.CancelOrder(openOrder);
            }
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

        public virtual void CancelOrders(OrderTestParameters parameters)
        {
            const int secondsTimeout = 20;
            Log.Trace("");
            Log.Trace("CANCEL ORDERS");
            Log.Trace("");

            var order = PlaceOrderWaitForStatus(parameters.CreateLongOrder(GetDefaultQuantity()), parameters.ExpectedStatus);

            var canceledOrderStatusEvent = new ManualResetEvent(false);
            EventHandler<List<OrderEvent>> orderStatusCallback = (sender, fills) =>
            {
                if (fills[0].Status == OrderStatus.Canceled)
                {
                    canceledOrderStatusEvent.Set();
                }
            };
            Brokerage.OrdersStatusChanged += orderStatusCallback;
            var cancelResult = false;
            try
            {
                cancelResult = Brokerage.CancelOrder(order);
            }
            catch (Exception exception)
            {
                Log.Error(exception);
            }

            Assert.AreEqual(IsCancelAsync() || parameters.ExpectedCancellationResult, cancelResult);

            if (parameters.ExpectedCancellationResult)
            {
                // We expect the OrderStatus.Canceled event
                canceledOrderStatusEvent.WaitOneAssertFail(1000 * secondsTimeout, "Order timedout to cancel");
            }

            var openOrders = Brokerage.GetOpenOrders();
            var cancelledOrder = openOrders.FirstOrDefault(x => x.Id == order.Id);
            Assert.IsNull(cancelledOrder);

            canceledOrderStatusEvent.Reset();

            var cancelResultSecondTime = false;
            try
            {
                cancelResultSecondTime = Brokerage.CancelOrder(order);
            }
            catch (Exception exception)
            {
                Log.Error(exception);
            }
            Assert.AreEqual(IsCancelAsync(), cancelResultSecondTime);
            // We do NOT expect the OrderStatus.Canceled event
            Assert.IsFalse(canceledOrderStatusEvent.WaitOne(new TimeSpan(0, 0, 10)));

            Brokerage.OrdersStatusChanged -= orderStatusCallback;
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

            PlaceOrderWaitForStatus(new MarketOrder(Symbol, GetDefaultQuantity(), DateTime.UtcNow));

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
            var manualResetEvent = new ManualResetEvent(false);

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
            var order = new MarketOrder(symbol, qty, DateTime.UtcNow) { Id = 1 };
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
            if (order.Status == OrderStatus.Filled)
            {
                return;
            }

            EventHandler<List<OrderEvent>> brokerageOnOrdersStatusChanged = (sender, args) =>
            {
                var orderEvent = args[0];
                order.Status = orderEvent.Status;
                if (orderEvent.Status == OrderStatus.Canceled || orderEvent.Status == OrderStatus.Invalid)
                {
                    Log.Trace("ModifyOrderUntilFilled(): " + order);
                    Assert.Fail("Unexpected order status: " + orderEvent.Status);
                }
            };
            EventHandler<BrokerageOrderIdChangedEvent> brokerageOrderIdChanged = (sender, args) => {
                order.BrokerId = args.BrokerId;
            };

            Brokerage.OrderIdChanged += brokerageOrderIdChanged;
            Brokerage.OrdersStatusChanged += brokerageOnOrdersStatusChanged;

            Log.Trace("");
            Log.Trace("MODIFY UNTIL FILLED: " + order);
            Log.Trace("");
            var stopwatch = Stopwatch.StartNew();
            while (order.Status != OrderStatus.Filled && !OrderFillEvent.WaitOne(3000) && stopwatch.Elapsed.TotalSeconds < secondsTimeout)
            {
                OrderFillEvent.Reset();
                if (order.Status == OrderStatus.PartiallyFilled) continue;

                var marketPrice = GetAskPrice(order.Symbol);
                Log.Trace("BrokerageTests.ModifyOrderUntilFilled(): Ask: " + marketPrice);

                var updateOrder = parameters.ModifyOrderToFill(Brokerage, order, marketPrice);
                if (updateOrder)
                {
                    if (order.Status == OrderStatus.Filled)
                    {
                        break;
                    }

                    Log.Trace("BrokerageTests.ModifyOrderUntilFilled(): " + order);
                    if (!Brokerage.UpdateOrder(order))
                    {
                        if (order.Status != OrderStatus.Filled)
                        {
                            Assert.Fail("Brokerage failed to update the order");
                        }
                    }
                }
            }

            Brokerage.OrderIdChanged -= brokerageOrderIdChanged;
            Brokerage.OrdersStatusChanged -= brokerageOnOrdersStatusChanged;
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
            var requiredStatusEvent = new ManualResetEvent(false);
            var desiredStatusEvent = new ManualResetEvent(false);
            EventHandler<List<OrderEvent>> brokerageOnOrdersStatusChanged = (sender, args) =>
            {
                var orderEvent = args[0];
                // no matter what, every order should fire at least one of these
                if (orderEvent.Status == OrderStatus.Submitted || orderEvent.Status == OrderStatus.Invalid)
                {
                    Log.Trace("");
                    Log.Trace("SUBMITTED: " + orderEvent);
                    Log.Trace("");
                    requiredStatusEvent.Set();
                }
                // make sure we fire the status we're expecting
                if (orderEvent.Status == expectedStatus)
                {
                    Log.Trace("");
                    Log.Trace("EXPECTED: " + orderEvent);
                    Log.Trace("");
                    desiredStatusEvent.Set();
                }
            };
            EventHandler<BrokerageOrderIdChangedEvent> brokerageOrderIdChanged = (sender, args) => {
                order.BrokerId = args.BrokerId;
            };

            Brokerage.OrderIdChanged += brokerageOrderIdChanged;
            Brokerage.OrdersStatusChanged += brokerageOnOrdersStatusChanged;

            OrderFillEvent.Reset();

            OrderProvider.Add(order);
            if (!Brokerage.PlaceOrder(order) && !allowFailedSubmission)
            {
                Assert.Fail("Brokerage failed to place the order: " + order);
            }

            // This is due to IB simulating stop orders https://www.interactivebrokers.com/en/trading/orders/stop.php
            // which causes the Status.Submitted order event to never be set
            bool assertOrderEventStatus = !(Brokerage.Name == "Interactive Brokers Brokerage"
                                            && new[] { OrderType.StopMarket, OrderType.StopLimit }.Contains(order.Type));
            if (assertOrderEventStatus)
            {
                requiredStatusEvent.WaitOneAssertFail((int)(1000 * secondsTimeout), "Expected every order to fire a submitted or invalid status event");
                desiredStatusEvent.WaitOneAssertFail((int)(1000 * secondsTimeout), "OrderStatus " + expectedStatus + " was not encountered within the timeout. Order Id:" + order.Id);
            }
            else
            {
                requiredStatusEvent.WaitOne((int)(1000 * secondsTimeout));
            }

            Brokerage.OrderIdChanged -= brokerageOrderIdChanged;
            Brokerage.OrdersStatusChanged -= brokerageOnOrdersStatusChanged;

            return order;
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

    }
}
