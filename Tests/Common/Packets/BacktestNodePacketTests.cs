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
using QuantConnect.Statistics;

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
                    { PerformanceMetrics.TotalOrders, "1" },
                    {"Average Win", "0%"},
                    { "Average Loss", "0%"},
                    { "Compounding Annual Return", "14.421%"},
                    { "Drawdown", "32.900%"},
                    { "Expectancy", "0"},
                    { "Net Profit", "30.857%"},
                    { "Sharpe Ratio", "0.492"},
                    { "Loss Rate", "0%"},
                    { "Win Rate", "0%"},
                    { "Profit-Loss Ratio", "0"},
                    { "Alpha", "-0.012"},
                    { "Beta", "1.012"},
                    { "Annual Standard Deviation", "0.263"},
                    { "Annual Variance", "0.069"},
                    { "Information Ratio", "-0.42"},
                    { "Tracking Error", "0.025"},
                    { "Treynor Ratio", "0.128"},
                    { "Total Fees", "$7.10"} },
                Language.CSharp,
                AlgorithmStatus.Completed);

            AlgorithmRunner.RunLocalBacktest(parameter.Algorithm,
                parameter.Statistics,
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
                    { PerformanceMetrics.TotalOrders, "5" },
                    {"Average Win", "0%"},
                    { "Average Loss", "0%"},
                    { "Compounding Annual Return", "-88.910%"},
                    { "Drawdown", "1.800%"},
                    { "Expectancy", "0"},
                    { "Net Profit", "-1.791%"},
                    { "Sharpe Ratio", "-4.495"},
                    { "Loss Rate", "0%"},
                    { "Win Rate", "0%"},
                    { "Profit-Loss Ratio", "0"},
                    { "Alpha", "-0.522"},
                    { "Beta", "1.48"},
                    { "Annual Standard Deviation", "0.201"},
                    { "Annual Variance", "0.04"},
                    { "Information Ratio", "-9.904"},
                    { "Tracking Error", "0.065"},
                    { "Treynor Ratio", "-0.611"},
                    { "Total Fees", "$3.00"} },
                Language.CSharp,
                AlgorithmStatus.Completed);

            AlgorithmRunner.RunLocalBacktest(parameter.Algorithm,
                parameter.Statistics,
                parameter.Language,
                parameter.ExpectedFinalStatus,
                startDate: new DateTime(2014, 03, 24),
                endDate: new DateTime(2014, 03, 26));
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
                    {PerformanceMetrics.TotalOrders, "1"},
                    {"Average Win", "0%"},
                    {"Average Loss", "0%"},
                    {"Compounding Annual Return", "424.497%"},
                    {"Drawdown", "0.800%"},
                    {"Expectancy", "0"},
                    {"Net Profit", "4.487%"},
                    {"Sharpe Ratio", "17.306"},
                    {"Probabilistic Sharpe Ratio", "96.835%"},
                    {"Loss Rate", "0%"},
                    {"Win Rate", "0%"},
                    {"Profit-Loss Ratio", "0"},
                    {"Alpha", "-0.249"},
                    {"Beta", "1.015"},
                    {"Annual Standard Deviation", "0.141"},
                    {"Annual Variance", "0.02"},
                    {"Information Ratio", "-18.937"},
                    {"Tracking Error", "0.011"},
                    {"Treynor Ratio", "2.403"},
                    {"Total Fees", "$34.86"} // 10x times more than original BasicTemplateDailyAlgorithm
                },
                Language.CSharp,
                AlgorithmStatus.Completed);

            AlgorithmRunner.RunLocalBacktest(parameter.Algorithm,
                parameter.Statistics,
                parameter.Language,
                parameter.ExpectedFinalStatus,
                initialCash: 1000000); // 1M vs 100K that is set in BasicTemplateDailyAlgorithm (10x)
        }

        [Test]
        public void ClearsOtherCashAmounts()
        {
            var parameter = new RegressionTests.AlgorithmStatisticsTestParameters(nameof(TestInitialCashAmountAlgorithm),
                new Dictionary<string, string> {
                    {PerformanceMetrics.TotalOrders, "1"},
                    {"Average Win", "0%"},
                    {"Average Loss", "0%"},
                    {"Compounding Annual Return", "338.765%"},
                    {"Drawdown", "0.800%"},
                    {"Expectancy", "0"},
                    {"Net Profit", "4.487%"},
                    {"Sharpe Ratio", "15.085"},
                    {"Probabilistic Sharpe Ratio", "97.122%"},
                    {"Loss Rate", "0%"},
                    {"Win Rate", "0%"},
                    {"Profit-Loss Ratio", "0"},
                    {"Alpha", "-0.194"},
                    {"Beta", "1.013"},
                    {"Annual Standard Deviation", "0.135"},
                    {"Annual Variance", "0.018"},
                    {"Information Ratio", "-15.836"},
                    {"Tracking Error", "0.01"},
                    {"Treynor Ratio", "2.013"},
                    {"Total Fees", "$34.86"} // 10x times more than original BasicTemplateDailyAlgorithm
                },
                Language.CSharp,
                AlgorithmStatus.Completed);

            AlgorithmRunner.RunLocalBacktest(parameter.Algorithm,
                parameter.Statistics,
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
