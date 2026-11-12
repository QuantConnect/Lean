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
using NodaTime;
using NUnit.Framework;
using Python.Runtime;
using QuantConnect.Algorithm;
using QuantConnect.Algorithm.Framework.Execution;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Tests.Common.Data.UniverseSelection;
using QuantConnect.Tests.Engine.DataFeeds;

namespace QuantConnect.Tests.Algorithm.Framework.Execution
{
    [TestFixture]
    public class StandardDeviationExecutionModelTests
    {
        [TestCase(Language.CSharp)]
        [TestCase(Language.Python)]
        public void OrdersAreNotSubmittedWhenNoTargetsToExecute(Language language)
        {
            var actualOrdersSubmitted = new List<SubmitOrderRequest>();

            var orderProcessor = new Mock<IOrderProcessor>();
            orderProcessor.Setup(m => m.Process(It.IsAny<SubmitOrderRequest>()))
                .Returns((OrderTicket)null)
                .Callback((OrderRequest request) => actualOrdersSubmitted.Add((SubmitOrderRequest)request));

            var algorithm = new QCAlgorithm();
            algorithm.SetPandasConverter();
            algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(algorithm));
            algorithm.Transactions.SetOrderProcessor(orderProcessor.Object);

            var model = GetExecutionModel(language);
            algorithm.SetExecution(model);

            var changes = SecurityChangesTests.CreateNonInternal(Enumerable.Empty<Security>(), Enumerable.Empty<Security>());
            model.OnSecuritiesChanged(algorithm, changes);

            model.Execute(algorithm, new IPortfolioTarget[0]);

            Assert.AreEqual(0, actualOrdersSubmitted.Count);
        }

        [TestCase(Language.CSharp, new[] { 270d, 260d, 250d }, 240, 1, 10)]
        [TestCase(Language.CSharp, new[] { 250d, 250d, 250d }, 250, 0, 0)]
        [TestCase(Language.Python, new[] { 270d, 260d, 250d }, 240, 1, 10)]
        [TestCase(Language.Python, new[] { 250d, 250d, 250d }, 250, 0, 0)]
        public void OrdersAreSubmittedWhenRequiredForTargetsToExecute(
            Language language,
            double[] historicalPrices,
            decimal currentPrice,
            int expectedOrdersSubmitted,
            decimal expectedTotalQuantity)
        {
            var time = new DateTime(2018, 8, 2, 16, 0, 0);
            var historyProvider = new Mock<IHistoryProvider>();
            historyProvider.Setup(m => m.GetHistory(It.IsAny<IEnumerable<HistoryRequest>>(), It.IsAny<DateTimeZone>()))
                .Returns(historicalPrices.Select((x,i) =>
                    new Slice(time.AddMinutes(i),
                        new List<BaseData>
                        {
                            new TradeBar
                            {
                                Time = time.AddMinutes(i),
                                Symbol = Symbols.AAPL,
                                Open = Convert.ToDecimal(x),
                                High = Convert.ToDecimal(x),
                                Low = Convert.ToDecimal(x),
                                Close = Convert.ToDecimal(x),
                                Volume = 100m
                            }
                        }, time.AddMinutes(i))));

            var algorithm = new QCAlgorithm();
            algorithm.SetPandasConverter();
            algorithm.SetHistoryProvider(historyProvider.Object);
            algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(algorithm));
            algorithm.SetDateTime(time.AddMinutes(5));

            var security = algorithm.AddEquity(Symbols.AAPL.Value);
            security.SetMarketPrice(new TradeBar { Value = currentPrice });

            algorithm.SetFinishedWarmingUp();

            var orderProcessor = ImmediateExecutionModelTests.GetAndSetBrokerageTransactionHandler(algorithm, out var brokerage);

            try
            {
                var model = GetExecutionModel(language);
                algorithm.SetExecution(model);

                var changes = SecurityChangesTests.CreateNonInternal(new[] { security }, Enumerable.Empty<Security>());
                model.OnSecuritiesChanged(algorithm, changes);

                var targets = new IPortfolioTarget[] { new PortfolioTarget(Symbols.AAPL, 10) };
                model.Execute(algorithm, targets);
                orderProcessor.ProcessSynchronousEvents();

                var orders = orderProcessor.GetOrders().ToList();

                Assert.AreEqual(expectedOrdersSubmitted, orders.Count);
                Assert.AreEqual(expectedTotalQuantity, orders.Sum(x => x.Quantity));

                if (expectedOrdersSubmitted == 1)
                {
                    var order = orders[0];
                    Assert.AreEqual(expectedTotalQuantity, order.Quantity);
                    Assert.AreEqual(algorithm.UtcTime, order.Time);
                }
            }
            finally
            {
                orderProcessor.Exit();
                brokerage?.Dispose();
            }
        }

        [TestCase(Language.CSharp, new[] { 270d, 260d, 250d }, MarketDataType.TradeBar)]
        [TestCase(Language.Python, new[] { 250d, 250d, 250d }, MarketDataType.TradeBar)]
        [TestCase(Language.CSharp, new[] { 270d, 260d, 250d }, MarketDataType.QuoteBar)]
        [TestCase(Language.Python, new[] { 250d, 250d, 250d }, MarketDataType.QuoteBar)]
        public void OnSecuritiesChangeDoesNotThrow(
            Language language,
            double[] historicalPrices,
            MarketDataType marketDataType)
        {
            var time = new DateTime(2018, 8, 2, 16, 0, 0);

            Func<double, int, BaseData> func = (x, i) =>
            {
                var price = Convert.ToDecimal(x);
                switch (marketDataType)
                {
                    case MarketDataType.TradeBar:
                        return new TradeBar(time.AddMinutes(i), Symbols.AAPL, price, price, price, price, 100m);
                    case MarketDataType.QuoteBar:
                        var bar = new Bar(price, price, price, price);
                        return new QuoteBar(time.AddMinutes(i), Symbols.AAPL, bar, 10m, bar, 10m);
                    default:
                        throw new ArgumentException($"Invalid MarketDataType: {marketDataType}");
                }
            };

            var historyProvider = new Mock<IHistoryProvider>();
            historyProvider.Setup(m => m.GetHistory(It.IsAny<IEnumerable<HistoryRequest>>(), It.IsAny<DateTimeZone>()))
                .Returns(historicalPrices.Select((x, i) => new Slice(time.AddMinutes(i), new List<BaseData> { func(x, i) }, time.AddMinutes(i))));

            var algorithm = new QCAlgorithm();
            algorithm.SetPandasConverter();
            algorithm.SetHistoryProvider(historyProvider.Object);
            algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(algorithm));
            algorithm.SetDateTime(time.AddMinutes(5));

            var security = algorithm.AddEquity(Symbols.AAPL.Value);
            security.SetMarketPrice(new TradeBar { Value = 250 });
            algorithm.SetFinishedWarmingUp();

            var model = GetExecutionModel(language);
            algorithm.SetExecution(model);

            var changes = SecurityChangesTests.CreateNonInternal(new[] { security }, Enumerable.Empty<Security>());
            Assert.DoesNotThrow(() => model.OnSecuritiesChanged(algorithm, changes));
        }

        [TestCase(Language.CSharp)]
        [TestCase(Language.Python)]
        public void ReAddedSecurityGetsFreshSymbolData(Language language)
        {
            var time = new DateTime(2018, 8, 2, 16, 0, 0);
            var historicalPrice = 100m;

            var historyProvider = new Mock<IHistoryProvider>();
            historyProvider.Setup(m => m.GetHistory(It.IsAny<IEnumerable<HistoryRequest>>(), It.IsAny<DateTimeZone>()))
                .Returns(() => CreateHistory(time, historicalPrice));

            var algorithm = new QCAlgorithm();
            algorithm.SetHistoryProvider(historyProvider.Object);
            algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(algorithm));
            algorithm.SetDateTime(time.AddMinutes(5));

            var security = algorithm.AddEquity(Symbols.AAPL.Value);
            algorithm.SetFinishedWarmingUp();

            var orderProcessor = ImmediateExecutionModelTests.GetAndSetBrokerageTransactionHandler(algorithm, out var brokerage);
            try
            {
                var model = GetExecutionModel(language);
                algorithm.SetExecution(model);
                model.OnSecuritiesChanged(algorithm, SecurityChangesTests.CreateNonInternal(new[] { security }, Enumerable.Empty<Security>()));

                // the SMA is around $100 and the quote sits below it, so selling is not favorable
                security.SetMarketPrice(new TradeBar { Time = time, Symbol = Symbols.AAPL, Close = 100m });
                security.SetMarketPrice(new Tick(time, Symbols.AAPL, 99.98m, 99.99m));

                var subscription = security.Subscriptions.First();
                var consolidators = subscription.Consolidators.ToList();

                // The engine reports a removal only once, when the security is deselected. When the removal
                // is deferred because a position is still open, the data subscription and its consolidators
                // are taken down later without another notification, so the model is never told to clear
                // its cached symbol data.
                security.Reset();
                algorithm.Securities.Remove(security.Symbol);
                foreach (var consolidator in consolidators)
                {
                    algorithm.SubscriptionManager.RemoveConsolidator(security.Symbol, consolidator);
                }

                // the symbol is selected again and gets its data subscription back
                historicalPrice = 50m;
                security.AddData(subscription);
                security.MakeTradable();
                algorithm.Securities.Add(security);

                model.OnSecuritiesChanged(algorithm, SecurityChangesTests.CreateNonInternal(new[] { security }, Enumerable.Empty<Security>()));

                // the price is now far below the SMA computed for the previous subscription
                security.SetMarketPrice(new TradeBar { Time = time.AddMinutes(1), Symbol = Symbols.AAPL, Close = 50m });
                security.SetMarketPrice(new Tick(time.AddMinutes(1), Symbols.AAPL, 50m, 50.01m));

                // ask price of $50.01 is above the new SMA of $50, so selling is favorable
                model.Execute(algorithm, new IPortfolioTarget[] { new PortfolioTarget(Symbols.AAPL, -10) });
                orderProcessor.ProcessSynchronousEvents();

                var orders = orderProcessor.GetOrders().ToList();
                Assert.AreEqual(1, orders.Count);
                Assert.AreEqual(-10, orders[0].Quantity);
            }
            finally
            {
                orderProcessor.Exit();
                brokerage?.Dispose();
            }
        }

        private static IEnumerable<Slice> CreateHistory(DateTime time, decimal price)
        {
            for (var i = 0; i < 3; i++)
            {
                yield return new Slice(time.AddMinutes(i),
                    new List<BaseData>
                    {
                        new TradeBar
                        {
                            Time = time.AddMinutes(i),
                            Symbol = Symbols.AAPL,
                            Open = price,
                            High = price,
                            Low = price,
                            Close = price,
                            Volume = 100m
                        }
                    }, time.AddMinutes(i));
            }
        }

        private static IExecutionModel GetExecutionModel(Language language)
        {
            const int period = 2;
            const decimal deviations = 1.5m;

            if (language == Language.Python)
            {
                using (Py.GIL())
                {
                    const string name = nameof(StandardDeviationExecutionModel);
                    var instance = Py.Import(name).GetAttr(name).Invoke(period.ToPython(), deviations.ToPython());
                    return new ExecutionModelPythonWrapper(instance);
                }
            }

            return new StandardDeviationExecutionModel(period, deviations);
        }
    }
}
