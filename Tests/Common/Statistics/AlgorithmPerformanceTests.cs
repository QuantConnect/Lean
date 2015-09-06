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
using NUnit.Framework;
using QuantConnect.Statistics;

namespace QuantConnect.Tests.Common.Statistics
{
    [TestFixture]
    class AlgorithmPerformanceTests
    {
        private const string Symbol = "EURUSD";
        private readonly DateTime _startTime = new DateTime(2015, 08, 06, 15, 30, 0);
        
        [Test]
        public void NoTrades()
        {
            var performance = new AlgorithmPerformance(new List<Trade>());

            Assert.AreEqual(null, performance.StartDateTime);
            Assert.AreEqual(null, performance.EndDateTime);
            Assert.AreEqual(0, performance.TotalNumberOfTrades);
            Assert.AreEqual(0, performance.NumberOfWinningTrades);
            Assert.AreEqual(0, performance.NumberOfLosingTrades);
            Assert.AreEqual(0, performance.TotalProfitLoss);
            Assert.AreEqual(0, performance.TotalProfit);
            Assert.AreEqual(0, performance.TotalLoss);
            Assert.AreEqual(0, performance.LargestProfit);
            Assert.AreEqual(0, performance.LargestLoss);
            Assert.AreEqual(0, performance.AverageProfitLoss);
            Assert.AreEqual(0, performance.AverageProfit);
            Assert.AreEqual(0, performance.AverageLoss);
            Assert.AreEqual(TimeSpan.Zero, performance.AverageTradeDuration);
            Assert.AreEqual(TimeSpan.Zero, performance.AverageWinningTradeDuration);
            Assert.AreEqual(TimeSpan.Zero, performance.AverageLosingTradeDuration);
            Assert.AreEqual(0, performance.MaxConsecutiveWinningTrades);
            Assert.AreEqual(0, performance.MaxConsecutiveLosingTrades);
            Assert.AreEqual(-1, performance.ProfitLossRatio);
            Assert.AreEqual(0, performance.WinLossRatio);
            Assert.AreEqual(0, performance.WinRate);
            Assert.AreEqual(0, performance.LossRate);
            Assert.AreEqual(0, performance.AverageMAE);
            Assert.AreEqual(0, performance.AverageMFE);
            Assert.AreEqual(0, performance.LargestMAE);
            Assert.AreEqual(0, performance.LargestMFE);
            Assert.AreEqual(0, performance.MaximumClosedTradeDrawdown);
            Assert.AreEqual(0, performance.MaximumIntraTradeDrawdown);
            Assert.AreEqual(0, performance.ProfitLossStandardDeviation);
            Assert.AreEqual(0, performance.ProfitLossDownsideDeviation);
            Assert.AreEqual(0, performance.ProfitFactor);
            Assert.AreEqual(0, performance.SharpeRatio);
            Assert.AreEqual(0, performance.SortinoRatio);
            Assert.AreEqual(0, performance.ProfitToMaxDrawdownRatio);
            Assert.AreEqual(0, performance.MaximumEndTradeDrawdown);
            Assert.AreEqual(0, performance.AverageEndTradeDrawdown);
            Assert.AreEqual(TimeSpan.Zero, performance.MaximumDrawdownDuration);
        }

        [Test]
        public void ThreeWinners()
        {
            var performance = new AlgorithmPerformance(CreateThreeWinners());

            Assert.AreEqual(_startTime, performance.StartDateTime);
            Assert.AreEqual(_startTime.AddMinutes(40), performance.EndDateTime);
            Assert.AreEqual(3, performance.TotalNumberOfTrades);
            Assert.AreEqual(3, performance.NumberOfWinningTrades);
            Assert.AreEqual(0, performance.NumberOfLosingTrades);
            Assert.AreEqual(50, performance.TotalProfitLoss);
            Assert.AreEqual(50, performance.TotalProfit);
            Assert.AreEqual(0, performance.TotalLoss);
            Assert.AreEqual(20, performance.LargestProfit);
            Assert.AreEqual(0, performance.LargestLoss);
            Assert.AreEqual(16.666666666666666666666666667m, performance.AverageProfitLoss);
            Assert.AreEqual(16.666666666666666666666666667m, performance.AverageProfit);
            Assert.AreEqual(0, performance.AverageLoss);
            Assert.AreEqual(TimeSpan.FromMinutes(20), performance.AverageTradeDuration);
            Assert.AreEqual(TimeSpan.FromMinutes(20), performance.AverageWinningTradeDuration);
            Assert.AreEqual(TimeSpan.Zero, performance.AverageLosingTradeDuration);
            Assert.AreEqual(3, performance.MaxConsecutiveWinningTrades);
            Assert.AreEqual(0, performance.MaxConsecutiveLosingTrades);
            Assert.AreEqual(-1, performance.ProfitLossRatio);
            Assert.AreEqual(10, performance.WinLossRatio);
            Assert.AreEqual(1, performance.WinRate);
            Assert.AreEqual(0, performance.LossRate);
            Assert.AreEqual(-16.666666666666666666666666667m, performance.AverageMAE);
            Assert.AreEqual(33.333333333333333333333333333m, performance.AverageMFE);
            Assert.AreEqual(-30, performance.LargestMAE);
            Assert.AreEqual(40, performance.LargestMFE);
            Assert.AreEqual(0, performance.MaximumClosedTradeDrawdown);
            Assert.AreEqual(-70, performance.MaximumIntraTradeDrawdown);
            Assert.AreEqual(5.77350269189626m, performance.ProfitLossStandardDeviation);
            Assert.AreEqual(0, performance.ProfitLossDownsideDeviation);
            Assert.AreEqual(10, performance.ProfitFactor);
            Assert.AreEqual(2.8867513459481276450914878051m, performance.SharpeRatio);
            Assert.AreEqual(0, performance.SortinoRatio);
            Assert.AreEqual(10, performance.ProfitToMaxDrawdownRatio);
            Assert.AreEqual(-20, performance.MaximumEndTradeDrawdown);
            Assert.AreEqual(-16.666666666666666666666666666m, performance.AverageEndTradeDrawdown);
            Assert.AreEqual(TimeSpan.Zero, performance.MaximumDrawdownDuration);
        }

        private IEnumerable<Trade> CreateThreeWinners()
        {
            var time = _startTime;

            return new List<Trade>
            {
                new Trade
                {
                    Symbol = Symbol,
                    EntryTime = time,
                    EntryPrice = 1.07m,
                    Direction = TradeDirection.Long,
                    Quantity = 1000,
                    ExitTime = time.AddMinutes(20),
                    ExitPrice = 1.09m,
                    ProfitLoss = 20,
                    MAE = -5,
                    MFE = 30
                },
                new Trade
                {
                    Symbol = Symbol,
                    EntryTime = time.AddMinutes(10),
                    EntryPrice = 1.08m,
                    Direction = TradeDirection.Long,
                    Quantity = 2000,
                    ExitTime = time.AddMinutes(40),
                    ExitPrice = 1.09m,
                    ProfitLoss = 20,
                    MAE = -30,
                    MFE = 40
                },
                new Trade
                {
                    Symbol = Symbol,
                    EntryTime = time.AddMinutes(30),
                    EntryPrice = 1.08m,
                    Direction = TradeDirection.Long,
                    Quantity = 1000,
                    ExitTime = time.AddMinutes(40),
                    ExitPrice = 1.09m,
                    ProfitLoss = 10,
                    MAE = -15,
                    MFE = 30
                }
            };
        }

        [Test]
        public void ThreeLosers()
        {
            var performance = new AlgorithmPerformance(CreateThreeLosers());

            Assert.AreEqual(_startTime, performance.StartDateTime);
            Assert.AreEqual(_startTime.AddMinutes(40), performance.EndDateTime);
            Assert.AreEqual(3, performance.TotalNumberOfTrades);
            Assert.AreEqual(0, performance.NumberOfWinningTrades);
            Assert.AreEqual(3, performance.NumberOfLosingTrades);
            Assert.AreEqual(-50, performance.TotalProfitLoss);
            Assert.AreEqual(0, performance.TotalProfit);
            Assert.AreEqual(-50, performance.TotalLoss);
            Assert.AreEqual(0, performance.LargestProfit);
            Assert.AreEqual(-20, performance.LargestLoss);
            Assert.AreEqual(-16.666666666666666666666666667m, performance.AverageProfitLoss);
            Assert.AreEqual(0, performance.AverageProfit);
            Assert.AreEqual(-16.666666666666666666666666667m, performance.AverageLoss);
            Assert.AreEqual(TimeSpan.FromMinutes(20), performance.AverageTradeDuration);
            Assert.AreEqual(TimeSpan.Zero, performance.AverageWinningTradeDuration);
            Assert.AreEqual(TimeSpan.FromMinutes(20), performance.AverageLosingTradeDuration);
            Assert.AreEqual(0, performance.MaxConsecutiveWinningTrades);
            Assert.AreEqual(3, performance.MaxConsecutiveLosingTrades);
            Assert.AreEqual(0, performance.ProfitLossRatio);
            Assert.AreEqual(0, performance.WinLossRatio);
            Assert.AreEqual(0, performance.WinRate);
            Assert.AreEqual(1, performance.LossRate);
            Assert.AreEqual(-33.333333333333333333333333333m, performance.AverageMAE);
            Assert.AreEqual(16.666666666666666666666666667m, performance.AverageMFE);
            Assert.AreEqual(-40, performance.LargestMAE);
            Assert.AreEqual(30, performance.LargestMFE);
            Assert.AreEqual(-50, performance.MaximumClosedTradeDrawdown);
            Assert.AreEqual(-80, performance.MaximumIntraTradeDrawdown);
            Assert.AreEqual(5.77350269189626m, performance.ProfitLossStandardDeviation);
            Assert.AreEqual(5.77350269189626m, performance.ProfitLossDownsideDeviation);
            Assert.AreEqual(0, performance.ProfitFactor);
            Assert.AreEqual(-2.8867513459481276450914878051m, performance.SharpeRatio);
            Assert.AreEqual(-2.8867513459481276450914878051m, performance.SortinoRatio);
            Assert.AreEqual(-1, performance.ProfitToMaxDrawdownRatio);
            Assert.AreEqual(-50, performance.MaximumEndTradeDrawdown);
            Assert.AreEqual(-33.333333333333333333333333334m, performance.AverageEndTradeDrawdown);
            Assert.AreEqual(TimeSpan.Zero, performance.MaximumDrawdownDuration);
        }

        private IEnumerable<Trade> CreateThreeLosers()
        {
            var time = _startTime;

            return new List<Trade>
            {
                new Trade
                {
                    Symbol = Symbol,
                    EntryTime = time,
                    EntryPrice = 1.07m,
                    Direction = TradeDirection.Short,
                    Quantity = 1000,
                    ExitTime = time.AddMinutes(20),
                    ExitPrice = 1.09m,
                    ProfitLoss = -20,
                    MAE = -30,
                    MFE = 5
                },
                new Trade
                {
                    Symbol = Symbol,
                    EntryTime = time.AddMinutes(10),
                    EntryPrice = 1.08m,
                    Direction = TradeDirection.Short,
                    Quantity = 2000,
                    ExitTime = time.AddMinutes(40),
                    ExitPrice = 1.09m,
                    ProfitLoss = -20,
                    MAE = -40,
                    MFE = 30
                },
                new Trade
                {
                    Symbol = Symbol,
                    EntryTime = time.AddMinutes(30),
                    EntryPrice = 1.08m,
                    Direction = TradeDirection.Short,
                    Quantity = 1000,
                    ExitTime = time.AddMinutes(40),
                    ExitPrice = 1.09m,
                    ProfitLoss = -10,
                    MAE = -30,
                    MFE = 15
                }
            };
        }

        [Test]
        public void TwoLosersOneWinner()
        {
            var performance = new AlgorithmPerformance(CreateTwoLosersOneWinner());

            Assert.AreEqual(_startTime, performance.StartDateTime);
            Assert.AreEqual(_startTime.AddMinutes(40), performance.EndDateTime);
            Assert.AreEqual(3, performance.TotalNumberOfTrades);
            Assert.AreEqual(1, performance.NumberOfWinningTrades);
            Assert.AreEqual(2, performance.NumberOfLosingTrades);
            Assert.AreEqual(-30, performance.TotalProfitLoss);
            Assert.AreEqual(10, performance.TotalProfit);
            Assert.AreEqual(-40, performance.TotalLoss);
            Assert.AreEqual(10, performance.LargestProfit);
            Assert.AreEqual(-20, performance.LargestLoss);
            Assert.AreEqual(-10, performance.AverageProfitLoss);
            Assert.AreEqual(10, performance.AverageProfit);
            Assert.AreEqual(-20, performance.AverageLoss);
            Assert.AreEqual(TimeSpan.FromSeconds(800), performance.AverageTradeDuration);
            Assert.AreEqual(TimeSpan.FromMinutes(10), performance.AverageWinningTradeDuration);
            Assert.AreEqual(TimeSpan.FromMinutes(15), performance.AverageLosingTradeDuration);
            Assert.AreEqual(1, performance.MaxConsecutiveWinningTrades);
            Assert.AreEqual(2, performance.MaxConsecutiveLosingTrades);
            Assert.AreEqual(0.5m, performance.ProfitLossRatio);
            Assert.AreEqual(0.5m, performance.WinLossRatio);
            Assert.AreEqual(0.3333333333333333333333333333m, performance.WinRate);
            Assert.AreEqual(0.6666666666666666666666666667m, performance.LossRate);
            Assert.AreEqual(-28.333333333333333333333333333333m, performance.AverageMAE);
            Assert.AreEqual(21.666666666666666666666666666667m, performance.AverageMFE);
            Assert.AreEqual(-40, performance.LargestMAE);
            Assert.AreEqual(30, performance.LargestMFE);
            Assert.AreEqual(-40, performance.MaximumClosedTradeDrawdown);
            Assert.AreEqual(-70, performance.MaximumIntraTradeDrawdown);
            Assert.AreEqual(17.3205080756888m, performance.ProfitLossStandardDeviation);
            Assert.AreEqual(0, performance.ProfitLossDownsideDeviation);
            Assert.AreEqual(0.25m, performance.ProfitFactor);
            Assert.AreEqual(-0.5773502691896248623516308943m, performance.SharpeRatio);
            Assert.AreEqual(0, performance.SortinoRatio);
            Assert.AreEqual(-0.75m, performance.ProfitToMaxDrawdownRatio);
            Assert.AreEqual(-50, performance.MaximumEndTradeDrawdown);
            Assert.AreEqual(-31.666666666666666666666666666667m, performance.AverageEndTradeDrawdown);
            Assert.AreEqual(TimeSpan.Zero, performance.MaximumDrawdownDuration);
        }

        private IEnumerable<Trade> CreateTwoLosersOneWinner()
        {
            var time = _startTime;

            return new List<Trade>
            {
                new Trade
                {
                    Symbol = Symbol,
                    EntryTime = time,
                    EntryPrice = 1.07m,
                    Direction = TradeDirection.Short,
                    Quantity = 1000,
                    ExitTime = time.AddMinutes(20),
                    ExitPrice = 1.09m,
                    ProfitLoss = -20,
                    MAE = -30,
                    MFE = 5
                },
                new Trade
                {
                    Symbol = Symbol,
                    EntryTime = time.AddMinutes(10),
                    EntryPrice = 1.08m,
                    Direction = TradeDirection.Short,
                    Quantity = 2000,
                    ExitTime = time.AddMinutes(20),
                    ExitPrice = 1.09m,
                    ProfitLoss = -20,
                    MAE = -40,
                    MFE = 30
                },
                new Trade
                {
                    Symbol = Symbol,
                    EntryTime = time.AddMinutes(30),
                    EntryPrice = 1.08m,
                    Direction = TradeDirection.Long,
                    Quantity = 1000,
                    ExitTime = time.AddMinutes(40),
                    ExitPrice = 1.09m,
                    ProfitLoss = 10,
                    MAE = -15,
                    MFE = 30
                }
            };
        }

        [Test]
        public void OneWinnerTwoLosers()
        {
            var performance = new AlgorithmPerformance(CreateOneWinnerTwoLosers());

            Assert.AreEqual(_startTime, performance.StartDateTime);
            Assert.AreEqual(_startTime.AddMinutes(40), performance.EndDateTime);
            Assert.AreEqual(3, performance.TotalNumberOfTrades);
            Assert.AreEqual(1, performance.NumberOfWinningTrades);
            Assert.AreEqual(2, performance.NumberOfLosingTrades);
            Assert.AreEqual(-30, performance.TotalProfitLoss);
            Assert.AreEqual(10, performance.TotalProfit);
            Assert.AreEqual(-40, performance.TotalLoss);
            Assert.AreEqual(10, performance.LargestProfit);
            Assert.AreEqual(-20, performance.LargestLoss);
            Assert.AreEqual(-10, performance.AverageProfitLoss);
            Assert.AreEqual(10, performance.AverageProfit);
            Assert.AreEqual(-20, performance.AverageLoss);
            Assert.AreEqual(TimeSpan.FromSeconds(800), performance.AverageTradeDuration);
            Assert.AreEqual(TimeSpan.FromMinutes(10), performance.AverageWinningTradeDuration);
            Assert.AreEqual(TimeSpan.FromMinutes(15), performance.AverageLosingTradeDuration);
            Assert.AreEqual(1, performance.MaxConsecutiveWinningTrades);
            Assert.AreEqual(2, performance.MaxConsecutiveLosingTrades);
            Assert.AreEqual(0.5m, performance.ProfitLossRatio);
            Assert.AreEqual(0.5m, performance.WinLossRatio);
            Assert.AreEqual(0.3333333333333333333333333333m, performance.WinRate);
            Assert.AreEqual(0.6666666666666666666666666667m, performance.LossRate);
            Assert.AreEqual(-28.333333333333333333333333333333m, performance.AverageMAE);
            Assert.AreEqual(21.666666666666666666666666666667m, performance.AverageMFE);
            Assert.AreEqual(-40, performance.LargestMAE);
            Assert.AreEqual(30, performance.LargestMFE);
            Assert.AreEqual(-40, performance.MaximumClosedTradeDrawdown);
            Assert.AreEqual(-80, performance.MaximumIntraTradeDrawdown);
            Assert.AreEqual(17.3205080756888m, performance.ProfitLossStandardDeviation);
            Assert.AreEqual(0, performance.ProfitLossDownsideDeviation);
            Assert.AreEqual(0.25m, performance.ProfitFactor);
            Assert.AreEqual(-0.5773502691896248623516308943m, performance.SharpeRatio);
            Assert.AreEqual(0, performance.SortinoRatio);
            Assert.AreEqual(-0.75m, performance.ProfitToMaxDrawdownRatio);
            Assert.AreEqual(-50, performance.MaximumEndTradeDrawdown);
            Assert.AreEqual(-31.666666666666666666666666666667m, performance.AverageEndTradeDrawdown);
            Assert.AreEqual(TimeSpan.Zero, performance.MaximumDrawdownDuration);
        }

        private IEnumerable<Trade> CreateOneWinnerTwoLosers()
        {
            var time = _startTime;

            return new List<Trade>
            {
                new Trade
                {
                    Symbol = Symbol,
                    EntryTime = time,
                    EntryPrice = 1.08m,
                    Direction = TradeDirection.Long,
                    Quantity = 1000,
                    ExitTime = time.AddMinutes(10),
                    ExitPrice = 1.09m,
                    ProfitLoss = 10,
                    MAE = -15,
                    MFE = 30
                },
                new Trade
                {
                    Symbol = Symbol,
                    EntryTime = time.AddMinutes(20),
                    EntryPrice = 1.07m,
                    Direction = TradeDirection.Short,
                    Quantity = 1000,
                    ExitTime = time.AddMinutes(40),
                    ExitPrice = 1.09m,
                    ProfitLoss = -20,
                    MAE = -30,
                    MFE = 5
                },
                new Trade
                {
                    Symbol = Symbol,
                    EntryTime = time.AddMinutes(30),
                    EntryPrice = 1.08m,
                    Direction = TradeDirection.Short,
                    Quantity = 2000,
                    ExitTime = time.AddMinutes(40),
                    ExitPrice = 1.09m,
                    ProfitLoss = -20,
                    MAE = -40,
                    MFE = 30
                }
            };
        }

        [Test]
        public void OneLoserTwoWinners()
        {
            var performance = new AlgorithmPerformance(CreateOneLoserTwoWinners());

            Assert.AreEqual(_startTime, performance.StartDateTime);
            Assert.AreEqual(_startTime.AddMinutes(40), performance.EndDateTime);
            Assert.AreEqual(3, performance.TotalNumberOfTrades);
            Assert.AreEqual(2, performance.NumberOfWinningTrades);
            Assert.AreEqual(1, performance.NumberOfLosingTrades);
            Assert.AreEqual(10, performance.TotalProfitLoss);
            Assert.AreEqual(30, performance.TotalProfit);
            Assert.AreEqual(-20, performance.TotalLoss);
            Assert.AreEqual(20, performance.LargestProfit);
            Assert.AreEqual(-20, performance.LargestLoss);
            Assert.AreEqual(3.3333333333333333333333333333m, performance.AverageProfitLoss);
            Assert.AreEqual(15, performance.AverageProfit);
            Assert.AreEqual(-20, performance.AverageLoss);
            Assert.AreEqual(TimeSpan.FromSeconds(800), performance.AverageTradeDuration);
            Assert.AreEqual(TimeSpan.FromMinutes(10), performance.AverageWinningTradeDuration);
            Assert.AreEqual(TimeSpan.FromMinutes(20), performance.AverageLosingTradeDuration);
            Assert.AreEqual(2, performance.MaxConsecutiveWinningTrades);
            Assert.AreEqual(1, performance.MaxConsecutiveLosingTrades);
            Assert.AreEqual(0.75m, performance.ProfitLossRatio);
            Assert.AreEqual(2, performance.WinLossRatio);
            Assert.AreEqual(0.6666666666666666666666666667m, performance.WinRate);
            Assert.AreEqual(0.3333333333333333333333333333m, performance.LossRate);
            Assert.AreEqual(-28.333333333333333333333333333333m, performance.AverageMAE);
            Assert.AreEqual(21.666666666666666666666666666667m, performance.AverageMFE);
            Assert.AreEqual(-40, performance.LargestMAE);
            Assert.AreEqual(30, performance.LargestMFE);
            Assert.AreEqual(-20, performance.MaximumClosedTradeDrawdown);
            Assert.AreEqual(-70, performance.MaximumIntraTradeDrawdown);
            Assert.AreEqual(20.8166599946613m, performance.ProfitLossStandardDeviation);
            Assert.AreEqual(0, performance.ProfitLossDownsideDeviation);
            Assert.AreEqual(1.5m, performance.ProfitFactor);
            Assert.AreEqual(0.1601281538050873438895842626m, performance.SharpeRatio);
            Assert.AreEqual(0, performance.SortinoRatio);
            Assert.AreEqual(0.5m, performance.ProfitToMaxDrawdownRatio);
            Assert.AreEqual(-25, performance.MaximumEndTradeDrawdown);
            Assert.AreEqual(-18.333333333333333333333333334m, performance.AverageEndTradeDrawdown);
            Assert.AreEqual(TimeSpan.FromMinutes(40), performance.MaximumDrawdownDuration);
        }

        private IEnumerable<Trade> CreateOneLoserTwoWinners()
        {
            var time = _startTime;

            return new List<Trade>
            {
                new Trade
                {
                    Symbol = Symbol,
                    EntryTime = time,
                    EntryPrice = 1.07m,
                    Direction = TradeDirection.Short,
                    Quantity = 1000,
                    ExitTime = time.AddMinutes(20),
                    ExitPrice = 1.09m,
                    ProfitLoss = -20,
                    MAE = -30,
                    MFE = 5
                },
                new Trade
                {
                    Symbol = Symbol,
                    EntryTime = time.AddMinutes(10),
                    EntryPrice = 1.08m,
                    Direction = TradeDirection.Long,
                    Quantity = 2000,
                    ExitTime = time.AddMinutes(20),
                    ExitPrice = 1.09m,
                    ProfitLoss = 20,
                    MAE = -40,
                    MFE = 30
                },
                new Trade
                {
                    Symbol = Symbol,
                    EntryTime = time.AddMinutes(30),
                    EntryPrice = 1.08m,
                    Direction = TradeDirection.Long,
                    Quantity = 1000,
                    ExitTime = time.AddMinutes(40),
                    ExitPrice = 1.09m,
                    ProfitLoss = 10,
                    MAE = -15,
                    MFE = 30
                }
            };
        }


    }
}
