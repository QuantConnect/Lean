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
using QuantConnect.Brokerages.Samco;
using QuantConnect.Configuration;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Tests.Common.Securities;
using System;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace QuantConnect.Tests.Brokerages.Samco
{
    [TestFixture, Ignore("This test requires a configured and active Samco account")]
    public class SamcoBrokerageTests : BrokerageTests
    {

        /// <summary>
        /// Provides the data required to test each order type in various cases
        /// </summary>
        private static TestCaseData[] OrderParameters()
        {
            return new[]
            {
                new TestCaseData(new MarketOrderTestParameters(Symbols.SBIN)).SetName("MarketOrder"),
                new TestCaseData(new LimitOrderTestParameters(Symbols.SBIN, 9.00m, 9.30m)).SetName("LimitOrder"),
                new TestCaseData(new StopMarketOrderTestParameters(Symbols.SBIN, 10.50m,  9.50m)).SetName("StopMarketOrder"),
                new TestCaseData(new StopLimitOrderTestParameters(Symbols.SBIN,  9.00m, 10.50m)).SetName("StopLimitOrder")
            };
        }

        /// <summary>
        /// Creates the brokerage under test
        /// </summary>
        /// <returns>A connected brokerage instance</returns>
        protected override IBrokerage CreateBrokerage(IOrderProvider orderProvider, ISecurityProvider securityProvider)
        {

            var securities = new SecurityManager(new TimeKeeper(DateTime.UtcNow, TimeZones.Kolkata))
            {
                { Symbol, CreateSecurity(Symbol) }
            };

            var transactions = new SecurityTransactionManager(null, securities);
            transactions.SetOrderProcessor(new FakeOrderProcessor());

            var algorithm = new Mock<IAlgorithm>();
            algorithm.Setup(a => a.Transactions).Returns(transactions);
            algorithm.Setup(a => a.BrokerageModel).Returns(new SamcoBrokerageModel());
            algorithm.Setup(a => a.Portfolio).Returns(new SecurityPortfolioManager(securities, transactions));

            var apiSecret = Config.Get("samco.client-password");
            var apiKey = Config.Get("samco.client-id");
            var yob = Config.Get("samco.year-of-birth");
            var tradingSegment = Config.Get("samco.trading-segment");
            var productType = Config.Get("samco.product-type");
            var Samco = new SamcoBrokerage(tradingSegment, productType, apiKey, apiSecret, yob, algorithm.Object, new AggregationManager());

            return Samco;
        }

        /// <summary>
        /// Gets the symbol to be traded, must be shortable
        /// </summary>
        protected override Symbol Symbol => Symbols.SBIN;

        /// <summary>
        /// Gets the security type associated with the <see cref="BrokerageTests.Symbol"/>
        /// </summary>
        protected override SecurityType SecurityType => SecurityType.Equity;

        /// <summary>
        /// Returns wether or not the brokers order methods implementation are async
        /// </summary>
        protected override bool IsAsync()
        {
            return false;
        }

        /// <summary>
        /// Gets the current market price of the specified security
        /// </summary>
        protected override decimal GetAskPrice(Symbol symbol)
        {
            var Samco = (SamcoBrokerage)Brokerage;
            var quotes = Samco.GetQuote(symbol);
            return Convert.ToDecimal(quotes.lastTradedPrice,CultureInfo.InvariantCulture);
        }

        [Test]
        public void ShortIOB()
        {
            PlaceOrderWaitForStatus(new MarketOrder(Symbols.SBIN, -1, DateTime.Now), OrderStatus.Submitted, allowFailedSubmission: true);

            // wait for output to be generated
            Thread.Sleep(20 * 1000);
        }

        [Test, TestCaseSource(nameof(OrderParameters))]
        public override void CancelOrders(OrderTestParameters parameters)
        {
            const int secondsTimeout = 20;
            Log.Trace("");
            Log.Trace("CANCEL ORDERS");
            Log.Trace("");

            var order = PlaceOrderWaitForStatus(parameters.CreateLongOrder(GetDefaultQuantity()), parameters.ExpectedStatus);

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
        }


        [Test]
        public void ValidateStopLimitOrders()
        {
            var Samco = (SamcoBrokerage)Brokerage;
            var symbol = Symbol;
            var lastPrice = GetAskPrice(symbol.Value);

            // Buy StopLimit order below market TODO: This might not work because of the Samco structure. Verify this.
            //var stopPrice = lastPrice - 0.10m; 
            //var limitPrice = stopPrice + 0.10m;
            //var order = new StopLimitOrder(symbol, 1, stopPrice, limitPrice, DateTime.UtcNow, properties: orderProperties);
            //Assert.IsTrue(Samco.PlaceOrder(order));

            // Buy StopLimit order above market
            var stopPrice = lastPrice + 0.20m;
            var limitPrice = stopPrice + 0.25m;
            var order = new StopLimitOrder(symbol, 1, stopPrice, limitPrice, DateTime.UtcNow);
            Assert.IsTrue(Samco.PlaceOrder(order));

            // In case there is no position, the following sell orders would not be placed
            // So build a position for them.
            var marketOrder = new MarketOrder(symbol, 2, DateTime.UtcNow);
            Assert.IsTrue(Samco.PlaceOrder(marketOrder));

            Thread.Sleep(20000);
            // Sell StopLimit order below market
            stopPrice = lastPrice - 0.25m;
            limitPrice = stopPrice - 0.5m;
            order = new StopLimitOrder(symbol, -1, stopPrice, limitPrice, DateTime.UtcNow);
            Assert.IsTrue(Samco.PlaceOrder(order));

            // Sell StopLimit order above market. TODO: This might not work because of the Samco structure. Verify this 
            //stopPrice = lastPrice + 0.5m;
            //limitPrice = stopPrice - 0.25m ;
            //order = new StopLimitOrder(symbol, -1, stopPrice, limitPrice, DateTime.UtcNow, properties: orderProperties);
            //Assert.IsTrue(Samco.PlaceOrder(order));
        }

        [Test, TestCaseSource(nameof(OrderParameters))]
        public override void LongFromZero(OrderTestParameters parameters)
        {
            base.LongFromZero(parameters);
        }

        [Test, TestCaseSource(nameof(OrderParameters))]
        public override void CloseFromLong(OrderTestParameters parameters)
        {
            base.CloseFromLong(parameters);
        }

        [Test, TestCaseSource(nameof(OrderParameters))]
        public override void ShortFromZero(OrderTestParameters parameters)
        {
            base.ShortFromZero(parameters);
        }

        [Test, TestCaseSource(nameof(OrderParameters))]
        public override void CloseFromShort(OrderTestParameters parameters)
        {
            base.CloseFromShort(parameters);
        }

        [Test, TestCaseSource(nameof(OrderParameters))]
        public override void ShortFromLong(OrderTestParameters parameters)
        {
            base.ShortFromLong(parameters);
        }

        [Test, TestCaseSource(nameof(OrderParameters))]
        public override void LongFromShort(OrderTestParameters parameters)
        {
            base.LongFromShort(parameters);
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
            var symbol = Symbols.SBIN;
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

        /// <summary>
        /// Returns whether or not the brokers order cancel method implementation is async
        /// </summary>
        protected override bool IsCancelAsync()
        {
            return false;
        }
    }
}
