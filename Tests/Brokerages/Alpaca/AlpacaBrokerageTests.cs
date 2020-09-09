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
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using QuantConnect.Brokerages.Alpaca;
using QuantConnect.Configuration;
using QuantConnect.Interfaces;
using QuantConnect.Orders;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Brokerages.Alpaca
{
    [TestFixture, Explicit("This test requires a configured and testable Alpaca practice account")]
    public class AlpacaBrokerageTests : BrokerageTests
    {
        /// <summary>
        /// Creates the brokerage under test and connects it
        /// </summary>
        /// <returns>A connected brokerage instance</returns>
        protected override IBrokerage CreateBrokerage(IOrderProvider orderProvider, ISecurityProvider securityProvider)
        {
            var keyId = Config.Get("alpaca-key-id");
            var secretKey = Config.Get("alpaca-secret-key");
            var tradingMode = Config.Get("alpaca-trading-mode");

            return new AlpacaBrokerage(orderProvider, securityProvider, keyId, secretKey, tradingMode);
        }

        /// <summary>
        /// Disposes of the brokerage and any external resources started in order to create it
        /// </summary>
        /// <param name="brokerage">The brokerage instance to be disposed of</param>
        protected override void DisposeBrokerage(IBrokerage brokerage)
        {
            brokerage.Disconnect();
            brokerage.Dispose();
        }

        /// <summary>
        /// Provides the data required to test each order type in various cases
        /// </summary>
        private static TestCaseData[] OrderParameters => new[]
        {
            new TestCaseData(new MarketOrderTestParameters(Symbols.SPY)).SetName("MarketOrder"),
            new TestCaseData(new NonUpdateableLimitOrderTestParameters(Symbols.SPY, 1000m,  0.1m)).SetName("LimitOrder"),
            new TestCaseData(new NonUpdateableStopMarketOrderTestParameters(Symbols.SPY, 1000m,  0.1m)).SetName("StopMarketOrder")
        };

        /// <summary>
        /// Gets the symbol to be traded, must be shortable
        /// </summary>
        protected override Symbol Symbol => Symbols.SPY;

        /// <summary>
        /// Gets the security type associated with the <see cref="BrokerageTests.Symbol" />
        /// </summary>
        protected override SecurityType SecurityType => Symbol.SecurityType;

        /// <summary>
        /// Returns whether or not the brokers order methods implementation are async
        /// </summary>
        protected override bool IsAsync()
        {
            return false;
        }

        /// <summary>
        /// Returns whether or not the brokers order cancel method implementation is async
        /// </summary>
        protected override bool IsCancelAsync()
        {
            return true;
        }

        /// <summary>
        /// Gets the current market price of the specified security
        /// </summary>
        protected override decimal GetAskPrice(Symbol symbol)
        {
            var alpaca = (AlpacaBrokerage)Brokerage;
            var quote = alpaca.GetRates(symbol.Value);
            return quote.AskPrice;
        }

        [Test, TestCaseSource(nameof(OrderParameters))]
        public override void CancelOrders(OrderTestParameters parameters)
        {
            base.CancelOrders(parameters);
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
            // https://github.com/alpacahq/Alpaca-API/issues/90
            Assert.Ignore("Alpaca brokerage does not currently support reversing a position with a single order.");
        }

        [Test, TestCaseSource(nameof(OrderParameters))]
        public override void LongFromShort(OrderTestParameters parameters)
        {
            // https://github.com/alpacahq/Alpaca-API/issues/90
            Assert.Ignore("Alpaca brokerage does not currently support reversing a position with a single order.");
        }

        [Test]
        public void ValidateMarketOrders()
        {
            var orderEventTracker = new ConcurrentBag<OrderEvent>();
            var alpaca = (AlpacaBrokerage)Brokerage;
            var symbol = Symbol;
            EventHandler<OrderEvent> orderStatusChangedCallback = (s, e) =>
            {
                orderEventTracker.Add(e);
            };
            alpaca.OrderStatusChanged += orderStatusChangedCallback;
            const int numberOfOrders = 2;
            for (var i = 0; i < numberOfOrders; i++)
            {
                var order = new MarketOrder(symbol, 10, DateTime.UtcNow);
                OrderProvider.Add(order);
                Console.WriteLine("Buy Order");
                alpaca.PlaceOrder(order);

                var orderr = new MarketOrder(symbol, -10, DateTime.UtcNow);
                OrderProvider.Add(orderr);
                Console.WriteLine("Sell Order");
                alpaca.PlaceOrder(orderr);
            }

            // We want to verify the number of order events with OrderStatus.Filled sent
            Thread.Sleep(14000);
            alpaca.OrderStatusChanged -= orderStatusChangedCallback;
            Assert.AreEqual(orderEventTracker.Count(x => x.Status == OrderStatus.Submitted), numberOfOrders * 2);
            Assert.AreEqual(orderEventTracker.Count(x => x.Status == OrderStatus.Filled), numberOfOrders * 2);
        }

        [Test]
        public void ValidateLimitOrders()
        {
            var orderEventTracker = new ConcurrentBag<OrderEvent>();
            var alpaca = (AlpacaBrokerage)Brokerage;
            var symbol = Symbol;
            var lastPrice = GetLastPrice(symbol.Value);
            EventHandler<OrderEvent> orderStatusChangedCallback = (s, e) =>
            {
                orderEventTracker.Add(e);
            };
            alpaca.OrderStatusChanged += orderStatusChangedCallback;

            // Buy Limit order above market - should be filled immediately
            var limitPrice = lastPrice + 0.5m;
            var order = new LimitOrder(symbol, 1, limitPrice, DateTime.UtcNow);
            OrderProvider.Add(order);
            Assert.IsTrue(alpaca.PlaceOrder(order));

            Thread.Sleep(10000);

            alpaca.OrderStatusChanged -= orderStatusChangedCallback;
            Assert.AreEqual(orderEventTracker.Count(x => x.Status == OrderStatus.Submitted), 1);
            Assert.AreEqual(orderEventTracker.Count(x => x.Status == OrderStatus.Filled), 1);
        }

        [Test]
        public void ValidateStopMarketOrders()
        {
            var alpaca = (AlpacaBrokerage)Brokerage;
            var symbol = Symbol;
            var lastPrice = GetLastPrice(symbol.Value);

            // Buy StopMarket order below market
            var price = lastPrice - 0.5m;
            var order = new StopMarketOrder(symbol, 1, price, DateTime.UtcNow);
            Assert.IsTrue(alpaca.PlaceOrder(order));

            // Buy StopMarket order above market
            price = lastPrice + 0.5m;
            order = new StopMarketOrder(symbol, 1, price, DateTime.UtcNow);
            Assert.IsTrue(alpaca.PlaceOrder(order));

            // Sell StopMarket order below market
            price = lastPrice - 0.5m;
            order = new StopMarketOrder(symbol, -1, price, DateTime.UtcNow);
            Assert.IsTrue(alpaca.PlaceOrder(order));

            // Sell StopMarket order above market
            price = lastPrice + 0.5m;
            order = new StopMarketOrder(symbol, -1, price, DateTime.UtcNow);
            Assert.IsTrue(alpaca.PlaceOrder(order));
        }

        [Test]
        public void ValidateStopLimitOrders()
        {
            var alpaca = (AlpacaBrokerage)Brokerage;
            var symbol = Symbol;
            var lastPrice = GetLastPrice(symbol.Value);

            // Buy StopLimit order below market
            var stopPrice = lastPrice - 0.5m;
            var limitPrice = stopPrice + 0.05m;
            var order = new StopLimitOrder(symbol, 1, stopPrice, limitPrice, DateTime.UtcNow);
            Assert.IsTrue(alpaca.PlaceOrder(order));

            // Buy StopLimit order above market
            stopPrice = lastPrice + 0.5m;
            limitPrice = stopPrice + 0.05m;
            order = new StopLimitOrder(symbol, 1, stopPrice, limitPrice, DateTime.UtcNow);
            Assert.IsTrue(alpaca.PlaceOrder(order));

            // In case there is no position, the following sell orders would not be placed
            // So build a position for them.
            var marketOrder = new MarketOrder(symbol, 2, DateTime.UtcNow);
            Assert.IsTrue(alpaca.PlaceOrder(marketOrder));

            Thread.Sleep(20000);
            // Sell StopLimit order below market
            stopPrice = lastPrice - 0.5m;
            limitPrice = stopPrice - 0.05m;
            order = new StopLimitOrder(symbol, -1, stopPrice, limitPrice, DateTime.UtcNow);
            Assert.IsTrue(alpaca.PlaceOrder(order));

            // Sell StopLimit order above market
            stopPrice = lastPrice + 0.5m;
            limitPrice = stopPrice - 0.05m;
            order = new StopLimitOrder(symbol, -1, stopPrice, limitPrice, DateTime.UtcNow);
            Assert.IsTrue(alpaca.PlaceOrder(order));
        }

        [Test]
        public void IsConnectedUpdatesCorrectly()
        {
            var brokerage = Brokerage;
            Assert.IsTrue(brokerage.IsConnected);

            brokerage.Disconnect();
            Assert.IsFalse(brokerage.IsConnected);

            brokerage.Connect();
            Assert.IsTrue(brokerage.IsConnected);
        }

        [Test, Ignore("This test requires disconnecting the internet to test for connection resiliency")]
        public void ClientReconnectsAfterInternetDisconnect()
        {
            var brokerage = Brokerage;
            Assert.IsTrue(brokerage.IsConnected);

            var tenMinutes = TimeSpan.FromMinutes(10);

            Console.WriteLine("------");
            Console.WriteLine("Waiting for internet disconnection ");
            Console.WriteLine("------");

            // spin while we manually disconnect the internet
            while (brokerage.IsConnected)
            {
                Thread.Sleep(2500);
                Console.Write(".");
            }

            var stopwatch = Stopwatch.StartNew();

            Console.WriteLine("------");
            Console.WriteLine("Trying to reconnect ");
            Console.WriteLine("------");

            // spin until we're reconnected
            while (!brokerage.IsConnected && stopwatch.Elapsed < tenMinutes)
            {
                Thread.Sleep(2500);
                Console.Write(".");
            }

            Assert.IsTrue(brokerage.IsConnected);
        }

        private decimal GetLastPrice(string ticker)
        {
            // Get a free API key from here: https://www.alphavantage.co/support/#api-key
            var apiKey = Config.Get("alpha-vantage-api-key");
            var url = $"https://www.alphavantage.co/query?function=GLOBAL_QUOTE&symbol={ticker}&apikey={apiKey}";
            using (var wc = new WebClient())
            {
                var json = wc.DownloadString(url);
                return Convert.ToDecimal(JObject.Parse(json)["Global Quote"]["05. price"], CultureInfo.InvariantCulture);
            }
        }
    }
}
