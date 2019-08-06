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
using QuantConnect.Algorithm;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Tests.Common.Securities;
using QuantConnect.Tests.Engine.DataFeeds;
using QuantConnect.Util;

namespace QuantConnect.Tests.Algorithm.Framework.Portfolio
{
    [TestFixture]
    public class PortfolioTargetCollectionTests
    {
        private string _symbol = "SPY";

        [Test]
        public void AddContainsAndRemoveWork()
        {
            var symbol = new Symbol(SecurityIdentifier.GenerateBase(null, _symbol, Market.USA), _symbol);
            var collection = new PortfolioTargetCollection();
            var target = new PortfolioTarget(symbol, 1);
            collection.Add(target);
            Assert.AreEqual(collection.Count, 1);
            Assert.IsTrue(collection.Contains(target));
            Assert.IsTrue(collection.Remove(target));
            Assert.AreEqual(collection.Count, 0);
        }

        [Test]
        public void ClearFulfilledDoesNotRemoveUnreachedTarget()
        {
            var algorithm = new FakeAlgorithm();
            var symbol = new Symbol(SecurityIdentifier.GenerateEquity(_symbol, Market.USA), _symbol);
            var equity = algorithm.AddEquity(symbol);
            var dummySecurityHolding = new FakeSecurityHolding(equity);
            equity.Holdings = dummySecurityHolding;
            var collection = new PortfolioTargetCollection();
            var target = new PortfolioTarget(symbol, -1);
            collection.Add(target);

            collection.ClearFulfilled(algorithm);
            Assert.AreEqual(collection.Count, 1);
        }

        [Test]
        public void ClearRemovesUnreachedTarget()
        {
            var algorithm = new FakeAlgorithm();
            var symbol = new Symbol(SecurityIdentifier.GenerateEquity(_symbol, Market.USA), _symbol);
            var equity = algorithm.AddEquity(symbol);
            var dummySecurityHolding = new FakeSecurityHolding(equity);
            equity.Holdings = dummySecurityHolding;
            var collection = new PortfolioTargetCollection();
            var target = new PortfolioTarget(symbol, -1);
            collection.Add(target);

            collection.Clear();
            Assert.AreEqual(collection.Count, 0);
        }

        [Test]
        public void ClearFulfilledRemovesPositiveTarget()
        {
            var algorithm = new FakeAlgorithm();
            var symbol = new Symbol(SecurityIdentifier.GenerateEquity(_symbol, Market.USA), _symbol);
            var equity = algorithm.AddEquity(symbol);
            var dummySecurityHolding = new FakeSecurityHolding(equity);
            equity.Holdings = dummySecurityHolding;
            var collection = new PortfolioTargetCollection();
            var target = new PortfolioTarget(symbol, 1);
            collection.Add(target);

            dummySecurityHolding.SetQuantity(1);
            collection.ClearFulfilled(algorithm);
            Assert.AreEqual(collection.Count, 0);
        }

        [Test]
        public void ClearFulfilledRemovesNegativeTarget()
        {
            var algorithm = new FakeAlgorithm();
            var symbol = new Symbol(SecurityIdentifier.GenerateEquity(_symbol, Market.USA), _symbol);
            var equity = algorithm.AddEquity(symbol);
            var dummySecurityHolding = new FakeSecurityHolding(equity);
            equity.Holdings = dummySecurityHolding;
            var collection = new PortfolioTargetCollection();
            var target = new PortfolioTarget(symbol, -1);
            collection.Add(target);

            dummySecurityHolding.SetQuantity(-1);
            collection.ClearFulfilled(algorithm);
            Assert.AreEqual(collection.Count, 0);
        }

        [Test]
        public void OrderByMarginImpactDoesNotReturnTargetsWithNoData()
        {
            var algorithm = new FakeAlgorithm();
            var symbol = new Symbol(SecurityIdentifier.GenerateEquity(_symbol, Market.USA), _symbol);
            algorithm.AddEquity(symbol);

            var collection = new PortfolioTargetCollection();
            var target = new PortfolioTarget(symbol, -1);
            collection.Add(target);
            var targets = collection.OrderByMarginImpact(algorithm);

            Assert.AreEqual(collection.Count, 1);
            Assert.IsTrue(targets.IsNullOrEmpty());
        }

        [Test]
        public void OrderByMarginImpactReturnsExpectedTargets()
        {
            var algorithm = new FakeAlgorithm();
            algorithm.Transactions.SetOrderProcessor(new FakeOrderProcessor());
            var symbol = new Symbol(SecurityIdentifier.GenerateEquity(_symbol, Market.USA), _symbol);
            var equity = algorithm.AddEquity(symbol);
            equity.Cache.AddData(new TradeBar(DateTime.UtcNow, symbol, 1, 1, 1, 1, 1));
            var collection = new PortfolioTargetCollection();
            var target = new PortfolioTarget(symbol, -1);
            collection.Add(target);

            var targets = collection.OrderByMarginImpact(algorithm);

            Assert.AreEqual(collection.Count, 1);
            Assert.AreEqual(targets.Count(), 1);
            Assert.AreEqual(targets.First(), target);
        }

        [Test]
        public void OrderByMarginImpactDoesNotReturnTargetsForWhichUnorderedQuantityIsZeroBecauseTargetIsZero()
        {
            var algorithm = new FakeAlgorithm();
            algorithm.Transactions.SetOrderProcessor(new FakeOrderProcessor());
            var symbol = new Symbol(SecurityIdentifier.GenerateEquity(_symbol, Market.USA), _symbol);
            var equity = algorithm.AddEquity(symbol);
            equity.Cache.AddData(new TradeBar(DateTime.UtcNow, symbol, 1, 1, 1, 1, 1));
            var collection = new PortfolioTargetCollection();
            var target = new PortfolioTarget(symbol, 0);
            collection.Add(target);

            var targets = collection.OrderByMarginImpact(algorithm);

            Assert.AreEqual(collection.Count, 1);
            Assert.IsTrue(targets.IsNullOrEmpty());
        }

        [Test]
        public void OrderByMarginImpactDoesNotReturnTargetsForWhichUnorderedQuantityIsZeroBecauseTargetReached()
        {
            var algorithm = new FakeAlgorithm();
            algorithm.Transactions.SetOrderProcessor(new FakeOrderProcessor());
            var symbol = new Symbol(SecurityIdentifier.GenerateEquity(_symbol, Market.USA), _symbol);
            var equity = algorithm.AddEquity(symbol);
            var dummySecurityHolding = new FakeSecurityHolding(equity);
            equity.Holdings = dummySecurityHolding;
            equity.Cache.AddData(new TradeBar(DateTime.UtcNow, symbol, 1, 1, 1, 1, 1));
            var collection = new PortfolioTargetCollection();
            var target = new PortfolioTarget(symbol, 1);
            collection.Add(target);
            dummySecurityHolding.SetQuantity(1);

            var targets = collection.OrderByMarginImpact(algorithm);
            Assert.AreEqual(collection.Count, 1);
            Assert.IsTrue(targets.IsNullOrEmpty());
        }

        [Test]
        public void OrderByMarginImpactDoesNotReturnTargetsForWhichUnorderedQuantityIsZeroBecauseOpenOrder()
        {
            var algorithm = new FakeAlgorithm();
            var orderProcessor = new FakeOrderProcessor();
            algorithm.Transactions.SetOrderProcessor(orderProcessor);
            var symbol = new Symbol(SecurityIdentifier.GenerateEquity(_symbol, Market.USA), _symbol);
            var equity = algorithm.AddEquity(symbol);
            equity.Cache.AddData(new TradeBar(DateTime.UtcNow, symbol, 1, 1, 1, 1, 1));
            var collection = new PortfolioTargetCollection();
            var target = new PortfolioTarget(symbol, 1);
            collection.Add(target);

            var openOrderRequest = new SubmitOrderRequest(OrderType.Market, symbol.SecurityType, symbol, 1, 0, 0, DateTime.UtcNow, "");
            openOrderRequest.SetOrderId(1);
            var openOrderTicket = new OrderTicket(algorithm.Transactions, openOrderRequest);

            orderProcessor.AddOrder(new MarketOrder(symbol, 1, DateTime.UtcNow));
            orderProcessor.AddTicket(openOrderTicket);

            var targets = collection.OrderByMarginImpact(algorithm);
            Assert.AreEqual(collection.Count, 1);
            Assert.IsTrue(targets.IsNullOrEmpty());
        }

        private class FakeSecurityHolding : SecurityHolding
        {
            public FakeSecurityHolding(Security security) :
                base(security, new IdentityCurrencyConverter(security.QuoteCurrency.Symbol))
            {
            }
            public void SetQuantity(int quantity)
            {
                Quantity = quantity;
            }
        }

        private class FakeAlgorithm : QCAlgorithm
        {
            public FakeAlgorithm()
            {
                SubscriptionManager.SetDataManager(new DataManagerStub(this));
            }
        }
    }
}
