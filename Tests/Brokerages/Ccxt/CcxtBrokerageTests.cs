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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using QuantConnect.Brokerages.Ccxt;
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Python;
using QuantConnect.Util;

namespace QuantConnect.Tests.Brokerages.Ccxt
{
    [TestFixture, Explicit("These tests require configuration and funded accounts on selected crypto exchanges.")]
    public class CcxtBrokerageTests
    {
        private readonly List<Order> _orders = new();

        [SetUp]
        public void Setup()
        {
            Log.LogHandler = new NUnitLogHandler();

            PythonInitializer.Initialize();

            // redirect python output
            PySysIo.ToTextWriter(TestContext.Progress);
        }

        [TestCase("binance")]
        [TestCase("bittrex")]
        [TestCase("coinbasepro")]
        [TestCase("ftx")]
        [TestCase("gateio")]
        [TestCase("kraken")]
        public void ClientConnects(string exchangeName)
        {
            using var brokerage = CreateBrokerage(exchangeName);

            brokerage.Connect();
            Assert.IsTrue(brokerage.IsConnected);
        }

        [TestCase("binance")]
        [TestCase("bittrex")]
        [TestCase("coinbasepro")]
        [TestCase("ftx")]
        [TestCase("gateio")]
        [TestCase("kraken")]
        public void IsConnectedUpdatesCorrectly(string exchangeName)
        {
            using var brokerage = CreateBrokerage(exchangeName);

            brokerage.Connect();
            Assert.IsTrue(brokerage.IsConnected);

            brokerage.Disconnect();
            Assert.IsFalse(brokerage.IsConnected);

            brokerage.Connect();
            Assert.IsTrue(brokerage.IsConnected);
        }

        [TestCase("binance")]
        [TestCase("bittrex")]
        [TestCase("coinbasepro")]
        [TestCase("ftx")]
        [TestCase("gateio")]
        [TestCase("kraken")]
        public void GetCashBalanceContainsSomething(string exchangeName)
        {
            using var brokerage = CreateBrokerage(exchangeName);

            brokerage.Connect();
            Assert.IsTrue(brokerage.IsConnected);

            var balances = brokerage.GetCashBalance();

            foreach (var balance in balances)
            {
                Log.Trace($"Balance {balance.Currency}: {balance.Amount}");
            }

            Assert.IsTrue(balances.Any());
        }

        [TestCase("binance")]
        [TestCase("bittrex")]
        [TestCase("coinbasepro")]
        [TestCase("ftx")]
        [TestCase("gateio")]
        [TestCase("kraken")]
        public void GetsOpenOrders(string exchangeName)
        {
            using var brokerage = CreateBrokerage(exchangeName);

            brokerage.Connect();
            Assert.IsTrue(brokerage.IsConnected);

            var orders = brokerage.GetOpenOrders();

            foreach (var order in orders)
            {
                Log.Trace($"Order: {order}");
            }
        }

        [TestCase("binance")]
        [TestCase("bittrex")]
        [TestCase("coinbasepro")]
        [TestCase("ftx")]
        [TestCase("gateio")]
        [TestCase("kraken")]
        public void CancelsOpenOrders(string exchangeName)
        {
            using var brokerage = CreateBrokerage(exchangeName);

            brokerage.Connect();
            Assert.IsTrue(brokerage.IsConnected);

            var orders = brokerage.GetOpenOrders();

            foreach (var order in orders)
            {
                Log.Trace($"Cancelling order: {order}");
                Assert.IsTrue(brokerage.CancelOrder(order));
            }
        }

        [TestCase("coinbasepro", "BTCEUR", 0.0001)]
        [TestCase("binance", "ETHBTC", 0.01)]
        [TestCase("bittrex", "ETHBTC", -0.002)]
        [TestCase("ftx", "ETHBTC", -0.001)]
        [TestCase("kraken", "ETHBTC", -0.004)]
        public void PlacesMarketOrder(string exchangeName, string leanTicker, decimal quantity)
        {
            using var brokerage = CreateBrokerage(exchangeName);

            brokerage.Connect();
            Assert.IsTrue(brokerage.IsConnected);

            var market = new CcxtSymbolMapper(exchangeName).GetLeanMarket();
            var symbol = Symbol.Create(leanTicker, SecurityType.Crypto, market);

            using var orderFilledResetEvent = new ManualResetEvent(false);

            brokerage.OrderStatusChanged += (_, e) =>
            {
                Log.Trace($"Order event: {e}");

                if (e.Status == OrderStatus.Filled)
                {
                    orderFilledResetEvent.Set();
                }
            };

            Log.Trace("Submitting market order");
            var order = new MarketOrder(symbol, quantity, DateTime.UtcNow);
            _orders.Add(order);
            brokerage.PlaceOrder(order);

            // TODO: no order events in unit tests?
            //Assert.IsTrue(orderFilledResetEvent.WaitOne(5000));
        }

        [TestCase("coinbasepro", "BTCEUR", 0.0001, 10000)]
        [TestCase("binance", "ETHBTC", 0.01, 0.03)]
        [TestCase("bittrex", "ETHBTC", -0.002, 0.09999999)]
        [TestCase("ftx", "ETHBTC", -0.001, 0.0999925)]
        [TestCase("gateio", "ETHBTC", -0.002, 0.099999)]
        [TestCase("kraken", "ETHBTC", -0.004, 0.09999)]
        public void PlacesLimitOrder(string exchangeName, string leanTicker, decimal quantity, decimal limitPrice)
        {
            using var brokerage = CreateBrokerage(exchangeName);

            brokerage.Connect();
            Assert.IsTrue(brokerage.IsConnected);

            var market = new CcxtSymbolMapper(exchangeName).GetLeanMarket();
            var symbol = Symbol.Create(leanTicker, SecurityType.Crypto, market);

            using var orderSubmittedResetEvent = new ManualResetEvent(false);

            brokerage.OrderStatusChanged += (_, e) =>
            {
                Log.Trace($"Order event: {e}");

                if (e.Status == OrderStatus.Submitted)
                {
                    orderSubmittedResetEvent.Set();
                }
            };

            Log.Trace("Submitting limit order");
            var order = new LimitOrder(symbol, quantity, limitPrice, DateTime.UtcNow);
            _orders.Add(order);
            brokerage.PlaceOrder(order);

            // TODO: no order events in unit tests?
            //Assert.IsTrue(orderSubmittedResetEvent.WaitOne(5000));
        }

        [TestCase("bittrex", "ETHBTC", -0.002, 0.03)]
        [TestCase("ftx", "ETHBTC", -0.001, 0.03)]
        [TestCase("kraken", "ETHBTC", -0.004, 0.03)]
        public void PlacesStopMarketOrder(string exchangeName, string leanTicker, decimal quantity, decimal stopPrice)
        {
            using var brokerage = CreateBrokerage(exchangeName);

            brokerage.Connect();
            Assert.IsTrue(brokerage.IsConnected);

            var market = new CcxtSymbolMapper(exchangeName).GetLeanMarket();
            var symbol = Symbol.Create(leanTicker, SecurityType.Crypto, market);

            using var orderSubmittedResetEvent = new ManualResetEvent(false);

            brokerage.OrderStatusChanged += (_, e) =>
            {
                Log.Trace($"Order event: {e}");

                if (e.Status == OrderStatus.Submitted)
                {
                    orderSubmittedResetEvent.Set();
                }
            };

            Log.Trace("Submitting stop market order");
            var order = new StopMarketOrder(symbol, quantity, stopPrice, DateTime.UtcNow);
            _orders.Add(order);
            brokerage.PlaceOrder(order);

            // TODO: no order events in unit tests?
            //Assert.IsTrue(orderSubmittedResetEvent.WaitOne(5000));
        }

        [TestCase("coinbasepro", "BTCEUR", -0.0001, 10000, 10000)]
        [TestCase("binance", "ETHBTC", -0.01, 0.03, 0.03)]
        [TestCase("ftx", "ETHBTC", -0.001, 0.03, 0.03)]
        [TestCase("kraken", "ETHBTC", -0.004, 0.03, 0.03)]
        public void PlacesStopLimitOrder(string exchangeName, string leanTicker, decimal quantity, decimal stopPrice, decimal limitPrice)
        {
            using var brokerage = CreateBrokerage(exchangeName);

            brokerage.Connect();
            Assert.IsTrue(brokerage.IsConnected);

            var market = new CcxtSymbolMapper(exchangeName).GetLeanMarket();
            var symbol = Symbol.Create(leanTicker, SecurityType.Crypto, market);

            using var orderSubmittedResetEvent = new ManualResetEvent(false);

            brokerage.OrderStatusChanged += (_, e) =>
            {
                Log.Trace($"Order event: {e}");

                if (e.Status == OrderStatus.Submitted)
                {
                    orderSubmittedResetEvent.Set();
                }
            };

            Log.Trace("Submitting stop limit order");
            var order = new StopLimitOrder(symbol, quantity, stopPrice, limitPrice, DateTime.UtcNow);
            _orders.Add(order);
            brokerage.PlaceOrder(order);

            // TODO: no order events in unit tests?
            //Assert.IsTrue(orderSubmittedResetEvent.WaitOne(5000));
        }

        [TestCase("binance", "BTCUSDT", TickType.Trade)]
        [TestCase("binance", "BTCUSDT", TickType.Quote)]
        [TestCase("bittrex", "BTCUSD", TickType.Trade)]     // TODO: failing
        [TestCase("bittrex", "BTCUSD", TickType.Quote)]     // TODO: failing
        [TestCase("coinbasepro", "BTCUSD", TickType.Trade)]
        [TestCase("coinbasepro", "BTCUSD", TickType.Quote)]
        [TestCase("ftx", "BTCUSD", TickType.Trade)]
        [TestCase("ftx", "BTCUSD", TickType.Quote)]
        [TestCase("gateio", "ETHBTC", TickType.Trade)]
        [TestCase("gateio", "ETHBTC", TickType.Quote)]
        [TestCase("kraken", "BTCUSD", TickType.Trade)]
        [TestCase("kraken", "BTCUSD", TickType.Quote)]
        public void ReceivesMarketData(string exchangeName, string ticker, TickType tickType)
        {
            Log.Trace("Starting test ReceivesMarketData");

            using var brokerage = CreateBrokerage(exchangeName);

            brokerage.Connect();
            Assert.IsTrue(brokerage.IsConnected);

            var market = new CcxtSymbolMapper(exchangeName).GetLeanMarket();
            var symbol = Symbol.Create(ticker, SecurityType.Crypto, market);

            var config = new SubscriptionDataConfig(
                typeof(Tick),
                symbol,
                Resolution.Tick,
                TimeZones.Utc,
                TimeZones.Utc,
                true,
                true,
                false,
                false,
                tickType);

            var ticksReceived = 0;
            using var cancellationToken = new CancellationTokenSource();

            ProcessFeed(
                brokerage.Subscribe(config, (_, _) => { }),
                cancellationToken,
                data =>
                {
                    if (data is Tick tick)
                    {
                        Log.Trace($"New tick: {tick}");
                        ticksReceived++;
                    }
                });

            Thread.Sleep(10000);

            Log.Trace($"Ticks received: {ticksReceived}");
            Assert.That(ticksReceived > 0);
        }

        private CcxtBrokerage CreateBrokerage(string exchangeName)
        {
            Config.Set("ccxt-exchange-name", exchangeName);

            using var brokerageFactory = new CcxtBrokerageFactory();

            var brokerageData = brokerageFactory.BrokerageData;

            Log.Trace($"Creating CCXT brokerage for exchange: {exchangeName}");

            var apiKey = brokerageData[$"ccxt-{exchangeName}-api-key"];
            var secret = brokerageData[$"ccxt-{exchangeName}-secret"];
            var password = brokerageData[$"ccxt-{exchangeName}-password"];

            return new CcxtBrokerage(
                new OrderProvider(_orders),
                exchangeName,
                apiKey,
                secret,
                password,
                Composer.Instance.GetExportedValueByTypeName<IDataAggregator>(
                    Config.Get("data-aggregator", "QuantConnect.Lean.Engine.DataFeeds.AggregationManager")));
        }

        protected void ProcessFeed(IEnumerator<BaseData> enumerator, CancellationTokenSource cancellationToken, Action<BaseData> callback = null)
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
