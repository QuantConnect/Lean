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
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Lean.Engine.TransactionHandlers;
using QuantConnect.Brokerages.Backtesting;
using QuantConnect.Tests.Engine;
using QuantConnect.Algorithm;
using QuantConnect.Lean.Engine.Results;
using Python.Runtime;
using QuantConnect.Logging;

namespace QuantConnect.Tests.Common.Securities
{
    [TestFixture]
    public class SecurityTransactionManagerTests
    {
        private static TimeKeeper TimeKeeper
        {
            get { return new TimeKeeper(DateTime.Now, new[] { TimeZones.NewYork }); }
        }

        private static SubscriptionDataConfig CreateTradeBarDataConfig(Symbol symbol)
        {
            return new SubscriptionDataConfig(typeof(TradeBar), symbol, Resolution.Minute, TimeZones.NewYork, TimeZones.NewYork, true, true, true);
        }

        private static readonly SecurityExchangeHours SecurityExchangeHours = SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork);

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
            var securities = new SecurityManager(TimeKeeper);
            var transactions = new SecurityTransactionManager(null, securities);
            var transactionHandler = new BacktestingTransactionHandler();
            var portfolio = new SecurityPortfolioManager(securities, transactions);

            algorithm.Securities = securities;
            transactionHandler.Initialize(algorithm, new BacktestingBrokerage(algorithm), _resultHandler);
            transactions.SetOrderProcessor(transactionHandler);

            // Adding cash: strike price times number of shares
            portfolio.SetCash(192 * 100);

            securities.Add(
                Symbols.SPY,
                new Security(
                    SecurityExchangeHours,
                    CreateTradeBarDataConfig(Symbols.SPY),
                    new Cash(Currencies.USD, 0, 1m),
                    SymbolProperties.GetDefault(Currencies.USD),
                    ErrorCurrencyConverter.Instance,
                    RegisteredSecurityDataTypesProvider.Null,
                    new SecurityCache()
                )
            );
            securities.Add(
                Symbols.IBM,
                new Security(
                    SecurityExchangeHours,
                    CreateTradeBarDataConfig(Symbols.IBM),
                    new Cash(Currencies.USD, 0, 1m),
                    SymbolProperties.GetDefault(Currencies.USD),
                    ErrorCurrencyConverter.Instance,
                    RegisteredSecurityDataTypesProvider.Null,
                    new SecurityCache()
                )
            );
            securities[Symbols.IBM].Holdings.SetHoldings(1, 1);
            securities[Symbols.SPY].Holdings.SetHoldings(1, 1);

            var holdings = securities[Symbols.IBM].Holdings.Quantity;
            transactions.AddOrder(new SubmitOrderRequest(OrderType.Market, SecurityType.Equity, Symbols.IBM, -holdings, 0, 0, securities.UtcTime, ""));
            transactions.AddOrder(new SubmitOrderRequest(OrderType.Market, SecurityType.Equity, Symbols.SPY, -holdings, 0, 0, securities.UtcTime, ""));

            Func<Order, bool> basicOrderFilter = x => true;
            Func<OrderTicket, bool> basicOrderTicketFilter = x => true;
            using (Py.GIL())
            {
                var orders = transactions.GetOrders(basicOrderFilter.ToPython());
                var orderTickets = transactions.GetOrderTickets(basicOrderTicketFilter.ToPython());
                var openOrders = transactions.GetOpenOrders(basicOrderFilter.ToPython());
                var openOrdersTickets = transactions.GetOpenOrderTickets(basicOrderTicketFilter.ToPython());
                var openOrdersRemaining = transactions.GetOpenOrdersRemainingQuantity(basicOrderTicketFilter.ToPython());
                Assert.AreEqual(2, orders.Count());
                Assert.AreEqual(2, orderTickets.Count());
                Assert.AreEqual(0, openOrders.Count);
                Assert.AreEqual(0, openOrdersTickets.Count());
                Assert.AreEqual(0, openOrdersRemaining);
                foreach(var ticket in orderTickets)
                {
                    Log.Trace($"Ticket symbol: {ticket.Symbol} - Status: {ticket.Status}");
                }
            }
        }
    }
}
