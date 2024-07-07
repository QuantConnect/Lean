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
using Python.Runtime;
using QuantConnect.Algorithm;
using QuantConnect.Brokerages.Backtesting;
using QuantConnect.Data.Market;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Lean.Engine.TransactionHandlers;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Tests.Engine;
using QuantConnect.Tests.Engine.DataFeeds;

namespace QuantConnect.Tests.Common.Securities
{
    [TestFixture]
    public class SecurityTransactionManagerTests
    {
        private IResultHandler _resultHandler;

        [SetUp]
        public void SetUp()
        {
            _resultHandler = new TestResultHandler(Console.WriteLine);
        }

        [Test]
        public void WorksProperlyWithPyObjects()
        {
            var algorithm = new QCAlgorithm();
            algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(algorithm));
            var spySecurity = algorithm.AddEquity("SPY");
            var ibmSecurity = algorithm.AddEquity("IBM");
            algorithm.SetTimeZone(TimeZones.NewYork);
            spySecurity.SetMarketPrice(new Tick { Value = 270m });
            ibmSecurity.SetMarketPrice(new Tick { Value = 270m });
            algorithm.SetFinishedWarmingUp();

            var transactionHandler = new BrokerageTransactionHandler();

            using var backtestingBrokerage = new BacktestingBrokerage(algorithm);
            transactionHandler.Initialize(algorithm, backtestingBrokerage, _resultHandler);
            algorithm.Transactions.SetOrderProcessor(transactionHandler);

            var spy = spySecurity.Symbol;
            var ibm = ibmSecurity.Symbol;

            // this order should timeout (no fills received within 5 seconds)
            algorithm.SetHoldings(spy, 0.5m);
            algorithm.SetHoldings(ibm, 0.5m);

            Func<Order, bool> basicOrderFilter = x => true;
            Func<OrderTicket, bool> basicOrderTicketFilter = x => true;
            using (Py.GIL())
            {
                var orders = algorithm.Transactions.GetOrders(basicOrderFilter.ToPython());
                var orderTickets = algorithm.Transactions.GetOrderTickets(
                    basicOrderTicketFilter.ToPython()
                );
                var openOrders = algorithm.Transactions.GetOpenOrders(basicOrderFilter.ToPython());
                var openOrderTickets = algorithm.Transactions.GetOpenOrderTickets(
                    basicOrderTicketFilter.ToPython()
                );
                var openOrdersRemaining = algorithm.Transactions.GetOpenOrdersRemainingQuantity(
                    basicOrderTicketFilter.ToPython()
                );

                Assert.AreEqual(2, orders.Count());
                Assert.AreEqual(2, orderTickets.Count());
                Assert.AreEqual(2, openOrders.Count);
                Assert.AreEqual(2, openOrderTickets.Count());
                Assert.AreEqual(368, openOrdersRemaining);

                var ibmOpenOrders = algorithm.Transactions.GetOpenOrders(ibm.ToPython()).Count;
                var ibmOpenOrderTickets = algorithm
                    .Transactions.GetOpenOrderTickets(ibm.ToPython())
                    .Count();
                var ibmOpenOrdersRemainingQuantity =
                    algorithm.Transactions.GetOpenOrdersRemainingQuantity(ibm.ToPython());
                var spyOpenOrders = algorithm.Transactions.GetOpenOrders(spy.ToPython()).Count;
                var spyOpenOrderTickets = algorithm
                    .Transactions.GetOpenOrderTickets(spy.ToPython())
                    .Count();
                var spyOpenOrdersRemainingQuantity =
                    algorithm.Transactions.GetOpenOrdersRemainingQuantity(spy.ToPython());

                Assert.AreEqual(1, ibmOpenOrders);
                Assert.AreEqual(1, ibmOpenOrderTickets);
                Assert.AreEqual(184, ibmOpenOrdersRemainingQuantity);

                Assert.AreEqual(1, spyOpenOrders);
                Assert.AreEqual(1, spyOpenOrderTickets);
                Assert.AreEqual(184, spyOpenOrdersRemainingQuantity);

                var defaultOrders = algorithm.Transactions.GetOrders();
                var defaultOrderTickets = algorithm.Transactions.GetOrderTickets();
                var defaultOpenOrders = algorithm.Transactions.GetOpenOrders();
                var defaultOpenOrderTickets = algorithm.Transactions.GetOpenOrderTickets();
                var defaultOpenOrdersRemaining =
                    algorithm.Transactions.GetOpenOrdersRemainingQuantity();

                Assert.AreEqual(2, defaultOrders.Count());
                Assert.AreEqual(2, defaultOrderTickets.Count());
                Assert.AreEqual(2, defaultOpenOrders.Count);
                Assert.AreEqual(2, defaultOpenOrderTickets.Count());
                Assert.AreEqual(368, defaultOpenOrdersRemaining);
            }

            transactionHandler.Exit();
        }
    }
}
