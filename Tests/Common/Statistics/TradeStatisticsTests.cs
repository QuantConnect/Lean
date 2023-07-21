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
    class TradeStatisticsTests
    {
        private const decimal TradeFee = 2;
        private readonly DateTime _startTime = new DateTime(2015, 08, 06, 15, 30, 0);

        [Test]
        public void NoTrades()
        {
            var statistics = new TradeStatistics(new List<Trade>());

            Assert.AreEqual(null, statistics.StartDateTime);
            Assert.AreEqual(null, statistics.EndDateTime);
            Assert.AreEqual(0, statistics.TotalNumberOfTrades);
            Assert.AreEqual(0, statistics.NumberOfWinningTrades);
            Assert.AreEqual(0, statistics.NumberOfLosingTrades);
            Assert.AreEqual(0, statistics.TotalProfitLoss);
            Assert.AreEqual(0, statistics.TotalProfit);
            Assert.AreEqual(0, statistics.TotalLoss);
            Assert.AreEqual(0, statistics.LargestProfit);
            Assert.AreEqual(0, statistics.LargestLoss);
            Assert.AreEqual(0, statistics.AverageProfitLoss);
            Assert.AreEqual(0, statistics.AverageProfit);
            Assert.AreEqual(0, statistics.AverageLoss);
            Assert.AreEqual(TimeSpan.Zero, statistics.AverageTradeDuration);
            Assert.AreEqual(TimeSpan.Zero, statistics.AverageWinningTradeDuration);
            Assert.AreEqual(TimeSpan.Zero, statistics.AverageLosingTradeDuration);
            Assert.AreEqual(0, statistics.MaxConsecutiveWinningTrades);
            Assert.AreEqual(0, statistics.MaxConsecutiveLosingTrades);
            Assert.AreEqual(0, statistics.ProfitLossRatio);
            Assert.AreEqual(0, statistics.WinLossRatio);
            Assert.AreEqual(0, statistics.WinRate);
            Assert.AreEqual(0, statistics.LossRate);
            Assert.AreEqual(0, statistics.AverageMAE);
            Assert.AreEqual(0, statistics.AverageMFE);
            Assert.AreEqual(0, statistics.LargestMAE);
            Assert.AreEqual(0, statistics.LargestMFE);
            Assert.AreEqual(0, statistics.MaximumClosedTradeDrawdown);
            Assert.AreEqual(0, statistics.MaximumIntraTradeDrawdown);
            Assert.AreEqual(0, statistics.ProfitLossStandardDeviation);
            Assert.AreEqual(0, statistics.ProfitLossDownsideDeviation);
            Assert.AreEqual(0, statistics.ProfitFactor);
            Assert.AreEqual(0, statistics.SharpeRatio);
            Assert.AreEqual(0, statistics.SortinoRatio);
            Assert.AreEqual(0, statistics.ProfitToMaxDrawdownRatio);
            Assert.AreEqual(0, statistics.MaximumEndTradeDrawdown);
            Assert.AreEqual(0, statistics.AverageEndTradeDrawdown);
            Assert.AreEqual(TimeSpan.Zero, statistics.MaximumDrawdownDuration);
            Assert.AreEqual(0, statistics.TotalFees);
        }

        [Test]
        public void ThreeWinners()
        {
            var statistics = new TradeStatistics(CreateThreeWinners());

            Assert.AreEqual(_startTime, statistics.StartDateTime);
            Assert.AreEqual(_startTime.AddMinutes(40), statistics.EndDateTime);
            Assert.AreEqual(3, statistics.TotalNumberOfTrades);
            Assert.AreEqual(3, statistics.NumberOfWinningTrades);
            Assert.AreEqual(0, statistics.NumberOfLosingTrades);
            Assert.AreEqual(50, statistics.TotalProfitLoss);
            Assert.AreEqual(50, statistics.TotalProfit);
            Assert.AreEqual(0, statistics.TotalLoss);
            Assert.AreEqual(20, statistics.LargestProfit);
            Assert.AreEqual(0, statistics.LargestLoss);
            Assert.AreEqual(16.666666666666666666666666667m, statistics.AverageProfitLoss);
            Assert.AreEqual(16.666666666666666666666666667m, statistics.AverageProfit);
            Assert.AreEqual(0, statistics.AverageLoss);
            Assert.AreEqual(TimeSpan.FromMinutes(20), statistics.AverageTradeDuration);
            Assert.AreEqual(TimeSpan.FromMinutes(20), statistics.AverageWinningTradeDuration);
            Assert.AreEqual(TimeSpan.Zero, statistics.AverageLosingTradeDuration);
            Assert.AreEqual(3, statistics.MaxConsecutiveWinningTrades);
            Assert.AreEqual(0, statistics.MaxConsecutiveLosingTrades);
            Assert.AreEqual(0, statistics.ProfitLossRatio);
            Assert.AreEqual(10, statistics.WinLossRatio);
            Assert.AreEqual(1, statistics.WinRate);
            Assert.AreEqual(0, statistics.LossRate);
            Assert.AreEqual(-16.666666666666666666666666667m, statistics.AverageMAE);
            Assert.AreEqual(33.333333333333333333333333333m, statistics.AverageMFE);
            Assert.AreEqual(-30, statistics.LargestMAE);
            Assert.AreEqual(40, statistics.LargestMFE);
            Assert.AreEqual(0, statistics.MaximumClosedTradeDrawdown);
            Assert.AreEqual(-70, statistics.MaximumIntraTradeDrawdown);
            Assert.AreEqual(5.77350269189626m, statistics.ProfitLossStandardDeviation);
            Assert.AreEqual(0, statistics.ProfitLossDownsideDeviation);
            Assert.AreEqual(10, statistics.ProfitFactor);
            Assert.AreEqual(2.8867513459481276450914878051m, statistics.SharpeRatio);
            Assert.AreEqual(0, statistics.SortinoRatio);
            Assert.AreEqual(10, statistics.ProfitToMaxDrawdownRatio);
            Assert.AreEqual(-20, statistics.MaximumEndTradeDrawdown);
            Assert.AreEqual(-16.666666666666666666666666666m, statistics.AverageEndTradeDrawdown);
            Assert.AreEqual(TimeSpan.Zero, statistics.MaximumDrawdownDuration);
            Assert.AreEqual(6, statistics.TotalFees);
        }

        private IEnumerable<Trade> CreateThreeWinners()
        {
            var time = _startTime;

            return new List<Trade>
            {
                new Trade
                {
                    Symbol = Symbols.EURUSD,
                    EntryTime = time,
                    EntryPrice = 1.07m,
                    Direction = TradeDirection.Long,
                    Quantity = 1000,
                    ExitTime = time.AddMinutes(20),
                    ExitPrice = 1.09m,
                    ProfitLoss = 20,
                    TotalFees = TradeFee,
                    MAE = -5,
                    MFE = 30
                },
                new Trade
                {
                    Symbol = Symbols.EURUSD,
                    EntryTime = time.AddMinutes(10),
                    EntryPrice = 1.08m,
                    Direction = TradeDirection.Long,
                    Quantity = 2000,
                    ExitTime = time.AddMinutes(40),
                    ExitPrice = 1.09m,
                    ProfitLoss = 20,
                    TotalFees = TradeFee,
                    MAE = -30,
                    MFE = 40
                },
                new Trade
                {
                    Symbol = Symbols.EURUSD,
                    EntryTime = time.AddMinutes(30),
                    EntryPrice = 1.08m,
                    Direction = TradeDirection.Long,
                    Quantity = 1000,
                    ExitTime = time.AddMinutes(40),
                    ExitPrice = 1.09m,
                    ProfitLoss = 10,
                    TotalFees = TradeFee,
                    MAE = -15,
                    MFE = 30
                }
            };
        }

        [Test]
        public void ThreeLosers()
        {
            var statistics = new TradeStatistics(CreateThreeLosers());

            Assert.AreEqual(_startTime, statistics.StartDateTime);
            Assert.AreEqual(_startTime.AddMinutes(40), statistics.EndDateTime);
            Assert.AreEqual(3, statistics.TotalNumberOfTrades);
            Assert.AreEqual(0, statistics.NumberOfWinningTrades);
            Assert.AreEqual(3, statistics.NumberOfLosingTrades);
            Assert.AreEqual(-50, statistics.TotalProfitLoss);
            Assert.AreEqual(0, statistics.TotalProfit);
            Assert.AreEqual(-50, statistics.TotalLoss);
            Assert.AreEqual(0, statistics.LargestProfit);
            Assert.AreEqual(-20, statistics.LargestLoss);
            Assert.AreEqual(-16.666666666666666666666666667m, statistics.AverageProfitLoss);
            Assert.AreEqual(0, statistics.AverageProfit);
            Assert.AreEqual(-16.666666666666666666666666667m, statistics.AverageLoss);
            Assert.AreEqual(TimeSpan.FromMinutes(20), statistics.AverageTradeDuration);
            Assert.AreEqual(TimeSpan.Zero, statistics.AverageWinningTradeDuration);
            Assert.AreEqual(TimeSpan.FromMinutes(20), statistics.AverageLosingTradeDuration);
            Assert.AreEqual(0, statistics.MaxConsecutiveWinningTrades);
            Assert.AreEqual(3, statistics.MaxConsecutiveLosingTrades);
            Assert.AreEqual(0, statistics.ProfitLossRatio);
            Assert.AreEqual(0, statistics.WinLossRatio);
            Assert.AreEqual(0, statistics.WinRate);
            Assert.AreEqual(1, statistics.LossRate);
            Assert.AreEqual(-33.333333333333333333333333333m, statistics.AverageMAE);
            Assert.AreEqual(16.666666666666666666666666667m, statistics.AverageMFE);
            Assert.AreEqual(-40, statistics.LargestMAE);
            Assert.AreEqual(30, statistics.LargestMFE);
            Assert.AreEqual(-50, statistics.MaximumClosedTradeDrawdown);
            Assert.AreEqual(-80, statistics.MaximumIntraTradeDrawdown);
            Assert.AreEqual(5.77350269189626m, statistics.ProfitLossStandardDeviation);
            Assert.AreEqual(5.77350269189626m, statistics.ProfitLossDownsideDeviation);
            Assert.AreEqual(0, statistics.ProfitFactor);
            Assert.AreEqual(-2.8867513459481276450914878051m, statistics.SharpeRatio);
            Assert.AreEqual(-2.8867513459481276450914878051m, statistics.SortinoRatio);
            Assert.AreEqual(-1, statistics.ProfitToMaxDrawdownRatio);
            Assert.AreEqual(-50, statistics.MaximumEndTradeDrawdown);
            Assert.AreEqual(-33.333333333333333333333333334m, statistics.AverageEndTradeDrawdown);
            Assert.AreEqual(TimeSpan.Zero, statistics.MaximumDrawdownDuration);
            Assert.AreEqual(6, statistics.TotalFees);
        }

        private IEnumerable<Trade> CreateThreeLosers()
        {
            var time = _startTime;

            return new List<Trade>
            {
                new Trade
                {
                    Symbol = Symbols.EURUSD,
                    EntryTime = time,
                    EntryPrice = 1.07m,
                    Direction = TradeDirection.Short,
                    Quantity = 1000,
                    ExitTime = time.AddMinutes(20),
                    ExitPrice = 1.09m,
                    ProfitLoss = -20,
                    TotalFees = TradeFee,
                    MAE = -30,
                    MFE = 5
                },
                new Trade
                {
                    Symbol = Symbols.EURUSD,
                    EntryTime = time.AddMinutes(10),
                    EntryPrice = 1.08m,
                    Direction = TradeDirection.Short,
                    Quantity = 2000,
                    ExitTime = time.AddMinutes(40),
                    ExitPrice = 1.09m,
                    ProfitLoss = -20,
                    TotalFees = TradeFee,
                    MAE = -40,
                    MFE = 30
                },
                new Trade
                {
                    Symbol = Symbols.EURUSD,
                    EntryTime = time.AddMinutes(30),
                    EntryPrice = 1.08m,
                    Direction = TradeDirection.Short,
                    Quantity = 1000,
                    ExitTime = time.AddMinutes(40),
                    ExitPrice = 1.09m,
                    ProfitLoss = -10,
                    TotalFees = TradeFee,
                    MAE = -30,
                    MFE = 15
                }
            };
        }

        [Test]
        public void TwoLosersOneWinner()
        {
            var statistics = new TradeStatistics(CreateTwoLosersOneWinner());

            Assert.AreEqual(_startTime, statistics.StartDateTime);
            Assert.AreEqual(_startTime.AddMinutes(40), statistics.EndDateTime);
            Assert.AreEqual(3, statistics.TotalNumberOfTrades);
            Assert.AreEqual(1, statistics.NumberOfWinningTrades);
            Assert.AreEqual(2, statistics.NumberOfLosingTrades);
            Assert.AreEqual(-30, statistics.TotalProfitLoss);
            Assert.AreEqual(10, statistics.TotalProfit);
            Assert.AreEqual(-40, statistics.TotalLoss);
            Assert.AreEqual(10, statistics.LargestProfit);
            Assert.AreEqual(-20, statistics.LargestLoss);
            Assert.AreEqual(-10, statistics.AverageProfitLoss);
            Assert.AreEqual(10, statistics.AverageProfit);
            Assert.AreEqual(-20, statistics.AverageLoss);
            Assert.AreEqual(TimeSpan.FromSeconds(800), statistics.AverageTradeDuration);
            Assert.AreEqual(TimeSpan.FromMinutes(10), statistics.AverageWinningTradeDuration);
            Assert.AreEqual(TimeSpan.FromMinutes(15), statistics.AverageLosingTradeDuration);
            Assert.AreEqual(1, statistics.MaxConsecutiveWinningTrades);
            Assert.AreEqual(2, statistics.MaxConsecutiveLosingTrades);
            Assert.AreEqual(0.5m, statistics.ProfitLossRatio);
            Assert.AreEqual(0.5m, statistics.WinLossRatio);
            Assert.AreEqual(0.3333333333333333333333333333m, statistics.WinRate);
            Assert.AreEqual(0.6666666666666666666666666667m, statistics.LossRate);
            Assert.AreEqual(-28.333333333333333333333333333333m, statistics.AverageMAE);
            Assert.AreEqual(21.666666666666666666666666666667m, statistics.AverageMFE);
            Assert.AreEqual(-40, statistics.LargestMAE);
            Assert.AreEqual(30, statistics.LargestMFE);
            Assert.AreEqual(-40, statistics.MaximumClosedTradeDrawdown);
            Assert.AreEqual(-70, statistics.MaximumIntraTradeDrawdown);
            Assert.AreEqual(17.3205080756888m, statistics.ProfitLossStandardDeviation);
            Assert.AreEqual(0, statistics.ProfitLossDownsideDeviation);
            Assert.AreEqual(0.25m, statistics.ProfitFactor);
            Assert.AreEqual(-0.5773502691896248623516308943m, statistics.SharpeRatio);
            Assert.AreEqual(0, statistics.SortinoRatio);
            Assert.AreEqual(-0.75m, statistics.ProfitToMaxDrawdownRatio);
            Assert.AreEqual(-50, statistics.MaximumEndTradeDrawdown);
            Assert.AreEqual(-31.666666666666666666666666666667m, statistics.AverageEndTradeDrawdown);
            Assert.AreEqual(TimeSpan.Zero, statistics.MaximumDrawdownDuration);
            Assert.AreEqual(6, statistics.TotalFees);
        }

        private IEnumerable<Trade> CreateTwoLosersOneWinner()
        {
            var time = _startTime;

            return new List<Trade>
            {
                new Trade
                {
                    Symbol = Symbols.EURUSD,
                    EntryTime = time,
                    EntryPrice = 1.07m,
                    Direction = TradeDirection.Short,
                    Quantity = 1000,
                    ExitTime = time.AddMinutes(20),
                    ExitPrice = 1.09m,
                    ProfitLoss = -20,
                    TotalFees = TradeFee,
                    MAE = -30,
                    MFE = 5
                },
                new Trade
                {
                    Symbol = Symbols.EURUSD,
                    EntryTime = time.AddMinutes(10),
                    EntryPrice = 1.08m,
                    Direction = TradeDirection.Short,
                    Quantity = 2000,
                    ExitTime = time.AddMinutes(20),
                    ExitPrice = 1.09m,
                    ProfitLoss = -20,
                    TotalFees = TradeFee,
                    MAE = -40,
                    MFE = 30
                },
                new Trade
                {
                    Symbol = Symbols.EURUSD,
                    EntryTime = time.AddMinutes(30),
                    EntryPrice = 1.08m,
                    Direction = TradeDirection.Long,
                    Quantity = 1000,
                    ExitTime = time.AddMinutes(40),
                    ExitPrice = 1.09m,
                    ProfitLoss = 10,
                    TotalFees = TradeFee,
                    MAE = -15,
                    MFE = 30
                }
            };
        }

        [Test]
        public void OneWinnerTwoLosers()
        {
            var statistics = new TradeStatistics(CreateOneWinnerTwoLosers());

            Assert.AreEqual(_startTime, statistics.StartDateTime);
            Assert.AreEqual(_startTime.AddMinutes(40), statistics.EndDateTime);
            Assert.AreEqual(3, statistics.TotalNumberOfTrades);
            Assert.AreEqual(1, statistics.NumberOfWinningTrades);
            Assert.AreEqual(2, statistics.NumberOfLosingTrades);
            Assert.AreEqual(-30, statistics.TotalProfitLoss);
            Assert.AreEqual(10, statistics.TotalProfit);
            Assert.AreEqual(-40, statistics.TotalLoss);
            Assert.AreEqual(10, statistics.LargestProfit);
            Assert.AreEqual(-20, statistics.LargestLoss);
            Assert.AreEqual(-10, statistics.AverageProfitLoss);
            Assert.AreEqual(10, statistics.AverageProfit);
            Assert.AreEqual(-20, statistics.AverageLoss);
            Assert.AreEqual(TimeSpan.FromSeconds(800), statistics.AverageTradeDuration);
            Assert.AreEqual(TimeSpan.FromMinutes(10), statistics.AverageWinningTradeDuration);
            Assert.AreEqual(TimeSpan.FromMinutes(15), statistics.AverageLosingTradeDuration);
            Assert.AreEqual(1, statistics.MaxConsecutiveWinningTrades);
            Assert.AreEqual(2, statistics.MaxConsecutiveLosingTrades);
            Assert.AreEqual(0.5m, statistics.ProfitLossRatio);
            Assert.AreEqual(0.5m, statistics.WinLossRatio);
            Assert.AreEqual(0.3333333333333333333333333333m, statistics.WinRate);
            Assert.AreEqual(0.6666666666666666666666666667m, statistics.LossRate);
            Assert.AreEqual(-28.333333333333333333333333333333m, statistics.AverageMAE);
            Assert.AreEqual(21.666666666666666666666666666667m, statistics.AverageMFE);
            Assert.AreEqual(-40, statistics.LargestMAE);
            Assert.AreEqual(30, statistics.LargestMFE);
            Assert.AreEqual(-40, statistics.MaximumClosedTradeDrawdown);
            Assert.AreEqual(-80, statistics.MaximumIntraTradeDrawdown);
            Assert.AreEqual(17.3205080756888m, statistics.ProfitLossStandardDeviation);
            Assert.AreEqual(0, statistics.ProfitLossDownsideDeviation);
            Assert.AreEqual(0.25m, statistics.ProfitFactor);
            Assert.AreEqual(-0.5773502691896248623516308943m, statistics.SharpeRatio);
            Assert.AreEqual(0, statistics.SortinoRatio);
            Assert.AreEqual(-0.75m, statistics.ProfitToMaxDrawdownRatio);
            Assert.AreEqual(-50, statistics.MaximumEndTradeDrawdown);
            Assert.AreEqual(-31.666666666666666666666666666667m, statistics.AverageEndTradeDrawdown);
            Assert.AreEqual(TimeSpan.Zero, statistics.MaximumDrawdownDuration);
            Assert.AreEqual(6, statistics.TotalFees);
        }

        private IEnumerable<Trade> CreateOneWinnerTwoLosers()
        {
            var time = _startTime;

            return new List<Trade>
            {
                new Trade
                {
                    Symbol = Symbols.EURUSD,
                    EntryTime = time,
                    EntryPrice = 1.08m,
                    Direction = TradeDirection.Long,
                    Quantity = 1000,
                    ExitTime = time.AddMinutes(10),
                    ExitPrice = 1.09m,
                    ProfitLoss = 10,
                    TotalFees = TradeFee,
                    MAE = -15,
                    MFE = 30
                },
                new Trade
                {
                    Symbol = Symbols.EURUSD,
                    EntryTime = time.AddMinutes(20),
                    EntryPrice = 1.07m,
                    Direction = TradeDirection.Short,
                    Quantity = 1000,
                    ExitTime = time.AddMinutes(40),
                    ExitPrice = 1.09m,
                    ProfitLoss = -20,
                    TotalFees = TradeFee,
                    MAE = -30,
                    MFE = 5
                },
                new Trade
                {
                    Symbol = Symbols.EURUSD,
                    EntryTime = time.AddMinutes(30),
                    EntryPrice = 1.08m,
                    Direction = TradeDirection.Short,
                    Quantity = 2000,
                    ExitTime = time.AddMinutes(40),
                    ExitPrice = 1.09m,
                    ProfitLoss = -20,
                    TotalFees = TradeFee,
                    MAE = -40,
                    MFE = 30
                }
            };
        }

        [Test]
        public void OneLoserTwoWinners()
        {
            var statistics = new TradeStatistics(CreateOneLoserTwoWinners());

            Assert.AreEqual(_startTime, statistics.StartDateTime);
            Assert.AreEqual(_startTime.AddMinutes(40), statistics.EndDateTime);
            Assert.AreEqual(3, statistics.TotalNumberOfTrades);
            Assert.AreEqual(2, statistics.NumberOfWinningTrades);
            Assert.AreEqual(1, statistics.NumberOfLosingTrades);
            Assert.AreEqual(10, statistics.TotalProfitLoss);
            Assert.AreEqual(30, statistics.TotalProfit);
            Assert.AreEqual(-20, statistics.TotalLoss);
            Assert.AreEqual(20, statistics.LargestProfit);
            Assert.AreEqual(-20, statistics.LargestLoss);
            Assert.AreEqual(3.3333333333333333333333333333m, statistics.AverageProfitLoss);
            Assert.AreEqual(15, statistics.AverageProfit);
            Assert.AreEqual(-20, statistics.AverageLoss);
            Assert.AreEqual(TimeSpan.FromSeconds(800), statistics.AverageTradeDuration);
            Assert.AreEqual(TimeSpan.FromMinutes(10), statistics.AverageWinningTradeDuration);
            Assert.AreEqual(TimeSpan.FromMinutes(20), statistics.AverageLosingTradeDuration);
            Assert.AreEqual(2, statistics.MaxConsecutiveWinningTrades);
            Assert.AreEqual(1, statistics.MaxConsecutiveLosingTrades);
            Assert.AreEqual(0.75m, statistics.ProfitLossRatio);
            Assert.AreEqual(2, statistics.WinLossRatio);
            Assert.AreEqual(0.6666666666666666666666666667m, statistics.WinRate);
            Assert.AreEqual(0.3333333333333333333333333333m, statistics.LossRate);
            Assert.AreEqual(-28.333333333333333333333333333333m, statistics.AverageMAE);
            Assert.AreEqual(21.666666666666666666666666666667m, statistics.AverageMFE);
            Assert.AreEqual(-40, statistics.LargestMAE);
            Assert.AreEqual(30, statistics.LargestMFE);
            Assert.AreEqual(-20, statistics.MaximumClosedTradeDrawdown);
            Assert.AreEqual(-70, statistics.MaximumIntraTradeDrawdown);
            Assert.AreEqual(20.8166599946613m, statistics.ProfitLossStandardDeviation);
            Assert.AreEqual(0, statistics.ProfitLossDownsideDeviation);
            Assert.AreEqual(1.5m, statistics.ProfitFactor);
            Assert.AreEqual(0.1601281538050873438895842626m, statistics.SharpeRatio);
            Assert.AreEqual(0, statistics.SortinoRatio);
            Assert.AreEqual(0.5m, statistics.ProfitToMaxDrawdownRatio);
            Assert.AreEqual(-25, statistics.MaximumEndTradeDrawdown);
            Assert.AreEqual(-18.333333333333333333333333334m, statistics.AverageEndTradeDrawdown);
            Assert.AreEqual(TimeSpan.FromMinutes(40), statistics.MaximumDrawdownDuration);
            Assert.AreEqual(6, statistics.TotalFees);
        }

        private IEnumerable<Trade> CreateOneLoserTwoWinners()
        {
            var time = _startTime;

            return new List<Trade>
            {
                new Trade
                {
                    Symbol = Symbols.EURUSD,
                    EntryTime = time,
                    EntryPrice = 1.07m,
                    Direction = TradeDirection.Short,
                    Quantity = 1000,
                    ExitTime = time.AddMinutes(20),
                    ExitPrice = 1.09m,
                    ProfitLoss = -20,
                    TotalFees = TradeFee,
                    MAE = -30,
                    MFE = 5
                },
                new Trade
                {
                    Symbol = Symbols.EURUSD,
                    EntryTime = time.AddMinutes(10),
                    EntryPrice = 1.08m,
                    Direction = TradeDirection.Long,
                    Quantity = 2000,
                    ExitTime = time.AddMinutes(20),
                    ExitPrice = 1.09m,
                    ProfitLoss = 20,
                    TotalFees = TradeFee,
                    MAE = -40,
                    MFE = 30
                },
                new Trade
                {
                    Symbol = Symbols.EURUSD,
                    EntryTime = time.AddMinutes(30),
                    EntryPrice = 1.08m,
                    Direction = TradeDirection.Long,
                    Quantity = 1000,
                    ExitTime = time.AddMinutes(40),
                    ExitPrice = 1.09m,
                    ProfitLoss = 10,
                    TotalFees = TradeFee,
                    MAE = -15,
                    MFE = 30
                }
            };
        }

        [Test]
        public void ITMOptionAssignment([Values] bool win)
        {
            var statistics = new TradeStatistics(CreateITMOptionAssignment(win));

            if (win)
            {
                Assert.AreEqual(2, statistics.NumberOfWinningTrades);
                Assert.AreEqual(0, statistics.NumberOfLosingTrades);
                Assert.AreEqual(2, statistics.MaxConsecutiveWinningTrades);
                Assert.AreEqual(0, statistics.MaxConsecutiveLosingTrades);
                Assert.AreEqual(10, statistics.WinLossRatio);
                Assert.AreEqual(1m, statistics.WinRate);
                Assert.AreEqual(0m, statistics.LossRate);
            }
            else
            {
                Assert.AreEqual(1, statistics.NumberOfWinningTrades);
                Assert.AreEqual(1, statistics.NumberOfLosingTrades);
                Assert.AreEqual(1, statistics.MaxConsecutiveWinningTrades);
                Assert.AreEqual(1, statistics.MaxConsecutiveLosingTrades);
                Assert.AreEqual(1m, statistics.WinLossRatio);
                Assert.AreEqual(0.5m, statistics.WinRate);
                Assert.AreEqual(0.5m, statistics.LossRate);
            }

            Assert.AreEqual(_startTime, statistics.StartDateTime);
            Assert.AreEqual(_startTime.AddMinutes(30), statistics.EndDateTime);
            Assert.AreEqual(2, statistics.TotalNumberOfTrades);
            Assert.AreEqual(28000m, statistics.TotalProfitLoss);
            Assert.AreEqual(108000m, statistics.TotalProfit);
            Assert.AreEqual(-80000m, statistics.TotalLoss);
            Assert.AreEqual(108000m, statistics.LargestProfit);
            Assert.AreEqual(-80000m, statistics.LargestLoss);
            Assert.AreEqual(14000m, statistics.AverageProfitLoss);
            Assert.AreEqual(108000m, statistics.AverageProfit);
            Assert.AreEqual(-80000m, statistics.AverageLoss);
            Assert.AreEqual(TimeSpan.FromMinutes(15), statistics.AverageTradeDuration);
            Assert.AreEqual(TimeSpan.FromMinutes(10), statistics.AverageWinningTradeDuration);
            Assert.AreEqual(TimeSpan.FromMinutes(20), statistics.AverageLosingTradeDuration);
            Assert.AreEqual(1.35m, statistics.ProfitLossRatio);
            Assert.AreEqual(-40000m, statistics.AverageMAE);
            Assert.AreEqual(54000m, statistics.AverageMFE);
            Assert.AreEqual(-80000, statistics.LargestMAE);
            Assert.AreEqual(108000, statistics.LargestMFE);
            Assert.AreEqual(-80000, statistics.MaximumClosedTradeDrawdown);
            Assert.AreEqual(-108000, statistics.MaximumIntraTradeDrawdown);
            Assert.AreEqual(132936.074863071m, statistics.ProfitLossStandardDeviation);
            Assert.AreEqual(0m, statistics.ProfitLossDownsideDeviation);
            Assert.AreEqual(1.35m, statistics.ProfitFactor);
            Assert.AreEqual(0.1053137759214006433027413265m, statistics.SharpeRatio);
            Assert.AreEqual(0m, statistics.SortinoRatio);
            Assert.AreEqual(0.35m, statistics.ProfitToMaxDrawdownRatio);
            Assert.AreEqual(-80000, statistics.MaximumEndTradeDrawdown);
            Assert.AreEqual(-40000m, statistics.AverageEndTradeDrawdown);
            Assert.AreEqual(TimeSpan.FromMinutes(30), statistics.MaximumDrawdownDuration);
            Assert.AreEqual(4, statistics.TotalFees);
        }

        private IEnumerable<Trade> CreateITMOptionAssignment(bool win)
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
                    ProfitLoss = -80000m,
                    TotalFees = TradeFee,
                    MAE = -80000m,
                    MFE = 0,
                    IsWin = win,
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
                    ProfitLoss = 108000m,
                    TotalFees = TradeFee,
                    MAE = 0,
                    MFE = 108000m,
                    IsWin = true,
                },
            };
        }
    }
}
