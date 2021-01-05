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
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Orders;
using QuantConnect.Securities;
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
                .Callback((SubmitOrderRequest request) => actualOrdersSubmitted.Add(request));

            var algorithm = new QCAlgorithm();
            algorithm.SetPandasConverter();
            algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(algorithm));
            algorithm.Transactions.SetOrderProcessor(orderProcessor.Object);

            var model = GetExecutionModel(language);
            algorithm.SetExecution(model);

            var changes = new SecurityChanges(Enumerable.Empty<Security>(), Enumerable.Empty<Security>());
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
            var actualOrdersSubmitted = new List<SubmitOrderRequest>();

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
                        })));

            var algorithm = new QCAlgorithm();
            algorithm.SetPandasConverter();
            algorithm.SetHistoryProvider(historyProvider.Object);
            algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(algorithm));
            algorithm.SetDateTime(time.AddMinutes(5));

            var security = algorithm.AddEquity(Symbols.AAPL.Value);
            security.SetMarketPrice(new TradeBar { Value = currentPrice });

            algorithm.SetFinishedWarmingUp();

            var orderProcessor = new Mock<IOrderProcessor>();
            orderProcessor.Setup(m => m.Process(It.IsAny<SubmitOrderRequest>()))
                .Returns((SubmitOrderRequest request) => new OrderTicket(algorithm.Transactions, request))
                .Callback((SubmitOrderRequest request) => actualOrdersSubmitted.Add(request));
            orderProcessor.Setup(m => m.GetOpenOrders(It.IsAny<Func<Order, bool>>()))
                .Returns(new List<Order>());
            algorithm.Transactions.SetOrderProcessor(orderProcessor.Object);

            var model = GetExecutionModel(language);
            algorithm.SetExecution(model);

            var changes = new SecurityChanges(new[] { security }, Enumerable.Empty<Security>());
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
                .Returns(historicalPrices.Select((x, i) => new Slice(time.AddMinutes(i), new List<BaseData> { func(x, i) })));

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

            var changes = new SecurityChanges(new[] { security }, Enumerable.Empty<Security>());
            Assert.DoesNotThrow(() => model.OnSecuritiesChanged(algorithm, changes));
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
