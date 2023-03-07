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
using System.Linq;
using Castle.DynamicProxy;
using Moq;
using Moq.Protected;
using NUnit.Framework;
using QuantConnect.Algorithm;
using QuantConnect.Configuration;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Logging;
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


            protectedMockResultHandler.Setup("SampleEquity", ItExpr.IsAny<DateTime>(), ItExpr.IsAny<decimal>());
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
                    ItExpr.IsAny<DateTime>(), ItExpr.IsAny<decimal>(), ItExpr.IsAny<string>())
                .Callback((string chartName, string seriesName, int seriesIndex, SeriesType seriesType, DateTime time, decimal value, string unit) =>
                {
                    sampleInvocations.Add(new SampleParams
                    {
                        ChartName = chartName,
                        SeriesName = seriesName,
                        SeriesIndex = seriesIndex,
                        SeriesType = seriesType,
                        Time = time,
                        Value = value,
                        Unit = unit
                    });
                })
                .Verifiable();

            // Now set everything up for the SampleExposure method
            var timeKeeper = new TimeKeeper(new DateTime(2014, 6, 24, 12, 0, 0).ConvertToUtc(TimeZones.NewYork), new[] { TimeZones.NewYork });
            var securities = new SecurityManager(timeKeeper);
            var transactions = new SecurityTransactionManager(null, securities);
            var portfolio = new SecurityPortfolioManager(securities, transactions);

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
            // and once by BaseResultHandler.GetPortfolioValue()
            protectedMockResultHandler.VerifyGet<IAlgorithm>("Algorithm", Times.Exactly(2));

            // Sample should've been called twice, by BaseResultHandler.SampleExposure(), once for the long and once for the short positions
            protectedMockResultHandler.Verify("Sample", Times.Exactly(2), ItExpr.IsAny<string>(), ItExpr.IsAny<string>(),
                ItExpr.IsAny<int>(), ItExpr.IsAny<SeriesType>(), ItExpr.IsAny<DateTime>(), ItExpr.IsAny<decimal>(), ItExpr.IsAny<string>());
            Assert.AreEqual(2, sampleInvocations.Count);

            var positionSides = new[] { PositionSide.Long, PositionSide.Short };
            for (int i = 0; i < sampleInvocations.Count; i++)
            {
                var invocation = sampleInvocations[i];
                Assert.AreEqual("Exposure", invocation.ChartName);
                Assert.AreEqual($"{spy.Type} - {positionSides[i]} Ratio", invocation.SeriesName);
                Assert.AreEqual(0, invocation.SeriesIndex);
                Assert.AreEqual(SeriesType.Line, invocation.SeriesType);
                Assert.AreEqual(timeKeeper.UtcTime, invocation.Time);
                Assert.AreEqual("", invocation.Unit);
            }

            var longInvocation = sampleInvocations[0];
            var shortInvocation = sampleInvocations[1];

            if (holdingsQuantity == 0)
            {
                Assert.AreEqual(0, longInvocation.Value);
                Assert.AreEqual(0, shortInvocation.Value);
            }
            else
            {
                var expectedExposure = Math.Round(spy.Holdings.HoldingsValue / portfolio.TotalPortfolioValue, 4);
                if (holdingsQuantity > 0)
                {
                    Assert.AreEqual(expectedExposure, longInvocation.Value);
                    Assert.AreEqual(0, shortInvocation.Value);
                }
                else
                {
                    Assert.AreEqual(0, longInvocation.Value);
                    Assert.AreEqual(expectedExposure, shortInvocation.Value);
                }
            }
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
                                           DateTime time,
                                           decimal value,
                                           string unit = "$")
            {
                throw new NotImplementedException();
            }

            protected override void AddToLogStore(string message)
            {
            }
        }

        private struct SampleParams
        {
            public string ChartName { get; set; }
            public string SeriesName { get; set; }
            public int SeriesIndex { get; set; }
            public SeriesType SeriesType { get; set; }
            public DateTime Time { get; set; }
            public decimal Value { get; set; }
            public string Unit { get; set; }
        }
    }
}
