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
 *
*/

using System;
using System.Collections.Generic;
using NodaTime;
using NUnit.Framework;
using QuantConnect.Algorithm;
using QuantConnect.Configuration;
using QuantConnect.Interfaces;
using QuantConnect.Packets;
using QuantConnect.Benchmarks;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Tests.Engine.DataFeeds;
using QuantConnect.Statistics;
using QuantConnect.Securities;
using System.Linq;

namespace QuantConnect.Tests.Algorithm
{
    [TestFixture]
    public class AlgorithmBenchmarkTests
    {
        private TestBenchmarkAlgorithm _algorithm;

        [TearDown]
        public void TearDown()
        {
            Config.Reset();
        }

        [TestCase(SecurityType.Forex)]
        [TestCase(SecurityType.Equity)]
        [TestCase(SecurityType.Crypto)]
        [TestCase(SecurityType.Index)]
        [TestCase(SecurityType.Option)]
        [TestCase(SecurityType.Future)]
        [TestCase(SecurityType.Cfd)]
        public void SetBenchmarksSecurityTypes(SecurityType securityType)
        {

            _algorithm = BenchmarkTestSetupHandler.TestAlgorithm = new TestBenchmarkAlgorithm(securityType);
            _algorithm.StartDateToUse = new DateTime(2014, 05, 03);
            _algorithm.EndDateToUse = new DateTime(2014, 05, 04);

            var results = AlgorithmRunner.RunLocalBacktest(nameof(TestBenchmarkAlgorithm),
                new Dictionary<string, string> { { PerformanceMetrics.TotalOrders, "0" } },
                Language.CSharp,
                AlgorithmStatus.Completed,
                setupHandler: "BenchmarkTestSetupHandler");


            var benchmark = _algorithm.Benchmark as SecurityBenchmark;
            Assert.IsNotNull(benchmark);
            Assert.AreEqual(securityType, benchmark.Security.Type);
            Assert.IsNull(_algorithm.RunTimeError);
        }

        [TestCase(Resolution.Daily)]
        [TestCase(Resolution.Hour)]
        [TestCase(Resolution.Minute)]
        [TestCase(Resolution.Second)]
        [TestCase(Resolution.Daily, true)]
        [TestCase(Resolution.Hour, true)]
        [TestCase(Resolution.Minute, true)]
        [TestCase(Resolution.Second, true)]
        public void MisalignedBenchmarkAndAlgorithmTimeZones(Resolution resolution, bool useUniverseSubscription = false)
        {
            // Verify that if we have algorithm:
            // - subscribed to a daily resolution via universe or directly
            // - a benchmark with timezone that is not algorithm time zone
            // that we post an warning via log that statistics will be affected

            // Setup a empty algorithm for the test
            var algorithm = new QCAlgorithm();
            var dataManager = new DataManagerStub(algorithm, new MockDataFeed(), liveMode: true);
            algorithm.SubscriptionManager.SetDataManager(dataManager);

            if (useUniverseSubscription)
            {
                // Change our universe resolution
                algorithm.UniverseSettings.Resolution = resolution;
            }
            else
            {
                // subscribe to an equity in our provided resolution
                algorithm.AddEquity("AAPL", resolution);
            }

            // Default benchmark is SPY which is NY TimeZone,
            // Set timezone to UTC.
            algorithm.SetTimeZone(DateTimeZone.Utc);
            algorithm.PostInitialize();

            // Verify if our log is there (Should only be there in Daily case)
            switch (resolution)
            {
                case Resolution.Daily:
                    if (algorithm.LogMessages.TryPeek(out string result))
                    {
                        Assert.IsTrue(result.Contains("Using a security benchmark of a different timezone", StringComparison.InvariantCulture));
                    }
                    else
                    {
                        Assert.Fail("Warning was not posted");
                    }
                    break;
                default:
                    Assert.AreEqual(0, algorithm.LogMessages.Count);
                    break;
            }
        }

        [Test]
        public void BenchmarkIsNotInitializeWithCustomSecurityInitializer()
        {
            var algorithm = new QCAlgorithm();
            var dataManager = new DataManagerStub(algorithm, new MockDataFeed());
            algorithm.SubscriptionManager.SetDataManager(dataManager);

            var securityInitializer = new CustomSecurityInitializer();
            algorithm.SetSecurityInitializer(securityInitializer);

            var spy = algorithm.AddEquity("SPY");

            algorithm.SetBenchmark("AAPL");
            var aapl = (algorithm.Benchmark as SecurityBenchmark).Security;

            algorithm.PostInitialize();

            Assert.IsTrue(securityInitializer.InitializedSecurities.Contains(spy));
            Assert.IsFalse(securityInitializer.InitializedSecurities.Contains(aapl));
        }

        [Test]
        public void BenchmarkIsNotAffectedBySecuritySeederDataNormalizationMode()
        {
            var algorithm = new TestBenchmarkDataNormalizationModeAlgorithm();
            BenchmarkTestSetupHandler.TestAlgorithm = algorithm;
            algorithm.StartDateToUse = new DateTime(2013, 10, 07);
            algorithm.EndDateToUse = new DateTime(2013, 10, 11);

            var results = AlgorithmRunner.RunLocalBacktest(nameof(TestBenchmarkAlgorithm),
                new Dictionary<string, string> { { PerformanceMetrics.TotalOrders, "0" } },
                Language.CSharp,
                AlgorithmStatus.Completed,
                setupHandler: nameof(BenchmarkTestSetupHandler));

            var benchmark = algorithm.Benchmark as SecurityBenchmark;
            Assert.IsNotNull(benchmark);
            Assert.AreEqual(Symbols.SPY, benchmark.Security.Symbol);

            // All values must be between 142 and 148 (expected adjusted data for the time range) for the benchmark
            Assert.IsTrue(algorithm.BenchmarkValues.All(x => x >= 142m && x <= 148m),
                $"Benchmark values are:\n{string.Join('\n', algorithm.BenchmarkValues)}");
        }

        public class BenchmarkTestSetupHandler : AlgorithmRunner.RegressionSetupHandlerWrapper
        {
            public static TestBenchmarkAlgorithm TestAlgorithm { get; set; }

            public override IAlgorithm CreateAlgorithmInstance(AlgorithmNodePacket algorithmNodePacket, string assemblyPath)
            {
                Algorithm = TestAlgorithm;
                return Algorithm;
            }
        }

        public class TestBenchmarkAlgorithm : QCAlgorithm
        {
            private Symbol _symbol;

            public SecurityType SecurityType { get; set; }

            public DateTime StartDateToUse { get; set; }

            public DateTime EndDateToUse { get; set; }

            public int WarmUpDataCount { get; set; }

            public TestBenchmarkAlgorithm(SecurityType securityType)
            {
                SecurityType = securityType;
            }

            public override void Initialize()
            {
                SetStartDate(StartDateToUse);
                SetEndDate(EndDateToUse);

                _symbol = Symbols.GetBySecurityType(SecurityType);
                AddSecurity(_symbol);
                SetBenchmark(_symbol);
            }
        }

        public class TestBenchmarkDataNormalizationModeAlgorithm : TestBenchmarkAlgorithm
        {
            public List<decimal> BenchmarkValues { get; } = new();

            public TestBenchmarkDataNormalizationModeAlgorithm()
                : base(SecurityType.Equity)
            {
            }

            public override void Initialize()
            {
                SetStartDate(StartDateToUse);
                SetEndDate(EndDateToUse);

                UniverseSettings.DataNormalizationMode = DataNormalizationMode.Raw;
                // If the benchmark is initialized using this security initializer, the security seeder would source data
                // for using the data normalization mode from the UniverseSettings, which is set to Raw
                SetSecurityInitializer(new BrokerageModelSecurityInitializer(BrokerageModel, new FuncSecuritySeeder(GetLastKnownPrices)));

                SetBenchmark("SPY");

                Schedule.On(DateRules.EveryDay(), TimeRules.Every(TimeSpan.FromHours(1)), () =>
                {
                    var value = (Benchmark as SecurityBenchmark).Evaluate(UtcTime);
                    BenchmarkValues.Add(value);
                    Log($"Benchmark: {value}");
                });
            }
        }

        public class CustomSecurityInitializer : ISecurityInitializer
        {
            public HashSet<Security> InitializedSecurities { get; } = new();

            public void Initialize(Security security)
            {
                InitializedSecurities.Add(security);
            }
        }
    }
}
