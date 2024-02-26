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
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Statistics;
using QuantConnect.Tests.Indicators;
using static Microsoft.FSharp.Core.ByRefKinds;

namespace QuantConnect.Tests.Common.Statistics
{
    [TestFixture]
    class PortfolioStatisticsTests
    {
        private const decimal TradeFee = 2;
        private readonly DateTime _startTime = new DateTime(2015, 08, 06, 15, 30, 0);

        /// <summary>
        /// TradingDaysPerYear: Use like backward compatibility
        /// </summary>
        /// <remarks><see cref="Interfaces.IAlgorithmSettings.TradingDaysPerYear"></remarks>
        protected const int _tradingDaysPerYear = 252;

        [Test]
        public void ITMOptionAssignment([Values] bool win)
        {
            var statistics = GetPortfolioStatistics(win, _tradingDaysPerYear, new List<double> { 0, 0 }, new List<double> { 0, 0 });

            if (win)
            {
                Assert.AreEqual(1m, statistics.WinRate);
                Assert.AreEqual(0m, statistics.LossRate);
            }
            else
            {
                Assert.AreEqual(0.5m, statistics.WinRate);
                Assert.AreEqual(0.5m, statistics.LossRate);
            }

            Assert.AreEqual(0.1173913043478260869565217391m, statistics.AverageWinRate);
            Assert.AreEqual(-0.08m, statistics.AverageLossRate);
            Assert.AreEqual(1.4673913043478260869565217388m, statistics.ProfitLossRatio);
        }


        public static IEnumerable<TestCaseData> StatisticsCases
        {
            get
            {
                yield return new TestCaseData(202, 0.00589787137120101M, 0.0767976000354244M, -3.0952570635188M, 0.167632655086644M, 0.252197874915608M);
                yield return new TestCaseData(252, 0.00735774052248839M, 0.0857772727620108M, -3.3486737318423M, 0.187233350684845M, 0.257146306116665M);
                yield return new TestCaseData(365, 0.0106570448043979M, 0.103232963748978M, -3.75507953923657M, 0.225335372429895M, 0.264390639112978M);
            }
        }

        [TestCaseSource(nameof(StatisticsCases))]
        public void ITMOptionAssignmentWithDifferentTradingDaysPerYearValue(
            int tradingDaysPerYear, decimal expectedAnnualVariance, decimal expectedAnnualStandardDeviation,
            decimal expectedSharpeRatio, decimal expectedTrackingError, decimal expectedProbabilisticSharpeRatio)
        {
            var listPerformance = new List<double> { -0.009025132, 0.003653969, 0, 0 };
            var listBenchmark = new List<double> { -0.011587791300935783, 0.00054375782787618543, 0.022165997700413956, 0.006263266301918822 };

            var statistics = GetPortfolioStatistics(true, tradingDaysPerYear, listPerformance, listBenchmark);

            Assert.AreEqual(expectedAnnualVariance, statistics.AnnualVariance);
            Assert.AreEqual(expectedAnnualStandardDeviation, statistics.AnnualStandardDeviation);
            Assert.AreEqual(expectedSharpeRatio, statistics.SharpeRatio);
            Assert.AreEqual(expectedTrackingError, statistics.TrackingError);
            Assert.AreEqual(expectedProbabilisticSharpeRatio, statistics.ProbabilisticSharpeRatio);
        }

        [Test]
        public void VaRMatchesExternalData()
        {
            var externalFileName = "spy_valueatrisk.csv";
            var data = TestHelper.GetCsvFileStream(externalFileName);
            var listPerformance = new List<double>();

            var iteration = 0;
            foreach (var row in data)
            {
                if (iteration == 0)
                {
                    iteration++;
                    continue;
                }

                Parse.TryParse(row["returns"], NumberStyles.Float, out double returns);
                listPerformance.Add(returns);

                Parse.TryParse(row["VaR_99"], NumberStyles.Float, out decimal expected99);
                Parse.TryParse(row["VaR_95"], NumberStyles.Float, out decimal expected95);

                var statistics = GetPortfolioStatistics(
                    true,
                    _tradingDaysPerYear,
                    listPerformance,
                    new List<double> { 0, 0 });

                Assert.AreEqual(Math.Round(expected99, 3), statistics.ValueAtRisk99);
                Assert.AreEqual(Math.Round(expected95, 3), statistics.ValueAtRisk95);
            }
        }

        [Test]
        public void VaRIsZeroIfLessThan2Samples()
        {
            var listPerformance = new List<double> { 0.006196177273682046 };

            var statistics = GetPortfolioStatistics(
                    true,
                    _tradingDaysPerYear,
                    listPerformance,
                    new List<double> { 0, 0 });

            Assert.Zero(statistics.ValueAtRisk99);
            Assert.Zero(statistics.ValueAtRisk95);
        }

        /// <summary>
        /// Initialize and return Portfolio Statistics depends on input data
        /// </summary>
        /// <param name="win">create profitable trade or not</param>
        /// <param name="tradingDaysPerYear">amount days per year for brokerage (e.g. crypto exchange use 365 days)</param>
        /// <param name="listPerformance">The list of algorithm performance values</param>
        /// <param name="listBenchmark">The list of benchmark values</param>
        /// <returns>The <see cref="PortfolioStatistics"/> class represents a set of statistics calculated from equity and benchmark samples</returns>
        private PortfolioStatistics GetPortfolioStatistics(bool win, int tradingDaysPerYear, List<double> listPerformance, List<double> listBenchmark)
        {
            var trades = CreateITMOptionAssignment(win);
            var profitLoss = new SortedDictionary<DateTime, decimal>(trades.ToDictionary(x => x.ExitTime, x => x.ProfitLoss));
            var winCount = trades.Count(x => x.IsWin);
            var lossCount = trades.Count - winCount;
            return new PortfolioStatistics(profitLoss, new SortedDictionary<DateTime, decimal>(),
                new SortedDictionary<DateTime, decimal>(), listPerformance, listBenchmark, 100000,
                new InterestRateProvider(), tradingDaysPerYear, winCount, lossCount);
        }

        private List<Trade> CreateITMOptionAssignment(bool win)
        {
            var time = _startTime;

            return new List<Trade>
            {
                new Trade
                {
                    Symbol = Symbols.SPY_C_192_Feb19_2016,
                    EntryTime = time,
                    EntryPrice = 80m,
                    Direction = TradeDirection.Long,
                    Quantity = 10,
                    ExitTime = time.AddMinutes(20),
                    ExitPrice = 0m,
                    ProfitLoss = -8000m,
                    TotalFees = TradeFee,
                    MAE = -8000m,
                    MFE = 0,
                    IsWin = win
                },
                new Trade
                {
                    Symbol = Symbols.SPY,
                    EntryTime = time.AddMinutes(20),
                    EntryPrice = 192m,
                    Direction = TradeDirection.Long,
                    Quantity = 1000,
                    ExitTime = time.AddMinutes(30),
                    ExitPrice = 300m,
                    ProfitLoss = 10800m,
                    TotalFees = TradeFee,
                    MAE = 0,
                    MFE = 10800m,
                    IsWin = true
                },
            };
        }
    }
}
