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
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Tests.Common.Securities;
using QuantConnect.Tests.Engine.DataFeeds;

namespace QuantConnect.Tests.Common.Orders
{
    [TestFixture]
    public class OrderSizingTests
    {
        [TestCase(0.98, 0)]
        [TestCase(-0.98, 0)]
        [TestCase(0.9999999, 1)]
        [TestCase(-0.9999999, -1)]
        public void AdjustByLotSize(decimal quantity, decimal expected)
        {
            var algo = new AlgorithmStub();
            var security = algo.AddEquity(Symbols.SPY.Value);

            var result = OrderSizing.AdjustByLotSize(security, quantity);

            Assert.AreEqual(expected, result);
        }

        [Test]
        public void GetOrderSizeForPercentVolume()
        {
            var algo = new AlgorithmStub();
            var security = algo.AddFutureContract(Symbols.Future_CLF19_Jan2019);
            security.SetMarketPrice(new TradeBar { Value = 250, Volume = 10});

            var result = OrderSizing.GetOrderSizeForPercentVolume(security, 0.5m, 100);
            Assert.AreEqual(5, result);
        }

        [TestCase(100000, 100, 0)]
        [TestCase(1000000, 100, 4)]
        [TestCase(1000000, 1, 1)]
        [TestCase(1000000, -1, -1)]
        public void GetOrderSizeForMaximumValue(decimal maximumOrderValue, decimal target, decimal expected)
        {
            var algo = new AlgorithmStub();
            var security = algo.AddFutureContract(Symbols.Future_CLF19_Jan2019);
            security.SetMarketPrice(new TradeBar { Value = 250 });

            var result = OrderSizing.GetOrderSizeForMaximumValue(security, maximumOrderValue, target);

            var expectedCalculated = maximumOrderValue / (security.Price * security.SymbolProperties.ContractMultiplier);
            expectedCalculated -= expectedCalculated % security.SymbolProperties.LotSize;

            Assert.AreEqual(Math.Min(expectedCalculated, Math.Abs(target)) * Math.Sign(target), result);
            Assert.AreEqual(expected, result);
        }

        [TestCase(2, 1, -1)]
        [TestCase(-2, -1, 1)]
        [TestCase(1, 1, 0)]
        [TestCase(0, 1, 1)]
        [TestCase(1, 2, 1)]
        [TestCase(-1, 2, 3)]
        [TestCase(1, -1, -2)]
        [TestCase(-1, -2, -1)]
        [TestCase(0, -1, -1)]
        [TestCase(-1, -1, 0)]
        public void GetUnorderedQuantityHoldingsNoOrders(decimal holdings, decimal target, decimal expected)
        {
            var algo = new AlgorithmStub();
            algo.Transactions.SetOrderProcessor(new FakeOrderProcessor());
            var security = algo.AddFutureContract(Symbols.Future_CLF19_Jan2019);
            security.SetMarketPrice(new TradeBar { Value = 250 });
            security.Holdings.SetHoldings(250, holdings);

            var result = OrderSizing.GetUnorderedQuantity(algo,
                new PortfolioTarget(Symbols.Future_CLF19_Jan2019, target));

            Assert.AreEqual(expected, result);
        }

        [TestCase(-1, -3, -1, -1)]
        [TestCase(1, 3, 1, 1)]
        [TestCase(2, 3, 1, 0)]
        [TestCase(2, 3, -1, 2)]
        [TestCase(2, 2, 1, -1)]
        [TestCase(-2, 3, 1, 4)]
        public void GetUnorderedQuantityHoldingsOpenOrders(decimal holdings, decimal target, decimal filledQuantity, decimal expected)
        {
            var algo = new AlgorithmStub();
            var orderProcessor = new FakeOrderProcessor();
            var orderRequest = new SubmitOrderRequest(
                    OrderType.Market,
                    SecurityType.Future,
                    Symbols.Future_CLF19_Jan2019,
                    filledQuantity * 2,
                    250,
                    250,
                    new DateTime(2020, 1, 1),
                    "Pepe"
                );

            var order = Order.CreateOrder(orderRequest);
            var ticket = new OrderTicket(algo.Transactions, orderRequest);
            ticket.SetOrder(order);
           
            ticket.AddOrderEvent(new OrderEvent(1,
                Symbols.Future_CLF19_Jan2019,
                new DateTime(2020, 1, 1),
                OrderStatus.Filled,
                filledQuantity > 0 ? OrderDirection.Buy : OrderDirection.Sell,
                250,
                filledQuantity,
                OrderFee.Zero));

            orderProcessor.AddTicket(ticket);
            algo.Transactions.SetOrderProcessor(orderProcessor);
            var security = algo.AddFutureContract(Symbols.Future_CLF19_Jan2019);
            security.SetMarketPrice(new TradeBar { Value = 250 });
            security.Holdings.SetHoldings(250, holdings);

            var result = OrderSizing.GetUnorderedQuantity(algo,
                new PortfolioTarget(Symbols.Future_CLF19_Jan2019, target));

            Assert.AreEqual(expected, result);
        }
    }
}
