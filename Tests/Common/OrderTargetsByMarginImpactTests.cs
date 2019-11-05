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

namespace QuantConnect.Tests.Common
{
    public class OrderTargetsByMarginImpactTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void LessThanLotSizeIsIgnored_NoHoldings(bool targetIsDelta)
        {
            var algorithm = GetAlgorithm();
            var collection = new[] {new PortfolioTarget(Symbols.AAPL, 0.9m)};

            var result = collection.OrderTargetsByMarginImpact(algorithm, targetIsDelta).ToList();
            Assert.AreEqual(0, result.Count);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void LessThanLotSizeIsIgnored_WithHoldings(bool targetIsDelta)
        {
            var algorithm = GetAlgorithm(holdings:1m);
            var collection = new[] { new PortfolioTarget(Symbols.AAPL, 1.9m) };

            var result = collection.OrderTargetsByMarginImpact(algorithm, targetIsDelta).ToList();

            if (targetIsDelta)
            {
                Assert.AreEqual(1, result.Count);
                Assert.AreEqual(1.9m, result[0].Quantity);
            }
            else
            {
                Assert.AreEqual(0, result.Count);
            }
        }

        [Test]
        public void SecurityWithNoDataIsIgnored()
        {
            var algorithm = GetAlgorithm();

            // SPY won't have any data and should be ignored
            algorithm.AddEquity(Symbols.SPY.Value);
            var collection = new[] {new PortfolioTarget(Symbols.SPY, 5000m),
                new PortfolioTarget(Symbols.AAPL, 1m)};

            var result = collection.OrderTargetsByMarginImpact(algorithm).ToList();
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(1m, result[0].Quantity);
        }

        [Test]
        public void NoExistingHoldings()
        {
            var algorithm = GetAlgorithm();
            var spy = algorithm.AddEquity(Symbols.SPY.Value);
            Update(spy, 1);

            var collection = new[] {new PortfolioTarget(Symbols.SPY, 5m),
                new PortfolioTarget(Symbols.AAPL, 1m)};

            var result = collection.OrderTargetsByMarginImpact(algorithm).ToList();
            Assert.AreEqual(2, result.Count);
            // highest order value first
            Assert.AreEqual(5m, result[0].Quantity);
            Assert.AreEqual(1m, result[1].Quantity);
        }

        [TestCase(OrderDirection.Buy, false)]
        [TestCase(OrderDirection.Sell, false)]
        [TestCase(OrderDirection.Buy, true)]
        [TestCase(OrderDirection.Sell, true)]
        public void ReducingPosition(OrderDirection direction, bool targetIsDelta)
        {
            var algorithm = GetAlgorithm(direction == OrderDirection.Sell ? 2 : -2);
            var spy = algorithm.AddEquity(Symbols.SPY.Value);
            Update(spy, 1);

            var target = direction == OrderDirection.Sell ? -1 : 1;
            var collection = new[] {new PortfolioTarget(Symbols.SPY, 5m),
                new PortfolioTarget(Symbols.AAPL, target)};

            var result = collection.OrderTargetsByMarginImpact(algorithm, targetIsDelta).ToList();

            Assert.AreEqual(2, result.Count);
            // target reducing the position first
            Assert.AreEqual(target, result[0].Quantity);
            Assert.AreEqual(5m, result[1].Quantity);
        }

        [TestCase(OrderDirection.Buy, false)]
        [TestCase(OrderDirection.Sell, false)]
        [TestCase(OrderDirection.Buy, true)]
        [TestCase(OrderDirection.Sell, true)]
        public void ReducingPositionDeltaEffect(OrderDirection direction, bool targetIsDelta)
        {
            var algorithm = GetAlgorithm(direction == OrderDirection.Sell ? 2 : -2);
            var spy = algorithm.AddEquity(Symbols.SPY.Value);
            Update(spy, 1);

            var target = direction == OrderDirection.Sell ? -2.5m : 2.5m;
            var collection = new[] {new PortfolioTarget(Symbols.SPY, 5m),
                new PortfolioTarget(Symbols.AAPL, target)};

            var result = collection.OrderTargetsByMarginImpact(algorithm, targetIsDelta).ToList();

            Assert.AreEqual(2, result.Count);

            if (targetIsDelta)
            {
                // target reducing the position first
                Assert.AreEqual(target, result[0].Quantity);
                Assert.AreEqual(5m, result[1].Quantity);
            }
            else
            {
                Assert.AreEqual(5m, result[0].Quantity);
                Assert.AreEqual(target, result[1].Quantity);
            }
        }

        [TestCase(OrderDirection.Buy, false)]
        [TestCase(OrderDirection.Sell, false)]
        [TestCase(OrderDirection.Buy, true)]
        [TestCase(OrderDirection.Sell, true)]
        public void IncreasePosition(OrderDirection direction, bool targetIsDelta)
        {
            var value = direction == OrderDirection.Sell ? -1 : 1;
            var algorithm = GetAlgorithm(value);
            var spy = algorithm.AddEquity(Symbols.SPY.Value);
            Update(spy, 1);

            var collection = new[] {new PortfolioTarget(Symbols.SPY, 2m),
                new PortfolioTarget(Symbols.AAPL, value * 2.1m)};

            var result = collection.OrderTargetsByMarginImpact(algorithm, targetIsDelta).ToList();

            Assert.AreEqual(2, result.Count);

            if (targetIsDelta)
            {
                // AAPL is increasing the position by 2.1
                Assert.AreEqual(Symbols.AAPL, result[0].Symbol);
                Assert.AreEqual(Symbols.SPY, result[1].Symbol);
            }
            else
            {
                Assert.AreEqual(Symbols.SPY, result[0].Symbol);
                // target with the least order value, AAPL is increasing the position by 1.1
                Assert.AreEqual(Symbols.AAPL, result[1].Symbol);
            }
        }

        private static void Update(Security security, decimal close)
        {
            security.SetMarketPrice(new TradeBar
            {
                Time = DateTime.Now,
                Symbol = security.Symbol,
                Open = close,
                High = close,
                Low = close,
                Close = close
            });
        }

        private QCAlgorithm GetAlgorithm(decimal? holdings = null)
        {
            var algorithm = new AlgorithmStub();
            algorithm.Transactions.SetOrderProcessor(new FakeOrderProcessor());
            var aapl = algorithm.AddEquity(Symbols.AAPL.Value);
            Update(aapl, 1);

            if (holdings != null)
            {
                aapl.Holdings.SetHoldings(10, holdings.Value);
            }
            return algorithm;
        }
    }
}
