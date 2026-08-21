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
using NUnit.Framework;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Brokerages;
using System.Collections.Generic;
using QuantConnect.Securities.Equity;

namespace QuantConnect.Tests.Brokerages
{
    /// <summary>
    /// Additional tests for the base <see cref="Brokerage"/> class
    /// </summary>
    public class DefaultBrokerageTests
    {
        [TestCase(OrderDirection.Buy, 0, ExpectedResult = OrderPosition.BuyToOpen)]
        [TestCase(OrderDirection.Buy, 100, ExpectedResult = OrderPosition.BuyToOpen)]
        [TestCase(OrderDirection.Buy, -100, ExpectedResult = OrderPosition.BuyToClose)]
        [TestCase(OrderDirection.Sell, 0, ExpectedResult = OrderPosition.SellToOpen)]
        [TestCase(OrderDirection.Sell, 100, ExpectedResult = OrderPosition.SellToClose)]
        [TestCase(OrderDirection.Sell, -100, ExpectedResult = OrderPosition.SellToOpen)]
        public OrderPosition GetsOrderPosition(OrderDirection direction, decimal holdingsQuantity)
        {
            return TestableBrokerage.GetOrderPositionPublic(direction, holdingsQuantity);
        }

        [TestCase("GOOGL")]
        [TestCase("GOOG")]
        [TestCase("SomeOtherTicker")]
        public void UpdatesOutdatedHoldingsTicker(string ticker)
        {
            // GOOGL first ticker is 'GOOG', so it's security identifier holds the outdated ticker
            var expectedSymbol = Symbol.Create("GOOGL", SecurityType.Equity, Market.USA);
            var brokerageData = new Dictionary<string, string>
            {
                { "live-holdings", $@"[{{""symbol"":{{""id"":""{expectedSymbol.ID}"",""value"":""{ticker}""}},""a"":10,""q"":100}}]" }
            };

            var holdings = new TestableBrokerage("test").GetAccountHoldingsPublic(brokerageData, null);

            Assert.AreEqual(1, holdings.Count);
            Assert.AreEqual(expectedSymbol.ID, holdings[0].Symbol.ID);
            Assert.AreEqual(expectedSymbol.Value, holdings[0].Symbol.Value);
            Assert.AreEqual(100, holdings[0].Quantity);
        }

        [Test]
        public void UpdatesOutdatedOptionHoldingsUnderlyingTicker()
        {
            var underlying = Symbol.Create("GOOGL", SecurityType.Equity, Market.USA);
            var expectedSymbol = Symbol.CreateOption(underlying, Market.USA, OptionStyle.American, OptionRight.Call, 100, new DateTime(2050, 1, 21));
            // no underlying provided, so it will be created from the security identifier which holds the outdated ticker
            var brokerageData = new Dictionary<string, string>
            {
                { "live-holdings", $@"[{{""symbol"":{{""id"":""{expectedSymbol.ID}"",""value"":""{expectedSymbol.Value}""}},""q"":1}}]" }
            };

            var holdings = new TestableBrokerage("test").GetAccountHoldingsPublic(brokerageData, null);

            Assert.AreEqual(1, holdings.Count);
            Assert.AreEqual(expectedSymbol.ID, holdings[0].Symbol.ID);
            Assert.AreEqual(expectedSymbol.Value, holdings[0].Symbol.Value);
            Assert.AreEqual(underlying.Value, holdings[0].Symbol.Underlying.Value);
        }

        [Test]
        public void DoesNotUpdateTickerForSecuritiesWhichDoNotRequireMapping()
        {
            var expectedSymbol = Symbol.Create("EURUSD", SecurityType.Forex, Market.Oanda);
            var brokerageData = new Dictionary<string, string>
            {
                { "live-holdings", $@"[{{""symbol"":{{""id"":""{expectedSymbol.ID}"",""value"":""{expectedSymbol.Value}""}},""q"":1000}}]" }
            };

            var holdings = new TestableBrokerage("test").GetAccountHoldingsPublic(brokerageData, null);

            Assert.AreEqual(1, holdings.Count);
            Assert.AreEqual(expectedSymbol, holdings[0].Symbol);
            Assert.AreEqual(expectedSymbol.Value, holdings[0].Symbol.Value);
        }

        [Test]
        public void UpdatesOutdatedSecurityHoldingsTicker()
        {
            var expectedSymbol = Symbol.Create("GOOGL", SecurityType.Equity, Market.USA);
            // the security was created before the rename, so it's ticker is outdated
            var cashBook = new CashBook();
            var security = new Equity(new Symbol(expectedSymbol.ID, "GOOG"),
                SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                cashBook.Add(Currencies.USD, 0, 1),
                SymbolProperties.GetDefault(Currencies.USD),
                cashBook,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache());
            security.SetLocalTimeKeeper(new TimeKeeper(DateTime.UtcNow, TimeZones.NewYork).GetLocalTimeKeeper(TimeZones.NewYork));
            security.Holdings.SetHoldings(10, 100);

            var holdings = new TestableBrokerage("test").GetAccountHoldingsPublic(null, new[] { security });

            Assert.AreEqual(1, holdings.Count);
            Assert.AreEqual(expectedSymbol.ID, holdings[0].Symbol.ID);
            Assert.AreEqual(expectedSymbol.Value, holdings[0].Symbol.Value);
            Assert.AreEqual(100, holdings[0].Quantity);
        }

        private class TestableBrokerage : Brokerage
        {
            public TestableBrokerage(string name) : base(name)
            {
            }

            public List<Holding> GetAccountHoldingsPublic(Dictionary<string, string> brokerageData, IEnumerable<Security> securities)
            {
                return GetAccountHoldings(brokerageData, securities);
            }

            public override bool IsConnected => throw new NotImplementedException();

            public override bool CancelOrder(Order order)
            {
                throw new NotImplementedException();
            }

            public override void Connect()
            {
                throw new NotImplementedException();
            }

            public override void Disconnect()
            {
                throw new NotImplementedException();
            }

            public override List<Holding> GetAccountHoldings()
            {
                throw new NotImplementedException();
            }

            public override List<CashAmount> GetCashBalance()
            {
                throw new NotImplementedException();
            }

            public override List<Order> GetOpenOrders()
            {
                throw new NotImplementedException();
            }

            public override bool PlaceOrder(Order order)
            {
                throw new NotImplementedException();
            }

            public override bool UpdateOrder(Order order)
            {
                throw new NotImplementedException();
            }

            public static OrderPosition GetOrderPositionPublic(OrderDirection direction, decimal holdingsQuantity)
            {
                return GetOrderPosition(direction, holdingsQuantity);
            }
        }

    }
}
