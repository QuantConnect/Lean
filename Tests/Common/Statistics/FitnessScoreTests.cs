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
using NUnit.Framework;
using QuantConnect.Algorithm;
using QuantConnect.Interfaces;
using QuantConnect.Orders;
using QuantConnect.Statistics;
using QuantConnect.Tests.Engine.DataFeeds;

namespace QuantConnect.Tests.Common.Statistics
{
    [TestFixture]
    public class FitnessScoreTests
    {
        [TestCase(10)]
        [TestCase(100000)]
        public void StaticAlgorithmScoreValue(int initialCash)
        {
            var algorithm = new QCAlgorithm();
            var initialDate = new DateTime(2018, 1, 1);
            algorithm.SetStartDate(initialDate);
            algorithm.SetCash(initialCash);

            var fitnessScore = new FitnessScore();
            fitnessScore.Initialize(algorithm);

            decimal score = 0;
            for (var i = 0; i < 50; i++)
            {
                algorithm.SetDateTime(initialDate.AddDays(i));
                score = fitnessScore.GetFitnessScore();
            }
            Assert.AreEqual(0.5m, score);
        }

        [Test]
        public void ZeroStartingPortfolioValueAlgorithm()
        {
            var algorithm = new QCAlgorithm();
            var initialDate = new DateTime(2018, 1, 1);
            algorithm.SetStartDate(initialDate);
            algorithm.SetCash(0);

            var fitnessScore = new FitnessScore();
            fitnessScore.Initialize(algorithm);

            decimal score = 0;
            for (var i = 0; i < 10; i++)
            {
                algorithm.SetDateTime(initialDate.AddDays(i));
                score = fitnessScore.GetFitnessScore();
            }
            Assert.AreEqual(0m, score);
        }

        [Test]
        public void ValueIsCalculatedCorrectly()
        {
            var algorithm = new QCAlgorithm();
            var initialDate = new DateTime(2018, 1, 1);
            algorithm.SetStartDate(initialDate);
            algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(algorithm));
            algorithm.AddEquity("SPY");
            var fitnessScore = new FitnessScore();
            fitnessScore.Initialize(algorithm);
            var testTradeBuilder = new TestTradeBuilder();
            algorithm.SetTradeBuilder(testTradeBuilder);

            algorithm.SetDateTime(initialDate.AddDays(1));
            IncreaseCashAmount(algorithm, 0.05);
            IncreaseSalesVolumeAmount(algorithm, 0.05);

            var score = fitnessScore.GetFitnessScore();
            // return 0.05 -> 5% for 1 day
            // anually 23.05263 -> 2305.2%
            // Sortino Ratio 5 -> no closed trades
            // RoMaD -> 5 no drawdown
            // turnover -> 0.05
            Assert.AreEqual(0.525m, score);

            algorithm.SetDateTime(initialDate.AddDays(1));
            // Lets add two closed losing trades
            testTradeBuilder.ClosedTrades = new List<Trade>
            {
                new Trade { ProfitLoss = -1000 },
                new Trade { ProfitLoss = -2000 }
            };
            // some drawdown
            IncreaseCashAmount(algorithm, -0.20);

            // return -0.160
            // annually -73.7684
            // Raw Sortino: -0.10432 -> scaled -0.1648
            // return over drawdown: -368.84 -> scaled -4.999
            // Portfolio turn over: 0.0625
            // Raw fitnessScore: -0.32279 = 0.0625 (-0.1648 + -4.999)
            // scaled result = (-0.32279 + 10) / 20
            score = fitnessScore.GetFitnessScore();
            Assert.AreEqual(0.484m, score);
        }

        [TestCase(2)]
        [TestCase(-2)]
        public void ExtremePerformanceAlgorithm(double returnFactor)
        {
            var algorithm = new QCAlgorithm();
            var initialDate = new DateTime(2018, 1, 1);
            algorithm.SetStartDate(initialDate);
            algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(algorithm));
            algorithm.AddEquity("SPY");
            var fitnessScore = new FitnessScore();
            fitnessScore.Initialize(algorithm);

            decimal score = 0;
            for (var i = 0; i < 10; i++)
            {
                algorithm.SetDateTime(initialDate.AddDays(i));
                score = fitnessScore.GetFitnessScore();
                IncreaseCashAmount(algorithm, returnFactor);
                IncreaseSalesVolumeAmount(algorithm, returnFactor);
            }
            Assert.AreEqual(returnFactor < 1 ? 0 : 1m, score);
        }

        private void IncreaseCashAmount(IAlgorithm algorithm, double factor)
        {
            var cash = algorithm.Portfolio.CashBook[algorithm.AccountCurrency];
            cash.AddAmount(cash.Amount * (decimal)factor);
        }

        private void IncreaseSalesVolumeAmount(IAlgorithm algorithm, double factor)
        {
            var security = algorithm.Securities.First().Value;
            security.Holdings.AddNewSale(algorithm.Portfolio.TotalPortfolioValue * (decimal)factor);
        }

        private class TestTradeBuilder : ITradeBuilder
        {
            public TestTradeBuilder()
            {
                ClosedTrades = new List<Trade>();
            }
            public void SetLiveMode(bool live)
            {
            }
            public List<Trade> ClosedTrades { get; set; }
            public bool HasOpenPosition(Symbol symbol)
            {
                return false;
            }
            public void SetMarketPrice(Symbol symbol, decimal price)
            {
            }
            public void ProcessFill(OrderEvent fill, decimal securityConversionRate, decimal feeInAccountCurrency, decimal multiplier = 1m)
            {
            }
        }
    }
}
