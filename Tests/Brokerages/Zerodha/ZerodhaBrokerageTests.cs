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

using Moq;
using NUnit.Framework;
using QuantConnect.Brokerages;
using QuantConnect.Brokerages.Zerodha;
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Tests.Common.Securities;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace QuantConnect.Tests.Brokerages.Zerodha
{
    [TestFixture, Ignore("This test requires a configured and active Zerodha account")]
    public class ZerodhaBrokerageTests
    {


        private static IOrderProperties orderProperties = new ZerodhaOrderProperties(exchange: "nse");

        private IBrokerage _brokerage;
        private OrderProvider _orderProvider;
        private SecurityProvider _securityProvider;


        [SetUp]
        public void Setup()
        {
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
            LiquidateZerodhaHoldings();
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
                LiquidateZerodhaHoldings();
                Thread.Sleep(1000);
            }
            finally
            {
                if (_brokerage != null)
                {
                    DisposeBrokerage(_brokerage);
                }
            }
        }

        /// <summary>
        /// Disposes of the brokerage and any external resources started in order to create it
        /// </summary>
        /// <param name="brokerage">The brokerage instance to be disposed of</param>
        protected virtual void DisposeBrokerage(IBrokerage brokerage)
        {
            brokerage.Disconnect();
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
            }
            brokerage.OrderStatusChanged += (sender, args) =>
            {
                Log.Trace("");
                Log.Trace("ORDER STATUS CHANGED: " + args);
                Log.Trace("");

                // we need to keep this maintained properly
                if (args.Status == OrderStatus.Filled || args.Status == OrderStatus.PartiallyFilled)
                {
                    Log.Trace("FILL EVENT: " + args.FillQuantity + " units of " + args.Symbol.ToString());

                    Security security;
                    if (_securityProvider.TryGetValue(args.Symbol, out security))
                    {
                        var holding = _securityProvider[args.Symbol].Holdings;
                        holding.SetHoldings(args.FillPrice, holding.Quantity + args.FillQuantity);
                    }
                    else
                    {
                        _securityProvider[args.Symbol] = CreateSecurity(args.Symbol);
                        _securityProvider[args.Symbol].Holdings.SetHoldings(args.FillPrice, args.FillQuantity);
                    }

                    Log.Trace("--HOLDINGS: " + _securityProvider[args.Symbol]);

                    // update order mapping
                    var order = _orderProvider.GetOrderById(args.OrderId);
                    order.Status = args.Status;
                }
            };
            return brokerage;
        }


        internal static Security CreateSecurity(Symbol symbol)
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
        /// This is used to ensure each test starts with a clean, known state.
        /// </summary>
        protected void LiquidateZerodhaHoldings()
        {
            Log.Trace("");
            Log.Trace("LIQUIDATE HOLDINGS");
            Log.Trace("");

            var holdings = Brokerage.GetAccountHoldings();

            foreach (var holding in holdings)
            {
                if (holding.Quantity == 0) continue;
                Log.Trace("Liquidating: " + holding);
                var order = new MarketOrder(holding.Symbol, -holding.Quantity, DateTime.UtcNow,properties:orderProperties);
                _orderProvider.Add(order);
                PlaceZerodhaOrderWaitForStatus(order, OrderStatus.Filled);
            }
        }

        /// <summary>
        /// Provides the data required to test each order type in various cases
        /// </summary>
        private static TestCaseData[] OrderParameters()
        {
            return new[]
            {
                new TestCaseData(new MarketOrderTestParameters(Symbols.IDEA,orderProperties)).SetName("MarketOrder"),
                new TestCaseData(new LimitOrderTestParameters(Symbols.IDEA, 9.00m, 9.30m,orderProperties)).SetName("LimitOrder"),
                new TestCaseData(new StopMarketOrderTestParameters(Symbols.IDEA, 10.50m,  9.50m,orderProperties)).SetName("StopMarketOrder"),
                //new TestCaseData(new StopLimitOrderTestParameters(Symbols.IDEA,  9.00m, 10.50m,orderProperties)).SetName("StopLimitOrder")
            };
        }

        /// <summary>
        /// Creates the brokerage under test
        /// </summary>
        /// <returns>A connected brokerage instance</returns>
        protected IBrokerage CreateBrokerage(IOrderProvider orderProvider, ISecurityProvider securityProvider)
        {

            var securities = new SecurityManager(new TimeKeeper(DateTime.UtcNow, TimeZones.Kolkata))
            {
                { Symbol, CreateSecurity(Symbol) }
            };

            var transactions = new SecurityTransactionManager(null, securities);
            transactions.SetOrderProcessor(new FakeOrderProcessor());

            var algorithm = new Mock<IAlgorithm>();
            algorithm.Setup(a => a.Transactions).Returns(transactions);
            algorithm.Setup(a => a.BrokerageModel).Returns(new ZerodhaBrokerageModel());
            algorithm.Setup(a => a.Portfolio).Returns(new SecurityPortfolioManager(securities, transactions));

            var accessToken = Config.Get("zerodha-access-token");
            var apiKey = Config.Get("zerodha-api-key");
            var tradingSegment = Config.Get("zerodha-trading-segment");
            var productType = Config.Get("zerodha-product-type");
            var zerodha = new ZerodhaBrokerage(tradingSegment, productType, apiKey, accessToken, algorithm.Object, algorithm.Object.Portfolio, new AggregationManager());

            return zerodha;
        }

        /// <summary>
        /// Gets the symbol to be traded, must be shortable
        /// </summary>
        protected Symbol Symbol => Symbols.IDEA;

        /// <summary>
        /// Gets the security type associated with the <see cref="BrokerageTests.Symbol"/>
        /// </summary>
        protected SecurityType SecurityType => SecurityType.Equity;

        /// <summary>
        /// Returns wether or not the brokers order methods implementation are async
        /// </summary>
        protected bool IsAsync()
        {
            return false;
        }


        /// <summary>
        /// Gets the default order quantity
        /// </summary>
        protected virtual decimal GetDefaultQuantity()
        {
            return 1;
        }

        /// <summary>
        /// Gets the current market price of the specified security
        /// </summary>
        protected decimal GetAskPrice(Symbol symbol)
        {
            var zerodha = (ZerodhaBrokerage)Brokerage;
            var quotes = zerodha.GetQuote(symbol);
            return quotes.LastPrice;
        }

        [Test]
        public void ShortIdea()
        {
            PlaceZerodhaOrderWaitForStatus(new MarketOrder(Symbols.IDEA, -1, DateTime.Now, properties: orderProperties), OrderStatus.Submitted, allowFailedSubmission: true);

            // wait for output to be generated
            Thread.Sleep(20 * 1000);
        }

        [Test, TestCaseSource(nameof(OrderParameters))]
        public void CancelOrders(OrderTestParameters parameters)
        {
            const int secondsTimeout = 20;
            Log.Trace("");
            Log.Trace("CANCEL ORDERS");
            Log.Trace("");

            var order = PlaceZerodhaOrderWaitForStatus(parameters.CreateLongOrder(GetDefaultQuantity()), parameters.ExpectedStatus);

            var canceledOrderStatusEvent = new ManualResetEvent(false);
            EventHandler<OrderEvent> orderStatusCallback = (sender, fill) =>
            {
                if (fill.Status == OrderStatus.Canceled)
                {
                    canceledOrderStatusEvent.Set();
                }
            };
            Brokerage.OrderStatusChanged += orderStatusCallback;
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

            //canceledOrderStatusEvent.Reset();

            //var cancelResultSecondTime = false;
            //try
            //{
            //    cancelResultSecondTime = Brokerage.CancelOrder(order);
            //}
            //catch (Exception exception)
            //{
            //    Log.Error(exception);
            //}
            //Assert.AreEqual(IsCancelAsync(), cancelResultSecondTime);
            //// We do NOT expect the OrderStatus.Canceled event
            //Assert.IsFalse(canceledOrderStatusEvent.WaitOne(new TimeSpan(0, 0, 10)));

            //Brokerage.OrderStatusChanged -= orderStatusCallback;
        }


        [Test]
        public void ValidateStopLimitOrders()
        {
            var zerodha = (ZerodhaBrokerage)Brokerage;
            var symbol = Symbol;
            var lastPrice = GetAskPrice(symbol.Value);

            // Buy StopLimit order below market TODO: This might not work because of the Zerodha structure. Verify this.
            //var stopPrice = lastPrice - 0.10m; 
            //var limitPrice = stopPrice + 0.10m;
            //var order = new StopLimitOrder(symbol, 1, stopPrice, limitPrice, DateTime.UtcNow, properties: orderProperties);
            //Assert.IsTrue(zerodha.PlaceOrder(order));

            // Buy StopLimit order above market
            var stopPrice = lastPrice + 0.20m;
            var limitPrice = stopPrice + 0.25m;
            var order = new StopLimitOrder(symbol, 1, stopPrice, limitPrice, DateTime.UtcNow, properties: orderProperties);
            Assert.IsTrue(zerodha.PlaceOrder(order));

            // In case there is no position, the following sell orders would not be placed
            // So build a position for them.
            var marketOrder = new MarketOrder(symbol, 2, DateTime.UtcNow, properties: orderProperties);
            Assert.IsTrue(zerodha.PlaceOrder(marketOrder));

            Thread.Sleep(20000);
            // Sell StopLimit order below market
            stopPrice = lastPrice - 0.25m;
            limitPrice = stopPrice - 0.5m;
            order = new StopLimitOrder(symbol, -1, stopPrice, limitPrice, DateTime.UtcNow, properties: orderProperties);
            Assert.IsTrue(zerodha.PlaceOrder(order));

            // Sell StopLimit order above market. TODO: This might not work because of the Zerodha structure. Verify this 
            //stopPrice = lastPrice + 0.5m;
            //limitPrice = stopPrice - 0.25m ;
            //order = new StopLimitOrder(symbol, -1, stopPrice, limitPrice, DateTime.UtcNow, properties: orderProperties);
            //Assert.IsTrue(zerodha.PlaceOrder(order));
        }


        [Test, TestCaseSource(nameof(OrderParameters))]
        public void LongFromZero(OrderTestParameters parameters)
        {
            Log.Trace("");
            Log.Trace("LONG FROM ZERO");
            Log.Trace("");
            PlaceZerodhaOrderWaitForStatus(parameters.CreateLongOrder(GetDefaultQuantity()), parameters.ExpectedStatus);
        }


        [Test, TestCaseSource(nameof(OrderParameters))]
        public void CloseFromLong(OrderTestParameters parameters)
        {
            Log.Trace("");
            Log.Trace("CLOSE FROM LONG");
            Log.Trace("");
            // first go long
            PlaceZerodhaOrderWaitForStatus(parameters.CreateLongMarketOrder(GetDefaultQuantity()), OrderStatus.Filled);

            // now close it
            PlaceZerodhaOrderWaitForStatus(parameters.CreateShortOrder(GetDefaultQuantity()), parameters.ExpectedStatus);
        }

        [Test, TestCaseSource(nameof(OrderParameters))]
        public void ShortFromZero(OrderTestParameters parameters)
        {
            Log.Trace("");
            Log.Trace("SHORT FROM ZERO");
            Log.Trace("");
            PlaceZerodhaOrderWaitForStatus(parameters.CreateShortOrder(GetDefaultQuantity()), parameters.ExpectedStatus);
        }

        [Test, TestCaseSource(nameof(OrderParameters))]
        public virtual void CloseFromShort(OrderTestParameters parameters)
        {
            Log.Trace("");
            Log.Trace("CLOSE FROM SHORT");
            Log.Trace("");
            // first go short
            PlaceZerodhaOrderWaitForStatus(parameters.CreateShortMarketOrder(GetDefaultQuantity()), OrderStatus.Filled);

            // now close it
            PlaceZerodhaOrderWaitForStatus(parameters.CreateLongOrder(GetDefaultQuantity()), parameters.ExpectedStatus);
        }

        [Test, TestCaseSource(nameof(OrderParameters))]
        public virtual void ShortFromLong(OrderTestParameters parameters)
        {
            Log.Trace("");
            Log.Trace("SHORT FROM LONG");
            Log.Trace("");
            // first go long
            PlaceZerodhaOrderWaitForStatus(parameters.CreateLongMarketOrder(GetDefaultQuantity()));

            // now go net short
            var order = PlaceZerodhaOrderWaitForStatus(parameters.CreateShortOrder(2 * GetDefaultQuantity()), parameters.ExpectedStatus);

            if (parameters.ModifyUntilFilled)
            {
                ModifyOrderUntilFilled(order, parameters);
            }
        }

        [Test, Ignore("This test requires reading the output and selection of a low volume security for the Brokerage")]
        public void PartialFills()
        {
            var manualResetEvent = new ManualResetEvent(false);

            var qty = 1000000m;
            var remaining = qty;
            var sync = new object();
            Brokerage.OrderStatusChanged += (sender, orderEvent) =>
            {
                lock (sync)
                {
                    remaining -= orderEvent.FillQuantity;
                    Console.WriteLine("Remaining: " + remaining + " FillQuantity: " + orderEvent.FillQuantity);
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

            Console.WriteLine("Remaining: " + remaining);
            Assert.AreEqual(0, remaining);
        }

        [Test, TestCaseSource(nameof(OrderParameters))]
        public virtual void LongFromShort(OrderTestParameters parameters)
        {
            Log.Trace("");
            Log.Trace("LONG FROM SHORT");
            Log.Trace("");
            // first fo short
            PlaceZerodhaOrderWaitForStatus(parameters.CreateShortMarketOrder(-GetDefaultQuantity()), OrderStatus.Filled);

            // now go long
            var order = PlaceZerodhaOrderWaitForStatus(parameters.CreateLongOrder(2 * GetDefaultQuantity()), parameters.ExpectedStatus);

            if (parameters.ModifyUntilFilled)
            {
                ModifyOrderUntilFilled(order, parameters);
            }
        }

        [Test]
        public void GetCashBalanceContainsSomething()
        {
            Log.Trace("");
            Log.Trace("GET CASH BALANCE");
            Log.Trace("");
            var balance = Brokerage.GetCashBalance();
            Assert.IsTrue(balance.Any());
        }

        [Test]
        public void GetAccountHoldings()
        {
            Log.Trace("");
            Log.Trace("GET ACCOUNT HOLDINGS");
            Log.Trace("");
            var before = Brokerage.GetAccountHoldings();

            PlaceZerodhaOrderWaitForStatus(new MarketOrder(Symbol, GetDefaultQuantity(), DateTime.UtcNow,properties:orderProperties));

            Thread.Sleep(3000);

            var after = Brokerage.GetAccountHoldings();

            var beforeHoldings = before.FirstOrDefault(x => x.Symbol == Symbol);
            var afterHoldings = after.FirstOrDefault(x => x.Symbol == Symbol);

            var beforeQuantity = beforeHoldings == null ? 0 : beforeHoldings.Quantity;
            var afterQuantity = afterHoldings == null ? 0 : afterHoldings.Quantity;

            Assert.AreEqual(GetDefaultQuantity(), afterQuantity - beforeQuantity);
        }


        [Test]
        public void IsConnected()
        {
            Assert.IsTrue(Brokerage.IsConnected);
        }

        /// <summary>
        /// Places the specified order with the brokerage and wait until we get the <paramref name="expectedStatus"/> back via an OrderStatusChanged event.
        /// This function handles adding the order to the <see cref="IOrderProvider"/> instance as well as incrementing the order ID.
        /// </summary>
        /// <param name="order">The order to be submitted</param>
        /// <param name="expectedStatus">The status to wait for</param>
        /// <param name="secondsTimeout">Maximum amount of time to wait for <paramref name="expectedStatus"/></param>
        /// <param name="allowFailedSubmission">Allow failed order submission</param>
        /// <returns>The same order that was submitted.</returns>
        protected Order PlaceZerodhaOrderWaitForStatus(Order order, OrderStatus expectedStatus = OrderStatus.Filled,
                                                double secondsTimeout = 10.0, bool allowFailedSubmission = false)
        {
            var requiredStatusEvent = new ManualResetEvent(false);
            var desiredStatusEvent = new ManualResetEvent(false);
            EventHandler<OrderEvent> brokerageOnOrderStatusChanged = (sender, args) =>
            {
                // no matter what, every order should fire at least one of these
                if (args.Status == OrderStatus.Submitted || args.Status == OrderStatus.Invalid)
                {
                    Log.Trace("");
                    Log.Trace("SUBMITTED: " + args);
                    Log.Trace("");
                    requiredStatusEvent.Set();
                }
                // make sure we fire the status we're expecting
                if (args.Status == expectedStatus)
                {
                    Log.Trace("");
                    Log.Trace("EXPECTED: " + args);
                    Log.Trace("");
                    desiredStatusEvent.Set();
                }
            };

            Brokerage.OrderStatusChanged += brokerageOnOrderStatusChanged;

            OrderProvider.Add(order);
            if (!Brokerage.PlaceOrder(order) && !allowFailedSubmission)
            {
                Assert.Fail("Brokerage failed to place the order: " + order);
            }


            requiredStatusEvent.WaitOneAssertFail((int)(1000 * secondsTimeout), "Expected every order to fire a submitted or invalid status event");
            desiredStatusEvent.WaitOneAssertFail((int)(1000 * secondsTimeout), "OrderStatus " + expectedStatus + " was not encountered within the timeout. Order Id:" + order.Id);
            

            Brokerage.OrderStatusChanged -= brokerageOnOrderStatusChanged;

            return order;
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

        /// <summary>
        /// Updates the specified order in the brokerage until it fills or reaches a timeout
        /// </summary>
        /// <param name="order">The order to be modified</param>
        /// <param name="parameters">The order test parameters that define how to modify the order</param>
        /// <param name="secondsTimeout">Maximum amount of time to wait until the order fills</param>
        protected void ModifyOrderUntilFilled(Order order, OrderTestParameters parameters, double secondsTimeout = 90)
        {
            if (order.Status == OrderStatus.Filled)
            {
                return;
            }

            var filledResetEvent = new ManualResetEvent(false);
            EventHandler<OrderEvent> brokerageOnOrderStatusChanged = (sender, args) =>
            {
                if (args.Status == OrderStatus.Filled)
                {
                    filledResetEvent.Set();
                }
                if (args.Status == OrderStatus.Canceled || args.Status == OrderStatus.Invalid)
                {
                    Log.Trace("ModifyOrderUntilFilled(): " + order);
                    Assert.Fail("Unexpected order status: " + args.Status);
                }
            };

            Brokerage.OrderStatusChanged += brokerageOnOrderStatusChanged;

            Log.Trace("");
            Log.Trace("MODIFY UNTIL FILLED: " + order);
            Log.Trace("");
            var stopwatch = Stopwatch.StartNew();
            while (!filledResetEvent.WaitOne(3000) && stopwatch.Elapsed.TotalSeconds < secondsTimeout)
            {
                filledResetEvent.Reset();
                if (order.Status == OrderStatus.PartiallyFilled) continue;

                var marketPrice = GetAskPrice(order.Symbol);
                Log.Trace("BrokerageTests.ModifyOrderUntilFilled(): Ask: " + marketPrice);

                var updateOrder = parameters.ModifyOrderToFill(Brokerage, order, marketPrice);
                if (updateOrder)
                {
                    if (order.Status == OrderStatus.Filled) break;

                    Log.Trace("BrokerageTests.ModifyOrderUntilFilled(): " + order);
                    if (!Brokerage.UpdateOrder(order))
                    {
                        Assert.Fail("Brokerage failed to update the order");
                    }
                }
            }

            Brokerage.OrderStatusChanged -= brokerageOnOrderStatusChanged;
        }

        /// <summary>
        /// Returns whether or not the brokers order cancel method implementation is async
        /// </summary>
        protected bool IsCancelAsync()
        {
            return false;
        }


    }
}
