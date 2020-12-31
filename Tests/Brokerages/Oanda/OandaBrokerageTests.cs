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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using QuantConnect.Brokerages.Oanda;
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Util;
using Environment = QuantConnect.Brokerages.Oanda.Environment;

namespace QuantConnect.Tests.Brokerages.Oanda
{
    [TestFixture, Ignore("This test requires a configured and testable Oanda practice account")]
    public partial class OandaBrokerageTests : BrokerageTests
    {
        /// <summary>
        ///     Creates the brokerage under test and connects it
        /// </summary>
        /// <returns>A connected brokerage instance</returns>
        protected override IBrokerage CreateBrokerage(IOrderProvider orderProvider, ISecurityProvider securityProvider)
        {
            var environment = Config.Get("oanda-environment").ConvertTo<Environment>();
            var accessToken = Config.Get("oanda-access-token");
            var accountId = Config.Get("oanda-account-id");
            var aggregator = new AggregationManager();

            return new OandaBrokerage(orderProvider, securityProvider, aggregator, environment, accessToken, accountId);
        }

        /// <summary>
        /// Provides the data required to test each order type in various cases
        /// </summary>
        public static TestCaseData[] OrderParameters => new[]
        {
            new TestCaseData(new MarketOrderTestParameters(Symbol.Create("EURUSD", SecurityType.Forex, Market.Oanda))).SetName("MarketOrder"),
            new TestCaseData(new LimitOrderTestParameters(Symbol.Create("EURUSD", SecurityType.Forex, Market.Oanda), 5m, 0.32m)).SetName("LimitOrder"),
            new TestCaseData(new StopMarketOrderTestParameters(Symbol.Create("EURUSD", SecurityType.Forex, Market.Oanda), 5m, 0.32m)).SetName("StopMarketOrder")
        };

        /// <summary>
        ///     Gets the symbol to be traded, must be shortable
        /// </summary>
        protected override Symbol Symbol => Symbol.Create("EURUSD", SecurityType.Forex, Market.Oanda);

        /// <summary>
        ///     Gets the security type associated with the <see cref="BrokerageTests.Symbol" />
        /// </summary>
        protected override SecurityType SecurityType => SecurityType.Forex;

        /// <summary>
        /// Returns wether or not the brokers order methods implementation are async
        /// </summary>
        protected override bool IsAsync()
        {
            return false;
        }

        /// <summary>
        ///     Gets the current market price of the specified security
        /// </summary>
        protected override decimal GetAskPrice(Symbol symbol)
        {
            var oanda = (OandaBrokerage) Brokerage;
            var quote = oanda.GetRates(new OandaSymbolMapper().GetBrokerageSymbol(symbol));
            return quote.AskPrice;
        }
        [Test]
        public void ValidateMarketOrders()
        {
            var orderEventTracker = new ConcurrentBag<OrderEvent>();
            var oanda = (OandaBrokerage)Brokerage;
            var symbol = Symbol;
            EventHandler<OrderEvent> orderStatusChangedCallback = (s, e) => {
                orderEventTracker.Add(e);
            };
            oanda.OrderStatusChanged += orderStatusChangedCallback;
            const int numberOfOrders = 100;
            Parallel.For(0, numberOfOrders, (i) =>
            {
                var order = new MarketOrder(symbol, 100, DateTime.Now);
                OrderProvider.Add(order);
                Assert.IsTrue(oanda.PlaceOrder(order));
                Assert.IsTrue(order.Status == OrderStatus.Filled || order.Status == OrderStatus.PartiallyFilled);
                var orderr = new MarketOrder(symbol, -100, DateTime.UtcNow);
                OrderProvider.Add(orderr);
                Assert.IsTrue(oanda.PlaceOrder(orderr));
                Assert.IsTrue(orderr.Status == OrderStatus.Filled || orderr.Status == OrderStatus.PartiallyFilled);

            });
            // We want to verify the number of order events with OrderStatus.Filled sent
            Thread.Sleep(4000);
            oanda.OrderStatusChanged -= orderStatusChangedCallback;
            Assert.AreEqual(orderEventTracker.Count(x => x.Status == OrderStatus.Submitted), numberOfOrders * 2);
            Assert.AreEqual(orderEventTracker.Count(x => x.Status == OrderStatus.Filled), numberOfOrders * 2);
        }

        [Test]
        public void ValidateLimitOrders()
        {
            var orderEventTracker = new ConcurrentBag<OrderEvent>();
            var oanda = (OandaBrokerage)Brokerage;
            var symbol = Symbol;
            var quote = oanda.GetRates(new OandaSymbolMapper().GetBrokerageSymbol(symbol));
            EventHandler<OrderEvent> orderStatusChangedCallback = (s, e) => {
                orderEventTracker.Add(e);
            };
            oanda.OrderStatusChanged += orderStatusChangedCallback;

            // Buy Limit order below market
            var limitPrice = quote.BidPrice - 0.5m;
            var order = new LimitOrder(symbol, 1, limitPrice, DateTime.Now);
            OrderProvider.Add(order);
            Assert.IsTrue(oanda.PlaceOrder(order));

            // update Buy Limit order with no changes
            Assert.IsTrue(oanda.UpdateOrder(order));

            // move Buy Limit order above market
            order.LimitPrice = quote.AskPrice + 0.5m;
            Assert.IsTrue(oanda.UpdateOrder(order));
            oanda.OrderStatusChanged -= orderStatusChangedCallback;
            Assert.AreEqual(orderEventTracker.Count(x => x.Status == OrderStatus.Submitted), 1);
            Assert.AreEqual(orderEventTracker.Count(x => x.Status == OrderStatus.Filled), 1);
        }

        [Test]
        public void ValidateStopMarketOrders()
        {
            var oanda = (OandaBrokerage)Brokerage;
            var symbol = Symbol;
            var quote = oanda.GetRates(new OandaSymbolMapper().GetBrokerageSymbol(symbol));

            // Buy StopMarket order below market
            var price = quote.BidPrice - 0.5m;
            var order = new StopMarketOrder(symbol, 1, price, DateTime.Now);
            Assert.IsTrue(oanda.PlaceOrder(order));

            // Buy StopMarket order above market
            price = quote.AskPrice + 0.5m;
            order = new StopMarketOrder(symbol, 1, price, DateTime.Now);
            Assert.IsTrue(oanda.PlaceOrder(order));

            // Sell StopMarket order below market
            price = quote.BidPrice - 0.5m;
            order = new StopMarketOrder(symbol, -1, price, DateTime.Now);
            Assert.IsTrue(oanda.PlaceOrder(order));

            // Sell StopMarket order above market
            price = quote.AskPrice + 0.5m;
            order = new StopMarketOrder(symbol, -1, price, DateTime.Now);
            Assert.IsTrue(oanda.PlaceOrder(order));
        }

        [Test]
        public void ValidateStopLimitOrders()
        {
            var oanda = (OandaBrokerage) Brokerage;
            var symbol = Symbol;
            var quote = oanda.GetRates(new OandaSymbolMapper().GetBrokerageSymbol(symbol));

            // Buy StopLimit order below market (Oanda accepts this order but cancels it immediately)
            var stopPrice = quote.BidPrice - 0.5m;
            var limitPrice = stopPrice + 0.0005m;
            var order = new StopLimitOrder(symbol, 1, stopPrice, limitPrice, DateTime.Now);
            Assert.IsTrue(oanda.PlaceOrder(order));

            // Buy StopLimit order above market
            stopPrice = quote.AskPrice + 0.5m;
            limitPrice = stopPrice + 0.0005m;
            order = new StopLimitOrder(symbol, 1, stopPrice, limitPrice, DateTime.Now);
            Assert.IsTrue(oanda.PlaceOrder(order));

            // Sell StopLimit order below market
            stopPrice = quote.BidPrice - 0.5m;
            limitPrice = stopPrice - 0.0005m;
            order = new StopLimitOrder(symbol, -1, stopPrice, limitPrice, DateTime.Now);
            Assert.IsTrue(oanda.PlaceOrder(order));

            // Sell StopLimit order above market (Oanda accepts this order but cancels it immediately)
            stopPrice = quote.AskPrice + 0.5m;
            limitPrice = stopPrice - 0.0005m;
            order = new StopLimitOrder(symbol, -1, stopPrice, limitPrice, DateTime.Now);
            Assert.IsTrue(oanda.PlaceOrder(order));
        }

        [Test, Ignore("This test requires disconnecting the internet to test for connection resiliency")]
        public void ClientReconnectsAfterInternetDisconnect()
        {
            var brokerage = Brokerage;
            Assert.IsTrue(brokerage.IsConnected);

            var tenMinutes = TimeSpan.FromMinutes(10);

            Log.Trace("------");
            Log.Trace("Waiting for internet disconnection ");
            Log.Trace("------");

            // spin while we manually disconnect the internet
            while (brokerage.IsConnected)
            {
                Thread.Sleep(2500);
                Console.Write(".");
            }

            var stopwatch = Stopwatch.StartNew();

            Log.Trace("------");
            Log.Trace("Trying to reconnect ");
            Log.Trace("------");

            // spin until we're reconnected
            while (!brokerage.IsConnected && stopwatch.Elapsed < tenMinutes)
            {
                Thread.Sleep(2500);
                Console.Write(".");
            }

            Assert.IsTrue(brokerage.IsConnected);
        }

        [TestCase("EURUSD", SecurityType.Forex, Market.Oanda, 50000)]
        [TestCase("EURUSD", SecurityType.Forex, Market.Oanda, -50000)]
        [TestCase("WTICOUSD", SecurityType.Cfd, Market.Oanda, 500)]
        [TestCase("WTICOUSD", SecurityType.Cfd, Market.Oanda, -500)]
        public void GetCashBalanceIncludesCurrencySwapsForOpenPositions(string ticker, SecurityType securityType, string market, decimal quantity)
        {
            // This test requires a practice account with GBP account currency

            var brokerage = Brokerage;
            Assert.IsTrue(brokerage.IsConnected);

            var symbol = Symbol.Create(ticker, securityType, market);
            var order = new MarketOrder(symbol, quantity, DateTime.UtcNow);
            PlaceOrderWaitForStatus(order);

            var holdings = brokerage.GetAccountHoldings();
            var balances = brokerage.GetCashBalance();

            Assert.IsTrue(holdings.Count == 1);

            // account currency
            Assert.IsTrue(balances.Any(x => x.Currency == "GBP"));

            if (securityType == SecurityType.Forex)
            {
                // base currency
                var baseCurrencyCash = balances.Single(x => x.Currency == ticker.Substring(0, 3));
                Assert.AreEqual(quantity, baseCurrencyCash.Amount);

                // quote currency
                var quoteCurrencyCash = balances.Single(x => x.Currency == ticker.Substring(3));
                Assert.AreEqual(-Math.Sign(quantity), Math.Sign(quoteCurrencyCash.Amount));
            }
            else if (securityType == SecurityType.Cfd)
            {
                Assert.AreEqual(1, balances.Count);
            }
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
            base.ShortFromLong(parameters);
        }

        [Test, TestCaseSource(nameof(OrderParameters))]
        public override void LongFromShort(OrderTestParameters parameters)
        {
            base.LongFromShort(parameters);
        }
    }
}
