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
            Globals.Reset();

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
                .Setup<decimal>("GetBenchmarkValue")
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
            protectedMockResultHandler.VerifyGet<IAlgorithm>("Algorithm", Times.Exactly(6));

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

            public void SetAlgorithmDirect(IAlgorithm algorithm)
            {
                Algorithm = algorithm;
            }

            public void CallSetAlgorithmState(string error, string stack) => SetAlgorithmState(error, stack);

            public bool CallTrySetRuntimeStatistic(string key, string value) => TrySetRuntimeStatistic(key, value);

            public Dictionary<string, string> GetRuntimeStatistics => RuntimeStatistics;

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

        [TestCase("Some readable value $1,234.56", true)]
        [TestCase("Long Only Strategy", true)]
        [TestCase("123456789012345678901234567890", true)]
        [TestCase("BUY", true)]
        [TestCase("TG9yZW0gaXBzdW0gZG9sb3Igc2l0IGFtZXQ=", false)]
        [TestCase("aGVsbG8gd29ybGQ=", false)]
        [TestCase("48656c6c6f20576f726c6421", false)]
        [TestCase("DEADBEEFDEADBEEF", false)]
        [TestCase("Stop Loss Hit 3 Times Today", true)]
        [TestCase("Avg Fill Price 123.45 USD", true)]
        [TestCase("SPY,QQQ,IWM,TLT,GLD,USO", true)]
        public void RuntimeStatisticRejectsEncodedValues(string value, bool expected)
        {
            var handler = new BaseResultsHandlerTestable(AlgorithmId);

            Assert.AreEqual(expected, handler.CallTrySetRuntimeStatistic("Key", value));
            Assert.AreEqual(expected, handler.CallTrySetRuntimeStatistic(value, "value"));
        }

        [Test]
        public void RuntimeStatisticTruncatesLongKeysAndValues()
        {
            var handler = new BaseResultsHandlerTestable(AlgorithmId);
            var longText = string.Join(" ", Enumerable.Repeat("word", 100));
            Assert.Greater(longText.Length, BaseResultsHandler.MaxRuntimeStatisticLength);

            Assert.IsTrue(handler.CallTrySetRuntimeStatistic(longText, longText));

            var pair = handler.GetRuntimeStatistics.Single();
            Assert.AreEqual(BaseResultsHandler.MaxRuntimeStatisticLength, pair.Key.Length);
            Assert.AreEqual(BaseResultsHandler.MaxRuntimeStatisticLength, pair.Value.Length);
        }

        [Test]
        public void RuntimeStatisticCapsCount()
        {
            var handler = new BaseResultsHandlerTestable(AlgorithmId);
            for (var i = 0; i < BaseResultsHandler.MaxRuntimeStatisticsCount; i++)
            {
                Assert.IsTrue(handler.CallTrySetRuntimeStatistic($"Key {i}", $"{i}"));
            }

            Assert.IsFalse(handler.CallTrySetRuntimeStatistic("One too many", "1"));
            Assert.AreEqual(BaseResultsHandler.MaxRuntimeStatisticsCount, handler.GetRuntimeStatistics.Count);

            // updating an existing key is still allowed
            Assert.IsTrue(handler.CallTrySetRuntimeStatistic("Key 0", "updated"));
            Assert.AreEqual("updated", handler.GetRuntimeStatistics["Key 0"]);
        }

        [Test]
        public void RuntimeErrorSetsAlgorithmRunTimeErrorAndStatus()
        {
            var handler = new BaseResultsHandlerTestable(AlgorithmId);
            var algorithm = new QCAlgorithm();
            handler.SetAlgorithmDirect(algorithm);

            handler.CallSetAlgorithmState("Something went wrong", "stack trace here");

            Assert.IsNotNull(algorithm.RunTimeError);
            Assert.AreEqual("Something went wrong", algorithm.RunTimeError.Message);
            Assert.AreEqual(AlgorithmStatus.RuntimeError, algorithm.Status);
        }

        [Test]
        public void RuntimeErrorDoesNotOverwriteExistingRunTimeError()
        {
            var handler = new BaseResultsHandlerTestable(AlgorithmId);
            var algorithm = new QCAlgorithm();
            handler.SetAlgorithmDirect(algorithm);

            handler.CallSetAlgorithmState("First error", "");
            handler.CallSetAlgorithmState("Second error", "");

            Assert.AreEqual("First error", algorithm.RunTimeError.Message);
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
