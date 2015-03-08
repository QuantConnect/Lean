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
using NUnit.Framework;
using QuantConnect.Brokerages.InteractiveBrokers;
using QuantConnect.Orders;

namespace QuantConnect.Tests.Brokerages.InteractiveBrokers
{
    [TestFixture]
    [Ignore("These tests require the IBController and IB TraderWorkstation to be installed.")]
    public class InteractiveBrokersBrokerageTests
    {
        private readonly object _lock = new object();
        private InteractiveBrokersBrokerage _interactiveBrokersBrokerage;
        private const string Symbol = "USDJPY";
        private const SecurityType Type = SecurityType.Forex;

        [SetUp]
        public void InitializeBrokerage()
        {
            // force only a single test to run at time
            Monitor.Enter(_lock);

            // grabs account info from configuration
            _interactiveBrokersBrokerage = new InteractiveBrokersBrokerage();
            _interactiveBrokersBrokerage.Connect();
        }

        [TearDown]
        public void CancelOpenOrders()
        {
            try
            {
                var manualResetEvent = new ManualResetEvent(false);
                _interactiveBrokersBrokerage.OrderEvent += (sender, orderEvent) =>
                {
                    if (orderEvent.Status == OrderStatus.Canceled)
                    {
                        manualResetEvent.Set();
                    }
                };

                var orders = _interactiveBrokersBrokerage.GetOpenOrders();
                foreach (var order in orders)
                {
                    _interactiveBrokersBrokerage.CancelOrder(order);
                    manualResetEvent.WaitOne(1500);
                    manualResetEvent.Reset();
                }

                Assert.AreEqual(0, _interactiveBrokersBrokerage.GetOpenOrders().Count);

                _interactiveBrokersBrokerage.Dispose();
                _interactiveBrokersBrokerage = null;
            }
            finally
            {
                // force only a single test to run at a time
                Monitor.Exit(_lock);
            }
        }

        [Test]
        public void ClientConnects()
        {
            var ib = _interactiveBrokersBrokerage;
            Assert.IsTrue(ib.IsConnected);
        }

        [Test]
        public void PlacedOrderHasNewBrokerageOrderID()
        {
            var ib = _interactiveBrokersBrokerage;

            var order = new MarketOrder(Symbol, 1, DateTime.Now, type: Type);
            ib.PlaceOrder(order);

            var brokerageID = order.BrokerId.Single();
            Assert.AreNotEqual(0, brokerageID);

            order = new MarketOrder(Symbol, 1, DateTime.Now, type: Type);
            ib.PlaceOrder(order);

            Assert.AreNotEqual(brokerageID, order.BrokerId.Single());
        }

        [Test]
        public void ClientPlacesMarketOrder()
        {
            bool orderFilled = false;
            var manualResetEvent = new ManualResetEvent(false);
            var ib = _interactiveBrokersBrokerage;

            ib.OrderEvent += (sender, orderEvent) =>
            {
                if (orderEvent.Status == OrderStatus.Filled)
                {
                    orderFilled = true;
                    manualResetEvent.Set();
                }
            };
            
            const int buyQuantity = 1;
            var order = new MarketOrder(Symbol, buyQuantity, DateTime.Now, type: Type);
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

            var ib = _interactiveBrokersBrokerage;

            ib.OrderEvent += (sender, orderEvent) =>
            {
                if (orderEvent.Status == OrderStatus.Filled)
                {
                    orderFilled = true;
                    manualResetEvent.Set();
                }
            };

            // sell a single share
            var order = new MarketOrder(Symbol, -1, DateTime.UtcNow, type: Type);
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
            var ib = _interactiveBrokersBrokerage;

            decimal aapl = 100m;
            decimal delta = 85.0m; // if we can't get a price then make the delta huge
            ib.OrderEvent += (sender, orderEvent) =>
            {
                if (orderEvent.Status == OrderStatus.Filled)
                {
                    orderFilled = true;
                    manualResetEvent.Set();
                }
                aapl = orderEvent.FillPrice;
                delta = 0.02m;
            };

            // get the current market price, couldn't get RequestMarketData to fire tick events
            int id = 0;
            ib.PlaceOrder(new MarketOrder(Symbol, 1, DateTime.UtcNow, type: Type) { Id = ++id });

            manualResetEvent.WaitOne(2000);
            manualResetEvent.Reset();

            // make a box around the current price +- a little

            const int quantity = 1;
            var order = new LimitOrder(Symbol, +quantity, aapl - delta, DateTime.Now, null, Type) { Id = ++id };
            ib.PlaceOrder(order);

            ib.PlaceOrder(new LimitOrder(Symbol, -quantity, aapl + delta, DateTime.Now, null, Type) { Id = ++id });

            manualResetEvent.WaitOne(1000);

            var orderFromIB = AssertOrderOpened(orderFilled, ib, order);
            Assert.AreEqual(OrderType.Limit, orderFromIB.Type);
        }

        [Test]
        public void ClientPlacesStopLimitOrder()
        {
            bool orderFilled = false;
            var manualResetEvent = new ManualResetEvent(false);
            var ib = _interactiveBrokersBrokerage;

            decimal fillPrice = 100m;
            decimal delta = 85.0m; // if we can't get a price then make the delta huge
            ib.OrderEvent += (sender, args) =>
            {
                orderFilled = true;
                fillPrice = args.FillPrice;
                delta = 0.02m;
                manualResetEvent.Set();
            };

            // get the current market price, couldn't get RequestMarketData to fire tick events
            int id = 0;
            ib.PlaceOrder(new MarketOrder(Symbol, 1, DateTime.UtcNow, type: Type) { Id = ++id });

            manualResetEvent.WaitOne(2000);
            manualResetEvent.Reset();
            Assert.IsTrue(orderFilled);

            orderFilled = false;

            // make a box around the current price +- a little

            const int quantity = 1;
            var order = new StopMarketOrder(Symbol, +quantity, fillPrice - delta, DateTime.Now, type: Type) { Id = ++id };
            ib.PlaceOrder(order);

            ib.PlaceOrder(new StopMarketOrder(Symbol, -quantity, fillPrice + delta, DateTime.Now, type: Type) { Id = ++id });

            manualResetEvent.WaitOne(1000);

            var orderFromIB = AssertOrderOpened(orderFilled, ib, order);
            Assert.AreEqual(OrderType.StopMarket, orderFromIB.Type);
        }

        [Test]
        public void ClientCancelsLimitOrder()
        {
            OrderStatus status = OrderStatus.New;
            var manualResetEvent = new ManualResetEvent(false);

            var ib = _interactiveBrokersBrokerage;

            ib.OrderEvent += (sender, orderEvent) =>
            {
                if (orderEvent.Status == OrderStatus.Canceled)
                {
                    status = orderEvent.Status;
                    return;
                }
                manualResetEvent.Set();
            };

            // try to sell a single share at a ridiculous price, we'll cancel this later
            var order = new LimitOrder(Symbol, -1, 100000, DateTime.UtcNow, null, Type);
            ib.PlaceOrder(order);
            manualResetEvent.WaitOne(2500);

            ib.CancelOrder(order);

            manualResetEvent.Reset();
            manualResetEvent.WaitOne(2500);

            Assert.AreEqual(OrderStatus.Canceled, status);
        }

        [Test]
        public void GetsAccountHoldings()
        {
            // when running all the tests in this class there seems to be some left overs sometime,
            // so wait a full second for the dust to settle before starting this guy
            Thread.Sleep(1000);

            var ib = _interactiveBrokersBrokerage;
            
            ib.Client.UpdatePortfolio += (sender, args) =>
            {
                Console.WriteLine("Symbol: " + args.Contract.LocalSymbol + " Quantity: " + args.Position);
            };

            var currentHoldings = ib.GetAccountHoldings().ToDictionary(x => x.Symbol);

            Console.WriteLine("Quantity: " + currentHoldings[Symbol].Quantity);

            bool hasSymbol = currentHoldings.ContainsKey(Symbol);

            // wait for order to complete before request account holdings
            var manualResetEvent = new ManualResetEvent(false);
            ib.PortfolioChanged += (sender, portfolioEvent) =>
            {
                manualResetEvent.Set();
            };

            // buy some currency
            const int quantity = 25000;
            var order = new MarketOrder(Symbol, -quantity, DateTime.UtcNow, type: Type);
            ib.PlaceOrder(order);

            // wait for the order to go through
            manualResetEvent.WaitOne(1500);

            // wait a little longer for the account update to be sent
            Thread.Sleep(250);

            var newHoldings = ib.GetAccountHoldings().ToDictionary(x => x.Symbol);
            Console.WriteLine("New Quantity: " + newHoldings[Symbol].Quantity);


            if (hasSymbol)
            {
                Assert.AreEqual(currentHoldings[Symbol].Quantity, newHoldings[Symbol].Quantity + quantity);
            }
            else
            {
                Assert.IsTrue(newHoldings.ContainsKey(Symbol));
                Assert.AreEqual(newHoldings[Symbol].Quantity, quantity);
            }
        }

        [Test]
        public void GetsCashBalanceAfterConnect()
        {
            var ib = _interactiveBrokersBrokerage;
            var cashBalance = ib.GetCashBalance();
            Assert.AreNotEqual(0m, cashBalance);
            
            var manualResetEvent = new ManualResetEvent(false);
            ib.AccountChanged += (sender, orderEvent) =>
            {
                manualResetEvent.Set();
            };
            ib.PlaceOrder(new MarketOrder(Symbol, 25000, new DateTime(), type: Type));
            manualResetEvent.WaitOne(1500);
            
            Thread.Sleep(50);

            Assert.AreNotEqual(cashBalance, ib.GetCashBalance());
        }

        [Test]
        public void FiresMultipleAccountBalanceEvents()
        {
            var ib = _interactiveBrokersBrokerage;

            var orderEventFired = new ManualResetEvent(false);
            ib.OrderEvent += (sender, args) =>
            {
                if (args.Status == OrderStatus.Filled)
                {
                    orderEventFired.Set();
                }
            };

            var cashBalanceUpdates = new List<decimal>();
            var accountChangedFired = new ManualResetEvent(false);
            ib.AccountChanged += (sender, args) =>
            {
                cashBalanceUpdates.Add(args.CashBalance);
                accountChangedFired.Set();
            };

            int orderCount = 3;
            for (int i = 0; i < orderCount; i++)
            {
                var quantity = 25000;
                //if (i%2 == 0) quantity *= -1;
                ib.PlaceOrder(new MarketOrder(Symbol, quantity, new DateTime(), type: Type));
                
                orderEventFired.WaitOne(1500);
                orderEventFired.Reset();

                accountChangedFired.WaitOne(1500);
                accountChangedFired.Reset();
            }

            Assert.AreEqual(orderCount, cashBalanceUpdates.Count);
        }

        [Test]
        public void GetsCashBalanceAfterTrade()
        {
            var ib = _interactiveBrokersBrokerage;
            

            decimal balance = ib.GetCashBalance();

            // wait for our order to fill
            var manualResetEvent = new ManualResetEvent(false);
            ib.AccountChanged += (sender, orderEvent) =>
            {
                manualResetEvent.Set();
            };

            var order = new MarketOrder(Symbol, 1, DateTime.Now, type: Type);
            ib.PlaceOrder(order);

            manualResetEvent.WaitOne(1500);

            decimal balanceAfterTrade = ib.GetCashBalance();

            Console.WriteLine("Pre  trade balance: " + balance);
            Console.WriteLine("Post trade balance: " + balanceAfterTrade);

            Assert.AreNotEqual(balance, balanceAfterTrade);
        }

        [Test]
        public void GetExecutions()
        {
            var ib = _interactiveBrokersBrokerage;

            var orderEventFired = new ManualResetEvent(false);
            ib.OrderEvent += (sender, args) =>
            {
                if (args.Status == OrderStatus.Filled)
                {
                    orderEventFired.Set();
                }
            };

            var order = new MarketOrder(Symbol, 25000, new DateTime(), type: Type);
            ib.PlaceOrder(order);
            orderEventFired.WaitOne(1500);

            var executions = ib.GetExecutions(null, null, null, DateTime.Now, null);
            var execution = executions.OrderByDescending(x => x.Execution.Time).First();

            Assert.AreEqual(Symbol, InteractiveBrokersBrokerage.MapSymbol(execution.Contract));
            Assert.AreEqual(order.BrokerId[0], execution.OrderId);
        }

        private static Order AssertOrderOpened(bool orderFilled, InteractiveBrokersBrokerage ib, Order order)
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