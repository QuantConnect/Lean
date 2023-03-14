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
        public void TryGetValue()
        {
            var collection = new PortfolioTargetCollection();

            Assert.IsFalse(collection.TryGetValue(Symbols.SPY, out var target));

            collection[Symbols.SPY] = new PortfolioTarget(Symbols.SPY, 1);

            Assert.IsTrue(collection.TryGetValue(Symbols.SPY, out target));
            Assert.AreEqual(target, collection[Symbols.SPY]);
        }

        [Test]
        public void IndexAccess()
        {
            var collection = new PortfolioTargetCollection();
            collection[Symbols.SPY] = new PortfolioTarget(Symbols.SPY, 1);

            Assert.AreEqual(1, collection.Count);
            Assert.AreEqual(1, collection.Values.Count);
            Assert.AreEqual(1, collection.Keys.Count);

            collection[Symbols.IBM] = new PortfolioTarget(Symbols.IBM, 1);

            Assert.AreEqual(2, collection.Count);
            Assert.AreEqual(2, collection.Values.Count);
            Assert.AreEqual(2, collection.Keys.Count);

            collection[Symbols.IBM] = null;

            Assert.AreEqual(2, collection.Count);
            Assert.AreEqual(2, collection.Values.Count);
            Assert.AreEqual(2, collection.Keys.Count);
        }

        [Test]
        public void Count()
        {
            var collection = new PortfolioTargetCollection();
            var targets = new[] { new PortfolioTarget(Symbols.SPY, 1) };
            collection.AddRange(targets);

            Assert.AreEqual(1, collection.Count);
            collection.AddRange(new[] { new PortfolioTarget(Symbols.IBM, 1), new PortfolioTarget(Symbols.AAPL, 1) });
            Assert.AreEqual(3, collection.Count);

            collection.Clear();
        }

        [Test]
        public void IsEmpty()
        {
            var collection = new PortfolioTargetCollection();
            Assert.IsTrue(collection.IsEmpty);
            Assert.IsFalse(collection.ContainsKey(Symbols.SPY));

            collection.Add(new PortfolioTarget(Symbols.SPY, 1));
            Assert.AreEqual(1, collection.Count);
            Assert.IsFalse(collection.IsEmpty);
            Assert.IsTrue(collection.ContainsKey(Symbols.SPY));
        }

        [Test]
        public void AddRange()
        {
            var collection = new PortfolioTargetCollection();
            var targets = new[] { new PortfolioTarget(Symbols.SPY, 1), new PortfolioTarget(Symbols.AAPL, 1) };
            collection.AddRange(targets);
            Assert.AreEqual(2, collection.Count);
            Assert.IsTrue(collection.ContainsKey(Symbols.SPY));
            Assert.IsTrue(collection.ContainsKey(Symbols.AAPL));

            Assert.AreEqual(targets[0], collection[Symbols.SPY]);
            Assert.AreEqual(targets[1], collection[Symbols.AAPL]);

            Assert.AreEqual(1, collection.Values.Count(target => target == targets[0]));
            Assert.AreEqual(1, collection.Values.Count(target => target == targets[1]));
            Assert.AreEqual(1, collection.Keys.Count(symbol => symbol == Symbols.SPY));
            Assert.AreEqual(1, collection.Keys.Count(symbol => symbol == Symbols.AAPL));
        }

        [Test]
        public void RemoveTargetRespectsReference()
        {
            var symbol = new Symbol(SecurityIdentifier.GenerateBase(null, _symbol, Market.USA), _symbol);
            var collection = new PortfolioTargetCollection();
            var target = new PortfolioTarget(symbol, 1);
            collection.Add(target);
            Assert.AreEqual(collection.Count, 1);
            Assert.IsTrue(collection.Contains(target));
            // removes by reference even if same symbol
            Assert.IsFalse(collection.Remove(new PortfolioTarget(symbol, 1)));
            Assert.AreEqual(collection.Count, 1);
        }

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
            algorithm.SetFinishedWarmingUp();
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
            algorithm.SetFinishedWarmingUp();
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
            algorithm.SetFinishedWarmingUp();
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
            algorithm.SetFinishedWarmingUp();
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
            algorithm.SetFinishedWarmingUp();
            algorithm.Transactions.SetOrderProcessor(new FakeOrderProcessor());
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
            algorithm.SetFinishedWarmingUp();
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
            algorithm.SetFinishedWarmingUp();
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
            algorithm.SetFinishedWarmingUp();
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
            var orderProcessor = new FakeOrderProcessor();
            var algorithm = GetAlgorithm(orderProcessor);
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

        private QCAlgorithm GetAlgorithm(IOrderProcessor orderProcessor)
        {
            var algorithm = new FakeAlgorithm();
            algorithm.SetFinishedWarmingUp();
            algorithm.Transactions.SetOrderProcessor(orderProcessor);
            return algorithm;
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
