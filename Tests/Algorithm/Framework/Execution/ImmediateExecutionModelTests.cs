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
using QuantConnect.Algorithm.Framework.Execution;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Lean.Engine.TransactionHandlers;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Python;
using QuantConnect.Securities;
using QuantConnect.Tests.Common.Data.UniverseSelection;
using QuantConnect.Tests.Engine.DataFeeds;

namespace QuantConnect.Tests.Algorithm.Framework.Execution
{
    [TestFixture]
    public class ImmediateExecutionModelTests
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

            var algorithm = new AlgorithmStub();
            algorithm.Transactions.SetOrderProcessor(orderProcessor.Object);

            var model = GetExecutionModel(language);
            algorithm.SetExecution(model);

            var changes = SecurityChangesTests.CreateNonInternal(Enumerable.Empty<Security>(), Enumerable.Empty<Security>());
            model.OnSecuritiesChanged(algorithm, changes);

            model.Execute(algorithm, new IPortfolioTarget[0]);

            Assert.AreEqual(0, actualOrdersSubmitted.Count);
        }

        [TestCase(Language.CSharp, new[] { 270d, 260d, 250d }, 0, 1, 10)]
        [TestCase(Language.CSharp, new[] { 270d, 260d, 250d }, 3, 1, 7)]
        [TestCase(Language.Python, new[] { 270d, 260d, 250d }, 0, 1, 10)]
        [TestCase(Language.Python, new[] { 270d, 260d, 250d }, 3, 1, 7)]
        public void OrdersAreSubmittedImmediatelyForTargetsToExecute(
            Language language,
            double[] historicalPrices,
            decimal openOrdersQuantity,
            int expectedOrdersSubmitted,
            decimal expectedTotalQuantity)
        {
            var time = new DateTime(2018, 8, 2, 16, 0, 0);
            var historyProvider = new Mock<IHistoryProvider>();
            historyProvider.Setup(m => m.GetHistory(It.IsAny<IEnumerable<HistoryRequest>>(), It.IsAny<DateTimeZone>()))
                .Returns(historicalPrices.Select((x, i) =>
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

            var algorithm = new AlgorithmStub();
            algorithm.SetHistoryProvider(historyProvider.Object);
            algorithm.SetDateTime(time.AddMinutes(5));

            var security = algorithm.AddEquity(Symbols.AAPL.Value);
            security.SetMarketPrice(new TradeBar { Value = 250 });

            algorithm.SetFinishedWarmingUp();

            var orderProcessor = GetAndSetBrokerageTransactionHandler(algorithm, out var brokerage);

            try
            {
                var openOrderRequest = new SubmitOrderRequest(OrderType.Market, SecurityType.Equity, Symbols.AAPL, openOrdersQuantity, 0, 0, DateTime.MinValue, "");
                openOrderRequest.SetOrderId(1);
                var order = Order.CreateOrder(openOrderRequest);
                orderProcessor.AddOpenOrder(order, algorithm);

                var model = GetExecutionModel(language, false);
                algorithm.SetExecution(model);

                var changes = SecurityChangesTests.CreateNonInternal(new[] { security }, Enumerable.Empty<Security>());
                model.OnSecuritiesChanged(algorithm, changes);

                var targets = new IPortfolioTarget[] { new PortfolioTarget(Symbols.AAPL, 10) };
                model.Execute(algorithm, targets);
                orderProcessor.ProcessSynchronousEvents();

                Assert.AreEqual(expectedOrdersSubmitted + 1, orderProcessor.GetOpenOrders().Count);

                var executionOrder = orderProcessor.GetOpenOrders().OrderByDescending(o => o.Id).First();
                Assert.AreEqual(expectedTotalQuantity, executionOrder.Quantity);
                Assert.AreEqual(algorithm.UtcTime, executionOrder.Time);
            }
            finally
            {
                brokerage.Dispose();
            }
        }

        [TestCase(Language.CSharp)]
        [TestCase(Language.Python)]
        public void PartiallyFilledOrdersAreTakenIntoAccount(Language language)
        {
            var algorithm = new AlgorithmStub();
            var security = algorithm.AddEquity(Symbols.AAPL.Value);
            security.SetMarketPrice(new TradeBar { Value = 250 });

            algorithm.SetFinishedWarmingUp();

            var orderProcessor = GetAndSetBrokerageTransactionHandler(algorithm, out var brokerage);

            try
            {
                var openOrderRequest = new SubmitOrderRequest(OrderType.Market, SecurityType.Equity, Symbols.AAPL, 100, 0, 0, DateTime.MinValue, "");
                openOrderRequest.SetOrderId(1);
                var order = Order.CreateOrder(openOrderRequest);
                orderProcessor.AddOpenOrder(order, algorithm);

                brokerage.OnOrderEvent(new OrderEvent(order.Id, order.Symbol, DateTime.MinValue, OrderStatus.PartiallyFilled, OrderDirection.Buy, 250, 70, OrderFee.Zero));

                var model = GetExecutionModel(language);
                algorithm.SetExecution(model);

                var changes = SecurityChangesTests.CreateNonInternal(Enumerable.Empty<Security>(), Enumerable.Empty<Security>());
                model.OnSecuritiesChanged(algorithm, changes);

                var targets = new IPortfolioTarget[] { new PortfolioTarget(Symbols.AAPL, 80) };
                model.Execute(algorithm, targets);
                orderProcessor.ProcessSynchronousEvents();

                Assert.AreEqual(2, orderProcessor.OrdersCount);

                // Remaining quantity for partially filled order = 100 - 70 = 30
                // Holdings from partially filled order = 70
                // Quantity submitted = target - holdings - remaining open orders quantity = 80 - 70 - 30 = -20
                Assert.AreEqual(-20, orderProcessor.GetOpenOrders().OrderByDescending(o => o.Id).First().Quantity);
            }
            finally
            {
                brokerage.Dispose();
            }
        }

        [TestCase(Language.CSharp)]
        [TestCase(Language.Python)]
        public void NonFilledAsyncOrdersAreTakenIntoAccount(Language language)
        {
            var algorithm = new AlgorithmStub();
            var security = algorithm.AddEquity(Symbols.AAPL.Value);
            security.SetMarketPrice(new TradeBar { Value = 250 });

            algorithm.SetFinishedWarmingUp();

            var orderProcessor = GetAndSetBrokerageTransactionHandler(algorithm, out var brokerage);

            try
            {
                var model = GetExecutionModel(language, true);
                algorithm.SetExecution(model);

                var changes = SecurityChangesTests.CreateNonInternal(Enumerable.Empty<Security>(), Enumerable.Empty<Security>());
                model.OnSecuritiesChanged(algorithm, changes);

                var targetQuantity = 80;
                var targets = new IPortfolioTarget[] { new PortfolioTarget(Symbols.AAPL, targetQuantity) };
                model.Execute(algorithm, targets);
                orderProcessor.ProcessSynchronousEvents();

                Assert.AreEqual(1, orderProcessor.OrdersCount);

                // Quantity submitted = 80
                Assert.AreEqual(targetQuantity, orderProcessor.GetOpenOrders().First().Quantity);

                var newTargetQuantity = 100;
                var newTargets = new IPortfolioTarget[] { new PortfolioTarget(Symbols.AAPL, newTargetQuantity) };
                model.Execute(algorithm, newTargets);
                orderProcessor.ProcessSynchronousEvents();

                Assert.AreEqual(2, orderProcessor.OrdersCount);

                // Remaining quantity for non-filled order = targetQuantity = 80
                // Quantity submitted = newTargetQuantity - targetQuantity = 100 - 80 = 20
                Assert.AreEqual(newTargetQuantity - targetQuantity, orderProcessor.GetOpenOrders().OrderByDescending(o => o.Id).First().Quantity);
            }
            finally
            {
                brokerage.Dispose();
            }
        }

        [TestCase(Language.CSharp, -1)]
        [TestCase(Language.Python, -1)]
        [TestCase(Language.CSharp, 1)]
        [TestCase(Language.Python, 1)]
        public void LotSizeIsRespected(Language language, int side)
        {
            var algorithm = new AlgorithmStub();
            algorithm.Settings.MinimumOrderMarginPortfolioPercentage = 0;
            var security = algorithm.AddForex(Symbols.EURUSD.Value);
            algorithm.Portfolio.SetCash("EUR", 1, 1);
            security.SetMarketPrice(new TradeBar { Value = 250 });

            algorithm.SetFinishedWarmingUp();

            var orderProcessor = GetAndSetBrokerageTransactionHandler(algorithm, out var brokerage);

            try
            {
                var model = GetExecutionModel(language);
                algorithm.SetExecution(model);

                model.Execute(algorithm,
                    new IPortfolioTarget[] { new PortfolioTarget(Symbols.EURUSD, security.SymbolProperties.LotSize * 1.5m * side) });
                orderProcessor.ProcessSynchronousEvents();

                var orders = orderProcessor.GetOrders().ToList();
                Assert.AreEqual(1, orders.Count);
                Assert.AreEqual(security.SymbolProperties.LotSize * side, orders.Single().Quantity);
            }
            finally
            {
                brokerage.Dispose();
            }
        }

        [Test]
        public void CustomPythonExecutionModelDoesNotRequireOnOrderEventMethod()
        {
            using var _ = Py.GIL();
            const string pythonCode = @"
class CustomExecutionModel:
    def execute(self, algorithm, targets):
        pass
    def on_securities_changed(self, algorithm, changes):
        pass
";
            using var module = PyModule.FromString("CustomExecutionModelModule", pythonCode);
            using var instance = module.GetAttr("CustomExecutionModel").Invoke();
            var model = new ExecutionModelPythonWrapper(instance);
            Assert.DoesNotThrow(() => model.OnOrderEvent(new AlgorithmStub(), new OrderEvent()));
        }

        private static IExecutionModel GetExecutionModel(Language language, bool asynchronous = false)
        {
            if (language == Language.Python)
            {
                using (Py.GIL())
                {
                    const string name = nameof(ImmediateExecutionModel);
                    var instance = Py.Import(name).GetAttr(name).Invoke(asynchronous);
                    return new ExecutionModelPythonWrapper(instance);
                }
            }

            return new ImmediateExecutionModel(asynchronous);
        }


        internal static BrokerageTransactionHandler GetAndSetBrokerageTransactionHandler(IAlgorithm algorithm, out NullBrokerage brokerage)
        {
            brokerage = new NullBrokerage();
            var orderProcessor = new BrokerageTransactionHandler();
            orderProcessor.Initialize(algorithm, brokerage, new BacktestingResultHandler());
            algorithm.Transactions.SetOrderProcessor(orderProcessor);
            algorithm.Transactions.MarketOrderFillTimeout = TimeSpan.Zero;

            return orderProcessor;
        }
    }
}
