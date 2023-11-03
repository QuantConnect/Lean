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
using System.IO;
using Moq;
using Moq.Protected;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using QuantConnect.Algorithm;
using QuantConnect.Configuration;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Packets;
using QuantConnect.Securities;
using QuantConnect.Tests.Engine.DataFeeds;

namespace QuantConnect.Tests.Engine.Results
{
    [TestFixture]
    public class BaseResultsHandlerTests
    {
        private BaseResultsHandlerTestable _baseResultsHandler;
        private const string ResultsDestinationFolderKey = "results-destination-folder";
        private const string AlgorithmId = "MyAlgorithm";

        [TestCase(true, "./temp")]
        [TestCase(false, "IGNORED")]
        [Test]
        public void ResultsDestinationFolderIsCorrect(bool overrideDefault, string overrideValue)
        {
            Config.Reset();
            if (overrideDefault)
            {
                Config.Set(ResultsDestinationFolderKey, overrideValue);
            }

            _baseResultsHandler = new BaseResultsHandlerTestable(AlgorithmId);

            var expectedValue = overrideDefault ? overrideValue : Directory.GetCurrentDirectory();

            Assert.AreEqual(expectedValue, _baseResultsHandler.GetResultsDestinationFolder);
        }

        [Test]
        public void CheckSaveLogs()
        {
            _baseResultsHandler = new BaseResultsHandlerTestable(AlgorithmId);

            var tempPath = Path.GetTempPath();

            _baseResultsHandler.SetResultsDestinationFolder(tempPath);

            const string id = "test";
            var logEntries = new List<LogEntry>
            {
                new LogEntry("Message 1"),
                new LogEntry("Message 2"),
                new LogEntry("Message 3"),
            };

            var saveLocation = _baseResultsHandler.SaveLogs(id, logEntries);

            Assert.True(File.Exists(saveLocation));
            Assert.AreEqual(Path.Combine(tempPath, $"{id}-log.txt"), saveLocation);
        }

        [TestCase(100)]
        [TestCase(-100)]
        [TestCase(0)]
        public void ExposureIsCalculatedEvenWhenPortfolioIsNotInvested(decimal holdingsQuantity)
        {
            var mockResultHandler = new Mock<BaseResultsHandler>();
            mockResultHandler.CallBase = true;
            var protectedMockResultHandler = mockResultHandler.Protected();

            protectedMockResultHandler.Setup("SampleEquity", ItExpr.IsAny<DateTime>());
            protectedMockResultHandler.Setup("SampleBenchmark", ItExpr.IsAny<DateTime>(), ItExpr.IsAny<decimal>());
            protectedMockResultHandler
                .Setup<decimal>("GetBenchmarkValue", ItExpr.IsAny<DateTime>())
                .Returns(0m);
            protectedMockResultHandler.Setup("SamplePerformance", ItExpr.IsAny<DateTime>(), ItExpr.IsAny<decimal>());
            protectedMockResultHandler.Setup("SampleDrawdown", ItExpr.IsAny<DateTime>(), ItExpr.IsAny<decimal>());
            protectedMockResultHandler.Setup("SampleSalesVolume", ItExpr.IsAny<DateTime>());
            protectedMockResultHandler.Setup("SampleCapacity", ItExpr.IsAny<DateTime>());
            protectedMockResultHandler.Setup("SamplePortfolioTurnover", ItExpr.IsAny<DateTime>(), ItExpr.IsAny<decimal>());

            var sampleInvocations = new List<SampleParams>();
            protectedMockResultHandler
                .Setup("Sample", ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<int>(), ItExpr.IsAny<SeriesType>(),
                    ItExpr.IsAny<ISeriesPoint>(), ItExpr.IsAny<string>())
                .Callback((string chartName, string seriesName, int seriesIndex, SeriesType seriesType, ISeriesPoint value, string unit) =>
                {
                    sampleInvocations.Add(new SampleParams
                    {
                        ChartName = chartName,
                        SeriesName = seriesName,
                        SeriesIndex = seriesIndex,
                        SeriesType = seriesType,
                        Value = value,
                        Unit = unit
                    });
                })
                .Verifiable();

            // Now set everything up for the SampleExposure method
            var timeKeeper = new TimeKeeper(new DateTime(2014, 6, 24, 12, 0, 0).ConvertToUtc(TimeZones.NewYork), new[] { TimeZones.NewYork });
            var securities = new SecurityManager(timeKeeper);
            var transactions = new SecurityTransactionManager(null, securities);
            var portfolio = new SecurityPortfolioManager(securities, transactions, new AlgorithmSettings());

            var algorithm = new QCAlgorithm();
            algorithm.Securities = securities;
            algorithm.Transactions = transactions;
            algorithm.Portfolio = portfolio;
            algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(algorithm));

            var spy = algorithm.AddEquity("SPY");
            spy.Holdings = new SecurityHolding(spy, new IdentityCurrencyConverter(algorithm.AccountCurrency));
            spy.Holdings.UpdateMarketPrice(100m);
            spy.Holdings.SetHoldings(100m, holdingsQuantity);
            portfolio.InvalidateTotalPortfolioValue();

            protectedMockResultHandler.SetupGet<IAlgorithm>("Algorithm").Returns(algorithm).Verifiable();

            mockResultHandler.Object.Sample(timeKeeper.UtcTime);

            // BaseResultHandler.Algorithm property accessed once by BaseResultHandler.SampleExposure()
            // and once by BaseResultHandler.GetPortfolioValue() + 2 for sampling current equity value
            protectedMockResultHandler.VerifyGet<IAlgorithm>("Algorithm", Times.Exactly(4));

            // Sample should've been called twice, by BaseResultHandler.SampleExposure(), once for the long and once for the short positions
            protectedMockResultHandler.Verify("Sample", Times.Exactly(2), ItExpr.IsAny<string>(), ItExpr.IsAny<string>(),
                ItExpr.IsAny<int>(), ItExpr.IsAny<SeriesType>(), ItExpr.IsAny<ISeriesPoint>(), ItExpr.IsAny<string>());
            Assert.AreEqual(2, sampleInvocations.Count);

            var positionSides = new[] { PositionSide.Long, PositionSide.Short };
            for (int i = 0; i < sampleInvocations.Count; i++)
            {
                var invocation = sampleInvocations[i];
                Assert.AreEqual("Exposure", invocation.ChartName);
                Assert.AreEqual($"{spy.Type} - {positionSides[i]} Ratio", invocation.SeriesName);
                Assert.AreEqual(0, invocation.SeriesIndex);
                Assert.AreEqual(SeriesType.Line, invocation.SeriesType);
                Assert.AreEqual(timeKeeper.UtcTime, invocation.Value.Time);
                Assert.AreEqual("", invocation.Unit);
            }

            var longInvocation = sampleInvocations[0];
            var shortInvocation = sampleInvocations[1];

            if (holdingsQuantity == 0)
            {
                Assert.AreEqual(0, ((ChartPoint)longInvocation.Value).y);
                Assert.AreEqual(0, ((ChartPoint)shortInvocation.Value).y);
            }
            else
            {
                var expectedExposure = Math.Round(spy.Holdings.HoldingsValue / portfolio.TotalPortfolioValue, 4);
                if (holdingsQuantity > 0)
                {
                    Assert.AreEqual(expectedExposure, ((ChartPoint)longInvocation.Value).y);
                    Assert.AreEqual(0, ((ChartPoint)shortInvocation.Value).y);
                }
                else
                {
                    Assert.AreEqual(0, ((ChartPoint)longInvocation.Value).y);
                    Assert.AreEqual(expectedExposure, ((ChartPoint)shortInvocation.Value).y);
                }
            }
        }

        [Test]
        public void StoresTradedSecuritiesSubscriptionDataConfigs()
        {
            var algorithmId = "test-algorithm";
            var mockResultHandler = new Mock<MockableBaseResultHandler>(algorithmId);
            mockResultHandler.CallBase = true;
            var protectedMockResultHandler = mockResultHandler.Protected();

            var algorithm = new QCAlgorithm();
            algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(algorithm));

            var spy = algorithm.AddEquity("SPY", Resolution.Daily).Symbol;
            algorithm.AddEquity("SPY", Resolution.Hour);
            algorithm.AddEquity("SPY", Resolution.Minute);

            var es = algorithm.AddFuture("ES", Resolution.Minute).Symbol;
            algorithm.AddFuture("ES", Resolution.Second);
            algorithm.AddFuture("ES", Resolution.Tick);

            // This will not be traded
            algorithm.AddIndex("SPX");

            protectedMockResultHandler.SetupGet<IAlgorithm>("Algorithm").Returns(algorithm).Verifiable();

            var orderEvents = new List<OrderEvent>()
            {
                new OrderEvent(1, spy, new DateTime(2023, 11, 3, 11, 0, 0), OrderStatus.Filled, OrderDirection.Buy, 300, 10, OrderFee.Zero),
                new OrderEvent(2, spy, new DateTime(2023, 11, 3, 12, 0, 0), OrderStatus.Canceled, OrderDirection.Sell, 310, 5, OrderFee.Zero),
                new OrderEvent(3, es, new DateTime(2023, 11, 3, 13, 0, 0), OrderStatus.Submitted, OrderDirection.Buy, 4300, 10, OrderFee.Zero),
                new OrderEvent(3, es, new DateTime(2023, 11, 3, 13, 0, 1), OrderStatus.Filled, OrderDirection.Buy, 4300, 10, OrderFee.Zero),
            };

            mockResultHandler.Object.StoreTradedSubscriptions(orderEvents);

            // Verification

            // Algorithm should be accessed twice, once per each symbol with an order event
            protectedMockResultHandler.VerifyGet<IAlgorithm>("Algorithm", Times.Exactly(2));

            var filePath = mockResultHandler.Object.GetResultsPath($"{algorithmId}-traded-securities-subscriptions.json");
            Assert.IsTrue(File.Exists(filePath));

            var resultJson = JArray.Parse(File.ReadAllText(filePath));

            Assert.AreEqual(2, resultJson.Count);

            var spyJsonSubscription = resultJson[0] as JObject;
            Assert.AreEqual(spy, spyJsonSubscription["symbol"].ToObject<Symbol>());
            CollectionAssert.AreEqual(
                new List<TickType>() { TickType.Trade, TickType.Quote },
                spyJsonSubscription["tick-types"].ToObject<List<TickType>>());

            var esJsonSubscription = resultJson[1] as JObject;
            Assert.AreEqual(es, esJsonSubscription["symbol"].ToObject<Symbol>());
            CollectionAssert.AreEqual(
                new List<TickType>() { TickType.Quote },
                esJsonSubscription["tick-types"].ToObject<List<TickType>>());
        }

        private class BaseResultsHandlerTestable : BaseResultsHandler
        {
            public BaseResultsHandlerTestable(string algorithmId)
            {
                AlgorithmId = algorithmId;
            }

            public void SetResultsDestinationFolder(string folder)
            {
                ResultsDestinationFolder = folder;
            }
            public string GetResultsDestinationFolder => ResultsDestinationFolder;
            protected override void Run()
            {
                throw new NotImplementedException();
            }

            protected override void StoreResult(Packet packet)
            {
                throw new NotImplementedException();
            }

            protected override void Sample(string chartName,
                                           string seriesName,
                                           int seriesIndex,
                                           SeriesType seriesType,
                                           ISeriesPoint value,
                                           string unit = "$")
            {
                throw new NotImplementedException();
            }

            protected override void AddToLogStore(string message)
            {
            }
        }

        public class MockableBaseResultHandler : BaseResultsHandler
        {
            public MockableBaseResultHandler(string algorithmId)
            {
                AlgorithmId = algorithmId;
            }

            new public void StoreTradedSubscriptions(List<OrderEvent> orderEvents)
            {
                base.StoreTradedSubscriptions(orderEvents);
            }

            new public string GetResultsPath(string filename)
            {
                return base.GetResultsPath(filename);
            }

            protected override void Run()
            {
                throw new NotImplementedException();
            }

            protected override void Sample(string chartName, string seriesName, int seriesIndex, SeriesType seriesType, ISeriesPoint value,
                string unit = "$")
            {
                throw new NotImplementedException();
            }

            protected override void StoreResult(Packet packet)
            {
                throw new NotImplementedException();
            }
        }

        private struct SampleParams
        {
            public string ChartName { get; set; }
            public string SeriesName { get; set; }
            public int SeriesIndex { get; set; }
            public SeriesType SeriesType { get; set; }
            public ISeriesPoint Value { get; set; }
            public string Unit { get; set; }
        }
    }
}
