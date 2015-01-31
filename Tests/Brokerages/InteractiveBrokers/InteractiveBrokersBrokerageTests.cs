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
using System.Linq;
using System.Threading;
using NUnit.Framework;
using QuantConnect.Brokerages.InteractiveBrokers;
using QuantConnect.Configuration;
using QuantConnect.Orders;

namespace QuantConnect.Tests.Brokerages.InteractiveBrokers
{
    [TestFixture]
    [Ignore("These tests require the IBController and IB TraderWorkstation to be installed.")]
    public class InteractiveBrokersBrokerageTests
    {
        [SetUp]
        public void InitializeGateway()
        {
            InteractiveBrokersGatewayRunner.Start(Config.Get("ib-account"));
        }

        [TearDown]
        public void KillGateway()
        {
            InteractiveBrokersGatewayRunner.Stop();
            Thread.Sleep(250);
        }

        [Test]
        public void ClientConnects()
        {
            var ib = new InteractiveBrokersBrokerage();
            ib.Connect();
        }

        [Test]
        public void ClientPlacesMarketOrder()
        {
            bool orderFilled = false;
            var manualResetEvent = new ManualResetEvent(false);
            var ib = new InteractiveBrokersBrokerage();
            ib.Connect();

            ib.Client.RequestOpenOrders();

            ib.OrderEvent += (sender, args) =>
            {
                orderFilled = true;
                manualResetEvent.Set();
            };
            
            const int buyQuantity = 1;
            //ib.PlaceOrder(new Order("AAPL", SecurityType.Equity, buyQuantity, OrderType.Market, DateTime.Now));
            var order = new Order("AAPL", SecurityType.Equity, buyQuantity, OrderType.Market, DateTime.Now);
            ib.PlaceOrder(order);

            manualResetEvent.WaitOne(2500);
            var orderFromIB = AssertOrderOpened(orderFilled, ib, order);
            Assert.AreEqual(OrderType.Market, orderFromIB.Type);
        }

        [Test]
        public void ClientSellsMarketOrder()
        {
            bool orderFilled = false;
            var manualResetEvent = new ManualResetEvent(false);

            var ib = new InteractiveBrokersBrokerage();
            ib.Connect();

            ib.OrderEvent += (sender, args) =>
            {
                orderFilled = true;
                manualResetEvent.Set();
            };

            // sell a single share
            var order = new Order("AAPL", SecurityType.Equity, -1, OrderType.Market, DateTime.UtcNow);
            ib.PlaceOrder(order);

            manualResetEvent.WaitOne(2500);

            var orderFromIB = AssertOrderOpened(orderFilled, ib, order);
            Assert.AreEqual(OrderType.Market, orderFromIB.Type);
        }

        [Test]
        public void ClientPlacesLimitOrder()
        {
            bool orderFilled = false;
            var manualResetEvent = new ManualResetEvent(false);
            var ib = new InteractiveBrokersBrokerage();
            ib.Connect();

            decimal aapl = 100m;
            decimal delta = 85.0m; // if we can't get a price then make the delta huge
            ib.OrderEvent += (sender, args) =>
            {
                orderFilled = true;
                aapl = args.FillPrice;
                delta = 0.02m;
                manualResetEvent.Set();
            };

            // get the current market price, couldn't get RequestMarketData to fire tick events
            int id = 0;
            ib.PlaceOrder(new Order("AAPL", SecurityType.Equity, 1, OrderType.Market, DateTime.UtcNow) { Id = ++id });

            manualResetEvent.WaitOne(2000);
            manualResetEvent.Reset();

            // make a box around the current price +- a little

            const int quantity = 1;
            var order = new Order("AAPL", SecurityType.Equity, +quantity, OrderType.Limit, DateTime.Now, aapl - delta) {Id = ++id};
            ib.PlaceOrder(order);

            ib.PlaceOrder(new Order("AAPL", SecurityType.Equity, -quantity, OrderType.Limit, DateTime.Now, aapl + delta) {Id = ++id});

            manualResetEvent.WaitOne(1000);

            var orderFromIB = AssertOrderOpened(orderFilled, ib, order);
            Assert.AreEqual(OrderType.Limit, orderFromIB.Type);
        }

        [Test]
        public void ClientPlacesStopLimitOrder()
        {
            bool orderFilled = false;
            var manualResetEvent = new ManualResetEvent(false);
            var ib = new InteractiveBrokersBrokerage();
            ib.Connect();

            decimal aapl = 100m;
            decimal delta = 85.0m; // if we can't get a price then make the delta huge
            ib.OrderEvent += (sender, args) =>
            {
                orderFilled = true;
                aapl = args.FillPrice;
                delta = 0.02m;
                manualResetEvent.Set();
            };

            // get the current market price, couldn't get RequestMarketData to fire tick events
            int id = 0;
            ib.PlaceOrder(new Order("AAPL", SecurityType.Equity, 1, OrderType.Market, DateTime.UtcNow) { Id = ++id });

            manualResetEvent.WaitOne(2000);
            manualResetEvent.Reset();
            Assert.IsTrue(orderFilled);

            orderFilled = false;

            // make a box around the current price +- a little

            const int quantity = 1;
            var order = new Order("AAPL", SecurityType.Equity, +quantity, OrderType.StopMarket, DateTime.Now, aapl - delta) {Id = ++id};
            ib.PlaceOrder(order);

            ib.PlaceOrder(new Order("AAPL", SecurityType.Equity, -quantity, OrderType.StopMarket, DateTime.Now, aapl + delta) { Id = ++id });

            manualResetEvent.WaitOne(1000);

            var orderFromIB = AssertOrderOpened(orderFilled, ib, order);
            Assert.AreEqual(OrderType.StopMarket, orderFromIB.Type);
        }

        [Test]
        public void ClientCancelsLimitOrder()
        {
            OrderStatus status = OrderStatus.New;
            var manualResetEvent = new ManualResetEvent(false);

            var ib = new InteractiveBrokersBrokerage();
            ib.Connect();

            ib.OrderEvent += (sender, args) =>
            {
                status = args.Status;
                manualResetEvent.Set();
            };

            // try to sell a single share at a ridiculous price, we'll cancel this later
            var order = new Order("AAPL", SecurityType.Equity, -1, OrderType.Limit, DateTime.UtcNow, 100000);
            ib.PlaceOrder(order);
            manualResetEvent.WaitOne(2500);

            ib.CancelOrder(order);

            manualResetEvent.Reset();
            manualResetEvent.WaitOne(2500);

            Assert.AreEqual(OrderStatus.Canceled, status);
        }

        private static Order AssertOrderOpened(bool orderFilled, QuantConnect.Brokerages.InteractiveBrokers.InteractiveBrokersBrokerage ib, Order order)
        {
            // if the order didn't fill check for it as an open order
            if (!orderFilled)
            {
                // find the right order and return it
                foreach (var openOrder in ib.GetOpenOrders())
                {
                    if (openOrder.BrokerId.Any(id => order.BrokerId.Any(x => x == id)))
                    {
                        return openOrder;
                    }
                }
                Assert.Fail("The order was not filled and was unable to be located via GetOpenOrders()");
            }

            Assert.Pass("The order was successfully filled!");
            return null;
        }
    }
}