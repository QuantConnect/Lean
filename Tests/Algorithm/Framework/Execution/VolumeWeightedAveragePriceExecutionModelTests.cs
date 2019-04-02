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
    public class VolumeWeightedAveragePriceExecutionModelTests
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
            algorithm.Transactions.SetOrderProcessor(orderProcessor.Object);

            var model = GetExecutionModel(language);
            algorithm.SetExecution(model);

            var changes = new SecurityChanges(Enumerable.Empty<Security>(), Enumerable.Empty<Security>());
            model.OnSecuritiesChanged(algorithm, changes);

            model.Execute(algorithm, new IPortfolioTarget[0]);

            Assert.AreEqual(0, actualOrdersSubmitted.Count);
        }

        [TestCase(Language.CSharp, new[] { 270d, 260d, 250d }, 5000, 1, 10)]
        [TestCase(Language.Python, new[] { 270d, 260d, 250d }, 5000, 1, 10)]
        [TestCase(Language.CSharp, new[] { 270d, 260d, 250d }, 500, 1, 5)]
        [TestCase(Language.Python, new[] { 270d, 260d, 250d }, 500, 1, 5)]
        [TestCase(Language.CSharp, new[] { 270d, 260d, 250d }, 50, 0, 0)]
        [TestCase(Language.Python, new[] { 270d, 260d, 250d }, 50, 0, 0)]
        [TestCase(Language.CSharp, new[] { 230d, 240d, 250d }, 50000, 0, 0)]
        [TestCase(Language.Python, new[] { 230d, 240d, 250d }, 50000, 0, 0)]
        public void OrdersAreSubmittedWhenRequiredForTargetsToExecute(
            Language language,
            double[] historicalPrices,
            decimal lastVolume,
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
            algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(algorithm));
            algorithm.SetPandasConverter();
            algorithm.SetHistoryProvider(historyProvider.Object);
            algorithm.SetDateTime(time.AddMinutes(5));

            var security = algorithm.AddEquity(Symbols.AAPL.Value);
            security.SetMarketPrice(new TradeBar { Value = 250, Volume = lastVolume });

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

            algorithm.History(new List<Symbol> { security.Symbol }, historicalPrices.Length, Resolution.Minute)
                .PushThroughConsolidators(symbol => algorithm.Securities[symbol].Subscriptions.First().Consolidators.First());

            var targets = new IPortfolioTarget[] { new PortfolioTarget(security.Symbol, 10) };
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

        private static IExecutionModel GetExecutionModel(Language language)
        {
            if (language == Language.Python)
            {
                using (Py.GIL())
                {
                    const string name = nameof(VolumeWeightedAveragePriceExecutionModel);
                    var instance = Py.Import(name).GetAttr(name).Invoke();
                    return new ExecutionModelPythonWrapper(instance);
                }
            }

            return new VolumeWeightedAveragePriceExecutionModel();
        }
    }
}
