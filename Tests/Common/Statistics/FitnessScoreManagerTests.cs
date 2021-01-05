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
using System.Linq;
using NUnit.Framework;
using QuantConnect.Algorithm;
using QuantConnect.Interfaces;
using QuantConnect.Statistics;
using QuantConnect.Tests.Engine.DataFeeds;

namespace QuantConnect.Tests.Common.Statistics
{
    [TestFixture]
    public class FitnessScoreManagerTests
    {
        [TestCase(10)]
        [TestCase(100000)]
        public void StaticAlgorithmScoreValue(int initialCash)
        {
            var algorithm = new QCAlgorithm();
            var initialDate = new DateTime(2018, 1, 1);
            algorithm.SetStartDate(initialDate);
            algorithm.SetCash(initialCash);

            var fitnessScore = new FitnessScoreManager();
            fitnessScore.Initialize(algorithm);

            decimal score = 0;
            for (var i = 0; i < 50; i++)
            {
                algorithm.SetDateTime(initialDate.AddDays(i));
                fitnessScore.UpdateScores();
                score = fitnessScore.FitnessScore;
            }
            Assert.AreEqual(0m, score);
        }

        [TestCase(-1000, 0.00)]
        [TestCase(1000, 10)]
        public void SigmoidalScaleWorks(decimal input, decimal expectedResult)
        {
            var result = FitnessScoreManager.SigmoidalScale(input);
            Assert.AreEqual(expectedResult, Math.Round(result, 2));
        }

        [Test]
        public void ZeroStartingPortfolioValueAlgorithm()
        {
            var algorithm = new QCAlgorithm();
            var initialDate = new DateTime(2018, 1, 1);
            algorithm.SetStartDate(initialDate);
            algorithm.SetCash(0);

            var fitnessScore = new FitnessScoreManager();
            fitnessScore.Initialize(algorithm);

            decimal score = 0;
            for (var i = 0; i < 10; i++)
            {
                algorithm.SetDateTime(initialDate.AddDays(i));
                fitnessScore.UpdateScores();
                score = fitnessScore.FitnessScore;
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
            var fitnessScore = new FitnessScoreManager();
            fitnessScore.Initialize(algorithm);

            algorithm.SetDateTime(initialDate.AddDays(1));
            IncreaseCashAmount(algorithm, 0.05);
            IncreaseSalesVolumeAmount(algorithm);

            fitnessScore.UpdateScores();
            var score = fitnessScore.FitnessScore;
            // FitnessScore: 1 * (5 + 5)
            Assert.AreEqual(1m, score);

            algorithm.SetDateTime(initialDate.AddDays(1));
            IncreaseCashAmount(algorithm, -0.20);
            fitnessScore.UpdateScores();
            algorithm.SetDateTime(initialDate.AddDays(1));
            IncreaseCashAmount(algorithm, -0.20);

            // FitnessScore: 0.333 * (-3.299 + -5)
            fitnessScore.UpdateScores();
            score = fitnessScore.FitnessScore;
            Assert.AreEqual(0.028m, score.TruncateTo3DecimalPlaces());
        }

        [TestCase(2)]
        [TestCase(-0.5)]
        public void ExtremePerformanceAlgorithm(double returnFactor)
        {
            var algorithm = new QCAlgorithm();
            var initialDate = new DateTime(2018, 1, 1);
            algorithm.SetStartDate(initialDate);
            algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(algorithm));
            algorithm.AddEquity("SPY");
            var fitnessScore = new FitnessScoreManager();
            fitnessScore.Initialize(algorithm);

            decimal score = 0;
            for (var i = 0; i < 10; i++)
            {
                algorithm.SetDateTime(initialDate.AddDays(i));
                fitnessScore.UpdateScores();
                score = fitnessScore.FitnessScore;
                IncreaseCashAmount(algorithm, returnFactor);
                IncreaseSalesVolumeAmount(algorithm);
            }
            Assert.AreEqual(returnFactor < 1 ? 0.174m : 1m, score.TruncateTo3DecimalPlaces());
        }

        private void IncreaseCashAmount(IAlgorithm algorithm, double factor)
        {
            var cash = algorithm.Portfolio.CashBook[algorithm.AccountCurrency];
            cash.AddAmount(cash.Amount * (decimal)factor);
        }

        private void IncreaseSalesVolumeAmount(IAlgorithm algorithm)
        {
            var security = algorithm.Securities.First().Value;
            security.Holdings.AddNewSale(algorithm.Portfolio.TotalPortfolioValue);
        }
    }
}
