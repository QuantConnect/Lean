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
using Newtonsoft.Json;
using NUnit.Framework;
using QuantConnect.Algorithm.CSharp;
using QuantConnect.Configuration;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Packets;

namespace QuantConnect.Tests.Common.Packets
{
    [TestFixture]
    public class BacktestNodePacketTests
    {
        [SetUp]
        public void SetUp()
        {
            Log.DebuggingEnabled = false;
        }

        [TearDown]
        public void TearDown()
        {
            // clear the config
            Config.Reset();
        }

        [Test]
        public void JobDatesAreRespected()
        {
            var parameter = new RegressionTests.AlgorithmStatisticsTestParameters(nameof(BasicTemplateDailyAlgorithm),
                new Dictionary<string, string> {
                    { "Total Trades", "1" },
                    {"Average Win", "0%"},
                    { "Average Loss", "0%"},
                    { "Compounding Annual Return", "17.457%"},
                    { "Drawdown", "30.300%"},
                    { "Expectancy", "0"},
                    { "Net Profit", "37.900%"},
                    { "Sharpe Ratio", "0.682"},
                    { "Loss Rate", "0%"},
                    { "Win Rate", "0%"},
                    { "Profit-Loss Ratio", "0"},
                    { "Alpha", "0.205"},
                    { "Beta", "-0.122"},
                    { "Annual Standard Deviation", "0.269"},
                    { "Annual Variance", "0.073"},
                    { "Information Ratio", "0.016"},
                    { "Tracking Error", "0.415"},
                    { "Treynor Ratio", "-1.5"},
                    { "Total Fees", "$7.00"} },
                Language.CSharp,
                AlgorithmStatus.Completed);

            AlgorithmRunner.RunLocalBacktest(parameter.Algorithm,
                parameter.Statistics,
                parameter.AlphaStatistics,
                parameter.Language,
                parameter.ExpectedFinalStatus,
                startDate: new DateTime(2008, 10, 10),
                endDate: new DateTime(2010, 10, 10));
        }

        [Test]
        public void JobDatesAreRespectedByAddUniverseAtInitialize()
        {
            var parameter = new RegressionTests.AlgorithmStatisticsTestParameters(nameof(CoarseFundamentalTop3Algorithm),
                new Dictionary<string, string> {
                    { "Total Trades", "3" },
                    {"Average Win", "0%"},
                    { "Average Loss", "0%"},
                    { "Compounding Annual Return", "-41.768%"},
                    { "Drawdown", "0.300%"},
                    { "Expectancy", "0"},
                    { "Net Profit", "-0.296%"},
                    { "Sharpe Ratio", "-9.376"},
                    { "Loss Rate", "0%"},
                    { "Win Rate", "0%"},
                    { "Profit-Loss Ratio", "0"},
                    { "Alpha", "-0.827"},
                    { "Beta", "0.603"},
                    { "Annual Standard Deviation", "0.033"},
                    { "Annual Variance", "0.001"},
                    { "Information Ratio", "-53.28"},
                    { "Tracking Error", "0.022"},
                    { "Treynor Ratio", "-0.517"},
                    { "Total Fees", "$3.00"} },
                Language.CSharp,
                AlgorithmStatus.Completed);

            AlgorithmRunner.RunLocalBacktest(parameter.Algorithm,
                parameter.Statistics,
                parameter.AlphaStatistics,
                parameter.Language,
                parameter.ExpectedFinalStatus,
                startDate: new DateTime(2014, 03, 24),
                endDate: new DateTime(2014, 03, 25));
        }

        [Test, Parallelizable(ParallelScope.Self)]
        public void RoundTripNullJobDates()
        {
            var job = new BacktestNodePacket(1, 2, "3", null, 9m, $"{nameof(BacktestNodePacketTests)}.Pepe");

            var serialized = JsonConvert.SerializeObject(job);
            var job2 = JsonConvert.DeserializeObject<BacktestNodePacket>(serialized);

            Assert.AreEqual(job.BacktestId, job2.BacktestId);
            Assert.AreEqual(job.Name, job2.Name);
            Assert.IsNull(job.PeriodFinish);
            Assert.IsNull(job.PeriodStart);
            Assert.AreEqual(job.PeriodFinish, job2.PeriodFinish);
            Assert.AreEqual(job.PeriodStart, job2.PeriodStart);
            Assert.AreEqual(job.ProjectId, job2.ProjectId);
            Assert.AreEqual(job.SessionId, job2.SessionId);
            Assert.AreEqual(job.Language, job2.Language);
        }

        [Test, Parallelizable(ParallelScope.Self)]
        public void RoundTripWithJobDates()
        {
            var job = new BacktestNodePacket(1, 2, "3", null, 9m, $"{nameof(BacktestNodePacketTests)}.Pepe");
            job.PeriodStart = new DateTime(2019, 1, 1);
            job.PeriodFinish = new DateTime(2020, 1, 1);

            var serialized = JsonConvert.SerializeObject(job);
            var job2 = JsonConvert.DeserializeObject<BacktestNodePacket>(serialized);

            Assert.AreEqual(job.PeriodStart, job2.PeriodStart);
            Assert.AreEqual(job.PeriodFinish, job2.PeriodFinish);
        }

        [Test, Parallelizable(ParallelScope.Self)]
        public void RoundTripWithInitialCashAmount()
        {
            var job = new BacktestNodePacket(1, 2, "3", null, 9m, $"{nameof(BacktestNodePacketTests)}.Pepe");
            Assert.AreEqual(9m, job.CashAmount.Value.Amount);
            Assert.AreEqual(Currencies.USD, job.CashAmount.Value.Currency);

            var serialized = JsonConvert.SerializeObject(job);
            var job2 = JsonConvert.DeserializeObject<BacktestNodePacket>(serialized);
            Assert.AreEqual(job.CashAmount, job2.CashAmount);
        }

        [Test, Parallelizable(ParallelScope.Self)]
        public void RoundTripWithNullInitialCashAmount()
        {
            var job = new BacktestNodePacket(1, 2, "3", null, $"{nameof(BacktestNodePacketTests)}.Pepe");
            Assert.IsNull(job.CashAmount);

            var serialized = JsonConvert.SerializeObject(job);
            var job2 = JsonConvert.DeserializeObject<BacktestNodePacket>(serialized);
            Assert.AreEqual(job.CashAmount, job2.CashAmount);
        }

        [Test]
        public void InitialCashAmountIsRespected()
        {
            var parameter = new RegressionTests.AlgorithmStatisticsTestParameters(nameof(BasicTemplateDailyAlgorithm),
                new Dictionary<string, string> {
                    {"Total Trades", "1"},
                    {"Average Win", "0%"},
                    {"Average Loss", "0%"},
                    {"Compounding Annual Return", "246.546%"},
                    {"Drawdown", "1.200%"},
                    {"Expectancy", "0"},
                    {"Net Profit", "3.464%"},
                    {"Sharpe Ratio", "9.933"},
                    {"Loss Rate", "0%"},
                    {"Win Rate", "0%"},
                    {"Profit-Loss Ratio", "0"},
                    {"Alpha", "1.957"},
                    {"Beta", "-0.125"},
                    {"Annual Standard Deviation", "0.164"},
                    {"Annual Variance", "0.027"},
                    {"Information Ratio", "-4.577"},
                    {"Tracking Error", "0.225"},
                    {"Treynor Ratio", "-13.006"},
                    {"Total Fees", "$34.45"} // 10x times more than original BasicTemplateDailyAlgorithm
                },
                Language.CSharp,
                AlgorithmStatus.Completed);

            AlgorithmRunner.RunLocalBacktest(parameter.Algorithm,
                parameter.Statistics,
                parameter.AlphaStatistics,
                parameter.Language,
                parameter.ExpectedFinalStatus,
                initialCash: 1000000); // 1M vs 100K that is set in BasicTemplateDailyAlgorithm (10x)
        }

        [Test]
        public void ClearsOtherCashAmounts()
        {
            var parameter = new RegressionTests.AlgorithmStatisticsTestParameters(nameof(TestInitialCashAmountAlgorithm),
                new Dictionary<string, string> {
                    {"Total Trades", "1"},
                    {"Average Win", "0%"},
                    {"Average Loss", "0%"},
                    {"Compounding Annual Return", "214.949%"},
                    {"Drawdown", "1.200%"},
                    {"Expectancy", "0"},
                    {"Net Profit", "3.464%"},
                    {"Sharpe Ratio", "8.903"},
                    {"Loss Rate", "0%"},
                    {"Win Rate", "0%"},
                    {"Profit-Loss Ratio", "0"},
                    {"Alpha", "1.588"},
                    {"Beta", "-0.092"},
                    {"Annual Standard Deviation", "0.155"},
                    {"Annual Variance", "0.024"},
                    {"Information Ratio", "-3.899"},
                    {"Tracking Error", "0.212"},
                    {"Treynor Ratio", "-15.033"},
                    {"Total Fees", "$34.45"} // 10x times more than original BasicTemplateDailyAlgorithm
                },
                Language.CSharp,
                AlgorithmStatus.Completed);

            AlgorithmRunner.RunLocalBacktest(parameter.Algorithm,
                parameter.Statistics,
                parameter.AlphaStatistics,
                parameter.Language,
                parameter.ExpectedFinalStatus,
                initialCash: 1000000, // 1M vs 100K that is set in BasicTemplateDailyAlgorithm (10x)
                setupHandler: "TestInitialCashAmountSetupHandler");

            Assert.AreEqual(0, TestInitialCashAmountSetupHandler.TestAlgorithm.Portfolio.CashBook["EUR"].Amount);
            Assert.AreEqual(Currencies.USD, TestInitialCashAmountSetupHandler.TestAlgorithm.AccountCurrency);
        }

        public class TestInitialCashAmountAlgorithm : BasicTemplateDailyAlgorithm
        {
            public override void Initialize()
            {
                SetAccountCurrency("EUR");
                base.Initialize();
                SetCash("EUR", 1000000);
            }
        }

        public class TestInitialCashAmountSetupHandler : AlgorithmRunner.RegressionSetupHandlerWrapper
        {
            public static TestInitialCashAmountAlgorithm TestAlgorithm { get; set; }

            public override IAlgorithm CreateAlgorithmInstance(AlgorithmNodePacket algorithmNodePacket, string assemblyPath)
            {
                Algorithm = TestAlgorithm = new TestInitialCashAmountAlgorithm();
                return Algorithm;
            }
        }
    }
}
