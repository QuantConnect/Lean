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
using QuantConnect.Statistics;

namespace QuantConnect.Tests.Common.Statistics
{
    [TestFixture]
    class PortfolioStatisticsTests
    {
        private const decimal TradeFee = 2;
        private readonly DateTime _startTime = new DateTime(2015, 08, 06, 15, 30, 0);

        [Test]
        public void ITMOptionAssignment([Values] bool win)
        {
            var trades = CreateITMOptionAssignment(win);
            var profitLoss = new SortedDictionary<DateTime, decimal>(trades.ToDictionary(x => x.ExitTime, x => x.ProfitLoss));
            var winCount = trades.Count(x => x.IsWin);
            var lossCount = trades.Count - winCount;
            var statistics = new PortfolioStatistics(profitLoss, new SortedDictionary<DateTime, decimal>(),
                new SortedDictionary<DateTime, decimal>(), new List<double> { 0, 0 }, new List<double> { 0, 0 }, 100000,
                winCount: winCount, lossCount: lossCount);

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
