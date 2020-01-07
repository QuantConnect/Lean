
using Deedle;
using Newtonsoft.Json;
using NUnit.Framework;
using QuantConnect.Orders;
using QuantConnect.Packets;
using QuantConnect.Statistics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace QuantConnect.Tests.Common.Statistics
{
    [TestFixture]
    public class StatisticsBuilderTests
    {
        [Test]
        public void BetaCalculationIsCorrect()
        {
            var backtestResult = JsonConvert.DeserializeObject<BacktestResult>(File.ReadAllText("TestData/BasicTemplateAlgorithm.json"), new OrderJsonConverter());

            var results = StatisticsBuilder.Generate(
                new List<Trade>(),
                new SortedDictionary<DateTime, decimal>(),
                backtestResult.Charts["Strategy Equity"].Series["Equity"].Values,
                backtestResult.Charts["Strategy Equity"].Series["Daily Performance"].Values,
                backtestResult.Charts["Benchmark"].Series["Benchmark"].Values,
                100000,
                0,
                0
            );

            Assert.IsTrue(results.TotalPerformance.PortfolioStatistics.Beta >= 0.95m && results.TotalPerformance.PortfolioStatistics.Beta <= 1.05m);
        }
    }
}
