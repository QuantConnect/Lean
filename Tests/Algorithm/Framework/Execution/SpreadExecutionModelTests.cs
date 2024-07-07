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
using Moq;
using NUnit.Framework;
using Python.Runtime;
using QuantConnect.Algorithm;
using QuantConnect.Algorithm.Framework.Execution;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Tests.Common.Data.UniverseSelection;
using QuantConnect.Tests.Engine.DataFeeds;

namespace QuantConnect.Tests.Algorithm.Framework.Execution
{
    [TestFixture]
    public class SpreadExecutionModelTests
    {
        [TestCase(Language.CSharp)]
        [TestCase(Language.Python)]
        public void OrdersAreNotSubmittedWhenNoTargetsToExecute(Language language)
        {
            var actualOrdersSubmitted = new List<SubmitOrderRequest>();

            var orderProcessor = new Mock<IOrderProcessor>();
            orderProcessor
                .Setup(m => m.Process(It.IsAny<SubmitOrderRequest>()))
                .Returns((OrderTicket)null)
                .Callback(
                    (OrderRequest request) => actualOrdersSubmitted.Add((SubmitOrderRequest)request)
                );

            var algorithm = new QCAlgorithm();
            algorithm.SetPandasConverter();
            algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(algorithm));
            algorithm.Transactions.SetOrderProcessor(orderProcessor.Object);

            var model = GetExecutionModel(language);
            algorithm.SetExecution(model);

            var changes = SecurityChangesTests.CreateNonInternal(
                Enumerable.Empty<Security>(),
                Enumerable.Empty<Security>()
            );
            model.OnSecuritiesChanged(algorithm, changes);

            model.Execute(algorithm, new IPortfolioTarget[0]);

            Assert.AreEqual(0, actualOrdersSubmitted.Count);
        }

        [TestCase(Language.CSharp, 240, 1, 10)]
        [TestCase(Language.CSharp, 250, 0, 0)]
        [TestCase(Language.Python, 240, 1, 10)]
        [TestCase(Language.Python, 250, 0, 0)]
        public void OrdersAreSubmittedWhenRequiredForTargetsToExecute(
            Language language,
            decimal currentPrice,
            int expectedOrdersSubmitted,
            decimal expectedTotalQuantity
        )
        {
            var actualOrdersSubmitted = new List<SubmitOrderRequest>();

            var time = new DateTime(2018, 8, 2, 14, 0, 0);

            var algorithm = new QCAlgorithm();
            algorithm.SetPandasConverter();
            algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(algorithm));
            algorithm.SetDateTime(time.AddMinutes(5));

            var security = algorithm.AddEquity(Symbols.AAPL.Value);
            security.SetMarketPrice(new TradeBar { Value = 250 });
            // pushing the ask higher will cause the spread the widen and no trade to happen
            var ask = expectedOrdersSubmitted == 0 ? currentPrice * 1.1m : currentPrice;
            security.SetMarketPrice(
                new QuoteBar
                {
                    Time = time,
                    Symbol = Symbols.AAPL,
                    Ask = new Bar(ask, ask, ask, ask),
                    Bid = new Bar(currentPrice, currentPrice, currentPrice, currentPrice)
                }
            );

            algorithm.SetFinishedWarmingUp();

            var orderProcessor = new Mock<IOrderProcessor>();
            orderProcessor
                .Setup(m => m.Process(It.IsAny<SubmitOrderRequest>()))
                .Returns(
                    (SubmitOrderRequest request) => new OrderTicket(algorithm.Transactions, request)
                )
                .Callback(
                    (OrderRequest request) => actualOrdersSubmitted.Add((SubmitOrderRequest)request)
                );
            orderProcessor
                .Setup(m => m.GetOpenOrders(It.IsAny<Func<Order, bool>>()))
                .Returns(new List<Order>());
            algorithm.Transactions.SetOrderProcessor(orderProcessor.Object);

            var model = GetExecutionModel(language);
            algorithm.SetExecution(model);

            var changes = SecurityChangesTests.CreateNonInternal(
                new[] { security },
                Enumerable.Empty<Security>()
            );
            model.OnSecuritiesChanged(algorithm, changes);

            var targets = new IPortfolioTarget[] { new PortfolioTarget(Symbols.AAPL, 10) };
            model.Execute(algorithm, targets);

            Assert.AreEqual(expectedOrdersSubmitted, actualOrdersSubmitted.Count);
            Assert.AreEqual(expectedTotalQuantity, actualOrdersSubmitted.Sum(x => x.Quantity));

            if (actualOrdersSubmitted.Count == 1)
            {
                var request = actualOrdersSubmitted[0];
                Assert.AreEqual(expectedTotalQuantity, request.Quantity);
                Assert.AreEqual(algorithm.UtcTime, request.Time);
            }
        }

        [TestCase(Language.CSharp, 1, 10, true)]
        [TestCase(Language.Python, 1, 10, true)]
        [TestCase(Language.CSharp, 0, 0, false)]
        [TestCase(Language.Python, 0, 0, false)]
        public void FillsOnTradesOnlyRespectingExchangeOpen(
            Language language,
            int expectedOrdersSubmitted,
            decimal expectedTotalQuantity,
            bool exchangeOpen
        )
        {
            var actualOrdersSubmitted = new List<SubmitOrderRequest>();

            var time = new DateTime(2018, 8, 2, 0, 0, 0);
            if (exchangeOpen)
            {
                time = time.AddHours(14);
            }

            var algorithm = new QCAlgorithm();
            algorithm.SetPandasConverter();
            algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(algorithm));
            algorithm.SetDateTime(time.AddMinutes(5));

            var security = algorithm.AddEquity(Symbols.AAPL.Value);
            security.SetMarketPrice(new TradeBar { Value = 250 });

            algorithm.SetFinishedWarmingUp();

            var orderProcessor = new Mock<IOrderProcessor>();
            orderProcessor
                .Setup(m => m.Process(It.IsAny<SubmitOrderRequest>()))
                .Returns(
                    (SubmitOrderRequest request) => new OrderTicket(algorithm.Transactions, request)
                )
                .Callback(
                    (OrderRequest request) => actualOrdersSubmitted.Add((SubmitOrderRequest)request)
                );
            orderProcessor
                .Setup(m => m.GetOpenOrders(It.IsAny<Func<Order, bool>>()))
                .Returns(new List<Order>());
            algorithm.Transactions.SetOrderProcessor(orderProcessor.Object);

            var model = GetExecutionModel(language);
            algorithm.SetExecution(model);

            var changes = SecurityChangesTests.CreateNonInternal(
                new[] { security },
                Enumerable.Empty<Security>()
            );
            model.OnSecuritiesChanged(algorithm, changes);

            var targets = new IPortfolioTarget[] { new PortfolioTarget(Symbols.AAPL, 10) };
            model.Execute(algorithm, targets);

            Assert.AreEqual(expectedOrdersSubmitted, actualOrdersSubmitted.Count);
            Assert.AreEqual(expectedTotalQuantity, actualOrdersSubmitted.Sum(x => x.Quantity));

            if (actualOrdersSubmitted.Count == 1)
            {
                var request = actualOrdersSubmitted[0];
                Assert.AreEqual(expectedTotalQuantity, request.Quantity);
                Assert.AreEqual(algorithm.UtcTime, request.Time);
            }
        }

        [TestCase(Language.CSharp, MarketDataType.TradeBar)]
        [TestCase(Language.Python, MarketDataType.TradeBar)]
        [TestCase(Language.CSharp, MarketDataType.QuoteBar)]
        [TestCase(Language.Python, MarketDataType.QuoteBar)]
        public void OnSecuritiesChangeDoesNotThrow(Language language, MarketDataType marketDataType)
        {
            var time = new DateTime(2018, 8, 2, 16, 0, 0);

            Func<double, int, BaseData> func = (x, i) =>
            {
                var price = Convert.ToDecimal(x);
                switch (marketDataType)
                {
                    case MarketDataType.TradeBar:
                        return new TradeBar(
                            time.AddMinutes(i),
                            Symbols.AAPL,
                            price,
                            price,
                            price,
                            price,
                            100m
                        );
                    case MarketDataType.QuoteBar:
                        var bar = new Bar(price, price, price, price);
                        return new QuoteBar(time.AddMinutes(i), Symbols.AAPL, bar, 10m, bar, 10m);
                    default:
                        throw new ArgumentException($"Invalid MarketDataType: {marketDataType}");
                }
            };

            var algorithm = new QCAlgorithm();
            algorithm.SetPandasConverter();
            algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(algorithm));
            algorithm.SetDateTime(time.AddMinutes(5));

            var security = algorithm.AddEquity(Symbols.AAPL.Value);
            security.SetMarketPrice(new TradeBar { Value = 250 });
            algorithm.SetFinishedWarmingUp();

            var model = GetExecutionModel(language);
            algorithm.SetExecution(model);

            var changes = SecurityChangesTests.CreateNonInternal(
                new[] { security },
                Enumerable.Empty<Security>()
            );
            Assert.DoesNotThrow(() => model.OnSecuritiesChanged(algorithm, changes));
        }

        private static IExecutionModel GetExecutionModel(Language language)
        {
            const decimal acceptingSpreadPercent = 0.005m;

            if (language == Language.Python)
            {
                using (Py.GIL())
                {
                    const string name = nameof(SpreadExecutionModel);
                    var instance = Py.Import(name)
                        .GetAttr(name)
                        .Invoke(acceptingSpreadPercent.ToPython());
                    return new ExecutionModelPythonWrapper(instance);
                }
            }

            return new SpreadExecutionModel(acceptingSpreadPercent);
        }
    }
}
