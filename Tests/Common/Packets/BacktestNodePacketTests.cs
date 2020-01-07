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
using QuantConnect.Packets;

namespace QuantConnect.Tests.Common.Packets
{
    [TestFixture]
    public class BacktestNodePacketTests
    {
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
                    { "Average Win", "0%"},
                    { "Average Loss", "0%"},
                    { "Compounding Annual Return", "17.555%"},
                    { "Drawdown", "30.400%"},
                    { "Expectancy", "0"},
                    { "Net Profit", "38.142%"},
                    { "Sharpe Ratio", "0.627"},
                    { "Loss Rate", "0%"},
                    { "Win Rate", "0%"},
                    { "Profit-Loss Ratio", "0"},
                    { "Alpha", "-0.002"},
                    { "Beta", "0.946"},
                    { "Annual Standard Deviation", "0.299"},
                    { "Annual Variance", "0.089"},
                    { "Information Ratio", "-0.714"},
                    { "Tracking Error", "0.017"},
                    { "Treynor Ratio", "0.198"},
                    { "Total Fees", "$6.62"} },
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

        [Test]
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

        [Test]
        public void RoundTripWithInitialCashAmount()
        {
            var job = new BacktestNodePacket(1, 2, "3", null, 9m, $"{nameof(BacktestNodePacketTests)}.Pepe");
            Assert.AreEqual(9m, job.CashAmount.Value.Amount);
            Assert.AreEqual(Currencies.USD, job.CashAmount.Value.Currency);

            var serialized = JsonConvert.SerializeObject(job);
            var job2 = JsonConvert.DeserializeObject<BacktestNodePacket>(serialized);
            Assert.AreEqual(job.CashAmount, job2.CashAmount);
        }

        [Test]
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
                    {"Compounding Annual Return", "239.531%"},
                    {"Drawdown", "1.100%"},
                    {"Expectancy", "0"},
                    {"Net Profit", "3.463%"},
                    {"Sharpe Ratio", "11.274"},
                    {"Loss Rate", "0%"},
                    {"Win Rate", "0%"},
                    {"Profit-Loss Ratio", "0"},
                    {"Alpha", "0"},
                    {"Beta", "0.997"},
                    {"Annual Standard Deviation", "0.147"},
                    {"Annual Variance", "0.022"},
                    {"Information Ratio", "-11.333"},
                    {"Tracking Error", "0"},
                    {"Treynor Ratio", "1.66"},
                    {"Total Fees", "$32.59"} // 10x times more than original BasicTemplateDailyAlgorithm
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
                    {"Compounding Annual Return", "237.841%"},
                    {"Drawdown", "1.100%"},
                    {"Expectancy", "0"},
                    {"Net Profit", "3.463%"},
                    {"Sharpe Ratio", "10.284"},
                    {"Loss Rate", "0%"},
                    {"Win Rate", "0%"},
                    {"Profit-Loss Ratio", "0"},
                    {"Alpha", "0"},
                    {"Beta", "0.997"},
                    {"Annual Standard Deviation", "0.141"},
                    {"Annual Variance", "0.02"},
                    {"Information Ratio", "-10.334"},
                    {"Tracking Error", "0"},
                    {"Treynor Ratio", "1.453"},
                    {"Total Fees", "$32.59"} // 10x times more than original BasicTemplateDailyAlgorithm
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

        internal class TestInitialCashAmountAlgorithm : BasicTemplateDailyAlgorithm
        {
            public override void Initialize()
            {
                SetAccountCurrency("EUR");
                base.Initialize();
                SetCash("EUR", 1000000);
            }
        }

        internal class TestInitialCashAmountSetupHandler : AlgorithmRunner.RegressionSetupHandlerWrapper
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
