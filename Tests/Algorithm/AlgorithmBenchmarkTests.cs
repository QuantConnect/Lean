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
using NUnit.Framework;
using QuantConnect.Algorithm;
using QuantConnect.Configuration;
using QuantConnect.Interfaces;
using QuantConnect.Packets;
using QuantConnect.Benchmarks;

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
                new Dictionary<string, string> { { "Total Trades", "0" } },
                null,
                Language.CSharp,
                AlgorithmStatus.Completed,
                setupHandler: "BenchmarkTestSetupHandler");


            var benchmark = _algorithm.Benchmark as SecurityBenchmark;
            Assert.IsNotNull(benchmark);
            Assert.AreEqual(securityType, benchmark.Security.Type);
            Assert.IsNull(_algorithm.RunTimeError);
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
    }
}
